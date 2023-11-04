using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Drt;
using static Vanara.PInvoke.Ws2_32;

namespace DrtSdkSample
{
	// Class:    CCustomNullSecuredAddressPayload
	//
	// Purpose:  Class for implementing the (de)serialization of a the SecuredAddressPayload.
	internal unsafe class CCustomNullSecuredAddressPayload : IDisposable
	{
		public const ALG_ID DRT_ALGORITHM = ALG_ID.CALG_SHA_256;
		public const string DRT_ALGORITHM_OID = AlgOID.szOID_RSA_SHA1RSA;
		public const uint DRT_DERIVED_KEY_SIZE = 32;

		// default security provider constants
		public const byte DRT_SECURITY_VERSION_MAJOR = 1;

		public const byte DRT_SECURITY_VERSION_MINOR = 0;
		public const uint DRT_SHA2_LENGTH = 32;
		public const uint DRT_SIG_LENGTH = SHA2_SIG_LENGTH;

		// Original 0x8000 + space for extended payload (4k plus some overhead)
		public const uint MAX_MESSAGE_SIZE = 0x8000 + 0x1200;

		public const uint SHA1_SIG_LENGTH = 0x80;
		public const uint SHA2_SIG_LENGTH = 0x80;

		private readonly byte[] m_signature = new byte[DRT_SIG_LENGTH];
		private SafeCoTaskMemStruct<SOCKET_ADDRESS_LIST> m_addressList;
		private byte m_bProtocolVersionMajor;
		private byte m_bProtocolVersionMinor;
		private DRT_DATA m_ddKey;
		private DRT_DATA m_ddNonce;
		private bool m_fAllocated; // set if the data needs to be freed when destroyed (true when deserializing)

								   //CERT_PUBLIC_KEY_INFO*
		private SafeCoTaskMemStruct<CERT_PUBLIC_KEY_INFO> m_pPublicKey;

		public CCustomNullSecuredAddressPayload()
		{
		}

		// Purpose: Retrieve or set the flags
		//
		// Args: dwFlags:
		public uint Flags { get; set; }

		// Serialized SecureAddressPayload format: bytes name 1 protocol major version 1 protocol minor version 1 security major version 1
		// security minor version 2 key length (KL) KL key 1 signature length (SL) SL signature 1 nonce length (NL) NL nonce 4 flags
		// ----- public key ----------- 1 algorithm length (AL) 2 key parameters length (PL) 2 public key length (KL) 1 unused bits AL
		// algorithm (byte) PL key parameters KL public key
		// ----- end public key ------- 1 address count
		// ----- for each address ----- 2 address length (AL) AL address data
		// ----- end each address -----
		// Function: CCustomNullSecuredAddressPayload::DeserializeAndValidate
		//
		// Purpose: Deserialize and validate the payload.
		//
		// Args: pData: data to deserialize
		// pNonce: expected nonce
		// pCertChain: opt. remote cert chain (if one was in the message)
		// hCryptProv: crypt provider to use with remote public key
		//
		// Notes: The deserialized data is later retrieved via Get* methods.
		public HRESULT DeserializeAndValidate(in DRT_DATA pData, DRT_DATA* pNonce)
		{
			HRESULT hr = HRESULT.S_OK;

			using var deserializer = new NativeMemoryStream(pData.pb, pData.cb);
			m_fAllocated = true;

			// protocol version
			m_bProtocolVersionMajor = (byte)deserializer.ReadByte();
			m_bProtocolVersionMinor = (byte)deserializer.ReadByte();

			// security version
			var bVersionMajor = (byte)deserializer.ReadByte();
			var bVersionMinor = (byte)deserializer.ReadByte();

			// ensure we are receiving a version we understand
			if (bVersionMajor != DRT_SECURITY_VERSION_MAJOR || bVersionMinor != DRT_SECURITY_VERSION_MINOR)
			{
				hr = HRESULT.DRT_E_INVALID_MESSAGE;
				goto cleanup;
			}

			// extract key
			var cb = deserializer.Read<ushort>();
			m_ddKey = new SafeDRT_DATA(cb);
			deserializer.ReadToPtr(m_ddKey.pb, cb);

			// extract signature
			var b = (byte)deserializer.ReadByte();
			if (b != DRT_SIG_LENGTH)
			{
				hr = HRESULT.DRT_E_INVALID_MESSAGE;
				goto cleanup;
			}

			var pbSignature = deserializer.Position;
			deserializer.Position += DRT_SIG_LENGTH; //deserializer.ReadArray(DRT_SIG_LENGTH, &ddSignature);

			// extract and validate nonce
			cb = (byte)deserializer.ReadByte();
			m_ddNonce = new SafeDRT_DATA(cb);
			deserializer.ReadToPtr(m_ddNonce.pb, cb);

			// if a nonce was supplied, ensure it matches the nonce in the message
			if (pNonce != null && (hr = CompareNonce(*pNonce)).Failed)
			{
				goto cleanup;
			}

			// extract flags
			Flags = deserializer.Read<uint>();

			// extract public key
			hr = ReadPublicKey(deserializer, out m_pPublicKey);

			// extract addresses
			var addressList = new SOCKET_ADDRESS_LIST { iAddressCount = (byte)deserializer.ReadByte() };
			addressList.Address = new SOCKET_ADDRESS[addressList.iAddressCount];
			for (var i = 0; i < addressList.iAddressCount; i++)
			{
				addressList.Address[i] = new SOCKET_ADDRESS { iSockaddrLength = deserializer.Read<ushort>() };
				// Store just the pointer and then pull that into the packed object
				addressList.Address[i].lpSockaddr = deserializer.Pointer.Offset(deserializer.Position);
				deserializer.Seek(addressList.Address[i].iSockaddrLength, System.IO.SeekOrigin.Current);
			}

			m_addressList = addressList.Pack();

			if (deserializer.Position != deserializer.Length)
			{
				hr = HRESULT.DRT_E_INVALID_MESSAGE;
				goto cleanup;
			}

			cleanup:
			// the remaining allocated memory is Marshal.FreeCoTaskMem in the destructor, or ownership is passed via GetAddresses
			return hr;
		}

