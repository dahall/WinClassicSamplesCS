using System.Runtime.InteropServices.ComTypes;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComDlg32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace sendto;

static class Program
{
	static readonly ResourceId IDC_ARROW = Macros.MAKEINTRESOURCE(32512);

	static HINSTANCE g_hinst;                /* My hinstance */
	static IShellFolder? g_psfDesktop;         /* The desktop folder */
	static HMENU g_hmenuSendTo;          /* Our SendTo popup */
	static string? g_szFileName; /* The name of the "open" file */

	const int IDM_OPEN = 0x0100;
	const int IDM_SENDTOPOPUP = 0x0101;
	const int IDM_SENDTOFIRST = 0x0200;
	const int IDM_SENDTOLAST = 0x02FF;

	/*****************************************************************************
	*
	* GetSpecialFolder
	*
	* Create an IShellFolder for the specified special folder.
	*
	* It is the responsibility of the caller to Release() the
	* IShellFolder that is returned.
	*
	*****************************************************************************/
	static IShellFolder? GetSpecialFolder(HWND hwnd, CSIDL idFolder)
	{
		if (SHGetFolderLocation(hwnd, idFolder, ppidl: out var pidl).Succeeded)
		{
			using (pidl)
				if (g_psfDesktop!.BindToObject(pidl, default, typeof(IShellFolder).GUID, out var ppv).Succeeded)
					return (IShellFolder?)ppv;
		}
		return null;
	}

	/*****************************************************************************
	*
	* PidlFromPath
	*
	* Convert a path to an PIDL.
	*
	*****************************************************************************/
	static PIDL? PidlFromPath(HWND hwnd, string pszPath)
	{
		SFGAO dwAttributes = 0;
		return g_psfDesktop!.ParseDisplayName(hwnd, default, pszPath, out _, out var pidl, ref dwAttributes).Failed ? null : pidl;
	}

	/*****************************************************************************
	*
	* GetUIObjectOfAbsPidl
	*
	* Given an absolute (desktop-relative) PIDL, get the
	* specified UI object.
	*
	*****************************************************************************/
	static HRESULT GetUIObjectOfAbsPidl<T>(HWND hwnd, PIDL pidl, out T? ppvOut) where T : class
	{
		/*
		* To get the UI object of an absolute pidl, we must first bind
		* to its parent, and then call GetUIObjectOf on the last part.
		*/

		/*
		* Just for safety's sake.
		*/
		ppvOut = default;

		/*
		* Bind to the parent folder of the item we are interested in.
		*/
		var hres = SHBindToParent(pidl, typeof(IShellFolder).GUID, out var psf, out var pidlLast);
		if (hres.Failed)
		{
			/*
			* Couldn't even get to the parent; we have no chance of
			* getting to the item itself.
			*/
			return hres;
		}

		/*
		* Now ask the parent for the the UI object of the child.
		*/
		hres = ((IShellFolder)psf!).GetUIObjectOf(pidlLast, out ppvOut, hwnd);

		/*
		* Regardless of whether or not the GetUIObjectOf succeeded,
		* we have no further use for the parent folder.
		*/
		Marshal.ReleaseComObject(psf!);

		return hres;
	}

	/*****************************************************************************
	*
	* GetUIObjectOfPath
	*
	* Given an absolute path, get its specified UI object.
	*
	*****************************************************************************/
	static HRESULT GetUIObjectOfPath<T>(HWND hwnd, string pszPath, out T? ppvOut) where T : class
	{
		/*
		* Just for safety's sake.
		*/
		ppvOut = default;

		using var pidl = PidlFromPath(hwnd, pszPath);
		if (pidl is null)
		{
			return HRESULT.E_FAIL;
		}

		return GetUIObjectOfAbsPidl(hwnd, pidl, out ppvOut);
	}

