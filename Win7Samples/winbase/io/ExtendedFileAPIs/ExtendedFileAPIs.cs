using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace ExtendedFileAPIs;

static class Program
{
	static int Main(string[] args)
	{
		SafeHFILE hFile;

		if (args.Length < 1)
		{
			PrintUsage();
			return 1;
		}

		//
		// Check for flag that indicates we should open by ID
		//
		if (string.Equals(args[0], "-id", StringComparison.InvariantCultureIgnoreCase))
		{
			if (args.Length < 2)
			{
				PrintUsage();
				return 1;
			}

			//
			// Open a handle to the current directory to use as a hint when
			// opening the file by ID.
			//
			using var hDir = CreateFile(".", FileAccess.GENERIC_READ, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete,
				default, System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, default);

			if (hDir.IsInvalid)
			{
				Console.Write("Couldn't open current directory.\n");
				return 1;
			}

			//
			// Capture the file ID and attempt to open by ID.
			//
			var fileId = new FILE_ID_DESCRIPTOR
			{
				dwSize = (uint)Marshal.SizeOf<FILE_ID_DESCRIPTOR>(),
				Type = FILE_ID_TYPE.FileIdType,
				Id = new FILE_ID_DESCRIPTOR.DUMMYUNIONNAME { FileId = long.Parse(args[1]) }
			};

			hFile = OpenFileById(hDir, fileId, FileAccess.GENERIC_READ, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete,
				default, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);

			if (hFile.IsInvalid)
			{
				Console.Write("\nError opening file with ID {0}. Last error was {1}.\n", args[1], GetLastError());
				return 1;
			}
		}
		else
		{
			hFile = CreateFile(args[0], FileAccess.GENERIC_READ, System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete,
				default, System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS, default);

			if (hFile.IsInvalid)
			{
				Console.Write("\nError opening file {0}. Last error was {1}.\n", args[0], GetLastError());
				return 1;
			}
		}

		//
		// Display information about the file/directory
		//
		var bResult = DisplayBasicInfo(hFile, out var bIsDirectory);

		if (!bResult)
		{

			Console.Write("\nError displaying basic information.\n");
			return 1;
		}

		bResult = DisplayStandardInfo(hFile);

		if (!bResult)
		{

			Console.Write("\nError displaying standard information.\n");
			return 1;
		}

		bResult = DisplayNameInfo(hFile);

		if (!bResult)
		{

			Console.Write("\nError displaying name information.\n");
			return 1;
		}

		//
		// For directories we query for full directory information, which gives us
		// various pieces of information about each entry in the directory.
		//
		if (bIsDirectory)
		{

			bResult = DisplayFullDirectoryInfo(hFile);

			if (!bResult)
			{

				Console.Write("\nError displaying directory information.\n");
				return 1;
			}

			//
			// Otherwise we query information about the streams associated with the 
			// file.
			//
		}
		else
		{

			bResult = DisplayStreamInfo(hFile);

			if (!bResult)
			{

				Console.Write("\nError displaying stream information.\n");
				return 1;
			}
		}

		hFile?.Dispose();
		return 0;
	}

	static void PrintUsage()
	{
		Console.Write("Usage: ExtendedFileAPIs [-id] targetFile\n\n");
		Console.Write(" Display extended information about the target file or directory\n");
		Console.Write(" using the GetFileInformationByHandleEx API.\n\n");
		Console.Write(" -id If this flag is specified the target file is assumed to be a file ID\n");
		Console.Write(" and the program will attempt to open the file using OpenFileById.\n");
		Console.Write(" The current directory will be used to determine which volume to scope\n");
		Console.Write(" the open to.\n");
	}

