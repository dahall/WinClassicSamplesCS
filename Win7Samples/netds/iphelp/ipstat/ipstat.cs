using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

const string programName = "IpStat";

ushort wVersionRequested = Macros.MAKEWORD(1, 1);
using var wsa = SafeWSA.DemandVersion(wVersionRequested);

if (args.Length < 1 || args[0][0] != '-')
	Usage(programName);
if (args[0].Length > 2)
	Usage(programName);

switch (args[0][1])
{
	case 'p':
		// Print connection table
		if (args.Length == 2)
			DoGetConnTable(args[1]);
		else
			Usage(programName);
		break;

	case 's':
		// show statistics
		if (args.Length == 4 && args[1][1] == 'p')
			DoGetStat(args[2]); // Get stat for a specific protocol
		else if (args.Length == 1)
			DoGetStat(); // Get stat for all protocols
		else
			Usage(programName);
		break;

	default:
		// help
		Usage(programName);
		break;
}

return 0;

void Usage(string pszProgramName)
{
	Console.Write("Manipulates IP Statistics.\n\n");

	Console.Write("{0} -p proto Shows connections for the protocol specified\n", pszProgramName);
	Console.Write(" by proto, proto may be tcp or udp.\n");
	Console.Write("{0} -s [-p proto] Displays per-protocol statistics.\n", pszProgramName);
	Console.Write(" By default, statistics are shown for\n");
	Console.Write(" IP, ICMP, TCP and UDP; the -p option\n");
	Console.Write(" may be used to specify a subset of the default.\n");

	Console.Write("Examples:\n\n");

	Console.Write("> IpStat -p tcp\n");
	Console.Write("> IpStat -s\n");

	Environment.Exit(1);
}

void DoGetConnTable(string pszProto)
{
	Win32Error dwStatus;
	if (_strnicmp(pszProto, "tcp", 3) == 0)
	{
		//Print Tcp Connnection Table
		dwStatus = MyGetTcpTable(out var pTcpTable, true);
		if (dwStatus.Failed)
		{
			Console.Write("Ipstat: Couldn't get tcp connection table.\n");
			return;
		}
		else
		{
			DumpTcpTable(pTcpTable!);
		}
	}
	else if (_strnicmp(pszProto, "udp", 3) == 0)
	{
		//Print Udp Table
		dwStatus = MyGetUdpTable(out var pUdpTable, true);
		if (dwStatus.Failed)
		{
			Console.Write("Ipstat: Couldn't get udp table.\n");
			return;
		}
		else
		{
			DumpUdpTable(pUdpTable!);
		}
	}
	else
		Usage(programName);
}

void DoGetStat(string? pszProto = null)
{
	if (pszProto is null)
	{
		// by default, display all statistics
		{
			if (MyGetIpStatistics(out var pIpStats).Failed)
				Console.Write("IpStat: error in getting ip statistics.\n");
			else
				PrintIpStats(pIpStats);
		}
		{
			if (MyGetIcmpStatistics(out var pIcmpStats).Failed)
				Console.Write("IpStat: error in getting icmp statistics.\n");
			else
				PrintIcmpStats(pIcmpStats.stats);
		}
		{
			if (MyGetTcpStatistics(out var pTcpStats).Failed)
				Console.Write("IpStat: error in getting tcp statistics.\n");
			else
				PrintTcpStats(pTcpStats);
		}
		{
			if (MyGetUdpStatistics(out var pUdpStats).Failed)
				Console.Write("IpStat: error in getting udp statistics.\n");
			else
				PrintUdpStats(pUdpStats);
		}
	}
	// make sure the protocol specified was ip not ipx or some other string
	else if (pszProto.Length == 2 && _strnicmp(pszProto, "ip", 2) == 0)
	{
		if (MyGetIpStatistics(out var pIpStats).Failed)
			Console.Write("IpStat: error in getting ip statistics.\n");
		else
			PrintIpStats(pIpStats);
	}

	else if (_strnicmp(pszProto, "icmp", 4) == 0)
	{
		if (MyGetIcmpStatistics(out var pIcmpStats).Failed)
			Console.Write("IpStat: error in getting icmp statistics.\n");
		else
			PrintIcmpStats(pIcmpStats.stats);
	}

	else if (_strnicmp(pszProto, "tcp", 3) == 0)
	{
		if (MyGetTcpStatistics(out var pTcpStats).Failed)
			Console.Write("IpStat: error in getting tcp statistics.\n");
		else
			PrintTcpStats(pTcpStats);
	}

	else if (_strnicmp(pszProto, "udp", 3) == 0)
	{
		if (MyGetUdpStatistics(out var pUdpStats).Failed)
			Console.Write("IpStat: error in getting udp statistics.\n");
		else
			PrintUdpStats(pUdpStats);
	}
	else
		Console.Write("IpStat: no available statistics for {0}.\n", pszProto);

}

