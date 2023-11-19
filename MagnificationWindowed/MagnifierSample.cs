using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Magnification;
using static Vanara.PInvoke.User32;

namespace MagnificationWindowed;

internal static class MagnifierSample
{
	// For simplicity, the sample uses a constant magnification factor.
	private const float MAGFACTOR = 2.0f;

	private const WindowStyles RESTOREDWINDOWSTYLES = WindowStyles.WS_THICKFRAME | WindowStyles.WS_SYSMENU |
		WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CAPTION | WindowStyles.WS_MAXIMIZEBOX;

	private const uint timerInterval = 16;

	// Global variables and strings.
	private const string WindowClassName = "MagnifierWindow";

	private const string WindowTitle = "Screen Magnifier Sample";

	// close to the refresh rate @60hz
	private static HINSTANCE hInst;

	private static RECT hostWindowRect;
	private static HWND hwndHost;
	private static HWND hwndMag;
	private static SafeHWND? safeHwndHost;
	private static SafeHWND? safeHwndMag;
	private static bool isFullScreen = false;
	private static RECT magWindowRect;

	// FUNCTION: WinMain()
	//
	// PURPOSE: Entry point for the application.
	private static int Main()
	{
		if (!MagInitialize())
			return 0;
		using var uninitMag = new GenericSafeHandle(p => MagUninitialize());

		hInst = GetModuleHandle();
		if (!SetupMagnifier(hInst))
		{
			return 0;
		}

		ShowWindow(hwndHost, ShowWindowCommand.SW_NORMAL);
		UpdateWindow(hwndHost);

		// Create a timer to update the control.
		var timerId = SetTimer(hwndHost, default, timerInterval, UpdateMagWindow);
		if (timerId == 0)
			return -1;
		using var killTimer = new GenericSafeHandle(p => KillTimer(default, timerId));

		// Main message loop.
		MSG msg;
		while (GetMessage(out msg, default, 0, 0) != 0)
		{
			TranslateMessage(msg);
			DispatchMessage(msg);
		}

		// Shut down.
		return (int)msg.wParam;
	}

