using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.NetApi32;
//using static Vanara.PInvoke.User32;

public static class Common
{
	///////////////////////////////////////////////////////////////////////////////
	// Access flags for funds RM
	//
	///////////////////////////////////////////////////////////////////////////////

	//
	// Expense failed insufficient funds
	//
	public const int ERROR_INSUFFICIENT_FUNDS = 0x20000002;

	//
	// Expense approved and subtracted from fund.
	//
	public const int EXPENSE_APPROVED = 0;

	//
	// Expense failed due to an unknown error
	//
	public const int EXPENSE_UNKNOWN_ERROR = 0x20000003;

	//
	// Error with bit 29 set are private errors (see SetLastError doc)
	//
	public const int PRIVATE_ERROR_BIT = 0x20000000;

	public static readonly string[] ExNames = ["", "PERSONAL", "CORPORATE", "", "TRANSFER"];

	[Flags]
	public enum ACCESS_FUND : uint
	{
		// Personal expenditures
		ACCESS_FUND_PERSONAL = 0x00000001,
		// Company spending
		ACCESS_FUND_CORPORATE = 0x00000002,
		// Transfer to other funds
		ACCESS_FUND_TRANSFER = 0x00000004,
	}


	///////////////////////////////////////////////////////////////////////////////
	// Codes for expense access attempts
	//
	///////////////////////////////////////////////////////////////////////////////
	//
	// We'll use existing Win32Error.Win32Error.Win32Error.ERROR_ACCESS_DENIED for access denied.
	//
	// ERROR_ACCESS_DENIED = 0x00000005
	///////////////////////////////////////////////////////////////////////////////
	// Expense request packing struct
	//
	///////////////////////////////////////////////////////////////////////////////

	public static bool BuildGenericAccessAcl(out SafePACL ppAcl)
	/*++

	Routine Description

	This function builds a Dacl which grants the creator of the objects
	GENERIC_ALL (Full Control) and Everyone GENERIC_READ, GENERIC_WRITE and
	GENERIC_EXECUTE access to the object.

	This Dacl allows for higher security than a default Dacl, as this only grants
	the creator/owner write access to the security descriptor, and grants 
	Everyone the ability to "use" the object. This scenario prevents a 
	malevolent user from disrupting service by preventing arbitrary access 
	manipulation.

	Arguments

	ref PACL pAcl - Pointer to buffer for pointer to allocated PACL. Must be
	freed with LocalFree

	ref uint cbAclSize - Pointer to dword receiving size of acl.

	Return value

	Bool, true on success, false on error

	--*/
	{
		//
		// build well known sids
		//

		// build EVERYONE SID
		using SafePSID pEveryoneSid = SafePSID.Everyone;

		// build Creator/Owner SID
		AllocateAndInitializeSid(KnownSIDAuthority.SECURITY_CREATOR_SID_AUTHORITY, 1, KnownSIDRelativeID.SECURITY_CREATOR_OWNER_RID, 0, 0, 0, 0, 0, 0, 0, out var pOwnerSid);

		ppAcl = new SafePACL([
			new SafePACE(ACE_TYPE.ACCESS_ALLOWED_ACE_TYPE, ACCESS_MASK.GENERIC_READ | ACCESS_MASK.GENERIC_WRITE | ACCESS_MASK.GENERIC_EXECUTE, pEveryoneSid),
			new SafePACE(ACE_TYPE.ACCESS_ALLOWED_ACE_TYPE, ACCESS_MASK.GENERIC_ALL, pOwnerSid),
		]);

		return true;
	}

	public static bool CreateLocalAcct(string pszName, string pszPassword, UserPrivilege priv = UserPrivilege.USER_PRIV_USER, UserAcctCtrlFlags flags = 0)
	{
		USER_INFO_1 ui = new() { usri1_name = pszName, usri1_password = pszPassword, usri1_priv = priv, usri1_flags = flags };
		try { NetUserAdd(null, ui); return true; } catch (Exception ex) { return ex.HResult == ((Win32Error)Win32Error.NERR_UserExists).ToHRESULT(); }
	}

