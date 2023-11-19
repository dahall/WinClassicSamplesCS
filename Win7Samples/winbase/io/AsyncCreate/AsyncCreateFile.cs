using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace AsyncCreate;

static class AsyncCreate
{
	//
	// Handle to thread create to call CreateFile
	//
	static SafeHTHREAD? hThread;

	//
	// CreateFile status
	//
	static Win32Error dwStatus;

	//
	// Handle returned by CreateFile
	//
	static SafeHFILE? hFile;

	//
	// Cancellation Object
	//
	static IoCancellation? pCancellationObject;

	//
	// Arguments to CreateFile call
	//
	static string? wszFileName;
	static FileAccess dwDesiredAccess;
	static System.IO.FileShare dwShareMode;
	static SECURITY_ATTRIBUTES? lpSecurityAttributes;
	static System.IO.FileMode dwCreationDisposition;
	static FileFlagsAndAttributes dwFlagsAndAttributes;
	static HFILE hTemplateFile;

	static int Main(string[] args)
	{
		Win32Error err = 0;
		bool bReturnOnTimeout = false;
		string? wszFileName = null;

		if (args.Length < 3)
		{
			Console.Write("Usage {0} [file name] [timeout in milliseconds] [return on timeout - 0/1]\n", Environment.CommandLine);
			goto cleanup;
		}

		//
		// Get the command line arguments
		//
		wszFileName = args[0];
		var dwTimeout = uint.Parse(args[1]);
		bReturnOnTimeout = uint.Parse(args[2]) == 0;

		//
		// Call CreateFile to check the existence of a file
		// 
		err = AsyncCreateFile(wszFileName,
							 FileAccess.GENERIC_READ,
							 System.IO.FileShare.ReadWrite | System.IO.FileShare.Delete,
							 default,
							 System.IO.FileMode.Open,
							 FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL,
							 default);

		if (err.Succeeded)
		{
			Console.Write("File found: {0}\n", wszFileName);
			goto cleanup;

		}
		else if (err != Win32Error.ERROR_IO_PENDING)
		{
			goto cleanup;
		}

		err = 0;

		//
		// Wait with timeout on the worker thread
		//
		var dwWaitStatus = WaitForSingleObject(hThread!, dwTimeout);

		switch (dwWaitStatus)
		{

			case WAIT_STATUS.WAIT_OBJECT_0:
				//
				// Check the status after CreateFile completed
				//
				if (dwStatus != Win32Error.ERROR_SUCCESS)
				{
					err = dwStatus;
				}
				else
				{
					Console.Write("File found: {0}\n", wszFileName);
				}
				break;

			case WAIT_STATUS.WAIT_TIMEOUT:
				//
				// If we timeout, attempt to cancel the create. Note that
				// this may not do anything depending on the current status of
				// the operation
				//

				CancelAsyncCreateFile();

				//
				// If the caller specified we should wait after the cancel call for
				// the create to complete, we do that here; otherwise we return
				// an error.
				//

				if (bReturnOnTimeout == true)
				{
					err = Win32Error.ERROR_OPERATION_ABORTED;
				}
				else
				{
					dwWaitStatus = WaitForSingleObject(hThread!, INFINITE);
					err = dwStatus;
				}
				break;

			default:
				err = GetLastError();
				break;
		}

		cleanup:

		if (err.Failed)
		{
			if (err == Win32Error.ERROR_OPERATION_ABORTED)
			{
				if (bReturnOnTimeout == true)
				{
					Console.Write("Operation timed out while trying to open {0}\n", wszFileName);
				}
				else
				{
					//
					// If we waited for the create to complete and we still got 
					// this error code, we know that it was successfully cancelled.
					//
					Console.Write("Operation timed out and was cancelled while trying to open {0}\n", wszFileName);
				}

			}
			else
			{

				Console.Write("Error {0} while trying to open {1}\n", err, wszFileName);
			}
		}

		CleanupAsyncCreateFile();

		return err.Failed ? 2 : 0;
	}

	/*++

	Routine Description:

		This is a worker routine that actually issues the asynchronous create.

	Arguments:

		pParameter - Pointer to async create context.

	Return Value:

		Always 0.

	--*/
	static uint AsyncCreateFileCallback(IntPtr pParameter)
	{
		var err = pCancellationObject!.CancelableCreateFile(out hFile, wszFileName!, dwDesiredAccess, dwShareMode,
			lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);

		if (err.Failed)
		{
			//
			// Ensure we propagate the status back
			//
			dwStatus = err;
		}

		return 0;
	}

