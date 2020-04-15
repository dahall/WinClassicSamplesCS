using System;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.DnsApi;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ws2_32;

namespace DNSAsyncQuery
{
	static partial class DnsQueryEx
	{
		private static SafeEventHandle QueryCompletedEvent;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct QUERY_CONTEXT
		{
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = DNS_MAX_NAME_BUFFER_LENGTH)]
			public string QueryName;

			public DNS_TYPE QueryType;
			public DNS_QUERY_OPTIONS QueryOptions;
			public DNS_QUERY_RESULT QueryResults;
			public DNS_QUERY_CANCEL QueryCancelContext;
		}

		private static int Main(string[] args)
		{
			const uint QueryTimeout = 5 * 1000; // 5 seconds

			// Allocate QueryContext
			AllocateQueryContext(out var QueryContext);

			// Create event
			QueryCompletedEvent = CreateEvent(default, true, false);
			if (QueryCompletedEvent is null || QueryCompletedEvent.IsInvalid)
			{
				var err = GetLastError();
				Console.Write("AllocateQueryContext failed with error {0}", err);
				return (int)err;
			}

			// Parse input arguments
			Win32Error Error = Win32Error.ERROR_SUCCESS;
			if ((Error = ParseArguments(args, out QueryContext.QueryName, out QueryContext.QueryType, out QueryContext.QueryOptions, out var ServerIp)).Failed)
			{
				return (int)Error;
			}

			// Initiate asynchronous DnsQuery: Note that QueryResults and QueryCancelContext should be valid till query completes.
			using var pqc = SafeCoTaskMemHandle.CreateFromStructure(QueryContext);
			var DnsQueryRequest = new DNS_QUERY_REQUEST
			{
				Version = DNS_QUERY_REQUEST_VERSION1,
				QueryName = QueryContext.QueryName,
				QueryType = QueryContext.QueryType,
				QueryOptions = QueryContext.QueryOptions,
				pQueryContext = pqc,
				pQueryCompletionCallback = QueryCompleteCallback
			};

			// If user specifies server, construct DNS_ADDR_ARRAY
			SafeAllocatedMemoryHandle DnsServerList = null;
			if (!string.IsNullOrEmpty(ServerIp))
			{
				var err = CreateDnsServerList(ServerIp, out DnsServerList);

				if (err != 0)
				{
					Console.Write("CreateDnsServerList failed with error {0}", err);
					goto exit;
				}

				DnsQueryRequest.pDnsServerList = DnsServerList;
			}

			// Initiate asynchronous query.
			Error = DnsQueryEx(DnsQueryRequest, ref QueryContext.QueryResults, out QueryContext.QueryCancelContext);

			// If DnsQueryEx() returns DNS_REQUEST_PENDING, Completion routine will be invoked. If not (when completed inline) completion
			// routine will not be invoked.
			if (Error != Win32Error.DNS_REQUEST_PENDING)
			{
				QueryCompleteCallback(pqc, ref QueryContext.QueryResults);
				goto exit;
			}

			// Wait for query completion for 5 seconds and initiate cancel if completion has not happened.
			if (WaitForSingleObject(QueryCompletedEvent, QueryTimeout) == WAIT_STATUS.WAIT_TIMEOUT)
			{
				// Initiate Cancel: Note that Cancel is just a request which will speed the process. It should still wait for the completion callback.
				Console.Write("The query took longer than {0} seconds to complete; cancelling the query...\n", QueryTimeout / 1000);

				DnsCancelQuery(QueryContext.QueryCancelContext);

				WaitForSingleObject(QueryCompletedEvent, INFINITE);
			}

			exit:

			DnsServerList?.Dispose();

			return (int)Error;
		}

		private static void AllocateQueryContext(out QUERY_CONTEXT QueryContext)
		{
			QueryContext = new QUERY_CONTEXT();
			QueryContext.QueryResults.Version = DNS_QUERY_REQUEST_VERSION1;
		}

		// Wrapper function that creates DNS_ADDR_ARRAY from IP address string.
		private static int CreateDnsServerList(string ServerIp, out SafeAllocatedMemoryHandle pDnsServerList)
		{
			DNS_ADDR_ARRAY DnsServerList = default;
			pDnsServerList = null;
			var Error = WSAStartup(Macros.MAKEWORD(2, 2), out _);
			if (Error != 0)
			{
				Console.Write("WSAStartup failed with {0}\n", Error);
				return Error;
			}

			var SockAddr = new SOCKADDR(new SOCKADDR_IN6());
			int AddressLength = SockAddr.Size;
			Error = WSAStringToAddress(ServerIp, ADDRESS_FAMILY.AF_INET, default, SockAddr, ref AddressLength);
			if (Error != 0)
			{
				AddressLength = SockAddr.Size;
				Error = WSAStringToAddress(ServerIp, ADDRESS_FAMILY.AF_INET6, default, SockAddr, ref AddressLength);
			}

			if (Error != 0)
			{
				Console.Write("WSAStringToAddress for {0} failed with error {1}\n", ServerIp, Error);
				goto exit;
			}

			DnsServerList.MaxCount = 1;
			DnsServerList.AddrCount = 1;
			DnsServerList.AddrArray = new DNS_ADDR[] { new DNS_ADDR { MaxSa = SockAddr.sa_data } };
			pDnsServerList = SafeCoTaskMemHandle.CreateFromStructure(DnsServerList);

			exit:

			WSACleanup();
			return Error;
		}

		// Callback function called by DNS as part of asynchronous query complete
		private static void QueryCompleteCallback(IntPtr pQueryContext, ref DNS_QUERY_RESULT pQueryResults)
		{
			using var recs = new SafeDnsRecordList(pQueryResults.pQueryRecords);
			if (pQueryResults.QueryStatus == Win32Error.ERROR_SUCCESS)
			{
				Console.Write("DnsQuery() succeeded.\n Query response records:\n");
				PrintDnsRecordList(recs);
			}
			else
			{
				Console.Write("DnsQuery() failed, {0}\n", pQueryResults.QueryStatus);
			}

			QueryCompletedEvent?.Set();
		}
	}
}