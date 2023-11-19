using System.Net;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Qwave;
using static Vanara.PInvoke.Traffic;
using static Vanara.PInvoke.Ws2_32;

//******************************************************************************
// Routine: 
// main
//
// Description:
// Entry point. Parses the commandline and calls CreateFilter and CreateFlow
// with appropriate parameters.
//******************************************************************************

const ushort NOT_SPECIFIED = ushort.MaxValue;

HeapSetInformation(default, HEAP_INFORMATION_CLASS.HeapEnableTerminationOnCorruption, default, 0);
SafeEventHandle? hEvent = null;

//
// Parse the Commandline and populate the TC_GEN_FILTER and TC_GEN_FLOW structures
//
if (!ParseCommandLine(args, out TC_GEN_FLOW? pTcFlow, out TC_GEN_FILTER pTcFilter))
{
	return help();
}

//
// Start Winsock
//
using var wsaData = SafeWSA.Initialize();

//
// Register TC client
//
TCI_CLIENT_FUNC_LIST ClientHandlerList = new() { ClNotifyHandler = ClNotifyHandler };
Win32Error err = TcRegisterClient(CURRENT_TCI_VERSION, default, ClientHandlerList, out SafeHCLIENT? hClient);
if (err.Failed)
{
	Console.Write("TcRegisterClient Failed {0}\n", err);

	if (err == Win32Error.ERROR_OPEN_FAILED)
	{
		Console.Write("Please make sure you are running with admin credentials\n");
	}
	return help();
}

using (hClient)
{
	//
	// Enumerate All TC enabled Interfaces and 
	// store the information in IfcList
	//
	if (!MakeIfcList(hClient, out List<IFC_INFO> IfcList))
	{
		Console.Write("Error reading interface list, make sure QoS Packet Scheduler is active for this interface\n");
		return help();
	}
	Console.Write("Interface list created...\n");
	try
	{
		//
		// Add pTcFlow on all the Ifcs in the IfcList
		//
#pragma warning disable CS8604 // Possible null reference argument.
		if (!AddTcFlows(IfcList, pTcFlow))
		{
			Console.Write("Error adding flows\n");
			return help();
		}
#pragma warning restore CS8604 // Possible null reference argument.
		Console.Write("Flows added...\n");

		//
		// Add pTcFilter to all the corresponding TcFlows
		// on all the Ifcs in the IfcList
		//
		if (!AddTcFilters(IfcList, pTcFilter))
		{
			Console.Write("Error adding filter...\n");
			return help();
		}
		Console.Write("Filter added...\n");

		//
		// Enable the Ctrl-C handler
		// 
		hEvent = CreateEvent(default, false, false, default);
		if (hEvent.IsInvalid)
		{
			Console.Write("Failed to create event\n");
			return help();
		}
		using (hEvent)
		{
			Console.Write("**Hit ^C to out Exit[] \n");

			SetConsoleCtrlHandler(Control_C_Handler, true);
			WaitForSingleObject(hEvent, INFINITE);
			SetConsoleCtrlHandler(Control_C_Handler, false);
		}
	}
	finally
	{
		//
		// Cleanup
		//
		ClearIfcList(IfcList);
		DeleteFilter(pTcFilter);
	}
}

return 0;

//******************************************************************************
// Routine: 
// Control_C_Handler
//
// Description:
// Handles the ctrl-c event and sets the gbRunning flag to false
// this indicates the app to exit
// 
//******************************************************************************
bool Control_C_Handler(CTRL_EVENT CtrlType)
{
	Console.Write("**Got ^C out event[] \n");
	hEvent?.Set();
	return true;
}

//******************************************************************************
// Routine: 
// ClNotifyHandler
//
// Description:
// Empty notification handler
// 
//******************************************************************************
void ClNotifyHandler(IntPtr ClRegCtx, IntPtr ClIfcCtx, TC_NOTIFY Event, IntPtr SubCode, uint BufSize, IntPtr Buffer)
{
	//
	// Notification was unexpected
	//
	Console.Write($"Unexpected notification: Event={Event}, SubCode={SubCode}, BufSize={BufSize}, Buffer={Buffer}");
}

