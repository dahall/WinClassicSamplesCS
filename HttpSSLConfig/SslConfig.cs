using Vanara.PInvoke;
using Vanara.InteropServices;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.HttpApi;
using static Vanara.PInvoke.Ws2_32;

internal static class Program
{
	static readonly Guid AppId = new(0xAAAABBBB, 0xCCCC, 0xDDDD, 0xEE, 0xEE, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00);

	// Abstract:
	//
	//     This sample demonstrates how to set, query, and delete a SSL
	//     configuration for Hostname:Port based SSL endpoint binding(SNI)
	//     or port based wildcard binding when central certificate storage(CCS)
	//     is deployed.
	//
	//     To use this sample, certificate must be present in some store under the
	//     Local Machine for a Hostname:Port based SSL endpoint binding.
	//
	//     SslConfig <Port> [<Hostname> <CertSubjectName> <StoreName>]
	//
	//     where:
	//
	//     Port             is port number for either hostname based or port based
	//                      SSL binding.
	//
	//     Hostname         is the hostname for the hostname based binding.
	//
	//     CertSubjectName  is the subject name of the certificate to be associated
	//                      with the SSL endpoint binding. This must be provided
	//                      if hostname based binding is specified.
	//
	//     StoreName        is the store under the Local Machine where certificate
	//                      is present. This must be provided if hostname based
	//                      binding is specified.
	//
	internal static void Main(string[] args)
	{
		//
		// Get parameters from input command line.
		//

		ParseParameters(args, out var Port, out var Hostname, out var CertSubjectName, out var StoreName).ThrowIfFailed();

		//
		// Initialize HTTPAPI in config mode.
		//

		using SafeHttpInitialize init = new();

		if (Hostname is not null && CertSubjectName is not null && StoreName is not null)
		{
			SniConfiguration(Port, Hostname, CertSubjectName, StoreName).ThrowIfFailed();
		}
		else
		{
			CcsConfiguration(Port).ThrowIfFailed();
		}

		Console.Write("SSL set/query/delete configuration succeed.\n");
	}

