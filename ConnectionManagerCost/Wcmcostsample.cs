using System;
using static ConnectionManagerCost.Utils;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WcmApi;

namespace ConnectionManagerCost
{
	static class Wcmcostsample
	{
		//********************************************************************************************
		// Function: WCMGetCost
		//
		// Description: Get cost based on interface Guid and profile name
		//
		//********************************************************************************************
		static void WCMGetCost()
		{
			//Get interface Guid and profile name
			var hr = GetInterfaceAndProfileName(out var interfaceGUID, out var profileName);
			if (hr.Succeeded)
			{
				//Get Cost using WcmQueryProperty
				var dwRetCode = WcmQueryProperty(interfaceGUID, profileName, WCM_PROPERTY.wcm_intf_property_connection_cost, default, out _, out var pData);
				if (dwRetCode.Succeeded)
				{
					if (pData.IsInvalid)
					{
						Console.Write("Cost data not available\n");
					}
					else
					{
						var data = pData.ToStructure<WCM_CONNECTION_COST_DATA>();
						Console.Write("Cost is: {0}\n", (data.ConnectionCost));

						//Display meaningful cost description
						DisplayCostDescription(data.ConnectionCost);
						DisplayCostSource(data.CostSource);
						pData.Dispose();
					}
				}
				else
				{
					Console.Write("WcmQueryProperty failed\n");
					//Error handling
					DisplayError(dwRetCode);
				}
			}
		}

		//********************************************************************************************
		// Function: WCMGetProfileData
		//
		// Description: Get Profile data based on interface Guid and profile name
		//
		//********************************************************************************************
		static void WCMGetProfileData()
		{
			//Get interface Guid and profile name
			var hr = GetInterfaceAndProfileName(out var interfaceGUID, out var profileName);
			if (hr.Succeeded)
			{
				//Get Cost using WcmQueryProperty
				var dwRetCode = WcmQueryProperty(interfaceGUID, profileName, WCM_PROPERTY.wcm_intf_property_dataplan_status, default, out _, out var pData);
				if (dwRetCode.Succeeded && !pData.IsInvalid)
				{
					var data = pData.ToStructure<WCM_DATAPLAN_STATUS>();
					DisplayProfileData(data);
					pData.Dispose();
				}
				else
				{
					Console.Write("WcmQueryProperty failed.\n");
					//Error handling
					DisplayError(dwRetCode);
				}
			}
		}

		//********************************************************************************************
		// Function: WCMSetCost
		//
		// Description: Set cost based on interface Guid and profile name
		//
		//********************************************************************************************
		static void WCMSetCost()
		{
			//Get interface Guid and profile name
			var hr = GetInterfaceAndProfileName(out var interfaceGUID, out var profileName);
			if (hr.Succeeded)
			{
				var dwNewCost = ReadIntegerFromConsole("Enter the new cost: ", 0, 0x40000, null);

				var wcmCostData = new WCM_CONNECTION_COST_DATA { ConnectionCost = (WCM_CONNECTION_COST)dwNewCost };

				//Set cost using WcmSetProperty 
				var dwRetCode = WcmSetProperty(interfaceGUID, profileName, WCM_PROPERTY.wcm_intf_property_connection_cost, wcmCostData);
				if (dwRetCode.Succeeded)
				{
					Console.Write("Cost set successfully\n");
				}
				else
				{
					Console.Write("WcmSetProperty failed \n");
					//Error handling
					DisplayError(dwRetCode);
				}
			}
		}

