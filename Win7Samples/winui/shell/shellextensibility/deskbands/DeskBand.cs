using Microsoft.Win32;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.UxTheme;

namespace deskbands
{
	[ComVisible(true)]
	public class DeskBand : IDeskBand2, IPersistStream, IObjectWithSite, IInputObject
	{
		public const string g_szDeskBandSampleClass = "DeskBandSampleClass";
		public static readonly Guid CLSID_DeskBandSample = new(0x46b3d3ef, 0x71a, 0x4b7e, 0x8a, 0xa2, 0xe5, 0x60, 0x81, 0xd, 0xab, 0x35);
		public static readonly Color bkClr = Color.Yellow;
		public static HINSTANCE g_hInst;
		private bool m_fCompositionEnabled;
		private bool m_fHasFocus;

		// whether deskband window currently has focus
		private bool m_fIsDirty;

		// whether deskband setting has changed whether glass is currently enabled in deskband ID of deskband
		private SafeHWND m_hwnd;

		// main window of deskband
		private HWND m_hwndParent;

		private IInputObjectSite m_pInputObjectSite;
		private object m_pSite; // IObjectWithSite site
								// parent site that contains deskband parent window of deskband

		private DeskBand()
		{
		}

		// IDeskBand2
		public HRESULT CanRenderComposited(out bool pfCanRenderComposited)
		{
			pfCanRenderComposited = true;
			return HRESULT.S_OK;
		}

		public HRESULT CloseDW(uint _)
		{
			if (!m_hwnd.IsNull)
			{
				ShowWindow(m_hwnd, ShowWindowCommand.SW_HIDE);
				DestroyWindow(m_hwnd);
				m_hwnd = default;
			}

			return HRESULT.S_OK;
		}

		public HRESULT ContextSensitiveHelp(bool _) => HRESULT.E_NOTIMPL;

		public void Dispose()
		{
			if (m_pSite is not null)
			{
				Marshal.ReleaseComObject(m_pSite);
			}
			if (m_pInputObjectSite is not null)
			{
				Marshal.ReleaseComObject(m_pInputObjectSite);
			}

			System.GC.SuppressFinalize(this);
		}

