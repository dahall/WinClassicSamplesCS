using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.BITS;
using static Vanara.PInvoke.Kernel32;

namespace BackgroundCopyFileProperties
{
	class BackgroundCopyFileProperties
	{

		/**
		 * Definition of constants
		 */
		const uint HALF_SECOND_AS_MILLISECONDS = 500;
		const uint TWO_SECOND_LOOP = 2000 / HALF_SECOND_AS_MILLISECONDS;


		/**
		* Array containing multiple tuples representing the files
		* that will be added to the download job.
		*/
		static (string RemoteFile, string LocalFile)[] FileList =
		{
		("https://download.microsoft.com/download/2/9/4/29413F94-2ACF-496A-AD9C-8F43598510B7/EIE11_EN-US_MCM_WIN764.EXE",
		"c:\\temp\\data\\EIE11_EN-US_MCM_WIN764.EXE"),
		("https://www.microsoft.com/en-us/download/confirmation.aspx?id=51188&6B49FDFB-8E5B-4B07-BC31-15695C5A2143=1",
		"c:\\temp\\data\\visioviewer_4339-1001_x86_en-us.exe")
		};


		/*
		* Main program entry point
		*/
		[STAThread]
		static int Main()
		{
			// Get the BITS Background Copy Manager 
			var hr = GetBackgroundCopyManager(out var Manager);
			if (hr.Succeeded)
			{
				// Create a new download job
				hr = CreateDownloadJob("MyJob", Manager, out var Job);
				if (hr.Succeeded)
				{
					// Add the files to the job
					for (var i = 0; i < FileList.Length; ++i)
					{
						try
						{
							Job.AddFile(FileList[i].RemoteFile, FileList[i].LocalFile);
							Console.Write("Downloading remote file '{0}' to local file '{1}'\n", FileList[i].RemoteFile, FileList[i].LocalFile);
						}
						catch (Exception ex)
						{
							Console.Write("Error: Unable to add remote file '{0}' to the download job (error {1:X}). {2}\n", FileList[i].RemoteFile, ex.HResult, ex.Message);
						}
					}

					// Start the job and display its progress
					try
					{
						Job.Resume();
						MonitorJobProgress(Job);
					}
					catch (Exception ex)
					{
						Console.Write("ERROR: Unable to start the BITS download job (error code {0:X}). {1}\n", ex.HResult, ex.Message);
					}

					// Release the BITS IBackgroundCopyJob interface
					Marshal.ReleaseComObject(Job);
				}

				// Release the IBackgroundCopyManager interface
				Marshal.ReleaseComObject(Manager);
			}

			return 0;
		}


		/**
	   * Gets a pointer to the BITS Background Copy Manager.
	   *
	   * If successful, it returns a success code and sets the
	   * referenced IBackgroundCopyFileManager interface pointer
	   * to a reference counted instance of the Background Copy Manager
	   * interface.
		*/
		static HRESULT GetBackgroundCopyManager(out IBackgroundCopyManager Manager)
		{
			//Specify the appropriate COM threading model for your application.
			try
			{
				Manager = new IBackgroundCopyManager();
				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				Manager = null;
				return ex.HResult;
			}
		}



		/**
		* Creates a new download job with the specified name.
		*/
		static HRESULT CreateDownloadJob(string Name, IBackgroundCopyManager Manager, out IBackgroundCopyJob Job)
		{
			try
			{
				Manager.CreateJob(Name, BG_JOB_TYPE.BG_JOB_TYPE_DOWNLOAD, out _, out Job);
				return HRESULT.S_OK;
			}
			catch (Exception cex)
			{
				Job = null;
				return cex.HResult;
			}
		}



