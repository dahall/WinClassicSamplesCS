using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace AclApi
{
	internal static class AclApi
	{
		private static int Main(string[] args)
		{
			if (args.Length < 3)
			{
				Console.Error.Write($"Usage: {Environment.CommandLine} <filename> {{/Deny | /Grant | /Revoke | /Set}} [<trustee>] [<permissions>] [<InheritFlag>]\n");
				return 1;
			}

			var FileName = args[0];
			var TrusteeName = args[2];

			ACCESS_MODE option;
			if (0 == string.Compare(args[1], "/Deny", true) || 0 == string.Compare(args[1], "/D"))
			{
				option = ACCESS_MODE.DENY_ACCESS;
			}
			else if (0 == string.Compare(args[1], "/Revoke", true) || 0 == string.Compare(args[1], "/R", true))
			{
				option = ACCESS_MODE.REVOKE_ACCESS;
			}
			else if (0 == string.Compare(args[1], "/Set", true) || 0 == string.Compare(args[1], "/S", true))
			{
				option = ACCESS_MODE.SET_ACCESS;
			}
			else if (0 == string.Compare(args[1], "/Grant", true) || 0 == string.Compare(args[1], "/G", true))
			{
				option = ACCESS_MODE.GRANT_ACCESS;
			}
			else
			{
				Console.Error.Write("Invalid action specified\n");
				return 13;
			}

			var AccessMask = args.Length > 3 ? uint.Parse(args[3]) : ACCESS_MASK.GENERIC_ALL;

			var InheritFlag = args.Length > 4 ? Enum.Parse<INHERIT_FLAGS>(args[4]) : INHERIT_FLAGS.NO_INHERITANCE;

			// get current Dacl on specified file
			GetNamedSecurityInfo(FileName, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, out _, out _, out var ExistingDacl, out _, out var psd).ThrowIfFailed();

			BuildExplicitAccessWithName(out var explicitaccess, TrusteeName, AccessMask, option, InheritFlag);

			// add specified access to the object
			SetEntriesInAcl(1, new[] { explicitaccess }, ExistingDacl, out var NewAcl).ThrowIfFailed();

			// apply new security to file
			SetNamedSecurityInfo(FileName, SE_OBJECT_TYPE.SE_FILE_OBJECT, SECURITY_INFORMATION.DACL_SECURITY_INFORMATION, default, default, NewAcl, default).ThrowIfFailed();

			return 0;
		}
	}
}