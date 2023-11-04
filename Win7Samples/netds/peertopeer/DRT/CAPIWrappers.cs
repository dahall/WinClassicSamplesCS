using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Kernel32;

namespace DrtSdkSample
{
	static class CAPIWrappers
	{
		public static readonly SafeCoTaskMemString DEFAULT_ALGORITHM = new(AlgOID.szOID_RSA_SHA1RSA, CharSet.Ansi);
		public const CertKeySpec DEFAULT_KEY_SPEC = CertKeySpec.AT_KEYEXCHANGE;
		public const CertEncodingType DEFAULT_ENCODING = CertEncodingType.X509_ASN_ENCODING | CertEncodingType.PKCS_7_ASN_ENCODING;
		public const int MAX_CONTAINER_NAME_LEN = 256;
		public const uint DEFAULT_PROV_TYPE = CryptProviderType.PROV_RSA_AES;
		public const int DEFAULT_PUBLIC_KEY_INFO_SIZE = 400;
		public const int DEFAULT_CRYPT_PROV_INFO_SIZE = 1024;
		public const int MAX_SIGNATURE_LENGTH = 16;
		public const int DEFAULT_KEYPAIR_EXPORT_SIZE = 1024;
		public const int MAX_KEYCONTAINER_NAME_LENGTH = 256;
		public const int SHA1_LENGTH = 20;
		public const int SHA1_LENGTH_IN_CHARS = SHA1_LENGTH * 2;     // two chars per byte
		public const int MD5_LENGTH = 16;

		//#define HRESULT _FROM_RPCSTATUS(x) \
		//(((x < 0) || (x == RPC_S_OK)) ? \
		//(HRESULT )x : \
		//(HRESULT )(((x) & 0x0000FFFF) | (FACILITY_RPC << 16) | 0x80000000))

		//#define SECOND_IN_FILETIME (10i64 * 1000 * 1000)

		public const uint SIXTYFOUR_K = 64 * 1024;
		public const uint SIXTEEN_K = 16 * 1024;
		public const uint ONE_K = 1024;

		public static SafeCoTaskMemHandle s_keyDataBuf = new(16 * 1024);
		public static SafeCoTaskMemHandle s_certBuf = new(16 * 1024);
		public static SafeCoTaskMemHandle s_fileBuf = new(64 * 1024);

		/****************************************************************************++

		Description :

		This function creates a well known sid using User domain. CreateWellKnownSid requires
		domain sid to be provided to generate such sids. This function first gets the domain sid
		out of the user information in the token and then generate a well known sid.

		Arguments:

		hToken - [supplies] The token for which sid has to be generated
		sidType - [supplies] The type of well known sid
		pSid - [receives] The newly create sid
		pdwSidSize - [Supplies/Receives] The size of the memory allocated for ppSid

		Returns:

		Errors returned by GetTokenInformation
		Errors returned by CreateWellKnownSid
		E_OUTOFMEMORY In case there is not enough memory
		Errors returned by GetWindowsAccountDomainSid
		--***************************************************************************/
		static SafePSID CreateWellKnownSidForAccount(HTOKEN hToken, WELL_KNOWN_SID_TYPE sidType)
		{
			var hTok = new SafeHTOKEN((IntPtr)hToken, false);
			using var pUserToken = hTok.GetInfo(TOKEN_INFORMATION_CLASS.TokenUser);

			//
			// Now get the domain sid from the TokenUser
			//
			Win32Error.ThrowLastErrorIfFalse(GetWindowsAccountDomainSid(pUserToken.ToStructure<TOKEN_USER>().User.Sid, out var pDomainSid));

			return SafePSID.CreateWellKnown(sidType, pDomainSid);
		}

