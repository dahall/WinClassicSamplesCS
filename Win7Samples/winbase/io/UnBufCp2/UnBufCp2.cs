using System;
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

namespace UnBufCp2
{
	internal static class Program
	{
		//
		// File handles for the copy operation. All read operations are
		// from SourceFile. All write operations are to DestFile.
		//
		private static SafeHFILE SourceFile;
		private static SafeHFILE DestFile;

		//
		// I/O completion ports. All reads from the source file complete
		// to ReadPort. All writes to the destination file complete to
		// WritePort.
		//
		private static HANDLE ReadPort;
		private static HANDLE WritePort;

		//
		// Structure used to track each outstanding I/O. The maximum
		// number of I/Os that will be outstanding at any time is
		// controllable by the MAX_CONCURRENT_IO definition.
		//

		private const int MAX_CONCURRENT_IO = 20;

		private struct COPY_CHUNK
		{
			public NativeOverlapped Overlapped;
			public IntPtr Buffer;
		}

		private static readonly COPY_CHUNK[] CopyChunk = new COPY_CHUNK[MAX_CONCURRENT_IO];

		//
		// Define the size of the buffers used to do the I/O.
		// 64K is a nice number.
		//
		private const int BUFFER_SIZE = 64 * 1024;

		//
		// The system's page size will always be a multiple of the
		// sector size. Do all I/Os in page-size chunks.
		//
		private static uint PageSize;

