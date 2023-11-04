using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ws2_32;

namespace recvmsg
{
	static class Program
	{
		//constants
		const int WS_VER = 0x0202;
		const int DEFAULT_PORT = 12345;
		const int DEFAULT_WAIT = 5000;
		const string MCAST_V6 = "ff1f::1";
		static readonly SafeCoTaskMemString TST_MSG = new("Hello\0");

		public static int Main()
		{
			WSAMSG wsamsg = default;
			WSAOVERLAPPED over = default;
			try
			{
				//Initialize Winsock
				using var wsa = SafeWSA.Initialize(WS_VER);

				// bind socket and register multicast
				//mcaddr.ss_family = ADDRESS_FAMILY.AF_INET6;

				InitMcastAddr(out var mcaddr);

				using var sock = socket(ADDRESS_FAMILY.AF_INET6, SOCK.SOCK_DGRAM, 0U);
				if (sock.IsInvalid)
				{
					ERR("socket");
					return -1;
				}

				if (!RouteLookup(mcaddr, out SOCKADDR_INET addr))
				{
					ERR("RouteLookup");
					return -1;
				}

				SET_PORT(ref addr, DEFAULT_PORT);

				if (!bind(sock, addr, addr.Size))
				{
					ERR("bind");
					return -1;
				}

				IPV6_MREQ mreq = new() { ipv6mr_multiaddr = mcaddr.Ipv6.sin6_addr };

				if (!setsockopt(sock, IPPROTO.IPPROTO_IPV6, 20 /*IPV6_ADD_MEMBERSHIP*/, mreq))
				{
					ERR("setsockopt IPV6_ADD_MEMBRESHIP");
					return -1;
				}

				// PktInfo

				if (!SetIpv6PktInfoOption(sock))
				{
					ERR("SetIpv6PktInfoOption");
					return -1;
				}

				if (!AllocAndInitIpv6PktInfo(ref wsamsg))
				{
					ERR("AllocAndInitIpv6PktInfo");
					return -1;
				}

				// data buffer
				using var wsabuf = new SafeCoTaskMemStruct<WSABUF>();
				var tmpbuf = new WSABUF() { buf = wsabuf.DangerousGetHandle().Offset(wsabuf.Size), len = 100 };
				wsabuf.Size += tmpbuf.len;
				wsabuf.Value = tmpbuf;

				wsamsg.lpBuffers = wsabuf;
				wsamsg.dwBufferCount = 1;

				// packet source address
				using var remoteaddr = new SafeCoTaskMemStruct<SOCKET_ADDRESS>();
				wsamsg.name = remoteaddr;
				wsamsg.namelen = remoteaddr.Size;

				//Post overlapped WSARecvMsg
				InitOverlap(ref over);

				var WSARecvMsg = GetWSARecvMsgFunctionPointer();
				if (default == WSARecvMsg)
				{
					ERR("GetWSARecvMsgFunctionPointer");
					return -1;
				}

				if (SOCKET_ERROR == WSARecvMsg(sock, ref wsamsg, out var dwBytes, ref over, default))
				{
					if (WSRESULT.WSA_IO_PENDING != WSAGetLastError())
					{
						ERR("WSARecvMsg");
						return -1;
					}
				}

				//set send interface

				if (!SetSendInterface(sock, addr))
				{
					ERR("SetSendInterface");
					return -1;
				}

				//send msg to multicast
				SET_PORT(ref mcaddr, DEFAULT_PORT);

				//send a few packets
				for (int i=0; i<5; i++)
				{
					WSRESULT rc = 0;
					if (!(rc = sendto(sock, TST_MSG, TST_MSG.Length, 0, mcaddr, mcaddr.Size)))
					{
						ERR("sendto");
						return -1;
					}

					Console.Write("Sent {0} bytes\n", rc);
				}

				var dwRet = WaitForSingleObject(over.hEvent, DEFAULT_WAIT);

				if (dwRet != 0)
				{
					Console.Write("{0}\n", dwRet);
					return -1;
				}

				if (!WSAGetOverlappedResult(sock, over, out dwBytes, true, out var dwFlags))
				{
					ERR("WSAGetOverlappedResult");
					return -1;
				}

				Console.Write("WSARecvMsg completed with {0} bytes\n", dwBytes);

				// if multicast packet do further processing
				if ((MsgFlags.MSG_MCAST & wsamsg.dwFlags) != 0)
				{
					if (ProcessIpv6Msg(wsamsg))
					{
						//do something more interesting here
						Console.Write("Recvd multicast msg.\n");
					}

				}

			}
			finally
			{
				Marshal.FreeCoTaskMem(wsamsg.Control.buf);
				WSACloseEvent(over.hEvent);
			}

			return 0;
		}