		/****************************************************************************++

		Routine Description:

		Verifies whether specified well-known SID is in the current user token

		Arguments:

		sid - one of the WELL_KNOWN_SID_TYPE consts
		hToken - Optional the token for which we want to test membership
		pfMember - [Receives] TRUE if specified sid is a member of the user token, false otherwise

		Notes:

		-

		Return Value:

		Errors returned by CreateWellKnownSid
		Errors returned by CheckTokenMembership
		--*****************************************************************************/
		static HRESULT IsMemberOf(WELL_KNOWN_SID_TYPE sid, HTOKEN hToken, out bool pfMember)
		{
			pfMember = false;

			try
			{
				var hTok = new SafeHTOKEN((IntPtr)hToken, false);
				using var pUserToken = hTok.GetInfo(TOKEN_INFORMATION_CLASS.TokenUser);

				SafePSID pSID = null;
				try
				{
					//
					// create SID for the authenticated users
					//
					pSID = SafePSID.CreateWellKnown(sid);
				}
				catch
				{
					//
					// In case of invalid-arg we might need to provide the domain, so create well known sid for domain
					//
					pSID = CreateWellKnownSidForAccount(hToken, sid);
				}

				//
				// check whether token has this sid
				//
				if (!CheckTokenMembership(hToken, pSID, out pfMember))
				{
					var hr = Win32Error.GetLastError().ToHRESULT();

					// just to be on the safe side (as we don't know that CheckTokenMembership
					// does not modify fAuthenticated in case of error)
					pfMember = false;
					if (hr == HRESULT.E_ACCESSDENIED && hToken.IsNull)
					{
						// unable to query the thread token. Open as self and try again
						using var ttok = SafeHTOKEN.FromThread(GetCurrentThread(), TokenAccess.TOKEN_QUERY);
						{
							if (CheckTokenMembership(ttok, pSID, out pfMember))
							{
								return HRESULT.S_OK;
							}
							else
							{
								// stick with the original error code, but ensure that fMember is correct
								pfMember = false;
							}
						}
					}
				}

				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				var hr = HRESULT.FromException(ex);
				if (hr == (HRESULT)Win32Error.ERROR_NON_ACCOUNT_SID)
					return HRESULT.S_OK;
				return hr;
			}
		}

		// helper used by CreateCryptProv
		static HRESULT IsServiceAccount(out bool pfMember)
		{
			var hr = IsMemberOf(WELL_KNOWN_SID_TYPE.WinLocalServiceSid, default, out pfMember);
			if (hr.Succeeded && !pfMember)
			{
				hr = IsMemberOf(WELL_KNOWN_SID_TYPE.WinLocalSystemSid, default, out pfMember);
				if (hr.Succeeded && !pfMember)
				{
					hr = IsMemberOf(WELL_KNOWN_SID_TYPE.WinNetworkServiceSid, default, out pfMember);
				}
			}
			return hr;
		}

		/****************************************************************************++

		Routine Description:

		Deletes the key container and the keys

		Arguments:

		pwzContainer -

		Notes:

		-

		Return Value:

		- S_OK

		- or -

		- no other errors are expected

		--*****************************************************************************/
		static HRESULT DeleteKeys(string pwzContainer)
		{
			var hr = IsServiceAccount(out var fServiceAccount);
			if (hr.Succeeded)
			{
				//
				// this is the most counter-intuitive API that i have seen in my life
				// in order to delete the contanier and all the keys in it, i have to call CryptAcquireContext
				//
				if (!CryptAcquireContext(out var hCryptProv, pwzContainer, default, DEFAULT_PROV_TYPE,
					fServiceAccount ? CryptAcquireContextFlags.CRYPT_DELETEKEYSET | CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET : CryptAcquireContextFlags.CRYPT_DELETEKEYSET))
				{
					hr = Win32Error.GetLastError().ToHRESULT();
				}
				hCryptProv.Dispose();
			}
			return hr;
		}

