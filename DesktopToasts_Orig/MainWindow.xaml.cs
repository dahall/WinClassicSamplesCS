using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Windows;
using System.Xml;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;

namespace DesktopToastsSample
{
    public partial class MainWindow : Window
    {
        private const string APP_ID = "Microsoft.Samples.DesktopToasts";

        public MainWindow()
        {
            InitializeComponent();
            RegisterAppForNotificationSupport();

            NotificationActivator.Initialize();
            ShowToastButton.Click += ShowToastButton_Click;
            this.Closing += CloseMainWindow;
        }

        public void ToastActivated()
        {
            Dispatcher.Invoke(() =>
            {
                Activate();
                Output.Text = "The user activated the toast.";
            });
        }

        private void CloseMainWindow(object sender, CancelEventArgs e)
        {
            NotificationActivator.Uninitialize();
        }

        // In order to display toasts, a desktop application must have a shortcut on the Start menu.
        // Also, an AppUserModelID must be set on that shortcut.
        //
        // For the app to be activated from Action Center, it needs to register a COM server with the OS
        // and register the CLSID of that COM server on the shortcut.
        //
        // The shortcut should be created as part of the installer. The following code shows how to create
        // a shortcut and assign the AppUserModelID and ToastActivatorCLSID properties using Windows APIs.
        //
        // Included in this project is a wxs file that be used with the WiX toolkit
        // to make an installer that creates the necessary shortcut. One or the other should be used.
        //
        // This sample doesn't clean up the shortcut or COM registration.

        private void RegisterAppForNotificationSupport()
        {
            string shortcutPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Microsoft\\Windows\\Start Menu\\Programs\\Nitro Desktop Toasts Sample CS.lnk";
            if (!File.Exists(shortcutPath))
            {
                // Find the path to the current executable
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                InstallShortcut(shortcutPath, exePath);
                RegisterComServer(exePath);
            }
        }

        private void InstallShortcut(string shortcutPath, string exePath)
        {
            IShellLinkW newShortcut = new IShellLinkW();

            // Create a shortcut to the exe
            newShortcut.SetPath(exePath);

            // Open the shortcut property store, set the AppUserModelId property
            IPropertyStore newShortcutProperties = (IPropertyStore)newShortcut;

            var varAppId = new PROPVARIANT(APP_ID);
            newShortcutProperties.SetValue(PROPERTYKEY.System.AppUserModel.ID, varAppId);

            var varToastId = new PROPVARIANT(typeof(NotificationActivator).GUID);
            newShortcutProperties.SetValue(PROPERTYKEY.System.AppUserModel.ToastActivatorCLSID, varToastId);

            // Commit the shortcut to disk
            var newShortcutSave = (IPersistFile)newShortcut;
            newShortcutSave.Save(shortcutPath, true);
        }

        private void RegisterComServer(string exePath)
        {
            // We register the app process itself to start up when the notification is activated, but
            // other options like launching a background process instead that then decides to launch
            // the UI as needed.
            string regString = string.Format("SOFTWARE\\Classes\\CLSID\\{{{0}}}\\LocalServer32", typeof(NotificationActivator).GUID);
            var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regString);
            key.SetValue(null, exePath);
        }

        // Create and show the toast.
        // See the "Toasts" sample for more detail on what can be done with toasts
        private void ShowToastButton_Click(object sender, RoutedEventArgs e)
        {
            // Get a toast XML template
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);

            // Fill in the text elements
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            for (int i = 0; i < stringElements.Count; i++)
            {
                stringElements[i].AppendChild(toastXml.CreateTextNode("Line " + i));
            }

            // Specify the absolute path to an image as a URI
            string imagePath = new System.Uri(Path.GetFullPath("toastImageAndText.png")).AbsoluteUri;
            XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
            imageElements[0].Attributes.GetNamedItem("src").Value = imagePath;

            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);
            toast.Activated += ToastActivated;
            toast.Dismissed += ToastDismissed;
            toast.Failed += ToastFailed;

            // Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
        }

        private void ToastActivated(ToastNotification sender, object e)
        {
            ToastActivated();
        }

        private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
        {
            string outputText = "";
            switch (e.Reason)
            {
                case ToastDismissalReason.ApplicationHidden:
                    outputText = "The app hid the toast using ToastNotifier.Hide";
                    break;
                case ToastDismissalReason.UserCanceled:
                    outputText = "The user dismissed the toast";
                    break;
                case ToastDismissalReason.TimedOut:
                    outputText = "The toast has timed out";
                    break;
            }

            Dispatcher.Invoke(() =>
            {
                Output.Text = outputText;
            });
        }

        private void ToastFailed(ToastNotification sender, ToastFailedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                Output.Text = "The toast encountered an error.";
            });
        }
    }
}
