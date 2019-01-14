using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using IServiceProvider = Vanara.PInvoke.Shell32.IServiceProvider;

namespace ExplorerBrowserSearch
{
	public partial class CExplorerBrowserSearchApp : Form, IServiceProvider, ICommDlgBrowser3, IExplorerBrowserEvents
	{
		private static string[] g_rgGenericProperties = { "System.Generic.String", "System.StructuredQueryType.String", "System.Generic.Integer",
			"System.StructuredQueryType.Integer", "System.Generic.DateTime", "System.StructuredQueryType.DateTime", "System.Generic.Boolean",
			"System.StructuredQueryType.Boolean", "System.Generic.FloatingPoint", "System.StructuredQueryType.FloatingPoint" };
		private static readonly Guid IID_ICommDlgBrowser = new Guid("000214F1-0000-0000-C000-000000000046");

		private uint _dwCookie;
		private bool _fPerformRenavigate;
		private bool _fSearchStringEmpty = true;
		private IExplorerBrowser _peb;
		private IQueryParser _pqp;

		public CExplorerBrowserSearchApp() => InitializeComponent();

		private RECT EBRect => IDC_EXPLORER_BROWSER.Bounds;

		HRESULT ICommDlgBrowser3.GetCurrentFilter(StringBuilder pszFileSpec, int cchFileSpec) => HRESULT.S_OK;

		HRESULT ICommDlgBrowser3.GetDefaultMenuText(IShellView shellView, StringBuilder buffer, int bufferMaxLength) => HRESULT.E_NOTIMPL;

		HRESULT ICommDlgBrowser3.GetViewFlags(out CDB2GVF pdwFlags) { pdwFlags = CDB2GVF.CDB2GVF_NOINCLUDEITEM; return HRESULT.S_OK; }

		HRESULT ICommDlgBrowser3.IncludeObject(IShellView ppshv, IntPtr pidl) => HRESULT.S_OK;

		HRESULT ICommDlgBrowser3.Notify(IShellView pshv, CDB2N notifyType) => HRESULT.S_OK;

		HRESULT ICommDlgBrowser3.OnColumnClicked(IShellView ppshv, int iColumn) => HRESULT.S_OK;

		HRESULT ICommDlgBrowser3.OnDefaultCommand(IShellView ppshv) => HRESULT.S_OK;

		HRESULT IExplorerBrowserEvents.OnNavigationComplete(IntPtr pidlFolder)
		{
			if (_fPerformRenavigate)
			{
				timer.Stop();
				_OnSearch();
				_fPerformRenavigate = false;
			}
			return HRESULT.S_OK;
		}

		HRESULT IExplorerBrowserEvents.OnNavigationFailed(IntPtr pidlFolder) => HRESULT.E_NOTIMPL;

		HRESULT IExplorerBrowserEvents.OnNavigationPending(IntPtr pidlFolder) => HRESULT.S_OK;

		HRESULT ICommDlgBrowser3.OnPreViewCreated(IShellView ppshv) => HRESULT.S_OK;

		HRESULT ICommDlgBrowser3.OnStateChange(IShellView ppshv, CDBOSC uChange)
		{
			if (uChange == CDBOSC.CDBOSC_SELCHANGE)
				_OnSelChange();
			return HRESULT.S_OK;
		}

		HRESULT IExplorerBrowserEvents.OnViewCreated(IShellView psv) => HRESULT.E_NOTIMPL;

		HRESULT IServiceProvider.QueryService(ref Guid guidService, ref Guid riid, out IntPtr ppvObject)
		{
			if (guidService.Equals(IID_ICommDlgBrowser) && (riid.Equals(IID_ICommDlgBrowser) || riid.Equals(typeof(ICommDlgBrowser3).GUID)))
			{
				ppvObject = Marshal.GetComInterfaceForObject(this, typeof(ICommDlgBrowser3));
				return HRESULT.S_OK;
			}
			ppvObject = default;
			return HRESULT.E_NOINTERFACE;
		}

		private static void AddCustomCondition(ISearchFolderItemFactory psfif)
		{
			var pcf = new IConditionFactory();
			// pv does not have to be freed
			var pv = new PROPVARIANT("*.jpg");
			var pc = pcf.MakeLeaf("System.FileName", CONDITION_OPERATION.COP_DOSWILDCARDS, null, pv, null, null, null, false);
			psfif.SetCondition(pc);
		}

		private static void AddStructuredQueryCondition(ISearchFolderItemFactory psfif, IQueryParser pqp, string pszQuery)
		{
			var pc = ParseStructuredQuery(pszQuery, pqp);
			psfif.SetCondition(pc);
		}

		// CExplorerBrowserSearchApp
		private static IQueryParser CreateQueryParser()
		{
			var pqpm = new IQueryParserManager();
			var pqp = pqpm.CreateLoadedParser<IQueryParser>("SystemIndex", 0x0400 /* LOCALE_USER_DEFAULT */);
			// Initialize the query parser and set default search property types
			pqpm.InitializeOptions(false, true, pqp);
			for (var i = 0; i < g_rgGenericProperties.Length / 2; i += 2)
			{
				pqp.SetMultiOption(STRUCTURED_QUERY_MULTIOPTION.SQMO_DEFAULT_PROPERTY, g_rgGenericProperties[i + 1], new PROPVARIANT(g_rgGenericProperties[i]));
			}
			return pqp;
		}

		private static ICondition ParseStructuredQuery(string pszString, IQueryParser pqp)
		{
			var pqs = pqp.Parse(pszString, null);
			pqs.GetQuery(out var pc, out var _);
			return pqs.Resolve(pc, STRUCTURED_QUERY_RESOLVE_OPTION.SQRO_DONT_SPLIT_WORDS, new SYSTEMTIME(DateTime.Now));
		}

