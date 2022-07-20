using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;

using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WinINet;

namespace async;

internal class Program
{
	const int BUFFER_LEN = 4096;
	const int ERR_MSG_LEN = 512;
	const uint BUFSIZE = 65536;
	const uint GET = 1;
	const uint POST = 2;
	const uint GET_REQ = 1;
	const uint POST_REQ = 2;
	const uint POST_RES = 3;
	const uint DEFAULT_TIMEOUT = 2 * 60 * 1000; //two minutes
	const int INVALID_DOWNLOAD_VALUE = -1;
	const int STATUS_SUCCESS = 0;
	const int STATUS_FAILURE = 1;
	static readonly IntPtr STRUCT_TYPE_MAIN_CONTEXT = (IntPtr)1;
	static readonly IntPtr STRUCT_TYPE_APP_CONTEXT = (IntPtr)2;

	//Structure containing the Session and Connect handles
	public class MAIN_CONTEXT
	{
		public SafeHINTERNET hSession;
		public SafeHINTERNET hConnect;
	}

	//Structure used for storing the context for the asynchronous calls
	//it contains the request handle and a pointer to a structure
	//Containing the Session and connect handle.
	//This allows having only one connection to the server and create multiple
	//request handles using that connection.
	public struct APP_CONTEXT
	{
		public MAIN_CONTEXT mainContext;
		public SafeHINTERNET hRequest;
		public SafeEventHandle hEvent;
		public byte[] pszOutBuffer;
		public int dwDownloaded;
		public uint dwRead;
		public uint dwWritten;
		public uint dwReadOffset;
		public int dwWriteOffset;
		public SafeHFILE hFile;
		public SafeHFILE hRes;
		public uint dwState;
		public int lPendingWrites;
		public bool bReceiveDone;
		public CRITICAL_SECTION crSection;
	}

	//Structure to be used in the Asynchronous IO operations
	public class IO_BUF
	{
		public NativeOverlapped lpo;
		public APP_CONTEXT aContext;
		public byte[] buffer = new byte[BUFFER_LEN];
	}

	//Action to perfom GET or POST
	static uint g_action;
	const string DEFAULTHOSTNAME = "www.microsoft.com";
	//Host to connect to
	static string g_hostName;
	//Resource to get from the server
	static string g_resource = "/"; //By default request the root object
	//File containing data to post
	static string g_inputFile;
	//File to write the data received from the server
	static string g_outputFile;
	//Flag to indicate the use of a proxy
	static bool g_bUseProxy = false;
	//Name of the proxy to use
	static string g_proxy = default;
	//Flag to indicate the use of SSL
	static bool g_bSecure = false;
	//Callback function
	static IntPtr g_callback;
	//Structures to be used in the Async File IO callbacks
	static IO_BUF g_readIO;
	static IO_BUF g_writeIO;
	//Timeout for the async operations
	static uint g_userTimeout = DEFAULT_TIMEOUT;
	//Indicate if we had to create a temp file
	static bool g_bCreatedTempFile = false;
	static MAIN_CONTEXT mainContext;
	static APP_CONTEXT context = new();

