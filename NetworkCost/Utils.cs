using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.PInvoke.NetListMgr;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

namespace NetworkCost
{
	static partial class NetCostSample
	{
		//
		// Structure to store destination address string and converted numeric IP address
		//
		public class DESTINATION_INFO
		{
			public string addrString;
			public NLM_SOCKADDR ipAddr;
		}

		//********************************************************************************************
		// Function: FlushCurrentLine
		//
		// Description: Clears any input lingering in the STDIN buffer
		//
		//********************************************************************************************
		static void FlushCurrentLine()
		{
			while (Console.KeyAvailable)
				Console.ReadKey();
		}

		//********************************************************************************************
		// Function: GetPreferredAddress
		//
		// Description: This function sorts a list of Ipv4 & Ipv6 addresses, and returns the "best" address that stack determines
		//
		//********************************************************************************************
		static Win32Error GetPreferredAddress(SOCKADDR_IN6[] pAddrList, out SOCKADDR_STORAGE pPreferredAddr)
		{
			Win32Error dwErr = Win32Error.ERROR_SUCCESS;

			pPreferredAddr = default;

			// Initialize WinSock 
			using var wsa = SafeWSA.Initialize();

			// create socket
			var socketIoctl = WSASocket(ADDRESS_FAMILY.AF_INET6, SOCK.SOCK_DGRAM, 0, IntPtr.Zero, 0, WSA_FLAG.WSA_FLAG_OVERLAPPED);
			if (socketIoctl == SOCKET.INVALID_SOCKET)
			{
				dwErr = WSAGetLastError();
				Console.Write("WSASocket failed, (dwErr = {0}).", dwErr);
				return dwErr;
			}

			var numElement = pAddrList.Length;
			var pSockAddrList = Array.ConvertAll(pAddrList, v6 => new SOCKADDR(v6));
			var pSocketAddrList = new SOCKET_ADDRESS_LIST { iAddressCount = numElement, Address = Array.ConvertAll(pSockAddrList, sa => new SOCKET_ADDRESS { iSockaddrLength = sa.Size, lpSockaddr = sa.DangerousGetHandle() } ) };

			// sort addresses
			using var ppSocketAddrList = SafeHGlobalHandle.CreateFromStructure(pSocketAddrList);
			uint dwSize = 4U + (uint)(Marshal.SizeOf<SOCKET_ADDRESS>() * pSocketAddrList.iAddressCount);
			dwErr = WSAIoctl(socketIoctl, WinSockIOControlCode.SIO_ADDRESS_LIST_SORT, ppSocketAddrList, dwSize, ppSocketAddrList, dwSize, out var dwBytes);
			pSocketAddrList = ppSocketAddrList.ToStructure<SOCKET_ADDRESS_LIST>();

			if (dwErr == SOCKET_ERROR)
			{
				dwErr = WSAGetLastError();
				Console.Write("WSAIoctl sort address failed, (dwErr = {0}).", dwErr);
				return dwErr;
			}

			var pBestAddress = new IPAddress(pSocketAddrList.Address[0].lpSockaddr.ToArray<byte>(pSocketAddrList.Address[0].iSockaddrLength));
			pPreferredAddr = (SOCKADDR_STORAGE)(pBestAddress.IsIPv4MappedToIPv6 ? pBestAddress.MapToIPv4() : pBestAddress);

			if (socketIoctl != SOCKET.INVALID_SOCKET)
			{
				closesocket(socketIoctl);
				socketIoctl = SOCKET.INVALID_SOCKET;
			}

			return dwErr;
		}


		//********************************************************************************************
		// Function: ConvertStringToSockAddr
		//
		// Description: Converts destination hostname or URL to IP address
		//
		//********************************************************************************************
		static HRESULT ConvertStringToSockAddr([In] string destIPAddr, out SOCKADDR_STORAGE socketAddress)
		{
			const int NO_ERROR = 0;
			HRESULT hr = HRESULT.S_OK;
			socketAddress = default;

			//Intialize socket, to use GetAddrInfoW()
			using var wsa = SafeWSA.Initialize();

			//This do--while(false) loop is used to break in between based on error conditions
			do
			{
				//get Destination IP address from Destination address string hostname or URL
				var dwErr = GetAddrInfoW(destIPAddr, default, default, out var result);
				if (dwErr == NO_ERROR)
				{
					//Get the number of socket addresses returned by GetAddrInfoW
					var destAddrList = new List<SOCKADDR_IN6>();
					using (result)
					{
						foreach (var ptr in result)
						{
							try
							{
								var addr = new IPAddress(ptr.addr.GetAddressBytes());
								var addr6 = addr.MapToIPv6();
								destAddrList.Add(new SOCKADDR_IN6(addr6.GetAddressBytes(), (uint)addr6.ScopeId));
							}
							catch
							{
								hr = HRESULT.E_UNEXPECTED;
								Console.Write("Unsupported IP family, please enter IPv4 or IPv6 destination\n");
								break;
							}
						}
					}

					// determine the preferred IP address
					dwErr = GetPreferredAddress(destAddrList.ToArray(), out socketAddress);
					if (dwErr != NO_ERROR)
					{
						Console.Write("WSAIoctl failed, (dwErr = {0}).", dwErr);
						hr = (HRESULT)dwErr;
					}
				}
				else
				{
					hr = (HRESULT)dwErr;
				}
			} while (false);

			DisplayError(hr);
			return hr;
		}


