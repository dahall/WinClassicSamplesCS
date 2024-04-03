using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

[assembly: SupportedOSPlatform("windows")]
namespace ParsingWithParameters;

internal static class SearchFolder
{
	/// <summary>The main entry point for the application.</summary>
	[STAThread]
	private static void Main()
	{
		DemonstrateFileSystemParsingParameters();
		DemonstrateParsingItemCacheContext();
		DemonstratePreferFolderBrowsingParsing();
	}

	// file system bind data is a parameter passed to IShellFolder::ParseDisplayName() to provide the item information to the file system
	// data source. this will enable parsing of items that do not exist and avoiding accessing the disk in the parse operation {fc0a77e6-9d70-4258-9783-6dab1d0fe31e}
	private static Guid CLSID_UnknownJunction = new(0xfc0a77e6, 0x9d70, 0x4258, 0x97, 0x83, 0x6d, 0xab, 0x1d, 0x0f, 0xe3, 0x1e);

	// create a bind context with a named object
	private static IBindCtx CreateBindCtxWithParam(string pszParam, object punk)
	{
		CreateBindCtx(0, out var pbc).ThrowIfFailed();
		pbc!.RegisterObjectParam(pszParam, punk);
		return pbc;
	}

	// create a bind context with a dummy unknown parameter that is used to pass flag values to operations that accept bind contexts
	private static IBindCtx CreateBindCtxWithParam(string pszParam)
	{
		var punk = new CDummyUnknown(Guid.Empty);
		return CreateBindCtxWithParam(pszParam, punk);
	}

	// "simple parsing" allows you to pass the WIN32_FILE_DATA to the file system data source to avoid it having to access the file. this
	// avoids the expense of getting the information from the file and allows you to parse items that may not necessarily exist
	//
	// the find data is passed to the data source via the bind context constructed here
	private static IBindCtx CreateFileSysBindCtx(in WIN32_FIND_DATA pfd)
	{
		var pfsbd = new CFileSysBindData(pfd);
		CreateBindCtx(0, out var pbc).ThrowIfFailed();
		var bo = new BIND_OPTS { cbStruct = Marshal.SizeOf(typeof(BIND_OPTS)), grfMode = (int)STGM.STGM_CREATE };
		pbc!.SetBindOptions(ref bo);
		pbc.RegisterObjectParam(STR_FILE_SYS_BIND_DATA, pfsbd);
		return pbc;
	}

	// STR_FILE_SYS_BIND_DATA and IFileSystemBindData enable passing the file system information that the file system data source needs
	// to perform a parse. this eliminates the IO that results when parsing an item and lets items that don't exist to be parsed. the
	// helper CreateFileSysBindCtx() internally implements IFileSystemBindData and stores an object in the bind context with thh
	// WIN32_FIND_DATA that it is provided
	private static void DemonstrateFileSystemParsingParameters()
	{
		var fd = new WIN32_FIND_DATA
		{
			dwFileAttributes = FileAttributes.Normal,    // a file (not a folder)
			nFileSizeLow = uint.MaxValue, // file size is null or an unknown value
			nFileSizeHigh = uint.MaxValue,
		};
		var pbcParse = CreateFileSysBindCtx(fd);

		// this item does not exist, but it can be parsed given the parameter provided in the bind context
		var psi = SHCreateItemFromParsingName<IShellItem2>("c:\\a.txt", pbcParse)!;
		Debug.WriteLine(psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));

