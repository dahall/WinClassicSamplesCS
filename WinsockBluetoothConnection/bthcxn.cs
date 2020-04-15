using System;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ws2_32;

namespace Vanara.PInvoke
{
	public static class Bthprops
	{
		public const uint BTHPROTO_RFCOMM = 0x0003;
		public const uint BTHPROTO_L2CAP = 0x0100;

		public const uint SOL_RFCOMM = BTHPROTO_RFCOMM;
		public const uint SOL_L2CAP = BTHPROTO_L2CAP;
		public const uint SOL_SDP = 0x0101;

		//
		// SOCKET OPTIONS
		//
		public const uint SO_BTH_AUTHENTICATE = 0x80000001;  // optlen=sizeof(ULONG), optval = &(ULONG)TRUE/FALSE 
		public const uint SO_BTH_ENCRYPT = 0x00000002;  // optlen=sizeof(ULONG), optval = &(ULONG)TRUE/FALSE
		public const uint SO_BTH_MTU = 0x80000007;  // optlen=sizeof(ULONG), optval = &mtu
		public const uint SO_BTH_MTU_MAX = 0x80000008;  // optlen=sizeof(ULONG), optval = &max. mtu
		public const uint SO_BTH_MTU_MIN = 0x8000000a;  // optlen=sizeof(ULONG), optval = &min. mtu

		public const uint BT_PORT_ANY = unchecked((uint)-1);
		public const uint BT_PORT_MIN = 0x1;
		public const uint BT_PORT_MAX = 0xffff;
		public const uint BT_PORT_DYN_FIRST = 0x1001;

		public const int BTH_MAX_NAME_SIZE = 248;

		/// <summary>The <c>SOCKADDR_BTH</c> structure is used in conjunction with Bluetooth socket operations, defined by address family AF_BTH.</summary>
		/// <remarks>
		/// <para>When used with the bind function on client applications, the <c>port</c> member must be zero to enable an appropriate local endpoint to be assigned. When used with <c>bind</c> on server applications, the <c>port</c> member must be a valid port number or BT_PORT_ANY; ports automatically assigned using BT_PORT_ANY may be queried subsequently with a call to the getsockname function. The valid range for requesting a specific RFCOMM port is 1 through 30.</para>
		/// <para>When using the connect function when <c>serviceClassId</c> is not provided, the port should directly specify the remote port number to which a <c>connect</c> operation is requested. Using the <c>port</c> member instead of the <c>serviceClassId</c> member requires the application to perform its own service (SDP) search before attempting the <c>connect</c> operation.</para>
		/// </remarks>
		// https://docs.microsoft.com/en-us/windows/win32/api/ws2bth/ns-ws2bth-sockaddr_bth
		// typedef struct _SOCKADDR_BTH { USHORT addressFamily; BTH_ADDR btAddr; GUID serviceClassId; ULONG port; } SOCKADDR_BTH, *PSOCKADDR_BTH;
		[PInvokeData("ws2bth.h", MSDNShortId = "e8eefa1d-94fa-45f3-a7c2-ea12a372a43b")]
		[StructLayout(LayoutKind.Sequential)]
		public struct SOCKADDR_BTH
		{
			/// <summary>Address family of the socket. This member is always AF_BTH.</summary>
			public ADDRESS_FAMILY addressFamily;
			/// <summary>Address of the target Bluetooth device. When used with the bind function, must be zero or a valid local radio address. If zero, a valid local Bluetooth device address is assigned when the connect or accept function is called. When used with the <c>connect</c> function, a valid remote radio address must be specified.</summary>
			public ulong btAddr;
			/// <summary>Service Class Identifier of the socket. When used with the bind function, serviceClassId is ignored. Also ignored if the port is specified. For the connect function, specifies the unique Bluetooth service class ID of the service to which it wants to connect. If the peer device has more than one port that corresponds to the service class identifier, the <c>connect</c> function attempts to connect to the first valid service; this mechanism can be used without prior SDP queries.</summary>
			public Guid serviceClassId;
			/// <summary>RFCOMM channel associated with the socket. See Remarks.</summary>
			public uint port;

