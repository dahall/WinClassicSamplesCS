using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace Vanara.Extensions;

public static class ShellHelpers
{
	// free the HICON that was set using SetDialogIcon()
	public static void ClearDialogIcon(HWND hdlg)
	{
		DestroyIcon((HICON)SendMessage(hdlg, WindowMessage.WM_GETICON, WM_ICON_WPARAM.ICON_SMALL, IntPtr.Zero));
		DestroyIcon((HICON)SendMessage(hdlg, WindowMessage.WM_GETICON, WM_ICON_WPARAM.ICON_BIG, IntPtr.Zero));
	}

	// remote COM methods are dispatched in the context of an exception handler that consumes all SEH exceptions including crahses and C++
	// exceptions. this is undesirable as it means programs will continue to run after such an exception has been thrown, leaving the process
	// in a inconsistent state.
	//
	// this applies to COM methods like IDropTarget::Drop()
	//
	// this code turns off that behavior
	public static void DisableComExceptionHandling()
	{
		IGlobalOptions pGlobalOptions = new();
		pGlobalOptions.Set(GLOBALOPT_PROPERTIES.COMGLB_EXCEPTION_HANDLING, GLOBALOPT_EH_VALUES.COMGLB_EXCEPTION_DONOT_HANDLE_ANY);
		Marshal.ReleaseComObject(pGlobalOptions);
	}

	public static HRESULT GetItemAt<T>(IShellItemArray psia, uint i, out T? ppv) where T : class
	{
		HRESULT hr = 0;
		ppv = default;
		IShellItem? psi = psia?.GetItemAt(i);
		if (psi is not null)
		{
			if ((hr = psi.QueryInterface(typeof(T).GUID, out var o)).Succeeded)
				ppv = (T?)o;
		}
		return hr;
	}

	public static HRESULT GetItemFromView(IFolderView2 pfv, int iItem, in Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object? ppv)
	{
		ppv = default;

		HRESULT hr = HRESULT.S_OK;

		if (iItem == -1)
		{
			hr = pfv.GetSelectedItem(-1, out iItem); // Returns S_FALSE if none selected
		}

		if (HRESULT.S_OK == hr)
		{
			ppv = pfv.GetItem(iItem, riid);
		}
		else
		{
			hr = HRESULT.E_FAIL;
		}
		return hr;
	}

	public static void GetWindowRectInClient(HWND hwnd, out RECT prc)
	{
		GetWindowRect(hwnd, out prc);
		MapWindowPoints(GetDesktopWindow(), GetParent(hwnd), ref prc, 2);
	}

	public static HRESULT ResultFromKnownLastError() => Win32Error.GetLastError().ToHRESULT();

	// map Win32 APIs that follow the return bool/set last error protocol into HRESULT
	//
	// example: MoveFileEx()
	public static HRESULT ResultFromWin32Bool(bool b) => b ? HRESULT.S_OK : ResultFromKnownLastError();

	public static void SafeRelease<T>(ref T? ppT) where T : class
	{
		if (ppT is not null)
		{
			Marshal.ReleaseComObject(ppT);
			ppT = default;
		}
	}

	// set the icon for your window using WM_SETICON from one of the set of stock system icons caller must call ClearDialogIcon() to free the
	// HICON that is created
	public static void SetDialogIcon(HWND hdlg, SHSTOCKICONID siid)
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

	// assign an interface pointer, release old, capture ref to new, can be used to set to zero too
	public static HRESULT SetInterface<T>(ref T? ppT, object punk) where T : class
	{
		SafeRelease(ref ppT);
		ppT = punk as T;
		return punk is not null ? HRESULT.S_OK : HRESULT.E_NOINTERFACE;
	}

	public static HRESULT SetItemImageImageInStaticControl(HWND hwndStatic, IShellItem? psi)
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

	public static HRESULT ShellAttributesToString(SFGAO sfgaof, out string? ppsz)
	{ ppsz = sfgaof.ToString(); return HRESULT.S_OK; }

	public static HRESULT ShellExecuteItem(HWND hwnd, string? pszVerb, IShellItem psi)
	{
		// how to activate a shell item, use ShellExecute().
		HRESULT hr = SHGetIDListFromObject(psi, out var pidl);
		if (hr.Succeeded)
		{
			SHELLEXECUTEINFO ei = new()
			{
				cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
				fMask = ShellExecuteMaskFlags.SEE_MASK_INVOKEIDLIST,
				hwnd = hwnd,
				nShellExecuteShow = ShowWindowCommand.SW_NORMAL,
				lpIDList = (IntPtr)pidl,
				lpVerb = pszVerb
			};

			hr = ResultFromWin32Bool(ShellExecuteEx(ref ei));
		}
		return hr;
	}

	public static HRESULT SHILClone(PIDL pidl, out PIDL ppidl) => SHILCloneFull(pidl, out ppidl);

	public static HRESULT SHILCloneFull(PIDL pidl, out PIDL ppidl)
	{
		ppidl = ILClone(pidl.DangerousGetHandle());
		return !ppidl.IsNull ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
	}

	public static HRESULT SHILCombine(PIDL pidl1, PIDL pidl2, out PIDL ppidl)
	{
		ppidl = ILCombine((IntPtr)pidl1, (IntPtr)pidl2);
		return !ppidl.IsNull ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
	}
}