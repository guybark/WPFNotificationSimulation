using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace WPFNotificationSimulation
{
    public partial class MainWindow : Window
    {
        private string notificationGuid = "5A5CA7F5-5683-4021-9821-B581DA0B3F26";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InputWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var input = InputWindowTB.Text.Trim();
                if (input.Length > 0)
                {
                    var response = "This is the response to " + input;
                    ImmediateWindowTB.RaiseNotificationEvent(
                        response,
                        notificationGuid);

                    ImmediateWindowTB.Text += response + "\r\n";
                    InputWindowTB.Text = "";
                }

                e.Handled = true;
            }
        }
    }

    public class NotificationTextBlock : TextBlock
    {
        // This control's AutomationPeer is the object that actually raises the UIA Notification event.
        private NotificationTextBlockAutomationPeer _peer;

        // Assume the UIA Notification event is available until we learn otherwise.
        // If we learn that the UIA Notification event is not available, no instance
        // of the NotificationTextBlock should attempt to raise it.
        static private bool _notificationEventAvailable = true;

        public bool NotificationEventAvailable
        {
            get
            {
                return _notificationEventAvailable;
            }
            set
            {
                _notificationEventAvailable = value;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            this._peer = new NotificationTextBlockAutomationPeer(this);

            return this._peer;
        }

        public void RaiseNotificationEvent(string notificationText, string notificationGuid)
        {
            // Only attempt to raise the event if we already have an AutomationPeer.
            if (this._peer != null)
            {
                this._peer.RaiseNotificationEvent(notificationText, notificationGuid);
            }
        }
    }

    internal class NotificationTextBlockAutomationPeer : TextBlockAutomationPeer
    {
        private NotificationTextBlock _notificationTextBlock;

        // The UIA Notification event requires the IRawElementProviderSimple
        // associated with this AutomationPeer.
        private IRawElementProviderSimple _reps;

        public NotificationTextBlockAutomationPeer(NotificationTextBlock owner) : base(owner)
        {
            this._notificationTextBlock = owner;
        }

        public void RaiseNotificationEvent(string notificationText, string notificationGuid)
        {
            // If we already know that the UIA Notification event is not available, do not
            // attempt to raise it.
            if (this._notificationTextBlock.NotificationEventAvailable)
            {
                // If no UIA clients are listening for events, don't bother raising one.
                if (NativeMethods.UiaClientsAreListening())
                {
                    // Get the IRawElementProviderSimple for this AutomationPeer if we don't
                    // have it already.
                    if (this._reps == null)
                    {
                        AutomationPeer peer = FrameworkElementAutomationPeer.FromElement(this._notificationTextBlock);
                        if (peer != null)
                        {
                            this._reps = ProviderFromPeer(peer);
                        }
                    }

                    if (this._reps != null)
                    {
                        try
                        {
                            // Todo: The NotificationKind and NotificationProcessing values shown here
                            // are sample values for this snippet. You should use whatever values are
                            // appropriate for your scenarios.

                            NativeMethods.UiaRaiseNotificationEvent(
                                this._reps,
                                NativeMethods.AutomationNotificationKind.ActionCompleted,
                                NativeMethods.AutomationNotificationProcessing.ImportantMostRecent,
                                notificationText,
                                notificationGuid);
                        }
                        catch (EntryPointNotFoundException)
                        {
                            // The UIA Notification event is not not available, so don't attempt
                            // to raise it again.
                            _notificationTextBlock.NotificationEventAvailable = false;
                        }
                    }
                }
            }
        }

        internal class NativeMethods
        {
            public enum AutomationNotificationKind
            {
                ItemAdded = 0,
                ItemRemoved = 1,
                ActionCompleted = 2,
                ActionAborted = 3,
                Other = 4
            }

            public enum AutomationNotificationProcessing
            {
                ImportantAll = 0,
                ImportantMostRecent = 1,
                All = 2,
                MostRecent = 3,
                CurrentThenMostRecent = 4
            }

            // Add a reference to UIAutomationProvider.
            [DllImport("UIAutomationCore.dll", CharSet = CharSet.Unicode)]
            public static extern int UiaRaiseNotificationEvent(
                IRawElementProviderSimple provider,
                AutomationNotificationKind notificationKind,
                AutomationNotificationProcessing notificationProcessing,
                string notificationText,
                string notificationGuid);

            [DllImport("UIAutomationCore.dll")]
            public static extern bool UiaClientsAreListening();
        }
    }
}
