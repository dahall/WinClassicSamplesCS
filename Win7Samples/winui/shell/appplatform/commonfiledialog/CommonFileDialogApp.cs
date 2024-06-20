using Vanara.PInvoke;
using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;

namespace PropertyEdit;

internal class Program
{
	static readonly COMDLG_FILTERSPEC[] c_rgSaveTypes = [
		new("Word Document (*.doc)", "*.doc"),
		new("Web Page (*.htm; *.html)", "*.htm;*.html"),
		new("Text Document (*.txt)", "*.txt"),
		new("All Documents (*.*)", "*.*"),
	];

	// Indices of file types
	const int INDEX_WORDDOC = 1;
	const int INDEX_WEBPAGE = 2;
	const int INDEX_TEXTDOC = 3;

	// Controls
	const int CONTROL_GROUP = 2000;
	const int CONTROL_RADIOBUTTONLIST = 2;
	const int CONTROL_RADIOBUTTON1 = 1;
	const int CONTROL_RADIOBUTTON2 = 2;       // It is OK for this to have the same ID as CONTROL_RADIOBUTTONLIST,
											  // because it is a child control under CONTROL_RADIOBUTTONLIST

	// IDs for the Task Dialog Buttons
	const int IDC_BASICFILEOPEN = 100;
	const int IDC_ADDITEMSTOCUSTOMPLACES = 101;
	const int IDC_ADDCUSTOMCONTROLS = 102;
	const int IDC_SETDEFAULTVALUESFORPROPERTIES = 103;
	const int IDC_WRITEPROPERTIESUSINGHANDLERS = 104;
	const int IDC_WRITEPROPERTIESWITHOUTUSINGHANDLERS = 105;

	/* File Dialog Event Handler *****************************************************************************************************/
	[ComVisible(true)]
	class CDialogEventHandler : IFileDialogEvents, IFileDialogControlEvents
	{
		// IFileDialogEvents methods
		HRESULT IFileDialogEvents.OnFileOk(IFileDialog pfd) => HRESULT.S_OK;
		HRESULT IFileDialogEvents.OnFolderChanging(IFileDialog pfd, IShellItem psiFolder) => HRESULT.S_OK;
		HRESULT IFileDialogEvents.OnFolderChange(IFileDialog pfd) => HRESULT.S_OK;
		HRESULT IFileDialogEvents.OnSelectionChange(IFileDialog pfd) => HRESULT.S_OK;
		HRESULT IFileDialogEvents.OnShareViolation(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse) { pResponse = default; return HRESULT.S_OK; }
		// This method gets called when the file-type is changed (combo-box selection changes).
		// For sample sake, let's react to this event by changing the properties show.
		HRESULT IFileDialogEvents.OnTypeChange(IFileDialog pfd)
		{
			HRESULT hr = default;
			IFileSaveDialog? pfsd = pfd as IFileSaveDialog;
			if (pfsd is not null)
			{
				uint uIndex = pfsd.GetFileTypeIndex(); // index of current file-type
				IPropertyDescriptionList? pdl = default;
				switch (uIndex)
				{
					case INDEX_WORDDOC:
						// When .doc is selected, let's ask for some arbitrary property, say Title.
						hr = PSGetPropertyDescriptionListFromString("prop:System.Title", typeof(IPropertyDescriptionList).GUID, out pdl);
						if (hr.Succeeded)
						{
							// false as second param == do not show default properties.
							pfsd.SetCollectedProperties(pdl, false);
							Marshal.ReleaseComObject(pdl);
						}
						break;

					case INDEX_WEBPAGE:
						// When .html is selected, let's ask for some other arbitrary property, say Keywords.
						hr = PSGetPropertyDescriptionListFromString("prop:System.Keywords", typeof(IPropertyDescriptionList).GUID, out pdl);
						if (hr.Succeeded)
						{
							// false as second param == do not show default properties.
							pfsd.SetCollectedProperties(pdl, false);
							Marshal.ReleaseComObject(pdl);
						}
						break;

					case INDEX_TEXTDOC:
						// When .txt is selected, let's ask for some other arbitrary property, say Author.
						hr = PSGetPropertyDescriptionListFromString("prop:System.Author", typeof(IPropertyDescriptionList).GUID, out pdl);
						if (hr.Succeeded)
						{
							// true as second param == show default properties as well, but show Author property first in list.
							pfsd.SetCollectedProperties(pdl, true);
							Marshal.ReleaseComObject(pdl);
						}
						break;
				}
				Marshal.ReleaseComObject(pfsd);
			}
			return hr;
		}
		HRESULT IFileDialogEvents.OnOverwrite(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse) { pResponse = default; return HRESULT.S_OK; }
		// This method gets called when an dialog control item selection happens (radio-button selection. etc).
		// For sample sake, let's react to this event by changing the dialog title.
		HRESULT IFileDialogControlEvents.OnItemSelected(IFileDialogCustomize pfdc, uint dwIDCtl, uint dwIDItem)
		{
			IFileDialog? pfd = pfdc as IFileDialog;
			HRESULT hr = default;
			if (pfd is not null)
			{
				if (dwIDCtl == CONTROL_RADIOBUTTONLIST)
				{
					switch (dwIDItem)
					{
						case CONTROL_RADIOBUTTON1:
							pfd.SetTitle("Longhorn Dialog");
							break;

						case CONTROL_RADIOBUTTON2:
							pfd.SetTitle("Vista Dialog");
							break;
					}
				}
				Marshal.ReleaseComObject(pfd);
			}
			return hr;
		}
		HRESULT IFileDialogControlEvents.OnButtonClicked(IFileDialogCustomize pfdc, uint dwIDCtl) => HRESULT.S_OK;
		HRESULT IFileDialogControlEvents.OnCheckButtonToggled(IFileDialogCustomize pfdc, uint dwIDCtl, bool bChecked) => HRESULT.S_OK;
		HRESULT IFileDialogControlEvents.OnControlActivating(IFileDialogCustomize pfdc, uint dwIDCtl) => HRESULT.S_OK;
	}

