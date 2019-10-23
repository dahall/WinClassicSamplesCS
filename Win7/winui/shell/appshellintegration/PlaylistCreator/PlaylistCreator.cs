using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Forms;

using static Vanara.PInvoke.COMHelpers;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShellHelpers;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.User32;

using Vanara.PInvoke;

namespace PlaylistCreator
{
	// debugging notes: run the program once and it will register itself run it again under the debugger with "-embedding", that will start
	// up the app so you can set breakpoints.

	[ClassInterface(ClassInterfaceType.None)]
	public abstract class CPlaylistCreator : INamespaceWalkCB2
	{
		protected const string c_szWPLFileFooter =
					"        </seq>\r\n" +
					"    </body>\r\n" +
					"</smil>\r\n";

		// CPlaylistCreator derivitive which implements the goo necessary to create WPL playlists.
		protected const string c_szWPLFileHeader =
			"<?wpl version=\"1.0\"?>\r\n" +
			"<smil>\r\n" +
			"    <head>\r\n" +
			"       <meta name=\"Generator\" content=\"GuzTools -- 0.0.0.2\"/>\r\n" +
			"       <meta name=\"ItemCount\" content=\"{0}\"/>\r\n" +
			"       <title>{1}</title>\r\n" +
			"    </head>\r\n" +
			"    <body>\r\n" +
			"        <seq>\r\n";

		// the properties we will be asking for (optimzation for the property store)
		protected static readonly PROPERTYKEY[] c_rgProps =
		{
			PROPERTYKEY.System.ParsingPath,   // use instead of ItemUrl
			PROPERTYKEY.System.PerceivedType,
			PROPERTYKEY.System.Media.Duration,
			PROPERTYKEY.System.Title,
			PROPERTYKEY.System.Music.TrackNumber,
			PROPERTYKEY.System.Music.AlbumArtist,
			PROPERTYKEY.System.Music.AlbumTitle
		};

		protected IProgressDialog _ppd;      // held so the callbacks can use this
		protected IStream _pstm;
		private uint _cFileCur;
		private uint _cFilesTotal;
		private readonly uint _dwThreadID;          // post WM_QUIT here when done
		private bool _fCountingFiles = true;

		// reference to Application host for proper ref counting call back used in the "counting files"
		// mode total computed in the count current, for progress UI
		public CPlaylistCreator() => _dwThreadID = GetCurrentThreadId();

		public object Application { get; set; }

		public HRESULT CreatePlaylist(IShellItemArray psia)
		{
			_ppd = new IProgressDialog();
			_ppd.StartProgressDialog(dwFlags: PROGDLG.PROGDLG_AUTOTIME);
			_ppd.SetTitle("Building Playlist");
			_ppd.SetLine(1, "Finding music files...", false);

			var pnsw = new INamespaceWalk();
			pnsw.Walk(psia, NAMESPACEWALKFLAG.NSWF_TRAVERSE_STREAM_JUNCTIONS | NAMESPACEWALKFLAG.NSWF_DONT_ACCUMULATE_RESULT, 4, this);
			_fCountingFiles = false;
			_ppd.SetLine(1, "Adding files...", false);
			_pstm = _GetPlaylistStream();
			var hr = WriteHeader();
			if (hr.Succeeded)
			{
				pnsw.Walk(psia, NAMESPACEWALKFLAG.NSWF_TRAVERSE_STREAM_JUNCTIONS | NAMESPACEWALKFLAG.NSWF_DONT_ACCUMULATE_RESULT | NAMESPACEWALKFLAG.NSWF_SHOW_PROGRESS, 4, this);
				hr = WriteFooter();
			}

			_pstm.Commit(0);

			if (hr.Succeeded)
			{
				var psiCreated = _GetPlaylistItem<IShellItem>();
				hr = OpenFolderAndSelectItem(psiCreated);
			}
			_ppd.StopProgressDialog();
			_ExitMessageLoop();
			return 0;
		}

