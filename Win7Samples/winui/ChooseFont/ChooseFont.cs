using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComDlg32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

internal static class Program
{
	private const int ID_BUTTON = 100;
	private const int ID_CHECKBOX = 200;
	private const int ID_LABEL = 300;
	private const string szWindowClass = "ChooseFontSampleWClass";
	private const string szWindowName = "ChooseFont Sample";
	private static readonly HINSTANCE g_hInst = GetModuleHandle(); // Save our hInstance for later
	private static SafeHFONT g_hfont;
	private static SafeHWND g_hwndLabel;
	private static SafeCoTaskMemStruct<LOGFONT> lf;

	private static void InitDefaultLF(out SafeCoTaskMemStruct<LOGFONT> plf)
	{
		using SafeHDC hdc = GetDC(default);
		plf = new SafeCoTaskMemStruct<LOGFONT>(new LOGFONT
		{
			lfCharSet = (CharacterSet)GetTextCharset(hdc),
			lfOutPrecision = LogFontOutputPrecision.OUT_DEFAULT_PRECIS,
			lfClipPrecision = LogFontClippingPrecision.CLIP_DEFAULT_PRECIS,
			lfQuality = LogFontOutputQuality.DEFAULT_QUALITY,
			Pitch = FontPitch.DEFAULT_PITCH,
			lfWeight = FW_NORMAL,
			lfHeight = -MulDiv(10, GetDeviceCaps(hdc, DeviceCap.LOGPIXELSY), 2)
		});
	}

	private static int Main()
	{
		MSG msg = default;
		var wc = new WNDCLASS
		{
			style = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW,
			lpfnWndProc = WndProc,
			hInstance = g_hInst,
			hIcon = LoadIcon(default, IDI_APPLICATION),
			hCursor = LoadCursor(default, IDC_ARROW),
			hbrBackground = (IntPtr)(int)(SystemColorIndex.COLOR_WINDOW + 1),
			lpszClassName = szWindowClass
		};

		RegisterClass(wc);

		SafeHWND g_hwndApp = CreateWindow(szWindowClass, szWindowName, WindowStyles.WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT,
			490, 120, default, default, g_hInst, default);
		if (!g_hwndApp.IsInvalid)
		{
			ShowWindow(g_hwndApp, ShowWindowCommand.SW_NORMAL);
			UpdateWindow(g_hwndApp);
			while (GetMessage(out msg) != 0)
			{
				TranslateMessage(msg);
				DispatchMessage(msg);
			}
		}

		return msg.wParam.ToInt32();
	}

	private static IntPtr WndProc(HWND hwnd, uint uMsg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)uMsg)
		{
			case WindowMessage.WM_CREATE:
				{
					// Create "Choose Font" button
					CreateWindow("button",
					"Choose Font",
					WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | (WindowStyles)ButtonStyle.BS_PUSHBUTTON,
					20, 20,
					100, 20,
					hwnd, (IntPtr)ID_BUTTON,
					g_hInst, default);

					// Create "Show all fonts?" checkbox
					CreateWindow("button",
					"Show all fonts?",
					WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | (WindowStyles)ButtonStyle.BS_AUTOCHECKBOX,
					20, 45,
					120, 20,
					hwnd, (IntPtr)ID_CHECKBOX,
					g_hInst, default);

					// Create the static label with our sample text
					g_hwndLabel = CreateWindow("static",
					"Some words.",
					WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | (WindowStyles)StaticStyle.SS_CENTER,
					150, 10,
					300, 40,
					hwnd, (IntPtr)ID_LABEL,
					g_hInst, default);
					InitDefaultLF(out lf);
					break;
				}
			case WindowMessage.WM_COMMAND:
				{
					if (Macros.LOWORD(wParam) == ID_BUTTON)
					{
						var cf = new CHOOSEFONT
						{
							lStructSize = (uint)Marshal.SizeOf<CHOOSEFONT>(),
							hwndOwner = hwnd,
							lpLogFont = lf
						};
						if (ButtonStateFlags.BST_CHECKED == IsDlgButtonChecked(hwnd, ID_CHECKBOX))
						{
							// show all fonts (ignore auto-activation)
							cf.Flags |= CF.CF_INACTIVEFONTS;
						}

						if (ChooseFont(ref cf))
						{
							var hfont = CreateFontIndirect(lf);
							if (!hfont.IsInvalid)
							{
								// delete the old font if being used for the control if there is one
								g_hfont?.Dispose();
								g_hfont = hfont;
								SendMessage(g_hwndLabel, (uint)WindowMessage.WM_SETFONT, g_hfont.DangerousGetHandle(), Macros.MAKELPARAM(1, 0));
							}
						}
					}
					break;
				}
			case WindowMessage.WM_DESTROY:
				{
					// cleanup font resoruces created above
					g_hfont?.Dispose();
					lf?.Dispose();
					PostQuitMessage(0);
					return default;
				}
		}
		return DefWindowProc(hwnd, uMsg, wParam, lParam);
	}
}