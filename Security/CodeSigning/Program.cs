using System;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WinTrust;

namespace CodeSigning;

internal class Program
{
	private static readonly SafeMemoryPool<CoTaskMemoryMethods> mem = new();

	public static int Main(string[] args)
	{
		uint ArgStart = 0;
		var UseStrongSigPolicy = false;
		SafeHFILE FileHandle = null;
		HRESULT Error;
		if (args.Length is <2 or >3)
		{
			PrintUsage();
			Error = (HRESULT)(Win32Error)Win32Error.ERROR_INVALID_PARAMETER;
			goto Cleanup;
		}

		if (args[ArgStart] == "-p")
		{
			UseStrongSigPolicy = true;
			ArgStart++;
		}

		if (ArgStart + 1 >= args.Length)
		{
			PrintUsage();
			Error = (HRESULT)(Win32Error)Win32Error.ERROR_INVALID_PARAMETER;
			goto Cleanup;
		}

		if (args[ArgStart].Length != 2 || string.Compare(args[ArgStart], "-c") != 0 && string.Compare(args[ArgStart], "-e") != 0)
		{
			PrintUsage();
			Error = (HRESULT)(Win32Error)Win32Error.ERROR_INVALID_PARAMETER;
			goto Cleanup;
		}

		FileHandle = CreateFile(args[ArgStart+1], FileAccess.GENERIC_READ, System.IO.FileShare.Read, default, System.IO.FileMode.Open, 0);
		if (FileHandle.IsInvalid)
		{
			PrintError(Error = Win32Error.GetLastError().ToHRESULT());
			goto Cleanup;
		}

		if (string.Compare(args[ArgStart], "-c") == 0)
		{
			Error = VerifyCatalogSignature(FileHandle, UseStrongSigPolicy).ToHRESULT();
		}
		else if (string.Compare(args[ArgStart], "-e") == 0)
		{
			Error = VerifyEmbeddedSignatures(args[ArgStart+1], FileHandle, UseStrongSigPolicy);
		}
		else
		{
			PrintUsage();
			Error = Win32Error.ERROR_INVALID_PARAMETER;
		}

Cleanup:
		FileHandle?.Dispose();

		return (int)Error;
	}

	//----------------------------------------------------------------------------
	//
	// PrintError
	// Prints error information to the console
	//
	//----------------------------------------------------------------------------
	private static void PrintError([In] HRESULT Status) => Console.Write("Error: 0x{0:X8} ({1})\n", (int)Status, Status);

