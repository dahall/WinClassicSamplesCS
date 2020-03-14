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
		[STAThread]
		static void Main()
		{
			// Get handle to the default provider.
			if (!CryptAcquireContext(out var hProv, default, default, CryptProviderType.PROV_RSA_FULL, 0))
			{
				Console.Write("Error {0:X} during CryptAcquireContext!\n", GetLastError());
				return;
			}
			using (hProv)
			{
				// Set the CRYPT_FIRST flag the first time through the loop.
				uint dwFlags = 1; // CRYPT_FIRST

				// Set size of data expected
				var dwDataLen = (uint)Marshal.SizeOf(typeof(PROV_ENUMALGS));

				// Enumerate the supported algorithms.
				while (true)
				{
					// Retrieve information about an algorithm.
					try
					{
						var EnumAlgs = CryptGetProvParam<PROV_ENUMALGS_EX>(hProv, ProvParam.PP_ENUMALGS_EX, dwFlags);

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
							EnumAlgs.szName.ToString(), pszAlgType, EnumAlgs.dwDefaultLen.ToString(), EnumAlgs.aiAlgid);

						// Clear the flags for the remaining interations of the loop.
						dwFlags = 0;
					}
					catch (Win32Exception ex) when (ex.NativeErrorCode == Win32Error.ERROR_NO_MORE_ITEMS)
					{
						// This indicates the end of the list.
						break;
					}
				}
			}
		}
	}
}
