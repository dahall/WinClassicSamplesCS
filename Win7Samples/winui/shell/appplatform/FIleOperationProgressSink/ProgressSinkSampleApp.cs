// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using Vanara;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.User32;
using static ProgressSinkSample.CommCtl;

[assembly: SupportedOSPlatform("windows")]

namespace ProgressSinkSample;

internal static class Program
{
	// List view column labels
	public const string SZ_COL_DESCRIPTION = "Description";
	public const string SZ_COL_SINKTYPE = "Type";
	public const string SZ_COL_TIME = "Time";

	// From resource.h
	private const ushort ID_CLEAR = 1006;
	private const ushort ID_COPY = 1005;
	private const ushort IDC_DEST = 1003;
	private const ushort IDC_SINKLIST = 1001;
	private const ushort IDC_SRC = 1002;
	private const ushort IDD_MAIN = 129;

	// Max buffer size for displaying sink messages in list view
	private const FILEOP_FLAGS OPERATION_FLAGS_DEFAULT = FILEOP_FLAGS.FOF_NOCONFIRMMKDIR;
	private const uint WM_COPY_END = WM_USER + 1;

	// The sink type labels we care about These are displayed in the list view
	private static readonly string[] g_rgpszSinkType = [ "StartOperations", "FinishOperations", "PreCopyItem", "PostCopyItem", "UpdateProgress" ];

	// Sink type enumeration
	private enum SINK_TYPE_ENUM
	{
		SINK_TYPE_STARTOPERATIONS = 0,
		SINK_TYPE_FINISHOPERATIONS,
		SINK_TYPE_PRECOPYITEM,
		SINK_TYPE_POSTCOPYITEM,
		SINK_TYPE_UPDATEPROGRESS
	};

	[STAThread]
	private static void Main() => new CFileOpProgSinkApp().DoModal();

	[ComVisible(true)]
	private class CFileOpProgSinkApp : IFileOperationProgressSink
	{
		private HWND hwnd;
		private HWND hwndLV;
		private IStream? pstm = null;

		public HRESULT DoModal()
		{
			DialogBoxParam(LoadLibraryEx("ProgressSinkSampleAppRes.dll", LoadLibraryExFlags.LOAD_LIBRARY_AS_DATAFILE),
				IDD_MAIN, default, MainDlgProc);
			return HRESULT.S_OK;
		}

		HRESULT IFileOperationProgressSink.FinishOperations(HRESULT hrResult)
		{
			AddSinkItem(SINK_TYPE_ENUM.SINK_TYPE_STARTOPERATIONS, default);
			return HRESULT.S_OK;
		}

		HRESULT IFileOperationProgressSink.PauseTimer() => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PostCopyItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string pwszNewName, HRESULT hrCopy, IShellItem psiNewlyCreated)
		{
			try
			{
				string pszItem = psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
				string pszDest = psiDestinationFolder.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
				string szBuff = string.Format("Flags: {0}, HRESULT: 0x{1:x}, Item: {2}, Destination: {3}",
					dwFlags, hrCopy, pszItem, pszDest);
				AddSinkItem(SINK_TYPE_ENUM.SINK_TYPE_POSTCOPYITEM, szBuff);
			}
			catch { }
			return HRESULT.S_OK;
		}

