using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace ShellLibraryBackup;

internal class Program
{
	// From resource.h
	private const int IDS_ERRORMESSAGENOTLIBRARY = 100;
	private const int IDS_ERRORMESSAGENOCONTENT = 101;
	private const int IDS_APP_TITLE = 102;
	private const int IDS_MAINTITLE = 103;
	private const int IDS_BACKUPTITLE = 104;
	private const int IDD_MainPage = 200;
	private const int IDD_BackupPage = 201;
	private const int IDC_BACKUP = 1000;
	private const int IDC_RESTORE = 1001;
	private const int IDC_BACKUPTREE = 1002;
	private const int IDC_BACKUPADDDIR = 1003;
	private const int IDC_BACKUPREMOVEDIR = 1004;
	private const int IDC_STATIC = -1;

	private const int MAX_TREE_DEPTH = 255;

	// INamespaceWalkCB implementation
	[ComVisible(true)]
	internal class CNameSpaceWalkCB : INamespaceWalkCB2
	{
		private int iCurTreeDepth = 0;
		private HTREEITEM[] rghTreeParentItemArray = new HTREEITEM[MAX_TREE_DEPTH];
		private HWND hwndTreeView;

		public CNameSpaceWalkCB(HWND hwndTreeView, HTREEITEM hTreeParent)
		{
			this.hwndTreeView = hwndTreeView;
			rghTreeParentItemArray[iCurTreeDepth] = hTreeParent;
		}

		public HRESULT FoundItem(IShellFolder psf, IntPtr pidl)
		{
			IShellItem? psi = SHCreateItemWithParent<IShellItem>(psf, new(pidl));
			HRESULT hr = HRESULT.S_OK;
			if (psi is not null)
			{
				hr = AddItemToTreeView(hwndTreeView, psi, rghTreeParentItemArray[iCurTreeDepth]).IsNull ? HRESULT.E_FAIL : HRESULT.S_OK;
				Marshal.ReleaseComObject(psi);
			}
			return hr;
		}

		public HRESULT EnterFolder(IShellFolder psf, IntPtr pidl)
		{
			HRESULT hr = (iCurTreeDepth < rghTreeParentItemArray.Length - 1) ? HRESULT.S_OK : HRESULT.E_FAIL;
			if (hr.Succeeded)
			{
				IShellItem? psi = SHCreateItemWithParent<IShellItem>(psf, new(pidl));
				if (psi is not null)
				{
					rghTreeParentItemArray[iCurTreeDepth + 1] = AddItemToTreeView(hwndTreeView, psi, rghTreeParentItemArray[iCurTreeDepth]);
					hr = rghTreeParentItemArray[iCurTreeDepth + 1].IsNull ? HRESULT.E_FAIL : HRESULT.S_OK;
					iCurTreeDepth++;
					Marshal.ReleaseComObject(psi);
				}
			}
			return hr;
		}

		public HRESULT LeaveFolder(IShellFolder psf, IntPtr pidl)
		{
			HRESULT hr = iCurTreeDepth > 0 ? HRESULT.S_OK : HRESULT.E_FAIL;
			if (hr.Succeeded)
			{
				rghTreeParentItemArray[iCurTreeDepth--] = default;
			}
			return hr;
		}


		public HRESULT InitializeProgressDialog(out string? ppszTitle, out string? ppszCancel)
		{
			ppszTitle = ppszCancel = default;
			return HRESULT.E_NOTIMPL;
		}

		HRESULT INamespaceWalkCB2.WalkComplete(HRESULT hr) => HRESULT.S_OK;
	}

