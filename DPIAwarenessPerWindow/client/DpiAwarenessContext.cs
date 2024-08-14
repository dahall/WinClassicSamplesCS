using Vanara;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComDlg32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Macros;
using static Vanara.PInvoke.User32;

namespace DpiClient;

internal static partial class Program
{
	private const int DEFAULT_BUTTON_HEIGHT96 = 25;
	private const int DEFAULT_BUTTON_WIDTH96 = 100;
	private const int DEFAULT_CHAR_BUFFER = 150;
	private const int DEFAULT_PADDING96 = 20;
	private const int EXTERNAL_CONTENT_HEIGHT96 = 400;
	private const int EXTERNAL_CONTENT_WIDTH96 = 400;
	private const string HWND_NAME_CHECKBOX = "CHECKBOX";
	private const string HWND_NAME_DIALOG = "Open a System Dialog";
	private const string HWND_NAME_RADIO = "RADIO";
	private const string HWND_NAME_STATIC = "Static";
	private const string PROP_DPIISOLATION = "PROP_ISOLATION";
	private const int SAMPLE_STATIC_HEIGHT96 = 50;
	private const int WINDOW_HEIGHT96 = 700;
	private const int WINDOW_WIDTH96 = 500;
	private const string WINDOWCLASSNAME = "SetThreadDpiAwarenessContextSample";

	// Globals
	private static HINSTANCE g_hInst;

	public static int Main()
	{
		g_hInst = LoadLibraryEx("DpiAwarenessContextRes.dll", LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE).ReleaseOwnership();

		WindowClass wcex = new(WINDOWCLASSNAME, g_hInst, WndProc, WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW);

		// Create the host window
		using var hHostDlg = CreateDialogParam(g_hInst, IDD_DIALOG1, default, HostDialogProc, default);
		if (!hHostDlg)
		{
			return Marshal.GetLastWin32Error();
		}

		ShowWindow(hHostDlg, ShowWindowCommand.SW_NORMAL);

		new MessagePump().Run();

		return 0;
	}

	// Create the sample window and set its initial size, based off of the DPI awareness mode that it's running under
	private static void CreateSampleWindow(HWND hWndDlg, DPI_AWARENESS_CONTEXT context, bool bEnableNonClientDpiScaling, bool bChildWindowDpiIsolation)
	{
		// Store the current thread's DPI-awareness context
		DPI_AWARENESS_CONTEXT previousDpiContext = SetThreadDpiAwarenessContext(context);

		// Create the window. Initially create it using unscaled (96 DPI) sizes. We'll resize the window after it's created

		CreateParams createParams = new(bEnableNonClientDpiScaling, bChildWindowDpiIsolation);

		// Windows 10 (1803) supports child-HWND DPI-mode isolation. This enables child HWNDs to run in DPI-scaling modes that are isolated
		// from that of their parent (or host) HWND. Without child-HWND DPI isolation, all HWNDs in an HWND tree must have the same
		// DPI-scaling mode.
		DPI_HOSTING_BEHAVIOR previousDpiHostingBehavior = default;
		if (bChildWindowDpiIsolation)
		{
			previousDpiHostingBehavior = SetThreadDpiHostingBehavior(DPI_HOSTING_BEHAVIOR.DPI_HOSTING_BEHAVIOR_MIXED);
		}

		HWND hWnd = CreateWindowEx(0L, WINDOWCLASSNAME, "", WindowStyles.WS_OVERLAPPEDWINDOW | WindowStyles.WS_HSCROLL | WindowStyles.WS_VSCROLL,
			CW_USEDEFAULT, 0, WINDOW_WIDTH96, WINDOW_HEIGHT96, hWndDlg, LoadMenu(g_hInst, IDC_MAINMENU), g_hInst, GCHandle.ToIntPtr(GCHandle.Alloc(createParams))).ReleaseOwnership();

		ShowWindow(hWnd, ShowWindowCommand.SW_SHOWNORMAL);

		// Restore the current thread's DPI awareness context
		SetThreadDpiAwarenessContext(previousDpiContext);

		// Restore the current thread DPI hosting behavior, if we changed it.
		if (bChildWindowDpiIsolation)
		{
			SetThreadDpiHostingBehavior(previousDpiHostingBehavior);
		}
	}

	// Find the child static control, get the font for the control, then delete it
	private static void DeleteWindowFont(HWND hWnd)
	{
		HWND hWndStatic = GetWindow(hWnd, GetWindowCmd.GW_CHILD);
		if (hWndStatic == default)
		{
			return;
		}

		// Get a handle to the font
		HFONT hFont = GetWindowFont(hWndStatic);
		if (hFont == default)
		{
			return;
		}

		SetWindowFont(hWndStatic, default, false);
		DeleteObject(hFont);
	}

