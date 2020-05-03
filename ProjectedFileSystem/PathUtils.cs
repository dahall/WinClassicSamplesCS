using System.IO;

namespace ProjectedFileSystem
{
	internal static class PathUtils
	{
		// Returns true if the given path is for the virtualization root. The path must be expressed relative to the virtualization root
		public static bool IsVirtualizationRoot(string filePathName) => string.IsNullOrEmpty(filePathName) || filePathName.StartsWith("\\");

		// Returns the last component and the parent path for the given path. Example:
		//
		// string parentPath; string fileName; fileName = GetLastComponent(L"foo\bar\a.txt", parentPath);
		//
		// Result:
		//
		// parentPath: "foo\bar"
		// fileName: "a.txt"
		private static string GetLastComponent(string path, out string parentPath)
		{
			parentPath = Path.GetDirectoryName(path) ?? "\\";
			return Path.GetFileName(path);
		}
	}
}