		public void Dispose()
		{
			m_addressList?.Dispose();
			m_pPublicKey?.Dispose();
			if (m_fAllocated)
			{
				Marshal.FreeCoTaskMem(m_ddKey.pb);
				Marshal.FreeCoTaskMem(m_ddNonce.pb);
			}
		}

		// Purpose: Retrieve the addresses. This returns the memory allocated during de-serialization, so can only be called once. Since it
		// will only be called once, there isn't benefit to making another copy of the data.
		public void GetAddresses(out SafeCoTaskMemStruct<SOCKET_ADDRESS_LIST> pAddressList)
		{
			pAddressList = m_addressList;
			// this object no longer owns the address list
			m_addressList = default;
		}

		// Purpose: Retrieve the key deserialized earlier. This returns memory allocated during deserialization, and passes ownership to the
		// caller. This method may only be called once.
		public void GetKey(out DRT_DATA pData)
		{
			pData = m_ddKey;
			// this object no longer owns the public key
			m_ddKey = default;
		}

		// Purpose: Retrieve the flags
		public void GetProtocolVersion(out byte pbMajor, out byte pbMinor)
		{
			pbMajor = m_bProtocolVersionMajor;
			pbMinor = m_bProtocolVersionMinor;
		}

		// Purpose: Retrieve the public key deserialized earlier. This returns memory allocated during deserialization, and passes ownership
		// to the caller. This method may only be called once.
		public void GetPublicKey(out SafeCoTaskMemStruct<CERT_PUBLIC_KEY_INFO> pKey)
		{
			pKey = m_pPublicKey;
			m_pPublicKey = null;
		}

