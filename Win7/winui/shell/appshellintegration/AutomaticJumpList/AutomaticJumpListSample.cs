using System;
using System.Text;
using System.Windows.Forms;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static AutomaticJumpList.FileRegistrations;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace AutomaticJumpList
{
	public partial class AutomaticJumpListSample : Form
	{
		internal const string c_szTitle = "AutomaticJumpListSample";
		private const string c_szWindowClass = "AUTOMATICJUMPLISTSAMPLE";
		private const string REGPATH_SAMPLE = "Software\\Microsoft\\Samples\\AutomaticJumpListSample";
		private const string REGVAL_RECENTCATEGORY = "RecentCategorySelected";

		private static readonly string[] c_rgpszFiles =
		{
			"Microsoft_Sample_1.txt",
			"Microsoft_Sample_2.txt",
			"Microsoft_Sample_3.doc",
			"Microsoft_Sample_4.doc"
		};

		public AutomaticJumpListSample()
		{
			InitializeComponent();

			// From wWinMain
			var mem = SafeCoTaskMemHandle.CreateFromStructure<uint>();
			var memSz = (uint)mem.Size;
			SetCategory(SHGetValue(HKEY.HKEY_CURRENT_USER, REGPATH_SAMPLE, REGVAL_RECENTCATEGORY, out var _, mem, ref memSz).Succeeded);
		}

		// Cleans up the sample files that were created in the current user's Documents directory
		private static void CleanupSampleFiles()
		{
			var hr = SHGetKnownFolderPath(KNOWNFOLDERID.FOLDERID_Documents.Guid(), KNOWN_FOLDER_FLAG.KF_FLAG_CREATE, default, out var pszPathDocuments);
			if (hr.Succeeded)
			{
				// Don't abort the loop if we fail to cleanup a file, we still want to try to clean up the rest
				foreach (var fn in c_rgpszFiles)
				{
					var szPathSample = new StringBuilder(MAX_PATH);
					if (PathCombine(szPathSample, pszPathDocuments, fn) != IntPtr.Zero)
					{
						DeleteFile(szPathSample.ToString());
					}
				}
			}
		}

		// Removes all items in the automatic Jump List for the calling application, except for items the user has pinned to the Jump List.
		// The list of pinned items is not accessible to applications.
		private static void ClearHistory()
		{
			var pad = new IApplicationDestinations();
			pad.RemoveAllDestinations();
		}

		// Creates a set of sample files in the current user's Documents directory to use as items in the custom category inserted into the
		// Jump List.
		private static HRESULT CreateSampleFiles()
		{
			var hr = SHGetKnownFolderPath(KNOWNFOLDERID.FOLDERID_Documents.Guid(), KNOWN_FOLDER_FLAG.KF_FLAG_CREATE, default, out var pszPathDocuments);
			if (hr.Succeeded)
			{
				foreach (var fn in c_rgpszFiles)
				{
					var szPathSample = new StringBuilder(MAX_PATH);
					if (PathCombine(szPathSample, pszPathDocuments, fn) != IntPtr.Zero)
					{
						hr = SHCreateStreamOnFileEx(szPathSample.ToString(), STGM.STGM_WRITE | STGM.STGM_FAILIFTHERE, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, true, null, out var pstm);
						if (hr.Succeeded)
						{
							hr = IStream_WriteStr(pstm, "This is a sample file for the CustomJumpListSample.\r\n");
						}
						else if (((Win32Error)Win32Error.ERROR_FILE_EXISTS).ToHRESULT() == hr)
						{
							// If the file exists, we're ok, we'll just reuse it
							hr = HRESULT.S_OK;
						}
					}
				}
			}
			return hr;
		}

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (!AreFileTypesRegistered())
			{
				string pszMessage;
				var hr = RegisterToHandleFileTypes();
				if (hr == HRESULT.E_ACCESSDENIED)
				{
					pszMessage = "Please relaunch this application as an administrator to register for the required file types.";
				}
				else if (hr.Failed)
				{
					pszMessage = "Unable to register the required file types.";
				}
				else
				{
					pszMessage = "The required file types were successfully registered.";
				}
				MessageBox.Show(pszMessage, c_szTitle);
			}

			if (CreateSampleFiles().Failed)
			{
				MessageBox.Show("Unable to create the sample files.", c_szTitle);
			}

			if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
			{
				MessageBox.Show(args[0], c_szTitle);

				// If available, it is preferable to pass an IShellItem (SHARD_SHELLITEM) or IDList (SHARD_PIDL), as they can represent items
				// that do not have a file path. However, parsing a path to produce an IShellItem or IDList will incur I/O unnecessarily for
				// the calling application. Instead, if only a path is available, pass it to SHAddToRecentDocs, to avoid the extraneous
				// parsing work.
				SHAddToRecentDocs(SHARD.SHARD_PATHW, args[0]);
			}

			Application.Run(new AutomaticJumpListSample());
		}

		// Selects an item using the common File Open dialog, to simulate opening a document, an operation that should result in the selected
		// item being added to the application's automatic Jump List.
		private static void OpenItem(HWND hwnd)
		{
			var pdlg = new IFileOpenDialog();
			COMDLG_FILTERSPEC[] c_rgTypes = { new COMDLG_FILTERSPEC { pszName = "Sample File Types (*.txt;*.doc)", pszSpec = "*.txt;*.doc" } };
			pdlg.SetFileTypes((uint)c_rgTypes.Length, c_rgTypes);
			pdlg.SetFileTypeIndex(1);
			// Start in the Documents folder, where the sample files were created
			var hr = SHCreateItemInKnownFolder(KNOWNFOLDERID.FOLDERID_Documents.Guid(), KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, null, typeof(IShellItem).GUID, out var ppv);
			if (hr.Succeeded)
			{
				var psiFolder = (IShellItem)ppv;
				pdlg.SetDefaultFolder(psiFolder);
				hr = pdlg.Show(hwnd);
				if (hr.Succeeded)
				{
					var psi = pdlg.GetResult();
					// Unless FOS_DONTADDTORECENT is set via IFileDialog(and derivatives)::SetOptions prior to calling IFileDialog::Show, the
					// common File Open/Save dialogs will call SHAddToRecentDocs on the application's behalf. However, it is preferred that
					// applications manually call this method in all locations where documents are opened via user action, to ensure no
					// documents are left out of the Jump List. Jump Lists handle the duplicate calls so that items are not improperly
					// promoted in the Recent or Frequent lists when their usage is reported many times in rapid succession.
					SHAddToRecentDocs(SHARD.SHARD_SHELLITEM, psi);
				}
			}
		}

		// Sets the Known Category (Frequent or Recent) that is displayed in the Jump List for this application. Document creation
		// applications are typically best served by the Recent category, while media consumption applications usually utilize the Frequent
		// category. Applications should never request that BOTH categories appear in the same Jump List, as the two categories may present
		// duplicates of each other.
		private static void SetKnownCategory(bool fRecentSelected)
		{
			// The visible categories are controlled via the ICustomDestinationList interface. If not customized, applications will get the
			// Recent category by default.
			var pcdl = new ICustomDestinationList();
			// The cMinSlots and poaRemoved values can be ignored when only a Known Category is being added - those parameters apply only to
			// applications adding custom categories or tasks to the Jump List.s
			var poaRemoved = pcdl.BeginList<IObjectArray>(out var cMinSlots);
			// Adds a known category, which is filled with items collected for the automatic Jump List. If an application also adds other
			// custom categories (see the CustomJumpList sample), the categories are displayed in the order they are appended to the list.
			// When combining custom categories with known categories, duplicates are not removed, so applications should only provide items
			// in custom categories that will not appear in the known categories.
			pcdl.AppendKnownCategory(fRecentSelected ? KNOWNDESTCATEGORY.KDC_RECENT : KNOWNDESTCATEGORY.KDC_FREQUENT);
			pcdl.CommitList();
		}

		private void IDM_CATEGORY_FREQUENT_Click(object sender, EventArgs e) => SetCategory(false);

		private void IDM_CATEGORY_RECENT_Click(object sender, EventArgs e) => SetCategory(true);

		private void IDM_EXIT_Click(object sender, EventArgs e) => Close();

		private void IDM_FILE_CLEARHISTORY_Click(object sender, EventArgs e) => ClearHistory();

		private void IDM_FILE_DEREGISTERFILETYPES_Click(object sender, EventArgs e)
		{
			CleanupSampleFiles();

			string pszMessage;
			var hr = UnRegisterFileTypeHandlers();
			if (HRESULT.E_ACCESSDENIED == hr)
			{
				pszMessage = "Please run this application as an administrator to remove file type registrations.";
			}
			else if (hr.Failed)
			{
				pszMessage = "Unable to remove file type registrations.";
			}
			else
			{
				pszMessage = "File type registrations were successfully removed.";
			}
			MessageBox.Show(this, pszMessage, c_szTitle);
		}

		private void IDM_FILE_OPEN_Click(object sender, EventArgs e) => OpenItem(Handle);

		// Sets the visible known category and updates the menu to reflect the current state
		private void SetCategory(bool fRecentSelected)
		{
			SetKnownCategory(fRecentSelected);
			IDM_CATEGORY_RECENT.Checked = fRecentSelected;
			IDM_CATEGORY_FREQUENT.Checked = !fRecentSelected;
			var mem = SafeCoTaskMemHandle.CreateFromStructure(fRecentSelected ? 1U : 0U);
			SHSetValue(HKEY.HKEY_CURRENT_USER, REGPATH_SAMPLE, REGVAL_RECENTCATEGORY, REG_VALUE_TYPE.REG_DWORD, (IntPtr)mem, (uint)mem.Size);
		}
	}
}