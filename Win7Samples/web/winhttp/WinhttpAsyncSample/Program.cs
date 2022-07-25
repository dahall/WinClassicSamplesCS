using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;

using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WinHTTP;

namespace WinhttpAsyncSample;

internal class Program
{
	private static readonly List<MYCONTEXT> contexts = new();
	private static SafeHINTERNET g_hConnect = default;
	private static SafeHINTERNET g_hSession = default;

	/*++
	Routine Description:
	main
	Arguments:
	argc -
	argv -
	Return Value:
	Win32.
	--*/
	public static int Main(string[] args)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		if (args.Length != 1)
		{
			Console.Write("Usage: WinhttpAsyncSample.exe <Server>\n");
			goto Exit;
		}

		dwError = InitializeGlobals(args[0]);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto Exit;
		}

		// Kick off a regular request.

		dwError = BeginRequest("/", out MYCONTEXT pRegularContext);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto Exit;
		}

		// Kick off a request we actually going to always cancel (for demonstration purposes).

		dwError = BeginRequest("/cancel", out MYCONTEXT pCancelContext);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto Exit;
		}

		dwError = DemoCancel(pCancelContext);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto Exit;
		}

		// Wait for the regular request to complete normally.

		dwError = EndRequest(pRegularContext);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto Exit;
		}

		Console.Write("pRegularContext completed successfully\n");

		// Wait for the cancel request to complete normally.

		dwError = EndRequest(pCancelContext);
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			if (dwError == Win32Error.ERROR_OPERATION_ABORTED || dwError == Win32Error.ERROR_WINHTTP_OPERATION_CANCELLED)
			{
				Console.Write("DemoCancelThreadFunc won the race and cancelled pCancelContext\n");
			}

			goto Exit;
		}

		Console.Write("pCancelContext completed successfully\n");

Exit:

		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("dwError={0}\n", dwError);
		}

		CleanupGlobals();

		return (int)(uint)dwError;
	}

	private static void AsyncCallback(HINTERNET hInternet, IntPtr dwContext, WINHTTP_CALLBACK_STATUS dwInternetStatus, IntPtr lpvStatusInformation, uint dwStatusInformationLength)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		var fLocked = false;
		var fReleaseContext = false;

		var pContext = MYCONTEXT.FromPtr(dwContext);

		if (pContext is null)
		{
			// No context, nothing to do.

			goto Exit;
		}

		if (dwInternetStatus is not WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING and not WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR)
		{
			// If we're going to try use the request handle then we'd better lock it.

			dwError = pContext.LockRequestHandle();
			if (dwError != Win32Error.ERROR_SUCCESS)
			{
				goto Exit;
			}
			fLocked = true;
		}

		switch (dwInternetStatus)
		{
			case WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE:
				Console.Write("AsyncCallback: WINHTTP_CALLBACK_STATUS_SENDREQUEST_COMPLETE\n");

				if (!WinHttpReceiveResponse(pContext.RequestHandle, default))
				{
					dwError = GetLastError();
					Console.Write("AsyncCallback: WinHttpReceiveResponse failed\n");
					goto Exit;
				}
				break;

			case WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_HEADERS_AVAILABLE:
				dwError = OnHeadersAvailable(pContext);
				if (dwError != Win32Error.ERROR_SUCCESS)
				{
					goto Exit;
				}
				break;

			case WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_READ_COMPLETE:
				dwError = OnReadComplete(pContext, dwStatusInformationLength);
				if (dwError != Win32Error.ERROR_SUCCESS)
				{
					goto Exit;
				}
				break;

			case WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING:

				// Garanteed last callback this context will ever receive. Release context when we're done on behalf of all callbacks.
				// (Balances the reference we took when we called WinHttpSendRequest)

				fReleaseContext = true;
				break;

			case WINHTTP_CALLBACK_STATUS.WINHTTP_CALLBACK_STATUS_REQUEST_ERROR:
				Console.Write("AsyncCallback: WINHTTP_CALLBACK_STATUS_REQUEST_ERROR\n");
				dwError = lpvStatusInformation.AsRef<WINHTTP_ASYNC_RESULT>(0, dwStatusInformationLength).dwError;
				goto Exit;
		}

