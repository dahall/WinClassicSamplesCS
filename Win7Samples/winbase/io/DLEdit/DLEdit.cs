/*-----------------------------------------------------------------------------
THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED
TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
PARTICULAR PURPOSE.

Copyright (C) 1999 - 2001 Microsoft Corporation.  All rights reserved.

DLEDIT  -- Drive Letter Assignment Editor

This program demonstrates how to add or remove persistent drive letter
assignments in Windows 2000, Windows XP, and Windows Server 2003.  These
drive letter assignments persist through machine reboots.

Platforms:
   This program requires Windows 2000 or later.

Command-line syntax:
   DLEDIT <drive letter> <NT device name>      -- Adds persistent drive letter
   DLEDIT -t <drive letter> <NT device name>   -- Adds temporary drive letter
   DLEDIT -r <drive letter>                    -- Removes a drive letter
   DLEDIT <drive letter>                       -- Shows drive letter mapping
   DLEDIT -a                                   -- Shows all drive letter mappings

Command-line examples:

   Say that E: refers to CD-ROM drive, and you want to make F: point to that
   CD-ROM drive instead.  Use the following two commands:

	  DLEDIT -r E:\
	  DLEDIT F:\ \Device\CdRom0

   To display what device a drive letter is mapped to, use the following
   command:

	  DLEDIT f:


*******************************************************************************
WARNING: WARNING: WARNING: WARNING: WARNING: WARNING: WARNING: WARNING:

   This program really will change drive letter assignments, and the changes
   persist through reboots.  Do not remove drive letters of your hard disks if
   you don't have this program on a floppy disk or you might not be able to
   access your hard disks again!
*******************************************************************************

-----------------------------------------------------------------------------*/
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace DLEdit;

internal static partial class Program
{
	private const byte HIDDEN_PARTITION_FLAG = 0x10;

	/* Private error codes.  Follows guidelines in winerror.h */
	private const uint PRIV_ERROR_DRIVE_LETTER_IN_USE = 0xE0000001;
	private const uint PRIV_ERROR_PARTITION_HIDDEN = 0x60000001;
	private const uint PRIV_ERROR_PARTITION_NOT_RECOGNIZED = 0x60000002;

