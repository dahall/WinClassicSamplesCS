using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.Windows.Forms;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

namespace ShellStorage;

internal class Program
{
	private const int IDC_DOCUMENTSLIBRARY = 101;
	private const int IDC_FOLDERPICKER = 102;
	private const int IDC_SHBROWSEFORFOLDER = 103;
	private const int IDC_FILEDIALOGITEM = 104;
	private const string c_szSampleFolderName = "ShellStorageSample";
	private const string c_szSampleFileName = "ShellStorageSample.txt";
	private const string c_szSampleFileContents = "This sample file created by the ShellStorage SDK sample";

	private static void CreateFileInContainer(IShellItem psi, string pszFileName, string pszContents)
	{
		using ComReleaser<IStorage> pstorage = new(psi.BindToHandler<IStorage>(default, BHID.BHID_Storage));
		using ComReleaser<IStream> pstream = new(pstorage.Item.CreateStream(pszFileName, STGM.STGM_READWRITE | STGM.STGM_SHARE_EXCLUSIVE | STGM.STGM_CREATE));
		var bytes = pszContents.GetBytes();
		pstream.Item.Write(bytes, bytes.Length, default);
		pstream.Item.Commit((int)STGC.STGC_OVERWRITE);
	}

	// Same as above but creates a folder
	private static void CreateFolderInContainer(IShellItem psi, string pszFolderName)
	{
		using ComReleaser<IStorage> pstorage = new(psi.BindToHandler<IStorage>(default, BHID.BHID_Storage));
		using ComReleaser<IStream> pnewstorage = new(pstorage.Item.CreateStorage(pszFolderName, STGM.STGM_READWRITE | STGM.STGM_SHARE_EXCLUSIVE | STGM.STGM_CREATE));
		pnewstorage.Item.Commit((int)STGC.STGC_OVERWRITE);
	}

	private static IBindCtx CreateBindCtxWithMode(STGM grfMode)
	{
		CreateBindCtx(0, out var ppbc).ThrowIfFailed();
		BIND_OPTS boptions = new() { cbStruct = Marshal.SizeOf(typeof(BIND_OPTS)), grfMode = (int)grfMode };
		ppbc!.SetBindOptions(ref boptions);
		return ppbc;
	}

	// Writes to a given file
	private static void CreateFileFromItem(IShellItem psi, string pszContents)
	{
		using ComReleaser<IBindCtx> pbc = new(CreateBindCtxWithMode(STGM.STGM_READWRITE | STGM.STGM_SHARE_EXCLUSIVE | STGM.STGM_CREATE));
		using ComReleaser<IStream> pstream = new(psi.BindToHandler<IStream>(pbc.Item, BHID.BHID_Stream));
		var bytes = pszContents.GetBytes();
		pstream.Item.Write(bytes, bytes.Length, default);
		pstream.Item.Commit((int)STGC.STGC_OVERWRITE);
	}

	private static void ExportToDocumentsLibrary()
	{
		using ComReleaser<IShellItem> psi = new(SHCreateItemInKnownFolder<IShellItem>(KNOWNFOLDERID.FOLDERID_DocumentsLibrary) ?? SHCreateItemInKnownFolder<IShellItem>(KNOWNFOLDERID.FOLDERID_Documents)!);
		CreateFileInContainer(psi.Item, c_szSampleFileName, c_szSampleFileContents);
		CreateFolderInContainer(psi.Item, c_szSampleFolderName);
	}

	private static void ExportToFolderPicker()
	{
		using ComReleaser<IFileOpenDialog> pfod = new(new());
		pfod.Item.SetOptions(FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);
		if (pfod.Item.Show().Succeeded)
		{
			using ComReleaser<IShellItem> psi = new(pfod.Item.GetResult());
			CreateFileInContainer(psi.Item, c_szSampleFileName, c_szSampleFileContents);
			CreateFolderInContainer(psi.Item, c_szSampleFolderName);
		}
	}

	private static void ExportToSHBrowseForFolder()
	{
		using PIDL pidl = SHBrowseForFolder(new BROWSEINFO() { ulFlags = BrowseInfoFlag.BIF_USENEWUI });
		if (!pidl.IsNull)
		{
			using ComReleaser<IShellItem> psi = new(SHCreateItemFromIDList<IShellItem>(pidl)!);
			CreateFileInContainer(psi.Item, c_szSampleFileName, c_szSampleFileContents);
			CreateFolderInContainer(psi.Item, c_szSampleFolderName);
		}
	}

	private static void ExportToFileDialogItem()
	{
		using ComReleaser<IFileSaveDialog> pfsd = new(new());
		pfsd.Item.SetFileName(c_szSampleFileName);
		COMDLG_FILTERSPEC[] rgSaveTypes =
		[
			new() { pszName = "Text Documents", pszSpec = "*.txt" },
			new() { pszName = "All Files", pszSpec = "*.*" },
		];

		pfsd.Item.SetFileTypes((uint)rgSaveTypes.Length, rgSaveTypes);
		if (pfsd.Item.Show().Succeeded)
		{
			using ComReleaser<IShellItem> psi = new(pfsd.Item.GetResult());
			CreateFileFromItem(psi.Item, c_szSampleFileContents);
		}
	}

	private static void Main()
	{
		TaskDialog taskDialog = new()
		{
			AllowDialogCancellation = true,
			CommonButtons = TaskDialogCommonButtons.Close,
			MainInstruction = "Select where to create items",
			WindowTitle = "Shell Storage Sample"
		};
		taskDialog.Buttons.AddRange([
			// For Vista save in the Documents folder
			new TaskDialogButton("Documents Library" , IDC_DOCUMENTSLIBRARY),
			new TaskDialogButton("Pick using Folder Picker..." , IDC_FOLDERPICKER),
			new TaskDialogButton("Pick using SHBrowseForFolder..." , IDC_SHBROWSEFORFOLDER),
			new TaskDialogButton("Create one item using save dialog...", IDC_FILEDIALOGITEM),
		]);

		bool fDone = false;
		while (!fDone)
		{
			if (taskDialog.ShowDialog() is System.Windows.Forms.DialogResult.OK or System.Windows.Forms.DialogResult.Cancel)
			{
				fDone = true;
			}
			else if (taskDialog.Result.DialogResult == IDC_DOCUMENTSLIBRARY)
			{
				ExportToDocumentsLibrary();
			}
			else if (taskDialog.Result.DialogResult == IDC_FOLDERPICKER)
			{
				ExportToFolderPicker();
			}
			else if (taskDialog.Result.DialogResult == IDC_SHBROWSEFORFOLDER)
			{
				ExportToSHBrowseForFolder();
			}
			else if (taskDialog.Result.DialogResult == IDC_FILEDIALOGITEM)
			{
				ExportToFileDialogItem();
			}
		}
	}
}