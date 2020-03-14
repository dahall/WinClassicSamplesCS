using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.BCrypt;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NCrypt;

namespace enumalgs
{
	static class EnumAlgs
	{
		static void Main()
		{
			SafeHCRYPTPROV hProv;
			PROV_ENUMALGS EnumAlgs = default;
			Win32Error dwError = Win32Error.ERROR_SUCCESS;

			// Get handle to the default provider.
			if (!CryptAcquireContext(out hProv, default, default, CryptProviderType.PROV_RSA_FULL, 0))
			{
				Console.Write("Error {0:X} during CryptAcquireContext!\n", GetLastError());
				return;
			}

			// Set the CRYPT_FIRST flag the first time through the loop.
			uint dwFlags = 1; // CRYPT_FIRST

			// Set size of data expected
			var dwDataLen = (uint)Marshal.SizeOf(typeof(PROV_ENUMALGS));

			// Enumerate the supported algorithms.
			using (var pEnumAlgs = new PinnedObject(EnumAlgs))
			while (dwError.Succeeded)
			{
				// Retrieve information about an algorithm.
				if (!CryptGetProvParam(hProv, ProvParam.PP_ENUMALGS, pEnumAlgs, ref dwDataLen, dwFlags))
				{
					dwError = GetLastError();
					if (dwError == Win32Error.ERROR_NO_MORE_ITEMS)
					{
						// Exit the loop.
						break;
					}

					Console.Write("Error {0:X} reading algorithm!\n", dwError);
					goto Error;
				}

				// Determine algorithm type.
				var pszAlgType = (GET_ALG_CLASS(EnumAlgs.aiAlgid)) switch
				{
					ALG_CLASS.ALG_CLASS_DATA_ENCRYPT => "Encrypt",
					ALG_CLASS.ALG_CLASS_HASH => "Hash",
					ALG_CLASS.ALG_CLASS_KEY_EXCHANGE => "Exchange",
					ALG_CLASS.ALG_CLASS_SIGNATURE => "Signature",
					_ => "Unknown",
				};

				// Print information about algorithm.
				Console.Write("Name:{0,-19}  Type:{1,-9}  Bits:{2,-4}  Algid:{3}\n",
				   EnumAlgs.szName.ToString(), pszAlgType, EnumAlgs.dwBitLen.ToString(), EnumAlgs.aiAlgid);

				// Clear the flags for the remaining interations of the loop.
				dwFlags = 0;
			}

			Error:

			// Release CSP handle (if open)
			hProv?.Dispose();
		}
	}
}
