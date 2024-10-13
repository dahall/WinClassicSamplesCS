using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

ushort wVersionRequested = Macros.MAKEWORD(1, 1);
using var wsa = SafeWSA.DemandVersion(wVersionRequested);

if (args.Length < 1 || args[0][0] != '-')
	Usage("IpRoute");
if (args[0].Length > 2)
	Usage("IpRoute");

switch (args[0][1])
{
	case 'p':
		// Print routing table
		DoGetIpForwardTable();
		break;

	case 'a':
		//Adds an entry into routing table
		if (args.Length == 5)
			DoSetIpForwardEntry(args[1], args[2], args[3], args[4]); // dest mask gateway if
		else if (args.Length == 6)
		{
			if (uint.TryParse(args[5], out uint dwMetric))
				DoSetIpForwardEntry(args[1], args[2], args[3], args[4], dwMetric);
			else
				Console.Write("IpRoute: Bad argument {0}\n", args[5]);
		}
		else
			Usage("IpRoute");
		break;

	case 'd':
		//Delete an entry from the routing table
		if (args.Length == 2)
			DoDeleteIpForwardEntry(args[1]);
		else
			Usage("IpRoute");
		break;

	default:
		// help
		Usage("IpRoute");
		break;
}

void Usage(string pszProgramName)
{
	Console.Write("Manipulates network routing tables.\n\n");

	Console.Write("{0} -p                                               ...Prints route table.\n", pszProgramName);
	Console.Write("{0} -a destination netmask gateway interface [metric]...Adds a route.\n", pszProgramName);
	Console.Write("{0} -d destination                                   ...Deletes routes to\n", pszProgramName);
	Console.Write("                                                             destination.\n\n");

	Console.Write("destination  Specifies the destination host.\n\n");

	Console.Write("netmask      Specifies a subnet mask value to be associated\n");
	Console.Write("             with this route entry.\n\n");

	Console.Write("gateway      Specifies gateway.\n\n");

	Console.Write("interface    Specifies the interface ip.\n\n");

	Console.Write("metric       The cost for this destination\n\n");

	Console.Write("Diagnostic Notes:\n\n");
	Console.Write("Invalid MASK generates an error, that is when (DEST & MASK) != DEST.\n");
	Console.Write("Example> IpRoute -a 157.0.0.0 155.0.0.0 157.55.80.1 157.55.80.9\n");
	Console.Write("         IpRoute: Invalid Mask 155.0.0.0\n\n");

	Console.Write("Examples:\n\n");

	Console.Write("> IpRoute -p\n");
	Console.Write("> IpRoute -a 157.0.0.0    255.0.0.0    157.55.80.1    157.55.80.9         1\n");
	Console.Write("             ^destination ^mask        ^gateway       ^existing interface ^metric\n");
	Console.Write("> IpRoute -p\n");
	Console.Write("> IpRoute -d 157.0.0.0\n");
	Console.Write("> IpRoute -p\n");

	Environment.Exit(1);
}

void DoGetIpForwardTable()
{
	Win32Error dwStatus;
	if ((dwStatus = MyGetIpForwardTable(out var pIpRouteTab, true)).Succeeded)
	{
		PrintIpForwardTable(pIpRouteTab!);
		return;
	}
	else if (dwStatus == Win32Error.ERROR_NO_DATA)
	{
		Console.Write("No entries in route table.\n");
		return;
	}
	else
	{
		Console.Write("IpRoute returned 0x{0:X}\n", dwStatus);
		return;
	}
}