		// Purpose: Serialize the SecuredAddressPayload according to the format specified above, and sign it using the specified credentials.
		//
		// Args: pCertChain: [out] pData: serialized/signed data. pData->pb is allocated.
		//
		// Notes: The data to be serialized has already been set using the Set* methods.
		public HRESULT SerializeAndSign(out DRT_DATA pData)
		{
			CERT_PUBLIC_KEY_INFO publicKey = default;
			using var emptyAddress = new SafeCoTaskMemString("0.0.0.0", CharSet.Ansi);
			publicKey.Algorithm.pszObjId = (IntPtr)emptyAddress;
			publicKey.PublicKey.cbData = sizeof(uint);
			var dwBaadFood = 0xbaadf00d;
			publicKey.PublicKey.pbData = (IntPtr)(&dwBaadFood);

			pData = default;

			uint cbAlgorithmId = emptyAddress.Size;

			// validate that the lengths are all reasonable (fit in the space provided for their count)
			var addressList = m_addressList.Value;
			if (m_ddNonce.cb > byte.MaxValue ||
				addressList.iAddressCount > byte.MaxValue ||
				m_ddKey.cb > ushort.MaxValue || cbAlgorithmId > byte.MaxValue ||
				publicKey.Algorithm.Parameters.cbData > ushort.MaxValue ||
				publicKey.PublicKey.cbData > ushort.MaxValue ||
				publicKey.PublicKey.cUnusedBits > byte.MaxValue)
			{
				return HRESULT.E_INVALIDARG;
			}

			// serialize away
			using var mem = new SafeCoTaskMemHandle(1024);
			var ddDataPtr = new NativeMemoryStream(mem);

			// protocol version
			ddDataPtr.Write(m_bProtocolVersionMajor);
			ddDataPtr.Write(m_bProtocolVersionMinor);

			// security version
			ddDataPtr.Write(DRT_SECURITY_VERSION_MAJOR);
			ddDataPtr.Write(DRT_SECURITY_VERSION_MINOR);

			// key
			ddDataPtr.Write((ushort)m_ddKey.cb);
			ddDataPtr.WriteFromPtr(m_ddKey.pb, m_ddKey.cb);

			// skip over the signature for now (leave it zero while we calculate the signature)
			ddDataPtr.Write((byte)DRT_SIG_LENGTH);
			var pbSignature = ddDataPtr.Position; // save the location of the signature for later
			ddDataPtr.Position += DRT_SIG_LENGTH;

			// nonce
			ddDataPtr.Write((byte)m_ddNonce.cb);
			ddDataPtr.WriteFromPtr(m_ddNonce.pb, m_ddNonce.cb);

			// flags
			ddDataPtr.Write(Flags);

			// public key sizes
			ddDataPtr.Write((byte)cbAlgorithmId);
			ddDataPtr.Write((ushort)publicKey.Algorithm.Parameters.cbData);
			ddDataPtr.Write((ushort)publicKey.PublicKey.cbData);
			ddDataPtr.Write((byte)publicKey.PublicKey.cUnusedBits);

			// public key data
			ddDataPtr.Write(publicKey.Algorithm.pszObjId.ToString(), CharSet.Ansi);
			if (publicKey.Algorithm.Parameters.cbData > 0)
				ddDataPtr.WriteFromPtr(publicKey.Algorithm.Parameters.pbData, publicKey.Algorithm.Parameters.cbData);
			ddDataPtr.WriteFromPtr(publicKey.PublicKey.pbData, publicKey.PublicKey.cbData);

			// addresses
			ddDataPtr.Write((byte)addressList.iAddressCount);
			for (var i = 0; i < addressList.iAddressCount; i++)
			{
				ddDataPtr.Write((ushort)addressList.Address[i].iSockaddrLength);
				ddDataPtr.WriteFromPtr(addressList.Address[i].lpSockaddr, addressList.Address[i].iSockaddrLength);
			}

			// pass the data back to the caller
			pData = new DRT_DATA { cb = (uint)ddDataPtr.Length, pb = mem.TakeOwnership() };

			return HRESULT.S_OK;
		}

		// Purpose: Copy the address data. The memory for the addresses is only referenced, the ownership is not passed (shallow copy).
		//
		// Args: pAddressList:
		public void SetAddresses(IntPtr pAddressList) => m_addressList = pAddressList.ToStructure<SOCKET_ADDRESS_LIST>();

		// Purpose: Copy the key DRT_DATA. The memory for the key itself is only referenced, the ownership is not passed (shallow copy).
		//
		// Args: pData:
		public void SetKey(in DRT_DATA pKey) => m_ddKey = pKey;

		// Purpose: Copy the nonce DRT_DATA. The memory for the nonce itself is only referenced, the ownership is not passed (shallow copy).
		//
		// Args: pData:
		public void SetNonce(in DRT_DATA pData) => m_ddNonce = pData;

		// Purpose: Set the protocol version
		//
		// Args: bMajor: bMinor:
		public void SetProtocolVersion(byte bMajor, byte bMinor)
		{
			m_bProtocolVersionMajor = bMajor;
			m_bProtocolVersionMinor = bMinor;
		}

