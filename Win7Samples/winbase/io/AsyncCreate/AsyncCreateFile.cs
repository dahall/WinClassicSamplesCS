using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace AsyncCreate
{
	static class AsyncCreate
	{
		class ASYNC_CREATE_FILE_CONTEXT
		{
			//
			// Handle to thread create to call CreateFile
			//
			public SafeHTHREAD hThread;

			//
			// CreateFile status
			//
			public Win32Error dwStatus;

			//
			// Handle returned by CreateFile
			//
			public SafeHFILE hFile;

			//
			// Cancellation Object
			//
			public IoCancellation pCancellationObject;

			//
			// Arguments to CreateFile call
			//
			public string wszFileName;
			public FileAccess dwDesiredAccess;
			public System.IO.FileShare dwShareMode;
			public SECURITY_ATTRIBUTES lpSecurityAttributes;
			public System.IO.FileMode dwCreationDisposition;
			public FileFlagsAndAttributes dwFlagsAndAttributes;
			public HFILE hTemplateFile;
		}

		static ASYNC_CREATE_FILE_CONTEXT sContext;

		static int Main(string[] args)
		{
			Win32Error err = 0;
			bool bReturnOnTimeout = false;
			ASYNC_CREATE_FILE_CONTEXT pContext = default;
			string wszFileName = null;

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
			err = AsyncCreateFile(out pContext,
								 wszFileName,
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
			var dwWaitStatus = WaitForSingleObject(pContext.hThread, dwTimeout);

			switch (dwWaitStatus)
			{

				case WAIT_STATUS.WAIT_OBJECT_0:
					//
					// Check the status after CreateFile completed
					//
					if (pContext.dwStatus != Win32Error.ERROR_SUCCESS)
					{
						err = pContext.dwStatus;
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

					CancelAsyncCreateFile(pContext);

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
						dwWaitStatus = WaitForSingleObject(pContext.hThread, INFINITE);
						err = pContext.dwStatus;
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

			if (pContext is not null)
				CleanupAsyncCreateFile(pContext);

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
			var pContext = sContext;

			var err = pContext.pCancellationObject.CancelableCreateFile(out pContext.hFile, pContext.wszFileName, pContext.dwDesiredAccess, pContext.dwShareMode,
				pContext.lpSecurityAttributes, pContext.dwCreationDisposition, pContext.dwFlagsAndAttributes, pContext.hTemplateFile);

			if (err.Failed)
			{
				//
				// Ensure we propagate the status back
				//
				pContext.dwStatus = err;
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
		static Win32Error AsyncCreateFile(out ASYNC_CREATE_FILE_CONTEXT ppObject,
			string wszFileName,
			FileAccess dwDesiredAccess,
			System.IO.FileShare dwShareMode,
			SECURITY_ATTRIBUTES lpSecurityAttributes,
			System.IO.FileMode dwCreationDisposition,
			FileFlagsAndAttributes dwFlagsAndAttributes,
			HFILE hTemplateFile)
		{
			Win32Error err = 0;

			ppObject = null;
			var pObject = new ASYNC_CREATE_FILE_CONTEXT
			{
				wszFileName = wszFileName,
				dwDesiredAccess = dwDesiredAccess,
				dwShareMode = dwShareMode,
				lpSecurityAttributes = lpSecurityAttributes,
				dwCreationDisposition = dwCreationDisposition,
				dwFlagsAndAttributes = dwFlagsAndAttributes,
				hTemplateFile = hTemplateFile
			};

			try
			{
				pObject.pCancellationObject = new IoCancellation();
			}
			catch (NotSupportedException)
			{
				pObject.pCancellationObject = default;
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
			if (pObject.pCancellationObject is not null)
			{
				sContext = pObject;
				pObject.hThread = CreateThread(default, 0, AsyncCreateFileCallback, default, 0, out _);

				if (pObject.hThread.IsInvalid)
				{
					pObject.hThread = null;
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
				pObject.hFile = CreateFile(pObject.wszFileName,
											 pObject.dwDesiredAccess,
											 pObject.dwShareMode,
											 pObject.lpSecurityAttributes,
											 pObject.dwCreationDisposition,
											 pObject.dwFlagsAndAttributes,
											 pObject.hTemplateFile);

				if (pObject.hFile.IsInvalid)
				{
					err = GetLastError();
				}

				if (err.Failed)
				{
					goto cleanup;
				}
			}

			ppObject = pObject;
			pObject = null;

			cleanup:

			if (pObject is not null)
			{
				CleanupAsyncCreateFile(pObject);
			}

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
		static void CancelAsyncCreateFile(ASYNC_CREATE_FILE_CONTEXT pObject)
		{
			pObject.pCancellationObject.Signal();
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
		static void CleanupAsyncCreateFile(ASYNC_CREATE_FILE_CONTEXT pObject)
		{
			sContext = null;

			if (pObject.hThread is not null)
			{
				WaitForSingleObject(pObject.hThread, INFINITE);
				pObject.hThread.Dispose();
			}

			//
			// Once the thread has exited we can safely assume that we
			// have full control of the object.
			//
			pObject.pCancellationObject?.Close();

			pObject.hFile?.Dispose();
		}
	}
}