		//********************************************************************************************
		// Function: GetInterfaceType
		//
		// Description: Gets the interface type for each connection
		//
		//********************************************************************************************
		static void GetInterfaceType([In] Guid interfaceGUID, [In] HRESULT hr)
		{
			if (hr == HRESULT.S_OK)
			{
				// Get interface LUID 
				var dwError = IpHlpApi.ConvertInterfaceGuidToLuid(interfaceGUID, out var interfaceLUID);
				if (dwError.Succeeded)
				{
					//Get interface info entry
					var mib = new MIB_IF_ROW2(interfaceLUID);
					dwError = GetIfEntry2(ref mib);
					if (dwError.Succeeded)
					{
						// Get interface type
						Console.Write("Connection Interface : {0}\n", mib.Description);
					}
				}
				hr = (HRESULT)dwError;
			}
			DisplayError(hr);
		}



		//********************************************************************************************
		// Function: IsDataPlanStatusAvailable
		//
		// Description: Checks if the data plan status values are default values, or provided by the MNO
		//
		//********************************************************************************************
		static bool IsDataPlanStatusAvailable(in NLM_DATAPLAN_STATUS pDataPlanStatus)
		{
			bool isAvailable = false;
			//
			// usage data is valid only if both planUsage and lastUpdatedTime are valid
			//
			if (pDataPlanStatus.UsageData.UsageInMegabytes != NLM_UNKNOWN_DATAPLAN_STATUS
			&& (pDataPlanStatus.UsageData.LastSyncTime.dwHighDateTime != 0
			|| pDataPlanStatus.UsageData.LastSyncTime.dwLowDateTime != 0))
			{
				isAvailable = true;
			}
			else if (pDataPlanStatus.DataLimitInMegabytes != NLM_UNKNOWN_DATAPLAN_STATUS)
			{
				isAvailable = true;
			}
			else if (pDataPlanStatus.InboundBandwidthInKbps != NLM_UNKNOWN_DATAPLAN_STATUS)
			{
				isAvailable = true;
			}
			else if (pDataPlanStatus.OutboundBandwidthInKbps != NLM_UNKNOWN_DATAPLAN_STATUS)
			{
				isAvailable = true;
			}
			else if (pDataPlanStatus.NextBillingCycle.dwHighDateTime != 0
			|| pDataPlanStatus.NextBillingCycle.dwLowDateTime != 0)
			{
				isAvailable = true;

			}
			else if (pDataPlanStatus.MaxTransferSizeInMegabytes != NLM_UNKNOWN_DATAPLAN_STATUS)
			{
				isAvailable = true;
			}
			return isAvailable;
		}

		//********************************************************************************************
		// Function: DisplayCost
		//
		// Description: Displays meaningful cost values to the user
		//
		//********************************************************************************************
		static void DisplayCostDescription(NLM_CONNECTION_COST cost)
		{
			if (cost == NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNKNOWN)
			{
				Console.Write("Cost : Unknown\n");
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNRESTRICTED) != 0)
			{
				Console.Write("Cost : Unrestricted\n");
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_FIXED) != 0)
			{
				Console.Write("Cost : Fixed\n");
			}