		//Helper functions

		static void CLOSESOCKEVENT(WSAEVENT h) { if (WSAEVENT.NULL != h) WSACloseEvent(h); }

		static void ERR(string e, [CallerMemberName] string callerName = "", [CallerFilePath] string callerFile = "", [CallerLineNumber] int callerLine = 0) => Console.Error.WriteLine("{0}:{1} failed: {2} [{3}@{4}]", callerName, e, WSAGetLastError(), callerFile, callerLine);

		static void SET_PORT(ref SOCKADDR_INET pAddr, ushort port)
		{
			if (ADDRESS_FAMILY.AF_INET6 == pAddr.si_family)
			{
				pAddr.Ipv6.sin6_port = htons(port);
			}
			else
			{
				pAddr.Ipv4.sin_port = htons(port);
			}
		}

		static void InitOverlap(ref WSAOVERLAPPED pOver)
		{
			CLOSESOCKEVENT(pOver.hEvent);

			pOver = default;

			if ((pOver.hEvent = WSACreateEvent().ReleaseOwnership()).IsNull)
			{
				ERR("WSACreateEvent");
			}
		}

		static void InitMcastAddr(out SOCKADDR_INET pmcaddr)
		{
			pmcaddr = WSAStringToAddress(MCAST_V6, ADDRESS_FAMILY.AF_INET6);
			pmcaddr.Ipv6.sin6_port = htons(DEFAULT_PORT);
		}

		static bool SetIpv6PktInfoOption(SafeSOCKET sock)
		{
			uint dwEnableOption = 1;

			if (!setsockopt(sock, IPPROTO.IPPROTO_IPV6, 2 /*IPV6_PKTINFO*/, dwEnableOption))
			{
				ERR("setsockopt IPV6_PKTINFO");
				return false;
			}

			return true;
		}

		static bool AllocAndInitIpv6PktInfo(ref WSAMSG pWSAMsg)
		{
			pWSAMsg.Control.len = WSA_CMSG_SPACE(Marshal.SizeOf(typeof(IN6_PKTINFO)));
			pWSAMsg.Control.buf = Marshal.AllocCoTaskMem((int)pWSAMsg.Control.len); //caller frees heap allocated CtrlBuf
			return true;
		}

		static bool ProcessIpv6Msg(in WSAMSG pWSAMsg)
		{
			unsafe
			{
				WSACMSGHDR* pCtrlInfo = WSA_CMSG_FIRSTHDR(pWSAMsg);

				if ((int)IPPROTO.IPPROTO_IPV6 == pCtrlInfo->cmsg_level && 2 /*IPV6_PKTINFO*/ == pCtrlInfo->cmsg_type)
				{
					var pPktInfo = (IN6_PKTINFO*)WSA_CMSG_DATA(pCtrlInfo);

					//do something with pPktInfo

					return true;
				}
			}

			return false;
		}

		static bool RouteLookup(in SOCKADDR_INET destAddr, out SOCKADDR_INET localAddr)
		{
			localAddr = default;

			using var s = socket(destAddr.si_family, SOCK.SOCK_DGRAM, 0U);
			if (s.IsInvalid)
			{
				ERR("socket");
				return false;
			}

			if (!WSAIoctl(s, WinSockIOControlCode.SIO_ROUTING_INTERFACE_QUERY, destAddr, out localAddr))
			{
				ERR("WSAIoctl");
				return false;
			}

			WSAAddressToString(destAddr);
			WSAAddressToString(localAddr);

			return true;
		}

