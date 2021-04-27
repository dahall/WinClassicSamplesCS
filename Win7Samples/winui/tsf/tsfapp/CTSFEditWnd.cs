using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.MSCTF;

namespace tsfapp
{
	[ComVisible(true)]
	internal partial class CTSFEditWnd : TextBox, ITextStoreACP, ITfContextOwnerCompositionSink, ITfFunctionProvider
	{
		private static readonly HRESULT CONNECT_E_FIRST = HRESULT.Make(true, HRESULT.FacilityCode.FACILITY_ITF, 0x0200);

		// this implementation's limit for advisory connections has been reached
		public static readonly HRESULT CONNECT_E_ADVISELIMIT = (int)CONNECT_E_FIRST + 1;

		// connection attempt failed
		public static readonly HRESULT CONNECT_E_CANNOTCONNECT = (int)CONNECT_E_FIRST + 2;

		// there is no connection for this connection id
		public static readonly HRESULT CONNECT_E_NOCONNECTION = CONNECT_E_FIRST;

		// must use a derived interface to connect
		public static readonly HRESULT CONNECT_E_OVERRIDDEN = (int)CONNECT_E_FIRST + 3;

		internal uint m_cCompositions;

		private const int ATTR_FLAG_DEFAULT = 2;
		private const int ATTR_FLAG_NONE = 0;
		private const int ATTR_FLAG_REQUESTED = 1;
		private const int ATTR_INDEX_MODEBIAS = 0;
		private const int ATTR_INDEX_TEXT_ORIENTATION = 1;
		private const int ATTR_INDEX_TEXT_VERTICALWRITING = 2;
		private const int BLOCK_SIZE = 256;
		private const uint EDIT_VIEW_COOKIE = 0;
		private const int MAX_COMPOSITIONS = 5;
		private const int NUM_SUPPORTED_ATTRS = 3;

		private readonly ADVISE_SINK m_AdviseSink = new();
		private readonly ATTRIBUTES[] m_rgAttributes = new ATTRIBUTES[NUM_SUPPORTED_ATTRS];
		private readonly ITfCompositionView[] m_rgCompositions = new ITfCompositionView[MAX_COMPOSITIONS];
		private int m_acpEnd;
		private int m_acpStart;
		private TsActiveSelEnd m_ActiveSelEnd;
		private int m_cchOldLength;
		private TS_LF m_dwInternalLockType;
		private TS_LF m_dwLockType;
		private uint m_EditCookie;
		private bool m_fInterimChar;
		private bool m_fLayoutChanged;
		private bool m_fLocked;
		private bool m_fNotify;
		private bool m_fPendingLockUpgrade;
		private ITfCategoryMgr m_pCategoryMgr;
		private ITfContext m_pContext;
		private ITfDisplayAttributeMgr m_pDisplayAttrMgr;
		private ITfDocumentMgr m_pDocMgr;
		private ITfDocumentMgr m_pPrevDocMgr;
		private ITextStoreACPServices m_pServices;
		private ITfThreadMgr m_pThreadMgr;

		public CTSFEditWnd()
		{
			TfClientID = Program.g_clientId.Value;
			Multiline = true;
			ReadOnly = false;
			ScrollBars = ScrollBars.Vertical;
		}

		public event Action<string, string> StatusUpdate;

		public bool CanPlaybackSelection
		{
			get
			{
				var fCanPlayback = false;

				InternalLockDocument(TS_LF.TS_LF_READ);

				ITfFunctionProvider pFuncProv = m_pThreadMgr.GetFunctionProvider(CLSID_SapiLayr);
				ITfFnPlayBack pPlayback = pFuncProv.GetFunction<ITfFnPlayBack>();
				if (pPlayback is null)
					return false;

				TF_SELECTION? ts = GetFirstSelection(m_EditCookie);
				if (ts.HasValue)
				{
					pPlayback.QueryRange(ts.Value.range, out _, out fCanPlayback);
				}

				InternalUnlockDocument();

				return fCanPlayback;
			}
		}

		public bool CanReconvertSelection
		{
			get
			{
				var fConv = false;

				InternalLockDocument(TS_LF.TS_LF_READ);

				ITfFunctionProvider pFuncProv = m_pThreadMgr.GetFunctionProvider(GUID_SYSTEM_FUNCTIONPROVIDER);
				ITfFnReconversion pRecon = pFuncProv.GetFunction<ITfFnReconversion>(Guid.Empty);
				if (pRecon is null)
					return false;

				TF_SELECTION? ts = GetFirstSelection(m_EditCookie);
				if (ts.HasValue)
				{
					pRecon.QueryRange(ts.Value.range, out _, out fConv);
				}

				InternalUnlockDocument();

				return fConv;
			}
		}

		public bool Composing => GetPropVals<uint>(GUID_PROP_COMPOSING).Any(u => u != 0);

		public IEnumerable<ITfDisplayAttributeInfo> DisplayAttributes =>
			GetPropVals<uint>(GUID_PROP_ATTRIBUTE).Select(g => m_pCategoryMgr.GetGUID(g)).Select(g => { m_pDisplayAttrMgr.GetDisplayAttributeInfo(g, out ITfDisplayAttributeInfo pDispInfo, out _); return pDispInfo; });

		public IEnumerable<(string, string)> ReadingText
		{
			get
			{
				//get the tracking property for the attributes
				var rGuidProperties = new[] { GUID_PROP_READING };
				ITfReadOnlyProperty pTrackProperty = m_pContext.TrackProperties(rGuidProperties, 1);

				//get the range of the entire text
				ITfRangeACP pRangeAllText = m_pServices.CreateRange(0, base.TextLength);

				pTrackProperty.EnumRanges(m_EditCookie, out IEnumTfRanges pEnumRanges, pRangeAllText);

				/*
				Each range in pEnumRanges represents a span of text that has
				the same properties specified in TrackProperties.
				*/
				foreach (ITfRange pPropRange in new Vanara.Collections.IEnumFromCom<ITfRange>((uint celt, ITfRange[] rgelt, out uint celtFetched) => pEnumRanges.Next(celt, rgelt, out celtFetched), () => pEnumRanges.Reset()))
//				foreach (ITfRange pPropRange in new Vanara.Collections.IEnumFromCom<ITfRange>(pEnumRanges))
				{
					//get the attribute property for the property range
					/*
					The property is actually a VT_UNKNOWN that contains an
					IEnumTfPropertyValue object.
					*/
					var pEnumPropertyVal = (IEnumTfPropertyValue)pTrackProperty.GetValue(m_EditCookie, pPropRange);

					//the property is a string.
					var wsz = new StringBuilder(MAX_PATH);
					pPropRange.GetText(m_EditCookie, 0, wsz, MAX_PATH - 1, out var cch);

					//var rgelt = new TF_PROPERTYVAL[1];
					//pEnumPropertyVal.Next(1, rgelt, out var celtFetched);
					//while (celtFetched > 0)
					foreach (TF_PROPERTYVAL rgelt in new Vanara.Collections.IEnumFromCom<TF_PROPERTYVAL>((uint celt, TF_PROPERTYVAL[] rgelt, out uint celtFetched) => pEnumPropertyVal.Next(celt, rgelt, out celtFetched), () => pEnumPropertyVal.Reset()))
					{
						yield return (wsz.ToString(), rgelt.varValue?.ToString());
						//pEnumPropertyVal.Next(1, rgelt, out celtFetched);
					}
				}
			}
		}

