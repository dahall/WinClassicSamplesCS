using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;

using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WinHTTP;

namespace WinHttpPostSample;

internal class Program
{
	static int Main(string[] args)
	{
		string szServerUrl = args[0];
		string szFilename = args[1];
		bool fShowHelp = false;

		string szProxyUrl;
		//
		// Make sure we got three or four params 
		//(the first is the executeable name, rest are user params)
		//

		if (args.Length != 3)
		{
			if (args.Length == 2)
			{
				szProxyUrl = default;
			}
			else
			{
				fShowHelp = true;
				goto done;
			}
		}
		else
		{
			szProxyUrl = args[2];
		}

		//
		// Do some validation on the input URLs..
		//

		WINHTTP_URL_COMPONENTS urlComponents = new()
		{
			dwStructSize = (uint)Marshal.SizeOf(typeof(WINHTTP_URL_COMPONENTS)),
			dwUserNameLength = 1,
			dwPasswordLength = 1,
			dwHostNameLength = 1,
			dwUrlPathLength = 1
		};

		if (!WinHttpCrackUrl(szServerUrl, 0, 0, ref urlComponents))
		{
			Console.Write("\nThere was a problem with the Server URL {0}.\n", szServerUrl);
			fShowHelp = true;
			goto done;
		}

		urlComponents = new()
		{
			dwStructSize = (uint)Marshal.SizeOf(typeof(WINHTTP_URL_COMPONENTS)),
			dwUserNameLength = 1,
			dwPasswordLength = 1,
			dwHostNameLength = 1,
			dwUrlPathLength = 1
		};

		if (szProxyUrl is not null 
			&& (!WinHttpCrackUrl(szProxyUrl, 0, 0, ref urlComponents)
			|| urlComponents.dwUrlPathLength > 1
			|| urlComponents.nScheme != INTERNET_SCHEME.INTERNET_SCHEME_HTTP))
		{
			Console.Write("\nThere was a problem with the Proxy URL {0}." +
				" It should be a http:// url, and should have" +
				" an empty path.\n", szProxyUrl);
			fShowHelp = true;
			goto done;
		}

		//
		// Make sure a file was passed in...
		//
		if (INVALID_FILE_ATTRIBUTES == (uint)GetFileAttributes(szFilename))
		{
			Console.Write("\nThe specified file, \"{0}\", was not found.\n", szFilename);
			fShowHelp = true;
			goto done;
		}

		if (!WinHttpSamplePost(szServerUrl, szFilename, szProxyUrl))
			goto done;

		SetLastError(0);

done:
		var dwTemp = GetLastError();
		if (dwTemp.Failed)
		{
			Console.Write("\nWinHttpPostSample failed with error code {0}.", dwTemp);
		}

		if (fShowHelp)
		{
			Console.Write("\n\n Proper usage of this example is \"Post <url1> <filename> [url2]\",\n" +
				"Where <url1> is the target HTTP URL and <filename> is the name of a file\n" +
				"which will be POST'd to <url1>. [url2] is optional and indicates the proxy\n" +
				"to use.\n" +
				" Urls are of the form http://[username]:[password]@<server>[:port]/<path>.");
		}

		Console.Write("\n\n");

		return 0;
	}

	static bool GetFileHandleAndSize(string szFilename, out SafeHFILE pHandle, out uint pdwSize)
	{
		bool returnValue = false;
		long liSize = 0;

		using var hFile = CreateFile(szFilename, FileAccess.GENERIC_READ, System.IO.FileShare.Read, default,
			System.IO.FileMode.Open, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, default);

		if (hFile.IsInvalid)
			goto done;

		if (!GetFileSizeEx(hFile, out liSize))
			goto done;

		if (liSize > uint.MaxValue)
		{
			// Lets not try to send anything larger than 2 gigs
			SetLastError(Win32Error.ERROR_OPEN_FAILED);
		}

		returnValue = true;
done:

		if (returnValue)
		{
			pHandle = hFile;
			pdwSize = (uint)liSize;
			return true;
		}
		else
		{
			pHandle = default;
			pdwSize = 0;
			return false;
		}
	}