		// Purpose:  Read a public key from the stream
		//
		// Args:     [out] ppPublicKey: public key allocated as a single block of memory (with self-refertial embedded pointers)
		private static HRESULT ReadPublicKey(NativeMemoryStream deserializer, out SafeCoTaskMemStruct<CERT_PUBLIC_KEY_INFO> ppPublicKey)
		{
			ppPublicKey = default;

			try
			{
				var cbAlgorithmId = (byte)deserializer.ReadByte();
				var cbParameters = deserializer.Read<ushort>();
				var cbPublicKey = deserializer.Read<ushort>();
				var cUnusedBits = (byte)deserializer.ReadByte();

				var szAlgId = cbAlgorithmId == 0 ? null : deserializer.Read<string>(CharSet.Ansi);
				var pParamData = cbParameters == 0 ? new byte[0] : deserializer.ReadArray<byte>(cbParameters, false).ToArray();
				var pKeyData = cbPublicKey == 0 ? new byte[0] : deserializer.ReadArray<byte>(cbPublicKey, false).ToArray();

				var cbTotal = sizeof(CERT_PUBLIC_KEY_INFO) + Macros.ALIGN_TO_MULTIPLE(cbAlgorithmId + 1, IntPtr.Size) +
					Macros.ALIGN_TO_MULTIPLE(cbParameters, IntPtr.Size) + Macros.ALIGN_TO_MULTIPLE(cbPublicKey, IntPtr.Size);

				var pPublicKey = new SafeCoTaskMemStruct<CERT_PUBLIC_KEY_INFO>(cbTotal);
				ref var rpk = ref pPublicKey.AsRef();
				var pbStructIter = ((IntPtr)pPublicKey).Offset(sizeof(CERT_PUBLIC_KEY_INFO)); // skip the structure

				// copy the algorithm id
				rpk.Algorithm.pszObjId = pbStructIter;
				StringHelper.Write(szAlgId, pbStructIter, out var written, true, CharSet.Ansi);
				pbStructIter += (int)Macros.ALIGN_TO_MULTIPLE(written, IntPtr.Size);

				// copy the key parameters
				if (cbParameters > 0)
				{
					rpk.Algorithm.Parameters.cbData = cbParameters;
					rpk.Algorithm.Parameters.pbData = pbStructIter;
					pbStructIter.Write(pParamData);
					pbStructIter += (int)Macros.ALIGN_TO_MULTIPLE(pParamData.Length, IntPtr.Size);
				}

				// copy the key
				rpk.PublicKey.cbData = cbPublicKey;
				rpk.PublicKey.cUnusedBits = cUnusedBits;
				rpk.PublicKey.pbData = pbStructIter;
				pbStructIter.Write(pKeyData);

				ppPublicKey = pPublicKey;

				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				return HRESULT.FromException(ex);
			}
		}

		// Purpose: Compare the nonce provided by the DRT to the nonce received on the wire, returning HRESULT.DRT_E_INVALID_MESSAGE if they
		// don't match.
		//
		// Args: pNonce:
		private HRESULT CompareNonce(in DRT_DATA pNonce)
		{
			if (!pNonce.pb.AsReadOnlySpan<byte>((int)pNonce.cb).SequenceEqual(m_ddNonce.pb.AsReadOnlySpan<byte>((int)m_ddNonce.cb)))
				return HRESULT.DRT_E_INVALID_MESSAGE;
			return HRESULT.S_OK;
		}
	}

	internal unsafe class CCustomNullSecurityProvider
	{
		public readonly DRT_SECURITY_PROVIDER provider;

		public CCustomNullSecurityProvider() =>
			// create and initialize public interface
			provider = new DRT_SECURITY_PROVIDER
			{
				SecureAndPackPayload = SecureAndPackPayload,
				ValidateAndUnpackPayload = ValidateAndUnpackPayload,
				RegisterKey = RegisterKey,
				UnregisterKey = UnregisterKey,
				FreeData = FreeData,
				Attach = Attach,
				Detach = Detach,
				EncryptData = EncryptData,
				DecryptData = DecryptData,
				ValidateRemoteCredential = ValidateRemoteCredential,
				GetSerializedCredential = GetSerializedCredential,
				SignData = SignData,
				VerifyData = VerifyData,
				pvContext = Program.MakeGCPtr(this)
			};