	private static void PrintUsage()
	{
		Console.Write("{0} [-p] <-c | -e> file\n", System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location));
		Console.Write("Flags:\n");
		Console.Write(" -p: Use signature policy of the current os (szOID_CERT_STRONG_SIGN_OS_CURRENT)\n");
		Console.Write(" -c: Search for the file in system catalogs\n");
		Console.Write(" -e: Verify embedded file signature\n");
	}

	//----------------------------------------------------------------------------
	//
	// VerifyCatalogSignature
	// Looks up a file by hash in the system catalogs.
	//
	//----------------------------------------------------------------------------
	private static Win32Error VerifyCatalogSignature(HFILE FileHandle, bool UseStrongSigPolicy)
	{
		Win32Error Error = Win32Error.ERROR_SUCCESS;
		var Found = false;
		SafeCoTaskMemHandle HashData = null;

		SafeHCATADMIN CatAdminHandle;
		SafeHCATINFO CatInfoHandle = null;
		if (UseStrongSigPolicy)
		{
			CERT_STRONG_SIGN_PARA SigningPolicy = new()
			{
				cbSize = (uint)Marshal.SizeOf(typeof(CERT_STRONG_SIGN_PARA)),
				dwInfoChoice = CERT_INFO_CHOICE.CERT_STRONG_SIGN_OID_INFO_CHOICE,
				pszOID = mem.Add(SignOID.szOID_CERT_STRONG_SIGN_OS_CURRENT, CharSet.Ansi)
			};
			if (!CryptCATAdminAcquireContext2(out CatAdminHandle, default, BCrypt.StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM, SigningPolicy))
			{
				Error = GetLastError();
				goto Cleanup;
			}
		}
		else
		{
			if (!CryptCATAdminAcquireContext2(out CatAdminHandle, default, BCrypt.StandardAlgorithmId.BCRYPT_SHA256_ALGORITHM))
			{
				Error = GetLastError();
				goto Cleanup;
			}
		}

		// Get size of hash to be used
		uint HashLength = 0;
		if (!CryptCATAdminCalcHashFromFileHandle2(CatAdminHandle, FileHandle, ref HashLength))
		{
			Error = Win32Error.GetLastError();
			goto Cleanup;
		}

		// Generate hash for a give file
		HashData = new(HashLength);
		if (!CryptCATAdminCalcHashFromFileHandle2(CatAdminHandle, FileHandle, ref HashLength, HashData))
		{
			Error = GetLastError();
			goto Cleanup;
		}

		// Find the first catalog containing this hash
		CatInfoHandle = CryptCATAdminEnumCatalogFromHash(CatAdminHandle, HashData, HashLength);

		while (!CatInfoHandle.IsNull)
		{
			CATALOG_INFO catalogInfo = new() { cbStruct = (uint)Marshal.SizeOf(typeof(CATALOG_INFO)) };
			Found = true;

			if (!CryptCATCatalogInfoFromContext(CatInfoHandle, ref catalogInfo))
			{
				Error = GetLastError();
				break;
			}

			Console.Write("Hash was found in catalog {0}\n\n", catalogInfo.wszCatalogFile);

			// Look for the next catalog containing the file's hash
			CatInfoHandle = CryptCATAdminEnumCatalogFromHash(CatAdminHandle, HashData, HashLength, 0, CatInfoHandle);
		}

		if (Found != true)
		{
			Console.Write("Hash was not found in any catalogs.\n");
		}

Cleanup:
		CatInfoHandle?.Dispose();
		HashData?.Dispose();

		return Error;
	}

	//----------------------------------------------------------------------------
	//
	// VerifyEmbeddedSignatures
	// Verifies all embedded signatures of a file
	//
	//----------------------------------------------------------------------------
	private static HRESULT VerifyEmbeddedSignatures(string FileName, HFILE FileHandle, bool UseStrongSigPolicy)
	{
		Guid GenericActionId = WINTRUST_ACTION_GENERIC_VERIFY_V2;

		// Setup data structures for calling WinVerifyTrustEx
		WINTRUST_FILE_INFO FileInfo = new()
		{
			cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)),
			hFile = FileHandle,
			pcwszFilePath = mem.Add(FileName, CharSet.Unicode)
		};

		// First verify the primary signature (index 0) to determine how many secondary signatures are present. We use WSS_VERIFY_SPECIFIC
		// and dwIndex to do this, also setting WSS_GET_SECONDARY_SIG_COUNT to have the number of secondary signatures returned.
		WINTRUST_SIGNATURE_SETTINGS SignatureSettings = new()
		{
			cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_SIGNATURE_SETTINGS)),
			dwFlags = WSS.WSS_GET_SECONDARY_SIG_COUNT | WSS.WSS_VERIFY_SPECIFIC,
			dwIndex = 0
		};

		if (UseStrongSigPolicy != false)
		{
			CERT_STRONG_SIGN_PARA StrongSigPolicy = new()
			{
				cbSize = (uint)Marshal.SizeOf(typeof(CERT_STRONG_SIGN_PARA)),
				dwInfoChoice = CERT_INFO_CHOICE.CERT_STRONG_SIGN_OID_INFO_CHOICE,
				pszOID = mem.Add(SignOID.szOID_CERT_STRONG_SIGN_OS_CURRENT, CharSet.Ansi)
			};
			SignatureSettings.pCryptoPolicy = mem.Add(StrongSigPolicy);
		}

		WINTRUST_DATA WintrustData = new()
		{
			dwStateAction = WTD_STATEACTION.WTD_STATEACTION_VERIFY,
			dwUIChoice = WTD_UI.WTD_UI_NONE,
			fdwRevocationChecks = WTD_REVOKE.WTD_REVOKE_NONE,
			pFile = FileInfo,
			pSignatureSettings = SignatureSettings
		};

		Console.Write("Verifying primary signature... ");
		HRESULT Error = WinVerifyTrustEx(default, GenericActionId, WintrustData);
		var WintrustCalled = true;
		if (Error.Failed)
		{
			PrintError(Error);
			goto Cleanup;
		}

		Console.Write("Success!\n");

		Console.Write("Found {0} secondary signatures\n", WintrustData.pSignatureSettings?.cSecondarySigs ?? 0);

		// Now attempt to verify all secondary signatures that were found
		for (uint x = 1; x <= WintrustData.pSignatureSettings?.cSecondarySigs; x++)
		{
			Console.Write("Verify secondary signature at index {0}... ", x);

			// Need to clear the previous state data from the last call to WinVerifyTrustEx
			WintrustData.dwStateAction = WTD_STATEACTION.WTD_STATEACTION_CLOSE;
			Error = WinVerifyTrustEx(default, GenericActionId, WintrustData);
			if (Error.Failed)
			{
				//No need to call WinVerifyTrustEx again
				WintrustCalled = false;
				PrintError(Error);
				goto Cleanup;
			}

			WintrustData.hWVTStateData = default;

			// Caller must reset dwStateAction as it may have been changed during the last call
			WintrustData.dwStateAction = WTD_STATEACTION.WTD_STATEACTION_VERIFY;
			SignatureSettings.dwIndex = x;
			WintrustData.pSignatureSettings = SignatureSettings;
			Error = WinVerifyTrustEx(default, GenericActionId, WintrustData);
			if (Error.Failed)
			{
				PrintError(Error);
				goto Cleanup;
			}

			Console.Write("Success!\n");
		}

Cleanup:

		// Caller must call WinVerifyTrustEx with WTD_STATEACTION_CLOSE to free memory allocate by WinVerifyTrustEx
		if (WintrustCalled)
		{
			WintrustData.dwStateAction = WTD_STATEACTION.WTD_STATEACTION_CLOSE;
			WinVerifyTrustEx(default, GenericActionId, WintrustData);
		}

		return Error;
	}
}