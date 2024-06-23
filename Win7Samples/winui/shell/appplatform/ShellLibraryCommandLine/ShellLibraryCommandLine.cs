#pragma warning disable IDE1006 // Naming Styles

using CmdLine;
using System.IO;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.FunctionHelper;
using static Vanara.PInvoke.Shell32;

namespace ShellLibCmdLine;

internal class Program
{
	// main function - sets up a 'meta command' to contain each of the library commands, and executes it on the given arguments
	private static void Main(string[] args)
	{
		string pszExeName = Path.GetFileNameWithoutExtension(Environment.ProcessPath)!;
		CMetaCommand main = new(pszExeName, "Displays and modifies the attributes of Shell Libraries.", [
			CMetaCommand.Create<CCreateCommand>,
			CMetaCommand.Create<CInfoCommand>,
			CMetaCommand.Create<CEnumCommand>,
			CMetaCommand.Create<CSetAttributeCommand>,
			CMetaCommand.Create<CAddCommand>,
			CMetaCommand.Create<CRemoveCommand>,
			CMetaCommand.Create<CSetDefaultSaveFolderCommand>,
			CMetaCommand.Create<CResolveCommand>,
			CMetaCommand.Create<CResolveAllCommand>,
			CMetaCommand.Create<CManageCommand>,
		]);
		main.Execute(args);
	}

	// CShlibCommandBase - Base class for commands in shlib.exe
	//
	// This class provides functionality that is shared across all commands, in particular:
	// - Processing the <Library> argument to create an IShellItem for the specified item (file system path or KNOWNFOLDERID)
	// - Loading the IShellLibrary interface for the specified item
	// - Committing changes after the operation is complete
	// - Options for specifying the creation disposition
	private abstract class CShlibCommandBase : CCmdBase
	{
		protected bool fCreate;
		protected IShellLibrary? plib = null;
		protected IShellItem? psiLibrary = null; // The IShellItem representing the library; may be default if fCreate is true.
												 // The IShellLibrary loaded for the specified item; initialized by v_ExecuteCommand prior to
												 // calling v_ExecuteLibCommand. Indicates whether the user specified to create a new library.

		private readonly bool fReadOnly; // Indicates that the library should be initialized for read-only access; specified by the derived class.
		private LIBRARYSAVEFLAGS lsfSaveOptions = LIBRARYSAVEFLAGS.LSF_FAILIFTHERE; // Specifies the creation disposition; provided by the user via the -create option.
		private string? pszSavePath = null; // Specifies the path to save the newly-created library to; may be default if fCreate is false.

		public CShlibCommandBase(string pszName, string pszDescription, bool fReadOnly = true, bool fCreate = false) :
			base(pszName, pszDescription, "<Library> [...]")
		{
			this.fReadOnly = fReadOnly;
			this.fCreate = fCreate;

			// Options for specifying the creation disposition.
			ARGENTRY<LIBRARYSAVEFLAGS>[] c_rgLibSaveFlags =
			[
				new( "", LIBRARYSAVEFLAGS.LSF_FAILIFTHERE, "Fail if the library already exists." ),
				new( "overwrite", LIBRARYSAVEFLAGS.LSF_OVERRIDEEXISTING, "Overwrite any existing library." ),
				new( "uniquename", LIBRARYSAVEFLAGS.LSF_MAKEUNIQUENAME, "Generate a unique name in case of conflict." ),
			];

			if (!fReadOnly)
			{
				AddEnumOptionHandler("create", "creation flag", "Specifies that a new library should be created.",
					SetCreateFlags, c_rgLibSaveFlags);
			}
		}

		~CShlibCommandBase()
		{
			if (psiLibrary is not null)
			{
				Marshal.ReleaseComObject(psiLibrary);
			}
			if (plib is not null)
			{
				Marshal.ReleaseComObject(plib);
			}
		}

