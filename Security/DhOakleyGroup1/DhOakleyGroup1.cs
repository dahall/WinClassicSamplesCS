using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.NCrypt;

class Program
{
	static readonly byte[] OakleyGroup1P = [
		0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xc9, 0x0f,
		0xda, 0xa2, 0x21, 0x68, 0xc2, 0x34, 0xc4, 0xc6, 0x62, 0x8b,
		0x80, 0xdc, 0x1c, 0xd1, 0x29, 0x02, 0x4e, 0x08, 0x8a, 0x67,
		0xcc, 0x74, 0x02, 0x0b, 0xbe, 0xa6, 0x3b, 0x13, 0x9b, 0x22,
		0x51, 0x4a, 0x08, 0x79, 0x8e, 0x34, 0x04, 0xdd, 0xef, 0x95,
		0x19, 0xb3, 0xcd, 0x3a, 0x43, 0x1b, 0x30, 0x2b, 0x0a, 0x6d,
		0xf2, 0x5f, 0x14, 0x37, 0x4f, 0xe1, 0x35, 0x6d, 0x6d, 0x51,
		0xc2, 0x45, 0xe4, 0x85, 0xb5, 0x76, 0x62, 0x5e, 0x7e, 0xc6,
		0xf4, 0x4c, 0x42, 0xe9, 0xa6, 0x3a, 0x36, 0x20, 0xff, 0xff,
		0xff, 0xff, 0xff, 0xff, 0xff, 0xff
	];

	static readonly byte[] OakleyGroup1G = [
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		0x00, 0x00, 0x00, 0x00, 0x00, 0x02
	];

	static readonly byte[] rgbrgbTlsSeed = [
		0x61, 0x62, 0x63, 0x64, 0x62, 0x63, 0x64, 0x65, 0x63, 0x64,
		0x65, 0x66, 0x64, 0x65, 0x66, 0x67, 0x65, 0x66, 0x67, 0x68,
		0x66, 0x67, 0x68, 0x69, 0x67, 0x68, 0x69, 0x6a, 0x68, 0x69,
		0x6a, 0x6b, 0x69, 0x6a, 0x6b, 0x6c, 0x6a, 0x6b, 0x6c, 0x6d,
		0x6b, 0x6c, 0x6d, 0x6e, 0x6c, 0x6d, 0x6e, 0x6f, 0x6d, 0x6e,
		0x66, 0x67, 0x68, 0x69, 0x67, 0x68, 0x69, 0x6a, 0x68, 0x69,
		0x6f, 0x70, 0x6e, 0x6f
	];

	const string Label = "MyTlsLabel";

