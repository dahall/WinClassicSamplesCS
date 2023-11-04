using System.IO;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Kernel32;

namespace signhash
{
	internal static class SignHash
	{
		private const CertEncodingType ENCODING = CertEncodingType.X509_ASN_ENCODING | CertEncodingType.PKCS_7_ASN_ENCODING;

		private static void Main(string[] args)
		{
			SafeHCRYPTPROV hProv = null;
			SafeHCRYPTKEY hPubKey = null;
			string szCertificateName = default;
			string szStoreName = default;
			string szContainerName = default;
			var dwOpenFlags = CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;
			CryptAcquireContextFlags dwAcquireFlags = 0;
			var dwKeySpec = CertKeySpec.AT_SIGNATURE;
			ALG_ID AlgId;

			if (args.Length != 8)
			{
				PrintUsage();
				return;
			}

			try
			{
				// Determine hash algorithm
				if (StringComparer.InvariantCultureIgnoreCase.Compare(args[0], "sha1") == 0)
				{
					AlgId = ALG_ID.CALG_SHA1;
				}
				else if (StringComparer.InvariantCultureIgnoreCase.Compare(args[0], "md5") == 0)
				{
					AlgId = ALG_ID.CALG_MD5;
				}
				else
				{
					PrintUsage();
					return;
				}

				bool fSign;
				if (StringComparer.InvariantCultureIgnoreCase.Compare(args[1], "/s") == 0)
				{
					fSign = true;
				}
				else if (StringComparer.InvariantCultureIgnoreCase.Compare(args[1], "/v") == 0)
				{
					fSign = false;
				}
				else
				{
					PrintUsage();
					return;
				}

				var szFileToSign = args[2];
				var szSigFile = args[3];
				bool fUseCert;
				// check to see if user wants to use a certificate
				if (StringComparer.InvariantCultureIgnoreCase.Compare(args[4], "/cert") == 0)
				{
					fUseCert = true;

					szCertificateName = args[5];
					szStoreName = args[6];

					// Determine if we have to use user or machine store
					if (StringComparer.InvariantCultureIgnoreCase.Compare(args[7], "u") == 0)
					{
						dwOpenFlags = CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER;
					}
					else if (StringComparer.InvariantCultureIgnoreCase.Compare(args[7], "m") == 0)
					{
						dwOpenFlags = CertStoreFlags.CERT_SYSTEM_STORE_LOCAL_MACHINE;
					}
					else
					{
						PrintUsage();
						return;
					}
				}
				else if (StringComparer.InvariantCultureIgnoreCase.Compare(args[4], "/key") == 0)
				{
					fUseCert = false;

					szContainerName = args[5];

					if (StringComparer.InvariantCultureIgnoreCase.Compare(args[6], "u") == 0)
					{
						dwAcquireFlags = 0;
					}
					else if (StringComparer.InvariantCultureIgnoreCase.Compare(args[6], "m") == 0)
					{
						dwAcquireFlags = CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET;
					}
					else
					{
						PrintUsage();
						return;
					}

					// Use exchange key or signature key
					if (StringComparer.InvariantCultureIgnoreCase.Compare(args[7], "x") == 0)
					{
						dwKeySpec = CertKeySpec.AT_KEYEXCHANGE;
					}
					else if (StringComparer.InvariantCultureIgnoreCase.Compare(args[7], "s") == 0)
					{
						dwKeySpec = CertKeySpec.AT_SIGNATURE;
					}
					else
					{
						PrintUsage();
						return;
					}
				}
				else
				{
					PrintUsage();
					return;
				}

				bool fResult;
				if (fUseCert)
				{
					using var pCertContext = GetCertificateContextFromName(szCertificateName, szStoreName, dwOpenFlags);
					if (pCertContext.IsInvalid)
						throw new Exception();

					fResult = GetRSAKeyFromCert(pCertContext, fSign, out hProv, out hPubKey, out dwKeySpec, out _);
					if (!fResult)
						throw new Exception();
				}
				else
				{
					fResult = GetRSAKeyFromContainer(szContainerName, dwAcquireFlags, dwKeySpec, out hProv, out hPubKey);
					if (!fResult)
						throw new Exception();
				}

				fResult = SignVerifyFile(hProv, hPubKey, dwKeySpec, AlgId, szFileToSign, szSigFile, fSign);
				if (!fResult)
					throw new Exception();

				if (fSign)
				{
					MyPrintf(("File %s hashed and signed successfully!\n"), szFileToSign);
				}
				else
				{
					MyPrintf(("File %s verified successfully!\n"), szSigFile);
				}
			}
			finally
			{
				// Clean up
			}
		}

