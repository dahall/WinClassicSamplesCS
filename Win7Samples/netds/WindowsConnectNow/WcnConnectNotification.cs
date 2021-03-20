using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WcnApi;

namespace WindowsConnectNow
{
	internal class WcnConnectNotification : IWCNConnectNotify, IDisposable
	{
		private const uint CONNECT_TIME_OUT = 180000;

		private SafeEventHandle connectEndEvent;
		public bool connectFailedCallBackInvoked;
		public bool connectSucceededCallBackInvoked;

		public HRESULT ConnectFailed(HRESULT hrFailure)
		{
			// This sample doesn't attempt to use the specific error code, but you can look at the hrFailure code to help determine what
			// went wrong.
			//
			// If the value is HRESULT _FROM_WIN32(ERROR_NOT_FOUND) or WCN_E_PEER_NOT_FOUND, then the device didn't respond to the
			// connection request.
			//
			// If the value is WCN_E_AUTHENTICATION_FAILED, then the device used an incorrect password.
			//
			// If the value is WCN_E_CONNECTION_REJECTED, then the other device send a NACK, and you can inspect the
			// WCN_TYPE_CONFIGURATION_ERROR integer to see if it sent an error code (if the code is
			// WCN_VALUE_CE_DEVICE_PASSWORD_AUTH_FAILURE, then the device detected that our password was incorrect). However, not all
			// devices send an error code correctly, so be prepared to handle the case where the code is WCN_VALUE_CE_NO_ERROR, even though
			// there was actually an error.
			//
			// If the value is WCN_E_SESSION_TIMEDOUT or HRESULT _FROM_WIN32(ERROR_TIMEOUT), then the device took too long to respond. Note
			// that this sample does impose its own connect timeout (in addition to the timeout built-in to the WCN API).
			connectFailedCallBackInvoked = true;

			// Tell the main thread to stop waiting; the connect has completed.
			SetEvent(connectEndEvent);

			return HRESULT.S_OK; // WCN ignores the return value
		}

		public HRESULT ConnectSucceeded()
		{
			connectSucceededCallBackInvoked = true;

			// Tell the main thread to stop waiting; the connect has completed.
			SetEvent(connectEndEvent);

			return HRESULT.S_OK; // WCN ignores the return value
		}

		public void Dispose()
		{
			if (connectEndEvent is not null)
			{
				connectEndEvent.Dispose();
				connectEndEvent = null;
			}
		}

		public HRESULT Init()
		{
			connectEndEvent = CreateEvent(default, true, false, default);

			if (connectEndEvent.IsInvalid)
			{
				Console.Write("\nERROR: WcnConnectNotification::CreateEvent() failed with the following error [{0}]", GetLastError());
				return HRESULT.E_FAIL;
			}
			return HRESULT.S_OK;
		}

		public HRESULT WaitForConnectionResult()
		{
			HRESULT hr = HRESULT.S_OK;

			if (WaitForSingleObject(connectEndEvent, CONNECT_TIME_OUT) == WAIT_STATUS.WAIT_OBJECT_0)
			{
				ResetEvent(connectEndEvent);
			}
			else
			{
				Console.Write("\nDiscovery timeout (after waiting {0}ms).", CONNECT_TIME_OUT);
			}

			return hr;
		}
	}
}