using System;
using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;

using static Vanara.PInvoke.HttpApi;
using static Vanara.PInvoke.Kernel32;

namespace server;

internal class Program
{
	public static int Main(string[] args)
	{
		var UrlAdded = 0;
		HTTPAPI_VERSION HttpApiVersion = HTTPAPI_VERSION.HTTPAPI_VERSION_1;

		if (args.Length < 2)
		{
			Console.Write("{0}: <Url1> [Url2] ... \n", "server.exe");
			return -1;
		}

		// Initialize HTTP APIs.
		Win32Error retCode = HttpInitialize(HttpApiVersion, HTTP_INIT.HTTP_INITIALIZE_SERVER);

		if (retCode.Failed)
		{
			Console.Write("HttpInitialize failed with {0} \n", retCode);
			return (int)(uint)retCode;
		}

		// Create a Request Queue Handle
		retCode = HttpCreateHttpHandle(out SafeHREQQUEUEv1 hReqQueue); // Req Queue

		if (retCode.Failed)
		{
			Console.Write("HttpCreateHttpHandle failed with {0} \n", retCode);
			goto CleanUp;
		}

		// The command line arguments represent URIs that we want to listen on. We will call HttpAddUrl for each of these URIs.
		//
		// The URI is a fully qualified URI and MUST include the terminating '/'
		for (var i = 0; i < args.Length; i++)
		{
			Console.Write("we are listening for requests on the following url: {0}\n", args[i]);

			retCode = HttpAddUrl(hReqQueue, args[i]); // Req Queue, Fully qualified URL

			if (retCode.Failed)
			{
				Console.Write("HttpAddUrl failed with {0} \n", retCode);
				goto CleanUp;
			}
			else
			{
				// Keep track of the URLs that we've currently added.
				UrlAdded++;
			}
		}

		// Loop while receiving requests
		DoReceiveRequests(hReqQueue);

CleanUp:

		// Call HttpRemoveUrl for all the URLs that we added.
		for (var i = 1; i<=UrlAdded; i++)
		{
			HttpRemoveUrl(hReqQueue, args[i]); // Req Queue, Fully qualified URL);
		}

		// Close the Request Queue handle.
		if (!hReqQueue.IsClosed)
		{
			hReqQueue.Dispose();
		}

		// Call HttpTerminate.
		HttpTerminate(HTTP_INIT.HTTP_INITIALIZE_SERVER);

		return (int)(uint)retCode;
	}

	private static void ADD_KNOWN_HEADER(ref HTTP_RESPONSE_V1 Response, HTTP_HEADER_ID HeaderId, string RawValue)
	{
		Response.Headers.KnownHeaders[(int)HeaderId].pRawValue = RawValue;
		Response.Headers.KnownHeaders[(int)HeaderId].RawValueLength = (ushort)RawValue.Length;
	}