	// Perform initial Window setup and DPI scaling when the window is created
	private static IntPtr DoInitialWindowSetup(HWND hWnd)
	{
		// Resize the window to account for DPI. The window might have been created on a monitor that has > 96 DPI. Windows does not send a
		// window a DPI change when it is created, even if it is created on a monitor with a DPI > 96
		int uDpi = 96;

		// Determine the DPI to use, according to the DPI awareness mode
		DPI_AWARENESS dpiAwareness = GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext());
		switch (dpiAwareness)
		{
			// Scale the window to the system DPI
			case DPI_AWARENESS.DPI_AWARENESS_SYSTEM_AWARE:
				uDpi = (int)GetDpiForSystem();
				break;

			// Scale the window to the monitor DPI
			case DPI_AWARENESS.DPI_AWARENESS_PER_MONITOR_AWARE:
				uDpi = (int)GetDpiForWindow(hWnd);
				break;
		}

		GetWindowRect(hWnd, out var rcWindow);
		rcWindow.right = rcWindow.left + MulDiv(WINDOW_WIDTH96, uDpi, 96);
		rcWindow.bottom = rcWindow.top + MulDiv(WINDOW_HEIGHT96, uDpi, 96);
		SetWindowPos(hWnd, default, rcWindow.right, rcWindow.top, rcWindow.right - rcWindow.left, rcWindow.bottom - rcWindow.top,
			SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

		// Create a static control for use displaying DPI-related information. Initially the static control will not be sized, but we will
		// next DPI scale it with a helper function.
		HWND hWndStatic = CreateWindowEx(WindowStylesEx.WS_EX_LEFT, "STATIC", HWND_NAME_STATIC, (WindowStyles)StaticStyle.SS_LEFT | WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE,
			0, 0, 0, 0, hWnd, default, g_hInst, default).ReleaseOwnership();
		if (hWndStatic == default)
		{
			return new(-1);
		}

		// Create some buttons
		_ = CreateWindow("BUTTON", HWND_NAME_CHECKBOX, WindowStyles.WS_TABSTOP | WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | (WindowStyles)(ButtonStyle.BS_DEFPUSHBUTTON | ButtonStyle.BS_CHECKBOX), 0, 0, 0, 0, hWnd, default, g_hInst, default).ReleaseOwnership();
		_ = CreateWindow("BUTTON", HWND_NAME_RADIO, (WindowStyles)(ButtonStyle.BS_PUSHBUTTON | ButtonStyle.BS_TEXT | ButtonStyle.BS_DEFPUSHBUTTON | ButtonStyle.BS_USERBUTTON | ButtonStyle.BS_AUTORADIOBUTTON) | WindowStyles.WS_CHILD | WindowStyles.WS_OVERLAPPED | WindowStyles.WS_VISIBLE, 0, 0, 0, 0, hWnd, default, g_hInst, default).ReleaseOwnership();
		_ = CreateWindow("BUTTON", HWND_NAME_DIALOG, WindowStyles.WS_TABSTOP | WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | (WindowStyles)ButtonStyle.BS_DEFPUSHBUTTON, 0, 0, 0, 0, hWnd, (IntPtr)IDM_SHOWDIALOG, g_hInst, default).ReleaseOwnership();

		// Load an HWND from an external source (a DLL in this example)
		//
		// HWNDs from external sources might not support Per-Monitor V2 awareness. Hosting HWNDs that don't support the same DPI awareness
		// mode as their host can lead to rendering problems. When child-HWND DPI isolation is enabled, Windows will try to let that HWND run
		// in its native DPI scaling mode (which might or might not have been defined explicitly).

		// First, determine if we are in the correct mode to use this feature
		BOOL bDpiIsolation = GetProp(hWnd, PROP_DPIISOLATION);

		DPI_AWARENESS_CONTEXT previousDpiContext = default;
		DPI_HOSTING_BEHAVIOR previousDpiHostingBehavior = default;

		if (bDpiIsolation)
		{
			previousDpiHostingBehavior = SetThreadDpiHostingBehavior(DPI_HOSTING_BEHAVIOR.DPI_HOSTING_BEHAVIOR_MIXED);

			// For this example, we'll have the external content run with System-DPI awareness
			previousDpiContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE);
		}

