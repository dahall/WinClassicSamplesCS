using System;
using System.IO;
using System.Windows.Forms;
using Windows.UI.Notifications;

namespace DesktopToastsSample
{
	public partial class DesktopToastsApp : Form
	{
		private const string APP_ID = "Microsoft.Samples.DesktopToasts";

		public DesktopToastsApp()
		{
			InitializeComponent();
		}

		public void ToastActivated() => DisplayToastStatus("The user activated the toast.");

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			NotificationActivator.Initialize(APP_ID);
			try
			{
				Application.Run(new DesktopToastsApp());
			}
			finally
			{
				NotificationActivator.Uninitialize();
			}
		}

		private void DisplayToast(object sender, EventArgs e)
		{
			// Get a toast XML template
			var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastImageAndText04);

			// Fill in the text elements
			var stringElements = toastXml.GetElementsByTagName("text");
			for (var i = 0; i < stringElements.Length; i++)
			{
				stringElements[i].AppendChild(toastXml.CreateTextNode("Line " + i));
			}

			// Specify the absolute path to an image as a URI
			var imagePath = new System.Uri(Path.GetFullPath("toastImageAndText.png")).AbsoluteUri;
			var imageElements = toastXml.GetElementsByTagName("image");
			imageElements[0].Attributes.GetNamedItem("src").NodeValue = imagePath;

			// Create the toast and attach event listeners
			var toast = new ToastNotification(toastXml);
			toast.Activated += ToastActivated;
			toast.Dismissed += ToastDismissed;
			toast.Failed += ToastFailed;

			// Show the toast. Be sure to specify the AppUserModelId on your application's shortcut!
			ToastNotificationManager.CreateToastNotifier(APP_ID).Show(toast);
		}

		private void ToastActivated(ToastNotification sender, object e) => ToastActivated();

		private void ToastDismissed(ToastNotification sender, ToastDismissedEventArgs e)
		{
			var outputText = "";
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

			DisplayToastStatus(outputText);
		}

		private void ToastFailed(ToastNotification sender, ToastFailedEventArgs e) => DisplayToastStatus("The toast encountered an error.");

		private void DisplayToastStatus(string message) => Invoke((Action)(() => { Activate(); Output.Text = message; }));
	}
}