using System;

using static Vanara.PInvoke.User32;

namespace DropTargetVerb
{
	// this class encapsulates the management of a message loop for an application. it supports queing a callback to the application via the
	// message loop to enable the app to return from a call and continue processing that call later. this behavior is needed when
	// implementing a shell verb as verbs must not block the caller. to use this class:
	//
	// 1) inherit from this class
	//
	// class CApplication : CAppMessageLoop
	//
	// 2) in the invoke function, for example IExecuteCommand::Execute() or IDropTarget::Drop() queue a callback by callong QueueAppCallback();
	//
	// IFACEMETHODIMP CExecuteCommandVerb::Execute() { QueueAppCallback(); }
	//
	// 3) implement OnAppCallback, this is the code that will execute the queued callback void OnAppCallback() { // do the work here }
	public abstract class CAppMessageLoop
	{
		// this timer is used to exit the message loop if the the application is activated but not invoked. this is needed if there is a
		// failure when the verb is being invoked due to low resources or some other uncommon reason. without this the app would not exit in
		// this case. this timer needs to be canceled once the app learns that it has should remain running.
		protected const uint uTimeout = 30 * 1000;

		private IntPtr _uPostTimerId;
		private IntPtr _uTimeoutTimerId;      // timer id used to exit the app if the app is not called back within a certain time
											  // timer id used to to queue a callback to the app

		// 30 seconds

		protected CAppMessageLoop() => _uTimeoutTimerId = SetTimer(default, default, uTimeout, null);

		// cancel the timeout timer, this should be called when the appliation knows that it wants to keep running, for example when it
		// recieves the incomming call to invoke the verb, this is done implictly when the app queues a callback
		protected void CancelTimeout()
		{
			if (_uPostTimerId != default)
			{
				KillTimer(default, _uTimeoutTimerId);
				_uTimeoutTimerId = default;
			}
		}

		protected void MessageLoop()
		{
			const uint WM_TIMER = 0x0113;
			while (GetMessage(out var msg, default, 0, 0))
			{
				if (msg.message == WM_TIMER)
				{
					KillTimer(default, msg.wParam);    // all are one shot timers

					if (msg.wParam == _uTimeoutTimerId)
					{
						_uTimeoutTimerId = default;
					}
					else if (msg.wParam == _uPostTimerId)
					{
						_uPostTimerId = default;
						OnAppCallback();
					}
					PostQuitMessage(0);
				}

				TranslateMessage(msg);
				DispatchMessage(msg);
			}
		}

		protected abstract void OnAppCallback();

		protected void QueueAppCallback()
		{
			// queue a callback on OnAppCallback() by setting a timer of zero seconds
			_uPostTimerId = SetTimer(default, default, 0, null);
			if (_uPostTimerId != default)
			{
				CancelTimeout();
			}
		}

		// app must implement
	}
}
