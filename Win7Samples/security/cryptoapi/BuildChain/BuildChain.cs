using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Crypt32;

namespace BuildChain;

static class BuildChain
{
	/*****************************************************************************
	 ReportError
		Prints error information to the console
	*****************************************************************************/
	static void ReportError(string? wszMessage, HRESULT dwErrCode)
	{
		if (!string.IsNullOrEmpty(wszMessage))
		{
			Console.WriteLine(wszMessage);
		}

		var ex = dwErrCode.GetException();
		if (ex != null)
		{
			Console.WriteLine($"Error: 0x{(uint)dwErrCode:X8} ({(uint)dwErrCode}) {ex.Message}");
		}
		else
		{
			Console.WriteLine($"Error: 0x{(uint)dwErrCode:X8} ({(uint)dwErrCode})");
		}
	}

	//----------------------------------------------------------------------------
	// HrFindCertificateBySubjectName
	//
	//----------------------------------------------------------------------------
	static HRESULT HrFindCertificateBySubjectName(string wszStore, string wszSubject, out SafePCCERT_CONTEXT ppcCert)
	{
		ppcCert = SafePCCERT_CONTEXT.Null;

		//-------------------------------------------------------------------
		// Open the certificate store to be searched.

		using var hStoreHandle = CertOpenStore(
			CertStoreProvider.CERT_STORE_PROV_SYSTEM, // the store provider type
			0, // the encoding type is not needed
			default, // use the default HCRYPTPROV
			CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER, // set the store location in a registry location
			wszStore); // the store name 

		if (hStoreHandle.IsInvalid)
		{
			return (HRESULT)Win32Error.GetLastError();
		}

		//-------------------------------------------------------------------
		// Get a certificate that has the specified Subject Name

		ppcCert = CertFindCertificateInStore(hStoreHandle,
			CertEncodingType.X509_ASN_ENCODING, // Use X509_ASN_ENCODING
			0, // No dwFlags needed
			CertFindType.CERT_FIND_SUBJECT_STR, // Find a certificate with a subject that matches the string in the next parameter
			wszSubject, // The Unicode string to be found in a certificate's subject
			default); // NULL for the first call to the function; In all subsequent calls, it is the last pointer returned by the function

		if (ppcCert.IsInvalid)
		{
			return (HRESULT)Win32Error.GetLastError();
		}

		return HRESULT.S_OK;
	}

	/*****************************************************************************
		Usage
	*****************************************************************************/
	static void Usage(string wsName)
	{
		Console.Write("{0} [Options] SubjectName\n", wsName);
		Console.Write("\tOptions:\n");
		Console.Write("\t        -f 0xHHHHHHHH    : CertGetCertificateChain flags\n");
		Console.Write("\t        -s STORENAME     : store name, (by default MY)\n");
	}

	/*****************************************************************************
	 wmain
	*****************************************************************************/
	static int Main(string[] args)
	{
		int i;

		string pwszStoreName = "MY"; // by default, MY

		CERT_CHAIN_PARA ChainPara = default;
		CERT_CHAIN_POLICY_PARA ChainPolicy = default;
		CERT_CHAIN_POLICY_STATUS PolicyStatus = default;

		var dwFlags = CertChainFlags.CERT_CHAIN_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT;

		HRESULT hr = HRESULT.S_OK;

		ChainPara.cbSize = (uint)Marshal.SizeOf(ChainPara);
		ChainPolicy.cbSize = (uint)Marshal.SizeOf(ChainPolicy);
		PolicyStatus.cbSize = (uint)Marshal.SizeOf(PolicyStatus);

		//
		// options
		//

		for (i = 0; i < args.Length; i++)
		{
			if (args[i] == "/?" || args[i] == "-?")
			{
				Usage("BuildChain.exe");
				goto CleanUp;
			}

			if (args[i][0] != '-')
				break;

			if (args[i] == "-s")
			{
				if (i + 1 >= args.Length)
				{
					hr = HRESULT.E_INVALIDARG;
					goto CleanUp;
				}

				pwszStoreName = args[++i];
			}
			else if (args[i] == "-f")
			{
				if (i + 1 >= args.Length)
				{
					hr = HRESULT.E_INVALIDARG;
					goto CleanUp;
				}

				dwFlags = (CertChainFlags)(uint.TryParse(args[++i], out var r) ? r : 0);
			}
		}

		if (i >= args.Length)
		{
			hr = HRESULT.E_INVALIDARG;
			goto CleanUp;
		}

		var pwszCName = args[i];

		//-------------------------------------------------------------------
		// Find the test certificate to be validated and obtain a pointer to it

		hr = HrFindCertificateBySubjectName(pwszStoreName, pwszCName, out var pcTestCertContext);
		if (hr.Failed)
		{
			goto CleanUp;
		}

		//-------------------------------------------------------------------
		// Build a chain using CertGetCertificateChain

		if (!CertGetCertificateChain(default, // use the default chain engine
			pcTestCertContext, // pointer to the end certificate
			default, // use the default time
			default, // search no additional stores
			ChainPara, // use AND logic and enhanced key usage as indicated in the ChainPara data structure
			dwFlags,
			default, // currently reserved
			out var pChainContext)) // return a pointer to the chain created
		{
			hr = (HRESULT)Win32Error.GetLastError();
			goto CleanUp;
		}

		var ctx = pChainContext.DangerousGetHandle().ToStructure<CERT_CHAIN_CONTEXT>();
		unsafe { Console.WriteLine($"Chain built with {ctx.GetChain()[0]->cElement} certificates."); }

		//---------------------------------------------------------------
		// Verify that the chain complies with policy

		ChainPolicy.dwFlags = CertChainPolicyFlags.CERT_CHAIN_POLICY_IGNORE_NOT_TIME_NESTED_FLAG;

		//
		// Base policy 
		//

		ChainPolicy.pvExtraPolicyPara = default;
		if (!CertVerifyCertificateChainPolicy(CertVerifyChainPolicy.CERT_CHAIN_POLICY_BASE, pChainContext, ChainPolicy, ref PolicyStatus))
		{
			hr = (HRESULT)Win32Error.GetLastError();
			goto CleanUp;
		}

		if (PolicyStatus.dwError != HRESULT.S_OK)
		{
			ReportError("Base Policy Chain Status Failure:", PolicyStatus.dwError);
			hr = PolicyStatus.dwError;

			// Instruction: If the PolicyStatus.dwError is CRYPT_E_NO_REVOCATION_CHECK or CRYPT_E_REVOCATION_OFFLINE, 
			// it indicates errors in obtaining revocation information. 
			// These can be ignored since the retrieval of revocation information 
			// depends on network availability

			goto CleanUp;
		}
		else
		{
			Console.WriteLine("Base Policy CertVerifyCertificateChainPolicy succeeded.");
		}

		CleanUp:

		if (hr.Failed)
		{
			ReportError(null, hr);
		}

		return (int)hr;
	}
}
