// Each individual permission for our resource manager
using System.Security.AccessControl;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static CustomResourceManager.Utility;
using static Vanara.PInvoke.AclUI;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Authz;
using static Vanara.PInvoke.Kernel32;

namespace CustomResourceManager;

internal class CSecInfo : ISecurityInformation, ISecurityInformation3, IEffectivePermission2, ISecurityObjectTypeInfo
{
	public const int CHANGE_PERMS_PERM = 0x0800;
	public const int CREATE_PERM = 0x0001;
	public const int DESTROY_PERM = 0x0200;
	public const int GENERIC_ADMIN_PERM = GENERIC_MOD_PERM | DESTROY_PERM | VIEW_PERMS_PERM | CHANGE_PERMS_PERM;
	public const int GENERIC_MOD_PERM = GENERIC_POST_PERM | UPDATE_OTHERS_PERM | HIDE_PERM | SHOW_PERM | LOCK_PERM | UNLOCK_PERM;

	// Each tier of permissions builds upon the last, but they don't have to.
	public const int GENERIC_POST_PERM = CREATE_PERM | READ_PERM | VOTE_PERM | UPDATE_OWN_PERM;

	public const int HIDE_PERM = 0x0020;
	public const INHERIT_FLAGS INHERIT_FULL = INHERIT_FLAGS.CONTAINER_INHERIT_ACE | INHERIT_FLAGS.OBJECT_INHERIT_ACE;
	public const int LOCK_PERM = 0x0080;
	public const int READ_PERM = 0x0002;
	public const int SHOW_PERM = 0x0040;
	public const int UNLOCK_PERM = 0x0100;
	public const int UPDATE_OTHERS_PERM = 0x0010;
	public const int UPDATE_OWN_PERM = 0x0008;
	public const int VIEW_PERMS_PERM = 0x0400;
	public const int VOTE_PERM = 0x0004;

	// This will be set to true if our ctor produces an error.
	public bool m_bFailedToConstruct;

	internal const int NUMBER_OF_RESOURCES = 10;

