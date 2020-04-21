using System;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.PInvoke.NetListMgr;

namespace NetworkCost
{
	static partial class NetCostSample
	{
		const int CHOICE_EXIT = 10;
		const int IP_ADDRESS_SIZE = 256;
		static readonly ushort SOCKET_VERSION_REQUESTED = Macros.MAKEWORD(2,2);

		static CNetCostEventSink g_pSinkCostMgr = default;
		static CNetCostEventSink g_pSinkConnectionCostMgr = default;
		static CNetCostEventSink g_pSinkDestCostMgr = default;

		//********************************************************************************************
		// Function: Main
		//
		// Description: The main function, initializes a multithreaded apartment, and if successful, starts user interaction.
		//
		//********************************************************************************************
		[MTAThread]
		static void Main(string[] args)
		{
			int userChoice = -1;
			while (userChoice != CHOICE_EXIT)
			{
				userChoice = GetUserChoice();
			}
			Console.Write("Net Cost Sample SDK exited\n");
		}

		//********************************************************************************************
		// Function: RegisterForMachineCostChangeNotifications
		//
		// Description: Registers for machine cost change notifications, and waits in the message loop.
		//
		//********************************************************************************************
		static void RegisterForMachineCostChangeNotifications()
		{
			//Registration is allowed only once, before unregister.
			if (g_pSinkCostMgr is null)
			{
				try
				{
					g_pSinkCostMgr = CNetCostEventSink.StartListeningForEvents(typeof(INetworkCostManagerEvents).GUID);
					Console.Write("Listening for Machine cost change events...\n");
				}
				catch (Exception ex)
				{
					Console.Write("Registration failed, please try again. \n");
					DisplayError(ex.HResult);
				}
			}
			else
			{
				Console.Write("You have already registered for Machine cost notifications. Please unregister before registering for events again.\n");
				Console.Write("The Win32 Cost API feature allows multiple registrations, but the sample SDK does not allow this. \n");
			}
		}

		//********************************************************************************************
		// Function: RegisterForDestinationCostChangeNotifications
		//
		// Description: Registers for Destination cost change notifications, and waits in the message loop.
		//
		//********************************************************************************************
		static void RegisterForDestinationCostChangeNotifications()
		{
			var hr = GetDestinationAddress(out var sockAddr);
			if (hr == HRESULT.S_OK)
			{
				//Registration is allowed only once, before unregister.
				if (g_pSinkDestCostMgr is null)
				{
					try
					{
						g_pSinkDestCostMgr = CNetCostEventSink.StartListeningForEvents(typeof(INetworkCostManagerEvents).GUID, sockAddr);
						Console.Write("Listening for Destination address based cost change events...\n");
					}
					catch (Exception ex)
					{
						Console.Write("Registration failed, please try again. \n");
						hr = ex.HResult;
					}
				}
				else
				{
					Console.Write("You have already registered for Destination cost notifications. Please unregister before registering for events again.\n");
					Console.Write("The Win32 Cost API feature allows multiple registrations, but the sample SDK does not allow this. \n");

				}
			}
			DisplayError(hr);
		}

		//********************************************************************************************
		// Function: RegisterForConnectionCostChangeNotifications
		//
		// Description: Registers for connection cost change notifications, and waits in the message loop.
		//
		//********************************************************************************************
		static void RegisterForConnectionCostChangeNotifications()
		{
			HRESULT hr = HRESULT.S_OK;

			//Registration is allowed only once, before unregister.
			if (g_pSinkConnectionCostMgr is null)
			{
				try
				{
					g_pSinkConnectionCostMgr = CNetCostEventSink.StartListeningForEvents(typeof(INetworkConnectionCostEvents).GUID);
					Console.Write("Listening for Connection cost change events...\n");
				}
				catch (Exception ex)
				{
					Console.Write("Registration failed, please try again. \n");
					hr = ex.HResult;
				}
			}
			else
			{
				Console.Write("You have already registered for Connection cost notifications. Please unregister before registering for events again.\n");
				Console.Write("The Win32 Cost API feature allows multiple registrations, but the sample SDK does not allow this. \n");
			}
			DisplayError(hr);
		}

