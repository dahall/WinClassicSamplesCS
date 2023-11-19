using System;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;
using IDataObject = System.Windows.Forms.IDataObject;

namespace ChangeNotifyWatcher;

public partial class CChangeNotifyApp : Form
{
	private const uint c_notifyMessage = 0x04C8; // WM_USER + 200

	private readonly CShellItemChangeWatcher _watcher = new();
	private IShellItem2? psiDrop;

	public CChangeNotifyApp()
	{
		InitializeComponent();
		_watcher.ChangeNotify += OnChangeNotify;
	}

	private enum GROUPID
	{
		DEFAULT,
		NAMES,
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == c_notifyMessage)
			_watcher.OnChangeMessage(m.WParam, (uint)m.LParam.ToInt32());
		base.WndProc(ref m);
	}

	private static IShellItem? GetDragItem(object data)
	{
		var hr = SHGetIDListFromObject(data, out var pidl);
		using (pidl)
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

	private static string GetIDListName(IShellItem psi)
	{
		try { return psi.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEEDITING); }
		catch { return "Can't display"; }
	}

	private static IShellItem2? GetShellItemFromCommandLine()
	{
		var ppszCmd = Environment.GetCommandLineArgs();
		var szSpec = new StringBuilder(ppszCmd.FirstOrDefault(s => s.Length > 1 && s[0] != '-' && s[0] != '/'));
		ShlwApi.PathUnquoteSpaces(szSpec);

		if (string.IsNullOrEmpty(szSpec.ToString())) return null;

		var hr = SHCreateItemFromParsingName(szSpec.ToString(), null, typeof(IShellItem2).GUID, out var ppv);
		return hr.Succeeded ? (IShellItem2)ppv : SHCreateItemFromParsingName<IShellItem2>(Environment.CurrentDirectory);
	}

	private static void SetBlob(IDataObject pdtobj, short cf, IntPtr pvBlob)
	{
		var fc = new FORMATETC { cfFormat = cf, dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -1, tymed = TYMED.TYMED_HGLOBAL };
		var medium = new STGMEDIUM { tymed = TYMED.TYMED_HGLOBAL, unionmember = pvBlob };
		((System.Runtime.InteropServices.ComTypes.IDataObject)pdtobj).SetData(ref fc, ref medium, true);
	}

	private static void SetDropTip(IDataObject pdtobj, DROPIMAGETYPE type, string pszMsg, string? pszInsert)
	{
		var dd = new DROPDESCRIPTION { type = type, szMessage = pszMsg, szInsert = pszInsert ?? "" };
		var hmem = dd.MarshalToPtr(Marshal.AllocHGlobal, out var _);
		try { SetBlob(pdtobj, (short)RegisterClipboardFormat(ShellClipboardFormat.CFSTR_DROPDESCRIPTION), hmem); }
		catch { Marshal.FreeHGlobal(hmem); }
	}

	private void AutoAdjustListView()
	{
		IDC_LISTVIEW.Columns[0].AutoResize(ColumnHeaderAutoResizeStyle.HeaderSize);
		var iSize = ClientRectangle.Right / 2;
		var cxScroll = SystemInformation.VerticalScrollBarWidth;
		if (IDC_LISTVIEW.Columns[0].Width > iSize)
		{
			IDC_LISTVIEW.Columns[0].Width = iSize;
			IDC_LISTVIEW.Columns[1].Width = iSize - cxScroll;
		}
		else
			IDC_LISTVIEW.Columns[1].Width = ClientRectangle.Right - IDC_LISTVIEW.Columns[0].Width - cxScroll;
	}

	private void Copy_Click(object sender, EventArgs e) => CopyTextToClipboard(true);

	private void CopyAll_Click(object sender, EventArgs e) => CopyTextToClipboard(false);

	private void CopyTextToClipboard(bool fSelectionOnly)
	{
		var psz = GetText(fSelectionOnly);
		if (!string.IsNullOrEmpty(psz))
		{
			Clipboard.Clear();
			Clipboard.SetData(DataFormats.UnicodeText, psz);
		}
	}

	private string GetText(bool fSelectionOnly)
	{
		if (fSelectionOnly)
			return string.Join("\t", IDC_LISTVIEW.SelectedItems.Cast<ListViewItem>().Select(i => $"{i.Text}\t{i.SubItems[0].Text}"));
		else
			return string.Join("\t", IDC_LISTVIEW.Items.Cast<ListViewItem>().Select(i => $"{i.Text}\t{i.SubItems[0].Text}"));
	}

	private void LogMessage(GROUPID groupid, string pszName, string pszBuf)
	{
		var lvi = new ListViewItem(new[] { pszName, pszBuf });
		if (groupid != GROUPID.DEFAULT)
			lvi.Group = IDC_LISTVIEW.Groups[0];
		IDC_LISTVIEW.Items.Add(lvi);
	}

	private void OnChangeNotify(SHCNE lEvent, IShellItem2 psi1, IShellItem2 psi2)
	{
		string? pszLeft = null, pszRight = null;

		if (psi1 != null)
			pszLeft = GetIDListName(psi1);

		if (psi2 != null)
			pszRight = GetIDListName(psi2);

		if (lEvent == SHCNE.SHCNE_RENAMEITEM || lEvent == SHCNE.SHCNE_RENAMEFOLDER)
			LogMessage(GROUPID.NAMES, lEvent.ToString(), $"{pszLeft ?? string.Empty} ==> {pszRight ?? string.Empty}");
		else
			LogMessage(GROUPID.NAMES, lEvent.ToString(), $"{pszLeft ?? string.Empty} , {pszRight ?? string.Empty}");

		AutoAdjustListView();
	}

	private void OnDestroyDlg(object sender, FormClosedEventArgs e) => _watcher.StopWatching();

	private void OnDragEnter(object sender, DragEventArgs e)
	{
		var psi = GetDragItem(e.Data);
		SetDropTip(e.Data, DROPIMAGETYPE.DROPIMAGE_LABEL, "Listen for Changes on {0}", psi?.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY));
		e.Effect = DragDropEffects.Copy;
	}

	private void OnDragLeave(object sender, EventArgs e)
	{
		//SetDropTip(e.Data, DROPIMAGETYPE.DROPIMAGE_INVALID, "", "");
	}

	private void OnDrop(object sender, DragEventArgs e)
	{
		psiDrop = GetDragItem(e.Data) as IShellItem2;
		if (psiDrop != null) StartWatching();
	}

	private void OnInitDlg(object sender, EventArgs e)
	{
		// optional cmd line param
		psiDrop = GetShellItemFromCommandLine();
		if (psiDrop != null)
			StartWatching();
	}

	private void PickItem(object sender, EventArgs e)
	{
		var pfd = new IFileOpenDialog();
		if (pfd != null)
		{
			var dwOptions = pfd.GetOptions();
			pfd.SetOptions(dwOptions | FILEOPENDIALOGOPTIONS.FOS_ALLNONSTORAGEITEMS | FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
			pfd.SetTitle("Item Picker");
			var hr = pfd.Show(Handle);
			if (hr.Succeeded)
			{
				if (pfd.GetResult() is IShellItem2 psi)
				{
					psiDrop = psi;
					StartWatching();
				}
			}
		}
	}

	private void StartWatching()
	{
		if (psiDrop is null) throw new InvalidOperationException("No IShellItem to watch.");
		IDC_LISTVIEW.Items.Clear();

		var fRecursive = IDC_RECURSIVE.Checked;
		_watcher.StartWatching(psiDrop, Handle, c_notifyMessage, SHCNE.SHCNE_ALLEVENTS, fRecursive);

		string pszName = psiDrop.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);
		LogMessage(GROUPID.NAMES, "Watching", $"{pszName} {(fRecursive ? "(Recursive)" : string.Empty)}");
		AutoAdjustListView();
		Text = "Watching - " + pszName;
	}
}

