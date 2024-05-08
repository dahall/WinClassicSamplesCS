using System.Runtime.CompilerServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.PInvoke.NetListMgr;
using static Vanara.PInvoke.OleAut32;
using static Vanara.PInvoke.WinHTTP;

namespace NetworkListManager;

internal static class Utility
{
	public enum CostGuidance
	{
		Normal,
		OptIn,
		Conservative
	}

	public static void EvaluateAndReportConnectionCost(NLM_CONNECTION_COST connectionCost)
	{
		var costGuidance = GetNetworkCostGuidance(connectionCost);

		switch (costGuidance)
		{
			case CostGuidance.OptIn:
				// In opt-in scenarios, apps handle cases where the network access cost is significantly higher than the plan cost. For
				// example, when a user is roaming, a mobile carrier may charge a higher rate data usage.
				Console.WriteLine("Apps should implement opt-in behavior.");
				break;

			case CostGuidance.Conservative:
				// In conservative scenarios, apps implement restrictions for optimizing network usage to handle transfers over metered networks.
				Console.WriteLine("Apps should implement conservative behavior.");
				break;

			case CostGuidance.Normal:
			default:
				// In normal scenarios, apps do not implement restrictions. Apps treat the connection as unlimited in cost.
				Console.WriteLine("Apps should implement normal behavior.");
				break;
		}
	}

