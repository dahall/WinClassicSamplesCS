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

namespace encrypt
{
	class decrypt
	{
		const ALG_ID ENCRYPT_ALGORITHM = ALG_ID.CALG_RC4;
		const int ENCRYPT_BLOCK_SIZE = 1;
		const CryptGenKeyFlags KEYLENGTH = (CryptGenKeyFlags)0x00800000;

		/*****************************************************************************/
		static int Main(string[] args)
		{
			// Validate argument count.
			if (args.Length < 2 || args.Length > 3)
			{
				Console.Write("USAGE: encrypt <source file> <dest file> [ <password> ]\n");
				return 1;
			}

			// Parse arguments.
			var szSource = args[0];
			var szDestination = args[1];
			var szPassword = args.Length == 3 ? args[2] : null;

			try { CAPIDecryptFile(szSource, szDestination, szPassword); }
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				GC.Collect();
				return 1;
			}

			return 0;
		}

		/*****************************************************************************/
		static void CAPIDecryptFile(string szSource, string szDestination, string szPassword)
		{
			SafeHCRYPTKEY hKey = default;
			int dwCount;

			// Open source file.
			// Open destination file.
			using (FileStream hSource = File.OpenRead(szSource), hDestination = File.Create(szDestination))
			{
				// Get handle to the CSP. In order to be used with different OSs 
				// with different default provides, the CSP is explicitly set. 
				// If the Microsoft Enhanced Provider is not installed, set parameter
				// three to MS_DEF_PROV 

				if (!CryptAcquireContext(out var hProv, default, "Microsoft Enhanced Cryptographic Provider v1.0", CryptProviderType.PROV_RSA_FULL, 0))
					Win32Error.ThrowLastError();

				if (szPassword == default)
				{
					// Decrypt the file with the saved session key.

					// Read key blob length from source file and allocate memory.
					var pdwKeyBlobLen = new byte[4];
					dwCount = hSource.Read(pdwKeyBlobLen, 0, pdwKeyBlobLen.Length);
					if (dwCount < 4)
						throw new InvalidOperationException();
					var dwKeyBlobLen = BitConverter.ToInt32(pdwKeyBlobLen);
					var pbKeyBlob = new byte[dwKeyBlobLen];

					// Read key blob from source file.
					dwCount = hSource.Read(pbKeyBlob, 0, dwKeyBlobLen);
					if (dwCount == 0)
					{
						Console.Write("Error reading file header!\n");
						throw new InvalidOperationException();
					}

					// Import key blob into CSP.
					if (!CryptImportKey(hProv, pbKeyBlob, dwKeyBlobLen, default, 0, out hKey))
						Win32Error.ThrowLastError();
				}
				else
				{
					// Decrypt the file with a session key derived from a password.

					// Create a hash object.
					if (!CryptCreateHash(hProv, ALG_ID.CALG_MD5, default, 0, out var hHash))
						Win32Error.ThrowLastError();
					using (hHash)
					{
						// Hash in the password data.
						using (var pszPassword = new SafeCoTaskMemString(szPassword))
							if (!CryptHashData(hHash, pszPassword, (uint)szPassword.Length, 0))
								Win32Error.ThrowLastError();

						// Derive a session key from the hash object.
						if (!CryptDeriveKey(hProv, ENCRYPT_ALGORITHM, hHash, KEYLENGTH, out hKey))
							Win32Error.ThrowLastError();
					}
				}

				// Determine number of bytes to encrypt at a time. This must be a multiple
				// of ENCRYPT_BLOCK_SIZE.
				var dwBlockLen = 1000 - 1000 % ENCRYPT_BLOCK_SIZE;

				// Determine the block size. If a block cipher is used this must have
				// room for an extra block.
				var dwBufferLen = dwBlockLen;

				// Allocate memory.
				var pbBuffer = new byte[dwBufferLen];

				// Encrypt source file and write to Source file.
				do
				{
					// Read up to 'dwBlockLen' bytes from source file.
					dwCount = hSource.Read(pbBuffer, 0, dwBlockLen);
					if (dwCount == 0) break;

					// Encrypt data
					if (!CryptDecrypt(hKey, default, dwCount == 0, 0, pbBuffer, ref dwCount))
						Win32Error.ThrowLastError();

					// Write data to destination file.
					hDestination.Write(pbBuffer, 0, dwCount);
				} while (dwCount != 0);
			}

			Console.Write("OK\n");
		}
	}
}