			else if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_VARIABLE) != 0)
			{
				Console.Write("Cost : Variable\n");
			}

			if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_OVERDATALIMIT) != 0)
			{
				Console.Write("OverDataLimit : Yes\n");
			}
			else
			{
				Console.Write("OverDataLimit : No\n");
			}

			if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_APPROACHINGDATALIMIT) != 0)
			{
				Console.Write("Approaching DataLimit : Yes\n");
			}
			else
			{
				Console.Write("Approaching DataLimit : No\n");
			}

			if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_CONGESTED) != 0)
			{
				Console.Write("Congested : Yes\n");
			}
			else
			{
				Console.Write("Congested : No\n");
			}

			if ((cost & NLM_CONNECTION_COST.NLM_CONNECTION_COST_ROAMING) != 0)
			{
				Console.Write("Roaming : Yes\n");
			}

			else
			{
				Console.Write("Roaming : No\n");
			}
		}

		//********************************************************************************************
		// Function: DisplayDataPlanStatus
		//
		// Description: Displays data plan status values to the user
		//
		//********************************************************************************************
		static void DisplayDataPlanStatus([In] in NLM_DATAPLAN_STATUS pDataPlanStatus)
		{
			if (false == IsDataPlanStatusAvailable(pDataPlanStatus))
			{
				Console.Write("Plan Data usage unknown\n");
			}
			else
			{
				Console.Write("Interface ID : {0}\n", pDataPlanStatus.InterfaceGuid);

				//check for default or unknown value
				if (pDataPlanStatus.UsageData.UsageInMegabytes != NLM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Data Usage in Megabytes : {0}\n", pDataPlanStatus.UsageData.UsageInMegabytes);
				}

				if ((pDataPlanStatus.UsageData.LastSyncTime.dwHighDateTime != 0) || (pDataPlanStatus.UsageData.LastSyncTime.dwLowDateTime != 0))
				{
					Console.Write("Data Usage Synced Time : ");
					PrintFileTime(pDataPlanStatus.UsageData.LastSyncTime);
				}
				if (pDataPlanStatus.DataLimitInMegabytes != NLM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Data Limit in Megabytes : {0}\n", pDataPlanStatus.DataLimitInMegabytes);
				}
				if (pDataPlanStatus.InboundBandwidthInKbps != NLM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Inbound Bandwidth in Kbps : {0}\n", pDataPlanStatus.InboundBandwidthInKbps);
				}
				if (pDataPlanStatus.OutboundBandwidthInKbps != NLM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Outbound Bandwidth in Kbps : {0}\n", pDataPlanStatus.OutboundBandwidthInKbps);
				}
				if ((pDataPlanStatus.NextBillingCycle.dwHighDateTime != 0) || (pDataPlanStatus.NextBillingCycle.dwLowDateTime != 0))
				{
					Console.Write("Next Billing Cycle : ");
					PrintFileTime(pDataPlanStatus.NextBillingCycle);
				}
				if (pDataPlanStatus.MaxTransferSizeInMegabytes != NLM_UNKNOWN_DATAPLAN_STATUS)
				{
					Console.Write("Maximum Transfer Size in Megabytes : {0}\n", pDataPlanStatus.MaxTransferSizeInMegabytes);
				}
			}
		}

		//********************************************************************************************
		// Function: PrintFileTime
		//
		// Description: Converts file time to local time, to display to the user
		//
		//********************************************************************************************
		static void PrintFileTime([In] FILETIME time)
		{
			// Convert filetime to local time.
			Console.WriteLine(time.ToDateTime().ToString("s"));
		}


		//struct errorDescription
		//{
		//	public HRESULT hr;
		//	public string description;
		//}

		//********************************************************************************************
		// Function: DisplayError
		//
		// Description: Maps common HRESULT s to descriptive error strings
		//
		//********************************************************************************************
		static void DisplayError([In] HRESULT hr)
		{
			if (hr.Failed)
			{
				Console.Write("{0}\n", hr.GetException().Message);
			}
		}

		//********************************************************************************************
		// Function: GetConnectionFromGUID
		//
		// Description: Gets the connection type from the connection Guid
		//
		//********************************************************************************************

		static HRESULT GetConnectionFromGUID([In] INetworkListManager pManager, Guid connID, out INetworkConnection ppConnection)
		{
			HRESULT hr = HRESULT.S_OK;
			bool bFound = false;
			bool bDone = false;
			ppConnection = default;

			try
			{
				var pNetworkConnections = pManager.GetNetworkConnections();
				while (!bDone)
				{
					hr = pNetworkConnections.Next(1, out var pConnection, out var cFetched);
					if (hr.Succeeded && cFetched > 0)
					{
						try
						{
							var guid = pConnection.GetConnectionId();
							if (guid == connID)
							{
								ppConnection = pConnection;
								bFound = true;
								break;
							}
						}
						catch (Exception ex)
						{
							hr = ex.HResult;
							break;
						}
					}
					else
					{
						bDone = true;
					}
				}
				if (!bFound)
				{
					hr = HRESULT.E_FAIL;
				}
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
			return hr;
		}

		//********************************************************************************************
		// Function: GetDestinationAddress
		//
		// Description: Gets Destination String and converts it to socket address
		//
		//********************************************************************************************
		static HRESULT GetDestinationAddress(out DESTINATION_INFO pDestIPAddr)
		{
			pDestIPAddr = default;

			//The feature allow registration for multiple destination addresses, the sample SDK restricts to one Destination address
			Console.Write("Please enter the destination address :\n");
			var destAddress = Console.ReadLine();
			//convert destination addr string to numeric IP address
			var hr = ConvertStringToSockAddr(destAddress, out var destSocketAddress);
			if (hr == HRESULT.S_OK)
			{
				//enter the destination addr string and the numeric IP address to structure, to retrieve the string later for display info
				pDestIPAddr.addrString = destAddress;
				pDestIPAddr.ipAddr = new NLM_SOCKADDR { data = ((SOCKADDR)destSocketAddress).GetAddressBytes() };
			}
			return hr;
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