		/****************************************************************************++

		Routine Description:

		Creates a handle to the CSP

		Arguments:

		pwzContainerName - name of the container to be created. if default, Guid is generated
		for the name of the container

		fCreateNewKeys - forces new keys to be created

		phCryptProv - pointer to the location, where handle should be returned

		Notes:

		-

		Return Value:

		- S_OK

		- or -

		- CAPI error returned by CryptAcquireContextW

		--*****************************************************************************/
		static HRESULT CreateCryptProv(string pwzContainerName, bool fCreateNewKeys, out SafeHCRYPTPROV phCryptProv)
		{
			bool fCreatedContainer = false;

			phCryptProv = null;

			if (pwzContainerName is null)
			{
				pwzContainerName = Guid.NewGuid().ToString();

				var hr = IsServiceAccount(out var fServiceAccount);
				if (hr.Failed) return hr;

				//
				// open the clean key container
				//
				// note: CRYPT_NEW_KEYSET is not creating new keys, it just
				// creates new key container. duh.
				//
				if (!CryptAcquireContext(out phCryptProv, pwzContainerName, default, DEFAULT_PROV_TYPE, fServiceAccount ?
					(CryptAcquireContextFlags.CRYPT_SILENT | CryptAcquireContextFlags.CRYPT_NEWKEYSET | CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) :
					(CryptAcquireContextFlags.CRYPT_SILENT | CryptAcquireContextFlags.CRYPT_NEWKEYSET)))
				{
					hr = Win32Error.GetLastError().ToHRESULT();

					//
					// we are seeing that CryptAcquireContextW returns NTE_FAIL under low
					// memory condition, so we just mask the error
					//
					if (HRESULT.NTE_FAIL == hr)
					{
						hr = HRESULT.E_OUTOFMEMORY;
					}
					return hr;
				}

				fCreatedContainer = true;
			}
			else
			{
				var hr = IsServiceAccount(out var fServiceAccount);
				if (hr.Failed) return hr;

				//
				// open the provider first, create the keys too
				//
				if (!CryptAcquireContext(out phCryptProv, pwzContainerName, default, DEFAULT_PROV_TYPE, fServiceAccount ?
					(CryptAcquireContextFlags.CRYPT_SILENT | CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) :
					(CryptAcquireContextFlags.CRYPT_SILENT)))
				{
					hr = Win32Error.GetLastError().ToHRESULT();

					//
					// we are seeing that CryptAcquireContextW returns NTE_FAIL under low
					// memory condition, so we just mask the error
					//
					if (HRESULT.NTE_FAIL == hr)
					{
						hr = HRESULT.E_OUTOFMEMORY;
					}
					return hr;
				}
			}

			if (fCreateNewKeys)
			{
				//
				// make sure keys exist
				//
				if (!CryptGetUserKey(phCryptProv, DEFAULT_KEY_SPEC, out _))
				{
					var hr = Win32Error.GetLastError().ToHRESULT();

					// if key does not exist, create it
					if (HRESULT.NTE_NO_KEY == hr)
					{
						if (!CryptGenKey(phCryptProv, (ALG_ID)DEFAULT_KEY_SPEC, CryptGenKeyFlags.CRYPT_EXPORTABLE, out _))
						{
							var err = Win32Error.GetLastError();

							//
							// we are seeing that CryptGenKey returns ERROR_CANTOPEN under low
							// memory condition, so we just mask the error
							//
							if (fCreatedContainer) DeleteKeys(pwzContainerName);
							return Win32Error.ERROR_CANTOPEN == err ? HRESULT.E_OUTOFMEMORY : err.ToHRESULT();
						}
					}
					else
					{
						// failed to get user key by some misterious reason, so bail out
						if (fCreatedContainer) DeleteKeys(pwzContainerName);
						return hr;
					}
				}
			}

			return HRESULT.S_OK;
		}

		/****************************************************************************++

		Routine Description:

		Retrieves the name of the CSP container.

		Arguments:

		hCryptProv - handle to the CSP

		pcChars - count of chars in the buffer on input. count of chars used on return

		pwzContainerName - pointer to output buffer

		Notes:

		-

		Return Value:

		- S_OK

		- or -

		- NTE_BAD_UID, if hCryptProv handle is not valid

		--*****************************************************************************/
		static HRESULT GetContainerName(HCRYPTPROV hCryptProv, out string pwzContainerName)
		{
			try
			{
				//
				// get the name of the key container
				//
				pwzContainerName = CryptGetProvParam<string>(hCryptProv, ProvParam.PP_CONTAINER, 0);
				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				pwzContainerName = null;
				return HRESULT.FromException(ex);
			}
		}