Exit:

		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("AsyncCallback: dwError = {0}\n", dwError);
			if (pContext is not null)
			{
				pContext.LastError = dwError;
				_=SetEvent(pContext.RequestFinishedEvent);
			}
		}

		if (fLocked)
		{
			pContext.UnlockRequestHandle();
		}

		if (fReleaseContext)
		{
			pContext.Release();
		}
	}

	/*++
	Routine Description:
	Creates and begins a request.
	Arguments:
	pwszPath - Supplies the abs_path to use.
	ppContext - Returns a context, caller should use it as follows:
	1. At least one of EndRequest and CancelRequest.
	2. Release
	Return Value:
	Win32.
	--*/
	private static Win32Error BeginRequest(string pwszPath, out MYCONTEXT ppContext)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;
		var fLocked = false;
		var pwszAcceptTypes = new[] { "*/*", default };
		MYCONTEXT pContext = default;

		ppContext = default;

		SafeHINTERNET hRequest = WinHttpOpenRequest(g_hConnect,
			"GET",
			pwszPath,
			default, // version
			default, // referrer
			pwszAcceptTypes,
			0);
		if (hRequest.IsInvalid)
		{
			dwError = GetLastError();
			goto Exit;
		}

		try
		{
			pContext = new(hRequest);

			// pContext now owns hRequest.

			hRequest = default;
		}
		catch { goto Exit; }

		dwError = pContext.LockRequestHandle();
		if (dwError != Win32Error.ERROR_SUCCESS)
		{
			goto Exit;
		}

		fLocked = true;

		// Take an extra reference for async callbacks.

		pContext.AddRef();
		contexts.Add(pContext);
		var ctxidx = contexts.Count - 1;
		if (!WinHttpSetOption(pContext.RequestHandle, WINHTTP_OPTION.WINHTTP_OPTION_CONTEXT_VALUE, (IntPtr)ctxidx, 0))
		{
			dwError = GetLastError();

			// Failed to kick off async work, so no async callbacks, so revoke that reference.

			pContext.Release();
			_=contexts.Remove(pContext);

			Console.Write("WinHttpSetOption WINHTTP_OPTION_CONTEXT_VALUE failed\n");
			goto Exit;
		}

		if (!WinHttpSendRequest(pContext.RequestHandle))
		{
			dwError = GetLastError();
			Console.Write("WinHttpSendRequest failed\n");
			goto Exit;
		}

		pContext.UnlockRequestHandle();
		fLocked = false;

		// Hand off context ownership to caller.

		ppContext = pContext;
		pContext = default;

Exit:

		if (fLocked)
		{
			pContext.UnlockRequestHandle();
		}

		pContext?.Release();

		return dwError;
	}

	/*++
	Routine Description:
	Kicks off a DemoCancelThreadFunc thread.
	Arguments:
	pContext - Request context to cancel.
	Return Value:
	None.
	--*/
	private static void CleanupGlobals()
	{
		g_hConnect?.Dispose();
		g_hSession?.Dispose();
		for (var i = contexts.Count -1; i <= 0; i++)
			contexts[i]?.Dispose();
	}

	private static Win32Error DemoCancel(MYCONTEXT pContext)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		// Take an additional reference for DemoCancelThreadFunc.

		pContext.AddRef();

		SafeHTHREAD hThread = CreateThread(default, 0, DemoCancelThreadFunc, (IntPtr)contexts.IndexOf(pContext), 0, out _);
		if (hThread.IsInvalid)
		{
			dwError = GetLastError();
			pContext.Release();
			goto Exit;
		}