		//********************************************************************************************
		// Function: GetMachineCostandDataPlanStatus
		//
		// Description: Gets machine cost and data plan status, and displays to the user, along with suggested appropriate actions based on the 
		// retrieved cost and data plan status values.
		//
		//********************************************************************************************
		static void GetMachineCostandDataPlanStatus()
		{
			HRESULT hr = HRESULT.S_OK;
			try
			{
				using var pCostManager = ComReleaserFactory.Create(new INetworkCostManager());
				pCostManager.Item.GetCost(out var cost);
				pCostManager.Item.GetDataPlanStatus(out var dataPlanStatus);
				DisplayCostDescription(cost);
				DisplayDataPlanStatus(dataPlanStatus);

				//to give suggestions for data usage, depending on cost and data usage.
				CostBasedSuggestions(cost, dataPlanStatus);
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
			DisplayError(hr);
		}

		//********************************************************************************************
		// Function: GetDestinationCostandDataPlanStatus
		//
		// Description: Gets cost and data plan status for the destination specified, and displays to the user, along with suggested appropriate actions based on the 
		// retrieved cost and data plan status values.
		//
		//********************************************************************************************
		static void GetDestinationCostandDataPlanStatus()
		{
			HRESULT hr = HRESULT.S_OK;
			try
			{
				using var pCostManager = ComReleaserFactory.Create(new INetworkCostManager());
				hr = GetDestinationAddress(out var sockAddr);
				if (hr == HRESULT.S_OK)
				{
					pCostManager.Item.GetCost(out var cost, sockAddr.ipAddr);
					pCostManager.Item.GetDataPlanStatus(out var dataPlanStatus, sockAddr.ipAddr);
					DisplayCostDescription(cost);
					DisplayDataPlanStatus(dataPlanStatus);
					//to give suggestions for data usage, depending on cost and data usage.
					CostBasedSuggestions(cost, dataPlanStatus);
				}
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
			DisplayError(hr);
		}

		//********************************************************************************************
		// Function: GetConnectionCostandDataPlanStatus
		//
		// Description: Enumerates the connections and displays the cost and data plan status for each connection
		//
		//********************************************************************************************
		static void GetConnectionCostandDataPlanStatus()
		{
			HRESULT hr = HRESULT.S_OK;
			bool bDone = false;
			int numberOfConnections = 0;

			try
			{
				using var pCostManager = ComReleaserFactory.Create(new INetworkCostManager());
				var pNLM = (INetworkListManager)pCostManager.Item;
				var pNetworkConnections = pNLM.GetNetworkConnections();

				while (!bDone)
				{
					//Get cost and data plan status info for each of the connections on the machine
					hr = pNetworkConnections.Next(1, out var ppConnection, out var cFetched);
					if ((HRESULT.S_OK == hr) && (cFetched > 0))
					{
						try
						{
							using var pConnection = ComReleaserFactory.Create(ppConnection);
							numberOfConnections++;
							var interfaceGUID = pConnection.Item.GetAdapterId();
							Console.Write("--------------------------------------------------------------\n");
							GetInterfaceType(interfaceGUID, hr);

							// get the connection interface
							var pConnectionCost = (INetworkConnectionCost)pConnection.Item;
							var cost = pConnectionCost.GetCost();
							var dataPlanStatus = pConnectionCost.GetDataPlanStatus();
							DisplayCostDescription(cost);
							DisplayDataPlanStatus(dataPlanStatus);

							//to give suggestions for data usage, depending on cost and data usage.
							CostBasedSuggestions(cost, dataPlanStatus);
						}
						catch (Exception ex)
						{
							hr = ex.HResult;
						}
					}
					else
					{
						bDone = true;
					}

					if (numberOfConnections == 0)
					{
						Console.Write("Machine has no network connection\n");
					}
				}

			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}

			if (hr != HRESULT.S_FALSE)
			{
				DisplayError(hr);
			}
		}


		//********************************************************************************************
		// Function: UnRegisterForMachineCostChangeNotifications
		//
		// Description: Cancels registration for machine cost change notifications, and quits the listening thread
		//
		//********************************************************************************************
		static void UnRegisterForMachineCostChangeNotifications()
		{
			if (g_pSinkCostMgr != null)
			{
				g_pSinkCostMgr.Dispose();
				Console.Write("Successfully cancelled Registration for Machine Cost Notifications\n");
				g_pSinkCostMgr = default;
			}
			else
			{
				Console.Write("You have not registered for Machine cost Notifications\n");
			}
		}


		//********************************************************************************************
		// Function: UnRegisterForDestinationCostChangeNotifications
		//
		// Description: 
		//
		//********************************************************************************************
		static void UnRegisterForDestinationCostChangeNotifications()
		{
			if (g_pSinkDestCostMgr != null)
			{
				g_pSinkDestCostMgr.Dispose();
				Console.Write("Successfully cancelled Registration for Destination Cost Notifications\n");
				g_pSinkDestCostMgr = default;
			}
			else
			{
				Console.Write("You have not registered for Destination cost Notifications\n");
			}
		}

		//********************************************************************************************
		// Function: UnRegisterForConnectionCostChangeNotifications
		//
		// Description: 
		//
		//********************************************************************************************
		static void UnRegisterForConnectionCostChangeNotifications()
		{
			if (g_pSinkConnectionCostMgr != null)
			{
				g_pSinkConnectionCostMgr.Dispose();
				Console.Write("Successfully cancelled Registration for Connection Cost Notifications\n");
				g_pSinkConnectionCostMgr = default;
			}
			else
			{
				Console.Write("You have not registered for Connection cost Notifications\n");
			}
		}

		// default value for unavailable field in data plan status structure
		const uint NLM_UNKNOWN_DATAPLAN_STATUS = 0xFFFFFFFF;

		//********************************************************************************************
		// Function: CostBasedSuggestions
		//
		// Description: Takes cost and data plan status as input, and suggests appropriate actions to the user based on the 
		// cost and data plan status values.
		//
		//********************************************************************************************
		static void CostBasedSuggestions(NLM_CONNECTION_COST cost, in NLM_DATAPLAN_STATUS pDataPlanStatus)
		{
			if (cost == NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNKNOWN)
			{
				Console.Write("Cost value unknown\n");
				Console.Write("Please register for cost change notifications, to receive cost change value. \n");
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_ROAMING) != 0)
			{
				Console.Write("Connection is out of the MNO's network. Continuing data usage may lead to high charges, so the application can try to to stop or limit its data usage, to avoid high charges \n");
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_OVERDATALIMIT) != 0)
			{
				if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNRESTRICTED) != 0)
				{
					Console.Write("Plan data usage has exceeded the cap limit, but Cost type is unrestricted, so application can continue its current data usage.\n");
				}
				else
				{
					Console.Write("Plan data usage has exceeded the cap limit, so the application can limit or stop its current data usage, and try again later, to prevent additional charges.\n");
				}
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_CONGESTED) != 0)
			{
				Console.Write("Network is in a state of congestion, so the application can limit or stop its current data usage, and try again later.\n");
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_VARIABLE) != 0)
			{
				Console.Write("Cost type is variable, and data usage charged on a per byte basis. The application can therefore try to limit or stop its current data usage\n");
			}
			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_FIXED) != 0)
			{
				if ((!IsDataPlanStatusAvailable(pDataPlanStatus)) || (NLM_UNKNOWN_DATAPLAN_STATUS == pDataPlanStatus.UsageData.UsageInMegabytes)
				|| (NLM_UNKNOWN_DATAPLAN_STATUS == pDataPlanStatus.DataLimitInMegabytes))
				{
					// No access to data usage, to compare the data usage and the data limit
					Console.Write("Cost type is Fixed. No access to data plan status, to compare the data usage as a percent of data limit.\n");
				}
				else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_APPROACHINGDATALIMIT) != 0)
				{
					Console.Write("Data usage is approaching the data limit, the application can limit its data usage to avoid high charges\n");
				}
				else
				{
					Console.Write("Data usage is within limits, application can continue its current usage\n");
				}
			}
			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNRESTRICTED) != 0)
			{
				Console.Write("Cost type is unrestricted, application can continue its current data usage\n");
			}
			Console.Write("--------------------------------------------------------------\n");
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
					RegisterForMachineCostChangeNotifications();
					break;
				case 2:
					RegisterForDestinationCostChangeNotifications();
					break;
				case 3:
					RegisterForConnectionCostChangeNotifications();
					break;
				case 4:
					GetMachineCostandDataPlanStatus();
					break;
				case 5:
					GetDestinationCostandDataPlanStatus();
					break;
				case 6:
					GetConnectionCostandDataPlanStatus();
					break;
				case 7:
					UnRegisterForMachineCostChangeNotifications();
					break;
				case 8:
					UnRegisterForDestinationCostChangeNotifications();
					break;
				case 9:
					UnRegisterForConnectionCostChangeNotifications();
					break;
				case 10:
					FreeMemoryAndExit();
					break;
				default:
					Console.Write("Invalid choice. Please enter a valid choice number \n");
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
				"Register for machine Internet cost notifications",
				"Register for destination cost notifications",
				"Register for connection cost notifications",
				"Get machine wide cost and data plan status",
				"Get destination address based cost and data plan status",
				"Get connection cost and data plan status",
				"Unregister for machine cost notifications",
				"Unregister for destination cost notifications",
				"Unregister for connection cost notifications",
				"Exit" };
			int numchoices = choices.Length;
			Console.Write("---------------------------------------------------------\n");
			for (int i = 0; i < numchoices; i++)
			{
				Console.Write(" {0}. {1}\n", i + 1, choices[i]);
			}
			Console.Write("---------------------------------------------------------\n");
			Console.Write("Enter a choice (1-{0}): ", numchoices);
			var chr = ReadIntegerFromConsole($"Enter a choice (1-{numchoices}): ", 1, numchoices, "Invalid Choice. Please enter a valid choice number");
			FlushCurrentLine();
			EvaluateUserChoice(chr);
			return chr;
		}

		//********************************************************************************************
		// Function: FreeMemoryAndExit
		//
		// Description: Cancels registration for notifications, quits the thread and closes the thread handle
		//
		//********************************************************************************************
		static void FreeMemoryAndExit()
		{
			HRESULT hr = HRESULT.S_OK;
			if (g_pSinkCostMgr != null)
			{
				Console.Write("Unregistering for machine cost change events..\n");
				g_pSinkCostMgr.Dispose();
			}
			if (g_pSinkConnectionCostMgr != null)
			{
				Console.Write("Unregistering for Connection cost change events..\n");
				g_pSinkConnectionCostMgr.Dispose();
			}
			if (g_pSinkDestCostMgr != null)
			{
				Console.Write("Unregistering for Destination address based cost change events..\n");
				g_pSinkDestCostMgr.Dispose();
			}
			DisplayError(hr);
		}
	}
}