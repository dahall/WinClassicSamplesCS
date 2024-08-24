using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NCrypt;

class Program
{
	static readonly byte[] PlainTextArray = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];
	static readonly byte[] Aes128Password = [(byte)'P', (byte)'A', (byte)'S', (byte)'S', (byte)'W', (byte)'O', (byte)'R', (byte)'D'];
	static readonly byte[] Salt = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F];

	static int Main()
	{
		//
		// Open an algorithm handle
		//

		NTStatus Status = BCryptOpenAlgorithmProvider(out var AesAlgHandle, // Alg Handle pointer
			StandardAlgorithmId.BCRYPT_AES_ALGORITHM, // Cryptographic Algorithm name (null terminated unicode string)
			default, // Provider name; if null, the default provider is loaded
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Derive the AES 128 key from the password 
		// Using PBKDF2
		//

		//
		// Open an algorithm handle
		//

		Status = BCryptOpenAlgorithmProvider(out var KdfAlgHandle, // Alg Handle pointer
			StandardAlgorithmId.BCRYPT_PBKDF2_ALGORITHM, // Cryptographic Algorithm name (null terminated unicode string)
			default, // Provider name; if null, the default provider is loaded
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Create a key handle to the password
		//

		Status = BCryptGenerateSymmetricKey(KdfAlgHandle, // Algorithm Handle 
			out var Aes128PasswordKeyHandle, // A pointer to a key handle
			IntPtr.Zero, // Buffer that recieves the key object;default implies memory is allocated and freed by the function
			0, // Size of the buffer in bytes
			Aes128Password, // Buffer that contains the key material
			(uint)Aes128Password.Length, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Convert bits to bytes
		//

		uint Aes128KeyLength = 128 / 8;

		//
		// Allocate Key buffer
		//
		SafeHeapBlock Aes128Key = new(Aes128KeyLength);
		if (Aes128Key.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Derive AES key from the password
		//
		const ulong IterationCount = 1024;
		NCryptBufferDesc PBKDF2Parameters = new()
		{
			cBuffers = 3,
			pBuffers = [
				new(KeyDerivationBufferType.KDF_HASH_ALGORITHM, StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM),
				new(KeyDerivationBufferType.KDF_SALT, Salt),
				new(KeyDerivationBufferType.KDF_ITERATION_COUNT, BitConverter.GetBytes(IterationCount))
			]
		};

		Status = BCryptKeyDerivation(Aes128PasswordKeyHandle, // Handle to the password key
			PBKDF2Parameters, // Parameters to the KDF algorithm
			Aes128Key, // Address of the buffer which recieves the derived bytes
			Aes128KeyLength, // Size of the buffer in bytes
			out _, // Variable that recieves number of bytes copied to above buffer 
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Obtain block size
		//

		uint BlockLength;
		try
		{
			BlockLength = BCryptGetProperty<uint>(AesAlgHandle, // Handle to a CNG object
				BCrypt.PropertyName.BCRYPT_BLOCK_LENGTH); // Property name (null terminated unicode string)
		}
		catch (Exception ex)
		{
			ReportError(ex.HResult);
			goto cleanup;
		}

		//
		// Allocate the InitVector on the heap
		//

		var InitVectorLength = BlockLength;
		SafeHeapBlock InitVector = new(InitVectorLength);
		if (InitVector.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Generate IV randomly
		//

		Status = BCryptGenRandom(default, // Alg Handle pointer; If default, the default provider is chosen
			InitVector, // Address of the buffer that recieves the random number(s)
			InitVectorLength, // Size of the buffer in bytes
			GenRandomFlags.BCRYPT_USE_SYSTEM_PREFERRED_RNG); // Flags 
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Encrypt plain text
		//

		Status = EncryptData(AesAlgHandle,
			Aes128Key,
			InitVector,
			ChainingMode.BCRYPT_CHAIN_MODE_CBC,
			PlainTextArray,
			out var CipherText);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// Decrypt Cipher text
		//

		Status = DecryptData(AesAlgHandle,
			Aes128Key,
			InitVector,
			ChainingMode.BCRYPT_CHAIN_MODE_CBC,
			CipherText,
			out var PlainText);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// Optional : Check if the original plaintext and the plaintext obtained after decrypt are the same 
		//

		if (!memcmp(PlainText.GetBytes(0, PlainTextArray.Length), PlainTextArray))
		{
			Status = NTStatus.STATUS_UNSUCCESSFUL;
			ReportError(Status);
			goto cleanup;
		}

		Status = NTStatus.STATUS_SUCCESS;
		Console.Write("Success : Plaintext has been encrypted, ciphertext has been decrypted with AES-128 bit key\n");

cleanup:

		return (int)Status;
	}

	static bool memcmp(byte[] a, byte[] b)
	{
		if (ReferenceEquals(a, b)) return true;
		if (a.Length != b.Length) return false;
		for (int i = 0; i < a.Length; i++)
			if (a[i] != b[i]) return false;
		return true;
	}

	//
	// Utilities and helper functions
	//

	//----------------------------------------------------------------------------
	//
	// ReportError
	// Prints error information to the console
	//
	//----------------------------------------------------------------------------
	static void ReportError(NTStatus dwErrCode) => Console.Write("Error: 0x{0:X} {1}\n", (int)dwErrCode, dwErrCode);

	//-----------------------------------------------------------------------------
	//
	// Encrypt Data
	//
	//-----------------------------------------------------------------------------
	static NTStatus EncryptData([In] BCRYPT_ALG_HANDLE AlgHandle, [In] SafeAllocatedMemoryHandle Key, [In] SafeAllocatedMemoryHandle InitVector, 
		[In] string ChainingMode, [In] byte[] PlainText, out SafeAllocatedMemoryHandle CipherTextPointer)
	{
		CipherTextPointer = SafeHeapBlock.Null;

		//
		// Generate an AES key from the key bytes
		//

		NTStatus Status = BCryptGenerateSymmetricKey(AlgHandle, // Algorithm provider handle
			out var KeyHandle, // A pointer to key handle
			default, // A pointer to the buffer that recieves the key object;default implies memory is allocated and freed by the function
			0, // Size of the buffer in bytes
			Key, // A pointer to a buffer that contains the key material
			Key.Size, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Set the chaining mode on the key handle
		//

		SafeLPWSTR pChainingMode = new(ChainingMode);
		Status = BCryptSetProperty((IntPtr)(BCRYPT_KEY_HANDLE)KeyHandle, // Handle to a CNG object 
			BCrypt.PropertyName.BCRYPT_CHAINING_MODE, // Property name(null terminated unicode string)
			(IntPtr)pChainingMode, // Address of the buffer that contains the new property value 
			(uint)pChainingMode.Size, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// Copy initialization vector into a temporary initialization vector buffer
		// Because after an encrypt/decrypt operation, the IV buffer is overwritten.
		//

		SafeHeapBlock TempInitVector = new(InitVector.GetBytes());
		if (TempInitVector.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Get CipherText's length
		// If the program can compute the length of cipher text(based on algorihtm and chaining mode info.), this call can be avoided.
		//

		Status = BCryptEncrypt(KeyHandle, // Handle to a key which is used to encrypt 
			PlainText, // Address of the buffer that contains the plaintext
			(uint)PlainText.Length, // Size of the buffer in bytes
			default, // A pointer to padding info, used with asymmetric and authenticated encryption; else set to default
			TempInitVector, // Address of the buffer that contains the IV. 
			TempInitVector.Size, // Size of the IV buffer in bytes
			IntPtr.Zero, // Address of the buffer the recieves the ciphertext
			0, // Size of the buffer in bytes
			out var CipherTextLength, // Variable that recieves number of bytes copied to ciphertext buffer 
			EncryptFlags.BCRYPT_BLOCK_PADDING); // Flags; Block padding allows to pad data to the next block size
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// Allocate Cipher Text on the heap
		//

		SafeHeapBlock CipherText = new(CipherTextLength);
		if (CipherText.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Peform encyption
		// For block length messages, block padding will add an extra block
		//

		Status = BCryptEncrypt(KeyHandle, // Handle to a key which is used to encrypt 
			PlainText, // Address of the buffer that contains the plaintext
			(uint)PlainText.Length, // Size of the buffer in bytes
			IntPtr.Zero, // A pointer to padding info, used with asymmetric and authenticated encryption; else set to default
			TempInitVector, // Address of the buffer that contains the IV. 
			TempInitVector.Size, // Size of the IV buffer in bytes
			CipherText, // Address of the buffer the recieves the ciphertext
			CipherText.Size, // Size of the buffer in bytes
			out var ResultLength, // Variable that recieves number of bytes copied to ciphertext buffer 
			EncryptFlags.BCRYPT_BLOCK_PADDING); // Flags; Block padding allows to pad data to the next block size
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		CipherText.Size = ResultLength;
		CipherTextPointer = CipherText;

cleanup:

		return Status;

	}

	//-----------------------------------------------------------------------------
	//
	// Decrypt Data
	//
	//-----------------------------------------------------------------------------
	static NTStatus DecryptData([In] BCRYPT_ALG_HANDLE AlgHandle, [In] SafeAllocatedMemoryHandle Key, [In] SafeAllocatedMemoryHandle InitVector,
		[In] string ChainingMode, [In] SafeAllocatedMemoryHandle CipherText, out SafeAllocatedMemoryHandle PlainTextPointer)
	{
		PlainTextPointer = SafeHeapBlock.Null;

		//
		// Generate an AES key from the key bytes
		//

		var Status = BCryptGenerateSymmetricKey(AlgHandle, // Algorithm provider handle
			out var KeyHandle, // A pointer to key handle
			default, // A pointer to the buffer that recieves the key object;default implies memory is allocated and freed by the function
			0, // Size of the buffer in bytes
			(IntPtr)Key, // A pointer to a buffer that contains the key material
			Key.Size, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Set the chaining mode on the key handle
		//

		SafeLPWSTR pChainingMode = new(ChainingMode);
		Status = BCryptSetProperty((IntPtr)(BCRYPT_KEY_HANDLE)KeyHandle, // Handle to a CNG object 
			BCrypt.PropertyName.BCRYPT_CHAINING_MODE, // Property name(null terminated unicode string)
			pChainingMode, // Address of the buffer that contains the new property value 
			pChainingMode.Size, // Size of the buffer in bytes
			0); // Flags
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// Copy initialization vector into a temporary initialization vector buffer
		// Because after an encrypt/decrypt operation, the IV buffer is overwritten.
		//


		SafeHeapBlock TempInitVector = new(InitVector.GetBytes());
		if (TempInitVector.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Get CipherText's length
		// If the program can compute the length of cipher text(based on algorihtm and chaining mode info.), this call can be avoided.
		//

		Status = BCryptDecrypt(KeyHandle, // Handle to a key which is used to encrypt 
			CipherText, // Address of the buffer that contains the ciphertext
			CipherText.Size, // Size of the buffer in bytes
			default, // A pointer to padding info, used with asymmetric and authenticated encryption; else set to default
			TempInitVector, // Address of the buffer that contains the IV. 
			TempInitVector.Size, // Size of the IV buffer in bytes
			IntPtr.Zero, // Address of the buffer the recieves the plaintext
			0, // Size of the buffer in bytes
			out var PlainTextLength, // Variable that recieves number of bytes copied to plaintext buffer 
			EncryptFlags.BCRYPT_BLOCK_PADDING); // Flags; Block padding allows to pad data to the next block size
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		SafeHeapBlock PlainText = new(PlainTextLength);
		if (PlainText.IsInvalid)
		{
			Status = NTStatus.STATUS_NO_MEMORY;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Decrypt CipherText
		//

		Status = BCryptDecrypt(KeyHandle, // Handle to a key which is used to encrypt 
			CipherText, // Address of the buffer that contains the ciphertext
			CipherText.Size, // Size of the buffer in bytes
			default, // A pointer to padding info, used with asymmetric and authenticated encryption; else set to default
			TempInitVector, // Address of the buffer that contains the IV. 
			TempInitVector.Size, // Size of the IV buffer in bytes
			PlainText, // Address of the buffer the recieves the plaintext
			PlainTextLength, // Size of the buffer in bytes
			out _, // Variable that recieves number of bytes copied to plaintext buffer 
			EncryptFlags.BCRYPT_BLOCK_PADDING); // Flags; Block padding allows to pad data to the next block size

		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		PlainTextPointer = PlainText;

cleanup:

		return Status;

	}
}