using System;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.CredUI;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.CryptUI;

namespace CertSelect
{
	static class CertSelect
	{
		/*****************************************************************************
		 ReportError
			Prints error information to the console
		*****************************************************************************/
		static void ReportError(string wszMessage, HRESULT dwErrCode)
		{
			if (!string.IsNullOrEmpty(wszMessage))
				Console.WriteLine(wszMessage);

			var ex = dwErrCode.GetException();
			Console.WriteLine($"Error: 0x{(uint)dwErrCode:X8} ({(uint)dwErrCode})" + ex is null ? "" : $" {ex.Message}");
		}

		static readonly SafeOID szOID_PKIX_KP_EMAIL_PROTECTION = new SafeOID("1.3.6.1.5.5.7.3.4");

		static unsafe int Main()
		{
			HRESULT hr = HRESULT.S_OK;

			IntPtr[] rgParaEKU = { szOID_PKIX_KP_EMAIL_PROTECTION };

			var EKUCriteria = new CERT_SELECT_CRITERIA
			{
				dwType = CertSelectBy.CERT_SELECT_BY_ENHKEY_USAGE,
				cPara = (uint)rgParaEKU.Length,
			};
			fixed (IntPtr* pArr = rgParaEKU)
				EKUCriteria.ppPara = (IntPtr)(void*)pArr;


			var bDigSig = (byte)CertKeyUsage.CERT_DIGITAL_SIGNATURE_KEY_USAGE; // in byte 0
			var pDigSig = &bDigSig;
			var extDigSig = new CERT_EXTENSION
			{
				Value = new CRYPTOAPI_BLOB { cbData = 1, pbData = (IntPtr)pDigSig }
			};

			using var pExtDigSig = SafeCoTaskMemHandle.CreateFromStructure(extDigSig);
			IntPtr[] rgParaKU = { pExtDigSig };

			var KUCriteria = new CERT_SELECT_CRITERIA
			{
				dwType = CertSelectBy.CERT_SELECT_BY_KEY_USAGE,
				cPara = (uint)rgParaKU.Length
			};
			fixed (IntPtr* pArr = rgParaKU)
				KUCriteria.ppPara = (IntPtr)(void*)pArr;

			CERT_SELECT_CRITERIA[] rgCriteriaFilter = { EKUCriteria, KUCriteria };

			using var hStore = CertOpenStore(CertStoreProvider.CERT_STORE_PROV_SYSTEM, 0, default,
				CertStoreFlags.CERT_SYSTEM_STORE_CURRENT_USER, "MY");

			if (hStore.IsInvalid)
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			Console.Write("Looking for certificates in MY store ...\n");

			if (!CertSelectCertificateChains(default, CertSelection.CERT_SELECT_TRUSTED_ROOT | CertSelection.CERT_SELECT_HAS_PRIVATE_KEY, default,
				(uint)rgCriteriaFilter.Length, rgCriteriaFilter, hStore, out var cSelection, out var prgpSelection))
			{
				hr = (HRESULT)Win32Error.GetLastError();
				goto CleanUp;
			}

			if (cSelection < 1)
			{
				Console.Write("No certificates found matching the selection criteria.\n");
				goto CleanUp;
			}

			Console.Write("{0} certificates found matching the selection criteria.\n", cSelection);

			//
			// show the selected cert in UI
			//

			var certInput = new CERT_SELECTUI_INPUT
			{
				prgpChain = prgpSelection,
				cChain = cSelection
			};

			hr = CertSelectionGetSerializedBlob(certInput, out var pBuffer, out var ulSize);
			if (HRESULT.S_OK != hr)
				goto CleanUp;
			using (var spBuffer = new SafeLocalHandle(pBuffer, ulSize))
			{
				var CredUiInfo = new CREDUI_INFO
				{
					cbSize = Marshal.SizeOf(typeof(CREDUI_INFO)),
					pszCaptionText = "Select your credentials",
					pszMessageText = "Please select a certificate"
				};
				var ulAuthPackage = unchecked((uint)(-509));
				var bSave = false;
				var dwError = CredUIPromptForWindowsCredentials(CredUiInfo, 0, ref ulAuthPackage, spBuffer, ulSize,
					out var pbAuthBuffer, out var cbAuthBuffer, ref bSave, WindowsCredentialsDialogOptions.CREDUIWIN_AUTHPACKAGE_ONLY);
				if (dwError.Failed)
				{
					hr = (HRESULT)dwError;
				}
				else
				{
					//get the selected cert context 
					if (!CertAddSerializedElementToStore(default, pbAuthBuffer, cbAuthBuffer, CertStoreAdd.CERT_STORE_ADD_ALWAYS, 0,
						CertStoreContextFlags.CERT_STORE_CERTIFICATE_CONTEXT_FLAG, out var dwContextType, out var pCertContext))
					{
						goto CleanUp;
					}

					using (var spCertContext = new SafePCCERT_CONTEXT(pCertContext))
					{
						//
						// pCertContext now is ready to use
						//
					}
				}
			}

			CleanUp:

			if (hr.Failed)
			{
				ReportError(default, hr);
			}

			return (int)hr;
		}
	}
}