	/*-----------------------------------------------------------------------------
	main( IN argc, IN argv )

	Parameters
	   argc
		  Count of the command-line arguments
	   argv
		  Array of pointers to the individual command-line arguments

	This function is the main program.  It parses the command-line arguments and
	performs the work of either removing a drive letter or adding a new one.
	-----------------------------------------------------------------------------*/
	private static void Main(string[] args)
	{
		string pszDriveLetter, pszNTDevice;

		/*
		 Command-line parsing.
			 1) Validate arguments
			 2) Determine what user wants to do
		*/
		if (2 == args.Length && 0 == StringComparer.OrdinalIgnoreCase.Compare(args[1 - 1], "-r") && IsDriveLetter(args[2 - 1]))
		{
			/*
				 User wants to remove the drive letter. Command line should be:
					 dledit -r <drive letter>
			*/
			pszDriveLetter = args[2 - 1];
			if (!RemovePersistentDriveLetter(pszDriveLetter))
			{
				if (!RemoveTemporaryDriveLetter(pszDriveLetter))
				{
					switch ((uint)Win32Error.GetLastError())
					{
						case Win32Error.ERROR_FILE_NOT_FOUND:
							Console.Write("{0} is not in use\n", pszDriveLetter);
							break;

						default:
							Console.Write("error {0}: couldn't remove {1}\n", GetLastError(), pszDriveLetter);
							break;
					}
				}
			}
		}
		else if (2 == args.Length && IsDriveLetter(args[1 - 1]))
		{
			/*
			 User wants to add a persistent drive letter. Command line should be:
				 dledit <drive letter> <NT device name>
			*/
			pszDriveLetter = args[1 - 1];
			pszNTDevice = args[2 - 1];

			/*
			 Try a persistent drive letter; if partition is hidden, the persistent
			 mapping will fail--try a temporary mapping and tell user about it.

			 Note: Hidden partitions can be assigned temporary drive letters.
			 These are nothing more than symbolic links created with
			 DefineDosDevice. Temporary drive letters will be removed when the
			 system is rebooted.
			*/
			if (!AssignPersistentDriveLetter(pszDriveLetter, pszNTDevice))
			{
				switch ((uint)Win32Error.GetLastError())
				{
					case PRIV_ERROR_PARTITION_HIDDEN:
						if (AssignTemporaryDriveLetter(pszDriveLetter, pszNTDevice))
							Console.Write("{0} is hidden; mapped {1} temporarily\n", pszNTDevice, pszDriveLetter);
						else
							Console.Write("{0} is hidden; couldn't map {1} to it\n", pszNTDevice, pszDriveLetter);
						break;

					case PRIV_ERROR_DRIVE_LETTER_IN_USE:
						Console.Write("{0} is in use, can't map it to {1}\n", pszDriveLetter, pszNTDevice);
						break;

					case Win32Error.ERROR_FILE_NOT_FOUND:
						Console.Write("{0} doesn't exist or can't be opened\n", pszNTDevice);
						break;

					case Win32Error.ERROR_INVALID_PARAMETER:
						Console.Write("{0} already has a drive letter; can't map {1} to it\n", pszNTDevice, pszDriveLetter);
						break;

					default:
						Console.Write("error {0}: couldn't map {1} to {2}\n", GetLastError(), pszDriveLetter, pszNTDevice);
						break;
				}
			}
		}
		else if (3 == args.Length && 0 == StringComparer.OrdinalIgnoreCase.Compare(args[1 - 1], "-t") && IsDriveLetter(args[2 - 1]))
		{
			/*
			 User wants to add a temporary drive letter. Command line should be:
				 dledit -t <drive letter> <NT device name>
			*/
			pszDriveLetter = args[2 - 1];
			pszNTDevice = args[3 - 1];

			if (!AssignTemporaryDriveLetter(pszDriveLetter, pszNTDevice))
			{
				switch ((uint)Win32Error.GetLastError())
				{
					case Win32Error.ERROR_FILE_NOT_FOUND:
						Console.Write("{0} doesn't exist or can't be opened\n", pszNTDevice);
						break;

					case PRIV_ERROR_DRIVE_LETTER_IN_USE:
						Console.Write("{0} is in use, can't map it to {1}\n", pszDriveLetter, pszNTDevice);
						break;

					default:
						Console.Write("error {0}: couldn't map {1} to {2}\n", GetLastError(), pszDriveLetter, pszNTDevice);
						break;
				}
			}
		}
		else if (1 == args.Length && IsDriveLetter(args[1 - 1]))
		{
			/*
			 User wants to show what device is connected to the drive letter.
			 Command line should be:
				 dledit <drive letter>
			*/
			pszDriveLetter = args[1 - 1];

			/*
			 Command-line argument for the drive letter could be in one of two
			 formats: C:\ or C:. We normalize this to C: for QueryDosDevice.
			*/
			var szDriveLetter = pszDriveLetter[0] + ":";
			if (QueryDosDevice(szDriveLetter, out var szNtDeviceName).Succeeded)
			{
				Console.Write("{0} is mapped to {1}\n", szDriveLetter, szNtDeviceName);
			}
			else
			{
				switch ((uint)Win32Error.GetLastError())
				{
					case Win32Error.ERROR_FILE_NOT_FOUND:
						Console.Write("{0} is not in use\n", pszDriveLetter);
						break;

					default:
						Console.Write("error {0}: couldn't get mapping for {1}\n", GetLastError(), pszDriveLetter);
						break;
				}
			}
		}
		else if (1 == args.Length && 0 == StringComparer.OrdinalIgnoreCase.Compare(args[1 - 1], "-a"))
		{
			/*
			 User wants to show all mappings of drive letters to their respective
			 devices. Command line should be:
				 dledit -a
			*/

			/*
			 Get a list of all current drive letters, then for each, print the
			 mapping. Drive letters are returned in the form
			 A:\<default>B:\<default>C:\<default>...<default>.
			*/
			Console.Write("Drive     Device\n" + "-----     ------\n");
			try
			{
				foreach (var drive in GetLogicalDriveStrings())
				{
					/* QueryDosDevice requires drive letters in the format X: */
					var szDriveLetter = drive.TrimEnd('\\');
					if (QueryDosDevice(szDriveLetter, out var szNtDeviceName).Succeeded)
						Console.Write("{0,-10}{1}\n", szDriveLetter, szNtDeviceName);
				}
			}
			catch
			{
				Console.Write("couldn't list drive letters and their devices\n");
			}
		}
		else
		{
			/* User has selected an invalid operation--display help. */
			PrintHelp(Environment.CommandLine);
		}
	}

