using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.NetApi32;
using static Vanara.PInvoke.NTDSApi;

namespace hello
{
	internal static class Spn
	{
		// Creates a spn if the user hasn't specified one.
		public static string Make()
		{
			uint ulSpn = 1;
			var lpCompDN = new StringBuilder(128);
			var ulCompDNSize = (uint)lpCompDN.Capacity;
			var NoFailure = true;
			SafeDsHandle hDS = SafeDsHandle.Null;

			Win32Error status = DsGetSpn(DS_SPN_NAME_TYPE.DS_SPN_NB_HOST, "hello",
				default, // DN of this service.
				0, // Use the default instance port.
				0, // Number of additional instance names.
				default, // No additional instance names.
				default, // No additional instance ports.
				ref ulSpn, // Size of SPN array.
				out SpnArrayHandle arrSpn); // Returned SPN(s).

			Console.Write("DsGetSpn returned {0}\n", status);
			if (status != Win32Error.ERROR_SUCCESS)
			{
				Environment.Exit((int)(uint)status);
			}

			// Get the name of domain if it is domain-joined
			if ((status = DsGetDcName(default, default, IntPtr.Zero, default, DsGetDcNameFlags.DS_RETURN_DNS_NAME, out SafeNetApiBuffer pDomainControllerInfo)).Failed)
			{
				Console.Write("DsGetDcName returned {0}\n", status);
				NoFailure = false;
			}

			// if it is domain joined
			if (NoFailure)
			{
				// Bind to the domain controller for our domain
				if ((status = DsBind(default, pDomainControllerInfo.ToStructure<DOMAIN_CONTROLLER_INFO>().DomainName, out hDS)).Failed)
				{
					Console.Write("DsBind returned {0}\n", status);
					NoFailure = false;
				}
			}

			if (NoFailure)
			{
				pDomainControllerInfo.Dispose();

				if (!Secur32.GetComputerObjectName(Secur32.EXTENDED_NAME_FORMAT.NameFullyQualifiedDN, lpCompDN, ref ulCompDNSize))
				{
					Console.Write("GetComputerObjectName returned {0}\n", status = Win32Error.GetLastError());
					Environment.Exit((int)(uint)status);
				}

				/* We could check whether the SPN is already registered for this
				computer's DN, but we don't have to. Modification is performed
				permissiely by this function, so that adding a value that already
				exists does not return an error. This way we can opt for the internal
				check instead of doing it ourselves. */

				status = DsWriteAccountSpn(hDS, DS_SPN_WRITE_OP.DS_SPN_ADD_SPN_OP, lpCompDN.ToString(), ulSpn, arrSpn);
				if (status.Failed)
				{
					Console.Write("DsWriteAccountSpn returned {0}\n", status);
					Environment.Exit((int)(uint)status);
				}
				hDS.Dispose();
			}

			return arrSpn.GetSPNs(ulSpn).FirstOrDefault();
		}
	}
}