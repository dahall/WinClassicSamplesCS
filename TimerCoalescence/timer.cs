using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

class Program
{
	/******************************************************************
	* Global Variables
	* ****************************************************************/
	const nuint TIMERID_NOCOALSCING = 0;
	const nuint TIMERID_COALESCING = 1;
	const int TIMERID_UPDATE_SCREEN = 100;
	const int TIMER_ELAPSE = (int)(15.6 * 2);
	const uint TIMER_TOLERANCE = 80;
	const int TIMER_CONTIGUOUS_RUN = 100;
	const int TIMER_AUTO_REFRESH_ELAPSE = 5 * 1000;

	class TimerRec
	{
		public long lLast;
		public long lCount;
		public long lElapsedMin;
		public long lElapsedMax;
		public long lSum;
	}

	static readonly TimerRec[] gTimerRec = [new(), new()];

	static long glFrequency;

	/******************************************************************
	* WinMain - This method is used to create and display the application window, and provides a convenient place to create any device independent resources that will be required.
	* ****************************************************************/
	internal static int Main()
	{
		// Prepare the high resolution performance counter.
		SetThreadAffinityMask(GetCurrentThread(), 0);
		if (!QueryPerformanceFrequency(out var freq))
		{
			return 0;
		}
		glFrequency = freq;

		// Initialize the result array.
		gTimerRec[0].lElapsedMin = gTimerRec[1].lElapsedMin = long.MaxValue;

		// Register window class
		WindowClass wcex = new("TimerApp", default, WndProc, WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_HREDRAW, extraBytes: IntPtr.Size);

		SetProcessDPIAware();

		// Create & prepare the main window
		using var ghwnd = CreateWindow(wcex.ClassName, "Coalescable Timer Sample", WindowStyles.WS_OVERLAPPEDWINDOW, nWidth: 600, nHeight: 400);
		if (ghwnd.IsInvalid)
		{
			return 0;
		}

		ShowWindow(ghwnd, ShowWindowCommand.SW_SHOWNORMAL);
		UpdateWindow(ghwnd);

		return new MessagePump().Run();
	}

	/******************************************************************
	* GetPerformanceCounter Returns the high refolustion time in milliseconds (resolution of tick counts is a bit too coarse for the purpose here)
	*******************************************************************/
	static long GetPerformanceCounter()
	{
		QueryPerformanceCounter(out var t);
		return (t * 1000 + 500) / glFrequency;
	}

	/******************************************************************
	* OnPaint
	* ****************************************************************/
	static void OnPaint([In] HDC hdc, in RECT prcPaint)
	{
		FillRect(hdc, prcPaint, (HBRUSH)GetStockObject(StockObjectType.WHITE_BRUSH));

		TimerRec nocoal = gTimerRec[TIMERID_NOCOALSCING];
		TimerRec coal = gTimerRec[TIMERID_COALESCING];

		string wzText = string.Format("Timer non-coalesced Min = {0}, Avg = {1:0.0}, Max = {2}, ({3} / {4})\n\n" +
			"Timer coalesced Min = {5}, Avg = {6:0.0}, Max = {7}, ({8} / {9})\n\n" +
			"[Elapse = {10}ms, Coaclescing tolerance = {11}ms]\n\n" +
			"Hit space to turn off the monitor",
			MakeItLookNormal(nocoal.lElapsedMin),
			Average(nocoal.lSum, nocoal.lCount),
			nocoal.lElapsedMax,
			nocoal.lSum,
			nocoal.lCount,
			MakeItLookNormal(coal.lElapsedMin),
			Average(coal.lSum, coal.lCount),
			coal.lElapsedMax,
			coal.lSum,
			coal.lCount,
			TIMER_ELAPSE,
			TIMER_TOLERANCE);

		_ = DrawText(hdc, wzText, -1, prcPaint, DrawTextFlags.DT_TOP | DrawTextFlags.DT_LEFT);

		// If the value is initialized but not set yet,
		// give it some reasonable sane value.
		static long MakeItLookNormal([In] long l) => l == long.MaxValue ? 0 : l;

		static double Average([In] long sum, [In] long count) => count == 0 ? 0 : (double)sum / count;
	}

