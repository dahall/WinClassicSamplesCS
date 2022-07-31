using System.Text;

namespace ServiceConfig4;

internal partial class Program
{
	private const int MAX_HASH = 20;

	// Public functions.

	/***************************************************************************++
	Routine Description:
	The function that parses parameters specific to SSL
	calls Set, Query or Delete.
	Arguments:
	args.Length - Count of arguments.
	args - Pointer to command line arguments.
	Type - Type of operation to be performed.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	public static Win32Error DoSsl(string[] args, HTTPCFG_TYPE type)
	{
		string pHash = default;
		string pCertStoreName = default;
		string pCtlIdentifier = default;
		string pCtlStoreName = default;
		uint CertCheckMode = 0;
		uint Freshness = 0;
		uint Timeout = 0;
		HTTP_SERVICE_CONFIG_SSL_FLAG Flags = 0;

		string pIp = default;

		for (var i = 0; args.Length >= i+2 && args[i][0] is '-' or '/'; i += 2)
		{
			switch (char.ToUpper(args[i][1]))
			{
				case 'I':
					pIp = args[i + 1];
					break;

				case 'C':
					pCertStoreName = args[i + 1];
					break;

				case 'N':
					pCtlStoreName = args[i + 1];
					break;

				case 'T':
					pCtlIdentifier = args[i + 1];
					break;

				case 'M':
					CertCheckMode = uint.Parse(args[i + 1]);
					break;

				case 'R':
					Freshness = uint.Parse(args[i + 1]);
					break;

				case 'X':
					Timeout = uint.Parse(args[i + 1]);
					break;

				case 'F':
					Flags = (HTTP_SERVICE_CONFIG_SSL_FLAG)uint.Parse(args[i + 1]);
					break;

				case 'H':
					pHash = args[i + 1];
					break;

				default:
					Console.Write("{0} is an invalid command", args[i]);
					return Win32Error.ERROR_INVALID_PARAMETER;
			}
		}

		var AppGuid = Guid.NewGuid();

		switch (type)
		{
			case HTTPCFG_TYPE.HttpCfgTypeSet:
				return DoSslSet(pIp,
					AppGuid,
					pHash,
					CertCheckMode,
					Freshness,
					Timeout,
					Flags,
					pCtlIdentifier,
					pCtlStoreName,
					pCertStoreName);

			case HTTPCFG_TYPE.HttpCfgTypeQuery:
				return DoSslQuery(pIp);

			case HTTPCFG_TYPE.HttpCfgTypeDelete:
				return DoSslDelete(pIp);

			default:
				Console.Write("{0} is not a valid command \n", args[0]);
				return Win32Error.ERROR_INVALID_PARAMETER;
		}
	}

	/***************************************************************************++
	Routine Description:
	Deletes a SSL entry.
	Arguments:
	pIP - The IP address of entry to be deleted.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoSslDelete([In, Optional] string pIp)
	{
		// Convert string IP address to a SOCKADDR structure
		if (GetAddress(pIp, out SOCKADDR TempSockAddr).Failed)
		{
			Console.Write("{0} is not a valid IP address.", pIp);
			return Win32Error.ERROR_INVALID_PARAMETER;
		}

		HTTP_SERVICE_CONFIG_SSL_SET SetParam = new();
		SetParam.KeyDesc.pIpPort = TempSockAddr;

		// Call the API.
		Win32Error Status = HttpDeleteServiceConfiguration(SetParam);

		Console.Write("HttpDeleteServiceConfiguration completed with {0}\n", Status);
		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Queries for a SSL entry.
	Arguments:
	pIp - The IP address (if default, then enumerate the store).
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoSslQuery([In, Optional] string pIp)
	{
		Win32Error Status;

		HTTP_SERVICE_CONFIG_SSL_QUERY QueryParam = new();
		if (pIp is not null)
		{
			// if an IP address is specified, we'll covert it to a SOCKADDR and do an exact query.

			if (GetAddress(pIp, out SOCKADDR TempSockAddr).Failed)
			{
				Console.Write("{0} is not a valid IP address.", pIp);
				return Win32Error.ERROR_INVALID_PARAMETER;
			}

			QueryParam.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact;
			QueryParam.KeyDesc.pIpPort = TempSockAddr;
		}
		else
		{
			// We are enumerating all the records in the SSL store.
			QueryParam.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext;
		}

		for (; ; )
		{
			// First, compute the bytes required to enumerate an entry.
			Status = HttpQueryServiceConfiguration(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo, QueryParam, out SafeCoTaskMemStruct<HTTP_SERVICE_CONFIG_SSL_SET> pOutput);

			if (Status.Succeeded)
			{
				// The query succeeded! We'll print the record that we just queried.
				PrintSslRecord(pOutput);

				if (pIp is not null)
				{
					// If we are not enumerating, we are done.
					break;
				}
				else
				{
					// Since we are enumerating, we'll move on to the next record. This is done by incrementing the cursor, till we get Win32Error.ERROR_NO_MORE_ITEMS.
					QueryParam.dwToken++;
				}
			}
			else if (Win32Error.ERROR_NO_MORE_ITEMS == Status && pIp is null)
			{
				// We are enumerating and we have reached the end. This is indicated by a Win32Error.ERROR_NO_MORE_ITEMS error code.

				// This is not a real error, since it is used to indicate that we've finished enumeration.

				Status = 0;
				break;
			}
			else
			{
				// Some other error, so we are done
				Console.Write("HttpQueryServiceConfiguration completed with {0}\n", Status);
				break;
			}
		}

		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Sets a SSL entry.
	Arguments:
	pIP - The IP address.
	pGuid - The Guid
	pHash - Hash of the certificate.
	CertCheckMode - CertCheckMode (Bit Field).
	Freshness - DefaultRevocationFreshnessTime (seconds)
	Timeout - DefaultRevocationUrlRetrievalTimeout
	Flags - DefaultFlags.
	pCtlIdentifier - List of issuers that we want to trust.
	pCtlStoreName - Store name under LOCAL_MACHINE where pCtlIdentifier
	can be found.
	pCertStoreName - Store name under LOCAL_MACHINE where certificate
	can be found.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoSslSet([In, Optional] string pIp,
		[In, Optional] Guid AppGuid,
		[In, Optional] string pHash,
		[In, Optional] uint CertCheckMode,
		[In, Optional] uint Freshness,
		[In, Optional] uint Timeout,
		[In, Optional] HTTP_SERVICE_CONFIG_SSL_FLAG Flags,
		[In, Optional] string pCtlIdentifier,
		[In, Optional] string pCtlStoreName,
		[In, Optional] string pCertStoreName
	)
	{
		using SafeNativeArray<byte> BinaryHash = new(MAX_HASH);

		// Convert the string based IP into a SOCKADDR
		if (GetAddress(pIp, out SOCKADDR TempSockAddr).Failed)
		{
			Console.Write("{0} is not a valid IP address.", pIp);
			return Win32Error.ERROR_INVALID_PARAMETER;
		}

		HTTP_SERVICE_CONFIG_SSL_SET SetParam = new();
		SetParam.KeyDesc.pIpPort = TempSockAddr;

		// Set the Guid. Note that this Guid is hardcoded to 0XAAAABEEF Please use your own Guid for real application
		SetParam.ParamDesc.AppId = AppGuid;

		if (pHash is not null)
		{
			var HashLength = pHash.Length;

			int i, j;
			for (i = 0, j = 0; i<MAX_HASH && HashLength >= 2;)
			{
				CONVERT_WCHAR(pHash[j], out byte n1);
				CONVERT_WCHAR(pHash[j+1], out byte n2);

				BinaryHash[i] = (byte)(((n1<<4) & 0xF0) | (n2 & 0x0F));

				// We've consumed 2 WCHARs
				HashLength -= 2;
				j += 2;

				// and used up one byte in BinaryHash
				i++;
			}

			if (HashLength != 0 || i != MAX_HASH)
			{
				Console.Write("Invalid Hash {0}\n", pHash);
				return Win32Error.ERROR_INVALID_PARAMETER;
			}

			SetParam.ParamDesc.SslHashLength = (uint)i;
			SetParam.ParamDesc.pSslHash = BinaryHash;
		}

		SetParam.ParamDesc.pSslCertStoreName = pCertStoreName;
		SetParam.ParamDesc.pDefaultSslCtlIdentifier = pCtlIdentifier;
		SetParam.ParamDesc.pDefaultSslCtlStoreName = pCtlStoreName;
		SetParam.ParamDesc.DefaultCertCheckMode = CertCheckMode;
		SetParam.ParamDesc.DefaultRevocationFreshnessTime = Freshness;
		SetParam.ParamDesc.DefaultRevocationUrlRetrievalTimeout = Timeout;
		SetParam.ParamDesc.DefaultFlags = Flags;

		Win32Error Status = HttpSetServiceConfiguration(SetParam);

		Console.Write("SetServiceConfiguration completed with Status {0}\n", Status);

		return Status;

		static void CONVERT_WCHAR(char ch, out byte n)
		{
			if (char.IsDigit(ch))
			{
				n = (byte)(ch - '0');
			}
			else if ("ABCDEF".Contains(char.ToUpper(ch)))
			{
				n = (byte)(char.ToUpper(ch) + 10 - 'A');
			}
			else
			{
				n = 0;
				Console.Error.Write("INVALID HASH \n");
			}
		}
	}

	/***************************************************************************++
	Routine Description:
	Prints a record in the SSL store.
	Arguments:
	pOutput - A pointer to HTTP_SERVICE_CONFIG_SSL_SET
	Return Value:
	None.
	--***************************************************************************/
	private static void PrintSslRecord(in HTTP_SERVICE_CONFIG_SSL_SET pSsl)
	{
		// Convert address to string.

		SOCKADDR_IN pSockAddrIn = pSsl.KeyDesc.pIpPort.AsRef<SOCKADDR_IN>();
		uint dwSockAddrLength;
		if (pSockAddrIn.sin_family == ADDRESS_FAMILY.AF_INET)
		{
			dwSockAddrLength = (uint)Marshal.SizeOf(typeof(SOCKADDR_IN));
		}
		else if (pSockAddrIn.sin_family == ADDRESS_FAMILY.AF_INET6)
		{
			dwSockAddrLength = (uint)Marshal.SizeOf(typeof(SOCKADDR_IN6));
		}
		else
		{
			// Status = Win32Error.ERROR_REGISTRY_CORRUPT;
			return;
		}

		StringBuilder IpAddr = new(INET6_ADDRSTRLEN);
		uint dwIpAddrLen = INET6_ADDRSTRLEN;
		WSRESULT Status = WSAAddressToString(new SOCKADDR(pSsl.KeyDesc.pIpPort), dwSockAddrLength,
			default, IpAddr, ref dwIpAddrLen);

		if (Status.Failed)
		{
			return;
		}

		// Print the Key.
		Console.Write("IP Address is {0} \n", IpAddr);

		Console.Write("SSL Hash is: \n");
		foreach (var b in pSsl.ParamDesc.pSslHash.AsReadOnlySpan<byte>((int)pSsl.ParamDesc.SslHashLength))
		{
			Console.Write("{0:X} ", b);
		}

		Console.Write("\n");

		Console.Write("SSL CertStore Name is {0}\n", pSsl.ParamDesc.pSslCertStoreName);

		Console.Write("SSL CertCheck Mode is {0}\n", pSsl.ParamDesc.DefaultCertCheckMode);

		Console.Write("SSL Revocation Freshness Time is {0}\n", pSsl.ParamDesc.DefaultRevocationFreshnessTime);

		Console.Write("SSL Revocation Retrieval Timeout is {0}\n", pSsl.ParamDesc.DefaultRevocationUrlRetrievalTimeout);

		Console.Write("SSLCTL Identifier is {0}\n", pSsl.ParamDesc.pDefaultSslCtlIdentifier);

		Console.Write("SSLCTL storename is {0}\n", pSsl.ParamDesc.pDefaultSslCtlStoreName);

		Console.Write("SSL Flags is {0}\n", pSsl.ParamDesc.DefaultFlags);
	}
}