	/***************************************************************************++
	Routine Description:
	The routine to receive a request. This routine calls the corresponding
	routine to deal with the response.
	Arguments:
	hReqQueue - Handle to the request queue.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoReceiveRequests([In] HREQQUEUEv1 hReqQueue)
	{
		Win32Error result = 0;

		// Allocate a 2K buffer. Should be good for most requests, we'll grow this if required. We also need space for a HTTP_REQUEST structure.
		using var pRequestBuffer = new SafeCoTaskMemHandle(Marshal.SizeOf(typeof(HTTP_REQUEST_V1)) + 2048);

		// Wait for a new request -- This is indicated by a default request ID.
		while (HttpReceiveHttpRequest(hReqQueue, HTTP_NULL_ID, 0, out HTTP_REQUEST pRequest).Succeeded)
		{
			// Worked!
			switch (pRequest.Verb)
			{
				case HTTP_VERB.HttpVerbGET:
					Console.Write("Got a GET request for {0} \n", pRequest.CookedUrl.pFullUrl);

					result = SendHttpResponse(hReqQueue,
						pRequest,
						200,
						"OK",
						"Hey! You hit the server \r\n");
					break;

				case HTTP_VERB.HttpVerbPOST:

					Console.Write("Got a POST request for {0} \n", pRequest.CookedUrl.pFullUrl);

					result = SendHttpPostResponse(hReqQueue, pRequest);
					break;

				default:
					Console.Write("Got a unknown request for {0} \n", pRequest.CookedUrl.pFullUrl);

					result = SendHttpResponse(hReqQueue,
						pRequest,
						503,
						"Not Implemented",
						default);
					break;
			}
		}

		return result;
	}

	private static HTTP_RESPONSE_V1 INITIALIZE_HTTP_RESPONSE(ushort status, string reason) =>
		new() { StatusCode = status, pReason = reason, ReasonLength = (ushort)reason.Length };

	/***************************************************************************++
	Routine Description:
	The routine sends a HTTP response after reading the entity body.
	Arguments:
	hReqQueue - Handle to the request queue.
	pRequest - The parsed HTTP request.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error SendHttpPostResponse([In] HREQQUEUEv1 hReqQueue, HTTP_REQUEST pRequest)
	{
		Win32Error result;
		uint bytesSent;
		uint TempFileBytesWritten;
		_ = (uint)uint.MaxValue.ToString().Length;
		uint TotalBytesRead = 0;
		SafeHFILE hTempFile = null;
		System.Text.StringBuilder szTempName = new(MAX_PATH + 1);

		// Allocate some space for an entity buffer. We'll grow this on demand.
		uint EntityBufferLength = 2048;
		using SafeCoTaskMemHandle pEntityBuffer = new(EntityBufferLength);

		// Initialize the HTTP response structure.
		HTTP_RESPONSE_V1 response = INITIALIZE_HTTP_RESPONSE(200, "OK");

		// For POST, we'll echo back the entity that we got from the client.
		//
		// NOTE: If we had passed the HTTP_RECEIVE_REQUEST_FLAG_COPY_BODY flag with HttpReceiveHttpRequest(), the entity would have been a
		// part of HTTP_REQUEST (using the pEntityChunks field). Since we have not passed that flag, we can be assured that there are no
		// entity bodies in HTTP_REQUEST.

		if ((pRequest.Flags & HTTP_REQUEST_FLAG.HTTP_REQUEST_FLAG_MORE_ENTITY_BODY_EXISTS) != 0)
		{
			// The entity body is send over multiple calls. Let's collect all of these in a file & send it back. We'll create a temp file

			if (GetTempFileName(".", "New", 0, szTempName) == 0)
			{
				result = GetLastError();
				Console.Write("GetTempFileName failed with {0} \n", result);
				goto Done;
			}

			hTempFile = CreateFile(szTempName.ToString(),
				FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE,
				0, // don't share.
				default, // no security descriptor
				System.IO.FileMode.Create, // overrwrite existing
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, // normal file.
				default);

			if (hTempFile.IsInvalid)
			{
				result = GetLastError();
				Console.Write("Could not create temporary file. Error {0} \n", result);
				goto Done;
			}

			do
			{
				// Read the entity chunk from the request.
				result = HttpReceiveRequestEntityBody(hReqQueue,
					pRequest.RequestId,
					0,
					pEntityBuffer,
					EntityBufferLength,
					out var BytesRead,
					default);

				switch ((uint)result)
				{
					case Win32Error.ERROR_SUCCESS:

						if (BytesRead != 0)
						{
							TotalBytesRead += BytesRead;
							WriteFile(hTempFile,
								pEntityBuffer,
								BytesRead,
								out TempFileBytesWritten,
								default);
						}
						break;

					case Win32Error.ERROR_HANDLE_EOF:

						// We have read the last request entity body. We can send back a response.
						//
						// To illustrate entity sends via HttpSendResponseEntityBody, we will send the response over multiple calls. This is
						// achieved by passing the HTTP_SEND_RESPONSE_FLAG_MORE_DATA flag.

						if (BytesRead != 0)
						{
							TotalBytesRead += BytesRead;
							WriteFile(hTempFile,
								pEntityBuffer,
								BytesRead,
								out TempFileBytesWritten,
								default);
						}

						// Since we are sending the response over multiple API calls, we have to add a content-length.
						//
						// Alternatively, we could have sent using chunked transfer encoding, by passing "Transfer-Encoding: Chunked".

						// NOTE: Since we are accumulating the TotalBytesRead in a uint, this will not work for entity bodies that are larger
						// than 4 GB. For supporting large entity bodies, we would have to use a ulong.

						var szContentLength = TotalBytesRead.ToString();

						ADD_KNOWN_HEADER(ref response, HTTP_HEADER_ID.HttpHeaderContentLength, szContentLength);

						result = HttpSendHttpResponse(hReqQueue, // ReqQueueHandle
							pRequest.RequestId, // Request ID
							HTTP_SEND_RESPONSE_FLAG.HTTP_SEND_RESPONSE_FLAG_MORE_DATA,
							response, // HTTP response
							default, // pReserved1
							out bytesSent, // bytes sent (optional)
							default, // pReserved2
							0, // Reserved3
							default, // [In] NativeOverlapped*
							default); // pReserved4

						if (result.Failed)
						{
							Console.Write("HttpSendHttpResponse failed with {0} \n", result);
							goto Done;
						}

						// Send entity body from a file handle.
						HTTP_DATA_CHUNK dataChunk = new(hTempFile);

						result = HttpSendResponseEntityBody(hReqQueue,
							pRequest.RequestId,
							0, // This is the last send.
							1, // Entity Chunk Count.
							new[] { dataChunk },
							out _,
							default,
							0,
							default,
							default);

						if (result.Failed)
						{
							Console.Write("HttpSendResponseEntityBody failed with {0} \n", result);
						}

						goto Done;

					default:
						Console.Write("HttpReceiveRequestEntityBody failed with {0} \n", result);
						goto Done;
				}
			} while (true);
		}
		else
		{
			// This request does not have any entity body.

			result = HttpSendHttpResponse(hReqQueue, // ReqQueueHandle
				pRequest.RequestId, // Request ID
				0,
				response, // HTTP response
				default, // pReserved1
				out bytesSent, // bytes sent (optional)
				default, // pReserved2
				0, // Reserved3
				default, // [In] NativeOverlapped*
				default); // pReserved4

			if (result.Failed)
			{
				Console.Write("HttpSendHttpResponse failed with {0} \n", result);
			}
		}

Done:

		if (!hTempFile.IsInvalid)
		{
			hTempFile.Dispose();
			DeleteFile(szTempName.ToString());
		}

		return result;
	}

	/***************************************************************************++
	Routine Description:
	The routine sends a HTTP response.
	Arguments:
	hReqQueue - Handle to the request queue.
	pRequest - The parsed HTTP request.
	StatusCode - Response Status Code.
	pReason - Response reason phrase.
	pEntityString - Response entity body.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error SendHttpResponse([In] HREQQUEUEv1 hReqQueue, HTTP_REQUEST pRequest, [In] ushort StatusCode, string pReason, string entityString)
	{
		// Initialize the HTTP response structure.
		HTTP_RESPONSE_V1 response = INITIALIZE_HTTP_RESPONSE(StatusCode, pReason);

		// Add a known header.
		ADD_KNOWN_HEADER(ref response, HTTP_HEADER_ID.HttpHeaderContentType, "text/html");
		using SafeLPSTR pEntityString = new(entityString);
		SafeCoTaskMemStruct<HTTP_DATA_CHUNK> pDataChunk = SafeCoTaskMemStruct<HTTP_DATA_CHUNK>.Null;
		if (!(pEntityString is null || pEntityString.IsNull))
		{
			// Add an entity chunk
			pDataChunk = new(new HTTP_DATA_CHUNK((SafeAllocatedMemoryHandle)pEntityString));

			response.EntityChunkCount = 1;
			response.pEntityChunks = pDataChunk;
		}

		// Since we are sending all the entity body in one call, we don't have to specify the Content-Length.

		Win32Error result = HttpSendHttpResponse(hReqQueue, // ReqQueueHandle
			pRequest.RequestId, // Request ID
			0, // Flags
			response, // HTTP response
			default, // pReserved1
			out var bytesSent, // bytes sent (OPTIONAL)
			default, // pReserved2 (must be default)
			0, // Reserved3 (must be 0)
			default, // [In] NativeOverlapped* (OPTIONAL)
			default); // pReserved4 (must be default)

		if (result.Failed)
		{
			Console.Write("HttpSendHttpResponse failed with {0} \n", result);
		}

		return result;
	}
}