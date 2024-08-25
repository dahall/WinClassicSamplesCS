using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NCrypt;

class Program
{
	//
	// Default Protection Descriptor String (Sample)
	//
	const string ProtectionDescriptorString = "LOCAL=logon";

	//
	// Some sample data to protect
	//
	const string SecretString = "Some message to protect";

	//-----------------------------------------------------------------------------
	//
	// wmain
	//
	//-----------------------------------------------------------------------------
	internal static int Main(string[] args)
	{
		//
		// Initialize secret to protect
		//

		SafeLPWSTR Secret = new(SecretString);

		//
		// Protect Secret
		//

		var Status = ProtectSecret(ProtectionDescriptorString, Secret, out var ProtectedSecret);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		if (ProtectedSecret.IsInvalid)
		{
			Status = HRESULT.NTE_INTERNAL_ERROR;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Open encrypted data to get descriptor information
		//

		Status = GetProtectionInfo(ProtectedSecret);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}


		//
		// Unprotect Secret
		//

		Status = UnprotectSecret(ProtectedSecret, out var UnprotectedSecret);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		if (UnprotectedSecret.IsInvalid)
		{
			Status = HRESULT.NTE_INTERNAL_ERROR;
			ReportError(Status);
			goto cleanup;
		}

		//
		// Optional : Check if the original message and the message obtained after decrypt are the same 
		//

		if (!UnprotectedSecret.Equals(Secret))
		{
			Status = HRESULT.NTE_FAIL;
			ReportError(Status);
			goto cleanup;
		}

		Status = HRESULT.S_OK;

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
	static void ReportError([In] HRESULT Status) => Console.Write($"Error:\n{Status}\n");

	//-----------------------------------------------------------------------------
	//
	// Protect Secret
	//
	//-----------------------------------------------------------------------------
	static HRESULT ProtectSecret(string ProtectionDescString, [In] SafeAllocatedMemoryHandle PlainText, out SafeLocalHandle ProtectedDataPointer)
	{
		ProtectedDataPointer = SafeLocalHandle.Null;

		//
		// Create Protection Descriptor Handle from the supplied 
		// protection string
		//

		var Status = NCryptCreateProtectionDescriptor(ProtectionDescString, 0, out var DescriptorHandle);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		//
		// Protect using the Protection Descriptor Handle
		//

		Status = NCryptProtectSecret(DescriptorHandle, 0, PlainText, PlainText.Size,
			default, // Use default allocations by LocalAlloc/LocalFree
			default, // Use default parent windows handle. 
			out var ProtectedData, // out LocalFree
			out var ProtectedDataLength);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		ProtectedDataPointer = new SafeLocalHandle(ProtectedData, ProtectedDataLength, true);

		Status = Win32Error.ERROR_SUCCESS;
cleanup:

		return Status;

	}

	//-----------------------------------------------------------------------------
	//
	// Unprotect Secret
	//
	//-----------------------------------------------------------------------------
	static HRESULT UnprotectSecret([In] SafeLocalHandle ProtectedData, out SafeLocalHandle PlainTextPointer)
	{
		PlainTextPointer = SafeLocalHandle.Null;

		//
		// Unprotect the secret
		//

		var Status = NCryptUnprotectSecret(out _, // Optional
			0, // no flags
			ProtectedData,
			ProtectedData.Size,
			default, // Use default allocations by LocalAlloc/LocalFree
			default, // Use default parent windows handle. 
			out var PlainText, // out LocalFree
			out var PlainTextLength);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		PlainTextPointer = new SafeLocalHandle(PlainText, PlainTextLength, true);

		Status = Win32Error.ERROR_SUCCESS;

cleanup:

		return Status;
	}

	//-----------------------------------------------------------------------------
	//
	// Get protection information from the encrypted data
	//
	//-----------------------------------------------------------------------------
	static HRESULT GetProtectionInfo([In] SafeLocalHandle ProtectedData)
	{
		//
		// Open the encrypted message without actually decrypting it.
		// This call will only reconstruct the Protectuion Descriptor
		//
		var Status = NCryptUnprotectSecret(out var DescriptorHandle,
			UnprotectSecretFlags.NCRYPT_UNPROTECT_NO_DECRYPT,
			ProtectedData,
			ProtectedData.Size,
			default,
			default,
			out var Data,
			out var DataLength);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Status = NCryptGetProtectionDescriptorInfo(DescriptorHandle,
			default,
			ProtectionDescriptorInfoType.NCRYPT_PROTECTION_INFO_TYPE_DESCRIPTOR_STRING,
			out var DescriptorString);
		if (Status.Failed)
		{
			ReportError(Status);
			goto cleanup;
		}

		Console.Write("Descriptor String constructed from the blob:\r\n {0}\r\n", Marshal.PtrToStringUni(DescriptorString));

		Status = Win32Error.ERROR_SUCCESS;

cleanup:
		return Status;
	}
}