// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved

// declare a static CLIPFORMAT and pass that that by ref as the first param

using System.Runtime.InteropServices.ComTypes;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace Vanara.Extensions;

public static class DragDropHelpers
{
	private static CLIPFORMAT g_cfURL = 0;
	private static CLIPFORMAT s_cfDropDescription = 0;

	public static void ClearDropTip(this IDataObject pdtobj) => SetDropTip(pdtobj, DROPIMAGETYPE.DROPIMAGE_INVALID, "", default);

	// helper to convert a data object with HIDA format or folder into a shell item
	// note: if the data object contains more than one item this function will fail if you want to operate on the full selection use SHCreateShellItemArrayFromDataObject
	public static HRESULT CreateItemFromObject<T>(this object punk, out T? ppv) where T : class
	{
		ppv = default;

		HRESULT hr = SHGetIDListFromObject(punk, out var pidl);
		if (hr.Succeeded)
		{
			ppv = SHCreateItemFromIDList<T>(pidl);
		}
		else
		{
			// perhaps the input is from IE and if so we can construct an item from the URL
			IDataObjectV? pdo = punk as IDataObjectV;
			if (pdo is not null)
			{
				FORMATETC fmte = new() { cfFormat = GetClipboardFormat(ref g_cfURL, ShellClipboardFormat.CFSTR_SHELLURL), dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -1, tymed = TYMED.TYMED_HGLOBAL };
				hr = pdo.GetData(fmte, out var medium);
				if (hr.Succeeded)
				{
					string? pszURL = Marshal.PtrToStringAnsi(medium.unionmember);
					if (pszURL is not null)
					{
						ppv = SHCreateItemFromParsingName<T>(pszURL);
					}
					ReleaseStgMedium(medium);
				}
				Marshal.ReleaseComObject(pdo);
			}
		}
		return hr;
	}

	public static CLIPFORMAT GetClipboardFormat(ref CLIPFORMAT pcf, string pszForamt)
	{
		if ((int)pcf == 0)
		{
			pcf = (CLIPFORMAT)RegisterClipboardFormat(pszForamt);
		}
		return pcf;
	}

	public static HRESULT SetBlob(this IDataObject pdtobj, CLIPFORMAT cf, IntPtr pvBlob, uint cbBlob)
	{
		using SafeHGlobalHandle pv = new(cbBlob);
		pvBlob.CopyTo(pv, cbBlob);

		FORMATETC fmte = new() { cfFormat = cf, ptd = IntPtr.Zero, dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -1, tymed = TYMED.TYMED_HGLOBAL };

		// The STGMEDIUM structure is used to define how to handle a global memory transfer. This structure includes a flag, tymed, which
		// indicates the medium to be used, and a union comprising pointers and a handle for getting whichever medium is specified in tymed.
		STGMEDIUM medium = new() { tymed = TYMED.TYMED_HGLOBAL, unionmember = pv.TakeOwnership() };

		try { pdtobj.SetData(ref fmte, ref medium, true); return HRESULT.S_OK; }
		catch (Exception ex) { return ex.HResult; }
	}

	public static void SetDropTip(this IDataObject pdtobj, DROPIMAGETYPE type, string pszMsg, string? pszInsert)
	{
		SafeHGlobalStruct<DROPDESCRIPTION> dd = new DROPDESCRIPTION() { type = type, szInsert = pszInsert ?? "", szMessage = pszMsg };

		SetBlob(pdtobj, GetClipboardFormat(ref s_cfDropDescription, ShellClipboardFormat.CFSTR_DROPDESCRIPTION), dd, dd.Size);
	}
}

// encapsulation of the shell drag drop presentation and Drop handling functionality this provides the following features 1) drag images, 2)
// drop tips, 3) ints OLE and registers drop target, 4) conversion of the data object item into shell items
//
// to use this
// 1) have the object that represents the main window of your app derive from CDragDropHelper exposing it as public. "class CAppClass : CDragDropHelper"
// 2) add IDropTarget to the QueryInterface() implementation of your class that is a COM object itself
// 3) in your WM_INITDIALOG handling call InitializeDragDropHelper(hdlg, DROPIMAGE_LINK, default) passing the dialog window and drop tip
// template and type
// 4) in your WM_DESTROY handler add a call to TerminateDragDropHelper(). note not doing this will lead to a leak of your object so this is important
// 5) add the delaration and implementation of OnDrop() to your class, this gets called when the drop happens
public abstract class CDragDropHelper : IDropTarget
{
	private DROPIMAGETYPE dropImageType = DROPIMAGETYPE.DROPIMAGE_LABEL;
	private HWND hwndRegistered = default;
	private IDropTargetHelper? pdth;
	private IDataObject? pdtobj = null;
	private string? pszDropTipTemplate = null;

