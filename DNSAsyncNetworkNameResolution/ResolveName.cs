using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ws2_32;

namespace DNSAsyncNetworkNameResolution;

internal static unsafe class ResolveName
{
	private const int MAX_ADDRESS_STRING_LENGTH = 64;

	private static ADDRINFOEXW[]? QueryResults;
	private static IntPtr hEvent;

	private static unsafe int Main(string[] args)
	{
		Win32Error Error = Win32Error.ERROR_SUCCESS;
		bool IsWSAStartupCalled = false;
		HANDLE CancelHandle = default;
		uint QueryTimeout = 5 * 1000; // 5 seconds

		// Validate the parameters

		if (args.Length != 1)
		{
			Console.Write("Usage: ResolveName <QueryName>\n");
			return 1;
		}

		// All Winsock functions require WSAStartup() to be called first

		Error = WSAStartup(Macros.MAKEWORD(2, 2), out _);
		if (Error.Failed)
		{
			Console.Write("WSAStartup failed with {0}\n", Error);
			return (int)Error.ToHRESULT();
		}

		IsWSAStartupCalled = true;

		ADDRINFOEXW Hints = new() { ai_family = ADDRESS_FAMILY.AF_UNSPEC };

		// Note that this is a simple sample that waits/cancels a single asynchronous query. The reader may extend this to support
		// multiple asynchronous queries.

		using var CompleteEvent = CreateEvent(default, true, false, default);

		if (CompleteEvent.IsInvalid)
		{
			Error = GetLastError();
			Console.Write("Failed to create completion event: Error {0}\n", Error);
			return (int)Error.ToHRESULT();
		}

		// Initiate asynchronous GetAddrInfoExW.
		//
		// Note GetAddrInfoEx can also be invoked asynchronously using an event in the overlapped object (Just set hEvent in the
		// Overlapped object and set default as completion callback.)
		//
		// This sample uses the completion callback method.

		// Asynchronous query context structure.
		NativeOverlapped QueryOverlapped = default;
		hEvent = CompleteEvent.DangerousGetHandle();
		Error = (Win32Error)GetAddrInfoExW(args[0], "http", NS.NS_ALL, default, &Hints, out var queryResults, default, &QueryOverlapped, QueryCompleteCallback, &CancelHandle);

		// If GetAddrInfoExW() returns WSA_IO_PENDING, GetAddrInfoExW will invoke the completion routine. If GetAddrInfoExW returned
		// anything else we must invoke the completion directly.

		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			QueryResults = queryResults.ToArray();
			QueryCompleteCallback((uint)Error, 0, &QueryOverlapped);
			goto exit;
		}

		// Wait for query completion for 5 seconds and cancel the query if it has not yet completed.

		if (WaitForSingleObject(CompleteEvent, QueryTimeout) == WAIT_STATUS.WAIT_TIMEOUT)
		{
			// Cancel the query: Note that the GetAddrInfoExCancelcancel call does not block, so we must wait for the completion routine
			// to be invoked. If we fail to wait, WSACleanup() could be called while an asynchronous query is still in progress,
			// possibly causing a crash.

			Console.Write("The query took longer than {0} seconds to complete; cancelling the query...\n", QueryTimeout / 1000);

			GetAddrInfoExCancel(CancelHandle);

			WaitForSingleObject(CompleteEvent, INFINITE);
		}

		exit:

		if (IsWSAStartupCalled)
		{
			WSACleanup();
		}

		return (int)Error.ToHRESULT();
	}

	// Callback function called by Winsock as part of asynchronous query complete

	private static unsafe void QueryCompleteCallback(uint Error, uint Bytes, NativeOverlapped* QueryOverlapped)
	{
		var AddrString = new StringBuilder(MAX_ADDRESS_STRING_LENGTH);
		uint AddressStringLength;

		if (Error != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("ResolveName failed with {0}\n", Error);
			goto exit;
		}

		Console.Write("ResolveName succeeded. Query Results:\n");

		if (QueryResults is not null)
		{
			foreach (var qr in QueryResults)
			{
				AddressStringLength = MAX_ADDRESS_STRING_LENGTH;

				WSAAddressToString(new SOCKADDR(qr.ai_addr), qr.ai_addrlen, default, AddrString, ref AddressStringLength);

				Console.Write("Ip Address: {0}\n", AddrString);
			}
		}

		exit:

		// Notify caller that the query completed

		SetEvent(new SafeEventHandle(QueryOverlapped->EventHandle, false));
		return;
	}
}