using System;
using System.Runtime.InteropServices;
using static Vanara.PInvoke.Shell32;

namespace Vanara.PInvoke
{
	internal static class ShellHelpers
	{
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