	static void PrintFileAttributes(FileFlagsAndAttributes FileAttributes)
	{
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_ARCHIVE) != 0)
		{
			Console.Write("Archive ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_ARCHIVE;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0)
		{
			Console.Write("Directory ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_READONLY) != 0)
		{
			Console.Write("Read-Only ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_READONLY;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_HIDDEN) != 0)
		{
			Console.Write("Hidden ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_HIDDEN;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_SYSTEM) != 0)
		{
			Console.Write("System ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_SYSTEM;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL) != 0)
		{
			Console.Write("Normal ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_TEMPORARY) != 0)
		{
			Console.Write("Temporary ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_TEMPORARY;
		}
		if ((FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_COMPRESSED) != 0)
		{
			Console.Write("Compressed ");
			FileAttributes &= ~FileFlagsAndAttributes.FILE_ATTRIBUTE_COMPRESSED;
		}

		if (FileAttributes != 0)
		{
			Console.Write(" Additional Attributes: {0:X}", (uint)FileAttributes);
		}

		Console.Write("\n");
	}

	static bool PrintDate(FILETIME Date)
	{
		Console.Write("{0:G}", Date.ToDateTime());
		return true;
	}

	static bool DisplayBasicInfo(HFILE hFile, out bool bIsDirectory)
	{
		FILE_BASIC_INFO basicInfo;
		bIsDirectory = false;

		try
		{
			basicInfo = GetFileInformationByHandleEx<FILE_BASIC_INFO>(hFile, FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo);
		}
		catch (Exception ex)
		{
			Console.Write("Failure fetching basic information: {0}\n", ex.Message);
			return false;
		}

		Console.Write("\n[Basic Information]\n\n");

		Console.Write(" Creation Time: ");

		var result = PrintDate(basicInfo.CreationTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving creation time.\n");
		}

		Console.Write(" Change Time: ");

		result = PrintDate(basicInfo.ChangeTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving creation time.\n");
		}

		Console.Write(" Last Access Time: ");

		result = PrintDate(basicInfo.LastAccessTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving last access time.\n");
		}

		Console.Write(" Last Write Time: ");

		result = PrintDate(basicInfo.LastWriteTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving last write time.\n");
		}

		Console.Write(" File Attributes: ");
		PrintFileAttributes(basicInfo.FileAttributes);

		bIsDirectory = (basicInfo.FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0;

		return result;
	}

	static bool DisplayStandardInfo(HFILE hFile)
	{
		FILE_STANDARD_INFO standardInfo;

		try
		{
			standardInfo = GetFileInformationByHandleEx<FILE_STANDARD_INFO>(hFile, FILE_INFO_BY_HANDLE_CLASS.FileStandardInfo);
		}
		catch (Exception ex)
		{
			Console.Write("Failure fetching standard information: {0}\n", ex.Message);
			return false;
		}

		Console.Write("\n[Standard Information]\n\n");

		Console.Write(" Allocation Size: {0}\n", standardInfo.AllocationSize);
		Console.Write(" End of File: {0}\n", standardInfo.EndOfFile);
		Console.Write(" Number of Links: {0}\n", standardInfo.NumberOfLinks);
		Console.Write(" Delete Pending: ");
		if (standardInfo.DeletePending)
		{
			Console.Write("Yes\n");
		}
		else
		{
			Console.Write("No\n");
		}
		Console.Write(" Directory: ");
		if (standardInfo.Directory)
		{
			Console.Write("Yes\n");
		}
		else
		{
			Console.Write("No\n");
		}

		return true;
	}

	static bool DisplayNameInfo(HFILE hFile)
	{
		try
		{
			var nameInfo = GetFileInformationByHandleEx<FILE_NAME_INFO>(hFile, FILE_INFO_BY_HANDLE_CLASS.FileNameInfo);

			Console.Write("\n[Name Information]\n\n");

			Console.Write(" File Name: {0}\n", nameInfo.FileName);
		}
		catch (Exception ex)
		{
			Console.Write("Failure fetching name information: {0}\n", ex.Message);
			return false;
		}

		return true;
	}

	static bool DisplayStreamInfo(HFILE hFile)
	{
		//
		// Allocate an information structure that is hopefully large enough to
		// retrieve stream information.
		//

		using var streamInfo = new SafeCoTaskMemHandle((uint)Marshal.SizeOf<FILE_STREAM_INFO>() + (sizeof(ushort) * MAX_PATH));

		retry:

		if (streamInfo.IsInvalid)
		{
			SetLastError(Win32Error.ERROR_NOT_ENOUGH_MEMORY);
			return false;
		}

		var result = GetFileInformationByHandleEx(hFile, FILE_INFO_BY_HANDLE_CLASS.FileStreamInfo, streamInfo, streamInfo.Size);

		if (!result)
		{
			//
			// If our buffer wasn't large enough try again with a larger one.
			//
			if (GetLastError() == Win32Error.ERROR_MORE_DATA)
			{
				streamInfo.Size *= 2;
				goto retry;
			}

			Console.Write("Failure fetching stream information: {0}\n", GetLastError());
			return result;
		}

		Console.Write("\n[Stream Information]\n\n");

		foreach (var currentStreamInfo in streamInfo.DangerousGetHandle().LinkedListToIEnum<FILE_STREAM_INFO>(i => i.NextEntryOffset, streamInfo.Size))
		{
			Console.Write(" Stream Name: {0}\n", currentStreamInfo.StreamName);
			Console.Write(" Stream Size: {0}\n", currentStreamInfo.StreamSize);
			Console.Write(" Stream Allocation Size: {0}\n", currentStreamInfo.StreamAllocationSize);
		}

		return true;
	}

	static void PrintDirectoryEntry(in FILE_ID_BOTH_DIR_INFO entry)
	{
		bool result;

		Console.Write("\n {0} ", entry.FileNameLength > 0 ? entry.FileName.Substring(0, (int)entry.FileNameLength / 2) : "");

		if (entry.ShortNameLength > 0)
		{
			Console.Write("[{0}]\n\n", entry.ShortNameLength > 0 ? entry.ShortName.Substring(0, entry.ShortNameLength / 2) : "");
		}
		else
		{
			Console.Write("\n\n");
		}

		Console.Write(" Creation Time: ");

		result = PrintDate(entry.CreationTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving creation time.\n");
		}

		Console.Write(" Change Time: ");

		result = PrintDate(entry.ChangeTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving creation time.\n");
		}

		Console.Write(" Last Access Time: ");

		result = PrintDate(entry.LastAccessTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving last access time.\n");
		}

		Console.Write(" Last Write Time: ");

		result = PrintDate(entry.LastWriteTime);

		if (result)
		{
			Console.Write("\n");
		}
		else
		{
			Console.Write(" Error retrieving last write time.\n");
		}

		Console.Write(" End of File: {0}\n", entry.EndOfFile);
		Console.Write(" Allocation Size: {0}\n", entry.AllocationSize);
		Console.Write(" File Attributes: ");
		PrintFileAttributes(entry.FileAttributes);
		Console.Write(" File ID: {0}\n", entry.FileId);
	}

	static bool DisplayFullDirectoryInfo(HFILE hFile)
	{
		//
		// Allocate an information structure that is hopefully large enough to
		// retrieve at least one directory entry.
		//

		using var dirInfo = new SafeCoTaskMemHandle((uint)Marshal.SizeOf<FILE_ID_BOTH_DIR_INFO>() + (sizeof(ushort) * MAX_PATH));

		//
		// We initially want to start our enumeration from the beginning so we
		// use the restart class.
		//
		var infoClass = FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryRestartInfo;

		retry:

		if (dirInfo.IsInvalid)
		{
			SetLastError(Win32Error.ERROR_NOT_ENOUGH_MEMORY);
			return false;
		}

		for (; ; )
		{
			var result = GetFileInformationByHandleEx(hFile, infoClass, dirInfo, dirInfo.Size);

			if (!result)
			{
				//
				// If our buffer wasn't large enough try again with a larger one.
				//
				if (GetLastError() == Win32Error.ERROR_MORE_DATA)
				{
					dirInfo.Size *= 2;
					goto retry;
				}
				else if (GetLastError() == Win32Error.ERROR_NO_MORE_FILES)
				{
					//
					// Enumeration completed successfully, we simply break out here.
					//
					break;
				}

				//
				// A real error occurred.
				//
				Console.Write("\nFailure fetching directory information: {0}\n", GetLastError());
				return result;
			}

			if (infoClass == FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryRestartInfo)
			{
				Console.Write("\n[Full Directory Information]\n\n");
				infoClass = FILE_INFO_BY_HANDLE_CLASS.FileIdBothDirectoryInfo;
			}

			foreach (var currentDirInfo in dirInfo.DangerousGetHandle().LinkedListToIEnum<FILE_ID_BOTH_DIR_INFO>(i => i.NextEntryOffset, dirInfo.Size))
			{
				PrintDirectoryEntry(currentDirInfo);
			}
		}

		return true;
	}
}