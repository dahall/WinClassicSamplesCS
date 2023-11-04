using System.Diagnostics;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace Mount
{
	internal static class Program
	{
		private const int IO_REPARSE_TAG_MOUNT_POINT = unchecked((int)0xA0000003);

		private static int Main(string[] args)
		{
			// Make sure user has supplied required number of command-line arguments.
			if (args.Length != 2 && args.Length != 3)
			{
				PrintHelp();
				return -1;
			}

			/*
			Since we have at least 3 args, we can initialize the pointers to them.
			We use pointers to explicitly refer to the arguments to make the rest
			of the code more understandable.
			*/
			var pszMountDir = args[^1];
			var pszDriveToMount = args[^2];
			var pszOptions = args[^3];

			// See if "-o" is present in command line. It must be the second argument.
			var bOverwriteMount = args.Length == 3 && pszOptions.Equals("-o", StringComparison.OrdinalIgnoreCase);

			/*
			If bOverwriteMount != true (i.e. user wants to keep an existing
			mount point), then need to check destination to see if it exists and
			if it is a mount point. If so, don't create a volume mount point on it.

			The way to tell if a directory is a mount point is to:

			1) Call FindFirstFile().
			2) If WIN32_FIND_DATA.dwFileAttributes contains
			FILE_ATTRIBUTE_REPARSE_POINT see if WIN32_FIND_DATA.dwReserved0
			is IO_REPARSE_TAG_MOUNT_POINT.
			3) If so, then the directory is a mount point.
			*/
			if (!bOverwriteMount)
			{
				SafeSearchHandle hFind = FindFirstFile(pszMountDir, out WIN32_FIND_DATA fileInfo);
				if (!hFind.IsInvalid)
				{
					hFind.Dispose(); // Don't need the find handle anymore.

					/*
					If the destination is a mount point, tell user we're not going
					to replace it, and then exit.
					*/
					if ((fileInfo.dwFileAttributes & System.IO.FileAttributes.ReparsePoint) != 0 &&
						(fileInfo.dwReserved0 == IO_REPARSE_TAG_MOUNT_POINT))
					{
						Console.Write("{0} is already a mount point; it will not be replaced\n", pszMountDir);
						return 0;
					}
				}
				/*
				If hFind == INVALID_HANDLE_VALUE here, we didn't find the directory
				to be the mount point. We could exit here, but we won't. We'll
				just keep going because CreateMountPoint will fail the creation of
				the mount point on the non-existant directory.
				*/
			}

			/*
			Create the mount point. Report whether we succeeded or failed.
			*/
			if (CreateMountPoint(pszDriveToMount, pszMountDir))
			{
				Console.Write("mounted {0} to {1}\n", pszDriveToMount, pszMountDir);
			}
			else
			{
				Console.Write("couldn't mount {0} to {1}\n", pszDriveToMount, pszMountDir);
				DEBUG_PRINT("CreateMountPoint failed with error", GetLastError());
			}

			return 0;
		}

		/*-----------------------------------------------------------------------------
		CreateMountPoint(pszDriveToMount, pszDirToMount )

		Parameters
		pszDriveToMount
		The drive that will be associated with the mount point directory.

		pszDirToMount
		The location that pszDriveToMount is to be mounted. This must be an
		empty directory. It can also be a current mount point; if it is, then
		the existing mount point will automatically be unmounted by
		SetVolumeMountPoint.

		Return Value
		Returns true if successful, or false otherwwise.

		Notes
		Since GetVolumeNameForVolumeMountPoint and SetVolumeMountPoint require
		trailing backslashes, we'll add them if necessary.
		-----------------------------------------------------------------------------*/
		private static bool CreateMountPoint(string pszDriveToMount, string pszDirToMount)
		{
			const int VOL_NAME_MAX = 80;
			var szUniqueVolumeName = new StringBuilder(VOL_NAME_MAX);
			string szDriveName, pszDirName = default;

			/*
			Add trailing backslashes to drive letter and mount point directory name
			because volume mount point APIs require them.

			Since drive letters are of the format C:\ or C:, we know that the max
			drive letter string is 4 chars long. We can thus use array addressing
			to do a faster equivalent of:

			lstrcpyn (szDriveName, pszDriveToMount, 3);
			lstrcat (szDriveName, "\\");

			If the directory name doesn't already have a trailing backslash, we
			just copy it to a new buffer and add the trailing backslash.
			*/
			try
			{
				szDriveName = new string(new[] { pszDriveToMount[0], pszDriveToMount[1], '\\' });

				// now the directory name
				pszDirName = pszDirToMount[^1] == '\\' ? pszDirToMount : pszDirToMount + '\\';
			}
			catch
			{
				return false;
			}

			// Create the mount point...
			if (!GetVolumeNameForVolumeMountPoint(szDriveName, szUniqueVolumeName, VOL_NAME_MAX))
			{
				DEBUG_PRINT("GetVolumeNameForVolumeMountPoint failed with error", GetLastError());
				return false;
			}

			if (!SetVolumeMountPoint(pszDirName, szUniqueVolumeName.ToString()))
			{
				DEBUG_PRINT("SetVolumeMountPoint failed with error", GetLastError());
				return false;
			}

			return true;
		}

		/*-----------------------------------------------------------------------------
		PrintHelp()

		Notes
		Prints usage notes for the command line syntax. Called if the user doesn't
		specify the command line correctly.
		-----------------------------------------------------------------------------*/
		private static void PrintHelp() => Console.Write("usage: mount [-o] <drive> <directory>\n" +
				"\t-o overwrite existing mount point on <directory>\n");

		/*-----------------------------------------------------------------------------
		DebugPrint(pszMsg, dwErr )

		Parameters
		pszMsg
		The string to be printed to STDOUT
		dwErr
		The error code; usually obtained from GetLastError. If dwErr is zero,
		then no error code is added to the error string. If dwErr is non-zero,
		then the error code will be printed in the error string.
		-----------------------------------------------------------------------------*/
		[Conditional("DEBUG")]
		private static void DEBUG_PRINT(string pszMsg, Win32Error dwErr)
		{
			if (dwErr.Failed)
				Console.Write("{0}: {1}\n", pszMsg, dwErr);
			else
				Console.Write("{0}\n", pszMsg);
		}
	}
}