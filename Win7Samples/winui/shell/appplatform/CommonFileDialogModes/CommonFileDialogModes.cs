using Vanara.PInvoke;
using Vanara.InteropServices;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.ShlwApi;
using static Vanara.PInvoke.Shell32;

namespace CommonFileDialogModes;

internal class Program
{
	const uint c_idAdd = 601;
	const uint c_idDone = 602;

	const int IDC_PICKITEM = 100;
	const int IDC_PICKCONTAINER = 101;
	const int IDC_FILEOPENBASKETPICKER = 102;
	const int IDC_PICKFILESANDFOLDERS = 103;

	/* Utility Classes and out Functions[] ***********************************************************************************************/

	/*
	Usage:

	CItemIterator itemIterator(psi);

	while (itemIterator.MoveNext())
	{
	IShellItem2 psi;
	hr = itemIterator.GetCurrent(IID_PPV_ARGS(&psi));
	if (hr.Succeeded)
	{
	// Perform action on psi
	Marshal.ReleaseComObject(psi);
	}
	}
	*/
	class CItemIterator : IDisposable
	{

		HRESULT hr;
		PIDL pidlFull;
		PIDL? pidlRel;
		IShellFolder? psfCur = null;

		public CItemIterator(IShellItem psi)
		{
			SHGetIDListFromObject(psi, out pidlFull);
			Init();
		}

		void IDisposable.Dispose()
		{
			if (psfCur != null) Marshal.ReleaseComObject(psfCur);
		}

		public bool MoveNext()
		{
			bool fMoreItems = false;
			if (hr.Succeeded)
			{
				if (pidlRel is null)
				{
					fMoreItems = true;
					pidlRel = pidlFull; // First item - Might be empty if it is the desktop
				}
				else if (!pidlRel.IsEmpty)
				{
					PIDL pidlChild = pidlRel; // Save the current segment for binding
					pidlRel = ILNext((IntPtr)pidlRel);

					// If we are not at the end setup for the next iteration
					if (!ILIsEmpty((IntPtr)pidlRel))
					{
						short cbSave = Marshal.ReadInt16((IntPtr)pidlRel); // Avoid cloning for the child by truncating temporarily
						Marshal.WriteInt16((IntPtr)pidlRel, 0); // Make this a child

						IShellFolder? psfNew = psfCur?.BindToObject<IShellFolder>(pidlChild);
						if (psfNew is not null)
						{
							Marshal.ReleaseComObject(psfCur!);
							psfCur = psfNew; // Transfer ownership
							fMoreItems = true;
						}

						Marshal.WriteInt16((IntPtr)pidlRel, cbSave); // Restore previous ID size
					}
				}
			}
			return fMoreItems;
		}

		public T? GetCurrent<T>() where T : class
		{
			if (hr.Succeeded)
			{
				// Create the childID by truncating pidlRel temporarily
				PIDL pidlNext = ILNext(pidlRel?.DangerousGetHandle() ?? IntPtr.Zero);
				short cbSave = Marshal.ReadInt16((IntPtr)pidlNext); // Save old cb
				Marshal.WriteInt16((IntPtr)pidlNext, 0); // Make pidlRel a child

				hr = SHCreateItemWithParent(PIDL.Null, psfCur, pidlRel!, typeof(T).GUID, out var ppv);

				Marshal.WriteInt16((IntPtr)pidlNext, cbSave); // Restore old cb

				if (hr.Succeeded) return (T?)ppv;
			}
			return null;
		}

		HRESULT GetResult() => hr;
		
		PIDL? GetRelativeIDList() => pidlRel;

		void Init()
		{
			if (hr.Succeeded)
			{
				hr = SHGetDesktopFolder(out psfCur);
			}
		}
	}

	static HRESULT GetIDListName(IShellItem psi, out string? ppsz)
	{
		StringBuilder pszOutput = new(2048);

		CItemIterator itemIterator = new(psi);
		while (itemIterator.MoveNext())
		{
			psi = itemIterator.GetCurrent<IShellItem2>()!;
			string pszName = psi.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVE);
			// Ignore errors, this is for debugging only
			pszOutput.Append("[");
			pszOutput.Append(pszName);
			pszOutput.Append("]");
			Marshal.ReleaseComObject(psi);
		}