//******************************************************************************
// Routine: 
// AddTcFlows
//
// Description:
// Add Tc Flow in pTcFlow to each interface in IfcList
// 
//******************************************************************************
bool AddTcFlows(List<IFC_INFO> IfcList, TC_GEN_FLOW pTcFlow)
{
	//
	// For each interface in the list, add a TC flow
	//
	for (int i = 0; i < IfcList.Count; i++)
	{
		var err = TcAddFlow(IfcList[i].hIfc!, default, 0, pTcFlow, out IfcList[i].hFlow);
		if (err.Failed)
		{
			Console.Write("TcAddFlow Failed {0}\n", err);
			return false;
		}
	}
	return true;
}

//******************************************************************************
// Routine: 
// AddTcFilters
//
// Description:
// Add Tc Filter in pTcFilter to each interface in IfcList
// 
//******************************************************************************
bool AddTcFilters(List<IFC_INFO> IfcList, in TC_GEN_FILTER pTcFilter)
{
	//
	// For each interface in the list, add TC filter on the corresponding TcFlow
	//
	for (int i = 0; i < IfcList.Count; i++)
	{
		err = TcAddFilter(IfcList[i].hFlow!, pTcFilter, out IfcList[i].hFilter);
		if (err.Failed)
		{
			Console.Write("TcAddFilter Failed {0}\n", err);
			return false;
		}
	}
	return true;
}

//******************************************************************************
// Routine: 
// ClearIfcList
//
// Description:
// Clears the IfcList and its member variables
// 
//******************************************************************************
bool ClearIfcList(List<IFC_INFO> pIfcList)
{
	foreach (var i in pIfcList)
		i.Dispose();
	pIfcList.Clear();
	return true;
}

//******************************************************************************
// Routine: 
// MakeIfcList
//
// Arguments:
// hClient - Handle returned by TcRegisterClient
// pIfcList - ptr to IfcList structure which will be populated by the function
// 
// Description:
// The function enumerates all TC enabled interfaces. 
// opens each TC enabled interface and stores each ifc handle in IFC_LIST struct
// pointed to by pIfcList
// 
//******************************************************************************
bool MakeIfcList(HCLIENT hClient, out List<IFC_INFO> pIfcList)
{
	pIfcList = new List<IFC_INFO>();

	//
	// Enumerate the TC enabled interfaces
	//
	foreach (var pCurrentIfc in TcEnumerateInterfaces(hClient))
	{
		//
		// Open Each interface and store the ifc handle in ifcList
		//
		var pCurrentIfcInfo = new IFC_INFO();
		var err = TcOpenInterface(pCurrentIfc.pInterfaceName, hClient, default, out pCurrentIfcInfo.hIfc);
		if (err.Failed)
		{
			Console.Write("TcOpenInterface Failed {0}\n", err);
			ClearIfcList(pIfcList);
			return false;
		}
		pIfcList.Add(pCurrentIfcInfo);
	}
	return true;
}

