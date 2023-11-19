using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;

namespace DesktopToastsSample;

[ClassInterface(ClassInterfaceType.None)]
[ComSourceInterfaces(typeof(INotificationActivationCallback))]
[Guid("23A5B06E-20BB-4E7E-A0AC-6982ED6A6041"), ComVisible(true)]
public class NotificationActivator : INotificationActivationCallback
{
	private static readonly string subKey = @"SOFTWARE\Classes\CLSID\" + typeof(NotificationActivator).GUID.ToString("B");
	private static int cookie = -1;
	private static RegistrationServices regService = new();
	private static string? shortcutPath;

	public static void Initialize(string appId)
	{
		cookie = regService.RegisterTypeForComClients(typeof(NotificationActivator), RegistrationClassContext.LocalServer,
			RegistrationConnectionType.MultipleUse);
		RegisterAppForNotificationSupport(appId);
	}

	public static void Uninitialize()
	{
		if (cookie != -1)
			regService?.UnregisterTypeForComClients(cookie);
		UnregisterAppForNotificationSupport();
	}

	public HRESULT Activate(string appUserModelId, string invokedArgs, NOTIFICATION_USER_INPUT_DATA[] data, int dataCount)
	{
		var mainForm = Application.OpenForms.Cast<Form>().FirstOrDefault() as DesktopToastsApp;
		mainForm?.Invoke(() => mainForm.ToastActivated());
		return HRESULT.S_OK;
	}

	private static void InstallShortcut(string shortcutPath, string exePath, string appId)
	{
		var shellLink = ComReleaserFactory.Create(new IShellLinkW());
		shellLink.Item.SetPath(exePath);
		var propertyStore = (IPropertyStore)shellLink.Item;
		propertyStore.SetValue(PROPERTYKEY.System.AppUserModel.ID, new PROPVARIANT(appId));
		propertyStore.SetValue(PROPERTYKEY.System.AppUserModel.ToastActivatorCLSID, new PROPVARIANT(typeof(NotificationActivator).GUID));
		propertyStore.Commit();
		((IPersistFile)shellLink.Item).Save(shortcutPath, true);
	}

	private static void RegisterAppForNotificationSupport(string appId)
	{
		shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\Desktop Toasts App.lnk");
		if (!File.Exists(shortcutPath))
		{
			var exePath = Process.GetCurrentProcess().MainModule.FileName;
			InstallShortcut(shortcutPath, exePath, appId);
			RegisterComServer(exePath);
		}
	}

	private static void RegisterComServer(string exePath)
	{
		using var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subKey + @"\LocalServer32");
		key.SetValue(null, exePath);
	}

	private static void UnregisterAppForNotificationSupport()
	{
		File.Delete(shortcutPath);
		UnregisterComServer();
	}

	private static void UnregisterComServer() => Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(subKey, false);
}