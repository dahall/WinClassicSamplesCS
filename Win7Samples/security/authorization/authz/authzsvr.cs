using Vanara.InteropServices;
using Vanara.PInvoke;
using static Common;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Authz;
using static Vanara.PInvoke.Kernel32;

internal partial class Program
{
	private static uint AuthzSvr(IntPtr ptr)
	{
		if (!Impersonate(Marshal.PtrToStringUni(ptr)!, pwd, out var hSvrToken))
			HandleError(GetLastError(), "Impersonate", true, true);

		if (!TogglePrivileges(["SeSecurityPrivilege"], true))
			HandleError(GetLastError(), "AdjustTokenPrivileges", true, true);

		using FUNDSRM FundsRM = new(200000000);

		SetupNamedPipe(out SafeHPIPE? hPipe, "\\\\.\\pipe\\AuthzSamplePipe");

		while (!svrExit.IsSet)
		{
			// Wait for a client...

			if (!ConnectNamedPipe(hPipe))
				HandleError(GetLastError(), "ConnectNamedPipe", true, true);

			try
			{
				var dwBytesRead = ReadFromPipe(hPipe, out EX_BUF exBuf);
				if (dwBytesRead == 0)
					Console.Write("Error reading from client\n");

				// Get Token

				if (!ImpersonateNamedPipeClient(hPipe))
					HandleError(GetLastError(), "ImpersonateNamedPipeClient", true, true);

				if (!GetUserName(out var ClientName))
					HandleError(GetLastError(), "GetUserName", true, true);

				if (!OpenThreadToken(GetCurrentThread(), TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_IMPERSONATE, false, out SafeHTOKEN? hToken))
					HandleError(GetLastError(), "OpenThreadToken", true, true);

				using (hToken)
				{
					RevertToSelf();

					Console.Write($"{ClientName} requests {ExNames[(int)exBuf.dwType]} expense of {exBuf.dwAmmount} cents\n");

					// use token to and context to vaidate - note that this sample uses the token if we just had a user's sid we could build
					// an authz context with that.

					if (!FundsRM.AuthorizeAndExecuteExpense(hToken, exBuf, out Win32Error dwResult))
					{
						Console.Write($"Error executing expense: {dwResult}\n");
					}

					WriteToPipe(hPipe, dwResult);
				}
			}
			finally
			{
				DisconnectNamedPipe(hPipe);
			}
		}

		return 0;
	}

	//
	// Routine Description:
	// Attempts to enable or disable a given privilege. Returns the previous state for the privilege.
	//
	private static bool TogglePrivileges(string[] privilegeNames, bool enable)
	{
		using SafeHTOKEN token = SafeHTOKEN.FromThread(GetCurrentThread(), TokenAccess.TOKEN_ADJUST_PRIVILEGES | TokenAccess.TOKEN_QUERY);
		var newPriv = new TOKEN_PRIVILEGES(Array.ConvertAll(privilegeNames, s => new LUID_AND_ATTRIBUTES(LUID.FromName(s), enable ? PrivilegeAttributes.SE_PRIVILEGE_ENABLED : 0)));
		return AdjustTokenPrivileges(token, false, newPriv, out _).Succeeded;
	}
}

// struct that maintains the state of the fund
internal class FUNDSRM : IDisposable
{
	private const uint MaxSpendingEmployee = 50000;
	private const uint MaxSpendingManager = 1000000;
	private const uint MaxSpendingVP = 100000000;

	// The amount of money available in the fund
	public uint dwFundsAvailable;

	// The resource manager, initialized with the callback functions
	public SafeAUTHZ_RESOURCE_MANAGER_HANDLE hRM;

	// The security descriptor for the fund, containing a callback ACE which causes the resource manager callbacks to be used
	public SafePSECURITY_DESCRIPTOR SD;

