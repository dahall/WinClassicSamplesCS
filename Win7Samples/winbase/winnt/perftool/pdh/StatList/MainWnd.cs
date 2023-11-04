using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Pdh;
using static Vanara.PInvoke.User32;

namespace StatList;

public partial class MainWnd : Form
{
	private const int COUNTER_STRING_SIZE = 1024;
	private const int NUM_STAT_SAMPLES = 100;
	private static readonly int[] nTabStops = { 300, 400, 500, 600, 700 };
	private static readonly string szHelpFileName = System.IO.Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".hlp";

	// font for text in window
	private Font hFinePrint = default;

	// PDH Query Handle for these counters
	private SafePDH_HQUERY hQuery = default;

	// pointer to first item in counter list
	private readonly List<CIB> pFirstCib = new();

	public MainWnd() => InitializeComponent();

	protected override void OnClosing(CancelEventArgs e)
	{
		// Tell WinHelp we don't need it any more...
		_=WinHelp(Handle, szHelpFileName, HelpCmd.HELP_QUIT, default);

		DeleteAllCounters();

		base.OnClosing(e);
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		if (hQuery is null)
		{
			PdhOpenQuery(default, default, out hQuery).ThrowIfFailed();
		}
		if (hFinePrint is null)
		{
			hFinePrint = new Font(Font.Name, 8f);
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);

		if (e.Button != MouseButtons.Right)
			return;

		// This is where you would determine the appropriate 'context' menu to bring up. Since this app has no real functionality, we will
		// just bring up the 'configure' menu:
		var ctx = new ContextMenuStrip();
		foreach (ToolStripMenuItem mi in configureToolStripMenuItem.DropDown.Items)
			_=ctx.Items.Add(mi);
		ctx.Show(this, e.Location);
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		int nX = 0, nY = menuStrip1.Height;

		// draw Title text
		using var hdc = new SafeTempHDC(e.Graphics);
		var szOutputString = "Performance Counter\tLast Value\tMinimum\tMaximum\tAverage";
		var lTextOutReturn = TabbedTextOut(hdc, nX, nY, szOutputString, szOutputString.Length, nTabStops.Length, nTabStops, 0);
		nY += Macros.HIWORD(unchecked((uint)lTextOutReturn));

		// select the fine print font for this window
		using var hfont = new SafeHFONT(hFinePrint.ToHfont());
		_=SelectObject(hdc, hfont);

		// for each CIB in the list draw the current text and value
		foreach (CIB pCib in pFirstCib)
		{
			szOutputString = $"{pCib.szCounterPath}\t{pCib.dLastValue}\t{pCib.pdhCurrentStats.min.doubleValue}\t{pCib.pdhCurrentStats.max.doubleValue}\t{pCib.pdhCurrentStats.mean.doubleValue}";
			lTextOutReturn = TabbedTextOut(hdc, nX, nY, szOutputString, szOutputString.Length, nTabStops.Length, nTabStops, 0);
			nY += Macros.HIWORD(unchecked((uint)lTextOutReturn));
		}
	}

	private void aboutToolStripMenuItem_Click(object sender, EventArgs e) => new AboutDlg().ShowDialog(this);