		private static int Main(string[] args)
		{
			SafeHTHREAD WritingThread;
			ULARGE_INTEGER FileSize = default;
			ULARGE_INTEGER InitialFileSize;
			bool Success;
			uint Status;
			uint StartTime, EndTime;
			SafeHFILE BufferedHandle;

			if (args.Length != 2)
			{
				Console.Error.Write("Usage: {0} SourceFile DestinationFile\n", Environment.CommandLine);
				return 1;
			}

			//
			//confirm we are running on Windows NT 3.5 or greater, if not, display notice and
			//terminate. Completion ports are only supported on Win32 & Win32s. Creating a
			//Completion port with no handle specified is only supported on NT 3.51, so we need
			//to know what we're running on. Note, Win32s does not support console apps, thats
			//why we exit here if we are not on Windows NT.
			//
			//
			//ver.dwOSVersionInfoSize needs to be set before calling GetVersionInfoEx()
			//
			var ver = new OSVERSIONINFOEX { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEX>() };

			//
			//Failure here could mean several things 1. On an NT system,
			//it indicates NT version 3.1 because GetVersionEx() is only
			//implemented on NT 3.5. 2. On Windows 3.1 system, it means
			//either Win32s version 1.1 or 1.0 is installed.
			//
			Success = GetVersionEx(ref ver);

			if ((!Success) || //GetVersionEx() failed - see above.
				(ver.dwPlatformId != PlatformID.Win32NT)) //GetVersionEx() succeeded but we are not on NT.
			{
				MessageBox(default,
					"This sample application can only be run on Windows NT. 3.5 or greater\n" +
					"This application will now terminate.",
					"UnBufCp2",
					MB_FLAGS.MB_OK | MB_FLAGS.MB_ICONSTOP | MB_FLAGS.MB_SETFOREGROUND);
				return 1;
			}

			//
			// Get the system's page size.
			//
			GetSystemInfo(out SYSTEM_INFO SystemInfo);
			PageSize = SystemInfo.dwPageSize;

			//
			// Open the source file and create the destination file.
			// Use FILE_FLAG_NO_BUFFERING to avoid polluting the
			// system cache with two copies of the same data.
			//

			SourceFile = CreateFile(args[0], FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE, System.IO.FileShare.Read,
				default, System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_NO_BUFFERING | FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED, default);
			if (SourceFile.IsInvalid)
			{
				Console.Error.Write("failed to open {0}, error {1}\n", args[0], GetLastError());
				return 1;
			}
			FileSize.LowPart = GetFileSize(SourceFile, out FileSize.HighPart);
			if ((FileSize.LowPart == 0xffffffff) && (GetLastError() != Win32Error.NO_ERROR))
			{
				Console.Error.Write("GetFileSize failed, error {0}\n", GetLastError());
				return 1;
			}

			DestFile = CreateFile(args[1], FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE, System.IO.FileShare.ReadWrite,
				default, System.IO.FileMode.Create, FileFlagsAndAttributes.FILE_FLAG_NO_BUFFERING | FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED, SourceFile);
			if (DestFile.IsInvalid)
			{
				Console.Error.Write("failed to open {0}, error {1}\n", args[1], GetLastError());
				return 1;
			}

			//
			// Extend the destination file so that the filesystem does not
			// turn our asynchronous writes into synchronous ones.
			//
			InitialFileSize = (FileSize + PageSize - 1) & ~(PageSize - 1);
			Status = SetFilePointer(DestFile, InitialFileSize.LowPart, ref InitialFileSize.HighPart, System.IO.SeekOrigin.Begin);
			if ((Status == INVALID_SET_FILE_POINTER) && (GetLastError() != Win32Error.NO_ERROR))
			{
				Console.Error.Write("SetFilePointer failed, error {0}\n", GetLastError());
				return 1;
			}
			Success = SetEndOfFile(DestFile);
			if (!Success)
			{
				Console.Error.Write("SetEndOfFile failed, error {0}\n", GetLastError());
				return 1;
			}

			//
			//In NT 3.51 it is not necessary to specify the FileHandle parameter
			//of CreateIoCompletionPort()--It is legal to specify the FileHandle
			//as INVALID_HANDLE_VALUE. However, for NT 3.5 an overlapped file
			//handle is needed.
			//
			//We know already that we are running on NT, or else we wouldn't have
			//gotten this far, so lets see what version we are running on.
			//
			if (ver.dwMajorVersion == 3 && ver.dwMinorVersion == 50)
			{
				//
				//we're running on NT 3.5 - Completion Ports exists
				//
				ReadPort = CreateIoCompletionPort(SourceFile, //file handle to associate with I/O completion port
					default, //optional handle to existing I/O completion port
					SourceFile.DangerousGetHandle(), //completion key
					1); //# of threads allowed to execute concurrently

				if (ReadPort == default)
				{
					Console.Error.Write("failed to create ReadPort, error {0}\n", GetLastError());
					return 1;
				}
			}

			else
			//
			//We are running on NT 3.51 or greater.
			//
			//Create the I/O Completion Port.
			//
			{
				ReadPort = CreateIoCompletionPort((IntPtr)HFILE.INVALID_HANDLE_VALUE, //file handle to associate with I/O completion port
					default, //optional handle to existing I/O completion port
					SourceFile.DangerousGetHandle(), //completion key
					1); //# of threads allowed to execute concurrently

				//
				//If we need to, aka we're running on NT 3.51, let's associate a file handle with the
				//completion port.
				//
				ReadPort = CreateIoCompletionPort(SourceFile,
					ReadPort,
					SourceFile.DangerousGetHandle(), //should be the previously specified key.
					1);

				if (ReadPort.IsNull)
				{
					Console.Error.Write("failed to create ReadPort, error {0}\n", GetLastError());
					return 1;
				}
			}

			WritePort = CreateIoCompletionPort(DestFile,
				default,
				DestFile.DangerousGetHandle(),
				1);
			if (WritePort.IsNull)
			{
				Console.Error.Write("failed to create WritePort, error {0}\n", GetLastError());
				return 1;
			}

			//
			// Start the writing thread
			//
			WritingThread = CreateThread(default, 0, WriteLoop, (IntPtr)unchecked((long)(ulong)FileSize), 0, out var ThreadId);
			if (WritingThread.IsNull)
			{
				Console.Error.Write("failed to create write thread, error {0}\n", GetLastError());
				return 1;
			}
			WritingThread.Dispose();

			StartTime = GetTickCount();

			//
			// Start the reads
			//
			ReadLoop(FileSize);

			EndTime = GetTickCount();

			//
			// We need another handle to the destination file that is
			// opened without FILE_FLAG_NO_BUFFERING. This allows us to set
			// the end-of-file marker to a position that is not sector-aligned.
			//
			BufferedHandle = CreateFile(args[1], FileAccess.GENERIC_WRITE, System.IO.FileShare.ReadWrite, default, System.IO.FileMode.Open, 0, default);
			if (BufferedHandle.IsInvalid)
			{
				Console.Error.Write("failed to open buffered handle to {0}, error {1}\n", args[1], GetLastError());
				return 1;
			}

			//
			// Set the destination's file size to the size of the
			// source file, in case the size of the source file was
			// not a multiple of the page size.
			//
			Status = SetFilePointer(BufferedHandle, FileSize.LowPart, ref FileSize.HighPart, System.IO.SeekOrigin.Begin);
			if ((Status == INVALID_SET_FILE_POINTER) && (GetLastError() != Win32Error.NO_ERROR))
			{
				Console.Error.Write("final SetFilePointer failed, error {0}\n", GetLastError());
				return 1;
			}
			Success = SetEndOfFile(BufferedHandle);
			if (!Success)
			{
				Console.Error.Write("SetEndOfFile failed, error {0}\n", GetLastError());
				return 1;
			}

			Console.Write("\n\n{0} bytes copied in {1} seconds\n", FileSize.LowPart, (float)(EndTime - StartTime) / 1000.0);
			Console.Write("{0} MB/sec\n", FileSize / (1024.0 * 1024.0) / (((float)(EndTime - StartTime)) / 1000.0));

			return (0);
		}