		HRESULT IFileOperationProgressSink.PostDeleteItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, HRESULT c, IShellItem? d) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PostMoveItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, IShellItem c, string d, HRESULT e, IShellItem f) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PostNewItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, string c, string? d, uint e, HRESULT f, IShellItem? g) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PostRenameItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, string c, HRESULT d, IShellItem e) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PreCopyItem(TRANSFER_SOURCE_FLAGS dwFlags, IShellItem psiItem, IShellItem psiDestinationFolder, string? pszNewName)
		{
			try
			{
				string pszItem = psiItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
				string pszDest = psiDestinationFolder.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
				string szBuff = string.Format("Flags: {0}, Item: {1}, Destination: {2}", dwFlags, pszItem, pszDest);
				AddSinkItem(SINK_TYPE_ENUM.SINK_TYPE_PRECOPYITEM, szBuff);
			}
			catch { }
			return HRESULT.S_OK;
		}

		HRESULT IFileOperationProgressSink.PreDeleteItem(TRANSFER_SOURCE_FLAGS a, IShellItem b) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PreMoveItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, IShellItem c, string? d) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PreNewItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, string c) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.PreRenameItem(TRANSFER_SOURCE_FLAGS a, IShellItem b, string c) => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.ResetTimer() => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.ResumeTimer() => HRESULT.S_OK;

		HRESULT IFileOperationProgressSink.StartOperations()
		{
			AddSinkItem(SINK_TYPE_ENUM.SINK_TYPE_STARTOPERATIONS, default);
			return HRESULT.S_OK;
		}

		HRESULT IFileOperationProgressSink.UpdateProgress(uint iWorkTotal, uint iWorkSoFar)
		{
			string szBuff = string.Format("Total Work: {0}, Work Completed: {1}", iWorkTotal, iWorkSoFar);
			AddSinkItem(SINK_TYPE_ENUM.SINK_TYPE_UPDATEPROGRESS, szBuff);
			return HRESULT.S_OK;
		}

		private uint CopyThreadProc(IntPtr p)
		{
			// Perform the operation Create the file operation object
			IFileOperation pfo = new();
			// Get our marshalled IFileOperationProgressSink
			var hr = CoGetInterfaceAndReleaseStream(pstm!, typeof(IFileOperationProgressSink).GUID, out var pfops);
			if (hr.Succeeded)
			{
				pstm = default;
				// Setup our callback interface (IFileOperationProgressSink)
				uint dwCookie = pfo.Advise((IFileOperationProgressSink)pfops);
				// Get the source and destination paths of the copy operation
				StringBuilder szSrcPath = new(MAX_PATH);
				hr = (GetDlgItemText(hwnd, IDC_SRC, szSrcPath, szSrcPath.Capacity) > 0) ? HRESULT.S_OK : HRESULT.E_FAIL;
				if (hr.Succeeded)
				{
					// Create an IShellItem from the supplied source path
					IShellItem? psiFrom = SHCreateItemFromParsingName<IShellItem>(szSrcPath.ToString());
					if (psiFrom is not null)
					{
						StringBuilder szDestPath = new(MAX_PATH);
						hr = (GetDlgItemText(hwnd, IDC_DEST, szDestPath, szDestPath.Capacity) > 0) ? HRESULT.S_OK : HRESULT.E_FAIL;
						if (hr.Succeeded)
						{
							// Create an IShellItem from the supplied path
							IShellItem? psiTo = SHCreateItemFromParsingName<IShellItem>(szDestPath.ToString());
							if (psiTo is not null)
							{
								// Add the copy operation. We do not add the IFileOperationProgressSink here since we already did this in
								// call to Advise(). If you add it again here you will get duplicate sink notifications for the PreCopyItem
								// and PostCopyItem.
								pfo.CopyItem(psiFrom, psiTo, default);
								Marshal.ReleaseComObject(psiTo);
							}
						}
						Marshal.ReleaseComObject(psiFrom);
					}
				}

				if (hr.Succeeded)
				{
					// Set the main dialog as the owner of any UI (progress, confirmations)
					pfo.SetOwnerWindow(hwnd);
					// Set our default operation flags for the operation
					pfo.SetOperationFlags(OPERATION_FLAGS_DEFAULT);
					// Perform the operation to copy the item
					pfo.PerformOperations();
				}
				// Remove the callback
				pfo.Unadvise(dwCookie);
				Marshal.ReleaseComObject(pfo);
			}
			if (pstm is not null)
			{
				Marshal.ReleaseComObject(pstm);
			}

			// Notify the main window that we are done
			PostMessage(hwnd, WM_COPY_END, default, default);

			// Clean up the passed in THREAD_INFO struct
			return default;
		}

		private IntPtr MainDlgProc(HWND hdlg, uint uMsg, IntPtr wParam, IntPtr lParam)
		{
			if (uMsg == (uint)WindowMessage.WM_INITDIALOG)
			{
				hwnd = hdlg;
			}
			return DlgProc(uMsg, wParam, lParam);
		}

		private void AddSinkItem(SINK_TYPE_ENUM eSinkType, string? pszDescription)
		{
			// Create a new list view item for this sink message
			LVITEM lvi = new(ListView_GetItemCount(hwndLV), 0, ListViewItemMask.LVIF_TEXT | ListViewItemMask.LVIF_STATE);
			ListView_InsertItem(hwndLV, lvi);

			// Add the sink type to the list item
			lvi.Text = g_rgpszSinkType[(int)eSinkType];
			ListView_SetItem(hwndLV, lvi);

			// Get the current time
			string szTime = DateTime.Now.ToShortTimeString();

			// Add the time to the list item
			lvi.iSubItem = 1;
			lvi.Text = szTime;
			ListView_SetItem(hwndLV, lvi);

			// Add the description for the event to the list item
			lvi.iSubItem = 2;
			lvi.Text = pszDescription ?? "";
			ListView_SetItem(hwndLV, lvi);
		}

		private IntPtr DlgProc(uint uMsg, IntPtr wParam, IntPtr _)
		{
			switch ((WindowMessage)uMsg)
			{
				case WindowMessage.WM_INITDIALOG:
					OnInitDlg();
					break;

				case WindowMessage.WM_CLOSE:
					// Ensure our copy thread is done
					OnCopyEnd();
					EndDialog(hwnd, default);
					break;

				case (WindowMessage)WM_COPY_END:
					// Copy thread has ended
					OnCopyEnd();
					break;

				case WindowMessage.WM_COMMAND:
					switch (Macros.LOWORD(wParam))
					{
						case ID_COPY:
							OnCopyStart();
							break;

						case ID_CLEAR:
							OnClear();
							break;
					}
					break;
			}
			return default;
		}

		private void OnClear()
		{
			// Clear the edit controls
			SetDlgItemText(hwnd, IDC_SRC, "");
			SetDlgItemText(hwnd, IDC_DEST, "");

			// Clear and disable the list view
			ListView_DeleteAllItems(hwndLV);
			EnableWindow(hwndLV, false);
		}

		private void OnCopyEnd()
		{
			// Enable the buttons on the dialog
			EnableWindow(GetDlgItem(hwnd, ID_COPY), true);
			EnableWindow(GetDlgItem(hwnd, ID_CLEAR), true);
		}

		private void OnCopyStart()
		{
			// Disable the buttons on the dialog
			EnableWindow(GetDlgItem(hwnd, ID_COPY), false);
			EnableWindow(GetDlgItem(hwnd, ID_CLEAR), false);

			// Enable the list view
			EnableWindow(hwndLV, true);

			// Ensure list view is cleared for the new operation
			ListView_DeleteAllItems(hwndLV);

			// We want to marshall over the IFileOperationProgressSink to the worker thread.
			if (CoMarshalInterThreadInterfaceInStream(typeof(IFileOperationProgressSink).GUID, this, out pstm).Succeeded)
			{
				// Launch the copy thread. We do our operation on a separate thread so we do not block the UI. We must marshall over our IFileOperationProgressSink.
				if (SHCreateThread(CopyThreadProc, default, SHCT_FLAGS.CTF_COINIT, default))
				{
					// thread assumes ownership
				}
				else
				{
					// Restore dialog state
					OnCopyEnd();
					// release our stream
					Marshal.ReleaseComObject(pstm);
				}
			}
		}

		private void OnInitDlg()
		{
			// Initialize the list view which shows the sink results
			hwndLV = GetDlgItem(hwnd, IDC_SINKLIST);

			ListView_SetExtendedListViewStyle(hwndLV, ListViewStyleEx.LVS_EX_FULLROWSELECT);

			// Initialize the columns
			using LVCOLUMN lvc = new(ListViewColumMask.LVCF_FMT | ListViewColumMask.LVCF_WIDTH | ListViewColumMask.LVCF_TEXT);
			lvc.fmt = ListViewColumnFormat.LVCFMT_LEFT;
			lvc.cx = 100;
			lvc.Text = SZ_COL_SINKTYPE;
			ListView_InsertColumn(hwndLV, 0, lvc);

			lvc.cx = 100;
			lvc.Text = SZ_COL_TIME;
			ListView_InsertColumn(hwndLV, 1, lvc);

			lvc.cx = 225;
			lvc.Text = SZ_COL_DESCRIPTION;
			ListView_InsertColumn(hwndLV, 2, lvc);

			// Disable the list view by default
			EnableWindow(hwndLV, false);

			// Setup edit controls for auto-complete
			SHAutoComplete(GetDlgItem(hwnd, IDC_SRC), SHACF.SHACF_FILESYSTEM);
			SHAutoComplete(GetDlgItem(hwnd, IDC_DEST), SHACF.SHACF_FILESYSTEM);
		}
	}
}

