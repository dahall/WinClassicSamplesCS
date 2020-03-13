using System;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NCrypt;

namespace CreateCertificate
{
	static class CreateCertificate
	{
		/*****************************************************************************
		 ReportError
			Prints error information to the console
		*****************************************************************************/
		static void ReportError(string wszMessage, HRESULT dwErrCode)
		{
			if (!string.IsNullOrEmpty(wszMessage))
				Console.WriteLine(wszMessage);

			var ex = dwErrCode.GetException();
			Console.WriteLine($"Error: 0x{(uint)dwErrCode:X8} ({(uint)dwErrCode})" + ex is null ? "" : $" {ex.Message}");
		}

		/*****************************************************************************
		 HrCreateKey

		  This function uses the NCrypt API to open the storage provider
		  and create the key
		*****************************************************************************/
		static HRESULT HrCreateKey(string pwszProvName, string wszContainerName, string pwszAlgid, uint dwBits, out SafeNCRYPT_KEY_HANDLE phKey)
		{
			SafeNCRYPT_PROV_HANDLE hProvider = default;
			HRESULT hr = HRESULT.S_OK;

			phKey = default;

			hr = NCryptOpenStorageProvider(out hProvider, pwszProvName ?? KnownStorageProvider.MS_KEY_STORAGE_PROVIDER);
			if (hr.Failed)
			{
				goto CleanUp;
			}

			hr = NCryptOpenKey(hProvider, out phKey, wszContainerName);
			if (hr.Succeeded)
			{
				hr = NCryptDeleteKey(phKey);
				phKey.Dispose();
				phKey = default;
				if (hr.Failed)
				{
					goto CleanUp;
				}
			}

			hr = NCryptCreatePersistedKey(hProvider, out phKey, pwszAlgid, wszContainerName);
			if (hr.Failed)
			{
				goto CleanUp;
			}

			if (0 != dwBits)
			{
				using var pdwBits = new PinnedObject(dwBits);
				hr = NCryptSetProperty(phKey, NCrypt.PropertyName.NCRYPT_LENGTH_PROPERTY, pdwBits, (uint)Marshal.SizeOf(dwBits), SetPropFlags.NCRYPT_PERSIST_FLAG);
				if (hr.Failed)
				{
					goto CleanUp;
				}
			}

			hr = NCryptFinalizeKey(phKey);

			if (hr.Failed)
			{
				goto CleanUp;
			}

			hr = HRESULT.S_OK;

			CleanUp:

			if (hr.Failed)
			{
				phKey?.Dispose();
				phKey = null;
			}
			hProvider?.Dispose();

			return hr;
		}

		/*****************************************************************************
		 Usage

		*****************************************************************************/
		static void Usage(string wsName)
		{
			Console.Write("{0} [Options] SubjectName\n", wsName);
			Console.Write("\tOptions:\n");
			Console.Write("\t        -c {Container}     : container name (by default \"SAMPLE\")\n");
			Console.Write("\t        -s {STORENAME}     : store name (by default \"MY\")\n");
			Console.Write("\t        -l {Bits}			: key size\n");
			Console.Write("\t        -k {CNGAlgName}    : key algorithm name (by default \"RSA\")\n");
			Console.Write("\t        -h {CNGAlgName}    : hash algorithm name (by default \"SHA1\")\n");
		}

		/*****************************************************************************
			wmain

		*****************************************************************************/
		static int Main(string[] args)
		{
			HRESULT hr = HRESULT.S_OK;
			SafeHCERTSTORE hStoreHandle = default;
			string wszStoreName = "MY"; // by default, MY
			string wszContainerName = "SAMPLE";
			uint dwBits = 0;

			string wszKeyAlgName = "RSA"; //
			string[] rgwszCNGAlgs = new string[] { "SHA1", "RSA" };

			SafeNCRYPT_KEY_HANDLE hCNGKey = default;
			SafePCCERT_CONTEXT pCertContext = default;
			CRYPTOAPI_BLOB SubjectName = default;
			int i;

			//
			// options
			//

			for (i = 0; i < args.Length; i++)
			{
				if (string.Compare(args[i], "/?") == 0 || string.Compare(args[i], "-?") == 0)
				{
					Usage("CreateCert.exe");
					goto CleanUp;
				}

				if (args[i][0] != '-')
					break;

				if (string.Compare(args[i], "-s") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;

						goto CleanUp;
					}

					wszStoreName = args[++i];
				}
				else
				if (string.Compare(args[i], "-c") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;

						goto CleanUp;
					}

					wszContainerName = args[++i];
				}
				else
				if (string.Compare(args[i], "-k") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;

						goto CleanUp;
					}

					wszKeyAlgName = args[++i];
				}
				else
				if (string.Compare(args[i], "-h") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;

						goto CleanUp;
					}

