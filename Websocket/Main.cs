global using System.Runtime.InteropServices;
global using Vanara.Extensions;
global using Vanara.InteropServices;
global using Vanara.PInvoke;
global using static Vanara.PInvoke.WebSocket;

namespace Websocket;

internal class Program
{
	public static int Main()
	{
		HRESULT hr = Initialize(out SafeWEB_SOCKET_HANDLE? clientHandle, out SafeWEB_SOCKET_HANDLE? serverHandle);
		if (hr.Failed)
		{
			goto quit;
		}

		hr = PerformHandshake(clientHandle!, serverHandle!);
		if (hr.Failed)
		{
			goto quit;
		}

		Transport transport = new();
		hr = PerformDataExchange(clientHandle!, serverHandle!, transport);
		if (hr.Failed)
		{
			goto quit;
		}

	quit:

		clientHandle?.Dispose();
		serverHandle?.Dispose();

		if (hr.Failed)
		{
			Console.Write("Websocket failed with error {0}\n", hr);
			return 0;
		}

		return 1;
	}

	private static void DumpData(IntPtr pbBuffer, uint ulBufferLength) => DumpData(pbBuffer == IntPtr.Zero ? null : pbBuffer.ToArray<byte>((int)ulBufferLength));

	private static void DumpData(byte[]? data)
	{
		if (data is null)
		{
			return;
		}

		for (int i = 0; i < data.Length; i++)
		{
			Console.Write("0x{0:X} ", data[i]);
		}

		Console.Write("\n");
	}

	private static void DumpHeaders(WEB_SOCKET_HTTP_HEADER[]? headers)
	{
		if (headers is null)
			return;
		foreach (WEB_SOCKET_HTTP_HEADER hdr in headers)
		{
			Console.Write("{0}: {1}\n", hdr.pcName, hdr.pcValue);
		}
	}

	private static HRESULT Initialize(out SafeWEB_SOCKET_HANDLE? clientHandle, out SafeWEB_SOCKET_HANDLE? serverHandle)
	{
		HRESULT hr;

		// Create a client side websocket handle.
		hr = WebSocketCreateClientHandle(default, 0, out clientHandle);
		if (hr.Succeeded)
		{
			// Create a server side websocket handle.
			hr = WebSocketCreateServerHandle(null, 0, out serverHandle);
			if (hr.Succeeded)
			{
				return hr;
			}
		}

		// Cleanup on failure
		clientHandle?.Dispose();
		clientHandle = serverHandle = null;
		return hr;
	}

	private static HRESULT PerformDataExchange([In] WEB_SOCKET_HANDLE clientHandle, [In] WEB_SOCKET_HANDLE serverHandle, in Transport transport)
	{
		SafeCoTaskMemHandle dataToSend = new(StringHelper.GetBytes("Hello World", Encoding.UTF8, false));
		WEB_SOCKET_BUFFER buffer = new(dataToSend);

		Console.Write("\n-- Queueing a send with a buffer --\n");
		DumpData(buffer.Data.pbBuffer, buffer.Data.ulBufferLength);

		HRESULT hr = WebSocketSend(clientHandle, WEB_SOCKET_BUFFER_TYPE.WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE, new[] { buffer }, default);
		if (hr.Failed)
		{
			goto quit;
		}

		hr = RunLoop(clientHandle, transport);
		if (hr.Failed)
		{
			goto quit;
		}

		Console.Write("\n-- Queueing a receive --\n");

		hr = WebSocketReceive(serverHandle, default, default);
		if (hr.Failed)
		{
			goto quit;
		}

		hr = RunLoop(serverHandle, transport);
		if (hr.Failed)
		{
			goto quit;
		}

	quit:

		return hr;
	}

