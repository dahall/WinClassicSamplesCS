using System.IO;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Crypt32;

namespace encrypt
{
	class encrypt
	{
		const ALG_ID ENCRYPT_ALGORITHM = ALG_ID.CALG_RC4;
		const int ENCRYPT_BLOCK_SIZE = 1;
		const CryptGenKeyFlags KEYLENGTH = (CryptGenKeyFlags)0x00800000;


		static int Main(string[] args)
		{
			string szPassword = default;

			// Validate argument count.
			if (args.Length < 2 || args.Length > 3)
			{
				Console.Write("USAGE: encrypt <source file> <dest file> [ <password> ]\n");
				return 1;
			}

			// Parse arguments.
			var szSource = args[0];
			var szDestination = args[1];
			if (args.Length == 3)
			{
				szPassword = args[2];
			}

			try { CAPIEncryptFile(szSource, szDestination, szPassword); }
			catch
			{
				GC.Collect();
				Console.Write("Error encrypting file!\n");
				return 1;
			}

			return 0;
		}


		/*****************************************************************************/
		static void CAPIEncryptFile(string szSource, string szDestination, string szPassword)
		{
			SafeHCRYPTKEY hKey = default;

			SafeHGlobalHandle pbKeyBlob = default;
			uint dwKeyBlobLen = 0;

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
					// Encrypt the file with a random session key.

					// Create a random session key.
					if (!CryptGenKey(hProv, ENCRYPT_ALGORITHM, KEYLENGTH | CryptGenKeyFlags.CRYPT_EXPORTABLE, out hKey))
						Win32Error.ThrowLastError();

					// Get handle to key exchange public key.
					if (!CryptGetUserKey(hProv, CertKeySpec.AT_KEYEXCHANGE, out var hXchgKey))
						Win32Error.ThrowLastError();

					using (hXchgKey)
					{
						// Determine size of the key blob and allocate memory.
						if (!CryptExportKey(hKey, hXchgKey, Crypt32.BlobType.SIMPLEBLOB, 0, null, ref dwKeyBlobLen))
							Win32Error.ThrowLastError();
						if ((pbKeyBlob = Marshal.AllocHGlobal((int)dwKeyBlobLen)) == default)
							Win32Error.ThrowLastError();

						// Export session key into a simple key blob.
						if (!CryptExportKey(hKey, hXchgKey, Crypt32.BlobType.SIMPLEBLOB, 0, pbKeyBlob, ref dwKeyBlobLen))
							Win32Error.ThrowLastError();
					}

					// Write size of key blob to destination file.
					var pdwKeyBlobLen = BitConverter.GetBytes(dwKeyBlobLen);
					hDestination.Write(pdwKeyBlobLen, 0, pdwKeyBlobLen.Length);

					// Write key blob to destination file.
					var pBlob = pbKeyBlob.ToArray<byte>((int)dwKeyBlobLen);
					hDestination.Write(pBlob, 0, pBlob.Length);
				}
				else
				{
					// Encrypt the file with a session key derived from a password.

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
				int dwCount;
				do
				{
					// Read up to 'dwBlockLen' bytes from source file.
					dwCount = hSource.Read(pbBuffer, 0, dwBlockLen);

					// Encrypt data
					if (!CryptEncrypt(hKey, default, dwCount == 0, 0, pbBuffer, ref dwCount, dwBufferLen))
						Win32Error.ThrowLastError();

					// Write data to destination file.
					hDestination.Write(pbBuffer, 0, dwCount);
				} while (dwCount != 0);
			}

			Console.Write("OK\n");
		}
	}
}