	private static readonly SafePSID EmployeeSid = new([0x00, 0x00, 0x05, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x15, 0x17, 0xb8, 0x51, 0x59, 0x25, 0x5d, 0x72, 0x66, 0x0b, 0x3b, 0x63, 0x64, 0x00, 0x01, 0x00, 0x03]);
	private static readonly SafePSID ManagerSid = new([0x00, 0x00, 0x05, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x15, 0x17, 0xb8, 0x51, 0x59, 0x25, 0x5d, 0x72, 0x66, 0x0b, 0x3b, 0x63, 0x64, 0x00, 0x01, 0x00, 0x02]);
	private static readonly SafePSID VPSid = new([0x00, 0x00, 0x05, 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x15, 0x17, 0xb8, 0x51, 0x59, 0x25, 0x5d, 0x72, 0x66, 0x0b, 0x3b, 0x63, 0x64, 0x00, 0x01, 0x00, 0x01]);
	private bool disposedValue;

	public FUNDSRM(uint dwFundsAvailable)
	/*++

	Routine Description

	Initializes the Authz Resource Manager, providing it
	with the appropriate callback functions.
	It also creates a security descriptor for the fund, allowing only
	corporate and transfer expenditures, not personal. Additional logic
	could be added to allow VPs to override these restrictions, etc.

	Arguments

	PFUNDSRM pFundsRM - Pointer to a FUNDSRM struct that maintains the state
	of the Resource Manager

	uint dwFundsAvailable - The amount of money in the fund managed by this
	resource manager

	Return Value
	None.
	--*/
	{
		PSID_IDENTIFIER_AUTHORITY siaWorld = KnownSIDAuthority.SECURITY_WORLD_SID_AUTHORITY;

		// The amount of money in the fund

		this.dwFundsAvailable = dwFundsAvailable;

		// Initialize the fund's resource manager

		if (!AuthzInitializeResourceManager(AuthzResourceManagerFlags.AUTHZ_RM_FLAG_NO_AUDIT | AuthzResourceManagerFlags.AUTHZ_RM_FLAG_INITIALIZE_UNDER_IMPERSONATION,
			AuthzAccessCheckCallback, AuthzComputeGroupsCallback, AuthzFreeGroupsCallback, "SampRM", out hRM))
			HandleError(GetLastError(), "AuthzInitializeResourceManager", true, true);
		else
			Console.Write("Funds Resource Manager initialized - waiting for client\n\n");

		// Create the fund's security descriptor

		SD = new(Marshal.SizeOf<SECURITY_DESCRIPTOR>());
		if (!SD.SetGroup(default, false))
			HandleError(GetLastError(), "SetSecurityDescriptorGroup", true, true);

		if (!SD.SetSacl(false, default, false))
			HandleError(GetLastError(), "SetSecurityDescriptorSacl", true, true);

		// an owner must be specified. Since VPs are the highest privileged group this sample we'll make them the owner.
		if (!SD.SetOwner(VPSid, false))
			HandleError(GetLastError(), "SetSecurityDescriptorOwner", true, true);

		// Initialize the DACL for the fund

		SafePACL pDaclFund = new(1024, ACL_REVISION_DS);

		// Add an access-allowed ACE for Everyone Only company spending and transfers are allowed for this fund

		// build EVERYONE SID
		using (SafePSID psidEveryone = SafePSID.Everyone)
		{
			using SafePACE pace = new(ACE_TYPE.ACCESS_ALLOWED_CALLBACK_ACE_TYPE, (int)(ACCESS_FUND.ACCESS_FUND_CORPORATE | ACCESS_FUND.ACCESS_FUND_TRANSFER), psidEveryone);
			pDaclFund.Add(pace);
		}

		// Add that ACL as the security descriptor's DACL

		if (!SD.SetDacl(true, pDaclFund, false))
			HandleError(GetLastError(), "SetSecurityDescriptorDacl", true, true);
	}