			public byte[] GetAddressBytes() => ((IntPtr)new PinnedObject(this)).ToArray<byte>(Marshal.SizeOf(typeof(SOCKADDR_BTH)));

			public static explicit operator SOCKADDR(SOCKADDR_BTH sblth) => SOCKADDR.CreateFromStructure(sblth);
		}
	}
}

namespace WinsockBluetoothConnection
{
	using static Vanara.PInvoke.Bthprops;

	public class ScopedAction : IDisposable
	{
		private Action OnClose;
		public ScopedAction(Action onClose) => OnClose = onClose;
		void IDisposable.Dispose() => OnClose?.Invoke();
	}

	static class BthCxn
	{
		// {B62C4E8D-62CC-404b-BBBF-BF3E3BBB1374}
		static Guid g_guidServiceClass = new Guid(0xb62c4e8d, 0x62cc, 0x404b, 0xbb, 0xbf, 0xbf, 0x3e, 0x3b, 0xbb, 0x13, 0x74);

		const string CXN_TEST_DATA_STRING = "~!@#$%^&*()-_=+?<>1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		static readonly int CXN_TRANSFER_DATA_LENGTH = CXN_TEST_DATA_STRING.Length;

		const uint CXN_BDADDR_STR_LEN = 17;   // 6 two-digit hex values plus 5 colons
		const uint CXN_MAX_INQUIRY_RETRY = 3;
		const uint CXN_DELAY_NEXT_INQUIRY = 15;
		const uint CXN_SUCCESS = 0;
		const uint CXN_ERROR = 1;
		const int CXN_DEFAULT_LISTEN_BACKLOG = 4;

		static string g_szRemoteName = string.Empty;  // 1 extra for trailing NULL character
		static string g_szRemoteAddr = string.Empty; // 1 extra for trailing NULL character
		static int g_ulMaxCxnCycles = 1;

		static int Main(string[] args)
		{
			//
			// Parse the command line
			//
			var ulRetCode = ParseCmdLine(args);
			if (CXN_ERROR == ulRetCode)
			{
				//
				// Command line syntax error. Display cmd line help
				//
				ShowCmdLineHelp();
			}
			else if (CXN_SUCCESS != ulRetCode)
			{
				Console.Write("-FATAL- | Error in parsing command line\n");
			}

			//
			// Ask for Winsock version 2.2.
			//
			if (CXN_SUCCESS == ulRetCode)
			{
				ulRetCode = (uint)WSAStartup(Macros.MAKEWORD(2, 2), out var WSAData);
				if (CXN_SUCCESS != ulRetCode)
				{
					Console.Write("-FATAL- | Unable to initialize Winsock version 2.2\n");
				}
			}

			if (CXN_SUCCESS == ulRetCode)
			{

				//
				// Note, this app "prefers" the name if provided, but it is app-specific
				// Other applications may provide more generic treatment.
				//
				if (string.IsNullOrEmpty(g_szRemoteName))
				{
					//
					// Get address from the name of the remote device and run the application
					// in client mode
					//
					ulRetCode = NameToBthAddr(g_szRemoteName, out var RemoteBthAddr);
					if (CXN_SUCCESS != ulRetCode)
					{
						Console.Write("-FATAL- | Unable to get address of the remote radio having name {0}\n", g_szRemoteName);
					}

					if (CXN_SUCCESS == ulRetCode)
					{
						ulRetCode = RunClientMode(RemoteBthAddr, g_ulMaxCxnCycles);
					}

				}
				else if (!string.IsNullOrEmpty(g_szRemoteAddr))
				{
					//
					// Get address from formated address-string of the remote device and
					// run the application in client mode
					//
					SOCKADDR_BTH RemoteBthAddr = default;
					int iAddrLen = Marshal.SizeOf(RemoteBthAddr);
					using var pRemoteBthAddr = new PinnedObject(RemoteBthAddr);
					ulRetCode = (uint)WSAStringToAddress(g_szRemoteAddr, ADDRESS_FAMILY.AF_BTH, default, new SOCKADDR(pRemoteBthAddr), ref iAddrLen);
					if (CXN_SUCCESS != ulRetCode)
					{
						Console.Write("-FATAL- | Unable to get address of the remote radio having formated address-string {0}\n", g_szRemoteAddr);
					}

					if (CXN_SUCCESS == ulRetCode)
					{
						ulRetCode = RunClientMode(RemoteBthAddr, g_ulMaxCxnCycles);
					}
				}
				else
				{
					//
					// No remote name/address specified. Run the application in server mode
					//
					ulRetCode = RunServerMode(g_ulMaxCxnCycles);
				}
			}

			return (int)ulRetCode;
		}