void DumpTcpTable(in MIB_TCPTABLE pTcpTable)
{
	Console.Write("TCP TABLE\n");
	Console.Write("{0,20} {1,10} {2,20} {3,10} {4}\n", "Loc Addr", "Loc Port", "Rem Addr", "Rem Port", "State");
	for (uint i = 0; i < pTcpTable.dwNumEntries; ++i)
	{
		var strState = pTcpTable.table[i].dwState.IsValid() ? pTcpTable.table[i].dwState.ToString().Substring(14) : "Error: unknown state!";
		IN_ADDR inadLocal = new(pTcpTable.table[i].dwLocalAddr);

		uint dwRemotePort = pTcpTable.table[i].dwState == MIB_TCP_STATE.MIB_TCP_STATE_LISTEN ? pTcpTable.table[i].dwRemotePort : 0;

		IN_ADDR inadRemote = new(pTcpTable.table[i].dwRemoteAddr);

		string szLocalIp = inet_ntoa(inadLocal).ToString();
		string szRemIp = inet_ntoa(inadRemote).ToString();
		Console.Write("{0,20} {1,10} {2,20} {3,10} {4}\n",
			szLocalIp, ntohs((ushort)(0x0000FFFF & pTcpTable.table[i].dwLocalPort)),
			szRemIp, ntohs((ushort)(0x0000FFFF & dwRemotePort)),
			strState);
	}
}

void DumpUdpTable(in MIB_UDPTABLE pUdpTable)
{
	IN_ADDR inadLocal;
	Console.Write("UDP TABLE\n");
	Console.Write("{0,20} {1,10}\n", "Loc Addr", "Loc Port");
	for (uint i = 0; i < pUdpTable.dwNumEntries; ++i)
	{
		inadLocal.S_addr = pUdpTable.table[i].dwLocalAddr;

		Console.Write("{0,20} {1,10} \n",
			inet_ntoa(inadLocal), ntohs((ushort)(0x0000FFFF & pUdpTable.table[i].dwLocalPort)));
	}
}

//----------------------------------------------------------------------------
// Wrapper to GetTcpTable()
//----------------------------------------------------------------------------
Win32Error MyGetTcpTable(out MIB_TCPTABLE? pTcpTable, bool fOrder)
{
	try
	{
		// query for buffer size needed
		pTcpTable = GetTcpTable(fOrder);
		return 0;
	}
	catch (Exception e)
	{
		pTcpTable = default;
		return Win32Error.FromException(e);
	}
}

//----------------------------------------------------------------------------
// Wrapper to GetUdpTable()
//----------------------------------------------------------------------------
Win32Error MyGetUdpTable(out MIB_UDPTABLE? pUdpTable, bool fOrder)
{
	try
	{
		// query for buffer size needed
		pUdpTable = GetUdpTable(fOrder);
		return 0;
	}
	catch (Exception e)
	{
		pUdpTable = default;
		return Win32Error.FromException(e);
	}
}

Win32Error MyGetIpStatistics(out MIB_IPSTATS pIpStats) => GetIpStatistics(out pIpStats);

Win32Error MyGetIcmpStatistics(out MIB_ICMP pIcmpStats) => GetIcmpStatistics(out pIcmpStats);

Win32Error MyGetTcpStatistics(out MIB_TCPSTATS pTcpStats) => GetTcpStatistics(out pTcpStats);

Win32Error MyGetUdpStatistics(out MIB_UDPSTATS pUdpStats) => GetUdpStatistics(out pUdpStats);

void PrintIpStats(in MIB_IPSTATS pStats)
{
	Console.Write("\nIP Statistics:\n");

	Console.Write("" +
		"  dwForwarding       = {0}\n" +
		"  dwDefaultTTL       = {1}\n" +
		"  dwInReceives       = {2}\n" +
		"  dwInHdrErrors      = {3}\n" +
		"  dwInAddrErrors     = {4}\n" +
		"  dwForwDatagrams    = {5}\n" +
		"  dwInUnknownProtos  = {6}\n" +
		"  dwInDiscards       = {7}\n" +
		"  dwInDelivers       = {8}\n" +
		"  dwOutRequests      = {9}\n" +
		"  dwRoutingDiscards  = {10}\n" +
		"  dwOutDiscards      = {11}\n" +
		"  dwOutNoRoutes      = {12}\n" +
		"  dwReasmTimeout     = {13}\n" +
		"  dwReasmReqds       = {14}\n" +
		"  dwReasmOks         = {15}\n" +
		"  dwReasmFails       = {16}\n" +
		"  dwFragOks          = {17}\n" +
		"  dwFragFails        = {18}\n" +
		"  dwFragCreates      = {19}\n" +
		"  dwNumIf            = {20}\n" +
		"  dwNumAddr          = {21}\n" +
		"  dwNumRoutes        = {22}\n",
		pStats.Forwarding,
		pStats.dwDefaultTTL,
		pStats.dwInReceives,
		pStats.dwInHdrErrors,
		pStats.dwInAddrErrors,
		pStats.dwForwDatagrams,
		pStats.dwInUnknownProtos,
		pStats.dwInDiscards,
		pStats.dwInDelivers,
		pStats.dwOutRequests,
		pStats.dwRoutingDiscards,
		pStats.dwOutDiscards,
		pStats.dwOutNoRoutes,
		pStats.dwReasmTimeout,
		pStats.dwReasmReqds,
		pStats.dwReasmOks,
		pStats.dwReasmFails,
		pStats.dwFragOks,
		pStats.dwFragFails,
		pStats.dwFragCreates,
		pStats.dwNumIf,
		pStats.dwNumAddr,
		pStats.dwNumRoutes);
}