void DoSetIpForwardEntry(string pszDest, string pszNetMask, string pszGateway, string pszInterface, uint dwMetric = uint.MaxValue)
{
	// converting and checking input arguments...
	if (pszDest is null || pszNetMask is null || pszGateway is null)
	{
		Console.Write("IpRoute: Bad Argument\n");
		return;
	}

	MIB_IPFORWARDROW routeEntry = new()
	{
		dwForwardDest = inet_addr(pszDest) // convert dotted ip addr. to ip addr.
	}; // Ip routing table row entry
	if (routeEntry.dwForwardDest == IN_ADDR.INADDR_NONE)
	{
		Console.Write("IpRoute: Bad Destination {0}\n", pszDest);
		return;
	}

	routeEntry.dwForwardMask = inet_addr(pszNetMask);
	if (routeEntry.dwForwardMask == IN_ADDR.INADDR_NONE && "255.255.255.255" == pszNetMask)
	{
		Console.Write("IpRoute: Bad Mask {0}\n", pszNetMask);
		return;
	}

	routeEntry.dwForwardNextHop = inet_addr(pszGateway);
	if (routeEntry.dwForwardNextHop == IN_ADDR.INADDR_NONE)
	{
		Console.Write("IpRoute: Bad Gateway {0}\n", pszGateway);
		return;
	}

	if ((routeEntry.dwForwardDest & routeEntry.dwForwardMask) != routeEntry.dwForwardDest)
	{
		Console.Write("IpRoute: Invalid Mask {0}\n", pszNetMask);
		return;
	}

	// Interface index number
	// Interface Subnet Mask
	uint dwIfIpAddr = inet_addr(pszInterface); // Interface Ip Address
	if (dwIfIpAddr == IN_ADDR.INADDR_NONE)
	{
		Console.Write("IpRoute: Bad Interface {0}\n", pszInterface);
		return;
	}

	// Check if we have the given interface
	Win32Error dwStatus;
	if ((dwStatus = MyGetIpAddrTable(out var pIpAddrTable)).Failed)
	{
		Console.Write("GetIpAddrTable returned 0x{0:X}\n", (uint)dwStatus);
		return;
	}

	if (InterfaceIpToIdxAndMask(pIpAddrTable!, pszInterface, out uint dwIfIndex, out uint dwIfMask) == false)
	{
		Console.Write("IpRoute: Bad Argument {0}\n", pszInterface);
		return;
	}

	if ((routeEntry.dwForwardNextHop & dwIfMask) != (dwIfIpAddr & dwIfMask))
	{
		Console.Write("IpRoute: Gateway {0} and Interface {1} are not in the same subnet.\n", pszGateway, pszInterface);
		return;
	}

	routeEntry.dwForwardIfIndex = dwIfIndex;

	routeEntry.dwForwardMetric1 = dwMetric;

	// some default values
	routeEntry.dwForwardProto = MIB_IPFORWARD_PROTO.MIB_IPPROTO_NETMGMT;
	routeEntry.dwForwardMetric2 = uint.MaxValue;
	routeEntry.dwForwardMetric3 = uint.MaxValue;
	routeEntry.dwForwardMetric4 = uint.MaxValue;

	dwStatus = SetIpForwardEntry(routeEntry);
	if (dwStatus.Failed)
	{
		Console.Write("IpRoute: couldn't add ({0}), dwStatus = {1}.\n", pszDest, dwStatus);
	}
}

void DoDeleteIpForwardEntry(string pszDest)
{
	bool fDeleted = false;

	MIB_IPFORWARDROW routeEntry = new(); // Ip routing table row entry
	uint dwForwardDest = inet_addr(pszDest); // convert dotted ip addr. to ip addr.
	if (dwForwardDest == IN_ADDR.INADDR_NONE)
	{
		Console.Write("IpRoute: Bad Destination {0}\n", pszDest);
		return;
	}

	Win32Error dwStatus;
	if ((dwStatus = MyGetIpForwardTable(out var pIpRouteTab, true)).Succeeded)
	{
		for (int i = 0; i < pIpRouteTab!.dwNumEntries; i++)
		{
			if (dwForwardDest == pIpRouteTab.table[i].dwForwardDest)
			{
				pIpRouteTab.table[i] = routeEntry;
				var dwDelStatus = DeleteIpForwardEntry(routeEntry);
				if (dwDelStatus.Failed)
				{
					Console.Write("IpRoute: couldn't delete ({0}), dwStatus = {1}.\n", pszDest, (uint)dwDelStatus);
					return;
				}
				else
					fDeleted = true;
			}
		}
		if (!fDeleted)
			Console.Write("IpRoute: The route specified was not found.\n");
		return;
	}
	else if (dwStatus == Win32Error.ERROR_NO_DATA)
	{
		Console.Write("IpRoute: No entries in route table.\n");
		return;
	}
	else
	{
		Console.Write("IpRoute returned 0x{0:X}\n", dwStatus);
		return;
	}
}

//----------------------------------------------------------------------------
// If returned status is NO_ERROR, then pIpRouteTab points to a routing
// table.
//----------------------------------------------------------------------------
Win32Error MyGetIpForwardTable(out MIB_IPFORWARDTABLE? pIpRouteTab, bool fOrder)
{
	try
	{
		// query for buffer size needed
		pIpRouteTab = GetIpForwardTable(fOrder);
		Console.WriteLine("No error");
		return Win32Error.NO_ERROR;
	}
	catch (Exception ex)
	{
		pIpRouteTab = null;
		return Win32Error.FromException(ex);
	}
}