	private void addCountersToolStripMenuItem_Click(object sender, EventArgs e)
	{
		SafeCoTaskMemString szCounterBuffer = new(COUNTER_STRING_SIZE);
		PDH_BROWSE_DLG_CONFIG BrowseInfo = new()
		{
			Flags = BrowseFlag.bSingleCounterPerDialog | BrowseFlag.bSingleCounterPerAdd | BrowseFlag.bIncludeCostlyObjects,
			hWndOwner = Handle,
			szReturnPathBuffer = szCounterBuffer,
			cchReturnPathLength = COUNTER_STRING_SIZE,
			CallBackStatus = Win32Error.ERROR_SUCCESS,
			dwDefaultDetailLevel = PERF_DETAIL.PERF_DETAIL_WIZARD,
			szDialogBoxCaption = "Select a counter to monitor"
		};

		if (PdhBrowseCounters(ref BrowseInfo).Succeeded)
		{
			// try to add the counter to the query
			if (PdhAddCounter(hQuery, szCounterBuffer, default, out SafePDH_HCOUNTER hCounter).Succeeded)
			{
				// add counter to the list
				CIB pNewCib = new()
				{
					hCounter = hCounter,
					szCounterPath = szCounterBuffer,
					// allocate the raw data buffer here
					pCounterArray = new PDH_RAW_COUNTER[NUM_STAT_SAMPLES]
				};
				pNewCib.pdhCurrentStats.min.CStatus = Win32Error.PDH_CSTATUS_INVALID_DATA;
				pNewCib.pdhCurrentStats.max.CStatus = Win32Error.PDH_CSTATUS_INVALID_DATA;
				pNewCib.pdhCurrentStats.mean.CStatus = Win32Error.PDH_CSTATUS_INVALID_DATA;

				//add to the top of the list
				pFirstCib.Add(pNewCib);

				// repaint window to get new entry
				Refresh();
			} // else unable to add counter
		} // else user cancelled
	}

	private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
	{
		// delete all current counters, then create a new query
		DeleteAllCounters();

		_=PdhOpenQuery(default, default, out hQuery);

		Refresh();
	}

	private void DeleteAllCounters()
	{
		// close PDH Query this removes all counters from the query as well as removes the query item itself
		if (hQuery is not null)
		{
			hQuery.Dispose();
			hQuery = default;
		}

		// clean up any memory allocations
		pFirstCib.Clear();
	}

	private void exitToolStripMenuItem_Click(object sender, EventArgs e) => Close();

	private void getDataToolStripMenuItem_Click(object sender, EventArgs e)
	{
		if (hQuery is null)
			return;

		// get the current values of the query data
		if (PdhCollectQueryData(hQuery).Succeeded)
		{
			// loop through all counters and update the display values and statistics
			foreach (CIB pCib in pFirstCib)
			{
				// update "Last value"
				_=PdhGetFormattedCounterValue(pCib.hCounter, PDH_FMT.PDH_FMT_DOUBLE, out _, out PDH_FMT_COUNTERVALUE pValue);
				pCib.dLastValue = pValue.doubleValue;
				// update "Raw Value" and statistics
				_=PdhGetRawCounterValue(pCib.hCounter, out _, out PDH_RAW_COUNTER pRaw);
				pCib.pCounterArray[pCib.dwNextIndex] = pRaw;
				_=PdhComputeCounterStatistics(pCib.hCounter, PDH_FMT.PDH_FMT_DOUBLE, pCib.dwFirstIndex, ++pCib.dwLastIndex, pCib.pCounterArray, out pCib.pdhCurrentStats);
				// update pointers & indeces
				if (pCib.dwLastIndex < NUM_STAT_SAMPLES)
				{
					pCib.dwNextIndex = ++pCib.dwNextIndex % NUM_STAT_SAMPLES;
				}
				else
				{
					--pCib.dwLastIndex;
					pCib.dwNextIndex = pCib.dwFirstIndex;
					pCib.dwFirstIndex = ++pCib.dwFirstIndex % NUM_STAT_SAMPLES;
				}
			}
			// cause the window to be repainted with the new values (NOTE: This isn't the most efficient method of display updating.)
			Refresh();
		}
	}

	private void helpTopicsToolStripMenuItem_Click(object sender, EventArgs e)
	{
		var bGotHelp = WinHelp(Handle, szHelpFileName, HelpCmd.HELP_FINDER, default);
		if (!bGotHelp)
		{
			_=System.Windows.Forms.MessageBox.Show(this, "Unable to activate help", null, MessageBoxButtons.OK, MessageBoxIcon.Hand);
		}
	}

	private class CIB
	{
		public double dLastValue;
		public uint dwFirstIndex;
		public uint dwLastIndex;
		public uint dwNextIndex;
		public SafePDH_HCOUNTER hCounter;
		public PDH_RAW_COUNTER[] pCounterArray;
		public PDH_STATISTICS pdhCurrentStats;
		public string szCounterPath;
	}
}