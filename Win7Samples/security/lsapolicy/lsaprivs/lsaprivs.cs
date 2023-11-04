using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace LsaPrivs
{
	internal static class LsaPrivs
	{
		private const int RTN_ERROR = 13;

		private static int Main(string[] args)
		{
			//
			// Pick up account name on argv[1].
			// Assumes source is ANSI. Resultant string is ANSI or Unicode
			//
			var AccountName = args.Length > 0 ? args[0] : Environment.UserName;

			//
			// Pick up machine name on argv[2], if appropriate
			// assumes source is ANSI. Resultant string is Unicode.
			//
			var wComputerName = args.Length > 1 ? args[1] : "";   // static machine name buffer

			//
			// Open the policy on the target machine. 
			//
			var Status = OpenPolicy(wComputerName.ToString(), LsaPolicyRights.POLICY_CREATE_ACCOUNT | LsaPolicyRights.POLICY_LOOKUP_NAMES, out var PolicyHandle);
			try
			{
				if (Status.Failed)
				{
					DisplayStatus("OpenPolicy", Status);
					return RTN_ERROR;
				}

				//
				// Obtain the SID of the user/group.
				// Note that we could target a specific machine, but we don't.
				// Specifying null for target machine searches for the SID in the
				// following order: well-known, Built-in and local, primary domain,
				// trusted domains.
				//
				if (GetAccountSid(
						null,          // default lookup logic
						AccountName,   // account to obtain SID
						out var pSid   // buffer to allocate to contain resultant SID
						))
				{
					//
					// We only grant the privilege if we succeeded in obtaining the
					// SID. We can actually add SIDs which cannot be looked up, but
					// looking up the SID is a good sanity check which is suitable for
					// most cases.

					//
					// Grant the SeServiceLogonRight to users represented by pSid.
					//
					Status = SetPrivilegeOnAccount(
								PolicyHandle,           // policy handle
								pSid,                   // SID to grant privilege
								"SeServiceLogonRight",  // Unicode privilege
								true                    // enable the privilege
								);

					pSid.Dispose();

					if (Status == NTStatus.STATUS_SUCCESS)
						return 0;
					else
						DisplayStatus("SetPrivilegeOnAccount", Status);
				}
				else
				{
					//
					// Error obtaining SID.
					//
					DisplayStatus("GetAccountSid", Win32Error.GetLastError());
				}
			}
			finally
			{
				PolicyHandle?.Dispose();
			}

			return RTN_ERROR;
		}

		private static bool GetAccountSid(string SystemName, string AccountName, out SafePSID Sid)
		{
			var cbSid = 128;    // initial allocation attempt
			var cchReferencedDomain = 16; // initial allocation size

			//
			// initial memory allocations
			//
			Sid = new SafePSID(cbSid);

			var ReferencedDomain = new StringBuilder(cchReferencedDomain);

			//
			// Obtain the SID of the specified account on the specified system.
			//
			while (!LookupAccountName(SystemName, AccountName, Sid, ref cbSid, ReferencedDomain, ref cchReferencedDomain, out _))
			{
				if (Win32Error.GetLastError() == Win32Error.ERROR_INSUFFICIENT_BUFFER)
				{
					//
					// reallocate memory
					//
					Sid.Size = cbSid;

					ReferencedDomain.Capacity = cchReferencedDomain;
				}
				else
					return false;
			}

			//
			// Indicate success.
			//
			return true;
		}

		private static NTStatus SetPrivilegeOnAccount(LSA_HANDLE PolicyHandle, PSID AccountSid, string PrivilegeName, bool bEnable)
		{
			//
			// grant or revoke the privilege, accordingly
			//
			return bEnable
				? LsaAddAccountRights(PolicyHandle, AccountSid, new[] { PrivilegeName }, 1)
				: LsaRemoveAccountRights(PolicyHandle, AccountSid, false, new[] { PrivilegeName }, 1);
		}

		private static NTStatus OpenPolicy(string ServerName, LsaPolicyRights DesiredAccess, out SafeLSA_HANDLE PolicyHandle) =>
			//
			// Attempt to open the policy.
			//
			LsaOpenPolicy(ServerName, LSA_OBJECT_ATTRIBUTES.Empty, DesiredAccess, out PolicyHandle);

		private static void DisplayStatus(string szAPI, IErrorProvider Status)
		{
			//
			// Convert the NTStatus to Winerror. Then call DisplayWinError().
			//
			Console.Error.WriteLine(szAPI + " error!");
			Console.Error.WriteLine(Status.ToString());
		}
	}
}