		// Loads the IShellLibrary interface for the specified item, calls the derived class to perform an operation on the library, and
		// commits/saves any changes as needed.
		protected override HRESULT v_ExecuteCommand()
		{
			HRESULT hr = 0;
			try
			{
				if (fCreate)
				{
					// If we're in 'create' mode, instantiate a new IShellLibrary in memory.
					plib = IidGetObj<IShellLibrary>(SHCreateLibrary);
				}
				else
				{
					// Otherwise, load it from the specified IShellItem.
					STGM grfMode = fReadOnly ? (STGM.STGM_READ | STGM.STGM_SHARE_DENY_WRITE) : (STGM.STGM_READWRITE | STGM.STGM_SHARE_EXCLUSIVE);
					plib = IidGetObj<IShellLibrary>((in Guid g, out object? o) => SHLoadLibraryFromItem(psiLibrary!, grfMode, g, out o));
				}
			}
			catch (Exception ex) { hr = ex.HResult; }

			if (hr.Succeeded)
			{
				// Call the derived class to execute the operation on the library.
				hr = v_ExecuteLibCommand();
				if (hr.Succeeded && !fReadOnly)
				{
					if (fCreate)
					{
						// We created a new library in memory; now save it to disk. The IShellLibrary::Save API takes the destination in the
						// form of the parent folder, and the name of the library (without any file extension). However, the argument is in
						// the form of a full file system path, possibly including the extension. So, we need to parse it into that form. For
						// example: "C:\some\folder\stuff.library-ms" => "C:\some\folder", "stuff"
						string pszName = Path.GetFileName(pszSavePath)!;
						if (string.Compare(Path.GetExtension(pszName), ".library-ms", StringComparison.InvariantCultureIgnoreCase) == 0)
						{
							pszName = Path.GetFileNameWithoutExtension(pszName);
						}
						pszSavePath = Path.GetFileNameWithoutExtension(pszSavePath);

						// Save the library with the specified name in the specified folder.
						hr = SHSaveLibraryInFolderPath(plib!, pszSavePath!, pszName, lsfSaveOptions, out var pszSavedToPath);
						if (hr.Succeeded)
						{
							// The API returns the full file system path that the library was saved to. (This may or may not match the
							// original argument, depending on whether LSF_MAKEUNIQUENAME was specified.)
							Output("Library saved to path: {0}\n", pszSavedToPath);
						}
						else
						{
							RuntimeError("Error {0:x8} saving library to path: {1}\\{2}.library-ms\n", hr, pszSavePath, pszName);
						}
					}
					else
					{
						// We're operating on an existing library; commit the changes to disk.
						plib!.Commit();
						Output("Changes successfully committed.\n");
					}
				}
			}
			else
			{
				RuntimeError("Error {0:x8} loading library from path: {1}\n", hr, pszSavePath);
			}
			return hr;
		}

		protected virtual HRESULT v_ExecuteLibCommand() => HRESULT.S_OK;

		protected override void v_PrintInstructions()
		{
			Output("The library may be specified by a file system path, or by a KNOWNFOLDERID (e.g. \"FOLDERID_DocumentsLibrary\").\n");
			v_PrintLibInstructions(); // Print additional instructions specified by the derived class.
		}

		protected virtual void v_PrintLibInstructions()
		{ }

