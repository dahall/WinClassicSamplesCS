using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace CloudMirror
{
	internal static class FileCopierWithProgress
	{
		// Arbitrary delay per chunk, again, so you can actually see the progress bar move
		private const int CHUNKDELAYMS = 250;

		// Since this is a local disk to local-disk copy, it would happen really fast. This is the size of each chunk to be copied due to
		// the overlapped approach. I pulled this number out of a hat.
		private const int CHUNKSIZE = 4096;

		public static void CancelCopyFromServerToClient(in CF_CALLBACK_INFO lpCallbackInfo, in CF_CALLBACK_PARAMETERS lpCallbackParameters)
		{
			CancelCopyFromServerToClientWorker(lpCallbackInfo,
				lpCallbackParameters.Cancel.FetchData.FileOffset,
				lpCallbackParameters.Cancel.FetchData.Length,
				lpCallbackParameters.Cancel.Flags);
		}

		public static void CopyFromServerToClient(in CF_CALLBACK_INFO lpCallbackInfo, in CF_CALLBACK_PARAMETERS lpCallbackParameters, string serverFolder)
		{
			try
			{
				CopyFromServerToClientWorker(lpCallbackInfo,
					lpCallbackInfo.ProcessInfo,
					lpCallbackParameters.FetchData.RequiredFileOffset,
					lpCallbackParameters.FetchData.RequiredLength,
					lpCallbackParameters.FetchData.OptionalFileOffset,
					lpCallbackParameters.FetchData.OptionalLength,
					lpCallbackParameters.FetchData.Flags,
					lpCallbackInfo.PriorityHint,
					serverFolder);
			}
			catch
			{
				TransferData(lpCallbackInfo.ConnectionKey,
					lpCallbackInfo.TransferKey,
					default,
					lpCallbackParameters.FetchData.RequiredFileOffset,
					lpCallbackParameters.FetchData.RequiredLength,
					NTStatus.STATUS_UNSUCCESSFUL);
			}
		}

		private static void CancelCopyFromServerToClientWorker(in CF_CALLBACK_INFO callbackInfo, long cancelFileOffset, long cancelLength, CF_CALLBACK_CANCEL_FLAGS cancelFlags)
		{
			// Yeah, a whole lotta noting happens here, because sample.
			Console.Write("[{0:X4}:{1:X4}] - Cancelling read for {2}{3}, offset {4:X16} length {5:X16}\n",
				GetCurrentProcessId(), GetCurrentThreadId(), callbackInfo.VolumeDosName, callbackInfo.NormalizedPath, cancelFileOffset, cancelLength);
		}

		private static unsafe void CopyFromServerToClientWorker(in CF_CALLBACK_INFO callbackInfo, IntPtr pProcessInfo, long requiredFileOffset, long requiredLength,
			long optionalFileOffset, long optionalLength, CF_CALLBACK_FETCH_DATA_FLAGS fetchFlags, byte priorityHint, string serverFolder)
		{
			var fullServerPath = Path.Combine(serverFolder, StringHelper.GetString(callbackInfo.FileIdentity, CharSet.Unicode));

			var fullClientPath = callbackInfo.VolumeDosName + callbackInfo.NormalizedPath;

			var processInfo = pProcessInfo.ToNullableStructure<CF_PROCESS_INFO>();
			Console.Write("[{0:X4}:{1:X4}] - Received data request from {2} for {3}{4}, priority {5}, offset {6:X16} length {7:X16}\n",
				GetCurrentProcessId(),
				GetCurrentThreadId(),
				(processInfo.HasValue && processInfo.Value.ImagePath != null) ? processInfo.Value.ImagePath : "UNKNOWN",
				callbackInfo.VolumeDosName,
				callbackInfo.NormalizedPath,
				priorityHint,
				requiredFileOffset,
				requiredLength);

			using var serverFileHandle = CreateFile(fullServerPath, Kernel32.FileAccess.GENERIC_READ, FileShare.Read | FileShare.Delete,
				default, FileMode.Open, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL | FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);

			if (serverFileHandle.IsInvalid)
			{
				var hr = (HRESULT)Win32Error.GetLastError();

				Console.Write("[{0:X4}:{1:X4}] - Failed to open {2} for read, hr {3:X}\n", GetCurrentProcessId(), GetCurrentThreadId(), fullServerPath, hr);

				hr.ThrowIfFailed();
			}

			// Allocate the buffer used in the overlapped read.
			var chunkBufferSize = Math.Min(requiredLength, CHUNKSIZE);

			// Tell the read completion context where to copy the chunk(s)
			var readCompletionContext = new READ_COMPLETION_CONTEXT
			{
				FullPath = fullClientPath,
				CallbackInfo = callbackInfo,
				Handle = serverFileHandle,
				PriorityHint = priorityHint,
				StartOffset = requiredFileOffset,
				RemainingLength = requiredLength,
				BufferSize = chunkBufferSize,
				Overlapped = new NativeOverlapped
				{
					OffsetLow = unchecked((int)requiredFileOffset.LowPart()),
					OffsetHigh = requiredFileOffset.HighPart()
				}
			}.MarshalToPtr(Marshal.AllocHGlobal, out _);

			Console.Write("[{0:X4}:{1:X4}] - Downloading data for {2}, priority {3}, offset {4:X16} length {5:X8}\n",
				GetCurrentProcessId(),
				GetCurrentThreadId(),
				fullClientPath,
				priorityHint,
				requiredFileOffset,
				chunkBufferSize);

			// Initiate the read for the first chunk. When this async operation completes (failure or success), it will call the
			// OverlappedCompletionRoutine above with that chunk. That OverlappedCompletionRoutine is responsible for subsequent ReadFileEx
			// calls to read subsequent chunks. This is only for the first one
			if (!ReadFileEx(serverFileHandle, (byte*)readCompletionContext.Offset(READ_COMPLETION_CONTEXT.BufferOffset), (uint)chunkBufferSize, (NativeOverlapped*)readCompletionContext, OverlappedCompletionRoutine))
			{
				var hr = (HRESULT)Win32Error.GetLastError();
				Console.Write("[{0:X4}:{1:X4}] - Failed to perform async read for {2}, Status {3:X}\n", GetCurrentProcessId(), GetCurrentThreadId(), fullServerPath, hr);

				serverFileHandle?.Dispose();
				Marshal.FreeHGlobal(readCompletionContext);

				hr.ThrowIfFailed();
			}

			// Mark as invalid so it is not closed when scope disposes
			serverFileHandle.SetHandleAsInvalid();
		}

		private static unsafe void OverlappedCompletionRoutine(uint errorCode, uint numberOfBytesTransfered, NativeOverlapped* overlapped)
		{
			var readContext = ((IntPtr)overlapped).ToStructure<READ_COMPLETION_CONTEXT>();
			var bufPtr = ((IntPtr)overlapped).Offset(READ_COMPLETION_CONTEXT.BufferOffset);

			// There is the possibility that this code will need to be retried, see end of loop
			var keepProcessing = false;

			do
			{
				// Determine how many bytes have been "downloaded"
				if (errorCode == 0)
				{
					if (!GetOverlappedResult(readContext.Handle, overlapped, out numberOfBytesTransfered, true))
					{
						errorCode = (uint)Win32Error.GetLastError();
					}
				}

				// Fix up bytes transfered for the failure case
				if (errorCode != 0)
				{
					Console.Write("[{0:X4}:{1:X4}] - Async read failed for {2}, Status {3:X}\n",
					GetCurrentProcessId(),
					GetCurrentThreadId(),
					readContext.FullPath,
					errorCode);

					numberOfBytesTransfered = (uint)(Math.Min(readContext.BufferSize, readContext.RemainingLength));
				}

				// Simulate passive progress. Note that the completed portion should be less than the total or we will end up "completing"
				// the hydration request prematurely.
				var total = readContext.CallbackInfo.FileSize + readContext.BufferSize;
				var completed = readContext.StartOffset + readContext.BufferSize;

				// Update the transfer progress
				Utilities.ApplyTransferStateToFile(readContext.FullPath, readContext.CallbackInfo, total, completed);

				// Slow it down so we can see it happening
				Sleep(CHUNKDELAYMS);

				// Complete whatever range returned
				Console.Write("[{0:X4}:{1:X4}] - Executing download for {2}, Status {3:X8}, priority {4}, offset {5:X16} length {6:X8}\n",
					GetCurrentProcessId(),
					GetCurrentThreadId(),
					readContext.FullPath,
					errorCode,
					readContext.PriorityHint,
					readContext.StartOffset,
					numberOfBytesTransfered);

				// This helper function tells the Cloud File API about the transfer, which will copy the data to the local syncroot
				TransferData(readContext.CallbackInfo.ConnectionKey, readContext.CallbackInfo.TransferKey, errorCode == 0 ? bufPtr : default,
					readContext.StartOffset, numberOfBytesTransfered, (Win32Error)errorCode);

				// Move the values in the read context to the next chunk
				readContext.StartOffset += numberOfBytesTransfered;
				readContext.RemainingLength -= numberOfBytesTransfered;

				// See if there is anything left to read
				if (readContext.RemainingLength > 0)
				{
					// Cap it at chunksize
					var bytesToRead = (uint)(Math.Min(readContext.RemainingLength, readContext.BufferSize));

					// And call ReadFileEx to start the next chunk read
					overlapped->OffsetLow = unchecked((int)readContext.StartOffset.LowPart());
					overlapped->OffsetHigh = readContext.StartOffset.HighPart();

					Console.Write("[{0:X4}:{1:X4}] - Downloading data for {2}, priority {3}, offset {4:X16} length {5:X8}\n",
						GetCurrentProcessId(),
						GetCurrentThreadId(),
						readContext.FullPath,
						readContext.PriorityHint,
						readContext.StartOffset,
						bytesToRead);

					// In the event of ReadFileEx succeeding, the while loop will complete, this chunk is done, and whenever the OS has
					// completed this new ReadFileEx again, then this entire method will be called again with the new chunk. In that case,
					// the handle and buffer need to remain intact
					if (!ReadFileEx(readContext.Handle, (byte*)bufPtr, bytesToRead, overlapped, OverlappedCompletionRoutine))
					{
						// In the event the ReadFileEx failed, we want to loop through again to try and process this again
						errorCode = (uint)Win32Error.GetLastError(); ;
						numberOfBytesTransfered = 0;

						keepProcessing = true;
					}
				}
				else
				{
					// Close the read file handle and free the buffer, because we are done.
					CloseHandle((IntPtr)readContext.Handle);
					Marshal.FreeHGlobal((IntPtr)overlapped);
				}
			} while (keepProcessing);
		}

		private static void TransferData(CF_CONNECTION_KEY connectionKey, CF_TRANSFER_KEY transferKey, IntPtr transferData, long startingOffset, long length, NTStatus completionStatus)
		{
			var opInfo = new CF_OPERATION_INFO()
			{
				StructSize = (uint)Marshal.SizeOf<CF_OPERATION_INFO>(),
				Type = CF_OPERATION_TYPE.CF_OPERATION_TYPE_TRANSFER_DATA,
				ConnectionKey = connectionKey,
				TransferKey = transferKey
			};
			var opParams = new CF_OPERATION_PARAMETERS
			{
				ParamSize = (uint)Marshal.SizeOf<CF_OPERATION_PARAMETERS.TRANSFERDATA>() + (uint)Marshal.SizeOf<uint>(),
				TransferData = new CF_OPERATION_PARAMETERS.TRANSFERDATA
				{
					CompletionStatus = completionStatus,
					Buffer = transferData,
					Offset = startingOffset,
					Length = length
				}
			};

			CfExecute(opInfo, ref opParams).ThrowIfFailed();
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct READ_COMPLETION_CONTEXT
		{
			public NativeOverlapped Overlapped;
			public CF_CALLBACK_INFO CallbackInfo;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			public string FullPath;

			public HFILE Handle;
			public byte PriorityHint;
			public long StartOffset;
			public long RemainingLength;
			public long BufferSize;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = CHUNKSIZE)]
			public byte[] Buffer;

			public static readonly int BufferOffset = Marshal.SizeOf<READ_COMPLETION_CONTEXT>() - CHUNKSIZE;
		}
	}
}