	static WINHTTP_AUTH_SCHEME ChooseAuthScheme(HINTERNET hRequest, WINHTTP_AUTH_SCHEME dwSupportedSchemes)
	{
		// It is the servers responsibility to only accept authentication schemes
		//which provide the level of security needed to protect the server's
		//resource.
		// However the client has some obligation when picking an authentication
		//scheme to ensure it provides the level of security needed to protect
		//the client's username and password from being revealed. The Basic authentication
		//scheme is risky because it sends the username and password across the
		//wire in a format anyone can read. This is not an issue for SSL connections though.

		if ((dwSupportedSchemes & WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_NEGOTIATE) != 0)
			return WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_NEGOTIATE;
		else if ((dwSupportedSchemes & WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_NTLM) != 0)
			return WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_NTLM;
		else if ((dwSupportedSchemes & WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_PASSPORT) != 0)
			return WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_PASSPORT;
		else if ((dwSupportedSchemes & WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_DIGEST) != 0)
			return WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_DIGEST;
		else if ((dwSupportedSchemes & WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_BASIC) != 0)
		{
			var dwValue = WinHttpQueryOption<SECURITY_FLAG>(hRequest, WINHTTP_OPTION.WINHTTP_OPTION_SECURITY_FLAGS);
			return (dwValue & SECURITY_FLAG.SECURITY_FLAG_SECURE) != 0 ? WINHTTP_AUTH_SCHEME.WINHTTP_AUTH_SCHEME_BASIC : 0;
		}
		else
			return 0;
	}