		// Processes a single argument which identifies the library to operate on; passes any remaining arguments to the derived class.
		protected override HRESULT v_ProcessArguments(string[] ppszArgs)
		{
			string? pszLibPath = ppszArgs.Length > 0 ? ppszArgs[0] : null;
			HRESULT hr = pszLibPath is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
			if (hr.Succeeded)
			{
				if (fCreate)
				{
					// When creating a new library, interpret the argument as the file system path to save the library to.
					pszSavePath = Path.GetFullPath(pszLibPath!);
				}
				else
				{
					// Check for the 'FOLDERID_' prefix, which indicates that the argument should be interpreted as a KNOWNFOLDERID.
					try
					{
						const string szPrefix = "FOLDERID_";
						if (pszLibPath!.StartsWith(szPrefix))
						{
							IKnownFolderManager pkfm = new();
							// KNOWNFOLDERIDs are GUIDs, but they have a corresponding canonical name which is a string. By convention, the
							// canonical name is the same as the name of the KNOWNFOLDERID #define. That is, FOLDERID_DocumentsLibrary =>
							// "DocumentsLibrary". So, skip the prefix and pass the remainder to GetFolderByName to retrieve the known folder.
							IKnownFolder pkf = pkfm.GetFolderByName(pszLibPath.Substring(szPrefix.Length));
							psiLibrary = pkf.GetShellItem<IShellItem>(KNOWN_FOLDER_FLAG.KF_FLAG_INIT);
							Marshal.ReleaseComObject(pkf);
							Marshal.ReleaseComObject(pkfm);
						}
						else
						{
							// Default - interpret the argument as a file system path, and create a shell item for it.
							psiLibrary = SHCreateItemFromParsingName<IShellItem>(Path.GetFullPath(pszLibPath));
						}
					}
					catch (Exception ex) { hr = ex.HResult; }
				}
			}
			else
			{
				ParseError("Missing library path argument.\n");
			}

			if (hr.Succeeded)
			{
				// Allow derived command to process any remaining arguments.
				hr = v_ProcessLibArguments(ppszArgs.Skip(1).ToArray());
			}
			return hr;
		}

		// derived classes override these functions to process arguments, execute operations on the library, and print usage instructions
		protected virtual HRESULT v_ProcessLibArguments(string[] args) => HRESULT.S_OK;

		// Called by the option processor in CCmdBase to set the creation disposition (if specified by the -create option).
		private HRESULT SetCreateFlags(LIBRARYSAVEFLAGS lsfSaveOptions)
		{
			fCreate = true;
			this.lsfSaveOptions = lsfSaveOptions;
			return HRESULT.S_OK;
		}
	}

	// CAddCommand - Adds the specified folder to the library.
	//
	// This command uses the AddFolder method of IShellLibrary to add a folder to the library.  The folder is specified
	// as the second argument, as per the usage of CFolderCommandBase.
	private class CAddCommand : CFolderCommandBase
	{
		public CAddCommand() : base("add", "Adds the specified folder to the specified library.")
		{
		}

		protected override HRESULT v_ExecuteLibCommand()
		{
			try { plib!.AddFolder(psiFolder!); }
			catch (Exception ex)
			{
				RuntimeError("Error {0:x8} adding folder {1} to the library.\n", ex.HResult, pszFolderPath);
				return ex.HResult;
			}
			return 0;
		}
	}

	// CCreateCommand - Creates a new library with the specified path/name.
	//
	// This simple command hard-codes the -create option (although the user can still specify it to indicate the creation disposition to use)
	// and performs no operations on the library. The result is that a new, empty library is created. CEnumCommand - Enumeates the locations
	// included in the specified library.
	private class CCreateCommand : CShlibCommandBase
	{
		public CCreateCommand() : base("create", "Creates a library at the specified path.", false, true)
		{
		}

		protected override HRESULT v_ExecuteLibCommand() => HRESULT.S_OK;
	}

	// CEnumCommand - Enumeates the locations included in the specified library.
	//
	// The 'enum' command uses the IShellLibrary::GetFolders method to list the locations that are included in the library.
	// It has a single option, which is used to specify the LIBRARYFOLDERFILTER enum to pass to the API to select what folders
	// to filter out of the list (if any).
	//
	// This class also serves as the base class for the 'info' command, since that command simply builds on the functionality of this one.
	private class CEnumCommand : CShlibCommandBase
	{
		private LIBRARYFOLDERFILTER lffFilter; // The filter to apply to folders in the library.

		public CEnumCommand() : this("enum", "Enumerates the folders in the library.")
		{
		}

