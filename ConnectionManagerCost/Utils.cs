using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WcmApi;
using static Vanara.PInvoke.WlanApi;

namespace ConnectionManagerCost
{
	internal static class Utils
	{
		//********************************************************************************************
		// Function: FlushCurrentLine
		//
		// Description: Clears any input lingering in the STDIN buffer
		//
		//********************************************************************************************
		public static void FlushCurrentLine()
		{
			while (Console.KeyAvailable)
				Console.ReadKey();
		}

		//********************************************************************************************
		// Function: GetInterfaceAndProfileName
		//
		// Description: Display the list of interfaces, and associated profile names, and get the user interested interface
		// Guid and the profile name
		//
		//********************************************************************************************
		public static HRESULT GetInterfaceAndProfileName(out Guid intfGuid, out string profName)
		{
			Win32Error dwRet = default;

			//variables used for WlanEnumInterfaces
			WLAN_INTERFACE_INFO_LIST pIfList = default;

			intfGuid = default;
			profName = default;

			//open handle to WLAN
			using var hWlan = WlanOpenHandle();
			if (!hWlan.IsInvalid)
			{
				//Get the list of interfaces for WLAN
				dwRet = WlanEnumInterfaces(hWlan, default, out pIfList);
			}
			else
			{
				Console.Write("Failed to open handle to WLAN. \n");
			}
			if (dwRet.Succeeded)
			{
				WLAN_PROFILE_INFO_LIST pProfileList = default;

				for (int i = 0; i < (int)pIfList.dwNumberOfItems; i++)
				{
					var pIfInfo = pIfList.InterfaceInfo[i];
					Console.Write(" Interface Guid [{0}]. {1}\n", i + 1, pIfInfo.InterfaceGuid);

					//Get list of profiles associated with this interface
					dwRet = WlanGetProfileList(hWlan, pIfInfo.InterfaceGuid, default, out pProfileList);

					if (dwRet.Failed)
					{
						Console.Write("WlanGetProfileList failed. \n");
						break;
					}
					for (int j = 0; j < (int)pProfileList.dwNumberOfItems; j++)
					{
						var pProfile = pProfileList.ProfileInfo[j];
						Console.Write(" Profile Name [{0}]: {1}\n", j + 1, pProfile.strProfileName);
					}
				}

				if (dwRet.Succeeded)
				{
					if ((pIfList.dwNumberOfItems == 0) || (pProfileList.dwNumberOfItems == 0))
					{
						dwRet = Win32Error.ERROR_NOT_FOUND;
						Console.Write("WLAN interface/profile not found! \n");
					}
					else
					{
						//User input for interested interface Guid and Profile name
						GetInterfaceIdAndProfileIndex(pIfList.dwNumberOfItems, pProfileList.dwNumberOfItems, out var iIntf, out var iProfile);
						//Get the interested Interface Guid and profile Name from the indices
						intfGuid = pIfList.InterfaceInfo[iIntf - 1].InterfaceGuid;
						profName = pProfileList.ProfileInfo[iProfile - 1].strProfileName;
					}
				}
			}
			else
			{
				Console.Write("WlanEnumInterfaces failed. \n");
			}

			if (dwRet.Failed)
			{
				DisplayError(dwRet);
			}
			return (HRESULT)dwRet;
		}

		//********************************************************************************************
		// Function: GetInterfaceIdAndProfileIndex
		//
		// Description: Choose indices for Interface Guids and Profile names displayed
		//
		//********************************************************************************************
		public static void GetInterfaceIdAndProfileIndex(uint numIntfItems, uint numProfNames, out int pIntf, out int pProfile)
		{
			//Get the interested interface Guid and profile name Indices
			Console.Write("The list of interfaces and profiles for each interface are listed as above.\n");
			pIntf = ReadIntegerFromConsole($"Choose an index for Interface Guid in the range [ 1 - {numIntfItems} ]: ", 1, (int)numIntfItems, null);
			pProfile = ReadIntegerFromConsole($"Choose an index for Profile Name in the range [ 1 - {numProfNames} ] : ", 1, (int)numProfNames, null);
		}

