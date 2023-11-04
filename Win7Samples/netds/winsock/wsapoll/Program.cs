using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ws2_32;

namespace recvmsg;

static class Program
{
	//constants
	const int WS_VER = 0x0202;
	const int DEFAULT_PORT = 12345;
	const int DEFAULT_WAIT = 30000;
	static readonly SafeCoTaskMemString TST_MSG = new("0123456789abcdefghijklmnopqrstuvwxyz\0");
	static SafeEventHandle hCloseSignal = null;
	static SafeHTHREAD hThread = null;

	public static int Main()
	{
		try
		{
			using var wsa = SafeWSA.Initialize(WS_VER);

			hCloseSignal = Win32Error.ThrowLastErrorIfInvalid(CreateEvent(default, true, false, default));
			hThread = Win32Error.ThrowLastErrorIfInvalid(CreateThread(lpStartAddress: ConnectThread, lpThreadId: out _));

			SOCKADDR_IN6 addr = (SOCKADDR_IN6)IN6_ADDR.Loopback;
			addr.sin6_port = htons(DEFAULT_PORT);

			using var lsock = WSRESULT.ThrowLastErrorIfInvalid(socket(ADDRESS_FAMILY.AF_INET6, SOCK.SOCK_STREAM));

			uint uNonBlockingMode = 1;
			using PinnedObject puNonBlockingMode = new(uNonBlockingMode);
			ioctlsocket(lsock, WinSockIOControlCode.FIONBIO, puNonBlockingMode).ThrowIfFailed();

			bind(lsock, addr, Marshal.SizeOf(typeof(SOCKADDR_IN6))).ThrowIfFailed();

			listen(lsock, 1).ThrowIfFailed();

			//Call WSAPoll for readability of listener (accepted)
			WSAPOLLFD[] fdarray = { new() { fd = lsock, events = PollFlags.POLLRDNORM } };

			SafeSOCKET asock = default;
			var ret = WSRESULT.ThrowLastErrorIf(WSAPoll(fdarray, 1, DEFAULT_WAIT), e => e == SOCKET_ERROR);
			if (ret > 0 && fdarray[0].revents.IsFlagSet(PollFlags.POLLRDNORM))
			{
				Console.Write("Main: Connection established.\n");

				asock = WSRESULT.ThrowLastErrorIf(accept(lsock), h => h.IsNull);

				byte[] buf = new byte[MAX_PATH];
				ret = WSRESULT.ThrowLastErrorIf(recv(asock, buf, buf.Length, 0), i => i == SOCKET_ERROR);

				Console.Write("Main: recvd {0} bytes\n", ret);
			}

			//Call WSAPoll for writeability of accepted
			fdarray[0].fd = asock;
			fdarray[0].events = PollFlags.POLLWRNORM;

			ret = WSRESULT.ThrowLastErrorIf(WSAPoll(fdarray, 1, DEFAULT_WAIT), e => e == SOCKET_ERROR);
			if (ret > 0 && fdarray[0].revents.IsFlagSet(PollFlags.POLLWRNORM))
			{
				ret = WSRESULT.ThrowLastErrorIf(send(asock, TST_MSG, TST_MSG.Size, 0), i => i == SOCKET_ERROR);
				Console.Write("Main: sent {0} bytes\n", ret);
			}
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("ERROR: " + ex.Message);
		}
		finally
		{
			hCloseSignal?.Set();
			if (hThread is not null)
				WaitForSingleObject(hThread, DEFAULT_WAIT);
		}
		return 0;
	}

	private static uint ConnectThread(IntPtr lpParam)
	{
		try
		{
			//using var wsa = SafeWSA.Initialize(WS_VER);
			using var csock = WSRESULT.ThrowLastErrorIfInvalid(socket(ADDRESS_FAMILY.AF_INET6, SOCK.SOCK_STREAM));

			uint uNonBlockingMode = 1;
			using PinnedObject puNonBlockingMode = new(uNonBlockingMode);
			ioctlsocket(csock, WinSockIOControlCode.FIONBIO, puNonBlockingMode).ThrowIfFailed();

			SOCKADDR_IN6 addrLoopback = (SOCKADDR_IN6)IN6_ADDR.Loopback;
			addrLoopback.sin6_port = htons(DEFAULT_PORT);

			connect(csock, (SOCKADDR)addrLoopback, Marshal.SizeOf(typeof(SOCKADDR_IN6))).ThrowIfFailed();

			// Call WSAPoll for writeability on connecting socket
			WSAPOLLFD[] fdarray = { new() { fd = csock, events = PollFlags.POLLWRNORM } };

			int ret = WSRESULT.ThrowLastErrorIf(WSAPoll(fdarray, (uint)fdarray.Length, DEFAULT_WAIT), e => e == SOCKET_ERROR);
			if (ret > 0 && fdarray[0].revents.IsFlagSet(PollFlags.POLLWRNORM))
			{
				Console.Write("ConnectThread: Established connection\n");

				//Send data
				ret = WSRESULT.ThrowLastErrorIf(send(csock, TST_MSG, TST_MSG.Size, 0), i => i == SOCKET_ERROR);
				Console.Write("ConnectThread: sent {0} bytes\n", ret);
			}

			// Call WSAPoll for readability on connected socket
			fdarray[0].events = PollFlags.POLLRDNORM;

			ret = WSRESULT.ThrowLastErrorIf(WSAPoll(fdarray, (uint)fdarray.Length, DEFAULT_WAIT), e => e == SOCKET_ERROR);
			if (ret > 0 && fdarray[0].revents.IsFlagSet(PollFlags.POLLRDNORM))
			{
				byte[] buf = new byte[MAX_PATH];
				ret = WSRESULT.ThrowLastErrorIf(recv(csock, buf, buf.Length, 0), i => i == SOCKET_ERROR);
				Console.Write("ConnectThread: recvd {0} bytes\n", ret);
			}

			WaitForSingleObject(hCloseSignal, DEFAULT_WAIT);
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine("THREAD ERROR: " + ex.Message);
			return 1;
		}
		return 0;
	}
}