		private static bool SignVerifyFile(HCRYPTPROV hProv, HCRYPTKEY hPubKey, CertKeySpec dwKeySpec, ALG_ID HashAlgId, string szFileToSign, string szSigFile, bool fSign)
		{
			const uint BUFFER_SIZE = 4096;
			uint dwSignature;
			bool fResult;
			bool fReturn = false;

			try
			{
				// Open Data file
				using var hDataFile = CreateFile(szFileToSign, Kernel32.FileAccess.GENERIC_READ, 0, default, FileMode.Open, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);
				if (hDataFile.IsInvalid)
				{
					MyPrintf(("CreateFile failed with %d\n"), GetLastError());
					throw new Exception();
				}

				// Open/Create signature file
				using var hSigFile = CreateFile(szSigFile, Kernel32.FileAccess.GENERIC_READ | Kernel32.FileAccess.GENERIC_WRITE, 0, default, FileMode.Open, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);
				if (hSigFile.IsInvalid)
				{
					MyPrintf(("CreateFile failed with %d\n"), GetLastError());
					throw new Exception();
				}

				SafeHCRYPTHASH hHash;
				// Create Hash
				fResult = CryptCreateHash(hProv, HashAlgId, default, 0, out hHash);
				if (!fResult)
				{
					MyPrintf(("CryptCreateHash failed with %x\n"), GetLastError());
					throw new Exception();
				}

				// Loop through file and hash file contents
				var pbBuffer = new SafeHGlobalHandle(BUFFER_SIZE);
				bool fFinished;
				do
				{
					fResult = ReadFile(hDataFile, pbBuffer, BUFFER_SIZE, out var dwBytesRead);

					if (dwBytesRead == 0) break;

					if (!fResult)
					{
						MyPrintf(("ReadFile failed with %d\n"), GetLastError());
						throw new Exception();
					}

					fFinished = (dwBytesRead < BUFFER_SIZE);

					fResult = CryptHashData(hHash, pbBuffer, dwBytesRead, 0);
					if (!fResult)
					{
						MyPrintf(("CryptHashData failed with %x\n"), GetLastError());
						throw new Exception();
					}
				} while (fFinished == false);

				if (fSign)
				{
					// Get Signature size
					dwSignature = 0;
					fResult = CryptSignHash(hHash, dwKeySpec, default, 0, default, ref dwSignature);
					if (!fResult)
					{
						MyPrintf(("CryptSignHash failed with %x\n"), GetLastError());
						throw new Exception();
					}

					// Allocate signature bytes
					using var pbSignature = new SafeHGlobalHandle(dwSignature);
					if (pbSignature.IsInvalid)
					{
						MyPrintf(("LocalAlloc failed with %d\n"), GetLastError());
						throw new Exception();
					}

					// Sign and get back signature
					fResult = CryptSignHash(hHash, dwKeySpec, default, 0, pbSignature, ref dwSignature);
					if (!fResult)
					{
						MyPrintf(("CryptSignHash failed with %x\n"), GetLastError());
						throw new Exception();
					}

					// Write signature to file
					fResult = WriteFile(hSigFile, pbSignature, dwSignature, out _);
					if (!fResult)
					{
						MyPrintf(("WriteFile failed with %d\n"), GetLastError());
						throw new Exception();
					}
				}
				else
				{
					// Get size of signature file
					dwSignature = GetFileSize(hSigFile, out _);
					if (dwSignature == INVALID_FILE_SIZE)
					{
						MyPrintf(("GetFileSize failed with %d\n"), GetLastError());
						throw new Exception();
					}

					// Allocate signature bytes
					using var pbSignature = new SafeHGlobalHandle(dwSignature);
					if (pbSignature.IsInvalid)
					{
						MyPrintf(("LocalAlloc failed with %d\n"), GetLastError());
						throw new Exception();
					}

					// Read Signature
					fResult = ReadFile(hSigFile, pbSignature, dwSignature, out var dwBytesRead);
					if (!fResult)
					{
						MyPrintf(("ReadFile failed with %d\n"), GetLastError());
						throw new Exception();
					}

					// Verify Signature
					fResult = CryptVerifySignature(hHash, pbSignature, dwSignature, hPubKey, default, 0);
					if (!fResult)
					{
						MyPrintf(("CryptVerifySignature failed with %x\n"), GetLastError());
						throw new Exception();
					}
				}

				fReturn = true;
			}
			finally
			{
			}

			return fReturn;
		}

