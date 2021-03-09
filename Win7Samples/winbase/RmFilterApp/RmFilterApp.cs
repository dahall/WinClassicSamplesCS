using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.RstrtMgr;

namespace RmFilterApp
{
	internal class Program
	{
		private static void Main()
		{
			// CCH_RM_SESSION_KEY: Character count of the Text-Encoded session key,defined in RestartManager.h
			var sessKey = new StringBuilder(CCH_RM_SESSION_KEY + 1);

			// NOTE:We register two calc executable files. The second one is for the redirection of 32 bit calc on 64 bit machines. Even if
			// you are using a 32 bit machine, you don't need to comment out the second line.
			string[] rgsFiles = { "C:\\Windows\\System32\\calc.exe", "C:\\Windows\\SysWow64\\calc.exe" };

			uint nRetry = 0;
			uint nAffectedApps = 0;
			RM_PROCESS_INFO[] rgAffectedApps = null;
			RM_REBOOT_REASON dwRebootReasons;

			// Start a Restart Manager Session
			var dwErrCode = RmStartSession(out var dwSessionHandle, 0, sessKey);
			if (Win32Error.ERROR_SUCCESS != dwErrCode)
			{
				goto RM_CLEANUP;
			}

			//
			// Register items with Restart Manager
			//
			// NOTE: we only register two calc executable files
			//in this sample. You can register files, processes
			// (in the form of process ID), and services (in the
			// form of service short names) with Restart Manager.
			//
			dwErrCode = RmRegisterResources(dwSessionHandle, (uint)rgsFiles.Length, rgsFiles);

			if (Win32Error.ERROR_SUCCESS != dwErrCode)
			{
				goto RM_CLEANUP;
			}

			// Obtain the list of affected applications/services.
			//
			// NOTE: Restart Manager returns the results into the buffer allocated by the caller. The first call to RmGetList() will return
			// the size of the buffer (i.e. nProcInfoNeeded) the caller needs to allocate. The caller then needs to allocate the buffer
			// (i.e. rgAffectedApps) and make another RmGetList() call to ask Restart Manager to write the results into the buffer. However,
			// since Restart Manager refreshes the list every time RmGetList()is called, it is possible that the size returned by the first
			// RmGetList()call is not sufficient to hold the results discovered by the second RmGetList() call. Therefore, it is recommended
			// that the caller follows the following practice to handle this race condition:
			//
			// Use a loop to call RmGetList() in case the buffer allocated according to the size returned in previous call is not enough.
			//
			// In this example, we use a do-while loop trying to make 3 RmGetList() calls (including the first attempt to get buffer size)
			// and if we still cannot succeed, we give up.
			do
			{
				dwErrCode = RmGetList(dwSessionHandle, out var nProcInfoNeeded, ref nAffectedApps, rgAffectedApps, out dwRebootReasons);
				if (Win32Error.ERROR_SUCCESS == dwErrCode)
				{
					// RmGetList() succeeded
					break;
				}

				if (Win32Error.ERROR_MORE_DATA != dwErrCode)
				{
					// RmGetList() failed, with errors other than ERROR_MORE_DATA
					goto RM_CLEANUP;
				}

				// RmGetList() is asking for more data
				nAffectedApps = nProcInfoNeeded;
				rgAffectedApps = new RM_PROCESS_INFO[nAffectedApps];
			} while ((Win32Error.ERROR_MORE_DATA == dwErrCode) && (nRetry++ < 3));

			if (Win32Error.ERROR_SUCCESS != dwErrCode)
			{
				goto RM_CLEANUP;
			}

			if (RM_REBOOT_REASON.RmRebootReasonNone != dwRebootReasons)
			{
				// Restart Manager cannot mitigate a reboot. We goes to the clean up. The caller may want to add additional code to handle
				// this scenario.
				goto RM_CLEANUP;
			}

			// Now rgAffectedApps contains the affected applications and services. The number of applications and services returned is
			// nAffectedApps. The result of RmGetList can be interpreted by the user to determine subsequent action (e.g. ask user's
			// permission to shutdown).
			//
			// CALLER CODE GOES HERE...

			// Shut down all running instances of affected applications and services.
			dwErrCode = RmShutdown(dwSessionHandle);
			if (Win32Error.ERROR_SUCCESS != dwErrCode)
			{
				goto RM_CLEANUP;
			}

			// An installer can now replace or update the calc executable file.
			//
			// CALLER CODE GOES HERE...

			// Restart applications and services, after the files have been replaced or updated.
			dwErrCode = RmRestart(dwSessionHandle);
			if (Win32Error.ERROR_SUCCESS != dwErrCode)
			{
				goto RM_CLEANUP;
			}

			RM_CLEANUP:

			if (0xFFFFFFFF != dwSessionHandle)
			{
				// Clean up the Restart Manager session.
				RmEndSession(dwSessionHandle);
			}
		}
	}
}