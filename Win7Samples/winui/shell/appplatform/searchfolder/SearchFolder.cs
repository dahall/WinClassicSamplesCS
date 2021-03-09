using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

using Vanara.PInvoke;
using static Vanara.PInvoke.SearchApi;

namespace searchfolder
{
	static class SearchFolder
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// File dialog drag and drop feature requires OLE
			var pSearchFolderItemFactory = new ISearchFolderItemFactory();
			var psiaScope = CreateScope();
			pSearchFolderItemFactory.SetScope(psiaScope);
			// Sets the display name of the search
			pSearchFolderItemFactory.SetDisplayName("Sample Query");
			var pCondition = GetCondition();
			// Sets the condition for pSearchFolderItemFactory
			pSearchFolderItemFactory.SetCondition(pCondition);
			// This retrieves an IShellItem of the search.  It is a virtual child of the desktop.
			var pShellItemSearch = pSearchFolderItemFactory.GetShellItem<IShellItem>();
			OpenCommonFileDialogTo(pShellItemSearch);
		}

		// Creates an instance of ICondition for the predicate "Kind=Document AND Size>10240"
		static ICondition GetCondition()
		{
			// Create the condition factory.  This interface helps create conditions.
			var pConditionFactory = new IConditionFactory2();
			var pConditionKind = (ICondition)pConditionFactory.CreateStringLeaf(PROPERTYKEY.System.Kind, CONDITION_OPERATION.COP_EQUAL, "Document", null, CONDITION_CREATION_OPTIONS.CONDITION_CREATION_DEFAULT, typeof(ICondition).GUID);
			var pConditionSize = (ICondition)pConditionFactory.CreateIntegerLeaf(PROPERTYKEY.System.Size, CONDITION_OPERATION.COP_GREATERTHAN, 102400, CONDITION_CREATION_OPTIONS.CONDITION_CREATION_DEFAULT, typeof(ICondition).GUID);
			// Once all of the leaf conditions are created successfully, "AND" them together
			ICondition[] rgConditions = { pConditionKind, pConditionSize };
			return (ICondition)pConditionFactory.CreateCompoundFromArray(CONDITION_TYPE.CT_AND_CONDITION, rgConditions, (uint)rgConditions.Length, CONDITION_CREATION_OPTIONS.CONDITION_CREATION_DEFAULT, typeof(ICondition).GUID);
		}

		// This opens up the common file dialog to an IShellItem and waits for the user to select a file from the results.
		// It then displays the name of the selected item in a message box.
		static void OpenCommonFileDialogTo(IShellItem pShellItemSearch)
		{
			// Create an instance of IFileOpenDialog
			IFileDialog pFileDialog = new IFileOpenDialog();
			// Set it to the folder we want to show
			pFileDialog.SetFolder(pShellItemSearch);
			pFileDialog.Show(default);
			try
			{
				// Now get the file that the user selected
				var pShellItemSelected = pFileDialog.GetResult();
				// Get the name from that file
				string pszName = pShellItemSelected.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);
				// Display it back to the user
				MessageBox.Show($"You Chose '{pszName}'\r", "Search Folder Sample");
			}
			catch { }
		}

		// Create a shell item array object that can be accessed using IObjectCollection
		// or IShellItemArray. IObjectCollection lets items be added or removed from the collection.
		// For code that needs to run on Vista use SHCreateShellItemArrayFromIDLists()
		static T CreateShellItemArray<T>() where T : class
		{
			var pLibrary = new IShellLibrary();
			return pLibrary.GetFolders<T>(LIBRARYFOLDERFILTER.LFF_ALLITEMS);
		}

		// This helper creates the scope object that is a collection of shell items that
		// define where the search will operate.
		static IShellItemArray CreateScope()
		{
			var pObjects = CreateShellItemArray<IObjectCollection>();
			if (SHCreateItemInKnownFolder(KNOWNFOLDERID.FOLDERID_DocumentsLibrary.Guid(), 0, null, typeof(IShellItem).GUID, out var psi).Succeeded)
				pObjects.AddObject(psi);

			// Other items can be added to pObjects similar to the code above.
			return (IShellItemArray)pObjects;
		}
	}
}
