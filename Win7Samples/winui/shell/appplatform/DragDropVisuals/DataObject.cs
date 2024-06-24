// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved
//
// Sample data object implementation that demonstrates how to leverage the shell provided data object for the SetData() support

using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

namespace DragDropVisuals;

internal class CDataObject : IDataObjectV
{
	private const string c_szText = "Clipboard Contents";
	private IDataObjectV? pdtobjShell;

	~CDataObject()
	{
		if (pdtobjShell is not null) Marshal.ReleaseComObject(pdtobjShell);
	}

	public static HRESULT CreateInstance<T>(out T? ppv) where T : class
	{
		ppv = default;
		CDataObject p = new();
		HRESULT hr = p.QueryInterface(typeof(T).GUID, out var ppvTemp);
		if (hr.Succeeded) ppv = (T?)ppvTemp;
		return hr;
	}

	HRESULT IDataObjectV.DAdvise(in FORMATETC pformatetc, ADVF advf, IAdviseSink? pAdvSink, out uint pdwConnection)
	{
		pdwConnection = 0;
		return HRESULT.E_NOTIMPL;
	}

	HRESULT IDataObjectV.DUnadvise(uint dwConnection) => HRESULT.E_NOTIMPL;

	HRESULT IDataObjectV.EnumDAdvise(out IEnumSTATDATA? ppenumAdvise)
	{
		ppenumAdvise = null;
		return HRESULT.E_NOTIMPL;
	}

	HRESULT IDataObjectV.EnumFormatEtc(DATADIR dwDirection, out IEnumFORMATETC? ppenumFormatEtc)
	{
		HRESULT hr = HRESULT.E_NOTIMPL;
		ppenumFormatEtc = null;
		if (dwDirection == DATADIR.DATADIR_GET)
		{
			FORMATETC[] rgfmtetc = [new() { cfFormat = CLIPFORMAT.CF_UNICODETEXT, tymed = TYMED.TYMED_HGLOBAL }];
			hr = SHCreateStdEnumFmtEtc((uint)rgfmtetc.Length, rgfmtetc, out ppenumFormatEtc);
		}
		return hr;
	}

	HRESULT IDataObjectV.GetCanonicalFormatEtc(in FORMATETC pformatectIn, out FORMATETC pformatetcOut)
	{
		pformatetcOut = pformatectIn;
		pformatetcOut.ptd = default;
		return HRESULT.DATA_S_SAMEFORMATETC;
	}

	// IDataObject
	HRESULT IDataObjectV.GetData(in FORMATETC pformatetcIn, out STGMEDIUM pmedium)
	{
		HRESULT hr = HRESULT.DV_E_FORMATETC;
		pmedium = new();
		if (pformatetcIn.cfFormat == CLIPFORMAT.CF_UNICODETEXT)
		{
			if ((pformatetcIn.tymed & TYMED.TYMED_HGLOBAL) != 0)
			{
				SafeHGlobalHandle h = new(c_szText);
				pmedium.unionmember = h.TakeOwnership();
				pmedium.tymed = TYMED.TYMED_HGLOBAL;
			}
		}
		else if (EnsureShellDataObject().Succeeded)
		{
			hr = pdtobjShell!.GetData(pformatetcIn, out pmedium);
		}
		return hr;
	}

	HRESULT IDataObjectV.GetDataHere(in FORMATETC pformatetc, ref STGMEDIUM pmedium) => HRESULT.E_NOTIMPL;

	HRESULT IDataObjectV.QueryGetData(in FORMATETC pformatetc)
	{
		HRESULT hr = HRESULT.S_FALSE;
		if (pformatetc.cfFormat == CLIPFORMAT.CF_UNICODETEXT)
		{
			hr = HRESULT.S_OK;
		}
		else if (EnsureShellDataObject().Succeeded)
		{
			hr = pdtobjShell!.QueryGetData(pformatetc);
		}
		return hr;
	}

	HRESULT IDataObjectV.SetData(in FORMATETC pformatetc, in STGMEDIUM pmedium, bool fRelease)
	{
		HRESULT hr = EnsureShellDataObject();
		if (hr.Succeeded)
		{
			hr = pdtobjShell!.SetData(pformatetc, pmedium, fRelease);
		}
		return hr;
	}

	private HRESULT EnsureShellDataObject()
	{
		// the shell data object imptlements ::SetData() in a way that will store any format this code delegates to that implementation to
		// avoid having to implement ::SetData()
		if (pdtobjShell is not null)
			return HRESULT.S_OK;
		else
		{
			var hr = SHCreateDataObject(PIDL.Null, 0, default, default, typeof(IDataObject).GUID, out var pdo);
			if (hr.Succeeded) pdtobjShell = (IDataObjectV)pdo;
			return hr;
		}
	}
}