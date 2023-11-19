using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.SHCore;
using static Vanara.PInvoke.User32;

namespace PerMonitorAwareWPFApplication;

public class PerMonitorDPIWindow : Window
{
	private readonly bool m_perMonitorEnabled;

	private HwndSource? m_source;

	//Constructor; sets the current process as Per_Monitor_DPI_Aware
	public PerMonitorDPIWindow()
	{
		Loaded += OnLoaded;
		m_perMonitorEnabled = PerMonitorDPIHelper.SetPerMonitorDPIAware() ? true :
			throw new Exception("Enabling Per-monitor DPI Failed. Do you have [assembly: DisableDpiAwareness] in your assembly manifest [AssemblyInfo.cs]?");
	}

	public event EventHandler? DPIChanged;

	public double CurrentDPI { get; private set; }

	public double ScaleFactor { get; private set; }

	public double WpfDPI { get; set; }

	public string GetCurrentDpiConfiguration()
	{
		var systemDpi = PerMonitorDPIHelper.GetSystemDPI();
		return PerMonitorDPIHelper.GetPerMonitorDPIAware() switch
		{
			PROCESS_DPI_AWARENESS.PROCESS_DPI_UNAWARE => $"Application is DPI Unaware. Using {systemDpi} DPI.",
			PROCESS_DPI_AWARENESS.PROCESS_SYSTEM_DPI_AWARE => $"Application is System DPI Aware. Using System DPI:{systemDpi}.",
			PROCESS_DPI_AWARENESS.PROCESS_PER_MONITOR_DPI_AWARE => $"Application is Per-Monitor DPI Aware. Using monitor DPI = {CurrentDPI} \t(System DPI = {systemDpi}).",
			_ => string.Empty,
		};
	}

	//Called when the DPI of the window changes. This method adjusts the graphics and text size based on the new DPI of the window
	protected void OnDPIChanged()
	{
		ScaleFactor = CurrentDPI / WpfDPI;
		UpdateLayoutTransform(ScaleFactor);
		DPIChanged?.Invoke(this, EventArgs.Empty);
	}

	protected void UpdateLayoutTransform(double scaleFactor)
	{
		// Adjust the rendering graphics and text size by applying the scale transform to the top level visual node of the Window
		if (m_perMonitorEnabled)
		{
			Visual child = GetVisualChild(0);
			if (ScaleFactor != 1.0)
			{
				var dpiScale = new ScaleTransform(scaleFactor, scaleFactor);
				child.SetValue(LayoutTransformProperty, dpiScale);
			}
			else
			{
				child.SetValue(LayoutTransformProperty, default);
			}
		}
	}

	// Message handler of the Per_Monitor_DPI_Aware window. The handles the WM_DPICHANGED message and adjusts window size, graphics and text
	// based on the DPI of the monitor. The window message provides the new window size (lparam) and new DPI (wparam)
	private IntPtr HandleMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool _)
	{
		double oldDpi;
		switch ((WindowMessage)msg)
		{
			case WindowMessage.WM_DPICHANGED:
				RECT lprNewRect = lParam.ToStructure<RECT>();
				_=SetWindowPos(hwnd, default, lprNewRect.left, lprNewRect.top, lprNewRect.right - lprNewRect.left, lprNewRect.bottom - lprNewRect.top, SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_NOOWNERZORDER | SetWindowPosFlags.SWP_NOACTIVATE);
				oldDpi = CurrentDPI;
				CurrentDPI = Macros.LOWORD(wParam);
				if (oldDpi != CurrentDPI)
				{
					OnDPIChanged();
				}
				break;
		}
		return IntPtr.Zero;
	}

	//OnLoaded Handler: Adjusts the window size and graphics and text size based on current DPI of the Window
	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		// WPF has already scaled window size, graphics and text based on system DPI. In order to scale the window based on monitor DPI,
		// update the window size, graphics and text based on monitor DPI. For example consider an application with size 600 x 400 in device
		// independent pixels
		// - Size in device independent pixels = 600 x 400
		// - Size calculated by WPF based on system/WPF DPI = 192 (scale factor = 2)
		// - Expected size based on monitor DPI = 144 (scale factor = 1.5)

		// Similarly the graphics and text are updated updated by applying appropriate scale transform to the top level node of the WPF application

		// Important Note: This method overwrites the size of the window and the scale transform of the root node of the WPF Window. Hence,
		// this sample may not work "as is" if
		// - The size of the window impacts other portions of the application like this WPF Window being hosted inside another application.
		// - The WPF application that is extending this class is setting some other transform on the root visual; the sample may overwrite
		// some other transform that is being applied by the WPF application itself.

		if (m_perMonitorEnabled)
		{
			m_source = (HwndSource)PresentationSource.FromVisual(this);
			m_source.AddHook(HandleMessages);

			//Calculate the DPI used by WPF; this is same as the system DPI.

			WpfDPI = 96.0 * m_source.CompositionTarget.TransformToDevice.M11;

			//Get the Current DPI of the monitor of the window.

			CurrentDPI = PerMonitorDPIHelper.GetDpiForWindow(m_source.Handle);

			//Calculate the scale factor used to modify window size, graphics and text
			ScaleFactor = CurrentDPI / WpfDPI;

			//Update Width and Height based on the on the current DPI of the monitor

			Width *= ScaleFactor;
			Height *= ScaleFactor;

			//Update graphics and text based on the current DPI of the monitor

			UpdateLayoutTransform(ScaleFactor);
		}
	}
}