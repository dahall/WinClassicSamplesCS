using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Storage.Provider;

namespace CloudMirror
{
	[ComVisible(true), Guid("97961bcb-601c-4950-927c-43b9319c7217")]
	public class UriSource : IStorageProviderUriSource
	{
		public void GetContentInfoForPath(string path, StorageProviderGetContentInfoForPathResult result)
		{
			result.Status = StorageProviderUriSourceStatus.FileNotFound;

			var fileName = Path.GetFileName(path);
			result.ContentId = "http://cloudmirror.example.com/contentId/" + fileName;
			result.ContentUri = "http://cloudmirror.example.com/contentUri/" + fileName + "?StorageProviderId=TestStorageProvider";
			result.Status = StorageProviderUriSourceStatus.Success;
		}

		public void GetPathForContentUri(string contentUri, StorageProviderGetPathForContentUriResult result)
		{
			result.Status = StorageProviderUriSourceStatus.FileNotFound;

			const string prefix = "http://cloudmirror.example.com/contentUri/";
			var uri = contentUri;
			if (uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			{
				var localPath = ProviderFolderLocations.GetClientFolder() + "\\" + uri[prefix.Length..uri.IndexOf('?')];

				if (File.Exists(localPath))
				{
					result.Path = localPath;
					result.Status = StorageProviderUriSourceStatus.Success;
				}
			}
		}
	}
}