using Vanara.IO;

if (args.Length != 2)
{
Console.Write("Usage: DOWNLOADS.exe [remote name] [local name]\n");
	return;
}

try
{
	// Create a Job
	Console.Write("Creating Job...\n");
	var pJob = BackgroundCopyManager.Jobs.Add("P2PSample", "", BackgroundCopyJobType.Download);

	// Set the File Completed Call
	var evt = new AutoResetEvent(false);
	pJob.Completed += (s, e) => {
		Console.WriteLine("Job transferred. Completing Job...");
		try { pJob.Complete(); evt.Set(); }
		catch (Exception jcex) { Console.WriteLine($"Job Completion Failed with error {jcex.Message}"); }
	};
	pJob.Error += (s, e) => {
		Console.WriteLine("Job entered error state...");
		Console.WriteLine($"Job {e.Job.DisplayName} encountered the following error: {e.Job.LastError}");
		evt.Set();
	};
	// Add a File
	Console.Write("Adding File to Job\n");
	pJob.Files.Add(args[0], args[1]);

	//Resume the job
	Console.Write("Resuming Job...\n");
	pJob.Resume();

	// Wait for QuitMessage from CallBack
	evt.WaitOne(TimeSpan.FromMinutes(15));
}
catch (Exception ex)
{
	Console.WriteLine($"Failure at {new System.Diagnostics.StackTrace(ex).GetFrame(0)!.GetMethod()!.Name}: {ex.Message}");
}