using System.Security.AccessControl;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace CustomResourceManager;

internal static class Utility
{
	// Go through all of the ACEs in the ACL and see if 'ace' is equal to any of them.
	public static HRESULT ACEAlreadyInACL(in PACL acl, in PACE ace, out bool lpbAcePresent, bool forInheritancePurposes)
	{
		lpbAcePresent = false;
		if (acl.IsNull || ace.IsNull)
		{
			return HRESULT.E_INVALIDARG;
		}

		var aceType = ace.GetHeader().AceType;
		var aceSize = ace.GetHeader().AceSize;
		var aceFlags = ace.GetHeader().AceFlags;

		if (forInheritancePurposes)
		{
			if (aceFlags.IsFlagSet(AceFlags.Inherited))
			{
				// It was inherited. Don't use that flag for comparison though because it won't appear on the source.
				aceFlags &= ~AceFlags.Inherited;
			}
			else
			{
				// If the ACE wasn't inherited, then we don't care about it
				lpbAcePresent = false;
				return HRESULT.S_OK;
			}
		}

		try
		{
			var totalCount = acl.AceCount();
			for (uint i = 0; i < totalCount; i++)
			{
				Win32Error.ThrowLastErrorIfFalse(GetAce(acl, i, out var pExistingAce), "GetAce");

				var existingAceType = pExistingAce.GetHeader().AceType;
				var existingAceSize = pExistingAce.GetHeader().AceSize;
				var existingAceFlags = pExistingAce.GetHeader().AceFlags;

				if (forInheritancePurposes)
				{
					if (!IsInheritableAce(existingAceFlags))
					{
						// We only care about inheritable ACEs
						continue;
					}
					else
					{
						// Wipe out the inheritance flags that couldn't possibly be on the child. (e.g. when we have a
						// NO_PROPAGATE_INHERIT_ACE, the ACE will make it to the child, but the flag will get unset in AddInheritableAcesFromAcl).
						existingAceFlags &= ~AceFlags.InheritOnly;

						if (existingAceFlags.IsFlagSet(AceFlags.NoPropagateInherit))
						{
							existingAceFlags &= ~AceFlags.NoPropagateInherit;

							// The child doesn't get the [...]_INHERIT_ACE flag if NO_PROPAGATE was specified.
							existingAceFlags &= ~AceFlags.ContainerInherit;
							existingAceFlags &= ~AceFlags.ObjectInherit;
						}
					}
				}

				if (existingAceFlags == aceFlags && existingAceSize == aceSize && existingAceType == aceType)
				{
					// That was our quick check, now we should compare the actual contents (mask and SID)
					var sidStart1 = GetSidStart(aceType, pExistingAce);
					var sidStart2 = GetSidStart(aceType, ace);
					var sidLength1 = GetLengthSid((PSID)sidStart1);
					var sidLength2 = GetLengthSid((PSID)sidStart2);

					// This is needed even though we just compared the sizes because of callback aces - the size of the ACLs may
					// coincidentally be the same because the sums of the sid length and condition sizes are equal
					if (sidLength1 != sidLength2)
					{
						continue;
					}

					var result = memcmp(sidStart1, sidStart2, aceSize - Marshal.SizeOf(typeof(ACE_HEADER)) - Marshal.SizeOf(typeof(ACCESS_MASK)));
					if (result == 0)
					{
						lpbAcePresent = true;
						return HRESULT.S_OK;
					}
				}
			}

			lpbAcePresent = false;
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return ex.HResult;
		}

		static int memcmp(in IntPtr p1, in IntPtr p2, SizeT len)
		{
			unsafe
			{
				byte* ptr1 = (byte*)p1, ptr2 = (byte*)p2;
				int ret = 0, idx = 0;
				while (ret == 0 && idx++ < len)
				{
					ret = *ptr1++ - *ptr2++;
				}
			}
			return 0;
		}
	}

