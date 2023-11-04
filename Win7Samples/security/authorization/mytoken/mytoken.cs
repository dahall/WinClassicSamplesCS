using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;

namespace MyToken
{
	internal static class MyToken
	{
		private static int Main(string[] args)
		{
			// Use OpenThreadToken() API first, to determine
			// if the calling thread is running under impersonation
			var bResult = OpenThreadToken(GetCurrentThread(), TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_QUERY_SOURCE, true, out var hToken);
			if (bResult == false && GetLastError() == Win32Error.ERROR_NO_TOKEN)
			{
				// Otherwise, use process access token
				bResult = OpenProcessToken(GetCurrentProcess(), TokenAccess.TOKEN_QUERY | TokenAccess.TOKEN_QUERY_SOURCE, out hToken);
			}
			if (bResult)
			{
				DisplayTokenInformation(hToken);
				hToken.Dispose();
			}
			else
				Console.Write("OpenThread/ProcessToken failed with {0}\n", GetLastError());
			return 0;
		}

		static string ConvertBinarySidToName(PSID pSid)
		{
			// Vanara Note: This entire function can be replaced with pSid.ToString("N");

			StringBuilder pAccountName = null;
			StringBuilder pDomainName = null;
			int dwAccountNameSize = 0;
			int dwDomainNameSize = 0;

			LookupAccountSid(
				  null,                      // lookup on local system
				  pSid,
				  pAccountName,              // buffer to recieve name
				  ref dwAccountNameSize,
				  pDomainName,
				  ref dwDomainNameSize,
				  out _);
			// If the SID cannot be resolved, LookupAccountSid will fail with
			// ERROR_NONE_MAPPED
			if (GetLastError() == Win32Error.ERROR_NONE_MAPPED)
			{
				pAccountName = new StringBuilder("NONE_MAPPED");
				pDomainName = null;
			}
			else if (GetLastError() == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				pAccountName = new StringBuilder(dwAccountNameSize);
				pDomainName = new StringBuilder(dwDomainNameSize);
				if (!LookupAccountSid(
					  null,                      // lookup on local system
					  pSid,
					  pAccountName,              // buffer to recieve name
					  ref dwAccountNameSize,
					  pDomainName,
					  ref dwDomainNameSize,
					  out _))
				{
					Console.Write("LookupAccountSid failed with {0}\n", Win32Error.GetLastError());
					return null;
				}
			}
			// Any other error code
			else
			{
				Console.Write("LookupAccountSid failed with {0}\n", Win32Error.GetLastError());
				return null;
			}

			return pDomainName?.Length == 0 ? pAccountName.ToString() : $"{pDomainName.ToString()}\\{pAccountName.ToString()}";
		}

		static string ConvertLUIDToName(in LUID pLuid)
		{
			// Vanara Note: This entire function can be replaced with pLuid.ToString();

			StringBuilder pPrivilegeName = null;
			uint dwSize = 0;

			LookupPrivilegeName(
				  null,                      // lookup on local system
				  pLuid,
				  pPrivilegeName,            // buffer to recieve name
				  ref dwSize);
			// Check for ERROR_INSUFFICIENT_BUFFER error code 
			if (GetLastError() != Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				Console.Write("LookupPrivilegeName failed with {0}\n", Win32Error.GetLastError());
				return null;
			}

			pPrivilegeName = new StringBuilder((int)dwSize);

			if (!LookupPrivilegeName(
				  null,                      // lookup on local system
				  pLuid,
				  pPrivilegeName,            // buffer to recieve name
				  ref dwSize))
			{
				Console.Write("LookupPrivilegeName failed with {0}\n", Win32Error.GetLastError());
				return null;
			}

			return pPrivilegeName.ToString();
		}

		static void DisplayUserInfo(SafeHTOKEN hToken)
		{
			//
			// Get User Information
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenUser);
			var pUserInfo = mem.ToStructure<TOKEN_USER>();
			var pName = ConvertBinarySidToName(pUserInfo.User.Sid);
			if (pName != null)
				Console.Write("User : {0}\n", pName);
		}

		static void DisplayOwnerInfo(SafeHTOKEN hToken)
		{
			//
			// Get Owner Information
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenUser);
			var pOwnerInfo = mem.ToStructure<TOKEN_OWNER>();
			var pName = ConvertBinarySidToName(pOwnerInfo.Owner);
			if (pName != null)
				Console.Write("Owner : {0}\n", pName);
		}

		static void DisplayPrimaryGroupInfo(SafeHTOKEN hToken)
		{
			//
			// Get Primary Group Information
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenPrimaryGroup);
			var pPrimaryGroupInfo = mem.ToStructure<TOKEN_PRIMARY_GROUP>();
			var pName = ConvertBinarySidToName(pPrimaryGroupInfo.PrimaryGroup);
			if (pName != null)
				Console.Write("Primary Group : {0}\n", pName);
		}

		static void DisplayStatistics(SafeHTOKEN hToken)
		{
			//
			// Get Token Statistics Information
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenStatistics);
			var pStatistics = mem.ToStructure<TOKEN_STATISTICS>();

			//
			// Display some of the token statistics information
			//

			Console.Write("LUID for this instance of token {0}\n", pStatistics.TokenId);
			Console.Write("LUID for this logon session     {0}\n", pStatistics.AuthenticationId);

			if (pStatistics.TokenType == TOKEN_TYPE.TokenPrimary)
				Console.Write("Token type is PRIMARY\n");
			else
				Console.Write("Token type is IMPERSONATION\n");
		}

		static void DisplaySource(SafeHTOKEN hToken)
		{
			//
			// Display source of access token
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenSource);
			var pSource = mem.ToStructure<TOKEN_SOURCE>();

			Console.WriteLine($"Token source is <{string.Join(",", pSource.SourceName)}>");
		}

		static void DisplayGroupsInfo(SafeHTOKEN hToken)
		{
			//
			//  List all groups in the access token
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenGroups);
			var pGroupInfo = mem.ToStructure<TOKEN_GROUPS>();

			Console.Write("\nRetrieving Group information from the access token\n");
			for (var i = 0; i < pGroupInfo.GroupCount; i++)
			{
				var pName = ConvertBinarySidToName(pGroupInfo.Groups[i].Sid);
				if (pName != null)
				{
					Console.Write("SID {0} Group: {1}\n", i, pName);
				}
			}
		}

		static void DisplayPrivileges(SafeHTOKEN hToken)
		{
			//
			// Display privileges associated with this access token
			//

			using var mem = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenPrivileges);
			var pPrivileges = mem.ToStructure<TOKEN_PRIVILEGES>();

			Console.Write("\nPrivileges associated with this token ({0})\n", pPrivileges.PrivilegeCount);
			for (var i = 0; i < pPrivileges.PrivilegeCount; i++)
			{
				var pPrivilegeName = ConvertLUIDToName(pPrivileges.Privileges[i].Luid);
				if (pPrivilegeName != null)
				{
					Console.Write("{0} - (attributes) {1}\n", pPrivilegeName, pPrivileges.Privileges[i].Attributes);
				}
			}
		}

		static void DisplayTokenInformation(SafeHTOKEN hToken)
		{
			//
			// Display access token information
			//

			try
			{
				DisplayUserInfo(hToken);
				DisplayOwnerInfo(hToken);
				DisplayPrimaryGroupInfo(hToken);
				DisplayStatistics(hToken);
				DisplaySource(hToken);
				DisplayGroupsInfo(hToken);
				DisplayPrivileges(hToken);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}