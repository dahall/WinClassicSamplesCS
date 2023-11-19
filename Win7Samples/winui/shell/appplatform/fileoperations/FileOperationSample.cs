using System.IO;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace fileoperations;

internal class Program
{
	private const uint c_cMaxFilesToCreate = 10;
	private const string c_szSampleDstDir = "FileOpSampleDestination";
	private const string c_szSampleFileExt = "txt";
	private const string c_szSampleFileName = "SampleFile";
	private const string c_szSampleFileNewname = "NewName";
	private const string c_szSampleSrcDir = "FileOpSampleSource";

	// Synopsis:  This example creates multiple files under the specified folder path and copies them to the same directory with a new name.
	//
	// Arguments: psiSrc - Source folder item psiDst - Destination folder item
	//
	// Returns:   S_OK if successful
	private static void CopyMultipleFiles(IShellItem psiSrc, IShellItem psiDst)
	{
		// Create the IFileOperation object
		var pfo = CreateAndInitializeFileOperation();
		var psiaSampleFiles = CreateShellItemArrayOfSampleFiles(psiSrc);
		pfo.CopyItems(psiaSampleFiles, psiDst);
		pfo.PerformOperations();
	}

	// Synopsis:  This example copies a single item from the sample source folder to the sample dest folder using a new item name.
	//
	// Arguments: psiSrc - Source folder item psiDst - Destination folder item
	//
	// Returns:   S_OK if successful
	private static void CopySingleFile(IShellItem psiSrc, IShellItem psiDst)
	{
		// Create the IFileOperation object
		var pfo = CreateAndInitializeFileOperation();
		var szSampleFileName = string.Format("{0}.{1}", c_szSampleFileName, c_szSampleFileExt);
		var psiSrcFile = SHCreateItemFromRelativeName<IShellItem>(psiSrc, szSampleFileName);
		var szNewName = string.Format("{0}.{1}", c_szSampleFileNewname, c_szSampleFileExt);
		pfo.CopyItem(psiSrcFile, psiDst, szNewName, null);
		pfo.PerformOperations();
	}

	private static IFileOperation CreateAndInitializeFileOperation()
	{
		// Create the IFileOperation object
		var pfo = new IFileOperation();
		// Set the operation flags. Turn off all UI from being shown to the user during the operation. This includes error, confirmation
		// and progress dialogs.
		pfo.SetOperationFlags(FILEOP_FLAGS.FOF_SILENT | FILEOP_FLAGS.FOF_NOCONFIRMATION | FILEOP_FLAGS.FOF_NOERRORUI | FILEOP_FLAGS.FOF_NOCONFIRMMKDIR);
		return pfo;
	}

	// Synopsis:  Creates all of the files needed by this sample the requested known folder
	//
	// Arguments: psiFolder - Folder that will contain the sample files
	//
	// Returns:   S_OK if successful
	private static void CreateSampleFiles(IShellItem psiFolder)
	{
		var pfo = CreateAndInitializeFileOperation();
		var szSampleFileName = string.Format("{0}.{1}", c_szSampleFileName, c_szSampleFileExt);
		// the file to be used for the single copy sample
		pfo.NewItem(psiFolder, FileAttributes.Normal, szSampleFileName, null, null);
		// the files to be used for the multiple copy sample
		for (var i = 0; i < c_cMaxFilesToCreate; i++)
		{
			szSampleFileName = string.Format("{0}{1}.{2}", c_szSampleFileName, i, c_szSampleFileExt);
			pfo.NewItem(psiFolder, FileAttributes.Normal, szSampleFileName, null, null);
		}
		pfo.PerformOperations();
	}

	// Synopsis:  Create the source and destination folders for the sample
	//
	// Arguments: psiSampleRoot - Item of the parent folder where the sample folders will be created ppsiSampleSrc - On success contains
	// the source folder item to be used for sample operations ppsiSampleDst - On success contains the destination folder item to be used
	// for sample operations
	//
	// Returns:   S_OK if successful
	private static void CreateSampleFolders(IShellItem psiSampleRoot, out IShellItem ppsiSampleSrc, out IShellItem ppsiSampleDst)
	{
		var pfo = CreateAndInitializeFileOperation();
		// Use the file operation to create a source and destination folder
		pfo.NewItem(psiSampleRoot, FileAttributes.Directory, c_szSampleSrcDir, null, null);
		pfo.NewItem(psiSampleRoot, FileAttributes.Directory, c_szSampleDstDir, null, null);
		pfo.PerformOperations();
		// Now that the folders have been created, create items for them. This is just an optimization so that the sample does not have
		// to rebind to these items for each sample type.
		ppsiSampleSrc = SHCreateItemFromRelativeName<IShellItem>(psiSampleRoot, c_szSampleSrcDir);
		ppsiSampleDst = SHCreateItemFromRelativeName<IShellItem>(psiSampleRoot, c_szSampleDstDir);
	}

	// Synopsis:  Creates an IShellItemArray containing the sample files to be used in the CopyMultipleFiles sample
	//
	// Arguments: psiSrc - Source folder item
	//
	// Returns:   S_OK if successful
	private static IShellItemArray CreateShellItemArrayOfSampleFiles(IShellItem psiSrc)
	{
		var psfSampleSrc = psiSrc.BindToHandler<IShellFolder>(null, BHID.BHID_SFObject.Guid());
		var rgpidlChildren = new PIDL[c_cMaxFilesToCreate];
		try
		{
			for (var i = 0; i < rgpidlChildren.Length; i++)
			{
				var szSampleFileName = string.Format("{0}{1}.{2}", c_szSampleFileName, i, c_szSampleFileExt);
				SFGAO attr = 0;
				psfSampleSrc.ParseDisplayName(default, null, szSampleFileName, out _, out rgpidlChildren[i], ref attr);
			}
			SHCreateShellItemArray(IntPtr.Zero, psfSampleSrc, c_cMaxFilesToCreate, rgpidlChildren.Select(p => p.DangerousGetHandle()).ToArray(), out var psia).ThrowIfFailed();
			return psia;
		}
		finally
		{
			for (var i = 0; i < rgpidlChildren.Length; i++)
				if (rgpidlChildren[i] != null)
					rgpidlChildren[i].Dispose();
		}
	}

	// Synopsis:  Deletes the files/folders created by this sample
	//
	// Arguments: psiSrc - Source folder item psiDst - Destination folder item
	//
	// Returns:   S_OK if successful
	private static void DeleteSampleFiles(IShellItem psiSrc, IShellItem psiDst)
	{
		var pfo = CreateAndInitializeFileOperation();
		pfo.DeleteItem(psiSrc, null);
		pfo.DeleteItem(psiDst, null);
		pfo.PerformOperations();
	}

	private static void Main()
	{
		// Get the documents known folder. This folder will be used to create subfolders for the sample source and destination
		var psiDocuments = SHCreateItemInKnownFolder<IShellItem>(KNOWNFOLDERID.FOLDERID_Documents, KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT_PATH);
		CreateSampleFolders(psiDocuments, out var psiSampleSrc, out var psiSampleDst);
		CreateSampleFiles(psiSampleSrc);
		CopySingleFile(psiSampleSrc, psiSampleDst);
		CopyMultipleFiles(psiSampleSrc, psiSampleDst);
		DeleteSampleFiles(psiSampleSrc, psiSampleDst);
	}
}