	/*-----------------------------------------------------------------------------
	AssignPersistentDriveLetter ([In] pszDriveLetter, [In] pszDeviceName)

	Description:
	Creates a persistent drive letter that refers to a specified device. This
	drive letter will remain even when the system is restarted.

	Parameters:
	pszDriveLetter
	The new drive letter to create. Must be in the form X: or X:\

	pszDeviceName
	The NT device name to which the drive letter will be assigned.

	Return Value:
	Returns true if the drive letter was added, or false if it wasn't.
	-----------------------------------------------------------------------------*/
	private static bool AssignPersistentDriveLetter(string pszDriveLetter, string pszDeviceName)
	{
		/*
		Make sure we are passed a drive letter and a device name. lstrlen
		is useful because it will return zero if the pointer points to memory
		that can't be read or a string that causes an invalid page fault before
		the terminating default.
		*/
		if (0 == pszDriveLetter.Length || 0 == pszDeviceName.Length || !IsDriveLetter(pszDriveLetter))
		{
			SetLastError(Win32Error.ERROR_INVALID_PARAMETER);
			return false;
		}

		/*
		GetVolumeNameForVolumeMountPoint, SetVolumeMountPoint, and
		DeleteVolumeMountPoint require drive letters to have a trailing backslash.
		However, DefineDosDevice and QueryDosDevice require that the trailing
		backslash be absent. So, we'll set up the following variables:

		szDriveLetterAndSlash for the mount point APIs
		szDriveLetter for DefineDosDevice
		*/
		var szDriveLetter = pszDriveLetter[0] + ":";
		var szDriveLetterAndSlash = szDriveLetter + '\\';

		/*
		Determine if the drive letter is currently in use. If so, return the
		error to the caller. NOTE: we temporarily reuse szUniqueVolumeName
		instead of allocating a large array for this one call.
		*/
		if (QueryDosDevice(szDriveLetter, out _).Succeeded)
		{
			SetLastError(PRIV_ERROR_DRIVE_LETTER_IN_USE);
			return false;
		}

		/*
		To map a persistent drive letter, we must make sure that the target
		device is one of the following:

		A recognized partition and is not hidden
		A dynamic volume
		A non-partitionable device such as CD-ROM

		Start by using the drive letter as a symbolic link to the device. Then,
		open the device to gather the information necessary to determine if the
		drive can have a persistent drive letter.
		*/
		if (DefineDosDevice(DDD.DDD_RAW_TARGET_PATH, szDriveLetter, pszDeviceName))
		{
			VOLUME_GET_GPT_ATTRIBUTES_INFORMATION volinfo;
			PARTITION_INFORMATION partinfo;

			var szDriveName = "\\\\.\\" + szDriveLetter; // holds \\.\X: plus default.

			using SafeHFILE hDevice = CreateFile(szDriveName, FileAccess.GENERIC_READ, System.IO.FileShare.ReadWrite, default, System.IO.FileMode.Open, 0);
			if (hDevice.IsInvalid)
			{
				/*
				Remove the drive letter symbolic link we created, let caller know
				we couldn't open the drive.
				*/
				DefineDosDevice(DDD.DDD_RAW_TARGET_PATH | DDD.DDD_REMOVE_DEFINITION | DDD.DDD_EXACT_MATCH_ON_REMOVE, szDriveLetter, pszDeviceName);
				return false;
			}
			else
			{
				/*
				See if drive is partitionable and retrieve the partition type.
				If the device doesn't have a partition, note it by setting the
				partition type to unused.
				*/
				if (!DeviceIoControl(hDevice, IOControlCode.IOCTL_DISK_GET_PARTITION_INFO, out partinfo))
				{
					partinfo.PartitionType = PartitionType.PARTITION_ENTRY_UNUSED;
				}

				/*
				On Windows XP, partition entries on Guid Partition Table drives
				have an attribute that determines whether partitions are hidden.
				Therefore, we must check this bit on the target partition.

				If we're running on Windows 2000, there are no GPT drives, so
				set flags to none. This is important for the check for hidden
				partitions later.
				*/
				if (!IsWindowsXP_orLater() || !DeviceIoControl(hDevice, IOControlCode.IOCTL_VOLUME_GET_GPT_ATTRIBUTES, out volinfo))
				{
					volinfo.GptAttributes = 0;
				}
			}

			/*
			Now, make sure drive meets requirements for receiving a persistent
			drive letter.

			Note: on Windows XP, partitions that were hidden when the system
			booted are not assigned a unique volume name. They will not be given
			one until they are marked as not hidden and the system rebooted.
			Therefore, we cannot create a mount point hidden partitions.

			On Windows 2000, hidden partitions are assigned unique volume names
			and so can have persistent drive letters. However, we will not allow
			that behavior in this tool as hidden partitions should not be assigned
			drive letters.
			*/
			if (IsPartitionHidden(partinfo.PartitionType, volinfo.GptAttributes))
			{
				// remove the drive letter we created, let caller know what happened.
				DefineDosDevice(DDD.DDD_RAW_TARGET_PATH | DDD.DDD_REMOVE_DEFINITION | DDD.DDD_EXACT_MATCH_ON_REMOVE, szDriveLetter, pszDeviceName);

				SetLastError(PRIV_ERROR_PARTITION_HIDDEN);
				return false;
			}

			/*
			Verify that the drive letter must refer to a recognized partition,
			a dynamic volume, or a non-partitionable device such as CD-ROM.
			*/
			if (IsRecognizedPartition(partinfo.PartitionType) ||
				PartitionType.PARTITION_LDM == partinfo.PartitionType ||
				PartitionType.PARTITION_ENTRY_UNUSED == partinfo.PartitionType)
			{
				/*
				Now add the drive letter by calling on the volume mount manager.
				Once we have the unique volume name that the new drive letter
				will point to, delete the symbolic link because the Mount Manager
				allows only one reference to a device at a time (the new one to
				be added).
				*/
				var szUniqueVolumeName = new StringBuilder(MAX_PATH);
				var fResult = GetVolumeNameForVolumeMountPoint(szDriveLetterAndSlash, szUniqueVolumeName, MAX_PATH);

				DefineDosDevice(DDD.DDD_RAW_TARGET_PATH | DDD.DDD_REMOVE_DEFINITION | DDD.DDD_EXACT_MATCH_ON_REMOVE, szDriveLetter, pszDeviceName);

				if (fResult)
					fResult = SetVolumeMountPoint(szDriveLetterAndSlash, szUniqueVolumeName.ToString());

				return fResult;
			}
			else
			{
				/*
				Device doesn't meet the criteria for persistent drive letter.
				Remove the drive letter symbolic link we created.
				*/
				DefineDosDevice(DDD.DDD_RAW_TARGET_PATH | DDD.DDD_REMOVE_DEFINITION | DDD.DDD_EXACT_MATCH_ON_REMOVE, szDriveLetter, pszDeviceName);
				SetLastError(PRIV_ERROR_PARTITION_NOT_RECOGNIZED);
				return false;
			}
		}
		else
		{
			return false;
		}
	}