	//-----------------------------------------------------------------------------
	//
	// wmain
	//
	//-----------------------------------------------------------------------------
	internal static int Main()
	{
		NTStatus Status;
		const uint KeyLength = 768;//bits

		//
		// Construct the DH parameter blob. this is the only supported
		// method for DH in CNG.
		//
		// Calculate size of param blob and allocate memory
		//
		// Set header properties on param blob
		//

		SafeHGlobalStruct<BCRYPT_DH_PARAMETER_HEADER> DhParamBlob = new(new() {
			cbKeyLength = KeyLength / 8,
			dwMagic = BCRYPT_DH_PARAMETERS_MAGIC
		}, 0);
		if (DhParamBlob.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Set prime
		//

		//
		// Set generator
		//

		DhParamBlob.Append(OakleyGroup1P);

		DhParamBlob.Append(OakleyGroup1G);

		DhParamBlob.InitializeSizeField(nameof(BCRYPT_DH_PARAMETER_HEADER.cbLength));

		//
		// Open alg provider handle
		//

		Status = BCryptOpenAlgorithmProvider(out var ExchAlgHandleA,
			StandardAlgorithmId.BCRYPT_DH_ALGORITHM,
			default,
			0);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptOpenAlgorithmProvider(out var ExchAlgHandleB,
			StandardAlgorithmId.BCRYPT_DH_ALGORITHM,
			default,
			0);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// A generates a private key
		// 

		Status = BCryptGenerateKeyPair(ExchAlgHandleA, // Algorithm handle
			out var PrivKeyHandleA, // Key handle - will be created
			KeyLength, // Length of the key - in bits
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptSetProperty(PrivKeyHandleA,
			BCrypt.PropertyName.BCRYPT_DH_PARAMETERS,
			DhParamBlob,
			DhParamBlob.Size,
			0);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptFinalizeKeyPair(PrivKeyHandleA, // Key handle
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// A exports DH public key
		// 

		Status = BCryptExportKey(PrivKeyHandleA, // Handle of the key to export
			default, // Handle of the key used to wrap the exported key
			BlobType.BCRYPT_DH_PUBLIC_BLOB, // Blob type (null terminated unicode string)
			IntPtr.Zero, // Buffer that recieves the key blob
			0, // Buffer length (in bytes)
			out var PubBlobLengthA, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		SafeHGlobalHandle PubBlobA = new(PubBlobLengthA);
		if (PubBlobA.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}


		Status = BCryptExportKey(PrivKeyHandleA, // Handle of the key to export
			default, // Handle of the key used to wrap the exported key
			BlobType.BCRYPT_DH_PUBLIC_BLOB, // Blob type (null terminated unicode string)
			PubBlobA, // Buffer that recieves the key blob
			PubBlobLengthA, // Buffer length (in bytes)
			out PubBlobLengthA, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// B generates a private key
		// 

		Status = BCryptGenerateKeyPair(ExchAlgHandleB, // Algorithm handle
			out var PrivKeyHandleB, // Key handle - will be created
			KeyLength, // Length of the key - in bits
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptSetProperty(PrivKeyHandleB,
			BCrypt.PropertyName.BCRYPT_DH_PARAMETERS,
			DhParamBlob,
			DhParamBlob.Size,
			0);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptFinalizeKeyPair(PrivKeyHandleB, // Key handle
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// B exports DH public key
		//

		Status = BCryptExportKey(PrivKeyHandleB, // Handle of the key to export
			default, // Handle of the key used to wrap the exported key
			BlobType.BCRYPT_DH_PUBLIC_BLOB, // Blob type (null terminated unicode string)
			IntPtr.Zero, // Buffer that recieves the key blob
			0, // Buffer length (in bytes)
			out var PubBlobLengthB, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		SafeHGlobalHandle PubBlobB = new(PubBlobLengthB);
		if (PubBlobB.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}


		Status = BCryptExportKey(PrivKeyHandleB, // Handle of the key to export
			default, // Handle of the key used to wrap the exported key
			BlobType.BCRYPT_DH_PUBLIC_BLOB, // Blob type (null terminated unicode string)
			PubBlobB, // Buffer that recieves the key blob
			PubBlobLengthB, // Buffer length (in bytes)
			out PubBlobLengthB, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// A imports B's public key
		//

		Status = BCryptImportKeyPair(ExchAlgHandleA, // Alg handle
			default, // Parameter not used
			BlobType.BCRYPT_DH_PUBLIC_BLOB, // Blob type (Null terminated unicode string)
			out var PubKeyHandleA, // Key handle that will be recieved
			PubBlobB, // Buffer than points to the key blob
			PubBlobLengthB, // Buffer length in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		ExchAlgHandleA.Dispose();

		//
		// Build KDF parameter list
		//

		//specify hash algorithm, SHA1 if null

		//specify secret to append and prepend
		NCryptBufferDesc ParameterList = new([
			new(KeyDerivationBufferType.KDF_TLS_PRF_SEED, rgbrgbTlsSeed),
			new(KeyDerivationBufferType.KDF_TLS_PRF_LABEL, Label),
		]);

		//
		// A generates the agreed secret
		//

		Status = BCryptSecretAgreement(PrivKeyHandleA, // Private key handle
			PubKeyHandleA, // Public key handle
			out var AgreedSecretHandleA, // Handle that represents the secret agreement value
			0);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptDeriveKey(AgreedSecretHandleA, // Secret agreement handle
			KDF.BCRYPT_KDF_TLS_PRF, // Key derivation function (null terminated unicode string)
			ParameterList, // KDF parameters
			IntPtr.Zero, // Buffer that recieves the derived key 
			0, // Length of the buffer
			out var AgreedSecretLengthA, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		SafeHGlobalHandle AgreedSecretA = new(AgreedSecretLengthA);
		if (AgreedSecretA.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptDeriveKey(AgreedSecretHandleA, // Secret agreement handle
			KDF.BCRYPT_KDF_TLS_PRF, // Key derivation function (null terminated unicode string)
			ParameterList, // KDF parameters
			AgreedSecretA, // Buffer that recieves the derived key 
			AgreedSecretLengthA, // Length of the buffer
			out _, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// B imports A's public key
		//

		Status = BCryptImportKeyPair(ExchAlgHandleB, // Alg handle
			default, // Parameter not used
			BlobType.BCRYPT_DH_PUBLIC_BLOB, // Blob type (Null terminated unicode string)
			out var PubKeyHandleB, // Key handle that will be recieved
			PubBlobA, // Buffer than points to the key blob
			PubBlobLengthA, // Buffer length in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		ExchAlgHandleB.Dispose();

		//
		// B generates the agreed secret
		//

		Status = BCryptSecretAgreement(PrivKeyHandleB, // Private key handle
			PubKeyHandleB, // Public key handle
			out var AgreedSecretHandleB, // Handle that represents the secret agreement value
			0);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptDeriveKey(AgreedSecretHandleB, // Secret agreement handle
			KDF.BCRYPT_KDF_TLS_PRF, // Key derivation function (null terminated unicode string)
			ParameterList, // KDF parameters
			IntPtr.Zero, // Buffer that recieves the derived key 
			0, // Length of the buffer
			out var AgreedSecretLengthB, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		SafeHGlobalHandle AgreedSecretB = new(AgreedSecretLengthB);
		if (AgreedSecretB.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptDeriveKey(AgreedSecretHandleB, // Secret agreement handle 
			KDF.BCRYPT_KDF_TLS_PRF, // Key derivation function (null terminated unicode string)
			ParameterList, // KDF parameters
			AgreedSecretB, // Buffer that recieves the derived key 
			AgreedSecretLengthB, // Length of the buffer
			out _, // Number of bytes copied to the buffer
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// At this point the AgreedSecretA should be the same as AgreedSecretB.
		// In a real scenario, the agreed secrets on both sides will probably 
		// be input to a BCryptGenerateSymmetricKey function. 
		// Optional : Compare them
		//

		if (!AgreedSecretA.Equals(AgreedSecretB))
		{
			Status = NTStatus.STATUS_UNSUCCESSFUL;
			ReportError(Status);
			goto cleanup;

		}

		Status = NTStatus.STATUS_SUCCESS;

		Console.Write("Success!\n");

cleanup:

		return (int)Status;
	}

	//----------------------------------------------------------------------------
	//
	// ReportError
	// Prints error information to the console
	//
	//----------------------------------------------------------------------------
	static void ReportError([In] NTStatus Status) => Console.Write($"Error:\n{Status}\n");
}