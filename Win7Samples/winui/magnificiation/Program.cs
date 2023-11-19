using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Magnification;
using static Vanara.PInvoke.User32;

/*************************************************************************************************
*
* File: MagnifierSample.cpp
*
* Description: Implements a simple control that magnifies the screen, using the 
* Magnification API.
*
* The magnification window is quarter-screen by default but can be resized.
* To make it full-screen, use the Maximize button or double-click the caption
* bar. To return to partial-screen mode, click on the application icon in the 
* taskbar and press ESC. 
*
* In full-screen mode, all keystrokes and mouse clicks are passed through to the
* underlying focused application. In partial-screen mode, the window can receive the 
* focus. 
*
* Multiple monitors are not supported.
*
* 
* Requirements: To compile, link to Magnification.lib. The sample must be run with 
* elevated privileges.
*
* The sample is not designed for multimonitor setups.
* 
*  This file is part of the Microsoft WinfFX SDK Code Samples.
* 
*  Copyright (C) Microsoft Corporation.  All rights reserved.
* 
* This source code is intended only as a supplement to Microsoft
* Development Tools and/or on-line documentation.  See these other
* materials for detailed information regarding Microsoft code samples.
* 
* THIS CODE AND INFORMATION ARE PROVIDED AS IS WITHOUT WARRANTY OF ANY
* KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
* IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
* PARTICULAR PURPOSE.
* 
*************************************************************************************************/

// For simplicity, the sample uses a constant magnification factor.
const float MAGFACTOR = 2.0f;
const WindowStyles RESTOREDWINDOWSTYLES = WindowStyles.WS_SIZEBOX | WindowStyles.WS_SYSMENU | WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CAPTION | WindowStyles.WS_MAXIMIZEBOX;

// Global variables and strings.
const string WindowClassName = "MagnifierWindow";
const string WindowTitle = "Screen Magnifier Sample";
const uint timerInterval = 16; // close to the refresh rate @60hz

HINSTANCE hInst = GetModuleHandle();
HWND hwndMag = default, hwndHost = default;
RECT hostWindowRect = default, magWindowRect = default;

bool isFullScreen = false;

//
// FUNCTION: WinMain()
//
// PURPOSE: Entry point for the application.
//
if (!MagInitialize())
{
	return 0;
}
if (!SetupMagnifier())
{
	return 0;
}

ShowWindow(hwndHost, ShowWindowCommand.SW_NORMAL);
UpdateWindow(hwndHost);

// Create a timer to update the control.
var timerId = SetTimer(hwndHost, default, timerInterval, UpdateMagWindow);

// Main message loop.
MSG msg;
while (GetMessage(out msg, default, 0, 0) != 0)
{
	TranslateMessage(msg);
	DispatchMessage(msg);
}

// Shut down.
KillTimer(default, timerId);
MagUninitialize();
return msg.wParam.ToInt32();

//
// FUNCTION: HostWndProc()
//
// PURPOSE: Window procedure for the window that hosts the magnifier control.
//
IntPtr HostWndProc(HWND hWnd, uint message, IntPtr wParam, IntPtr lParam)
{
	switch ((WindowMessage)message)
	{
		case WindowMessage.WM_KEYDOWN:
			if (wParam.ToInt32() == (int)VK.VK_ESCAPE)
			{
				if (isFullScreen)
				{
					GoPartialScreen();
				}
			}
			break;

		case WindowMessage.WM_SYSCOMMAND:
			if (GET_SC_WPARAM(wParam) == SysCommand.SC_MAXIMIZE)
			{
				GoFullScreen();
			}
			else
			{
				return DefWindowProc(hWnd, message, wParam, lParam);
			}
			break;

		case WindowMessage.WM_DESTROY:
			PostQuitMessage(0);
			break;

		case WindowMessage.WM_SIZE:
			if (!hwndMag.IsNull)
			{
				GetClientRect(hWnd, out magWindowRect);
				// Resize the control to fill the window.
				SetWindowPos(hwndMag, default, magWindowRect.left, magWindowRect.top, magWindowRect.right, magWindowRect.bottom, 0);
			}
			break;

		default:
			return DefWindowProc(hWnd, message, wParam, lParam);
	}
	return default;
}

static SysCommand GET_SC_WPARAM(IntPtr wParam) => (SysCommand)(wParam.ToInt32() & 0xfff0);

//
// FUNCTION: RegisterHostWindowClass()
//
// PURPOSE: Registers the window class for the window that contains the magnification control.
//
ATOM RegisterHostWindowClass()
{
	WNDCLASSEX wcex = new()
	{
		cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
		style = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW,
		lpfnWndProc = HostWndProc,
		hInstance = hInst,
		hCursor = LoadCursor(default, IDC_ARROW),
		hbrBackground = (HBRUSH)(1 + SystemColorIndex.COLOR_BTNFACE),
		lpszClassName = WindowClassName
	};
	return RegisterClassEx(wcex);
}

