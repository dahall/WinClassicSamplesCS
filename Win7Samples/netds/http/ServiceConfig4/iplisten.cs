namespace ServiceConfig4;

internal partial class Program
{
	public static Win32Error DoIpListen(string[] args, HTTPCFG_TYPE Type)
	{
		string pIp = default;

		for (var i = 0; args.Length >= i+2 && args[i][0] is '-' or '/'; i += 2)
		{
			switch (char.ToUpper(args[i][1]))
			{
				case 'I':
					pIp = args[i+1];
					break;

				default:
					Console.Write("{0} is not a valid command. ", args[i]);

					return Win32Error.ERROR_INVALID_PARAMETER;
			}
		}

		switch (Type)
		{
			case HTTPCFG_TYPE.HttpCfgTypeSet:
				return DoIpSet(pIp);

			case HTTPCFG_TYPE.HttpCfgTypeQuery:
				return DoIpQuery();

			case HTTPCFG_TYPE.HttpCfgTypeDelete:
				return DoIpDelete(pIp);

			default:
				Console.Write("{0} is not a valid command. ", args[0]);
				return Win32Error.ERROR_INVALID_PARAMETER;
		}
	}

	private static Win32Error DoIpDelete([In, Optional] string pIp)
	{
		if (GetAddress(pIp, out SOCKADDR TempSockAddr).Failed)
		{
			Console.Write("{0} is not a valid IP address. ", pIp);
			return Win32Error.ERROR_INVALID_PARAMETER;
		}

		Win32Error Status = HttpDeleteServiceConfiguration(new HTTP_SERVICE_CONFIG_IP_LISTEN_PARAM(TempSockAddr));

		Console.Write("HttpDeleteServiceConfiguration completed with {0}", Status);
		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Queries for a URL ACL entry.
	Arguments:
	None.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoIpQuery()
	{
		Win32Error Status = HttpQueryServiceConfiguration<HTTP_SERVICE_CONFIG_IP_LISTEN_QUERY>(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigIPListenList, out var pOutput);
		if (Status.Succeeded)
			PrintIpListenRecords(pOutput);

		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Sets an IP Listen entry
	Arguments:
	pIp - IP address to set.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoIpSet([In, Optional] string pIp)
	{
		Win32Error Status;

		// convert IP to a SOCKADDR.
		if (GetAddress(pIp, out SOCKADDR TempSockAddr).Failed)
		{
			Console.Write("{0} is not a valid IP address.", pIp);
			return Win32Error.ERROR_INVALID_PARAMETER;
		}

		// Call the API
		Status = HttpSetServiceConfiguration(new HTTP_SERVICE_CONFIG_IP_LISTEN_PARAM(TempSockAddr));

		Console.Write("HttpSetServiceConfiguration completed with {0}", Status);

		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Prints a record in the IP Listen store.
	Arguments:
	pOutput - A pointer to HTTP_SERVICE_CONFIG_URLACL_SET
	Return Value:
	None.
	--***************************************************************************/
	private static void PrintIpListenRecords(in HTTP_SERVICE_CONFIG_IP_LISTEN_QUERY pListenQuery)
	{
		for (var i = 0; i<pListenQuery.AddrCount; i++)
		{
			// Convert address to string.

			string IpAddr;
			if (pListenQuery.AddrList[i].ss_family == ADDRESS_FAMILY.AF_INET)
			{
				IpAddr = ((SOCKADDR_IN)pListenQuery.AddrList[i]).ToString();
			}
			else if (pListenQuery.AddrList[i].ss_family == ADDRESS_FAMILY.AF_INET6)
			{
				IpAddr = ((SOCKADDR_IN6)pListenQuery.AddrList[i]).ToString();
			}
			else
			{
				break;
			}

			Console.Write("IP :{0}.", IpAddr);
			Console.Write("------------------------------------------------------------------------------");
		}
	}
}