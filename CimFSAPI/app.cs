using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.CimFs;
using static Vanara.PInvoke.Kernel32;

namespace CimFSAPI;

static class Program
{
	static int Main(string[] args)
	{
		if (args.Length != 4)
		{
			Console.Error.WriteLine($"Usage: CimFSAPI.exe <cim_path> <image_name> <file_to_add_path> <image_file_path>");
			return 1;
		}

		string cimPath = args[0], imageName = args[1], filePath = args[2], imageRelativePath = args[3];

		try
		{
			TogglePrivileges(new[] { "SeSecurityPrivilege", "SeBackupPrivilege" }, true);

			// Create a new image and add a file
			AddFileToNewCim(cimPath, imageName, filePath, imageRelativePath);

			CompareFileWithCimFile(cimPath, imageName, imageRelativePath, filePath);

			var attributes = GetFileAttributes(filePath);
			string imageHardLinkPath = "link";

			if ((attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0)
			{
				// Extend the existing image by adding a hardlink
				AddHardLinkInCim(cimPath, imageName, imageRelativePath, imageHardLinkPath);

				ValidateHardLinkInCim(cimPath, imageName, imageRelativePath, imageHardLinkPath);

				CompareFileWithCimFile(cimPath, imageName, imageHardLinkPath, filePath);
			}

			string forkImageName = imageName + "fork";

			// Create a fork of the image where the path has been deleted
			DeletePathFromCimFork(cimPath, imageName, imageRelativePath, forkImageName);

			if (TestFileExistsInCim(cimPath, forkImageName, imageRelativePath))
			{
				Console.Error.WriteLine($"The file {imageRelativePath} should have been removed in image {forkImageName}");
			}

			if ((attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0)
			{
				// check the link still exists in the base image
				if (!TestFileExistsInCim(cimPath, imageName, imageHardLinkPath))
				{
					Console.Error.WriteLine($"The file {imageHardLinkPath} should not have been removed in image {imageName}");
				}
			}

			return 0;
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine(ex.Message);
			System.IO.File.Delete(System.IO.Path.Combine(cimPath, imageName));
			return ex.HResult;
		}
	}

	//
	// Routine Description:
	// Copies the contents of an open file to a Writer.
	//
	// Parameters:
	// streamHandle - Cim image writer handle, obtained by a call to CimAddFile.
	//
	// file - Opened handle to the file to copy data from.
	//
	static void CopyFileContentsToCim([In] CIMFS_STREAM_HANDLE streamHandle, [In] HFILE file)
	{
		Console.WriteLine("Copying data from file / stream");

		byte[] buffer = new byte[65536];

		for (; ; )
		{
			Win32Error.ThrowLastErrorIfFalse(ReadFile(file, buffer, (uint)buffer.Length, out var read));

			if (read == 0)
			{
				break;
			}

			Console.WriteLine($"\tRead {read} bytes, writing in image's stream ...");

			CimWriteStream(streamHandle, buffer, read).ThrowIfFailed();
		}
	}

	//
	// Routine Description:
	// Gets the list of alternate data streams for an open handle.
	//
	// Parameters:
	// filePath - Path to the file to get the data from.
	//
	static IEnumerable<WIN32_FIND_STREAM_DATA> GetAlternateDataStreams([In] string filePath) => EnumFileStreams(filePath);

	//
	// Routine Description:
	// Gets the meta data, attributes and stream information from
	// an existing file that will be copied to a Cim image.
	//
	// Parameters:
	// filePath - Path to the file to get the data from.
	//
	// fileData - Structure that will hold the required attributes and stream info.
	//
	static CimFileData GetFileData([In] string filePath)
	{
		Console.WriteLine("Getting data for file: " + filePath);

		// Open the file, ensure in case of a symlink we open the symlink itself
		var file = Win32Error.ThrowLastErrorIfInvalid(CreateFile(filePath, FileAccess.GENERIC_READ | FileAccess.FILE_READ_ATTRIBUTES | (FileAccess)0x01000000, System.IO.FileShare.Read,
			null, System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS | FileFlagsAndAttributes.FILE_FLAG_OPEN_REPARSE_POINT));

		// Get the in formation we need from the source file that is required when
		// adding a file to the image

		using SafeCoTaskMemStruct<FILE_ID_INFO> fileID = new();

		Win32Error.ThrowLastErrorIfFalse(GetFileInformationByHandleEx(file, FILE_INFO_BY_HANDLE_CLASS.FileIdInfo, fileID, fileID.Size));

		using SafeCoTaskMemStruct<FILE_BASIC_INFO> basicInfo = new();

		Win32Error.ThrowLastErrorIfFalse(GetFileInformationByHandleEx(file, FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, basicInfo, basicInfo.Size));

		CimFileData fileData = new(file, basicInfo.Value, fileID.Value);

		if ((basicInfo.Value.FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
		{
			Console.WriteLine("\t\tFile is a reparse point, getting reparse info");

			const int MAXIMUM_REPARSE_DATA_BUFFER_SIZE = 16 * 1024;
			fileData.ReparsePointData = new SafeCoTaskMemHandle(MAXIMUM_REPARSE_DATA_BUFFER_SIZE);

			Win32Error.ThrowLastErrorIfFalse(DeviceIoControl(file, IOControlCode.FSCTL_GET_REPARSE_POINT, default, 0U, fileData.ReparsePointData, fileData.ReparsePointData.Size, out var bytes));

			fileData.MetaData.ReparseDataBuffer = fileData.ReparsePointData;
			fileData.MetaData.ReparseDataSize = bytes;
		}

		if ((basicInfo.Value.FileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0)
		{
			fileData.MetaData.FileSize = 0;
		}
		else
		{
			Win32Error.ThrowLastErrorIfFalse(GetFileSizeEx(file, out var fileSize));
			fileData.MetaData.FileSize = fileSize;
		}

		// Retrieve the security descriptor.
		var secInfo = SECURITY_INFORMATION.DACL_SECURITY_INFORMATION | SECURITY_INFORMATION.LABEL_SECURITY_INFORMATION |
			SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION | SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION |
			SECURITY_INFORMATION.SACL_SECURITY_INFORMATION;

		Win32Error.ThrowIfFailed(GetSecurityInfo(file.DangerousGetHandle(), SE_OBJECT_TYPE.SE_FILE_OBJECT, secInfo, out _, out _, out _, out _, out var sd));

		fileData.MetaData.SecurityDescriptorBuffer = sd;
		fileData.MetaData.SecurityDescriptorSize = GetSecurityDescriptorLength(sd);
		fileData.Sd = sd;

		// Retrieve Alternate streams info
		fileData.StreamData = GetAlternateDataStreams(filePath).Where(d => d.cStreamName != "::$DATA").ToList();

		// Finally keep the handle used to retrieve the information
		fileData.FileHandle = file;

		return fileData;
	}

	//
	// Routine Description:
	// Retrieves the information of an existing file and writes it into
	// the Cim Image.
	//
	// Parameters:
	// cimHandle - Opened handle to the Cim Image by calling CimCreateImage.
	//
	// filePath - Source Path in the local filesystem to copy the data from.
	//
	// imageRelativePath - Destination path in the CIM Image
	//
	// fileAttributes - Attributes of the copied file
	//
	static void WriteFileEntry([In] CIMFS_IMAGE_HANDLE cimHandle, [In] string filePath, [In] string imageRelativePath, [Out] out FileFlagsAndAttributes fileAttributes)
	{
		Console.WriteLine($"Adding file:{filePath} as {imageRelativePath} in image");

		// Get the information we need from the source file
		var cimFileData = GetFileData(filePath);

		CimCreateFile(cimHandle, imageRelativePath, cimFileData.MetaData, out var streamHandle).ThrowIfFailed();

		using (streamHandle)
		{
			// Write the payload data.
			if (cimFileData.MetaData.FileSize > 0)
			{
				CopyFileContentsToCim(streamHandle, cimFileData.FileHandle);
			}
		}

		// Write alternate data streams.

		if (cimFileData.StreamData.Count > 0)
		{

			foreach (WIN32_FIND_STREAM_DATA streamData in cimFileData.StreamData)
			{

				// stream.Name is of the format ":name:$TYPE" no need to check for $DATA as we skip it when collecting data for alternate data streams
				var end = streamData.cStreamName.IndexOf(':', 1);
				if (end != -1)
				{
					var streamName = filePath + streamData.cStreamName.Substring(0, end);

					using var stream = Win32Error.ThrowLastErrorIfInvalid(CreateFile(streamName, FileAccess.GENERIC_READ, System.IO.FileShare.Read, null, System.IO.FileMode.Open, 0));

					// This time we're not creating a new file
					// but adding a stream
					HRESULT.ThrowIfFailed(CimCreateAlternateStream(cimHandle, streamName, (ulong)streamData.StreamSize, out var alternateStreamHandle));

					if (streamData.StreamSize > 0)
					{
						CopyFileContentsToCim(alternateStreamHandle, stream);
					}
				}
			}
		}

		fileAttributes = cimFileData.MetaData.Attributes;
	}

	//
	// Routine Description:
	// Copies the content of an existing file into a new Cim.
	//
	// Parameters:
	// cimPath - Path to a directory to contain the CIM image.
	// For example: C:\MyCimImage
	//
	// imageName - Name of the image. The image should not already exist
	// cimPath.
	// For example: image0.cim
	//
	// filePath - Source Path in the local filesystem to copy the data from.
	// For example: C:\dir\file1.txt
	//
	// imageRelativePath - Destination path relative to the root in the CIM Image.
	// For example: dir\file1.txt
	//
	static void AddFileToNewCim([In] string cimPath, [In] string imageName, [In] string filePath, [In] string imageRelativePath)
	{
		Console.WriteLine($"Creating new image {imageName} in directory {cimPath}");

		var hr = CimCreateImage(cimPath, null, imageName, out var imageHandle);

		using (imageHandle)
		{
			if (hr == (HRESULT)(Win32Error)Win32Error.ERROR_FILE_EXISTS)
			{
				Console.WriteLine($"ERROR: image {imageName} already exists in directory {cimPath}");
			}

			hr.ThrowIfFailed();

			WriteFileEntry(imageHandle, filePath, imageRelativePath, out var fileAttributes);

			if ((fileAttributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) != 0)
			{
				Console.Write("Directory " + filePath + " added as an empty directory in CIM Image as ");
				Console.WriteLine(imageRelativePath + "(add files separately if desired)");
			}

			// Commit changes to a new CIM
			HRESULT.ThrowIfFailed(CimCommitImage(imageHandle));
		}
	}

	//
	// Routine Description:
	// Adds a Hard Link entry in the CIM image to an existing file in the Cim Image.
	//
	// Parameters:
	// cimPath - Path to a CIM image directory.
	//
	// imageName - Name of the existing image the file will be added to.
	//
	// existingImageRelativePath - An existing Path in the CIM image that the hardlink will point to.
	//
	// imageRelativePath - Name of the hardlink
	//
	static void AddHardLinkInCim([In] string cimPath, [In] string imageName, [In] string existingImageRelativePath, [In] string imageRelativePath)
	{
		Console.WriteLine("Opening image " + imageName + " in directory " + cimPath + " to add a hardlink ");

		// extend the parent CIM
		HRESULT.ThrowIfFailed(CimCreateImage(cimPath, imageName, imageName, out var imageHandle));

		Console.WriteLine($"Adding HardLink {imageRelativePath} . {existingImageRelativePath}");

		HRESULT hr = CimCreateHardLink(imageHandle, imageRelativePath, existingImageRelativePath);

		if (hr.Failed)
		{
			// Can't create a hardlink to a directory
			if (hr == HRESULT.E_ACCESSDENIED)
			{
				Console.WriteLine("Error, can't add hardlink to a directory");
			}
			else
			{
				hr.ThrowIfFailed();
			}
		}
		else
		{
			HRESULT.ThrowIfFailed(CimCommitImage(imageHandle));
		}
	}

	//
	// Routine Description:
	// Deletes a file or directory from and existing image creating a fork of the image
	//
	// Parameters:
	// cimPath - Path to a CIM image directory.
	//
	// imageName - Name of the existing image the file will be deleted from.
	//
	// imageRelativePath - An existing path in the CIM image that will be deleted.
	//
	// forkedImageName - Name of the image fork to be created.
	//
	static void DeletePathFromCimFork([In] string cimPath, [In] string imageName, [In] string imageRelativePath, [In] string forkedImageName)
	{
		Console.WriteLine("Opening image " + imageName + " in directory " + cimPath + " to delete a file ");

		var hr = CimCreateImage(cimPath, imageName, forkedImageName, out var imageHandle);

		if (hr == (Win32Error)Win32Error.ERROR_FILE_EXISTS)
		{
			Console.WriteLine($"ERROR: image {forkedImageName} already exists in directory {cimPath}");
		}
		hr.ThrowIfFailed();

		Console.WriteLine("Deleting " + imageRelativePath + " from image");

		HRESULT.ThrowIfFailed(CimDeletePath(imageHandle, imageRelativePath));

		// Fork the parent CIM by specifying a new name in the commit

		HRESULT.ThrowIfFailed(CimCommitImage(imageHandle));

		return;
	}

	//
	// Routine Description:
	// Mounts an existing Cim image and returns the path the image was mounted to.
	//
	// Parameters:
	// cimPath - Path to a CIM image, the image has been created already.
	//
	// imageName - Name of the image.
	//
	static MountedCimInformation MountImage([In] string cimPath, [In] string imageName)
	{
		var uuid = Guid.NewGuid();

		HRESULT.ThrowIfFailed(CimMountImage(cimPath, imageName, CIM_MOUNT_IMAGE_FLAGS.CIM_MOUNT_IMAGE_NONE, uuid));

		return new MountedCimInformation() { VolumeId = uuid, VolumeRootPath = $@"\\?\Volume{uuid:B}\" };
	}

	static void DismountImage([In] in Guid volumeId) => HRESULT.ThrowIfFailed(CimDismountImage(volumeId));

	//
	// Routine Description:
	// A basic routine that compares the contents of 2 streams.
	// This routine assumes both paths are normal files (no directories or reparse points)
	//
	// Parameters:
	// source - Path to a source file.
	//
	// target - Path to a target file.
	//
	// Returns:
	// bool, true if both streams have the same content.
	//
	static bool CompareStreams([In] string source, [In] string target)
	{
		byte[] sourceBuffer = new byte[4096];
		byte[] targetBuffer = new byte[4096];

		using var sourceHandle = Win32Error.ThrowLastErrorIfInvalid(CreateFile(source, FileAccess.GENERIC_READ, System.IO.FileShare.Read,
			null, System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS));

		using var targetHandle = Win32Error.ThrowLastErrorIfInvalid(CreateFile(target, FileAccess.GENERIC_READ, System.IO.FileShare.Read,
			null, System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS));

		for (; ; )
		{
			Win32Error.ThrowLastErrorIfFalse(ReadFile(sourceHandle, sourceBuffer, (uint)sourceBuffer.Length, out var sourceRead));

			Win32Error.ThrowLastErrorIfFalse(ReadFile(targetHandle, targetBuffer, (uint)targetBuffer.Length, out var targetRead));

			if (targetRead != sourceRead)
			{
				return false;
			}

			if (sourceRead == 0)
			{
				break;
			}

			if (Interop.memcmp(sourceBuffer, targetBuffer, sourceRead) != 0)
			{
				Console.Error.WriteLine("\tContents do not match");
				return false;
			}
		}

		return true;
	}

	//
	// Routine Description:
	// Compares the content of a file in the local filesystem against a
	// file in a target CIM image as well as some basic attributes.
	//
	// Parameters:
	// cimPath - Path to a CIM image directory.
	//
	// imageName - Name of the image that will be mounted and used to compare.
	//
	// imageRelativePath - Destination path in the CIM Image to compare
	//
	// filePath - Source Path in the local filesystem to compare.
	//
	static void CompareFileWithCimFile([In] string cimPath, [In] string imageName, [In] string imageRelativePath, [In] string filePath)
	{
		MountedCimInformation volumeInfo = MountImage(cimPath, imageName);
		try
		{
			var cimFilePath = volumeInfo.VolumeRootPath + imageRelativePath;

			CimFileData fileData = GetFileData(filePath);
			CimFileData cimFileData = GetFileData(cimFilePath);

			// start by comparing attributes
			if (fileData.MetaData.Attributes != cimFileData.MetaData.Attributes)
			{
				Console.Error.WriteLine("Attributes do not match");
			}

			// if file is a reparse point compare reparse buffer
			if ((fileData.MetaData.Attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_REPARSE_POINT) != 0)
			{
				if (fileData.MetaData.ReparseDataSize != cimFileData.MetaData.ReparseDataSize)
				{
					Console.Error.WriteLine("\tReparse buffer sizes do not match");
				}

				if (Interop.memcmp(fileData.MetaData.ReparseDataBuffer, cimFileData.MetaData.ReparseDataBuffer, (SizeT)fileData.MetaData.ReparseDataSize) != 0)
				{
					Console.Error.WriteLine("\tReparse buffer contents do not match");
				}
			}

			// if file is not a directory compare file contents
			if ((fileData.MetaData.Attributes & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0)
			{
				if (fileData.MetaData.FileSize != cimFileData.MetaData.FileSize)
				{
					Console.Error.WriteLine("File sizes do not match");
				}

				if (fileData.MetaData.CreationTime.ToInt64() != cimFileData.MetaData.CreationTime.ToInt64())
				{
					Console.Error.WriteLine("Creation times do not match");
				}

				// Compare file contents

				if (!CompareStreams(filePath, cimFilePath))
				{
					Console.Error.WriteLine("Content does not match");
				}
			}
		}
		finally
		{
			DismountImage(volumeInfo.VolumeId);
		}
	}

	//
	// Routine Description:
	// Validates if linkPath is a hardlink of filePath.
	//
	// Parameters:
	// cimPath - Path to a CIM image, the image has been created already.
	//
	// imageName - Name of the image that will be mounted and used to compare.
	//
	// existingImagePath - An existing Path in the CIM image that the hardlink poins to.
	//
	// imageLinkPath - An existing Path in the im age previously added via CimAddLink
	//
	static void ValidateHardLinkInCim([In] string cimPath, [In] string imageName, [In] string existingImagePath, [In] string imageLinkPath)
	{
		MountedCimInformation volumeInfo = MountImage(cimPath, imageName);
		try
		{
			var cimFilePath = volumeInfo.VolumeRootPath + existingImagePath;
			var cimLinkPath = volumeInfo.VolumeRootPath + imageLinkPath;

			CimFileData cimFileData = GetFileData(cimFilePath);
			CimFileData cimLinkData = GetFileData(cimLinkPath);

			if (Interop.memcmp(cimFileData.FileIdInfo.FileId.Identifier, cimLinkData.FileIdInfo.FileId.Identifier, Marshal.SizeOf(typeof(FILE_ID_128))) != 0)
			{
				Console.Error.WriteLine("did not match");
			}
		}
		finally
		{
			DismountImage(volumeInfo.VolumeId);
		}
	}

	// Routine Description:
	// Checks if the file exists or not in the provided image.
	//
	// Parameters:
	// cimPath - Path to a CIM image.
	//
	// imageName - Name of the image that will be mounted and used to check
	//
	// imageRelativePath - Relative path from the root in the CIM image
	//
	static bool TestFileExistsInCim([In] string cimPath, [In] string imageName, [In] string imageRelativePath)
	{
		MountedCimInformation volumeInfo = MountImage(cimPath, imageName);
		try
		{
			string cimImageFilePath = volumeInfo.VolumeRootPath + imageRelativePath;
			var attributes = GetFileAttributes(cimImageFilePath);
			if (attributes == (FileFlagsAndAttributes)uint.MaxValue)
				Win32Error.ThrowLastErrorUnless(Win32Error.ERROR_FILE_NOT_FOUND);

			return (attributes != (FileFlagsAndAttributes)uint.MaxValue);
		}
		finally
		{
			DismountImage(volumeInfo.VolumeId);
		}
	}

	//
	// Routine Description:
	// Attempts to enable or disable a given privilege. Returns the previous state for the privilege.
	//
	static bool TogglePrivileges(string[] privilegeNames, bool enable)
	{
		using SafeHTOKEN token = SafeHTOKEN.FromProcess(GetCurrentProcess(), TokenAccess.TOKEN_ADJUST_PRIVILEGES | TokenAccess.TOKEN_QUERY);
		var newPriv = new TOKEN_PRIVILEGES(Array.ConvertAll(privilegeNames, s => new LUID_AND_ATTRIBUTES(LUID.FromName(s), enable ? PrivilegeAttributes.SE_PRIVILEGE_ENABLED : 0)));
		AdjustTokenPrivileges(token, false, newPriv, out var prev).ThrowIfFailed();

		return prev.PrivilegeCount == privilegeNames.Length && prev.Privileges.All(p => (p.Attributes & (PrivilegeAttributes.SE_PRIVILEGE_ENABLED | PrivilegeAttributes.SE_PRIVILEGE_ENABLED_BY_DEFAULT)) != 0);
	}

	struct MountedCimInformation
	{
		// Guid used to mount the volume
		public Guid VolumeId;

		// In the form of \\?\Volume{VolumeId}\ 
		public string VolumeRootPath;
	}

	static class Interop
	{
		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int memcmp(IntPtr b1, IntPtr b2, SizeT count);

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int memcmp(byte[] b1, byte[] b2, SizeT count);
	}

	class CimFileData
	{
		// The handle used to get the information of the file
		public SafeHFILE FileHandle;

		public FILE_ID_INFO FileIdInfo;

		// A copy from filesystem's attributes and metadata that Cimfs
		// needs when creating a file in the image
		public CIMFS_FILE_METADATA MetaData;

		// buffer for reparse point data
		public SafeAllocatedMemoryHandle ReparsePointData;

		// While getting the data we also need to keep the sd alive
		public SafePSECURITY_DESCRIPTOR Sd;

		// Alternate streams information
		public List<WIN32_FIND_STREAM_DATA> StreamData;

		public CimFileData(SafeHFILE hf, in FILE_BASIC_INFO fi, in FILE_ID_INFO fid)
		{
			FileHandle = hf;
			MetaData = new() { Attributes = fi.FileAttributes, CreationTime = fi.CreationTime, ChangeTime = fi.ChangeTime, LastWriteTime = fi.LastWriteTime, LastAccessTime = fi.LastAccessTime };
			FileIdInfo = fid;
		}
	}
}