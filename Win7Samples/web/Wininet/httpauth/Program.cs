using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.WinINet;

namespace httpauth;

internal class Program
{
	static bool NeedAuth(HINTERNET hRequest)
	{

		// Get status code.
		var dwStatus = HttpQueryInfo<HTTP_STATUS>(hRequest, HTTP_QUERY.HTTP_QUERY_FLAG_NUMBER | HTTP_QUERY.HTTP_QUERY_STATUS_CODE);
		Console.Error.Write("Status: %d\n", dwStatus);

		// Look for 401 or 407.
		bool fRet = false;
		HTTP_QUERY dwFlags = 0;
		switch (dwStatus)
		{
			case HTTP_STATUS.HTTP_STATUS_DENIED:
				dwFlags = HTTP_QUERY.HTTP_QUERY_WWW_AUTHENTICATE;
				break;

			case HTTP_STATUS.HTTP_STATUS_PROXY_AUTH_REQ:
				dwFlags = HTTP_QUERY.HTTP_QUERY_PROXY_AUTHENTICATE;
				break;

			default:
				fRet = false;
				goto Done;
		}

		// Enumerate the authentication types.
		uint dwIndex = 0;
		do
		{
			using SafeCoTaskMemString szScheme = new(64, CharSet.Ansi);
			uint cbScheme = szScheme.Size;
			fRet = HttpQueryInfo(hRequest, dwFlags, szScheme, ref cbScheme, ref dwIndex);
			if (!fRet)
			{
				Console.Write("Found auth scheme: {0}\n", szScheme);
			}
		} while (fRet);

Done:
		return fRet;
	}


	//==============================================================================
	static Win32Error DoCustomUI(HINTERNET hConnect, HINTERNET hRequest)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		var rgbBuf = new byte[1024];
		int cbBuf = rgbBuf.Length;

		// Prompt for username and password.

		Console.Write("Enter Username: ");
		var szUser = Console.ReadLine();

		Console.Write("Enter Password: ");
		var szPass = Console.ReadLine();

		// Set the values in the handle.
		InternetSetOption(hConnect, InternetOptionFlags.INTERNET_OPTION_USERNAME, szUser);
		InternetSetOption(hConnect, InternetOptionFlags.INTERNET_OPTION_PASSWORD, szPass);

		// Drain the socket.
		int cbRead = 0;
		while (InternetReadFile(hRequest, rgbBuf, cbBuf, out cbRead) && cbRead != 0)
		{
		}

		return Win32Error.ERROR_INTERNET_FORCE_RETRY;
	}

	//==============================================================================
	public static void Main(string[] args)
	{
		bool fRet = false;
		string pszErr = default;
		bool fAllowCustomUI = false;
		string pszHost = default;
		string pszObject = default;
		string pszUser = default;
		string pszPass = default;

		// Check usage.
		if (args.Length < 1)
		{
			Console.Write("Usage: httpauth [-c] <server> [<object> [<user> [<pass>]]]\n");
			Console.Write(" -c: Use custom UI to prompt for user/pass");
			Environment.Exit(1);
		}

		// Parse arguments.
		fAllowCustomUI = args.Any(a => a == "-c");

		pszHost = args[0];

		if (args.Length >= 2)
		{
			pszObject = args[1];
		}
		else
		{
			pszObject = "/";
		}

		if (args.Length >= 3)
		{
			pszUser = args[2];
		}

		if (args.Length >= 4)
		{
			pszPass = args[3];
		}

		// Initialize wininet.
		using var hInternet = Win32Error.ThrowLastErrorIfInvalid(InternetOpen("HttpAuth Sample", InternetOpenType.INTERNET_OPEN_TYPE_PRECONFIG));

		// Connect to host.
		using var hConnect = Win32Error.ThrowLastErrorIfInvalid(InternetConnect(hInternet, // wininet handle,
			pszHost, // host
			0, // port
			pszUser, // user
			default, // pass
			InternetService.INTERNET_SERVICE_HTTP, // service
			0, // flags
			default)); // context

		if (pszPass is not null)
		{
			// Work around InternetConnect disallowing empty passwords.
			InternetSetOption(hConnect, InternetOptionFlags. INTERNET_OPTION_PASSWORD, pszPass);
		}

		// Create request.
		INTERNET_FLAG dwFlags = INTERNET_FLAG.INTERNET_FLAG_KEEP_CONNECTION | INTERNET_FLAG.INTERNET_FLAG_RELOAD;
		using var hRequest = Win32Error.ThrowLastErrorIfInvalid(HttpOpenRequest(hConnect, // connect handle
			"GET", // request method
			pszObject, // object name
			default, // version
			default, // referrer
			default, // accept types
			dwFlags, // flags: keep-alive, bypass cache
			default)); // context

		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		do
		{
			// Send request.
			fRet = HttpSendRequest(hRequest, // request handle
				"", // header string
				0, // header length
				default, // post data
				0); // post length

			// Handle any authentication dialogs.
			if (NeedAuth(hRequest) && fAllowCustomUI)
			{
				dwError = DoCustomUI(hConnect, hRequest);
			}
			else
			{
				var dwUIFlags = FLAGS_ERROR_UI.FLAGS_ERROR_UI_FILTER_FOR_ERRORS |
					FLAGS_ERROR_UI.FLAGS_ERROR_UI_FLAGS_CHANGE_OPTIONS |
					FLAGS_ERROR_UI.FLAGS_ERROR_UI_FLAGS_GENERATE_DATA;

				dwError = InternetErrorDlg(GetDesktopWindow(),
					hRequest,
					fRet ? Win32Error.ERROR_SUCCESS : Win32Error.GetLastError(),
					dwUIFlags,
					default);
			}
		} while (dwError == Win32Error.ERROR_INTERNET_FORCE_RETRY);

		SetLastError((uint)dwError);
		dwError.ThrowIfFailed("Authentication");

		// Dump some bytes.
		byte[] rgbBuf = new byte[1024];
		while (InternetReadFile(hRequest, rgbBuf, rgbBuf.Length, out var cbRead))
		{
			if (cbRead == 0)
			{
				break;
			}

			Console.Write(string.Join(' ', rgbBuf.Take((int)cbRead).Select(b => $"{b:X}")));
		}

		if (pszErr is not null)
		{
			Console.Error.Write("Failed on {0}, last error {1}\n", pszErr, GetLastError());
		}
	}
}