internal static class CommCtl
{
	/// <summary>Removes all items from a list-view control. You can use this macro or send the LVM_DELETEALLITEMS message explicitly.</summary>
	/// <param name="hwnd">
	/// <para>Type: <c>HWND</c></para>
	/// <para>A handle to the list-view control.</para>
	/// </param>
	/// <returns>None</returns>
	/// <remarks>
	/// When a list-view control receives the LVM_DELETEALLITEMS message, it sends the LVN_DELETEALLITEMS notification code to its parent window.
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-listview_deleteallitems void ListView_DeleteAllItems(
	// hwnd );
	[PInvokeData("commctrl.h", MSDNShortId = "NF:commctrl.ListView_DeleteAllItems")]
	public static bool ListView_DeleteAllItems(HWND hwnd) => (BOOL)SendMessage(hwnd, ListViewMessage.LVM_DELETEALLITEMS);

	/// <summary>Gets the number of items in a list-view control. You can use this macro or send the LVM_GETITEMCOUNT message explicitly.</summary>
	/// <param name="hwnd">
	/// <para>Type: <c>HWND</c></para>
	/// <para>A handle to the list-view control.</para>
	/// </param>
	/// <returns>None</returns>
	// https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-listview_getitemcount void ListView_GetItemCount( hwnd );
	[PInvokeData("commctrl.h", MSDNShortId = "NF:commctrl.ListView_GetItemCount")]
	public static int ListView_GetItemCount(HWND hwnd) => (int)SendMessage(hwnd, ListViewMessage.LVM_GETITEMCOUNT);

	/// <summary>Inserts a new column in a list-view control. You can use this macro or send the LVM_INSERTCOLUMN message explicitly.</summary>
	/// <param name="hwnd">
	/// <para>Type: <c>HWND</c></para>
	/// <para>A handle to the list-view control.</para>
	/// </param>
	/// <param name="iCol">
	/// <para>Type: <c>int</c></para>
	/// <para>The index of the new column.</para>
	/// </param>
	/// <param name="pcol">
	/// <para>Type: <c>const LPLVCOLUMN</c></para>
	/// <para>A pointer to an LVCOLUMN structure that contains the attributes of the new column.</para>
	/// </param>
	/// <returns>None</returns>
	/// <remarks>Columns are visible only in report (details) view.</remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-listview_insertcolumn void ListView_InsertColumn( hwnd,
	// iCol, pcol );
	[PInvokeData("commctrl.h", MSDNShortId = "NF:commctrl.ListView_InsertColumn")]
	public static int ListView_InsertColumn(HWND hwndLV, int iCol, LVCOLUMN pcol) =>
		(int)SendMessage(hwndLV, ListViewMessage.LVM_INSERTCOLUMN, iCol, pcol);

	/// <summary>Inserts a new item in a list-view control. You can use this macro or send the LVM_INSERTITEM message explicitly.</summary>
	/// <param name="hwnd">
	/// <para>Type: <c>HWND</c></para>
	/// <para>A handle to the list-view control.</para>
	/// </param>
	/// <param name="pitem">
	/// <para>Type: <c>const LPLVITEM</c></para>
	/// <para>
	/// A pointer to an LVITEM structure that specifies the attributes of the list-view item. Use the <c>iItem</c> member to specify the
	/// zero-based index at which the new item should be inserted. If this value is greater than the number of items currently contained
	/// by the listview control, the new item will be appended to the end of the list and assigned the correct index. Examine the macro's
	/// return value to determine the actual index assigned to the item.
	/// </para>
	/// </param>
	/// <returns>None</returns>
	/// <remarks>
	/// <para>
	/// You cannot use <c>ListView_InsertItem</c> or LVM_INSERTITEM to insert subitems. The <c>iSubItem</c> member of the LVITEM
	/// structure must be zero. See LVM_SETITEM for information on setting subitems.
	/// </para>
	/// <para>
	/// If a list-view control has the LVS_EX_CHECKBOXES style set, any value placed in bits 12 through 15 of the <c>state</c> member of
	/// the LVITEM structure will be ignored. When an item is added with this style set, it will always be set to the unchecked state.
	/// </para>
	/// <para>
	/// If a list-view control has either the LVS_SORTASCENDING or LVS_SORTDESCENDING window style, an LVM_INSERTITEM message will fail
	/// if you try to insert an item that has LPSTR_TEXTCALLBACK as the <c>pszText</c> member of its LVITEM structure.
	/// </para>
	/// <para>
	/// The <c>ListView_InsertItem</c> macro will insert the new item in the proper position in the sort order if the following
	/// conditions hold:
	/// </para>
	/// <list type="bullet">
	/// <item>
	/// <description>You are using one of the LVS_SORTXXX styles.</description>
	/// </item>
	/// <item>
	/// <description>You are not using the LVS_OWNERDRAW style.</description>
	/// </item>
	/// <item>
	/// <description>The <c>pszText</c> member of the structure pointed to by <c>pitem</c> is not set to LPSTR_TEXTCALLBACK.</description>
	/// </item>
	/// </list>
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-listview_insertitem void ListView_InsertItem( hwnd, pitem );
	[PInvokeData("commctrl.h", MSDNShortId = "NF:commctrl.ListView_InsertItem")]
	public static int ListView_InsertItem(HWND hwnd, [In] LVITEM pitem) => (int)SendMessage(hwnd, ListViewMessage.LVM_INSERTITEM, 0, pitem);

	/// <summary>
	/// Sets extended styles for list-view controls. You can use this macro or send the LVM_SETEXTENDEDLISTVIEWSTYLE message explicitly.
	/// </summary>
	/// <param name="hwndLV">
	/// <para>Type: <c>HWND</c></para>
	/// <para>A handle to the list-view control that will receive the style change.</para>
	/// </param>
	/// <param name="dw">
	/// <para>Type: <c>DWORD</c></para>
	/// <para>
	/// A <c>DWORD</c> value that specifies the extended list-view control style. This parameter can be a combination of Extended
	/// List-View Styles.
	/// </para>
	/// </param>
	/// <returns>None</returns>
	/// <remarks>
	/// <para>
	/// For backward compatibility reasons, the <c>ListView_SetExtendedListViewStyle</c> macro has not been updated to use
	/// <c>dwExMask</c>. To use the <c>dwExMask</c> value, use the ListView_SetExtendedListViewStyleEx macro.
	/// </para>
	/// <para>
	/// When you use this macro to set the LVS_EX_CHECKBOXES style, any previously set state image index will be discarded. All check
	/// boxes will be initialized to the unchecked state. The state image index is contained in bits 12 through 15 of the <c>state</c>
	/// member of the LVITEM structure.
	/// </para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-listview_setextendedlistviewstyle void
	// ListView_SetExtendedListViewStyle( hwndLV, dw );
	[PInvokeData("commctrl.h", MSDNShortId = "NF:commctrl.ListView_SetExtendedListViewStyle")]
	public static ListViewStyleEx ListView_SetExtendedListViewStyle(HWND hwndLV, ListViewStyleEx dw) =>
		(ListViewStyleEx)(int)SendMessage(hwndLV, ListViewMessage.LVM_SETEXTENDEDLISTVIEWSTYLE, dw, (IntPtr)(int)dw);

	/// <summary>
	/// Sets some or all of a list-view item's attributes. You can also use <c>ListView_SetItem</c> to set the text of a subitem. You can
	/// use this macro or send the LVM_SETITEM message explicitly.
	/// </summary>
	/// <param name="hwnd">
	/// <para>Type: <c>HWND</c></para>
	/// <para>A handle to the list-view control.</para>
	/// </param>
	/// <param name="pitem">
	/// <para>Type: <c>const LPLVITEM</c></para>
	/// <para>
	/// A pointer to an LVITEM structure that contains the new item attributes. The <c>iItem</c> and <c>iSubItem</c> members identify the
	/// item or subitem, and the <c>mask</c> member specifies which attributes to set. If the <c>mask</c> member specifies the LVIF_TEXT
	/// value, the <c>pszText</c> member is the address of a null-terminated string and the <c>cchTextMax</c> member is ignored. If the
	/// <c>mask</c> member specifies the LVIF_STATE value, the <c>stateMask</c> member specifies which item states to change, and the
	/// <c>state</c> member contains the values for those states.
	/// </para>
	/// </param>
	/// <returns>None</returns>
	/// <remarks>
	/// <para>
	/// To set the attributes of a list-view item, set the <c>iItem</c> member of the LVITEM structure to the index of the item, and set
	/// the <c>iSubItem</c> member to zero. For an item, you can use the <c>state</c>, <c>pszText</c>, <c>iImage</c>, and <c>lParam</c>
	/// members of the <c>LVITEM</c> structure to modify these item parameters.
	/// </para>
	/// <para>
	/// To set the text of a subitem, set the <c>iItem</c> and <c>iSubItem</c> members to indicate the specific subitem, and use the
	/// <c>pszText</c> member to specify the text. Alternatively, you can use the ListView_SetItemText macro to set the text of a
	/// subitem. You cannot set the <c>state</c> or <c>lParam</c> members for subitems because subitems do not have these attributes. In
	/// version 4.70 and later, you can set the <c>iImage</c> member for subitems. The subitem image will be displayed if the list-view
	/// control has the LVS_EX_SUBITEMIMAGES extended style. Previous versions will ignore the subitem image.
	/// </para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/commctrl/nf-commctrl-listview_setitem void ListView_SetItem( hwnd, pitem );
	[PInvokeData("commctrl.h", MSDNShortId = "NF:commctrl.ListView_SetItem")]
	public static bool ListView_SetItem(HWND hwnd, [In] LVITEM pitem) => (BOOL)SendMessage(hwnd, ListViewMessage.LVM_SETITEM, 0, pitem);
}