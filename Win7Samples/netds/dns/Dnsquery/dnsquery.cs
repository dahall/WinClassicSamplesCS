using Vanara.Extensions;
using Vanara.InteropServices;

using static Vanara.PInvoke.DnsApi;
using static Vanara.PInvoke.Ws2_32;

IN_ADDR? dnsSvr = default;//pinter to IP4_ARRAY structure
string pOwnerName = default; //owner name to be queried
DNS_TYPE wType = 0; //Type of the record to be queried

if (args.Length is 4 or 6)
{
	for (int i = 0; i < args.Length; i++)
	{
		if (args[i][0] is '-' or '/')
		{
			switch (char.ToLower(args[i][1]))
			{
				case 'n':
					pOwnerName = args[++i];
					break;

				case 't':
					if (0 == string.Compare(args[i+1], "A", true))
						wType = DNS_TYPE.DNS_TYPE_A; //Query host records to resolve a name
					else if (0 == string.Compare(args[i+1], "PTR", true))
						wType = DNS_TYPE.DNS_TYPE_PTR; //Query PTR records to resovle an IP address
					else
						Usage();
					i++;
					break;

				case 's':
					// Allocate memory for IP4_ARRAY structure
					if (args[++i] is not null)
					{
						dnsSvr = new IN_ADDR(inet_addr(args[i]));
						if (dnsSvr.Value == IN_ADDR.INADDR_NONE || DnsValidateServerStatus(new SOCKADDR(dnsSvr.Value), null, out var stat).Failed || stat != DnsServerStatus.ERROR_SUCCESS)
						{
							Console.Write("Invalid DNS server IP address \n");
							Usage();
						}
					}
					break;

				default:
					Usage();
					break;
			}
		}
		else
			Usage();
	}
	if (wType == DNS_TYPE.DNS_TYPE_PTR)
	{
		var lookup = new IN_ADDR(inet_addr(pOwnerName));
		if (lookup == IN_ADDR.INADDR_NONE)
		{
			Console.Write("Invalid IP address\n");
			Usage();
		}
		pOwnerName = $"{lookup}.in-addr.arpa";
	}
}
else
	Usage();

// Calling function DnsQuery_A() to query Host or PTR records
using SafeCoTaskMemStruct<IP4_ARRAY> pSrvList = dnsSvr.HasValue ? new(new IP4_ARRAY(dnsSvr.Value)) : SafeCoTaskMemStruct<IP4_ARRAY>.Null;
var status = DnsQuery(pOwnerName, //pointer to OwnerName
	wType, //Type of the record to be queried
	pSrvList.HasValue ? DNS_QUERY_OPTIONS.DNS_QUERY_BYPASS_CACHE : 0, // Bypasses the resolver cache on the lookup.
	pSrvList, //contains DNS server IP address
	out var pDnsRecord); //Resource record comprising the response

if (status.Failed)
{
	if (wType == DNS_TYPE.DNS_TYPE_A)
		Console.Write("Failed to query the host record for {0} and the error is {1} \n", pOwnerName, status);
	else
		Console.Write("Failed to query the PTR record and the error is {0} \n", status);
}
else
{
	if (wType == DNS_TYPE.DNS_TYPE_A)
	{
		//convert the Internet network address into a string
		//in Internet standard dotted format.
		var ipaddrs = pDnsRecord.Select(r => ((DNS_A_DATA)r.Data).IpAddress).ToArray();
		Console.Write("The IP addresses of the host {0} are:\n{1}\n", pOwnerName, string.Join('\n', ipaddrs));
	}
	else
	{
		Console.Write("The host name is {0} \n", ((DNS_PTR_DATA)pDnsRecord.First().Data).pNameHost);
	}
}

static void Usage()
{
	Console.Error.Write("Usage\nDnsquery.exe -n [OwnerName] -t [Type] -s [DnsServerIp]\n");
	Console.Error.Write("Where:\n\t\"OwnerName\" is name of the owner of the record set being queried\n");
	Console.Error.Write("\t\"Type\" is the type of record set to be queried A or PTR\n");
	Console.Error.Write("\t\"DnsServerIp\"is the IP address of DNS server (in dotted decimal notation)");
	Console.Error.Write("to which the query should be sent\n");
	Environment.Exit(1);
}