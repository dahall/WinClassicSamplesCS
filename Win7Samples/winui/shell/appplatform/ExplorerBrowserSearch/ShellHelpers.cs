using System.Runtime.InteropServices;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

namespace Vanara.PInvoke
{
	internal static class ShellHelpers
	{
		// register the COM local server for the current running module this is for self registering applications
		public static HRESULT HRESULT_FROM_WIN32(Win32Error err) => err.ToHRESULT();

		public static IShellItem GetDragItem(object data)
		{
			var hr = SHGetIDListFromObject(data, out var pidl);
			if (hr.Succeeded)
				return SHCreateItemFromIDList<IShellItem>(pidl);

			if (data is System.Runtime.InteropServices.ComTypes.IDataObject pdo)
			{
				hr = SHCreateShellItemArrayFromDataObject(pdo, typeof(IShellItem2).GUID, out var ppv);
				if (hr.Succeeded && ppv.GetCount() > 0)
					return ppv.GetItemAt(0);

				//pdo.GetData(ref fmt, out var medium);
			}

			return null;
		}

		public static IShellItem GetShellItem(this IFolderView2 pfv, int iItem = -1)
		{
			if (iItem == -1 && pfv.GetSelectedItem(-1, out iItem).Failed)
				return null;
			return pfv.GetItem<IShellItem>(iItem);
		}

		public static object QueryInterface(in object iUnk, in Guid riid)
		{
			QueryInterface(iUnk, riid, out var ppv);
			return ppv;
		}

		public static HRESULT QueryInterface(in object iUnk, in Guid riid, out object ppv)
		{
			var tmp = riid;
			HRESULT hr = Marshal.QueryInterface(Marshal.GetIUnknownForObject(iUnk), ref tmp, out var ippv);
			ppv = hr.Succeeded ? Marshal.GetObjectForIUnknown(ippv) : null;
			System.Diagnostics.Debug.WriteLine($"Successful QI:\t{riid}");
			return hr;
		}

		public static void SetObjectSite(object eb, Shell32.IServiceProvider sp) => (eb as IObjectWithSite)?.SetSite(sp);

		public static bool ShellExecuteItem(this IShellItem psi, string pszVerb = null, HWND hwnd = default)
		{
			SHGetIDListFromObject(psi, out var pidl).ThrowIfFailed();

			var ei = new SHELLEXECUTEINFO
			{
				cbSize = Marshal.SizeOf(typeof(SHELLEXECUTEINFO)),
				fMask = ShellExecuteMaskFlags.SEE_MASK_INVOKEIDLIST,
				hwnd = hwnd,
				nShellExecuteShow = ShowWindowCommand.SW_NORMAL,
				lpIDList = pidl.DangerousGetHandle(),
				lpVerb = pszVerb
			};
			return ShellExecuteEx(ref ei);
		}
	}
}