		public IEnumerable<Guid> TextOwner => GetPropVals<uint>(GUID_PROP_TEXTOWNER).Select(g => m_pCategoryMgr.GetGUID(g));

		public uint TfClientID { get; set; }

		public bool IsLocked(TS_LF dwLockType = TS_LF.TS_LF_READ) => m_dwInternalLockType != 0 || m_fLocked && (m_dwLockType & dwLockType) != 0;

		public void LoadFromFile(string pszFile)
		{
			if (pszFile is null)
				return;

			using SafeHFILE hFile = CreateFile(pszFile, Kernel32.FileAccess.GENERIC_READ, 0, default, FileMode.Open,
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);

			if (hFile.IsInvalid)
				return;

			//create a stream on global memory
			Ole32.CreateStreamOnHGlobal(default, true, out IStream pStream).ThrowIfFailed();

			//read the contents of the file into the stream

			//set the stream pointer to the start of the stream
			pStream.Seek(0, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);

			//write the contents of the stream to the file
			var buffer = new byte[BLOCK_SIZE];

			Win32Error.ThrowLastErrorIfFalse(ReadFile(hFile, buffer, BLOCK_SIZE, out var uRead));
			while (uRead > 0)
			{
				pStream.Write(buffer, (int)uRead, default);
				Win32Error.ThrowLastErrorIfFalse(ReadFile(hFile, buffer, BLOCK_SIZE, out uRead));
			}

			Load(pStream);
		}

		public void OnGetPreservedKey()
		{
			var pKeyMgr = (ITfKeystrokeMgr)m_pThreadMgr;
			TF_PRESERVEDKEY tfPreKey = new() { uVKey = 'F', uModifiers = TF_MOD.TF_MOD_CONTROL };
			Guid guid = pKeyMgr.GetPreservedKey(m_pContext, tfPreKey);
			if (guid != Guid.Empty)
			{
				pKeyMgr.IsPreservedKey(guid, tfPreKey);
				var guidBytes = guid.ToByteArray();
				var twelveBytes = BitConverter.GetBytes(12U);
				Array.Copy(twelveBytes, guidBytes, sizeof(uint));
				pKeyMgr.SimulatePreservedKey(m_pContext, new Guid(guidBytes), out _);
			}
		}

		public void Playback()
		{
			InternalLockDocument(TS_LF.TS_LF_READ);

			ITfFunctionProvider pFuncProv = m_pThreadMgr.GetFunctionProvider(CLSID_SapiLayr);
			ITfFnPlayBack pPlayback = pFuncProv.GetFunction<ITfFnPlayBack>();
			TF_SELECTION? ts = GetFirstSelection(m_EditCookie);
			if (ts.HasValue)
			{
				HRESULT hr = pPlayback.QueryRange(ts.Value.range, out ITfRange pRange, out _);
				if (hr.Succeeded && pRange is not null)
				{
					pPlayback.Play(pRange);
				}
			}

			InternalUnlockDocument();
		}

		public void Reconvert()
		{
			InternalLockDocument(TS_LF.TS_LF_READ);

			try
			{
				ITfFunctionProvider pFuncProv = m_pThreadMgr.GetFunctionProvider(GUID_SYSTEM_FUNCTIONPROVIDER);
				ITfFnReconversion pRecon = pFuncProv.GetFunction<ITfFnReconversion>();

				TF_SELECTION? ts = GetFirstSelection(m_EditCookie);
				if (ts.HasValue)
				{
					//get the range that covers the text to be reconverted
					pRecon.QueryRange(ts.Value.range, out ITfRange pRange, out var fConv);
					if (pRange is not null)
					{
						var wsz = pRange.GetText(m_EditCookie);

						//get the list of reconversion candidates
						HRESULT hr = pRecon.GetReconversion(pRange, out ITfCandidateList pCandList);
						if (hr.Succeeded)
						{
							hr = pCandList.GetCandidateNum(out var uCandidateCount);

							for (uint i = 0; i < uCandidateCount; i++)
							{
								hr = pCandList.GetCandidate(i, out ITfCandidateString pCandString);
								if (hr.Succeeded)
								{
									hr = pCandString.GetString(out var bstr);
									if (hr.Succeeded)
									{
										System.Diagnostics.Debug.WriteLine($"\tCandidate - \"{bstr}\"");
									}
								}
							}
						}
						//cause the reconversion to happen
						hr = pRecon.Reconvert(pRange);
					}
				}
			}
			catch
			{
			}
			finally
			{
				InternalUnlockDocument();
			}
		}

		public void SaveToFile(string pszFile)
		{
			if (pszFile is null)
				return;

			using SafeHFILE hFile = CreateFile(pszFile, Kernel32.FileAccess.GENERIC_WRITE, 0, default, FileMode.Create,
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL);

			if (hFile.IsInvalid)
				return;

			//create a stream on global memory
			Ole32.CreateStreamOnHGlobal(default, true, out IStream pStream).ThrowIfFailed();

			Save(pStream);

			//initialize the file
			Win32Error.ThrowLastErrorIf(SetFilePointer(hFile, 0, default, SeekOrigin.Begin), u => u == INVALID_SET_FILE_POINTER);
			Win32Error.ThrowLastErrorIfFalse(SetEndOfFile(hFile));

			//set the stream pointer to the start of the stream
			pStream.Seek(0, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);

			//write the contents of the stream to the file
			var buffer = new byte[BLOCK_SIZE];
			unsafe
			{
				uint uRead = 0;
				pStream.Read(buffer, BLOCK_SIZE, (IntPtr)(&uRead));
				while (uRead > 0)
				{
					WriteFile(hFile, buffer, uRead, out _);
					uRead = 0;
					pStream.Read(buffer, BLOCK_SIZE, (IntPtr)(&uRead));
				}
			}
		}

		public void TerminateAllCompositions()
		{
			//get the ITfContextOwnerCompositionServices interface pointer
			var pCompServices = (ITfContextOwnerCompositionServices)m_pContext;

			//passing default terminates all compositions
			pCompServices.TerminateComposition();
		}

		HRESULT ITextStoreACP.AdviseSink(in Guid riid, object punk, TS_AS dwMask)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			HRESULT hr = HRESULT.E_INVALIDARG;