	/*-----------------------------------------------------------------------------
	AssignTemporaryDriveLetter([In] pszDriveLetter, [In] pszDeviceName)

	Description:
	Creates a temporary drive letter that refers to a specified device. This
	drive letter will exist only until the system is shut down or restarted.

	Parameters:
	pszDriveLetter
	The new drive letter to create. Must be in the form X: or X:\

	pszDeviceName
	The NT device name to which the drive letter will be assigned.

	Return Value:
	Returns true if the temporary drive letter was assigned, or false if it
	could not be.

	Notes:
	A temporary drive letter is just a symbolic link. It can be removed at any
	time by deleting the symbolic link. If it exists when the system is shut
	down or restarted, it will be removed automatically.

	AssignTemporaryDriveLetter requires device to be present.
	-----------------------------------------------------------------------------*/
	private static bool AssignTemporaryDriveLetter(string pszDriveLetter, string pszDeviceName)
	{
		/* Verify that caller passed a drive letter and device name. */
		if (0 == pszDriveLetter.Length || 0 == pszDeviceName.Length || !IsDriveLetter(pszDriveLetter))
		{
			SetLastError(Win32Error.ERROR_INVALID_PARAMETER);
			return false;
		}

		/*
		Make sure the drive letter isn't already in use. If not in use,
		create the symbolic link to establish the temporary drive letter.

		pszDriveLetter could be in the format X: or X:\; QueryDosDevice and
		DefineDosDevice need X:
		*/
		var szDriveLetter = pszDriveLetter[0] + ":";
		if (QueryDosDevice(szDriveLetter, out _).Failed)
		{
			/*
			If we can create the symbolic link, verify that it points to a real
			device. If not, remove the link and return an error. CreateFile sets
			the last error code to ERROR_FILE_NOT_FOUND.
			*/
			if (DefineDosDevice(DDD.DDD_RAW_TARGET_PATH, szDriveLetter, pszDeviceName))
			{
				var szDriveName = "\\\\.\\" + szDriveLetter;

				using SafeHFILE hDevice = CreateFile(szDriveName, FileAccess.GENERIC_READ, System.IO.FileShare.ReadWrite,
					default, System.IO.FileMode.Open, 0);
				if (hDevice.IsInvalid)
				{
					DefineDosDevice(DDD.DDD_RAW_TARGET_PATH | DDD.DDD_REMOVE_DEFINITION | DDD.DDD_EXACT_MATCH_ON_REMOVE, szDriveLetter, pszDeviceName);
					return false;
				}
				return true;
			}
		}
		else
			SetLastError(PRIV_ERROR_DRIVE_LETTER_IN_USE);
		return false;
	}