	public static int Main(string[] args)
	{
		Win32Error dwError = 0;
		bool bRequestSuccess;
		uint dwFileSize = 0;
		InternetOpenType dwOpenType = InternetOpenType.INTERNET_OPEN_TYPE_PRECONFIG; //Use pre-configured options as default
		INTERNET_PORT serverPort = INTERNET_PORT.INTERNET_DEFAULT_HTTP_PORT;
		INTERNET_FLAG dwRequestFlags = 0;
		string verb;
		int dwStatus = STATUS_SUCCESS;

		//Parse the command line arguments
		ParseArguments(args);

		//Initialize the context for the Session and Connection handles
		InitMainContext(out mainContext);

		if (g_bUseProxy)
		{
			dwOpenType = InternetOpenType.INTERNET_OPEN_TYPE_PROXY;
		}
		//Create Session handle and specify Async Mode
		mainContext.hSession = InternetOpen("WinInet HTTP Async Session", //User Agent
			dwOpenType, //Preconfig or Proxy
			g_proxy, //g_proxy name
			default, //g_proxy bypass, do not bypass any address
			InternetApiFlags.INTERNET_FLAG_ASYNC); // 0 for Synchronous

		if (mainContext.hSession.IsInvalid)
		{
			LogInetError(Win32Error.GetLastError(), "InternetOpen");
			dwStatus = STATUS_FAILURE;
			goto Exit;
		}

		//Set the dwStatus callback for the handle to the Callback function
		g_callback = InternetSetStatusCallback(mainContext.hSession, CallBack);

		if (g_callback == INTERNET_INVALID_STATUS_CALLBACK)
		{
			LogInetError(Win32Error.GetLastError(), "InternetSetStatusCallback");
			dwStatus = STATUS_FAILURE;
			goto Exit;
		}

		//Set the correct server port if using SSL
		//Also set the flag for HttpOpenRequest 
		if (g_bSecure)
		{
			serverPort = INTERNET_PORT.INTERNET_DEFAULT_HTTPS_PORT;
			dwRequestFlags = INTERNET_FLAG.INTERNET_FLAG_SECURE;
		}

		//Create Connection handle and provide context for async operations
		mainContext.hConnect = InternetConnect(mainContext.hSession,
			g_hostName, //Name of the server to connect to
			serverPort, //HTTP (80) or HTTPS (443)
			default, //Do not provide a user name for the server
			default, //Do not provide a password for the server
			InternetService.INTERNET_SERVICE_HTTP, 0, STRUCT_TYPE_MAIN_CONTEXT);

		//For HTTP InternetConnect returns synchronously.
		//For FTP, Win32Error.ERROR_IO_PENDING should be verified too 
		if (mainContext.hConnect.IsInvalid)
		{
			LogInetError(GetLastError(), "InternetConnect");
			dwStatus = STATUS_FAILURE;
			goto Exit;
		}


		//Initialize the context to be used in the asynchronous calls
		InitRequestContext(mainContext, ref context);

		//Open the file to dump the response entity body and
		//if required the file with the data to post
		OpenFiles(ref context);

		//Verify if we've opened a file to post and get its size
		if (!context.hFile.IsInvalid)
		{
			dwFileSize = GetFileSize(context.hFile, out _);
		}

		//Set the initial state of the context and the verb depending on the operation to perform
		if (g_action == GET)
		{
			context.dwState = GET_REQ;
			verb="GET";
		}
		else
		{
			context.dwState = POST_REQ;
			verb="POST";
		}

		//We're overriding WinInet's default behavior.
		//Setting this flags, we make sure we get the response from the server and not the cache.
		//Also ask WinInet not to store the response in the cache.
		dwRequestFlags |= INTERNET_FLAG.INTERNET_FLAG_RELOAD | INTERNET_FLAG.INTERNET_FLAG_NO_CACHE_WRITE;

		//Create a Request handle
		context.hRequest = HttpOpenRequest(context.mainContext.hConnect,
			verb, //GET or POST
			g_resource, //root "/" by default
			default, //USe default HTTP/1.1 as the version
			default, //Do not provide any referrer
			default, //Do not provide Accept types
			dwRequestFlags, STRUCT_TYPE_APP_CONTEXT);//(0 or INTERNET_FLAG_SECURE) | INTERNET_FLAG_RELOAD | INTERNET_FLAG_NO_CACHE_WRITE

		if (context.hRequest.IsInvalid)
		{
			LogInetError(GetLastError(), "HttpOpenRequest");
			dwStatus = STATUS_FAILURE;
			goto Exit;
		}

		//Send the request using two different options.
		//HttpSendRequest for GET and HttpSendRequestEx for POST.
		//HttpSendRequest can also be used also to post data to a server, 
		//to do so, the data should be provided using the lpOptional
		//parameter and it's size on dwOptionalLength.
		//Here we decided to depict the use of both HttpSendRequest functions.
		if (g_action == GET)
		{
			bRequestSuccess = HttpSendRequest(context.hRequest,
				default, //do not provide additional Headers
				0, //dwHeadersLength 
				default, //Do not send any data 
				0); //dwOptionalLength 
		}
		else
		{
			//Prepare the Buffers to be passed to HttpSendRequestEx
			INTERNET_BUFFERS buffersIn = new()
			{
				dwStructSize = (uint)Marshal.SizeOf(typeof(INTERNET_BUFFERS)),
				dwBufferTotal = dwFileSize //content-length of data to post
			};

			bRequestSuccess = HttpSendRequestEx(context.hRequest, buffersIn);
		}

		if (!bRequestSuccess && (dwError=GetLastError())!=Win32Error.ERROR_IO_PENDING)
		{
			LogInetError(dwError, "HttpSendRequest(Ex)");
			dwStatus = STATUS_FAILURE;
			goto Exit;
		}

		//If you're using a UI thread, this call is not required
		var dwSync = WaitForSingleObject(context.hEvent,
			g_userTimeout); // Wait until we receive the completion

		switch (dwSync)
		{
			case WAIT_STATUS.WAIT_OBJECT_0:
				Console.Write("Done!\n");
				break;
			case WAIT_STATUS.WAIT_ABANDONED:
				Console.Error.Write("The callback thread was terminated\n");
				dwStatus = STATUS_FAILURE;
				break;
			case WAIT_STATUS.WAIT_TIMEOUT:
				Console.Error.Write("Timeout while waiting for event\n");
				dwStatus = STATUS_FAILURE;
				break;
		}

Exit:

		CleanUp(ref context);
		return dwStatus;
	}