	private static HRESULT PerformHandshake([In] WEB_SOCKET_HANDLE clientHandle, [In] WEB_SOCKET_HANDLE serverHandle)
	{
		// Static "Host" header.
		WEB_SOCKET_HTTP_HEADER host = new("Host", "localhost");

		// Start a client side of the handshake - 'additionalHeaders' will hold an array of websocket specific headers. Production
		// applications must add these headers to the outgoing HTTP request.
		HRESULT hr = WebSocketBeginClientHandshake(clientHandle, null, 0, null, 0, null, 0, out WEB_SOCKET_HTTP_HEADER[]? clientAdditionalHeaders);
		if (hr.Failed)
		{
			return hr;
		}

		// Concatenate list of headers that the HTTP stack must send (the Host header) with a list returned by WebSocketBeginClientHandshake.
		Array.Resize(ref clientAdditionalHeaders, (clientAdditionalHeaders?.Length ?? 0) + 1);
		clientAdditionalHeaders[^1] = host;

		Console.Write("-- Client side headers that need to be send with a request --\n");
		DumpHeaders(clientAdditionalHeaders);

		// Start a server side of the handshake. Production applications must parse the incoming HTTP request and pass all headers to the
		// function. The function will return an array websocket specific headers that must be added to the outgoing HTTP response.
		hr = WebSocketBeginServerHandshake(serverHandle, null, null, 0, clientAdditionalHeaders, (uint)clientAdditionalHeaders.Length,
			out WEB_SOCKET_HTTP_HEADER[]? serverAdditionalHeaders);
		if (hr.Failed)
		{
			return hr;
		}

		Console.Write("\n-- Server side headers that need to be send with a response --\n");
		DumpHeaders(serverAdditionalHeaders);

		// Finish handshake. Once the client/server handshake is completed, memory allocated by ref the ref Begin functions is reclaimed and
		// must not be used by the application.
		hr = WebSocketEndClientHandshake(clientHandle, serverAdditionalHeaders!, (uint)serverAdditionalHeaders!.Length);
		if (hr.Failed)
		{
			return hr;
		}

		hr = WebSocketEndServerHandshake(serverHandle);
		if (hr.Failed)
		{
			Console.Write("4\n");
		}

		return hr;
	}

	private static HRESULT RunLoop([In] WEB_SOCKET_HANDLE handle, Transport transport)
	{
		HRESULT hr;
		WEB_SOCKET_BUFFER[] buffers = new WEB_SOCKET_BUFFER[2];
		uint bufferCount, bytesTransferred;
		WEB_SOCKET_ACTION action;

		do
		{
			// Initialize variables that change with every loop revolution.
			bufferCount = (uint)buffers.Length;
			bytesTransferred = 0;

			// Get an action to process.
			hr = WebSocketGetAction(handle, WEB_SOCKET_ACTION_QUEUE.WEB_SOCKET_ALL_ACTION_QUEUE, buffers, ref bufferCount,
				out action, out _, out _, out IntPtr actionContext);
			if (hr.Failed)
			{
				// If we cannot get an action, abort the handle but continue processing until all operations are completed.
				WebSocketAbortHandle(handle);
			}

			switch (action)
			{
				case WEB_SOCKET_ACTION.WEB_SOCKET_NO_ACTION:
					// No action to perform - just exit the loop.
					break;

				case WEB_SOCKET_ACTION.WEB_SOCKET_RECEIVE_FROM_NETWORK_ACTION:

					Console.Write("Receiving data from a network:\n");
					for (int i = 0; i < bufferCount; i++)
					{
						// Read data from a transport (in production application this may be a socket).
						hr = transport.ReadData(buffers[i].Data.ulBufferLength, out bytesTransferred, buffers[i].Data.pbBuffer);
						if (hr.Failed)
						{
							break;
						}

						DumpData(buffers[i].Data.pbBuffer, bytesTransferred);

						// Exit the loop if there were not enough data to fill this buffer.
						if (buffers[i].Data.ulBufferLength > bytesTransferred)
						{
							break;
						}
					}

					break;

				case WEB_SOCKET_ACTION.WEB_SOCKET_INDICATE_RECEIVE_COMPLETE_ACTION:

					Console.Write("Receive operation completed with a buffer:\n");

					if (bufferCount != 1)
					{
						hr = HRESULT.E_FAIL;
						goto quit;
					}

					DumpData(buffers[0].Data.pbBuffer, buffers[0].Data.ulBufferLength);

					break;

				case WEB_SOCKET_ACTION.WEB_SOCKET_SEND_TO_NETWORK_ACTION:

					Console.Write("Sending data to a network:\n");

					for (int i = 0; i < bufferCount; i++)
					{
						DumpData(buffers[i].Data.pbBuffer, buffers[i].Data.ulBufferLength);

						// Write data to a transport (in production application this may be a socket).
						hr = transport.WriteData(buffers[i].Data.pbBuffer, buffers[i].Data.ulBufferLength);
						if (hr.Failed)
						{
							break;
						}

						bytesTransferred += buffers[i].Data.ulBufferLength;
					}
					break;

				case WEB_SOCKET_ACTION.WEB_SOCKET_INDICATE_SEND_COMPLETE_ACTION:

					Console.Write("Send operation completed\n");
					break;

				default:

					// This should never happen.
					hr = HRESULT.E_FAIL;
					goto quit;
			}

			if (hr.Failed)
			{
				// If we failed at some point processing actions, abort the handle but continue processing until all operations are completed.
				WebSocketAbortHandle(handle);
			}

			// Complete the action. If application performs asynchronous operation, the action has to be completed after the async operation
			// has finished. The 'actionContext' then has to be preserved so the operation can complete properly.
			WebSocketCompleteAction(handle, actionContext, bytesTransferred);
		}
		while (action != WEB_SOCKET_ACTION.WEB_SOCKET_NO_ACTION);

	quit:
		return hr;
	}
}