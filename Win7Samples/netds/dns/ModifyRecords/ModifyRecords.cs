using Vanara.InteropServices;

using static Vanara.PInvoke.DnsApi;
using static Vanara.PInvoke.Ws2_32;

DNS_RECORD myDnsRecord = new(); //pointer to DNS_RECORD structure
string? pOwnerName = default; //owner name and the data for CNAME resource record
IN_ADDR? dnsSvr = default;//pinter to IP4_ARRAY structure

if (args.Length > 7)
{
	for (int i = 0; i < args.Length; i++)
	{
		if (args[i][0] is '-' or '/')
		{
			switch (char.ToLower(args[i][1]))
			{
				case 'n':
					pOwnerName = args[++i];
					myDnsRecord.pName = pOwnerName; //copy the Owner name information 
					break;

				case 't':
					if (0 == string.Compare(args[i+1], "A", true))
						myDnsRecord.wType = DNS_TYPE.DNS_TYPE_A; //add host records
					else if (0 == string.Compare(args[i+1], "CNAME", true))
						myDnsRecord.wType = DNS_TYPE.DNS_TYPE_CNAME; //add CNAME records
					else
						Usage();
					i++;
					break;

				case 'l':
					myDnsRecord.dwTtl = uint.Parse(args[++i]); // time to live value in seconds
					break;

				case 'd':
					if (myDnsRecord.wType == DNS_TYPE.DNS_TYPE_A)
					{
						myDnsRecord.wDataLength = (ushort)Marshal.SizeOf(typeof(DNS_A_DATA)); //data structure for A records
						var HostipAddress = inet_addr(args[++i]);
						myDnsRecord.Data = new DNS_A_DATA() { IpAddress = HostipAddress }; //convert string to proper address
						if (HostipAddress == IN_ADDR.INADDR_NONE)
						{
							Console.Write("Invalid IP address in A record data \n");
							Usage();
						}
						break;
					}
					else
					{
						myDnsRecord.wDataLength = (ushort)Marshal.SizeOf(typeof(DNS_PTR_DATA)); //data structure for CNAME records
						myDnsRecord.Data = new DNS_PTR_DATA { pNameHost = args[++i] };
						break;
					}
				case 's':
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
}
else
{
	Usage();
}

// Calling function DNSModifyRecordsInSet_A to add Host or CNAME records 
using SafeCoTaskMemStruct<IP4_ARRAY> pSrvList = dnsSvr.HasValue ? new(new IP4_ARRAY(dnsSvr.Value)) : SafeCoTaskMemStruct<IP4_ARRAY>.Null;
var status = DnsModifyRecordsInSet(myDnsRecord, //pointer to DNS_RECORD
	default,
	DNS_UPDATE.DNS_UPDATE_SECURITY_USE_DEFAULT, //do not attempt secure dynamic updates
	default, //use default credentials
	pSrvList, //contains DNS server IP address
	default); //reserved for future use

if (status.Failed)
{
	if (myDnsRecord.wType == DNS_TYPE.DNS_TYPE_A)
		Console.Write("Failed to add the host record for {0} and the error is {1} \n", pOwnerName, status);
	else
		Console.Write("Failed to add the Cname record for {0} and the error is {1} \n", pOwnerName, status);
}
else
{
	if (myDnsRecord.wType == DNS_TYPE.DNS_TYPE_A)
		Console.Write("Successfully added the host record for {0} \n", pOwnerName);
	else
		Console.Write("Successfully added the Cname record for {0} \n", pOwnerName);
}

static void Usage()
{
	Console.Error.Write("Usage\nModifyRecords.exe -n [OwnerName] -t [Type] -l [Ttl] -d [Data] -s [DnsServerIp]\n");
	Console.Error.Write("Where:\n\tOwnerName is the owner field to be added\n");
	Console.Error.Write("\tType is the type of resource record to be added A or CNAME\n");
	Console.Error.Write("\tData is the data corresponding to RR to be added\n");
	Console.Error.Write("\tTtl is the time to live value in seconds \n");
	Console.Error.Write("\tDnsServerIp is the ipaddress of DNS server (in dotted decimal notation)\n");
	Environment.Exit(1);
}