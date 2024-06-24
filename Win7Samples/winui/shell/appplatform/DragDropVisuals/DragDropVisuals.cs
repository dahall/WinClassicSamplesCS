using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;
using static Vanara.Extensions.ShellHelpers;
using Vanara.Collections;

namespace DragDropVisuals;

internal class Program
{
	[STAThread]
	public static void Main()
	{
		if (OleInitialize().Succeeded)
		{
			new CDragDropVisualsApp().DoModal(default);
			OleUninitialize();
		}
	}
}

internal class CDragDropVisualsApp : CDragDropHelper
{
	const int IDD_DIALOG = 100;
	const int IDC_STATIC = 101;
	const int IDC_IMAGE = 102;
	const int IDC_NAME = 103;
	const int IDC_OPEN = 104;
	const int IDC_CLEAR = 105;
	const int IDC_ATTRIBUTES = 106;
	const int IDC_CUSTOM_DATAOBJECT = 107;

	private HWND hdlg;
	private IShellItemArray? psiaDrop;

	~CDragDropVisualsApp()
	{
		if (psiaDrop is not null)
			Marshal.ReleaseComObject(psiaDrop);
	}

	public HRESULT DoModal(HWND hwnd)
	{
		DialogBoxParam(LoadLibraryEx("DragDropVisualsRes.dll", LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE), IDD_DIALOG, hwnd.IsNull ? GetDesktopWindow() : hwnd, s_DlgProc);
		return HRESULT.S_OK;
	}

	private IntPtr s_DlgProc(HWND hdlg, uint uMsg, IntPtr wParam, IntPtr lParam)
	{
		if ((WindowMessage)uMsg == WindowMessage.WM_INITDIALOG)
		{
			this.hdlg = hdlg;
		}
		return DlgProc(uMsg, wParam, lParam);
	}

	private void BeginDrag(HWND hwndDragBegin)
	{
		GetCursorPos(out var pt);
		MapWindowPoints(default, hwndDragBegin, ref pt, 1); // screen . client

		if (CheckForDragBegin(hwndDragBegin, pt.x, pt.y))
		{
			HRESULT hr = GetDataObject(hwndDragBegin, out var pdtobj);
			if (hr.Succeeded)
			{
				hr = SHDoDragDrop(hdlg, pdtobj!, null!, GetDropEffects(), out var dwEffectResult);
				Marshal.ReleaseComObject(pdtobj!);
			}
		}
	}

