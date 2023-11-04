using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace AppVisibility
{
	internal class AppVisibilitySample
	{
		private static bool DisplayMonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, PRECT lprcMonitor, IntPtr dwData)
		{
			try
			{
				var pAppVisibility = Marshal.GetObjectForIUnknown(dwData) as IAppVisibility;
				var monitorAppVisibility = pAppVisibility.GetAppVisibilityOnMonitor(hMonitor);
				Console.Write("\tMonitor {0} has {1}\r\n", hMonitor, GetMonitorAppVisibilityString(monitorAppVisibility));
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Simple helper function to turn a MONITOR_APP_VISIBILITY enumeration into a string
		private static string GetMonitorAppVisibilityString(MONITOR_APP_VISIBILITY monitorAppVisibility) => monitorAppVisibility switch
		{
			MONITOR_APP_VISIBILITY.MAV_NO_APP_VISIBLE => "no apps visible",
			MONITOR_APP_VISIBILITY.MAV_APP_VISIBLE => "a visible app",
			_ => "unknown",
		};

		private static void Main()
		{
			Console.Write("Toggle Start menu visibility 5 times to exit\r\n");

			// Create the App Visibility component
			using var spAppVisibility = ComReleaserFactory.Create(new IAppVisibility());

			// Enumerate the current display devices and display app visibility status
			Console.Write("Current app visibility status is:\r\n");
			EnumDisplayMonitors(default, default, DisplayMonitorEnumProc, Marshal.GetIUnknownForObject(spAppVisibility.Item));
			Console.Write("\r\n\r\n");

			// Display the current launcher visibility
			Console.Write("The Start menu is currently {0}\r\n", spAppVisibility.Item.IsLauncherVisible() ? "visible" : "not visible");

			// Create an object that implements IAppVisibilityEvents that will receive callbacks when either app visibility or Start menu
			// visibility changes.
			var spSubscriber = new CAppVisibilityNotificationSubscriber();

			// Advise to receive change notifications from the AppVisibility object
			// NOTE: There must be a reference held on the AppVisibility object in order to continue
			// NOTE: receiving notifications on the implementation of the IAppVisibilityEvents object
			spAppVisibility.Item.Advise(spSubscriber, out var dwCookie);

			// Since the visibility notifications are delivered via COM, a message loop must be employed in order to receive notifications
			MSG msg;
			while (GetMessage(out msg) != 0)
			{
				DispatchMessage(msg);
			}

			// Unadvise from the AppVisibility component to stop receiving notifications
			spAppVisibility.Item.Unadvise(dwCookie);
		}

		// This class will implement the IAppVisibilityEvents interface and will receive notifications from the AppVisibility COM object.
		[ComVisible(true)]
		public class CAppVisibilityNotificationSubscriber : IAppVisibilityEvents
		{
			// This variable will be used to trigger this program's message loop to exit. The variable will be incremented when the launcher
			// becomes visible. When the launcher becomes visible five times, the program will exit.
			private uint cLauncherChanges = 0;

			// AppVisibilityOnMonitorChanged will be called when applications appear or disappear on a monitor
			HRESULT IAppVisibilityEvents.AppVisibilityOnMonitorChanged(HMONITOR hMonitor, MONITOR_APP_VISIBILITY previousMode, MONITOR_APP_VISIBILITY currentMode)
			{
				Console.Write("Monitor {0} previously had {1} and now has {2}\r\n", hMonitor, GetMonitorAppVisibilityString(previousMode), GetMonitorAppVisibilityString(currentMode));
				return HRESULT.S_OK;
			}

			// LauncherVisibilityChange will be called whenever the Start menu becomes visible or hidden
			HRESULT IAppVisibilityEvents.LauncherVisibilityChange(bool currentVisibleState)
			{
				Console.Write("The Start menu is now {0}\r\n", currentVisibleState ? "visible" : "not visible");
				if (currentVisibleState && ++cLauncherChanges >= 5)
				{
					System.Environment.Exit(0); // PostQuitMessage(0);
				}
				return HRESULT.S_OK;
			}
		}
	}
}