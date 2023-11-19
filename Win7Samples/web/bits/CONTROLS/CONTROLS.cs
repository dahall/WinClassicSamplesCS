using Vanara.IO;

if (args.Length != 2)
{
	Console.Write("Usage: CONTROLS.exe [remote name] [local name]\n");
	return;
}

try
{
	// Create a Job
	BackgroundCopyJob pJob = BackgroundCopyManager.Jobs.Add("MyJobName", "", BackgroundCopyJobType.Download);

	// Add a File
	pJob.Files.Add(args[0], args[1]);

	// Enable local caching for this job
	BackgroundCopyManager.PeerCacheAdministration.ConfigurationFlags = PeerCaching.EnableClient | PeerCaching.EnableServer;

	// Set Max Cache Size to 2% of disk
	BackgroundCopyManager.PeerCacheAdministration.MaximumCacheSize = 2;

	//Resume the job
	AutoResetEvent evt = new(false);
	pJob.Completed += (s, e) => evt.Set();
	pJob.Error += (s, e) => evt.Set();
	pJob.Resume();

	//Wait for completion
	evt.WaitOne(TimeSpan.FromMinutes(5));

	if (pJob.State == BackgroundCopyJobState.Transferred)
	{
		pJob.Complete();
	}
	else
	{
		pJob.Cancel();
	}
}
catch (Exception ex)
{
	Console.WriteLine($"Failure at {new System.Diagnostics.StackTrace(ex).GetFrame(0)!.GetMethod()!.Name}: {ex.Message}");
}