using System.Runtime.InteropServices.ComTypes;
using Vanara.InteropServices;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Mpr;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace Vanara.PInvoke;

public static class COMHelpers
{
	private delegate bool PFNGETNETRESOURCEFROMLOCALPATH([MarshalAs(UnmanagedType.LPWStr)] string pcszPath, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszNameBuf, uint cchNameBufLen, out uint pdwNetType);

	public static bool GetNetResourceFromLocalPath(string pcszPath, StringBuilder pszNameBuf, uint cchNameBufLen, out uint pdwNetType)
	{
		var bRet = false;
		pdwNetType = 0;
		using (var hinst = LoadLibrary("ntshrui.dll"))
			if (!hinst.IsInvalid)
			{
				var pfn = GetProcAddress(hinst, "GetNetResourceFromLocalPathW");
				if (pfn != IntPtr.Zero)
				{
					var fn = (PFNGETNETRESOURCEFROMLOCALPATH)Marshal.GetDelegateForFunctionPointer(pfn, typeof(PFNGETNETRESOURCEFROMLOCALPATH));
					bRet = fn(pcszPath, pszNameBuf, cchNameBufLen, out pdwNetType);
				}
			}
		return bRet;
	}

	public static HRESULT GetUNCPathFromItem(IShellItem psi, out string? ppszUNC)
	{
		string pszPath = psi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
		return PathConvertToUNC(pszPath, out ppszUNC);
	}

	public static HRESULT IStream_CchPrintfAsUTF8(IStream pstm, string pszKeyFormatString, params object?[] argList) => IStream_WriteStringAsUTF8(pstm, string.Format(pszKeyFormatString, argList));

	// write to a UTF8 stream
	public static HRESULT IStream_ReadToBuffer(IStream pstm, uint uMaxSize, out byte[]? ppBytes)
	{
		ppBytes = null;

		var hr = IStream_Size(pstm, out var uli);
		if (hr.Succeeded)
		{
			hr = (uli < uMaxSize) ? HRESULT.S_OK : HRESULT.E_FAIL;
			if (hr.Succeeded)
			{
				var uliLowPart = (uint)(uli & uint.MaxValue);
				using var pdata = new SafeByteArray((int)uliLowPart);
				hr = IStream_Read(pstm, (IntPtr)pdata, uliLowPart);
				if (hr.Succeeded)
				{
					ppBytes = pdata.ToArray();
				}
			}
		}
		return hr;
	}

	public static HRESULT IStream_WriteStringAsUTF8(IStream pstm, string pszBuf)
	{
		var szBufA = Encoding.UTF8.GetBytes(pszBuf);
		return IStream_Write(pstm, (IntPtr)new SafeHGlobalHandle(szBufA), (uint)szBufA.Length);
	}

	public static HRESULT OpenFolderAndSelectItem(IShellItem psi)
	{
		var hr = SHGetIDListFromObject(psi, out var pidl);
		if (hr.Succeeded)
		{
			hr = SHOpenFolderAndSelectItems(pidl, 0, null, 0);
		}
		return hr;
	}

	// Return UNC version of a path if it is a mapped net drive
	//
	// pszPath - initial path (drive letter or UNC style) ppszUNC- UNC path returned here
	//
	// Return: HRESULT.S_OK - UNC path returned
	//
	// The function fails is the path is not a valid network path. If the path is already UNC, a copy is made without validating the
	// path. *ppszUNC must be CoTaskMemFree()'d by the caller.

	public static bool PathConvertLocalToUNC(string pcszLocalPath, out string? ppszUNC)
	{
		ppszUNC = null;

		var szPath = new StringBuilder(pcszLocalPath, MAX_PATH);
		var szResult = new StringBuilder("", MAX_PATH);

		do
		{
			var szCanidate = new StringBuilder("", MAX_PATH); // UNC path
			if (GetNetResourceFromLocalPath(szPath.ToString(), szCanidate, (uint)szCanidate.Capacity, out var _) &&
				('$' != szCanidate[szCanidate.Length - 1]))
			{
				PathAppend(szCanidate, pcszLocalPath.Substring(szPath.Length));
				if ((0 == szResult.Length) || (szCanidate.Length < szResult.Length))
				{
					szResult.Append(szCanidate);
				}
			}
		}
		while (PathRemoveFileSpec(szPath));
		ppszUNC = szResult.Length > 0 ? szResult.ToString() : null;

		return szResult.Length > 0;
	}

	public static HRESULT PathConvertMappedDriveToUNC(string pszPath, out string? ppszUNC)
	{
		ppszUNC = null;

		HRESULT hr = HRESULT.E_FAIL;

		// alternate implementation
		var cbBuffer = 0U;

		// test this with disconnected drive letters, might fail for them

		var err = WNetGetUniversalName(pszPath, INFO_LEVEL.UNIVERSAL_NAME_INFO_LEVEL, default, ref cbBuffer);
		if (err != Win32Error.ERROR_MORE_DATA) return err.ToHRESULT();
		var mem = new SafeHGlobalHandle((int)cbBuffer);
		err = WNetGetUniversalName(pszPath, INFO_LEVEL.UNIVERSAL_NAME_INFO_LEVEL, (IntPtr)mem, ref cbBuffer);
		if (err.Succeeded)
		{
			ppszUNC = mem.ToStructure<UNIVERSAL_NAME_INFO>().lpUniversalName.TrimEnd('\\');
		}

		return hr;
	}

	// search the shares on this machine to find if the pszLocalPath is scoped by one of those shares, if so convert that path to the UNC version
	//
	// since there can be many shares that scope a path this function searches for the one that produces the shortest UNC path (the
	// deepest share)
	//
	// C:\Documents and Settings\All Users\Shared Docs\foo.txt -> \\machine\SharedDocs\foo.txt make best effort to convert a path, either
	// local or mapped net drive, to a UNC path also deals with the case where the path is already a UNC path

	// convert a path that might be local, mapped drive letters or already UNC into a UNC path
	//
	// example: "\\Unc\Path" -> "\\Unc\Path" (return the input) "X:\folder" -> "\\mappedserver\share\folder" "C:\Users" -> "\\Machine\Users"
	// "D:\unshared" -> FAIL
	//
	// free result with CoTaskMemFree()

	public static HRESULT PathConvertToUNC(string pszPath, out string? ppszUNC)
	{
		ppszUNC = null;

		HRESULT hr = default;
		if (PathIsUNC(pszPath))
		{
			ppszUNC = pszPath;
		}
		else
		{
			hr = PathConvertMappedDriveToUNC(pszPath, out ppszUNC);
			if (hr.Failed)
			{
				hr = PathConvertLocalToUNC(pszPath, out ppszUNC) ? HRESULT.S_OK : HRESULT.E_FAIL;
			}
		}
		return hr;
	}
}
