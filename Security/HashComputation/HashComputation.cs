using Vanara.PInvoke;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.Kernel32;

class Program
{
	static readonly byte[] Message = [
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
		0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
	];

	//-----------------------------------------------------------------------------
	//
	// wmain
	//
	//-----------------------------------------------------------------------------
	internal static int Main()
	{
		//
		// Open an algorithm handle
		// This sample passes BCRYPT_HASH_REUSABLE_FLAG with BCryptAlgorithmProvider(...) to load a provider which supports reusable hash
		//
		NTStatus Status = BCryptOpenAlgorithmProvider(out var AlgHandle, // Alg Handle pointer
			StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM, // Cryptographic Algorithm name (null terminated unicode string)
			default, // Provider name; if null, the default provider is loaded
			AlgProviderFlags.BCRYPT_HASH_REUSABLE_FLAG); // Flags; Loads a provider which supports reusable hash
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Obtain the length of the hash
		//
		uint HashLength;
		try
		{
			HashLength = BCryptGetProperty<uint>(AlgHandle, // Handle to a CNG object
				PropertyName.BCRYPT_HASH_LENGTH); // Property name (null terminated unicode string)
		}
		catch (Exception e)
		{
			ReportError(e.HResult);
			goto cleanup;
		}

		//
		// Allocate the hash buffer on the heap
		//
		SafeHeapBlock Hash = new(HashLength);
		if (Hash.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Create a hash handle
		//
		Status = BCryptCreateHash(AlgHandle, // Handle to an algorithm provider 
			out var HashHandle); // A pointer to a hash handle - can be a hash or hmac object
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Hash the message(s)
		// More than one message can be hashed by calling BCryptHashData 
		//
		Status = BCryptHashData(HashHandle, // Handle to the hash or MAC object
			Message, // A pointer to a buffer that contains the data to hash
			(uint)Message.Length, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Obtain the hash of the message(s) into the hash buffer
		//
		Status = BCryptFinishHash(HashHandle, // Handle to the hash or MAC object
			Hash, // A pointer to a buffer that receives the hash or MAC value
			HashLength, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = NTStatus.STATUS_SUCCESS;

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