	static HRESULT DoBackup(HWND hwnd)
	{
		HRESULT hr = ShowBackupFolderPicker(hwnd, out var psi);
		if (hr.Succeeded)
		{
			HWND hwndTreeView = GetDlgItem(hwnd, IDC_BACKUPTREE);
			hr = !hwndTreeView.IsNull ? HRESULT.S_OK : HRESULT.E_FAIL;
			if (hr.Succeeded)
			{
				// Enumerate the locations if the folder is a library
				if (SHLoadLibraryFromItem(psi!, STGM.STGM_READ, typeof(IShellLibrary).GUID, out var ppv).Succeeded)
				{
					IShellLibrary psl = (IShellLibrary)ppv!;
					// Make sure the library is up-to-date
					SHResolveLibrary(psi!);

					HTREEITEM htiLibrary = AddItemToTreeView(hwndTreeView, psi!, HTREEITEM.TVI_ROOT);
					hr = !htiLibrary.IsNull ? HRESULT.S_OK : HRESULT.E_FAIL;
					if (hr.Succeeded)
					{
						IShellItemArray? psiaFolders = psl.GetFolders<IShellItemArray>(LIBRARYFOLDERFILTER.LFF_FORCEFILESYSTEM);
						if (psiaFolders is not null)
						{
							uint cFolders = psiaFolders.GetCount();
							if (hr.Succeeded)
							{
								uint cFoldersAdded = 0;
								for (uint i = 0; hr.Succeeded && i < cFolders; i++)
								{
									IShellItem psiTemp = psiaFolders.GetItemAt(i);
									if (hr.Succeeded)
									{
										// Walk the contents of this location
										hr = WalkFolderContents(hwndTreeView, psiTemp, htiLibrary);
										if (hr.Succeeded)
										{
											cFoldersAdded++;
										}
										Marshal.ReleaseComObject(psiTemp);
									}
								}

								if (cFoldersAdded != 0)
								{
									TreeView_SortChildren(hwndTreeView, htiLibrary, false);
								}
							}
							Marshal.ReleaseComObject(psiaFolders);
						}
					}
					Marshal.ReleaseComObject(psl);
				}
				else
				{
					hr = WalkFolderContents(hwndTreeView, psi!, HTREEITEM.TVI_ROOT);
					if (hr.Succeeded)
					{
						TreeView_SortChildren(hwndTreeView, HTREEITEM.TVI_ROOT, false);
					}
				}
			}
			Marshal.ReleaseComObject(psi!);
		}
		return hr;
	}

	// Folder picker
	private static HRESULT ShowBackupFolderPicker(HWND hwnd, out IShellItem? ppsi)
	{
		IFileOpenDialog pfod = new();
		try
		{
			pfod.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
			pfod.Show(hwnd);
			ppsi = pfod.GetResult();
		}
		catch (Exception ex) {
			ppsi = null;
			return ex.HResult;
		}
		finally
		{
			Marshal.ReleaseComObject(pfod);
		}
		return HRESULT.S_OK;
	}

	private static HRESULT WalkFolderContents(HWND hwndTreeView, IShellItem psiItem, HTREEITEM hTreeParent)
	{
		INamespaceWalkCB pnswcb = new CNameSpaceWalkCB(hwndTreeView, hTreeParent);
		INamespaceWalk pnsw = new();
		var hr = pnsw.Walk(psiItem, NAMESPACEWALKFLAG.NSWF_ASYNC | NAMESPACEWALKFLAG.NSWF_DONT_TRAVERSE_LINKS | NAMESPACEWALKFLAG.NSWF_DONT_ACCUMULATE_RESULT, MAX_TREE_DEPTH, pnswcb);
		Marshal.ReleaseComObject(pnsw);
		//Marshal.ReleaseComObject(pnswcb);
		return hr;
	}