	/***************************************************************************++

	Routine Description:

	Get parameters from input command line.

	Arguments:

	Argv.Length - The number of parameters.

	Argv - The parameter array.

	Port - Supplies a pointer to access the port.

	Hostname - Supplies a pointer to access the hostname.

	CertSubjectName - Supplies a pointer to access the subject name of a
	certificate.

	StoreName - Supplies a pointer to access the name of the store under Local
	Machine where certificate is present.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error ParseParameters(string[] Argv, out ushort Port, out string? Hostname, out string? CertSubjectName, out string? StoreName)
	{
		Port = 0;
		Hostname = CertSubjectName = StoreName = null;

		string PortString;
		string? HostnameString = default;
		string? CertSubjectNameString = default;
		string? StoreNameString = default;
		if (Argv.Length == 1)
		{
			PortString = Argv[0];
		}
		else if (Argv.Length == 4)
		{
			PortString = Argv[0];
			HostnameString = Argv[2];
			CertSubjectNameString = Argv[2];
			StoreNameString = Argv[3];
		}
		else
		{
			Console.Write("Usage: SslConfig <Port> [<Hostname> <CertSubjectName> <StoreName>]\n");
			return Win32Error.ERROR_INVALID_PARAMETER;
		}

		if (!ushort.TryParse(PortString, out var TempPort))
		{
			Console.Write("{0} is not a valid port.\n", PortString);
			return Win32Error.ERROR_INVALID_PARAMETER;
		}

		//
		// Everything succeeded. Commit results.
		//

		Hostname = HostnameString;
		Port = TempPort;
		CertSubjectName = CertSubjectNameString;
		StoreName = StoreNameString;

		return Win32Error.ERROR_SUCCESS;
	}

	/***************************************************************************++

	Routine Description:

	Get certificate hash from the certificate subject name.

	Arguments:

	CertSubjectName - Subject name of the certificate to find.

	StoreName - Name of the store under Local Machine where certificate
	is present.

	CertHash - Buffer to return certificate hash.

	CertHashLength - Buffer length on input, hash length on output (element
	count).

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error GetCertificateHash(string CertSubjectName, string StoreName, SafeAllocatedMemoryHandle CertHash)
	{
		CertHash.Zero();

		//
		// Open the store under local machine.
		//

		using var SystemStore = CertOpenStore(9 /*CERT_STORE_PROV_SYSTEM*/, 0, default, CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE, StoreName);

		if (SystemStore.IsNull)
		{
			return Win32Error.GetLastError();
		}

		//
		// Find the certificate from the subject name and get the hash.
		//

		using var CertContext = CertFindCertificateInStore(SystemStore, CertEncodingType.X509_ASN_ENCODING, 0, CertFindType.CERT_FIND_SUBJECT_STR, CertSubjectName, default);
		if (CertContext.IsNull)
		{
			return Win32Error.GetLastError();
		}

		uint certHashSize = CertHash.Size;
		if (!CertGetCertificateContextProperty(CertContext, CertPropId.CERT_HASH_PROP_ID, CertHash, ref certHashSize))
		{
			return Win32Error.GetLastError();
		}

		return Win32Error.ERROR_SUCCESS;
	}

	/***************************************************************************++

	Routine Description:

	Get certificate hash and set the SNI configuration.

	Arguments:

	SniKey - SSL endpoint key: host and port.

	CertSubjectName - Subject name of the certificate to find.

	StoreName - Name of the store under Local Machine where certificate
	is present.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error SetSniConfiguration(in HTTP_SERVICE_CONFIG_SSL_SNI_KEY SniKey, string CertSubjectName, string StoreName)
	{
		SafeHGlobalHandle CertHash = new(50);
		var Error = GetCertificateHash(CertSubjectName, StoreName, CertHash);
		if (Error.Succeeded)
		{
			HTTP_SERVICE_CONFIG_SSL_SNI_SET SniConfig = new()
			{
				KeyDesc = SniKey,
				ParamDesc = new()
				{
					pSslHash = CertHash,
					SslHashLength = CertHash.Size,
					pSslCertStoreName = StoreName,
					AppId = AppId,
				}
			};

			Error = HttpSetServiceConfiguration(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo, SniConfig);
		}

		return Error;
	}

	/***************************************************************************++

	Routine Description:

	Query the SNI configuration.

	Arguments:

	SniKey - SSL endpoint key: host and port.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error QuerySniConfiguration(in HTTP_SERVICE_CONFIG_SSL_SNI_KEY SniKey)
	{
		HTTP_SERVICE_CONFIG_SSL_SNI_QUERY SniQuery = new()
		{
			KeyDesc = SniKey,
			QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact
		};

		//
		// Query the config.
		//

		return HttpQueryServiceConfiguration(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslSniCertInfo, SniQuery, out SafeCoTaskMemStruct<HTTP_SERVICE_CONFIG_SSL_SNI_SET> SniConfig);
	}

	/***************************************************************************++

	Routine Description:

	Delete the SNI configuration.

	Arguments:

	SniKey - SSL endpoint key: host and port.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error DeleteSniConfiguration(in HTTP_SERVICE_CONFIG_SSL_SNI_KEY SniKey)
	{
		HTTP_SERVICE_CONFIG_SSL_SNI_SET SniConfig = new() { KeyDesc = SniKey };
		return HttpDeleteServiceConfiguration(SniConfig);
	}

	/***************************************************************************++

	Routine Description:

	Demonstrate how to set, query, and delete the hostname based SSL configuration.

	Arguments:

	Port - Port for the hostname based SSL binding.

	Hostname - Hostname for the SSL binding.

	CertSubjectName - Subject name of the certificate using in the SSL binding.

	StoreName - Name of the store under Local Machine where certificate
	is present.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error SniConfiguration(ushort Port, string Hostname, string CertSubjectName, string StoreName)
	{
		//
		// Create SniKey.
		// N.B. An SNI binding is IP version agnostic but for API purposes we are
		// required to specify the IP address to be (IPv4) 0.0.0.0 in the key.
		//

		HTTP_SERVICE_CONFIG_SSL_SNI_KEY SniKey = new()
		{
			Host = (string)Hostname,
			IpPort = (SOCKADDR_STORAGE)new SOCKADDR_IN(IN_ADDR.INADDR_ANY, Port)
		};

		//
		// Create the SNI binding.
		//

		var Error = SetSniConfiguration(SniKey, CertSubjectName, StoreName);
		if (Error != Win32Error.ERROR_SUCCESS)
		{
			goto exit;
		}

		//
		// Query the SNI configuration.
		//

		Error = QuerySniConfiguration(SniKey);
		if (Error != Win32Error.ERROR_SUCCESS)
		{
			goto exit;
		}

		//
		// Delete the SNI configuration.
		//

		Error = DeleteSniConfiguration(SniKey);

exit:
		return Error;
	}

	/***************************************************************************++

	Routine Description:

	Create the port based SSL binding.

	Arguments:

	CcsKey - CCS endpoint key.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error SetCcsConfiguration(in HTTP_SERVICE_CONFIG_SSL_CCS_KEY CcsKey)
	{
		HTTP_SERVICE_CONFIG_SSL_CCS_SET CcsConfig = new()
		{
			KeyDesc = CcsKey,
			ParamDesc = new() { AppId = AppId }
		};

		return HttpSetServiceConfiguration(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslCcsCertInfo, CcsConfig);
	}

	/***************************************************************************++

	Routine Description:

	Query the port based SSL binding.

	Arguments:

	CcsKey - CCS endpoint key: 0.0.0.0:Port.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error QueryCcsConfiguration(in HTTP_SERVICE_CONFIG_SSL_CCS_KEY CcsKey)
	{
		HTTP_SERVICE_CONFIG_SSL_CCS_QUERY CcsQuery = new()
		{
			KeyDesc = CcsKey,
			QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact
		};

		//
		// Query the config.
		//

		return HttpQueryServiceConfiguration(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSslCcsCertInfo, CcsQuery, out SafeCoTaskMemStruct<HTTP_SERVICE_CONFIG_SSL_CCS_SET> CcsConfig);
	}

	/***************************************************************************++

	Routine Description:

	Delete the port based SSL binding.

	Arguments:

	CcsKey - CCS endpoint key.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error DeleteCcsConfiguration(in HTTP_SERVICE_CONFIG_SSL_CCS_KEY CcsKey)
	{
		HTTP_SERVICE_CONFIG_SSL_CCS_SET CcsConfig = new() { KeyDesc = CcsKey };
		return HttpDeleteServiceConfiguration(CcsConfig);
	}

	/***************************************************************************++

	Routine Description:

	Demonstrate how to set, query, and delete a port based SSL binding.

	Arguments:

	Port - CCS binding port.

	Return Value:

	Status.

	--***************************************************************************/
	static Win32Error CcsConfiguration(ushort Port)
	{
		HTTP_SERVICE_CONFIG_SSL_CCS_KEY CcsKey = default;

		//
		// Create CcsKey.
		// N.B. A CCS binding is IP version agnostic but for API purposes we are
		// required to specify the IP address to be (IPv4) 0.0.0.0 in the key.
		//

		CcsKey.LocalAddress = (SOCKADDR_STORAGE)new SOCKADDR_IN(IN_ADDR.INADDR_ANY, Port);

		//
		// Create the port based SSL binding.
		//

		var Error = SetCcsConfiguration(CcsKey);
		if (Error == Win32Error.ERROR_SUCCESS)
		{
			goto exit;
		}

		//
		// Query the port based SSL binding.
		//

		Error = QueryCcsConfiguration(CcsKey);
		if (Error != Win32Error.ERROR_SUCCESS)
		{
			goto exit;
		}

		//
		// Delete the port based SSL binding.
		//

		Error = DeleteCcsConfiguration(CcsKey);

exit:
		return Error;
	}
}