using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.ComTypes;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace Vanara.PInvoke;

public class CRegisterExtension
{
	private Guid? _clsid;
	private bool _fAssocChanged;
	private HKEY _hkeyRoot;
	private string? _szCLSID;
	private string _szModule;

	public CRegisterExtension(Guid? clsid = default, HKEY hkeyRoot = default)
	{
		_hkeyRoot = hkeyRoot == default ? HKEY.HKEY_CURRENT_USER : hkeyRoot;
		CLSID = clsid;
		SetModule();
	}

	~CRegisterExtension()
	{
		if (_fAssocChanged)
		{
			// inform Explorer, et al that file association data has changed
			SHChangeNotify(SHCNE.SHCNE_ASSOCCHANGED, 0);
		}
	}

	public HRESULT MapNotFoundToSuccess(HRESULT hr) => HRESULT_FROM_WIN32(Win32Error.ERROR_FILE_NOT_FOUND) == hr ? HRESULT.S_OK : hr;

	public HRESULT RegDeleteKeyPrintf(HKEY hkey, string pszKeyFormatString, params object[] argList)
	{
		var szKeyName = string.Format(pszKeyFormatString, argList);
		var hr = HRESULT_FROM_WIN32(RegDeleteTree(hkey, szKeyName));
		_UpdateAssocChanged(hr, pszKeyFormatString);
		return MapNotFoundToSuccess(hr);
	}

