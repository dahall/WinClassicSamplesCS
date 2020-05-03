using System;

namespace CloudMirror
{
	class Program
	{
		[STAThread]
		static int Main(string[] args)
		{
			Console.Write("Press ctrl-C to stop gracefully\n");
			Console.Write("-------------------------------\n");

			var returnCode = 0;

			try
			{
				if (FakeCloudProvider.Start(args.Length > 0 ? args[0] : null, args.Length > 1 ? args[1] : null).Result)
				{
					returnCode = 1;
				}
			}
			catch
			{
				CloudProviderSyncRootWatcher.Stop(0); // Param is unused
			}

			return returnCode;
		}
	}
}
