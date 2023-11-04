using System.IO;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NCrypt;

namespace CertSign
{
	static class CertSign
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
		HrLoadFile

		Load file into allocated (*ppbData). 
		The caller must free the memory by LocalFree().
		*****************************************************************************/
		static HRESULT HrLoadFile(string wszFileName, out SafeLocalHandle ppbData)
		{
			HRESULT hr = HRESULT.S_OK;
			ppbData = default;

			using var hFile = CreateFile(wszFileName, Kernel32.FileAccess.GENERIC_READ, 0, default, FileMode.Open, 0);

			if (hFile.IsInvalid)
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			var pcbData = GetFileSize(hFile, out _);
			if (pcbData == 0)
			{
				hr = HRESULT.S_FALSE;
				goto CleanUp;
			}

			ppbData = new SafeLocalHandle(pcbData);
			if (!ReadFile(hFile, ppbData, pcbData, out var cbRead))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			CleanUp:

			if (hr.Failed)
			{
				ppbData = default;
				pcbData = 0;
			}

			return hr;
		}

		/*****************************************************************************
		HrSaveFile

		*****************************************************************************/
		static HRESULT HrSaveFile(string wszFileName, IntPtr pbData, uint cbData)
		{
			using var hFile = CreateFile(wszFileName, Kernel32.FileAccess.GENERIC_WRITE, 0, default, FileMode.Create, 0);

			if (hFile.IsInvalid)
			{
				return (HRESULT)Win32Error.GetLastError();
			}

			if (!WriteFile(hFile, pbData, cbData, out _))
			{
				return (HRESULT)Win32Error.GetLastError();
			}

			return HRESULT.S_OK;
		}

		//----------------------------------------------------------------------------
		// HrFindCertificateBySubjectName
		//
		//----------------------------------------------------------------------------
		static HRESULT HrFindCertificateBySubjectName(string wszStore, string wszSubject, out SafePCCERT_CONTEXT ppcCert)
		{
			ppcCert = default;

			//-------------------------------------------------------------------
			// Open the certificate store to be searched.

			using var hStoreHandle = CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM, 0, default, CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER, wszStore);

			if (hStoreHandle.IsInvalid)
			{
				return (HRESULT)Win32Error.GetLastError();
			}

			//-------------------------------------------------------------------
			// Get a certificate that has the specified Subject Name

			ppcCert = CertFindCertificateInStore(hStoreHandle, CertEncodingType.X509_ASN_ENCODING, 0, CertFindType.CERT_FIND_SUBJECT_STR, wszSubject, default);
			if (ppcCert.IsInvalid)
			{
				return (HRESULT)Win32Error.GetLastError();
			}

