using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.SHCore;
using static Vanara.PInvoke.User32;

namespace PerMonitorAwareWPFApplication;

internal static class PerMonitorDPIHelper
{
	//Returns the DPI of the window handle passed in the parameter
	public static double GetDpiForWindow(HWND hWnd)
	{
		HMONITOR monitor = MonitorFromWindow(hWnd, MonitorFlags.MONITOR_DEFAULTTONEAREST);
		if (GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var newDpiX, out _).Failed)
			newDpiX = 96;
		return newDpiX;
	}

	public static PROCESS_DPI_AWARENESS GetPerMonitorDPIAware()
	{
		GetProcessDpiAwareness(default, out PROCESS_DPI_AWARENESS awareness).ThrowIfFailed();
		return awareness;
	}

	//Returns the system DPI
	public static double GetSystemDPI()
	{
		using SafeHDC hDC = GetDC(default);
		return GetDeviceCaps(hDC, DeviceCap.LOGPIXELSX);
	}

	//Sets the current process as Per_Monitor_DPI_Aware. Returns True if the process was marked as Per_Monitor_DPI_Aware
	public static bool SetPerMonitorDPIAware() => SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE).Succeeded;
}