		public CEnumCommand(string pszName, string pszDescription) : base(pszName, pszDescription)
		{
			lffFilter = LIBRARYFOLDERFILTER.LFF_ALLITEMS;

			ARGENTRY<LIBRARYFOLDERFILTER>[] c_rgLibFolderFilters =
			[
				new("allitems", LIBRARYFOLDERFILTER.LFF_ALLITEMS, "Include all items." ),
				new("all", LIBRARYFOLDERFILTER.LFF_ALLITEMS, "Synonym for 'allitems'." ),
				new("", LIBRARYFOLDERFILTER.LFF_ALLITEMS, "Synonym for 'allitems'." ),
				new("filesys", LIBRARYFOLDERFILTER.LFF_FORCEFILESYSTEM, "Include only file system items." ),
				new("fs", LIBRARYFOLDERFILTER.LFF_FORCEFILESYSTEM, "Synonym for 'filesys'." ),
				new("storage", LIBRARYFOLDERFILTER.LFF_STORAGEITEMS, "Include any IStorage-based item." ),
				new("stg", LIBRARYFOLDERFILTER.LFF_STORAGEITEMS, "Synonym for 'storage'." ),
			];

			AddEnumOptionHandler("filter", "folder filter", "Specifies which library locations to include in the enumeration.",
				SetFolderFilter, c_rgLibFolderFilters);
		}

		protected override HRESULT v_ExecuteLibCommand()
		{
			// Get the private and public save locations.
			try
			{
				IShellItem psiPrivateSaveLoc = plib!.GetDefaultSaveFolder<IShellItem>(DEFAULTSAVEFOLDERTYPE.DSFT_PRIVATE);
				IShellItem psiPublicSaveLoc = plib!.GetDefaultSaveFolder<IShellItem>(DEFAULTSAVEFOLDERTYPE.DSFT_PUBLIC);
				// Get the list of folders that match the specified filter.
				IShellItemArray psiaFolders = plib!.GetFolders<IShellItemArray>(lffFilter)!;
				uint cFolders = psiaFolders.GetCount();
				Output("Library contains {0} folders:\n", cFolders);
				for (uint iFolder = 0; iFolder < cFolders; iFolder++)
				{
					IShellItem psiFolder = psiaFolders.GetItemAt(iFolder);
					// Print each folder's name as an absolute path, suitable for parsing in the Shell Namespace (e.g SHParseDisplayName).
					// For file system folders (the typical case), this will be the file system path of the folder.
					string pszDisplay = psiFolder.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
					string pszPrefix = " ";
					if (HRESULT.S_OK == psiPrivateSaveLoc.Compare(psiFolder, SICHINTF.SICHINT_CANONICAL | SICHINTF.SICHINT_TEST_FILESYSPATH_IF_NOT_EQUAL, out _))
					{
						pszPrefix = "* ";
					}
					else if (HRESULT.S_OK == psiPublicSaveLoc.Compare(psiFolder, SICHINTF.SICHINT_CANONICAL | SICHINTF.SICHINT_TEST_FILESYSPATH_IF_NOT_EQUAL, out _))
					{
						pszPrefix = "# ";
					}

					Output("{0}{1}\n", pszPrefix, pszDisplay);
					Marshal.ReleaseComObject(psiFolder);
				}
				Marshal.ReleaseComObject(psiaFolders);
				Marshal.ReleaseComObject(psiPublicSaveLoc);
				Marshal.ReleaseComObject(psiPrivateSaveLoc);
				return HRESULT.S_OK;
			}
			catch (Exception ex) { return ex.HResult; }
		}

		protected override void v_PrintInstructions() => Output("The private and public default save locations are indicated in the output with the \'*\' and \'#\' symbols, respectively.\n");

		// Called by the option processor in CCmdBase to set the value specified by the -filter option (if any).
		private HRESULT SetFolderFilter(LIBRARYFOLDERFILTER lffFilter)
		{
			this.lffFilter = lffFilter;
			return HRESULT.S_OK;
		}
	}

	// CFolderCommandBase - Base class for commands that operate on folders within a library.
	//
	// This class handles interpreting the second argument as the path to a folder to operate on with respect to the library.
	// The specified folder is provided to derived commands via the protected member variables _psiFolder and/or _pszFolderPath.
	private class CFolderCommandBase : CShlibCommandBase
	{
		protected IShellItem? psiFolder;
		protected string? pszFolderPath;