		// NameToBthAddr converts a bluetooth device name to a bluetooth address,
		// if required by performing inquiry with remote name requests.
		// This function demonstrates device inquiry, with optional LUP flags.
		//
		static uint NameToBthAddr(string pszRemoteName, out SOCKADDR_BTH RemoteBtAddr)
		{
			uint iResult = CXN_SUCCESS;
			bool bRemoteDeviceFound = false;

			RemoteBtAddr = default;
			using var pRemoteBtAddr = new PinnedObject(RemoteBtAddr);

			//
			// Search for the device with the correct name
			//
			for (var iRetryCount = 0; !bRemoteDeviceFound && iRetryCount < CXN_MAX_INQUIRY_RETRY; iRetryCount++)
			{
				//
				// WSALookupService is used for both service search and device inquiry
				// LUP_CONTAINERS is the flag which signals that we're doing a device inquiry.
				//
				var ulFlags = LUP.LUP_CONTAINERS;

				//
				// Friendly device name (if available) will be returned in lpszServiceInstanceName
				//
				ulFlags |= LUP.LUP_RETURN_NAME;

				//
				// BTH_ADDR will be returned in lpcsaBuffer member of WSAQUERYSET
				//
				ulFlags |= LUP.LUP_RETURN_ADDR;

				if (0 == iRetryCount)
				{
					Console.Write("*INFO* | Inquiring device from cache...\n");
				}
				else
				{
					//
					// Flush the device cache for all inquiries, except for the first inquiry
					//
					// By setting LUP_FLUSHCACHE flag, we're asking the lookup service to do
					// a fresh lookup instead of pulling the information from device cache.
					//
					ulFlags |= LUP.LUP_FLUSHCACHE;

					//
					// Pause for some time before all the inquiries after the first inquiry
					//
					// Remote Name requests will arrive after device inquiry has
					// completed. Without a window to receive IN_RANGE notifications,
					// we don't have a direct mechanism to determine when remote
					// name requests have completed.
					//
					Console.Write("*INFO* | Unable to find device. Waiting for {0} seconds before re-inquiry...\n", CXN_DELAY_NEXT_INQUIRY);
					Sleep(CXN_DELAY_NEXT_INQUIRY * 1000);

					Console.Write("*INFO* | Inquiring device ...\n");
				}

				//
				// Start the lookup service
				//
				iResult = CXN_SUCCESS;
				var bContinueLookup = false;
				uint ulPQSSize = (uint)Marshal.SizeOf(typeof(WSAQUERYSET));
				iResult = (uint)WSALookupServiceBegin(new WSAQUERYSET(NS.NS_BTH), ulFlags, out var hLookup);

				//
				// Even if we have an error, we want to continue until we
				// reach the CXN_MAX_INQUIRY_RETRY
				//
				if (0 == iResult && default != hLookup)
				{
					bContinueLookup = true;
				}
				else if (0 < iRetryCount)
				{
					Console.Write("=CRITICAL= | WSALookupServiceBegin() failed with error code {0}, WSAGetLastError = {1}\n", iResult, WSAGetLastError());
					break;
				}

				//
				// End the lookup service when out of scope
				//
				using var closeLookup = new ScopedAction(() => WSALookupServiceEnd(hLookup));
				using var pWSAQuerySet = SafeCoTaskMemHandle.CreateFromStructure<WSAQUERYSET>();
				while (bContinueLookup)
				{
					//
					// Get information about next bluetooth device
					//
					// Note you may pass the same WSAQUERYSET from LookupBegin
					// as int as you don't need to modify any of the pointer
					// members of the structure, etc.
					//
					// ZeroMemory(pWSAQuerySet, ulPQSSize);
					// pWSAQuerySet->dwNameSpace = NS_BTH;
					// pWSAQuerySet->dwSize = sizeof(WSAQUERYSET);
					if (0 == WSALookupServiceNext(hLookup, ulFlags, ref ulPQSSize, pWSAQuerySet))
					{
						var querySet = pWSAQuerySet.ToStructure<WSAQUERYSET>();
						//
						// Compare the name to see if this is the device we are looking for.
						//
						if (querySet.lpszServiceInstanceName != default && 0 == StringComparer.OrdinalIgnoreCase.Compare(querySet.lpszServiceInstanceName, pszRemoteName))
						{
							//
							// Found a remote bluetooth device with matching name.
							// Get the address of the device and exit the lookup.
							//
							querySet.lpcsaBuffer.ToStructure<CSADDR_INFO>().RemoteAddr.lpSockAddr.CopyTo(pRemoteBtAddr, Marshal.SizeOf(RemoteBtAddr));
							bRemoteDeviceFound = true;
							bContinueLookup = false;
						}
					}
					else
					{
						iResult = (uint)(int)WSAGetLastError();
						if (Win32Error.WSA_E_NO_MORE == iResult)
						{ //No more data
						  //
						  // No more devices found. Exit the lookup.
						  //
							bContinueLookup = false;
						}
						else if (Win32Error.WSAEFAULT == iResult)
						{
							//
							// The buffer for QUERYSET was insufficient.
							// In such case 3rd parameter "ulPQSSize" of function "WSALookupServiceNext()" receives
							// the required size. So we can use this parameter to reallocate memory for QUERYSET.
							//
							pWSAQuerySet.Size = ulPQSSize;
							if (pWSAQuerySet.IsInvalid)
							{
								Console.Write("!ERROR! | Unable to allocate memory for WSAQERYSET\n");
								iResult = NTStatus.STATUS_NO_MEMORY;
								bContinueLookup = false;
							}
						}
						else
						{
							Console.Write("=CRITICAL= | WSALookupServiceNext() failed with error code {0}\n", iResult);
							bContinueLookup = false;
						}
					}
				}

				if (NTStatus.STATUS_NO_MEMORY == iResult)
				{
					break;
				}
			}

			if (bRemoteDeviceFound)
			{
				iResult = CXN_SUCCESS;
			}
			else
			{
				iResult = CXN_ERROR;
			}

			return iResult;
		}

