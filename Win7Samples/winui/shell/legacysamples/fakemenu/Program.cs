//
//      Overview
//
//      Normally, pop-up windows receive activation, resulting in the
//      owner window being de-activated.  To prevent the owner from
//      being de-activated, the pop-up window should not receive
//      activation.
//
//      Since the pop-up window is not active, input messages are not
//      delivered to the pop-up.  Instead, the input messages must be
//      explicitly inspected by the message loop.
//
//      Our sample program illustrates how you can create a pop-up
//      window that contains a selection of colors.
//
//      Right-click in the window to change its background color
//      via the fake menu popup.  Observe
//
//      -   The caption of the main application window remains
//          highlighted even though the fake-menu is "active".
//
//      -   The current fake-menu item highlight follows the mouse.
//
//      -   The keyboard arrows can be used to move the highlight,
//          ESC cancels the fake-menu, Enter accepts the fake-menu.
//
//      -   The fake-menu appears on the correct monitor (for
//          multiple-monitor systems).
//
//      -   The fake-menu switches to keyboard focus highlighting
//          once keyboard menu navigation is employed.

using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.DwmApi;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Macros;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.UxTheme;

internal class Program
{
	// This is the array of predefined colors we put into the color picker.
	static readonly COLORREF[] c_rgclrPredef = new COLORREF[]
	{
		new(0x00, 0x00, 0x00), // 0 = black
		new(0x80, 0x00, 0x00), // 1 = maroon
		new(0x00, 0x80, 0x00), // 2 = green
		new(0x80, 0x80, 0x00), // 3 = olive
		new(0x00, 0x00, 0x80), // 4 = navy
		new(0x80, 0x00, 0x80), // 5 = purple
		new(0x00, 0x80, 0x80), // 6 = teal
		new(0x80, 0x80, 0x80), // 7 = gray
		new(0xC0, 0xC0, 0xC0), // 8 = silver
		new(0xFF, 0x00, 0x00), // 9 = red
		new(0x00, 0xFF, 0x00), // A = lime
		new(0xFF, 0xFF, 0x00), // B = yellow
		new(0x00, 0x00, 0xFF), // C = blue
		new(0xFF, 0x00, 0xFF), // D = fuchsia
		new(0x00, 0xFF, 0xFF), // E = cyan
		new(0xFF, 0xFF, 0xFF), // F = white
	};

	static COLORREF g_clrBackground = new(0xFF, 0xFF, 0xFF);
	static SafeHINSTANCE? g_hInstance;
	static COLORPICKSTATE cps;

	//
	// Program entry point - demonstrate pseudo-menus.
	//
	private static void Main()
	{
		g_hInstance = GetModuleHandle();

		WindowClass wc = new("ColorPick", g_hInstance, ColorPick_WndProc, hCursor: LoadCursor(default, IDC_ARROW), hbrBkgd: SystemColorIndex.COLOR_3DFACE + 1);

		VisibleWindow.Run(FakeMenuDemo_WndProc, "Fake Menu Demo - Right-click in window to change color");
	}

	// FillRectClr
	//
	// Helper function to fill a rectangle with a solid color.
	//
	static void FillRectClr(HDC hdc, in RECT prc, COLORREF clr)
	{
		SetDCBrushColor(hdc, clr);
		FillRect(hdc, prc, (HBRUSH)GetStockObject(StockObjectType.DC_BRUSH));
	}

	static bool IsHighContrast()
	{
		SystemParametersInfo<HIGHCONTRAST>(SPI.SPI_GETHIGHCONTRAST, out var hc);
		return hc.dwFlags.IsFlagSet(HFC.HCF_HIGHCONTRASTON);
	}

	static void ColorPickState_Initialize(HWND hwndOwner) => cps = new()
	{
		fDone = false, // Not done yet
		iSel = -1, // No initial selection
		iResult = -1, // No result
		hwndOwner = hwndOwner, // Owner window
		fKeyboardUsed = false // No keyboard usage yet
	};

	const int CYCOLOR = 16; // Height of a single color pick
	const int CXFAKEMENU = 100; // Width of our fake menu

	//
	// ColorPick_GetColorRect
	//
	// Returns the rectangle that encloses the specified color.
	//
	static RECT ColorPick_GetColorRect(int iColor) => new()
	{
		left = 0,
		right = CXFAKEMENU,
		top = iColor * CYCOLOR,
		bottom = iColor * CYCOLOR + CYCOLOR
	};