	/*++
	Routine Description:
	Callback routine for asynchronous WinInet operations
	Arguments:
	hInternet - The handle for which the callback function is called.
	dwContext - Pointer to the application defined context.
	dwInternetStatus - Status code indicating why the callback is called.
	lpvStatusInformation - Pointer to a buffer holding callback specific data.
	dwStatusInformationLength - Specifies size of lpvStatusInformation buffer.
	Return Value:
	None.
	--*/
	static void CallBack(HINTERNET hInternet, IntPtr dwContext, InternetStatus dwInternetStatus, IntPtr lpvStatusInformation, uint dwStatusInformationLength)
	{
		Win32Error dwError;
		uint dwBytes;
		bool bQuit = false;

		Console.Error.Write("Callback Received for Handle {0} \t", hInternet);

		switch (dwInternetStatus)
		{
			case InternetStatus.INTERNET_STATUS_COOKIE_SENT:
				Console.Error.Write("Status: Cookie found and will be sent with request\n");
				break;
			case InternetStatus.INTERNET_STATUS_COOKIE_RECEIVED:
				Console.Error.Write("Status: Cookie Received\n");
				break;
			case InternetStatus.INTERNET_STATUS_COOKIE_HISTORY:
				{
					InternetCookieHistory cookieHistory = default;
					Console.Error.Write("Status: Cookie History\n");

					//Verify we've a valid pointer with the correct size
					if (lpvStatusInformation != default && dwStatusInformationLength == Marshal.SizeOf(typeof(InternetCookieHistory)))
					{
						cookieHistory = lpvStatusInformation.ToStructure<InternetCookieHistory>(dwStatusInformationLength);
					}
					else
					{
						Console.Error.Write("Cookie History not valid\n");
						goto ExitSwitch;
					}
					if (cookieHistory.fAccepted)
					{
						Console.Error.Write("Cookie Accepted\n");
					}
					if (cookieHistory.fLeashed)
					{
						Console.Error.Write("Cookie Leashed\n");
					}
					if (cookieHistory.fDowngraded)
					{
						Console.Error.Write("Cookie Downgraded\n");
					}
					if (cookieHistory.fRejected)
					{
						Console.Error.Write("Cookie Rejected\n");
					}
				}
ExitSwitch:
				break;
			case InternetStatus.INTERNET_STATUS_CLOSING_CONNECTION:
				Console.Error.Write("Status: Closing Connection\n");
				break;
			case InternetStatus.INTERNET_STATUS_CONNECTED_TO_SERVER:
				Console.Error.Write("Status: Connected to Server\n");
				break;
			case InternetStatus.INTERNET_STATUS_CONNECTING_TO_SERVER:
				Console.Error.Write("Status: Connecting to Server\n");
				break;
			case InternetStatus.INTERNET_STATUS_CONNECTION_CLOSED:
				Console.Error.Write("Status: Connection Closed\n");
				break;
			case InternetStatus.INTERNET_STATUS_HANDLE_CLOSING:
				Console.Error.Write("Status: Handle Closing\n");
				//Signal the event for closing the handle
				//only for the Request Handle
				if (dwContext == STRUCT_TYPE_APP_CONTEXT)
				{
					SetEvent(context.hEvent);
				}
				break;
			case InternetStatus.INTERNET_STATUS_HANDLE_CREATED:
				//Verify we've a valid pointer
				if (lpvStatusInformation != default)
				{
					Console.Error.Write("Handle {0:x} created\n", lpvStatusInformation.AsRef<INTERNET_ASYNC_RESULT>().dwResult);
				}
				break;
			case InternetStatus.INTERNET_STATUS_INTERMEDIATE_RESPONSE:
				Console.Error.Write("Status: Intermediate response\n");
				break;
			case InternetStatus.INTERNET_STATUS_RECEIVING_RESPONSE:
				Console.Error.Write("Status: Receiving Response\n");
				break;
			case InternetStatus.INTERNET_STATUS_RESPONSE_RECEIVED:
				//Verify we've a valid pointer with the correct size
				if (lpvStatusInformation != default && dwStatusInformationLength == Marshal.SizeOf(typeof(uint)))
				{
					dwBytes = lpvStatusInformation.ToStructure<uint>();
					Console.Error.Write("Status: Response Received ({0} Bytes)\n", dwBytes);
				}
				else
				{
					Console.Error.Write("Response Received: lpvStatusInformation not valid\n");
				}
				break;
			case InternetStatus.INTERNET_STATUS_REDIRECT:
				Console.Error.Write("Status: Redirect\n");
				break;
			case InternetStatus.INTERNET_STATUS_REQUEST_COMPLETE:
				Console.Error.Write("Status: Request complete\n");

				//check for error first 
				dwError = lpvStatusInformation.AsRef<INTERNET_ASYNC_RESULT>().dwError;

				if (dwError != Win32Error.ERROR_SUCCESS)
				{
					LogInetError(dwError, "Request_Complete");
					Environment.Exit(1);
				}

				switch (context.dwState)
				{
					case POST_REQ:
						//read bytes to write 
						if ((dwError = DoReadFile(ref context)) != Win32Error.ERROR_SUCCESS && dwError!= Win32Error.ERROR_IO_PENDING)
						{
							LogSysError(dwError, "DoReadFile");
							Environment.Exit(1);
						}

						break;

					case POST_RES: //fall through 
					case GET_REQ:

						if (context.dwDownloaded == 0)
						{
							EnterCriticalSection(ref context.crSection);
							{
								context.bReceiveDone=true;

								if (context.lPendingWrites == 0)
								{
									bQuit = true;
								}

							}
							LeaveCriticalSection(ref context.crSection);

							if (bQuit)
							{
								SetEvent(context.hEvent);
							}

							break;
						}
						else if (context.dwDownloaded != INVALID_DOWNLOAD_VALUE)
						{
							g_writeIO = new();

							InterlockedIncrement(ref context.lPendingWrites);

							g_writeIO.aContext = context;

							g_writeIO.lpo.OffsetLow = context.dwWriteOffset;
							Array.Copy(context.pszOutBuffer, g_writeIO.buffer, context.pszOutBuffer.Length);

							if (!WriteFile(context.hRes, g_writeIO.buffer, context.dwDownloaded, out _, ref g_writeIO.lpo))
							{
								if ((dwError=GetLastError())!= Win32Error.ERROR_IO_PENDING)
								{
									LogSysError(dwError, "WriteFile");
									Environment.Exit(1);
								}
							}

							context.dwWriteOffset += context.dwDownloaded;

						}
						else
						{
							//context.dwDownloaded ==INVALID_DOWNLOAD_VALUE
							//We're in the initial state of the response's download
							Console.Error.Write("Ready to start reading the Response Entity Body\n");
						}

						DoInternetRead(ref context);

						break;
				}

				break;
			case InternetStatus.INTERNET_STATUS_REQUEST_SENT:
				//Verify we've a valid pointer with the correct size
				if (lpvStatusInformation != default && dwStatusInformationLength == Marshal.SizeOf(typeof(uint)))
				{
					dwBytes = lpvStatusInformation.ToStructure<uint>();
					Console.Error.Write("Status: Request sent ({0} Bytes)\n", dwBytes);
				}
				else
				{
					Console.Error.Write("Request sent: lpvStatusInformation not valid\n");
				}
				break;
			case InternetStatus.INTERNET_STATUS_DETECTING_PROXY:
				Console.Error.Write("Status: Detecting Proxy\n");
				break;
			case InternetStatus.INTERNET_STATUS_RESOLVING_NAME:
				Console.Error.Write("Status: Resolving Name\n");
				break;
			case InternetStatus.INTERNET_STATUS_NAME_RESOLVED:
				Console.Error.Write("Status: Name Resolved\n");
				break;
			case InternetStatus.INTERNET_STATUS_SENDING_REQUEST:
				Console.Error.Write("Status: Sending request\n");
				break;
			case InternetStatus.INTERNET_STATUS_STATE_CHANGE:
				Console.Error.Write("Status: State Change\n");
				break;
			case InternetStatus.INTERNET_STATUS_P3P_HEADER:
				Console.Error.Write("Status: Received P3P header\n");
				break;
			default:
				Console.Error.Write("Status: Unknown ({0})\n", dwInternetStatus);
				break;
		}
	}