					rgwszCNGAlgs[0] = args[++i];
				}
				else
				if (string.Compare(args[i], "-l") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;

						goto CleanUp;
					}

					dwBits = uint.Parse(args[++i]);
				}
			}

			if (i >= args.Length)
			{
				hr = HRESULT.E_INVALIDARG;

				goto CleanUp;
			}

			var wszSubject = args[i];

			//
			// Find the Signature algorithm
			//

			var pOidInfo = CryptFindOIDInfo(CryptOIDInfoFlags.CRYPT_OID_INFO_NAME_KEY, new SafeCoTaskMemString(wszKeyAlgName), OIDGroupId.CRYPT_PUBKEY_ALG_OID_GROUP_ID);
			if (default == pOidInfo)
			{
				Console.Write("FAILED: Unable to find Public Key algorithm: '{0}'.\n", wszKeyAlgName);
				hr = HRESULT.CRYPT_E_UNKNOWN_ALGO;
				goto CleanUp;
			}

			var oidInfo = (CRYPT_OID_INFO)pOidInfo;
			if (!string.IsNullOrEmpty(oidInfo.pwszCNGExtraAlgid))
			{
				rgwszCNGAlgs[1] = oidInfo.pwszCNGExtraAlgid;
			}
			else
			{
				rgwszCNGAlgs[1] = oidInfo.pwszCNGAlgid;
			}

			using (var pAlgs = SafeLocalHandle.CreateFromStringList(rgwszCNGAlgs, StringListPackMethod.Packed, CharSet.Unicode))
				pOidInfo = CryptFindOIDInfo(CryptOIDInfoFlags.CRYPT_OID_INFO_CNG_SIGN_KEY, pAlgs, OIDGroupId.CRYPT_SIGN_ALG_OID_GROUP_ID);
			if (default == pOidInfo)
			{
				Console.Write("FAILED: Unable to find signature algorithm: '{0}:{1}'\n", rgwszCNGAlgs[0], rgwszCNGAlgs[1]);
				hr = HRESULT.CRYPT_E_UNKNOWN_ALGO;
				goto CleanUp;
			}

			var SignatureAlgorithm = new CRYPT_ALGORITHM_IDENTIFIER { pszObjId = ((CRYPT_OID_INFO)pOidInfo).pszOID };

			//-------------------------------------------------------------------
			// Open a system store, in this case, the My store.

			hStoreHandle = CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM, 0, default, CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER, new SafeCoTaskMemString(wszStoreName));
			if (hStoreHandle.IsInvalid)
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			//+--------------------------------------------
			// Create a certificate context and add it to the store.
			//
			// The structure of a X.509 v3 digital certificate is as follows:
			// Version 
			// Serial Number 
			// Algorithm ID 
			// Issuer Name
			// Validity 
			// Not Before 
			// Not After 
			// Subject Name
			// Subject Public Key Info 
			// Public Key Algorithm 
			// Subject Public Key 
			// Issuer Unique Identifier (Optional) 
			// Subject Unique Identifier (Optional) 
			// Extensions (Optional) 
			// Certificate Signature Algorithm 
			// Certificate Signature 

			//---------------------------------------------

			if (!CertStrToName(CertEncodingType.X509_ASN_ENCODING, wszSubject, CertNameStringFormat.CERT_X500_NAME_STR | CertNameStringFormat.CERT_NAME_STR_SEMICOLON_FLAG,
				default, default, ref SubjectName.cbData, out _))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				Console.Write("FAILED: CertStrToName('{0}').\n", wszSubject);
				goto CleanUp;
			}

			SubjectName.pbData = (IntPtr)LocalAlloc(LMEM.LPTR, SubjectName.cbData);
			if (default == SubjectName.pbData)
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			if (!CertStrToName(CertEncodingType.X509_ASN_ENCODING, wszSubject, CertNameStringFormat.CERT_X500_NAME_STR | CertNameStringFormat.CERT_NAME_STR_SEMICOLON_FLAG,
				default, SubjectName.pbData, ref SubjectName.cbData, out _))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				Console.Write("FAILED: CertStrToName('{0}').\n", wszSubject);
				goto CleanUp;
			}

			// Get a CERT_PUBLIC_KEY_INFO

			hr = HrCreateKey(KnownStorageProvider.MS_KEY_STORAGE_PROVIDER, wszContainerName, wszKeyAlgName, dwBits, out hCNGKey);
			if (hr.Failed)
			{
				goto CleanUp;
			}

			// Tie this certificate to the Private key by setting a property
			var KeyProvInfo = new CRYPT_KEY_PROV_INFO
			{
				pwszContainerName = wszContainerName,
				pwszProvName = KnownStorageProvider.MS_KEY_STORAGE_PROVIDER
			};

			pCertContext = CertCreateSelfSignCertificate(default, SubjectName, 0, KeyProvInfo, SignatureAlgorithm, default, default, default);
			if (pCertContext.IsInvalid)
			{
				hr = (HRESULT)Win32Error.GetLastError();

				goto CleanUp;
			}

			Console.Write("Successfully created certificate.\n");

			// Add it to the Store
			if (!CertAddCertificateContextToStore(hStoreHandle, pCertContext, CertStoreAdd.CERT_STORE_ADD_ALWAYS, out _))
			{
				hr = (HRESULT)Win32Error.GetLastError();

				goto CleanUp;
			}

			Console.Write("Certificate added to the store.\n");

			CleanUp:

			//-------------------------------------------------------------------
			// Clean up memory.

			if (SubjectName.pbData != default)
			{
				LocalFree(SubjectName.pbData);
			}

			hCNGKey?.Dispose();
			pCertContext?.Dispose();
			hStoreHandle?.Dispose();

			if (hr.Failed)
			{
				ReportError(default, hr);
			}

			return (int)hr;
		}
	}
}