		HWND hWndExternal = PlugInDll.PlugInDll.CreateContentHwnd(g_hInst, EXTERNAL_CONTENT_WIDTH96, EXTERNAL_CONTENT_HEIGHT96);

		// Return the thread context and hosting behavior to its previous value, if using DPI-isolation
		if (bDpiIsolation)
		{
			SetThreadDpiAwarenessContext(previousDpiContext);
			SetThreadDpiHostingBehavior(previousDpiHostingBehavior);
		}

		// After the external content HWND was create with a system-DPI awareness context, reparent it
		_ = SetParent(hWndExternal, hWnd);

		// DPI scale child-windows
		UpdateAndDpiScaleChildWindows(hWnd, uDpi);

		return default;
	}

	private static bool GetParentRelativeWindowRect(HWND hWnd, out RECT childBounds) => PlugInDll.PlugInDll.GetParentRelativeWindowRect(hWnd, out childBounds);

	private static HBRUSH GetStockBrush(StockObjectType i) => (HBRUSH)GetStockObject(i);

	// DPI Change handler. on WM_DPICHANGE resize the window and then call a function to redo layout for the child controls
	private static uint HandleDpiChange(HWND hWnd, IntPtr wParam, IntPtr lParam)
	{
		HWND hWndStatic = FindWindowEx(hWnd, default, "STATIC", default);

		if (!hWndStatic.IsNull)
		{
			int uDpi = HIWORD(wParam);

			// Resize the window
			var lprcNewScale = lParam.ToStructure<RECT>();

			SetWindowPos(hWnd, default, lprcNewScale.left, lprcNewScale.top,
				lprcNewScale.right - lprcNewScale.left, lprcNewScale.bottom - lprcNewScale.top,
				SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

			// Redo layout of the child controls
			UpdateAndDpiScaleChildWindows(hWnd, uDpi);
		}

		return 0;
	}

	// The dialog procedure for the sample host window
	private static IntPtr HostDialogProc(HWND hWndDlg, uint message, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)message)
		{
			case WindowMessage.WM_CTLCOLORDLG:
			case WindowMessage.WM_CTLCOLORSTATIC:
				return (IntPtr)GetStockBrush(StockObjectType.WHITE_BRUSH);

			case WindowMessage.WM_INITDIALOG:
				string appDescription =
					"This sample app lets you create windows with different DPI Awareness modes so " +
					"that you can observe how Win32 windows behave under these modes. " +
					"Each window will show different behaviors depending on the mode (will be blurry or " +
					"crisp, non-client area will scale differently, etc.)." +
					"\r\n\r\n" +
					"The best way to observe these differences is to move each window to a display with a " +
					"different display scaling (DPI) value. On single-display devices you can simulate " +
					"this by changing the display scaling value of your display (the \"Change the size " +
					"of text, apps, and other items\" setting in the Display settings page of the Settings " +
					"app, as of Windows 10, 1703). Make these settings changes while the app is still " +
					"running to observe the different DPI-scaling behavior.";
				SetDlgItemText(hWndDlg, IDC_EDIT1, appDescription);
				return default;

			case WindowMessage.WM_COMMAND:
				DPI_AWARENESS_CONTEXT context = default;
				bool bNonClientScaling = false;
				bool bChildWindowDpiIsolation = false;
				switch (LOWORD(wParam))
				{
					case IDC_BUTTON_UNAWARE:
						context = DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_UNAWARE;
						break;

					case IDC_BUTTON_SYSTEM:
						context = DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_SYSTEM_AWARE;
						break;

					case IDC_BUTTON_81:
						context = DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE;
						break;

					case IDC_BUTTON_1607:
						context = DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE;
						bNonClientScaling = true;
						break;

					case IDC_BUTTON_1703:
						context = DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2;
						break;

					case IDC_BUTTON_1803:
						context = DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2;
						bChildWindowDpiIsolation = true;
						break;

					case IDM_EXIT:
						DestroyWindow(hWndDlg);
						return default;
				}

				if (context != default)
				{
					CreateSampleWindow(hWndDlg, context, bNonClientScaling, bChildWindowDpiIsolation);
				}
				return new(1);

			case WindowMessage.WM_CLOSE:
				DestroyWindow(hWndDlg);
				return default;

			case WindowMessage.WM_DESTROY:
				DeleteWindowFont(hWndDlg);
				PostQuitMessage(0);
				return default;
		}
		return default;
	}
	private static void ShowFileOpenDialog(HWND hWnd)
	{
		SafeLPTSTR szFile = new(MAX_PATH);      // buffer for file name
		OPENFILENAME ofn = new()
		{
			lStructSize = (uint)Marshal.SizeOf(typeof(OPENFILENAME)),
			hwndOwner = hWnd,
			lpstrFile = szFile,
			nMaxFile = (uint)szFile.Capacity,
			lpstrFilter = "All\ref 0 .*\0Text\ref 0 .TXT\0",
			nFilterIndex = 1,
			Flags = OFN.OFN_PATHMUSTEXIST | OFN.OFN_FILEMUSTEXIST
		};

		// Display the Open dialog box.
		GetOpenFileName(ref ofn);
	}

	// Resize and reposition child controls for DPI
	private static void UpdateAndDpiScaleChildWindows(HWND hWnd, int uDpi)
	{
		HWND hWndRadio;
		HWND hWndDialog;

		// Resize the static control
		int uPadding = MulDiv(DEFAULT_PADDING96, uDpi, 96);
		GetClientRect(hWnd, out var rcClient);

		// Size and position the static control
		HWND hWndStatic = FindWindowEx(hWnd, default, "STATIC", default);
		if (hWndStatic == default)
		{
			return;
		}
		int uWidth = rcClient.right - rcClient.left - 2 * uPadding;
		int uHeight = MulDiv(SAMPLE_STATIC_HEIGHT96, uDpi, 96);
		SetWindowPos(hWndStatic, default, uPadding, uPadding, uWidth, uHeight, SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

		UpdateDpiString(hWndStatic, uDpi);

		// Size and position the checkbox
		HWND hWndCheckbox = FindWindowEx(hWnd, default, "BUTTON", HWND_NAME_CHECKBOX);
		if (hWndCheckbox == default)
		{
			return;
		}
		GetParentRelativeWindowRect(hWndStatic, out rcClient);
		SetWindowPos(hWndCheckbox, default, uPadding, rcClient.bottom + uPadding, MulDiv(DEFAULT_BUTTON_WIDTH96, uDpi, 96),
			MulDiv(DEFAULT_BUTTON_HEIGHT96, uDpi, 96), SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

		// Size and position the radio button
		hWndRadio = FindWindowEx(hWnd, default, "BUTTON", HWND_NAME_RADIO);
		if (hWndCheckbox == default)
		{
			return;
		}
		GetParentRelativeWindowRect(hWndCheckbox, out rcClient);
		SetWindowPos(hWndRadio, default, rcClient.right + uPadding, rcClient.top, MulDiv(DEFAULT_BUTTON_WIDTH96, uDpi, 96),
			MulDiv(DEFAULT_BUTTON_HEIGHT96, uDpi, 96), SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

		// Size and position the dialog button
		hWndDialog = FindWindowEx(hWnd, default, "BUTTON", HWND_NAME_DIALOG);
		GetParentRelativeWindowRect(hWndCheckbox, out rcClient);
		SetWindowPos(hWndDialog, default, uPadding, rcClient.bottom + uPadding,
			MulDiv(DEFAULT_BUTTON_WIDTH96 * 2, uDpi, 96), // Make this one twice as wide as the others
			MulDiv(DEFAULT_BUTTON_HEIGHT96, uDpi, 96),
			SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

		// Size and position the external content HWND
		HWND hWndExternal = FindWindowEx(hWnd, default, PlugInDll.PlugInDll.PLUGINWINDOWCLASSNAME, PlugInDll.PlugInDll.HWND_NAME_EXTERNAL);
		GetParentRelativeWindowRect(hWndDialog, out rcClient);
		SetWindowPos(hWndExternal, hWndDialog, uPadding, rcClient.bottom + uPadding,
			MulDiv(EXTERNAL_CONTENT_WIDTH96, uDpi, 96),
			MulDiv(EXTERNAL_CONTENT_HEIGHT96, uDpi, 96),
			SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);

		// Send a new font to all child controls (the 'plugin' content is subclassed to ignore WM_SETFONT)
		var hFontOld = GetWindowFont(hWndStatic);
		using SafeHGlobalStruct<LOGFONT> lfText = new();
		SystemParametersInfoForDpi(SPI.SPI_GETICONTITLELOGFONT, lfText.Size, lfText, 0, (uint)uDpi);
		SafeHFONT hFontNew = CreateFontIndirect(lfText);
		if (!hFontNew.IsNull)
		{
			DeleteObject(hFontOld);
			EnumChildWindows(hWnd, (HWND hWnd, IntPtr lParam) =>
			{
				SendMessage(hWnd, WindowMessage.WM_SETFONT, lParam, Macros.MAKELPARAM(1, 0));
				return true;
			}, hFontNew.ReleaseOwnership());
		}
	}

	// Create a string that shows the current thread's DPI context and DPI, then send this string to the provided static control
	private static void UpdateDpiString(HWND hWnd, int uDpi)
	{
		// Get the DPI awareness of the window from the DPI-awareness context of the thread
		DPI_AWARENESS_CONTEXT dpiAwarenessContext = GetThreadDpiAwarenessContext();
		DPI_AWARENESS dpiAwareness = GetAwarenessFromDpiAwarenessContext(dpiAwarenessContext);

		// Convert DPI awareness to a string
		string awareness, awarenessContext;
		switch (dpiAwareness)
		{
			case DPI_AWARENESS.DPI_AWARENESS_SYSTEM_AWARE:
				awareness = "DPI_AWARENESS_SYSTEM_AWARE";
				awarenessContext = "DPI_AWARENESS_CONTEXT_SYSTEM_AWARE";
				break;

			case DPI_AWARENESS.DPI_AWARENESS_PER_MONITOR_AWARE:
				awareness = "DPI_AWARENESS_PER_MONITOR_AWARE";
				awarenessContext = AreDpiAwarenessContextsEqual(dpiAwarenessContext, DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
					? "DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2"
					: "DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE";
				break;

			case DPI_AWARENESS.DPI_AWARENESS_UNAWARE:
			// intentional fallthrough
			default:
				awareness = "DPI_AWARENESS_UNAWARE";
				awarenessContext = "DPI_AWARENESS_CONTEXT_UNAWARE";
				break;
		}

		string result = $"DPI Awareness: {awareness}\rDPI Awareness Context: {awarenessContext}\rGetDpiForWindow(...): {uDpi}";
		SetWindowText(hWnd, result);
	}

	// The window procedure for the sample windows
	private static IntPtr WndProc(HWND hWnd, uint message, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)message)
		{
			case WindowMessage.WM_NCCREATE:
				// Enable per-monitor DPI scaling for caption, menu, and top-level scroll bars.
				//
				// Non-client area (scroll bars, caption bar, etc.) does not DPI scale automatically on Windows 8.1. In Windows 10 (1607)
				// support was added for this via a call to EnableNonClientDpiScaling. Windows 10 (1703) supports this automatically when the
				// DPI_AWARENESS_CONTEXT is DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2.
				//
				// Here we are detecting if a bool was set to enable non-client DPI scaling via the call to CreateWindow that resulted in
				// this window. Doing this detection is only necessary in the context of this sample.
				CREATESTRUCT createStruct = lParam.ToStructure<CREATESTRUCT>();
				CreateParams createParams = (CreateParams)GCHandle.FromIntPtr(createStruct.lpCreateParams).Target!;
				if (createParams.bEnableNonClientDpiScaling)
				{
					EnableNonClientDpiScaling(hWnd);
				}

				// Store a flag on the window to note that it'll run its child in a different awareness
				if (createParams.bChildWindowDpiIsolation)
				{
					SetProp(hWnd, PROP_DPIISOLATION, (IntPtr)1);
				}

				return DefWindowProc(hWnd, message, wParam, lParam);

			// Set static text background to white.
			case WindowMessage.WM_CTLCOLORSTATIC:
				return (IntPtr)GetStockBrush(StockObjectType.WHITE_BRUSH);

			case WindowMessage.WM_CREATE:
				return DoInitialWindowSetup(hWnd);

			// On DPI change resize the window, scale the font, and update the DPI-info string
			case WindowMessage.WM_DPICHANGED:
				return (IntPtr)HandleDpiChange(hWnd, wParam, lParam);

			case WindowMessage.WM_CLOSE:
				DestroyWindow(hWnd);
				return default;

			case WindowMessage.WM_COMMAND:
				int wmId = LOWORD(wParam);
				// Parse the menu selections:
				switch (wmId)
				{
					case IDM_SHOWDIALOG:
						ShowFileOpenDialog(hWnd);
						return default;

					default:
						return DefWindowProc(hWnd, message, wParam, lParam);
				}

			case WindowMessage.WM_DESTROY:
				DeleteWindowFont(hWnd);
				return default;
		}
		return DefWindowProc(hWnd, message, wParam, lParam);
	}

	private class CreateParams(bool bEnableNonClientDpiScaling, bool bChildWindowDpiIsolation)
	{
		public bool bChildWindowDpiIsolation = bChildWindowDpiIsolation;
		public bool bEnableNonClientDpiScaling = bEnableNonClientDpiScaling;
	}
}