	/*++
	Routine Description:
	This routine handles asynschronous file reads.
	Arguments:
	aContext - Pointer to application context structure
	Return Value:
	Error code for the operation.
	--*/
	static Win32Error DoReadFile(ref APP_CONTEXT aContext)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		g_readIO = new() { aContext = aContext, lpo = new NativeOverlapped { OffsetLow = unchecked((int)aContext.dwReadOffset) } };

		if (!ReadFile(aContext.hFile, g_readIO.buffer, BUFFER_LEN, out _, ref g_readIO.lpo))
		{
			if ((dwError=GetLastError()) == Win32Error.ERROR_HANDLE_EOF)
			{
				//Clear the error code since we've handled the error conditions
				dwError = DoCompleteReadFile(ref aContext);
			}
			else if (dwError != Win32Error.ERROR_IO_PENDING)
			{
				LogSysError(dwError, "ReadFile");
				goto Exit;
			}
		}

Exit:
		return dwError;
	}

	/*++
	Routine Description:
	This routine handles asynschronous file reads.
	Arguments:
	aContext - Pointer to application context structure
	Return Value:
	Error Code for the operation.
	--*/
	static Win32Error DoCompleteReadFile(ref APP_CONTEXT aContext)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		Console.Error.Write("Finished posting file\n");
		aContext.dwState = POST_RES;
		if (!HttpEndRequest(aContext.hRequest))
		{
			if ((dwError = Win32Error.GetLastError()) == Win32Error.ERROR_IO_PENDING)
			{
				Console.Error.Write("Waiting for HttpEndRequest to complete \n");
			}
			else
			{
				LogInetError(dwError, "HttpEndRequest");
				goto Exit;
			}
		}
		else
		{
			DoInternetRead(ref aContext);
		}

