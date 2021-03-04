using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace FCopy
{	internal static class Program
	{
		// maximum view size
		private static uint dwMaxViewSize;

		// multiplying the system allocation size by the following constant
		// determines the maximum view size
		private const ushort ALLOCATION_MULTIPLIER = 1;
		private const int SUCCESS = 0; /* for return value from main() */
		private const int FAILURE = 1; /* for return value from main() */

		/*---------------------------------------------------------------------------
		main (args.Length, args)

		The main program. Takes the command line arguments, copies the source file
		to the destination file.

		Parameters
		 args.Length
			 Count of command-line arguments, including the name of the program.
		 args
			 Array of pointers to strings that contain individual command-line
			 arguments.

		Returns
		 Zero if program executed successfully, non-zero otherwise.
		---------------------------------------------------------------------------*/
		private static int Main(string[] args)
		{
			var fResult = FAILURE;
			ulong liBytesRemaining;
			ULARGE_INTEGER liSrcFileSize = default, liMapSize, liOffset;
			SafeHFILE hSrcFile, hDstFile = null;
			SafeHSECTION hSrcMap = null, hDstMap = null;
			IntPtr pSrc = default, pDst = default;

			if (args.Length != 2)
			{
				Console.Write("usage: fcopy <srcfile> <dstfile>\n");
				return FAILURE;
			}

			var pszSrcFileName = args[^2]; // Src is second to last argument
			var pszDstFileName = args[^1]; // Dst is the last argument

			// Obtain the system's allocation granularity, then multiply it by an 
			// arbitrary factor to obtain the maximum view size
			GetSystemInfo(out SYSTEM_INFO siSystemInfo);
			dwMaxViewSize = siSystemInfo.dwAllocationGranularity * ALLOCATION_MULTIPLIER;

			/*
			Steps to open and access a file's contents:
			1) Open the file,
			2) Create a mapping of the file,
			3) Map a view of the file.

			This yields a pointer to the file's contents, which can then be used
			to access the file, just as if it's contents were in a memory buffer.

			For the source file, open and map it as read only; for the destination
			file, open and map it as read-write. We allow other processes to read
			the source file while we're copying it, but do not allow access to the
			destination file since we're writing it.
			*/

			// Open the source and destination files
			hSrcFile = CreateFile(pszSrcFileName, FileAccess.GENERIC_READ, System.IO.FileShare.Read,
				default, System.IO.FileMode.Open, 0);
			if (hSrcFile.IsInvalid)
			{
				Console.Write("fcopy: couldn't open source file.\n");
				goto DONE;
			}

			hDstFile = CreateFile(pszDstFileName, FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE, 0,
				default, System.IO.FileMode.Create, 0);
			if (hDstFile.IsInvalid)
			{
				Console.Write("fcopy: couldn't create destination file.\n");
				goto DONE;
			}

			// Need source file's size to know how big to make the destination mapping.
			liSrcFileSize.LowPart = GetFileSize(hSrcFile, out liSrcFileSize.HighPart);
			if ((unchecked((uint)-1) == liSrcFileSize.LowPart) && (GetLastError() != Win32Error.NO_ERROR))
			{
				DEBUG_PRINT("couldn't get size of source file.\n");
				goto DONE;
			}

			/*
			Special case: If the source file is zero bytes, we don't map it because
			there's no need to and CreateFileMapping cannot map a zero-length file.
			But since we've created the destination, we've successfully "copied" the
			source.
			*/
			if (0 == liSrcFileSize)
			{
				fResult = SUCCESS;
				goto DONE;
			}


			/*
			Map the source and destination files. A mapping size of zero means the
			whole file will be mapped.
			*/
			hSrcMap = CreateFileMapping(hSrcFile, null, MEM_PROTECTION.PAGE_READONLY, 0, 0, default);
			if (hSrcMap.IsInvalid)
			{
				DEBUG_PRINT("couldn't map source file\n");
				goto DONE;
			}

			hDstMap = CreateFileMapping(hDstFile, null, MEM_PROTECTION.PAGE_READWRITE, liSrcFileSize.HighPart, liSrcFileSize.LowPart, default);
			if (hDstMap.IsInvalid)
			{
				DEBUG_PRINT("couldn't map destination file.\n");
				goto DONE;
			}


			/*
			Now that we have the source and destination mapping objects, map views
			of the source and destination files, and do the file copy.

			To minimize the amount of memory consumed for large files and make it
			possible to copy files that couldn't be mapped into our address
			space entirely (those over 2GB), we limit the source and destination
			views to the smaller of the file size or a specified maximum view size
			(dwMaxViewSize, which is ALLOCATION_MULTIPLIER times the system's 
			allocation size).

			If the file is smaller than the max view size, we'll just map and copy
			it. Otherwise, we'll map a portion of the file, copy it, then map the
			next portion, copy it, etc. until the entire file is copied.

			MAP_SIZE is 32 bits because MapViewOfFile requires a 32-bit value for
			the size of the view. This makes sense because a Win32 process's
			address space is 4GB, of which only 2GB (2^31) bytes may be used by the
			process. However, for the sake of making 64-bit arithmetic work below
			for file offets, we need to make sure that all 64 bits of liMapSize
			are initialized correctly.

			Note structured exception handling is used in case a MapViewOfFile call
			failed. That should never happen in this program, but in case it does,
			we should handle it. Since the possibility is so remote, it is faster
			to handle the exception when it occurs rather than test for failure in
			the loop.
			*/
			liBytesRemaining = liSrcFileSize;

			// stan bug fix
			liMapSize = dwMaxViewSize;

			// Make sure that the arithmetic below is correct during debugging.
			Debug.Assert(liMapSize.HighPart == 0);

			try
			{
				do
				{
					liMapSize = Math.Min(liBytesRemaining, liMapSize);

					liOffset = liSrcFileSize - liBytesRemaining;

					try
					{
						pSrc = MapViewOfFile(hSrcMap, FILE_MAP.FILE_MAP_READ, liOffset.HighPart, liOffset.LowPart, liMapSize.LowPart);
						try
						{
							pDst = MapViewOfFile(hDstMap, FILE_MAP.FILE_MAP_WRITE, liOffset.HighPart, liOffset.LowPart, liMapSize.LowPart);

							pSrc.CopyTo(pDst, liMapSize.LowPart);

						}
						finally
						{
							UnmapViewOfFile(pDst);
						}
					}
					finally
					{
						UnmapViewOfFile(pSrc);
					}

					liBytesRemaining -= liMapSize;

				} while (liBytesRemaining > 0);

				fResult = SUCCESS;
			}
			catch
			{
			}

			DONE:
			hSrcFile?.Dispose();
			hDstFile?.Dispose();
			hSrcMap?.Dispose();
			hDstMap?.Dispose();

			// Report to user only if a problem occurred.
			if (fResult != SUCCESS)
			{
				Console.Write("fcopy: copying failed.\n");
				DeleteFile(pszDstFileName);
			}

			return fResult;
		}

		[StructLayout(LayoutKind.Explicit)]
		[DebuggerDisplay(nameof(QuadPart))]
		internal struct ULARGE_INTEGER : IEquatable<ULARGE_INTEGER>, IEquatable<ulong>
		{
			[FieldOffset(0)]
			public uint LowPart;
			[FieldOffset(4)]
			public uint HighPart;
			[FieldOffset(0)]
			public ulong QuadPart;

			public static implicit operator ulong(ULARGE_INTEGER ul) => ul;
			public static implicit operator ULARGE_INTEGER (ulong ul) => new ULARGE_INTEGER { QuadPart = ul };

			public static bool operator ==(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.Equals(right);
			public static bool operator !=(ULARGE_INTEGER left, ULARGE_INTEGER right) => !(left == right);

			public override bool Equals(object obj) => obj is ULARGE_INTEGER uli && Equals(uli) || obj is ulong ul && Equals(ul);
			public bool Equals(ULARGE_INTEGER other) => QuadPart == other.QuadPart;
			public bool Equals(ulong other) => QuadPart == other;
			public override int GetHashCode() => HashCode.Combine(QuadPart);
		}

		private static void DEBUG_PRINT(string value) => Debug.Write(value);
	}
}