		static uint GetInterfaceIndexForAddress(in SOCKADDR_INET pAddr)
		{
			try
			{
				ADDRESS_FAMILY Family = pAddr.si_family switch
				{
					ADDRESS_FAMILY.AF_INET => ADDRESS_FAMILY.AF_INET,
					ADDRESS_FAMILY.AF_INET6 => ADDRESS_FAMILY.AF_INET6,
					_ => ADDRESS_FAMILY.AF_UNSPEC,
				};
				if (Family == ADDRESS_FAMILY.AF_UNSPEC)
				{
					WSASetLastError(WSRESULT.WSAEAFNOSUPPORT);
					return unchecked((uint)-1);
				}

				var pAdaptAddr = GetAdaptersAddresses(GetAdaptersAddressesFlags.GAA_FLAG_SKIP_ANYCAST|GetAdaptersAddressesFlags.GAA_FLAG_SKIP_MULTICAST|GetAdaptersAddressesFlags.GAA_FLAG_SKIP_DNS_SERVER, Family);

				//look at each IP_ADAPTER_ADDRESSES node 
				foreach (IP_ADAPTER_ADDRESSES pTmpAdaptAddr in pAdaptAddr)
				{
					foreach (var tmpUniAddr in pTmpAdaptAddr.UnicastAddresses)
					{
						SOCKADDR uniAddr = new(tmpUniAddr.Address.lpSockaddr);
						if (ADDRESS_FAMILY.AF_INET == uniAddr.sa_family)
						{
							if (pAddr.Ipv4.sin_addr == ((SOCKADDR_IN)uniAddr).sin_addr)
							{
								return pTmpAdaptAddr.IfIndex;
							}

						}
						else
						{
							if (pAddr.Ipv6.sin6_addr == ((SOCKADDR_IN6)uniAddr).sin6_addr)
							{
								return pTmpAdaptAddr.Ipv6IfIndex;
							}
						}
					}
				}

				MIB_IPADDRTABLE pMibTable = GetIpAddrTable(true);
				foreach (var row in pMibTable)
				{
					if (pAddr.Ipv4.sin_addr == row.dwAddr)
					{
						return row.dwIndex;
					}
				}
			}
			catch
			{
			}

			return unchecked((uint)-1);
		}

		static WSRESULT SetSendInterface(SOCKET s, in SOCKADDR_INET iface)
		{
			if (iface.si_family == ADDRESS_FAMILY.AF_INET)
			{
				// Setup the v4 option values
				var rc = setsockopt(s, IPPROTO.IPPROTO_IP, 9 /*IP_MULTICAST_IF*/, iface.Ipv4.sin_addr);
				if (!rc)
					ERR("setsockopt");
				return rc;
			}
			else if (iface.si_family == ADDRESS_FAMILY.AF_INET6)
			{
				// Setup the v6 option values
				var dwIPv6Index = GetInterfaceIndexForAddress(iface);
				if (SOCKET_ERROR == unchecked((int)dwIPv6Index))
					return SOCKET_ERROR;
				var rc = setsockopt(s, IPPROTO.IPPROTO_IPV6, 9 /*IPV6_MULTICAST_IF*/, dwIPv6Index);
				if (rc.Failed)
					ERR("setsockopt");
				return rc;
			}

			WSASetLastError(WSRESULT.WSAEAFNOSUPPORT);
			return SOCKET_ERROR;
		}

		static LPFN_WSARECVMSG GetWSARecvMsgFunctionPointer()
		{
			using var sock = socket(ADDRESS_FAMILY.AF_INET6, SOCK.SOCK_DGRAM, 0U);

			if (WSAIoctl(sock, WinSockIOControlCode.SIO_GET_EXTENSION_FUNCTION_POINTER, WSAID_WSARECVMSG, out HANDLE lpfnWSARecvMsg).Failed)
			{
				ERR("WSAIoctl SIO_GET_EXTENSION_FUNCTION_POINTER");
				return default;
			}

			return Marshal.GetDelegateForFunctionPointer<LPFN_WSARECVMSG>(lpfnWSARecvMsg.DangerousGetHandle());
		}
	}
}