	public CDragDropHelper()
	{
		if (CoCreateInstance(CLSID_DragDropHelper, default, CLSCTX.CLSCTX_INPROC, typeof(IDropTargetHelper).GUID, out var ppv).Succeeded)
			pdth = (IDropTargetHelper)ppv;
	}

	~CDragDropHelper()
	{
		if (pdth is not null) Marshal.ReleaseComObject(pdth);
	}

	public HRESULT GetDragDropHelper<T>(out T? ppv) where T : class
	{
		ppv = pdth as T;
		return ppv is null ? HRESULT.E_NOINTERFACE : HRESULT.S_OK;
	}

	public void InitializeDragDropHelper(HWND hwnd, DROPIMAGETYPE dropImageType, string? pszDropTipTemplate)
	{
		this.dropImageType = dropImageType;
		this.pszDropTipTemplate = pszDropTipTemplate;
		if (RegisterDragDrop(hwnd, this).Succeeded)
		{
			hwndRegistered = hwnd;
		}
	}

	public void SetDropTipTemplate(string? pszDropTipTemplate) => this.pszDropTipTemplate = pszDropTipTemplate;

	public void TerminateDragDropHelper()
	{
		if (!hwndRegistered.IsNull)
		{
			RevokeDragDrop(hwndRegistered);
			hwndRegistered = default;
		}
	}

	// IDropTarget
	HRESULT IDropTarget.DragEnter(IDataObject pdtobj, MouseButtonState grfKeyState, POINT pt, ref DROPEFFECT pdwEffect)
	{
		// ref leave pdwEffect unchanged, we support all operations
		try { pdth?.DragEnter(hwndRegistered, pdtobj!, pt, pdwEffect); } catch { }
		ShellHelpers.SetInterface(ref this.pdtobj, pdtobj);

		HRESULT hr = pdtobj.CreateItemFromObject(out IShellItem? psi);
		if (hr.Succeeded)
		{
			string pszName = psi!.GetDisplayName(SIGDN.SIGDN_NORMALDISPLAY);
			pdtobj!.SetDropTip(dropImageType, pszDropTipTemplate ?? "%1", pszName);
			Marshal.ReleaseComObject(psi);
		}
		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.DragLeave()
	{
		pdth?.DragLeave();
		pdtobj!.ClearDropTip();
		if (pdtobj is not null) Marshal.ReleaseComObject(pdtobj);
		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.DragOver(MouseButtonState grfKeyState, POINT pt, ref DROPEFFECT pdwEffect)
	{
		// ref leave pdwEffect unchanged, we support all operations
		pdth?.DragOver(pt, pdwEffect);
		return HRESULT.S_OK;
	}

	HRESULT IDropTarget.Drop(IDataObject pdtobj, MouseButtonState grfKeyState, POINT pt, ref DROPEFFECT pdwEffect)
	{
		pdth?.Drop(pdtobj!, pt, pdwEffect);

		HRESULT hr = SHCreateShellItemArrayFromDataObject((IDataObject)this.pdtobj!, typeof(IShellItemArray).GUID, out IShellItemArray psia);
		if (hr.Succeeded)
		{
			OnDrop(psia, grfKeyState);
			Marshal.ReleaseComObject(psia);
		}
		else
		{
			OnDropError(this.pdtobj!);
		}

		return HRESULT.S_OK;
	}

	// direct access to the data object, if you don't want to use the shell item array
	private IDataObject? GetDataObject() => pdtobj;

	// client provides
	protected abstract HRESULT OnDrop(IShellItemArray psia, MouseButtonState grfKeyState);

	protected virtual HRESULT OnDropError(IDataObject pdo) => HRESULT.S_OK;
}