		// Read a single cert from a file
		public static HRESULT ReadCertFromFile(string pwzFileName, out SafePCCERT_CONTEXT ppCert, out SafeHCRYPTPROV phCryptProv)
		{
			ppCert = default;
			phCryptProv = null;

			// read local cert into *ppLocalCert, allocating memory
			using (var pFile = System.IO.File.Open(pwzFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
				pFile.Read(s_fileBuf.AsBytes());

			var blob = new CRYPTOAPI_BLOB { cbData = s_fileBuf.Size, pbData = s_fileBuf };

			HCERTSTORE hCertStore = PFXImportCertStore(blob, "DRT Rocks!", PFXImportFlags.CRYPT_EXPORTABLE);
			if (hCertStore.IsNull)
				return Win32Error.GetLastError().ToHRESULT();

			// TODO: does this have to be a c style cast? I get compile errors if I try reinterpret or static cast
			// the first cert is always the leaf cert (since we encoded it that way)
			var pCert = CertEnumCertificatesInStore(hCertStore, default);
			if (pCert.IsNull)
				return Win32Error.GetLastError().ToHRESULT();
			ppCert = new SafePCCERT_CONTEXT((IntPtr)pCert);

			// retreive the crypt provider which has the private key for this certificate
			if (!CryptAcquireCertificatePrivateKey(ppCert, CryptAcquireFlags.CRYPT_ACQUIRE_SILENT_FLAG | CryptAcquireFlags.CRYPT_ACQUIRE_COMPARE_KEY_FLAG,
				default, out var hCryptProv, out _, out _))
				return Win32Error.GetLastError().ToHRESULT();

			// make sure provider stays around for duration of the test run. We need hCryptProv of root cert to sign local certs
			CryptContextAddRef(hCryptProv);

			// everything succeeded, safe to set outparam
			phCryptProv = new SafeHCRYPTPROV(hCryptProv);

			return HRESULT.S_OK;
		}


		// helper function to write the cert store out to a file
		public static HRESULT WriteStoreToFile(HCERTSTORE hCertStore, string pwzFileName)
		{
			var blob = new CRYPTOAPI_BLOB { cbData = s_fileBuf.Size, pbData = s_fileBuf };
			if (!PFXExportCertStore(hCertStore, ref blob, "DRT Rocks!", PFXExportFlags.EXPORT_PRIVATE_KEYS))
				return Win32Error.GetLastError().ToHRESULT();

			using (var pFile = System.IO.File.OpenWrite(pwzFileName))
				pFile.Write(s_fileBuf.AsBytes());

			return HRESULT.S_OK;
		}

		// helper function used by make certs to encode a name for storage in a cert (modified from drt\test\drtcert\main.cpp)
		static HRESULT EncodeName(string pwzName, ref uint pcbEncodedName, IntPtr pbEncodedName)
		{
			using var pName = new SafeCoTaskMemString(pwzName, CharSet.Ansi);
			var rdnAttr = new SafeCoTaskMemStruct<CERT_RDN_ATTR>(new CERT_RDN_ATTR
			{
				dwValueType = CertRDNType.CERT_RDN_UNICODE_STRING,
				pszObjId = AttrOID.szOID_COMMON_NAME,
				Value = new CRYPTOAPI_BLOB { cbData = pName.Size, pbData = pName },
			});
			var rdn = new SafeCoTaskMemStruct<CERT_RDN>(new CERT_RDN { cRDNAttr = 1, rgRDNAttr = rdnAttr });
			var nameInfo = new SafeCoTaskMemStruct<CERT_NAME_INFO>(new CERT_NAME_INFO { cRDN = 1, rgRDN = rdn });

			if (!CryptEncodeObject(DEFAULT_ENCODING, 7 /*X509_NAME*/, nameInfo, pbEncodedName, ref pcbEncodedName))
			{
				return Win32Error.GetLastError().ToHRESULT();
			}

			return HRESULT.S_OK;
		}

		/// <summary>
		/// The <c>CERT_RDN_ATTR</c> structure contains a single attribute of a relative distinguished name (RDN). A whole RDN is expressed
		/// in a CERT_RDN structure that contains an array of <c>CERT_RDN_ATTR</c> structures.
		/// </summary>
		// https://docs.microsoft.com/en-us/windows/win32/api/wincrypt/ns-wincrypt-cert_rdn_attr
		// typedef struct _CERT_RDN_ATTR { LPSTR pszObjId; DWORD dwValueType; CERT_RDN_VALUE_BLOB Value; } CERT_RDN_ATTR, *PCERT_RDN_ATTR;
		[PInvokeData("wincrypt.h", MSDNShortId = "NS:wincrypt._CERT_RDN_ATTR")]
		[StructLayout(LayoutKind.Sequential)]
		public struct CERT_RDN_ATTR
		{
			/// <summary>
			/// <para>
			/// Object identifier (OID) for the type of the attribute defined in this structure. This member can be one of the <see cref="AttrOID"/> values.
			/// </para>
			/// <list type="table">
			/// <listheader>
			/// <term>Value</term>
			/// <term>Meaning</term>
			/// </listheader>
			/// <item>
			/// <term>szOID_AUTHORITY_REVOCATION_LIST</term>
			/// <term>Security attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_BUSINESS_CATEGORY</term>
			/// <term>Case-insensitive string. Explanatory attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_CA_CERTIFICATE</term>
			/// <term>Security attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_CERTIFICATE_REVOCATION_LIST</term>
			/// <term>Security attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_COMMON_NAME</term>
			/// <term>Case-insensitive string. Labeling attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_COUNTRY_NAME</term>
			/// <term>Two-character printable string. Geographic attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_CROSS_CERTIFICATE_PAIR</term>
			/// <term>Security attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_DESCRIPTION</term>
			/// <term>Case-insensitive string. Explanatory attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_DESTINATION_INDICATOR</term>
			/// <term>Printable string. Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_DEVICE_SERIAL_NUMBER</term>
			/// <term>Printable string. Labeling attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_DOMAIN_COMPONENT</term>
			/// <term>IA5 string. DNS name component such as "com."</term>
			/// </item>
			/// <item>
			/// <term>szOID_FACSIMILE_TELEPHONE_NUMBER</term>
			/// <term>Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_GIVEN_NAME</term>
			/// <term>Case-insensitive string. Name attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_INITIALS</term>
			/// <term>Case-insensitive string. Name attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_INTERNATIONAL_ISDN_NUMBER</term>
			/// <term>Numeric string. Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_LOCALITY_NAME</term>
			/// <term>Case-insensitive string. Geographic attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_MEMBER</term>
			/// <term>Relational application attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_ORGANIZATION_NAME</term>
			/// <term>Case-insensitive string. Organizational attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_ORGANIZATIONAL_UNIT_NAME</term>
			/// <term>Case-insensitive string. Organizational attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_OWNER</term>
			/// <term>Relational application attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_PHYSICAL_DELIVERY_OFFICE_NAME</term>
			/// <term>Case-insensitive string. Postal addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_PKCS_12_FRIENDLY_NAME_ATTR</term>
			/// <term>PKCS #12 attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_PKCS_12_LOCAL_KEY_ID</term>
			/// <term>PKCS #12 attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_POST_OFFICE_BOX</term>
			/// <term>Case-insensitive string. Postal addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_POSTAL_ADDRESS</term>
			/// <term>Printable string. Postal addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_POSTAL_CODE</term>
			/// <term>Case-insensitive string. Postal addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_PREFERRED_DELIVERY_METHOD</term>
			/// <term>Preference attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_PRESENTATION_ADDRESS</term>
			/// <term>OSI application attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_REGISTERED_ADDRESS</term>
			/// <term>Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_ROLE_OCCUPANT</term>
			/// <term>Relational application attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_RSA_emailAddr</term>
			/// <term>IA5 string. Email attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_SEARCH_GUIDE</term>
			/// <term>Explanatory attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_SEE_ALSO</term>
			/// <term>Relational application attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_STATE_OR_PROVINCE_NAME</term>
			/// <term>Case-insensitive string. Geographic attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_STREET_ADDRESS</term>
			/// <term>Case-insensitive string. Geographic attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_SUPPORTED_APPLICATION_CONTEXT</term>
			/// <term>OSI application attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_SUR_NAME</term>
			/// <term>Case-insensitive string. Labeling attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_TELEPHONE_NUMBER</term>
			/// <term>Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_TELETEXT_TERMINAL_IDENTIFIER</term>
			/// <term>Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_TELEX_NUMBER</term>
			/// <term>Telecommunications addressing attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_TITLE</term>
			/// <term>Case-insensitive string. Organizational attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_USER_CERTIFICATE</term>
			/// <term>Security attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_USER_PASSWORD</term>
			/// <term>Security attribute.</term>
			/// </item>
			/// <item>
			/// <term>szOID_X21_ADDRESS</term>
			/// <term>Numeric string. Telecommunications addressing attribute.</term>
			/// </item>
			/// </list>
			/// </summary>
			[MarshalAs(UnmanagedType.LPStr)]
			public string pszObjId;

			/// <summary>
			/// <para>Indicates the interpretation of the <c>Value</c> member.</para>
			/// <para>This member can be one of the following values.</para>
			/// <list type="table">
			/// <listheader>
			/// <term>Value</term>
			/// <term>Meaning</term>
			/// </listheader>
			/// <item>
			/// <term>CERT_RDN_ANY_TYPE</term>
			/// <term>The pszObjId member determines the assumed type and length.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_BMP_STRING</term>
			/// <term>An array of Unicode characters (16-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_ENCODED_BLOB</term>
			/// <term>An encoded data BLOB.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_GENERAL_STRING</term>
			/// <term>Currently not used.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_GRAPHIC_STRING</term>
			/// <term>Currently not used.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_IA5_STRING</term>
			/// <term>An arbitrary string of IA5 (ASCII) characters.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_INT4_STRING</term>
			/// <term>An array of INT4 elements (32-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_ISO646_STRING</term>
			/// <term>A 128-character set (8-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_NUMERIC_STRING</term>
			/// <term>Only the characters 0 through 9 and the space character (8-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_OCTET_STRING</term>
			/// <term>An arbitrary string of octets (8-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_PRINTABLE_STRING</term>
			/// <term>An arbitrary string of printable characters (8-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_T61_STRING</term>
			/// <term>An arbitrary string of T.61 characters (8-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_TELETEX_STRING</term>
			/// <term>An arbitrary string of T.61 characters (8-bit)</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_UNICODE_STRING</term>
			/// <term>An array of Unicode characters (16-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_UNIVERSAL_STRING</term>
			/// <term>An array of INT4 elements (32-bit).</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_UTF8_STRING</term>
			/// <term>An array of 16 bit Unicode characters UTF8 encoded on the wire as a sequence of one, two, or three, eight-bit characters.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_VIDEOTEX_STRING</term>
			/// <term>An arbitrary string of videotext characters.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_VISIBLE_STRING</term>
			/// <term>A 95-character set (8-bit).</term>
			/// </item>
			/// </list>
			/// <para>The following flags can be combined by using a bitwise- <c>OR</c> operation into the <c>dwValueType</c> member.</para>
			/// <list type="table">
			/// <listheader>
			/// <term>Value</term>
			/// <term>Meaning</term>
			/// </listheader>
			/// <item>
			/// <term>CERT_RDN_DISABLE_CHECK_TYPE_FLAG</term>
			/// <term>For encoding. When set, the characters are not checked to determine whether they are valid for the value type.</term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_DISABLE_IE4_UTF8_FLAG</term>
			/// <term>
			/// For decoding. By default, CERT_RDN_T61_STRING encoded values are initially decoded as UTF8. If the UTF8 decoding fails, the
			/// value is decoded as 8-bit characters. If this flag is set, it skips the initial attempt to decode as UTF8 and decodes the
			/// value as 8-bit characters.
			/// </term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_ENABLE_T61_UNICODE_FLAG</term>
			/// <term>
			/// For encoding. When set, if all the Unicode characters are &lt;= 0xFF, the CERT_RDN_T61_STRING value is selected instead of
			/// the CERT_RDN_UNICODE_STRING value.
			/// </term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_ENABLE_UTF8_UNICODE_FLAG</term>
			/// <term>
			/// For encoding. When set, strings are encoded with the CERT_RDN_UTF8_STRING value instead of the CERT_RDN_UNICODE_STRING value.
			/// </term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_FORCE_UTF8_UNICODE_FLAG</term>
			/// <term>
			/// For encoding. When set, strings are encoded with the CERT_RDN_UTF8_STRING value instead of CERT_RDN_PRINTABLE_STRING value
			/// for DirectoryString types. In addition, CERT_RDN_ENABLE_UTF8_UNICODE_FLAG is enabled. Windows Vista, Windows Server 2003 and
			/// Windows XP: This flag is not supported.
			/// </term>
			/// </item>
			/// <item>
			/// <term>CERT_RDN_ENABLE_PUNYCODE_FLAG</term>
			/// <term>
			/// For encoding. If the string contains an email RDN, and the email address is Punycode encoded, then the resultant email
			/// address is encoded as an IA5String. The Punycode encoding of the host name is performed on a label-by-label basis. For
			/// decoding. If the name contains an email RDN, and the local part or host name portion of the email address contains a
			/// Punycode encoded IA5String, the RDN string value is converted to its Unicode equivalent. Windows Server 2008, Windows Vista,
			/// Windows Server 2003 and Windows XP: This flag is not supported.
			/// </term>
			/// </item>
			/// </list>
			/// </summary>
			public CertRDNType dwValueType;

			/// <summary>
			/// <para>
			/// A CERT_RDN_VALUE_BLOB that contains the attribute value. The <c>cbData</c> member of <c>Value</c> is the length, in bytes,
			/// of the <c>pbData</c> member. It is not the number of elements in the <c>pbData</c> string.
			/// </para>
			/// <para>
			/// For example, a <c>DWORD</c> is 32 bits or 4 bytes long. If <c>pbData</c> is a <c>DWORD</c> array, <c>cbData</c> would be
			/// four times the number of <c>DWORD</c> elements in the array. A <c>SHORT</c> is 16 bits or 2 bytes long. If <c>pbData</c> is
			/// an array of <c>SHORT</c> elements, <c>cbData</c> must be two times the length of the array.
			/// </para>
			/// <para>
			/// The <c>pbData</c> member of <c>Value</c> can be a null-terminated array of 8-bit or 16-bit characters or a fixed-length
			/// array of elements. If <c>dwValueType</c> is set to CERT_RDN_ENCODED_BLOB, <c>pbData</c> is encoded.
			/// </para>
			/// </summary>
			public CRYPTOAPI_BLOB Value;
		}

		// manufacture a single cert and export it to a file
		static HRESULT MakeAndExportACert(string pwzSignerName, string pwzCertName, string pwzFileName, HCERTSTORE hCertStore,
			HCRYPTPROV hCryptProvSigner, HCRYPTPROV hCryptProvThisCert, PCCERT_CONTEXT pIssuerCertContext)
		{
			// encode the names for use in a cert
			uint cSignerName = ONE_K;
			using var pbSignerName = new SafeCoTaskMemHandle(ONE_K);
			var hr = EncodeName(pwzSignerName, ref cSignerName, pbSignerName);
			if (hr.Failed)
				return hr;

			uint cCertName = ONE_K;
			using var pbCertName = new SafeCoTaskMemHandle(ONE_K);
			hr = EncodeName(pwzCertName, ref cCertName, pbCertName);
			if (hr.Failed)
				return hr;

			// first retrieve the public key from the hCryptProv (which abstracts the key pair)
			uint dwSize = s_keyDataBuf.Size;
			var bRet = CryptExportPublicKeyInfo(hCryptProvThisCert, DEFAULT_KEY_SPEC, DEFAULT_ENCODING, s_keyDataBuf, ref dwSize);
			if (!bRet)
				return Win32Error.GetLastError().ToHRESULT();

			// set the cert properties
			using var serialNumberBuf = new SafeCoTaskMemHandle(16);
			var certInfo = new CERT_INFO();
			certInfo.dwVersion = 2 /*CERT_V3*/;
			certInfo.SerialNumber.cbData = serialNumberBuf.Size;
			certInfo.SerialNumber.pbData = serialNumberBuf;
			certInfo.SignatureAlgorithm.pszObjId = (IntPtr)DEFAULT_ALGORITHM;
			var ullTime = DateTime.Now;
			certInfo.NotBefore = ullTime.AddDays(-1).ToFileTimeStruct();
			certInfo.NotAfter = ullTime.AddDays(365).ToFileTimeStruct();
			certInfo.Issuer.cbData = cSignerName;
			certInfo.Issuer.pbData = pbSignerName;
			certInfo.Subject.cbData = cCertName;
			certInfo.Subject.pbData = pbCertName;
			certInfo.SubjectPublicKeyInfo = s_keyDataBuf.ToStructure<CERT_PUBLIC_KEY_INFO>();
			using var pCertInfo = new SafeCoTaskMemStruct<CERT_INFO>(certInfo);

			// create the cert
			uint cCertBuf = s_certBuf.Size;
			bRet = CryptSignAndEncodeCertificate(hCryptProvSigner, // Crypto provider
				DEFAULT_KEY_SPEC, // Key spec, we always use the same
				DEFAULT_ENCODING, // Encoding type, default
				2 /*X509_CERT_TO_BE_SIGNED*/, // Structure type - certificate
				pCertInfo, // Structure information
				certInfo.SignatureAlgorithm, // Signature algorithm
				default, // reserved, must be default
				s_certBuf, // hopefully it will fit in 1K
				ref cCertBuf);
			if (!bRet)
				return Win32Error.GetLastError().ToHRESULT();

			// retrieve the cert context. pCertContext gets a pointer into the crypto api heap, we must treat it as read only. 
			// pCertContext must be freed with CertFreeCertificateContext(p); we use a smart pointer to do the free
			using var pCertContext = CertCreateCertificateContext(DEFAULT_ENCODING, s_certBuf, cCertBuf);
			if (pCertContext.IsInvalid)
				return Win32Error.GetLastError().ToHRESULT();


			// next attach the private key
			// =================


			// retrieve container name
			hr = GetContainerName(hCryptProvThisCert, out var wzContainer);
			if (hr.Failed)
				return hr;

			// set up key info struct for CAPI call
			using var keyInfo = new SafeCoTaskMemStruct<CRYPT_KEY_PROV_INFO>(new CRYPT_KEY_PROV_INFO
			{
				pwszContainerName = wzContainer,
				pwszProvName = default,
				dwProvType = DEFAULT_PROV_TYPE,
				dwKeySpec = DEFAULT_KEY_SPEC
			});

			// attach private key
			bRet = CertSetCertificateContextProperty(pCertContext, CertPropId.CERT_KEY_PROV_INFO_PROP_ID, 0, keyInfo);
			if (!bRet)
				return Win32Error.GetLastError().ToHRESULT();

			// put the cert into the store
			bRet = CertAddCertificateContextToStore(hCertStore, pCertContext, CertStoreAdd.CERT_STORE_ADD_NEW);
			if (!bRet)
				return Win32Error.GetLastError().ToHRESULT();

			// make sure the issuer cert is also in the store, if we have an issuer for the cert (ie, the non self signed case)
			if (!pIssuerCertContext.IsNull)
			{
				bRet = CertAddCertificateContextToStore(hCertStore, pIssuerCertContext, CertStoreAdd.CERT_STORE_ADD_NEW);
				if (!bRet)
					return Win32Error.GetLastError().ToHRESULT();
			}

			// now export the cert to a file
			hr = WriteStoreToFile(hCertStore, pwzFileName);

			return hr;
		}

		// using cryptoApi, make a cert and export it. If there is an existing root cert, this will
		// use the existing root cert to sign the local cert.
		// export a local cert to the file <currentdir>\LocalCert.cer
		// export a root cert to the file <currentDir>\RootCert.cer (if one does not already exist)
		public static HRESULT MakeCert(string pwzLocalCertFileName, string pwzLocalCertName, string pwzIssuerCertFileName, string pwzIssuerCertName)
		{
			SafeHCRYPTPROV hCryptProvIssuer = new(IntPtr.Zero, false);
			SafePCCERT_CONTEXT pIssuerCert = new(IntPtr.Zero, false);

			// If there is an issuer cert, make sure it exists
			if (pwzIssuerCertFileName != null)
			{
				var _hr = ReadCertFromFile(pwzIssuerCertFileName, out pIssuerCert, out hCryptProvIssuer);
				if (_hr.Failed)
					return _hr;
			}

			// create this cert key pair (util function from peernet\common)
			var hr = CreateCryptProv(default, true, out var hCryptProvThis);
			if (hr.Failed)
				return hr;

			// create cert store
			using var hSelfCertStore = CertOpenStore(2 /*CERT_STORE_PROV_MEMORY*/, 0, default, CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG | CertStoreFlags.CERT_STORE_NO_CRYPT_RELEASE_FLAG);
			if (hSelfCertStore.IsNull)
				return Win32Error.GetLastError().ToHRESULT();


			// Make the self signed cert, and save it to a file
			hr = MakeAndExportACert(pwzLocalCertName, pwzLocalCertName, pwzLocalCertFileName, hSelfCertStore, hCryptProvThis, hCryptProvThis, default);
			if (hr.Failed)
				return hr;

			// then, sign it if an issuer name was supplied
			// (surprisingly, the same function does both, since it adds a signing record to existing cert)
			// FUTURE: this is a bit inefficient, since we write the file twice, we can add a fWrite paramater, and not write the file when it is false
			if (pwzIssuerCertFileName != null)
			{
				// must create a separate store, or we end up with a single cert and a chain cert in same store, apps can pick wrong cert
				using var hSignedCertStore = CertOpenStore(2 /*CERT_STORE_PROV_MEMORY*/, 0, default, CertStoreFlags.CERT_STORE_CREATE_NEW_FLAG | CertStoreFlags.CERT_STORE_NO_CRYPT_RELEASE_FLAG);
				if (hSignedCertStore.IsNull)
					return Win32Error.GetLastError().ToHRESULT();

				hr = MakeAndExportACert(pwzIssuerCertName, pwzLocalCertName, pwzLocalCertFileName, hSignedCertStore, hCryptProvIssuer, hCryptProvThis, pIssuerCert);
			}
			return hr;
		}
	}
}
