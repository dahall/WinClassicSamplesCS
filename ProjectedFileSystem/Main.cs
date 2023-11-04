using Vanara.InteropServices;
using static Vanara.PInvoke.ProjectedFSLib;

namespace ProjectedFileSystem
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length != 1)
			{
				Console.Write("Usage: \n");
				Console.Write("> regfs.exe <Virtualization Root Path> \n");

				return -1;
			}

			// args[1] should be the path to the virtualization root.
			string rootPath = args[0];

			// Specify the notifications that we want ProjFS to send to us.  Everywhere under the virtualization
			// root we want ProjFS to tell us when files have been opened, when they're about to be renamed,
			// and when they're about to be deleted.
			PRJ_NOTIFICATION_MAPPING[] notificationMappings = new[] { new PRJ_NOTIFICATION_MAPPING {
				NotificationRoot = "",
				NotificationBitMask = PRJ_NOTIFY_TYPES.PRJ_NOTIFY_FILE_OPENED | PRJ_NOTIFY_TYPES.PRJ_NOTIFY_PRE_RENAME | PRJ_NOTIFY_TYPES.PRJ_NOTIFY_PRE_DELETE
			} };

			// Store the notification mapping we set up into a start options structure.  We leave all the
			// other options at their defaults.
			using var mem = SafeCoTaskMemHandle.CreateFromList(notificationMappings);
			PRJ_STARTVIRTUALIZING_OPTIONS opts = new()
			{
				NotificationMappings = mem,
				NotificationMappingsCount = 1
			};

			// Start the provider using the options we set up.
			RegfsProvider provider = new();
			var hr = provider.Start(rootPath, opts);
			if (hr.Failed)
			{
				Console.Write("Failed to start virtualization instance: 0x{0:X8}\n", hr);
				return -1;
			}

			Console.Write("RegFS is running at virtualization root [{0}]\n", rootPath);
			Console.Write("Press Enter to stop the provider...");

			Console.ReadKey();

			provider.Stop();

			return 0;
		}
	}
}