		private static void ReadLoop(ULARGE_INTEGER FileSize)
		{
			ULARGE_INTEGER ReadPointer = 0;
			bool Success;
			uint NumberBytes;
			var PendingIO = 0;

			//
			// Start reading the file. Kick off MAX_CONCURRENT_IO reads, then just
			// loop waiting for writes to complete.
			//
			for (var i = 0; i < MAX_CONCURRENT_IO; i++)
			{
				if (ReadPointer >= FileSize)
				{
					break;
				}
				//
				// Use VirtualAlloc so we get a page-aligned buffer suitable
				// for unbuffered I/O.
				//
				unsafe
				{
					var chunk = new COPY_CHUNK
					{
						Buffer = VirtualAlloc(default, BUFFER_SIZE, MEM_ALLOCATION_TYPE.MEM_COMMIT, MEM_PROTECTION.PAGE_READWRITE),
						Overlapped = new NativeOverlapped
						{
							OffsetLow = unchecked((int)ReadPointer.LowPart),
							OffsetHigh = unchecked((int)ReadPointer.HighPart),
						}
					};
					if (chunk.Buffer == default)
					{
						Console.Error.Write("VirtualAlloc {0} failed, error {1}\n", i, GetLastError());
						Environment.Exit(1);
					}

					Success = ReadFile(SourceFile, (byte*)chunk.Buffer, BUFFER_SIZE, &NumberBytes, &chunk.Overlapped);

					if (!Success && (GetLastError() != Win32Error.ERROR_IO_PENDING))
					{
						Console.Error.Write("ReadFile at {0:X} failed, error {0}\n", ReadPointer.LowPart, GetLastError());
						Environment.Exit(1);
					}
					else
					{
						CopyChunk[i] = chunk;
						ReadPointer += BUFFER_SIZE;
						++PendingIO;
					}
				}
			}

			//
			// We have started the initial async. reads, enter the main loop.
			// This simply waits until a write completes, then issues the next
			// read.
			//
			unsafe
			{
				NativeOverlapped* CompletedOverlapped;
				while (PendingIO != 0)
				{
					Success = GetQueuedCompletionStatus(WritePort,
						out NumberBytes,
						out IntPtr Key,
						&CompletedOverlapped,
						INFINITE);
					if (!Success)
					{
						//
						// Either the function failed to dequeue a completion packet
						// (CompletedOverlapped is not default) or it dequeued a completion
						// packet of a failed I/O operation (CompletedOverlapped is default). 
						//
						Console.Error.Write("GetQueuedCompletionStatus on the IoPort failed, error {0}\n", GetLastError());
						Environment.Exit(1);
					}
					//
					// Issue the next read using the buffer that has just completed.
					//
					if (ReadPointer.QuadPart < FileSize.QuadPart)
					{
						var Chunk = (COPY_CHUNK*)CompletedOverlapped;
						Chunk->Overlapped.OffsetLow = unchecked((int)ReadPointer.LowPart);
						Chunk->Overlapped.OffsetHigh = unchecked((int)ReadPointer.HighPart);
						ReadPointer.QuadPart += BUFFER_SIZE;
						Success = ReadFile(SourceFile, (byte*)Chunk->Buffer, BUFFER_SIZE, &NumberBytes, &Chunk->Overlapped);

						if (!Success && (GetLastError() != Win32Error.ERROR_IO_PENDING))
						{
							Console.Error.Write("ReadFile at {0:X} failed, error {0}\n", Chunk->Overlapped.OffsetLow, GetLastError());
							Environment.Exit(1);
						}
					}
					else
					{
						//
						// There are no more reads left to issue, just wait
						// for the pending writes to drain.
						//
						--PendingIO;
					}
				}
			}
			//
			// All done. There is no need to call VirtualFree() to free CopyChunk 
			// buffers here. The buffers will be freed when this process exits.
			//
		}

		private static uint WriteLoop(IntPtr FileSize)
		{
			bool Success;
			long TotalBytesWritten = 0;

			for (; ; )
			{
				Success = GetQueuedCompletionStatus(ReadPort, out var NumberBytes, out IntPtr Key, out IntPtr CompletedOverlapped, INFINITE);

				if (!Success)
				{
					//
					// Either the function failed to dequeue a completion packet
					// (CompletedOverlapped is not default) or it dequeued a completion
					// packet of a failed I/O operation (CompletedOverlapped is default). 
					//
					Console.Error.Write("GetQueuedCompletionStatus on the IoPort failed, error {0}\n", GetLastError());
					return 1;
				}

				//
				// Update the total number of bytes written.
				//
				TotalBytesWritten += NumberBytes;

				unsafe
				{
					//
					// Issue the next write using the buffer that has just been read into.
					//
					var Chunk = (COPY_CHUNK*)CompletedOverlapped;

					//
					// Round the number of bytes to write up to a sector boundary
					//
					NumberBytes = (NumberBytes + PageSize - 1) & ~(PageSize - 1);

					Success = WriteFile(DestFile, (byte*)Chunk->Buffer, NumberBytes, &NumberBytes, &Chunk->Overlapped);

					if (!Success && (GetLastError() != Win32Error.ERROR_IO_PENDING))
					{
						Console.Error.Write("WriteFile at {0:X} failed, error {1}\n", Chunk->Overlapped.OffsetLow, GetLastError());
						return 1;
					}
				}

				//
				//Check to see if we've copied the complete file, if so return
				//
				if (TotalBytesWritten >= FileSize.ToInt64())
					return 0;
			}
		}
	}
}