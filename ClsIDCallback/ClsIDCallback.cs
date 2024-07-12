// ClsIDSample.cpp : Defines the entry point for the console application.
namespace ClsIDCallback;

using Vanara.PInvoke;
using static Vanara.PInvoke.BITS;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.User32;
using static Utils;

public static class Program
{
	static readonly DOWNLOAD_FILE[] FileList =
	[
		new()
		{
			RemoteFile = "https://download.microsoft.com/download/4/7/c/47c6134b-d61f-4024-83bd-b9c9ea951c25/Readme.txt",
			LocalFile = "C:\\Temp\\Readme.txt"
		}
	];

	[STAThread]
	public static int Main(string[] args)
	{
		if (args.Length == 0)
			return HRESULT.E_INVALIDARG;

		if (args[0].ToLower() == "unregserver")
		{
			UnRegisterServer();
			return 0;
		}

		if (args[0] == "regserver")
		{
			RegisterServer();
			return 0;
		}

		bool bRun = true;
		if (args[0] == "demo")
		{
			IBackgroundCopyManager pQueueMgr = new();

			// Create a Job
			Console.Write("Creating Job...\n");

			pQueueMgr.CreateJob("BITS_CLSID_SAMPLE", BG_JOB_TYPE.BG_JOB_TYPE_DOWNLOAD, out var guidJob, out var pBackgroundCopyJob);

			IBackgroundCopyJob5? pBackgroundCopyJob5 = pBackgroundCopyJob as IBackgroundCopyJob5;

			if (pBackgroundCopyJob5 is null)
			{
				Console.Write("Failed to Get the Job Interface, error = {0:X}\n", HRESULT.E_NOINTERFACE);
				goto cancel;
			}

			// Set the CLS ID.
			BITS_JOB_PROPERTY_VALUE propval = new() { ClsID = CLSID_CNotifyInterfaceImp };

			Console.Write("Setting Guid Callback Property ...\n");

			pBackgroundCopyJob5.SetProperty(BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_NOTIFICATION_CLSID, propval);

			// get Guid for the new job
			BITS_JOB_PROPERTY_VALUE actual_propval = pBackgroundCopyJob5.GetProperty(BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_NOTIFICATION_CLSID);

			// actual_propval.ClsID will contain the Guid registered for the Job.

			Console.Write("Setting notification flags ...\n");
			// Set appropriate notify flags for the Job
			pBackgroundCopyJob5.SetNotifyFlags(BG_NOTIFY.BG_NOTIFY_JOB_TRANSFERRED | BG_NOTIFY.BG_NOTIFY_JOB_ERROR);

			Console.Write("Adding Download files ...\n");
			// Now add one or more files to the Job using AddFile() and Resume() the job.
			pBackgroundCopyJob5.AddFile(FileList[0].RemoteFile, FileList[0].LocalFile);

			// Start the download
			pBackgroundCopyJob5.Resume();

			Console.Write("Download started, terminating the process.\n");
			Console.Write("BITS should start a new process when transfer is done.\n");

			// It is OK to terminate the application after this. BITS will instantiate the 
			// Guid to deliver the registered callbacks of the job.

			goto done;

			// NOTE: In actual scenario please do not call Cancel() until the Job is done with the download.
			cancel:
			pBackgroundCopyJob.Cancel();

			done:
			bRun = false;
		}

		if (bRun)
		{
			g_dwMainThreadID = Kernel32.GetCurrentThreadId();

			RegisterClassObject<CNotifyInterfaceImp_Factory>(CLSID_CNotifyInterfaceImp, out var dwCookie_CNotifyInterfaceImp);

			CoResumeClassObjects();

			int bRet; 
			while ((bRet = GetMessage(out var msg)) != 0)
			{
				if (bRet == -1)
				{
					break;
				}
				else
				{
					TranslateMessage(msg);
					DispatchMessage(msg);
				}
			}

			CoRevokeClassObject(dwCookie_CNotifyInterfaceImp);
		}

		return 0;
	}
}