Exit:

		return dwError;
	}

	/*++
	Routine Description:
	Background thread to demonstrate async cancellation.
	Arguments:
	lpParameter - Request context to cancel.
	Return Value:
	Thread exit value.
	--*/
	private static uint DemoCancelThreadFunc(IntPtr lpParameter)
	{
		var pContext = MYCONTEXT.FromPtr(lpParameter);

		if (Random.Shared.Next() % 2 == 0)
		{
			// Make the cancellation race interesting by sleeping sometimes ..

			Console.Write("DemoCancelThreadFunc sleeping..\n");
			Sleep(100);
		}
		else
		{
			Console.Write("DemoCancelThreadFunc NOT sleeping..\n");
		}

		pContext?.CancelRequest();
		pContext?.Release();
		return Win32Error.ERROR_SUCCESS;
	}

	/*++
	Routine Description:
	Waits for request to complete.
	Arguments:
	pContext - Request context to wait for.
	Return Value:
	None.
	--*/
	private static Win32Error EndRequest(MYCONTEXT pContext)
	{
		_ = Win32Error.ERROR_SUCCESS;
		Win32Error dwError;
		if (WaitForSingleObject(pContext.RequestFinishedEvent, INFINITE) == WAIT_STATUS.WAIT_FAILED)
		{
			dwError = GetLastError();
			Console.Write("WaitForSingleObject failed\n");
			goto Exit;
		}

		dwError = pContext.LastError;

Exit:

// Success or failure, we're done with this RequestHandle.

		pContext.CancelRequest();

		return dwError;
	}

	private static Win32Error InitializeGlobals(string pwszServer)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		g_hSession = WinHttpOpen("winhttp async sample/0.1", WINHTTP_ACCESS_TYPE.WINHTTP_ACCESS_TYPE_DEFAULT_PROXY,
			WINHTTP_NO_PROXY_NAME, WINHTTP_NO_PROXY_BYPASS, WINHTTP_OPEN_FLAG.WINHTTP_FLAG_ASYNC);
		if (g_hSession.IsInvalid)
		{
			dwError = GetLastError();
			goto Exit;
		}

		if (WinHttpSetStatusCallback(g_hSession, AsyncCallback, WINHTTP_CALLBACK_FLAG.WINHTTP_CALLBACK_FLAG_ALL_NOTIFICATIONS) == WINHTTP_INVALID_STATUS_CALLBACK)
		{
			dwError = GetLastError();
			goto Exit;
		}

		g_hConnect = WinHttpConnect(g_hSession, pwszServer, INTERNET_DEFAULT_HTTP_PORT, 0);
		if (g_hConnect.IsInvalid)
		{
			dwError = GetLastError();
			goto Exit;
		}

