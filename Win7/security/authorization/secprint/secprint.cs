using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

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
				Console.Write("USAGE: secprint <printer name> <user name> <P|MD|MP|F> [D]\n";
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
					if (!AddAccessRights(args[0], PRINTER_EXECUTE, CONTAINER_INHERIT_ACE, args[1], bDeny ? ACCESS_DENY : ACCESS_ALLOW))
						Console.Write("Error Adding Access Rights\n");
					break;

				case "f":
					// Add ACLs for "Full Control" access
					if (!AddAccessRights(args[0],
											 GENERIC_ALL,
											 OBJECT_INHERIT_ACE | INHERIT_ONLY_ACE,
											 args[1],
											 (byte)(bDeny ? ACCESS_DENY : ACCESS_ALLOW)))

						Console.Write("Error Adding Access Rights\n");

					if (!AddAccessRights(args[0],
										 PRINTER_ALL_ACCESS,
										 CONTAINER_INHERIT_ACE,
										 args[1],
										 (byte)(bDeny ? ACCESS_DENY : ACCESS_ALLOW)))

						Console.Write("Error Adding Access Rights\n");
					break;

				case "md":
					// Add ACLs for "Manage Documents" access

					if (!AddAccessRights(args[0],
										 GENERIC_ALL,
										 OBJECT_INHERIT_ACE | INHERIT_ONLY_ACE,
										 args[1],
										 (byte)(bDeny ? ACCESS_DENY : ACCESS_ALLOW)))

						Console.Write("Error Adding Access Rights\n");

					if (!AddAccessRights(args[0],
										 READ_CONTROL,
										 CONTAINER_INHERIT_ACE,
										 args[1],
										 (byte)(bDeny ? ACCESS_DENY : ACCESS_ALLOW)))

						Console.Write("Error Adding Access Rights\n");
					break;

				case "mp":
					// Add ACLs for "Manage Printers" access

					if (!AddAccessRights(args[0],
										 PRINTER_ACCESS_ADMINISTER | PRINTER_ACCESS_USE,
										 0,
										 args[1],
										 (byte)(bDeny ? ACCESS_DENY : ACCESS_ALLOW)))

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
		static bool GetPrinterDACL(string szPrinterName, out SafePACL ppACL)
		{
			HANDLE hPrinter = null;
			PPRINTER_INFO_3 pPrnInfo3 = null;
			PRINTER_DEFAULTS PrnDefs;
			uint cbNeeded = 0;
			uint cbBuf = 0;
			bool bDaclPres;
			bool bDef;
			bool bRes = false;
			ACL_SIZE_INFORMATION AclInfo;
			PACL pACL = null;

			PrnDefs.DesiredAccess = READ_CONTROL;
			PrnDefs.pDatatype = null;
			PrnDefs.pDevMode = null;

			try
			{
				if (!OpenPrinter(szPrinterName, &hPrinter, &PrnDefs))
					__leave;

				// Call GetPrinter twice to get size of printer info structure.

				while (!GetPrinter(hPrinter, 3, (LPBYTE)pPrnInfo3, cbBuf, &cbNeeded))
				{
					if (GetLastError() == ERROR_INSUFFICIENT_BUFFER)
					{
						cbBuf = cbNeeded;
						pPrnInfo3 = LocalAlloc(LPTR, cbNeeded);
						if (pPrnInfo3 == null)
							__leave;
					}
					else
						__leave;
				}

				if (!GetPrinter(hPrinter, 3, (LPBYTE)pPrnInfo3, cbBuf, &cbNeeded))
					__leave;

				if (!GetSecurityDescriptorDacl(pPrnInfo3->pSecurityDescriptor,
												 &bDaclPres, &pACL, &bDef))
					__leave;

				if (!GetAclInformation(pACL, &AclInfo,
									  sizeof(ACL_SIZE_INFORMATION), AclSizeInformation))
					__leave;

				// The caller just needs the DACL. So, make a copy of the DACL for the caller and free the printer info structure. The caller
				// must free the allocated memory for this DACL copy.

				*ppACL = LocalAlloc(LPTR, AclInfo.AclBytesInUse);
				if (*ppACL == null)
					__leave;

				memcpy(*ppACL, pACL, AclInfo.AclBytesInUse);

				bRes = true;
			}
			finally
			{
				if (pPrnInfo3 != null) LocalFree((HLOCAL)pPrnInfo3);
				if (hPrinter != null) ClosePrinter(hPrinter);
			}

			return bRes;
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
			HANDLE hPrinter = null;
			PRINTER_INFO_3 pi3;
			PRINTER_DEFAULTS PrnDefs;
			SECURITY_DESCRIPTOR NewSD;
			bool bRes = false;

			PrnDefs.DesiredAccess = READ_CONTROL | WRITE_DAC;
			PrnDefs.pDatatype = null;
			PrnDefs.pDevMode = null;
			pi3.pSecurityDescriptor = &NewSD;

			try
			{
				if (!OpenPrinter(szPrinterName, &hPrinter, &PrnDefs))
					__leave;

				if (!InitializeSecurityDescriptor(&NewSD, SECURITY_DESCRIPTOR_REVISION))
					__leave;

				if (!SetSecurityDescriptorDacl(&NewSD, true, pDacl, false))
					__leave;

				if (!SetPrinter(hPrinter, 3, (LPBYTE) & pi3, 0))
					__leave;

				bRes = true;
			}
			finally
			{
				if (hPrinter != null) ClosePrinter(hPrinter);
			}

			return bRes;
		}

		// FUNCTION: AddAccessRights
		//
		// PURPOSE: Applies ACE for access to specified object DACL
		//
		// RETURN VALUE: true or false
		//
		// COMMENTS:
		static bool AddAccessRights(string szPrinterName, uint dwAccessMask, byte bAceFlags, string szUserName, byte bType)
		{
			// ACL variables
			ACL_SIZE_INFORMATION AclInfo;
			uint dwNewAceCount = 0;

			// New ACL variables
			PACL pNewACL = null;
			uint dwNewACLSize;

			// Temporary ACE
			PACCESS_ALLOWED_ACE pTempAce; // structure is same for access denied ace
			UINT CurrentAceIndex;

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

				if (pACL)  // Get size of old ACL
				{
					if (!GetAclInformation(pACL, &AclInfo, sizeof(ACL_SIZE_INFORMATION),
					   AclSizeInformation))
					{
						Console.Write("Error %d: GetAclInformation\n", Win32Error.GetLastError());
						__leave;
					}
				}

				if (pACL)  // Add room for new ACEs
				{
					dwNewACLSize = AclInfo.AclBytesInUse +
								   sizeof(ACCESS_ALLOWED_ACE) +
								   GetLengthSid(pSid) - sizeof(uint);
				}
				else
				{
					dwNewACLSize = sizeof(ACCESS_ALLOWED_ACE) +
								   sizeof(ACL) +
								   GetLengthSid(pSid) - sizeof(uint);
				}

				// Allocate and setup ACL.

				pNewACL = (PACL)LocalAlloc(LPTR, dwNewACLSize);
				if (pNewACL == null)
				{
					Console.Write("LocalAlloc failed.\n");
					__leave;
				}

				if (!InitializeAcl(pNewACL, dwNewACLSize, ACL_REVISION2))
				{
					Console.Write("Error %d: InitializeAcl\n", Win32Error.GetLastError());
					__leave;
				}

				// If new ACE is Access Denied ACE add it to front of new ACL

				if (bType == ACCESS_DENY)
				{
					// Add the access-denied ACE to the new DACL
					if (!AddAccessDeniedAce(pNewACL, ACL_REVISION2, dwAccessMask, pSid))
					{
						Console.Write("Error %d: AddAccessDeniedAce\n", Win32Error.GetLastError());
						__leave;
					}

					dwNewAceCount++;

					// get pointer to ace we just added, so we can change the AceFlags

					if (!GetAce(pNewACL,
								0, // we know it is the first ace in the Acl
								&pTempAce))
					{
						Console.Write("Error %d: GetAce\n", Win32Error.GetLastError());
						__leave;
					}

					pTempAce->Header.AceFlags = bAceFlags;
				}

				// If a DACL was present, copy the resident ACEs to the new DACL

				if (pACL)
				{
					// Copy the file's old ACEs to our new ACL

					if (AclInfo.AceCount)
					{
						for (CurrentAceIndex = 0; CurrentAceIndex < AclInfo.AceCount;
																	 CurrentAceIndex++)
						{
							// Get an ACE

							if (!GetAce(pACL, CurrentAceIndex, &pTempAce))
							{
								Console.Write("Error %d: GetAce\n", Win32Error.GetLastError());
								__leave;
							}

							// Check to see if this ACE is identical to one were adding. If it is identical don't copy it over.
							if (!EqualSid((PSID) & (pTempAce->SidStart), pSid) ||
								(pTempAce->Mask != dwAccessMask) ||
								(pTempAce->Header.AceFlags != bAceFlags))
							{
								// ACE is distinct, add it to the new ACL

								if (!AddAce(pNewACL, ACL_REVISION, MAXuint, pTempAce,
								   ((PACE_HEADER)pTempAce)->AceSize))
								{
									Console.Write("Error %d: AddAce\n", Win32Error.GetLastError());
									__leave;
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

					if (!AddAccessAllowedAce(pNewACL, ACL_REVISION2, dwAccessMask, pSid))
					{
						Console.Write("Error %d: AddAccessAllowedAce\n", Win32Error.GetLastError());
						__leave;
					}

					// Get pointer to ACE we just added, so we can change the AceFlags

					if (!GetAce(pNewACL,
								dwNewAceCount, // Zero based position of the last ace
								&pTempAce))
					{
						Console.Write("Error %d: GetAce\n", Win32Error.GetLastError());
						__leave;
					}

					pTempAce->Header.AceFlags = bAceFlags;
				}

				// Set the DACL to the object

				if (!SetPrinterDACL(szPrinterName, pNewACL))
				{
					Console.Write("Error %d setting printer DACL\n", Win32Error.GetLastError());
					__leave;
				}
			}
			finally
			{
				// Free the memory allocated for the old and new ACL and user info
				if (pACL != null) LocalFree((HLOCAL)pACL);
				if (pNewACL != null) LocalFree((HLOCAL)pNewACL);
				if (pSid != null) LocalFree(pSid);
				if (szDomainName != null) LocalFree(szDomainName);
			}

			return (true);
		}
	}
}