		/**
		* Monitors and displays the progress of the download job.
		*
		* A new status message is output whenever the job's status changes or,
		* when transferring data, every 2 seconds displays how much data
		* has been transferred.
		*/
		static HRESULT MonitorJobProgress(IBackgroundCopyJob Job)
		{
			BG_JOB_STATE PreviousState = (BG_JOB_STATE)(-1);
			bool Exit = false;
			int ProgressCounter = 0;

			string JobName = Job.GetDisplayName();
			Console.Write("Progress report for download job '{0}'.\n", JobName);

			// display the download progress
			while (!Exit)
			{
				var State = Job.GetState();

				if (State != PreviousState)
				{
					switch (State)
					{
						case BG_JOB_STATE.BG_JOB_STATE_QUEUED:
							Console.Write("Job is in the queue and waiting to run.\n");
							break;

						case BG_JOB_STATE.BG_JOB_STATE_CONNECTING:
							Console.Write("BITS is trying to connect to the remote server.\n");
							break;

						case BG_JOB_STATE.BG_JOB_STATE_TRANSFERRING:
							Console.Write("BITS has started downloading data.\n");
							DisplayProgress(Job);
							break;

						case BG_JOB_STATE.BG_JOB_STATE_ERROR:
							Console.Write("ERROR: BITS has encountered a non-recoverable error (error code {0:X}).\n", GetLastError());
							Console.Write(" Exiting job.\n");
							Exit = true;
							break;

						case BG_JOB_STATE.BG_JOB_STATE_TRANSIENT_ERROR:
							Console.Write("ERROR: BITS has encountered a recoverable error.\n");
							DisplayError(Job);
							Console.Write(" Continuing to retry.\n");
							break;

						case BG_JOB_STATE.BG_JOB_STATE_TRANSFERRED:
							DisplayProgress(Job);
							Console.Write("The job has been successfully completed.\n");
							Console.Write("Finalizing local files.\n");
							Job.Complete();
							break;

						case BG_JOB_STATE.BG_JOB_STATE_ACKNOWLEDGED:
							Console.Write("Finalization complete.\n");
							Exit = true;
							break;

						case BG_JOB_STATE.BG_JOB_STATE_CANCELLED:
							Console.Write("WARNING: The job has been cancelled.\n");
							Exit = true;
							break;

						default:
							Console.Write("WARNING: Unknown BITS state {0}.\n", State);
							Exit = true;
							break;
					}

					PreviousState = State;
				}

				else if (State == BG_JOB_STATE.BG_JOB_STATE_TRANSFERRING)
				{
					// display job progress every 2 seconds
					if (++ProgressCounter % TWO_SECOND_LOOP == 0)
					{
						DisplayProgress(Job);
					}
				}

				Sleep(HALF_SECOND_AS_MILLISECONDS);
			}

			Console.Write("\n");

			DisplayFileHeaders(Job);

			return HRESULT.S_OK;
		}



		/**
		* For each file in the job, obtains the (final) HTTP headers received from the
		* remote server that hosts the files and then displays the HTTP headers.
		*/
		static HRESULT DisplayFileHeaders(IBackgroundCopyJob Job)
		{
			Console.Write("Individual file information.\n");

			try
			{
				var FileEnumerator = Job.EnumFiles();
				try
				{
					foreach (var TempFile in FileEnumerator.Next(FileEnumerator.GetCount()))
					{
						try
						{
							var File = (IBackgroundCopyFile5)TempFile;

							string RemoteFileName = File.GetRemoteName();
							Console.Write("HTTP headers for remote file '{0}'\n", RemoteFileName);

							var Value = File.GetProperty(BITS_FILE_PROPERTY_ID.BITS_FILE_PROPERTY_ID_HTTP_RESPONSE_HEADERS);
							if (Value.String != null)
							{
								DisplayHeaders(Value.String);
							}
						}
						catch
						{
							Console.Write("WARNING: Unable to obtain an IBackgroundCopyFile interface for the next file in the job.\n");
							Console.Write(" No further information can be provided about this file.\n");
						}
						finally
						{
							Marshal.ReleaseComObject(TempFile);
						}
					}
				}
				catch (Exception ex)
				{
					Console.Write("WARNING: Unable to obtain a count of the number of files in the job.\n");
					Console.Write(" No further information can be provided about the files in the job.\n");
					return ex.HResult;
				}
				finally
				{
					Marshal.ReleaseComObject(FileEnumerator);
				}
			}
			catch (Exception ex)
			{
				Console.Write("WARNING: Unable to obtain an IEnumBackgroundCopyFiles interface.\n");
				Console.Write(" No further information can be provided about the files in the job.\n");
				return ex.HResult;
			}

			return HRESULT.S_OK;
		}


		/**
		* Displays the current progress of the job in terms of the amount of data
		* and number of files transferred.
		*/
		static void DisplayProgress(IBackgroundCopyJob Job)
		{
			try
			{
				var Progress = Job.GetProgress();
				Console.Write("{0} of {1} bytes transferred ({2} of {3} files).\n", Progress.BytesTransferred, Progress.BytesTotal, Progress.FilesTransferred, Progress.FilesTotal);
			}
			catch (Exception ex)
			{
				Console.Write("ERROR: Unable to get job progress (error code {0:X}).\n", ex.HResult);
			}
		}



		/**
		* Parses the provided string containing HTTP headers,
		* splits them apart and displays them to the user.
		*/
		static void DisplayHeaders(string Headers)
		{
			Console.Write("Headers: {0}\n", Headers);
		}


		static void DisplayError(IBackgroundCopyJob Job)
		{
			try
			{
				var Error = Job.GetError();
				var ErrorDescription = Error.GetErrorDescription(GetThreadUILanguage());
				Console.Write(" Error details: %ws\n", ErrorDescription);
				Marshal.ReleaseComObject(Error);
			}
			catch
			{
				Console.Write("WARNING: Error details are not available.\n");
			}
		}
	}
}