		//
		// RunClientMode runs the application in client mode. It opens a socket, connects it to a
		// remote socket, transfer some data over the connection and closes the connection.
		//
		static uint RunClientMode(SOCKADDR_BTH RemoteAddr, int iMaxCxnCycles)
		{
			uint ulRetCode = CXN_SUCCESS;
			SOCKADDR_BTH SockAddrBthServer = RemoteAddr;

			using var pszData = new SafeCoTaskMemString(CXN_TEST_DATA_STRING);
			if (pszData.IsInvalid)
			{
				ulRetCode = NTStatus.STATUS_NO_MEMORY;
				Console.Write("=CRITICAL= | HeapAlloc failed | out of memory, gle = [{0}] \n", GetLastError());
			}

			//
			// Setting address family to AF_BTH indicates winsock2 to use Bluetooth sockets
			// Port should be set to 0 if ServiceClassId is spesified.
			//
			SockAddrBthServer.addressFamily = ADDRESS_FAMILY.AF_BTH;
			SockAddrBthServer.serviceClassId = g_guidServiceClass;
			SockAddrBthServer.port = 0;

			//
			// Run the connection/data-transfer for user specified number of cycles
			//
			for (var iCxnCount = 0; 0 == ulRetCode && (iCxnCount < iMaxCxnCycles || iMaxCxnCycles == 0); iCxnCount++)
			{

				Console.Write("\n");

				//
				// Open a bluetooth socket using RFCOMM protocol
				//
				using var LocalSocket = socket(ADDRESS_FAMILY.AF_BTH, SOCK.SOCK_STREAM, BTHPROTO_RFCOMM);
				if (LocalSocket.IsInvalid)
				{
					Console.Write("=CRITICAL= | socket() call failed. WSAGetLastError = [{0}]\n", WSAGetLastError());
					ulRetCode = CXN_ERROR;
					break;
				}

				//
				// Connect the socket (pSocket) to a given remote socket represented by address (pServerAddr)
				//
				if (SOCKET_ERROR == connect(LocalSocket, (SOCKADDR)SockAddrBthServer, Marshal.SizeOf<SOCKADDR_BTH>()))
				{
					Console.Write("=CRITICAL= | connect() call failed. WSAGetLastError=[{0}]\n", WSAGetLastError());
					ulRetCode = CXN_ERROR;
					break;
				}

				//
				// send() call indicates winsock2 to send the given data
				// of a specified length over a given connection.
				//
				Console.Write("*INFO* | Sending following data string:\n{0}\n", pszData);
				if (SOCKET_ERROR == send(LocalSocket, pszData, CXN_TRANSFER_DATA_LENGTH, 0))
				{
					Console.Write("=CRITICAL= | send() call failed w/socket = [0x{0:X}], szData = [{1}], dataLen = [{2}]. WSAGetLastError=[{3}]\n", LocalSocket, pszData.ToString(), (ulong)CXN_TRANSFER_DATA_LENGTH, WSAGetLastError());
					ulRetCode = CXN_ERROR;
					break;
				}

				//
				// Close the socket
				//
				if (SOCKET_ERROR == closesocket(LocalSocket))
				{
					Console.Write("=CRITICAL= | closesocket() call failed w/socket = [0x{0:X}]. WSAGetLastError=[{1}]\n", LocalSocket, WSAGetLastError());
					ulRetCode = CXN_ERROR;
					break;
				}

			}

			return ulRetCode;
		}

