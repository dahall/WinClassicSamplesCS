using System;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NetApi32;

namespace MachAcct
{
	internal static class MachAcct
	{
		private static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.Error.WriteLine($"Usage: {Environment.CommandLine} <machineaccountname> [domain]");
				return;
			}

			var wMachineAccount = args[0];

			// if a domain name was specified, fetch the computer name of the primary domain controller
			string wPrimaryDC = null;
			if (args.Length == 2)
				NetGetDCName(null, args[1], out wPrimaryDC).ThrowIfFailed();

			AddMachineAccount(
				wPrimaryDC,         // primary DC computer name
				wMachineAccount,    // computer account name
				UserAcctCtrlFlags.UF_WORKSTATION_TRUST_ACCOUNT  // computer account type
				);
		}

		private static void AddMachineAccount(
			string wTargetComputer,
			string MachineAccount,
			UserAcctCtrlFlags AccountType
			)
		{
			// ensure a valid computer account type was passed
			if (AccountType != UserAcctCtrlFlags.UF_WORKSTATION_TRUST_ACCOUNT &&
				AccountType != UserAcctCtrlFlags.UF_SERVER_TRUST_ACCOUNT &&
				AccountType != UserAcctCtrlFlags.UF_INTERDOMAIN_TRUST_ACCOUNT)
				throw ((Win32Error)Win32Error.ERROR_INVALID_PARAMETER).GetException();

			// obtain number of chars in computer account name
			var cchLength = MachineAccount.Length;

			// ensure computer name doesn't exceed maximum length
			if (cchLength > MAX_COMPUTERNAME_LENGTH)
				throw ((Win32Error)Win32Error.ERROR_INVALID_ACCOUNT_NAME).GetException();

			// password is the computer account name converted to lowercase convert the passed MachineAccount in place
			var wPassword = MachineAccount.ToLower();

			// convert computer account name to uppercase. computer account names have a trailing Unicode '$'
			var wAccount = MachineAccount.ToUpper() + '$';

			// if the password is greater than the max allowed, truncate
			if (cchLength > LM20_PWLEN) wPassword = wPassword.Substring(0, LM20_PWLEN);

			// initialize USER_INFO_x structure
			var ui = new USER_INFO_1
			{
				usri1_name = wAccount,
				usri1_password = wPassword,
				usri1_flags = AccountType | UserAcctCtrlFlags.UF_SCRIPT,
				usri1_priv = UserPrivilege.USER_PRIV_USER
			};

			try
			{
				NetUserAdd(wTargetComputer, ui);
			}
			catch (UnauthorizedAccessException)
			{
				// try to enable the SeMachineAccountPrivilege
				SetCurrentPrivilege("SeMachineAccountPrivilege", true, out var Previous);

				try
				{
					// enabled the privilege. retry the add operation
					NetUserAdd(wTargetComputer, ui);
				}
				finally
				{
					// disable the privilege
					SetCurrentPrivilege("SeMachineAccountPrivilege", Previous, out _);
				}
			}
		}

		private static void SetCurrentPrivilege(
			string Privilege,           // Privilege to enable/disable
			bool bEnablePrivilege,      // to enable or disable privilege
			out bool bPreviousPrivilege // returns previous state of the privilege
			)
		{
			bPreviousPrivilege = false;

			if (!LookupPrivilegeValue(null, Privilege, out var luid)) Win32Error.ThrowLastError();

			using var hToken = SafeHTOKEN.FromProcess(GetCurrentProcess(), TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_ADJUST_PRIVILEGES);

			// first pass. get current privilege setting
			AdjustTokenPrivileges(hToken, false, new TOKEN_PRIVILEGES(luid, 0), out var tpPrevious).ThrowIfFailed();

			// second pass. set privilege based on previous setting
			bPreviousPrivilege = tpPrevious.Privileges[0].Attributes.IsFlagSet(PrivilegeAttributes.SE_PRIVILEGE_ENABLED);

			AdjustTokenPrivileges(hToken, false, new TOKEN_PRIVILEGES(luid, tpPrevious.Privileges[0].Attributes.SetFlags(PrivilegeAttributes.SE_PRIVILEGE_ENABLED, bEnablePrivilege)), out _).ThrowIfFailed();
		}
	}
}