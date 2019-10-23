using System;
using System.Text;

using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

using Vanara.InteropServices;
using Vanara.PInvoke;

namespace AutomaticJumpList
{
	public static class FileRegistrations
	{
		private const string c_szProgID = "Microsoft.Samples.AutomaticJumpListProgID";
		private static readonly string[] c_rgszExtsToRegister = { ".txt", ".doc" };

		// Creates a basic ProgID to use for file type registrations. For a document to appear in Jump Lists, the associated application must
		// be registered to handle the document's file type (extension).
		public static HRESULT _RegisterProgid(bool fRegister)
		{
			HRESULT hr;
			if (fRegister)
			{
				hr = HRESULT_FROM_WIN32(RegCreateKeyEx(HKEY.HKEY_CLASSES_ROOT, c_szProgID, 0, null, RegOpenOptions.REG_OPTION_NON_VOLATILE, REGSAM.KEY_SET_VALUE | REGSAM.KEY_CREATE_SUB_KEY, null, out var hkeyProgid, out var _));
				if (hr.Succeeded)
				{
					hr = _RegSetString(hkeyProgid, null, "FriendlyTypeName", "Automatic Jump List Document");
					if (hr.Succeeded)
					{
						var szAppPath = new StringBuilder(MAX_PATH);
						hr = (GetModuleFileName(default, szAppPath, (uint)szAppPath.Capacity) > 0) ? HRESULT.S_OK : HRESULT_FROM_WIN32(GetLastError());
						if (hr.Succeeded)
						{
							hr = _RegSetString(hkeyProgid, "DefaultIcon", null, $"{szAppPath},0");
							if (hr.Succeeded)
							{
								hr = _RegSetString(hkeyProgid, "CurVer", null, c_szProgID);
								if (hr.Succeeded)
								{
									hr = HRESULT_FROM_WIN32(RegCreateKeyEx(hkeyProgid, "shell", 0, null, RegOpenOptions.REG_OPTION_NON_VOLATILE, REGSAM.KEY_SET_VALUE | REGSAM.KEY_CREATE_SUB_KEY,
										null, out var hkeyShell, out var _));
									if (hr.Succeeded)
									{
										// The list of verbs provided by the ProgID is located uner the "shell" key. Here, only the single
										// "Open" verb is registered.
										var szCmdLine = $"{szAppPath} %1";
										hr = _RegSetString(hkeyShell, "Open\\Command", null, szCmdLine);
										if (hr.Succeeded)
										{
											// Set "Open" as the default verb for this ProgID.
											hr = _RegSetString(hkeyShell, null, null, "Open");
										}
									}
								}
							}
						}
					}
				}
			}
			else
			{
				var lRes = RegDeleteTree(HKEY.HKEY_CLASSES_ROOT, c_szProgID);
				hr = (Win32Error.ERROR_SUCCESS == lRes || Win32Error.ERROR_FILE_NOT_FOUND == lRes) ? HRESULT.S_OK : HRESULT_FROM_WIN32(lRes);
			}
			return hr;
		}

		public static HRESULT _RegisterToHandleExt(string pszExt, bool fRegister)
		{
			// All ProgIDs that can handle a given file type should be listed under OpenWithProgids, even if listed as the default, so they
			// can be enumerated in the Open With dialog, and so the Jump Lists can find the correct ProgID to use when relaunching a
			// document with the specific application the Jump List is associated with.
			var szKey = System.IO.Path.Combine(pszExt, "OpenWithProgids");
			var hr = HRESULT_FROM_WIN32(RegCreateKeyEx(HKEY.HKEY_CLASSES_ROOT, szKey, 0, null, RegOpenOptions.REG_OPTION_NON_VOLATILE,
				REGSAM.KEY_SET_VALUE, null, out var hkeyProgidList, out var _));
			if (hr.Succeeded)
			{
				if (fRegister)
				{
					hr = HRESULT_FROM_WIN32(RegSetValueEx(hkeyProgidList, c_szProgID, 0, (uint)REG_VALUE_TYPE.REG_NONE, IntPtr.Zero, 0));
				}
				else
				{
					hr = HRESULT_FROM_WIN32(RegDeleteKeyValue(hkeyProgidList, null, c_szProgID));
				}
			}
			return hr;
		}

		public static HRESULT _RegSetString(HKEY hkey, string pszSubKey, string pszValue, string pszData)
		{
			var mem = new SafeCoTaskMemString(pszData);
			return HRESULT_FROM_WIN32(SHSetValue(hkey, pszSubKey, pszValue, REG_VALUE_TYPE.REG_SZ, (IntPtr)mem, (uint)mem.Capacity));
		}

		public static bool AreFileTypesRegistered() => HRESULT_FROM_WIN32(RegOpenKey(HKEY.HKEY_CLASSES_ROOT, c_szProgID, out var hkeyProgid)).Succeeded;

		public static HRESULT RegisterToHandleFileTypes()
		{
			var hr = _RegisterProgid(true);
			if (hr.Succeeded)
			{
				foreach (var ext in c_rgszExtsToRegister)
				{
					hr = _RegisterToHandleExt(ext, true);
				}

				if (hr.Succeeded)
				{
					// Notify that file associations have changed
					SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST);
				}
			}
			return hr;
		}

		public static HRESULT UnRegisterFileTypeHandlers()
		{
			var hr = _RegisterProgid(false);
			if (hr.Succeeded)
			{
				foreach (var ext in c_rgszExtsToRegister)
				{
					hr = _RegisterToHandleExt(ext, false);
				}

				if (hr.Succeeded)
				{
					// Notify that file associations have changed
					SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, SHCNF.SHCNF_IDLIST);
				}
			}
			return hr;
		}

		private static HRESULT HRESULT_FROM_WIN32(Win32Error err) => err.ToHRESULT();
	}
}