//
// FUNCTION: SetupMagnifier
//
// PURPOSE: Creates the windows and initializes magnification.
//
bool SetupMagnifier()
{
	// Set bounds of host window according to screen size.
	hostWindowRect = new(0, 0, GetSystemMetrics(SystemMetric.SM_CXSCREEN), GetSystemMetrics(SystemMetric.SM_CYSCREEN) / 4);

	// Create the host window.
	RegisterHostWindowClass();
	hwndHost = CreateWindowEx(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_LAYERED, WindowClassName, WindowTitle, RESTOREDWINDOWSTYLES,
		0, 0, hostWindowRect.right, hostWindowRect.bottom, default, default, hInst, default);
	if (!hwndHost)
	{
		return false;
	}

	// Make the window opaque.
	SetLayeredWindowAttributes(hwndHost, 0, 255, LayeredWindowAttributes.LWA_ALPHA);

	// Create a magnifier control that fills the client area.
	GetClientRect(hwndHost, out magWindowRect);
	hwndMag = CreateWindow(WC_MAGNIFIER, "MagnifierWindow", WindowStyles.WS_CHILD | (WindowStyles)MagnifierStyles.MS_SHOWMAGNIFIEDCURSOR | WindowStyles.WS_VISIBLE,
		magWindowRect.left, magWindowRect.top, magWindowRect.right, magWindowRect.bottom, hwndHost, default, hInst, default);
	if (!hwndMag)
	{
		return false;
	}

	// Set the magnification factor.
	MAGTRANSFORM matrix = new();
	matrix[0,0] = MAGFACTOR;
	matrix[1,1] = MAGFACTOR;
	matrix[2,2] = 1.0f;

	bool ret = MagSetWindowTransform(hwndMag, matrix);
	if (ret)
	{
		MAGCOLOREFFECT magEffectInvert = new(new float[,]
		{
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

//
// FUNCTION: UpdateMagWindow()
//
// PURPOSE: Sets the source rectangle and updates the window. Called by a timer.
//
void UpdateMagWindow(HWND hwnd, uint uMsg, nuint idEvent, uint dwTime)
{
	GetCursorPos(out POINT mousePoint);

	int width = (int)((magWindowRect.right - magWindowRect.left) / MAGFACTOR);
	int height = (int)((magWindowRect.bottom - magWindowRect.top) / MAGFACTOR);
	RECT sourceRect = new(mousePoint.X - width / 2, mousePoint.Y - height / 2, 0, 0);

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
	SetWindowPos(hwndHost, HWND.HWND_TOPMOST, 0, 0, 0, 0, SetWindowPosFlags.SWP_NOACTIVATE | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE);

	// Force redraw.
	InvalidateRect(hwndMag, default, true);
}

//
// FUNCTION: GoFullScreen()
//
// PURPOSE: Makes the host window full-screen by placing non-client elements outside the display.
//
void GoFullScreen()
{
	isFullScreen = true;
	// The window must be styled as layered for proper rendering. 
	// It is styled as transparent so that it does not capture mouse clicks.
	SetWindowLong(hwndHost, WindowLongFlags.GWL_EXSTYLE, (int)(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_LAYERED | WindowStylesEx.WS_EX_TRANSPARENT));
	// Give the window a system menu so it can be closed on the taskbar.
	SetWindowLong(hwndHost, WindowLongFlags.GWL_STYLE, (int)(WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU));

	// Calculate the span of the display area.
	HDC hDC = GetDC(default);
	int xSpan = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
	int ySpan = GetSystemMetrics(SystemMetric.SM_CYSCREEN);
	ReleaseDC(default, hDC);

	// Calculate the size of system elements.
	int xBorder = GetSystemMetrics(SystemMetric.SM_CXFRAME);
	int yCaption = GetSystemMetrics(SystemMetric.SM_CYCAPTION);
	int yBorder = GetSystemMetrics(SystemMetric.SM_CYFRAME);

	// Calculate the window origin and span for full-screen mode.
	int xOrigin = -xBorder;
	int yOrigin = -yBorder - yCaption;
	xSpan += 2 * xBorder;
	ySpan += 2 * yBorder + yCaption;

	SetWindowPos(hwndHost, HWND.HWND_TOPMOST, xOrigin, yOrigin, xSpan, ySpan,
		SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
}

//
// FUNCTION: GoPartialScreen()
//
// PURPOSE: Makes the host window resizable and focusable.
//
void GoPartialScreen()
{
	isFullScreen = false;

	SetWindowLong(hwndHost, WindowLongFlags.GWL_EXSTYLE, (int)(WindowStylesEx.WS_EX_TOPMOST | WindowStylesEx.WS_EX_LAYERED));
	SetWindowLong(hwndHost, WindowLongFlags.GWL_STYLE, (int)RESTOREDWINDOWSTYLES);
	SetWindowPos(hwndHost, HWND.HWND_TOPMOST, hostWindowRect.left, hostWindowRect.top, hostWindowRect.right, hostWindowRect.bottom,
		SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
}