			//see if this advise sink already exists
			if (m_AdviseSink.punkID is not null)
			{
				//only one advise sink is allowed at a time
				hr = CONNECT_E_ADVISELIMIT;
			}
			else if (m_AdviseSink.punkID == punk)
			{
				//this is the same advise sink, so just update the advise mask
				m_AdviseSink.dwMask = dwMask;

				hr = HRESULT.S_OK;
			}
			else if (riid == typeof(ITextStoreACPSink).GUID)
			{
				//set the advise mask
				m_AdviseSink.dwMask = dwMask;

				/*
				Set the IUnknown pointer. This is used for comparison in
				UnadviseSink and future calls to this method.
				*/
				m_AdviseSink.punkID = punk;

				//get the ITextStoreACPSink interface
				m_AdviseSink.pTextStoreACPSink = (ITextStoreACPSink)punk;

				//get the ITextStoreACPServices interface
				m_pServices = (ITextStoreACPServices)punk;

				hr = HRESULT.S_OK;
			}

			return hr;
		}

		HRESULT ITextStoreACP.FindNextAttrTransition(int acpStart, int acpHalt, uint cFilterAttrs, Guid[] paFilterAttrs, TS_ATTR_FIND dwFlags, out int pacpNext, out bool pfFound, out int plFoundOffset)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pacpNext = plFoundOffset = 0;
			pfFound = false;
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITextStoreACP.GetACPFromPoint(uint vcView, in Point ptScreen, GXFPF dwFlags, out int pacp)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pacp = 0;
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITextStoreACP.GetActiveView(out uint pvcView)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			//this app only supports one view, so this can be constant
			pvcView = EDIT_VIEW_COOKIE;