//******************************************************************************
// Routine: 
// DeleteFlow
//
// Description:
// Deletes the flow and its member variables
// 
//******************************************************************************
// Routine: 
// CreateFlow
//
// Arguments:
// ppTcFlowObj - double ptr to Flow struct in which the function returns the flow
// DSCPValue - dscp value for the flow
// OnePValue - 802.1p value for the flow
// ThrottleRate - throttle rate for the flow
//
// Description:
// The function returns a tc flow in ppTcFlowObj on success 
// 
//******************************************************************************
bool CreateFlow(out TC_GEN_FLOW pTcFlowObj, ushort DSCPValue, ushort OnePValue, uint ThrottleRate)
{
	//
	// Flow Parameters
	//
	SERVICETYPE ServiceType = SERVICETYPE.SERVICETYPE_BESTEFFORT;

	//
	// Calculate the memory size required for the optional TC objects
	//
	int Length = (OnePValue == NOT_SPECIFIED ? 0 : 1) + (DSCPValue == NOT_SPECIFIED ? 0 : 1);

	//
	// Print the Flow parameters
	//
	Console.Write("Flow Parameters:\n");
	Console.Write($"\tDSCP: {(DSCPValue == NOT_SPECIFIED ? "*" : DSCPValue.ToString())}\n");
	Console.Write($"\t802.1p: {(OnePValue == NOT_SPECIFIED ? "*" : OnePValue.ToString())}\n");
	if (ThrottleRate == QOS_NOT_SPECIFIED)
	{
		Console.Write("\tThrottleRate: *\n");
		Console.Write("\tServiceType: Best effort\n");
	}
	else
	{
		Console.Write("\tThrottleRate: {0}\n", ThrottleRate);
		Console.Write("\tServiceType: Guaranteed\n");
		ServiceType = SERVICETYPE.SERVICETYPE_GUARANTEED;
	}

	pTcFlowObj = new() { SendingFlowspec = FLOWSPEC.NotSpecified, TcObjects = new IQoSObjectHdr[Length] };
	pTcFlowObj.SendingFlowspec.TokenRate = ThrottleRate;
	pTcFlowObj.SendingFlowspec.TokenBucketSize = ThrottleRate;
	pTcFlowObj.SendingFlowspec.ServiceType = ServiceType;
	pTcFlowObj.ReceivingFlowspec = pTcFlowObj.SendingFlowspec;

	//
	// Add any requested objects
	//
	if (OnePValue != NOT_SPECIFIED)
	{
		QOS_TRAFFIC_CLASS pTClassObject = new();
		pTClassObject.ObjectHdr.ObjectType = QOS_OBJ_TYPE.QOS_OBJECT_TRAFFIC_CLASS;
		pTClassObject.ObjectHdr.ObjectLength = (uint)Marshal.SizeOf(typeof(QOS_TRAFFIC_CLASS));
		pTClassObject.TrafficClass = OnePValue; //802.1p tag to be used
		pTcFlowObj.TcObjects[0] = pTClassObject;
	}

	if (DSCPValue != NOT_SPECIFIED)
	{
		QOS_DS_CLASS pDSClassObject = new();
		pDSClassObject.ObjectHdr.ObjectType = QOS_OBJ_TYPE.QOS_OBJECT_DS_CLASS;
		pDSClassObject.ObjectHdr.ObjectLength = (uint)Marshal.SizeOf(typeof(QOS_DS_CLASS));
		pDSClassObject.DSField = DSCPValue; //Services Type
		pTcFlowObj.TcObjects[OnePValue != NOT_SPECIFIED ? 0 : 1] = pDSClassObject;
	}

	Console.Write("Flow Creation Succeeded\n");
	return true;
}

//******************************************************************************
// Routine: 
// DeleteFilter
//
// Description:
// Deletes the filter and its member variables
// 
//******************************************************************************
bool DeleteFilter(in TC_GEN_FILTER pFilter)
{
	if (pFilter.Pattern != IntPtr.Zero)
	{
		Marshal.FreeHGlobal(pFilter.Pattern);
	}

	if (pFilter.Mask != IntPtr.Zero)
	{
		Marshal.FreeHGlobal(pFilter.Mask);
	}

	return true;
}

//******************************************************************************
// Routine: 
// PrintAddress
//
// Description:
// The function prints out the address contained in the SOCKADDR_STORAGE
// 
//******************************************************************************
void PrintAddress(in SOCKADDR_STORAGE pSocketAddress)
{
	Console.Write("\tAddress: {0}\n", (SOCKADDR_INET)pSocketAddress);
}

