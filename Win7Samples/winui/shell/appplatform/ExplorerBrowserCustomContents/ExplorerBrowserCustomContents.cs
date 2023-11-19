using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using IServiceProvider = Vanara.PInvoke.Shell32.IServiceProvider;

[assembly: SupportedOSPlatform("windows")]
namespace ExplorerBrowserCustomContents;

public partial class CExplorerBrowserHostDialog : Form, IServiceProvider, ICommDlgBrowser3
{
	private static readonly Guid IID_ICommDlgBrowser = new("000214F1-0000-0000-C000-000000000046");

	private bool _fEnumerated;
	private IExplorerBrowser? _peb;
	private IResultsFolder? _prf;

	public CExplorerBrowserHostDialog() => InitializeComponent();

	private RECT EBRect => IDC_BROWSER.Bounds;

	HRESULT IServiceProvider.QueryService(in Guid guidService, in Guid riid, out IntPtr ppvObject)
	{
		if (guidService.Equals(IID_ICommDlgBrowser) && (riid.Equals(IID_ICommDlgBrowser) || riid.Equals(typeof(ICommDlgBrowser3).GUID)))
		{
			ppvObject = Marshal.GetComInterfaceForObject(this, typeof(ICommDlgBrowser3));
			return HRESULT.S_OK;
		}
		ppvObject = default;
		return HRESULT.E_NOINTERFACE;
	}

	// ICommDlgBrowser
	public HRESULT OnDefaultCommand(IShellView ppshv)
	{
		_OnExplore(this, EventArgs.Empty);
		return HRESULT.S_OK;
	}

	public HRESULT OnStateChange(IShellView ppshv, CDBOSC uChange)
	{
		if (uChange == CDBOSC.CDBOSC_SELCHANGE)
			_OnSelChange();
		return HRESULT.S_OK;
	}

	public HRESULT IncludeObject(IShellView ppshv, IntPtr pidl) => HRESULT.S_OK;

	HRESULT ICommDlgBrowser3.GetCurrentFilter(StringBuilder pszFileSpec, int cchFileSpec) => HRESULT.S_OK;

	HRESULT ICommDlgBrowser3.GetDefaultMenuText(IShellView shellView, StringBuilder buffer, int bufferMaxLength) => HRESULT.E_NOTIMPL;

	HRESULT ICommDlgBrowser3.GetViewFlags(out CDB2GVF pdwFlags) { pdwFlags = CDB2GVF.CDB2GVF_NOINCLUDEITEM; return HRESULT.S_OK; }

	HRESULT ICommDlgBrowser3.Notify(IShellView pshv, CDB2N notifyType) => HRESULT.S_OK;

	HRESULT ICommDlgBrowser3.OnColumnClicked(IShellView ppshv, int iColumn) => HRESULT.S_OK;

	HRESULT ICommDlgBrowser3.OnPreViewCreated(IShellView ppshv) => HRESULT.S_OK;

	private void _OnInitDlg(object sender, EventArgs e)
	{
		// Hide initial folder information
		IDC_STATIC.Hide();

		try
		{
			_peb = new IExplorerBrowser();
			ShellHelpers.SetObjectSite(_peb, this);
			_peb.Initialize(Handle, EBRect, new FOLDERSETTINGS(FOLDERVIEWMODE.FVM_DETAILS, FOLDERFLAGS.FWF_AUTOARRANGE | FOLDERFLAGS.FWF_NOWEBVIEW));
			_peb.SetOptions(EXPLORER_BROWSER_OPTIONS.EBO_NAVIGATEONCE); // do not allow navigations

			// Initialize the explorer browser so that we can use the results folder as the data source. This enables us to program the
			// contents of the view via IResultsFolder
			_peb.FillFromObject(null, EXPLORER_BROWSER_FILL_FLAGS.EBF_NODROPTARGET);
			var pfv2 = _peb.GetCurrentView<IFolderView2>();

			if (pfv2 is IColumnManager pcm)
			{
				var rgkeys = new[] { PROPERTYKEY.System.ItemNameDisplay, PROPERTYKEY.System.ItemFolderPathDisplay };
				pcm.SetColumns(rgkeys, (uint)rgkeys.Length);
				var ci = new CM_COLUMNINFO(CM_MASK.CM_MASK_WIDTH | CM_MASK.CM_MASK_DEFAULTWIDTH | CM_MASK.CM_MASK_IDEALWIDTH);
				pcm.GetColumnInfo(rgkeys[1], ref ci);
				ci.uWidth += 100;
				ci.uDefaultWidth += 100;
				ci.uIdealWidth += 100;
				pcm.SetColumnInfo(rgkeys[1], ci);
			}

			_prf = pfv2.GetFolder<IResultsFolder>();
			_StartFolderEnum();
		}
		catch
		{
			Close();
		}
	}

