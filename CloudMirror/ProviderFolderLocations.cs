using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;

namespace CloudMirror
{
	internal static class ProviderFolderLocations
	{
		private static string s_clientFolder;

		private static string s_serverFolder;

		public static string GetClientFolder() => s_clientFolder;

		public static string GetServerFolder() => s_serverFolder;

		public static bool Init(string serverFolder = "", string clientFolder = "")
		{
			if (!string.IsNullOrEmpty(serverFolder))
			{
				s_serverFolder = serverFolder;
			}
			if (!string.IsNullOrEmpty(clientFolder))
			{
				s_clientFolder = clientFolder;
			}
			if (string.IsNullOrEmpty(s_serverFolder))
			{
				s_serverFolder = PromptForFolderPath("\"Server in the Fluffy Cloud\" Location");
			}

			if (!string.IsNullOrEmpty(s_serverFolder) && string.IsNullOrEmpty(s_clientFolder))
			{
				s_clientFolder = PromptForFolderPath("\"Syncroot (Client)\" Location");
			}

			var result = false;
			if (!string.IsNullOrEmpty(s_serverFolder) && !string.IsNullOrEmpty(s_clientFolder))
			{
				// In case they were passed in params we may need to create the folder. If the folder is already there then these are benign calls.
				CreateDirectory(s_serverFolder);
				CreateDirectory(s_clientFolder);
				result = true;
			}
			return result;
		}

		private static string PromptForFolderPath(string title)
		{
			var fileOpen = ComReleaserFactory.Create(new IFileOpenDialog());
			fileOpen.Item.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
			fileOpen.Item.SetTitle(title);

			// Restore last location used
			var settings = Windows.Storage.ApplicationData.Current.LocalSettings.Values;
			if (settings.TryGetValue(title, out var lastLocation))
			{
				var lastItem = SHCreateItemFromParsingName<IShellItem>(lastLocation?.ToString());
				fileOpen.Item.SetFolder(lastItem);
			}

			var hr = fileOpen.Item.Show();
			if (hr == (HRESULT)Win32Error.ERROR_CANCELLED)
				return null;
			hr.ThrowIfFailed();

			using var item = ComReleaserFactory.Create(fileOpen.Item.GetResult());
			using var path = item.Item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

			// Save the last location
			settings[title] = (string)path;

			return path;
		}
	}
}