		private static void SetScope(ISearchFolderItemFactory psfif)
		{
			// Set scope to the Pictures library. you can use SHGetKnownFolderItem instead of SHCreateItemInKnownFolder on Win7 and greater
			var hr = SHCreateItemInKnownFolder(KNOWNFOLDERID.FOLDERID_PicturesLibrary.Guid(), 0, null, typeof(IShellItem).GUID, out var psi);
			if (hr.Succeeded)
			{
				SHCreateShellItemArrayFromShellItem((IShellItem)psi, typeof(IShellItemArray).GUID, out var psia).ThrowIfFailed();
				psfif.SetScope(psia);
			}
			else
			{
				// If no Pictures library is available (on Vista, for example), set scope to the Pictures and Public Pictures folders
				var rgItemIDs = new PIDL[2];
				SHGetKnownFolderIDList(KNOWNFOLDERID.FOLDERID_Pictures.Guid(), 0, default, out rgItemIDs[0]).ThrowIfFailed();
				SHGetKnownFolderIDList(KNOWNFOLDERID.FOLDERID_PublicPictures.Guid(), 0, default, out rgItemIDs[1]).ThrowIfFailed();
				var ptrs = Array.ConvertAll(rgItemIDs, p => p.DangerousGetHandle());
				SHCreateShellItemArrayFromIDLists((uint)rgItemIDs.Length, ptrs, out var psia).ThrowIfFailed();
				psfif.SetScope(psia);
			}
		}

		private IShellItem _GetSelectedItem()
		{
			var pfv = _peb.GetCurrentView<IFolderView2>();
			return pfv?.GetShellItem(-1);
		}

		private void _OnDestroyDialog(object sender, FormClosedEventArgs e)
		{
			timer.Stop();
			if (_peb != null)
			{
				ShellHelpers.SetObjectSite(_peb, null);
				_peb.Unadvise(_dwCookie);
				_peb.Destroy();
				_peb = null;
			}
			_pqp = null;
		}

		private void _OnInitializeDialog(object sender, EventArgs e)
		{
			try
			{
				_peb = new IExplorerBrowser();
				ShellHelpers.SetObjectSite(_peb, this);
				_peb.Initialize(Handle, EBRect, new FOLDERSETTINGS(FOLDERVIEWMODE.FVM_ICON, FOLDERFLAGS.FWF_HIDEFILENAMES | FOLDERFLAGS.FWF_NOSUBFOLDERS | FOLDERFLAGS.FWF_NOCOLUMNHEADER));
				_peb.Advise(this, out _dwCookie);

				_pqp = CreateQueryParser();
				BrowseToCustomQuery("kind:picture", "Sample Query");
			}
			catch
			{
				Close();
			}
		}

		private void _OnOpenItem(object sender, EventArgs e) => _GetSelectedItem()?.ShellExecuteItem(null, Handle);

		private void _OnSearch()
		{
			try
			{
				BrowseToCustomQuery(IDC_SEARCHBOX.Text);
			}
			catch (Exception e)
			{
				// The BrowseToObject call is asynchronous, so if it fails because previous navigation is in progress, make sure we
				// re-navigate to process this search query
				if (e.HResult == new Win32Error(Win32Error.ERROR_BUSY).ToHRESULT())
					_fPerformRenavigate = true;
			}
		}

		private void _OnSelChange() => IDC_NAME.Text = _GetSelectedItem()?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);

		private void _UpdateSearchIcon()
		{
			var cchText = IDC_SEARCHBOX.TextLength;

			// If the search box was empty but is no longer, switch icons to "clear"
			if (_fSearchStringEmpty && cchText != 0)
			{
				_fSearchStringEmpty = false;
				IDC_SEARCHING.Image = Properties.Resources.clear;
			}
			else if (!_fSearchStringEmpty && (cchText == 0))
			{
				// When the search box becomes empty again, switch icons to "search"
				_fSearchStringEmpty = true;
				IDC_SEARCHING.Image = Properties.Resources.search;
			}
		}

		private void BrowseToCustomQuery(string pszQueryString, string pszDisplayName = null)
		{
			try
			{
				var psfif = new ISearchFolderItemFactory();
				psfif.SetDisplayName(pszDisplayName ?? pszQueryString);
				SetScope(psfif);
				AddStructuredQueryCondition(psfif, _pqp, pszQueryString);
				var psi = psfif.GetShellItem<IShellItem>();
				_peb.BrowseToObject(psi, 0);
			}
			catch (Exception e)
			{
				MessageBox.Show(this, e.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void IDC_EXPLORER_BROWSER_Resize(object sender, EventArgs e)
		{
			var nullPtr = HDWP.NULL;
			_peb.SetRect(ref nullPtr, EBRect);
		}

		private void IDC_SEARCHBOX_TextChanged(object sender, EventArgs e)
		{
			_UpdateSearchIcon();
			timer.Start();
		}

		// From _SearchIconProc
		private void IDC_SEARCHING_Click(object sender, EventArgs e)
		{
			if (!_fSearchStringEmpty)
				IDC_SEARCHBOX.Text = "";
		}

		private void IDC_SEARCHING_MouseLeave(object sender, EventArgs e)
		{
			if (!_fSearchStringEmpty)
				IDC_SEARCHING.Image = Properties.Resources.clear;
		}

		private void IDC_SEARCHING_MouseMove(object sender, MouseEventArgs e)
		{
			if (!_fSearchStringEmpty)
				IDC_SEARCHING.Image = Properties.Resources.clearhot;
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			timer.Stop();
			_OnSearch();
		}
	}
}