global using System;
global using System.Runtime.InteropServices;
global using Vanara.PInvoke;
global using static Vanara.PInvoke.WinHTTP;

const string USER_AGENT = "ProxySample";

if (args.Length != 1)
{
	Console.Write("Usage: WinhttpProxySample.exe <url>\n");
	return;
}

string pwszUrl = args[0];

try
{
	// Open a session in synchronous mode for http request.

	using SafeHINTERNET hRequestSession = WinHttpOpen(USER_AGENT);
	Win32Error.ThrowLastErrorIfInvalid(hRequestSession);

	// Open a session in asynchronous mode for proxy resolve.

	using SafeHINTERNET hProxyResolveSession = WinHttpOpen(USER_AGENT, dwFlags: WINHTTP_OPEN_FLAG.WINHTTP_FLAG_ASYNC);
	Win32Error.ThrowLastErrorIfInvalid(hProxyResolveSession);

	// Create a proxy resolver to use for this URL.

	ProxyResolver pProxyResolver = new();

	// Resolve the proxy for the specified URL.

	pProxyResolver.ResolveProxy(hProxyResolveSession, pwszUrl);

	CrackHostAndPath(pwszUrl, out string pwszHost, out string pwszPath);

	// Attempt to connect to the host and retrieve a status code.

	SendRequestToHost(hRequestSession, pProxyResolver, pwszHost, pwszPath, out HTTP_STATUS dwStatusCode);

	Console.Write("Status: {0}\n", dwStatusCode);
}
catch (Exception ex)
{
	Console.WriteLine($"Error: {ex.Message}");
}

/*++

Routine Description:

Sends a request using the Request Handle specified and implements
proxy failover if supported.

Arguments:

hRequest - The request handle returned by WinHttpOpenRequest.

pProxyResolver - The Proxy Resolver to use for the request. The resolver
is used to set the proxy infomation on the request handle and
to implement proxy failover. At this point the proxy
resolver should have been initialized by calling Resolve().
Return Value:

Win32 Error codes.

--*/
void SendRequestWithProxyFailover([In] HINTERNET hRequest, [In] ProxyResolver pProxyResolver)
{
	Win32Error dwError = Win32Error.ERROR_SUCCESS;
	Win32Error dwRequestError = Win32Error.ERROR_SUCCESS;

	// Reset the proxy list to the beginning in case it is being reused.

	pProxyResolver.ResetProxyCursor();

	for (; ; )
	{
		dwError = pProxyResolver.SetNextProxySetting(hRequest, dwRequestError);

		if (dwError == Win32Error.ERROR_NO_MORE_ITEMS)
		{
			// We reached the end of the list, failover is not supported, or the error was fatal. Fail with last sendrequest error.

			dwError = dwRequestError;
			goto quit;
		}

		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			// Some other error occured such as a bad proxy setting, bad handle, out of memory, etc.

			goto quit;
		}

		if (!WinHttpSendRequest(hRequest)) // context
		{
			dwRequestError = Win32Error.GetLastError();
			continue;
		}

		if (!WinHttpReceiveResponse(hRequest)) // reserved
		{
			dwRequestError = Win32Error.GetLastError();
			continue;
		}

		dwError = Win32Error.ERROR_SUCCESS;
		break;
	}

quit:

	Win32Error.ThrowIfFailed(dwError);
}

/*++

Routine Description:

Connects to a host with the specified proxy and returns the status code
to the caller.

Arguments:

hSession - The WinHTTP session to use for the connection.

pProxyResolver - The proxy resolver for the request.

pwszHost - The host name of the resource to connect to.

pwszPath - The path of the resource to connect to.

pdwStatusCode - The status code of the connection to the server.

Return Value:

Win32 Error codes.

--*/
void SendRequestToHost([In] HINTERNET hSession, ProxyResolver pProxyResolver, string pwszHost, string pwszPath, out HTTP_STATUS pdwStatusCode)
{
	string[] pcwszAcceptTypes = { "*/*", default };

	pdwStatusCode = 0;

	// Connect session.

	using SafeHINTERNET hConnect = WinHttpConnect(hSession, pwszHost, INTERNET_DEFAULT_HTTP_PORT);
	Win32Error.ThrowLastErrorIfInvalid(hConnect);

	// Open HTTP request.

	using SafeHINTERNET hRequest = WinHttpOpenRequest(hConnect, "GET", pwszPath, default, default, pcwszAcceptTypes, 0); // flags
	Win32Error.ThrowLastErrorIfInvalid(hRequest);

	// Send the HTTP request with proxy failover if valid.

	SendRequestWithProxyFailover(hRequest, pProxyResolver);

	// Get the status code from the response.

	pdwStatusCode = WinHttpQueryHeaders<HTTP_STATUS>(hRequest, WINHTTP_QUERY.WINHTTP_QUERY_FLAG_NUMBER | WINHTTP_QUERY.WINHTTP_QUERY_STATUS_CODE);
}

/*++

Routine Description:

Cracks the Host name and Path from a URL and returns the result to the
caller.

Arguments:

pwszUrl - The URL to crack.

ppwszHost - The Host name cracked from the URL.
Free ppwszHost with free.

ppwszPath - The Path cracked from the URL or default if no path was provided.
Free ppwszPath with free.

Return Value:

Win32 Error codes.

--*/
void CrackHostAndPath(string pwszUrl, out string ppwszHost, out string ppwszPath)
{
	ppwszHost = ppwszPath = null;

	// Get the length of each component.

	WINHTTP_URL_COMPONENTS urlComponents = new();
	Win32Error.ThrowLastErrorIfFalse(WinHttpCrackUrl(pwszUrl, 0, 0, ref urlComponents));

	ppwszHost = urlComponents.lpszHostName;
	ppwszPath = urlComponents.lpszUrlPath;
}