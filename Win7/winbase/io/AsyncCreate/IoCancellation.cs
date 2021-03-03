using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace AsyncCreate
{
	/*++

		Module Name:

			IoCancellation.h

		Abstract:

			IO cancellation facility that provides safe coding pattern for 
			cancellation of synchronous IO operations using CancelSynchronousIo.

			This facility allows to cancel synchronous IO during an IO 
			cancelable code section that is bounded using
			IoCancellationSectionEnter and IoCancellationSectionLeave

			In the cancelable section you should call only operations that 
			are cancellation safe (e.g. CreateFile) and can be canceled a 
			using single call to IoCancellationSignal.

			Using CancelSynchronousIo without proper synchronization can 
			lead to unpredictable results since CancelSynchronousIo 
			can accidentally cancel the wrong operation.

			See README.TXT and the comments in IoCancellation.c for
			descriptions of the various routines.

	--*/
	internal class IoCancellation
	{
		private HTHREAD hThread;
		private uint dwThreadId;
		private bool fPendingIo;
		private bool fCanceled;
		private object lockObj = new object();

		/*++

		Routine Description:

			Creates new cancellation object.

		Arguments:

			ppObject - Pointer to receive new cancellation object.

		Return values:

			Win32Error.ERROR_NOT_SUPPORTED if the OS does not support synchronous cancellation.
			Win32Error.ERROR_SUCCESS if the creation was successful.
			Otherwise an appropriate error code is returned.


		--*/
		public IoCancellation()
		{
			Win32Error err = 0;

			//
			// Load CancelSynchronousIo dynamically from kernel32.dll
			//
			var hKernel32 = GetModuleHandle("kernel32.dll");
			if (hKernel32.IsNull)
			{
				err = GetLastError();
			}

			//
			// Return Win32Error.ERROR_NOT_SUPPORTED CancelSynchronousIo not found
			//
			var pfnCancel = GetProcAddress(hKernel32, "CancelSynchronousIo");
			if (pfnCancel == default)
			{
				err = GetLastError();
				if (err == Win32Error.ERROR_PROC_NOT_FOUND)
				{
					err = Win32Error.ERROR_NOT_SUPPORTED;
				}
			}

			err.ThrowIfFailed();
		}

		/*++

		Routine Description:

		Closes a cancellation object.

		Arguments:

		pObject - Pointer to cancellation object.

		--*/
		public void Close()
		{
			//
			// Lock used as memory barrier
			//
			lock (lockObj)
			{
				if (!hThread.IsNull)
				{
					CloseHandle(hThread);
				}
			}
		}

		private static bool DuplicateHandle<THandle, TAccess>(HPROCESS hSourceProcessHandle, THandle hSourceHandle,
			HPROCESS hTargetProcessHandle, out THandle lpTargetHandle, TAccess dwDesiredAccess, bool bInheritHandle = false,
			DUPLICATE_HANDLE_OPTIONS dwOptions = 0) where THandle : IKernelHandle where TAccess : struct, IConvertible
		{
			var ret = Kernel32.DuplicateHandle(hSourceProcessHandle, hSourceHandle.DangerousGetHandle(), hTargetProcessHandle,
				out var h, Convert.ToUInt32(dwDesiredAccess), bInheritHandle, dwOptions);
			lpTargetHandle = (THandle)Activator.CreateInstance(typeof(THandle), h);
			return ret;
		}

		private static bool CloseHandle<THandle>(THandle handle) where THandle : IKernelHandle => Kernel32.CloseHandle(handle.DangerousGetHandle());

		/*++

		Routine Description:

		This routine will attempt to cancel the operation that
		is represented by the cancellation object by calling
		CancelSynchronousIo on the target thread.

		Arguments:

		pObject - Pointer to a cancellation object.

		--*/
		public void Signal()
		{
			lock (lockObj)
			{
				fCanceled = true;

				//
				// Retry 3 times in case that the IO has not yet made it to
				// the driver and CancelSynchronousIo returns Win32Error.ERROR_NOT_FOUND
				//
				for (var dwRetryCount = 0U; dwRetryCount < 3; ++dwRetryCount)
				{
					if (!fPendingIo)
					{
						break;
					}

					if (hThread.IsNull) throw new InvalidOperationException();

					//
					// Ignore cancel errors since it's only optional for drivers
					//
					var fOK = CancelSynchronousIo(hThread);
					if (fOK || GetLastError() != Win32Error.ERROR_NOT_FOUND)
					{
						break;
					}

					//
					// Retry after short sleep
					//
					SwitchToThread();
				}
			}
		}

		/*++

		Routine Description:

		Function that calls CreateFileW in a cancellation section and therefore 
		can be canceled using IoCancellationSignal from another thread.

		Arguments:

		phFile - Pointer to a handle that will be returned on successful create.
		On failure this will be INVALID_HANDLE_VALUE.


		pCancellationObject - Pointer to cancellation object created with 
		IoCancellationCreate.

		wszFileName - File name to open.

		dwDesiredAccess - Desired access for handle.

		dwShareMode - Share mode.

		lpSecurityAttributes - Optional pointer to security attributes.

		dwCreationDisposition - Disposition for create.

		dwFlagsAndAttributes - Flags for create.

		hTemplateFile - Optional handle to template file.

		Return value:

		Win32Error.ERROR_SUCCESS on success.
		Win32Error.ERROR_OPERATION_ABORTED on cancellation.
		Otherwise use GetLastError to determine the cause of failure.

		--*/
		public Win32Error CancelableCreateFile(out SafeHFILE hFile,
					string wszFileName,
					FileAccess dwDesiredAccess,
					System.IO.FileShare dwShareMode,
					SECURITY_ATTRIBUTES lpSecurityAttributes,
					System.IO.FileMode dwCreationDisposition,
					FileFlagsAndAttributes dwFlagsAndAttributes,
					HFILE hTemplateFile)
		{
			Win32Error err = 0;

			lock (lockObj)
			{
				//
				// GetCurrentThread returns pseudo handle for the calling thread
				// and it always refers to the current thread; therefore it can't 
				// be used for CancelSynchronousIo.
				// DuplicateHandle will return a normal handle that can
				// be used cross thread.
				//
				if (hThread.IsNull)
				{
					//
					// CancelSynchronousIo actually requires only THREAD_TERMINATE
					//
					bool fOK = DuplicateHandle(GetCurrentProcess(), GetCurrentThread(), GetCurrentProcess(), out hThread,
						ThreadAccess.THREAD_ALL_ACCESS);
					if (!fOK)
					{
						err = GetLastError();
					}
					else
					{
						dwThreadId = GetCurrentThreadId();
					}
				}

				fPendingIo = true;

				//
				// Cancellation section contains a single call to CreateFileW
				//
				hFile = CreateFile(wszFileName, dwDesiredAccess, dwShareMode, lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
				if (hFile.IsInvalid)
				{
					err = GetLastError();
				}

				fPendingIo = false;

				if (fCanceled)
					err = Win32Error.ERROR_OPERATION_ABORTED;
			}

			if (err.Failed)
			{
				hFile?.Dispose();
				hFile = new SafeHFILE((IntPtr)HFILE.INVALID_HANDLE_VALUE, false);
			}

			return err;
		}

	}
}