using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;

namespace DropTargetVerb
{
	// template class that encapsulates a local server class factory to be declared on the stack that will factory an already existing object
	// provided to the constructor usually that object is the application or a sub object within the application.
	//
	// class __declspec(uuid("<object CLSID>")) CMyClass : public IUnknown { public: IFACEMETHODIMP QueryInterface(REFIID riid, void **ppv);
	//
	// as follows
	//
	// CStaticClassFactory<CMyClass> classFactory(this); hr = classFactory.Register(CLSCTX_LOCAL_SERVER, REGCLS_SINGLEUSE); if
	// (SUCCEEDED(classFactory.Register(CLSCTX_LOCAL_SERVER, REGCLS_SINGLEUSE))) { classFactory.MessageLoop() }
	public class CStaticClassFactory<TObjectToFactory> : IClassFactory, IDisposable where TObjectToFactory : class
	{
		private uint _dwRegisterClass;
		private readonly TObjectToFactory _punkObject;

		public CStaticClassFactory(TObjectToFactory punkObject)
		{
			_punkObject = punkObject ?? throw new ArgumentNullException(nameof(punkObject));
			if (!Marshal.IsComObject(_punkObject)) throw new ArgumentException("Object must be a COM object", nameof(punkObject));
		}

		public void Register(CLSCTX classContent = CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS classUse = REGCLS.REGCLS_SINGLEUSE) =>
			CoRegisterClassObject(typeof(TObjectToFactory).GUID, this, classContent, classUse, out _dwRegisterClass);

		// IClassFactory
		HRESULT IClassFactory.CreateInstance(object punkOuter, in Guid riid, out object ppv)
		{
			ppv = null;
			if (!(punkOuter is null)) return HRESULT.CLASS_E_NOAGGREGATION;
			return ShellHelpers.QueryInterface(_punkObject, riid, out ppv); //  : TObjectToFactory::CreateInstance(riid, ppv);
		}

		HRESULT IClassFactory.LockServer(bool _) => HRESULT.S_OK;

		void IDisposable.Dispose()
		{
			if (_dwRegisterClass == 0) return;
			CoRevokeClassObject(_dwRegisterClass);
			_dwRegisterClass = 0;
		}
	}
}