	/* Utility out Functions[] ***********************************************************************************************************/

	// A helper function that converts UNICODE data to ANSI and writes it to the given file.
	// We write in ANSI format to make it easier to open the output file in Notepad.
	static HRESULT WriteDataToFile(HFILE hFile, string pszDataIn)
	{
		// First figure out our required buffer size.
		var pszData = Encoding.ASCII.GetBytes(pszDataIn);
		return WriteFile(hFile, pszData, (uint)pszData.Length, out var dwBytesWritten) ? HRESULT.S_OK : Win32Error.GetLastError().ToHRESULT();
	}

	// Helper function to write property/value into a custom file format.
	//
	// We are inventing a dummy format here:
	// [APPDATA]
	// xxxxxx
	// [ENDAPPDATA]
	// [PROPERTY]foo=bar[ENDPROPERTY]
	// [PROPERTY]foo2=bar2[ENDPROPERTY]
	static HRESULT WritePropertyToCustomFile(string pszFileName, string pszPropertyName, string pszValue)
	{
		using var hFile = CreateFile(pszFileName, FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE, FILE_SHARE.FILE_SHARE_READ,
			default, CreationOption.OPEN_ALWAYS, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);
		HRESULT hr = (hFile.IsInvalid) ? Win32Error.GetLastError().ToHRESULT() : HRESULT.S_OK;
		if (hr.Succeeded)
		{
			hr = SetFilePointer(hFile, 0, default, System.IO.SeekOrigin.End) > 0 ? HRESULT.S_OK : Win32Error.GetLastError().ToHRESULT();
			if (hr.Succeeded)
			{
				hr = WriteDataToFile(hFile, $"[PROPERTY]{pszPropertyName}={pszValue}[ENDPROPERTY]\r\n");
			}
		}
		return hr;
	}

	// Helper function to write dummy content to a custom file format.
	//
	// We are inventing a dummy format here:
	// [APPDATA]
	// xxxxxx
	// [ENDAPPDATA]
	// [PROPERTY]foo=bar[ENDPROPERTY]
	// [PROPERTY]foo2=bar2[ENDPROPERTY]
	static HRESULT WriteDataToCustomFile(string pszFileName)
	{
		using var hFile = CreateFile(pszFileName, FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE, FILE_SHARE.FILE_SHARE_READ,
			default, CreationOption.CREATE_ALWAYS, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);

		HRESULT hr = (hFile.IsInvalid) ? Win32Error.GetLastError().ToHRESULT() : HRESULT.S_OK;
		if (hr.Succeeded)
		{
			const string wszDummyContent = "[MYAPPDATA]\r\nThis is an example of how to use the IFileSaveDialog interface.\r\n[ENDMYAPPDATA]\r\n";

			hr = WriteDataToFile(hFile, wszDummyContent);
		}
		return hr;
	}