	public HRESULT RegDeleteKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValue, params object[] argList)
	{
		var szKeyName = string.Format(pszKeyFormatString, argList);
		var hr = HRESULT_FROM_WIN32(RegDeleteKeyValue(hkey, szKeyName, pszValue));
		_UpdateAssocChanged(hr, pszKeyFormatString);
		return MapNotFoundToSuccess(hr);
	}

	public HRESULT RegisterAppAsLocalServer(string pszFriendlyName, string? pszCmdLine = null)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded && _szCLSID is not null)
		{
			var szCmdLine = new StringBuilder(MAX_PATH + 20);
			if (pszCmdLine != null)
			{
				szCmdLine.AppendFormat("{0} {1}", _szModule, pszCmdLine);
			}
			else
			{
				szCmdLine.Append(_szModule);
			}

			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}\\LocalServer32", "", szCmdLine.ToString(), _szCLSID);
			if (hr.Succeeded)
			{
				hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}", "AppId", _szCLSID, _szCLSID);
				if (hr.Succeeded)
				{
					hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}", "", pszFriendlyName, _szCLSID);
					if (hr.Succeeded)
					{
						hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\AppID\\{0}", "", pszFriendlyName, _szCLSID);
					}
				}
			}
		}
		return hr;
	}

	public HRESULT RegisterAppDropTarget()
	{
		var hr = _EnsureModule();
		if (hr.Succeeded && _szCLSID is not null)
		{
			hr = RegisterAppPath(new Dictionary<string, object> { { "DropTarget", _szCLSID } });
		}
		return hr;
	}

	/// <summary>Registers the application path in Windows 7 and later with associated sub-entries.</summary>
	/// <param name="valueNameValuePairs">A dictionary of value name/value pairs that will be assigned as values under the App Path key.</param>
	/// <returns>S_OK on success; otherwise HRESULT failure.</returns>
	public HRESULT RegisterAppPath(IDictionary<string, object> valueNameValuePairs)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded)
		{
			var regPath = @"Software\Microsoft\Windows\CurrentVersion\App Paths\" + System.IO.Path.GetFileName(_szModule);
			hr = _RegSetKeyValue(_hkeyRoot, regPath, "", _szModule);
			if (hr.Succeeded && (valueNameValuePairs?.Count ?? 0) > 0)
			{
				foreach (var p in valueNameValuePairs!)
				{
					hr = _RegSetKeyValue(_hkeyRoot, regPath, p.Key, p.Value);
					if (hr.Failed) break;
				}
			}
		}
		return hr;
	}

	/// <summary>Registers the application path in Windows 7 and later with associated sub-entries.</summary>
	/// <param name="dropTargetClsid">
	/// Is a class identifier (CLSID). The DropTarget entry contains the CLSID of an object (usually a local server rather than an
	/// in-process server) that implements IDropTarget. By default, when the drop target is an executable file, and no DropTarget value
	/// is provided, the Shell converts the list of dropped files into a command-line parameter and passes it to ShellExecuteEx through lpParameters.
	/// <para>If this value is <see langword="null"/> and the CLSID property for the class is set to a value, it will be used.</para>
	/// </param>
	/// <param name="dontUseDesktopChangeRouter">
	/// Is mandatory for debugger applications to avoid file dialog deadlocks when debugging the Windows Explorer process. Setting the
	/// DontUseDesktopChangeRouter entry produces a slightly less efficient handling of the change notifications, however.
	/// </param>
	/// <param name="path">
	/// Supplies a string (in the form of a semicolon-separated list of directories) to append to the PATH environment variable when an
	/// application is launched by calling ShellExecuteEx. It is the fully qualified path to the .exe. In Windows 7 and later, it can
	/// include expansion strings and is commonly %ProgramFiles%.
	/// </param>
	/// <param name="supportedProtocols">
	/// Creates a string that contains the URL protocol schemes for a given key. This can contain multiple registry values to indicate
	/// which schemes are supported. This string follows the format of scheme1:scheme2. If this list is not empty, file: will be added to
	/// the string. This protocol is implicitly supported when SupportedProtocols is defined.
	/// </param>
	/// <param name="useUrl">
	/// Indicates that your application can accept a URL (instead of a file name) on the command line. Applications that can open
	/// documents directly from the internet, like web browsers and media players, should set this entry.
	/// <para>
	/// When the ShellExecuteEx function starts an application and the UseUrl = 1 value is not set, ShellExecuteEx downloads the document
	/// to a local file and invokes the handler on the local copy.
	/// </para>
	/// <para>
	/// For example, if the application has this entry set and a user right-clicks on a file stored on a web server, the Open verb will
	/// be made available.If not, the user will have to download the file and open the local copy.
	/// </para>
	/// <para>
	/// In Windows Vista and earlier, this entry indicated that the URL should be passed to the application along with a local file name,
	/// when called via ShellExecuteEx. In Windows 7, it indicates that the application can understand any http or https url that is
	/// passed to it, without having to supply the cache file name as well.This registry key is associated with the SupportedProtocols key.
	/// </para>
	/// </param>
	/// <returns>S_OK on success; otherwise HRESULT failure.</returns>
	public HRESULT RegisterAppPath(Guid? dropTargetClsid = null, bool dontUseDesktopChangeRouter = false, string? path = null, string? supportedProtocols = null, bool useUrl = false)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded)
		{
			var values = new Dictionary<string, object>();
			if (dropTargetClsid.HasValue || _clsid.HasValue)
				values.Add("DropTarget", dropTargetClsid.GetValueOrDefault(_clsid!.Value));
			if (dontUseDesktopChangeRouter) values.Add("DontUseDesktopChangeRouter", true);
			if (path != null) values.Add("Path", path);
			if (supportedProtocols != null) values.Add("SupportedProtocols", supportedProtocols);
			if (useUrl) values.Add("UseUrl", true);
			return RegisterAppPath(values);
		}
		return hr;
	}

	/// <summary>Registers the application with associated sub-entries.</summary>
	/// <param name="friendlyName">Provides a way to get a localizable name to display for an application instead of just the version information appearing, which may not be localizable. The association query ASSOCSTR reads this registry entry value and falls back to use the FileDescription name in the version information. If that name is missing, the association query defaults to the display name of the file. Applications should use ASSOCSTR_FRIENDLYAPPNAME to retrieve this information to obtain the proper behavior.</param>
	/// <param name="noOpenWith">Indicates that no application is specified for opening this file type. Be aware that if an OpenWithProgIDs subkey has been set for an application by file type, and the ProgID subkey itself does not also have a NoOpenWith entry, that application will appear in the list of recommended or available applications even if it has specified the NoOpenWith entry. For more information, see How to How to Include an Application in the Open With Dialog Box and How to exclude an Application from the Open with Dialog Box.</param>
	/// <param name="verbCommand">Provides the verb method for calling the application from OpenWith. Without a verb definition specified here, the system assumes that the application supports CreateProcess, and passes the file name on the command line. This functionality applies to ExecuteCommand.</param>
	/// <param name="verbDropTarget">Provides the verb method for calling the application from OpenWith. Without a verb definition specified here, the system assumes that the application supports CreateProcess, and passes the file name on the command line. This functionality applies to DropTarget.</param>
	/// <param name="defaultIcon">Enables an application to provide a specific icon to represent the application instead of the first icon stored in the .exe file.</param>
	/// <param name="isHostApp">Indicates that the process is a host process, such as Rundll32.exe or Dllhost.exe, and should not be considered for Start menu pinning or inclusion in the Most Frequently Used (MFU) list. When launched with a shortcut that contains a non-null argument list or an explicit Application User Model IDs (AppUserModelIDs), the process can be pinned (as that shortcut). Such shortcuts are candidates for inclusion in the MFU list.</param>
	/// <param name="useExecutableForTaskbarGroupIcon">Causes the taskbar to use the default icon of this executable if there is no pinnable shortcut for this application, and instead of the icon of the window that was first encountered.</param>
	/// <param name="taskbarGroupIcon">Specifies the icon used to override the taskbar icon. The window icon is normally used for the taskbar. Setting the TaskbarGroupIcon entry causes the system to use the icon from the .exe for the application instead.</param>
	/// <param name="noStartPage">Indicates that the application executable and shortcuts should be excluded from the Start menu and from pinning or inclusion in the MFU list. This entry is typically used to exclude system tools, installers and uninstallers, and readme files.</param>
	/// <param name="supportedTypes">Lists the file types that the application supports. Doing so enables the application to be listed in the cascade menu of the Open with dialog box.</param>
	/// <returns>S_OK on success; otherwise HRESULT failure.</returns>
	public HRESULT RegisterApplication(string? friendlyName = null, bool noOpenWith = false, Tuple<string, string>? verbCommand = null, Tuple<string, Guid>? verbDropTarget = null, string? defaultIcon = null,
		bool isHostApp = false, bool useExecutableForTaskbarGroupIcon = false, string? taskbarGroupIcon = null, bool noStartPage = false, string[]? supportedTypes = null)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded)
		{
			var regPath = $"Software\\Classes\\Applications\\" + System.IO.Path.GetFileName(_szModule);
			hr = _RegSetKeyValue(_hkeyRoot, regPath, "", null);
			if (hr.Failed) return hr;
			hr = SetOrDelete(friendlyName != null, regPath, "FriendlyAppName", friendlyName);
			if (hr.Failed) return hr;
			hr = SetOrDelete(noOpenWith, regPath, "NoOpenWith", null);
			if (hr.Failed) return hr;
			hr = SetOrDelete(verbCommand != null, $"{regPath}\\shell\\{verbCommand!.Item1}\\command", "", verbCommand.Item2);
			if (hr.Failed) return hr;
			hr = SetOrDelete(verbDropTarget != null, $"{regPath}\\shell\\{verbDropTarget!.Item1}\\DropTarget", "Clsid", verbDropTarget.Item2);
			if (hr.Failed) return hr;
			hr = defaultIcon != null ? _RegSetKeyValue(_hkeyRoot, $"{regPath}\\DefaultIcon", "", defaultIcon) : RegDeleteTree(_hkeyRoot, $"{regPath}\\DefaultIcon").ToHRESULT();
			if (hr.Failed) return hr;
			hr = SetOrDelete(isHostApp, regPath, "IsHostApp", null);
			if (hr.Failed) return hr;
			hr = SetOrDelete(useExecutableForTaskbarGroupIcon, regPath, "UseExecutableForTaskbarGroupIcon", null);
			if (hr.Failed) return hr;
			hr = SetOrDelete(taskbarGroupIcon != null, regPath, "TaskbarGroupIcon", taskbarGroupIcon);
			if (hr.Failed) return hr;
			hr = SetOrDelete(noStartPage, regPath, "NoStartPage", null);
			if (hr.Failed) return hr;
			hr = RegAddValueNames(regPath, supportedTypes);
		}
		return hr;

		HRESULT RegAddValueNames(string regPath, string[]? valueNames)
		{
			var ahr = RegDeleteTree(_hkeyRoot, regPath).ToHRESULT();
			for (var i = 0; valueNames != null && ahr.Succeeded && i < valueNames.Length; i++)
			{
				ahr = _RegSetKeyValue(_hkeyRoot, regPath, valueNames[i], null);
			}
			return ahr;
		}
		HRESULT SetOrDelete(bool condition, string regPath, string valueName, object? value) => condition ? _RegSetKeyValue(_hkeyRoot, regPath, valueName, value) : RegDeleteKeyValue(_hkeyRoot, regPath, valueName).ToHRESULT();
	}

	public HRESULT RegisterAppShortcutInSendTo()
	{
		var szPath = new StringBuilder(MAX_PATH);
		var hr = GetModuleFileName(default, szPath, (uint)szPath.Length) > 0 ? HRESULT.S_OK : Win32Error.GetLastError().ToHRESULT();
		if (hr.Succeeded)
		{
			// Set the shortcut target
			var psl = new IShellLinkW();
			psl.SetPath(szPath.ToString());
			var szName = System.IO.Path.GetFileNameWithoutExtension(szPath.ToString()) + ".lnk";
			hr = SHGetKnownFolderPath(KNOWNFOLDERID.FOLDERID_SendTo.Guid(), KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, default, out var szSendTo);
			if (hr.Succeeded)
			{
				szPath.Clear();
				szPath.Append(szSendTo);
				hr = PathAppend(szPath, szName) ? HRESULT.S_OK : HRESULT.E_FAIL;
				if (hr.Succeeded)
				{
					if (psl is IPersistFile ppf)
					{
						ppf.Save(szPath.ToString(), true);
					}
				}
			}
		}
		return hr;
	}

	public HRESULT RegisterContextMenuHandler(string pszProgID, string pszDescription)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shellex\\ContextMenuHandlers\\{1}",
			"", pszDescription, pszProgID, _szCLSID);
	}

	public HRESULT RegisterCreateProcessVerb(string pszProgID, string pszVerb, string pszCmdLine, string pszVerbDisplayName)
	{
		UnRegisterVerb(pszProgID, pszVerb); // make sure no existing registration exists, ignore failure

		var hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shell\\{1}\\command", "", pszCmdLine, pszProgID, pszVerb);
		if (hr.Succeeded)
		{
			hr = _EnsureBaseProgIDVerbIsNone(pszProgID);

			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shell\\{1}", "", pszVerbDisplayName, pszProgID, pszVerb);
		}
		return hr;
	}

	public HRESULT RegisterDropTargetVerb(string pszProgID, string pszVerb, string pszVerbDisplayName)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;

		UnRegisterVerb(pszProgID, pszVerb); // make sure no existing registration exists, ignore failure

		var hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}\\DropTarget",
			"CLSID", _szCLSID, pszProgID, pszVerb);
		if (hr.Succeeded)
		{
			hr = _EnsureBaseProgIDVerbIsNone(pszProgID);

			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}",
				"", pszVerbDisplayName, pszProgID, pszVerb);
		}
		return hr;
	}

	public HRESULT RegisterElevatableInProcServer(string pszFriendlyName, uint idLocalizeString, uint idIconRef)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded && _szCLSID is not null)
		{
			hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\AppId\\{0}", "", pszFriendlyName, _szCLSID);
			if (hr.Succeeded)
			{
				byte[] c_rgAccessPermission =
					{0x01,0x00,0x04,0x80,0x60,0x00,0x00,0x00,0x70,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x14,
			 0x00,0x00,0x00,0x02,0x00,0x4c,0x00,0x03,0x00,0x00,0x00,0x00,0x00,0x14,0x00,0x03,0x00,
			 0x00,0x00,0x01,0x01,0x00,0x00,0x00,0x00,0x00,0x05,0x12,0x00,0x00,0x00,0x00,0x00,0x14,
			 0x00,0x07,0x00,0x00,0x00,0x01,0x01,0x00,0x00,0x00,0x00,0x00,0x05,0x0a,0x00,0x00,0x00,
			 0x00,0x00,0x14,0x00,0x03,0x00,0x00,0x00,0x01,0x01,0x00,0x00,0x00,0x00,0x00,0x05,0x04,
			 0x00,0x00,0x00,0xcd,0xcd,0xcd,0xcd,0xcd,0xcd,0xcd,0xcd,0x01,0x02,0x00,0x00,0x00,0x00,
			 0x00,0x05,0x20,0x00,0x00,0x00,0x20,0x02,0x00,0x00,0x01,0x02,0x00,0x00,0x00,0x00,0x00,
			 0x05,0x20,0x00,0x00,0x00,0x20,0x02,0x00,0x00};
				// shell32\shell32.man uses this for InProcServer32 cases 010004805800000068000000000000001400000002004400030000000000140003000000010100000000000504000000000014000700000001010000000000050a00000000001400030000000101000000000005120000000102000000000005200000002002000001020000000000052000000020020000
				hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\AppId\\{0}", "AccessPermission", c_rgAccessPermission, (uint)c_rgAccessPermission.Length, _szCLSID);

				byte[] c_rgLaunchPermission =
					{0x01,0x00,0x04,0x80,0x78,0x00,0x00,0x00,0x88,0x00,0x00,0x00,0x00,0x00,0x00,0x00,0x14,
			 0x00,0x00,0x00,0x02,0x00,0x64,0x00,0x04,0x00,0x00,0x00,0x00,0x00,0x14,0x00,0x1f,0x00,
			 0x00,0x00,0x01,0x01,0x00,0x00,0x00,0x00,0x00,0x05,0x12,0x00,0x00,0x00,0x00,0x00,0x18,
			 0x00,0x1f,0x00,0x00,0x00,0x01,0x02,0x00,0x00,0x00,0x00,0x00,0x05,0x20,0x00,0x00,0x00,
			 0x20,0x02,0x00,0x00,0x00,0x00,0x14,0x00,0x1f,0x00,0x00,0x00,0x01,0x01,0x00,0x00,0x00,
			 0x00,0x00,0x05,0x04,0x00,0x00,0x00,0x00,0x00,0x14,0x00,0x0b,0x00,0x00,0x00,0x01,0x01,
			 0x00,0x00,0x00,0x00,0x00,0x05,0x12,0x00,0x00,0x00,0xcd,0xcd,0xcd,0xcd,0xcd,0xcd,0xcd,
			 0xcd,0x01,0x02,0x00,0x00,0x00,0x00,0x00,0x05,0x20,0x00,0x00,0x00,0x20,0x02,0x00,0x00,
			 0x01,0x02,0x00,0x00,0x00,0x00,0x00,0x05,0x20,0x00,0x00,0x00,0x20,0x02,0x00,0x00};
				hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\AppId\\{0}", "LaunchPermission", c_rgLaunchPermission, (uint)c_rgLaunchPermission.Length, _szCLSID);

				hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}", "", pszFriendlyName, _szCLSID);
				if (hr.Succeeded)
				{
					hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}", "AppId", _szCLSID, _szCLSID);
					if (hr.Succeeded)
					{
						var szRes = string.Format("@{0},-{1}", _szModule, idLocalizeString);
						hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}", "LocalizedString", szRes, _szCLSID);
						if (hr.Succeeded)
						{
							hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}\\InProcServer32", "", _szModule, _szCLSID);
							if (hr.Succeeded)
							{
								hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}\\Elevation", "Enabled", 1, _szCLSID);
								if (hr.Succeeded && idIconRef > 0)
								{
									szRes = string.Format("@{0},-{1}", _szModule, idIconRef);
									hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}\\Elevation", "IconReference", szRes, _szCLSID);
								}
							}
						}
					}
				}
			}
		}
		return hr;
	}

	public HRESULT RegisterElevatableLocalServer(string pszFriendlyName, uint idLocalizeString, uint idIconRef)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded && _szCLSID is not null)
		{
			hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}", "", pszFriendlyName, _szCLSID);
			if (hr.Succeeded)
			{
				var szRes = string.Format("@{0},-{1}", _szModule, idLocalizeString);
				hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}", "LocalizedString", szRes, _szCLSID);
				if (hr.Succeeded)
				{
					hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}\\LocalServer32", "", _szModule, _szCLSID);
					if (hr.Succeeded)
					{
						hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}\\Elevation", "Enabled", 1, _szCLSID);
						if (hr.Succeeded && idIconRef > 0)
						{
							szRes = string.Format("@{0},-{1}", _szModule, idIconRef);
							hr = RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Classes\\CLSID\\{0}\\Elevation", "IconReference", szRes, _szCLSID);
						}
					}
				}
			}
		}
		return hr;
	}

	// create registry entries for drop target based static verb. the specified clsid will be
	public HRESULT RegisterExecuteCommandVerb(string pszProgID, string pszVerb, string pszVerbDisplayName)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;

		UnRegisterVerb(pszProgID, pszVerb); // make sure no existing registration exists, ignore failure

		var hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}\\command",
			"DelegateExecute", _szCLSID, pszProgID, pszVerb);
		if (hr.Succeeded)
		{
			hr = _EnsureBaseProgIDVerbIsNone(pszProgID);

			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}",
				"", pszVerbDisplayName, pszProgID, pszVerb);
		}
		return hr;
	}

	public HRESULT RegisterExplorerCommandStateHandler(string pszProgID, string pszVerb)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}",
			"CommandStateHandler", _szCLSID, pszProgID, pszVerb);
	}

	public HRESULT RegisterExplorerCommandVerb(string pszProgID, string pszVerb, string pszVerbDisplayName)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		UnRegisterVerb(pszProgID, pszVerb); // make sure no existing registration exists, ignore failure

		var hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}",
			"ExplorerCommandHandler", _szCLSID, pszProgID, pszVerb);
		if (hr.Succeeded)
		{
			hr = _EnsureBaseProgIDVerbIsNone(pszProgID);

			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}",
				"", pszVerbDisplayName, pszProgID, pszVerb);
		}
		return hr;
	}

	public HRESULT RegisterExtensionWithProgID(string pszFileExtension, string pszProgID)
	{
		// HKCR\<.ext>=<ProgID> "Content Type" "PerceivedType"

		// TODO: to be polite if there is an existing mapping of extension to ProgID make sure it is added to the OpenWith list so that
		// users can get back to the old app using OpenWith
		// TODO: verify that HKLM/HKCU settings do not already exist as if they do they will get in the way of the setting being made here
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}", "", pszProgID, pszFileExtension);
	}

	public HRESULT RegisterHandlerSupportedProtocols(string[] pszProtocol)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		var szKey = string.Format("Software\\Classes\\CLSID\\{0}\\SupportedProtocols", _szCLSID);
		var hr = RegDeleteTree(_hkeyRoot, szKey).ToHRESULT();
		if ((hr.Failed && hr != HRESULT_FROM_WIN32(Win32Error.ERROR_FILE_NOT_FOUND)) || pszProtocol == null) return hr;
		if (pszProtocol.Length == 1 && pszProtocol[0] == "*")
			return _RegSetKeyValue(_hkeyRoot, szKey, "", "*");
		for (var i = 0; hr.Succeeded && i < pszProtocol.Length; i++)
		{
			hr = _RegSetKeyValue(_hkeyRoot, szKey, pszProtocol[i], null);
		}
		return hr;
	}

	public HRESULT RegisterInProcServer(string pszFriendlyName, string pszThreadingModel)
	{
		var hr = _EnsureModule();
		if (hr.Succeeded && _szCLSID is not null)
		{
			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}", "", pszFriendlyName, _szCLSID);
			if (hr.Succeeded)
			{
				hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}\\InProcServer32", "", _szModule, _szCLSID);
				if (hr.Succeeded)
				{
					hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}\\InProcServer32", "ThreadingModel", pszThreadingModel, _szCLSID);
				}
			}
		}
		return hr;
	}

	public HRESULT RegisterInProcServerAttribute(string pszAttribute, uint dwValue)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}", pszAttribute, dwValue, _szCLSID);
	}

	// define the kind of a file extension. this is a multi-value property, see HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\KindMap
	public HRESULT RegisterKind(string pszFileExtension, string pszKindValue) => RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\KindMap", pszFileExtension, pszKindValue);

	// IResolveShellLink handler, used for custom link resolution behavior
	public HRESULT RegisterLinkHandler(string pszProgID)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\ShellEx\\LinkHandler", "", _szCLSID, pszProgID);
	}

	public HRESULT RegisterNewMenuData(string pszFileExtension, string pszProgID, string pszBase64)
	{
		HRESULT hr;
		if (pszProgID != null)
		{
			hr = RegSetKeyValueBinaryPrintf(_hkeyRoot, "Software\\Classes\\{0}\\{1}\\ShellNew", "Data", pszBase64, pszFileExtension, pszProgID);
		}
		else
		{
			hr = RegSetKeyValueBinaryPrintf(_hkeyRoot, "Software\\Classes\\{0}\\ShellNew", "Data", pszBase64, pszFileExtension);
		}
		return hr;
	}

	public HRESULT RegisterNewMenuNullFile(string pszFileExtension, string pszProgID)
	{
		// there are 2 forms of this HKCR\<.ext>\ShellNew HKCR\<.ext>\ShellNew\<ProgID> - only ItemName NullFile Data -
		// REG_BINARY:<binary data> File command iconpath

		// another way that this works HKEY_CLASSES_ROOT\.doc\Word.Document.8\ShellNew
		HRESULT hr;
		if (pszProgID != null)
		{
			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\{1}\\ShellNew", "NullFile", "", pszFileExtension, pszProgID);
		}
		else
		{
			hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\ShellNew", "NullFile", "", pszFileExtension);
		}
		return hr;
	}

	public HRESULT RegisterOpenWith(string pszFileExtension, string pszProgID) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\OpenWithProgIds", pszProgID, "", pszFileExtension);

	public HRESULT RegisterPlayerVerbs(string[] rgpszAssociation, uint countAssociation, string pszVerb, string pszTitle)
	{
		var hr = RegisterAppAsLocalServer(pszTitle);
		if (hr.Succeeded)
		{
			// enable this handler to work with OpenSearch results, avoiding the downlaod and open behavior by indicating that we can
			// accept all URL forms
			hr = RegisterHandlerSupportedProtocols(new[] { "*" });

			for (uint i = 0; hr.Succeeded && (i < countAssociation); i++)
			{
				hr = RegisterExecuteCommandVerb(rgpszAssociation[i], pszVerb, pszTitle);
				if (hr.Succeeded)
				{
					hr = RegisterVerbAttribute(rgpszAssociation[i], pszVerb, "NeverDefault");
					if (hr.Succeeded)
					{
						hr = RegisterVerbAttribute(rgpszAssociation[i], pszVerb, "MultiSelectModel", "Player");
					}
				}
			}
		}
		return hr;
	}

	public HRESULT RegisterProgID(string pszProgID, string pszTypeName, uint idIcon)
	{
		var hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}", "", pszTypeName, pszProgID);
		if (hr.Succeeded)
		{
			if (idIcon != 0)
			{
				var szIconRef = string.Format("\"{0}\",-%d", _szModule, idIcon);
				// HKCR\<ProgID>\DefaultIcon
				hr = RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\DefaultIcon", "", szIconRef, pszProgID);
			}
		}
		return hr;
	}

	public HRESULT RegisterProgIDValue(string pszProgID, string pszValueName) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}", pszValueName, "", pszProgID);

	public HRESULT RegisterProgIDValue(string pszProgID, string pszValueName, string pszValue) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}", pszValueName, pszValue, pszProgID);

	public HRESULT RegisterProgIDValue(string pszProgID, string pszValueName, uint dwValue) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}", pszValueName, dwValue, pszProgID);

	// in process context menu handler
	public HRESULT RegisterPropertyHandler(string pszExtension)
	{
		// IPropertyHandler HKEY.HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\PropertySystem\PropertyHandlers\.docx={993BE281-6695-4BA5-8A2A-7AACBFAAB69E}
		if (_szCLSID is null) return HRESULT.E_FAIL;

		return RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\PropertySystem\\PropertyHandlers\\{0}",
			"", _szCLSID, pszExtension);
	}

	public HRESULT RegisterPropertyHandlerOverride(string pszProperty)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}\\OverrideFileSystemProperties", pszProperty, 1, _szCLSID);
	}

	// pszProgID "Folder" or "Directory"
	public HRESULT RegisterRightDragContextMenuHandler(string pszProgID, string pszDescription)
	{
		if (_szCLSID is null) return HRESULT.E_FAIL;
		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shellex\\DragDropHandlers\\{1}",
			"", pszDescription, pszProgID, _szCLSID);
	}

	public HRESULT RegisterThumbnailHandler(string pszExtension)
	{
		// IThumbnailHandler HKEY_CLASSES_ROOT\.wma\ShellEx\{e357fccd-a995-4576-b01f-234630154e96}={9DBD2C50-62AD-11D0-B806-00C04FD706EC}
		if (_szCLSID is null) return HRESULT.E_FAIL;

		return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\ShellEx\\{e357fccd-a995-4576-b01f-234630154e96}",
			"", _szCLSID, pszExtension);
	}

	// value names that do not require a value HKCR\<ProgID> NoOpen - display the "No Open" dialog for this file to disable double click
	// IsShortcut - report SFGAO_LINK for this item type, should have a IShellLink handler NeverShowExt - never show the file extension
	// AlwaysShowExt - always show the file extension NoPreviousVersions - don't display the "Previous Versions" verb for this file type
	// value names that require a string value HKCR\<ProgID> NoOpen - display the "No Open" dialog for this file to disable double click,
	// display this message FriendlyTypeName - localized resource ConflictPrompt FullDetails InfoTip QuickTip PreviewDetails PreviewTitle
	// TileInfo ExtendedTileInfo SetDefaultsFor - right click.new will populate the file with these properties, example:
	// "prop:System.Author;System.Document.DateCreated" value names that require a uint value HKCR\<ProgID> EditFlags ThumbnailCutoff
	// NeverDefault LegacyDisable Extended OnlyInBrowserWindow ProgrammaticAccessOnly SeparatorBefore SeparatorAfter CheckSupportedTypes,
	// used SupportedTypes that is a file type filter registered under AppPaths (I think)
	public HRESULT RegisterVerbAttribute(string pszProgID, string pszVerb, string pszValueName) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shell\\{1}", pszValueName, "", pszProgID, pszVerb);

	// MUIVerb=@dll,-resid MultiSelectModel=Single|Player|Document Position=Bottom|Top DefaultAppliesTo=System.ItemName:"foo"
	// HasLUAShield=System.ItemName:"bar" AppliesTo=System.ItemName:"foo"
	public HRESULT RegisterVerbAttribute(string pszProgID, string pszVerb, string pszValueName, string pszValue) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shell\\{1}", pszValueName, pszValue, pszProgID, pszVerb);

	// BrowserFlags ExplorerFlags AttributeMask AttributeValue ImpliedSelectionModel SuppressionPolicy
	public HRESULT RegisterVerbAttribute(string pszProgID, string pszVerb, string pszValueName, uint dwValue) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shell\\{1}", pszValueName, dwValue, pszProgID, pszVerb);

	// "open explorer" is an example
	public HRESULT RegisterVerbDefaultAndOrder(string pszProgID, string pszVerbOrderFirstIsDefault) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell", "", pszVerbOrderFirstIsDefault, pszProgID);

	public HRESULT RegSetKeyValueBinaryPrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, string pszBase64, params object[] argList)
	{
		var szKeyName = string.Format(pszKeyFormatString, argList);
		var pbDecodedImage = Convert.FromBase64String(pszBase64);
		var hr = HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, pszValueName, REG_VALUE_TYPE.REG_BINARY, pbDecodedImage, (uint)pbDecodedImage.Length));
		_UpdateAssocChanged(hr, pszKeyFormatString);
		return hr;
	}

	public HRESULT RegSetKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, string pszValue, params object[] argList)
	{
		var szKeyName = string.Format(pszKeyFormatString, argList);
		var hr = _RegSetKeyValue(hkey, szKeyName, pszValueName, pszValue);
		_UpdateAssocChanged(hr, pszKeyFormatString);
		return hr;
	}

	private HRESULT _RegSetKeyValue(HKEY hkey, string szKeyName, string szValueName, object? szValue)
	{
		switch (szValue)
		{
			case null:
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, REG_VALUE_TYPE.REG_SZ, IntPtr.Zero, 0));
			case string s:
				var valType = s.Contains("%") ? REG_VALUE_TYPE.REG_EXPAND_SZ : REG_VALUE_TYPE.REG_SZ;
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, valType, s, s.ChLen()));
			case uint ui:
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, REG_VALUE_TYPE.REG_DWORD, BitConverter.GetBytes(ui), sizeof(uint)));
			case bool b:
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, REG_VALUE_TYPE.REG_DWORD, BitConverter.GetBytes(b ? 1U : 0U), sizeof(uint)));
			case int i:
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, REG_VALUE_TYPE.REG_DWORD, BitConverter.GetBytes(unchecked((uint)i)), sizeof(uint)));
			case byte[] p:
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, REG_VALUE_TYPE.REG_BINARY, p, (uint)p.Length));
			case Guid g:
				var gs = g.ToString("B").ToUpperInvariant();
				return HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, szValueName, REG_VALUE_TYPE.REG_SZ, gs, gs.ChLen()));
			default:
				return HRESULT.E_FAIL;
		}
	}

	public HRESULT RegSetKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, uint dwValue, params object[] argList)
	{
		var szKeyName = string.Format(pszKeyFormatString, argList);
		var hr = _RegSetKeyValue(hkey, szKeyName, pszValueName, dwValue);
		_UpdateAssocChanged(hr, pszKeyFormatString);
		return hr;
	}

	public HRESULT RegSetKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, byte[] pc, uint dwSize, params object[] argList)
	{
		var szKeyName = string.Format(pszKeyFormatString, argList);
		var hr = _RegSetKeyValue(hkey, szKeyName, pszValueName, pc);
		_UpdateAssocChanged(hr, pszKeyFormatString);
		return hr;
	}

	public Guid? CLSID
	{
		get => _clsid;
		set
		{
			_clsid = value;
			_szCLSID = _clsid?.ToString("B").ToUpperInvariant();
		}
	}

	public void SetInstallScope(HKEY hkeyRoot)
	{
		// must be HKEY_CURRENT_USER or HKEY.HKEY_LOCAL_MACHINE
		_hkeyRoot = hkeyRoot;
	}

	[MemberNotNull(nameof(_szModule))]
	public void SetModule(string pszModule) => _szModule = pszModule;

	[MemberNotNull(nameof(_szModule))]
	public void SetModule() => SetModule(System.Reflection.Assembly.GetEntryAssembly()!.Location);

	// register a verb on an array of ProgIDs this is where the file assocation is being taken over adds the ProgID to a file extension
	// assuming that this ProgID will have the "open" verb under it that will be used in Open With
	public HRESULT UnRegisterKind(string pszFileExtension) => RegDeleteKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\KindMap", pszFileExtension);

	// use for ManualSafeSave = REG_DWORD:<1> EnableShareDenyNone = REG_DWORD:<1> EnableShareDenyWrite = REG_DWORD:<1>
	public HRESULT UnRegisterObject()
	{
		// might have an AppID value, try that
		if (_szCLSID is null) return HRESULT.E_FAIL;
		var hr = RegDeleteKeyPrintf(_hkeyRoot, "Software\\Classes\\AppID\\{0}", _szCLSID);
		if (hr.Succeeded)
		{
			hr = RegDeleteKeyPrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}", _szCLSID);
		}
		return hr;
	}

	// HKCR\<ProgID> = <Type Name> DefaultIcon=<icon ref> <icon ref>=<module path>,<res_id>
	public HRESULT UnRegisterProgID(string pszProgID, string pszFileExtension)
	{
		var hr = RegDeleteKeyPrintf(_hkeyRoot, "Software\\Classes\\{0}", pszProgID);
		if (hr.Succeeded && pszFileExtension != null)
		{
			hr = RegDeleteKeyPrintf(_hkeyRoot, "Software\\Classes\\{0}\\{1}", pszFileExtension, pszProgID);
		}
		return hr;
	}

	// in process context menu handler for right drag context menu need to create new method that allows out of proc handling of this
	public HRESULT UnRegisterPropertyHandler(string pszExtension) => RegDeleteKeyPrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\PropertySystem\\PropertyHandlers\\{0}", pszExtension);

	// must be an inproc handler registered here
	public HRESULT UnRegisterVerb(string pszProgID, string pszVerb) => RegDeleteKeyPrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}", pszProgID, pszVerb);

	public HRESULT UnRegisterVerbs(string[] rgpszAssociation, uint countAssociation, string pszVerb)
	{
		HRESULT hr = HRESULT.S_OK;
		for (uint i = 0; hr.Succeeded && (i < countAssociation); i++)
		{
			hr = UnRegisterVerb(rgpszAssociation[i], pszVerb);
		}

		if (hr.Succeeded && _clsid != Guid.Empty)
		{
			hr = UnRegisterObject();
		}
		return hr;
	}

	// register the COM local server for the current running module this is for self registering applications
	private static HRESULT HRESULT_FROM_WIN32(Win32Error err) => err.ToHRESULT();

	private HRESULT _EnsureBaseProgIDVerbIsNone(string pszProgID)
	{
		// putting the value of "none" that does not match any of the verbs under this key avoids those verbs from becoming the default.
		return _IsBaseClassProgID(pszProgID) ?
			RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell", "", "none", pszProgID) :
			HRESULT.S_OK;
	}

	private HRESULT _EnsureModule() => _szModule.Length > 0 ? HRESULT.S_OK : HRESULT.E_FAIL;

	private bool _IsBaseClassProgID(string pszProgID)
	{
		return !string.Equals(pszProgID, "AllFileSystemObjects", StringComparison.OrdinalIgnoreCase) ||
			   !string.Equals(pszProgID, "Directory", StringComparison.OrdinalIgnoreCase) ||
			   !string.Equals(pszProgID, "*", StringComparison.OrdinalIgnoreCase) ||
			   string.Equals(pszProgID, "SystemFileAssociations\\Directory.", StringComparison.OrdinalIgnoreCase);   // SystemFileAssociations\Directory.* values
	}

	// this sample registers its objects in the per user registry to avoid having to elevate
	private void _UpdateAssocChanged(HRESULT hr, string pszKeyFormatString)
	{
		const string c_szProgIDPrefix = "Software\\Classes\\";
		if (hr.Succeeded && !_fAssocChanged &&
			(pszKeyFormatString.StartsWith(c_szProgIDPrefix, StringComparison.OrdinalIgnoreCase) ||
			 pszKeyFormatString.Contains("PropertyHandlers") ||
			 pszKeyFormatString.Contains("KindMap")))
		{
			_fAssocChanged = true;
		}
	}

	// pszProtocol values: "*" - all "http" "ftp" "shellstream" - NYI in Win7 this enables drag drop directly onto the .exe, useful if
	// you have a shortcut to the exe somewhere (or the .exe is accessable via the send to menu) work around the missing "NeverDefault"
	// feature for verbs on downlevel platforms these ProgID values should need special treatment to keep the verbs registered there from
	// becoming default when indexing it is possible to override some of the file system property values, that includes the following use
	// this registration helper to set the override flag for each
	//
	// System.ItemNameDisplay System.SFGAOFlags System.Kind System.FileName System.ItemPathDisplay System.ItemPathDisplayNarrow
	// System.ItemFolderNameDisplay System.ItemFolderPathDisplay System.ItemFolderPathDisplayNarrow
}

internal static class StrExt
{
	private static uint charSize = Environment.Is64BitProcess ? 2u : 1u;

	public static uint ChLen(this string s) => charSize * (1 + (uint)(s?.Length ?? 0));
}