//******************************************************************************
// Routine: 
// CreateFilter
//
// Arguments:
// ppFilter - double ptr to Filter struct in which the function returns the filter
// Address - destination address of the outgoing packets of interest.
// Port - destination port of the outgoing packets of interest.
// ProtocolId - protocol of the outgoing packets of interest.
//
// Description:
// The function returns a tc filter in ppFilter on success 
// 
//******************************************************************************
bool CreateFilter(out TC_GEN_FILTER pFilter, SOCKADDR_STORAGE Address, ushort Port, IPPROTO ProtocolId)
{
	pFilter = default;
	ADDRESS_FAMILY AddressFamily = Address.ss_family;

	//
	// Print out the Filter Parameters
	//
	Console.Write("Filter Parameters:\n");
	PrintAddress(Address);
	Console.Write("\tDest Port: {0}\n", Port);
	Console.Write("\tProtocol: ");
	switch (ProtocolId)
	{
		case IPPROTO.IPPROTO_IP:
			{
				Console.Write("IP\n");
				break;
			}
		case IPPROTO.IPPROTO_TCP:
			{
				Console.Write("TCP\n");
				break;
			}
		case IPPROTO.IPPROTO_UDP:
			{
				Console.Write("UDP\n");
				break;
			}
		default:
			{
				Console.Write("Invalid Protocol\n");
				break;
			}
	};

	if (AddressFamily != ADDRESS_FAMILY.AF_INET)
	{
		Console.Write("Filter Creation Failed\n");
		return false;
	}

	//
	// Allocate memory for the pattern and mask
	//
	IP_PATTERN pPattern = new() { DstAddr = ((SOCKADDR_IN)Address).sin_addr, tcDstPort = htons(Port), ProtocolId = (byte)ProtocolId };

	//
	// Set the source address and port to wildcard
	// 0 . wildcard, 0xFF. exact match 
	//
	IP_PATTERN pMask = new();

	//
	// If the user specified 0 for dest port, dest address or protocol
	// set the appropriate mask as wildcard
	// 0 . wildcard, 0xFF. exact match 
	//

	if (pPattern.tcDstPort == 0)
	{
		pMask.tcDstPort = 0;
	}

	if (pPattern.ProtocolId == 0)
	{
		pMask.ProtocolId = 0;
	}

	if (pPattern.DstAddr == 0)
	{
		pMask.DstAddr = 0;
	}

	pFilter.AddressType = NDIS_PROTOCOL_ID.NDIS_PROTOCOL_ID_TCP_IP;
	pFilter.PatternSize = (uint)Marshal.SizeOf(typeof(IP_PATTERN));
	pFilter.Pattern = pPattern.MarshalToPtr(Marshal.AllocHGlobal, out _);
	pFilter.Mask = pMask.MarshalToPtr(Marshal.AllocHGlobal, out _);

	Console.Write("Filter Creation Succeeded\n");
	return true;
}

//******************************************************************************
// Routine: 
// GetSockAddrFromString
//
// Arguments:
// strAddress - Address in the String format
// pSocketAddress - Pointer to SOCKADDR_STORAGE structure where the 
// address is returned
//
// Description:
// Takes a string format address and returns a pointer to 
// SOCKADDR_STORAGE structure containing the address.
// Only resolves numeric addresses
//******************************************************************************
bool GetSockAddrFromString(string strAddress, ref SOCKADDR_STORAGE pSocketAddress)
{
	try
	{
		pSocketAddress = (SOCKADDR_STORAGE)IPAddress.Parse(strAddress);
		return true;
	}
	catch { return false; }
}

bool _wcsicmp(string? l, string? r) => string.Compare(l, r, true) == 0;