		//********************************************************************************************
		// Function: WCMSetProfileData
		//
		// Description: Set Profile data based on interface Guid and profile name
		//
		//********************************************************************************************
		static void WCMSetProfileData()
		{
			var hr = GetInterfaceAndProfileName(out var interfaceGUID, out var profileName);
			if (hr.Succeeded)
			{
				//initialize Profile data values
				var wcmProfData = new WCM_DATAPLAN_STATUS
				{
					UsageData = new WCM_USAGE_DATA { UsageInMegabytes = WCM_UNKNOWN_DATAPLAN_STATUS },
					DataLimitInMegabytes = WCM_UNKNOWN_DATAPLAN_STATUS,
					InboundBandwidthInKbps = WCM_UNKNOWN_DATAPLAN_STATUS,
					OutboundBandwidthInKbps = WCM_UNKNOWN_DATAPLAN_STATUS,
					MaxTransferSizeInMegabytes = WCM_UNKNOWN_DATAPLAN_STATUS
				};

				//Set Profile data usage
				wcmProfData.UsageData.UsageInMegabytes = (uint)ReadIntegerFromConsole("Enter Profile Usage value in Megabytes: ");
				GetSystemTime(out var currentTime);
				SystemTimeToFileTime(currentTime, out wcmProfData.UsageData.LastSyncTime);

				//Set Profile cap value
				wcmProfData.DataLimitInMegabytes = (uint)ReadIntegerFromConsole("Enter Profile Data Limit value n Megabytes: ");

				//Set Profile speed value
				wcmProfData.InboundBandwidthInKbps = (uint)ReadIntegerFromConsole("Enter Profile Inbound Bandwidth value in Kbps: ");

				wcmProfData.OutboundBandwidthInKbps = (uint)ReadIntegerFromConsole("Enter Profile Outbound Bandwidth value in Kbps: ");

				//Set Profile speed value
				wcmProfData.MaxTransferSizeInMegabytes = (uint)ReadIntegerFromConsole("Enter Profile Max Transfer Size in Megabytes: ");

				//Set Profile Data using WcmSetProperty 
				var dwRetCode = WcmSetProperty(interfaceGUID, profileName, WCM_PROPERTY.wcm_intf_property_dataplan_status, wcmProfData);

				if (dwRetCode.Succeeded)
				{
					Console.Write("Profile Data set successfully\n");
				}
				else
				{
					Console.Write("WcmSetProperty failed\n");
					//Error handling
					DisplayError(dwRetCode);
				}
			}
		}

		//********************************************************************************************
		// Function: EvaluateUserChoice
		//
		// Description: Evaulates the user choice and calls the appropriate function.
		//
		//********************************************************************************************
		static void EvaluateUserChoice(int userchoice)
		{
			switch (userchoice)
			{
				case 1:
					WCMGetCost();
					break;
				case 2:
					WCMGetProfileData();
					break;
				case 3:
					WCMSetCost();
					break;
				case 4:
					WCMSetProfileData();
					break;
				case 5:
					break;
			}
		}

		//********************************************************************************************
		// Function: GetUserChoice
		//
		// Description: Presents an interactive menu to the user and calls the function EvaluateUserChoice to implement user's choice
		//
		// ********************************************************************************************
		static int GetUserChoice()
		{
			string[] choices = {
				"Get Cost",
				"Get Profile Data ",
				"Set Cost",
				"Set Profile Data",
				"Exit" };
			int numchoices = choices.Length;
			Console.Write("---------------------------------------------------------\n");

			for (int i = 0; i < numchoices; i++)
			{
				Console.Write(" {0}. {1}\n", i + 1, choices[i]);
			}

			Console.Write("---------------------------------------------------------\n");
			var chr = ReadIntegerFromConsole($"Enter a choice (1-{numchoices}): ", 1, numchoices, "Invalid Choice. Please enter a valid choice number");
			EvaluateUserChoice(chr);
			return chr;
		}

		const int CHOICE_EXIT = 5;

		//********************************************************************************************
		// Function: Main
		//
		// Description: The main function, calls GetUserChoice, to enable the user to play with the Sample SDK
		//
		//********************************************************************************************
		static void Main()
		{
			//Get user choice option to play with the WCM sample SDK
			while (GetUserChoice() != CHOICE_EXIT)
			{
			}
			Console.Write("WCM Sample SDK exited\n");
		}
	}
}