		// IDeskBand
		public HRESULT GetBandInfo(uint dwBandID, DBIF dwViewMode, ref DESKBANDINFO pdbi)
		{
			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_MINSIZE))
			{
				pdbi.ptMinSize.Width = 200;
				pdbi.ptMinSize.Height = 30;
			}

			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_MAXSIZE))
			{
				pdbi.ptMaxSize.Height = -1;
			}

			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_INTEGRAL))
			{
				pdbi.ptIntegral.Height = 1;
			}

			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_ACTUAL))
			{
				pdbi.ptActual.Width = 200;
				pdbi.ptActual.Height = 30;
			}

			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_TITLE))
			{
				// Don't show title by removing this flag.
				pdbi.dwMask &= ~DBIM.DBIM_TITLE;
			}

			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_MODEFLAGS))
			{
				pdbi.dwModeFlags = DBIMF.DBIMF_NORMAL | DBIMF.DBIMF_VARIABLEHEIGHT;
			}

			if (pdbi.dwMask.IsFlagSet(DBIM.DBIM_BKCOLOR))
			{
				// Use the default background color by removing this flag.
				pdbi.dwMask &= ~DBIM.DBIM_BKCOLOR;
			}

			return HRESULT.S_OK;
		}

		// IPersist
		public Guid GetClassID() => CLSID_DeskBandSample;

		public HRESULT GetCompositionState(out bool pfCompositionEnabled)
		{
			pfCompositionEnabled = m_fCompositionEnabled;
			return HRESULT.S_OK;
		}

		public HRESULT GetSite(in Guid riid, out object ppv)
		{
			HRESULT hr = HRESULT.E_FAIL;

			if (m_pSite is not null)
			{
				hr = ShellUtil.QueryInterface(m_pSite, riid, out ppv);
			}
			else
			{
				ppv = default;
			}

			return hr;
		}

		public ulong GetSizeMax() => throw new NotImplementedException();

		// IOleWindow
		public HRESULT GetWindow(out HWND phwnd)
		{
			phwnd = m_hwnd;
			return HRESULT.S_OK;
		}

		public HRESULT HasFocusIO() => m_fHasFocus ? HRESULT.S_OK : HRESULT.S_FALSE;

		// IPersistStream
		public HRESULT IsDirty() => m_fIsDirty ? HRESULT.S_OK : HRESULT.S_FALSE;

		public void Load(IStream _)
		{
		}

		public void OnFocus(bool fFocus)
		{
			m_fHasFocus = fFocus;

			if (m_pInputObjectSite is not null)
			{
				m_pInputObjectSite.OnFocusChangeIS(this, m_fHasFocus);
			}
		}

		public void OnPaint(HDC hdcIn)
		{
			HDC hdc = hdcIn;
			PAINTSTRUCT ps = default;
			const string szContent = "DeskBand Sample";
			const string szContentGlass = "DeskBand Sample (Glass)";

			if (hdc.IsNull)
			{
				hdc = BeginPaint(m_hwnd, out ps);
			}

			if (!hdc.IsNull)
			{
				GetClientRect(m_hwnd, out RECT rc);

				SIZE size;

				if (m_fCompositionEnabled)
				{
					using SafeHTHEME hTheme = OpenThemeData(default, "BUTTON");
					if (!hTheme.IsInvalid)
					{
						using SafeHPAINTBUFFER hBufferedPaint = BeginBufferedPaint(hdc, rc, BP_BUFFERFORMAT.BPBF_TOPDOWNDIB, default, out HDC hdcPaint);

						DrawThemeParentBackground(m_hwnd, hdcPaint, rc);

						GetTextExtentPoint32(hdc, szContentGlass, szContentGlass.Length, out size);
						RECT rcText = new((rc.Width - size.cx) / 2, (rc.Height - size.cy) / 2, 0, 0);
						rcText.right = rcText.left + size.cx;
						rcText.bottom = rcText.top + size.cy;

						DTTOPTS dttOpts = new(null) { TextColor = System.Drawing.Color.Yellow, GlowSize = 10 };
						DrawThemeTextEx(hTheme, hdcPaint, 0, 0, szContentGlass, -1, 0, ref rcText, dttOpts);
					}
				}
				else
				{
					SetBkColor(hdc, bkClr);
					GetTextExtentPoint32(hdc, szContent, szContent.Length, out size);
					TextOut(hdc, (rc.Width - size.cx) / 2, (rc.Width - size.cy) / 2, szContent, szContent.Length);
				}
			}

			if (hdcIn.IsNull)
			{
				EndPaint(m_hwnd, ps);
			}
		}

		public HRESULT ResizeBorderDW(PRECT prcBorder, object punkToolbarSite, bool fReserved) => HRESULT.E_NOTIMPL;

		public void Save(IStream _, bool fClearDirty)
		{
			if (fClearDirty)
			{
				m_fIsDirty = false;
			}
		}

		public HRESULT SetCompositionState(bool fCompositionEnabled)
		{
			m_fCompositionEnabled = fCompositionEnabled;

			InvalidateRect(m_hwnd, default, true);
			UpdateWindow(m_hwnd);

			return HRESULT.S_OK;
		}

		// IObjectWithSite
		public HRESULT SetSite(object pUnkSite)
		{
			HRESULT hr = HRESULT.S_OK;

			m_hwndParent = default;
			if (m_pSite is not null)
			{
				Marshal.ReleaseComObject(m_pSite);
				m_pSite = default;
			}
			if (m_pInputObjectSite is not null)
			{
				Marshal.ReleaseComObject(m_pInputObjectSite);
				m_pInputObjectSite = default;
			}

			if (pUnkSite is not null)
			{
				m_pSite = pUnkSite;

				try
				{
					var pow = (IOleWindow)pUnkSite;

					hr = pow.GetWindow(out m_hwndParent);
					if (hr.Succeeded)
					{
						WindowClass wc = new(g_szDeskBandSampleClass, g_hInst, WndProc, WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW,
							default, default, LoadCursor(default, IDC_ARROW), CreateSolidBrush(bkClr));

						m_hwnd = CreateWindowEx(0, wc.ClassName, default, WindowStyles.WS_CHILD | WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CLIPSIBLINGS,
							hWndParent: m_hwndParent, hInstance: g_hInst);

						if (m_hwnd.IsInvalid)
						{
							hr = HRESULT.E_FAIL;
						}
					}

					pow = null;
					m_pInputObjectSite = (IInputObjectSite)pUnkSite;
				}
				catch (Exception ex)
				{
					hr = ex.HResult;
				}
			}

			return hr;
		}

		// IDockingWindow
		public HRESULT ShowDW(bool fShow)
		{
			if (!m_hwnd.IsNull)
			{
				ShowWindow(m_hwnd, fShow ? ShowWindowCommand.SW_SHOW : ShowWindowCommand.SW_HIDE);
			}

			return HRESULT.S_OK;
		}

		public HRESULT TranslateAcceleratorIO(in MSG _) => HRESULT.S_FALSE;

		// IInputObject
		public HRESULT UIActivateIO(bool fActivate, in MSG _)
		{
			if (fActivate)
			{
				SetFocus(m_hwnd);
			}

			return HRESULT.S_OK;
		}

		public IntPtr WndProc(HWND hwnd, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			IntPtr lResult = default;

			switch ((WindowMessage)uMsg)
			{
				case WindowMessage.WM_CREATE:
					//m_hwnd = hwnd;
					break;

				case WindowMessage.WM_SETFOCUS:
					OnFocus(true);
					break;

				case WindowMessage.WM_KILLFOCUS:
					OnFocus(false);
					break;

				case WindowMessage.WM_PAINT:
					OnPaint(default);
					break;

				case WindowMessage.WM_PRINTCLIENT:
					OnPaint(wParam);
					break;

				case WindowMessage.WM_ERASEBKGND:
					if (m_fCompositionEnabled)
					{
						lResult = (IntPtr)1;
					}
					break;
			}

			if (uMsg != (uint)WindowMessage.WM_ERASEBKGND)
			{
				lResult = DefWindowProc(hwnd, uMsg, wParam, lParam);
			}

			return lResult;
		}

		[ComRegisterFunction]
		public static void Register(Type t)
		{
			using (RegistryKey rkClass = Registry.ClassesRoot.CreateSubKey($@"CLSID\{t.GUID:B}"))
				rkClass.SetValue(null, t.Name);

			var pcr = new ICatRegister();
			pcr.RegisterClassImplCategories(DeskBand.CLSID_DeskBandSample, 1, new[] { CATID_DeskBand });
		}

		[ComUnregisterFunction]
		public static void Unregister(Type t)
		{
			var pcr = new ICatRegister();
			pcr.UnRegisterClassImplCategories(DeskBand.CLSID_DeskBandSample, 1, new[] { CATID_DeskBand });

			Registry.ClassesRoot.DeleteSubKeyTree($@"CLSID\{t.GUID:B}");
		}
	}
}