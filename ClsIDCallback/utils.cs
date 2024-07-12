using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using Vanara.PInvoke;
using Vanara.Extensions;

namespace ClsIDCallback;

public static class Utils
{
	public static readonly Guid CLSID_CNotifyInterfaceImp = new(0xC48FF713, 0xB257, 0x4242, 0xB1, 0xFF, 0xA8, 0x6B, 0x61, 0xDF, 0x3B, 0x3E);
	const string JOBTRANSFERRED_EVENTNAME = "EVENT_CALLBACK_JOBTRANSFERRED";
	const string JOBERROR_EVENTNAME = "EVENT_CALLBACK_JOBERROR";

	public struct DOWNLOAD_FILE
	{
		public string RemoteFile;
		public string LocalFile;
	}

	public static void RegisterClassObject<T>(in Guid rclsid, out uint lpdwCookieReceiver, T? pT_in = null) where T : class, new()
	{
		T? pT = pT_in ?? new();
		HRESULT hr = HRESULT.S_OK;

		IntPtr pIUnknown = Marshal.GetIUnknownForObject(pT);
		if (pIUnknown != IntPtr.Zero)
		{
			hr = CoRegisterClassObject(rclsid, pT, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE | REGCLS.REGCLS_SUSPENDED, out lpdwCookieReceiver);

			Marshal.Release(pIUnknown);
		}
		else
			lpdwCookieReceiver = 0;

		pT = default;
	}

	public static long g_lObjsInUse;
	public static long g_lServerLocks;
	public static uint g_dwMainThreadID;

	const string szClassKey = szClassRoot + szCLSID;
	const string szAppIDKey = szAppIDRoot + szCLSID;
	const string szClassRoot = "Guid\\";
	const string szAppIDRoot = "SOFTWARE\\Classes\\AppID\\";
	const string szCLSID = "{C48FF713-B257-4242-B1FF-A86B61DF3B3E}";

	// This code demonstrate the COM server registration code. Generally this happens during the installation of the component.
	public static HRESULT RegisterServer()
	{
		//SafeRegistryHandle hCLSIDKey1 = default, hCLSIDKey2 = default, hInProcSvrKey = default, hImpCategoriesKey = default, hIIDMarshalKey = default;
		//string szModulePath;
		const string szClassDescription = "NotifyInterfaceImp class";
		const string szRunAs = "Interactive User";

		try
		{
			// Create a key under Guid for our COM server.
			RegCreateKeyEx(HKEY.HKEY_CLASSES_ROOT, szClassKey,
				0, default, RegOpenOptions.REG_OPTION_NON_VOLATILE, REGSAM.KEY_SET_VALUE | REGSAM.KEY_CREATE_SUB_KEY,
				default, out var hCLSIDKey1, out _).ThrowIfFailed();

			// The default value of the key is a human-readable description of the coclass.
			RegSetValueEx(hCLSIDKey1, default, 0, REG_VALUE_TYPE.REG_SZ, szClassDescription,
				(uint)StringHelper.GetByteCount(szClassDescription)).ThrowIfFailed();

			// Create the "AppID" key
			RegSetValueEx(hCLSIDKey1, "AppID", 0, REG_VALUE_TYPE.REG_SZ, szCLSID,
				(uint)StringHelper.GetByteCount(szCLSID)).ThrowIfFailed();

			// Create the LocalServer32 key, which holds info about our coclass.
			RegCreateKeyEx(hCLSIDKey1, "LocalServer32", 0, default, RegOpenOptions.REG_OPTION_NON_VOLATILE,
				REGSAM.KEY_SET_VALUE, default, out var hInProcSvrKey, out _).ThrowIfFailed();

			// The default value of the LocalServer32 key holds the full path to our executable.
			var szModulePath = GetModuleFileName(default);
			RegSetValueEx(hInProcSvrKey, default, 0, REG_VALUE_TYPE.REG_SZ, szModulePath,
				(uint)StringHelper.GetByteCount(szModulePath)).ThrowIfFailed();

			// Create the "Implemented Categories" key
			RegCreateKeyEx(hCLSIDKey1, "Implemented Categories", 0, default, RegOpenOptions.REG_OPTION_NON_VOLATILE,
				REGSAM.KEY_SET_VALUE, default, out var hImpCategoriesKey, out _).ThrowIfFailed();

			// Create the typeof(IMarshal).GUID key
			string strIIDIMarshal = typeof(IMarshal).GUID.ToString();

			RegCreateKeyEx(hImpCategoriesKey, strIIDIMarshal, 0, default, RegOpenOptions.REG_OPTION_NON_VOLATILE,
				REGSAM.KEY_SET_VALUE, default, out var hIIDMarshalKey, out _).ThrowIfFailed();

			// Create a key under SOFTWARE\\Classes\\AppID for our COM server.
			RegCreateKeyEx(HKEY.HKEY_LOCAL_MACHINE, szAppIDKey,
				0, default, RegOpenOptions.REG_OPTION_NON_VOLATILE, REGSAM.KEY_SET_VALUE | REGSAM.KEY_CREATE_SUB_KEY,
				default, out var hCLSIDKey2, out _).ThrowIfFailed();

			// The default value of the key is a human-readable description of the coclass.
			RegSetValueEx(hCLSIDKey2, "RunAs", 0, REG_VALUE_TYPE.REG_SZ, szRunAs, (uint)StringHelper.GetByteCount(szRunAs)).ThrowIfFailed();
		}
		catch (Exception err)
		{
			Console.Write("Registration failed - {0}", err);
			UnRegisterServer();
			return err.HResult;
		}
		return 0;
	}

	public static HRESULT UnRegisterServer()
	{
		//Delete the ClassID key
		RegDeleteTree(HKEY.HKEY_CLASSES_ROOT, szClassKey);
		//Delete the AppID key
		RegDeleteTree(HKEY.HKEY_LOCAL_MACHINE, szAppIDKey);
		return HRESULT.S_OK;
	}
}