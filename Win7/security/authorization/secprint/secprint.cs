using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.WinSpool;

namespace AclApi
{
	internal static class AclApi
	{
		private const byte ACCESS_ALLOW = 2;
		private const byte ACCESS_DENY = 1;

		private static int Main(string[] args)
		{
			bool bDeny = false;

			if (args.Length < 3)
			{
				Console.Write("USAGE: secprint <printer name> <user name> <P|MD|MP|F> [D]\n");
				Console.Write(" Where P  = Print access\n");
				Console.Write("       MD = Manage documents\n");
				Console.Write("       MP = Manage printers\n");
				Console.Write("       F  = Full control <P + MD + MP>\n");
				Console.Write("\nUse <D> to deny specified access to the specified\n");
				Console.Write("group or user (otherwise specified access is granted.)\n");
				Console.Write("\nNote: On NT4 or earlier denied 'Full Control' is displayed\n");
				Console.Write("as 'No Access' by the Explorer Printer Permissions dialog.\n");
				Console.Write("Denying P, MD, or MP on NT4 or earlier will not be viewable\n");
				Console.Write("using this dialog eventhough the DACL is valid.\n");

				return 0;
			}

			if (args.Length >= 4 && args[3].ToLower() == "d")
				bDeny = true;

			switch (args[2].ToLower())
			{
				case "p":
					// Add an ACL for "Print" access
					if (!AddAccessRights(args[0], (uint)AccessRights.PRINTER_EXECUTE, AceFlags.ContainerInherit, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");
					break;

				case "f":
					// Add ACLs for "Full Control" access
					if (!AddAccessRights(args[0], ACCESS_MASK.GENERIC_ALL, AceFlags.ObjectInherit | AceFlags.InheritOnly, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");

					if (!AddAccessRights(args[0], (uint)AccessRights.PRINTER_ALL_ACCESS, AceFlags.ContainerInherit, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");
					break;

				case "md":
					// Add ACLs for "Manage Documents" access

					if (!AddAccessRights(args[0], ACCESS_MASK.GENERIC_ALL, AceFlags.ObjectInherit | AceFlags.InheritOnly, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");

					if (!AddAccessRights(args[0], ACCESS_MASK.READ_CONTROL, AceFlags.ContainerInherit, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");
					break;

				case "mp":
					// Add ACLs for "Manage Printers" access

					if (!AddAccessRights(args[0], (uint)(AccessRights.PRINTER_ACCESS_ADMINISTER | AccessRights.PRINTER_ACCESS_USE), 0, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");
					break;

				default:
					Console.Write("Unknown Access type\n");
					break;
			}

			return 0;
		}

		// FUNCTION: GetPrinterDACL
		//
		// PURPOSE: Obtains DACL from specified printer
		//
		// RETURN VALUE: true or false
		//
		// COMMENTS:
		unsafe static bool GetPrinterDACL(string szPrinterName, out SafePACL ppACL)
		{
			ppACL = default;

			if (!OpenPrinter(szPrinterName, out var hPrinter, new PRINTER_DEFAULTS { DesiredAccess = ACCESS_MASK.READ_CONTROL }))
				return false;

			using (hPrinter)
			{
				// Call GetPrinter twice to get size of printer info structure.

				var PrnInfo3 = GetPrinter<PRINTER_INFO_3>(hPrinter);

				if (!GetSecurityDescriptorDacl(PrnInfo3.pSecurityDescriptor, out var bDaclPres, out var pACL, out var bDef))
					return false;

				ppACL = new SafePACL(pACL);
			}

			return true;
		}

		// FUNCTION: SetPrinterDACL
		//
		// PURPOSE: Applies DACL to specified printer
		//
		// RETURN VALUE: true or false
		//
		// COMMENTS:
		static bool SetPrinterDACL(string szPrinterName, PACL pDacl)
		{
			var PrnDefs = new PRINTER_DEFAULTS
			{
				DesiredAccess = ACCESS_MASK.READ_CONTROL | ACCESS_MASK.WRITE_DAC
			};

			if (!OpenPrinter(szPrinterName, out var hPrinter, PrnDefs))
				return false;

			using (hPrinter)
			{
				var NewSD = new SafePSECURITY_DESCRIPTOR();
				if (!SetSecurityDescriptorDacl(NewSD, true, pDacl, false))
					return false;

				if (!SetPrinter(hPrinter, new PRINTER_INFO_3 { pSecurityDescriptor = NewSD }))
					return false;
			}

			return true;
		}

		// FUNCTION: AddAccessRights
		//
		// PURPOSE: Applies ACE for access to specified object DACL
		//
		// RETURN VALUE: true or false
		//
		// COMMENTS:
		unsafe static bool AddAccessRights(string szPrinterName, uint dwAccessMask, AceFlags bAceFlags, string szUserName, byte bType)
		{
			// ACL variables
			ACL_SIZE_INFORMATION? AclInfo = null;
			uint dwNewAceCount = 0;

			// New ACL variables
			int dwNewACLSize;

			// Temporary ACE
			ACCESS_ALLOWED_ACE* pTempAce; // structure is same for access denied ace
			uint CurrentAceIndex;

			try
			{
				// Call LookupAccountName

				if (!LookupAccountName(null, szUserName, out var pSid, out var szDomainName, out var SidType))
				{
					Console.WriteLine("Error {0} - LookupAccountName", Win32Error.GetLastError());
					return false;
				}

				Console.Write("Adding ACE for {0}\n", szUserName);

				// Get security DACL for printer

				if (!GetPrinterDACL(szPrinterName, out var pACL))
				{
					Console.Write("Error {0} getting printer DACL\n", Win32Error.GetLastError());
					return false;
				}

				// Compute size needed for the new ACL
				using (pACL)
				{
					if (!pACL.IsInvalid)  // Get size of old ACL
					{
						AclInfo = ((PACL)pACL).GetAclInformation<ACL_SIZE_INFORMATION>();
					}

					if (!pACL.IsInvalid)  // Add room for new ACEs
					{
						dwNewACLSize = (int)AclInfo.Value.AclBytesInUse +
									   Marshal.SizeOf(typeof(ACCESS_ALLOWED_ACE)) +
									   GetLengthSid(pSid) - sizeof(uint);
					}
					else
					{
						dwNewACLSize = Marshal.SizeOf(typeof(ACCESS_ALLOWED_ACE)) +
									   Marshal.SizeOf(typeof(ACL)) +
									   GetLengthSid(pSid) - sizeof(uint);
					}
				}
				// Allocate and setup ACL.

				using var pNewACL = new SafePACL(dwNewACLSize);
				if (pNewACL.IsInvalid)
				{
					Console.Write("LocalAlloc failed.\n");
					throw new Exception();
				}

				// If new ACE is Access Denied ACE add it to front of new ACL

				if (bType == ACCESS_DENY)
				{
					// Add the access-denied ACE to the new DACL
					if (!AddAccessDeniedAce(pNewACL, 2, dwAccessMask, pSid))
					{
						Console.Write("Error %d: AddAccessDeniedAce\n", Win32Error.GetLastError());
						throw new Exception();
					}

					dwNewAceCount++;

					// get pointer to ace we just added, so we can change the AceFlags

					if (!GetAce(pNewACL, 0, out var mTempAce))
					{
						Console.Write("Error %d: GetAce\n", Win32Error.GetLastError());
						throw new Exception();
					}

					pTempAce = (ACCESS_ALLOWED_ACE*)(void*)mTempAce.DangerousGetHandle();
					pTempAce->Header.AceFlags = bAceFlags;
				}

				// If a DACL was present, copy the resident ACEs to the new DACL

				if (!pACL.IsInvalid)
				{
					// Copy the file's old ACEs to our new ACL

					if (AclInfo.Value.AceCount > 0)
					{
						for (CurrentAceIndex = 0; CurrentAceIndex < AclInfo.Value.AceCount; CurrentAceIndex++)
						{
							// Get an ACE

							if (!GetAce(pACL, CurrentAceIndex, out var mTempAce))
							{
								Console.Write("Error %d: GetAce\n", Win32Error.GetLastError());
								throw new Exception();
							}
							pTempAce = (ACCESS_ALLOWED_ACE*)(void*)mTempAce.DangerousGetHandle();

							// Check to see if this ACE is identical to one were adding. If it is identical don't copy it over.
							if (!EqualSid((IntPtr)(void*)&(pTempAce->SidStart), pSid) ||
								(pTempAce->Mask != dwAccessMask) ||
								(pTempAce->Header.AceFlags != bAceFlags))
							{
								// ACE is distinct, add it to the new ACL

								if (!AddAce(pNewACL, 2, uint.MaxValue, (IntPtr)pTempAce, ((ACE_HEADER*)pTempAce)->AceSize))
								{
									Console.Write("Error %d: AddAce\n", Win32Error.GetLastError());
									throw new Exception();
								}

								dwNewAceCount++;
							}
						}
					}
				}

				// If the new ACE is an Access allowed ACE add it at the end.

				if (bType == ACCESS_ALLOW)
				{
					// Add the access-allowed ACE to the new DACL

					if (!AddAccessAllowedAce(pNewACL, 2, dwAccessMask, pSid))
					{
						Console.Write("Error %d: AddAccessAllowedAce\n", Win32Error.GetLastError());
						throw new Exception();
					}

					// Get pointer to ACE we just added, so we can change the AceFlags

					if (!GetAce(pNewACL,
								dwNewAceCount, // Zero based position of the last ace
								out var mTempAce))
					{
						Console.Write("Error %d: GetAce\n", Win32Error.GetLastError());
						throw new Exception();
					}
					pTempAce = (ACCESS_ALLOWED_ACE*)(void*)mTempAce.DangerousGetHandle();

					pTempAce->Header.AceFlags = bAceFlags;
				}

				// Set the DACL to the object

				if (!SetPrinterDACL(szPrinterName, pNewACL))
				{
					Console.Write("Error %d setting printer DACL\n", Win32Error.GetLastError());
					throw new Exception();
				}
			}
			finally
			{
			}

			return (true);
		}
	}
}