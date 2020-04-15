using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;


namespace DesktopToastsSample
{
	public partial class DesktopToastsApp : Form
	{
		private Button btn;
		private TextBox edit;

		public DesktopToastsApp()
		{
			InitializeComponent();
			Controls.Add(btn = new Button { Text = "View Text Toast", Bounds = new Rectangle(0, 0, 150, 25) });
			btn.Click += DisplayToast;
			Controls.Add(edit = new TextBox { Multiline = true, Text = "Whatever action you take on the displayed toast will be shown here.", Bounds = new Rectangle(0, 20, 300, 50) });
		}

		private void SetMessage(string message)
		{
			Activate();
			edit.Text = message;
		}

		private static void RegisterAppForNotificationSupport()
		{
			var shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Desktop Toasts App.lnk");
			if (!File.Exists(shortcutPath))
			{
				var exePath = Process.GetCurrentProcess().MainModule.FileName;
				InstallShortcut(shortcutPath, exePath);
				RegisterComServer(exePath);
			}
		}

		private static void RegisterComServer(string exePath)
		{
			using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\CLSID\{23A5B06E-20BB-4E7E-A0AC-6982ED6A6041}\LocalServer32"))
				key.SetValue(null, exePath);
		}

		private static void InstallShortcut(string shortcutPath, string exePath)
		{
			var shellLink = ComReleaserFactory.Create(new IShellLinkW());
			shellLink.Item.SetPath(exePath);
			var propertyStore = (IPropertyStore)shellLink;
			propertyStore.SetValue(PROPERTYKEY.System.AppUserModel.ID, new PROPVARIANT(AppId));
			propertyStore.SetValue(PROPERTYKEY.System.AppUserModel.ToastActivatorCLSID, new PROPVARIANT(typeof(NotificationActivator).GUID));
			propertyStore.Commit();
			((IPersistFile)shellLink).Save(shortcutPath, true);
		}

		private void DisplayToast(object sender, EventArgs e)
		{
			// Get a toast XML template
			XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);

			// Fill in the text elements
			XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
			for (int i = 0; i < stringElements.Length; i++)
			{
				stringElements[i].AppendChild(toastXml.CreateTextNode("Line " + i));
			}

			// Specify the absolute path to an image as a URI
			String imagePath = new System.Uri(Path.GetFullPath("toastImageAndText.png")).AbsoluteUri;
			XmlNodeList imageElements = toastXml.GetElementsByTagName("image");
			imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;

			// Create the toast and attach event listeners
			ToastNotification toast = new ToastNotification(toastXml);
			toast.Activated += ToastActivated;
			toast.Dismissed += ToastDismissed;
			toast.Failed += ToastFailed;

			// Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
			ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
		}

		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			if (RegisterAppForNotificationSupport().Succeeded)
			{
				Application.Run(new DesktopToastsApp());
				RegisterActivator();
			}
		}

		private void ToastActivated(ToastNotification sender, object e)
		{
			ToastActivated();
		}

		private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
		{
			String outputText = "";
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

		private static void RegisterActivator() => throw new NotImplementedException();

		private static void UnregisterActivator() => throw new NotImplementedException();
	}
}
