using Vanara.IO;

using static Vanara.PInvoke.NetApi32;

namespace PEERCACHING;

internal class Program
{
	public static void Main(string[] args)
	{
		// BITS requires that you run in a domain for peercaching to be used. This will be important in the following examples.
		if (!InDomain())
		{
			Console.Write("Peer Caching is enabled only in a domain environment\n");
			return;
		}

		if (args.Length != 2)
		{
			Console.Write("Usage: PEERCACHING.exe [remote name] [local name]\n");
			return;
		}

		try
		{
			var ulCount = BackgroundCopyManager.PeerCacheAdministration.Peers.Count;
			Console.Write("Peers count: {0}\n", ulCount);
			foreach (CachePeer pPeer in BackgroundCopyManager.PeerCacheAdministration.Peers)
			{
				Console.Write("Neighbor: {0}\n", pPeer.Name);
			}

			// Create a Job
			Console.Write("Creating Job...\n");
			BackgroundCopyJob pJob = BackgroundCopyManager.Jobs.Add("P2PSample", "", BackgroundCopyJobType.Download);
			pJob.AutoCompleteOnSuccess = true;

			// Set the File Completed Call
			AutoResetEvent evt = new(false);
			pJob.Completed += (s, e) => { Console.WriteLine("Job transferred. Completing Job..."); evt.Set(); };
			pJob.Error += (s, e) => { Console.WriteLine($"Job entered error state...\nJob {pJob.DisplayName} encountered the following error: {pJob.LastError.Message}"); evt.Set(); };
			pJob.FileTransferred += (s, e) =>
			{
				Console.WriteLine($"Temporary location of the downloaded file: {e.FileInfo.TemporaryName}\nIs this a valid file?");
				while (true)
				{
					if (GetYesNoAnswer(out var b))
						e.FileInfo.IsFileContentValid = b;
				}
			};

			// Add a File
			Console.Write("Adding File to Job\n");
			pJob.Files.Add(args[0], args[1]);

			// Enable local caching for this job
			BackgroundCopyManager.PeerCacheAdministration.ConfigurationFlags = PeerCaching.EnableClient | PeerCaching.EnableServer;
			pJob.PeerCachingEnablment = BackgroundCopyJobEnablePeerCaching.EnableServer | BackgroundCopyJobEnablePeerCaching.EnableClient;

			// Say it's ok to get it elsewhere, but tell us later
			pJob.SecurityOptions = BackgroundCopyJobSecurity.AllowReportedRedirect;

			//Resume the job
			Console.Write("Resuming Job...\n");
			pJob.Resume();

			// Wait for QuitMessage from CallBack
			evt.WaitOne(TimeSpan.FromMinutes(15));
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failure at {new System.Diagnostics.StackTrace(ex).GetFrame(0).GetMethod().Name}: {ex.Message}");
		}
	}

	private static bool GetYesNoAnswer(out bool b)
	{
		Console.Write("Please enter yes or no:\n");
		var ans = Console.ReadLine();

		b = false;
		if (0 == string.Compare(ans, "YES", true))
		{
			b = true;
			return true;
		}
		else if (0 == string.Compare(ans, "NO", true))
		{
			return true;
		}

		return false;
	}

	private static bool InDomain()
	{
		// get the computer name and domain
		NetGetJoinInformation(default, out _, out var status).ThrowIfFailed();

		return status == NETSETUP_JOIN_STATUS.NetSetupDomainName;
	}
}