	/* Common File Dialog out Snippets[] *************************************************************************************************/

	// This code snippet demonstrates how to work with the common file dialog interface
	static HRESULT BasicFileOpen()
	{
		HRESULT hr = 0;
		// CoCreate the File Open Dialog object.
		IFileOpenDialog pfd = new();
		// Create an event handling object, and hook it up to the dialog.
		IFileDialogEvents pfde = new CDialogEventHandler();
		// Hook up the event handler.
		uint dwCookie = pfd.Advise(pfde);
		if (dwCookie > 0)
		{
			try
			{
				// Set the options on the dialog.
				// Before setting, always get the options first in order not to override existing options.
				var dwFlags = pfd.GetOptions();
				// In this case, get shell items only for file system items.
				pfd.SetOptions(dwFlags | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);
				// Set the file types to display only. Notice that, this is a 1-based array.
				pfd.SetFileTypes((uint)c_rgSaveTypes.Length, c_rgSaveTypes);
				// Set the selected file type index to Word Docs for this example.
				pfd.SetFileTypeIndex(INDEX_WORDDOC);
				// Set the default extension to be ".doc" file.
				pfd.SetDefaultExtension("doc");
				// Show the dialog
				hr = pfd.Show(default);
				if (hr.Succeeded)
				{
					// Obtain the result, once the user clicks the 'Open' button.
					// The result is an IShellItem object.
					IShellItem psiResult = pfd.GetResult();
					// We are just going to print out the name of the file for sample sake.
					string pszFilePath = psiResult.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
					TaskDialog(default, default, "CommonFileDialogApp", pszFilePath, default, TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON, (int)TaskDialogIcon.TD_INFORMATION_ICON, out _);
					Marshal.ReleaseComObject(psiResult);
				}
			}
			catch (Exception ex) { hr = ex.HResult; }
			finally
			{
				// Unhook the event handler.
				pfd.Unadvise(dwCookie);
			}
		}
		Marshal.ReleaseComObject(pfd);
		return hr;
	}

	// The Common Places area in the File Dialog is extensible.
	// This code snippet demonstrates how to extend the Common Places area.
	// Look at CDialogEventHandler::OnItemSelected to see how messages pertaining to the added
	// controls can be processed.
	static HRESULT AddItemsToCommonPlaces()
	{
		// CoCreate the File Open Dialog object.
		IFileOpenDialog pfd = new();
		// Always use known folders instead of hard-coding physical file paths.
		// In this case we are using Public Music KnownFolder.
		IKnownFolderManager pkfm = new();
		// Get the known folder.
		IKnownFolder pKnownFolder = KNOWNFOLDERID.FOLDERID_PublicMusic.GetIKnownFolder();
		// File Dialog APIs need an IShellItem that represents the location.
		IShellItem psi = pKnownFolder.GetShellItem<IShellItem>();
		// Add the place to the bottom of default list in Common File Dialog.
		pfd.AddPlace(psi, FDAP.FDAP_BOTTOM);
		// Show the File Dialog.
		var hr = pfd.Show(default);
		if (hr.Succeeded)
		{
			//
			// You can add your own code here to handle the results.
			//
		}
		Marshal.ReleaseComObject(psi);
		Marshal.ReleaseComObject(pKnownFolder);
		Marshal.ReleaseComObject(pkfm);
		Marshal.ReleaseComObject(pfd);
		return hr;
	}