			return HRESULT.S_OK;
		}

		HRESULT ITfFunctionProvider.GetDescription(out string pbstrDesc)
		{
			pbstrDesc = "TSFApp Function Provider";
			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.GetEmbedded(int acpPos, in Guid rguidService, in Guid riid, out object ppunk)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			ppunk = default;
			//this implementation doesn't support embedded objects
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITextStoreACP.GetEndACP(out int pacp)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pacp = 0;

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READWRITE))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			GetCurrentSelection();

			pacp = m_acpEnd;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.GetFormattedText(int acpStart, int acpEnd, out System.Runtime.InteropServices.ComTypes.IDataObject ppDataObject)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			ppDataObject = default;

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READ))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			HRESULT hr;

			//get the text
			uint cchOut;

			if (-1 == acpEnd)
			{
				//get the length of all of the text
				hr = ((ITextStoreACP)this).GetText(acpStart, acpEnd, default, 0, out cchOut, default, 0, out _, out _);
			}
			else
			{
				cchOut = (uint)(acpEnd - acpStart);
				hr = HRESULT.S_OK;
			}

			if (hr.Succeeded)
			{
				var pwszTemp = new StringBuilder((int)cchOut + 1);
				hr = ((ITextStoreACP)this).GetText(acpStart, acpEnd, pwszTemp, cchOut, out _, default, 0, out _, out _);
				if (hr.Succeeded)
				{
					var pdo = new DataObject();
					//set the text in the data object
					pdo.SetText(pwszTemp.ToString());
					//get the IID_IDataObject interface
					ppDataObject = pdo;
				}
			}

			return hr;
		}

		HRESULT ITfFunctionProvider.GetFunction(in Guid rguid, in Guid riid, out object ppunk)
		{
			HRESULT hr = HRESULT.E_NOINTERFACE;

			ppunk = default;

			if (rguid == Guid.Empty)
			{
			}

			return hr;
		}

		HRESULT ITextStoreACP.GetScreenExt(uint vcView, out RECT prc)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			prc = default;

			if (EDIT_VIEW_COOKIE != vcView)
			{
				return HRESULT.E_INVALIDARG;
			}

			//no lock is necessary for this method.

			prc = ClientRectangle;
			User32.MapWindowPoints(Handle, default, ref prc);

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.GetSelection(uint ulIndex, uint ulCount, TS_SELECTION_ACP[] pSelection, out uint pcFetched)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pcFetched = 0;

			//verify pSelection
			if (default == pSelection || ulCount < 1 || pSelection.Length < 1)
			{
				return HRESULT.E_INVALIDARG;
			}

			//verify pcFetched
			if (default == pcFetched)
			{
				return HRESULT.E_INVALIDARG;
			}

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READ))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			//check the requested index
			if (TF_DEFAULT_SELECTION == ulIndex)
			{
				ulIndex = 0;
			}
			else if (ulIndex > 1)
			{
				/*
				The index is too high. This app only supports one selection.
				*/
				return HRESULT.E_INVALIDARG;
			}

			GetCurrentSelection();

			//find out which end of the selection the caret (insertion point) is
			User32.GetCaretPos(out Point pt);
			Point lPos = GetPositionFromCharIndex(m_acpStart);

			//if the caret position is the same as the start character, then the selection end is the start of the selection
			m_ActiveSelEnd = pt == lPos ? TsActiveSelEnd.TS_AE_START : TsActiveSelEnd.TS_AE_END;

			pSelection[0].acpStart = m_acpStart;
			pSelection[0].acpEnd = m_acpEnd;
			pSelection[0].style.fInterimChar = m_fInterimChar;
			if (m_fInterimChar)
			{
				/*
				fInterimChar will be set when an intermediate character has been
				set. One example of when this will happen is when an IME is being
				used to enter characters and a character has been set, but the IME
				is still active.
				*/
				pSelection[0].style.ase = TsActiveSelEnd.TS_AE_NONE;
			}
			else
			{
				pSelection[0].style.ase = m_ActiveSelEnd;
			}

			pcFetched = 1;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.GetStatus(out TS_STATUS pdcs)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			pdcs = default;

			/*
			Can be zero or:
			TS_SD_READONLY // if set, document is read only; writes will fail
			TS_SD_LOADING // if set, document is loading, expect additional inserts
			*/
			pdcs.dwDynamicFlags = 0;

			/*
			Can be zero or:
			TS_SS_DISJOINTSEL // if set, the document supports multiple selections
			TS_SS_REGIONS // if clear, the document will never contain multiple regions
			TS_SS_TRANSITORY // if set, the document is expected to have a short lifespan
			TS_SS_NOHIDDENTEXT // if set, the document will never contain hidden text (for perf)
			*/
			pdcs.dwStaticFlags = TS_SS.TS_SS_REGIONS;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.GetText(int acpStart, int acpEnd, StringBuilder pchPlain, uint cchPlainReq,
			out uint pcchPlainRet, TS_RUNINFO[] prgRunInfo, uint cRunInfoReq, out uint pcRunInfoRet, out int pacpNext)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pcchPlainRet = pcRunInfoRet = 0;
			pacpNext = 0;

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READ))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			var fDoText = cchPlainReq > 0;
			var fDoRunInfo = cRunInfoReq > 0 && prgRunInfo is not null && prgRunInfo.Length > 0;
			var cchTotal = 0;
			_ = HRESULT.E_FAIL;
			HRESULT hr;
			//validate the start pos
			if ((acpStart < 0) || (acpStart > cchTotal))
			{
				hr = TS_E_INVALIDPOS;
			}
			else
			{
				//are we at the end of the document
				if (acpStart == cchTotal)
				{
					hr = HRESULT.S_OK;
				}
				else
				{
					int cchReq;

					/*
					acpEnd will be -1 if all of the text up to the end is being requested.
					*/

					if (acpEnd >= acpStart)
					{
						cchReq = acpEnd - acpStart;
					}
					else
					{
						cchReq = cchTotal - acpStart;
					}

					if (fDoText)
					{
						if (cchReq > cchPlainReq)
						{
							cchReq = (int)cchPlainReq;
						}

						//extract the specified text range
						if (pchPlain is not null && cchPlainReq > 0)
						{
							//the text output is not default terminated
							pchPlain.Clear();
							pchPlain.Append(Text.Substring(acpStart, cchReq));
						}
					}

					//it is possible that only the length of the text is being requested
					pcchPlainRet = (uint)cchReq;

					if (fDoRunInfo)
					{
						/*
						Runs are used to separate text characters from formatting characters.

						In this example, sequences inside and including the <> are treated as
						control sequences and are not displayed.

						Plain text = "Text formatting."
						Actual text = "Text <B><I>formatting</I></B>."

						If all of this text were requested, the run sequence would look like this:

						prgRunInfo[0].type = TS_RT_PLAIN; //"Text "
						prgRunInfo[0].uCount = 5;

						prgRunInfo[1].type = TS_RT_HIDDEN; //<B><I>
						prgRunInfo[1].uCount = 6;

						prgRunInfo[2].type = TS_RT_PLAIN; //"formatting"
						prgRunInfo[2].uCount = 10;

						prgRunInfo[3].type = TS_RT_HIDDEN; //</B></I>
						prgRunInfo[3].uCount = 8;

						prgRunInfo[4].type = TS_RT_PLAIN; //"."
						prgRunInfo[4].uCount = 1;

						TS_RT_OPAQUE is used to indicate characters or character sequences
						that are in the document, but are used privately by the application
						and do not map to text. Runs of text ged with TS_RT_OPAQUE should
						NOT be included in the pchPlain or cchPlainOut [out] parameters.
						*/

						/*
						This implementation is plain text, so the text only consists of one run.
						If there were multiple runs, it would be an error to have consecuative runs
						of the same type.
						*/
						pcRunInfoRet = 1;
						prgRunInfo[0].type = TsRunType.TS_RT_PLAIN;
						prgRunInfo[0].uCount = (uint)cchReq;
					}

					pacpNext = acpStart + cchReq;

					hr = HRESULT.S_OK;
				}
			}

			return hr;
		}

		HRESULT ITextStoreACP.GetTextExt(uint vcView, int acpStart, int acpEnd, out RECT prc, out bool pfClipped)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			pfClipped = false;
			prc = default;

			if (EDIT_VIEW_COOKIE != vcView)
			{
				return HRESULT.E_INVALIDARG;
			}

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READ))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			//is this an empty request?
			if (acpStart == acpEnd)
			{
				return HRESULT.E_INVALIDARG;
			}

			RECT rc = default;
			var pwszText = Text;

			var lTextLength = TextLength;

			//are the start and end reversed?
			if (acpStart > acpEnd)
			{
				var lTemp = acpStart;
				acpStart = acpEnd;
				acpEnd = lTemp;
			}

			//request to the end of the text?
			if (-1 == acpEnd)
			{
				acpEnd = lTextLength - 1;
			}

			using Gdi32.SafeHDC hdc = User32.GetDC(Handle);
			var hfont = (HFONT)Gdi32.SelectObject(hdc, Font.ToHfont());

			//get the position of the start character
			rc.Location = GetPositionFromCharIndex(acpStart);

			//get the position of the last character
			/*
			The character offset passed to this method is inclusive. For example, if
			the first character is being requested, acpStart will be 0 and acpEnd will
			be 1. If the last character is requested, acpEnd will not equal a valid
			character, so EM_POSFROMCHAR fails. If the next character is on another
			line, EM_POSFROMCHAR won't return a valid value. To work around this, get
			the position of the beginning of the end character, calculate the width of
			the end character and add the width to the rectangle.
			*/
			acpEnd--;
			Point ptEnd = GetPositionFromCharIndex(acpEnd);

			//calculate the width of the last character
			Gdi32.GetTextExtentPoint32(hdc, pwszText + acpEnd, 1, out SIZE size);
			rc.right = ptEnd.X + size.cx;
			rc.bottom = ptEnd.Y;

			//calculate the line height
			Gdi32.GetTextMetrics(hdc, out Gdi32.TEXTMETRIC tm);
			var lLineHeight = tm.tmHeight;

			Gdi32.SelectObject(hdc, hfont);

			/*
			If the text range spans multiple lines, expand the rectangle to include all
			of the requested text.
			*/
			if (rc.bottom > rc.top)
			{
				RECT rcEdit = ClientRectangle;
				var dwMargins = (uint)User32.SendMessage(Handle, User32.EditMessage.EM_GETMARGINS).ToInt32();

				//set the left point of the rectangle to the left margin of the edit control
				rc.left = Macros.LOWORD(dwMargins);

				//set the right member to the width of the edit control less both the right margin
				rc.right -= Macros.HIWORD(dwMargins);
			}

			//add the line height to the bottom of the rectangle
			rc.bottom += lLineHeight;

			prc = rc;

			//if any part of the text rectangle is not visible, set *pfClipped to true
			rc = ClientRectangle;

			if ((prc.left < rc.left) || (prc.top < rc.top) || (prc.right > rc.right) || (prc.bottom > rc.bottom))
			{
				pfClipped = true;
			}

			//convert the rectangle to screen coordinates
			User32.MapWindowPoints(Handle, default, ref prc);

			return HRESULT.S_OK;
		}

		HRESULT ITfFunctionProvider.GetType(out Guid pguid)
		{
			pguid = Guid.Empty;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.GetWnd(uint vcView, out HWND phwnd)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			if (EDIT_VIEW_COOKIE == vcView)
			{
				phwnd = Parent.Handle;
				return HRESULT.S_OK;
			}

			phwnd = default;
			return HRESULT.E_INVALIDARG;
		}

		HRESULT ITextStoreACP.InsertEmbedded(TS_IE dwFlags, int acpStart, int acpEnd, System.Runtime.InteropServices.ComTypes.IDataObject pDataObject, out TS_TEXTCHANGE pChange)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pChange = default;
			//this implementation doesn't support embedded objects
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITextStoreACP.InsertEmbeddedAtSelection(TS_IAS dwFlags, System.Runtime.InteropServices.ComTypes.IDataObject pDataObject, out int pacpStart, out int pacpEnd, out TS_TEXTCHANGE pChange)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pacpStart = pacpEnd = 0;
			pChange = default;
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITextStoreACP.InsertTextAtSelection(TS_IAS dwFlags, string pchText, uint cch, out int pacpStart, out int pacpEnd, out TS_TEXTCHANGE pChange)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pacpStart = pacpEnd = 0;
			pChange = default;

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READWRITE))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			//verify pwszText
			if (pchText is null)
			{
				return HRESULT.E_INVALIDARG;
			}

			GetCurrentSelection();

			var acpOldEnd = m_acpEnd;
			TestInsert(m_acpStart, m_acpEnd, cch, out var acpStart, out var acpNewEnd);

			if ((dwFlags & TS_IAS.TS_IAS_QUERYONLY) != 0)
			{
				pacpStart = acpStart;
				pacpEnd = acpOldEnd;
				return HRESULT.S_OK;
			}

			var pwszCopy = pchText;

			//don't notify TSF of text and selection changes when in response to a TSF action
			m_fNotify = false;

			//insert the text
			SelectedText = pwszCopy;

			//set the selection
			Select(acpStart, acpNewEnd);

			m_fNotify = true;

			GetCurrentSelection();

			if ((dwFlags & TS_IAS.TS_IAS_NOQUERY) == 0)
			{
				pacpStart = acpStart;
				pacpEnd = acpNewEnd;
			}

			//set the TS_TEXTCHANGE members
			pChange.acpStart = acpStart;
			pChange.acpOldEnd = acpOldEnd;
			pChange.acpNewEnd = acpNewEnd;

			//defer the layout change notification until the document is unlocked
			m_fLayoutChanged = true;

			return HRESULT.S_OK;
		}

		HRESULT ITfContextOwnerCompositionSink.OnEndComposition(ITfCompositionView pComposition)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			//find the composition pointer in the array
			for (var i = 0; i < MAX_COMPOSITIONS; i++)
			{
				if (pComposition == m_rgCompositions[i])
				{
					m_rgCompositions[i] = default;
					m_cCompositions--;
					break;
				}
			}

			UpdateStatusBar();

			return HRESULT.S_OK;
		}

		HRESULT ITfContextOwnerCompositionSink.OnStartComposition(ITfCompositionView pComposition, out bool pfOk)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			pfOk = true;

			if (m_cCompositions >= MAX_COMPOSITIONS)
			{
				//can't handle any more compositions
				pfOk = false;
				return HRESULT.S_OK;
			}

			m_cCompositions++;

			//find an empty slot to put the composition pointer in
			for (var i = 0; i < MAX_COMPOSITIONS; i++)
			{
				if (default == m_rgCompositions[i])
				{
					m_rgCompositions[i] = pComposition;
					break;
				}
			}

			UpdateStatusBar();

			return HRESULT.S_OK;
		}

		HRESULT ITfContextOwnerCompositionSink.OnUpdateComposition(ITfCompositionView pComposition, ITfRange pRangeNew)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			UpdateStatusBar();

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.QueryInsert(int acpTestStart, int acpTestEnd, uint cch, out int pacpResultStart, out int pacpResultEnd)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			pacpResultStart = pacpResultEnd = 0;

			var lTextLength = TextLength;

			//make sure the parameters are within range of the document
			if ((acpTestStart > acpTestEnd) || (acpTestEnd > lTextLength))
			{
				return HRESULT.E_INVALIDARG;
			}

			//set the start point to the given start point
			pacpResultStart = acpTestStart;

			//set the end point to the given end point
			pacpResultEnd = acpTestEnd;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.QueryInsertEmbedded(GuidPtr pguidService, IntPtr pFormatEtc, out bool pfInsertable)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			//this implementation doesn't support embedded objects
			pfInsertable = false;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.RequestAttrsAtPosition(int acpPos, uint cFilterAttrs, Guid[] paFilterAttrs, TS_ATTR_FIND dwFlags)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			var cch = TextLength;

			if (acpPos < 0 || acpPos > cch)
			{
				return TS_E_INVALIDPOS;
			}

			ClearRequestedAttributes();

			/*
			This app doesn't maintain per-character attributes, so just return the default attributes.
			*/
			for (var i = 0; i < NUM_SUPPORTED_ATTRS; i++)
			{
				for (uint x = 0; x < cFilterAttrs; x++)
				{
					if (m_rgAttributes[i].attrid == paFilterAttrs[x])
					{
						m_rgAttributes[i].dwFlags = ATTR_FLAG_REQUESTED;
						if ((dwFlags & TS_ATTR_FIND.TS_ATTR_FIND_WANT_VALUE) != 0)
						{
							m_rgAttributes[i].dwFlags |= ATTR_FLAG_DEFAULT;
						}
						else
						{
							//just copy the default value into the regular value
							m_rgAttributes[i].varValue = m_rgAttributes[i].varDefaultValue;
						}
					}
				}
			}

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.RequestAttrsTransitioningAtPosition(int acpPos, uint cFilterAttrs, Guid[] paFilterAttrs, TS_ATTR_FIND dwFlags)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITextStoreACP.RequestLock(TS_LF dwLockFlags, out HRESULT phrSession)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);

			phrSession = HRESULT.E_FAIL;

			if (default == m_AdviseSink.pTextStoreACPSink)
			{
				return HRESULT.E_UNEXPECTED;
			}

			if (default == phrSession)
			{
				return HRESULT.E_INVALIDARG;
			}

			if (m_fLocked)
			{
				//the document is locked

				if (dwLockFlags.IsFlagSet(TS_LF.TS_LF_SYNC))
				{
					/*
					The caller wants an immediate lock, but this cannot be granted because
					the document is already locked.
					*/
					phrSession = TS_E_SYNCHRONOUS;
					return HRESULT.S_OK;
				}
				else
				{
					//the request is asynchronous

					/*
					The only type of asynchronous lock request this application
					supports while the document is locked is to upgrade from a read
					lock to a read/write lock. This scenario is referred to as a lock
					upgrade request.
					*/
					if (((m_dwLockType & TS_LF.TS_LF_READWRITE) == TS_LF.TS_LF_READ) &&
						((dwLockFlags & TS_LF.TS_LF_READWRITE) == TS_LF.TS_LF_READWRITE))
					{
						m_fPendingLockUpgrade = true;

						phrSession = TS_S_ASYNC;

						return HRESULT.S_OK;
					}
				}
				return HRESULT.E_FAIL;
			}

			//lock the document
			LockDocument(dwLockFlags);

			//call OnLockGranted
			phrSession = m_AdviseSink.pTextStoreACPSink.OnLockGranted(dwLockFlags);

			//unlock the document
			UnlockDocument();

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.RequestSupportedAttrs(TS_ATTR_FIND dwFlags, uint cFilterAttrs, Guid[] paFilterAttrs)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			ClearRequestedAttributes();

			for (var i = 0; i < NUM_SUPPORTED_ATTRS; i++)
			{
				for (uint x = 0; x < cFilterAttrs; x++)
				{
					if (m_rgAttributes[i].attrid == paFilterAttrs[x])
					{
						m_rgAttributes[i].dwFlags = ATTR_FLAG_REQUESTED;
						if ((dwFlags & TS_ATTR_FIND.TS_ATTR_FIND_WANT_VALUE) != 0)
						{
							m_rgAttributes[i].dwFlags |= ATTR_FLAG_DEFAULT;
						}
						else
						{
							//just copy the default value into the regular value
							m_rgAttributes[i].varValue = m_rgAttributes[i].varDefaultValue;
						}
					}
				}
			}

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.RetrieveRequestedAttrs(uint ulCount, TS_ATTRVAL[] paAttrVals, out uint pcFetched)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			uint uFetched = 0;

			for (var i = 0; i < NUM_SUPPORTED_ATTRS && ulCount > 0; i++)
			{
				if ((m_rgAttributes[i].dwFlags & ATTR_FLAG_REQUESTED) != 0)
				{
					paAttrVals[uFetched].varValue = null;

					//copy the attribute ID
					paAttrVals[uFetched].idAttr = m_rgAttributes[i].attrid;

					//this app doesn't support overlapped attributes
					paAttrVals[uFetched].dwOverlapId = 0;

					if ((m_rgAttributes[i].dwFlags & ATTR_FLAG_DEFAULT) != 0)
					{
						paAttrVals[uFetched].varValue = m_rgAttributes[i].varDefaultValue;
					}
					else
					{
						paAttrVals[uFetched].varValue = m_rgAttributes[i].varValue;
					}

					uFetched++;
					ulCount--;

					//remove the item from the requested state
					m_rgAttributes[i].varValue = null;
					m_rgAttributes[i].dwFlags = ATTR_FLAG_NONE;
				}
			}

			pcFetched = uFetched;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.SetSelection(uint ulCount, TS_SELECTION_ACP[] pSelection)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			//verify pSelection
			if (pSelection is null || ulCount != 1 || pSelection.Length < 1)
			{
				return HRESULT.E_INVALIDARG;
			}

			//does the caller have a lock
			if (!IsLocked(TS_LF.TS_LF_READWRITE))
			{
				//the caller doesn't have a lock
				return TS_E_NOLOCK;
			}

			m_acpStart = pSelection[0].acpStart;
			m_acpEnd = pSelection[0].acpEnd;
			m_fInterimChar = pSelection[0].style.fInterimChar;
			if (m_fInterimChar)
			{
				/*
				fInterimChar will be set when an intermediate character has been
				set. One example of when this will happen is when an IME is being
				used to enter characters and a character has been set, but the IME
				is still active.
				*/
				m_ActiveSelEnd = TsActiveSelEnd.TS_AE_NONE;
			}
			else
			{
				m_ActiveSelEnd = pSelection[0].style.ase;
			}

			//if the selection end is at the start of the selection, reverse the parameters
			var lStart = m_acpStart;
			var lEnd = m_acpEnd;

			if (TsActiveSelEnd.TS_AE_START == m_ActiveSelEnd)
			{
				lStart = m_acpEnd;
				lEnd = m_acpStart;
			}

			m_fNotify = false;

			Select(lStart, lEnd);

			m_fNotify = true;

			return HRESULT.S_OK;
		}

		HRESULT ITextStoreACP.SetText(TS_ST dwFlags, int acpStart, int acpEnd, string pchText, uint cch, out TS_TEXTCHANGE pChange)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			HRESULT hr;

			/*
			dwFlags can be:
			TS_ST_CORRECTION
			*/

			if ((dwFlags & TS_ST.TS_ST_CORRECTION) != 0)
			{
				System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			}

			//set the selection to the specified range
			TS_SELECTION_ACP tsa = new()
			{
				acpStart = acpStart,
				acpEnd = acpEnd,
				style = new TS_SELECTIONSTYLE { ase = TsActiveSelEnd.TS_AE_START, fInterimChar = false }
			};

			hr = ((ITextStoreACP)this).SetSelection(1, new[] { tsa });

			if (hr.Succeeded)
			{
				//call InsertTextAtSelection
				hr = ((ITextStoreACP)this).InsertTextAtSelection(TS_IAS.TS_IAS_NOQUERY, pchText, cch, out _, out _, out pChange);
			}
			else
			{
				pChange = default;
			}

			return hr;
		}

		HRESULT ITextStoreACP.UnadviseSink(object pUnknown)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			HRESULT hr;

			//find the advise sink
			if (Marshal.GetIUnknownForObject(m_AdviseSink.punkID) == Marshal.GetIUnknownForObject(pUnknown))
			{
				//remove the advise sink from the list
				ClearAdviseSink(m_AdviseSink);

				m_pServices = null;

				hr = HRESULT.S_OK;
			}
			else
			{
				hr = CONNECT_E_NOCONNECTION;
			}

			return hr;
		}

		/// <summary>Clean up any resources being used.</summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
			}

			/*
			Make sure the advise sink is cleaned up. This should have been done
			before, but this is just in case.
			*/
			ClearAdviseSink(m_AdviseSink);

			m_pServices = default;

			Uninitialize();

			base.Dispose(disposing);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			if (m_pDocMgr is not null)
				m_pThreadMgr.SetFocus(m_pDocMgr);
			base.OnGotFocus(e);
		}

		protected override void OnParentChanged(EventArgs e)
		{
			m_pThreadMgr = Program.g_pThreadMgr.Value;
			m_pCategoryMgr = new();

			//create the display attribute manager
			m_pDisplayAttrMgr = new();

			//create the document manager
			m_pDocMgr = m_pThreadMgr.CreateDocumentMgr();

			//create the context
			m_pDocMgr.CreateContext(TfClientID, 0, this, out m_pContext, out m_EditCookie);

			//push the context onto the document stack
			m_pDocMgr.Push(m_pContext);

			UpdateStatusBar();

			/*
			Associate the focus with this window. The TSF Manager watches for
			focus changes throughout the system. When a window handle that has
			been associated gets the focus, it then knows the window receiving
			the focus is TSF enabled.
			*/
			m_pPrevDocMgr = m_pThreadMgr.AssociateFocus(Handle, m_pDocMgr);

			//initialize the supported attributes

			//mode bias
			var guidatom = m_pCategoryMgr.RegisterGUID(GUID_MODEBIAS_NONE);
			m_rgAttributes[ATTR_INDEX_MODEBIAS] = new() { attrid = GUID_PROP_MODEBIAS, varDefaultValue = guidatom };
				//text orientation - this is a VT_I4 that is always zero in this app
			m_rgAttributes[ATTR_INDEX_TEXT_ORIENTATION] = new() { attrid = TSATTRID.TSATTRID_Text_Orientation, varDefaultValue = 0 };

			//vertical writing - this is a VT_BOOL that is always false in this app
			m_rgAttributes[ATTR_INDEX_TEXT_VERTICALWRITING] = new() { attrid = TSATTRID.TSATTRID_Text_VerticalWriting, varDefaultValue = false };

			InitFunctionProvider();

			Parent.Update();
		}
		protected override void OnTextChanged(EventArgs e)
		{
			System.Diagnostics.Debug.WriteLine(System.Reflection.MethodBase.GetCurrentMethod().Name);
			if (m_fNotify && m_AdviseSink.pTextStoreACPSink is not null && m_AdviseSink.dwMask.IsFlagSet(TS_AS.TS_AS_TEXT_CHANGE))
			{
				var cch = TextLength;

				TS_TEXTCHANGE tc = new() { acpStart = 0, acpOldEnd = m_cchOldLength, acpNewEnd = cch };

				// dwFlags can be 0 or TS_TC_CORRECTION
				TS_ST dwFlags = 0;
				m_AdviseSink.pTextStoreACPSink.OnTextChange(dwFlags, tc);

				m_cchOldLength = cch;
			}
			base.OnTextChanged(e);
		}

		private static void ClearAdviseSink(in ADVISE_SINK pAdviseSink)
		{
			pAdviseSink.punkID = default;
			pAdviseSink.pTextStoreACPSink = default;
			pAdviseSink.dwMask = 0;
		}

		private static HRESULT TestInsert(int acpTestStart, int acpTestEnd, uint cch, out int pacpResultStart, out int pacpResultEnd)
		{
			pacpResultStart = pacpResultEnd = 0;

			//make sure the parameters are within range of the document
			if (acpTestStart > acpTestEnd)
			{
				return HRESULT.E_INVALIDARG;
			}

			//set the start point after the insertion
			pacpResultStart = acpTestStart;

			//set the end point after the insertion
			pacpResultEnd = acpTestStart + (int)cch;

			return HRESULT.S_OK;
		}

		private void ClearRequestedAttributes()
		{
			for (var i = 0; i < NUM_SUPPORTED_ATTRS; i++)
			{
				m_rgAttributes[i].varValue = null;
				m_rgAttributes[i].dwFlags = ATTR_FLAG_NONE;
			}
		}

		private void ClearText()
		{
			//can't do this if someone has a lock
			if (IsLocked(TS_LF.TS_LF_READ))
			{
				return;
			}

			LockDocument(TS_LF.TS_LF_READWRITE);

			//empty the text in the edit control, but don't send a change notification
			var fOldNotify = m_fNotify;
			m_fNotify = false;
			Text = default;
			m_fNotify = fOldNotify;

			//update current selection
			m_acpStart = m_acpEnd;

			//notify TSF about the changes
			m_AdviseSink.pTextStoreACPSink.OnSelectionChange();

			OnTextChanged(EventArgs.Empty);

			UnlockDocument();

			// make sure to send the OnLayoutChange notification AFTER releasing the lock so clients can do something useful during the notification
			m_AdviseSink.pTextStoreACPSink.OnLayoutChange(TsLayoutCode.TS_LC_CHANGE, EDIT_VIEW_COOKIE);
		}

		private bool GetCurrentSelection()
		{
			//get the selection from the edit control
			m_acpStart = SelectionStart;
			m_acpEnd = SelectionStart + SelectionLength - 1;

			return true;
		}

		private TF_SELECTION? GetFirstSelection(uint editCookie, uint index = TF_DEFAULT_SELECTION)
		{
			var ts = new TF_SELECTION[1];
			try
			{
				m_pContext.GetSelection(editCookie, index, 1, ts, out var uFetched);
				return uFetched == 0 ? null : ts[0];
			}
			catch { return null; }
		}

		private IEnumerable<T> GetPropVals<T>(Guid prop)
		{
			//get the tracking property for the attributes
			var rGuidProperties = new Guid[] { prop };
			ITfReadOnlyProperty pTrackProperty = m_pContext.TrackProperties(rGuidProperties, 1, default, 0);

			//get the range of the entire text
			ITfRangeACP pRangeAllText = m_pServices.CreateRange(0, base.TextLength);

			pTrackProperty.EnumRanges(m_EditCookie, out IEnumTfRanges pEnumRanges, pRangeAllText);

			/*
			Each range in pEnumRanges represents a span of text that has
			the same properties specified in TrackProperties.
			*/
			//foreach (ITfRange pPropRange in new Vanara.Collections.IEnumFromCom<ITfRange>((uint celt, ITfRange[] rgelt, out uint celtFetched) => pEnumRanges.Next(celt, rgelt, out celtFetched), () => pEnumRanges.Reset()))
			foreach (ITfRange pPropRange in Vanara.Collections.IEnumFromCom<ITfRange>.Create(pEnumRanges))
			{
				//get the attribute property for the property range
				/*
				The property is actually a VT_UNKNOWN that contains an
				IEnumTfPropertyValue object.
				*/
				var pEnumPropertyVal = (IEnumTfPropertyValue)pTrackProperty.GetValue(m_EditCookie, pPropRange);

				/*
				The GUID_PROP_TEXTOWNER attribute value is the in Guid of the text service that owns the text. If the text is not owned, the value is VT_EMPTY.
				*/
				foreach (T val in Vanara.Collections.IEnumFromCom<TF_PROPERTYVAL>.Create(pEnumPropertyVal).OfType<T>())
				{
					yield return val;
				}
			}
		}

		private bool InitFunctionProvider()
		{
			try
			{
				var pSourceSingle = (ITfSourceSingle)m_pThreadMgr;
				pSourceSingle.AdviseSingleSink(TfClientID, typeof(ITfFunctionProvider).GUID, this);
				return true;
			}
			catch { return false; }
		}

		private bool InternalLockDocument(TS_LF dwLockFlags)
		{
			m_dwInternalLockType = dwLockFlags;
			return true;
		}

		private void InternalUnlockDocument() => m_dwInternalLockType = 0;

		private void Load(IStream pStream)
		{
			if (pStream is null)
				return;

			//can't do this if someone has a lock
			if (IsLocked(TS_LF.TS_LF_READ))
				return;

			ClearText();

			//set the stream pointer to the start of the stream
			pStream.Seek(0, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);

			unsafe
			{
				//get the size of the text, in BYTES. This is the first uint in the stream
				uint uRead = 0;
				var uSizeBytes = new byte[sizeof(uint)];
				pStream.Read(uSizeBytes, sizeof(uint), (IntPtr)(&uRead));
				if (sizeof(uint) == uRead)
				{
					//allocate a buffer for the text plus one default character
					var uSize = BitConverter.ToInt32(uSizeBytes, 0);
					var pwsz = new byte[uSize + sizeof(ushort)];

					//get the plain UNICODE text from the stream
					pStream.Read(pwsz, uSize, (IntPtr)(&uRead));
					if (uSize == uRead)
					{
						//put the text into the edit control, but don't send a change notification
						var fOldNotify = m_fNotify;
						m_fNotify = false;
						Text = Encoding.Unicode.GetString(pwsz);
						m_fNotify = fOldNotify;

						/*
						Read each property header and property data from the stream. The
						list of properties is terminated by a TF_PERSISTENT_PROPERTY_HEADER_ACP
						structure with a cb member of zero.
						*/
						var phBytes = new byte[sizeof(TF_PERSISTENT_PROPERTY_HEADER_ACP)];
						fixed (byte* ptr = phBytes)
						{
							pStream.Read(phBytes, phBytes.Length, (IntPtr)(&uRead));
							while (sizeof(TF_PERSISTENT_PROPERTY_HEADER_ACP) == uRead)
							{
								var PropHeader = (TF_PERSISTENT_PROPERTY_HEADER_ACP*)ptr;
								if (PropHeader->cb == 0)
									break;

								ITfProperty pProp = m_pContext.GetProperty(PropHeader->guidType);

								/*
								Have TSF read the property data from the stream. This call
								will request a read lock, so make sure it can be granted
								or else this method will fail.
								*/
								CTSFPersistentPropertyLoader pLoader = new(*PropHeader, pStream);
								m_pServices.Unserialize(pProp, *PropHeader, default, pLoader);
							}

							pStream.Read(phBytes, phBytes.Length, (IntPtr)(&uRead));
						}
					}
				}
			}
		}

		private bool LockDocument(TS_LF dwLockFlags)
		{
			if (m_fLocked)
				return false;

			m_fLocked = true;
			m_dwLockType = dwLockFlags;

			return true;
		}

		private void Save(IStream pStream)
		{
			if (pStream is null)
				return;

			//write the plain UNICODE text into the stream
			long li = 0;

			//set the stream pointer to the start of the stream
			pStream.Seek(li, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);

			//write the size, in BYTES, of the text
			var uSize = (uint)(TextLength + 1) * sizeof(ushort);
			pStream.Write(BitConverter.GetBytes(uSize), sizeof(uint), default);

			//write the text, including the default_terminator, into the stream
			pStream.Write(StringHelper.GetBytes(Text, true, CharSet.Unicode), (int)uSize, default);

			//enumerate the properties in the context
			foreach (ITfProperty pProp in Vanara.Collections.IEnumFromCom<ITfProperty>.Create(m_pContext.EnumProperties()))
			{
				//create a temporary stream to write the property data to
				Ole32.CreateStreamOnHGlobal(default, true, out IStream pTempStream).ThrowIfFailed();

				//enumerate all the ranges that contain the property
				pProp.EnumRanges(m_EditCookie, out IEnumTfRanges pEnumRanges);
				foreach (ITfRange pRange in Vanara.Collections.IEnumFromCom<ITfRange>.Create(pEnumRanges))
				{
					//reset the temporary stream's pointer
					li = 0;
					pTempStream.Seek(li, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);

					//get the property header and data for the range
					m_pServices.Serialize(pProp, pRange, out TF_PERSISTENT_PROPERTY_HEADER_ACP PropHeader, pTempStream);

					/*
					Write the property header into the primary stream.
					The header also contains the size of the property
					data.
					*/
					using (var ph = new SafeCoTaskMemStruct<TF_PERSISTENT_PROPERTY_HEADER_ACP>(PropHeader))
						pStream.Write(ph.GetBytes(0, ph.Size), ph.Size, default);

					//reset the temporary stream's pointer
					li = 0;
					pTempStream.Seek(li, (int)Ole32.STREAM_SEEK.STREAM_SEEK_SET, default);

					//copy the property data from the temporary stream into the primary stream
					long uli = PropHeader.cb;

					pTempStream.CopyTo(pStream, uli, default, default);
				}

				pTempStream = null;
			}

			//write a property header with zero size and guid into the stream as a terminator
			using (var ph = new SafeCoTaskMemStruct<TF_PERSISTENT_PROPERTY_HEADER_ACP>())
				pStream.Write(ph.GetBytes(0, ph.Size), ph.Size, default);
		}

		private void UninitFunctionProvider()
		{
			var pSourceSingle = (ITfSourceSingle)m_pThreadMgr;
			try { pSourceSingle.UnadviseSingleSink(TfClientID, typeof(ITfFunctionProvider).GUID); } catch { }
		}

		private void Uninitialize()
		{
			if (m_pThreadMgr is null)
				return;

			UninitFunctionProvider();

			/*
			Its okay if m_pPrevDocMgr is default as this will just disassociate the
			focus from the window.
			*/
			ITfDocumentMgr pTempDocMgr = m_pThreadMgr.AssociateFocus(Handle, m_pPrevDocMgr);
			if (pTempDocMgr is not null)
				Marshal.ReleaseComObject(pTempDocMgr);

			if (m_pPrevDocMgr is not null)
			{
				Marshal.ReleaseComObject(m_pPrevDocMgr);
				m_pPrevDocMgr = null;
			}

			//for (int i = 0; i < NUM_SUPPORTED_ATTRS; i++)
			//{
			//	VariantClear(&m_rgAttributes[i].varValue);
			//	VariantClear(&m_rgAttributes[i].varDefaultValue);
			//	m_rgAttributes[i].dwFlags = ATTR_FLAG_NONE;
			//}

			if (m_pDocMgr is not null)
			{
				//pop all of the contexts off of the stack
				m_pDocMgr.Pop(TF_POPF.TF_POPF_ALL);
				m_pDocMgr = default;
			}

			m_pDisplayAttrMgr = default;
			m_pContext = default;
			m_pCategoryMgr = default;
			m_pThreadMgr = default;
		}
		private void UnlockDocument()
		{
			m_fLocked = false;
			m_dwLockType = 0;

			//if there is a pending lock upgrade, grant it
			if (m_fPendingLockUpgrade)
			{
				m_fPendingLockUpgrade = false;

				((ITextStoreACP)this).RequestLock(TS_LF.TS_LF_READWRITE, out _);
			}

			//if any layout changes occurred during the lock, notify the manager
			if (m_fLayoutChanged)
			{
				m_fLayoutChanged = false;
				m_AdviseSink.pTextStoreACPSink.OnLayoutChange(TsLayoutCode.TS_LC_CHANGE, EDIT_VIEW_COOKIE);
			}
		}

		private void UpdateStatusBar()
		{
			var szComposition = m_cCompositions > 0 ? "In Composition" : "No Composition";
			StatusUpdate?.Invoke(szComposition, string.Empty);
		}

		private class ADVISE_SINK
		{
			public TS_AS dwMask;
			public ITextStoreACPSink pTextStoreACPSink;
			public object punkID;
		}

		private class ATTRIBUTES
		{
			public Guid attrid;
			public uint dwFlags;
			public object varDefaultValue;
			public object varValue;
		}
	}
}