	/********************************************************************
	* TimerHandler Handles both coalesced and non-coalesced timers, and ref keeps the record of the effective elapsed time.
	* Switches the coalesced modes periodically for comparison.
	*********************************************************************/
	static void TimerHandler([In] HWND hwnd, [In] nuint idEvent)
	{
		if (idEvent >= (uint)gTimerRec.Length)
		{
			return;
		}

		TimerRec t = gTimerRec[idEvent];

		long lTime = GetPerformanceCounter();
		long lElapsed = lTime - t.lLast;
		t.lElapsedMin = Math.Min(t.lElapsedMin, lElapsed);
		t.lElapsedMax = Math.Max(t.lElapsedMax, lElapsed);
		t.lLast = lTime;
		++t.lCount;
		t.lSum += lElapsed;

		if (t.lCount % TIMER_CONTIGUOUS_RUN == 0)
		{
			// Now, let's switch the timer types.
			// First, kill the current timer.
			KillTimer(hwnd, idEvent);

			// Reverse the timer id.
			idEvent = ~idEvent;

			/*
			* Setting new timer - switching the coalescing mode.
			*
			* Note that the coalesced timers may be fired together with other timers that are readied during the coalescing tolerance. As such, in an environment that has a lot of short timers may only see a small or no increase in the average time.
			* If that's the case, try running this sample ref with the minimum number of processes.
			*/
			gTimerRec[idEvent].lLast = GetPerformanceCounter();
			SetCoalescableTimer(hwnd, idEvent, TIMER_ELAPSE, default,
				idEvent == TIMERID_NOCOALSCING ? 0xFFFFFFFF /*TIMERV_NO_COALESCING*/ : TIMER_TOLERANCE);
		}
	}

	/******************************************************************
	* WndProc
	* ****************************************************************/
	static IntPtr WndProc(HWND hwnd, uint message, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)message)
		{
			case WindowMessage.WM_CREATE:
				// Start with non-coalescable timer.
				// Later we switch to the coalescable timer.
				gTimerRec[TIMERID_NOCOALSCING].lLast = GetPerformanceCounter();
				if (SetCoalescableTimer(hwnd, TIMERID_NOCOALSCING, TIMER_ELAPSE, default, 0xFFFFFFFF /*TIMERV_NO_COALESCING*/) == 0)
				{
					return new(-1);
				}

				// Let's update the screen periodically.
				SetTimer(hwnd, TIMERID_UPDATE_SCREEN, TIMER_AUTO_REFRESH_ELAPSE, default);
				return default;

			case WindowMessage.WM_TIMER:
				if (wParam.ToInt32() < gTimerRec.Length)
				{
					TimerHandler(hwnd, (nuint)wParam.ToInt64());
				}
				else if (wParam.ToInt32() == TIMERID_UPDATE_SCREEN)
				{
					// Periodically update the results.
					InvalidateRect(hwnd, default, false);
				}
				break;

			case WindowMessage.WM_MOUSEMOVE:
				InvalidateRect(hwnd, default, false);
				break;

			case WindowMessage.WM_PAINT:
			case WindowMessage.WM_DISPLAYCHANGE:
				HDC hdc = BeginPaint(hwnd, out var ps);
				OnPaint(hdc, ps.rcPaint);
				EndPaint(hwnd, ps);
				return default;

			case WindowMessage.WM_KEYDOWN:
				if (wParam.ToInt32() == (int)VK.VK_SPACE)
				{
					// Space key to power down the monitor.
					DefWindowProc(GetDesktopWindow(), (uint)WindowMessage.WM_SYSCOMMAND, (IntPtr)SysCommand.SC_MONITORPOWER, (IntPtr)2);
				}
				break;

			case WindowMessage.WM_DESTROY:
				PostQuitMessage(0);
				return new(1);
		}

		return DefWindowProc(hwnd, message, wParam, lParam);
	}
}