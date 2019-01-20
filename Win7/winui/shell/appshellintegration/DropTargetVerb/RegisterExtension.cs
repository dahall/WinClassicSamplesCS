using System;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace DropTargetVerb
{
	public class CRegisterExtension
	{
		private Guid _clsid;
		private bool _fAssocChanged;
		private HKEY _hkeyRoot;
		private string _szCLSID;
		private string _szModule;

		public CRegisterExtension(in Guid clsid = default, HKEY hkeyRoot = default)
		{
			_hkeyRoot = hkeyRoot == default ? HKEY.HKEY_CURRENT_USER : hkeyRoot;
			SetHandlerCLSID(clsid);
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

		public HRESULT RegisterAppAsLocalServer(string pszFriendlyName, string pszCmdLine = null)
		{
			var hr = _EnsureModule();
			if (hr.Succeeded)
			{
				var szCmdLine = new StringBuilder(MAX_PATH + 20);
				if (pszCmdLine != null)
				{
					szCmdLine.AppendFormat("{0} {1}", _szModule, pszCmdLine);
				}
				else
				{
					szCmdLine.Clear();
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
			if (hr.Succeeded)
			{
				// Windows7 supports per user App Paths, downlevel requires HKLM
				hr = RegSetKeyValuePrintf(_hkeyRoot,
					"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\{0}",
					"DropTarget", _szCLSID, System.IO.Path.GetFileName(_szModule));
			}
			return hr;
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
					hr = (HRESULT)PathAppend(szPath, szName);
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
			if (hr.Succeeded)
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
			if (hr.Succeeded)
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
			return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\Shell\\{1}",
				"CommandStateHandler", _szCLSID, pszProgID, pszVerb);
		}

		public HRESULT RegisterExplorerCommandVerb(string pszProgID, string pszVerb, string pszVerbDisplayName)
		{
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

		public HRESULT RegisterHandlerSupportedProtocols(string pszProtocol) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}\\SupportedProtocols", pszProtocol, "", _szCLSID);

		public HRESULT RegisterInProcServer(string pszFriendlyName, string pszThreadingModel)
		{
			var hr = _EnsureModule();
			if (hr.Succeeded)
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

		public HRESULT RegisterInProcServerAttribute(string pszAttribute, uint dwValue) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}", pszAttribute, dwValue, _szCLSID);

		// define the kind of a file extension. this is a multi-value property, see HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\explorer\KindMap
		public HRESULT RegisterKind(string pszFileExtension, string pszKindValue) => RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\KindMap", pszFileExtension, pszKindValue);

		// IResolveShellLink handler, used for custom link resolution behavior
		public HRESULT RegisterLinkHandler(string pszProgID) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\ShellEx\\LinkHandler", "", _szCLSID, pszProgID);

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
			var hr = RegisterAppAsLocalServer(pszTitle, null);
			if (hr.Succeeded)
			{
				// enable this handler to work with OpenSearch results, avoiding the downlaod and open behavior by indicating that we can
				// accept all URL forms
				hr = RegisterHandlerSupportedProtocols("*");

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

			return RegSetKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\PropertySystem\\PropertyHandlers\\{0}",
				"", _szCLSID, pszExtension);
		}

		public HRESULT RegisterPropertyHandlerOverride(string pszProperty) => RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\CLSID\\{0}\\OverrideFileSystemProperties", pszProperty, 1, _szCLSID);

		// pszProgID "Folder" or "Directory"
		public HRESULT RegisterRightDragContextMenuHandler(string pszProgID, string pszDescription)
		{
			return RegSetKeyValuePrintf(_hkeyRoot, "Software\\Classes\\{0}\\shellex\\DragDropHandlers\\{1}",
				"", pszDescription, pszProgID, _szCLSID);
		}

		public HRESULT RegisterThumbnailHandler(string pszExtension)
		{
			// IThumbnailHandler HKEY_CLASSES_ROOT\.wma\ShellEx\{e357fccd-a995-4576-b01f-234630154e96}={9DBD2C50-62AD-11D0-B806-00C04FD706EC}

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
			var pbDecodedImage = System.Convert.FromBase64String(pszBase64);
			var hr = HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, pszValueName, REG_VALUE_TYPE.REG_BINARY, pbDecodedImage, (uint)pbDecodedImage.Length));
			_UpdateAssocChanged(hr, pszKeyFormatString);
			return hr;
		}

		public HRESULT RegSetKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, string pszValue, params object[] argList)
		{
			var szKeyName = string.Format(pszKeyFormatString, argList);
			var hr = HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, pszValueName, REG_VALUE_TYPE.REG_SZ, pszValue, pszValue.ChLen()));
			_UpdateAssocChanged(hr, pszKeyFormatString);
			return hr;
		}

		public HRESULT RegSetKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, uint dwValue, params object[] argList)
		{
			var szKeyName = string.Format(pszKeyFormatString, argList);
			var hr = HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, pszValueName, REG_VALUE_TYPE.REG_DWORD, BitConverter.GetBytes(dwValue), sizeof(uint)));
			_UpdateAssocChanged(hr, pszKeyFormatString);
			return hr;
		}

		public HRESULT RegSetKeyValuePrintf(HKEY hkey, string pszKeyFormatString, string pszValueName, byte[] pc, uint dwSize, params object[] argList)
		{
			var szKeyName = string.Format(pszKeyFormatString, argList);
			var hr = HRESULT_FROM_WIN32(RegSetKeyValue(hkey, szKeyName, pszValueName, REG_VALUE_TYPE.REG_BINARY, pc, dwSize));
			_UpdateAssocChanged(hr, pszKeyFormatString);
			return hr;
		}

		public void SetHandlerCLSID(in Guid clsid)
		{
			_clsid = clsid;
			_szCLSID = _clsid.ToString("B").ToUpperInvariant();
		}

		public void SetInstallScope(HKEY hkeyRoot)
		{
			// must be HKEY_CURRENT_USER or HKEY.HKEY_LOCAL_MACHINE
			_hkeyRoot = hkeyRoot;
		}

		public void SetModule(string pszModule)
		{
			_szModule = pszModule;
		}

		public void SetModule() => SetModule(System.Reflection.Assembly.GetEntryAssembly().Location);

		// register a verb on an array of ProgIDs this is where the file assocation is being taken over adds the ProgID to a file extension
		// assuming that this ProgID will have the "open" verb under it that will be used in Open With
		public HRESULT UnRegisterKind(string pszFileExtension) => RegDeleteKeyValuePrintf(HKEY.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\KindMap", pszFileExtension);

		// use for ManualSafeSave = REG_DWORD:<1> EnableShareDenyNone = REG_DWORD:<1> EnableShareDenyWrite = REG_DWORD:<1>
		public HRESULT UnRegisterObject()
		{
			// might have an AppID value, try that
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

		private HRESULT _EnsureModule() => (HRESULT)(_szModule.Length > 0);

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
}