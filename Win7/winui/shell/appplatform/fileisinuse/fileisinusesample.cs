using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

namespace fileisinuse
{
	public partial class CFileInUseApp : Form
	{
		private const string c_szInstructions = "Drag and Drop a file here or click Open File... from the File menu";

		private HFILE _hFile = HFILE.INVALID_HANDLE_VALUE;
		private CFileIsInUseImpl _pfiu;

		public CFileInUseApp() => InitializeComponent();

		private void _CloseFile()
		{
			// Close the file handle
			if (!_hFile.IsInvalid)
			{
				CloseHandle((IntPtr)_hFile);
				_hFile = HFILE.INVALID_HANDLE_VALUE;
			}

			// Release the IFileIsInUse instance which will remove it from the Running Object Table
			_pfiu = null;

			// Remove the file path from the dialog
			IDC_INFO.Text = c_szInstructions;
		}

		private void _DragEnter(object sender, DragEventArgs e)
		{
			var psi = ShellHelpers.GetDragItem(e.Data);
			e.Effect = psi == null ? DragDropEffects.None : DragDropEffects.Link;
		}

		private void _Drop(object sender, DragEventArgs e)
		{
			// Create a IShellItemArray from the IDataObject
			// For this sample, we only open the first item that was dragged and dropped to our application.
			var psi = ShellHelpers.GetDragItem(e.Data);
			if (psi != null)
			{
				// Get the full path of the file
				string pszPath = psi.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
				// Open the file
				_OpenFile(pszPath);
			}
		}

		private void _InitMenuPopup(object sender, EventArgs e) => IDM_FILE_CLOSEFILE.Enabled = !_hFile.IsInvalid;

		private void _OnCommand(object sender, EventArgs e)
		{
			switch ((sender as ToolStripMenuItem)?.Name)
			{
				case nameof(IDM_FILE_OPENFILE):
					_OnOpenFile();
					break;

				case nameof(IDM_FILE_EXIT):
					_CloseFile();
					Close();
					break;

				case nameof(IDM_FILE_CLOSEFILE):
					_CloseFile();
					break;
			}
		}

		private void _OnDestroy(object sender, FormClosedEventArgs e)
		{
			// Drag and drop shut down automatically
		}

		private void _OnInitDlg(object sender, EventArgs e)
		{
			IDC_INFO.Text = c_szInstructions;
			// Drag and drop initialized automatically
		}

		private void _OnOpenFile()
		{
			var ofn = new OpenFileDialog { Filter = "All Files|*.*" };
			if (ofn.ShowDialog(this) == DialogResult.OK)
			{
				// Open the file that was selected in the Open File dialog
				_OpenFile(ofn.FileName);
			}
		}

		private void _OpenFile(string pszPath)
		{
			// Close the file if it is already opened
			_CloseFile();
			// Initialize the IFileIsInUse object. We use some default flags here as an example. If you modify these you will notice Windows
			// Explorer modify its File In Use dialog contents accordingly to match the usage type and available capabilities.
			_pfiu = new CFileIsInUseImpl(Handle, pszPath, FILE_USAGE_TYPE.FUT_EDITING, OF_CAP.OF_CAP_CANCLOSE | OF_CAP.OF_CAP_CANSWITCHTO);
			_pfiu.CloseFile += (s, e) => _CloseFile();
			// The lack of FILE_SHARE_READ or FILE_SHARE_WRITE attributes for the dwShareMode parameter will cause the file to be locked from
			// other processes.
			_hFile = CreateFile(pszPath, Kernel32.FileAccess.FILE_GENERIC_READ, 0, null, FileMode.Open, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL | FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
			if (!_hFile.IsInvalid)
				IDC_INFO.Text = pszPath;
			else
				_pfiu = null;
		}
	}

	[ComVisible(true)]
	public class CFileIsInUseImpl : IFileIsInUse
	{
		private readonly OF_CAP _dwCapabilities;
		private uint _dwCookie;
		private readonly FILE_USAGE_TYPE _fut = FILE_USAGE_TYPE.FUT_GENERIC;
		private HWND _hwnd;
		private readonly string _szFilePath;

		public CFileIsInUseImpl(HWND hwnd, string pszFilePath, FILE_USAGE_TYPE fut, OF_CAP dwCapabilities)
		{
			_hwnd = hwnd;
			_szFilePath = pszFilePath;
			_fut = fut;
			_dwCapabilities = dwCapabilities;
			_AddFileToROT();
		}

		public event EventHandler CloseFile;

		HRESULT IFileIsInUse.CloseFile()
		{
			CloseFile?.Invoke(this, EventArgs.Empty);
			_RemoveFileFromROT();
			return HRESULT.S_OK;
		}

		HRESULT IFileIsInUse.GetAppName(out string ppszName)
		{
			ppszName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
			return HRESULT.S_OK;
		}

		HRESULT IFileIsInUse.GetCapabilities(out OF_CAP pdwCapFlags)
		{
			pdwCapFlags = _dwCapabilities;
			return HRESULT.S_OK;
		}

		HRESULT IFileIsInUse.GetSwitchToHWND(out HWND phwnd)
		{
			phwnd = _hwnd;
			return HRESULT.S_OK;
		}

		HRESULT IFileIsInUse.GetUsage(out FILE_USAGE_TYPE pfut)
		{
			pfut = _fut;
			return HRESULT.S_OK;
		}

		private void _AddFileToROT()
		{
			var hr = GetRunningObjectTable(0, out var prot);
			if (hr.Succeeded)
			{
				hr = CreateFileMoniker(_szFilePath, out var pmk);
				if (hr.Succeeded)
				{
					// Add ROTFLAGS_ALLOWANYCLIENT to make this work accross security boundaries
					try
					{
						_dwCookie = prot.Register(ROTFLAGS.ROTFLAGS_REGISTRATIONKEEPSALIVE | ROTFLAGS.ROTFLAGS_ALLOWANYCLIENT, this, pmk);
					}
					catch
					{
						// this failure is due to ROTFLAGS_ALLOWANYCLIENT and the fact that we don't have the AppID registered for our CLSID.
						// Try again without ROTFLAGS_ALLOWANYCLIENT knowing that this means this can only work in the scope of apps running
						// with the same MIC level.
						_dwCookie = prot.Register(ROTFLAGS.ROTFLAGS_REGISTRATIONKEEPSALIVE, this, pmk);
					}
				}
			}
		}

		private void _RemoveFileFromROT()
		{
			var hr = GetRunningObjectTable(0, out var prot);
			if (hr.Succeeded)
			{
				if (_dwCookie != 0)
				{
					prot.Revoke(_dwCookie);
					_dwCookie = 0;
				}
			}
		}
	}
}