			return HRESULT.S_OK;
		}

		//----------------------------------------------------------------------------------------------------------------
		// 
		// Function: HrCreateCNGHash()
		//
		// The caller must call LocalFree to release (*ppbHash)
		//------------------------------------------------------------------------------------------------------------------
		static HRESULT HrCreateCNGHash(string wszHashAlgName, IntPtr pbData, uint cbData, out SafeLocalHandle ppbHash)
		{
			//initialize [Out]"), parameters
			ppbHash = default;

			//get a handle to a cryptographic provider
			var hr = BCryptOpenAlgorithmProvider(out var hAlgorithm, wszHashAlgName, default, 0);
			if (hr.Failed)
			{
				goto CleanUp;
			}
			using (hAlgorithm)
			{
				//first get size of HASH buffer
				var cbHashObject = BCryptGetProperty<uint>(hAlgorithm, BCrypt.PropertyName.BCRYPT_OBJECT_LENGTH);

				//now allocate memory for hash object
				using var pbHashObject = new SafeLocalHandle(cbHashObject);

				//create hash
				hr = BCryptCreateHash(hAlgorithm, out var hHash, pbHashObject, cbHashObject, default, 0, 0);
				if (hr.Failed)
				{
					goto CleanUp;
				}
				using (hHash)
				{
					//hash data
					hr = BCryptHashData(hHash, pbData, cbData);
					if (hr.Failed)
					{
						goto CleanUp;
					}

					//now get size of final HASH buffer
					var cbHash = BCryptGetProperty<uint>(hAlgorithm, BCrypt.PropertyName.BCRYPT_HASH_LENGTH);

					//now allocate memory for hash object
					ppbHash = new SafeLocalHandle(cbHash);

					//compute the hash and receive it in the buffer
					hr = BCryptFinishHash(hHash, ppbHash, cbHash);
					if (hr.Failed)
					{
						goto CleanUp;
					}
				}
			}

			hr = NTStatus.STATUS_SUCCESS;

			CleanUp:

			if (hr.Failed)
				ppbHash = default;

			return (HRESULT)hr;
		}

		//----------------------------------------------------------------------------------------------------------------
		// 
		// Function: HrSignCNGHash()
		//
		// The caller must call LocalFree to release (*ppbSignature)
		//------------------------------------------------------------------------------------------------------------------
		static HRESULT HrSignCNGHash(NCRYPT_KEY_HANDLE hKey, IntPtr pPaddingInfo, EncryptFlags dwFlag, IntPtr pbHash, uint cbHash, out SafeLocalHandle ppbSignature)
		{
			HRESULT hr = HRESULT.S_OK;

			//initialize [Out]"), parameters
			ppbSignature = default;

			//get a size of signature
			hr = NCryptSignHash(hKey, pPaddingInfo, pbHash, cbHash, default, 0, out var pcbSignature, dwFlag);

			if (hr.Failed)
			{
				goto CleanUp;
			}

			// allocate buffer for signature
			ppbSignature = new SafeLocalHandle(pcbSignature);

			hr = NCryptSignHash(hKey, pPaddingInfo, pbHash, cbHash, ppbSignature, pcbSignature, out pcbSignature, dwFlag);

			if (hr.Failed)
			{
				goto CleanUp;
			}

			hr = HRESULT.S_OK;

			CleanUp:

			if (hr.Failed)
				ppbSignature = default;

			return hr;
		}

		//----------------------------------------------------------------------------------------------------------------
		// 
		// Function: HrVerifySignature()
		//
		//------------------------------------------------------------------------------------------------------------------
		static unsafe HRESULT HrVerifySignature(PCCERT_CONTEXT pCertContext, string wszHashAlgName, IntPtr pbData, uint cbData, SafeAllocatedMemoryHandle pbSignature)
		{
			HRESULT hr = HRESULT.S_OK;

			EncryptFlags dwCngFlags = 0;

			//get certificate public key
			var keyInfo = ((CERT_CONTEXT*)pCertContext)->pUnsafeCertInfo->SubjectPublicKeyInfo;
			if (!CryptImportPublicKeyInfoEx2(CertEncodingType.X509_ASN_ENCODING, keyInfo,
				CryptOIDInfoFlags.CRYPT_OID_INFO_PUBKEY_SIGN_KEY_FLAG, default, out var hKey))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}
			using (hKey)
			{
				hr = HrCreateCNGHash(wszHashAlgName, pbData, cbData, out var pbHash);
				if (hr.Failed)
				{
					goto CleanUp;
				}
				using (pbHash)
				{
					// =======================================================================
					// Verify
					//

					// TODO:
					// The production code must specify valid padding.
					// SAMPLE:
					// This padding valid for RSA non PSS only:
					//

					var pOidInfo = CryptFindOIDInfo(CryptOIDInfoFlags.CRYPT_OID_INFO_OID_KEY, (IntPtr)keyInfo.Algorithm.pszObjId, OIDGroupId.CRYPT_PUBKEY_ALG_OID_GROUP_ID);
					if (default != pOidInfo && ((CRYPT_OID_INFO)pOidInfo).pwszCNGAlgid == "RSA")
					{
						dwCngFlags = EncryptFlags.BCRYPT_PAD_PKCS1;
					}

					hr = (HRESULT)BCryptVerifySignature(hKey, new SafeCoTaskMemString(wszHashAlgName), pbHash, pbHash.Size, pbSignature, pbSignature.Size, dwCngFlags);

					if (hr.Failed)
					{
						goto CleanUp;
					}
				}
			}

			hr = HRESULT.S_OK;

			CleanUp:

			return hr;
		}

		//----------------------------------------------------------------------------------------------------------------
		// 
		// Function: HrSignCAPI()
		//
		//------------------------------------------------------------------------------------------------------------------
		static unsafe HRESULT HrSignCAPI(HCRYPTPROV hProvider, CertKeySpec dwKeySpec, string wszHashAlgName, IntPtr pbData, uint cbData, out SafeLocalHandle ppbSignature)
		{
			HRESULT hr = HRESULT.S_OK;
			SafeHCRYPTHASH hHash = null;

			//initialize [Out]"), parameters
			ppbSignature = default;

			// Find ALGID for
			var pOidInfo = CryptFindOIDInfo(CryptOIDInfoFlags.CRYPT_OID_INFO_NAME_KEY, wszHashAlgName, OIDGroupId.CRYPT_HASH_ALG_OID_GROUP_ID);

			if (pOidInfo == default || IS_SPECIAL_OID_INFO_ALGID(((CRYPT_OID_INFO)pOidInfo).Union.Algid))
			{
				hr = HRESULT.CRYPT_E_UNKNOWN_ALGO;
				goto CleanUp;
			}

			//create hash
			if (!CryptCreateHash(hProvider, ((CRYPT_OID_INFO)pOidInfo).Union.Algid, default, 0, out hHash))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			//hash data
			if (!CryptHashData(hHash, pbData, cbData, 0))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			//get a size of signature
			uint cbSignature = 0;
			if (!CryptSignHash(hHash, dwKeySpec, default, 0, default, ref cbSignature))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			//now allocate memory for signature object
			ppbSignature = new SafeLocalHandle(cbSignature);

			//now sign it
			if (!CryptSignHash(hHash, dwKeySpec, default, 0, ppbSignature, ref cbSignature))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			//
			// Reverse bytes to Big Endian
			//

			//
			// TODO: Check if the keys is DSA, then Reverse R&S separately
			// at the middle of the buffer.
			//

			// works for non DSA keys only
			if (cbSignature > 1)
			{
				var p = (byte*)ppbSignature.DangerousGetHandle();
				for (var i = 0; i < cbSignature / 2; i++)
				{
					var b = p[i];
					p[i] = p[cbSignature - i - 1];
					p[cbSignature - i - 1] = b;
				}
			}

			hr = HRESULT.S_OK;

			CleanUp:

			hHash?.Dispose();

			if (hr.Failed)
				ppbSignature = default;

			return hr;
		}

		/*****************************************************************************
		Usage

		*****************************************************************************/
		static void Usage(string wsName)
		{
			Console.Write("{0} [Options] {SIGN|VERIFY} InputFile SignatureFile\n", wsName);
			Console.Write("\tOptions:\n");
			Console.Write("\t -s {STORENAME} : store name, (by default \"MY\")\n");
			Console.Write("\t -n {SubjectName} : Certificate CN to search for, (by default \"Test\")\n");
			Console.Write("\t -h {HashAlgName} : hash algorithm name, (by default \"SHA1\")\n");
		}

		/*****************************************************************************
		wmain

		*****************************************************************************/
		static unsafe int Main(string[] args)
		{
			HRESULT hr = HRESULT.S_OK;

			bool fSign = true;

			// The message to be signed.
			SafeLocalHandle pbPlainText = null;

			//certificate to be used to sign data
			SafePCCERT_CONTEXT pCertContext = null;

			string pwszInputFile = default;
			string pwszSignatureFile = default;

			string pwszStoreName = "MY"; // by default, MY

			//subject name string of certificate to be used in signing
			//choose what cert do you want to use - CAPI or CNG
			string pwszCName = "Test";

			//choose what hash algorithm to use, default SHA1
			string pwszHashAlgName = "SHA1";

			//variable that receives the handle of either the CryptoAPI provider or the CNG key
			IntPtr hCryptProvOrNCryptKey = default;

			EncryptFlags dwCngFlags = 0;

			//TRUE if user needs to free handle to a private key
			var fCallerFreeKey = true;

			//hashed data and signature
			SafeLocalHandle pbHash = null;
			SafeLocalHandle pbSignature = null;

			//key spec; will be used to determine key type
			CertKeySpec dwKeySpec = 0;

			BCRYPT_PKCS1_PADDING_INFO PKCS1PaddingInfo = default;
			var pPKCS1PaddingInfo = SafeLocalHandle.Null;

			int i;

			//
			// options
			//

			for (i = 0; i < args.Length; i++)
			{
				if (string.Compare(args[i], "/?") == 0 ||
				string.Compare(args[i], "-?") == 0)
				{
					Usage("certsign.exe");
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

					pwszStoreName = args[++i];
				}
				else
				if (string.Compare(args[i], "-n") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;
						goto CleanUp;
					}

					pwszCName = args[++i];
				}
				else
				if (string.Compare(args[i], "-h") == 0)
				{
					if (i + 1 >= args.Length)
					{
						hr = HRESULT.E_INVALIDARG;
						goto CleanUp;
					}

					pwszHashAlgName = args[++i];
				}
			}

			if (i + 2 >= args.Length)
			{
				hr = HRESULT.E_INVALIDARG;
				goto CleanUp;
			}

			if (0 == string.Compare(args[i], "SIGN"))
				fSign = true;
			else
			if (0 == string.Compare(args[i], "VERIFY"))
				fSign = false;
			else
			{
				hr = HRESULT.E_INVALIDARG;
				goto CleanUp;
			}

			pwszInputFile = args[i + 1];
			pwszSignatureFile = args[i + 2];

			//-------------------------------------------------------------------
			// Find the test certificate to be validated and obtain a pointer to it

			hr = HrFindCertificateBySubjectName(pwszStoreName, pwszCName, out pCertContext);
			if (hr.Failed)
			{
				goto CleanUp;
			}

			//
			// Load file
			// 

			hr = HrLoadFile(pwszInputFile, out pbPlainText);
			if (hr.Failed)
			{
				Console.Write("Unable to read file: %s\n", pwszInputFile);
				goto CleanUp;
			}

			if (fSign)
			{
				if (!CryptAcquireCertificatePrivateKey(pCertContext, CryptAcquireFlags.CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG, default, out hCryptProvOrNCryptKey, out dwKeySpec, out fCallerFreeKey))
				{
					hr = (HRESULT)Win32Error.GetLastError();
					goto CleanUp;
				}

				//
				// check whether we have CNG or CAPI key
				//

				switch (dwKeySpec)
				{
					case CertKeySpec.CERT_NCRYPT_KEY_SPEC: //CNG key
						{
							hr = HrCreateCNGHash(pwszHashAlgName, pbPlainText, pbPlainText.Size, out pbHash);
							if (hr.Failed)
							{
								goto CleanUp;
							}

							// TODO:
							// The production code must specify valid padding.
							// SAMPLE:
							// This padding valid for RSA non PSS only:
							//

							var keyInfo = ((CERT_CONTEXT*)pCertContext)->pUnsafeCertInfo->SubjectPublicKeyInfo;
							var pOidInfo = CryptFindOIDInfo(CryptOIDInfoFlags.CRYPT_OID_INFO_OID_KEY, (IntPtr)keyInfo.Algorithm.pszObjId, OIDGroupId.CRYPT_PUBKEY_ALG_OID_GROUP_ID);
							if (default != pOidInfo)
							{
								var oidInfo = (CRYPT_OID_INFO)pOidInfo;
								if (0 == string.Compare(oidInfo.pwszCNGAlgid, "RSA"))
								{
									PKCS1PaddingInfo.pszAlgId = pwszHashAlgName;
									pPKCS1PaddingInfo = SafeLocalHandle.CreateFromStructure(PKCS1PaddingInfo);
									dwCngFlags = EncryptFlags.BCRYPT_PAD_PKCS1;
								}
							}

							hr = HrSignCNGHash(hCryptProvOrNCryptKey, pPKCS1PaddingInfo, dwCngFlags, pbHash, pbHash.Size, out pbSignature);
							if (hr.Failed)
							{
								goto CleanUp;
							}

							Console.Write("Signed message using CNG key.\n");
						}
						break;

					case CertKeySpec.AT_SIGNATURE: //CAPI key 
					case CertKeySpec.AT_KEYEXCHANGE:
						{
							//
							// Legacy (pre-Vista) key
							//

							hr = HrSignCAPI(hCryptProvOrNCryptKey, dwKeySpec, pwszHashAlgName, pbPlainText, pbPlainText.Size, out pbSignature);
							if (hr.Failed)
							{
								goto CleanUp;
							}

							Console.Write("Successfully signed message using legacy CSP key.\n");
						}
						break;

					default:
						Console.Write("Unexpected dwKeySpec returned from CryptAcquireCertificatePrivateKey.\n");
						break;

				}

				hr = HrSaveFile(pwszSignatureFile, pbSignature, pbSignature.Size);
				if (hr.Failed)
				{
					Console.Write("Unable to save file: %s\n", pwszSignatureFile);
					goto CleanUp;
				}

				Console.Write("Created signature file: %s\n", pwszSignatureFile);
			}
			else
			{
				hr = HrLoadFile(pwszSignatureFile, out pbSignature);
				if (hr.Failed)
				{
					Console.Write("Unable to read file: %s\n", pwszSignatureFile);
					goto CleanUp;
				}

				//
				// For Public Key operations use BCrypt
				//

				hr = HrVerifySignature(pCertContext, pwszHashAlgName, pbPlainText, pbPlainText.Size, pbSignature);
				if (hr.Failed)
				{
					goto CleanUp;
				}

				Console.Write("Successfully verified signature.\n");
			}

			hr = HRESULT.S_OK;

			CleanUp:

			//free CNG key or CAPI provider handle
			if (fCallerFreeKey)
			{
				switch (dwKeySpec)
				{
					case CertKeySpec.CERT_NCRYPT_KEY_SPEC: //CNG key
						NCryptFreeObject(hCryptProvOrNCryptKey);
						break;

					case CertKeySpec.AT_SIGNATURE: //CAPI key 
					case CertKeySpec.AT_KEYEXCHANGE:
						CryptReleaseContext(hCryptProvOrNCryptKey, 0);
						break;
				}
			}

			pCertContext?.Dispose();
			pbHash?.Dispose();
			pbPlainText?.Dispose();
			pbSignature?.Dispose();

			if (hr.Failed)
			{
				ReportError(default, hr);
			}

			return (int)hr;
		}
	}
}