		private static HRESULT Attach(IntPtr pvContext) => HRESULT.S_OK;

		private static HRESULT DecryptData(IntPtr pvContext, in DRT_DATA pKeyToken, IntPtr pvKeyContext, uint dwBuffers, DRT_DATA[] pData) => HRESULT.S_OK;

		private static void Detach(IntPtr pvContext)
		{
		}

		private static HRESULT EncryptData(IntPtr pvContext, in DRT_DATA pRemoteCredential, uint dwBuffers, DRT_DATA[] pDataBuffers, DRT_DATA[] pEncryptedBuffers, out DRT_DATA pKeyToken)
		{
			HRESULT hr = HRESULT.S_OK;
			pKeyToken = default;

			//copy all input buffers into out buffers unmodified
			for (uint dwIdx = 0; dwIdx < dwBuffers; dwIdx++)
			{
				if (IntPtr.Zero == (pEncryptedBuffers[dwIdx].pb = Marshal.AllocCoTaskMem((int)(pEncryptedBuffers[dwIdx].cb = pDataBuffers[dwIdx].cb))))
				{
					while (dwIdx-- >= 1)
					{
						Marshal.FreeCoTaskMem(pEncryptedBuffers[dwIdx].pb);
					}
					hr = HRESULT.E_OUTOFMEMORY;
					goto cleanup;
				}
				pDataBuffers[dwIdx].pb.CopyTo(pEncryptedBuffers[dwIdx].pb, pEncryptedBuffers[dwIdx].cb);
			}

			cleanup:
			return hr;
		}

		private static void FreeData(IntPtr pvContext, IntPtr pv)
		{
			Marshal.FreeCoTaskMem(pv);
			Program.FreeGCPtr(pvContext);
		}

		private static HRESULT GetSerializedCredential(IntPtr pvContext, out DRT_DATA pSelfCredential)
		{
			pSelfCredential = default;
			return HRESULT.S_OK;
		}

		private static HRESULT RegisterKey(IntPtr pvContext, in DRT_REGISTRATION pRegistration, IntPtr pvKeyContext) => HRESULT.S_OK;

		private static HRESULT SecureAndPackPayload(IntPtr pvContext, IntPtr pvKeyContext, byte bProtocolMajor, byte bProtocolMinor, uint dwFlags,
			in DRT_DATA pKey, DRT_DATA* pPayload, IntPtr pAddressList, in DRT_DATA pNonce, out DRT_DATA pSecuredAddressPayload,
			DRT_DATA* pClassifier, DRT_DATA* pSecuredPayload, DRT_DATA* pCertChain)
		{
			// NULL out the out params
			pSecuredAddressPayload = default;
			if (pClassifier != null)
				*pClassifier = default;
			if (pSecuredPayload != null)
				*pSecuredPayload = default;
			if (pCertChain != null)
				*pCertChain = default;

			// set the payload contents
			var sap = new CCustomNullSecuredAddressPayload();
			sap.SetProtocolVersion(bProtocolMajor, bProtocolMinor);
			sap.SetKey(pKey);
			sap.SetAddresses(pAddressList);
			sap.SetNonce(pNonce);
			sap.Flags = dwFlags;

			var hr = sap.SerializeAndSign(out pSecuredAddressPayload);
			if (hr.Failed)
			{
				goto cleanup;
			}

			if (pPayload != null && pSecuredPayload != null)
			{
				pSecuredPayload->cb = pPayload->cb;
				pSecuredPayload->pb = Marshal.AllocCoTaskMem((int)pSecuredPayload->cb);
				if (pSecuredPayload->pb == default)
				{
					hr = HRESULT.E_OUTOFMEMORY;
					goto cleanup;
				}
				pPayload->pb.CopyTo(pSecuredPayload->pb, pSecuredPayload->cb);
			}

			// make a copy of the serialized local cert chain
			if (pCertChain != null)
			{
				pCertChain->cb = sizeof(uint);
				pCertChain->pb = Marshal.AllocCoTaskMem((int)pCertChain->cb);
				if (pCertChain->pb == default)
				{
					hr = HRESULT.E_OUTOFMEMORY;
					goto cleanup;
				}
				pCertChain->pb.Write(0xdeadbeefU, 0, sizeof(uint));
			}

			cleanup:
			// if something failed, free all the out params and NULL them out
			if (hr.Failed)
			{
				Marshal.FreeCoTaskMem(pSecuredAddressPayload.pb);
				pSecuredAddressPayload = default;
				if (pSecuredPayload != null)
				{
					Marshal.FreeCoTaskMem(pSecuredPayload->pb);
					*pSecuredPayload = default;
				}
				if (pCertChain != null)
				{
					Marshal.FreeCoTaskMem(pCertChain->pb);
					*pCertChain = default;
				}
			}

			return hr;
		}