	private void BindUI()
	{
		HRESULT hr = GetFirstItem(out IShellItem2? psi);
		if (hr.Succeeded)
		{
			SetDlgItemText(hdlg, IDC_STATIC, "Start Drag Drop by clicking on icon");

			SetItemImageImageInStaticControl(GetDlgItem(hdlg, IDC_IMAGE), psi!);

			string? psz = psi!.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);
			SetDlgItemText(hdlg, IDC_NAME, psz);

			var sfgaof = psi.GetAttributes(SFGAO.SFGAO_CANCOPY | SFGAO.SFGAO_CANLINK | SFGAO.SFGAO_CANMOVE);
			hr = ShellAttributesToString(sfgaof, out psz);
			if (hr.Succeeded)
			{
				SetDlgItemText(hdlg, IDC_ATTRIBUTES, psz!);
			}
		}
		else
		{
			SetItemImageImageInStaticControl(GetDlgItem(hdlg, IDC_IMAGE), default);
			SetDlgItemText(hdlg, IDC_STATIC, "Drop An Item Here");
			SetDlgItemText(hdlg, IDC_NAME, "");
			SetDlgItemText(hdlg, IDC_ATTRIBUTES, "");
		}
		EnableWindow(GetDlgItem(hdlg, IDC_CUSTOM_DATAOBJECT), hr.Succeeded);
		EnableWindow(GetDlgItem(hdlg, IDC_OPEN), hr.Succeeded);
		EnableWindow(GetDlgItem(hdlg, IDC_CLEAR), hr.Succeeded);
	}

	// x, y in client coordinates
	private bool CheckForDragBegin(HWND hwnd, int x, int y)
	{
		int cxDrag = GetSystemMetrics(SystemMetric.SM_CXDRAG);
		int cyDrag = GetSystemMetrics(SystemMetric.SM_CYDRAG);

		// See if the user moves a certain number of pixels in any direction
		RECT rcDragRadius = new(x - cxDrag, y - cyDrag, x + cxDrag, y + cyDrag);

		MapWindowRect(hwnd, default, ref rcDragRadius); // client . screen

		SetCapture(hwnd);

		do
		{
			// Sleep the thread waiting for mouse input. Prevents pegging the CPU in a PeekMessage loop.
			MSG msg;
			switch ((WAIT_STATUS)MsgWaitForMultipleObjectsEx(0, default, INFINITE, QS.QS_MOUSE, MWMO.MWMO_INPUTAVAILABLE))
			{
				case WAIT_STATUS.WAIT_OBJECT_0:
					if (PeekMessage(out msg, default, WindowMessage.WM_MOUSEFIRST, WindowMessage.WM_MOUSELAST, PM.PM_REMOVE))
					{
						// See if the application wants to process the message...
						if (!CallMsgFilter(ref msg, MSGF_COMMCTRL_BEGINDRAG))
						{
							switch ((WindowMessage)msg.message)
							{
								case WindowMessage.WM_LBUTTONUP:
								case WindowMessage.WM_RBUTTONUP:
								case WindowMessage.WM_LBUTTONDOWN:
								case WindowMessage.WM_RBUTTONDOWN:
									// Released the mouse without moving outside the drag radius, not beginning a drag.
									ReleaseCapture();
									return false;

								case WindowMessage.WM_MOUSEMOVE:
									if (!PtInRect(rcDragRadius, msg.pt))
									{
										// Moved outside the drag radius, beginning a drag.
										ReleaseCapture();
										return true;
									}
									break;

								default:
									TranslateMessage(msg);
									DispatchMessage(msg);
									break;
							}
						}
					}
					break;
				default:
					break;
			}

			// WM_CANCELMODE messages will unset the capture, in that case I want to exit this loop
		}
		while (GetCapture() == hwnd);

		return false;
	}

	// The IDataObject passed in by OLE through the CDragDropHelper::Drop function is only valid until the Drop function returns This means
	// the the IShellItemArray we are receiving may go bad as well since it is based on the incoming IDataObject Here we will create a stream
	// and marshal the IShellItemArray which will create a copied IShellItemArray which does not depend on the IDataObject
	private HRESULT CopyShellItemArray(IShellItemArray psia, out IShellItemArray? ppsiaOut)
	{
		var e = IEnumFromCom<IShellItem>.Create(psia.EnumItems());
		return SHCreateShellItemArrayFromIDLists(e.Select(i => SHGetIDListFromObject(i, out var p).Succeeded ? p : throw new InvalidCastException()), out ppsiaOut);
	}

	private IntPtr DlgProc(uint uMsg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)uMsg)
		{
			case WindowMessage.WM_INITDIALOG:
				OnInitDlg();
				break;

			case WindowMessage.WM_COMMAND:
				int idCmd = Macros.LOWORD(wParam);
				switch (idCmd)
				{
					case (int)MB_RESULT.IDOK:
					case (int)MB_RESULT.IDCANCEL:
						return EndDialog(hdlg, (IntPtr)idCmd) ? (IntPtr)1 : IntPtr.Zero;

					case IDC_OPEN:
						OnOpen();
						break;

					case IDC_CLEAR:
						SafeRelease(ref psiaDrop);
						BindUI();
						break;

					case IDC_IMAGE:
						switch ((StaticNotification)Macros.HIWORD(wParam))
						{
							case StaticNotification.STN_CLICKED:
								BeginDrag(GetDlgItem(hdlg, idCmd));
								break;

							case StaticNotification.STN_DBLCLK:
								OnOpen();
								break;
						}
						break;
				}
				break;

			case WindowMessage.WM_DESTROY:
				OnDestroyDlg();
				break;

			default:
				return IntPtr.Zero;
		}
		return (IntPtr)1;
	}

	// USE_ITEMS_DATAOBJECT demonstrates what happens when using the data object from the shell items. that data object supports all of the
	// features needed to get drag images, drop tips, etc.
	//
	// for appliations that create their own data object you need to have support for SetData(), QueryGetData() and GetData() of custom
	// formats. this is demonstrated in DataObject.cpp
	private HRESULT GetDataObject(HWND hwndDragBegin, out IDataObject? ppdtobj)
	{
		HRESULT hr;
		if (UseCustomDataObject())
		{
			hr = CDataObject.CreateInstance(out ppdtobj);
			if (hr.Succeeded)
			{
				if (GetDragDropHelper(out IDragSourceHelper2? pdsh).Succeeded)
				{
					// enable drop tips
					pdsh!.SetFlags(DSH_FLAGS.DSH_ALLOWDROPDESCRIPTIONTEXT);

					// we need to make a copy of the HBITMAP held by the static control as InitializeFromBitmap() takes owership of this
					HBITMAP hbmp = (HBITMAP)(IntPtr)CopyImage((HANDLE)SendMessage(hwndDragBegin, StaticMessage.STM_GETIMAGE, LoadImageType.IMAGE_BITMAP, IntPtr.Zero), LoadImageType.IMAGE_BITMAP, 0, 0, 0);

					// alternate, load the bitmap from a resource HBITMAP hbmp = (HBITMAP)LoadImage(default, MAKEINTRESOURCE(OBM_CLOSE),
					// IMAGE_BITMAP, 128, 128, 0);

					InitializeDragImageFromWindow(hwndDragBegin, hbmp, out var di);

					// note that InitializeFromBitmap() takes ownership of the hbmp so we should not free it by calling DeleteObject()
					pdsh.InitializeFromBitmap(di, ppdtobj!);

					Marshal.ReleaseComObject(pdsh);
				}
			}
		}
		else
		{
			ppdtobj = default;
			hr = GetSelectedItems(out var psiaItems);
			if (hr.Succeeded)
			{
				ppdtobj = psiaItems!.BindToHandler<IDataObject>(default, BHID.BHID_DataObject);
			}
		}
		return hr;
	}

	private DROPEFFECT GetDropEffects()
	{
		DROPEFFECT dwEffect;
		if (UseCustomDataObject())
		{
			dwEffect = DROPEFFECT.DROPEFFECT_MOVE | DROPEFFECT.DROPEFFECT_COPY | DROPEFFECT.DROPEFFECT_LINK;
		}
		else
		{
			dwEffect = DROPEFFECT.DROPEFFECT_NONE;
			if (GetSelectedItems(out var psiaItems).Succeeded)
			{
				dwEffect = (DROPEFFECT)psiaItems!.GetAttributes(SIATTRIBFLAGS.SIATTRIBFLAGS_AND, (SFGAO)(DROPEFFECT.DROPEFFECT_COPY | DROPEFFECT.DROPEFFECT_MOVE | DROPEFFECT.DROPEFFECT_LINK));
			}
		}
		return dwEffect;
	}

	private HRESULT GetFirstItem<T>(out T? ppv) where T : class
	{
		ppv = default;
		HRESULT hr = GetSelectedItems(out var psia);
		if (hr.Succeeded)
		{
			hr = GetItemAt(psia!, 0, out ppv);
		}
		return hr;
	}

	private HRESULT GetSelectedItems(out IShellItemArray? ppsia)
	{
		ppsia = psiaDrop;
		return psiaDrop is not null ? HRESULT.S_OK : HRESULT.E_NOINTERFACE;
	}

	private void InitializeDragImageFromWindow(HWND hwndDragBegin, HBITMAP hbmp, out SHDRAGIMAGE pdi)
	{
		pdi = new()
		{
			crColorKey = CLR_NONE, // assume alpha image, no need for color key
			hbmpDragImage = hbmp
		};

		BITMAP bm = GetObject<BITMAP>(hbmp);
		pdi.sizeDragImage.cx = bm.bmWidth;
		pdi.sizeDragImage.cy = bm.bmHeight;

		if (GetWindowRect(hwndDragBegin, out var rc) && GetCursorPos(out var ptDrag))
		{
			pdi.ptOffset.x = ptDrag.x - rc.left;
			pdi.ptOffset.y = ptDrag.y - rc.top;
		}
	}

	private void OnDestroyDlg()
	{
		TerminateDragDropHelper();
		// cleanup the allocated HBITMAP
		SetItemImageImageInStaticControl(GetDlgItem(hdlg, IDC_IMAGE), default);
	}

	protected override HRESULT OnDrop(IShellItemArray psia, MouseButtonState mbs)
	{
		HRESULT hr = CopyShellItemArray(psia, out psiaDrop);
		if (hr.Succeeded)
		{
			BindUI();
		}
		return hr;
	}

	private void OnInitDlg()
	{
		InitializeDragDropHelper(hdlg, DROPIMAGETYPE.DROPIMAGE_COPY, "Drop Visuals Sample App");
		BindUI();
	}

	private void OnOpen()
	{
		// If multiple items are dropped into our sample, the "Open" button will only open the first item from the array
		HRESULT hr = GetFirstItem(out IShellItem? psi);
		if (hr.Succeeded)
		{
			hr = ShellExecuteItem(hdlg, default, psi!);
		}
	}

	private bool UseCustomDataObject() => IsDlgButtonChecked(hdlg, IDC_CUSTOM_DATAOBJECT) == ButtonStateFlags.BST_UNCHECKED;
}