	/*-----------------------------------------------------------------------------
	IsDriveLetter([In] pszDriveLetter)

	Description:
	Verifies string passed in is of the form X: or X:\ where X is a letter.

	Parameters:
	pszDriveLetter
	A null terminated string.

	Return Value:
	true if the string is of the form X: or X:\ where X is a letter. X
	may be upper-case or lower-case. If the string isn't of this from,
	returns false.
	-----------------------------------------------------------------------------*/
	private static bool IsDriveLetter(string pszDriveLetter) =>
		System.Text.RegularExpressions.Regex.IsMatch(pszDriveLetter, @"[A-Za-z]\:\\?");

	/*-----------------------------------------------------------------------------
	IsPartitionHidden([In] partitionType, [In] partitionAttribs)

	Description:
	Determines if the partition hosting a volume is hidden or not.

	Parameters:
	partitionType
	Specifies the partitition type; this value is obtained from
	IOCTL_DISK_GET_PARTITION_INFO or IOCTL_DISK_GET_DRIVE_GEOMETRY.
	A partition type that has bit 0x10 set is hidden.

	partitionAttribs
	Specifies the attributes of a Guid Partition Type (GPT)-style partition
	entry. This value is obtained from IOCTL_VOLUME_GET_GPT_ATTRIBUTES
	or IOCTL_DISK_GET_DRIVE_GEOMETRY_EX. A GPT partition that has
	GPT_BASIC_DATA_ATTRIBUTE_HIDDEN set is hidden.

	Return Value:
	Returns true if the partition is hidden or false if it is not.

	Notes:
	On Windows XP and Windows Server 2003, partitions that were hidden when
	the system booted are not assigned a unique volume name. They will not
	be given one until they are marked as not hidden and the system rebooted.
	Therefore, we cannot create a mount point hidden partitions.

	On Windows 2000, hidden partitions are assigned unique volume names
	and so can have persistent drive letters. However, we will not allow
	that behavior in this tool as hidden partitions should not be assigned
	drive letters.
	-----------------------------------------------------------------------------*/
	private static bool IsPartitionHidden(PartitionType partitionType, GPT_BASIC_DATA_ATTRIBUTE partitionAttribs) =>
		partitionAttribs.IsFlagSet(GPT_BASIC_DATA_ATTRIBUTE.GPT_BASIC_DATA_ATTRIBUTE_HIDDEN) ||
			(((byte)partitionType & HIDDEN_PARTITION_FLAG) != 0 &&
			IsRecognizedPartition((PartitionType)((byte)partitionType & ~HIDDEN_PARTITION_FLAG)));

	/*-----------------------------------------------------------------------------
	IsWindowsXP_orLater()

	Description:
	Determines if the currently-running version of Windows is Windows XP or
	Windows Server 2003 or later.

	Return Value:
	Returns true if the currently-running system is Windows XP or Windows 2003
	Server or later systems. Returns false otherwise.
	-----------------------------------------------------------------------------*/
	private static bool IsWindowsXP_orLater()
	{
		var comparisonMask = VerSetConditionMask(0, VERSION_MASK.VER_MAJORVERSION, VERSION_CONDITION.VER_GREATER_EQUAL);
		comparisonMask = VerSetConditionMask(comparisonMask, VERSION_MASK.VER_MINORVERSION, VERSION_CONDITION.VER_GREATER_EQUAL);
		var osvi = new OSVERSIONINFOEX { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEX>(), dwMajorVersion = 5, dwMinorVersion = 1 };
		return VerifyVersionInfo(ref osvi, VERSION_MASK.VER_MAJORVERSION | VERSION_MASK.VER_MINORVERSION, comparisonMask);
	}