//----------------------------------------------------------------------------
// Print out ip routing table in the following format:
//Active Routes:
//
//  Network Address          Netmask  Gateway Address        Interface  Metric
//          0.0.0.0          0.0.0.0     157.54.176.1   157.54.177.149       1
//        127.0.0.0        255.0.0.0        127.0.0.1        127.0.0.1       1
//     157.54.176.0    255.255.252.0   157.54.177.149   157.54.177.149       1
//   157.54.177.149  255.255.255.255        127.0.0.1        127.0.0.1       1
//   157.54.255.255  255.255.255.255   157.54.177.149   157.54.177.149       1
//        224.0.0.0        224.0.0.0   157.54.177.149   157.54.177.149       1
//  255.255.255.255  255.255.255.255   157.54.177.149   157.54.177.149       1
//----------------------------------------------------------------------------
void PrintIpForwardTable(in MIB_IPFORWARDTABLE pIpRouteTable)
{
	// get IP Address Table for mapping interface index number to ip address
	Win32Error dwStatus;
	if ((dwStatus = MyGetIpAddrTable(out var pIpAddrTable)).Failed)
	{
		Console.Write("GetIpAddrTable returned 0x{0:X}\n", (uint)dwStatus);
		return;
	}

	Console.Write("Active Routes:\n\n");

	Console.Write(" Network Address          Netmask  Gateway Address        Interface  Metric\n");
	for (int i = 0; i < pIpRouteTable.dwNumEntries; i++)
	{
		var dwCurrIndex = pIpRouteTable.table[i].dwForwardIfIndex;
		if (InterfaceIdxToInterfaceIp(pIpAddrTable!, dwCurrIndex, out var szIpAddr) == false)
		{
			Console.Write("Error: Could not convert Interface number 0x{0:X} to IP address.\n", dwCurrIndex);
			return;
		}

		Console.Write(" {0,15} {1,16} {2,16} {3,16} {4,7}\n",
			pIpRouteTable.table[i].dwForwardDest,
			pIpRouteTable.table[i].dwForwardMask,
			pIpRouteTable.table[i].dwForwardNextHop,
			szIpAddr,
			pIpRouteTable.table[i].dwForwardMetric1);
	}
}

//----------------------------------------------------------------------------
// Inputs: pIpAddrTable is the IP address table
//         dwIndex is the Interface Number
// Output: If it returns true, str contains the ip address of the interface
//----------------------------------------------------------------------------
bool InterfaceIdxToInterfaceIp(in MIB_IPADDRTABLE pIpAddrTable, uint dwIndex, out string str)
{
	str = string.Empty;
	for (uint dwIdx = 0; dwIdx < pIpAddrTable.dwNumEntries; dwIdx++)
	{
		if (dwIndex == pIpAddrTable.table[dwIdx].dwIndex)
		{
			str = pIpAddrTable.table[dwIdx].dwAddr.ToString();
			return true;
		}
	}
	return false;
}

//----------------------------------------------------------------------------
// Inputs: pIpAddrTable is the IP address table
// str is the Interface Ip address in dotted decimal format
// Output: If it returns true, dwIndex contains the interface index number
// and dwMask contains the corresponding subnet mask.
//----------------------------------------------------------------------------
bool InterfaceIpToIdxAndMask(in MIB_IPADDRTABLE pIpAddrTable, string str, out uint dwIndex, out uint dwMask)
{
	dwIndex = dwMask = 0;

	var dwIfIpAddr = inet_addr(str);
	if (dwIfIpAddr == IN_ADDR.INADDR_NONE)
		return false;

	for (uint dwIdx = 0; dwIdx < pIpAddrTable.dwNumEntries; dwIdx++)
	{
		if (dwIfIpAddr == pIpAddrTable.table[dwIdx].dwAddr)
		{
			dwIndex = pIpAddrTable.table[dwIdx].dwIndex;
			dwMask = pIpAddrTable.table[dwIdx].dwMask;
			return true;
		}
	}
	return false;
}

//----------------------------------------------------------------------------
// If returned status is NO_ERROR, then pIpAddrTable points to a Ip Address
// table.
//----------------------------------------------------------------------------
Win32Error MyGetIpAddrTable(out MIB_IPADDRTABLE? pIpAddrTable, bool fOrder = false)
{
	try
	{
		// query for buffer size needed
		pIpAddrTable = GetIpAddrTable(fOrder);
		Console.WriteLine("No error");
		return Win32Error.NO_ERROR;
	}
	catch (Exception ex)
	{
		pIpAddrTable = null;
		return Win32Error.FromException(ex);
	}
}