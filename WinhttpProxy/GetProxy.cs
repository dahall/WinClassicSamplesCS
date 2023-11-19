internal class ProxyResolver
{
	// m_dwError - WIN32 Error codes returned from call back function. It's used by extended APIs.
	private Win32Error m_dwError;

	// m_dwProxyCursor - The current location in the m_wprProxyResult proxy list. Activated when WinHttpGetProxyForUrlEx is used.
	private uint m_dwProxyCursor;

	// m_fExtendedAPI - Indicates whether extended APIs are used.
	private static readonly bool m_fExtendedAPI = Environment.OSVersion.Version >= new Version(6, 2); // Windows 8

	// m_fInit - The proxy has been resolved.
	private bool m_fInit;

	// m_fProxyFailOverValid - Indicates whether it is valid to iterate through the proxy list for proxy failover. Proxy failover is valid
	// for a list of proxies returned by executing a proxy script. This occurs in auto-detect and auto-config URL proxy detection modes. When
	// static proxy settings are used fallback is not allowed.
	private bool m_fProxyFailOverValid;

	// m_fReturnedFirstProxy - The first proxy in the list has been returned to the application.
	private bool m_fReturnedFirstProxy;

	// m_fReturnedLastProxy - The end of the proxy list was reached.
	private bool m_fReturnedLastProxy;

	// m_hEvent - The handle to the event object. It's used after calling WinHttpGetProxyForUrlEx to wait for proxy results.
	private ManualResetEvent? m_hEvent;

	// m_pwszProxyList - The current location in the m_wpiProxyInfo proxy list. Activated when WinHttpGetProxyForUrl is used.
	private Queue<string>? m_pwszProxyList;

	// m_wpiProxyInfo - The initial proxy and bypass list returned by calls to WinHttpGetIEProxyConfigForCurrentUser and WinHttpGetProxyForUrl.
	private WINHTTP_PROXY_INFO_IN m_wpiProxyInfo;

	// m_wprProxyResult - The structure introduced for extended APIs. It contains well-structured proxy results. Used when 1) Auto-Detect if configured.
	// 2) Auto-Config URL if configured.
	private WINHTTP_PROXY_RESULT m_wprProxyResult;

	public ProxyResolver() => m_dwError = Win32Error.ERROR_SUCCESS;

	~ProxyResolver()
	{
		if (m_fExtendedAPI)
		{
			//
			// When extended APIs are used, m_wprProxyResult will be freed by using
			// the new API WinHttpFreeProxyResult.
			//

			WinHttpFreeProxyResult(ref m_wprProxyResult);
		}

		m_hEvent?.Dispose();
	}

	/*++

	Routine Description:

		Resets the proxy cursor for reuse starting at the beginning of the list.

	Arguments:

	Return Value:

		None.

	--*/
	public void ResetProxyCursor()
	{
		m_fReturnedFirstProxy = false;
		m_fReturnedLastProxy = false;
		m_pwszProxyList?.Clear();
		m_dwProxyCursor = 0;
	}

	/*++

	Routine Description:

		Uses the users IE settings to get the proxy for the URL.

	Arguments:

		pwszUrl - The URL to get the proxy for.

		hSession - The session to use for the proxy resolution.

	Return Value:

		WIN32 Error codes.

	--*/
	public void ResolveProxy([In] HINTERNET hSession, string pwszUrl)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		WINHTTP_CURRENT_USER_IE_PROXY_CONFIG ProxyConfig = default;

		if (m_fInit)
		{
			dwError = Win32Error.ERROR_INVALID_OPERATION;
			goto quit;
		}

		if (!WinHttpGetIEProxyConfigForCurrentUser(out ProxyConfig))
		{
			dwError = Win32Error.GetLastError();
			if (dwError != Win32Error.ERROR_FILE_NOT_FOUND)
			{
				goto quit;
			}

			//
			// No IE proxy settings found, just do autodetect.
			//

			ProxyConfig.fAutoDetect = true;
			dwError = Win32Error.ERROR_SUCCESS;
		}

		string? pwszProxy;
		string? pwszProxyBypass;
		bool fFailOverValid;
		//
		// Begin processing the proxy settings in the following order:
		// 1) Auto-Detect if configured.
		// 2) Auto-Config URL if configured.
		// 3) Static Proxy Settings if configured.
		//
		// Once any of these methods succeed in finding a proxy we are finished.
		// In the event one mechanism fails with an expected error code it is
		// required to fall back to the next mechanism. If the request fails
		// after exhausting all detected proxies, there should be no attempt
		// to discover additional proxies.
		//

		if (ProxyConfig.fAutoDetect)
		{
			fFailOverValid = true;

			//
			// Detect Proxy Settings.
			//

			dwError = GetProxyForAutoSettings(hSession,
											 pwszUrl,
											 default,
											 out pwszProxy,
											 out pwszProxyBypass);

			if (dwError == Win32Error.ERROR_SUCCESS)
			{
				goto commit;
			}

			if (!IsRecoverableAutoProxyError(dwError))
			{
				goto quit;
			}

			//
			// Fall back to Autoconfig URL or Static settings. Application can
			// optionally take some action such as logging, or creating a mechanism
			// to expose multiple error codes in the class.
			//

			dwError = Win32Error.ERROR_SUCCESS;
		}

		if (!ProxyConfig.lpszAutoConfigUrl.IsNull)
		{
			fFailOverValid = true;

			//
			// Run autoproxy with AutoConfig URL.
			//

			dwError = GetProxyForAutoSettings(hSession,
											 pwszUrl,
											 ProxyConfig.lpszAutoConfigUrl,
											 out pwszProxy,
											 out pwszProxyBypass);
			if (dwError == Win32Error.ERROR_SUCCESS)
			{
				goto commit;
			}

			if (!IsRecoverableAutoProxyError(dwError))
			{
				goto quit;
			}

			//
			// Fall back to Static Settings. Application can optionally take some
			// action such as logging, or creating a mechanism to to expose multiple
			// error codes in the class.
			//

			dwError = Win32Error.ERROR_SUCCESS;
		}

		fFailOverValid = false;

		//
		// Static Proxy Config. Failover is not valid for static proxy since
		// it is always either a single proxy or a list containing protocol
		// specific proxies such as "proxy" or http=httpproxy;https=sslproxy
		//

		pwszProxy = ProxyConfig.lpszProxy;

		pwszProxyBypass = ProxyConfig.lpszProxyBypass;

commit:

		m_fProxyFailOverValid = fFailOverValid;

		m_wpiProxyInfo.dwAccessType = pwszProxy is null ? WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_NO_PROXY : WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_NAMED_PROXY;

		m_wpiProxyInfo.lpszProxy = pwszProxy!;

		m_wpiProxyInfo.lpszProxyBypass = pwszProxyBypass!;

		m_fInit = true;

quit:

		ProxyConfig.FreeMemory();

		Win32Error.ThrowIfFailed(dwError);
	}

	/*++

	Routine Description:

		Finds the next proxy in a list of proxies separated by whitespace and/or
		semicolons if proxy failover is supported. It is not safe to use this
		function concurrently, implement a concurrency mechanism for proxy lists
		if needed, such as making a copy or a separate iterator.

		Each sequential request to the same URL should use ResetProxyCursor
		before the first call for proxy settings during a single request.

	Arguments:

		hInternet - The Session or Request handle to set the proxy info on.

		dwRequestError - The Win32 error code from WinHttpSendRequest (Sync) or from
						 WINHTTP_CALLBACK_STATUS_REQUEST_ERROR (Async) or
						 Win32Error.ERROR_SUCCESS if this is the first usage.

	Return Value:

		Win32Error.ERROR_SUCCESS - Found the next proxy and it has been set on the HINTERNET.

		Win32Error.ERROR_NO_MORE_ITEMS - Reached the end of the list or failover not valid.

		Win32Error.ERROR_INVALID_OPERATION - The class is not initialized. Call ResolveProxy first.

		Other Win32 Errors returned from WinHttpSetOption.

	--*/
	public Win32Error SetNextProxySetting([In] HINTERNET hInternet, [In] Win32Error dwRequestError)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		string? pwszCursor = null;

		if (!m_fInit)
		{
			dwError = Win32Error.ERROR_INVALID_OPERATION;
			goto quit;
		}

		if (m_fExtendedAPI)
		{
			dwError = SetNextProxySettingEx(hInternet, dwRequestError);
			goto quit;
		}

		if (!m_fReturnedFirstProxy)
		{
			//
			// We have yet to set the first proxy type, the first one is always
			// valid.
			//

			var proxyArray = m_wpiProxyInfo.lpszProxy?.Split(new[] { ' ', ';', '\t', '\n', '\v', '\f', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			m_pwszProxyList = (proxyArray?.Length ?? 0) > 0 ? new Queue<string>(proxyArray!) : new Queue<string>();
			m_fReturnedFirstProxy = true;
			goto commit;
		}

		//
		// Find the next proxy in the list if it is valid to do so.
		//

		if (m_fReturnedLastProxy || !m_fProxyFailOverValid || m_wpiProxyInfo.lpszProxy is null)
		{
			//
			// Already reached end, failover not valid, or type is not proxy.
			//

			dwError = Win32Error.ERROR_NO_MORE_ITEMS;
			goto quit;
		}

		if (!IsErrorValidForProxyFailover(dwRequestError))
		{
			dwError = Win32Error.ERROR_NO_MORE_ITEMS;
			goto quit;
		}

		if (m_pwszProxyList is null || !m_pwszProxyList.TryDequeue(out pwszCursor))
		{
			//
			// Hit the end of the list.
			//

			m_fReturnedLastProxy = true;
			dwError = Win32Error.ERROR_NO_MORE_ITEMS;
			goto quit;
		}

commit:
		WINHTTP_PROXY_INFO_IN NextProxyInfo = new()
		{
			dwAccessType = m_wpiProxyInfo.dwAccessType,
			lpszProxy = pwszCursor!,
			lpszProxyBypass = m_wpiProxyInfo.lpszProxyBypass
		};

		Win32Error.ThrowLastErrorIfFalse(WinHttpSetOption(hInternet, WINHTTP_OPTION.WINHTTP_OPTION_PROXY, NextProxyInfo));

quit:

		return dwError;
	}

	/*++

	Routine Description:

		Fetch proxy query results asynchronizely. This application shows how to cope
		with new APIs. In multithreaded environment, developers must keep in mind resource
		contention.

	Arguments:

		hSession - The WinHttp session to use for the proxy resolution.

		dwContext - The context value supplied by this application to associate with
					the callback handle hSession.

		dwInternetStatus - The INTERNET_STATUS_ value which specifies the status code
						 that indicates why the callback function is called.

		pvStatusInformation - A pointer to a buffer that specifies information
							 pertinent to this call to the callback function.

		dwStatusInformationLength - A value of type uint integer that
									specifies the size of the lpvStatusInformation buffer.

	Return Value:

		None.

	--*/
	private void GetProxyCallBack([In] HINTERNET hResolver, [In] IntPtr dwContext, [In] WINHTTP_CALLBACK_STATUS dwInternetStatus, [In] IntPtr pvStatusInformation, [In] uint dwStatusInformationLength)
	{
		if (dwInternetStatus is not WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE and
			not WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR)
		{
			goto quit;
		}

		if (dwInternetStatus == WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR)
		{
			unsafe
			{
				WINHTTP_ASYNC_RESULT* pAsyncResult = (WINHTTP_ASYNC_RESULT*)pvStatusInformation;

				if (pAsyncResult->dwResult != ASYNC_RESULT.API_GET_PROXY_FOR_URL)
				{
					goto quit;
				}

				m_dwError = pAsyncResult->dwError;
			}
		}
		else if (dwInternetStatus == WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_GETPROXYFORURL_COMPLETE)
		{
			m_dwError = WinHttpGetProxyResult(hResolver, out m_wprProxyResult);
		}

		m_hEvent?.Set();

quit:
		return;
	}

	/*++

	Routine Description:

		Uses Auto detection or AutoConfigURL to run WinHttpGetProxyForUrl.

		Additionally provides autologon by calling once without autologon, which is
		most performant, and then with autologon if logon fails.

	Arguments:

		hSession - The WinHttp session to use for the proxy resolution.

		pwszUrl - The URL to get the proxy for.

		pwszAutoConfig - The autoconfig URL or default for Autodetection.

		ppwszProxy - Upon success, the proxy string found for pwszUrl or default if
					 no proxy should be used for this URL.
					 Use GlobalFree to free.

		ppwszProxyBypass - Upon success, the proxy bypass string found for pwszUrl
						 or default if there is no proxy bypass for the
						 configuration type.
						 Use GlobalFree to free.
	Return Value:

		WIN32 Error codes. The caller should use IsRecoverableAutoProxyError to
			decide whether execution can continue.

	--*/
	private Win32Error GetProxyForAutoSettings([In] HINTERNET hSession, string pwszUrl, [Optional] string? pwszAutoConfigUrl, out string? ppwszProxy, out string? ppwszProxyBypass)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		WINHTTP_AUTOPROXY_OPTIONS waoOptions = default;
		WINHTTP_PROXY_INFO wpiProxyInfo = default;

		ppwszProxy = ppwszProxyBypass = default;

		if (pwszAutoConfigUrl is not null)
		{
			waoOptions.dwFlags = WINHTTP_AUTOPROXY.WINHTTP_AUTOPROXY_CONFIG_URL;
			waoOptions.lpszAutoConfigUrl = pwszAutoConfigUrl;
		}
		else
		{
			waoOptions.dwFlags = WINHTTP_AUTOPROXY.WINHTTP_AUTOPROXY_AUTO_DETECT;
			waoOptions.dwAutoDetectFlags = WINHTTP_AUTO_DETECT_TYPE.WINHTTP_AUTO_DETECT_TYPE_DHCP | WINHTTP_AUTO_DETECT_TYPE.WINHTTP_AUTO_DETECT_TYPE_DNS_A;
		}

		//
		// First call with no autologon. Autologon prevents the
		// session (in proc) or autoproxy service (out of proc) from caching
		// the proxy script. This causes repetitive network traffic, so it is
		// best not to do autologon unless it is required according to the
		// result of WinHttpGetProxyForUrl.
		// This applies to both WinHttpGetProxyForUrl and WinhttpGetProxyForUrlEx.
		//

		if (m_fExtendedAPI)
		{
			m_hEvent = new(false);

			dwError = GetProxyForUrlEx(hSession, pwszUrl, waoOptions);

			if (dwError != Win32Error.ERROR_WINHTTP_LOGIN_FAILURE)
			{
				//
				// Unless we need to retry with auto-logon exit the function with the
				// result, on success the proxy list will be stored in m_wprProxyResult
				// by GetProxyCallBack.
				//

				goto quit;
			}

			//
			// Enable autologon if challenged.
			//

			waoOptions.fAutoLogonIfChallenged = true;
			dwError = GetProxyForUrlEx(hSession, pwszUrl, waoOptions);

			goto quit;
		}

		if (!WinHttpGetProxyForUrl(hSession, pwszUrl, waoOptions, out wpiProxyInfo))
		{
			dwError = Win32Error.GetLastError();

			if (dwError != Win32Error.ERROR_WINHTTP_LOGIN_FAILURE)
			{
				goto quit;
			}

			//
			// Enable autologon if challenged.
			//

			dwError = Win32Error.ERROR_SUCCESS;
			waoOptions.fAutoLogonIfChallenged = true;
			if (!WinHttpGetProxyForUrl(hSession, pwszUrl, waoOptions, out wpiProxyInfo))
			{
				dwError = Win32Error.GetLastError();
				goto quit;
			}
		}

		ppwszProxy = wpiProxyInfo.lpszProxy;

		ppwszProxyBypass = wpiProxyInfo.lpszProxyBypass;

quit:

		wpiProxyInfo.FreeMemory();

		return dwError;
	}

	/*++

	Routine Description:

		Retrieves the proxy data with the specified option using WinhttpGetProxyForUrlEx.

	Arguments:

		hSession - The WinHttp session to use for the proxy resolution.

		pwszUrl - The URL to get the proxy for.

		pAutoProxyOptions - Specifies the auto-proxy options to use.

	Return Value:

		WIN32 Error codes.

	--*/
	private Win32Error GetProxyForUrlEx([In] HINTERNET hSession, string pwszUrl, in WINHTTP_AUTOPROXY_OPTIONS pAutoProxyOptions)
	{
		//
		// Create proxy resolver handle. It's best to close the handle during call back.
		//

		var dwError = WinHttpCreateProxyResolver(hSession, out var hResolver);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto quit;
		}

		//
		// Sets up a callback function that WinHTTP can call as proxy results are resolved.
		//

		var wscCallback = WinHttpSetStatusCallback(hResolver, GetProxyCallBack, WINHTTP_CALLBACK_FLAG.WINHTTP_CALLBACK_FLAG_REQUEST_ERROR | WINHTTP_CALLBACK_FLAG.WINHTTP_CALLBACK_FLAG_GETPROXYFORURL_COMPLETE);
		if (wscCallback == WINHTTP_INVALID_STATUS_CALLBACK)
		{
			dwError = Win32Error.GetLastError();
			goto quit;
		}

		//
		// The extended API works in asynchronous mode, therefore wait until the
		// results are set in the call back function.
		//

		dwError = WinHttpGetProxyForUrlEx(hResolver, pwszUrl, pAutoProxyOptions);
		if (dwError != Win32Error.ERROR_IO_PENDING)
		{
			goto quit;
		}

		//
		// The resolver handle will get closed in the callback and cannot be used any longer.
		//

		hResolver = default;

		if (!m_hEvent!.WaitOne())
		{
			dwError = Win32Error.GetLastError();
			goto quit;
		}

		dwError = m_dwError;