		//********************************************************************************************
		// Function: DisplayCost
		//
		// Description: Displays meaningful cost values to the user
		//
		//********************************************************************************************
		public static void DisplayCostDescription(WCM_CONNECTION_COST cost)
		{
			if (cost == WCM_CONNECTION_COST.WCM_CONNECTION_COST_UNKNOWN)
			{
				Console.Write("Cost : Unknown\n");
			}
			else if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_UNRESTRICTED))
			{
				Console.Write("Cost : Unrestricted\n");
			}
			else if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_FIXED))
			{
				Console.Write("Cost : Fixed\n");
			}
			else if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_VARIABLE))
			{
				Console.Write("Cost : Variable\n");
			}
			if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_OVERDATALIMIT))
			{
				Console.Write("OverDataLimit : Yes\n");
			}
			else
			{
				Console.Write("OverDataLimit : No\n");
			}

			if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_APPROACHINGDATALIMIT))
			{
				Console.Write("Approaching DataLimit : Yes\n");
			}
			else
			{
				Console.Write("Approaching DataLimit : No\n");
			}

			if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_CONGESTED))
			{
				Console.Write("Congested : Yes\n");
			}
			else
			{
				Console.Write("Congested : No\n");
			}

			if (cost.IsFlagSet(WCM_CONNECTION_COST.WCM_CONNECTION_COST_ROAMING))
			{
				Console.Write("Roaming : Yes\n");
			}
			else
			{
				Console.Write("Roaming : No\n");
			}
		}

		//********************************************************************************************
		// Function: DisplayCostSource
		//
		// Description: Displays cost source to the user
		//
		//********************************************************************************************
		public static void DisplayCostSource(WCM_CONNECTION_COST_SOURCE costSource)
		{
			if (costSource == WCM_CONNECTION_COST_SOURCE.WCM_CONNECTION_COST_SOURCE_GP)
			{
				Console.Write("Cost Source is Group Policy\n");
			}
			else if (costSource == WCM_CONNECTION_COST_SOURCE.WCM_CONNECTION_COST_SOURCE_USER)
			{
				Console.Write("Cost Source is User\n");
			}
			else if (costSource == WCM_CONNECTION_COST_SOURCE.WCM_CONNECTION_COST_SOURCE_OPERATOR)
			{
				Console.Write("Cost Source is Operator\n");
			}
			else if (costSource == WCM_CONNECTION_COST_SOURCE.WCM_CONNECTION_COST_SOURCE_DEFAULT)
			{
				Console.Write("Cost Source is Default\n");
			}
		}

		//********************************************************************************************
		// Function: DisplayProfileData
		//
		// Description: Displays profile data values to the user
		//
		//********************************************************************************************
		public static void DisplayProfileData(in WCM_DATAPLAN_STATUS pProfileData)
		{
			if (false == IsProfileDataAvailable(pProfileData))
			{
				Console.Write("Profile Data Unknown\n");
			}
			else
			{
				//check for default or unknown value
				if (pProfileData.UsageData.UsageInMegabytes != WCM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Data Usage in MB : {0}\n", pProfileData.UsageData.UsageInMegabytes);
				}

				if ((pProfileData.UsageData.LastSyncTime.dwHighDateTime != 0) || (pProfileData.UsageData.LastSyncTime.dwLowDateTime != 0))
				{
					Console.Write("Last Sync Time : ");
					PrintFileTime(pProfileData.UsageData.LastSyncTime);
				}
				if (pProfileData.DataLimitInMegabytes != WCM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Data Limit value in MB : {0}\n", pProfileData.DataLimitInMegabytes);
				}
				if (pProfileData.InboundBandwidthInKbps != WCM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Inbound Bandwidth value in Kbps : {0}\n", pProfileData.InboundBandwidthInKbps);
				}
				if (pProfileData.OutboundBandwidthInKbps != WCM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Outbound Bandwidth value in Kbps : {0}\n", pProfileData.OutboundBandwidthInKbps);
				}
				if ((pProfileData.BillingCycle.StartDate.dwHighDateTime != 0) || (pProfileData.BillingCycle.StartDate.dwLowDateTime != 0))
				{
					Console.Write("Billing Cycle Start Date : ");
					PrintFileTime(pProfileData.BillingCycle.StartDate);
					if (IsProfilePlanDurationAvailable(pProfileData.BillingCycle.Duration))
					{
						Console.Write("Billing Cycle Duration : \n");
						DisplayProfilePlanDuration(pProfileData.BillingCycle.Duration);
					}

				}

				if (pProfileData.MaxTransferSizeInMegabytes != WCM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Maximum Transfer Size in MB : {0}\n", pProfileData.MaxTransferSizeInMegabytes);
				}
			}

		}

		//********************************************************************************************
		// Function: IsProfilePlanDurationAvailable
		//
		// Description: Return true if Profile Plan Duration is available
		//
		//********************************************************************************************
		public static bool IsProfilePlanDurationAvailable(WCM_TIME_INTERVAL Duration)
		{
			return (Duration.wYear > 0 ||
			(Duration.wMonth > 0) ||
			(Duration.wDay > 0) ||
			(Duration.wHour > 0) ||
			(Duration.wMinute > 0) ||
			(Duration.wSecond > 0) ||
			(Duration.wMilliseconds > 0));
		}

		//********************************************************************************************
		// Function: DisplayProfilePlanDuration
		//
		// Description: Display the Profile Plan Duration 
		//
		//********************************************************************************************
		public static void DisplayProfilePlanDuration(WCM_TIME_INTERVAL Duration)
		{
			if (Duration.wYear > 0)
			{
				Console.Write("Years : %d\n", Duration.wYear);
			}

			if (Duration.wMonth > 0)
			{
				Console.Write("Months : %d\n", Duration.wMonth);
			}

			if (Duration.wDay > 0)
			{
				Console.Write("Days : %d\n", Duration.wDay);
			}

			if (Duration.wHour > 0)
			{
				Console.Write("Hours : %d\n", Duration.wHour);
			}

			if (Duration.wMinute > 0)
			{
				Console.Write("Minutes : %d\n", Duration.wMinute);
			}

			if (Duration.wSecond > 0)
			{
				Console.Write("Seconds : %d\n", Duration.wSecond);
			}

			if (Duration.wMilliseconds > 0)
			{
				Console.Write("Milliseconds : %d\n", Duration.wMilliseconds);
			}

		}

		//********************************************************************************************
		// Function: PrintFileTime
		//
		// Description: Converts file time to local time, to display to the user
		//
		//********************************************************************************************
		public static void PrintFileTime(in FILETIME time)
		{
			// Convert filetime to local time.
			FileTimeToSystemTime(time, out var stUTC);
			SystemTimeToTzSpecificLocalTime(default, in stUTC, out var stLocal);
			Console.Write("{0}/{1}/{2} {3}:{4}:{5}\n",
			stLocal.wMonth, stLocal.wDay, stLocal.wYear, stLocal.wHour, stLocal.wMinute, stLocal.wSecond);
		}

		//********************************************************************************************
		// Function: IsProfileDataAvailable
		//
		// Description: Checks if the profile data values are default values, or provided by the MNO
		//
		//********************************************************************************************
		public static bool IsProfileDataAvailable(in WCM_DATAPLAN_STATUS pProfileData)
		{
			bool isDefined = false;
			//
			// usage data is valid only if both planUsage and lastUpdatedTime are valid
			//
			if (pProfileData.UsageData.UsageInMegabytes != WCM_UNKNOWN_DATAPLAN_STATUS
			&& (pProfileData.UsageData.LastSyncTime.dwHighDateTime != 0
			|| pProfileData.UsageData.LastSyncTime.dwLowDateTime != 0))
			{
				isDefined = true;
			}
			else if (pProfileData.DataLimitInMegabytes != WCM_UNKNOWN_DATAPLAN_STATUS)
			{
				isDefined = true;
			}
			else if (pProfileData.InboundBandwidthInKbps != WCM_UNKNOWN_DATAPLAN_STATUS)
			{
				isDefined = true;
			}
			else if (pProfileData.OutboundBandwidthInKbps != WCM_UNKNOWN_DATAPLAN_STATUS)
			{
				isDefined = true;
			}
			else if (pProfileData.BillingCycle.StartDate.dwHighDateTime != 0
			|| pProfileData.BillingCycle.StartDate.dwLowDateTime != 0)
			{
				isDefined = true;

			}
			else if (pProfileData.MaxTransferSizeInMegabytes != WCM_UNKNOWN_DATAPLAN_STATUS)
			{
				isDefined = true;
			}
			return isDefined;
		}

		//********************************************************************************************
		// Function: DisplayError
		//
		// Description: Displays error description
		//
		//********************************************************************************************
		public static void DisplayError(Win32Error dwError)
		{
			Console.Write("{0}\n", dwError.GetException().Message);
		}

		public static int ReadIntegerFromConsole(string prompt, int min = 0, int max = int.MaxValue, string outOfRangeMsg = null)
		{
			int i;
			if (prompt != null) Console.Write(prompt);
			while (!int.TryParse(Console.ReadLine(), out i) || i < min || i > max)
				if (outOfRangeMsg != null) Console.WriteLine(outOfRangeMsg);
			return i;
		}
	}
}