	private void _OnSelChange()
	{
		if (_fEnumerated)
		{
			var pfv2 = _peb.GetCurrentView<IFolderView2>();
			var psi = pfv2.GetShellItem(-1);
			IDC_FOLDERNAME.Text = psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY) ?? "";
			IDC_FOLDERPATH.Text = psi?.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING) ?? "";
			IDC_EXPLORE.Enabled = psi is not null;
		}
	}

	private void _OnDestroyDlg(object sender, FormClosedEventArgs e)
	{
		if (_peb is not null)
		{
			ShellHelpers.SetObjectSite(_peb, null);
			_peb.Destroy();
			_peb = null;
		}
		_prf = null;
	}

	private void _OnExplore(object sender, EventArgs e)
	{
		var pfv2 = _peb.GetCurrentView<IFolderView2>();
		var psi = pfv2.GetShellItem();
		psi?.ShellExecuteItem(null, Handle);
	}

	private void _OnRefresh(object sender, EventArgs e)
	{
		_fEnumerated = false;

		// Update UI
		IDC_EXPLORE.Enabled = false;
		IDC_REFRESH.Enabled = false;

		try
		{
			_peb?.RemoveAll();
			_StartFolderEnum();
		}
		catch { }
	}

	private void _StartFolderEnum() => new CFillResultsOnBackgroundThread(this).StartThread(_prf);

	private void FillResultsOnBackgroundThread(IResultsFolder prf)
	{
		// Adjust dialog to show proper enumerating info and buttons
		IDC_STATUS.Show();
		IDC_STATIC.Hide();
		IDC_STATUS.Text = "Starting Enumeration...";

		// Fill in the results (from FillResultsOnBackgroundThread)
		var pManager = new IKnownFolderManager();
		foreach (var i in pManager.GetFolderIds().Select(g => pManager.GetFolder(g)).Select(f => f.GetShellItem<IShellItem>(0)))
			try
			{
				prf.AddItem(i);
			}
			catch { }

		_fEnumerated = true;

		// Adjust dialog to show proper view info and buttons
		IDC_STATUS.Hide();
		IDC_STATIC.Show();
		IDC_REFRESH.Enabled = true;
	}

	private void IDC_BROWSER_Resize(object sender, EventArgs e) => _peb?.SetRect(default, EBRect);

	private void IDC_CANCEL_Click(object sender, EventArgs e) => Close();

	private class CFillResultsOnBackgroundThread
	{
		private CExplorerBrowserHostDialog _pebhd;
		private IStream? _pStream;

		public CFillResultsOnBackgroundThread(CExplorerBrowserHostDialog pebhd) => _pebhd = pebhd;

		public void StartThread(IResultsFolder? prf)
		{
			CoMarshalInterThreadInterfaceInStream(typeof(IResultsFolder).GUID, prf, out _pStream).ThrowIfFailed();
			var sc = SynchronizationContext.Current;
			ThreadPool.QueueUserWorkItem(delegate
			{
				var hr = CoGetInterfaceAndReleaseStream(_pStream, typeof(IResultsFolder).GUID, out var ppv);
				if (hr.Succeeded && ppv is IResultsFolder lprf && sc is not null)
				{
					sc.Post(delegate { _pebhd.FillResultsOnBackgroundThread(lprf); }, null);
				}
			});
		}
	}
}
