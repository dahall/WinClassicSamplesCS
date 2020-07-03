using System;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace DesktopAutomationDismiss
{
	internal static class Program
	{
		public const int MAX_RETRIES = 5;
		public const int VK_D = 0x44;

		// Static variables
		private static bool g_fOutputToFile = false;
		private static SafeHFILE g_hFile;

		//***********************************************************************
		//
		// Checks UIAccess
		//
		//***********************************************************************
		private static bool HasUIAccess()
		{
			var fUIAccess = false;

			try
			{
				// Get process token in order to check for UIAccess
				using var hToken = SafeHTOKEN.FromProcess(GetCurrentProcess(), TokenAccess.TOKEN_QUERY);
				// Verify that the process has UIAccess
				fUIAccess = hToken.GetInfo<uint>(TOKEN_INFORMATION_CLASS.TokenUIAccess) > 0;
			}
			catch (Exception ex)
			{
				OutputString("GetTokenInformation error: {0}\n", ex.Message);
			}

			return fUIAccess;
		}

		//**********************************************************************
		//
		// Checks to see if any apps or the Start menu is visible
		//
		//**********************************************************************
		private static bool IsAppVisible()
		{
			OutputString("Checking for apps\r\n");
			var fAppVisible = false;

			try
			{
				using var pAppVisible = ComReleaserFactory.Create(new IAppVisibility());
				fAppVisible = pAppVisible.Item.IsLauncherVisible();
				OutputString("\tIsLauncherVisible:\t{0}\r\n", fAppVisible);

				if (!fAppVisible)
				{
					using var data = new SafeCoTaskMemHandle(IntPtr.Size + 4);
					data.Write(Marshal.GetIUnknownForObject(pAppVisible.Item));
					if (!EnumDisplayMonitors(default, default, MonitorEnumProc, data))
					{
						OutputString("EnumDisplayMonitors failed.\r\n");
					}
					else
					{
						fAppVisible = data.ToStructure<uint>(IntPtr.Size) != 0;
					}
				}
			}
			catch (Exception ex)
			{
				OutputString("IsAppVisible failed: {0}\r\n", ex.Message);
			}

			OutputString("\tApps:\t\t{0}\r\n", fAppVisible ? "FOUND" : "NOT FOUND");
			return fAppVisible;
		}

		//***********************************************************************
		//
		// ShowDesktop entry point
		//
		//***********************************************************************
		private static int Main(string[] args)
		{
			// Uncomment for debugging: System.Diagnostics.Debugger.Launch();
			Win32Error nResult = 0;

			// Check input args for flags
			var fOverwrite = false;
			var fIgnoreUIAccess = false;
			string pszfileName = default;
			for (var i = 0; i < args.Length; i++)
			{
				// Flag to give help
				if (args[i] == "-?")
				{
					Console.Write("Command line options:\n" +
					"-o <file path>: Writes output to a the specified log file instead of the console\n" +
					"-f: Forces the file specified in -o flag to be overwritten if the file exists\n" +
					"-i: Ignores the return value of the check for UIAccess\n");
				}

				// Flag for force overwrite
				if (string.Compare(args[i], "-f", StringComparison.OrdinalIgnoreCase) == 0)
				{
					fOverwrite = true;
				}

				// Flag for output file path
				if (string.Compare(args[i - 1], "-o", StringComparison.OrdinalIgnoreCase) == 0)
				{
					pszfileName = args[i];
					g_fOutputToFile = true;
				}

				// Flag to ignore the check for UIAccess
				if (string.Compare(args[i], "-i", StringComparison.OrdinalIgnoreCase) == 0)
				{
					fIgnoreUIAccess = true;
				}
			}

			// Open the specified file for write
			if (g_fOutputToFile && pszfileName != default)
			{
				var dwCreationDisposition = fOverwrite ? System.IO.FileMode.Create : System.IO.FileMode.CreateNew;
				g_hFile = CreateFile(pszfileName, FileAccess.GENERIC_WRITE, System.IO.FileShare.Read, default, dwCreationDisposition, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);
				if (g_hFile.IsInvalid)
				{
					nResult = GetLastError();
					Console.Write("Cannot write to the specified file, error: {0}\r\n", nResult);
				}
			}

			if (nResult.Succeeded)
			{
				if (fIgnoreUIAccess || HasUIAccess())
				{
					if (fIgnoreUIAccess)
					{
						OutputString("Ignoring the check for UIAccess.\r\n");
					}
					else
					{
						OutputString("UI automation access is allowed.\r\n");
					}

					if (IsAppVisible())
					{
						ShowDesktop();

						// Wait for the dismiss animation to complete and for the desktop to be visible.
						Sleep(1000);
						if (WaitForDesktop())
						{
							nResult = Win32Error.ERROR_SUCCESS;
						}
						else
						{
							nResult = Win32Error.ERROR_CAN_NOT_COMPLETE;
						}
					}
					else
					{
						// System is already in Desktop--no need to send Win + D
						nResult = Win32Error.ERROR_SUCCESS;
					}
				}
				else
				{
					OutputString("UI automation access is NOT allowed.\r\n");
					nResult = Win32Error.ERROR_ACCESS_DENIED;
				}

				g_hFile?.Dispose();
			}

			return (int)nResult.ToHRESULT();
		}

		//**********************************************************************
		//
		// Callback for EnumWindows in IsAppVisible
		//
		//**********************************************************************
		private static bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, PRECT pRect, IntPtr dwData)
		{
			try
			{
				var monitorAppVisibility = (Marshal.GetObjectForIUnknown(dwData.ToStructure<IntPtr>()) as IAppVisibility)?.GetAppVisibilityOnMonitor(hMonitor) ?? 0;
				OutputString("\tMonitor app visibility:\t\t");
				switch (monitorAppVisibility)
				{
					case MONITOR_APP_VISIBILITY.MAV_UNKNOWN:
						OutputString("UNKNOWN\r\n");
						break;

					case MONITOR_APP_VISIBILITY.MAV_NO_APP_VISIBLE:
						OutputString("NO APP VISIBLE\r\n");
						break;

					case MONITOR_APP_VISIBILITY.MAV_APP_VISIBLE:
						OutputString("APP VISIBLE\r\n");
						dwData.Write(1U, IntPtr.Size);
						break;

					default:
						OutputString("UNDEFINED\r\n");
						break;
				}
				return true;
			}
			catch { return false; }
		}

		//***********************************************************************
		//
		// Outputs a string, either to a log file or to the console
		//
		//***********************************************************************
		private static bool OutputString(string pszFormatString, params object[] other)
		{
			var fRetValue = true;

			// Make the string for the variadic function
			var szOutputBuffer = string.Format(pszFormatString, other);
			if (!string.IsNullOrEmpty(szOutputBuffer))
			{
				// Output
				if (g_fOutputToFile)
				{
					var szMultibyteOut = Encoding.UTF8.GetBytes(szOutputBuffer);
					if (szMultibyteOut.Length == 0)
					{
						fRetValue = false;
					}
					else
					{
						fRetValue = WriteFile(g_hFile, szMultibyteOut, (uint)szMultibyteOut.Length, out _);
					}
				}
				else
				{
					Console.Write(szOutputBuffer);
				}
			}
			else
			{
				fRetValue = false;
			}

			return fRetValue;
		}

		//**********************************************************************
		//
		// Sends Win + D to toggle to the desktop
		//
		//**********************************************************************
		private static void ShowDesktop()
		{
			const ushort VK_LWIN = 91;
			const ushort VK_D = 68;

			OutputString("Sending 'Win-D'\r\n");
			var inputs = new[] {
				new INPUT { type = INPUTTYPE.INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_LWIN } },
				new INPUT { type = INPUTTYPE.INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_D } },
				new INPUT { type = INPUTTYPE.INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_D, dwFlags = KEYEVENTF.KEYEVENTF_KEYUP } },
				new INPUT { type = INPUTTYPE.INPUT_KEYBOARD, ki = new KEYBDINPUT { wVk = VK_LWIN, dwFlags = KEYEVENTF.KEYEVENTF_KEYUP } },
			};

			if (SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT))) != (uint)inputs.Length)
			{
				OutputString("SendInput failed: {0}\n", Win32Error.GetLastError().ToHRESULT());
			}
		}

		//*********************************************************************
		//
		// Waits for apps or Start menu to be dismissed
		// Returns true if apps and Start menu are no longer visible
		//
		//*********************************************************************
		private static bool WaitForDesktop()
		{
			var fAppVisible = IsAppVisible();

			for (var i = 0; i < MAX_RETRIES && fAppVisible; i++)
			{
				Sleep(1000);
				fAppVisible = IsAppVisible();
			}

			return !fAppVisible;
		}

		// Forward declarations and typedefs
		[StructLayout(LayoutKind.Sequential)]
		public struct ENUMDISPLAYDATA
		{
			public bool fAppVisible;
			public IntPtr pAppVisible;
		}
	}
}