using System.Diagnostics;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace KnownFolders;

internal class Program
{
	private const int IDC_ATTRIBUTES = 1016;
	private const int IDC_AUTOEXPAND = 1027;
	private const int IDC_BOX = 102;
	private const int IDC_BROWSER = 101;
	private const int IDC_BUTTON1 = 1004;
	private const int IDC_CANCEL = 1000;
	private const int IDC_COMBO1 = 1003;
	private const int IDC_DATETIME = 1000;
	private const int IDC_ENUMNAME = 1007;
	private const int IDC_ENUMPATH = 1008;
	private const int IDC_EXPANDOS = 1018;
	private const int IDC_EXPLORE = 1004;
	private const int IDC_FILTERPINNED = 1026;
	private const int IDC_FIND = 1001;
	private const int IDC_FOLDERNAME = 1010;
	private const int IDC_FULLROWSELECT = 1020;
	private const int IDC_HORIZONTALSCROLL = 1028;
	private const int IDC_IMAGE = 1014;
	private const int IDC_LBLFOLDER = 1012;
	private const int IDC_LBLPATH = 1013;
	private const int IDC_LINES = 1019;
	private const int IDC_NAME = 1015;
	private const int IDC_NEXT = 1005;
	private const int IDC_PADDING = 1025;
	private const int IDC_PATH = 1011;
	private const int IDC_SETTINGS = 1017;
	private const int IDC_STATIC = 103;
	private const int IDC_STATIC1 = 101;
	private const int IDC_STATUS = 1009;
	private const int IDD_DIALOG1 = 101;
	private const int IDS_EMPTY = 101;

	private static HINSTANCE g_hinst;

	[STAThread]
	public static void Main()
	{
		g_hinst = GetModuleHandle();
		if (OleInitialize().Succeeded) // for drag and drop
		{
			new CNameSpaceTreeHost().DoModal(default);
			OleUninitialize();
		}
	}

	private static void ClearDialogIcon(HWND hdlg)
	{
		DestroyIcon((HICON)SendMessage(hdlg, WindowMessage.WM_GETICON, WM_ICON_WPARAM.ICON_SMALL, IntPtr.Zero));
		DestroyIcon((HICON)SendMessage(hdlg, WindowMessage.WM_GETICON, WM_ICON_WPARAM.ICON_BIG, IntPtr.Zero));
	}

