using System;
using System.Diagnostics;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace TextSid
{
	internal static class TextSid
	{
		private static int Main(string[] args)
		{
			try
			{
				//
				// obtain current process token
				//
				using var hToken = SafeHTOKEN.FromProcess(Process.GetCurrentProcess(), TokenAccess.TOKEN_QUERY);

				// obtain user identified by current process' access token
				//
				// Vanara Note: since the SID value of SID_AND_ATTRIBUTES is allocated in memory, you need to get the memory allocation and then
				// pull the SID value before destroying the memory
				//
				using var ptgUser = hToken.GetInfo(TOKEN_INFORMATION_CLASS.TokenUser);

				//
				// obtain the textual representaion of the Sid
				//
				var szTextualSid = GetTextualSid(ptgUser.ToStructure<TOKEN_USER>().User.Sid);

				// display the TextualSid representation
				Console.WriteLine($"Process Sid: {szTextualSid}");
				return 0;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return 1;
			}
		}

		private static string GetTextualSid(PSID pSid)
		{
			const int SID_REVISION = 1;

			//
			// test if parameters passed in are valid, IsValidSid can not take
			// a NULL parameter
			//
			if (pSid.IsNull || !pSid.IsValidSid())
				throw new ArgumentException(nameof(pSid));

			// obtain SidIdentifierAuthority
			var psia = GetSidIdentifierAuthority(pSid);

			// obtain sidsubauthority count
			var dwSubAuthorities = GetSidSubAuthorityCount(pSid);

			//
			// compute approximate buffer length
			// S-SID_REVISION- + identifierauthority + -subauthorities + NULL
			//
			var cchMaxLen = 6 + 14 + 11 * dwSubAuthorities + 1;
			var TextualSid = new StringBuilder(cchMaxLen);

			//
			// prepare S-SID_REVISION-
			//
			TextualSid.AppendFormat("S-{0}-", SID_REVISION);

			//
			// prepare SidIdentifierAuthority
			//
			if (psia.Value[0] != 0 || psia.Value[1] != 0)
			{
				TextualSid.AppendFormat("0x{0:X2}{1:X2}{2:X2}{3:X2}{4:X2}{5:X2}", psia.Value[0], psia.Value[1], psia.Value[2], psia.Value[3], psia.Value[4], psia.Value[5]);
			}
			else
			{
				TextualSid.Append(psia.Value[5] + (uint)(psia.Value[4] << 8) + (uint)(psia.Value[3] << 16) + (uint)(psia.Value[2] << 24));
			}

			//
			// loop through SidSubAuthorities
			//
			for (var dwCounter = 0U; dwCounter < dwSubAuthorities; dwCounter++)
			{
				TextualSid.AppendFormat("-{0}", GetSidSubAuthority(pSid, dwCounter));
			}

			return TextualSid.ToString();
		}
	}
}