		private static SafePCCERT_CONTEXT GetCertificateContextFromName(string lpszCertificateName, string lpszCertificateStoreName, CertStoreFlags dwCertStoreOpenFlags)
		{
			var szStoreProvider = CertStoreProvider.CERT_STORE_PROV_SYSTEM;

			// Open the specified certificate store
			using var hCertStore = CertOpenStore(szStoreProvider, 0, default, dwCertStoreOpenFlags | CertStoreFlags.CERT_STORE_READONLY_FLAG, lpszCertificateStoreName);
			if (hCertStore.IsInvalid)
			{
				MyPrintf(("CertOpenStore failed with {0:X}\n"), GetLastError());
				return null;
			}

			var dwFindType = CertFindType.CERT_FIND_SUBJECT_STR_W;

			// Find the certificate by CN.
			var pCertContext = CertFindCertificateInStore(hCertStore, ENCODING, 0, dwFindType, lpszCertificateName, default);
			if (pCertContext.IsInvalid)
			{
				MyPrintf(("CertFindCertificateInStore failed with %X\n"), GetLastError());
			}

			return pCertContext;
		}

		private static unsafe bool GetRSAKeyFromCert(PCCERT_CONTEXT pCertContext, bool fSign, out SafeHCRYPTPROV hProv, out SafeHCRYPTKEY hPubKey, out CertKeySpec dwKeySpec, out bool fFreeProv)
		{
			bool fResult;
			bool fReturn = false;

			try
			{
				hProv = null;
				hPubKey = null;
				dwKeySpec = 0;
				fFreeProv = false;

				if (fSign)
				{
					// Acquire the certificate's private key
					fResult = CryptAcquireCertificatePrivateKey(pCertContext, CryptAcquireFlags.CRYPT_ACQUIRE_USE_PROV_INFO_FLAG | CryptAcquireFlags.CRYPT_ACQUIRE_COMPARE_KEY_FLAG,
						default, out var hProvPtr, out dwKeySpec, out fFreeProv);
					if (!fResult)
					{
						MyPrintf(("CryptAcquireCertificatePrivateKey failed with %x\n"), GetLastError());
						throw new Exception();
					}
					hProv = new SafeHCRYPTPROV(hProvPtr);
				}
				else
				{
					fResult = CryptAcquireContext(out hProv, default, "Microsoft Base Cryptographic Provider v1.0", CryptProviderType.PROV_RSA_FULL, CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT);
					if (!fResult)
					{
						MyPrintf(("CryptAcquireContext failed with %x\n"), GetLastError());
						throw new Exception();
					}

					fFreeProv = true;

					// Import the public key from the certificate so we can verify
					fResult = CryptImportPublicKeyInfo(hProv, ENCODING, ((CERT_CONTEXT*)pCertContext)->pUnsafeCertInfo->SubjectPublicKeyInfo, out hPubKey);
					if (!fResult)
					{
						MyPrintf(("CryptImportPublicKeyInfo failed with %x\n"), GetLastError());
						throw new Exception();
					}
				}

				fReturn = true;
			}
			finally
			{
				if (!fReturn)
				{
					hPubKey = default;
					hProv = default;
					fFreeProv = false;
				}
			}

			return fReturn;
		}

		private static bool GetRSAKeyFromContainer(string szContainerName, CryptAcquireContextFlags dwAcquireFlags, CertKeySpec dwKeySpec, out SafeHCRYPTPROV hProv, out SafeHCRYPTKEY hPubKey)
		{
			bool fReturn = false;

			try
			{
				// acquire crypto context using container name
				var fResult = CryptAcquireContext(out hProv, szContainerName, "Microsoft Base Cryptographic Provider v1.0", CryptProviderType.PROV_RSA_FULL, dwAcquireFlags);
				if (!fResult)
				{
					MyPrintf(("CryptAcquireContext failed with %x\n"), GetLastError());
					throw new Exception();
				}

				// Get the key handle for verification
				fResult = CryptGetUserKey(hProv, dwKeySpec, out hPubKey);
				if (!fResult)
				{
					MyPrintf(("CryptGetUserKey failed with %x\n"), GetLastError());
					throw new Exception();
				}

				fReturn = true;
			}
			finally
			{
				if (!fReturn)
				{
					hPubKey = null;
					hProv = null;
				}
			}

			return fReturn;
		}

		private static void MyPrintf(string lpszFormat, params object[] p)
		{
			string szOutput = string.Format(lpszFormat, p);
			OutputDebugString(szOutput);
			Console.Write(szOutput);
		}

		private static void PrintUsage()
		{
			MyPrintf(("SignHash [md5|sha1] [</s>|</v>] <DataFile> <SigFile> [</cert>|</key>]\n"));
			MyPrintf(("/s to Sign.\n"));
			MyPrintf(("/v to Verify.\n"));
			MyPrintf(("/cert <CertName> <StoreName> [<u>|<m>] - use a certificate.\n"));
			MyPrintf(("/key <ContainerName> [<u>|<m>] [<x>|<s>] use container with exchange or signature key.\n"));
		}
	}
}