		public CFolderCommandBase(string pszName, string pszDescription) : base(pszName, pszDescription, false)
		{
		}

		~CFolderCommandBase()
		{
			if (psiFolder is not null)
			{
				Marshal.ReleaseComObject(psiFolder);
			}
		}

		protected override void v_PrintLibInstructions() => Output("Specify the path of the folder to operate on after <Library>.\n");

		protected virtual HRESULT v_ProcessFolderArguments(string[] ppszArgs) => HRESULT.S_OK;

		// Interpret the next argument as a path, and obtain the IShellItem for it to be consumed by the derived class.
		protected override HRESULT v_ProcessLibArguments(string[] ppszArgs)
		{
			string? pszFolderPath = ppszArgs.Length > 0 ? ppszArgs[0] : null;
			HRESULT hr = pszFolderPath is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
			if (hr.Succeeded)
			{
				this.pszFolderPath = pszFolderPath;
				psiFolder = SHCreateItemFromParsingName<IShellItem>(pszFolderPath!);
			}
			else
			{
				ParseError("Missing folder path argument.\n");
			}
			// On success, pass any remaining arguments on to the derived class.
			return hr.Succeeded ? v_ProcessFolderArguments(ppszArgs.Skip(1).ToArray()) : hr;
		}
	}

	// CInfoCommand - Displays information about the specified library, including its options, folder type, icon, and locations.
	//
	// The 'info' command builds on the 'enum' command by outputting some additional information about the library that is exposed
	// by the IShellLibrary API.
	private class CInfoCommand : CEnumCommand
	{
		public CInfoCommand() : base("info", "Prints info about the given library.")
		{
		}

		protected override HRESULT v_ExecuteLibCommand()
		{
			// Display the option flags for the library (there is presently only one flag defined - LOF_PINNEDTONAVPANE)
			LIBRARYOPTIONFLAGS lofOptions = plib!.GetOptions();
			Output("Option flags: ");
			if (lofOptions == LIBRARYOPTIONFLAGS.LOF_DEFAULT)
			{
				Output("LOF_DEFAULT");
			}
			else if ((lofOptions & LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE) != 0)
			{
				Output("LOF_PINNEDTONAVPANE");
			}
			Output("\n");

			// Display the folder type of the library; see shlguid.h for a list of valid folder types.
			FOLDERTYPEID? ftid = plib!.GetFolderTypeId();
			string szFolderType = ftid?.ToString() ?? "<none>";
			Output("Folder type: {0}\n", szFolderType);

			// Display the path of the library's icon; this is also accessible via the Property System as PKEY_IconPath
			string pszIcon = plib.GetIcon();
			Output("Icon path: {0}\n", pszIcon ?? "<none>");

			// Call the super-class to enumerate and display the locations in the library.
			Output("\n");
			return base.v_ExecuteLibCommand();
		}
	}

	// CManageCommand - Displays the Manage Library Dialog for the library.
	//
	// The SHShowManageLibraryUI API provides an entry-point for an application to display UI for the user to view and modify
	// the settings of the library.  This command exposes that UI, and provides options to specify the title and instructions
	// text, as well as behavior options.
	private class CManageCommand : CShlibCommandBase
	{
		private LIBRARYMANAGEDIALOGOPTIONS lmdOptions = LIBRARYMANAGEDIALOGOPTIONS.LMD_DEFAULT;
		private string? pszInstructions = null;
		private string? pszTitle = null;

		public CManageCommand() : base("manage", "Displays the Manage Library Dialog for the library.")
		{
			// Options for the Manage Library Dialog UI.
			ARGENTRY<LIBRARYMANAGEDIALOGOPTIONS>[] c_rgLibManageDialogOptions =
			[
				new( "default", LIBRARYMANAGEDIALOGOPTIONS.LMD_DEFAULT, "Prevent un-indexable network locations from being added." ),
				new( "", LIBRARYMANAGEDIALOGOPTIONS.LMD_DEFAULT, "Synonym for 'default'" ),
				new( "allowslow", LIBRARYMANAGEDIALOGOPTIONS.LMD_ALLOWUNINDEXABLENETWORKLOCATIONS, "Allow un-indexable network locations to be added." ),
			];

			AddStringOptionHandler("title", "Sets the title of the dialog to ARG.", SetTitle);
			AddStringOptionHandler("instructions", "Sets the instructions text of the dialog to ARG.", SetInstructions);
			AddEnumOptionHandler("options", "dialog option", "Specifies options for controlling the dialog's behavior.", SetOptions, c_rgLibManageDialogOptions);
		}