	private static readonly SI_ACCESS[] g_siForumsAccess =
	[
		// This structure describes each flag in the file access mask. It is constant. ACLUI displays these strings in its UI.
		new(GENERIC_ADMIN_PERM, "Administer", INHERIT_FLAGS.SI_ACCESS_GENERAL | INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(GENERIC_MOD_PERM, "Moderate", INHERIT_FLAGS.SI_ACCESS_GENERAL | INHERIT_FULL ),
		new(GENERIC_POST_PERM, "Post", INHERIT_FLAGS.SI_ACCESS_GENERAL | INHERIT_FULL ),
		new(CREATE_PERM, "Create", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(READ_PERM, "Read", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(VOTE_PERM, "Vote", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(UPDATE_OWN_PERM, "Update / edit own content", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(UPDATE_OTHERS_PERM, "Update / edit others' content", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(HIDE_PERM, "Hide", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(SHOW_PERM, "Show", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(LOCK_PERM, "Lock", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(UNLOCK_PERM, "Unlock", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(DESTROY_PERM, "Destroy / delete", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(VIEW_PERMS_PERM, "View permissions", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
		new(CHANGE_PERMS_PERM, "Change permissions", INHERIT_FLAGS.SI_ACCESS_SPECIFIC | INHERIT_FULL ),
	];

	private static readonly SafeCoTaskMemStruct<Guid> guidNullPtr = Guid.Empty;

	// ObjectInherit - applies to parent and only child OBJECTS (e.g. files) ContainerInherit - applies to parent and only child CONTAINERS
	// (e.g. folders) InheritOnly - doesn't apply to parent, must be combined with something else NoPropagateInherit - only applies to child,
	// not grandchildren
	//
	// Note: I only use container inheritance
	private static readonly SI_INHERIT_TYPE[] siSDKInheritTypes =
	[
		new(guidNullPtr, 0, "This object only"),
		new(guidNullPtr, INHERIT_FLAGS.CONTAINER_INHERIT_ACE, "This object and children (sections/topics)"),
		new(guidNullPtr, INHERIT_FLAGS.INHERIT_ONLY_ACE | INHERIT_FLAGS.CONTAINER_INHERIT_ACE, "Children (sections/topics) only"),
	];

	// Define the generic mapping array. This is used to denote the mapping of each generic access right to a specific access mask. This is
	// used on the basic ACL Editor page.
	private static GENERIC_MAPPING ObjectMap = new(GENERIC_POST_PERM, GENERIC_MOD_PERM, GENERIC_ADMIN_PERM, 0);

	private SI_ACCESS[] m_AccessTable = g_siForumsAccess;
	private uint m_AccessTableCount = (uint)g_siForumsAccess.Length;
	private uint m_DefaultAccess;
	const string m_defaultSecurityDescriptorSddl = @"O:WDG:BAD:AI(A;CIIO;FA;;;WD)(A;;FA;;;BA)S:AI(AU;SAFACIIO;FA;;;WD)";

	// Tell ACL UI what to show
	private SI_OBJECT_INFO_Flags m_dwSIFlags;

	// This represents the index (see resource.h's ResourceIndices) of the resource that we're currently editing.
	private ResourceIndices m_editingResource;

	private OBJECT_TYPE_LIST m_objectTypeList;

	// This points to all of the resources that the sample keeps track of. The constructor sets these up.
	private Dictionary<ResourceIndices, Resource> m_resources;

	public CSecInfo()
	{
		const string noDaclOrSacl = "O:WDG:BA";

		m_resources = new() {
			{ ResourceIndices.CONTOSO_FORUMS, new Resource("Contoso forums", ResourceType.FORUM, noDaclOrSacl, ResourceIndices.NONEXISTENT_OBJECT) },
			{ ResourceIndices.SPORTS, new Resource("Sports", ResourceType.SECTION, noDaclOrSacl, ResourceIndices.CONTOSO_FORUMS) },
			{ ResourceIndices.FAVORITE_TEAM, new Resource("Favorite team", ResourceType.TOPIC, noDaclOrSacl, ResourceIndices.SPORTS) },
			{ ResourceIndices.UPCOMING_EVENTS, new Resource("Upcoming events", ResourceType.TOPIC, noDaclOrSacl, ResourceIndices.SPORTS) },
			{ ResourceIndices.MOVIES, new Resource("Movies", ResourceType.SECTION, noDaclOrSacl, ResourceIndices.CONTOSO_FORUMS) },
			{ ResourceIndices.NEW_RELEASES, new Resource("2012 releases", ResourceType.TOPIC, noDaclOrSacl, ResourceIndices.MOVIES) },
			{ ResourceIndices.CLASSICS, new Resource("Classics", ResourceType.TOPIC, noDaclOrSacl, ResourceIndices.MOVIES) },
			{ ResourceIndices.HOBBIES, new Resource("Hobbies", ResourceType.SECTION, noDaclOrSacl, ResourceIndices.CONTOSO_FORUMS) },
			{ ResourceIndices.LEARNING_TO_COOK, new Resource("Learning to cook", ResourceType.TOPIC, noDaclOrSacl, ResourceIndices.HOBBIES) },
			{ ResourceIndices.SNOWBOARDING, new Resource("Snowboarding", ResourceType.TOPIC, noDaclOrSacl, ResourceIndices.HOBBIES) },
		};

		// Associate parents with their children
		foreach (ResourceIndices i in Enum.GetValues<ResourceIndices>().Where(i => i != ResourceIndices.NONEXISTENT_OBJECT))
		{
			var parentIndex = m_resources[i].ParentIndex;
			if (parentIndex != ResourceIndices.NONEXISTENT_OBJECT)
			{
				m_resources[parentIndex].AddChild(i);
			}
		}

		m_objectTypeList = new(ObjectTypeListLevel.ACCESS_OBJECT_GUID);

		// Initialize to a sane value even if we won't be editing the grandparent
		SetCurrentObject(0);

		SafePSECURITY_DESCRIPTOR pSelfRelativeSD;
		try
		{
			pSelfRelativeSD = new(m_defaultSecurityDescriptorSddl);
		}
		catch
		{
			Console.Write("Error calling ConvertStringSecurityDescriptorToSecurityDescriptor: {0}\n", GetLastError());
			m_bFailedToConstruct = true;
			return;
		}

		// Call SetSecurity on the forums root so that everything gets an inherited DACL.
		var hr = SetSecurity(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, pSelfRelativeSD);
		if (!hr.Succeeded)
		{
			Console.Write("Error calling SetSecurity: {0}\n", GetLastError());
			m_bFailedToConstruct = true;
			return;
		}
	}

	public HRESULT ComputeEffectivePermissionWithSecondarySecurity(PSID pSid, PSID pDeviceSid, string? pszServerName, SECURITY_OBJECT[] pSecurityObjects, uint dwSecurityObjectCount,
			in TOKEN_GROUPS pUserGroups, Authz.AUTHZ_SID_OPERATION[]? pAuthzUserGroupsOperations, in TOKEN_GROUPS pDeviceGroups, Authz.AUTHZ_SID_OPERATION[]? pAuthzDeviceGroupsOperations,
			in Authz.AUTHZ_SECURITY_ATTRIBUTES_INFORMATION pAuthzUserClaims, Authz.AUTHZ_SECURITY_ATTRIBUTE_OPERATION[]? pAuthzUserClaimsOperations,
			in Authz.AUTHZ_SECURITY_ATTRIBUTES_INFORMATION pAuthzDeviceClaims, Authz.AUTHZ_SECURITY_ATTRIBUTE_OPERATION[]? pAuthzDeviceClaimsOperations, EFFPERM_RESULT_LIST[] pEffpermResultLists)
	{
		try
		{
			// There is no concept of shares or CAPs in this resource manager, so the only security object in pSecurityObjects will be the SD
			// in question. The following checks aren't necessary when considering the sample code.
			if (dwSecurityObjectCount != 1)
			{
				Console.Write("Unexpected effective permissions argument data: dwSecurityObjectCount=={0}\n", dwSecurityObjectCount);
				return HRESULT.E_FAIL;
			}

			if (pSecurityObjects[0].Id != (uint)SECURITY_OBJECT_ID.SECURITY_OBJECT_ID_OBJECT_SD)
			{
				Console.Write("Unexpected[] pSecurityObjects = new[] Unexpected = new new[0].Id: {0}\n", pSecurityObjects[0].Id);
				return HRESULT.E_FAIL;
			}

			if (pSid.IsNull)
			{
				return HRESULT.E_INVALIDARG;
			}

			Win32Error.ThrowLastErrorIfFalse(AuthzInitializeResourceManager(AuthzResourceManagerFlags.AUTHZ_RM_FLAG_NO_AUDIT,
				null, null, null, "SDK Sample Resource Manager", out var hAuthzResourceManager), "AuthzInitializeResourceManager");

			Win32Error.ThrowLastErrorIfFalse(AuthzInitializeContextFromSid(0,
				pSid, // use the SID passed in to this function
				hAuthzResourceManager,
				default, // token will never expire (this isn't enforced anyway)
				new LUID(), // never interpreted by authz
				default,
				out var hAuthzUserContext), "AuthzInitializeContextFromSid");

			// AuthZ context representing the combination of client and device. If no device SID is passed in to this function, then it only
			// represents the user context.
			SafeAUTHZ_CLIENT_CONTEXT_HANDLE hAuthzCompoundContext;

			// Set up the different contexts
			if (pDeviceSid != default)
			{
				Win32Error.ThrowLastErrorIfFalse(AuthzInitializeContextFromSid(0,
					pDeviceSid, // use the device SID passed in to this function
					hAuthzResourceManager,
					default, // token will never expire (this isn't enforced anyway)
					new LUID(), // never interpreted by authz
					default,
					out var hAuthzDeviceContext), "AuthzInitializeContextFromSid (device)");

				Win32Error.ThrowLastErrorIfFalse(AuthzInitializeCompoundContext(hAuthzUserContext,
					hAuthzDeviceContext, out hAuthzCompoundContext), "AuthzInitializeCompoundContext");

				// Add device claims
				if (pAuthzDeviceClaimsOperations is not null)
				{
					Win32Error.ThrowLastErrorIfFalse(AuthzModifyClaims(hAuthzCompoundContext,
						AUTHZ_CONTEXT_INFORMATION_CLASS.AuthzContextInfoDeviceClaims,
						pAuthzDeviceClaimsOperations,
						pAuthzDeviceClaims), "AuthzModifyClaims (device claims)");
				}
			}
			else
			{
				hAuthzCompoundContext = hAuthzUserContext;
			}

			// Add user claims
			if (pAuthzUserClaimsOperations != default)
			{
				Win32Error.ThrowLastErrorIfFalse(AuthzModifyClaims(hAuthzCompoundContext,
					AUTHZ_CONTEXT_INFORMATION_CLASS.AuthzContextInfoUserClaims,
					pAuthzUserClaimsOperations,
					pAuthzUserClaims), "AuthzModifyClaims (user claims)");
			}

			// Add "what-if" device groups
			if (pAuthzDeviceGroupsOperations != default)
			{
				Win32Error.ThrowLastErrorIfFalse(AuthzModifySids(hAuthzCompoundContext,
					AUTHZ_CONTEXT_INFORMATION_CLASS.AuthzContextInfoDeviceSids,
					pAuthzDeviceGroupsOperations,
					pDeviceGroups), "AuthzModifySids (device groups)");
			}

			// Add "what-if" user groups
			if (pAuthzUserGroupsOperations != default)
			{
				Win32Error.ThrowLastErrorIfFalse(AuthzModifySids(hAuthzCompoundContext,
					AUTHZ_CONTEXT_INFORMATION_CLASS.AuthzContextInfoGroupsSids,
					pAuthzUserGroupsOperations,
					pUserGroups), "AuthzModifySids (user groups)");
			}

			SafePSECURITY_DESCRIPTOR pSD = new(pSecurityObjects[0].pData, false);

			// Access request specifies the desired access mask, principal self sid, the object type list strucutre (if any).
			AUTHZ_ACCESS_REQUEST request = new() { DesiredAccess = ACCESS_MASK.MAXIMUM_ALLOWED };

			using AUTHZ_ACCESS_REPLY reply = new(1);
			if (reply.GrantedAccessMask == IntPtr.Zero || reply.Error == IntPtr.Zero)
			{
				Console.Write("LocalAlloc failed.\n");
				return HRESULT.E_OUTOFMEMORY;
			}

			// Finally, call access check. This is the heart of this function.
			Win32Error.ThrowLastErrorIfFalse(AuthzAccessCheck(0, // deep copy the SD (default)
				hAuthzCompoundContext,
				request,
				default,
				pSD,
				default,
				0,
				reply,
				default), "AuthzAccessCheck");

			// Only one security object is passed into this function (the object's SD), and we ensured that at the beginning.
			pEffpermResultLists[0].fEvaluated = true;
			pEffpermResultLists[0].pGrantedAccessList = reply.GrantedAccessMaskValues;

			// We don't support object ACEs, so cObjectTypeListLength has to be 1 pObjectTypeList has to be the null Guid, and
			// pGrantedAccessList should be a single DWORD
			pEffpermResultLists[0].cObjectTypeListLength = 1;
			pEffpermResultLists[0].pObjectTypeList = [m_objectTypeList];

			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return ex.HResult;
		}
	}

	public HRESULT GetAccessRights(GuidPtr pguidObjectType, SI_OBJECT_INFO_Flags dwFlags, out SI_ACCESS[] ppAccess, ref uint pcAccesses, out uint piDefaultAccess)
	{
		if (pguidObjectType.IsNull || pguidObjectType.Value == Guid.Empty)
		{
			ppAccess = m_AccessTable;
			pcAccesses = m_AccessTableCount;

			// This is the index of the default access you want when you're adding a permission. It ends up indexing m_AccessTable, which is
			// really g_siForumsAccess, so 0 is Full Control.
			piDefaultAccess = m_DefaultAccess;
		}
		else
		{
			ppAccess = [];
			pcAccesses = piDefaultAccess = 0;
		}

		return HRESULT.S_OK;
	}

	public HRESULT GetFullResourceName(out string ppszResourceName)
	{
		ppszResourceName = m_resources[m_editingResource].Name;

		return HRESULT.S_OK;
	}

	public HRESULT GetInheritSource(SECURITY_INFORMATION si, PACL pACL, out INHERITED_FROM[] ppInheritArray) =>
		GetInheritSourceHelper(m_editingResource, si, pACL, out ppInheritArray);

	public HRESULT GetInheritTypes(out SI_INHERIT_TYPE[] ppInheritTypes, out uint pcInheritTypes)
	{
		if (m_resources[m_editingResource].IsContainer)
		{
			ppInheritTypes = siSDKInheritTypes;
			pcInheritTypes = (uint)siSDKInheritTypes.Length;
			return HRESULT.S_OK;
		}

		ppInheritTypes = [];
		pcInheritTypes = 0;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT GetObjectInformation(ref SI_OBJECT_INFO pObjectInfo)
	{
		// SI_OBJECT_INFO: http://msdn.microsoft.com/en-us/library/windows/desktop/aa379605(v=vs.85).aspx
		m_dwSIFlags = 0
			| SI_OBJECT_INFO_Flags.SI_ADVANCED // The "advanced" button is displayed on the basic
											   // security property page
			| SI_OBJECT_INFO_Flags.SI_EDIT_PERMS // The basic security property page always allows
												 // basic editing of the object's DACL
			| SI_OBJECT_INFO_Flags.SI_EDIT_OWNER // This lets you change the owner on the advanced page
			| SI_OBJECT_INFO_Flags.SI_PAGE_TITLE // Use pObjectInfo.pszPageTitle for the basic page's
												 // title
			| SI_OBJECT_INFO_Flags.SI_VIEW_ONLY // Displays a read-only version of the ACL Editor
												// dialog boxes. This is required if you're implementing ISecurityInformation3.
			| SI_OBJECT_INFO_Flags.SI_EDIT_EFFECTIVE
			| SI_OBJECT_INFO_Flags.SI_ENABLE_EDIT_ATTRIBUTE_CONDITION;

		var currentResource = m_resources[m_editingResource];

		if (currentResource.IsContainer)
		{
			// This will make ACL UI show the inheritance controls
			m_dwSIFlags |= SI_OBJECT_INFO_Flags.SI_CONTAINER;
		}

		pObjectInfo.dwFlags = m_dwSIFlags;
		pObjectInfo.hInstance = default;
		pObjectInfo.pszServerName = default;

		// ACL Editor won't free this, so we don't need to make a copy
		pObjectInfo.pszObjectName = currentResource.Name;
		pObjectInfo.pszPageTitle = "Forums Resource Manager";

		return HRESULT.S_OK;
	}

	public Resource GetResource(ResourceIndices index) => m_resources[index];

	public HRESULT GetSecurity(SECURITY_INFORMATION si, out PSECURITY_DESCRIPTOR ppSD, bool fDefault)
	{
		Resource currentResource = m_resources[m_editingResource];

		// This may be the default SD, or it may be the SD on the resource we're editing
		string sdToEdit = fDefault ? m_defaultSecurityDescriptorSddl : currentResource.SD;
		ppSD = PSECURITY_DESCRIPTOR.NULL;

		if (si.IsFlagSet(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) ||
			si.IsFlagSet(SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION) ||
			si.IsFlagSet(SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION)
		)
		{
			// The following function will populate the entire SD.
			try { ppSD = new SafePSECURITY_DESCRIPTOR(sdToEdit); }
			catch (Exception ex)
			{
				Console.Write("ConvertStringSecurityDescriptorToSecurityDescriptor: " + ex.ToString());
				return ex.HResult;
			}
		}
		return HRESULT.S_OK;
	}

	public HRESULT MapGeneric(GuidPtr guidObjectType, ref AceFlags pAceFlags, ref ACCESS_MASK pMask)
	{
		// This sample doesn't include object inheritance, so that bit can be safely removed.
		pAceFlags &= ~AceFlags.ObjectInherit;
		MapGenericMask(ref pMask, ObjectMap);

		return HRESULT.S_OK;
	}

	public HRESULT OpenElevatedEditor(HWND hWnd, SI_PAGE_TYPE uPage) => HRESULT.E_NOTIMPL;

	public HRESULT PropertySheetPageCallback(HWND hwnd, PropertySheetCallbackMessage uMsg, SI_PAGE_TYPE uPage) => HRESULT.E_NOTIMPL;

	public void SetCurrentObject(ResourceIndices index) => m_editingResource = index;

	public HRESULT SetSecurity(SECURITY_INFORMATION si, PSECURITY_DESCRIPTOR pSD)
	{
		//bool bResult;
		//uint errorCode;
		//HRESULT hr = HRESULT.S_OK;
		//PACL pDestDacl = default;
		//bool bDaclPresent;
		//bool bDaclDefaulted;
		//PACL pSourceDacl = default;
		//uint dwSizeNeeded;
		//SECURITY_DESCRIPTOR_CONTROL sdControl;
		//uint dwRevision;
		//int parentIndex;
		//PSECURITY_DESCRIPTOR pSDOfParent;
		//PSID group;
		//bool bGroupDefaulted;
		//PSID owner;
		//bool bOwnerDefaulted;
		//string stringSD;
		//uint stringSDLen;
		//SECURITY_DESCRIPTOR_CONTROL currentObjectSDControl;

		//PSECURITY_DESCRIPTOR absoluteCurrentSD;
		Resource currentResource = m_resources[m_editingResource];

		try
		{
			SafePSECURITY_DESCRIPTOR absoluteCurrentSD = new(currentResource.SD);

			if (si.IsFlagSet(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION))
			{
				Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(pSD, out var bDaclPresent, out var pSourceDacl, out _), "GetSecurityDescriptorDacl");

				Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(absoluteCurrentSD, out bDaclPresent, out var destDacl, out var bDaclDefaulted), "GetSecurityDescriptorDacl");

				SafePACL pDestDacl;
				if (destDacl.IsNull)
				{
					// Align sizeNeeded to a uint
					var dwSizeNeeded = (Marshal.SizeOf(typeof(ACL)) + (Marshal.SizeOf(typeof(uint)) - 1)) & 0xfffffffc;

					pDestDacl = new SafePACL((int)dwSizeNeeded);
				}
				else
					pDestDacl = new(destDacl, true);

				// Before doing anything else, we need to change the protected bit in case we end up reenabling inheritance.

				// If the 'P' flag was set, e.g. D:PAR(A;CIIO;FA;;;WD) then we need to remove all inherited entries
				Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorControl(pSD, out var sdControl, out var dwRevision), "GetSecurityDescriptorControl");

				Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorControl(absoluteCurrentSD, out var currentObjectSDControl, out dwRevision), "GetSecurityDescriptorControl");
				bool currentObjectWasProtected = currentObjectSDControl.IsFlagSet(SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED);

				// Now that we've gotten the SE_DACL_PROTECTED bit off of the current object, we can set it to what it needs to be. We needed
				// to capture it in the case that we're reenabling inheritance.
				Win32Error.ThrowLastErrorIfFalse(SetSecurityDescriptorControl(absoluteCurrentSD, SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED,
					sdControl.IsFlagSet(SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED) ? SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED : 0), "SetSecurityDescriptorControl");

				if (sdControl.IsFlagSet(SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED))
				{
					RemoveAllInheritedAces(pDestDacl).ThrowIfFailed("RemoveAllInheritedAces");
				}
				else
				{
					// The user reenabled inheritance (i.e. didn't pass in SE_DACL_PROTECTED, but the object used to have that flag).
					if (currentObjectWasProtected)
					{
						// This means we need to call SetSecurityOfChildren on the parent. This is why we disabled the SE_DACL_PROTECTED flag
						// already - otherwise the function would exit immediately.
						var parentIndex = currentResource.ParentIndex;
						if (parentIndex != ResourceIndices.NONEXISTENT_OBJECT)
						{
							Resource parentResource = m_resources[parentIndex];
							// Because we're keeping SDs as strings, we need to do a bit of hackery here to set up for SetSecurityOfChildren.

							// First, save the current security descriptor to where SetSecurityOfChildren can pick it up
							{
								currentResource.SD = ConvertSecurityDescriptorToStringSecurityDescriptor(absoluteCurrentSD,
									SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION |
									SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION |
									SECURITY_INFORMATION.DACL_SECURITY_INFORMATION |
									SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION |
									SECURITY_INFORMATION.ATTRIBUTE_SECURITY_INFORMATION |
									SECURITY_INFORMATION.SCOPE_SECURITY_INFORMATION);
							}

							SafePSECURITY_DESCRIPTOR pSDOfParent = new(parentResource.SD);

							SetSecurityOfChildren(parentIndex, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, pSDOfParent).ThrowIfFailed("SetSecurityOfChildren");

							// Now, the current resource's SD will be set to what we want... so we need to make sure we're working on that by
							// making absoluteCurrentSD into that.
							absoluteCurrentSD = new(currentResource.SD);

							// The DACL probably just changed
							Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(absoluteCurrentSD, out bDaclPresent, out destDacl, out bDaclDefaulted), "GetSecurityDescriptorDacl");
							pDestDacl = new(destDacl, true);
						}
					}
				}

				AddAllAcesFromAcl(pSourceDacl, pDestDacl, true).ThrowIfFailed("AddAllAcesFromAcl");

				// Finally, remove the explicit ACEs that don't appear in pSD. These are ACEs that used to be on the DACL, but the user just removed.
				RemoveExplicitUniqueAces(pSourceDacl, pDestDacl).ThrowIfFailed("RemoveExplicitUniqueAces");

				OrderDacl(m_editingResource, pDestDacl, out pDestDacl).ThrowIfFailed("OrderDacl");

				ConvertSecurityDescriptor(absoluteCurrentSD, out var ppAbsoluteSD).ThrowIfFailed();
				absoluteCurrentSD = ppAbsoluteSD;
				Win32Error.ThrowLastErrorIfFalse(SetSecurityDescriptorDacl(absoluteCurrentSD, true, pDestDacl, bDaclDefaulted), "SetSecurityDescriptorDacl");
			}
			if (si.IsFlagSet(SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION))
			{
				Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorGroup(pSD, out var group, out var bGroupDefaulted), "GetSecurityDescriptorGroup");

				Win32Error.ThrowLastErrorIfFalse(SetSecurityDescriptorGroup(absoluteCurrentSD, group, bGroupDefaulted), "SetSecurityDescriptorGroup");
			}
			if (si.IsFlagSet(SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION))
			{
				Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorOwner(pSD, out var owner, out var bOwnerDefaulted), "GetSecurityDescriptorOwner");

				Win32Error.ThrowLastErrorIfFalse(SetSecurityDescriptorOwner(absoluteCurrentSD, owner, bOwnerDefaulted), "SetSecurityDescriptorOwner");
			}

			// Finally, convert whatever changes we made to the absolute SD back to a string
			currentResource.SD = ConvertSecurityDescriptorToStringSecurityDescriptor(absoluteCurrentSD,
									SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION |
									SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION |
									SECURITY_INFORMATION.DACL_SECURITY_INFORMATION |
									SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION |
									SECURITY_INFORMATION.ATTRIBUTE_SECURITY_INFORMATION |
									SECURITY_INFORMATION.SCOPE_SECURITY_INFORMATION);

			if (currentResource.IsContainer)
			{
				SetSecurityOfChildren(m_editingResource, si, absoluteCurrentSD).ThrowIfFailed("SetSecurityOfChildren");
			}

			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return ex.HResult;
		}
	}

	// This function takes a string representing a security descriptor, converts it to a self-relative SD, then finally makes it absolute.
	private static HRESULT ConvertStringToAbsSD(string stringSD, out SafePSECURITY_DESCRIPTOR sd)
	{
		try
		{
			sd = new SafePSECURITY_DESCRIPTOR(stringSD);
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			sd = SafePSECURITY_DESCRIPTOR.Null;
			return ex.HResult;
		}
	}

	// Helper function for GetInheritSource so that we can call it on specific children and not just the object that we're currently editing.
	// childIndex represents the index of the child (see ResourceIndices)
	private HRESULT GetInheritSourceHelper(ResourceIndices childIndex, SECURITY_INFORMATION si, PACL acl, out INHERITED_FROM[] ppInheritArray)
	{
		/*HRESULT hr = HRESULT.S_OK;
		bool Win32Error.ThrowLastErrorIfFalse(false;
		uint errorCode = HRESULT.S_OK;
		ACL_SIZE_INFORMATION aclInformation;
		ref Resource parentResource = default;
		uint totalCount;
		IntPtr ace;
		byte aceFlags;
		bool alreadyExists = false;
		PSECURITY_DESCRIPTOR pParentSD;
		int grandparentIndex;
		PSECURITY_DESCRIPTOR pGrandparentSD;
		ref Resource grandparentResource;
		uint defaultSecurityDescriptorSize;
		bool bDaclPresent;
		bool bDaclDefaulted;
		PACL pGrandparentDacl = default;
		PACL pParentDacl = default;*/

		ppInheritArray = [];
		Resource childResource = m_resources[childIndex];
		var indexOfParent = childResource.ParentIndex;

		if (acl.IsNull)
		{
			return HRESULT.E_INVALIDARG;
		}

		// If there's no parent, just return HRESULT.E_NOTIMPL
		if (indexOfParent == ResourceIndices.NONEXISTENT_OBJECT)
		{
			return HRESULT.E_NOTIMPL;
		}

		try
		{
			ppInheritArray = new INHERITED_FROM[(int)acl.AceCount()];

			// Iterate over all of the ACEs.
			Win32Error.ThrowLastErrorIfFalse(GetAclInformation(acl, out ACL_SIZE_INFORMATION aclInformation), "GetAclInformation");

			var totalCount = aclInformation.AceCount;

			for (uint aceIndex = 0; aceIndex < totalCount; aceIndex++)
			{
				Win32Error.ThrowLastErrorIfFalse(GetAce(acl, aceIndex, out var ace), "GetAce");
				var aceFlags = ace.GetHeader().AceFlags;

				Resource? parentResource = null;
				if (indexOfParent != ResourceIndices.NONEXISTENT_OBJECT)
				{
					parentResource = m_resources[indexOfParent];
				}

				// If we're a SECTION, then it could only have come from the FORUM (assuming it wasn't orphaned). If we're a TOPIC, then it
				// could either be the parent SECTION or the grandparent FORUM.
				if (aceFlags.IsFlagSet(AceFlags.Inherited))
				{
					if (childResource.Type == ResourceType.SECTION)
					{
						// can't just assume that this ACE will be on the parent... need to check it; it may be an orphan
						using SafePSECURITY_DESCRIPTOR pParentSD = new(parentResource!.SD);

						bool alreadyExists = false;

						if (si.IsFlagSet(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION))
						{
							Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(pParentSD, out var bDaclPresent, out var pParentDacl, out var bDaclDefaulted), "GetSecurityDescriptorDacl");

							ACEAlreadyInACL(pParentDacl, ace, out alreadyExists, true).ThrowIfFailed("ACEAlreadyInACL");
						}

						if (alreadyExists)
						{
							ppInheritArray[aceIndex].GenerationGap = 1;
							ppInheritArray[aceIndex].AncestorName = parentResource.Name;
						}
						else
						{
							ppInheritArray[aceIndex].GenerationGap = 0;
							ppInheritArray[aceIndex].AncestorName = default;
						}
					}
					else
					{
						// We're a TOPIC. Check to see if this ACE is in the parent. If it isn't, then we check the grandparent. We can't
						// skip that check because it may be possible that this ACE was orphaned, so it may not exist on any ancestor
						var grandparentIndex = parentResource!.ParentIndex;
						SafePSECURITY_DESCRIPTOR pParentSD = new(parentResource.SD);

						bool alreadyExists = false;
						if (si.IsFlagSet(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION))
						{
							Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(pParentSD, out var bDaclPresent,
								out var pParentDacl, out var bDaclDefaulted), "GetSecurityDescriptorDacl");

							ACEAlreadyInACL(pParentDacl, ace, out alreadyExists, true).ThrowIfFailed("ACEAlreadyInACL");
						}

						if (alreadyExists)
						{
							ppInheritArray[aceIndex].GenerationGap = 1;
							ppInheritArray[aceIndex].AncestorName = parentResource.Name;
						}
						else
						{
							// The parent didn't have it, so check the grandparent.
							var grandparentResource = m_resources[grandparentIndex];
							SafePSECURITY_DESCRIPTOR pGrandparentSD = new(grandparentResource.SD);

							alreadyExists = false;
							if (si.IsFlagSet(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION))
							{
								Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(pGrandparentSD, out var bDaclPresent,
									out var pGrandparentDacl, out var bDaclDefaulted), "GetSecurityDescriptorDacl");

								ACEAlreadyInACL(pGrandparentDacl, ace, out alreadyExists, true);
							}

							if (alreadyExists)
							{
								// It came from the grandparent
								ppInheritArray[aceIndex].GenerationGap = 2;
								ppInheritArray[aceIndex].AncestorName = grandparentResource.Name;
							}
							else
							{
								// This ACE did not come from a [grand]parent
								ppInheritArray[aceIndex].GenerationGap = 0;
								ppInheritArray[aceIndex].AncestorName = default;
							}
						}
					}
				}
				else
				{
					// This ACE did not come from a [grand]parent
					ppInheritArray[aceIndex].GenerationGap = 0;
					ppInheritArray[aceIndex].AncestorName = default;
				}
			}
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return ex.HResult;
		}
	}

	// This orders a DACL canonically. For more information, see: http://msdn.microsoft.com/en-us/library/windows/desktop/aa379298(v=vs.85).aspx
	private HRESULT OrderDacl(ResourceIndices childIndex, in PACL ppAcl, out SafePACL pOrderedAcl)
	{
		pOrderedAcl = ppAcl;
		if (childIndex == 0)
		{
			// The base object (forums) can only have explicit ACEs since there's nothing above it (GetInheritSource would fail for the
			// forums anyway because we return not impl)
			return HRESULT.S_OK;
		}

		try
		{
			var resourceToOrder = m_resources[childIndex];

			// This call has to be made on the child, not the thing we're currently editing
			GetInheritSourceHelper(childIndex, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, ppAcl, out var pInheritArray).ThrowIfFailed("GetInheritSourceHelper");

			Win32Error.ThrowLastErrorIfFalse(GetAclInformation(ppAcl, out ACL_SIZE_INFORMATION aclInformation), "GetAclInformation");

			var totalCount = aclInformation.AceCount;
			var dwSizeNeeded = aclInformation.AclBytesFree + aclInformation.AclBytesInUse;
			pOrderedAcl = new((int)dwSizeNeeded);
			if (pOrderedAcl.IsInvalid)
			{
				Console.Write("LocalAlloc failed.\n");
				throw new OutOfMemoryException();
			}

			Win32Error.ThrowLastErrorIfFalse(InitializeAcl(pOrderedAcl, dwSizeNeeded, ACL_REVISION), "InitializeAcl");

			var parentIndex = resourceToOrder.ParentIndex;
			var grandparentIndex = parentIndex == ResourceIndices.NONEXISTENT_OBJECT ? ResourceIndices.NONEXISTENT_OBJECT : m_resources[parentIndex].ParentIndex;

			// Do two passes for each set of ACEs: explicit, parent, grandparent. One pass is for deny ACEs, one is for allow. This gives us
			// a total of 6 passes for TOPICs (because they have a grandparent), 4 passes for SECTIONs (because they only have a parent), and
			// 2 passes for FORUMs.
			var numPasses = 2;
			if ((int)parentIndex > -1) numPasses += 2;
			if ((int)grandparentIndex > -1) numPasses += 2;

			for (int pass = 0; pass < numPasses; pass++)
			{
				for (uint aceIndex = 0; aceIndex < totalCount; aceIndex++)
				{
					Win32Error.ThrowLastErrorIfFalse(GetAce(ppAcl, aceIndex, out var ace), "GetAce");

					AceType aceType = ace.GetAceType();
					if (
						// Pass 0: explicit deny ACEs
						pass == 0 && pInheritArray[aceIndex].GenerationGap == 0 && aceType == AceType.AccessDenied ||

						// Pass 1: explicit allow ACEs
						pass == 1 && pInheritArray[aceIndex].GenerationGap == 0 && IsAccessAllowedAce(aceType) ||

						// Pass 2: inherited-from-parent deny ACEs
						pass == 2 && pInheritArray[aceIndex].GenerationGap == 1 && aceType == AceType.AccessDenied ||

						// Pass 3: inherited-from-parent allow ACEs
						pass == 3 && pInheritArray[aceIndex].GenerationGap == 1 && IsAccessAllowedAce(aceType) ||

						// Pass 3: inherited-from-grandparent deny ACEs
						pass == 4 && pInheritArray[aceIndex].GenerationGap == 2 && aceType == AceType.AccessDenied ||

						// Pass 3: inherited-from-grandparent allow ACEs
						pass == 5 && pInheritArray[aceIndex].GenerationGap == 2 && IsAccessAllowedAce(aceType)
					)
					{
						// We COULD just use AddAce, because we're guaranteed not to overflow the ACL's size (since we allocated it to
						// exactly the same size and we're going to add all of the ACEs)
						AddAceToAcl(ace, pOrderedAcl, true).ThrowIfFailed("AddAceToAcl");
					}
				}
			}

			Win32Error.ThrowLastErrorIfFalse(GetAclInformation(ppAcl, out ACL_SIZE_INFORMATION aclInformation2), "GetAclInformation");

			var totalCount2 = aclInformation2.AceCount;

			// Sanity check: ensure that we didn't leave out any ACEs
			if (totalCount != totalCount2)
			{
				Console.Write("A different amount of ACEs exists in the ACL now. Before: {0} after: {1}\n", totalCount, totalCount2);
				throw Marshal.GetExceptionForHR(HRESULT.E_FAIL)!;
			}

			// Sanity check: ensure that the ACLs are the same size
			if (aclInformation.AclBytesFree != aclInformation2.AclBytesFree || aclInformation.AclBytesInUse != aclInformation2.AclBytesInUse)
			{
				Console.Write("Either AclBytesFree or AclBytesInUse doesn't match up\n");
				throw Marshal.GetExceptionForHR(HRESULT.E_FAIL)!;
			}

			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return ex.HResult;
		}
	}

	// This function iterates over a container's children and sets their security. parentIndex represents the index of the parent (see
	// ResourceIndices) si can include either DACL_SECURITY_INFORMATION, SACL_SECURITY_INFORMATION, or both pSD is the security descriptor of
	// the parent
	private HRESULT SetSecurityOfChildren(ResourceIndices parentIndex, SECURITY_INFORMATION si, PSECURITY_DESCRIPTOR pSD)
	{
		HRESULT hr = HRESULT.S_OK;
		Resource parentResource = m_resources[parentIndex];
		List<ResourceIndices> childIndices = parentResource.ChildIndices;

		try
		{
			foreach (var childIndex in childIndices)
			{
				var childResource = m_resources[childIndex];
				SafePSECURITY_DESCRIPTOR childAbsoluteSD = new SafePSECURITY_DESCRIPTOR(childResource.SD).MakePackedAbsolute();

				if (si.IsFlagSet(SECURITY_INFORMATION.DACL_SECURITY_INFORMATION))
				{
					// First of all, if the child has the protected bit set, then they aren't interested in inheriting anything.
					Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorControl(childAbsoluteSD, out var sdControl, out var dwRevision), "GetSecurityDescriptorControl");

					// No need to set the security on the children if it's a protected DACL
					if (sdControl.IsFlagSet(SECURITY_DESCRIPTOR_CONTROL.SE_DACL_PROTECTED))
					{
						return HRESULT.S_OK;
					}

					// Get the DACL of the parent
					Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(pSD, out var bAclPresent, out var acl, out var bAclDefaulted), "GetSecurityDescriptorDacl");

					// If there was no supplied ACL, then there's nothing for the child to inherit
					if (!bAclPresent)
					{
						return HRESULT.S_OK;
					}

					// Now we know there's a DACL, but we don't know if there are any inheritable ACEs.
					GetSizeOfAllInheritableAces(acl, out var dwSizeNeeded).ThrowIfFailed("GetSizeOfAllInheritableAces");

					// At this point, we know that the parent has a DACL. We need to get the child's DACL too because we may need to delete
					// or add entries
					Win32Error.ThrowLastErrorIfFalse(GetSecurityDescriptorDacl(childAbsoluteSD, out bAclPresent, out var childAcl, out bAclDefaulted), "GetSecurityDescriptorDacl");
					SafePACL pChildAcl = childAcl.IsNull ? SafePACL.Null : childAcl;

					if (!pChildAcl.IsInvalid)
					{
						// Keep only the explicit ACEs. This way we don't need to do a differential thing and find out which inherited ACEs
						// went away or which ones were added. We just wipe them all out and then add all the inheritable ACEs from the parent.
						RemoveAllInheritedAces(pChildAcl).ThrowIfFailed("RemoveAllInheritedAces");
					}

					// There are no inheritable ACEs, so we're done here.
					if (dwSizeNeeded == 0)
					{
						return HRESULT.S_OK;
					}

					// At this point, we know that there is a parent DACL and that it contains inheritable entries. If the child's ACL is
					// null, then we need to initialize it.
					if (pChildAcl.IsInvalid)
					{
						// We know how much space we need, so we can allocate it. First though, align it to a uint (this is necessary)
						dwSizeNeeded = (dwSizeNeeded + sizeof(uint) - 1) & 0xfffffffc;

						pChildAcl = new SafePACL((int)dwSizeNeeded);
						if (pChildAcl.IsInvalid)
						{
							Console.Write("LocalAlloc failed.\n");
							throw new OutOfMemoryException();
						}

						//Win32Error.ThrowLastErrorIfFalse(InitializeAcl(pChildAcl, dwSizeNeeded, ACL_REVISION), "InitializeAcl");
					}

					AddInheritableAcesFromAcl(acl, pChildAcl).ThrowIfFailed("AddInheritableAcesFromAcl");

					// Now, the ACL is fully formed, so set it on the child
					OrderDacl(childIndex, pChildAcl, out pChildAcl).ThrowIfFailed("OrderDacl");

					Win32Error.ThrowLastErrorIfFalse(SetSecurityDescriptorDacl(childAbsoluteSD, true, pChildAcl, false), "SetSecurityDescriptorDacl");

					Win32Error.ThrowLastErrorIfFalse(SetSecurityDescriptorControl(childAbsoluteSD, SECURITY_DESCRIPTOR_CONTROL.SE_DACL_AUTO_INHERITED, SECURITY_DESCRIPTOR_CONTROL.SE_DACL_AUTO_INHERITED), "SetSecurityDescriptorControl");
				}

				// Finally, convert the SD back to a string
				childResource.SD = ConvertSecurityDescriptorToStringSecurityDescriptor(childAbsoluteSD, SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION |
					SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION | SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION |
					SECURITY_INFORMATION.ATTRIBUTE_SECURITY_INFORMATION | SECURITY_INFORMATION.SCOPE_SECURITY_INFORMATION);

				// Now, call SetSecurityOfChildren on the child
				SetSecurityOfChildren(childIndex, si, childAbsoluteSD).ThrowIfFailed("SetSecurityOfChildren");
			}
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			hr = ex.HResult;
		}
		return hr;
	}
}