	// FUNCTION: GoFullScreen()
	//
	// PURPOSE: Makes the host window full-screen by placing non-client elements outside the display.
	private static void GoFullScreen()
	{
		isFullScreen = true;
		// The window must be styled as layered for proper rendering. It is styled as transparent so that it does not capture mouse clicks.
		SetWindowLong(hwndHost, WindowLongFlags.GWL_EXSTYLE, (int)(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_TRANSPARENT));
		// Give the window a system menu so it can be closed on the taskbar.
		SetWindowLong(hwndHost, WindowLongFlags.GWL_STYLE, (int)(WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU));

		// Calculate the span of the display area.
		HDC hDC = GetDC(default);
		var xSpan = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
		var ySpan = GetSystemMetrics(SystemMetric.SM_CYSCREEN);
		ReleaseDC(default, hDC);

		// Calculate the size of system elements.
		var xBorder = GetSystemMetrics(SystemMetric.SM_CXFRAME);
		var yCaption = GetSystemMetrics(SystemMetric.SM_CYCAPTION);
		var yBorder = GetSystemMetrics(SystemMetric.SM_CYFRAME);

		// Calculate the window origin and span for full-screen mode.
		var xOrigin = -xBorder;
		var yOrigin = -yBorder - yCaption;
		xSpan += 2 * xBorder;
		ySpan += 2 * yBorder + yCaption;

		SetWindowPos(hwndHost, SpecialWindowHandles.HWND_TOPMOST, xOrigin, yOrigin, xSpan, ySpan,
			SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
	}

	// FUNCTION: GoPartialScreen()
	//
	// PURPOSE: Makes the host window resizable and focusable.
	private static void GoPartialScreen()
	{
		isFullScreen = false;

		SetWindowLong(hwndHost, WindowLongFlags.GWL_EXSTYLE, (int)(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_LAYERED));
		SetWindowLong(hwndHost, WindowLongFlags.GWL_STYLE, (int)RESTOREDWINDOWSTYLES);
		SetWindowPos(hwndHost, SpecialWindowHandles.HWND_TOPMOST, hostWindowRect.left, hostWindowRect.top, hostWindowRect.right, hostWindowRect.bottom,
			SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
	}

	// FUNCTION: HostWndProc()
	//
	// PURPOSE: Window procedure for the window that hosts the magnifier control.
	private static IntPtr HostWndProc(HWND hWnd, uint message, IntPtr wParam, IntPtr lParam)
	{
		switch (message)
		{
			case (uint)WindowMessage.WM_KEYDOWN:
				if ((int)wParam == (int)ConsoleKey.Escape)
				{
					if (isFullScreen)
					{
						GoPartialScreen();
					}
				}
				break;

			case (uint)WindowMessage.WM_SYSCOMMAND:
				if ((int)wParam == (int)SysCommand.SC_MAXIMIZE)
				{
					GoFullScreen();
				}
				else
				{
					return DefWindowProc(hWnd, message, wParam, lParam);
				}
				break;

			case (uint)WindowMessage.WM_DESTROY:
				PostQuitMessage(0);
				break;

			case (uint)WindowMessage.WM_SIZE:
				if (hwndMag != default)
				{
					GetClientRect(hWnd, out magWindowRect);
					// Resize the control to fill the window.
					SetWindowPos(hwndMag, default, magWindowRect.left, magWindowRect.top, magWindowRect.right, magWindowRect.bottom, 0);
				}
				break;

			default:
				return DefWindowProc(hWnd, message, wParam, lParam);
		}
		return IntPtr.Zero;
	}

	// FUNCTION: RegisterHostWindowClass()
	//
	// PURPOSE: Registers the window class for the window that contains the magnification control.
	private static ATOM RegisterHostWindowClass(HINSTANCE hInstance)
	{
		var wcex = new WNDCLASSEX
		{
			cbSize = (uint)Marshal.SizeOf<WNDCLASSEX>(),
			style = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW,
			lpfnWndProc = HostWndProc,
			hInstance = hInstance,
			hCursor = LoadCursor(default, StandardCursor.IDC_ARROW),
			hbrBackground = (IntPtr)(1 + (int)SystemColorIndex.COLOR_BTNFACE),
			lpszClassName = WindowClassName
		};

		return RegisterClassEx(wcex);
	}

	// FUNCTION: SetupMagnifier
	//
	// PURPOSE: Creates the windows and initializes magnification.
	private static bool SetupMagnifier(HINSTANCE hinst)
	{
		// Set bounds of host window according to screen size.
		hostWindowRect = new RECT(0, 0, GetSystemMetrics(SystemMetric.SM_CXSCREEN), GetSystemMetrics(SystemMetric.SM_CYSCREEN) / 4);

		// Create the host window.
		RegisterHostWindowClass(hinst);
		safeHwndHost = CreateWindowEx(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_LAYERED, WindowClassName, WindowTitle,
			RESTOREDWINDOWSTYLES, 0, 0, hostWindowRect.right, hostWindowRect.bottom, default, default, hInst, default);
		if (safeHwndHost.IsNull)
		{
			return false;
		}
		hwndHost = safeHwndHost;

		// Make the window opaque.
		SetLayeredWindowAttributes(hwndHost, 0, 255, LayeredWindowAttributes.LWA_ALPHA);

		// Create a magnifier control that fills the client area.
		GetClientRect(hwndHost, out magWindowRect);
		safeHwndMag = CreateWindowEx(0, WC_MAGNIFIER, "MagnifierWindow",
			WindowStyles.WS_CHILD | (WindowStyles)(int)MagnifierStyles.MS_SHOWMAGNIFIEDCURSOR | WindowStyles.WS_VISIBLE,
			magWindowRect.left, magWindowRect.top, magWindowRect.right, magWindowRect.bottom,
			hwndHost, default, hInst, default);
		if (safeHwndMag.IsInvalid)
		{
			return false;
		}
		hwndMag = safeHwndMag;

		// Set the magnification factor.
		//var matrix = new MAGTRANSFORM(new[,] { { MAGFACTOR, 0f, 0f }, { 0f, MAGFACTOR, 0f }, { 0f, 0f, 1f } });
		var matrix = new MAGTRANSFORM(MAGFACTOR);

		var ret = MagSetWindowTransform(hwndMag, matrix);

		if (ret)
		{
			var magEffectInvert = new MAGCOLOREFFECT(new[,] { // MagEffectInvert
				{ -1.0f, 0.0f, 0.0f, 0.0f, 0.0f },
				{ 0.0f, -1.0f, 0.0f, 0.0f, 0.0f },
				{ 0.0f, 0.0f, -1.0f, 0.0f, 0.0f },
				{ 0.0f, 0.0f, 0.0f, 1.0f, 0.0f },
				{ 1.0f, 1.0f, 1.0f, 0.0f, 1.0f }
				});

			ret = MagSetColorEffect(hwndMag, magEffectInvert);
		}

		return ret;
	}

	// FUNCTION: UpdateMagWindow()
	//
	// PURPOSE: Sets the source rectangle and updates the window. Called by a timer.
	private static void UpdateMagWindow(HWND hwnd, uint msg, nuint lp, uint id)
	{
		GetCursorPos(out var mousePoint);

		var width = (int)((magWindowRect.right - magWindowRect.left) / MAGFACTOR);
		var height = (int)((magWindowRect.bottom - magWindowRect.top) / MAGFACTOR);
		var sourceRect = new RECT(mousePoint.X - width / 2, mousePoint.Y - height / 2, 0, 0);

		// Don't scroll outside desktop area.
		if (sourceRect.left < 0)
		{
			sourceRect.left = 0;
		}
		if (sourceRect.left > GetSystemMetrics(SystemMetric.SM_CXSCREEN) - width)
		{
			sourceRect.left = GetSystemMetrics(SystemMetric.SM_CXSCREEN) - width;
		}
		sourceRect.right = sourceRect.left + width;

		if (sourceRect.top < 0)
		{
			sourceRect.top = 0;
		}
		if (sourceRect.top > GetSystemMetrics(SystemMetric.SM_CYSCREEN) - height)
		{
			sourceRect.top = GetSystemMetrics(SystemMetric.SM_CYSCREEN) - height;
		}
		sourceRect.bottom = sourceRect.top + height;

		// Set the source rectangle for the magnifier control.
		MagSetWindowSource(hwndMag, sourceRect);

		// Reclaim topmost status, to prevent unmagnified menus from remaining in view.
		SetWindowPos(hwndHost, SpecialWindowHandles.HWND_TOPMOST, 0, 0, 0, 0, SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);

		// Force redraw.
		InvalidateRect(hwndMag, default, true);
	}
}