	private static void SetDialogIcon(HWND hdlg, SHSTOCKICONID siid)
	{
		SHSTOCKICONINFO sii = new() { cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO)) };
		if (SHGetStockIconInfo(siid, SHGSI.SHGSI_ICON | SHGSI.SHGSI_SMALLICON, ref sii).Succeeded)
		{
			SendMessage(hdlg, WindowMessage.WM_SETICON, WM_ICON_WPARAM.ICON_SMALL, (IntPtr)sii.hIcon);
		}
		if (SHGetStockIconInfo(siid, SHGSI.SHGSI_ICON | SHGSI.SHGSI_LARGEICON, ref sii).Succeeded)
		{
			SendMessage(hdlg, WindowMessage.WM_SETICON, WM_ICON_WPARAM.ICON_BIG, (IntPtr)sii.hIcon);
		}
	}

	// pass a default shell item to cleanup the allocated bitmap
	private static HRESULT SetItemImageImageInStaticControl(HWND hwndStatic, IShellItem? psi)
	{
		SafeHBITMAP hbmp = SafeHBITMAP.Null;
		HRESULT hr = HRESULT.S_OK;
		if (psi is not null)
		{
			IShellItemImageFactory? psiif = psi as IShellItemImageFactory;
			if (psiif is not null)
			{
				GetWindowRect(hwndStatic, out var rc);
				int dxdy = Math.Min(rc.right - rc.left, rc.bottom - rc.top); // make it square
				SIZE size = new(dxdy, dxdy);

				hr = psiif.GetImage(size, SIIGBF.SIIGBF_RESIZETOFIT, out hbmp);
			}
		}

		if (hr.Succeeded)
		{
			HGDIOBJ hgdiOld = (HGDIOBJ)SendMessage(hwndStatic, StaticMessage.STM_SETIMAGE, LoadImageType.IMAGE_BITMAP, hbmp.DangerousGetHandle());
			if (!hgdiOld.IsNull)
			{
				DeleteObject(hgdiOld); // if there was an old one clean it up
			}
		}

		return hr;
	}

	private static HRESULT ShellAttributesToString(SFGAO sfgaof, out string ppsz) { ppsz = sfgaof.ToString(); return 0; }

	[ComVisible(true)]
	private class CNameSpaceTreeHost : Shell32.IServiceProvider, INameSpaceTreeControlEvents
	{
		private const SFGAO sfgaofAll =
			SFGAO.SFGAO_CANCOPY |
			SFGAO.SFGAO_CANMOVE |
			SFGAO.SFGAO_CANLINK |
			SFGAO.SFGAO_STORAGE |
			SFGAO.SFGAO_CANRENAME |
			SFGAO.SFGAO_CANDELETE |
			SFGAO.SFGAO_HASPROPSHEET |
			SFGAO.SFGAO_DROPTARGET |
			SFGAO.SFGAO_ENCRYPTED |
			SFGAO.SFGAO_ISSLOW |
			SFGAO.SFGAO_GHOSTED |
			SFGAO.SFGAO_SHARE |
			SFGAO.SFGAO_READONLY |
			SFGAO.SFGAO_HIDDEN |
			// SFGAO_HASSUBFOLDER |
			SFGAO.SFGAO_REMOVABLE |
			SFGAO.SFGAO_COMPRESSED |
			SFGAO.SFGAO_BROWSABLE |
			SFGAO.SFGAO_NONENUMERATED |
			SFGAO.SFGAO_NEWCONTENT |
			SFGAO.SFGAO_LINK |
			SFGAO.SFGAO_STREAM |
			SFGAO.SFGAO_FILESYSTEM |
			SFGAO.SFGAO_FILESYSANCESTOR |
			SFGAO.SFGAO_FOLDER |
			SFGAO.SFGAO_STORAGEANCESTOR;

		private uint dwAdviseCookie = 0;
		private HWND hdlg = default;
		private INameSpaceTreeControl? pnstc = null;
		private INameSpaceTreeControl2? pnstc2 = null;

		public HRESULT DoModal(HWND hwnd)
		{
			DialogBoxParam(LoadLibraryEx("NameSpaceTreeSampleRes.dll", LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE), IDD_DIALOG1, hwnd, s_DlgProc);
			return HRESULT.S_OK;
		}

		// INameSpaceTreeControlEvents
		HRESULT INameSpaceTreeControlEvents.OnAfterContextMenu(IShellItem? psi, IContextMenu pcmIn, in Guid riid, out object? ppv)
		{
			Debug.Write("AfterContextMenu\n"); ppv = default; return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnAfterExpand(IShellItem psi)
		{
			Debug.Write("AfterExpand\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnBeforeContextMenu(IShellItem? psi, in Guid riid, out object? ppv)
		{
			Debug.Write("BeforeContextMenu\n"); ppv = default; return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnBeforeExpand(IShellItem psi)
		{
			Debug.Write("BeforeExpand\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnBeforeItemDelete(IShellItem psi)
		{
			Debug.Write("BeforeItemDelete\n"); return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnBeforeStateImageChange(IShellItem psi)
		{
			Debug.Write("BeforeStateImageChange\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnBeginLabelEdit(IShellItem psi)
		{
			Debug.Write("BeginLabelEdit\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnEndLabelEdit(IShellItem psi)
		{
			Debug.Write("EndLabelEdit\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnGetDefaultIconIndex(IShellItem psi, out int piDefaultIcon, out int piOpenIcon)
		{
			Debug.Write("GetDefaultIconIndex\n"); piDefaultIcon = piOpenIcon = 0; return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnGetToolTip(IShellItem psi, StringBuilder pszTip, int cchTip)
		{
			Debug.Write("GetToolTip\n"); return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnItemAdded(IShellItem psi, bool fIsRoot)
		{
			Debug.Write("ItemAdded\n"); return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnItemClick(IShellItem psi, NSTCEHITTEST nstceHitTest, NSTCECLICKTYPE nstceClickType) { Debug.Write("ItemClick\n"); return HRESULT.S_FALSE; }

		HRESULT INameSpaceTreeControlEvents.OnItemDeleted(IShellItem psi, bool fIsRoot)
		{
			Debug.Write("ItemDeleted\n"); return HRESULT.E_NOTIMPL;
		}

		HRESULT INameSpaceTreeControlEvents.OnItemStateChanged(IShellItem psi, NSTCITEMSTATE nstcisMask, NSTCITEMSTATE nstcisState)
		{
			Debug.Write("ItemStateChanged\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnItemStateChanging(IShellItem psi, NSTCITEMSTATE nstcisMask, NSTCITEMSTATE nstcisState)
		{
			Debug.Write("ItemStateChanging\n"); return HRESULT.S_OK;
		}

		HRESULT INameSpaceTreeControlEvents.OnKeyboardInput(uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			Debug.Write($"KeyboardInput: msg=0x{uMsg:X8}\n");
			return HRESULT.S_FALSE;
		}

		HRESULT INameSpaceTreeControlEvents.OnPropertyItemCommit(IShellItem psi)
		{
			Debug.Write("PropertyItemCommit\n"); return HRESULT.S_FALSE;
		}

		HRESULT INameSpaceTreeControlEvents.OnSelectionChanged(IShellItemArray psiaSelection)
		{
			IShellItem psi = psiaSelection.GetItemAt(0);
			IShellItem2? psi2 = psi as IShellItem2;
			if (psi2 is not null)
			{
				InspectItem(psi2);
			}
			return HRESULT.S_OK;
		}

		// IServiceProvider
		HRESULT Shell32.IServiceProvider.QueryService(in Guid guidService, in Guid riid, out IntPtr ppvObject)
		{
			ppvObject = default;
			return HRESULT.E_NOINTERFACE;
		}

		private IntPtr DlgProc(uint uMsg, IntPtr wParam, IntPtr _)
		{
			bool fRet = true;
			switch ((WindowMessage)uMsg)
			{
				case WindowMessage.WM_INITDIALOG:
					OnInitDlg();
					break;

				case WindowMessage.WM_COMMAND:
					fRet = OnCommand(Macros.LOWORD(wParam));
					break;

				case WindowMessage.WM_DESTROY:
					OnDestroyDlg();
					break;

				default:
					fRet = false;
					break;
			}
			return fRet ? (IntPtr)1 : IntPtr.Zero;
		}

		private void InitializeRootsAndControls()
		{
			pnstc!.RemoveAllRoots();

			bool fEnableStyleChange;
			NSTCSTYLE nsctsFlags;
			NSTCSTYLE2 nsctsFlags2;
			if (pnstc2 is not null)
			{
				fEnableStyleChange = true;
				pnstc2.GetControlStyle(NSTCSTYLE.NSTCS_HASEXPANDOS | NSTCSTYLE.NSTCS_HASLINES | NSTCSTYLE.NSTCS_FULLROWSELECT | NSTCSTYLE.NSTCS_HORIZONTALSCROLL | NSTCSTYLE.NSTCS_RICHTOOLTIP | NSTCSTYLE.NSTCS_AUTOHSCROLL | NSTCSTYLE.NSTCS_EMPTYTEXT, out nsctsFlags);
				pnstc2.GetControlStyle2(NSTCSTYLE2.NSTCS2_DISPLAYPADDING | NSTCSTYLE2.NSTCS2_DISPLAYPINNEDONLY | NSTCSTYLE2.NTSCS2_NOSINGLETONAUTOEXPAND, out nsctsFlags2);
			}
			else
			{
				// When running downlevel INameSpaceTreeControl2 may not be available Set styles to defaults.
				fEnableStyleChange = false;
				nsctsFlags = NSTCSTYLE.NSTCS_HASEXPANDOS | NSTCSTYLE.NSTCS_ROOTHASEXPANDO | NSTCSTYLE.NSTCS_FADEINOUTEXPANDOS | NSTCSTYLE.NSTCS_NOINFOTIP | NSTCSTYLE.NSTCS_ALLOWJUNCTIONS | NSTCSTYLE.NSTCS_SHOWSELECTIONALWAYS | NSTCSTYLE.NSTCS_FULLROWSELECT;
				nsctsFlags2 = NSTCSTYLE2.NSTCS2_DEFAULT;
			}
			SetCheckBoxState(IDC_EXPANDOS, (nsctsFlags & NSTCSTYLE.NSTCS_HASEXPANDOS) != 0, fEnableStyleChange);
			SetCheckBoxState(IDC_LINES, (nsctsFlags & NSTCSTYLE.NSTCS_HASLINES) != 0, fEnableStyleChange);
			SetCheckBoxState(IDC_FULLROWSELECT, (nsctsFlags & NSTCSTYLE.NSTCS_FULLROWSELECT) != 0, fEnableStyleChange);
			SetCheckBoxState(IDC_HORIZONTALSCROLL, (nsctsFlags & NSTCSTYLE.NSTCS_HORIZONTALSCROLL) != 0, fEnableStyleChange);
			SetCheckBoxState(IDC_PADDING, (nsctsFlags2 & NSTCSTYLE2.NSTCS2_DISPLAYPADDING) != 0, fEnableStyleChange);
			SetCheckBoxState(IDC_FILTERPINNED, (nsctsFlags2 & NSTCSTYLE2.NSTCS2_DISPLAYPINNEDONLY) != 0, fEnableStyleChange);
			SetCheckBoxState(IDC_AUTOEXPAND, (nsctsFlags2 & NSTCSTYLE2.NTSCS2_NOSINGLETONAUTOEXPAND) != 0, fEnableStyleChange);

			// CLSID_CommonPlacesFolder

			//HRESULT hr = SHCreateItemFromParsingName("shell:::{323CA680-C24D-4099-B94D-446DD2D7249E}", default, IID_PPV_ARGS(&psiFavorites));
			using PIDL pidlFull = new("shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}");
			IShellItem? psiFavorites = SHCreateItemFromIDList<IShellItem>(pidlFull);
			if (psiFavorites is not null)
			{
				// Add a visible root
				pnstc.AppendRoot(psiFavorites, SHCONTF.SHCONTF_NONFOLDERS, NSTCROOTSTYLE.NSTCRS_VISIBLE | NSTCROOTSTYLE.NSTCRS_EXPANDED, default); // ignore result

				IShellItem? psiDesktop = SHCreateItemInKnownFolder<IShellItem>(KNOWNFOLDERID.FOLDERID_Desktop);
				if (psiDesktop is not null)
				{
					// Add hidden root
					pnstc.AppendRoot(psiDesktop, SHCONTF.SHCONTF_FOLDERS, NSTCROOTSTYLE.NSTCRS_HIDDEN | NSTCROOTSTYLE.NSTCRS_EXPANDED, default); // ignore result
					Marshal.ReleaseComObject(psiDesktop);
				}
				Marshal.ReleaseComObject(psiFavorites);
			}
		}

		private void InspectItem(IShellItem2 psi)
		{
			SetItemImageImageInStaticControl(GetDlgItem(hdlg, IDC_IMAGE), psi); // ignore result

			string psz = psi.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
			SetDlgItemText(hdlg, IDC_NAME, psz);

			SetDlgItemText(hdlg, IDC_PATH, "");
			try
			{
				IShellLinkW psl = psi.BindToHandler<IShellLinkW>(default, BHID.BHID_SFUIObject);
				using PIDL pidl = psl.GetIDList();
				var hr = SHGetNameFromIDList(pidl, SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out psz);
				if (hr.Succeeded)
				{
					SetDlgItemText(hdlg, IDC_PATH, psz);
				}
				Marshal.ReleaseComObject(psl);
			}
			catch { }

			string pszAttributes = "";
			SFGAO sfgaof = psi.GetAttributes(sfgaofAll);
			ShellAttributesToString(sfgaof, out pszAttributes);
			SetDlgItemText(hdlg, IDC_ATTRIBUTES, pszAttributes);
		}

		private bool OnCommand(int id)
		{
			switch (id)
			{
				case (int)MB_RESULT.IDOK:
				case (int)MB_RESULT.IDCANCEL:
				case IDC_CANCEL:
					EndDialog(hdlg, (IntPtr)1);
					break;

				case IDC_EXPANDOS:
					pnstc2!.SetControlStyle(NSTCSTYLE.NSTCS_HASEXPANDOS, IsDlgButtonChecked(hdlg, IDC_EXPANDOS) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE.NSTCS_HASEXPANDOS : 0);
					InitializeRootsAndControls();
					break;

				case IDC_LINES:
					pnstc2!.SetControlStyle(NSTCSTYLE.NSTCS_HASLINES, IsDlgButtonChecked(hdlg, IDC_LINES) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE.NSTCS_HASLINES : 0);
					InitializeRootsAndControls();
					break;

				case IDC_FULLROWSELECT:
					pnstc2!.SetControlStyle(NSTCSTYLE.NSTCS_FULLROWSELECT, IsDlgButtonChecked(hdlg, IDC_FULLROWSELECT) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE.NSTCS_FULLROWSELECT : 0);
					InitializeRootsAndControls();
					break;

				case IDC_PADDING:
					pnstc2!.SetControlStyle2(NSTCSTYLE2.NSTCS2_DISPLAYPADDING, IsDlgButtonChecked(hdlg, IDC_PADDING) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE2.NSTCS2_DISPLAYPADDING : NSTCSTYLE2.NSTCS2_DEFAULT);
					InitializeRootsAndControls();
					break;

				case IDC_FILTERPINNED:
					pnstc2!.SetControlStyle2(NSTCSTYLE2.NSTCS2_DISPLAYPINNEDONLY, IsDlgButtonChecked(hdlg, IDC_FILTERPINNED) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE2.NSTCS2_DISPLAYPINNEDONLY : NSTCSTYLE2.NSTCS2_DEFAULT);
					InitializeRootsAndControls();
					break;

				case IDC_AUTOEXPAND:
					pnstc2!.SetControlStyle2(NSTCSTYLE2.NTSCS2_NOSINGLETONAUTOEXPAND, IsDlgButtonChecked(hdlg, IDC_AUTOEXPAND) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE2.NTSCS2_NOSINGLETONAUTOEXPAND : NSTCSTYLE2.NSTCS2_DEFAULT);
					InitializeRootsAndControls();
					break;

				case IDC_HORIZONTALSCROLL:
					pnstc2!.SetControlStyle(NSTCSTYLE.NSTCS_HORIZONTALSCROLL, IsDlgButtonChecked(hdlg, IDC_HORIZONTALSCROLL) == ButtonStateFlags.BST_CHECKED ? NSTCSTYLE.NSTCS_HORIZONTALSCROLL : 0);
					InitializeRootsAndControls();
					break;

				case IDC_EXPLORE:
					OnOpen();
					break;
			};
			return true;
		}

		private void OnDestroyDlg()
		{
			ClearDialogIcon(hdlg);

			// cleanup the allocated HBITMAP
			SetItemImageImageInStaticControl(GetDlgItem(hdlg, IDC_IMAGE), default);

			if (pnstc2 is not null)
			{
				pnstc2 = default;
			}
			if (pnstc is not null)
			{
				if (dwAdviseCookie != uint.MaxValue)
				{
					pnstc.TreeUnadvise(dwAdviseCookie);
					dwAdviseCookie = uint.MaxValue;
				}

				ShlwApi.IUnknown_SetSite(pnstc, default!);
				Marshal.ReleaseComObject(pnstc);
				pnstc = default;
			}
		}

		private void OnInitDlg()
		{
			SetDialogIcon(hdlg, SHSTOCKICONID.SIID_APPLICATION);

			HWND hwndStatic = GetDlgItem(hdlg, IDC_BROWSER);
			if (!hwndStatic.IsNull)
			{
				GetWindowRect(hwndStatic, out var rc);
				MapWindowRect(GetDesktopWindow(), hdlg, ref rc);

				pnstc = new();
				NSTCSTYLE nsctsFlags = NSTCSTYLE.NSTCS_HASEXPANDOS | // Show expandos
					NSTCSTYLE.NSTCS_ROOTHASEXPANDO | // Root nodes have expandos
					NSTCSTYLE.NSTCS_FADEINOUTEXPANDOS | // Fade-in-out based on focus
					NSTCSTYLE.NSTCS_RICHTOOLTIP | // NSTCS_NOINFOTIP | // Don't show infotips
					NSTCSTYLE.NSTCS_ALLOWJUNCTIONS | // Show folders such as zip folders and libraries
					NSTCSTYLE.NSTCS_SHOWSELECTIONALWAYS | // Show selection when NSC doesn't have focus
					NSTCSTYLE.NSTCS_FULLROWSELECT; // Select full width of item
				var hr = pnstc.Initialize(hdlg, rc, nsctsFlags);
				if (hr.Succeeded)
				{
					// New Windows 7 features
					pnstc2 = pnstc as INameSpaceTreeControl2;
					if (pnstc2 is not null)
					{
						NSTCSTYLE2 nscts2Flags = NSTCSTYLE2.NSTCS2_DISPLAYPADDING | // Padding between top-level nodes
							NSTCSTYLE2.NTSCS2_NOSINGLETONAUTOEXPAND | // Don't auto-expand nodes with a single child node
							NSTCSTYLE2.NSTCS2_INTERRUPTNOTIFICATIONS | // Register for interrupt notifications on a per-node basis
							NSTCSTYLE2.NSTCS2_DISPLAYPINNEDONLY | // Filter on pinned property
							NSTCSTYLE2.NTSCS2_NEVERINSERTNONENUMERATED; // Don't insert items with property SFGAO_NONENUMERATED
						hr = pnstc2.SetControlStyle2(nscts2Flags, nscts2Flags);
					}
					if (hr.Succeeded)
					{
						pnstc.TreeAdvise(this, out dwAdviseCookie);
						ShlwApi.IUnknown_SetSite(pnstc, this);

						InitializeRootsAndControls();
					}
				}
			}
		}

		private void OnOpen()
		{
			HRESULT hr = pnstc!.GetSelectedItems(out var psiaItems);
			if (hr.Succeeded)
			{
				IShellItem psi = psiaItems.GetItemAt(0);
				hr = SHGetIDListFromObject(psi, out var pidl);
				if (hr.Succeeded)
				{
					SHELLEXECUTEINFO ei = new()
					{
						cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
						fMask = ShellExecuteMaskFlags.SEE_MASK_INVOKEIDLIST,
						hwnd = hdlg,
						nShellExecuteShow = ShowWindowCommand.SW_NORMAL,
						lpIDList = (IntPtr)pidl
					};

					ShellExecuteEx(ref ei);
					pidl.Dispose();
				}
				Marshal.ReleaseComObject(psiaItems);
			}
		}

		private IntPtr s_DlgProc(HWND hdlg, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			if (uMsg == (uint)WindowMessage.WM_INITDIALOG)
			{
				this.hdlg = hdlg;
			}
			return DlgProc(uMsg, wParam, lParam);
		}

		private void SetCheckBoxState(int id, bool fChecked, bool fEnabled)
		{
			EnableWindow(GetDlgItem(hdlg, id), fEnabled);
			CheckDlgButton(hdlg, id, fChecked ? ButtonStateFlags.BST_CHECKED : ButtonStateFlags.BST_UNCHECKED);
		}
	}
}