Exit:
		return dwError;
	}

	/*++
	Routine Description:
	This routine handles WinInet Writes. If a write completes synchronously
	this routine issues a file read to gather data to post.
	Arguments:
	aContext - Pointer to application context structure
	Return Value:
	None.
	--*/
	static void DoInternetWrite(ref APP_CONTEXT aContext)
	{
		Win32Error dwError;

		if (InternetWriteFile(aContext.hRequest,
			aContext.pszOutBuffer,
			(int)aContext.dwRead,
			out aContext.dwWritten))
		{
			//read bytes to write 
			if ((dwError=DoReadFile(ref aContext)) != Win32Error.ERROR_SUCCESS
			&& dwError != Win32Error.ERROR_IO_PENDING)

			{
				LogSysError(dwError, "DoReadFile");
				Environment.Exit(1);
			}
		}

		if ((dwError=GetLastError()) == Win32Error.ERROR_IO_PENDING)
		{
			Console.Error.Write("Waiting for InternetWriteFile to complete\n");
		}
		else if (dwError != Win32Error.ERROR_SUCCESS)
		{
			LogInetError(dwError, "InternetWriteFile");
			Environment.Exit(1);
		}
	}

	/*++
	Routine Description:
	This routine handles WinInet/Internet reads.
	Arguments:
	aContext - Pointer to application context structure
	Return Value:
	None.
	--*/
	static void DoInternetRead(ref APP_CONTEXT aContext)
	{
		Win32Error dwError;

		while (InternetReadFile(aContext.hRequest,
			aContext.pszOutBuffer,
			BUFFER_LEN,
			out aContext.dwDownloaded))
		{
			//completed synchronously ; callback won't be issued
			bool bQuit = false;

			if (aContext.dwDownloaded == 0)
			{
				EnterCriticalSection(ref aContext.crSection);
				{
					aContext.bReceiveDone = true;

					if (aContext.lPendingWrites == 0)
					{
						bQuit = true;
					}

				}
				LeaveCriticalSection(ref aContext.crSection);

				if (bQuit)
				{
					SetEvent(aContext.hEvent);
				}
				return;
			}

			g_writeIO = new() { aContext = aContext, lpo = new NativeOverlapped { OffsetLow = unchecked((int)aContext.dwWriteOffset) } };

			InterlockedIncrement(ref aContext.lPendingWrites);

			Array.Copy(aContext.pszOutBuffer, g_writeIO.buffer, aContext.dwDownloaded);

			if (!WriteFile(aContext.hRes,
				g_writeIO.buffer,
				aContext.dwDownloaded,
				out _,
				ref g_writeIO.lpo))
			{

				if ((dwError = GetLastError()) != Win32Error.ERROR_IO_PENDING)
				{
					LogSysError(dwError, "WriteFile");
					Environment.Exit(1);
				}
			}

			aContext.dwWriteOffset += aContext.dwDownloaded;

		}

		if ((dwError=GetLastError()) == Win32Error.ERROR_IO_PENDING)
		{
			Console.Error.Write("Waiting for InternetReadFile to complete\n");
		}
		else
		{
			LogInetError(dwError, "InternetReadFile");
			Environment.Exit(1);
		}
	}

	/*++
	Routine Description:
	Callback routine for Asynchronous file write completions. This 
	routine determines if the response is completely received and 
	all writes are completed before signalling the event to terminate
	the program. Frees the Overlapped object.
	Arguments:
	dwErrorCode - I/O completion Status
	dwNumberOfBytesTransfered - Number of bytes transfered
	lpOverlapped - Pointer to the overlapped structure used in WriteFile
	The way IO_BUF structure is designed, this pointer also
	points to the IO_BUF structure.
	Return Value:
	None.
	--*/
	static void WriteFileCallBack(uint dwErrorCode, uint dwNumberOfBytesTransfered, IntPtr lpOverlapped)
	{
		bool bQuit = false;

		if (dwErrorCode == Win32Error.ERROR_SUCCESS)
		{
			EnterCriticalSection(ref context.crSection);
			{
				if (InterlockedDecrement(ref context.lPendingWrites) == 0 && context.bReceiveDone)
				{
					bQuit = true;

				}
			}
			LeaveCriticalSection(ref context.crSection);

			if (bQuit)
			{
				SetEvent(context.hEvent);
			}
		}

		return;
	}

	/*++
	Routine Description:
	Callback routine for Asynchronous file read completions. On successful
	file read this routine triggers a WinInet Write operation to transfer data
	to the http server.
	Arguments:
	dwErrorCode - I/O completion Status
	dwNumberOfBytesTransfered - Number of bytes read
	lpOverlapped - Pointer to the overlapped structure used in WriteFile
	The way IO_BUF structure is designed, this pointer also
	points to the IO_BUF structure.
	Return Value:
	None.
	--*/
	static void ReadFileCallBack(uint dwErrorCode, uint dwNumberOfBytesTransfered, IntPtr lpOverlapped)
	{
		if (dwErrorCode == Win32Error.ERROR_SUCCESS || NTStatus.RtlNtStatusToDosError(unchecked((int)dwErrorCode)) == Win32Error.ERROR_HANDLE_EOF)
		{
			if (dwErrorCode == Win32Error.ERROR_SUCCESS)
			{
				context.dwReadOffset += dwNumberOfBytesTransfered;
				context.dwRead = dwNumberOfBytesTransfered;

				Array.Copy(context.pszOutBuffer, g_readIO.buffer, context.dwRead);

				DoInternetWrite(ref context);
			}
			else //Win32Error.ERROR_HANDLE_EOF
			{
				DoCompleteReadFile(ref context);
			}
		}
		return;
	}

	/*++
	Routine Description:
	This routine initializes the session and connection handles.
	Arguments:
	aMainContext - Pointer to MAIN_CONTEXT structure
	Return Value:
	None.
	--*/
	static void InitMainContext(out MAIN_CONTEXT aMainContext)
	{
		aMainContext = new()
		{
			hSession = default,
			hConnect = default
		};
	}

	/*++
	Routine Description:
	This routine initializes application request context variables to appropriate
	values.
	Arguments:
	aContext - Pointer to Application context structure
	aMainContext - Pointer to MAIN_CONTEXT structure containing the sesion and 
	connection handles.
	Return Value:
	None.
	--*/
	static void InitRequestContext(MAIN_CONTEXT aMainContext, ref APP_CONTEXT aContext)
	{
		aContext.mainContext = aMainContext;
		aContext.hRequest = default;
		aContext.dwDownloaded = INVALID_DOWNLOAD_VALUE;
		aContext.dwRead = 0;
		aContext.dwWritten = 0;
		aContext.dwReadOffset = 0;
		aContext.dwWriteOffset = 0;
		aContext.lPendingWrites = 0;
		aContext.bReceiveDone = false;
		aContext.hFile = default;
		aContext.hRes = default;

		aContext.pszOutBuffer = new byte[BUFFER_LEN];

		//create event
		aContext.hEvent = CreateEvent(default, //Sec attrib
			false, //Auto reset
			false, //Initial state unsignalled
			"MAIN_SYNC");

		if (aContext.hEvent.IsInvalid)
		{
			LogSysError(GetLastError(), "CreateEvent");
			Environment.Exit(1);
		}

		//initialize critical section
		InitializeCriticalSection(out aContext.crSection);

		return;
	}

	/*++
	Routine Description:
	Used to cleanup application context before exiting.
	Arguments:
	aContext - Application context structure
	Return Value:
	None.
	--*/
	static void CleanUp(ref APP_CONTEXT aContext)
	{
		aContext.hFile?.Dispose();
		aContext.hRes?.Dispose();
		if (!(aContext.hRequest?.IsClosed ?? false))
		{
			aContext.hRequest.Dispose();
			// Wait for the closing of the handle
			var dwSync = WaitForSingleObject(aContext.hEvent, INFINITE);
			if (WAIT_STATUS.WAIT_ABANDONED == dwSync)
			{
				Console.Error.Write("The callback thread has terminated.\n");
			}
		}
		if (!aContext.mainContext.hConnect.IsClosed)
		{
			//Remove the callback from the Connection handle.
			//Since se set the callback on the session handle previous to create the conenction and
			//request handles, they inherited the callback function.
			//Setting the callback function to null in the Connect handle, will ensure we don't get 
			//a notification when the handle is closed
			g_callback = InternetSetStatusCallback(aContext.mainContext.hConnect, default);

			//Call InternetCloseHandle and do not wait for the closing notification 
			//in the callback funciton
			aContext.mainContext.hConnect.Dispose();

		}
		if (!aContext.mainContext.hSession.IsClosed)
		{
			//Remove the callback from the Session handle
			g_callback = InternetSetStatusCallback(aContext.mainContext.hSession, default);
			//At this point the Session handle should be valid
			aContext.mainContext.hSession.Dispose();
		}
		aContext.hEvent?.Dispose();

		DeleteCriticalSection(ref aContext.crSection);
	}

	/*++
	Routine Description:
	This routine opens files in async mode and binds a thread from the
	thread-pool to handle the callback for asynchronous operations. Always
	opens a file to write output to. 
	Arguments:
	aContext - Pointer to Application context structure
	Return Value:
	None.
	--*/
	static void OpenFiles(ref APP_CONTEXT aContext)
	{
		if (g_action == POST)
		{
			//Open input file
			aContext.hFile = CreateFile(g_inputFile,
				FileAccess.GENERIC_READ,
				System.IO.FileShare.Read,
				default, // handle cannot be inherited
				System.IO.FileMode.Open, // if file exists, open it
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL|FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED,
				default); //No template file

			if (aContext.hFile.IsInvalid)
			{
				LogSysError(GetLastError(), "CreateFile");
				Environment.Exit(1);
			}

			if (!BindIoCompletionCallback(aContext.hFile, ReadFileCallBack, 0))
			{
				LogSysError(GetLastError(), "BindIoCompletionCallback");
				Environment.Exit(1);

			}
		}

		//Open output file
		aContext.hRes = CreateFile(g_outputFile,
			FileAccess.GENERIC_WRITE,
			0, //Open exclusively
			default, //handle cannot be inherited
			System.IO.FileMode.Create, // if file exists, delete it
			FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL|FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED,
			default); //No template file

		if (aContext.hRes.IsInvalid)
		{
			LogSysError(GetLastError(), "CreateFile");
			Environment.Exit(1);
		}

		if (!BindIoCompletionCallback(aContext.hRes, WriteFileCallBack, 0))
		{
			LogSysError(GetLastError(), "BindIoCompletionCallback");
			Environment.Exit(1);
		}
	}


	/*++
	Routine Description:
	This routine is used to Parse command line arguments. Flags are
	case sensitive.
	Arguments:
	args.Length - Number of arguments
	args - Pointer to the argument vector
	Return Value:
	None.
	--*/
	static void ParseArguments(string[] args)
	{
		uint dwError = 0;
		uint uRetVal;

		for (int i = 1; 0 < args.Length; ++i)
		{
			if (string.Compare(args[i], 0, "-", 0, 1) != 0)
			{
				Console.Write("Invalid switch {0}\n", args[i]);
				i++;
				continue;
			}

			switch (args[i][1])
			{
				case 'p':

g_bUseProxy = true;
					if (i < args.Length-1)
					{
						g_proxy = args[++i];
					}
					break;

				case 'h':

if (i < args.Length-1)
					{
						g_hostName = args[++i];
					}

					break;

				case 'o':

if (i < args.Length-1)
					{
						g_resource = args[++i];
					}

					break;

				case 'r':

if (i < args.Length-1)
					{
						g_inputFile = args[++i];
					}

					break;

				case 'w':

if (i < args.Length-1)
					{
						g_outputFile = args[++i];
					}

					break;

				case 'a':

if (i < args.Length-1)
					{
						if (string.Compare(args[i+1], 0, "get", 0, 3, true) == 0)
						{
							g_action = GET;
						}
						else if (string.Compare(args[i+1], 0, "post", 0, 4, true) == 0)
						{
							g_action = POST;
						}
					}
					++i;
					break;

				case 's':
g_bSecure = true;
					break;

				case 't':
if (i < args.Length-1)
					{
						//Verify the user provided a valid number for the default time
						if (false != char.IsDigit(args[i+1][0]))
						{
							g_userTimeout = uint.Parse(args[++i]);
						}
					}
					break;

				default:
					ShowUsage();
					Environment.Exit(1);
					break;
			}
		}

		if (g_hostName is null)
		{
			Console.Write("Defaulting hostname to: {0}\n", DEFAULTHOSTNAME);
			g_hostName = DEFAULTHOSTNAME;
		}

		if (g_action == 0)
		{
			Console.Write("Defaulting action to: GET\n");
			g_action = GET;
		}

		if (g_inputFile is null && g_action == POST)
		{
			Console.Write("Error: File to post not specified\n");
			dwError++;
		}

		if (g_outputFile is null)
		{
			g_bCreatedTempFile = true;
			var outputFile = new StringBuilder(MAX_PATH);
			// Create a temporary file. 
			uRetVal = GetTempFileName(".", // current directory 
			"TMP", // temp file name prefix 
			0, // create unique name 
			outputFile); // buffer for name 
			if (uRetVal == 0)
			{
				Console.Write("GetTempFileName failed with error %d.\n", GetLastError());
				dwError++;
			}
			else
			{
				Console.Write("Defaulting output file to: %ws\n", g_outputFile = outputFile.ToString());
			}
		}

		if (dwError is not 0)
		{
			Environment.Exit(1);
		}
	}

	/*++
	Routine Description:
	Shows the usage of the application.
	Arguments:
	None.
	Return Value:
	None.
	--*/
	static void ShowUsage()
	{
		Console.Write("Usage: async [-a {get|post}] [-h <hostname>] [-o <resourcename>] [-s] ");
		Console.Write("[-p <proxyname>] [-w <output filename>] [-r <file to post>] [-t <userTimeout>]\n");
		Console.Write("Flag Semantics: \n");
		Console.Write("-a : Specify action (\"get\" if omitted)\n");
		Console.Write("-h : Specify Hostname (\"www.microsoft.com\" if omitted)\n");
		Console.Write("-o : Specify resource name in the server (\"/\" if omitted)\n");
		Console.Write("-s : Use secure connection - https\n");
		Console.Write("-p : Specify Proxy\n");
		Console.Write("-w : Specify file to write output to (generate temp file if omitted)\n");
		Console.Write("-r : Specify file to post data from\n");
		Console.Write("-t : Specify time to wait for completing the operation in async mode. Default 2 minutes");
	}

	/*++
	Routine Description:
	This routine is used to log WinInet errors in human readable form.
	Arguments:
	err - Error number obtained from GetLastError()
	str - String pointer holding caller-context information 
	Return Value:
	None.
	--*/
	static void LogInetError(Win32Error err, string str)
	{
		StringBuilder msgBuffer = new(ERR_MSG_LEN);
		var dwResult = FormatMessage(FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE, GetModuleHandle("wininet.dll"),
			(uint)err, LANGID.LANG_USER_DEFAULT, msgBuffer, (uint)msgBuffer.Capacity, default);
		if (dwResult != 0)
			Console.Error.Write("{0}: {1}\n", str, msgBuffer);
		else
			Console.Error.Write("Error {0} while formatting message for {1} in {2}\n", GetLastError(), err, str);
	}

	/*++
	Routine Description:
	This routine is used to log System Errors in human readable form.
	Arguments:
	err - Error number obtained from GetLastError()
	str - String pointer holding caller-context information 
	Return Value:
	None.
	--*/
	static void LogSysError(Win32Error err, string str) => Console.Error.Write("{0}: {1}\n", str, err.ToString());
}