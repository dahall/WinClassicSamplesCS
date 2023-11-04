using System.Net;
using System.Runtime.InteropServices;
using Vanara;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Qwave;
using static Vanara.PInvoke.Ws2_32;

//******************************************************************************
// Global defines
//******************************************************************************

// Size of the data part of each datagram that is exchanged between the
// client and the server. This does not count the IP and UDP header.
const int DATAGRAM_SIZE = 1400;

// Number of bursts we are aiming for per second
const uint BURSTS_PER_SECOND = 40;

// Port to be used for communication, 
// 40007 in hex in network byte order 
const int PORT = 0x479c;

//******************************************************************************
// Global variables
//******************************************************************************

// Array used by both the client routine and the server routine to
// send and receive data
using SafeNativeArray<byte> dataBuffer = new(DATAGRAM_SIZE);

//******************************************************************************
// Routine: 
//      main
//
// Description:
//      Entry point. Verifies that the number of parameters is enough to 
//      ascertain whether this instance is to be a client or server.
// 
//******************************************************************************
if (args.Length < 1)
	return help();

if (args[0] == "-s")
	server();
else if (args[0] == "-c")
	client(args);

help();
return 1;

//******************************************************************************
// Routine: 
//      help
//
// Description:
//      This routine prints out the usage information for the program
//
//******************************************************************************
int help()
{
	Console.WriteLine(@"USAGE:
\tSERVER: qossample -s
\tCLIENT: qossample -c IP-ADDRESS BIT-RATE\n
\tIn the following example, the application would try to send
\t20 Mb of traffic to the host at 10.0.0.1:
\tqossample -c 10.0.0.1 20000000
");
	return 0;
}

//******************************************************************************
// Routine: 
// socketCreate
//
// Description:
// This routine prepares the socket for the client instance. To do so,
// it converts the address parameter from string to IP address. Then
// it creates a UDP socket which it connects to this destination.
//
// Since we will use TransmitPackets for our send operations, the function
// pointer is obtained from Winsock.
// 
//******************************************************************************
void socketCreate([In] string destination, out SOCKET socket, out ADDRESS_FAMILY addressFamily, out LPFN_TRANSMITPACKETS transmitPackets)
{
	// Start Winsock
	using var x = SafeWSA.Initialize();

	// First attempt to convert the string to an IPv4 address
	IPEndPoint mDestAddr = IPEndPoint.Parse(destination);

	// Set the destination port.
	mDestAddr.Port = PORT;

	// Copy the address family back to ref caller addressFamily = destAddr.ss_family;

	// Create a UDP ref socket
	using SOCKADDR destAddr = new(mDestAddr);
	socket = WSASocket(addressFamily = destAddr.sa_family, SOCK.SOCK_DGRAM, 0, default, 0, WSA_FLAG.WSA_FLAG_OVERLAPPED);
	if (socket.IsNull)
	{
		throw WSAGetLastError().GetException()!;
	}

	// Connect the new socket to the destination
	WSAConnect(socket, destAddr, destAddr.Size).ThrowIfFailed();

	// Query the function pointer for the TransmitPacket function

	// Guid of the TransmitPacket Winsock2 function which we will
	// use to send the traffic at the client side.
	using SafeCoTaskMemStruct<Guid> TransmitPacketsGuid = WSAID_TRANSMITPACKETS;
	IntPtr pTransmitPackets = default;
	WSAIoctl(socket, SIO_GET_EXTENSION_FUNCTION_POINTER, TransmitPacketsGuid, TransmitPacketsGuid.Size, pTransmitPackets, (uint)IntPtr.Size, out var bytesReturned).ThrowIfFailed();
	transmitPackets = Marshal.GetDelegateForFunctionPointer<LPFN_TRANSMITPACKETS>(pTransmitPackets);
}
	   
//******************************************************************************
// Routine: 
// client
//
// Description:
// This routine creates a datagram socket which it connects to the remote
// IP address. This socket is then added to a QOS flow. The application
// uses the flow to rate control its outgoing traffic. Packets on this flow
// will be prioritized if the network path between the client and receiver
// support prioritization. Specifically, if the receiver is:
//
// A) On-link (same subnet), both 802.1Q and DSCP are applied subject to
// available bandwidth and network support.
// B) Off-link (different subnet), only DSCP is applied regardless of
// available bandwidth.
//
// Moreover, the application queries the characteristics of the network 
// path for the socket. If estimates are available, the application:
//
// 1) may readjust its throughput so as to not cause congestion on the 
// network.
// 2) will be notified when the network enters congestion. As a result,
// it will lower it's throughput to 1/10 the targeted rate.
// 3) will be notified when the network is no longer congested and enough
// bandwidth is available for the application to return to its targeted
// rate.
//
//******************************************************************************
void client([In] string[] args)
{
	// Address family of the datagram socket
	ADDRESS_FAMILY addressFamily;

	// Socket for our traffic experiment
	SOCKET socket;

	// Function pointer to the TransmitPacket Winsock2 function
	LPFN_TRANSMITPACKETS transmitPacketsFn;

	// Target packet rate and bit rate this application will aim to send
	// If there is no congestion
	uint targetPacketRate, targetBitRate;
	// Current rate at which the application is sending. If the network is
	// congested, this will be less than the target rate
	uint currentPacketRate, currentBitRate;
	// Counters for the achieved packet rate and achieved bit rate
	uint achievedPacketRate, achievedBitRate;

	// Handle to the QOS subsystem
	// Returned by QOSCreateHandle
	SafeHQOS qosHandle;
	// ID of the QOS flow we will create for the socket. 
	// Returned by QOSAddSocketToFlow
	uint flowID = 0;
	// Result of QOSQueryFlow
	QOS_FLOW_FUNDAMENTALS flowFund = default;
	// Parameter to QOSSetFlow
	QOS_FLOWRATE_OUTGOING flowRate = default;
	// If true, the QOS subsystem is running network experiments on the
	// network path to the destination. If false, estimates are not available.
	// The flow is still marked and shaped.
	bool estimatesAvailable;
	// Result of the QOS API calls.
	bool result;

	// Overlapped operation used for TransmitPackets
	WSAOVERLAPPED sendOverlapped = default;
	// Overlapped operation used for QOSNotifyFlow to be notified
	// of network congestions.
	NativeOverlapped congestionOverlapped = default;
	// Overlapped operation used for QOSNotifyFlow to be notified when, 
	// after congestion, there is enough bandwidth for the target rate
	NativeOverlapped availableOverlapped = default;
	// true if the network is currently congested
	bool congestion;

	// Verify the number of command line arguments
	if (args.Length != 3)
	{
		help();
		Environment.Exit(1);
	}

	// Identify what destination IP address we're trying to talk to and
	// connect a UDP socket to it
	socketCreate(args[1], out socket, out addressFamily, out transmitPacketsFn);

	if (false == QOSCreateHandle(out qosHandle))
	{
		Console.Write("QOSCreateHandle failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	// Create a flow for our socket
	Win32Error.ThrowLastErrorIfFalse(QOSAddSocketToFlow(qosHandle, socket, IntPtr.Zero, QOS_TRAFFIC_TYPE.QOSTrafficTypeExcellentEffort, 0, ref flowID));

	// Read the data rate in bits/s passed on the command line
	if (!uint.TryParse(args[2], out targetBitRate))
	{
		help();
		Environment.Exit(1);
	}

	// Convert from bits to bytes
	targetBitRate /= 8;

	// Calculate how many packets we need; round up.
	targetPacketRate = (uint)(targetBitRate / dataBuffer.Size);
	if (targetBitRate % dataBuffer.Size != 0)
		targetPacketRate++;

	// Calculate the number of packets per bursts; round up
	if (targetPacketRate % BURSTS_PER_SECOND != 0)
	{
		targetPacketRate /= BURSTS_PER_SECOND;
		targetPacketRate++;
	}
	else
	{
		targetPacketRate /= BURSTS_PER_SECOND;
	}

	// Calculate the final bit rate, targetBitRate, in bps that the application
	// will send.
	targetBitRate = (uint)(BURSTS_PER_SECOND * targetPacketRate * dataBuffer.Size * 8U);

	//
	// Allocate an array of transmit elements big enough to send 
	// targetPacketRate packets every burst.
	var transmitEl = new TRANSMIT_PACKETS_ELEMENT[(int)targetPacketRate];

	//
	// Initialize each of these transmit element to point to the same zeroed out
	// data buffer
	for (int temp = 0; temp < targetPacketRate; temp++)
	{
		transmitEl[temp] = new() { dwElFlags = TP_ELEMENT.TP_ELEMENT_MEMORY | TP_ELEMENT.TP_ELEMENT_EOP, pBuffer = dataBuffer, cLength = dataBuffer.Size };
	}

	// Print out what we'll be doing
	Console.Write(@"----------------------------------
----------------------------------
This instance of the QOS sample program is aiming to:
	- Send {0} bits of UDP traffic per second
	- In packets of {1} bytes
	- In bursts of {2} packets every {3} milliseconds

----------------------------------
----------------------------------", targetBitRate, dataBuffer.Size, targetPacketRate, 1000 / BURSTS_PER_SECOND);

	// Assume, by default, that estimates are not available
	estimatesAvailable = false;

	Console.Write("Querying fundamentals about the network path: ");
	do
	{
		// Query the flow fundamentals for the path to the destination. This will return estimates of the bottleneck bandwidth, available
		// bandwidth and average RTT.
		try
		{
			flowFund = QOSQueryFlow<QOS_FLOW_FUNDAMENTALS>(qosHandle, flowID, QOS_QUERY_FLOW.QOSQueryFlowFundamentals);
		}
		catch (Exception ex)
		{
			Win32Error lastError = Win32Error.FromException(ex);

			if (lastError == Win32Error.ERROR_NO_DATA)
			{
				// If the call failed, this could be because the QOS subsystem 
				// has not yet gathered enough data for estimates. If so, the
				// QOS2 api returns Win32Error.ERROR_NO_DATA.
				Console.Write(".");
				Thread.Sleep(1000);
			}
			else if (lastError == Win32Error.ERROR_NOT_SUPPORTED)
			{
				// If the call failed, this could be because the QOS subsystem 
				// cannot run network experiments on the network path to the
				// destination. If so, the API returns Win32Error.ERROR_NOT_SUPPORTED.
				Console.Write("NOT SUPPORTED\n" +
					"\t Network conditions to this destination could not\n" +
					"\t be detected as your network configuration is not\n" +
					"\t supported for network experiments\n");
				break;
			}
			else
			{
				Console.Write("QOSQueryFlow failed ({0})\n", lastError);
				Environment.Exit(1);
			}
		}

		if ((flowFund.BottleneckBandwidthSet == false) || (flowFund.AvailableBandwidthSet == false))
		{
			// If the call succeeded but bottleneck bandwidth or 
			// available bandwidth are not known then estimates are still
			// processing; query again in 1 second.
			Console.Write(".");
			Thread.Sleep(1000);
		}
		else
		{
			// Estimate where available.
			double bottleneck;
			double available;

			estimatesAvailable = true;

			// Convert the bottleneck bandwidth from bps to mbps
			bottleneck = (double)flowFund.BottleneckBandwidth;
			bottleneck /= 1000000.0;

			// Convert available bandwidth from bps to mbps
			available = (double)flowFund.AvailableBandwidth;
			available /= 1000000.0;

			Console.Write("DONE\n" +
			"\t - Bottleneck bandwidth is approximately %4.2f Mbps\n" +
			"\t - Available bandwidth is approximately %4.2f Mbps\n",
			bottleneck,
			available);

			break;
		}
	} while (true);

	if (estimatesAvailable == true)
	{
		ulong targetBitRateWithHeaders;

		Console.Write("\nNOTE: the accuracy of the QOS estimates can be impacted by\n" +
		"any of the following,\n\n" +
		"\t - Both the network interface at the client\n" +
		"\t and at the server must be using NDIS 6 drivers.\n" +
		"\t - If the server is not a Windows Vista host, verify that \n" +
		"\t it implements the LLD2 networking protocol. You can\n" +
		"\t find more information at http://www.microsoft.com.\n" +
		"\t - IPSec, VPNs and enterprise class networking equipment\n" +
		"\t may interfere with network experiments.\n\n");

		// Calculate what our target bit rate, with protocol headers for 
		// IP(v4/v6) and UDP will be.
		targetBitRateWithHeaders = QOS_ADD_OVERHEAD(addressFamily, IPPROTO.IPPROTO_UDP, dataBuffer.Size, targetBitRate);

		if (flowFund.AvailableBandwidth < targetBitRateWithHeaders)
		{
			// If the estimate of available bandwidth is not sufficient for the
			// target bit rate (with headers), drop to a lesser throughput
			ulong availableBandwidth;

			// The estimate returned does not account for headers
			// Remove the estimated overhead for our address family and UDP.
			availableBandwidth = QOS_SUBTRACT_OVERHEAD(addressFamily, IPPROTO.IPPROTO_UDP, dataBuffer.Size, flowFund.AvailableBandwidth);

			// Calculate the rate of packets we can realistically send
			targetPacketRate = (uint)(availableBandwidth / 8);
			targetPacketRate /= dataBuffer.Size;
			targetPacketRate /= BURSTS_PER_SECOND;

			// Calculate the real bit rate we'll be using
			targetBitRate = BURSTS_PER_SECOND * targetPacketRate * dataBuffer.Size * 8;

			Console.Write("Not enough available bandwidth for the requested bitrate.\n" +
			"Downgrading to lesser bitrate - {0}.\n", targetBitRate);
		}
	}

	// Our starting rate is this target bit rate
	currentBitRate = targetBitRate;
	currentPacketRate = targetPacketRate;

	// Ask the QOS subsystem to shape our traffic to this bit rate. Note that
	// the application needs to account for the size of the IP(v4/v6) 
	// and UDP headers.

	// Calculate the real bandwidth we will need to be shaped to.
	flowRate.Bandwidth = QOS_ADD_OVERHEAD(addressFamily, IPPROTO.IPPROTO_UDP, dataBuffer.Size, currentBitRate);

	// Set shaping characteristics on our QOS flow to smooth out our bursty
	// traffic.
	flowRate.ShapingBehavior = QOS_SHAPING.QOSShapeAndMark;

	// The reason field is not applicable for the initial call.
	flowRate.Reason = QOS_FLOWRATE_REASON.QOSFlowRateNotApplicable;
	result = QOSSetFlow(qosHandle, flowID, QOS_SET_FLOW.QOSSetOutgoingRate, flowRate);

	if (result == false)
	{
		Console.Write("QOSSetFlow failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	// Allocate a waitable timer. We will use this timer to periodically
	// awaken and output statistics.
	using var timerEvent = CreateWaitableTimer(default, false, default);
	if (timerEvent is null || timerEvent.IsInvalid)
	{
		Console.Write("CreateWaitableTimer failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	//
	// Set the sampling time to 1 second
	long timerAwakenTime = -10000000; // 1 second
	
	if (false == SetWaitableTimer(timerEvent, timerAwakenTime, 1000))
	{
		Console.Write("SetWaitableTimer failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	// Prepare the support overlapped structures to detect congestion,
	// notifications of bandwidth change and the completion of our send 
	// routines.
	congestionOverlapped.EventHandle = CreateEvent(default, false, false, default).ReleaseOwnership();

	if (congestionOverlapped.EventHandle == IntPtr.Zero)
	{
		Console.Write("CreateEvent failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	availableOverlapped.EventHandle = CreateEvent(default, false, false, default).ReleaseOwnership();

	if (availableOverlapped.EventHandle == IntPtr.Zero)
	{
		Console.Write("CreateEvent failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	sendOverlapped.hEvent = CreateEvent(default, false, false, default).ReleaseOwnership();

	if (sendOverlapped.hEvent == IntPtr.Zero)
	{
		Console.Write("CreateEvent failed ({0})\n", Win32Error.GetLastError());
		Environment.Exit(1);
	}

	if (estimatesAvailable == true)
	{
		// If estimates are available, we request a notification 
		// for congestion. This notification will complete whenever network
		// congestion is detected.
		result = QOSNotifyFlow(qosHandle, flowID, QOS_NOTIFY_FLOW.QOSNotifyCongested, ref congestionOverlapped);

		if (result == false)
		{
			var lastError = Win32Error.GetLastError();
			if (lastError != Win32Error.ERROR_IO_PENDING)
			{
				Console.Write("QOSNotifyFlow failed ({0})\n", lastError);
				Environment.Exit(1);
			}
		}
	}

	Console.Write("--------------------------------------------------------------------\n" +
		" # packets | # of bits | Bottleneck | Available | Congestion \n" +
		"--------------------------------------------------------------------\n");

	// Initialize our counters to 0
	achievedBitRate = achievedPacketRate = 0;
	congestion = false;

	do
	{
		bool sendNextGroup;

		// Send the first burst of packets
		unsafe
		{
			result = transmitPacketsFn(socket, transmitEl, currentPacketRate, 0xFFFFFFFF, (IntPtr)(void*)&sendOverlapped, TF.TF_USE_KERNEL_APC);
		}

		if (result == false)
		{
			WSRESULT lastError = WSRESULT.GetLastError();
			if ((Win32Error)lastError != Win32Error.ERROR_IO_PENDING)
			{
				Console.Write("TransmitPackets failed ({0})\n", lastError);
				Environment.Exit(1);
			}
		}

		// Increase the counter of sent data
		achievedPacketRate += currentPacketRate;
		achievedBitRate += currentPacketRate * dataBuffer.Size * 8;
		sendNextGroup = false;

		do
		{
			HEVENT[] waitEvents = new HEVENT[3];

			// Wait for any of the 3 events to complete

			// The 1 second periodic timer
			waitEvents[0] = timerEvent.DangerousGetHandle();

			if (congestion)
				// Notification of available bandwidth
				waitEvents[1] = availableOverlapped.EventHandle;
			else
				// Notification of congestion
				waitEvents[1] = congestionOverlapped.EventHandle;

			// Notification that the send operation has completed
			waitEvents[2] = (IntPtr)sendOverlapped.hEvent;

			var waitResult = WaitForMultipleObjects(waitEvents, false, INFINITE);

			switch (waitResult)
			{
				case WAIT_STATUS.WAIT_OBJECT_0:
					{
						// The event for the periodic timer is set

						Console.Write("{0,10} | ", achievedPacketRate);
						Console.Write("{0,10} | ", achievedBitRate);

						if (estimatesAvailable)
						{
							// If estimates are available for the network path
							// query for estimates and output to the console along
							// with our counters.

							// Ascertained through QOSQueryFlow
							bool estimateIndicatesCongestion = false;

							flowFund = QOSQueryFlow<QOS_FLOW_FUNDAMENTALS>(qosHandle, flowID, QOS_QUERY_FLOW.QOSQueryFlowFundamentals);

							if (flowFund.BottleneckBandwidthSet)
								Console.Write("{0,10} | ", flowFund.BottleneckBandwidth);
							else
								Console.Write(" NO DATA | ");

							if (flowFund.AvailableBandwidthSet)
							{
								if (flowFund.AvailableBandwidth == 0)
									estimateIndicatesCongestion = true;

								Console.Write("{0,10} | ", flowFund.AvailableBandwidth);
							}
							else
								Console.Write(" NO DATA | ");

							if (estimateIndicatesCongestion)
								Console.Write(" CONGESTION\n");
							else
								Console.Write("\n");
						}
						else
						{
							// Bandwidth estimates are not available
							Console.Write(" N/A | N/A |\n");
						}

						//
						// Reset the counters
						achievedPacketRate = achievedBitRate;
						break;
					}
				case WAIT_STATUS.WAIT_OBJECT_0 + 1:
					{
						// This is either a notification for congestion or 
						// for bandwidth available 

						if (congestion == false)
						{
							ulong targetBandwidthWithOverhead;
							uint bufferSize;
							//
							// Congestion
							//
							Console.Write("--------------------------------------------------------------------\n" +
								"CONGESTION\n" +
								"--------------------------------------------------------------------\n");

							//
							// Reduce the current rate to one-tenth of the initial rate
							// Insure you're always sending at least 1 packet per
							// burst.
							if (currentPacketRate < 10)
								currentPacketRate = 1;
							else
								currentPacketRate /= 10;

							// Calculate the new bit rate we'll be using
							currentBitRate = BURSTS_PER_SECOND * currentPacketRate * dataBuffer.Size * 8;

							// Update the shaping characteristics on our QOS flow 
							// to smooth out our bursty traffic.
							flowRate.Bandwidth = QOS_ADD_OVERHEAD(addressFamily, IPPROTO.IPPROTO_UDP, dataBuffer.Size, currentBitRate);
							flowRate.ShapingBehavior = QOS_SHAPING.QOSShapeAndMark;
							flowRate.Reason = QOS_FLOWRATE_REASON.QOSFlowRateCongestion;

							result = QOSSetFlow(qosHandle, flowID, QOS_SET_FLOW.QOSSetOutgoingRate, flowRate);

							if (result == false)
							{
								Console.Write("QOSSetFlow failed ({0})\n", Win32Error.GetLastError());
								Environment.Exit(1);
							}

							// Request a notification for when there is enough bandwidth
							// to return to our previous targeted bit rate.
							// This will complete only when the network is no longer
							// congested and bandwidth is available.
							targetBandwidthWithOverhead = QOS_ADD_OVERHEAD(addressFamily, IPPROTO.IPPROTO_UDP, dataBuffer.Size, targetBitRate);

							bufferSize = (uint)Marshal.SizeOf(typeof(ulong));

							result = QOSNotifyFlow(qosHandle, flowID, QOS_NOTIFY_FLOW.QOSNotifyAvailable, targetBandwidthWithOverhead, ref availableOverlapped);
							if (result == false)
							{
								var lastError = Win32Error.GetLastError();
								if (lastError != Win32Error.ERROR_IO_PENDING)
								{
									Console.Write("QOSNotifyFlow failed ({0})\n", lastError);
									Environment.Exit(1);
								}
							}

							congestion = true;
						}
						else
						{
							//
							// End of congestion
							//
							Console.Write("--------------------------------------------------------------------\n" +
							"END OF CONGESTION\n" +
							"--------------------------------------------------------------------\n");

							//
							// Reset the current packet rate to the initial target rate
							currentPacketRate = targetPacketRate;

							// Reset the current bit rate to the initial target rate
							currentBitRate = targetBitRate;


							// Update the shaping characteristics on our QOS flow 
							// to smooth out our bursty traffic.
							flowRate.Bandwidth = QOS_ADD_OVERHEAD(addressFamily, IPPROTO.IPPROTO_UDP, dataBuffer.Size, targetBitRate);
							flowRate.ShapingBehavior = QOS_SHAPING.QOSShapeAndMark;
							flowRate.Reason = QOS_FLOWRATE_REASON.QOSFlowRateCongestion;
							result = QOSSetFlow(qosHandle, flowID, QOS_SET_FLOW.QOSSetOutgoingRate, flowRate);
							if (result == false)
							{
								Console.Write("QOSSetFlow failed ({0})\n", Win32Error.GetLastError());
								Environment.Exit(1);
							}

							// Request a notification for the next network congestion
							result = QOSNotifyFlow(qosHandle, flowID, QOS_NOTIFY_FLOW.QOSNotifyCongested, ref congestionOverlapped);

							if (result == false)
							{
								var lastError = Win32Error.GetLastError();
								if (lastError != Win32Error.ERROR_IO_PENDING)
								{
									Console.Write("QOSNotifyFlow failed ({0})\n", lastError);
									Environment.Exit(1);
								}
							}

							congestion = false;
						}
						break;
					}
				case WAIT_STATUS.WAIT_OBJECT_0 + 2:
					{
						// The transmit packet has completed its send, 
						// If it did so successfully, it's time to send the next 
						// burst of packets.
						var overlappedResult = WSAGetOverlappedResult(socket, sendOverlapped, out var ignoredNumOfBytesSent, false, out var ignoredFlags);

						if (overlappedResult == false)
						{
							Console.Write("TransmitPackets failed ({0})\n", WSAGetLastError());
							Environment.Exit(1);
						}

						// Time to send out the next bunch of packets
						sendNextGroup = true;
					}
					break;
				default:
					// The wait call failed.
					Console.Write("WaitForMultipleObjects failed ({0})\n", Win32Error.GetLastError());
					Environment.Exit(1);
					break;
			}

		} while (sendNextGroup == false);
	} while (true);
}

//******************************************************************************
// Routine: 
// server
//
// Description:
// This routine creates a socket through which it will receive
// any datagram sent by the client. It counts the number of packet 
// and the number of bytes received. Periodically, it outputs this 
// information to the console.
//
//******************************************************************************
void server()
{
	long timerAwakenTime;

	// The socket used to receive data
	SOCKET socket;

	// IPv6 wildcard address and port number 40007 to listen on at the server
	SOCKADDR_IN6 IPv6ListeningAddress = SOCKADDR_IN6.IN6ADDR_ANY;
	IPv6ListeningAddress.sin6_port = PORT;

	// Overlapped structure used to post asynchronous receive calls
	WSAOVERLAPPED recvOverlapped = default;

	// Counters of the number of bytes and packets received over 
	// a period of time.
	uint numPackets, numBytes;

	// Initialize Winsock
	using var wsaData = SafeWSA.Initialize();

	// Create an IPv6 datagram socket
	socket = WSASocket(ADDRESS_FAMILY.AF_INET6, SOCK.SOCK_DGRAM, 0, default, 0, WSA_FLAG.WSA_FLAG_OVERLAPPED);

	if (socket == SOCKET.INVALID_SOCKET)
	{
		Console.Write("WSASocket failed ({0})\n", WSAGetLastError());
		Environment.Exit(1);
	}

	// Set IPV6_V6ONLY to false before the bind operation
	// This will permit us to receive both IPv6 and IPv4 traffic on the socket.
	BOOL optionValue = false;
	var result = setsockopt(socket, IPPROTO.IPPROTO_IPV6, (int)IPV6.IPV6_V6ONLY, optionValue);

	if (SOCKET_ERROR == result)
	{
		Console.Write("setsockopt failed ({0})\n", WSAGetLastError());
		Environment.Exit(1);
	}

	// Bind the socket
	using var listeningSockAddr = new SOCKADDR(IPv6ListeningAddress);
	result = bind(socket, listeningSockAddr, listeningSockAddr.Size);

	if (result.Failed)
	{
		Console.Write("bind failed ({0})\n", WSAGetLastError());
		Environment.Exit(1);
	}

	// Create an event to be used for the overlapped of our
	// receive operations. The event is initialized to false and set
	// to auto-reset.
	recvOverlapped.hEvent = CreateEvent(default, false, false, default).ReleaseOwnership();

	if (recvOverlapped.hEvent.IsNull)
	{
		Console.Write("CreateEvent failed ({0})\n", WSAGetLastError());
		Environment.Exit(1);
	}

	// Create a timer event on which we will be able to wait.
	// We wish to be awaken every second to print out the count of packets
	// and number of bytes received.
	var timerEvent = CreateWaitableTimer(default, false, default);
	if (timerEvent.IsNull)
	{
		Console.Write("CreateWaitableTimer failed ({0})\n", WSAGetLastError());
		Environment.Exit(1);
	}

	// Awaken first in 1 second
	timerAwakenTime = -10000000; // 1 second

	if (false == SetWaitableTimer(timerEvent, timerAwakenTime, 1000))
	{
		Console.Write("SetWaitableTimer failed ({0})\n", WSAGetLastError());
		Environment.Exit(1);
	}

	// Initialize the counters to 0
	numPackets = numBytes = 0;

	Console.Write("-------------------------\n" +
	" # packets | # of bits |\n" +
	"-------------------------\n");
	do
	{
		// Array of events for WaitForMultipleObjects
		HEVENT[] waitEvents = new HEVENT[2];

		// Used for WSARecv
		WSABUF[] buf = new WSABUF[] { new() { len = dataBuffer.Size, buf = dataBuffer } };

		// No flags.
		MsgFlags dwFlags = 0;

		// Post a receive operation
		// We only have one receive outstanding at a time. 
		result = WSARecv(socket, buf, 1, default, ref dwFlags, ref recvOverlapped, default);

		if (result.Failed)
		{
			// The receive call failed. This could be because the
			// call will be completed asynchronously (WSA_IO_PENDING) or
			// it could be a legitimate error
			var errorCode = WSAGetLastError();
			if (errorCode != WSRESULT.WSA_IO_PENDING)
			{
				Console.Write("WSARecv failed ({0})\n", errorCode);
				Environment.Exit(1);
			}

			// If the error was WSA_IO_PENDING the call will be completed
			// asynchronously.
		}

		// Prepare our array of events to wait on. We will wait on:
		// 1) The event from the receive operation
		// 2) The event for the timer[] waitEvents = new timer[0] = recvOverlapped.hEvent;
		waitEvents[1] = timerEvent.DangerousGetHandle();

		// We wait for either event to complete
		var waitStatus = WaitForMultipleObjects(waitEvents, false, INFINITE);

		switch (waitStatus)
		{
			case WAIT_STATUS.WAIT_OBJECT_0:
				{
					// The receive operation completed.
					// Determine the true result of the receive call.
					bool overlappedResult = WSAGetOverlappedResult(socket, recvOverlapped, out var numberOfBytesReceived, false, out _);

					if (overlappedResult == false)
					{
						// The receive call failed.
						Console.Write("WSARecv failed ({0})\n", WSAGetLastError());
						Environment.Exit(1);
					}

					// The receive call succeeded
					// Increase our counters and post a new receive.
					numPackets++;
					numBytes += numberOfBytesReceived;
					break;
				}
			case WAIT_STATUS.WAIT_OBJECT_0 + 1:
				{
					// The timer event fired: our 1 second period has gone by.
					// Print the average to the console
					Console.Write("{0,10} | {1,10} |\n", numPackets, numBytes * 8);

					// Reset the counters
					numPackets = numBytes;
					break;
				}
			default:
				// The wait call failed.
				Console.Write("WaitForMultipleObjects failed ({0})\n", Win32Error.GetLastError());
				Environment.Exit(1);
				break;
		}

		// We continue this loop until the process is forceably stopped
		// through Ctrl-C.
	} while (true);
}