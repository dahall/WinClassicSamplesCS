using System;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;

namespace CloudMirror
{
	internal static class ShellServices
	{
		public static void InitAndStartServiceTask()
		{
			Task.Run(() =>
			{
				uint cookie;
				var thumbnailProvider = new ThumbnailProvider();
				CoRegisterClassObject(typeof(ThumbnailProvider).GUID, thumbnailProvider, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out cookie).ThrowIfFailed();

				var contextMenu = new TestExplorerCommandHandler();
				CoRegisterClassObject(typeof(TestExplorerCommandHandler).GUID, contextMenu, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out cookie).ThrowIfFailed();

				var customStateProvider = new CustomStateProvider();
				CoRegisterClassObject(typeof(CustomStateProvider).GUID, customStateProvider, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out cookie).ThrowIfFailed();

				var uriSource = new UriSource();
				CoRegisterClassObject(typeof(UriSource).GUID, uriSource, CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_MULTIPLEUSE, out cookie).ThrowIfFailed();

				using var dummyEvent = CreateEvent(null, false, false);
				if (dummyEvent.IsInvalid)
					Win32Error.ThrowLastError();
				CoWaitForMultipleHandles(COWAIT_FLAGS.COWAIT_DISPATCH_CALLS, INFINITE, 1, new[] { (IntPtr)dummyEvent }, out _);
			});
		}
	}
}