	/*****************************************************************************
	*
	* DoDrop
	*
	* Drop a data object on a drop target.
	*
	*****************************************************************************/
	static void DoDrop(IDataObject pdto, IDropTarget pdt)
	{
		/*
		* The data object enters the drop target via the left button
		* with all drop effects permitted.
		*/
		var dwEffect = DROPEFFECT.DROPEFFECT_COPY | DROPEFFECT.DROPEFFECT_MOVE | DROPEFFECT.DROPEFFECT_LINK;
		var hres = pdt.DragEnter(pdto, MouseButtonState.MK_LBUTTON, System.Drawing.Point.Empty, ref dwEffect);
		if (hres.Succeeded && dwEffect != 0)
		{
			/*
			* The drop target likes the data object and the effect.
			* Go drop it.
			*/
			pdt.Drop(pdto, MouseButtonState.MK_LBUTTON, System.Drawing.Point.Empty, ref dwEffect);
		}
		else
		{
			/*
			* The drop target didn't like us. Tell it we're leaving,
			* sorry to bother you.
			*/
			pdt.DragLeave();
		}
	}

	/*****************************************************************************
	*
	* SendTo_OnCreate
	*
	* When we are created, remember the handle of our SendTo menu
	* so we can recognize it later.
	*
	*****************************************************************************/
	static bool SendTo_OnCreate(HWND hwnd)
	{
		var mii = new MENUITEMINFO
		{
			cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
			fMask = MenuItemInfoMask.MIIM_SUBMENU
		};

		var hmenu = GetMenu(hwnd);
		if (GetMenuItemInfo(hmenu, IDM_SENDTOPOPUP, false, ref mii))
		{
			g_hmenuSendTo = mii.hSubMenu;
		}

		return true;
	}

	/*****************************************************************************
	*
	* SendTo_ResetSendToMenu
	*
	* Wipe out all the items in the menu, freeing the associated memory.
	*
	*****************************************************************************/
	static void SendTo_ResetSendToMenu(HMENU hmenu)
	{
		var mii = new MENUITEMINFO
		{
			cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
			fMask = MenuItemInfoMask.MIIM_DATA
		};

		while (GetMenuItemInfo(hmenu, 0, true, ref mii))
		{
			if (mii.dwItemData != default)
			{
				CoTaskMemFree(mii.dwItemData);
			}
			DeleteMenu(hmenu, 0, MenuFlags.MF_BYPOSITION);
		}
	}

	/*****************************************************************************
	*
	* SendTo_FillSendToMenu
	*
	* Enumerate the contents of the SendTo folder and fill the
	* menu with the items therein.
	*
	*****************************************************************************/
	static void SendTo_FillSendToMenu(HWND hwnd, HMENU hmenu)
	{
		uint idm = IDM_SENDTOFIRST;

		var psf = GetSpecialFolder(hwnd, CSIDL.CSIDL_SENDTO);
		if (psf is not null)
		{
			try
			{
				var hres = psf.EnumObjects(hwnd, SHCONTF.SHCONTF_FOLDERS | SHCONTF.SHCONTF_NONFOLDERS, out var peidl);
				if (hres.Succeeded)
				{
					try
					{
						var pidls = new IntPtr[1];
						while (peidl!.Next(1, pidls, out _) == HRESULT.S_OK && idm < IDM_SENDTOLAST)
						{
							hres = psf.GetDisplayNameOf(pidls[0], SHGDNF.SHGDN_NORMAL, out var str);
							if (hres.Succeeded)
							{
								if (AppendMenu(hmenu, MenuFlags.MF_ENABLED | MenuFlags.MF_STRING, (IntPtr)idm, str!))
								{
									var mii = new MENUITEMINFO
									{
										cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
										fMask = MenuItemInfoMask.MIIM_DATA,
										dwItemData = pidls[0]
									};
									SetMenuItemInfo(hmenu, idm, false, mii);
									idm++;
								}
							}
						}
					}
					finally
					{
						Marshal.ReleaseComObject(peidl!);
					}
				}
			}
			finally
			{
				Marshal.ReleaseComObject(psf);
			}
		}

		/*
		* If the menu is still empty (the user has an empty SendTo folder),
		* then add a disabled "None" item so we have at least something
		* to display.
		*/
		if (idm == IDM_SENDTOFIRST)
		{
			AppendMenu(hmenu, MenuFlags.MF_GRAYED | MenuFlags.MF_DISABLED | MenuFlags.MF_STRING, (IntPtr)idm, "(none)");
		}
	}

