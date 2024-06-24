using System.Runtime.Versioning;
using System.Windows.Forms;
using Vanara.PInvoke;
using static CustomJumpList.FileRegistrations;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

[assembly: SupportedOSPlatform("windows")]
namespace CustomJumpList;

public partial class CustomJumpListSample : Form
{
	internal const string c_szTitle = "CustomJumpListSample";
	private const string c_szWindowClass = "CUSTOMJUMPLISTSAMPLE";
	private const string REGPATH_SAMPLE = "Software\\Microsoft\\Samples\\CustomJumpListSample";
	private const string REGVAL_RECENTCATEGORY = "RecentCategorySelected";

	private static readonly string[] c_rgpszFiles =
	{
		"Microsoft_Sample_1.txt",
		"Microsoft_Sample_2.txt",
		"Microsoft_Sample_3.doc",
		"Microsoft_Sample_4.doc"
	};

	public CustomJumpListSample() => InitializeComponent();

	// Adds a custom category to the Jump List. Each item that should be in the category is added to an ordered collection, and then the
	// category is appended to the Jump List as a whole.
	private static void _AddCategoryToList(ICustomDestinationList pcdl, IObjectArray poaRemoved)
	{
		var poc = new IObjectCollection();
		foreach (var fn in c_rgpszFiles)
		{
			if (SHCreateItemInKnownFolder(KNOWNFOLDERID.FOLDERID_Documents.Guid(), KNOWN_FOLDER_FLAG.KF_FLAG_DEFAULT, fn, typeof(IShellItem).GUID, out var ppv).Succeeded)
			{
				var psi = (IShellItem)ppv!;
				// Items listed in the removed list may not be re-added to the Jump List during this list-building transaction. They
				// should not be re-added to the Jump List until the user has used the item again. The AppendCategory call below will
				// fail if an attempt to add an item in the removed list is made.
				if (!_IsItemInArray(psi, poaRemoved))
					poc.AddObject(psi);
			}

			// Add the category to the Jump List. If there were more categories, they would appear from top to bottom in the order they
			// were appended.
			pcdl.AppendCategory("Custom Category", poc);
		}
	}

	// Builds the collection of task items and adds them to the Task section of the Jump List. All tasks should be added to the canonical
	// "Tasks" category by calling ICustomDestinationList::AddUserTasks.
	private static void _AddTasksToList(ICustomDestinationList pcdl)
	{
		var poc = new IObjectCollection();
		var psl = _CreateShellLink("/Task1", "Task 1")!;
		poc.AddObject(psl);
		psl = _CreateShellLink("/Task2", "Second Task")!;
		poc.AddObject(psl);
		psl = _CreateSeparatorLink();
		poc.AddObject(psl);
		psl = _CreateShellLink("/Task3", "Task 3")!;
		poc.AddObject(psl);
		var poa = (IObjectArray)poc;
		// Add the tasks to the Jump List. Tasks always appear in the canonical "Tasks" category that is displayed at the bottom of the
		// Jump List, after all other categories.
		pcdl.AddUserTasks(poa);
	}

	// The Tasks category of Jump Lists supports separator items. These are simply IShellLinkW instances that have the
	// PKEY_AppUserModel_IsDestListSeparator property set to TRUE. All other values are ignored when this property is set.
	private static IShellLinkW _CreateSeparatorLink()
	{
		var psl = new IShellLinkW();
		var pps = (IPropertyStore)psl;
		var propvar = new PROPVARIANT(true);
		pps.SetValue(PROPERTYKEY.System.AppUserModel.IsDestListSeparator, propvar);
		pps.Commit();
		return psl;
	}

	// Creates a CLSID_ShellLink to insert into the Tasks section of the Jump List. This type of Jump List item allows the specification
	// of an explicit command line to execute the task.
	private static IShellLinkW? _CreateShellLink(string pszArguments, string pszTitle)
	{
		var psl = new IShellLinkW();
		// Determine our executable's file path so the task will execute this application
		var szAppPath = new StringBuilder(MAX_PATH, MAX_PATH);
		if (GetModuleFileName(default, szAppPath, (uint)szAppPath.Capacity) > 0)
		{
			psl.SetPath(szAppPath.ToString());
			psl.SetArguments(pszArguments);
			// The title property is required on Jump List items provided as an IShellLinkW instance. This value is used as the display
			// name in the Jump List.
			var pps = (IPropertyStore)psl;
			var propvar = new PROPVARIANT(pszTitle);
			pps.SetValue(PROPERTYKEY.System.Title, propvar);
			pps.Commit();
			return psl;
		}
		return null;
	}

	// Determines if the provided IShellItem is listed in the array of items that the user has removed
	private static bool _IsItemInArray(IShellItem psi, IObjectArray poaRemoved)
	{
		for (var i = 0U; i < poaRemoved.GetCount(); i++)
		{
			try
			{
				var psiCompare = poaRemoved.GetAt<IShellItem>(i);
				if (psiCompare.Compare(psi, SICHINTF.SICHINT_CANONICAL, out _) == 0)
					return true;
			}
			catch { }
		}
		return false;
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

	// Builds a new custom Jump List for this application.
	private static void CreateJumpList()
	{
		// Create the custom Jump List object.
		var pcdl = new ICustomDestinationList();

		// Custom Jump Lists follow a push model - applications are responsible for providing an updated list anytime the contents should
		// be changed. Lists are generated in a list-building transaction that starts by calling BeginList. Until the list is committed,
		// Windows will display the previous version of the list, if available.
		//
		// The cMinSlots out parameter indicates the minimum number of items that the Jump List UI is guaranteed to display. Applications
		// can provide more items when building a custom Jump List, but the extra items may not be displayed. The number is dependant
		// upon a number of factors, such as screen resolution and the "Number of recent items to display in Jump Lists" user setting.
		// See the MSDN documentation on BeginList for more information.
		//
		// The IObjectArray returned from BeginList contains a list of items the user has chosen to remove from their Jump List.
		// Applications must respect the user's removal of an item and not re-add any item in the removed list during this list-building
		// transaction. Applications should also clear any persited usage-tracking data for any item in the removed list. If the user
		// begins using a previously removed item in the future, it may be re-added to the list.
		var poaRemoved = pcdl.BeginList<IObjectArray>(out _);
		// Add content to the Jump List.
		_AddCategoryToList(pcdl, poaRemoved);
		_AddTasksToList(pcdl);
		pcdl.CommitList();
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

	// Removes that existing custom Jump List for this application.
	private static void DeleteJumpList()
	{
		var pcdl = new ICustomDestinationList();
		pcdl.DeleteList(null);
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
		}

		Application.Run(new CustomJumpListSample());
	}

	private void IDM_EXIT_Click(object sender, EventArgs e) => Close();

	private void IDM_FILE_CREATECUSTOMJUMPLIST_Click(object sender, EventArgs e) => CreateJumpList();

	private void IDM_FILE_DELETECUSTOMJUMPLIST_Click(object sender, EventArgs e) => DeleteJumpList();

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
}