		ppsz = pszOutput.ToString();
		return 0;
	}

	static HRESULT GetSelectionFromSite(object punkSite, bool fNoneImpliesFolder, out IShellItemArray? ppsia)
	{
		IFolderView2? pfv = IUnknown_QueryService<IFolderView2>(punkSite, typeof(IFolderView).GUID);
		pfv!.GetSelection(fNoneImpliesFolder, out ppsia);
		Marshal.ReleaseComObject(pfv);
		return 0;
	}

	static void DeletePerUserDialogState()
	{
		IFileOpenDialog pfd = new();
		// Delete window size, MRU and other saved data for testing initial case
		pfd.ClearClientData();
		Marshal.ReleaseComObject(pfd);
	}

	static void ReportSelectedItems(object punkSite, IShellItemArray psia)
	{
		uint cItems = psia.GetCount();
		HRESULT hr = 0;
		for (uint i = 0; hr.Succeeded && (i < cItems); i++)
		{
			IShellItem psi = psia.GetItemAt(i);
			hr = GetIDListName(psi, out var pszName);
			if (hr.Succeeded)
			{
				IUnknown_GetWindow(punkSite, out var hwnd);
				TASKDIALOG_COMMON_BUTTON_FLAGS buttonFlags = (i == (cItems - 1)) ? TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON : TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_OK_BUTTON | TASKDIALOG_COMMON_BUTTON_FLAGS.TDCBF_CANCEL_BUTTON;
				string szMsg = $"Item {i + 1} of {cItems} added to basket";
				if (TaskDialog(hwnd, default, "Items Addded to Basket", szMsg, pszName, buttonFlags, 0, out var nButton).Succeeded)
				{
					hr = (nButton == 2/*IDCANCEL*/) ? HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_CANCELLED) : HRESULT.S_OK;
				}
			}
			Marshal.ReleaseComObject(psi);
		}
	}

	static void ReportSelectedItemsFromSite(object punkSite)
	{
		HRESULT hr = GetSelectionFromSite(punkSite, true, out var psia);
		if (hr.Succeeded)
		{
			ReportSelectedItems(punkSite, psia!);
			Marshal.ReleaseComObject(psia!);
		}
	}

	/* Picking a out file[] **************************************************************************************************************/

	static void PickItem()
	{
		IFileOpenDialog pfd = new();
		if (pfd.Show(default).Succeeded)
		{
			IShellItem psi = pfd.GetResult();
			if (GetIDListName(psi, out var pszPath).Succeeded)
			{
				MessageBox(default, pszPath!, "Selected Item", MB_FLAGS.MB_OK);
			}
			Marshal.ReleaseComObject(psi);
		}
		Marshal.ReleaseComObject(pfd);
	}

	/* Picking a out container[] *********************************************************************************************************/

	static void PickContainer()
	{
		IFileOpenDialog pfd = new();
		var dwOptions = pfd.GetOptions();
		pfd.SetOptions(dwOptions | FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS);

		if (pfd.Show(default).Succeeded)
		{
			IShellItem psi = pfd.GetResult();
			if (GetIDListName(psi, out var pszPath).Succeeded)
			{
				MessageBox(default, pszPath!, "Selected Container", MB_FLAGS.MB_OK);
				Marshal.ReleaseComObject(psi);
			}
			Marshal.ReleaseComObject(pfd);
		}
	}

	/* Picking Files in Basket out Mode[] ************************************************************************************************/

	[ComVisible(true)]
	class CFileOpenBasketPickerCallback : IFileDialogEvents, IFileDialogControlEvents
	{
		// This class makes special assumptions about how it is used, specifically
		// 1) This class will only reside on the stack.
		// 2) Components that consume this object have well-defined reference lifetimes.
		// In this case, this is only consumed by the file dialog advise and unadvise.
		// Unadvising will release the file dialog's only reference to this object.
		//
		// Do not do this for heap allocated objects.

		// IFileDialogEvents
		HRESULT IFileDialogEvents.OnFileOk(IFileDialog pfd)
		{
			// if this button is in the "Add" mode then do this, otherwise return HRESULT.S_OK
			IFileOpenDialog? pfod = pfd as IFileOpenDialog;
			if (pfod is not null)
			{
				IShellItemArray psia = pfod.GetSelectedItems();
				ReportSelectedItems(pfd, psia);
				Marshal.ReleaseComObject(psia);
			}
			return HRESULT.S_FALSE; // S_FALSE keeps the dialog up; return HRESULT.S_OK to allow it to dismiss.
		}

		HRESULT IFileDialogEvents.OnFolderChanging(IFileDialog d, IShellItem i) => HRESULT.E_NOTIMPL;

		HRESULT IFileDialogEvents.OnFolderChange(IFileDialog d) => HRESULT.E_NOTIMPL;

		HRESULT IFileDialogEvents.OnSelectionChange(IFileDialog pfd)
		{
			// Update the text of the Open/Add button here based on the selection
			try
			{
				IShellItem psi = pfd.GetCurrentSelection();
				SFGAO attr = psi.GetAttributes(SFGAO.SFGAO_FOLDER | SFGAO.SFGAO_STREAM);
				if (SFGAO.SFGAO_FOLDER == attr)
				{
					pfd.SetOkButtonLabel("Open");
				}
				else
				{
					pfd.SetOkButtonLabel("Add");
				}
				Marshal.ReleaseComObject(psi);
				return HRESULT.S_OK;
			}
			catch (Exception ex) { return ex.HResult; }
		}

		HRESULT IFileDialogEvents.OnShareViolation(IFileDialog d, IShellItem i, out FDE_SHAREVIOLATION_RESPONSE r) { r = default; return HRESULT.E_NOTIMPL; }
		HRESULT IFileDialogEvents.OnTypeChange(IFileDialog d) => HRESULT.E_NOTIMPL;
		HRESULT IFileDialogEvents.OnOverwrite(IFileDialog d, IShellItem i, out FDE_SHAREVIOLATION_RESPONSE r) { r = default; return HRESULT.E_NOTIMPL; }

		// IFileDialogControlEvents
		HRESULT IFileDialogControlEvents.OnItemSelected(IFileDialogCustomize d, uint a, uint b) => HRESULT.E_NOTIMPL;

		HRESULT IFileDialogControlEvents.OnButtonClicked(IFileDialogCustomize pfdc, uint dwIDCtl)
		{
			switch (dwIDCtl)
			{
				case c_idDone:
					IFileDialog pfd = (IFileDialog)pfdc;
					pfd.Close(HRESULT.S_OK);
					Marshal.ReleaseComObject(pfd);
					break;

				default:
					break;
			}

			return HRESULT.S_OK;
		}

		HRESULT IFileDialogControlEvents.OnCheckButtonToggled(IFileDialogCustomize dc, uint a, bool b) => HRESULT.E_NOTIMPL;
		HRESULT IFileDialogControlEvents.OnControlActivating(IFileDialogCustomize dc, uint a) => HRESULT.E_NOTIMPL;
	}

	// This sample demonstrates how to use the file dialog in a modal way such that
	// users can easily pick multiple files. It does this by overriding the normal "Open" button
	// with an "Add" button that passes the selection back to this app.
	//
	// One case this sample does not support is selecting folders this way. This has
	// the issue of the "Open" button being overloaded for "navigate into the folder" and "add the folder."
	// One way to deal with this is to add a new button, "Add Folder", the PickFilesAndFolders sample demonstrates this.
	static void FileOpenBasketPicker()
	{
		IFileOpenDialog pfd = new();
		CFileOpenBasketPickerCallback foacb = new();
		uint dwCookie = pfd.Advise(foacb);
		var dwOptions = pfd.GetOptions();
		pfd.SetOptions(dwOptions | FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT | FILEOPENDIALOGOPTIONS.FOS_ALLNONSTORAGEITEMS);

		IFileDialog2? pfd2 = pfd as IFileDialog2;
		if (pfd2 is not null)
		{
			pfd2.SetCancelButtonLabel("Done");
		}
		else
		{
			IFileDialogCustomize? pfdc = pfd as IFileDialogCustomize;
			if (pfdc is not null)
			{
				pfdc.AddPushButton(c_idDone, "Done");
			}
		}

		pfd.SetTitle("File Open Modal Basket Picker Sample");

		// We do not process the results of the dialog since
		// the selected items are passed back via OnFileOk()
		pfd.Show(default); // hr intentionally ignored

		pfd.Unadvise(dwCookie);
		Marshal.ReleaseComObject(pfd);
	}

	/* Picking Files and Folders in Basket out Mode[] ************************************************************************************/

	[ComVisible(true)]
	class CPickFilesAndFoldersCallback : IFileDialogEvents, IFileDialogControlEvents
	{
		// This class makes special assumptions about how it is used, specifically
		// 1) This class will only reside on the stack.
		// 2) Components that consume this object have well-defined reference lifetimes.
		// In this case, this is only consumed by the file dialog advise and unadvise.
		// Unadvising will release the file dialog's only reference to this object.
		//
		// Do not do this for heap allocated objects.

		// IFileDialogEvents
		HRESULT IFileDialogEvents.OnFileOk(IFileDialog pfd)
		{
			ReportSelectedItemsFromSite(pfd);
			return HRESULT.S_FALSE; // S_FALSE keeps the dialog up, return HRESULT.S_OK to allows it to dismiss
		}

		HRESULT IFileDialogEvents.OnFolderChanging(IFileDialog pfd, IShellItem psi) => HRESULT.E_NOTIMPL;

		HRESULT IFileDialogEvents.OnFolderChange(IFileDialog pfd) => HRESULT.E_NOTIMPL;

		HRESULT IFileDialogEvents.OnSelectionChange(IFileDialog pfd)
		{
			// Design for the text of the "Add" button
			// ---------------------------------------
			// Single select item "Add file"
			// Single select folder "Add folder"
			// Multiselect "Add items"
			// Null select "Add current folder"
			IFileDialogCustomize? pfdc = pfd as IFileDialogCustomize;
			if (pfdc is not null)
			{
				// GetSelectionFromSite() fails on no selection
				// When that happens, default to the current folder.
				string pszLabel = "Add current folder";
				if (GetSelectionFromSite(pfd, false, out var psia).Succeeded && psia is not null)
				{
					uint count = psia!.GetCount();
					if (count == 1)
					{
						IShellItem psi = psia.GetItemAt(0);
						SFGAO attributes = psi.GetAttributes(SFGAO.SFGAO_FOLDER);
						pszLabel = attributes != 0 ? "Add folder" : "Add file";
						Marshal.ReleaseComObject(psi);
					}
					else if (count > 1)
					{
						pszLabel = "Add items";
					}
					Marshal.ReleaseComObject(psia);
				}
				pfdc.SetControlLabel(c_idAdd, pszLabel);
			}

			return HRESULT.S_OK;
		}

		HRESULT IFileDialogEvents.OnShareViolation(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE r) { r = default; return HRESULT.E_NOTIMPL; }
		HRESULT IFileDialogEvents.OnTypeChange(IFileDialog pfd) => HRESULT.E_NOTIMPL;
		HRESULT IFileDialogEvents.OnOverwrite(IFileDialog pfd, IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE r) { r = default; return HRESULT.E_NOTIMPL; }

		// IFileDialogControlEvents
		HRESULT IFileDialogControlEvents.OnItemSelected(IFileDialogCustomize pfdc, uint a, uint b) => HRESULT.E_NOTIMPL;

		HRESULT IFileDialogControlEvents.OnButtonClicked(IFileDialogCustomize pfdc, uint dwIDCtl)
		{
			switch (dwIDCtl)
			{
				case c_idAdd:
					// Instead of using IFileDialog::GetCurrentSelection(), we need to get the
					// selection from the view to handle the "no selection implies folder" case
					ReportSelectedItemsFromSite(pfdc);
					break;

				case c_idDone:
					{
						IFileDialog? pfd = pfdc as IFileDialog;
						if (pfd is not null)
						{
							pfd.Close(HRESULT.S_OK);
						}
					}
					break;

				default:
					break;
			}

			return HRESULT.S_OK;
		}

		HRESULT IFileDialogControlEvents.OnCheckButtonToggled(IFileDialogCustomize pfdc, uint a, bool b) => HRESULT.E_NOTIMPL;
		HRESULT IFileDialogControlEvents.OnControlActivating(IFileDialogCustomize pfdc, uint a) => HRESULT.E_NOTIMPL;
	}

	static void PickFilesAndFolders()
	{
		IFileOpenDialog pfd = new();
		CPickFilesAndFoldersCallback foacb = new();
		uint dwCookie = pfd.Advise(foacb);
		var dwOptions = pfd.GetOptions();
		pfd.SetOptions(dwOptions | FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT | FILEOPENDIALOGOPTIONS.FOS_ALLNONSTORAGEITEMS);

		IFileDialogCustomize? pfdc = pfd as IFileDialogCustomize;
		if (pfdc is not null)
		{
			// The spacing pads the button a bit.
			pfdc.AddPushButton(c_idAdd, " Add current folder ");
		}

		IFileDialog2? pfd2 = pfd as IFileDialog2;
		if (pfd2 is not null)
		{
			pfd2.SetCancelButtonLabel("Done");
		}
		else
		{
			// pre Win7 we need to add a 3rd button, ugly but workable
			pfdc = pfd as IFileDialogCustomize;
			if (pfdc is not null)
			{
				pfdc.AddPushButton(c_idDone, "Done");
			}
		}

		pfd.SetTitle("Pick Files and Folder Sample");

		// the items selected are passed back via OnFileOk()
		// so we don't process the results of the dialog
		pfd.Show(default); // hr intentionally ignored
		pfd.Unadvise(dwCookie);
		Marshal.ReleaseComObject(pfd);
	}

	// Application entry point
	static void Main()
	{
		using Vanara.Windows.Forms.ComCtl32v6Context ccc = new();

		TASKDIALOG_BUTTON[] buttons = [
			new() { nButtonID = IDC_PICKITEM, pszButtonText = new SafeLPTSTR("Pick File") },
			new() { nButtonID = IDC_PICKCONTAINER, pszButtonText = new SafeLPTSTR("Pick Folder") },
			new() { nButtonID = IDC_FILEOPENBASKETPICKER, pszButtonText = new SafeLPTSTR("Pick Files (Basket Mode)") },
			new() { nButtonID = IDC_PICKFILESANDFOLDERS, pszButtonText = new SafeLPTSTR("Pick Files and Folders (Basket Mode)") },
		];

		TASKDIALOGCONFIG taskDialogParams = new()
		{
			dwFlags = TASKDIALOG_FLAGS.TDF_USE_COMMAND_LINKS | TASKDIALOG_FLAGS.TDF_ALLOW_DIALOG_CANCELLATION,
			pButtons = new SafeNativeArray<TASKDIALOG_BUTTON>(buttons),
			cButtons = (uint)buttons.Length,
			MainInstruction = "Pick the file dialog samples you want to try",
			WindowTitle = "Common File Dialog Modes"
		};

		HRESULT hr = 0;
		while (hr.Succeeded)
		{
			hr = TaskDialogIndirect(taskDialogParams, out var selectedId, out _, out _);
			if (hr.Succeeded)
			{
				if (selectedId == 2 /*IDCANCEL*/)
				{
					break;
				}
				else if (selectedId == IDC_PICKITEM)
				{
					PickItem();
				}
				else if (selectedId == IDC_PICKCONTAINER)
				{
					PickContainer();
				}
				else if (selectedId == IDC_FILEOPENBASKETPICKER)
				{
					FileOpenBasketPicker();
				}
				else if (selectedId == IDC_PICKFILESANDFOLDERS)
				{
					PickFilesAndFolders();
				}
			}
		}
	}
}