	/*****************************************************************************
	*
	* SendTo_OnInitMenuPopup
	*
	* When the SendTo menu pops up, enumerate the contents of the
	* SendTo folder and populate the menu.
	*
	*****************************************************************************/
	static void SendTo_OnInitMenuPopup(HWND hwnd, HMENU hmenu)
	{
		/*
		* If it's the SendTo menu, then rebuild it.
		*/
		if (hmenu == g_hmenuSendTo)
		{
			SendTo_ResetSendToMenu(hmenu);
			SendTo_FillSendToMenu(hwnd, hmenu);
		}
	}

	/*****************************************************************************
	*
	* SendTo_OnOpen
	*
	* "Open" a file. Just get its name and save it in our global
	* g_szFileName variable. (And update the title too, just to
	* look pretty.)
	*
	*****************************************************************************/
	static void SendTo_OnOpen(HWND hwnd)
	{
		var szFileName = new SafeCoTaskMemString(g_szFileName, MAX_PATH, CharSet.Auto);
		var szTitle = new SafeCoTaskMemString(40, CharSet.Auto);

		var ofn = new OPENFILENAME
		{
			lStructSize = (uint)Marshal.SizeOf<OPENFILENAME>(),
			hwndOwner = hwnd,
			lpstrFile = (IntPtr)szFileName,
			nMaxFile = (uint)szFileName.Capacity,
			lpstrFileTitle = (IntPtr)szTitle,
			nMaxFileTitle = (uint)szTitle.Capacity,
			Flags = OFN.OFN_FILEMUSTEXIST | OFN.OFN_HIDEREADONLY | OFN.OFN_PATHMUSTEXIST
		};
		if (GetOpenFileName(ref ofn))
		{
			g_szFileName = szFileName;
			SetWindowText(hwnd, $"{szTitle} - SendTo Demo");
		}
	}

	/*****************************************************************************
	*
	* SendTo_SendToItem
	*
	* The user selected an item from our SendTo menu.
	*
	* hwnd - window
	* idm - menu item id
	*
	*****************************************************************************/
	static void SendTo_SendToItem(HWND hwnd, uint idm)
	{
		/*
		* First convert our filename to a data object.
		*/
		var hres = GetUIObjectOfPath<IDataObject>(hwnd, g_szFileName!, out var pdto);
		if (hres.Succeeded)
		{
			try
			{
				/*
				* Now go find the item we should send to.
				*/
				var mii = new MENUITEMINFO
				{
					cbSize = (uint)Marshal.SizeOf<MENUITEMINFO>(),
					fMask = MenuItemInfoMask.MIIM_DATA
				};
				if (GetMenuItemInfo(g_hmenuSendTo, idm, false, ref mii) && mii.dwItemData != default)
				{
					/*
					* Now convert the send to pidl to a drop target.
					*/
					var psf = GetSpecialFolder(hwnd, CSIDL.CSIDL_SENDTO);
					if (psf is not null)
					{
						try
						{
							hres = psf.GetUIObjectOf<IDropTarget>(mii.dwItemData, out var pdt, hwnd);
							if (hres.Succeeded)
							{
								/*
								* Now drop the file on the drop target.
								*/
								DoDrop(pdto!, pdt!);
								Marshal.ReleaseComObject(pdt!);
							}
						}
						finally
						{
							Marshal.ReleaseComObject(psf);
						}
					}
				}
			}
			finally
			{
				Marshal.ReleaseComObject(pdto!);
			}
		}
	}

	/*****************************************************************************
	*
	* SendTo_OnCommand
	*
	* Handle our menu messages.
	*
	* Our "Send To" commands live in the range
	* IDM_SENDTOFIRST through IDM_SENDTOLAST.
	*
	*****************************************************************************/
	static void SendTo_OnCommand(HWND hwnd, uint id)
	{
		if (id >= IDM_SENDTOFIRST && id <= IDM_SENDTOLAST)
		{
			SendTo_SendToItem(hwnd, id);
		}
		else if (id == IDM_OPEN)
		{
			SendTo_OnOpen(hwnd);
		}
	}