	private static HTREEITEM AddItemToTreeView(HWND hwndTreeView, IShellItem psiItem, HTREEITEM hTreeParent)
	{
		using SafeLPTSTR pszName = new(psiItem.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));
		TVINSERTSTRUCT tvins = new()
		{
			hInsertAfter = HTREEITEM.TVI_LAST,
			hParent = hTreeParent,
			itemex = new() { mask = TreeViewItemMask.TVIF_TEXT, pszText = pszName }
		};
		return TreeView_InsertItem(hwndTreeView, ref tvins);
	}

	[STAThread]
	private static void Main()
	{
		int cPages = 0;
		SafeNativeArray<HPROPSHEETPAGE> rhpsp = new(2);
		using SafeHINSTANCE g_hinst = LoadLibraryEx("ShellLibraryBackupRes.dll", LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE);
		if (g_hinst.IsNull) throw Win32Error.GetLastError().GetException()!;

		// Create the main page of the wizard
		PROPSHEETPAGE psp = new()
		{
			dwSize = (uint)Marshal.SizeOf(typeof(PROPSHEETPAGE)),
			dwFlags = PropSheetFlags.PSP_USEHEADERTITLE,
			hInstance = g_hinst,
			pszTemplate = IDD_MainPage,
			pfnDlgProc = MainPageDialogProc,
			pszHeaderTitle = IDS_MAINTITLE
		};

		rhpsp[cPages] = CreatePropertySheetPage(psp);
		if (!rhpsp[cPages].IsNull)
		{
			cPages++;

			// Create the Backup page of the wizard
			psp.dwFlags = PropSheetFlags.PSP_USEHEADERTITLE;
			psp.pszTemplate = IDD_BackupPage;
			psp.pfnDlgProc = BackupDialogProc;
			psp.pszHeaderTitle = IDS_BACKUPTITLE;

			rhpsp[cPages] = CreatePropertySheetPage(psp);
			if (!rhpsp[cPages].IsNull)
			{
				cPages++;
			}
		}

		if (cPages == rhpsp.Count)
		{
			PROPSHEETHEADER psh = new()
			{
				dwSize = (uint)Marshal.SizeOf(typeof(PROPSHEETHEADER)),
				dwFlags = PropSheetHeaderFlags.PSH_AEROWIZARD | PropSheetHeaderFlags.PSH_WIZARD,
				hInstance = g_hinst,
				nPages = (uint)rhpsp.Count,
				phpage = rhpsp,
				pszCaption = IDS_APP_TITLE
			};
			PropertySheet(ref psh);
		}
		else
		{
			for (int iPage = 0; iPage < cPages; iPage++)
			{
				DestroyPropertySheetPage(rhpsp[iPage]);
			}
		}
	}

	private static IntPtr MainPageDialogProc(HWND hwndDialog, uint uMsg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)uMsg)
		{
			case WindowMessage.WM_NOTIFY:
				{
					ref NMHDR pnmh = ref lParam.AsRef<NMHDR>();
					switch ((PropSheetNotification)pnmh.code)
					{
						case PropSheetNotification.PSN_SETACTIVE:
							PropSheet_ShowWizButtons(hwndDialog, PSWIZB.PSWIZB_SHOW,
								PSWIZB.PSWIZB_BACK | PSWIZB.PSWIZB_CANCEL | PSWIZB.PSWIZB_FINISH | PSWIZB.PSWIZB_NEXT | PSWIZB.PSWIZB_RESTORE);
							break;
					}
				}
				break;

			case WindowMessage.WM_COMMAND:
				switch (Macros.LOWORD(wParam))
				{
					case IDC_BACKUP:
						PropSheet_ShowWizButtons(hwndDialog, PSWIZB.PSWIZB_NEXT, PSWIZB.PSWIZB_NEXT);
						SendMessage(hwndDialog, PropSheetMessage.PSM_PRESSBUTTON, PSBTN.PSBTN_NEXT);
						break;
				}
				break;
		}
		return default;
	}

	private static IntPtr BackupDialogProc(HWND hwndDialog, uint uMsg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)uMsg)
		{
			case WindowMessage.WM_NOTIFY:
				{
					ref NMHDR pnmh = ref lParam.AsRef<NMHDR>();
					switch ((PropSheetNotification)pnmh.code)
					{
						case PropSheetNotification.PSN_SETACTIVE:
							PropSheet_ShowWizButtons(hwndDialog, PSWIZB.PSWIZB_BACK | PSWIZB.PSWIZB_FINISH,
								PSWIZB.PSWIZB_BACK | PSWIZB.PSWIZB_CANCEL | PSWIZB.PSWIZB_FINISH | PSWIZB.PSWIZB_NEXT | PSWIZB.PSWIZB_RESTORE);
							break;
						case PropSheetNotification.PSN_WIZBACK:
							TreeView_DeleteAllItems(GetDlgItem(hwndDialog, IDC_BACKUPTREE));
							break;
					}
				}
				break;

			case WindowMessage.WM_COMMAND:
				switch (Macros.LOWORD(wParam))
				{
					case IDC_BACKUPADDDIR:
						DoBackup(hwndDialog);
						break;

					case IDC_BACKUPREMOVEDIR:
						{
							HWND hwndTreeView = GetDlgItem(hwndDialog, IDC_BACKUPTREE);
							if (!hwndTreeView.IsNull)
							{
								HTREEITEM hTreeItem = TreeView_GetSelection(hwndTreeView);
								if (!hTreeItem.IsNull)
								{
									TreeView_DeleteItem(hwndTreeView, hTreeItem);
								}
							}
						}
						break;
				}
				break;
		}
		return default;
	}
}