internal class CShellItemChangeWatcher : IDisposable
{
	private uint _ulRegister;

	public CShellItemChangeWatcher()
	{
	}

	public event Action<SHCNE, IShellItem2, IShellItem2>? ChangeNotify;

	public void Dispose() => StopWatching();

	// in your window procedure call this message to dispatch the events
	public void OnChangeMessage(HWND wParam, uint lParam)
	{
		var hNotifyLock = SHChangeNotification_Lock(wParam, lParam, out var rawrgpidl, out var lEvent);
		if (hNotifyLock != default)
		{
			try
			{
				if (IsItemNotificationEvent())
				{
					IShellItem2? psi1 = null, psi2 = null;
					var rgpidl = rawrgpidl.ToStructure<LockStruct>();
					if (rgpidl.p1 != default)
						psi1 = SHCreateItemFromIDList<IShellItem2>(rgpidl.p1);
					if (rgpidl.p2 != default)
						psi2 = SHCreateItemFromIDList<IShellItem2>(rgpidl.p2);

					// derived class implements this method, that is where the events are delivered
					OnChangeNotify(lEvent, psi1!, psi2!);
				}
				else
				{
					// dispatch non-item events here in the future
				}
			}
			finally
			{
				SHChangeNotification_Unlock(hNotifyLock);
			}
		}

		bool IsItemNotificationEvent()
		{
			return (lEvent & (SHCNE.SHCNE_UPDATEIMAGE | SHCNE.SHCNE_ASSOCCHANGED | SHCNE.SHCNE_EXTENDED_EVENT | SHCNE.SHCNE_FREESPACE | SHCNE.SHCNE_DRIVEADDGUI | SHCNE.SHCNE_SERVERDISCONNECT)) == 0;
		}
	}

	public HRESULT StartWatching(IShellItem psi, HWND hwnd, uint uMsg, SHCNE lEvents, bool fRecursive)
	{
		StopWatching();

		var hr = SHGetIDListFromObject(psi, out var pidlWatch);
		using (pidlWatch)
			if (hr.Succeeded)
			{
				SHChangeNotifyEntry[] entries = { new SHChangeNotifyEntry { pidl = (IntPtr)pidlWatch, fRecursive = fRecursive } };
				_ulRegister = SHChangeNotifyRegister(hwnd, SHCNRF.SHCNRF_ShellLevel | SHCNRF.SHCNRF_InterruptLevel | SHCNRF.SHCNRF_NewDelivery, lEvents, uMsg, entries.Length, entries);
				hr = _ulRegister != 0 ? HRESULT.S_OK : HRESULT.E_FAIL;
			}
		return hr;
	}

	public void StopWatching()
	{
		if (_ulRegister != 0)
		{
			SHChangeNotifyDeregister(_ulRegister);
			_ulRegister = 0;
		}
	}

	// derived class implements this event
	protected void OnChangeNotify(SHCNE lEvent, IShellItem2 psi1, IShellItem2 psi2) => ChangeNotify?.Invoke(lEvent, psi1, psi2);

	[StructLayout(LayoutKind.Sequential)]
	private struct LockStruct { public IntPtr p1; public IntPtr p2; }
}