		// INamespaceWalkCB
		public HRESULT FoundItem(IShellFolder psf, IntPtr pidl)
		{
			HRESULT hr = HRESULT.S_OK;
			if (_fCountingFiles)
			{
				_cFilesTotal++;
			}
			else
			{
				_cFileCur++;

				var psi = SHCreateItemWithParent<IShellItem2>(psf, pidl);
				hr = _ProcessItem(psi);
				try
				{
					string pszName = psi.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);
					_ppd.SetProgress64(_cFileCur, _cFilesTotal);
					_ppd.SetLine(2, pszName, true);
				}
				catch { }
			}
			return _ppd.HasUserCancelled() ? HRESULT_FROM_WIN32(Win32Error.ERROR_CANCELLED) : hr;
		}

		public HRESULT EnterFolder(IShellFolder a, IntPtr b) => HRESULT.S_OK;

		public HRESULT LeaveFolder(IShellFolder a, IntPtr b) => HRESULT.S_OK;

		public HRESULT InitializeProgressDialog(out string ppszTitle, out string ppszCancel)
		{
			ppszTitle = ppszCancel = null;
			return HRESULT.E_NOTIMPL;
		}

		public HRESULT WalkComplete(HRESULT _)
		{
			if (!_fCountingFiles)
			{
				_ExitMessageLoop();
			}
			return HRESULT.S_OK;
		}

		protected abstract HRESULT FormatItem(uint ulDuration, string pszName, string pszPath);

		protected abstract string GetFileName();

		protected uint GetTotalFiles() => _cFilesTotal;

		protected virtual HRESULT WriteFooter() => HRESULT.S_OK;

		protected virtual HRESULT WriteHeader() => HRESULT.S_OK;

		private void _ExitMessageLoop() => PostThreadMessage(_dwThreadID, 0x0012 /*WM_QUIT*/);

		private T _GetPlaylistItem<T>() where T : class =>
			SHCreateItemFromRelativeName<T>(_GetSaveInFolder<IShellItem>(), GetFileName());

		private IStream _GetPlaylistStream()
		{
			var psiFolder = _GetSaveInFolder<IShellItem>();
			var pstg = (psiFolder as IShellItem)?.BindToHandler<IStorage>(null, BHID.BHID_Storage.Guid());
			return pstg?.CreateStream(GetFileName(), STGM.STGM_CREATE | STGM.STGM_WRITE | STGM.STGM_SHARE_DENY_NONE);
		}

		private T _GetSaveInFolder<T>() where T : class =>
			SHCreateItemInKnownFolder<T>(KNOWNFOLDERID.FOLDERID_Playlists, KNOWN_FOLDER_FLAG.KF_FLAG_CREATE);

