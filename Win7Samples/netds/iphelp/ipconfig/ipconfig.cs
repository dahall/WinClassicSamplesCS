using static Vanara.PInvoke.IpHlpApi;

try
{
	FIXED_INFO pFixedInfo = GetNetworkParams();
	Console.Write("\tHost Name . . . . . . . . . : {0}\n", pFixedInfo.HostName);
	Console.Write("\tDNS Servers . . . . . . . . : \n");
	foreach (var s in pFixedInfo.DnsServers)
	{
		Console.Write("{0,51}\n", s.IpAddress.String);
	}

	Console.Write("\tNode Type . . . . . . . . . : ");
	switch (pFixedInfo.NodeType)
	{
		case NetBiosNodeType.BROADCAST_NODETYPE:
			Console.Write("{0}\n", "Broadcast");
			break;
		case NetBiosNodeType.PEER_TO_PEER_NODETYPE:
			Console.Write("{0}\n", "Peer to peer");
			break;
		case NetBiosNodeType.MIXED_NODETYPE:
			Console.Write("{0}\n", "Mixed");
			break;
		case NetBiosNodeType.HYBRID_NODETYPE:
			Console.Write("{0}\n", "Hybrid");
			break;
		default:
			Console.Write("\n");
			break;
	}

	Console.Write("\tNetBIOS Scope ID. . . . . . : {0}\n", pFixedInfo.ScopeId);
	Console.Write("\tIP Routing Enabled. . . . . : {0}\n", pFixedInfo.EnableRouting);
	Console.Write("\tWINS Proxy Enabled. . . . . : {0}\n", pFixedInfo.EnableProxy);
	Console.Write("\tNetBIOS Resolution Uses DNS : {0}\n", pFixedInfo.EnableDns);
}
catch (Exception ex)
{
	Console.Write("GetNetworkParams failed with error {0}\n", ex.HResult);
	return;
}

//
// Enumerate all of the adapter specific information using the IP_ADAPTER_INFO structure.
// Note: IP_ADAPTER_INFO contains a linked list of adapter entries.
//
#pragma warning disable CS0618 // Type or member is obsolete
foreach (IP_ADAPTER_INFO pAdapt in GetAdaptersInfo())
{
	switch ((MIB_IFTYPE)pAdapt.Type)
	{
		case MIB_IFTYPE.MIB_IF_TYPE_ETHERNET:
			Console.Write("\nEthernet adapter ");
			break;
		case MIB_IFTYPE.MIB_IF_TYPE_TOKENRING:
			Console.Write("\nToken Ring adapter ");
			break;
		case MIB_IFTYPE.MIB_IF_TYPE_FDDI:
			Console.Write("\nFDDI adapter ");
			break;
		case MIB_IFTYPE.MIB_IF_TYPE_PPP:
			Console.Write("\nPPP adapter ");
			break;
		case MIB_IFTYPE.MIB_IF_TYPE_LOOPBACK:
			Console.Write("\nLoopback adapter ");
			break;
		case MIB_IFTYPE.MIB_IF_TYPE_SLIP:
			Console.Write("\nSlip adapter ");
			break;
		case MIB_IFTYPE.MIB_IF_TYPE_OTHER:
		default:
			Console.Write("\nOther adapter ");
			break;
	}
	Console.Write("{0}:\n\n", pAdapt.AdapterName);

	Console.Write("\tDescription . . . . . . . . : {0}\n", pAdapt.AdapterDescription);

	Console.Write("\tPhysical Address. . . . . . : ");

	Console.WriteLine(string.Join('-', pAdapt.Address.Select(b => $"{b:X2}")));

	Console.Write("\tDHCP Enabled. . . . . . . . : {0}\n", (pAdapt.DhcpEnabled ? "yes" : "no"));

	foreach (var pAddrStr in pAdapt.IpAddresses)
	{
		Console.Write("\tIP Address. . . . . . . . . : {0}\n", pAddrStr.IpAddress.String);
		Console.Write("\tSubnet Mask . . . . . . . . : {0}\n", pAddrStr.IpMask.String);
	}

	Console.Write("\tDefault Gateway . . . . . . : \n");
	foreach (var pAddrStr in pAdapt.Gateways)
	{
		Console.Write("{0,51}\n", pAddrStr.IpAddress.String);
	}

	Console.Write("\tDHCP Server . . . . . . . . : {0}\n", pAdapt.DhcpServer.IpAddress.String);
	Console.Write("\tPrimary WINS Server . . . . : {0}\n", pAdapt.PrimaryWinsServer.IpAddress.String);
	Console.Write("\tSecondary WINS Server . . . : {0}\n", pAdapt.SecondaryWinsServer.IpAddress.String);

	// Display coordinated universal time - GMT 
	Console.Write("\tLease Obtained. . . . . . . : {0}\n", pAdapt.LeaseObtained);
	Console.Write("\tLease Expires . . . . . . . : {0}", pAdapt.LeaseExpires);
}
#pragma warning restore CS0618 // Type or member is obsolete

public enum MIB_IFTYPE
{
	MIB_IF_TYPE_OTHER = 1,
	MIB_IF_TYPE_ETHERNET = 6,
	MIB_IF_TYPE_TOKENRING = 9,
	MIB_IF_TYPE_FDDI = 15,
	MIB_IF_TYPE_PPP = 23,
	MIB_IF_TYPE_LOOPBACK = 24,
	MIB_IF_TYPE_SLIP = 28
}