		//
		// RunServerMode runs the application in server mode. It opens a socket, connects it to a
		// remote socket, transfer some data over the connection and closes the connection.
		//

		const string CXN_INSTANCE_STRING = "Sample Bluetooth Server";

		static uint RunServerMode(int iMaxCxnCycles)
		{
			uint ulRetCode = CXN_SUCCESS;
			SafeSOCKET LocalSocket = SafeSOCKET.INVALID_SOCKET;
			SOCKADDR_BTH SockAddrBthLocal = default;

			//
			// Open a bluetooth socket using RFCOMM protocol
			//
			if (CXN_SUCCESS == ulRetCode)
			{
				LocalSocket = socket(ADDRESS_FAMILY.AF_BTH, SOCK.SOCK_STREAM, BTHPROTO_RFCOMM);
				if (LocalSocket == SOCKET.INVALID_SOCKET)
				{
					Console.Write("=CRITICAL= | socket() call failed. WSAGetLastError = [{0}]\n", WSAGetLastError());
					ulRetCode = CXN_ERROR;
				}
			}

			if (CXN_SUCCESS == ulRetCode)
			{

				//
				// Setting address family to AF_BTH indicates winsock2 to use Bluetooth port
				//
				SockAddrBthLocal.addressFamily = ADDRESS_FAMILY.AF_BTH;
				SockAddrBthLocal.port = BT_PORT_ANY;

				//
				// bind() associates a local address and port combination
				// with the socket just created. This is most useful when
				// the application is a server that has a well-known port
				// that clients know about in advance.
				//
				if (SOCKET_ERROR == bind(LocalSocket, (SOCKADDR)SockAddrBthLocal, Marshal.SizeOf(SockAddrBthLocal)))
				{
					Console.Write("=CRITICAL= | bind() call failed w/socket = [0x{0:X}]. WSAGetLastError=[{1}]\n", LocalSocket, WSAGetLastError());
					ulRetCode = CXN_ERROR;
				}
			}

			using var pSockAddrBthLocal = new PinnedObject(SockAddrBthLocal);
			if (CXN_SUCCESS == ulRetCode)
			{
				var iAddrLen = Marshal.SizeOf<SOCKADDR_BTH>();
				if (SOCKET_ERROR == getsockname(LocalSocket, new SOCKADDR(pSockAddrBthLocal), ref iAddrLen))
				{
					Console.Write("=CRITICAL= | getsockname() call failed w/socket = [0x{0:X}]. WSAGetLastError=[{1}]\n", LocalSocket, WSAGetLastError());
					ulRetCode = CXN_ERROR;
				}
			}

			if (CXN_SUCCESS == ulRetCode)
			{
				//
				// CSADDR_INFO
				//
				var scktAddr = new SOCKET_ADDRESS { iSockaddrLength = Marshal.SizeOf(typeof(SOCKADDR_BTH)), lpSockAddr = pSockAddrBthLocal };
				var csaddrInfo = new CSADDR_INFO
				{
					LocalAddr = scktAddr,
					RemoteAddr = scktAddr,
					iSocketType = SOCK.SOCK_STREAM,
					iProtocol = (IPPROTO)BTHPROTO_RFCOMM
				};

				//
				// If we got an address, go ahead and advertise it.
				//
				using var pg_guidServiceClass = new PinnedObject(g_guidServiceClass);
				using var lpCSAddrInfo = SafeCoTaskMemHandle.CreateFromStructure(csaddrInfo);
				var wsaQuerySet = new WSAQUERYSET
				{
					dwSize = (uint)Marshal.SizeOf(typeof(WSAQUERYSET)),
					lpServiceClassId = (IntPtr)pg_guidServiceClass,
					lpszServiceInstanceName = $"{Environment.MachineName} {CXN_INSTANCE_STRING}",
					lpszComment = "Example Service instance registered in the directory service through RnR",
					dwNameSpace = NS.NS_BTH,
					dwNumberOfCsAddrs = 1, // Must be 1.
					lpcsaBuffer = lpCSAddrInfo // Req'd.
				};

				//
				// As int as we use a blocking accept(), we will have a race
				// between advertising the service and actually being ready to
				// accept connections. If we use non-blocking accept, advertise
				// the service after accept has been called.
				//
				if (SOCKET_ERROR == WSASetService(wsaQuerySet, WSAESETSERVICEOP.RNRSERVICE_REGISTER, 0))
				{
					Console.Write("=CRITICAL= | WSASetService() call failed. WSAGetLastError=[{0}]\n", WSAGetLastError());
					ulRetCode = CXN_ERROR;
				}
			}

			//
			// listen() call indicates winsock2 to listen on a given socket for any incoming connection.
			//
			if (CXN_SUCCESS == ulRetCode)
			{
				if (SOCKET_ERROR == listen(LocalSocket, CXN_DEFAULT_LISTEN_BACKLOG))
				{
					Console.Write("=CRITICAL= | listen() call failed w/socket = [0x{0:X}]. WSAGetLastError=[{1}]\n", LocalSocket, WSAGetLastError());
					ulRetCode = CXN_ERROR;
				}
			}

			if (CXN_SUCCESS == ulRetCode)
			{

				for (var iCxnCount = 0; CXN_SUCCESS == ulRetCode && (iCxnCount < iMaxCxnCycles || iMaxCxnCycles == 0); iCxnCount++)
				{

					Console.Write("\n");

					//
					// accept() call indicates winsock2 to wait for any
					// incoming connection request from a remote socket.
					// If there are already some connection requests on the queue,
					// then accept() extracts the first request and creates a new socket and
					// returns the handle to this newly created socket. This newly created
					// socket represents the actual connection that connects the two sockets.
					//
					var ClientSocket = accept(LocalSocket); // TODO: using
					using var pClientSocket = new ScopedAction(() => closesocket(ClientSocket));
					if (ClientSocket == SOCKET.INVALID_SOCKET)
					{
						Console.Write("=CRITICAL= | accept() call failed. WSAGetLastError=[{0}]\n", WSAGetLastError());
						ulRetCode = CXN_ERROR;
						break; // Break out of the for loop
					}

					//
					// Read data from the incoming stream
					//
					bool bContinue = true;
					using var pszDataBuffer = new SafeCoTaskMemHandle(CXN_TRANSFER_DATA_LENGTH);
					if (pszDataBuffer.IsInvalid)
					{
						Console.Write("-FATAL- | HeapAlloc failed | out of memory | gle = [{0}] \n", GetLastError());
						ulRetCode = CXN_ERROR;
						break;
					}
					IntPtr pszDataBufferIndex = pszDataBuffer;
					var uiTotalLengthReceived = 0;
					while (bContinue && uiTotalLengthReceived < CXN_TRANSFER_DATA_LENGTH)
					{
						//
						// recv() call indicates winsock2 to receive data
						// of an expected length over a given connection.
						// recv() may not be able to get the entire length
						// of data at once. In such case the return value,
						// which specifies the number of bytes received,
						// can be used to calculate how much more data is
						// pending and accordingly recv() can be called again.
						//
						var iLengthReceived = recv(ClientSocket, pszDataBufferIndex, CXN_TRANSFER_DATA_LENGTH - uiTotalLengthReceived, 0);

						switch (iLengthReceived)
						{
							case 0: // socket connection has been closed gracefully
								bContinue = false;
								break;

							case SOCKET_ERROR:
								Console.Write("=CRITICAL= | recv() call failed. WSAGetLastError=[{0}]\n", WSAGetLastError());
								bContinue = false;
								ulRetCode = CXN_ERROR;
								break;

							default:

								//
								// Make sure we have enough room
								//
								if (iLengthReceived > CXN_TRANSFER_DATA_LENGTH - uiTotalLengthReceived)
								{
									Console.Write("=CRITICAL= | received too much data\n");
									bContinue = false;
									ulRetCode = CXN_ERROR;
									break;
								}

								pszDataBufferIndex += iLengthReceived;
								uiTotalLengthReceived += iLengthReceived;
								break;
						}
					}

					if (CXN_SUCCESS == ulRetCode)
					{

						if (CXN_TRANSFER_DATA_LENGTH != uiTotalLengthReceived)
						{
							Console.Write("+WARNING+ | Data transfer aborted mid-stream. Expected Length = [{0}], Actual Length = [{1}]\n", (ulong)CXN_TRANSFER_DATA_LENGTH, uiTotalLengthReceived);
						}
						Console.Write("*INFO* | Received following data string from remote device:\n{0}\n", pszDataBuffer.ToString(uiTotalLengthReceived));
					}
				}
			}

			return ulRetCode;
		}