		// Since we just need the IShellItem and not the IShellLibrary provided by CShlibCommandBase, override v_ExecuteCommand directly.
		protected override HRESULT v_ExecuteCommand()
		{
			HRESULT hr = SHShowManageLibraryUI(psiLibrary!, User32.GetDesktopWindow(), pszTitle, pszInstructions, lmdOptions);
			if (hr.Failed)
			{
				RuntimeError("Error {0:x8} returned from Manage Library Dialog.\n", hr);
			}
			return hr;
		}

		// This is not used since CShlibCommandBase::v_ExecuteCommand has been overridden directly.
		protected override HRESULT v_ExecuteLibCommand() => HRESULT.E_NOTIMPL;

		private HRESULT SetInstructions(string pszInstructions)
		{
			this.pszInstructions = pszInstructions;
			return HRESULT.S_OK;
		}

		private HRESULT SetOptions(LIBRARYMANAGEDIALOGOPTIONS lmdOptions)
		{
			this.lmdOptions = lmdOptions;
			return HRESULT.S_OK;
		}

		private HRESULT SetTitle(string pszTitle)
		{
			this.pszTitle = pszTitle;
			return HRESULT.S_OK;
		}
	}

	// CRemoveCommand - Removes the specified folder from the library.
	//
	// This command uses the RemoveFolder method of IShellLibrary to remove a folder from the library.  The folder is specified
	// as the second argument, as per the usage of CFolderCommandBase.
	private class CRemoveCommand : CFolderCommandBase
	{
		public CRemoveCommand() : base("remove", "Removes the specified folder from the library.")
		{
		}

		protected override HRESULT v_ExecuteLibCommand()
		{
			try { plib!.RemoveFolder(psiFolder!); }
			catch (Exception ex)
			{
				RuntimeError("Error {0:x8} removing folder {1} from the library.\n", ex.HResult, pszFolderPath);
				return ex.HResult;
			}
			return 0;
		}
	}

	// CResolveAllCommand - Resolves all of the locations in the library at once.
	//
	// This command uses the SHResolveLibrary API to execute a 'bulk resolve' of all of the locations in the library,
	// rather than iterating over each one returned from IShellLibrary::GetFolders and calling ResolveFolder.
	private class CResolveAllCommand : CShlibCommandBase
	{
		public CResolveAllCommand() : base("resolveall", "Resolves all locations in the library in bulk.")
		{
		}

		// Since we just need the IShellItem and not the IShellLibrary provided by CShlibCommandBase, override v_ExecuteCommand directly.
		protected override HRESULT v_ExecuteCommand()
		{
			HRESULT hr = SHResolveLibrary(psiLibrary!);
			if (hr.Succeeded)
			{
				Output("Resolution succeeded.\n");
			}
			else
			{
				RuntimeError("Error {0:x8} resolving the library.\n", hr);
			}
			return hr;
		}

		// This is not used since CShlibCommandBase::v_ExecuteCommand has been overridden directly.
		protected override HRESULT v_ExecuteLibCommand() => HRESULT.E_NOTIMPL;
	};

	// CResolveCommand - Resolves the specified folder in the library.
	//
	// This command uses the ResolveFolder method of IShellLibrary to resolve a folder in the library.  The folder is specified
	// as the second argument, as per the usage of CFolderCommandBase.  Libraries store references to their constituent locations
	// as Shell Links (shortcuts), and when the target is moved or renamed, the shortcut must be resolved to point to the new target.
	// This process is normally fast, but can be time consuming, so IShellLibrary::ResolveFolder includes a timeout parameter to
	// specify the maximum number of milliseconds to wait before aborting the resolution.
	private class CResolveCommand : CFolderCommandBase
	{
		private uint dwTimeout = 1000;