	/*-----------------------------------------------------------------------------
	PrintHelp([In] pszAppName )

	Parameters
	pszAppName
	The name of the executable. Used in displaying the help for this app.

	Prints the command-line usage help.
	-----------------------------------------------------------------------------*/

	private static void PrintHelp(string pszAppName)
	{
		Console.Write("Adds, removes, queries drive letter assignments\n\n");
		Console.Write("usage: {0} <Drive Letter> <NT device name> add a drive letter\n", pszAppName);
		Console.Write(" {0} -t <Drive Letter> <NT device name> add a temporary drive letter\n", pszAppName);
		Console.Write(" {0} -r <Drive Letter> remove a drive letter\n", pszAppName);
		Console.Write(" {0} <Drive Letter> show mapping for drive letter\n", pszAppName);
		Console.Write(" {0} -a show all mappings\n\n", pszAppName);
		Console.Write("example: {0} e:\\ \\Device\\CdRom0\n", pszAppName);
		Console.Write(" {0} -r e:\\\n", pszAppName);
	}

	private static Win32Error QueryDosDevice(string lpDeviceName, out string? targetPath)
	{
		var qddBuf = new StringBuilder(MAX_PATH);
		while (0 == Kernel32.QueryDosDevice(lpDeviceName, qddBuf, qddBuf.Capacity))
		{
			var err = Win32Error.GetLastError();
			if (err == Win32Error.ERROR_INSUFFICIENT_BUFFER)
				qddBuf.Capacity *= 2;
			else
			{
				targetPath = null;
				return err;
			}
		}
		targetPath = qddBuf.ToString();
		return Win32Error.ERROR_SUCCESS;
	}
	/*-----------------------------------------------------------------------------
	RemovePersistentDriveLetter([In] pszDriveLetter)

	Description:
	Removes a drive letter that was created by AssignPersistentDriveLetter().

	Parameters:
	pszDriveLetter
	The drive letter to remove. Must be in the format X: or X:\

	Return Value:
	Returns true if the drive letter was removed, or false if it wasn't.
	-----------------------------------------------------------------------------*/
	private static bool RemovePersistentDriveLetter(string pszDriveLetter)
	{
		/* Make sure we have a drive letter. */
		if (0 == pszDriveLetter.Length || !IsDriveLetter(pszDriveLetter))
		{
			SetLastError(Win32Error.ERROR_INVALID_PARAMETER);
			return false;
		}

		/*
		pszDriveLetter could be in the format X: or X:\. DeleteVolumeMountPoint
		requires X:\, so add a trailing backslash.
		*/
		var szDriveLetterAndSlash = pszDriveLetter[0] + ":\\";
		return DeleteVolumeMountPoint(szDriveLetterAndSlash);
	}

	/*-----------------------------------------------------------------------------
	RemoveTemporaryDriveLetter([In] pszDriveLetter)

	Description:
	Removes a drive letter that was created by AssignTemporaryDriveLetter().

	Parameters:
	pszDriveLetter
	The drive letter to remove. Must be in the format X: or X:\

	Return Value:
	Returns true if the drive letter was removed, or false if it wasn't.
	-----------------------------------------------------------------------------*/
	private static bool RemoveTemporaryDriveLetter(string pszDriveLetter)
	{
		/* Verify that caller passed a drive letter and device name. */
		if (0 == pszDriveLetter.Length || !IsDriveLetter(pszDriveLetter))
		{
			SetLastError(Win32Error.ERROR_INVALID_PARAMETER);
			return false;
		}

		/*
		pszDriveLetter could be in the format X: or X:\; DefineDosDevice
		needs X:
		*/
		var szDriveLetter = pszDriveLetter[0] + ":";
		return QueryDosDevice(szDriveLetter, out var szDeviceName).Succeeded &&
			DefineDosDevice(DDD.DDD_RAW_TARGET_PATH | DDD.DDD_REMOVE_DEFINITION | DDD.DDD_EXACT_MATCH_ON_REMOVE, szDriveLetter, szDeviceName!);
	}
}