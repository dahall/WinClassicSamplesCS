using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.WinHTTP;

const ushort Port = INTERNET_DEFAULT_HTTP_PORT;
const string pcwszServerName = "localhost";
const string pcwszPath = "/WinHttpWebSocketSample/EchoWebSocket.ashx";

//
// Create session, connection and request handles.
//

using var hSessionHandle = WinHttpOpen("WebSocket sample", WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY);
if (hSessionHandle.IsInvalid)
	Win32Error.ThrowLastError();

using var hConnectionHandle = WinHttpConnect(hSessionHandle, pcwszServerName, Port, 0);
if (hConnectionHandle.IsInvalid)
	Win32Error.ThrowLastError();

using var hRequestHandle = WinHttpOpenRequest(hConnectionHandle, "GET", pcwszPath);
if (hRequestHandle.IsInvalid)
	Win32Error.ThrowLastError();

//
// Request protocol upgrade from http to websocket.
//
Win32Error.ThrowLastErrorIfFalse(WinHttpSetOption(hRequestHandle, WINHTTP_OPTION.WINHTTP_OPTION_UPGRADE_TO_WEB_SOCKET, default, 0));

//
// Perform websocket handshake by sending a request and receiving server's response.
// Application may specify additional headers if needed.
//

Win32Error.ThrowLastErrorIfFalse(WinHttpSendRequest(hRequestHandle, WINHTTP_NO_ADDITIONAL_HEADERS, 0));

Win32Error.ThrowLastErrorIfFalse(WinHttpReceiveResponse(hRequestHandle));

//
// Application should check what is the HTTP status code returned by the server and behave accordingly.
// WinHttpWebSocketCompleteUpgrade will fail if the HTTP status code is different than 101.
//

using var hWebSocketHandle = WinHttpWebSocketCompleteUpgrade(hRequestHandle);
if (hWebSocketHandle.IsInvalid)
	Win32Error.ThrowLastError();

//
// The request handle is not needed anymore. From now on we will use the websocket handle.
//

hRequestHandle.Dispose();

Console.Write("Succesfully upgraded to websocket protocol\n");

//
// Send and receive data on the websocket protocol.
//

using SafeLPWSTR pcwszMessage = "Hello world";
WinHttpWebSocketSend(hWebSocketHandle, WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE, pcwszMessage, pcwszMessage.Size).ThrowIfFailed();

Console.Write("Sent message to the server: '{0}'\n", pcwszMessage);

using SafeCoTaskMemHandle rgbBuffer = new(1024);
IntPtr pbCurrentBufferPointer = rgbBuffer;
uint dwBufferLength = rgbBuffer.Size;
uint dwBytesTransferred;
WINHTTP_WEB_SOCKET_BUFFER_TYPE eBufferType;

do
{
	if (dwBufferLength == 0)
	{
		throw ((Win32Error)Win32Error.ERROR_NOT_ENOUGH_MEMORY).GetException();
	}

	WinHttpWebSocketReceive(hWebSocketHandle, pbCurrentBufferPointer, dwBufferLength, out dwBytesTransferred, out eBufferType).ThrowIfFailed();

	//
	// If we receive just part of the message restart the receive operation.
	//

	pbCurrentBufferPointer = pbCurrentBufferPointer.Offset(dwBytesTransferred);
	dwBufferLength -= dwBytesTransferred;
}
while (eBufferType == WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE);

//
// We expected server just to echo single binary message.
//

if (eBufferType != WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE)
{
	Console.Write("Unexpected buffer type\n");
	throw ((Win32Error)Win32Error.ERROR_INVALID_PARAMETER).GetException();
}

Console.Write("Received message from the server: '{0}'\n", rgbBuffer.ToString(-1, CharSet.Unicode));

//
// Gracefully close the connection.
//

WinHttpWebSocketClose(hWebSocketHandle, WINHTTP_WEB_SOCKET_CLOSE_STATUS.WINHTTP_WEB_SOCKET_SUCCESS_CLOSE_STATUS).ThrowIfFailed();

//
// Check close status returned by the server.
//

using var rgbCloseReasonBuffer = new SafeLPSTR(123);
WinHttpWebSocketQueryCloseStatus(hWebSocketHandle, out var usStatus, rgbCloseReasonBuffer, rgbCloseReasonBuffer.Size, out var dwCloseReasonLength).ThrowIfFailed();

Console.Write("The server closed the connection with status code: '{0}' and reason: '{1}'\n", (int)usStatus, rgbCloseReasonBuffer);