void PrintIcmpStats(in MIBICMPINFO pStats)
{
	Console.Write("\n{0,-20} {1,10} {2,10}\n", "ICMP Statistics", "[In]", "[Out]");
	Console.Write("{0,-20} {1,10} {2,10}\n", "---------------", "------", "------");
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwMsgs", pStats.icmpInStats.dwMsgs, pStats.icmpOutStats.dwMsgs);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwErrors", pStats.icmpInStats.dwErrors, pStats.icmpOutStats.dwErrors);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwDestUnreachs", pStats.icmpInStats.dwDestUnreachs, pStats.icmpOutStats.dwDestUnreachs);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwTimeExcds", pStats.icmpInStats.dwTimeExcds, pStats.icmpOutStats.dwTimeExcds);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwParmProbs", pStats.icmpInStats.dwParmProbs, pStats.icmpOutStats.dwParmProbs);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwSrcQuenchs", pStats.icmpInStats.dwSrcQuenchs, pStats.icmpOutStats.dwSrcQuenchs);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwRedirects", pStats.icmpInStats.dwRedirects, pStats.icmpOutStats.dwRedirects);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwEchos", pStats.icmpInStats.dwEchos, pStats.icmpOutStats.dwEchos);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwEchoReps", pStats.icmpInStats.dwEchoReps, pStats.icmpOutStats.dwEchoReps);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwTimestamps", pStats.icmpInStats.dwTimestamps, pStats.icmpOutStats.dwTimestamps);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwTimestampReps", pStats.icmpInStats.dwTimestampReps, pStats.icmpOutStats.dwTimestampReps);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwAddrMasks", pStats.icmpInStats.dwAddrMasks, pStats.icmpOutStats.dwAddrMasks);
	Console.Write("{0,-20} {1,10} {2,10}\n", "dwAddrMaskReps", pStats.icmpInStats.dwAddrMaskReps, pStats.icmpOutStats.dwAddrMaskReps);
}

void PrintTcpStats(in MIB_TCPSTATS pStats)
{
	Console.Write("\nTCP Statistics\n");
	Console.Write("" +
		"  dwRtoAlgorithm     = {0}\n" +
		"  dwRtoMin           = {1}\n" +
		"  dwRtoMax           = {2}\n" +
		"  dwMaxConn          = {3}\n" +
		"  dwActiveOpens      = {4}\n" +
		"  dwPassiveOpens     = {5}\n" +
		"  dwAttemptFails     = {6}\n" +
		"  dwEstabResets      = {7}\n" +
		"  dwCurrEstab        = {8}\n" +
		"  dwInSegs           = {9}\n" +
		"  dwOutSegs          = {10}\n" +
		"  dwRetransSegs      = {11}\n" +
		"  dwInErrs           = {12}\n" +
		"  dwOutRsts          = {13}\n" +
		"  dwNumConns         = {14}\n",
		pStats.RtoAlgorithm,
		pStats.dwRtoMin,
		pStats.dwRtoMax,
		pStats.dwMaxConn,
		pStats.dwActiveOpens,
		pStats.dwPassiveOpens,
		pStats.dwAttemptFails,
		pStats.dwEstabResets,
		pStats.dwCurrEstab,
		pStats.dwInSegs,
		pStats.dwOutSegs,
		pStats.dwRetransSegs,
		pStats.dwInErrs,
		pStats.dwOutRsts,
		pStats.dwNumConns);
}

void PrintUdpStats(in MIB_UDPSTATS pStats)
{
	Console.Write("\nUDP Statistics\n");
	Console.Write("" +
		"  dwInDatagrams      = {0}\n" +
		"  dwNoPorts          = {1}\n" +
		"  dwInErrors         = {2}\n" +
		"  dwOutDatagrams     = {3}\n" +
		"  dwNumAddrs         = {4}\n",
		pStats.dwInDatagrams,
		pStats.dwNoPorts,
		pStats.dwInErrors,
		pStats.dwOutDatagrams,
		pStats.dwNumAddrs);
}

int _strnicmp(string s1, string s2, int n) => s1 is null || s1.Length < n ? -1 : string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);