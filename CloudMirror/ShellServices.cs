using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;

namespace CloudMirror;

internal static class ShellServices
{
	public static void InitAndStartServiceTask(CancellationToken cancellationToken)
	{
		var thread = new Thread(() =>
		{
			CoInitializeEx(default, COINIT.COINIT_APARTMENTTHREADED).ThrowIfFailed();

			var thumbnailProviderFactory = new Factory(() => new ThumbnailProvider());
			CoRegisterClassObject(typeof(ThumbnailProvider).GUID, thumbnailProviderFactory, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out var thumbnailProviderFactoryCookie).ThrowIfFailed();

			var contextMenuFactory = new Factory(() => new TestExplorerCommandHandler());
			CoRegisterClassObject(typeof(TestExplorerCommandHandler).GUID, contextMenuFactory, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out var contextMenuFactoryCookie).ThrowIfFailed();

			var customStateProviderFactory = new Factory(() => new CustomStateProvider());
			CoRegisterClassObject(typeof(CustomStateProvider).GUID, customStateProviderFactory, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out var customStateProviderFactoryCookie).ThrowIfFailed();

			var uriSourceFactory = new Factory(() => new UriSource());
			CoRegisterClassObject(typeof(UriSource).GUID, uriSourceFactory, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out var uriSourceFactoryCookie).ThrowIfFailed();

			var stopEvent = cancellationToken.WaitHandle.SafeWaitHandle.DangerousGetHandle();
			CoWaitForMultipleHandles(COWAIT_FLAGS.COWAIT_DISPATCH_CALLS, INFINITE, 1, new[] { stopEvent }, out _);

			CoRevokeClassObject(uriSourceFactoryCookie);
			CoRevokeClassObject(customStateProviderFactoryCookie);
			CoRevokeClassObject(contextMenuFactoryCookie);
			CoRevokeClassObject(thumbnailProviderFactoryCookie);

			CoUninitialize();
		});
		thread.SetApartmentState(ApartmentState.STA);
		thread.Start();
	}

	private class Factory : IClassFactory
	{
		public Factory(Func<object> generator)
		{
			_generator = generator;
		}

		private readonly Func<object> _generator;
		private static readonly Guid IID_IUnknown = new("{00000000-0000-0000-c000-000000000046}");

		HRESULT IClassFactory.CreateInstance(object? pUnkOuter, in Guid riid, out object? ppvObject)
		{
			if (pUnkOuter != null)
			{
				ppvObject = null;
				return HRESULT.CLASS_E_NOAGGREGATION;
			}
			if (riid != IID_IUnknown)
			{
				// We cannot handle this for now
				ppvObject = null;
				return HRESULT.E_NOINTERFACE;
			}
			else
			{
				var obj = _generator();
				ppvObject = obj;
				return HRESULT.S_OK;
			}
		}

		HRESULT IClassFactory.LockServer(bool fLock)
		{
			return HRESULT.S_OK;
		}
	}
}