	// ColorPick_UpdateVisuals
	//
	// Update the theme and nonclient rendering to match current user preference.
	//
	static void ColorPick_UpdateVisuals(HWND hwnd)
	{
		if (cps.hTheme is not null)
		{
			cps.hTheme = default;
		}

		if (IsThemeActive() && !IsHighContrast())
		{
			cps.hTheme = OpenThemeData(hwnd, VSCLASS_MENU);
		}

		if (cps.hTheme is not null)
		{
			DWM_WINDOW_CORNER_PREFERENCE preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUNDSMALL;
			DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, preference);

			if (GetThemeColor(cps.hTheme, (int)MENUPARTS.MENU_POPUPBORDERS, 0, (int)ThemeProperty.TMT_FILLCOLORHINT, out COLORREF color).Succeeded)
			{
				DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, color);
			}
		}
		else
		{
			DWM_WINDOW_CORNER_PREFERENCE preference = DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_DEFAULT;
			DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, preference);

			COLORREF color = DWMWA_COLOR_DEFAULT;
			DwmSetWindowAttribute(hwnd, DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR, color);
		}

		// Use the MENU_POPUPITEMKBFOCUS and MENU_POPUPITEM_FOCUSABLE parts if available.
		if (cps.hTheme is not null &&
			IsThemePartDefined(cps.hTheme, (int)MENUPARTS.MENU_POPUPITEM_FOCUSABLE, (int)POPUPITEMFOCUSABLESTATES.MPIF_HOT))
		{
			cps.iPartKeyboardFocus = (int)MENUPARTS.MENU_POPUPITEMKBFOCUS;
			cps.iPartNonFocus = (int)MENUPARTS.MENU_POPUPITEM_FOCUSABLE;
			cps.iStateFocus = (int)POPUPITEMFOCUSABLESTATES.MPIF_HOT;
		}
		else
		{
			cps.iPartKeyboardFocus = (int)MENUPARTS.MENU_POPUPITEM;
			cps.iPartNonFocus = (int)MENUPARTS.MENU_POPUPITEM;
			cps.iStateFocus = (int)POPUPITEMSTATES.MPI_HOT;
		}
		if (cps.hTheme is not null &&
			GetThemeMargins(cps.hTheme, default, (int)MENUPARTS.MENU_POPUPITEM, 0, (int)ThemeProperty.TMT_CONTENTMARGINS, default, out cps.marginsItem).Succeeded)
		{
			// Successfully obtained item content margins.
		}
		else
		{
			// Use fallback values for item content margins.
			cps.marginsItem.cxLeftWidth = cps.marginsItem.cxRightWidth = GetSystemMetrics(SystemMetric.SM_CXEDGE);
			cps.marginsItem.cyTopHeight = cps.marginsItem.cyBottomHeight = GetSystemMetrics(SystemMetric.SM_CYEDGE);
		}

		// Force everything to repaint with the new visuals.
		InvalidateRect(hwnd, default, true);
	}

	// ColorPick_OnCreate
	//
	// Stash away our state.
	//
	static IntPtr ColorPick_OnCreate(HWND hwnd, in CREATESTRUCT pcs)
	{
		//SetWindowLong(hwnd, WindowLongFlags.GWL_USERDATA, pcs.lpCreateParams);
		ColorPick_UpdateVisuals(hwnd);
		return IntPtr.Zero;
	}

	//
	// ColorPick_OnPaint
	//
	// Draw the background, color bars, and appropriate highlighting for the selected color.
	//
	static void ColorPick_OnPaint(HWND hwnd)
	{
		HDC hdc = BeginPaint(hwnd, out var ps);
		if (!hdc.IsNull)
		{
			GetClientRect(hwnd, out _);

			// Let the theme chooses the background fill color.
			if (cps.hTheme is not null)
			{
				if (GetThemeColor(cps.hTheme, (int)MENUPARTS.MENU_POPUPBACKGROUND, 0, (int)ThemeProperty.TMT_FILLCOLORHINT, out var color).Succeeded)
				{
					FillRectClr(hdc, ps.rcPaint, color);
				}
			}

			// For each of our predefined colors, draw it in a little
			// rectangular region, leaving some border so the user can
			// see if the item is highlighted or not.

			for (int iColor = 0; iColor < c_rgclrPredef.Length; iColor++)
			{
				// Build the "menu" item rect.
				RECT rc = ColorPick_GetColorRect(iColor);

				// If the item is highlighted, then draw a highlighted background.
				if (iColor == cps.iSel)
				{
					// Draw focus effect.
					if (cps.hTheme is not null)
					{
						// If keyboard navigation active, then use the keyboard focus visuals.
						// Otherwise, use the traditional visuals.
						DrawThemeBackground(cps.hTheme, hdc,
							cps.fKeyboardUsed ? cps.iPartKeyboardFocus : (int)MENUPARTS.MENU_POPUPITEM,
							cps.fKeyboardUsed ? cps.iStateFocus : (int)POPUPITEMSTATES.MPI_HOT, rc, null);
					}
					else
					{
						FillRect(hdc, rc, GetSysColorBrush(SystemColorIndex.COLOR_HIGHLIGHT));
					}
				}
				else
				{
					// Draw non-focus effect.
					if (cps.hTheme is not null)
					{
						DrawThemeBackground(cps.hTheme, hdc,
							cps.fKeyboardUsed ? cps.iPartNonFocus : (int)MENUPARTS.MENU_POPUPITEM,
							(int)POPUPITEMSTATES.MPI_NORMAL, rc, null);
					}
					else
					{
						// If not themed, then our background brush already filled for us.
					}
				}

				// Now shrink the rectangle by the margins and fill the rest with the
				// color of the item itself.
				rc.left += cps.marginsItem.cxLeftWidth;
				rc.right -= cps.marginsItem.cxRightWidth;
				rc.top += cps.marginsItem.cyTopHeight;
				rc.bottom -= cps.marginsItem.cyBottomHeight;

				FillRectClr(hdc, rc, c_rgclrPredef[iColor]);
			}

			EndPaint(hwnd, ps);
		}
	}

	//
	// ColorPick_ChangeSel
	//
	// Change the selection to the specified item.
	//
	static void ColorPick_ChangeSel(HWND hwnd, int iSel)
	{
		// If the selection changed, then repaint the items that need repainting.
		if (cps.iSel != iSel)
		{
			if (cps.iSel >= 0)
			{
				RECT rc = ColorPick_GetColorRect(cps.iSel);
				InvalidateRect(hwnd, rc, true);
			}

			cps.iSel = iSel;
			if (cps.iSel >= 0)
			{
				RECT rc = ColorPick_GetColorRect(cps.iSel);
				InvalidateRect(hwnd, rc, true);
			}
		}
	}

	//
	// ColorPick_OnMouseMove
	//
	// Track the mouse to see if it is over any of our colors.
	//
	static void ColorPick_OnMouseMove(HWND hwnd, int x, int y)
	{
		int iSel;

		if (x >= 0 && x < CXFAKEMENU && y >= 0 && y < c_rgclrPredef.Length * CYCOLOR)
		{
			iSel = y / CYCOLOR;
		}
		else
		{
			iSel = -1;
		}

		ColorPick_ChangeSel(hwnd, iSel);
	}

	//
	// ColorPick_OnLButtonUp
	//
	// When the button comes up, we are done.
	//
	static void ColorPick_OnLButtonUp(HWND hwnd, int x, int y)
	{
		// First track to the final location, in case the user moves the mouse
		// REALLY FAST and immediately lets go.
		ColorPick_OnMouseMove(hwnd, x, y);

		// Set the result to the current selection.
		cps.iResult = cps.iSel;

		// And tell the message loop that we're done.
		cps.fDone = true;
	}

	//
	// ColorPick_OnKeyDown
	//
	// If the ESC key is pressed, then abandon the fake menu.
	// If the Enter key is pressed, then accept the current selection.
	// If an arrow key is pressed, the move the selection.
	//
	static void ColorPick_OnKeyDown(HWND hwnd, IntPtr vk)
	{
		cps.fKeyboardUsed = true;

		switch ((VK)vk.ToInt32())
		{
			case VK.VK_ESCAPE:
				cps.fDone = true; // Abandoned
				break;

			case VK.VK_RETURN:
				cps.iResult = cps.iSel; // Accept current selection
				cps.fDone = true;
				break;

			case VK.VK_UP:
				if (cps.iSel > 0) // Decrement selection
				{
					ColorPick_ChangeSel(hwnd, cps.iSel - 1);
				}
				else
				{
					ColorPick_ChangeSel(hwnd, c_rgclrPredef.Length - 1);
				}
				break;

			case VK.VK_DOWN: // Increment selection
				if (cps.iSel + 1 < c_rgclrPredef.Length)
				{
					ColorPick_ChangeSel(hwnd, cps.iSel + 1);
				}
				else
				{
					ColorPick_ChangeSel(hwnd, 0);
				}
				break;
		}
	}

	//
	// ColorPick_WndProc
	//
	// Window procedure for the color picker popup.
	//
	static IntPtr ColorPick_WndProc(HWND hwnd, uint uiMsg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)uiMsg)
		{
			case WindowMessage.WM_CREATE:
				return ColorPick_OnCreate(hwnd, lParam.ToStructure<CREATESTRUCT>());

			case WindowMessage.WM_MOUSEMOVE:
				ColorPick_OnMouseMove(hwnd, (short)LOWORD(lParam), (short)HIWORD(lParam));
				break;

			case WindowMessage.WM_LBUTTONUP:
				ColorPick_OnLButtonUp(hwnd, (short)LOWORD(lParam), (short)HIWORD(lParam));
				break;

			case WindowMessage.WM_SYSKEYDOWN:
			case WindowMessage.WM_KEYDOWN:
				ColorPick_OnKeyDown(hwnd, wParam);
				break;

			// Do not activate when somebody clicks the window.
			case WindowMessage.WM_MOUSEACTIVATE:
				return new((int)MouseActivateCode.MA_NOACTIVATE);

			case WindowMessage.WM_PAINT:
				ColorPick_OnPaint(hwnd);
				return IntPtr.Zero;

			case WindowMessage.WM_THEMECHANGED:
			case WindowMessage.WM_SETTINGCHANGE:
				ColorPick_UpdateVisuals(hwnd);
				break;
		}
		return DefWindowProc(hwnd, uiMsg, wParam, lParam);
	}

	//
	// ColorPick_ChooseLocation
	//
	// Find a place to put the window so it won't go off the screen
	// or straddle two monitors.
	//
	// x, y = location of mouse click (preferred upper-left corner)
	// cx, cy = size of window being created
	//
	// We use the same logic that real menus use.
	//
	// - If (x, y) is too high or too far left, then slide onto screen.
	// - Use (x, y) if all fits on the monitor.
	// - If too low, then slide up.
	// - If too far right, then flip left.
	//
	static void ColorPick_ChooseLocation(HWND hwnd, int x, int y, int cx, int cy, out POINT ppt)
	{
		// First get the dimensions of the monitor that contains (x, y).
		ppt = new(x, y);
		HMONITOR hmon = MonitorFromPoint(ppt, MonitorFlags.MONITOR_DEFAULTTONULL);

		// If (x, y) is not on any monitor, then use the monitor that the owner
		// window is on.
		if (hmon.IsNull)
		{
			hmon = MonitorFromWindow(hwnd, MonitorFlags.MONITOR_DEFAULTTONEAREST);
		}

		MONITORINFO minf = new() { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
		GetMonitorInfo(hmon, ref minf);

		// Now slide things around until they fit.

		// If too high, then slide down.
		if (ppt.Y < minf.rcMonitor.top)
		{
			ppt.Y = minf.rcMonitor.top;
		}

		// If too far left, then slide right.
		if (ppt.X < minf.rcMonitor.left)
		{
			ppt.X = minf.rcMonitor.left;
		}

		// If too low, then slide up.
		if (ppt.Y > minf.rcMonitor.bottom - cy)
		{
			ppt.Y = minf.rcMonitor.bottom - cy;
		}

		// If too far right, then flip left.
		if (ppt.X > minf.rcMonitor.right - cx)
		{
			ppt.X -= cx;
		}
	}

	//
	// ColorPick_Popup
	//
	// Display a fake menu to allow the user to select the color.
	//
	// Return the color index the user selected, or -1 if no color
	// was selected.
	//
	static int ColorPick_Popup(HWND hwndOwner, int x, int y)
	{
		// Early check: We must be on same thread as the owner so we can see
		// its mouse and keyboard messages when we set capture to it.
		if (GetCurrentThreadId() != GetWindowThreadProcessId(hwndOwner, out _))
		{
			// Error: Menu must be on same thread as parent window.
			return -1;
		}

		// Set up the style and extended style we want to use.
		WindowStyles dwStyle = WindowStyles.WS_POPUP | WindowStyles.WS_BORDER;
		WindowStylesEx dwExStyle = WindowStylesEx.WS_EX_TOOLWINDOW | // So it doesn't show up in taskbar
			WindowStylesEx.WS_EX_DLGMODALFRAME | // Get the edges right
			WindowStylesEx.WS_EX_WINDOWEDGE |
			WindowStylesEx.WS_EX_TOPMOST; // So it isn't obscured

		// We want a client area of size (CXFAKEMENU, c_rgclrPredef.ref Length CYCOLOR),
		// so use AdjustWindowRectEx to figure out what window rect will give us a
		// client rect of that size.
		RECT rc = new() { right = CXFAKEMENU, bottom = c_rgclrPredef.Length * CYCOLOR };
		AdjustWindowRectEx(ref rc, dwStyle, false, dwExStyle);

		// Now find a proper home for the window that won't go off the screen or
		// straddle two monitors.
		int cx = rc.right - rc.left;
		int cy = rc.bottom - rc.top;
		ColorPick_ChooseLocation(hwndOwner, x, y, cx, cy, out POINT pt);

		ColorPickState_Initialize(hwndOwner);
		HWND hwndPopup = CreateWindowEx(dwExStyle, "ColorPick", "", dwStyle, pt.X, pt.Y, cx, cy, hwndOwner, default, g_hInstance);

		// Show the window but don't activate it!
		ShowWindow(hwndPopup, ShowWindowCommand.SW_SHOWNOACTIVATE);

		// We want to receive all mouse messages, but since only the active
		// window can capture the mouse, we have to set the capture to our
		// owner window, and then steal the mouse messages out from under it.
		SetCapture(hwndOwner);

		// Go into a message loop that filters all the messages it receives
		// and route the interesting ones to the color picker window.
		MSG msg;
		while (GetMessage(out msg) != 0)
		{
			// Something may have happened that caused us to stop.
			if (cps.fDone)
			{
				break;
			}

			// If our owner stopped being the active window (e.g. the user
			// Alt+Tab'd to another window in the meantime), then stop.
			HWND hwndActive = GetActiveWindow();
			if (hwndActive != hwndOwner && !IsChild(hwndActive, hwndOwner) || GetCapture() != hwndOwner)
			{
				break;
			}

			// At this point, we get to snoop at all input messages before
			// they get dispatched. This allows us to route all input to our
			// popup window even if really belongs to somebody else.

			// All mouse messages are remunged and directed at our popup
			// menu. If the mouse message arrives as client coordinates, then
			// we have to convert it from the client coordinates of the original
			// target to the client coordinates of the new target.
			switch ((WindowMessage)msg.message)
			{
				// These mouse messages arrive in client coordinates, so in
				// addition to stealing the message, we also need to convert the
				// coordinates.
				case WindowMessage.WM_MOUSEMOVE:
				case WindowMessage.WM_LBUTTONDOWN:
				case WindowMessage.WM_LBUTTONUP:
				case WindowMessage.WM_LBUTTONDBLCLK:
				case WindowMessage.WM_RBUTTONDOWN:
				case WindowMessage.WM_RBUTTONUP:
				case WindowMessage.WM_RBUTTONDBLCLK:
				case WindowMessage.WM_MBUTTONDOWN:
				case WindowMessage.WM_MBUTTONUP:
				case WindowMessage.WM_MBUTTONDBLCLK:
					pt.X = (short)LOWORD(msg.lParam);
					pt.Y = (short)HIWORD(msg.lParam);
					MapWindowPoints(msg.hwnd, hwndPopup, ref pt);
					msg.lParam = MAKELPARAM((ushort)pt.X, (ushort)pt.Y);
					msg.hwnd = hwndPopup;
					break;

				// These mouse messages arrive in screen coordinates, so we just
				// need to steal the message.
				case WindowMessage.WM_NCMOUSEMOVE:
				case WindowMessage.WM_NCLBUTTONDOWN:
				case WindowMessage.WM_NCLBUTTONUP:
				case WindowMessage.WM_NCLBUTTONDBLCLK:
				case WindowMessage.WM_NCRBUTTONDOWN:
				case WindowMessage.WM_NCRBUTTONUP:
				case WindowMessage.WM_NCRBUTTONDBLCLK:
				case WindowMessage.WM_NCMBUTTONDOWN:
				case WindowMessage.WM_NCMBUTTONUP:
				case WindowMessage.WM_NCMBUTTONDBLCLK:
					msg.hwnd = hwndPopup;
					break;

				// We need to steal all keyboard messages, too.
				case WindowMessage.WM_KEYDOWN:
				case WindowMessage.WM_KEYUP:
				case WindowMessage.WM_CHAR:
				case WindowMessage.WM_DEADCHAR:
				case WindowMessage.WM_SYSKEYDOWN:
				case WindowMessage.WM_SYSKEYUP:
				case WindowMessage.WM_SYSCHAR:
				case WindowMessage.WM_SYSDEADCHAR:
					msg.hwnd = hwndPopup;
					break;
			}

			TranslateMessage(msg);
			DispatchMessage(msg);

			// Something may have happened that caused us to stop.
			if (cps.fDone)
			{
				break;
			}

			// If our owner stopped being the active window (e.g. the user
			// Alt+Tab'd to another window in the meantime), then stop.
			hwndActive = GetActiveWindow();
			if (hwndActive != hwndOwner && !IsChild(hwndActive, hwndOwner) || GetCapture() != hwndOwner)
			{
				break;
			}
		}

		// Clean up the capture we created.
		ReleaseCapture();

		DestroyWindow(hwndPopup);

		// Clean up any theme data.
		if (cps.hTheme is not null)
		{
			cps.hTheme = null;
		}

		// If we got a WM_QUIT message, then re-post it so the caller's message
		// loop will see it.
		if (msg.message == (uint)WindowMessage.WM_QUIT)
		{
			PostQuitMessage((int)msg.wParam);
		}

		return cps.iResult;
	}

	//
	// FakeMenuDemo_OnEraseBkgnd
	//
	// Erase the background in the selected color.
	//
	static void FakeMenuDemo_OnEraseBkgnd(HWND hwnd, HDC hdc)
	{
		GetClientRect(hwnd, out RECT rc);
		FillRectClr(hdc, rc, g_clrBackground);
	}

	//
	// FakeMenuDemo_OnContextMenu
	//
	// Display the color-picker pseudo-menu so the user can change
	// the color.
	//
	static void FakeMenuDemo_OnContextMenu(HWND hwnd, int x, int y)
	{
		// If the coordinates are (-1, -1), then the user used the keyboard -
		// we'll pretend the user clicked at client (0, 0).
		if (x == -1 && y == -1)
		{
			POINT pt = default;
			ClientToScreen(hwnd, ref pt);
			x = pt.X;
			y = pt.Y;
		}

		int iColor = ColorPick_Popup(hwnd, x, y);

		// If the user picked a color, then change to that color.
		if (iColor >= 0)
		{
			g_clrBackground = c_rgclrPredef[iColor];
			InvalidateRect(hwnd, default, true);
		}
	}

	//
	// FakeMenuDemo_WndProc
	//
	// Window procedure for the fake menu demo.
	//
	static IntPtr FakeMenuDemo_WndProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)msg)
		{
			case WindowMessage.WM_ERASEBKGND:
				FakeMenuDemo_OnEraseBkgnd(hwnd, (HDC)wParam);
				return new(1);

			case WindowMessage.WM_CONTEXTMENU:
				FakeMenuDemo_OnContextMenu(hwnd, (short)LOWORD(lParam),
				(short)HIWORD(lParam));
				return IntPtr.Zero;

			case WindowMessage.WM_DESTROY:
				PostQuitMessage(0);
				break;
		}

		return DefWindowProc(hwnd, msg, wParam, lParam);
	}

	//
	// COLORPICKSTATE
	//
	// Structure that records the state of a color-picker pop-up.
	//
	// A pointer to this state information is kept in the GWLP_USERDATA
	// window long.
	//
	// The iSel field is the index of the selected color, or the
	// special value -1 to mean that no item is highlighted.
	//
	internal struct COLORPICKSTATE
	{
		public bool fDone; // Set when we should get out
		public int iSel; // Which color is selected?
		public int iResult; // Which color should be returned?
		public HWND hwndOwner; // Our owner window
		public bool fKeyboardUsed; // Has the keyboard been used?
		public SafeHTHEME? hTheme; // The active theme, if any
		public int iPartKeyboardFocus; // MENU_POPUPITEMKBFOCUS if supported, else MENU_POPUPITEM
		public int iPartNonFocus; // MENU_POPUPITEM_FOCUSABLE if supported, else MENU_POPUPITEM
		public int iStateFocus; // MPIF_HOT if using the FOCUSABLE parts, else MPI_HOT
		public UxTheme.MARGINS marginsItem; // Margins of a popup item
	}
}