Exit:

		return dwError;
	}

	private static Win32Error OnHeadersAvailable(MYCONTEXT pContext)
	{
		_ = Win32Error.ERROR_SUCCESS;
		WINHTTP_QUERY dwFlags = WINHTTP_QUERY.WINHTTP_QUERY_FLAG_NUMBER | WINHTTP_QUERY.WINHTTP_QUERY_STATUS_CODE;
		uint StatusCode;
		_ = Marshal.SizeOf(typeof(uint));

		Console.Write("OnHeadersAvailable\n");
		Win32Error dwError;
		try { StatusCode = WinHttpQueryHeaders<uint>(pContext.RequestHandle, dwFlags); }
		catch
		{
			dwError = GetLastError();
			Console.Write("OnHeadersAvailable: WinHttpQueryHeaders failed\n");
			goto Exit;
		}

		Console.Write("OnHeadersAvailable: Status={0}\n", StatusCode);

		dwError = StartReadData(pContext);
Exit:

		return dwError;
	}

	private static Win32Error OnReadComplete(MYCONTEXT pContext, uint dwStatusInformationLength)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		Console.Write("OnReadComplete\n");

		if (dwStatusInformationLength != 0)
		{
			Console.Write("OnReadComplete: Bytes read = {0}\n", dwStatusInformationLength);
			dwError = StartReadData(pContext);
		}
		else
		{
			Console.Write("OnReadComplete: Read complete\n");
			_=SetEvent(pContext.RequestFinishedEvent);
		}

		return dwError;
	}

	private static Win32Error StartReadData(MYCONTEXT pContext)
	{
		Win32Error dwError = Win32Error.ERROR_SUCCESS;

		// Notice how we're under LockRequestHandle, so it's OK to touch pContext.RequestHandle.

		if (!WinHttpReadData(pContext.RequestHandle, pContext.Buffer, pContext.Buffer.Size, out _))
		{
			dwError = GetLastError();
			Console.Write("WinHttpReadData failed\n");
		}

		return dwError;
	}

	private class MYCONTEXT : IDisposable
	{
		public SafeCoTaskMemHandle Buffer = new(8192);
		public Win32Error LastError = Win32Error.ERROR_SUCCESS;
		public CRITICAL_SECTION Lock;
		public bool LockInitialized = true;
		public int ReferenceCount = 1;
		public SafeEventHandle RequestFinishedEvent;
		public SafeHINTERNET RequestHandle;

		public MYCONTEXT(SafeHINTERNET Request)
		{
			_=Win32Error.ThrowLastErrorIfFalse(InitializeCriticalSectionAndSpinCount(out Lock, 1000));

			_=Win32Error.ThrowLastErrorIfInvalid(RequestFinishedEvent = CreateEvent(default, true, false, default));

			RequestHandle = Request;
		}

		public static MYCONTEXT FromPtr(IntPtr ptr)
		{
			var idx = ptr.ToInt32();
			return idx >= 0 && idx < contexts.Count ? contexts[idx] : null;
		}

		public void AddRef() => InterlockedIncrement(ref ReferenceCount);

		/*++
		Routine Description:
		A cancel function that is safe to call at any time pContext is valid, from
		any thread.
		Arguments:
		pContext - Request context to cancel.
		Return Value:
		None.
		--*/
		public void CancelRequest()
		{
			var fLocked = false;

			// This is a short piece of code, but there's a lot going on here:
			// - We do not touch pContext without owning a reference.
			// - We do not use RequestHandle without being under the lock.
			// - We check that the RequestHandle is valid before using. The request may have finished successfully, or someone else may have
			// cancelled it, while we were waiting for the lock. (this check is inside LockRequestHandle)
			// - We default the RequestHandle before calling WinHttpCloseHandle, cause there are cases where winhttp will call back inside WinHttpCloseHandle.

			Win32Error dwError = LockRequestHandle();
			if (dwError != Win32Error.ERROR_SUCCESS)
			{
				goto Exit;
			}

			fLocked = true;

			SafeHINTERNET hRequest = RequestHandle;
			RequestHandle = default;
			hRequest?.Dispose();

Exit:

			if (fLocked)
			{
				UnlockRequestHandle();
			}
		}

		/*++
		Routine Description:
		Frees a context. Generally this should only be called by Release.
		Arguments:
		pContext - Request context to free.
		Return Value:
		None.
		--*/
		public void Dispose()
		{
			if (LockInitialized)
			{
				DeleteCriticalSection(ref Lock);
				LockInitialized = false;
			}

			RequestFinishedEvent?.Dispose();
			RequestFinishedEvent = default;
		}

		public Win32Error LockRequestHandle()
		{
			Win32Error dwError = Win32Error.ERROR_SUCCESS;

			EnterCriticalSection(ref Lock);

			if (RequestHandle is null)
			{
				// Request handle is gone already, no point in trying to use it.

				dwError = Win32Error.ERROR_OPERATION_ABORTED;
				LeaveCriticalSection(ref Lock);
			}

			return dwError;
		}

		public void Release()
		{
			int lRefCount;
			lRefCount = InterlockedDecrement(ref ReferenceCount);
			if (lRefCount == 0)
			{
				CancelRequest();
				Dispose();
				contexts[contexts.IndexOf(this)] = null;
			}
		}

		public void UnlockRequestHandle() => LeaveCriticalSection(ref Lock);
	}
}