		public CResolveCommand() : base("resolve", "Resolves the specified folder in the library.") => AddStringOptionHandler("timeout", "Specifies the timeout value in milliseconds (defaults to 1000).", SetTimeout);

		protected override HRESULT v_ExecuteLibCommand()
		{
			// Attempt to resolve the folder. An IShellItem representing the updated target location is returned.
			try
			{
				IShellItem psiResolved = plib!.ResolveFolder<IShellItem>(psiFolder!, dwTimeout);
				string pszResolvedPath = psiResolved.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
				Output("Resolved folder path {0} to: {1}\n", pszFolderPath, pszResolvedPath);
				Marshal.ReleaseComObject(psiResolved);
				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				RuntimeError("Error {0:x8} resolving folder %s from the library.\n", ex.HResult, pszFolderPath);
				return ex.HResult;
			}
		}

		private HRESULT SetTimeout(string pszTimeout)
		{
			// Convert the specified timeout to an integer.
			HRESULT hr = uint.TryParse(pszTimeout, out dwTimeout) ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
			if (hr.Failed)
			{
				ParseError("Invalid timeout: {0}\n", pszTimeout);
			}
			return hr;
		}
	}

	// CSetAttributeCommand - Modifies the attributes of the library.
	//
	// Several attributes of the library can be modified, including the option flags, folder type, and icon path.
	// This command provides options for setting each of these attributes.
	private class CSetAttributeCommand : CShlibCommandBase
	{
		private Guid ftid = default;
		private LIBRARYOPTIONFLAGS lofClear = LIBRARYOPTIONFLAGS.LOF_DEFAULT;
		private LIBRARYOPTIONFLAGS lofSet = LIBRARYOPTIONFLAGS.LOF_DEFAULT;
		private string? pszIconPath = null;

		public CSetAttributeCommand() : base("setattrib", "Modifies the attributes of the library.", false)
		{
			// Initialize option handlers for setting and clearing flags.
			ARGENTRY<LIBRARYOPTIONFLAGS>[] c_rgOptions =
			[
				new( "pinned", LIBRARYOPTIONFLAGS.LOF_PINNEDTONAVPANE, "Indicates that the library should be shown in the navigation pane in Explorer."),
				new( "all", LIBRARYOPTIONFLAGS.LOF_MASK_ALL, "Sets/clears all option flags on the library."),
			];

			AddEnumOptionHandler("setflag", "option flag", "Sets the specified option flag on the library.", SetFlag, c_rgOptions);

			AddEnumOptionHandler("clearflag", "option flag", "Clears the specified option flag from the library.", ClearFlag, c_rgOptions);

			// Initialize an option handler for specifying the folder type.
			ARGENTRY<Guid>[] c_rgFolderTypes =
			[
				new( "documents", FOLDERTYPEID.FOLDERTYPEID_Documents.Guid(), "Specifies that the library primarily contains document content." ),
				new( "pictures", FOLDERTYPEID.FOLDERTYPEID_Pictures.Guid(), "Specifies that the library primarily contains pictures content." ),
				new( "music", FOLDERTYPEID.FOLDERTYPEID_Music.Guid(), "Specifies that the library primarily contains music content." ),
				new( "videos", FOLDERTYPEID.FOLDERTYPEID_Videos.Guid(), "Specifies that the library primarily contains video content." ),
				new( "none", Guid.Empty, "Clears the folder type of the library." ),
			];

			AddEnumOptionHandler("foldertype", "folder type", "Sets the folder type of the library.", SetFolderType, c_rgFolderTypes);

			// Initialize option handler for specifying the icon.
			AddStringOptionHandler("icon", "Specifies the path to the icon to display for the library.", SetIconPath);
		}