	public static void EvaluateAndReportConnectivity(bool optedIn, NLM_CONNECTIVITY connectivity, INetworkListManager networkListManager)
	{
		if (ShouldAttemptToConnectToInternet(connectivity, networkListManager))
		{
			EvaluateCostAndConnect(optedIn, networkListManager);
		}
		else
		{
			Console.WriteLine("Not attempting to connect to the Internet.");
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static ulong GetNumericValue<TEnum>(this TEnum enumValue) where TEnum : unmanaged, Enum
	{
		Span<ulong> ulongSpan = stackalloc ulong[] { 0UL };
		Span<TEnum> span = MemoryMarshal.Cast<ulong, TEnum>(ulongSpan);
		span[0] = enumValue;
		return ulongSpan[0];
	}

	internal static bool IsAnyFlagSet<TEnum>(this TEnum value, TEnum flags) where TEnum : unmanaged, Enum =>
		// CheckHasFlags<T>(value);
		(value.GetNumericValue() & flags.GetNumericValue()) != 0UL;

	private static void EvaluateCostAndConnect(bool optedIn, INetworkListManager networkListManager)
	{
		INetworkCostManager netCostManager = (INetworkCostManager)networkListManager;

		netCostManager.GetCost(out var nlmConnectionCost);
		CostGuidance costGuidance = GetNetworkCostGuidance(nlmConnectionCost);

		switch (costGuidance)
		{
			case CostGuidance.OptIn:
				{
					Console.WriteLine("Network access cost is significantly higher.");
					if (optedIn)
					{
						Console.WriteLine("User has opted into network usage while roaming. Connecting.");
						SendHttpGetRequest();
					}
					else
					{
						Console.WriteLine("User has not opted into network usage while roaming. Not connecting.");
					}
					break;
				}
			case CostGuidance.Conservative:
				{
					Console.WriteLine("Attempt connecting to the Internet for critical requests.");
					SendHttpGetRequest();
					break;
				}
			case CostGuidance.Normal:
			default:
				Console.WriteLine("Attempt connecting to the Internet.");
				SendHttpGetRequest();
				break;
		}
	}

	private static CostGuidance GetNetworkCostGuidance(NLM_CONNECTION_COST cost)
	{
		if (cost.IsFlagSet(NLM_CONNECTION_COST.NLM_CONNECTION_COST_ROAMING | NLM_CONNECTION_COST.NLM_CONNECTION_COST_OVERDATALIMIT))
		{
			if (cost.IsFlagSet(NLM_CONNECTION_COST.NLM_CONNECTION_COST_ROAMING))
			{
				Console.WriteLine("Connection is roaming; using the connection may result in additional charge.");
			}
			else
			{
				Console.WriteLine("Connection has exceeded the usage cap limit.");
			}
			return CostGuidance.OptIn;
		}
		else if (cost.IsAnyFlagSet(NLM_CONNECTION_COST.NLM_CONNECTION_COST_FIXED | NLM_CONNECTION_COST.NLM_CONNECTION_COST_VARIABLE))
		{
			if (cost.IsFlagSet(NLM_CONNECTION_COST.NLM_CONNECTION_COST_FIXED))
			{
				Console.WriteLine("Connection has limited allowed usage.");
			}
			else
			{
				Console.WriteLine("Connection is charged based on usage.");
			}
			return CostGuidance.Conservative;
		}
		else
		{
			if (cost.IsFlagSet(NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNRESTRICTED))
			{
				Console.WriteLine("Connection cost is unrestricted.");
			}
			else
			{
				Console.WriteLine("Connection cost is unknown.");
			}
			return CostGuidance.Normal;
		}
	}

	private static void SendHttpGetRequest()
	{
		try
		{
			using var session = WinHttpOpen("NetworkListManagerSample.exe", WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY, WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, 0);
			using var connect = WinHttpConnect(session, "www.msftconnecttest.com", INTERNET_DEFAULT_HTTP_PORT, 0);
			using var request = WinHttpOpenRequest(connect, "GET", "/connecttest.txt", null, null /*WINHTTP_NO_REFERER*/, null /*WINHTTP_DEFAULT_ACCEPT_TYPES*/, 0);
			Win32Error.ThrowLastErrorIfFalse(WinHttpSendRequest(request, WINHTTP_NO_ADDITIONAL_HEADERS, 0, default /*WINHTTP_NO_REQUEST_DATA*/));
			Win32Error.ThrowLastErrorIfFalse(WinHttpReceiveResponse(request));

			uint statusCode = WinHttpQueryHeaders<uint>(request, WINHTTP_QUERY.WINHTTP_QUERY_STATUS_CODE | WINHTTP_QUERY.WINHTTP_QUERY_FLAG_NUMBER);
			if (statusCode is >= 200 and < 300)
			{
				Console.WriteLine($"Http request succeeded with status code {statusCode}");

				if (WinHttpQueryDataAvailable(request, out var bytesRead) && bytesRead > 0)
				{
					SafeByteArray readBuffer = new((int)bytesRead);
					Win32Error.ThrowLastErrorIfFalse(WinHttpReadData(request, readBuffer, bytesRead, out bytesRead));
					Console.WriteLine($"Received {bytesRead} bytes in response.");
					return;
				}
			}
			else
			{
				Console.WriteLine($"Http request completed with status code {statusCode}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"SendHttpGetRequest failed with error:\n{ex.Message}");
		}
	}

	private static bool ShouldAttemptToConnectToInternet(NLM_CONNECTIVITY connectivity, INetworkListManager networkListManager)
	{
		// check internet connectivity
		if (connectivity.IsAnyFlagSet(NLM_CONNECTIVITY.NLM_CONNECTIVITY_IPV4_INTERNET | NLM_CONNECTIVITY.NLM_CONNECTIVITY_IPV6_INTERNET))
		{
			Console.WriteLine("Machine has internet connectivity.");
			return true;
		}
		else if (connectivity.IsAnyFlagSet(NLM_CONNECTIVITY.NLM_CONNECTIVITY_IPV4_LOCALNETWORK | NLM_CONNECTIVITY.NLM_CONNECTIVITY_IPV6_LOCALNETWORK))
		{
			// we are local connected, check if we're behind a captive portal before attempting to connect to the Internet.
			//
			// note: being behind a captive portal means connectivity is local and there is at least one interface(network) behind a captive portal.

			bool localConnectedBehindCaptivePortal = false;
			foreach (INetwork networkConnection in networkListManager.GetNetworks(NLM_ENUM_NETWORK.NLM_ENUM_NETWORK_CONNECTED))
			{
				IPropertyBag networkProperties = (IPropertyBag)networkConnection;

				// these might fail if there's no value
				object variantInternetConnectivityV4 = new();
				networkProperties.Read(NetworkPropertyName.NA_InternetConnectivityV4, ref variantInternetConnectivityV4, null);
				object variantInternetConnectivityV6 = new();
				networkProperties.Read(NetworkPropertyName.NA_InternetConnectivityV6, ref variantInternetConnectivityV6, null);

				// read the VT_UI4 from the VARIANT and cast it to a NLM_INTERNET_CONNECTIVITY If there is no value, then assume no special treatment.
				NLM_INTERNET_CONNECTIVITY v4Connectivity = (NLM_INTERNET_CONNECTIVITY)(variantInternetConnectivityV6 is uint u ? u : 0);
				NLM_INTERNET_CONNECTIVITY v6Connectivity = (NLM_INTERNET_CONNECTIVITY)(variantInternetConnectivityV6 is uint u1 ? u1 : 0);

				if (v4Connectivity.IsFlagSet(NLM_INTERNET_CONNECTIVITY.NLM_INTERNET_CONNECTIVITY_WEBHIJACK) || v6Connectivity.IsFlagSet(NLM_INTERNET_CONNECTIVITY.NLM_INTERNET_CONNECTIVITY_WEBHIJACK))
				{
					// at least one connected interface is behind a captive portal we should assume that the device is behind it
					localConnectedBehindCaptivePortal = true;
				}
			}

			if (!localConnectedBehindCaptivePortal)
			{
				Console.WriteLine("Machine has local connectivity and not behind a captive portal.");
				return true;
			}
			else
			{
				Console.WriteLine("Machine is behind a captive portal.");
			}
		}
		else
		{
			Console.WriteLine("Machine is not connected.");
		}
		return false;
	}
}