using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

/*****************************************************************************
*
* Overview To determine whether the client area is obscured, determine whether
* it has an empty clip region.
*
* To be notified when the client area is again unobscured, invalidate the
* entire client rectangle and wait for Windows to send a WM_PAINT message.
* (Explicit invalidation is required, for if you were obscured by a menu or a
* window with the CS_SAVEBITS class style, Windows will restore your client
* area automatically instead of sending a WM_PAINT message.)
*
* So that you can see the behavior of the application, we will put information
* in the title of the window. Watch the taskbar to see the application change
* its state.
*
*****************************************************************************/

const int OBS_COMPLETELYCOVERED = 0;
const int OBS_PARTIALLYVISIBLE = 1;
const int OBS_COMPLETELYVISIBLE = 2;

bool g_fTimerActive = false;

using SafeHINSTANCE hInst = GetModuleHandle();
WindowClass wc = new("Obscure", hInst, Obscure_WndProc, hbrBkgd: SystemColorIndex.COLOR_WINDOW + 1);

using SafeHWND hwnd = CreateWindow(wc.ClassName, wc.ClassName, WindowStyles.WS_OVERLAPPEDWINDOW,
	CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, default, default, hInst);
ShowWindow(hwnd, ShowWindowCommand.SW_NORMAL);

MSG msg;
while (GetMessage(out msg) != 0)
{
	TranslateMessage(msg);
	DispatchMessage(msg);
}

/*****************************************************************************
*
* GetClientObscuredness Returns one of the OBS_ values.
*
*****************************************************************************/

int GetClientObscuredness(HWND hwnd)
{
	using SafeHDC hdc = GetDC(hwnd);
	RegionFlags iType = GetClipBox(hdc, out RECT rc);

	if (iType == RegionFlags.NULLREGION)
	{
		return OBS_COMPLETELYCOVERED;
	}

	if (iType == RegionFlags.COMPLEXREGION)
	{
		return OBS_PARTIALLYVISIBLE;
	}

	GetClientRect(hwnd, out RECT rcClient);
	return rc == rcClient ? OBS_COMPLETELYVISIBLE : OBS_PARTIALLYVISIBLE;
}

/*****************************************************************************
*
* Obscure_EnsureTimerStopped If the timer that causes us to do work is running, then stop it.
*
*****************************************************************************/
void Obscure_EnsureTimerStopped(HWND hwnd)
{
	if (g_fTimerActive)
	{
		KillTimer(hwnd, (IntPtr)1);
		g_fTimerActive = false;
		InvalidateRect(hwnd, default, false);
		SetWindowText(hwnd, "Covered (Paused)");
	}
}

/*****************************************************************************
*
* Obscure_TimerProc If we have become obscured, then stop the timer.
*
* Otherwise, invalidate our rectangle so we will redraw with the new time.
*
*****************************************************************************/
void Obscure_TimerProc(HWND hwnd, uint uiMsg, IntPtr idTimer, uint tm)
{
	/*
	* If the client area is totally obscured, then stop the timer so we don't waste any more CPU.
	*/
	int iState = GetClientObscuredness(hwnd);
	if (iState == OBS_COMPLETELYCOVERED)
	{
		Obscure_EnsureTimerStopped(hwnd);
	}
	else
	{
		InvalidateRect(hwnd, default, false);
		if (iState == OBS_PARTIALLYVISIBLE)
		{
			SetWindowText(hwnd, "Partially Visible");
		}
		else if (iState == OBS_COMPLETELYVISIBLE)
		{
			SetWindowText(hwnd, "Completely Visible");
		}
	}
}

/*****************************************************************************
*
* Obscure_EnsureTimerRunning If the timer that causes us to do work is not
* running, then start it.
*
*****************************************************************************/
void Obscure_EnsureTimerRunning(HWND hwnd)
{
	if (!g_fTimerActive)
	{
		SetTimer(hwnd, (IntPtr)1, 100, Obscure_TimerProc);
		g_fTimerActive = true;
	}
}

/*****************************************************************************
*
* Obscure_OnPaint Draw the current time and restart the timer if needed.
*
*****************************************************************************/
void Obscure_OnPaint(HWND hwnd)
{
	using SafeHDC hdc = new((IntPtr)BeginPaint(hwnd, out PAINTSTRUCT ps), false);
	if (!hdc.IsNull)
	{
		if (ps.rcPaint.IsEmpty)
		{
			/*
			* Nothing to do. Don't wake up either.
			*/
		}
		else
		{
			using (var ctx = hdc.SelectObject(GetStockObject(StockObjectType.ANSI_FIXED_FONT)))
			{
				COLORREF crTextOld = SetTextColor(hdc, GetSysColor(SystemColorIndex.COLOR_WINDOWTEXT));
				COLORREF crBkOld = SetBkColor(hdc, GetSysColor(SystemColorIndex.COLOR_WINDOW));

				GetClientRect(hwnd, out RECT rc);
				string sz = GetTickCount().ToString();
				ExtTextOut(hdc, 0, 0, ETO.ETO_OPAQUE, rc, sz, (uint)sz.Length, default);

				SetBkColor(hdc, crBkOld);
				SetTextColor(hdc, crTextOld);
			}

			Obscure_EnsureTimerRunning(hwnd);
		}

		EndPaint(hwnd, ps);
	}
}

/*****************************************************************************
*
* Obscure_WndProc Window procedure for obscured window demo.
*
*****************************************************************************/
IntPtr Obscure_WndProc(HWND hwnd, uint uiMsg, IntPtr wParam, IntPtr lParam)
{
	switch ((WindowMessage)uiMsg)
	{
		case WindowMessage.WM_CREATE:
			Obscure_EnsureTimerRunning(hwnd);
			break;

		case WindowMessage.WM_PAINT:
			Obscure_OnPaint(hwnd);
			break;

		/*
		* The app exits when this window is destroyed.
		*/
		case WindowMessage.WM_DESTROY:
			Obscure_EnsureTimerStopped(hwnd);
			PostQuitMessage(0);
			break;
	}

	return DefWindowProc(hwnd, uiMsg, wParam, lParam);
}