	// This code snippet demonstrates how to add custom controls in the Common File Dialog.
	static HRESULT AddCustomControls()
	{
		HRESULT hr = 0;
		// CoCreate the File Open Dialog object.
		IFileOpenDialog pfd = new();
		// Create an event handling object, and hook it up to the dialog.
		IFileDialogEvents pfde = new CDialogEventHandler();
		// Hook up the event handler.
		uint dwCookie = pfd.Advise(pfde);
		if (dwCookie > 0)
		{
			try
			{
				// Set up a Customization.
				IFileDialogCustomize? pfdc = pfd as IFileDialogCustomize;
				if (pfdc is not null)
				{
					// Create a Visual Group.
					pfdc.StartVisualGroup(CONTROL_GROUP, "Sample Group");
					// Add a radio-button list.
					pfdc.AddRadioButtonList(CONTROL_RADIOBUTTONLIST);
					// Set the state of the added radio-button list.
					pfdc.SetControlState(CONTROL_RADIOBUTTONLIST, CDCONTROLSTATEF.CDCS_VISIBLE | CDCONTROLSTATEF.CDCS_ENABLED);
					// Add individual buttons to the radio-button list.
					pfdc.AddControlItem(CONTROL_RADIOBUTTONLIST, CONTROL_RADIOBUTTON1, "Change Title to Longhorn");
					pfdc.AddControlItem(CONTROL_RADIOBUTTONLIST, CONTROL_RADIOBUTTON2, "Change Title to Vista");
					// Set the default selection to option 1.
					pfdc.SetSelectedControlItem(CONTROL_RADIOBUTTONLIST, CONTROL_RADIOBUTTON1);
					// End the visual group.
					pfdc.EndVisualGroup();

					// Now show the dialog.
					hr = pfd.Show(default);
					if (hr.Succeeded)
					{
						//
						// You can add your own code here to handle the results.
						//
					}
					Marshal.ReleaseComObject(pfdc);
				}
			}
			catch (Exception ex) { hr = ex.HResult; }
			finally
			{
				// Unhook the event handler.
				pfd.Unadvise(dwCookie);
			}
		}
		Marshal.ReleaseComObject(pfd);
		return hr;
	}

	// This code snippet demonstrates how to add default metadata in the Common File Dialog.
	// Look at CDialogEventHandler::OnTypeChange to see to change the order/list of properties
	// displayed in the Common File Dialog.
	static HRESULT SetDefaultValuesForProperties()
	{
		HRESULT hr = 0;
		// CoCreate the File Open Dialog object.
		IFileSaveDialog pfsd = new();
		// Create an event handling object, and hook it up to the dialog.
		IFileDialogEvents pfde = new CDialogEventHandler();
		// Hook up the event handler.
		uint dwCookie = pfsd.Advise(pfde);
		if (dwCookie > 0)
		{
			try
			{
				// Set the file types to display.
				pfsd.SetFileTypes((uint)c_rgSaveTypes.Length, c_rgSaveTypes);
				pfsd.SetFileTypeIndex(INDEX_WORDDOC);
				pfsd.SetDefaultExtension("doc");

				// The InMemory Property Store is a Property Store that is
				// kept in the memory instead of persisted in a file stream.
				var pps = PSCreateMemoryPropertyStore<IPropertyStore>();
				using PROPVARIANT propvarValue = new("SampleKeywordsValue");
				// Set the value to the property store of the item.
				pps!.SetValue(PROPERTYKEY.System.Keywords, propvarValue);
				// Commit does the actual writing back to the in memory store.
				pps.Commit();
				// Hand these properties to the File Dialog.
				pfsd.SetCollectedProperties(default, true);
				pfsd.SetProperties(pps);
				Marshal.ReleaseComObject(pps);
				// Now show the dialog.
				hr = pfsd.Show(default);
				if (hr.Succeeded)
				{
					//
					// You can add your own code here to handle the results.
					//
				}
			}
			catch (Exception ex) { hr = ex.HResult; }
			finally
			{
				// Unhook the event handler.
				pfsd.Unadvise(dwCookie);
			}
		}
		Marshal.ReleaseComObject(pfsd);
		return hr;
	}

