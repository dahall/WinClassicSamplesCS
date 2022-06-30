using System;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Dhcp;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Kernel32;

namespace dhcpnotify;

internal static class Program
{
	private static SafeEventHandle g_hExitEvent;

	/*
	* main
	*
	* this is where it all happens
	*/
	public static int Main(string[] args)
	{
		g_hExitEvent = Win32Error.ThrowLastErrorIfInvalid(CreateEvent(default, false, false, "DHCP_EXIT_EVENT"));

		using (g_hExitEvent)
		{
			Win32Error.ThrowLastErrorIfFalse(SetConsoleCtrlHandler(CtrlHandler, true));

			var wszAdapter = args.Length > 0 ? args[0] : DetermineAdapter(); // the adapter name in wide chars

			// initialize the DHCP Client Options API
			DhcpCApiInitialize(out var dwVersion).ThrowIfFailed();
			try
			{
				Console.Write("DHCP Client Options API version {0}\n", dwVersion);

				// Here the watch is set up - since this is an easy example, the request is set up statically, however in a real-world
				// scenario this may require building the watch array in a more 'dynamic' way
				//
				// Also for this sample we are using one item that is almost always in a DHCP configuration.
				//
				// the DHCP Client Options API array for watching the options
				SafeNativeArray<DHCPAPI_PARAMS> watch = new(new[] { new DHCPAPI_PARAMS { OptionId = DHCP_OPTION_ID.OPTION_ROUTER_ADDRESS } }); // gateway address

				// set-up the actual array
				DHCPCAPI_PARAMS_ARRAY watcharray = new() { nParams = (uint)watch.Count, Params = watch }; // we are watching 1 item

				Console.Write("Watching DHCP Options Change on Adapter [{0}]\n", wszAdapter);

				// make the request on the adapter
				DhcpRegisterParamChange(DHCPCAPI_REGISTER_HANDLE_EVENT, default, wszAdapter, default, watcharray, out HEVENT hEvent).ThrowIfFailed();

				// wait for the events to become signaled
				HEVENT[] lpHandles = { g_hExitEvent, hEvent };
				WAIT_STATUS rc;

				while ((rc = WaitForMultipleObjects(lpHandles, false, INFINITE)) != 0)
				{
					if (rc == WAIT_STATUS.WAIT_OBJECT_0)
						break;

					ResetEvent((IntPtr)hEvent);
					Console.Write("Parameter has changed.\n");

					// the change could then be read and applied as needed see the sample DHCPREQUEST or the DhcpRequestParams() API for more information
				}

				DhcpDeRegisterParamChange(DHCPCAPI_REGISTER_HANDLE_EVENT, default, hEvent).ThrowIfFailed();
			}
			finally
			{
				// de-init the api
				DhcpCApiCleanup();
			}
		}

		return 0;
	}

	/*
	* CtrlHandler
	*
	* handle control events to provide graceful cleanup
	*/
	private static bool CtrlHandler(CTRL_EVENT dwCtrlType)
	{
		Console.Write("\n\nStop Event Received... Aborting... \n\n");

		switch (dwCtrlType)
		{
			case CTRL_EVENT.CTRL_C_EVENT:
			case CTRL_EVENT.CTRL_BREAK_EVENT:
			case CTRL_EVENT.CTRL_SHUTDOWN_EVENT:
			case CTRL_EVENT.CTRL_LOGOFF_EVENT:
			case CTRL_EVENT.CTRL_CLOSE_EVENT:
				SetEvent(g_hExitEvent);
				break;

			default:
				return false;
		}

		return true;
	}

	/*
	* DetermineAdapter
	*
	* NOTE:
	*
	* This code retrieves the Adapter Name to use for the DHCP Client API
	* using the IPHelper API.
	*
	* NT has a name for the adapter that through this API has device
	* information in front of it followed by a {Guid}, 98 does not and
	* the Index is used instead. So if the string is set to ?? (what it is
	* in 98) we revert to using the string representation of the index.
	*
	*/
	private static string DetermineAdapter()
	{
		IP_INTERFACE_INFO pInfo = GetInterfaceInfo();

		// convert, parse, and convert back
		var szAdapter = pInfo.Adapter[0].Name;
		if (szAdapter[0] == '?')
			// use index if the pointer is not set
			szAdapter = pInfo.Adapter[0].Index.ToString();
		var idx = szAdapter.IndexOf('{');
		if (idx >= 0)
			szAdapter = szAdapter.Remove(0, idx);

		return szAdapter;
	}
}