		private HRESULT _ProcessItem(IShellItem2 psi)
		{
			var hr = GetUNCPathFromItem(psi, out var pszPath);
			if (hr.Failed)
			{
				pszPath = psi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
			}

			// the property store for this item. note that this binds to the file system property store even for DBFolder items. we want
			// to fix this in the future
			var psr = new CPropertyStoreReader(psi, PropSys.GETPROPERTYSTOREFLAGS.GPS_DELAYCREATION, c_rgProps, (uint)c_rgProps.Length);
			var perceivedType = psr.GetInt32(PROPERTYKEY.System.PerceivedType);
			if (3 /* PERCEIVED_TYPE_AUDIO */ == perceivedType)
			{
				var ullDuration = psr.GetUInt64(PROPERTYKEY.System.Media.Duration);
				ullDuration /= 10000000; // scale ns to seconds

				var pszTitle = psr.GetString(PROPERTYKEY.System.Title);
				//var ulTrackNumber = psr.GetUInt32(PROPERTYKEY.System.Music.TrackNumber);
				//var pszArtist = psr.GetString(PROPERTYKEY.System.Music.AlbumArtist);
				//var pszAlbum = psr.GetString(PROPERTYKEY.System.Music.AlbumTitle);
				FormatItem((uint)ullDuration, pszTitle, pszPath);
			}
			return HRESULT.S_OK;
		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	public class CM3UPlaylistCreator : CPlaylistCreator
	{
		protected override HRESULT FormatItem(uint ulDuration, string pszName, string pszPath)
		{
			// #EXTINF:###,Artist - Title
			return IStream_CchPrintfAsUTF8(_pstm, "#EXTINF:{0},{1}\r\n{2}\r\n", (int)ulDuration, pszName, pszPath);
		}

		protected override string GetFileName() => "New Playlist.m3u";

		protected override HRESULT WriteHeader() => IStream_WriteStringAsUTF8(_pstm, "#EXTM3U\r\n");
	}

	[ClassInterface(ClassInterfaceType.None)]
	public class CWPLPlaylistCreator : CPlaylistCreator
	{
		protected override HRESULT FormatItem(uint ulDuration, string pszName, string pszPath) =>
			IStream_CchPrintfAsUTF8(_pstm, "            <media src=\"{0}\"/>\r\n", pszPath);

		protected override string GetFileName() => "New Playlist.wpl";

		protected override HRESULT WriteFooter() => IStream_WriteStringAsUTF8(_pstm, c_szWPLFileFooter);

		protected override HRESULT WriteHeader()
		{
			// _cFilesTotal is the # of items found, not the number of music files
			return IStream_CchPrintfAsUTF8(_pstm, c_szWPLFileHeader, GetTotalFiles(), GetFileName());
		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	public class CPlaylistCreatorApp
	{
		internal const uint WM_APP_CREATE_M3U = WM_APP + 101;
		internal const uint WM_APP_CREATE_WPL = WM_APP + 100;
		private readonly CM3UPlaylistCreator _M3UCreator = new CM3UPlaylistCreator();
		private IShellItemArray _psia;
		private readonly CCreateM3UPlaylistVerb _verbCreateM3U = new CCreateM3UPlaylistVerb();
		private readonly CCreateWPLPlaylistVerb _verbCreateWPL = new CCreateWPLPlaylistVerb();
		private readonly CWPLPlaylistCreator _WPLCreator = new CWPLPlaylistCreator();

		public CPlaylistCreatorApp()
		{
			// set CPlaylistCreatorApp as the application host on the playlistcreator and verb classes
			_WPLCreator.Application = _M3UCreator.Application = this;
			_verbCreateWPL.Application = _verbCreateM3U.Application = this;
		}

		~CPlaylistCreatorApp()
		{
			_psia = null;
		}

		// to make the verbs async capture the item array and post a message to do the work later
		public void CreatePlaylistAsync(uint msg, IShellItemArray psia)
		{
			// only allow one at a time
			if (_psia == null)
			{
				_psia = psia;
				PostThreadMessage(GetCurrentThreadId(), msg);
			}
		}

		public void DoMessageLoop()
		{
			try
			{
				_verbCreateWPL.Register();
				_verbCreateM3U.Register();

				while (GetMessage(out var msg))
				{
					if (msg.message == WM_APP_CREATE_WPL)
					{
						_WPLCreator.CreatePlaylist(_psia);
					}
					else if (msg.message == WM_APP_CREATE_M3U)
					{
						_M3UCreator.CreatePlaylist(_psia);
					}
					TranslateMessage(msg);
					DispatchMessage(msg);
				}

				_verbCreateWPL.Unregister();
				_verbCreateM3U.Unregister();
			}
			catch (Exception e)
			{
				System.Windows.Forms.MessageBox.Show(e.ToString(), "SDK Sample - Playlist Creator");
			}
		}

		public void SetSite(object p)
		{
		}

		[ComVisible(true)]
		[Guid("B011CE4C-1C1B-4A68-9240-D1D8866537E9")]
		public class CCreateM3UPlaylistVerb : CApplicationVerb<CPlaylistCreatorApp, CCreateM3UPlaylistVerb>
		{
			public CCreateM3UPlaylistVerb() { }

			protected override void DoVerb(IShellItemArray psia) => Application?.CreatePlaylistAsync(WM_APP_CREATE_M3U, psia);
		}

		[ComVisible(true)]
		[Guid("352D62AD-3B26-4C1F-AD43-C2A4E6DFC916")]
		public class CCreateWPLPlaylistVerb : CApplicationVerb<CPlaylistCreatorApp, CCreateWPLPlaylistVerb>
		{
			public CCreateWPLPlaylistVerb() { }

			protected override void DoVerb(IShellItemArray psia) => Application?.CreatePlaylistAsync(WM_APP_CREATE_WPL, psia);
		}
	}

	[ClassInterface(ClassInterfaceType.None)]
	static class Program
	{
		static readonly string[] rgAssociationElementsMusic =
		{
			"SystemFileAssociations\\Directory.Audio", // music folders
			"Stack.System.Music",                      // music stacks anywhere
			"Stack.Audio",                             // stacks in music library
			"SystemFileAssociations\\Audio",           // music items
		};
		const string c_szCreateWPLPlaylistVerb = "CreateWPLPlaylist";
		const string c_szCreateM3UPlaylistVerb = "CreateM3UPlaylist";

		[STAThread]
		private static void Main(string[] pszCmdLine)
		{
			//DisableComExceptionHandling();

			if (pszCmdLine.Length > 0)
			{
				if (string.Equals(pszCmdLine[0], "-Embedding", StringComparison.OrdinalIgnoreCase))
				{
					new CPlaylistCreatorApp().DoMessageLoop();
				}
				else if (string.Equals(pszCmdLine[0], "-Debug", StringComparison.OrdinalIgnoreCase))
				{
					var cpca = new CPlaylistCreatorApp();
					var psi = SHCreateItemFromParsingName<IShellItem>(@"C:\Users\dahall\Downloads\Blue Oyster Cult - (Don't Fear) The Reaper.mp3");
					SHCreateShellItemArrayFromShellItem(psi, typeof(IShellItemArray).GUID, out var psia);
					cpca.CreatePlaylistAsync(CPlaylistCreatorApp.WM_APP_CREATE_M3U, psia);
					cpca.DoMessageLoop();
				}
				else if (string.Equals(pszCmdLine[0], "-Unregister", StringComparison.OrdinalIgnoreCase))
				{
					UnregisterApp();
					System.Windows.Forms.MessageBox.Show("Uninstalled 'Create Playlist' verbs for audio files and containers", "SDK Sample - Playlist Creator");
				}
			}
			else
			{
				RegisterApp();
				System.Windows.Forms.MessageBox.Show("Installed 'Create Playlist' verbs for audio files and containers", "SDK Sample - Playlist Creator");
			}
		}

		private static void RegisterApp()
		{
			var reCreateWPLPlaylist = new CRegisterExtension(typeof(CPlaylistCreatorApp.CCreateWPLPlaylistVerb).GUID);
			reCreateWPLPlaylist.RegisterPlayerVerbs(rgAssociationElementsMusic, (uint)rgAssociationElementsMusic.Length,
				c_szCreateWPLPlaylistVerb, "Create Playlist (.WPL)");

			var reCreateM3ULPlaylist = new CRegisterExtension(typeof(CPlaylistCreatorApp.CCreateM3UPlaylistVerb).GUID);
			reCreateM3ULPlaylist.RegisterPlayerVerbs(rgAssociationElementsMusic, (uint)rgAssociationElementsMusic.Length,
				c_szCreateM3UPlaylistVerb, "Create Playlist (.M3U)");
		}

		private static void UnregisterApp()
		{
			var reCreateWPLPlaylist = new CRegisterExtension(typeof(CPlaylistCreatorApp.CCreateWPLPlaylistVerb).GUID);
			reCreateWPLPlaylist.UnRegisterVerbs(rgAssociationElementsMusic, (uint)rgAssociationElementsMusic.Length, c_szCreateWPLPlaylistVerb);
			reCreateWPLPlaylist.UnRegisterObject();

			var reCreateM3ULPlaylist = new CRegisterExtension(typeof(CPlaylistCreatorApp.CCreateM3UPlaylistVerb).GUID);
			reCreateM3ULPlaylist.UnRegisterVerbs(rgAssociationElementsMusic, (uint)rgAssociationElementsMusic.Length, c_szCreateM3UPlaylistVerb);
			reCreateM3ULPlaylist.UnRegisterObject();
		}
	}
}