	static bool WinHttpSamplePost(string szServerUrl, string szFile, string szProxyUrl)
	{
		bool returnValue = false;
		string strTargetUsername = null;
		string strTargetPassword = null;
		string strProxyServer = null;
		string strProxyUsername = null;
		string strProxyPassword = null;
		SafeHINTERNET hSession = default, hConnect = default, hRequest = default;
		WINHTTP_AUTH_SCHEME dwProxyAuthScheme = 0;
		uint dwStatusCode;

		if (!GetFileHandleAndSize(szFile, out var hFile, out var dwFileSize))
			goto done;

		//
		// Its a long but straightforward chunk of code below that
		//splits szServerUrl and szProxyUrl into the various
		//strTarget*,strProxy* components. They need to be put
		//into the separate str* variables so that they are individually
		//default-terminated.
		//

		// From the server URL, we need a host, path, username and password.
		WINHTTP_URL_COMPONENTS urlServerComponents = new()
		{
			dwStructSize = (uint)Marshal.SizeOf(typeof(WINHTTP_URL_COMPONENTS)),
			dwHostNameLength = 1,
			dwUrlPathLength = 1,
			dwUserNameLength = 1,
			dwPasswordLength = 1
		};

		if (!WinHttpCrackUrl(szServerUrl, 0, 0, ref urlServerComponents))
			goto done;

		//
		// An earlier version of WinHttp v5.1 has a bug where it misreports
		//the length of the username or password if they are not present.
		//

		if (urlServerComponents.lpszUserName.IsNull)
			urlServerComponents.dwUserNameLength = 0;

		if (urlServerComponents.lpszPassword.IsNull)
			urlServerComponents.dwPasswordLength = 0;

		string strTargetServer = urlServerComponents.lpszHostName;
		string strTargetPath = urlServerComponents.lpszUrlPath;

		// for the username and password, if they are empty, leave the string pointers as default.
		// This allows for the current process's default credentials to be used.
		if (urlServerComponents.dwUserNameLength != 0)
			strTargetUsername = urlServerComponents.lpszUserName;

		if (urlServerComponents.dwPasswordLength != 0)
			strTargetPassword = urlServerComponents.lpszPassword;

		if (szProxyUrl is not null)
		{
			// From the proxy URL, we need a host, username and password.
			WINHTTP_URL_COMPONENTS urlProxyComponents = new()
			{
				dwStructSize = (uint)Marshal.SizeOf(typeof(WINHTTP_URL_COMPONENTS)),
				dwHostNameLength = 1,
				dwUserNameLength = 1,
				dwPasswordLength = 1
			};

			if (!WinHttpCrackUrl(szProxyUrl, 0, 0, ref urlProxyComponents))
				goto done;

			//
			// An earlier version of WinHttp v5.1 has a bug where it misreports
			//the length of the username or password if they are not present.
			//

			if (urlProxyComponents.lpszUserName.IsNull)
				urlProxyComponents.dwUserNameLength = 0;

			if (urlProxyComponents.lpszPassword.IsNull)
				urlProxyComponents.dwPasswordLength = 0;

			// We do something tricky here, taking from the host beginning 
			// to the beginning of the path as the strProxyServer. What this 
			// does, is if you have urls like "http://proxy","http://proxy/",
			// "http://proxy:8080" is copy them as "proxy","proxy","proxy:8080" 
			// respectively. This makes the port available for WinHttpOpen.
			strProxyServer = urlProxyComponents.lpszHostName;
			var idx = urlProxyComponents.lpszHostName.ToString()?.IndexOf("://") ?? -1;
			if (idx >= 0)
				strProxyServer = strProxyServer.Remove(0, idx + 3);

			// for the username and password, if they are empty, leave the string pointers as default.
			// This allows for the current process's default credentials to be used.
			if (urlProxyComponents.dwUserNameLength != 0)
				strProxyUsername = urlProxyComponents.lpszUserName;

			if (urlProxyComponents.dwPasswordLength != 0)
				strProxyPassword = urlProxyComponents.lpszPassword;
		}


		//
		// whew, now we can go on and start the request.
		//

		//
		// Open a WinHttp session using the specified proxy
		//
		if (szProxyUrl is not null)
		{
			hSession = WinHttpOpen("WinHttpPostSample", WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_NAMED_PROXY,
				strProxyServer, "<local>", 0);
		}
		else
		{
			hSession = WinHttpOpen("WinHttpPostSample", WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
				default, default, 0);
		}

		if (hSession.IsInvalid)
			goto done;

		//
		// Open a connection to the target server
		//
		hConnect = WinHttpConnect(hSession, strTargetServer, urlServerComponents.nPort, 0);

		if (hConnect.IsInvalid)
			goto done;

		//
		// Open the request
		//
		hRequest = WinHttpOpenRequest(hConnect, "POST", strTargetPath, default, default, default,
			urlServerComponents.nScheme == INTERNET_SCHEME.INTERNET_SCHEME_HTTPS ? WINHTTP_OPENREQ_FLAG.WINHTTP_FLAG_SECURE : 0);

		if (hRequest is null)
			goto done;

		//
		// Send the request.
		//
		// This is done in a loop so that authentication challenges can be handled.
		//
		bool bDone;
		uint dwLastStatusCode = 0;
		bDone = false;
		while (!bDone)
		{
			// If a proxy auth challenge was responded to, reset those credentials
			//before each SendRequest. This is done because after responding to a 401
			//or perhaps a redirect the proxy may require re-authentication. You
			//could get into a 407,401,407,401,etc loop otherwise.
			if (dwProxyAuthScheme != 0)
			{
				if (!WinHttpSetCredentials(hRequest, WINHTTP_AUTH_TARGET.WINHTTP_AUTH_TARGET_PROXY,
					dwProxyAuthScheme, strProxyUsername, strProxyPassword, default))
				{
					goto done;
				}
			}
			// Send a request.

			if (!WinHttpSendRequest(hRequest, WINHTTP_NO_ADDITIONAL_HEADERS, 0,
				default, 0, dwFileSize, default))
			{
				goto done;
			}

			//
			// Now we send the contents of the file. We may have to redo this
			//after an auth challenge, and so we will reset the file position
			//to the beginning on each loop.
			//
			if (INVALID_SET_FILE_POINTER == SetFilePointer(hFile, 0, default, System.IO.SeekOrigin.Begin))
				goto done;

			// Load the file 4k at a time and write it. fwFileLeft will track
			//how much more needs to be written.
			uint dwFileLeft = dwFileSize;
			while (dwFileLeft > 0)
			{
				using var buffer = new SafeCoTaskMemHandle(4096);

				if (!ReadFile(hFile, buffer, buffer.Size, out var dwBytesRead, default))
					goto done;

				if (dwBytesRead == 0)
				{
					dwFileLeft = 0;
					continue;
				}
				else if (dwBytesRead > dwFileLeft)
				{
					// unexpectedly read more from the file than we expected to find.. bail out
					goto done;
				}
				else
					dwFileLeft -= dwBytesRead;

				if (!WinHttpWriteData(hRequest, buffer, dwBytesRead, out dwBytesRead))
					goto done;
			}

			// End the request.
			if (!WinHttpReceiveResponse(hRequest, default))
			{
				// There is a special error we can get here indicating we need to try again
				if (GetLastError() == Win32Error.ERROR_WINHTTP_RESEND_REQUEST)
					continue;
				else
					goto done;
			}

			// Check the status code.
			try { dwStatusCode = WinHttpQueryHeaders<uint>(hRequest, WINHTTP_QUERY.WINHTTP_QUERY_STATUS_CODE| WINHTTP_QUERY.WINHTTP_QUERY_FLAG_NUMBER); }
			catch { goto done; }

			WINHTTP_AUTH_SCHEME dwSupportedSchemes, dwSelectedScheme;
			WINHTTP_AUTH_TARGET dwTarget;
			switch (dwStatusCode)
			{
				case 200:
					// The resource was successfully retrieved.
					// You could use WinHttpReadData to read the contents of the server's response.
					Console.Write("\nThe POST was successfully completed.");
					bDone = true;
					break;
				case 401:
					// The server requires authentication.
					Console.Write("\nThe server requires authentication. Sending credentials...");

					// Obtain the supported and preferred schemes.
					if (!WinHttpQueryAuthSchemes(hRequest, out dwSupportedSchemes, out _, out dwTarget))
						goto done;

					// Set the credentials before resending the request.
					dwSelectedScheme = ChooseAuthScheme(hRequest, dwSupportedSchemes);

					if (dwSelectedScheme == 0)
					{
						bDone = true;
					}
					else
					{
						if (!WinHttpSetCredentials(hRequest, dwTarget, dwSelectedScheme,
							strTargetUsername, strTargetPassword, default))
						{
							goto done;
						}
					}

					// If the same credentials are requested twice, abort the
					// request. For simplicity, this sample does not check for
					// a repeated sequence of status codes.
					if (dwLastStatusCode==401)
					{
						Console.Write("\nServer Authentication failed.");
						bDone = true;
					}

					break;
				case 407:
					// The proxy requires authentication.
					Console.Write("\nThe proxy requires authentication. Sending credentials...");

					// Obtain the supported and preferred schemes.
					if (!WinHttpQueryAuthSchemes(hRequest, out dwSupportedSchemes, out _, out _))
						goto done;

					// Set the credentials before resending the request.
					dwProxyAuthScheme = ChooseAuthScheme(hRequest, dwSupportedSchemes);

					// If the same credentials are requested twice, abort the
					// request. For simplicity, this sample does not check for
					// a repeated sequence of status codes.
					if (dwLastStatusCode==407)
					{
						Console.Write("\nProxy Authentication failed.");
						bDone = true;
					}
					break;

				default:
					// The status code does not indicate success.
					Console.Write("\nStatus code {0} returned.\n", dwStatusCode);
					bDone = true;
					break;
			}

			// Keep track of the last status code.
			dwLastStatusCode = dwStatusCode;
		}

		returnValue = true;

done:
		var dwTemp = GetLastError();

		hRequest?.Dispose();

		hConnect?.Dispose();

		hSession?.Dispose();

		hFile?.Dispose();

		SetLastError((uint)dwTemp);

		return returnValue;
	}
}