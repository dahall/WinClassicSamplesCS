using System;
using System.IO;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace CloudMirror
{
	static class Placeholders
	{
		public static void Create(string sourcePathStr, string sourceSubDirStr, string destPath)
		{
			try
			{
				WIN32_FIND_DATA findData;

				// Ensure that the source path ends in a backslash.
				// Ensure that a nonempty subdirectory ends in a backslash.
				var fileName = Path.Combine(sourcePathStr, sourceSubDirStr, "*");

				using var hFileHandle = FindFirstFileEx(fileName, FINDEX_INFO_LEVELS.FindExInfoStandard, out findData, FINDEX_SEARCH_OPS.FindExSearchNameMatch, default, FIND_FIRST.FIND_FIRST_EX_ON_DISK_ENTRIES_ONLY);
				if (!hFileHandle.IsInvalid)
				{
					do
					{
						if (findData.cFileName == "." || findData.cFileName == ".." || (findData.dwFileAttributes & FileAttributes.Hidden) != 0)
						{
							continue;
						}

						var relativeName = Path.Combine(sourceSubDirStr, findData.cFileName);
						using var pRelativeName = new SafeCoTaskMemString(relativeName);
						var cloudEntry = new CF_PLACEHOLDER_CREATE_INFO
						{
							FileIdentity = pRelativeName,
							FileIdentityLength = pRelativeName.Size,
							RelativeFileName = relativeName,
							Flags = CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_MARK_IN_SYNC,
							FsMetadata = new CF_FS_METADATA
							{
								FileSize = Macros.MAKELONG64(findData.nFileSizeLow, unchecked((int)findData.nFileSizeHigh)),
								BasicInfo = new FILE_BASIC_INFO
								{
									FileAttributes = (FileFlagsAndAttributes)findData.dwFileAttributes,
									CreationTime = findData.ftCreationTime,
									LastWriteTime = findData.ftLastWriteTime,
									LastAccessTime = findData.ftLastAccessTime,
									ChangeTime = findData.ftLastWriteTime,
								}
							}
						};

						if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
						{
							cloudEntry.Flags |= CF_PLACEHOLDER_CREATE_FLAGS.CF_PLACEHOLDER_CREATE_FLAG_DISABLE_ON_DEMAND_POPULATION;
							cloudEntry.FsMetadata.FileSize = 0;
						}

						try
						{
							Console.Write("Creating placeholder for {0}\n", relativeName);
							CfCreatePlaceholders(destPath, new[] { cloudEntry }, 1, CF_CREATE_FLAGS.CF_CREATE_FLAG_NONE, out _).ThrowIfFailed();
						}
						catch (Exception ex)
						{
							// to_hresult() will eat the exception if it is a result of check_hresult,
							// otherwise the exception will get rethrown and this method will crash out as it should
							Console.Write("Failed to create placeholder for {0} with {1:X8}\n", relativeName, ex.HResult);
							// Eating it here lets other files still get a chance. Not worth crashing the sample, but
							// certainly noteworthy for production code
							continue;
						}

						try
						{
							var prop = new StorageProviderItemProperty
							{
								Id = 1,
								Value = "Value1",
								// This icon is just for the sample. You should provide your own branded icon here
								IconResource = "shell32.dll,-44"
							};

							Console.Write("Applying custom state for {0}\n", relativeName);
							Utilities.ApplyCustomStateToPlaceholderFile(destPath, relativeName, prop);

							if ((findData.dwFileAttributes & FileAttributes.Directory) != 0)
							{
								Create(sourcePathStr, relativeName, destPath);
							}
						}
						catch (Exception ex)
						{
							// to_hresult() will eat the exception if it is a result of check_hresult,
							// otherwise the exception will get rethrown and this method will crash out as it should
							Console.Write("Failed to set custom state on {0} with {1:X8}\n", relativeName, ex.HResult);
							// Eating it here lets other files still get a chance. Not worth crashing the sample, but
							// certainly noteworthy for production code
						}

					} while (FindNextFile(hFileHandle, out findData));
				}
			}
			catch (Exception ex)
			{
				Console.Write("Could not create cloud file placeholders in the sync root with {0:X8}\n", ex.HResult);
				// Something weird enough happened that this is worth crashing out
				throw;
			}
		}
	}
}