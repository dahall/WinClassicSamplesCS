using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;

namespace CloudMirror;

internal static class ProviderFolderLocations
{
	public static string ClientFolder { get; private set; } = "";

	public static string ServerFolder { get; private set; } = "";

	public static bool Init(string serverFolder = "", string clientFolder = "")
	{
		if (!string.IsNullOrEmpty(serverFolder))
		{
			ServerFolder = serverFolder;
		}
		if (!string.IsNullOrEmpty(clientFolder))
		{
			ClientFolder = clientFolder;
		}
		if (string.IsNullOrEmpty(ServerFolder))
		{
			ServerFolder = PromptForFolderPath("\"Server in the Fluffy Cloud\" Location");
		}

		if (!string.IsNullOrEmpty(ServerFolder) && string.IsNullOrEmpty(ClientFolder))
		{
			ClientFolder = PromptForFolderPath("\"Syncroot (Client)\" Location");
		}

		var result = false;
		if (!string.IsNullOrEmpty(ServerFolder) && !string.IsNullOrEmpty(ClientFolder))
		{
			// In case they were passed in params we may need to create the folder. If the folder is already there then these are benign calls.
			CreateDirectory(ServerFolder);
			CreateDirectory(ClientFolder);
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
			return "";
		hr.ThrowIfFailed();

		using var item = ComReleaserFactory.Create(fileOpen.Item.GetResult());
		var path = item.Item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

		// Save the last location
		settings[title] = path;

		return path;
	}
}