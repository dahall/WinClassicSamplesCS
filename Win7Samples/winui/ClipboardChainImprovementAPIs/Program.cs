using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;

//
// Create the sample window to use with the clipboard functions
//
using SafeHWND hWindow = CreateWindow("Edit", "Sample Window", WindowStyles.WS_VISIBLE | WindowStyles.WS_OVERLAPPED, 100, 100, 100, 100);

if (hWindow.IsInvalid)
{
	Console.Write("Cannot create sample window\n");
}
else
{
	//
	// Call GetUpdatedClipboardFormats to get the number of clipboard formats,
	// without getting the clipboard formats themselves
	//
	GetUpdatedClipboardFormats(default, 0, out var numberOfClipboardFormats);
	Console.Write("GetUpdatedClipboardFormats: initial number of clipboard formats: {0}\n\n", numberOfClipboardFormats);

	//
	// Add the sample window to the list of clipboard format listeners
	//
	if (AddClipboardFormatListener(hWindow))
	{
		Console.Write("AddClipboardFormatListener: Sample window added to list of clipboard format listeners\n\n");
	}

	//
	// Add bitmap data to the clipboard, which will generate a WM_CLIPBOARDUPDATE message
	//
	AddBitmapDataToClipboard(hWindow);

	//
	// Peek for a WM_CLIPBOARDUPDATE message
	//
	PeekMessage(out var message, hWindow, WindowMessage.WM_CLIPBOARDUPDATE, WindowMessage.WM_CLIPBOARDUPDATE, PM.PM_REMOVE);
	if (message.message == (uint)WindowMessage.WM_CLIPBOARDUPDATE)
	{
		Console.Write("Sample window received WM_CLIPBOARDUPDATE message\n\n");
	}

	//
	// Call GetUpdatedClipboardFormats, getting the list of formats
	//
	uint[] clipboardFormats = new uint[numberOfClipboardFormats + 10];
	if (GetUpdatedClipboardFormats(clipboardFormats, (uint)clipboardFormats.Length, out numberOfClipboardFormats))
	{
		Console.Write("GetUpdatedClipboardFormats: number of clipboard formats written: {0}\n\n", numberOfClipboardFormats);
	}

	//
	// Remove the sample window from the list of clipboard format listeners
	//
	if (RemoveClipboardFormatListener(hWindow))
	{
		Console.Write("RemoveClipboardFormatListener: sample window removed from list of clipboard format change listeners\n\n");
	}

	//
	// Add bitmap data to the clipboard, which will generate a WM_CLIPBOARDUPDATE message
	//
	AddBitmapDataToClipboard(hWindow);

	//
	// Peek for a WM_CLIPBOARDUPDATE message
	//
	message.message = 0;
	PeekMessage(out message, hWindow, WindowMessage.WM_CLIPBOARDUPDATE, WindowMessage.WM_CLIPBOARDUPDATE, PM.PM_REMOVE);
	if (message.message != (uint)WindowMessage.WM_CLIPBOARDUPDATE)
	{
		Console.Write("Sample window did NOT receive the WM_CLIPBOARDUPDATE message\n\n");
	}
}

bool AddBitmapDataToClipboard([In] HWND hWindow)
{
	bool result = true;

	SafeHBITMAP hBitmap = SafeHBITMAP.Null;

	if (!OpenClipboard(hWindow))
	{
		Console.Write("Cannot open clipboard\n");
		result = false;
	}

	if (result)
	{
		hBitmap = LoadBitmap(default, OBM_CHECK);

		if (hBitmap.IsNull)
		{
			Console.Write("Cannot load bitmap\n");
			result = false;
		}
	}

	if (result)
	{
		if (SetClipboardData(CLIPFORMAT.CF_BITMAP, hBitmap) != default)
		{
			Console.Write("Cannot set clipboard data\n");
			result = false;
		}
	}

	if (result)
	{
		if (!CloseClipboard())
		{
			Console.Write("Cannot close clipboard\n");
			result = false;
		}
	}

	if (result)
	{
		Console.Write("Loaded bitmap data onto the clipboard... WM_CLIPBOARDUPDATE message generated\n");
	}

	return result;
}