	// The following code snippet demonstrates two things:
	// 1. How to write properties using property handlers.
	// 2. Replicating properties in the "Save As" scenario where the user choses to save an existing file
	// with a different name. We need to make sure we replicate not just the data,
	// but also the properties of the original file.
	static HRESULT WritePropertiesUsingHandlers()
	{
		// CoCreate the File Open Dialog object.
		IFileSaveDialog pfsd = new();

		// For this exercise, let's just support only one file type to make things simpler.
		// Also, let's use the jpg format for sample purpose because the Windows ships with
		// property handlers for jpg files.
		COMDLG_FILTERSPEC[] rgSaveTypes = [ new("Photo Document (*.jpg)", "*.jpg") ];

		// Set the file types to display.
		pfsd.SetFileTypes((uint)rgSaveTypes.Length, rgSaveTypes);
		pfsd.SetFileTypeIndex(0);
		// Set default file extension.
		pfsd.SetDefaultExtension("jpg");
		// Ensure the dialog only returns items that can be represented by file system paths.
		var dwFlags = pfsd.GetOptions();
		pfsd.SetOptions(dwFlags | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);

		// Let's first get the current property set of the file we are replicating
		// and give it to the file dialog object.
		//
		// For simplicity sake, let's just get the property set from a pre-existing jpg file (in the Pictures folder).
		// In the real-world, you would actually add code to get the property set of the file
		// that is currently open and is being replicated.
		var pszPicturesFolderPath = KNOWNFOLDERID.FOLDERID_SamplePictures.FullPath();
		var szFullPathToTestFile = System.IO.Path.Combine(pszPicturesFolderPath, "Flower.jpg");
		IPropertyStore? pps = SHGetPropertyStoreFromParsingName<IPropertyStore>(szFullPathToTestFile);
		if (pps is null)
		{
			// Flower.jpg is probably not in the Pictures folder.
			TaskDialog(default, default, "CommonFileDialogApp", "Create Flower.jpg in the Pictures folder and try again.", default,
				TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON, (int)TaskDialogIcon.TD_ERROR_ICON, out _);
		}
		else
		{
			// Call SetProperties on the file dialog object for getting back later.
			pfsd.SetCollectedProperties(default, true);
			pfsd.SetProperties(pps);
			Marshal.ReleaseComObject(pps);
		}

		var hr = pfsd.Show(default);
		if (hr.Succeeded)
		{
			IShellItem psiResult = pfsd.GetResult();
			string pszNewFileName = psiResult.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
			// This is where you add code to write data to your file.
			// For simplicity, let's just copy a pre-existing dummy jpg file.
			//
			// In the real-world, you would actually add code to replicate the data of
			// file that is currently open.
			hr = CopyFile(szFullPathToTestFile, pszNewFileName, false) ? HRESULT.S_OK : Win32Error.GetLastError().ToHRESULT();
			if (hr.Succeeded)
			{
				// Now apply the properties.
				//
				// Get the property store first by calling GetPropertyStore and pass it on to ApplyProperties.
				// This will make the registered propety handler for the specified file type (jpg)
				// do all the work of writing the properties for you.
				//
				// Property handlers for the specified file type should be registered for this
				// to work.

				// When we call GetProperties, we get back all the properties that we originally set
				// (in our call to SetProperties above) plus the ones user modified in the file dialog.
				pps = pfsd.GetProperties();
				// Now apply the properties making use of the registered property handler for the file type.
				//
				// hWnd is used as parent for any error dialogs that might popup when writing properties.
				// Pass default for IFileOperationProgressSink as we don't want to register any callback for progress notifications.
				pfsd.ApplyProperties(psiResult, pps);
				Marshal.ReleaseComObject(pps);
			}
			Marshal.ReleaseComObject(psiResult);
		}
		Marshal.ReleaseComObject(pfsd);
		return hr;
	}

