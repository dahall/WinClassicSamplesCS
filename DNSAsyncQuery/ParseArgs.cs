using Vanara.PInvoke;
using static Vanara.PInvoke.DnsApi;

namespace DNSAsyncQuery;

static partial class DnsQueryEx
{
	private static DNS_QUERY_OPTIONS ExtractUserFriendlyQueryOptions(string QueryOptions) => Enum.TryParse<DNS_QUERY_OPTIONS>(QueryOptions, out var opt) ? opt : (DNS_QUERY_OPTIONS)uint.Parse(QueryOptions);

	private static DNS_TYPE ExtractUserFriendlyQueryType(string QueryType) => Enum.TryParse<DNS_TYPE>("DNS_TYPE_" + QueryType, out var type) ? type : 0;

	private static Win32Error ParseArguments(string[] args, out string QueryName, out DNS_TYPE QueryType, out DNS_QUERY_OPTIONS QueryOptions, out string ServerIp)
	{
		int CurrentIndex = 0;
		bool ArgContainsQueryName = false;
		Win32Error Error = Win32Error.ERROR_INVALID_PARAMETER;

		QueryType = DNS_TYPE.DNS_TYPE_A; // default type
		QueryOptions = 0; // default query options
		QueryName = string.Empty;
		ServerIp = string.Empty;

		while (CurrentIndex < args.Length)
		{
			// Query Name:

			if (StringComparer.OrdinalIgnoreCase.Compare(args[CurrentIndex], "-q") == 0)
			{
				CurrentIndex++;
				if (CurrentIndex < args.Length)
				{
					QueryName = args[CurrentIndex];
					ArgContainsQueryName = true;
				}
				else
				{
					goto exit;
				}
			}

			// Query Type
			else if (StringComparer.OrdinalIgnoreCase.Compare(args[CurrentIndex], "-t") == 0)
			{
				CurrentIndex++;
				if (CurrentIndex < args.Length)
				{
					QueryType = ExtractUserFriendlyQueryType(args[CurrentIndex]);
					if (QueryType == 0)
					{
						goto exit;
					}
				}
				else
				{
					goto exit;
				}
			}

			// Query Options
			else if (StringComparer.OrdinalIgnoreCase.Compare(args[CurrentIndex], "-o") == 0)
			{
				CurrentIndex++;
				if (CurrentIndex < args.Length)
				{
					try { QueryOptions = ExtractUserFriendlyQueryOptions(args[CurrentIndex]); }
					catch { goto exit; }
				}
				else
				{
					goto exit;
				}
			}

			// Server List
			else if (StringComparer.OrdinalIgnoreCase.Compare(args[CurrentIndex], "-s") == 0)
			{
				CurrentIndex++;
				if (CurrentIndex < args.Length)
				{
					ServerIp = args[CurrentIndex];
				}
				else
				{
					goto exit;
				}
			}
			else
			{
				goto exit;
			}
			CurrentIndex++;
		}

		if (ArgContainsQueryName)
		{
			Error = Win32Error.ERROR_SUCCESS;
		}

		exit:
		if (Error.Failed)
		{
			PrintHelp();
		}

		return Error;
	}

	private static void PrintHelp()
	{
		Console.Write("Usage: DnsQuery -q <QueryName> [-t QueryType] [-s DnsServerIP] [-o QueryOptions]\n" +
			"<QueryName>\t\tInput query Name\n" +
			"<QueryType>\t\tInput query type: A, PTR, NS, AAAA, TXT....\n" +
			"<DnsServerIP>\t\tDNS Server IP address\n" +
			"<QueryOptions>\t\tInput query flags (use combination of following numerics or one of the below option string):\n" +
			"\t\t\t\t0x0 DNS_QUERY_STANDARD\n" +
			"\t\t\t\t0x1 DNS_QUERY_ACCEPT_TRUNCATED_RESPONSE\n" +
			"\t\t\t\t0x2 DNS_QUERY_USE_TCP_ONLY\n" +
			"\t\t\t\t0x4 DNS_QUERY_NO_RECURSION\n" +
			"\t\t\t\t0x8 DNS_QUERY_BYPASS_CACHE\n" +
			"\t\t\t\t0x10 DNS_QUERY_NO_WIRE_QUERY\n" +
			"\t\t\t\t0x20 DNS_QUERY_NO_LOCAL_NAME\n" +
			"\t\t\t\t0x40 DNS_QUERY_NO_HOSTS_FILE\n" +
			"\t\t\t\t0x100 DNS_QUERY_WIRE_ONLY\n" +
			"\t\t\t\t0x400 DNS_QUERY_MULTICAST_ONLY\n" +
			"\t\t\t\t0x800 DNS_QUERY_NO_MULTICAST\n" +
			"\t\t\t\t0x1000 DNS_QUERY_TREAT_AS_FQDN\n" +
			"\t\t\t\t0x200000 DNS_QUERY_DISABLE_IDN_ENCODING\n" +
			"\n");
	}
}