		// this is useful for resources on the network where the IO in these cases is more costly
		psi = SHCreateItemFromParsingName<IShellItem2>("\\\\Server\\Share\\file.txt", pbcParse)!;
		Debug.WriteLine(psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));
	}

	// STR_ITEM_CACHE_CONTEXT provides a context that can be used for caching to a data source to speed up parsing of multiple items. the
	// file system data source uses this so any clients that will be parsing multiple items should provide this to speed up the parsing function.
	//
	// the cache context object is itself a bind context stored in the bind context under the name STR_ITEM_CACHE_CONTEXT passed to the
	// parse operation via the bind context that it is stored in
	private static void DemonstrateParsingItemCacheContext()
	{
		CreateBindCtx(0, out var pbcItemContext).ThrowIfFailed();

		var pbcParse = CreateBindCtxWithParam(STR_ITEM_CACHE_CONTEXT, pbcItemContext!);
		var pfsbd = new CFileSysBindData(default);
		pbcParse.RegisterObjectParam(STR_FILE_SYS_BIND_DATA, pfsbd);
		var psi = SHCreateItemFromParsingName<IShellItem2>("C:\\folder\\file.txt", pbcParse)!;
		Debug.WriteLine(psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));
		psi = SHCreateItemFromParsingName<IShellItem2>("C:\\folder\\file.doc", pbcParse)!;
		Debug.WriteLine(psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));
	}

	// STR_PARSE_PREFER_FOLDER_BROWSING indicates that an item referenced via an http or https protocol should be parsed using the file
	// system data source that supports such items via the WebDAV redirector. the default parsing these name forms is handled by the
	// internet data source. this option lets you select the file system data source instead.
	//
	// note, unlike the internet data source the file system parsing operation verifies the resource is accessable (issuing IOs to the
	// file system) so these will be slower than the default parsing behavior.
	//
	// providing this enables accessing these items as file system items using the file system data source getting the behavior you would
	// if you provided a file system path (UNC in this case)
	private static void DemonstratePreferFolderBrowsingParsing()
	{
		var pbcParse = CreateBindCtxWithParam(STR_PARSE_PREFER_FOLDER_BROWSING);
		try
		{
			var psi = SHCreateItemFromParsingName<IShellItem2>("http://unknownserver/abc/file.extension", pbcParse)!;
			// the network path is valid
			Debug.WriteLine($"{psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)} {psi.GetAttributes(SFGAO.SFGAO_FILESYSTEM)}"); // will return SFGAO_FILESYSTEM
		}
		catch { }

		// in combination with the file system bind context data this avoids the IO and still parses the item as a file system item
		var pfsbd = new CFileSysBindData(new WIN32_FIND_DATA { dwFileAttributes = FileAttributes.Directory });
		pbcParse.RegisterObjectParam(STR_FILE_SYS_BIND_DATA, pfsbd);
		var psi2 = SHCreateItemFromParsingName<IShellItem2>("http://unknownserver/dav/folder", pbcParse)!;
		Debug.WriteLine($"{psi2.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY)} {psi2.GetAttributes(SFGAO.SFGAO_FILESYSTEM)}"); // will return SFGAO_FILESYSTEM
	}

	[ComVisible(true)]
	private class CDummyUnknown : IPersist
	{
		private Guid _clsid;

		public CDummyUnknown(in Guid clsid) => _clsid = clsid;

		public Guid GetClassID() => _clsid;
	};

	[ComVisible(true)]
	private class CFileSysBindData : IFileSystemBindData, IFileSystemBindData2
	{
		private Guid _clsidJunction = CLSID_UnknownJunction;
		private WIN32_FIND_DATA _fd;
		private long _liFileID;

		public CFileSysBindData(in WIN32_FIND_DATA pfd) => SetFindData(pfd);

		public HRESULT GetFileID(out long pliFileID) { pliFileID = _liFileID; return HRESULT.S_OK; }

		public HRESULT GetFindData(out WIN32_FIND_DATA pfd) { pfd = _fd; return HRESULT.S_OK; }

		public HRESULT GetJunctionCLSID(out Guid pclsid)
		{
			if (_clsidJunction != CLSID_UnknownJunction)
			{
				pclsid = _clsidJunction;
				return HRESULT.S_OK;
			}
			pclsid = Guid.Empty;
			return HRESULT.E_FAIL;
		}

		public HRESULT SetFileID(long liFileID) { _liFileID = liFileID; return HRESULT.S_OK; }

		public HRESULT SetFindData(in WIN32_FIND_DATA pfd) { _fd = pfd; return HRESULT.S_OK; }

		public HRESULT SetJunctionCLSID(in Guid clsid) { _clsidJunction = clsid; return HRESULT.S_OK; }
	}
}