//******************************************************************************
// Routine: 
// ParseCommandline
//
// Arguments:
// args - Commandline Parameters
// pTcFlow - Pointer to PTC_GEN_FLOW structure.
// pTcFilter - Pointer to PTC_GEN_FILTER structure.
//
// Description:
// Parses the commandline and populates creates TC_GEN_FLOW and 
// TC_GEN_FILTER structures with the appropriate values.
//******************************************************************************
bool ParseCommandLine(string[] args, out TC_GEN_FLOW? pTcFlow, out TC_GEN_FILTER pTcFilter)
{
	bool status = false;
	IPPROTO ProtocolId = 0;
	ushort Port = 0, DSCPValue = NOT_SPECIFIED, OnePValue = NOT_SPECIFIED;
	uint ThrottleRate = QOS_NOT_SPECIFIED;
	string strAddress = "0";
	SOCKADDR_STORAGE Address = new() { ss_family = ADDRESS_FAMILY.AF_INET };
	pTcFlow = default;
	pTcFilter = default;

	//
	// Extract commandline parameters and do some basic validation
	//
	int i = 0;
	while (i < args.Length)
	{
		if (!_wcsicmp(args[i], "-proto"))
		{
			i++;
			if (string.IsNullOrEmpty(args[i]))
			{
				goto Exit;
			}

			if (!_wcsicmp(args[i], "tcp"))
			{
				ProtocolId = IPPROTO.IPPROTO_TCP;
			}
			else if (!_wcsicmp(args[i], "udp"))
			{
				ProtocolId = IPPROTO.IPPROTO_UDP;
			}
			else if (!_wcsicmp(args[i], "ip"))
			{
				ProtocolId = IPPROTO.IPPROTO_IP;
			}
			else
			{
				Console.Write("Invalid Protocol\n");
				goto Exit;
			}
		}
		else if (!_wcsicmp(args[i], "-destip"))
		{
			i++;
			if (string.IsNullOrEmpty(args[i]))
			{
				goto Exit;
			}

			strAddress = args[i];
		}
		else if (!_wcsicmp(args[i], "-destport"))
		{
			i++;
			if (string.IsNullOrEmpty(args[i]))
			{
				goto Exit;
			}

			Port = ushort.Parse(args[i]);
		}
		else if (!_wcsicmp(args[i], "-dscp"))
		{
			i++;
			if (string.IsNullOrEmpty(args[i]))
			{
				goto Exit;
			}

			DSCPValue = ushort.Parse(args[i]);
			if (DSCPValue < 0 || DSCPValue > 63)
			{
				Console.Write("Invalid DSCP Value\n");
				goto Exit;
			}
		}
		else if (!_wcsicmp(args[i], "-onep"))
		{
			i++;
			if (string.IsNullOrEmpty(args[i]))
			{
				goto Exit;
			}

			OnePValue = ushort.Parse(args[i]);
			if (OnePValue < 0 || OnePValue > 7)
			{
				Console.Write("Invalid 802.1p Value\n");
				goto Exit;
			}
		}
		else if (!_wcsicmp(args[i], "-throttle"))
		{
			i++;
			if (string.IsNullOrEmpty(args[i]))
			{
				goto Exit;
			}

			ThrottleRate = uint.Parse(args[i]);
		}
		else
		{
			goto Exit;
		}

		i++;
	}

	//
	// if dest addr is not specified or specified as zero
	// treat it as wildcard, dont try to resolve it
	//
	if (!string.IsNullOrWhiteSpace(strAddress) && strAddress.Trim() != "0")
	{
		if (!GetSockAddrFromString(strAddress, ref Address))
		{
			goto Exit;
		}
	}

	//
	// Create the TC Flow with the parameters
	//
	if (!CreateFlow(out pTcFlow, DSCPValue, OnePValue, ThrottleRate))
	{
		goto Exit;
	}

	//
	// Create the TC Filter with the parameters
	//
	if (!CreateFilter(out pTcFilter, Address, Port, ProtocolId))
	{
		goto Exit;
	}

	status = true;

Exit:
	if (!status)
	{
		Console.Write("Invalid Argument(s)\n");
	}

	return status;
}

//******************************************************************************
// Routine: 
// help
//
// Description:
// This routine prints out the usage information for the program
//
//******************************************************************************
int help()
{
	Console.Write("\nUsage: tcsample <optional parameters>\n");
	Console.Write("\t-proto : protocol(tcp/udp/ip) (Default: All IP Protocols)\n");
	Console.Write("\t-destip : destination IP Address (Default: All IP Addresses)\n");
	Console.Write("\t-destport: destination port number (Default: All Ports)\n");
	Console.Write("\t-dscp : dscp value to tag matching packets (Default: No DSCP Override)\n");
	Console.Write("\t-onep : 802.1p value to tag matching packets (Default: No 802.1p Override)\n");
	Console.Write("\t-throttle: throttle rate(Bps) to throttle matching packets (Default: No Throttling)\n");
	Console.Write("\nExample: tcsample -destip 192.168.1.10 -proto ip -dscp 40\n");
	Console.Write("will result in all outgoing IP Packets destined to\n");
	Console.Write("192.168.1.10 to be marked with DSCP value 40\n");
	return 0;
}

//******************************************************************************
// Global defines
//******************************************************************************

public class IFC_INFO : IDisposable
{
	public SafeHIFC? hIfc;
	public SafeHFLOW? hFlow;
	public SafeHFILTER? hFilter;

	public void Dispose()
	{
		hIfc?.Dispose();
		hFlow?.Dispose();
		hFilter?.Dispose();
	}
}