		//
		// ShowCmdLineSyntaxHelp displays the command line usage
		//
		static void ShowCmdLineHelp()
		{
			Console.Write("\n Bluetooth Connection Sample application for demonstrating connection and data transfer." +
				"\n" +
				"\n" +
				"\n BTHCxn.exe [-n<RemoteName> | -a<RemoteAddress>] " +
				"\n [-c<ConnectionCycles>]" +
				"\n" +
				"\n" +
				"\n Switches applicable for Client mode:" +
				"\n -n<RemoteName> Specifies name of remote BlueTooth-Device." +
				"\n" +
				"\n -a<RemoteAddress> Specifies address of remote BlueTooth-Device." +
				"\n The address is in form XX:XX:XX:XX:XX:XX" +
				"\n where XX is a hexidecimal byte" +
				"\n" +
				"\n One of the above two switches is required for client." +
				"\n" +
				"\n" +
				"\n Switches applicable for both Client and Server mode:" +
				"\n -c<ConnectionCycles> Specifies number of connection cycles." +
				"\n Default value for this parameter is 1. Specify 0 to " +
				"\n run infinite number of connection cycles." +
				"\n" +
				"\n" +
				"\n" +
				"\n Command Line Examples:" +
				"\n \"BTHCxn.exe -c0\"" +
				"\n Runs the BTHCxn server for infinite connection cycles." +
				"\n The application reports minimal information onto the cmd window." +
				"\n" +
				"\n \"BTHCxn.exe -nServerDevice -c50\"" +
				"\n Runs the BTHCxn client connecting to remote device (having name " +
				"\n \"ServerDevice\" for 50 connection cycles." +
				"\n The application reports minimal information onto the cmd window." +
				"\n");
		}

