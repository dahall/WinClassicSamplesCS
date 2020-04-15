using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace DesktopToastsSample
{
	[ClassInterface(ClassInterfaceType.None)]
	[ComSourceInterfaces(typeof(INotificationActivationCallback))]
	[Guid("23A5B06E-20BB-4E7E-A0AC-6982ED6A6041"), ComVisible(true)]
	public class NotificationActivator : INotificationActivationCallback
	{
		private static int cookie = -1;

		private static RegistrationServices regService = null;

		public static void Initialize()
		{
			regService = new RegistrationServices();

			cookie = regService.RegisterTypeForComClients(
				typeof(NotificationActivator),
				RegistrationClassContext.LocalServer,
				RegistrationConnectionType.MultipleUse);
		}

		public static void Uninitialize()
		{
			if (cookie != -1 && regService != null)
			{
				regService.UnregisterTypeForComClients(cookie);
			}
		}

		public HRESULT Activate(string appUserModelId, string invokedArgs, NOTIFICATION_USER_INPUT_DATA[] data, int dataCount)
		{
			App.Current.Dispatcher.Invoke(() => (App.Current.MainWindow as MainWindow).ToastActivated());
		}
	}
}