		private static HRESULT SignData(IntPtr pvContext, uint dwBuffers, DRT_DATA[] pDataBuffers, out DRT_DATA pKeyIdentifier, out DRT_DATA pSignature)
		{
			pKeyIdentifier = default;
			pSignature = default;
			return HRESULT.S_OK;
		}

		private static HRESULT UnregisterKey(IntPtr pvContext, in DRT_DATA pKey, IntPtr pvKeyContext) => HRESULT.S_OK;

		private static HRESULT ValidateAndUnpackPayload(IntPtr pvContext, in DRT_DATA pSecuredAddressPayload, DRT_DATA* pCertChain, DRT_DATA* pClassifier,
			DRT_DATA* pNonce, DRT_DATA* pSecuredPayload, byte* pbProtocolMajor, byte* pbProtocolMinor, out DRT_DATA pKey, DRT_DATA* pPayload,
			Crypt32.CERT_PUBLIC_KEY_INFO** ppPublicKey, void** ppAddressList, out uint pdwFlags)
		{
			var sap = new CCustomNullSecuredAddressPayload();
			HRESULT hr = HRESULT.S_OK;

			// NULL out the out params
			*pbProtocolMajor = 0;
			*pbProtocolMinor = 0;
			pKey = default;
			if (pPayload != null)
				*pPayload = default;
			*ppPublicKey = null;
			pdwFlags = 0;

			// deserialize Secured Address Payload
			hr = sap.DeserializeAndValidate(pSecuredAddressPayload, pNonce);
			if (hr.Failed)
			{
				goto cleanup;
			}

			// When we asked for the payload validate signature of payload
			if (pPayload != null && pSecuredPayload != null)
			{
				pPayload->cb = pSecuredPayload->cb;
				pPayload->pb = Marshal.AllocCoTaskMem((int)pPayload->cb);
				if (pPayload->pb == default)
				{
					hr = HRESULT.E_OUTOFMEMORY;
					goto cleanup;
				}
				pSecuredPayload->pb.CopyTo(pPayload->pb, pPayload->cb);
			}

			pdwFlags = sap.Flags;

			// everything is valid, time to extract the data
			if (ppAddressList != null)
			{
				sap.GetAddresses(out var addr);
				*ppAddressList = (void*)addr.TakeOwnership();
			}
			sap.GetPublicKey(out var pk);
			*ppPublicKey = (CERT_PUBLIC_KEY_INFO*)pk.TakeOwnership();
			sap.GetKey(out pKey);
			sap.GetProtocolVersion(out *pbProtocolMajor, out *pbProtocolMinor);

			cleanup:
			// if something failed, free all the out params and NULL them out
			if (hr.Failed)
			{
				*pbProtocolMajor = 0;
				*pbProtocolMinor = 0;
				pdwFlags = 0;
				Marshal.FreeCoTaskMem(pKey.pb);
				pKey = default;
				if (pPayload != null)
				{
					Marshal.FreeCoTaskMem(pPayload->pb);
					*pPayload = default;
				}
				Marshal.FreeCoTaskMem((IntPtr)(*ppPublicKey));
				*ppPublicKey = null;

				// free all the addresses
				if (ppAddressList != null)
				{
					Marshal.FreeCoTaskMem((IntPtr)(*ppAddressList));
					*ppAddressList = null;
				}
			}

			return hr;
		}

		private static HRESULT ValidateRemoteCredential(IntPtr pvContext, in DRT_DATA pRemoteCredential) => HRESULT.S_OK;

		private static HRESULT VerifyData(IntPtr pvContext, uint dwBuffers, DRT_DATA[] pDataBuffers, in DRT_DATA pRemoteCredentials, in DRT_DATA pKeyIdentifier, in DRT_DATA pSignature) =>
			pSignature.cb == 0 ? HRESULT.S_OK : HRESULT.DRT_E_INVALID_MESSAGE;
	};
}