	// This code snippet demonstrates how to write properties without using property handlers.
	static HRESULT WritePropertiesWithoutUsingHandlers()
	{
		// CoCreate the File Open Dialog object.
		IFileSaveDialog pfsd = new();
		// For this exercise, let's use a custom file type.
		COMDLG_FILTERSPEC[] rgSaveTypes = [new("MyApp Document (*.myApp)", "*.myApp")];

		// Set the file types to display.
		pfsd.SetFileTypes((uint)rgSaveTypes.Length, rgSaveTypes);
		pfsd.SetFileTypeIndex(0);
		// Set default file extension.
		pfsd.SetDefaultExtension("myApp");
		// Ensure the dialog only returns items that can be represented by file system paths.
		var dwFlags = pfsd.GetOptions();
		pfsd.SetOptions(dwFlags | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM);
		// Set the properties you want the FileSave dialog to collect from the user.
		var hr = PSGetPropertyDescriptionListFromString("prop:System.Keywords", typeof(IPropertyDescriptionList).GUID, out var pdl);
		if (hr.Succeeded)
		{
			// true as second param == show default properties as well, but show Keyword first.
			pfsd.SetCollectedProperties(pdl, true);
			Marshal.ReleaseComObject(pdl);
		}

		if (hr.Succeeded)
		{
			// Now show the dialog.
			hr = pfsd.Show(default);
			if (hr.Succeeded)
			{
				IShellItem psiResult = pfsd.GetResult();
				// Get the path to the file.
				string pszNewFileName = psiResult.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);
				// Write data to the file.
				hr = WriteDataToCustomFile(pszNewFileName);
				if (hr.Succeeded)
				{
					// Now get the property store and write each individual property to the file.
					IPropertyStore pps = pfsd.GetProperties();
					uint cProps = pps.GetCount();

					// Loop over property set and write each property/value pair to the file.
					for (uint i = 0; i < cProps && hr.Succeeded; i++)
					{
						PROPERTYKEY key = pps.GetAt(i);
						hr = PSGetNameFromPropertyKey(key, out var pszPropertyName);
						if (hr.Succeeded)
						{
							// Get the value of the property.
							PROPVARIANT propvarValue = new();
							pps.GetValue(key, propvarValue);
							// Write the property to the file.
							hr = WritePropertyToCustomFile(pszNewFileName, pszPropertyName, propvarValue.ToString());
						}
					}
					Marshal.ReleaseComObject(pps);
				}
				Marshal.ReleaseComObject(psiResult);
			}
		}
		Marshal.ReleaseComObject(pfsd);
		return hr;
	}

	// Application entry point
	[STAThread]
	static void Main()
	{
		using Vanara.Windows.Forms.ComCtl32v6Context ccc = new();
		TASKDIALOG_BUTTON[] buttons = [
			new() { nButtonID = IDC_BASICFILEOPEN, pszButtonText = new SafeLPTSTR("Basic File Open") },
			new() { nButtonID = IDC_ADDITEMSTOCUSTOMPLACES, pszButtonText = new SafeLPTSTR("Add Items to Common Places") },
			new() { nButtonID = IDC_ADDCUSTOMCONTROLS, pszButtonText = new SafeLPTSTR("Add Custom Controls") },
			new() { nButtonID = IDC_SETDEFAULTVALUESFORPROPERTIES, pszButtonText = new SafeLPTSTR("Change Property Order") },
			new() { nButtonID = IDC_WRITEPROPERTIESUSINGHANDLERS, pszButtonText = new SafeLPTSTR("Write Properties Using Handlers") },
			new() { nButtonID = IDC_WRITEPROPERTIESWITHOUTUSINGHANDLERS, pszButtonText = new SafeLPTSTR("Write Properties without Using Handlers") },
		];

		TASKDIALOGCONFIG taskDialogParams = new()
		{
			dwFlags = TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS | TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION,
			pButtons = new SafeNativeArray<TASKDIALOG_BUTTON>(buttons),
			cButtons = (uint)buttons.Length,
			MainInstruction = "Pick the file dialog sample you want to try",
			WindowTitle = "Common File Dialog",
		};

		HRESULT hr = 0;
		while (hr.Succeeded)
		{
			hr = TaskDialogIndirect(taskDialogParams, out var selectedId, out _, out _);
			if (hr.Succeeded)
			{
				if (selectedId == 2/*IDCANCEL*/)
				{
					break;
				}
				else if (selectedId == IDC_BASICFILEOPEN)
				{
					BasicFileOpen();
				}
				else if (selectedId == IDC_ADDITEMSTOCUSTOMPLACES)
				{
					AddItemsToCommonPlaces();
				}
				else if (selectedId == IDC_ADDCUSTOMCONTROLS)
				{
					AddCustomControls();
				}
				else if (selectedId == IDC_SETDEFAULTVALUESFORPROPERTIES)
				{
					SetDefaultValuesForProperties();
				}
				else if (selectedId == IDC_WRITEPROPERTIESUSINGHANDLERS)
				{
					WritePropertiesUsingHandlers();
				}
				else if (selectedId == IDC_WRITEPROPERTIESWITHOUTUSINGHANDLERS)
				{
					WritePropertiesWithoutUsingHandlers();
				}
			}
		}
	}
}