		//
		// ParseCmdLine parses the command line and sets the global variables accordingly.
		// It returns CXN_SUCCESS if successful and CXN_ERROR if it detected a mistake in the
		// command line parameter used.
		//
		static uint ParseCmdLine(string[] args)
		{
			SizeT cbStrLen;
			uint ulRetCode = CXN_SUCCESS;

			for (int i = 0; i < args.Length; i++)
			{
				string pszToken = args[i];
				if (pszToken[0] == '-' || pszToken[0] == '/')
				{
					char token;

					//
					// skip over the "-" or "/"
					//
					pszToken = pszToken.Substring(1);

					//
					// Get the command line option
					//
					token = pszToken[0];

					//
					// Go one past the option the option-data
					//
					pszToken = pszToken.Substring(1);

					//
					// Get the option-data
					//
					switch (token)
					{
						case 'n':
							cbStrLen = pszToken.Length;
							if (0 < cbStrLen && BTH_MAX_NAME_SIZE >= cbStrLen)
							{
								g_szRemoteName = pszToken;
							}
							else
							{
								ulRetCode = CXN_ERROR;
								Console.Write("!ERROR! | cmd line | Unable to parse -n<RemoteName>, length error (min 1 byte, max {0} chars)\n", BTH_MAX_NAME_SIZE);
							}
							break;

						case 'a':
							cbStrLen = pszToken.Length;
							if (CXN_BDADDR_STR_LEN == cbStrLen)
							{
								g_szRemoteAddr = pszToken;
							}
							else
							{
								ulRetCode = CXN_ERROR;
								Console.Write("!ERROR! | cmd line | Unable to parse -a<RemoteAddress>, Remote bluetooth radio address string length expected {0} | Found: {1})\n", CXN_BDADDR_STR_LEN, (ulong)cbStrLen);
							}
							break;

						case 'c':
							if (0 < pszToken.Length)
							{
								if (!int.TryParse(pszToken, out g_ulMaxCxnCycles) || 0 > g_ulMaxCxnCycles)
								{
									ulRetCode = CXN_ERROR;
									Console.Write("!ERROR! | cmd line | Must provide +ve or 0 value with -c option\n");
								}
							}
							else
							{
								ulRetCode = CXN_ERROR;
								Console.Write("!ERROR! | cmd line | Must provide a value with -c option\n");
							}
							break;

						case '?':
						case 'h':
						case 'H':
						default:
							ulRetCode = CXN_ERROR;
							break;
					}
				}
				else
				{
					ulRetCode = CXN_ERROR;
					Console.Write("!ERROR! | cmd line | Bad option prefix, use '/' or '-' \n");
				}
			}

			return ulRetCode;
		}
	}
}