		// Modify any attributes that have been specified.
		protected override HRESULT v_ExecuteLibCommand()
		{
			HRESULT hr = HRESULT.S_OK;

			try
			{
				if (lofSet != LIBRARYOPTIONFLAGS.LOF_DEFAULT)
				{
					plib!.SetOptions(lofSet, lofSet);
				}

				if (hr.Succeeded && lofClear != LIBRARYOPTIONFLAGS.LOF_DEFAULT)
				{
					plib!.SetOptions(lofClear, LIBRARYOPTIONFLAGS.LOF_DEFAULT);
				}

				if (hr.Succeeded && ftid != Guid.Empty)
				{
					plib!.SetFolderType(ftid);
				}

				if (hr.Succeeded && pszIconPath is not null)
				{
					plib!.SetIcon(pszIconPath);
				}
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}

			if (hr.Failed)
			{
				RuntimeError("Error {0:x8} modifying library attributes.\n", hr);
			}
			return hr;
		}

		protected override void v_PrintInstructions() => Output("The form of the icon path is \"C:\\Path\\To\\Some\\Module.dll,-123\" where the icon is resource ID 123 in Module.dll. Alternatively, the path to a .ico file can be specified. Relative paths will be evaluated against the %PATH% environment variable.\n");

		private HRESULT ClearFlag(LIBRARYOPTIONFLAGS lofClear)
		{
			this.lofClear |= lofClear;
			return HRESULT.S_OK;
		}

		private HRESULT SetFlag(LIBRARYOPTIONFLAGS lofSet)
		{
			this.lofSet |= lofSet;
			return HRESULT.S_OK;
		}

		private HRESULT SetFolderType(Guid ftid)
		{
			this.ftid = ftid;
			return HRESULT.S_OK;
		}

		private HRESULT SetIconPath(string pszIconPath)
		{
			this.pszIconPath = pszIconPath;
			return HRESULT.S_OK;
		}
	}

	// CSetDefaultSaveFolderCommand - Sets the default save location of the library.
	//
	// When a user attempts to save an item into a library, the library needs to know which location to actually save the data into.
	// The default save location is one of the folders included in the library that has been designated for this purpose.
	private class CSetDefaultSaveFolderCommand : CFolderCommandBase
	{
		private DEFAULTSAVEFOLDERTYPE dsft = DEFAULTSAVEFOLDERTYPE.DSFT_DETECT;

		public CSetDefaultSaveFolderCommand() : base("setsaveloc", "Sets the default save location of the library.")
		{
			ARGENTRY<DEFAULTSAVEFOLDERTYPE>[] c_rgScopes =
			[
				new( "detect", DEFAULTSAVEFOLDERTYPE.DSFT_DETECT, "Detect which save location to set based on the current user and the owner of the library. (default)" ),
				new( "private", DEFAULTSAVEFOLDERTYPE.DSFT_PRIVATE, "Set the private default save location." ),
				new( "public", DEFAULTSAVEFOLDERTYPE.DSFT_PUBLIC, "Set the public default save location." ),
			];

			AddEnumOptionHandler("scope", "scope", "Specifies which default save location to set (public or private).", SetScope, c_rgScopes);
		}

		protected override HRESULT v_ExecuteLibCommand()
		{
			try { plib!.SetDefaultSaveFolder(dsft, psiFolder!); return 0; }
			catch (Exception ex)
			{
				if (ex.HResult == HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_NOT_FOUND))
				{
					RuntimeError("The specified folder is not included in the library.\n");
				}
				else
				{
					RuntimeError("Error {0:x8} setting default save location to {1}.\n", ex.HResult, pszFolderPath);
				}
				return ex.HResult;
			}
		}

		protected override void v_PrintInstructions() => Output("The default save location must be one of the folders already included in the library. This is the folder that Explorer and other applications will use when saving items into the library. Since libraries can be shared with other users, each library has a \"private\" and a \"public\" save location, which take effect for the owner of the library, and other users, respectively.\n");

		private HRESULT SetScope(DEFAULTSAVEFOLDERTYPE dsft)
		{
			this.dsft = dsft;
			return HRESULT.S_OK;
		}
	}
}