	~FUNDSRM()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: false);
	}

	public bool AuthorizeAndExecuteExpense(HTOKEN hToken, in EX_BUF exBuf, out Win32Error pdwResult)
	/*++

	Routine Description

	Setups the Authz context and makes call to AuthzAccessCheck. Then
	modifies the remaining funds depending on acccess and ammount

	Arguments

	PFUNDSRM pFundsRM - Pointer to a FUNDSRM struct that maintains the state
	of the Resource Manager

	HANDLE hToken - The token representing the user were doing the access
	check for

	EX_BUF exBuf - struct that contains desired access and expense ammount

	ref uint pdwResult - Pointer to uint to put result/error

	Return Value
	bool True if no errors.
	--*/
	{
		// first we need an Authz context

		if (!AuthzInitializeContextFromToken(0,
			hToken,
			hRM,
			default,
			default,
			default,
			out SafeAUTHZ_CLIENT_CONTEXT_HANDLE? AuthzClient))
		{
			HandleError(pdwResult = GetLastError(), "AuthzInitializeContextFromToken", true, false);
			return false;
		}

		try
		{
			// Do AccessCheck
			AUTHZ_ACCESS_REQUEST AccessRequest = new()
			{
				DesiredAccess = (int)exBuf.dwType,
				PrincipalSelfSid = default,
				ObjectTypeList = default,
				ObjectTypeListLength = 0,
				OptionalArguments = (int)exBuf.dwAmmount,
			};

			// The ResultListLength is set to the number of ObjectType GUIDs in the Request, indicating that the caller would like
			// detailed information about granted access to each node in the tree.
			AUTHZ_ACCESS_REPLY AccessReply = new(1)
			{
				GrantedAccessMaskValues = [0],
				ErrorValues = [0]
			};

			if (!AuthzAccessCheck(0,
				AuthzClient,
				AccessRequest,
				default,
				SD,
				default,
				0,
				AccessReply,
				default))
			{
				HandleError(pdwResult = GetLastError(), "AuthzAccessCheck", true, false);
				return false;
			}

			if ((AccessReply.GrantedAccessMaskValues[0] & (uint)exBuf.dwType) != (uint)exBuf.dwType)
			{
				// Access is denied get error from reply if there else Getlasterror.
				pdwResult = AccessReply.ErrorValues[0];
				Console.Write("Access denied\n\n");
			}
			else
			{
				// Access is granted, is there enough funds, this could have been done within the AuthzAccessCheckCallback but since this
				// is more of an execution problem than access checking we'll do it here.
				if (dwFundsAvailable < exBuf.dwAmmount)
				{
					// not enough in ref fund
					pdwResult = ERROR_INSUFFICIENT_FUNDS;
					Console.Write("Failed : NSF\n\n");
				}
				else
				{
					dwFundsAvailable -= exBuf.dwAmmount;
					pdwResult = EXPENSE_APPROVED;
					Console.Write("Expense Approved. Remaining funds:{0} cents.\n\n", dwFundsAvailable);
				}
			}

			return true;
		}
		finally
		{
			AuthzClient?.Dispose();
		}
	}

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				// TODO: dispose managed state (managed objects)
			}

			hRM.Dispose();
			SD.Dispose();
			disposedValue = true;
		}
	}

	private static bool AuthzAccessCheckCallback([In] AUTHZ_CLIENT_CONTEXT_HANDLE hAuthzClientContext, [In] PACE pAce, [In] IntPtr pArgs, ref bool pbAceApplicable)

	/*++

	Routine Description

	This is the callback access check. It is registered with a
	resource manager. AuthzAccessCheck calls this function when it
	encounters a callback type ACE, one of:
	ACCESS_ALLOWED_CALLBACK_ACE_TYPE
	ACCESS_ALLOWED_CALLBACK_OBJECT_ACE_TYPE
	ACCESS_DENIED_CALLBACK_OBJECT_ACE_TYPE

	This function determines if the given callback ACE applies to the
	client context (which has already had dynamic groups computed) and
	the optional arguments, in this case the request amount.

	The list of groups which apply to the user is traversed. If a group
	is found which allows the user the requested access, pbAceApplicable
	is set to true and the function returns. If the end of the group list
	is reached, pbAceApplicable is set to false and the function returns.

	Arguments

	hAuthzClientContext - handle to the AuthzClientContext.

	pAce - pointer to the Ace header.

	pArgs - optional arguments, in this case ref uint , uint is the spending
	request amount in cents

	pbAceApplicable - returns true iff the ACE allows the client's request

	Return value

	Bool, true on success, false on error

	--*/
	{
		uint dwRequestedSpending = (uint)pArgs.ToInt32();

		// By default, the ACE does not apply to the request

		pbAceApplicable = false;

		// The object's access mask (right after the ACE_HEADER) The access mask determines types of expenditures allowed from this fund

		//uint pAccessMask = pAce.GetMask();

		// Get the TOKEN_GROUPS array

		TOKEN_GROUPS pvTokenGroupsBuf;
		try { pvTokenGroupsBuf = AuthzGetInformationFromContext<TOKEN_GROUPS>(hAuthzClientContext, AUTHZ_CONTEXT_INFORMATION_CLASS.AuthzContextInfoGroupsSids); }
		catch { return false; }

		// Go through the groups until end is reached or a group applying to the request is found

		for (int i = 0; i < pvTokenGroupsBuf.GroupCount && pbAceApplicable != true; i++)
		{
			// This is the business logic. Each level of employee can approve different amounts.

			// VP

			if (VPSid.Equals(pvTokenGroupsBuf.Groups[i].Sid) && dwRequestedSpending <= MaxSpendingVP)
			{
				pbAceApplicable = true;
			}

			// Manager

			if (ManagerSid.Equals(pvTokenGroupsBuf.Groups[i].Sid) && dwRequestedSpending <= MaxSpendingManager)
			{
				pbAceApplicable = true;
			}

			// Employee

			if (EmployeeSid.Equals(pvTokenGroupsBuf.Groups[i].Sid) && dwRequestedSpending <= MaxSpendingEmployee)
			{
				pbAceApplicable = true;
			}
		}

		// return true when access check completed (when a callback ace applies not) If we had a runtime error, such as mem alloc errors
		// we would return false
		return true;
	}

	private static bool AuthzComputeGroupsCallback(AUTHZ_CLIENT_CONTEXT_HANDLE hAuthzClientContext, nint pArgs, out nint pSidAttrArray, out uint pSidCount, out nint pRestrictedSidAttrArray, out uint pRestrictedSidCount)
	/*++

	Routine Description

	Resource manager callback to compute dynamic groups. This is used by the RM
	to decide if the specified client context should be included in any RM defined groups.

	In this example, the employees are hardcoded into their roles. However, this is the
	place you would normally retrieve data from an external source to determine the
	users' additional roles.

	Arguments

	hAuthzClientContext - handle to client context.
	Args - optional parameter to pass information for evaluating group membership.
	pSidAttrArray - computed group membership SIDs
	pSidCount - count of SIDs
	pRestrictedSidAttrArray - computed group membership restricted SIDs
	pRestrictedSidCount - count of restricted SIDs

	Return Value

	Bool, true for success, false on failure.

	--*/
	{
		pSidAttrArray = pRestrictedSidAttrArray = default;
		pSidCount = pRestrictedSidCount = 0;

		// First, look up the user's SID from the context

		// Get the SID (inside a TOKEN_USER structure)

		TOKEN_USER pvSidBuf;
		try { pvSidBuf = AuthzGetInformationFromContext<TOKEN_USER>(hAuthzClientContext, AUTHZ_CONTEXT_INFORMATION_CLASS.AuthzContextInfoUserSid); }
		catch { return false; }

		// The hardcoded Sample logic:
		//
		// Lookup the sid to get the username and grant dynamic sid based on username
		//
		// Bob is a VP Martha is a Manager Joe is an Employee

		if (!LookupAccountSid(default, pvSidBuf.User.Sid.GetBinaryForm(), out var UserName, out _, out _))
		{
			HandleError(GetLastError(), "LookupAccountSid", true, true);
		}

		PSID userSid = UserName!.ToLowerInvariant() switch
		{
			"bob" => VPSid,
			"martha" => ManagerSid,
			"joe" => EmployeeSid,
			_ => PSID.NULL
		};
		if (userSid != PSID.NULL)
		{
			// Allocate the memory for the returns, which will be deallocated by FreeDynamicGroups Only a single group will be returned,
			// determining the employee type

			pSidCount = 1;
			// No restricted group sids
			SafeHGlobalStruct<SID_AND_ATTRIBUTES> pSidAttrArrayBuf = new SID_AND_ATTRIBUTES(userSid, (uint)GroupAttributes.SE_GROUP_ENABLED);
			pSidAttrArray = pSidAttrArrayBuf.ReleaseOwnership();
		}

		return true;
	}

	private static void AuthzFreeGroupsCallback(nint pSidAttrArray)
	/*++

	Routine Description

	Frees memory allocated for the dynamic group array.

	Arguments

	pSidAttrArray - array to free.

	Return Value
	None.
	--*/
	{
		if (pSidAttrArray != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(pSidAttrArray);
		}
	}
}