	/*++

	Routine Description:

		This function will attempt to create a thread in order to initiate
		a create request that can be cancelled at a later point in time
		using CancelAsyncCreateFile.

		If the OS does not support cancellation (pre-Vista), the create will
		take place synchronously.

	Arguments:

		ppObject - This parameter will receive a pointer to an async create context
			object that represents this instance of the asynchronous create. If
			this is default the create was not successful. In the success case the
			file handle can be found in this object.

		wszFileName - Target file name to pass to CreateFile call. Caller needs
			to ensure this memory is valid until the create completes.

		dwDesiredAccess - Desired Access to pass to CreateFile call.

		dwShareMode - Share mode to pass to CreateFile call.

		lpSecurityAttributes - Security Attributes to pass to CreateFile call.

		dwCreationDisposition - Creation Disposition to pass to CreateFile call.

		dwFlagsAndAttributes - Flags and Attributes to pass to CreateFile call.

		hTemplateFile - Template File handle to pass to CreateFile call.

	Return Value:

		Win32Error.ERROR_SUCCESS - The create was successfully completed synchronously.

		Win32Error.ERROR_IO_PENDING - The create was successfully issued asynchronously.
			The caller should wait for the thread to exit at which point
			they can retrieve the status of the create from ppObject.

		Other failures may happen with an appropriate error code (such as 
		Win32Error.ERROR_OUTOFMEMORY).

	--*/
	static Win32Error AsyncCreateFile(string _wszFileName,
		FileAccess _dwDesiredAccess,
		System.IO.FileShare _dwShareMode,
		SECURITY_ATTRIBUTES? _lpSecurityAttributes,
		System.IO.FileMode _dwCreationDisposition,
		FileFlagsAndAttributes _dwFlagsAndAttributes,
		HFILE _hTemplateFile)
	{
		Win32Error err = 0;
		wszFileName = _wszFileName;
		dwDesiredAccess = _dwDesiredAccess;
		dwShareMode = _dwShareMode;
		lpSecurityAttributes = _lpSecurityAttributes;
		dwCreationDisposition = _dwCreationDisposition;
		dwFlagsAndAttributes = _dwFlagsAndAttributes;
		hTemplateFile = _hTemplateFile;

		try
		{
			pCancellationObject = new IoCancellation();
		}
		catch (NotSupportedException)
		{
			pCancellationObject = default;
			err = 0;
		}
		catch (System.ComponentModel.Win32Exception ex)
		{
			err = (uint)ex.NativeErrorCode;
			goto cleanup;
		}

		//
		// Call CreateFile asynchronously if cancellation is applicable
		//
		if (pCancellationObject is not null)
		{
			hThread = CreateThread(default, 0, AsyncCreateFileCallback, default, 0, out _);
			if (hThread.IsInvalid)
			{
				hThread = null;
				err = GetLastError();
				goto cleanup;
			}

			// not a failure
			err = Win32Error.ERROR_IO_PENDING;
		}
		else
		{
			//
			// Call CreateFile synchronously if cancellation is not applicable
			//
			hFile = CreateFile(wszFileName,
										 dwDesiredAccess,
										 dwShareMode,
										 lpSecurityAttributes,
										 dwCreationDisposition,
										 dwFlagsAndAttributes,
										 hTemplateFile);

			if (hFile.IsInvalid)
			{
				err = GetLastError();
			}

			if (err.Failed)
			{
				goto cleanup;
			}
		}

		return err;

		cleanup:

		CleanupAsyncCreateFile();

		return err;
	}

	/*++

	Routine Description:

		Attemps to cancel an asynchronous CreateFile.

		This function can be called if AsyncCreateFile returned Win32Error.ERROR_IO_PENDING.

		After calling this function, the called should wait on the thread
		in the object to determine when the create has actually returned (since
		there is no guarantee it will successfully be cancelled) and be sure
		to clean up using CleanupAsyncCreateFile.

	Arguments:

		pObject - Pointer to a context object returned by AsyncCreateFile.

	--*/
	static void CancelAsyncCreateFile()
	{
		pCancellationObject?.Signal();
	}

	/*++

	Routine Description:

		Cleans up the context created to support AsyncCreateFile.

		Callers should use this routine after a call to AsyncCreateFile
		has returned Win32Error.ERROR_SUCCESS or Win32Error.ERROR_IO_PENDING and they are
		finished with the result of the create (for instance on a successful
		create they no longer need the file handle).

	Arguments:

		pObject - Pointer to a context object returned by AsyncCreateFile.

	--*/
	static void CleanupAsyncCreateFile()
	{
		if (hThread is not null)
		{
			WaitForSingleObject(hThread, INFINITE);
			hThread.Dispose();
		}

		//
		// Once the thread has exited we can safely assume that we
		// have full control of the object.
		//
		pCancellationObject?.Close();

		hFile?.Dispose();
	}
}