quit:

		return dwError;
	}

	/*++

	Routine Description:

		Determines whether the result of WinHttpSendRequest (Sync) or the error
		from WINHTTP_CALLBACK_STATUS_REQUEST_ERROR (Async) can assume a possible
		proxy error and fallback to the next proxy.

	Arguments:

		dwError - The Win32 error code from WinHttpSendRequest (Sync) or from
				 WINHTTP_CALLBACK_STATUS_REQUEST_ERROR (Async)

	Return Value:

		true - The caller should set the next proxy and resend the request.

		false - The caller should assume the request has failed.

	--*/
	private static bool IsErrorValidForProxyFailover([In] Win32Error dwError) => (uint)dwError switch
	{
		Win32Error.ERROR_WINHTTP_NAME_NOT_RESOLVED or Win32Error.ERROR_WINHTTP_CANNOT_CONNECT or Win32Error.ERROR_WINHTTP_CONNECTION_ERROR or Win32Error.ERROR_WINHTTP_TIMEOUT => true,
		_ => false,
	};

	/*++

	Routine Description:

		Determines whether the result of WinHttpGetProxyForUrl is recoverable,
		allowing the caller to fall back to other proxy mechanisms.

	Arguments:

		dwError - The Win32 error code returned by GetLastError on
				 WinHttpGetProxyForUrl failure.

	Return Value:

		true - The caller can continue execution safely.

		false - The caller should immediately fail with dwError.

	--*/
	private static bool IsRecoverableAutoProxyError([In] Win32Error dwError) => (uint)dwError switch
	{
		Win32Error.ERROR_SUCCESS or Win32Error.ERROR_INVALID_PARAMETER or Win32Error.ERROR_WINHTTP_AUTO_PROXY_SERVICE_ERROR or Win32Error.ERROR_WINHTTP_AUTODETECTION_FAILED or Win32Error.ERROR_WINHTTP_BAD_AUTO_PROXY_SCRIPT or Win32Error.ERROR_WINHTTP_LOGIN_FAILURE or Win32Error.ERROR_WINHTTP_OPERATION_CANCELLED or Win32Error.ERROR_WINHTTP_TIMEOUT or Win32Error.ERROR_WINHTTP_UNABLE_TO_DOWNLOAD_SCRIPT or Win32Error.ERROR_WINHTTP_UNRECOGNIZED_SCHEME => true,
		_ => false,
	};

	/*++

	Routine Description:

	 Determines if a wide character is a whitespace character.

	Arguments:

		wcChar - The character to check for whitespace.

	Return Value:

		true if the character is whitespace. false otherwise.

	--*/
	private static bool IsWhitespace([In] char wcChar) => char.IsWhiteSpace(wcChar);

	/*++

	Routine Description:

		Finds the next proxy from m_wprProxyResult queried from extended API.
		It is not safe to use this function concurrently.

		Each sequential request to the same URL should use ResetProxyCursor
		before the first call for proxy settings during a single request.

	Arguments:

		hInternet - The Session or Request handle to set the proxy info on.

		dwRequestError - The Win32 error code from WinHttpSendRequest (Sync) or from
						 WINHTTP_CALLBACK_STATUS_REQUEST_ERROR (Async) or
						 Win32Error.ERROR_SUCCESS if this is the first usage.
	Return Value:

		Win32 Errors Codes.

	--*/
	private Win32Error SetNextProxySettingEx([In] HINTERNET hInternet, [In] Win32Error dwRequestError)
	{
		uint dwError = Win32Error.ERROR_SUCCESS;

		//
		// Use static proxy settings if it's activated.
		//

		if (!m_fProxyFailOverValid)
		{
			if (m_fReturnedFirstProxy)
			{
				dwError = Win32Error.ERROR_NO_MORE_ITEMS;
				goto quit;
			}

			m_fReturnedFirstProxy = true;

			Win32Error.ThrowLastErrorIfFalse(WinHttpSetOption(hInternet, WINHTTP_OPTION.WINHTTP_OPTION_PROXY, m_wpiProxyInfo));

			goto quit;
		}

		if (m_dwProxyCursor >= m_wprProxyResult.cEntries)
		{
			dwError = Win32Error.ERROR_NO_MORE_ITEMS;
			goto quit;
		}

		//
		// The first proxy is always valid. Only check request errors after first run.
		//

		if (m_dwProxyCursor != 0 && !IsErrorValidForProxyFailover(dwRequestError))
		{
			dwError = Win32Error.ERROR_NO_MORE_ITEMS;
			goto quit;
		}

		Win32Error.ThrowLastErrorIfFalse(WinHttpSetOption(hInternet, WINHTTP_OPTION.WINHTTP_OPTION_PROXY_RESULT_ENTRY, m_wprProxyResult.Entries[m_dwProxyCursor]));

		m_dwProxyCursor++;

quit:

		return dwError;
	}
}