	public static bool Impersonate(string pszName, string pszPassword, out SafeHTOKEN hToken)
	{
		if (!LogonUser(pszName, ".", pszPassword, LogonUserType.LOGON32_LOGON_INTERACTIVE, LogonUserProvider.LOGON32_PROVIDER_DEFAULT, out hToken))
			return false;
		return ImpersonateLoggedOnUser(hToken);
	}

	//////////////////////////////////////////////////////////////////////
	public static Win32Error DisplayAPIError(string pszAPI, bool bConsole, bool bMsgBox, bool bExit)
	{
		var dwError = Win32Error.GetLastError();

		//... now display this string
		var szErrMsgBuffer = $"ERROR: API = {pszAPI}.\nERROR CODE = {(uint)dwError} (0x{(uint)dwError:X}).\nMESSAGE = {dwError}";

		if (bConsole)
			Console.Write(szErrMsgBuffer);
		//if (bMsgBox)
		//	MessageBox(GetDesktopWindow(), szErrMsgBuffer, "Execution Error", MB_FLAGS.MB_OK);

		OutputDebugString(szErrMsgBuffer);

		if (bExit)
			ExitProcess((uint)dwError);

		return dwError;
	}

	public static void HandleError(Win32Error dwErr, string pszAPI, bool fAPI, bool fExit)
	{
		if (fAPI)
		{
			DisplayAPIError(pszAPI, true, true, fExit);
		}
		else
		{
			Console.Write(pszAPI);
			if (dwErr.Failed)
				Console.Write($"{(uint)dwErr}\n");

			if (fExit)
				ExitProcess(0);
		}

		return;
	}

	public static uint ReadFromPipe<T>(HFILE hPipe, out T pBuffer) where T : struct
	{
		var bRet = ReadFile<T>(hPipe, out pBuffer);
		if (!bRet)
		{
			var dwErr = GetLastError();
			// if ERROR_BROKEN_PIPE or ERROR_NO_DATA then pipe naturally ended
			if ((uint)dwErr is not (Win32Error.ERROR_BROKEN_PIPE or Win32Error.ERROR_NO_DATA))
				HandleError(dwErr, "ReadFile", true, true);
		}

		return InteropExtensions.SizeOf<T>();
	}

	public static bool SetupNamedPipe(out SafeHPIPE phPipe, string szPipeName)
	{
		phPipe = SafeHPIPE.Null;
		SafePSECURITY_DESCRIPTOR sd = new(Marshal.SizeOf<SECURITY_DESCRIPTOR>());

		if (!BuildGenericAccessAcl(out var pDacl))
		{
			HandleError(0, "Error setting up pipe dacl", false, true);
			return false;
		}

		if (!sd.SetDacl(true, pDacl, false))
		{
			HandleError(GetLastError(), "SetSecurityDescriptorDacl", true, true);
			return false;
		}

		SECURITY_ATTRIBUTES sa = new() { lpSecurityDescriptor = sd };

		// setup pipe and wait for a ref connection
		phPipe = CreateNamedPipe(szPipeName, PIPE_ACCESS.FILE_FLAG_OVERLAPPED | PIPE_ACCESS.PIPE_ACCESS_DUPLEX,
			PIPE_TYPE.PIPE_TYPE_MESSAGE | PIPE_TYPE.PIPE_READMODE_MESSAGE | PIPE_TYPE.PIPE_WAIT, 1, 0, 0,
			NMPWAIT_USE_DEFAULT_WAIT, sa);
		if (phPipe.IsInvalid)
		{
			HandleError(GetLastError(), "CreateNamedPipe", true, true);
			return false;
		}

		return true;
	}

	public static bool WriteToPipe<T>(HFILE hPipe, T pData) where T : struct
	{
		using var pDataBuffer = SafeHGlobalHandle.CreateFromStructure(pData);
		var bRet = WriteFile(hPipe, pDataBuffer, (uint)pDataBuffer.Size, out var nBytesWrote);
		if (!bRet)
		{
			var dwErr = GetLastError();
			// if ERROR_BROKEN_PIPE or ERROR_NO_DATA then pipe naturally ended
			if ((uint)dwErr is not (Win32Error.ERROR_BROKEN_PIPE or Win32Error.ERROR_NO_DATA))
				HandleError(dwErr, "WriteFile", true, true);
		}
		return bRet;
	}

	public struct EX_BUF
	{
		public uint dwAmmount;
		public ACCESS_FUND dwType;
	}
}