	/*****************************************************************************
	*
	* SendTo_OnDestroy
	*
	* When our window is destroyed, clean up the memory associated
	* with the SendTo submenu.
	*
	* Also post a quit message because our application is over.
	*
	*****************************************************************************/
	static void SendTo_OnDestroy(HWND hwnd)
	{
		if (!g_hmenuSendTo.IsNull)
		{
			SendTo_ResetSendToMenu(g_hmenuSendTo);
		}
		PostQuitMessage(0);
	}

	/*****************************************************************************
	*
	* SendTo_WndProc
	*
	* Window procedure for the Send To demo.
	*
	*****************************************************************************/
	static IntPtr SendTo_WndProc(HWND hwnd, uint uiMsg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)uiMsg)
		{
			case WindowMessage.WM_CREATE: SendTo_OnCreate(hwnd); break;
			case WindowMessage.WM_COMMAND: SendTo_OnCommand(hwnd, Macros.LOWORD(wParam)); break;
			case WindowMessage.WM_INITMENUPOPUP: SendTo_OnInitMenuPopup(hwnd, wParam); break;
			case WindowMessage.WM_DESTROY: SendTo_OnDestroy(hwnd); break;
		}
		return DefWindowProc(hwnd, uiMsg, wParam, lParam);
	}

	/*****************************************************************************
	*
	* InitApp
	*
	* Register our window classes and otherwise prepare for action.
	*
	*****************************************************************************/
	static bool InitApp()
	{
		WNDCLASS wc = new()
		{
			lpfnWndProc = SendTo_WndProc,
			hInstance = g_hinst,
			hCursor = LoadCursor(default, IDC_ARROW),
			hbrBackground = (IntPtr)(int)(SystemColorIndex.COLOR_WINDOW + 1),
			//lpszMenuName = "SendToMenu",
			lpszClassName = "SendTo"
		};

		RegisterClass(wc);

		return SHGetDesktopFolder(out g_psfDesktop).Succeeded;
	}

	/*****************************************************************************
	*
	* TermApp
	*
	* Clean up.
	*
	*****************************************************************************/
	static void TermApp()
	{
		if (g_psfDesktop is not null)
		{
			Marshal.ReleaseComObject(g_psfDesktop);
			g_psfDesktop = null;
		}
	}

	/*****************************************************************************
	*
	* WinMain
	*
	* Program entry point.
	*
	* Demonstrate Send To menu.
	*
	*****************************************************************************/
	static int Main()
	{
		g_hinst = GetModuleHandle();

		if (!InitApp()) return 0;

		var hrInit = CoInitialize(default);

		var hmenu = CreateMenu();
		var hFileMenu = CreatePopupMenu();
		if (AppendMenu(hmenu, MenuFlags.MF_POPUP | MenuFlags.MF_STRING, hFileMenu.DangerousGetHandle(), "&File"))
		{
			AppendMenu(hFileMenu, MenuFlags.MF_STRING, (IntPtr)IDM_OPEN, "&Open");
			var hSendToMenu = CreatePopupMenu();
			if (AppendMenu(hFileMenu, MenuFlags.MF_STRING | MenuFlags.MF_POPUP, hSendToMenu.DangerousGetHandle(), "Se&nd To"))
			{
				SetMenuItemInfo(hFileMenu, 1, true, new MENUITEMINFO(IDM_SENDTOPOPUP));
				hSendToMenu.SetHandleAsInvalid();
			}
			hFileMenu.SetHandleAsInvalid();
			AppendMenu(hSendToMenu, MenuFlags.MF_STRING, (IntPtr)IDM_SENDTOFIRST, "(none)");
		}

		var hwnd = CreateWindow("SendTo", "SendTo Demo", WindowStyles.WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, default, hmenu, g_hinst, default);
		hmenu.SetHandleAsInvalid();
		ShowWindow(hwnd, ShowWindowCommand.SW_NORMAL);

		try
		{
			MSG msg;
			while (GetMessage(out msg, default, 0, 0) != 0)
			{
				TranslateMessage(msg);
				DispatchMessage(msg);
			}
		}
		finally
		{
			TermApp();

			if (hrInit.Succeeded)
				CoUninitialize();
		}

		return 0;
	}
}