	// Add an ACE to an ACL, expanding the ACL if needed.
	public static HRESULT AddAceToAcl(PACE pNewAce, SafePACL ppAcl, bool bAddToEndOfList)
	{
		if (pNewAce.IsNull || ppAcl.IsInvalid)
		{
			return HRESULT.E_INVALIDARG;
		}

		try
		{
			ppAcl.Insert(bAddToEndOfList ? int.MaxValue : 0, pNewAce);
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			Console.Write(ex.ToString());
			return ex.HResult;
		}
	}

	// Adds all ACEs from sourceAcl to ppDestAcl. If onlyAddUnique is specified, then this function will ensure that ACEs from sourceAcl
	// aren't added if they already appear in ppDestAcl.
	public static HRESULT AddAllAcesFromAcl(PACL sourceAcl, SafePACL ppDestAcl, bool onlyAddUnique)
	{
		bool addIt = true;
		bool alreadyExists = false;

		if (sourceAcl.IsNull || ppDestAcl.IsInvalid)
		{
			return HRESULT.E_INVALIDARG;
		}

		try
		{
			for (uint i = 0; i < sourceAcl.AceCount(); i++)
			{
				Win32Error.ThrowLastErrorIfFalse(GetAce(sourceAcl, i, out var ace), "GetAce");

				// If we only want to add unique ACEs, then we need to make sure that this doesn't already exist in the destination.
				addIt = true;
				if (onlyAddUnique)
				{
					ACEAlreadyInACL(ppDestAcl, ace, out alreadyExists, false).ThrowIfFailed("ACEAlreadyInACL");
					if (alreadyExists)
					{
						addIt = false;
					}
				}

				if (addIt)
				{
					AddAceToAcl(ace, ppDestAcl, false).ThrowIfFailed("AddAceToAcl");
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

	// Adds any inheritable ACEs from sourceAcl to ppDestAcl.
	public static HRESULT AddInheritableAcesFromAcl(PACL sourceAcl, SafePACL ppDestAcl)
	{
		try
		{
			GetSizeOfAllInheritableAces(sourceAcl, out var sz).ThrowIfFailed();

			ppDestAcl.Size = ppDestAcl.Length + sz;
			foreach (var ace in sourceAcl.EnumerateAces().Where(a => IsInheritableAce(a.GetHeader().AceFlags)))
			{
				AddAceToAcl(ace, ppDestAcl, true);
			}
			return HRESULT.S_OK;
		}
		catch (Exception ex) { return ex.HResult; }
	}

	public static HRESULT ConvertSecurityDescriptor(PSECURITY_DESCRIPTOR pSelfRelSD, out SafePSECURITY_DESCRIPTOR ppAbsoluteSD)
	{
		ppAbsoluteSD = SafePSECURITY_DESCRIPTOR.Null;
		if (!pSelfRelSD.IsSelfRelative())
			return HRESULT.E_INVALIDARG;

		try { ppAbsoluteSD = new SafePSECURITY_DESCRIPTOR(pSelfRelSD, false).MakeAbsolute().pAbsoluteSecurityDescriptor; return HRESULT.S_OK; }
		catch (Exception ex) { return ex.HResult; }
	}

	public static IntPtr GetSidStart(AceType aceType, PACE ace)
	{
		IntPtr offset;
		switch (aceType)
		{
			case AceType.AccessAllowed:
			case AceType.AccessDenied:
			case AceType.SystemAudit:
				offset = Marshal.OffsetOf<ACCESS_ALLOWED_ACE>(nameof(ACCESS_ALLOWED_ACE.SidStart));
				break;

			case AceType.SystemAuditCallback:
				offset = Marshal.OffsetOf<SYSTEM_AUDIT_CALLBACK_ACE>(nameof(SYSTEM_AUDIT_CALLBACK_ACE.SidStart));
				break;

			case AceType.AccessAllowedCallback:
				offset = Marshal.OffsetOf<ACCESS_ALLOWED_CALLBACK_ACE>(nameof(ACCESS_ALLOWED_CALLBACK_ACE.SidStart));
				break;

			default:
				return default;
		}

		// We use ref byte so that we can add an offset to the pointer
		return ((IntPtr)ace).Offset(offset.ToInt64());
	}

	// This goes through every ACE in 'acl', sums the size of the inheritable ACEs, and puts it in dwSizeNeeded.
	public static HRESULT GetSizeOfAllInheritableAces(PACL acl, out uint dwSizeNeeded)
	{
		dwSizeNeeded = 0;
		if (acl.IsNull)
			return HRESULT.E_INVALIDARG;

		try { dwSizeNeeded = (uint)acl.EnumerateAces().Where(a => IsInheritableAce(a.GetHeader().AceFlags)).Sum(a => Macros.ALIGN_TO_MULTIPLE(a.Length(), 4)); return HRESULT.S_OK; }
		catch (Exception ex) { return ex.HResult; }
	}

	public static bool IsAccessAllowedAce(AceType aceType) => aceType is AceType.AccessAllowedCallback or AceType.AccessAllowed;

	public static bool IsInheritableAce(AceFlags aceFlags) => aceFlags.IsFlagSet(AceFlags.ObjectInherit) || aceFlags.IsFlagSet(AceFlags.ContainerInherit);

	public static HRESULT RemoveAllInheritedAces(PACL ppAcl)
	{
		try
		{
			var totalCount = ppAcl.AceCount();
			for (uint aceIndex = 0; aceIndex < totalCount; aceIndex++)
			{
				Win32Error.ThrowLastErrorIfFalse(GetAce(ppAcl, aceIndex, out var ace), "GetAce");

				var aceFlags = ace.GetHeader().AceFlags;
				if (aceFlags.IsFlagSet(AceFlags.Inherited))
				{
					Win32Error.ThrowLastErrorIfFalse(DeleteAce(ppAcl, aceIndex), "DeleteAce");

					totalCount--;
				}
				else
				{
					aceIndex++;
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

	// This function will remove the noninherited ACEs from ppDestAcl that don't show up in 'acl'. This is needed for SetSecurity - inherited
	// ACEs need to stay, and any ACEs that are being set need to stay, but all others need to go.
	public static HRESULT RemoveExplicitUniqueAces(PACL acl, PACL ppDestAcl)
	{
		if (acl.IsNull || ppDestAcl.IsNull)
		{
			return HRESULT.E_INVALIDARG;
		}

		try
		{
			var totalCount = acl.AceCount();
			uint aceIndex = 0;
			while (aceIndex < totalCount)
			{
				Win32Error.ThrowLastErrorIfFalse(GetAce(ppDestAcl, aceIndex, out var ace), "GetAce");

				var aceFlags = ace.GetHeader().AceFlags;

				// We only care about explcit (i.e. non-inherited) ACEs
				if (aceFlags.IsFlagSet(AceFlags.Inherited))
				{
					aceIndex++;
					continue;
				}

				ACEAlreadyInACL(acl, ace, out var alreadyExists, false).ThrowIfFailed("ACEAlreadyInACL");

				// If it's not in 'acl', then it's unique
				if (!alreadyExists)
				{
					Win32Error.ThrowLastErrorIfFalse(DeleteAce(ppDestAcl, aceIndex), "DeleteAce");
					totalCount--;
				}
				else
				{
					aceIndex++;
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

	private static HRESULT UShortAdd(ushort a, ushort b, out ushort c)
	{
		uint r = (uint)a + b;
		if (r > ushort.MaxValue)
		{
			c = default;
			return HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_ARITHMETIC_OVERFLOW);
		}
		c = (ushort)r;
		return HRESULT.S_OK;
	}

	private static HRESULT UShortMult(ushort a, ushort b, out ushort c)
	{
		ulong r = (ulong)a * b;
		if (r > ushort.MaxValue)
		{
			c = default;
			return HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_ARITHMETIC_OVERFLOW);
		}
		c = (ushort)r;
		return HRESULT.S_OK;
	}
}