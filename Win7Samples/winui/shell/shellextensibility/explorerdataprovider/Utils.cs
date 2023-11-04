using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace explorerdataprovider
{
	internal static class Utils
	{
		public const int MAX_OBJS = 10;

		public static bool ISFOLDERFROMINDEX(int u) => (u % 2) == 1;

		public static HRESULT DisplayItem(this IShellItemArray psia, HWND hwnd = default)
		{
			// Get the first ShellItem and display its name
			IShellItem psi = psia.GetItemAt(0);
			var pszDisplayName = psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);
			User32.MessageBox(hwnd, pszDisplayName, pszDisplayName, User32.MB_FLAGS.MB_OK);
			return HRESULT.S_OK;
		}

		public static HRESULT GetIndexFromDisplayString(string psz, out uint puIndex)
		{
			try { puIndex = uint.Parse(psz.Substring(4)); return HRESULT.S_OK; }
			catch { puIndex = 0; return HRESULT.E_FAIL; }
		}

		public static HRESULT LoadFolderViewImplDisplayString(int i, out string str)
		{
			try { str = Properties.Resources.ResourceManager.GetString($"IDS_{i}"); }
			catch { str = null; return HRESULT.E_INVALIDARG; }
			return HRESULT.S_OK;
		}

		public static HRESULT LoadFolderViewImplDisplayStrings(out string[] wszArrStrings)
		{
			wszArrStrings = new string[MAX_OBJS];
			for (var i = 0; i < MAX_OBJS; i++)
			{
				HRESULT hr = LoadFolderViewImplDisplayString(i, out var str);
				if (hr.Failed)
					return hr;
				wszArrStrings[i] = str;
			}
			return HRESULT.S_OK;
		}

		public static HRESULT ResultFromShort(int i) => HRESULT.Make(false, 0U, unchecked((uint)i));

		public static HRESULT StringCchCopy(StringBuilder szDest, SizeT szLen, string szSrc)
		{
			if (szDest is not null && !string.IsNullOrEmpty(szSrc) && szLen >= 0)
			{
				try
				{
					szDest.Clear();
					szDest.Append(szSrc.Take(szLen - 1));
					return HRESULT.S_OK;
				}
				catch { }
			}
			return HRESULT.E_FAIL;
		}

		public static HRESULT StringToStrRet(string pszName, out STRRET pStrRet)
		{
			pStrRet = new STRRET { uType = STRRET_TYPE.STRRET_WSTR, pOleStr = Marshal.StringToCoTaskMemUni(pszName) };
			return HRESULT.S_OK;
		}
	}
}