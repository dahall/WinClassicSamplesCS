global using System;
global using System.Runtime.InteropServices;
global using Vanara.Extensions;
global using Vanara.InteropServices;
global using Vanara.PInvoke;
global using static Vanara.PInvoke.HttpApi;
global using static Vanara.PInvoke.Ws2_32;
using System.Linq;

namespace ServiceConfig4;

internal partial class Program
{
	public enum HTTPCFG_TYPE
	{
		HttpCfgTypeSet,
		HttpCfgTypeQuery,
		HttpCfgTypeDelete,
		HttpCfgTypeMax
	}

	/***************************************************************************++
	Routine Description:
		main routine.
	Arguments:
		argc - # of command line arguments.
		argv - Arguments.
	Return Value:
		Success/Failure.
	--***************************************************************************/
	public static int Main(string[] args)
	{
		HTTPCFG_TYPE Type;

		// Parse command line parameters.

		if (args.Length < 2)
		{
			Usage();
			return 0;
		}

		var i = 0;

		// First parse the type of operation.

		if (wcsicmp(args[i], "set") == 0)
		{
			Type = HTTPCFG_TYPE.HttpCfgTypeSet;
		}
		else if (wcsicmp(args[i], "query") == 0)
		{
			Type = HTTPCFG_TYPE.HttpCfgTypeQuery;
		}
		else if (wcsicmp(args[i], "delete") == 0)
		{
			Type = HTTPCFG_TYPE.HttpCfgTypeDelete;
		}
		else if (wcsicmp(args[i], "?") == 0)
		{
			Usage();
			return 0;
		}
		else
		{
			Console.Write("{0} is an invalid command", args[i]);
			return (int)Win32Error.ERROR_INVALID_PARAMETER;
		}
		i++;

		// Call HttpInitialize.

		using var init = new SafeHttpInitialize(HTTP_INIT.HTTP_INITIALIZE_CONFIG);

		// Call WSAStartup as we are using some winsock functions.

		using var wsa = SafeWSA.Initialize();

		// Call the corresponding API.

		Win32Error Status;
		if (wcsicmp(args[i], "ssl") == 0)
		{
			Status = DoSsl(args.Skip(++i).ToArray(), Type);
		}
		else if (wcsicmp(args[i], "urlacl") == 0)
		{
			Status = DoUrlAcl(args.Skip(++i).ToArray(), Type);
		}
		else if (wcsicmp(args[i], "iplisten") == 0)
		{
			Status = DoIpListen(args.Skip(++i).ToArray(), Type);
		}
		else
		{
			Console.Write("{0} is an invalid command", args[i]);
			Status = Win32Error.ERROR_INVALID_PARAMETER;
		}

		return (int)(uint)Status;

		static int wcsicmp(string v1, string v2) => string.Compare(v1, v2, true);
	}

	/***************************************************************************++
	Routine Description:
		Given a WCHAR IP, this routine converts it to a SOCKADDR.
	Arguments:
		pIp     - IP address to covert.
		pBuffer - Buffer, must be == sizeof(SOCKADDR_STORAGE)
		Length  - Length of buffer
	Return Value:
		Success/Failure.
	--***************************************************************************/
	private static WSRESULT GetAddress([In, Optional] string pIp, out SOCKADDR pBuffer)
	{
		pBuffer = new SOCKADDR(default(SOCKADDR_IN6));
		if (pIp is null)
		{
			return WSRESULT.WSA_INVALID_PARAMETER;
		}

		// The address could be a v4 or a v6 address. First, let's try v4.
		int Length = pBuffer.Size;
		WSRESULT Status = WSAStringToAddress(pIp, ADDRESS_FAMILY.AF_INET, default, pBuffer, ref Length);

		if (Status.Failed)
		{
			// Now, try v6
			WSRESULT TempStatus = WSAStringToAddress(pIp, ADDRESS_FAMILY.AF_INET6, default, pBuffer, ref Length);

			// If IPv6 also fails, then we want to return the original error.
			//
			// If it succeeds, we want to return NO_ERROR.

			if (TempStatus.Succeeded)
			{
				Status = 0;
			}
		}

		return Status;
	}

	private static void Usage() => Console.Write("httpcfg ACTION STORENAME [OPTIONS] \n"
			+ "ACTION -set | query | delete \n"
			+ "STORENAME -ssl | urlacl | iplisten \n"
			+ "[OPTIONS] -See Below\n"
			+ "\n"
			+ "Options for ssl:\n"
			+ " -i IP-Address - IP:port for the SSL certificate (record key)\n"
			+ "\n"
			+ " -h SslHash - Hash of the Certificate.\n"
			+ "\n"
			+ " -g Guid - Guid to identify the owning application.\n"
			+ "\n"
			+ " -c CertStoreName - Store name for the certificate. Defaults to\n"
			+ " MY. Certificate must be stored in the\n"
			+ " LOCAL_MACHINE context.\n"
			+ "\n"
			+ " -m CertCheckMode - Bit Flag\n"
			+ " 0x00000001 - Client certificate will not be\n"
			+ " verified for revocation.\n"
			+ " 0x00000002 - Only cached client certificate\n"
			+ " revocation will be used.\n"
			+ " 0x00000004 - Enable use of the Revocation\n"
			+ " freshness time setting.\n"
			+ " 0x00010000 - No usage check.\n"
			+ "\n"
			+ " -r RevocationFreshnessTime - How often to check for an updated certificate\n"
			+ " revocation list (CRL). If this value is 0,\n"
			+ " then the new CRL is updated only if the\n"
			+ " previous one expires. Time is specified in\n"
			+ " seconds.\n"
			+ "\n"
			+ " -x UrlRetrievalTimeout - Timeout on attempt to retrieve certificate\n"
			+ " revocation list from the remote URL.\n"
			+ " Timeout is specified in Milliseconds.\n"
			+ "\n"
			+ " -t SslCtlIdentifier - Restrict the certificate issuers that can be\n"
			+ " trusted. Can be a subset of the certificate\n"
			+ " issuers that are trusted by the machine.\n"
			+ "\n"
			+ " -n SslCtlStoreName - Store name under LOCAL_MACHINE where\n"
			+ " SslCtlIdentifier is stored.\n"
			+ "\n"
			+ " -f Flags - Bit Field\n"
			+ " 0x00000001 - Use DS Mapper.\n"
			+ " 0x00000002 - Negotiate Client certificate.\n"
			+ " 0x00000004 - Do not route to Raw ISAPI\n"
			+ " filters.\n"
			+ "\n"
			+ "Options for urlacl:\n"
			+ " -u Url - Fully Qualified URL. (record key)\n"
			+ " -a ACL - ACL specified as a SDDL string.\n"
			+ "\n"
			+ "Options for iplisten:\n"
			+ " -i IPAddress - IPv4 or IPv6 address. (for set/delete only)\n");
}