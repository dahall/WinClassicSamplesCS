using Vanara.PInvoke;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NCrypt;

class Program
{
	const uint DERIVED_KEY_LEN = 60;

	static readonly byte[] Secret = [
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a,
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a,
	];

	//
	// Algorithm name array
	//

	static readonly string[] KdfAlgorithmNameArray = [
		StandardAlgorithmId.BCRYPT_SP800108_CTR_HMAC_ALGORITHM,
		StandardAlgorithmId.BCRYPT_SP80056A_CONCAT_ALGORITHM,
		StandardAlgorithmId.BCRYPT_PBKDF2_ALGORITHM,
		StandardAlgorithmId.BCRYPT_CAPI_KDF_ALGORITHM,
	];

	//
	// Sample Parameters for SP800-108 KDF
	// 

	static readonly byte[] Label = [ 0x41,0x4C,0x49,0x43,0x45,0x31,0x32,0x33,0x00 ];

	const string Context = "Context";

	static readonly NCryptBuffer[] SP800108ParamBuffer = [
		new(KeyDerivationBufferType.KDF_LABEL, Label),
		new(KeyDerivationBufferType.KDF_CONTEXT, Context),
		new(KeyDerivationBufferType.KDF_HASH_ALGORITHM, StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM),
	];

	//
	// Sample Parameters for SP800-56A KDF
	// 

	static readonly byte[] AlgorithmID = [ 0x12,0x34,0x56,0x78,0x9A,0xBC,0xDE,0xF0 ];

	static readonly byte[] PartyUInfo = [ 0x41,0x4C,0x49,0x43,0x45,0x31,0x32,0x33 ];

	static readonly byte[] PartyVInfo = [0x42, 0x4F, 0x42, 0x42, 0x59, 0x34, 0x35, 0x36];

	static readonly NCryptBuffer[] SP80056AParamBuffer = [
		new(KeyDerivationBufferType.KDF_ALGORITHMID, AlgorithmID),
		new(KeyDerivationBufferType.KDF_PARTYUINFO, PartyUInfo),
		new(KeyDerivationBufferType.KDF_PARTYVINFO, PartyVInfo),
		new(KeyDerivationBufferType.KDF_HASH_ALGORITHM, StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM),
	];

	//
	// Sample Parameters for PBKDF2
	// 

	static readonly byte[] Salt = [0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88];

	static readonly ulong IterationCount = 12000;

	static readonly NCryptBuffer[] PBKDF2ParamBuffer = [
		new(KeyDerivationBufferType.KDF_SALT, Salt),
		new(KeyDerivationBufferType.KDF_ITERATION_COUNT, BitConverter.GetBytes(IterationCount)),
		new(KeyDerivationBufferType.KDF_HASH_ALGORITHM, StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM),
	];

	//
	// Sample Parameters for CAPI_KDF
	// 

	static readonly NCryptBuffer[] CAPIParamBuffer = [new(KeyDerivationBufferType.KDF_HASH_ALGORITHM, StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM)];

	static readonly NCryptBufferDesc[] ParamList = [
		new(SP800108ParamBuffer),
		new(SP80056AParamBuffer),
		new(PBKDF2ParamBuffer),
		new(CAPIParamBuffer),
	];

	//-----------------------------------------------------------------------------
	//
	// wmain
	//
	//-----------------------------------------------------------------------------
	internal static int Main()
	{
		NTStatus Status = NTStatus.STATUS_SUCCESS;

		for (uint i = 0; i < KdfAlgorithmNameArray.Length; i++)
		{
			Status = PeformKeyDerivation(i);

			if (Status.Failed)
			{
				ReportError(Status);
				goto exit;
			}

		} // for loop

exit:

		return (int)Status;
	}

	//----------------------------------------------------------------------------
	//
	// PerformKeyDerivation
	//
	//----------------------------------------------------------------------------
	private static NTStatus PeformKeyDerivation(uint ArrayIndex)
	{
		NTStatus Status = BCryptOpenAlgorithmProvider(out var KdfAlgHandle, // Alg Handle
			KdfAlgorithmNameArray[ArrayIndex], // Cryptographic Algorithm name (null terminated unicode string)
			default, // Provider name; if null, the default provider is loaded
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = BCryptGenerateSymmetricKey(KdfAlgHandle, // Algorithm Handle 
			out var SecretKeyHandle, // A pointer to a key handle
			IntPtr.Zero, // Buffer that recieves the key object;default implies memory is allocated and freed by the function
			0, // Size of the buffer in bytes
			Secret, // Buffer that contains the key material
			(uint)Secret.Length, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Derive the key
		//

		uint DerivedKeyLength = DERIVED_KEY_LEN;

		SafeHeapBlock DerivedKey = new(DerivedKeyLength);
		if (DerivedKey.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		// Generic parameters: 
		// KDF_GENERIC_PARAMETER and KDF_HASH_ALGORITHM are the generic parameters that can be passed for the following KDF algorithms:
		// BCRYPT_SP800108_CTR_HMAC_ALGORITHM 
		// KDF_GENERIC_PARAMETER = KDF_LABEL||0x00||KDF_CONTEXT 
		// BCRYPT_SP80056A_CONCAT_ALGORITHM
		// KDF_GENERIC_PARAMETER = KDF_ALGORITHMID || KDF_PARTYUINFO || KDF_PARTYVINFO {|| KDF_SUPPPUBINFO } {|| KDF_SUPPPRIVINFO }
		// BCRYPT_PBKDF2_ALGORITHM
		// KDF_GENERIC_PARAMETER = KDF_SALT
		// BCRYPT_CAPI_KDF_ALGORITHM
		// KDF_GENERIC_PARAMETER = Not used
		//
		// Alternatively, KDF specific parameters can be passed.
		// For BCRYPT_SP800108_CTR_HMAC_ALGORITHM: 
		// KDF_HASH_ALGORITHM, KDF_LABEL and KDF_CONTEXT are required
		// For BCRYPT_SP80056A_CONCAT_ALGORITHM:
		// KDF_HASH_ALGORITHM, KDF_ALGORITHMID, KDF_PARTYUINFO, KDF_PARTYVINFO are required
		// KDF_SUPPPUBINFO, KDF_SUPPPRIVINFO are optional
		// For BCRYPT_PBKDF2_ALGORITHM
		// KDF_HASH_ALGORITHM is required
		// KDF_ITERATION_COUNT, KDF_SALT are optional
		// Iteration count, (if not specified) will default to 10,000
		// For BCRYPT_CAPI_KDF_ALGORITHM
		// KDF_HASH_ALGORITHM is required
		//

		// 
		// This sample uses KDF specific parameters defined in KeyDerivation.h
		//

		Status = BCryptKeyDerivation(SecretKeyHandle, // Handle to the password key
			ParamList[ArrayIndex], // Parameters to the KDF algorithm
			DerivedKey, // Address of the buffer which recieves the derived bytes
			DerivedKeyLength, // Size of the buffer in bytes
			out _, // Variable that recieves number of bytes copied to above buffer 
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// DerivedKeyLength bytes have been derived
		//

cleanup:

		return Status;
	}

	//----------------------------------------------------------------------------
	//
	// ReportError
	// Prints error information to the console
	//
	//----------------------------------------------------------------------------
	static void ReportError([In] NTStatus Status) => Console.Write($"Error:\n{Status}\n");
}