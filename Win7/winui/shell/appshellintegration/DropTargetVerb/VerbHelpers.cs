using System;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.User32_Gdi;

namespace DropTargetVerb
{
	// template class that encapsulates a local server class factory to be declared on the stack
	// that will factory an already existing object provided to the constructor
	// usually that object is the application or a sub object within the
	// application.
	//
	// class __declspec(uuid("<object CLSID>")) CMyClass : public IUnknown
	// {
	// public:
	//     IFACEMETHODIMP QueryInterface(REFIID riid, void **ppv);
	//
	// as follows
	//
	// CStaticClassFactory<CMyClass> classFactory(this);
	// hr = classFactory.Register(CLSCTX_LOCAL_SERVER, REGCLS_SINGLEUSE);
	// if (SUCCEEDED(classFactory.Register(CLSCTX_LOCAL_SERVER, REGCLS_SINGLEUSE)))
	// {
	//     classFactory.MessageLoop()
	// }
	public class CStaticClassFactory<TObjectToFactory> : IClassFactory where TObjectToFactory : class
	{
		private uint _dwRegisterClass;
		private object _punkObject;

		public CStaticClassFactory(object punkObject)
		{
			_punkObject = punkObject ?? throw new ArgumentNullException(nameof(punkObject));
		}

		~CStaticClassFactory()
		{
			if (_dwRegisterClass != 0)
				CoRevokeClassObject(_dwRegisterClass);
		}

		public void Register(CLSCTX classContent, REGCLS classUse)
		{
			CoRegisterClassObject(typeof(TObjectToFactory).GUID, this, classContent, classUse, out _dwRegisterClass);
		}

		// IClassFactory
		public HRESULT CreateInstance(object punkOuter, in Guid riid, out object ppv)
		{
			if (punkOuter != null)
			{
				ppv = null;
				return HRESULT.CLASS_E_NOAGGREGATION;
			}
			return ShellHelpers.QueryInterface(_punkObject, riid, out ppv); //  : TObjectToFactory::CreateInstance(riid, ppv);
		}

		public HRESULT LockServer(bool _) => HRESULT.S_OK;
	}


	// this class encapsulates the management of a message loop for an
	// application. it supports queing a callback to the application via the message
	// loop to enable the app to return from a call and continue processing that call
	// later. this behavior is needed when implementing a shell verb as verbs
	// must not block the caller.  to use this class:
	//
	// 1) inherit from this class
	//
	// class CApplication : CAppMessageLoop
	//
	// 2) in the invoke function, for example IExecuteCommand::Execute() or IDropTarget::Drop()
	// queue a callback by callong QueueAppCallback();
	//
	//   IFACEMETHODIMP CExecuteCommandVerb::Execute()
	//   {
	//       QueueAppCallback();
	//   }
	//
	// 3) implement OnAppCallback, this is the code that will execute the queued callback
	//    void OnAppCallback()
	//    {
	//        // do the work here
	//    }
	public abstract class CAppMessageLoop
	{
		private IntPtr _uTimeoutTimerId;      // timer id used to exit the app if the app is not called back within a certain time
		private IntPtr _uPostTimerId;         // timer id used to to queue a callback to the app

		// this timer is used to exit the message loop if the the application is
		// activated but not invoked. this is needed if there is a failure when the
		// verb is being invoked due to low resources or some other uncommon reason.
		// without this the app would not exit in this case. this timer needs to be
		// canceled once the app learns that it has should remain running.
		protected const uint uTimeout = 30 * 1000;    // 30 seconds

		protected CAppMessageLoop()
		{
			_uTimeoutTimerId = SetTimer(default, default, uTimeout, null);
		}

		protected void QueueAppCallback()
		{
			// queue a callback on OnAppCallback() by setting a timer of zero seconds
			_uPostTimerId = SetTimer(default, default, 0, null);
			if (_uPostTimerId != default)
			{
				CancelTimeout();
			}
		}

		// cancel the timeout timer, this should be called when the appliation
		// knows that it wants to keep running, for example when it recieves the
		// incomming call to invoke the verb, this is done implictly when the
		// app queues a callback
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

		protected abstract void OnAppCallback();  // app must implement
	}
}

