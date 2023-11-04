using System.Runtime.InteropServices;
using Vanara.Collections;
using Vanara.PInvoke;
using static Vanara.PInvoke.MSCTF;

namespace tsfcase
{
	[ComVisible(true), Guid("6912EA1F-B14D-4E4A-9490-05ABF0C3E94D"), ClassInterface(ClassInterfaceType.None)]
	public class CCaseTextService : ITfTextInputProcessor, ITfThreadMgrEventSink, ITfTextEditSink, ITfThreadFocusSink, ITfKeyEventSink
	{
		internal readonly (string, Action, Func<bool>)[] c_rgMenuItems;

		private const string c_szCaseDesc = "Case Text Service";

		private const string c_szCaseDescW = "Case Text Service";

		private const string c_szCaseModel = "Apartment";

		private const string c_szInfoKeyPrefix = "CLSID\\";

		private const string c_szInProcSvr32 = "InProcServer32";

		private const string c_szModelName = "ThreadingModel";

		private const uint CASE_ICON_INDEX = 0;

		// arbitrary hotkey: ctl-f
		private static readonly TF_PRESERVEDKEY c_FlipCaseKey = new() { uVKey = 'F', uModifiers = TF_MOD.TF_MOD_CONTROL };

		private static readonly Guid c_guidCaseProfile = new(0x4d5459db, 0x7543, 0x42c0, 0x92, 0x04, 0x91, 0x95, 0xb9, 0x1f, 0x6f, 0xb8);

		private static readonly (Guid pguidCategory, Guid pguid)[] c_rgCategories = new (Guid pguidCategory, Guid pguid)[]
		{
			(GUID_TFCAT_TIP_KEYBOARD, typeof(CCaseTextService).GUID)
		};

		private static readonly LANGID CASE_LANGID = new(LANGID.LANG.LANG_ENGLISH, LANGID.SUBLANG.SUBLANG_ENGLISH_US);
		private static readonly Guid GUID_PRESERVEDKEY_FLIPCASE = new(0x5d6d1b1e, 0x64f2, 0x47cd, 0x9f, 0xe1, 0x4e, 0x03, 0x2c, 0x2d, 0xae, 0x77);
		private readonly uint dwThreadFocusSinkCookie = TF_INVALID_COOKIE;
		private readonly bool fShowSnoop;
		private readonly CLangBarItemButton pLangBarItem;

		// hide/show the snoop window popup
		private readonly CSnoopWnd pSnoopWnd;

		private uint dwTextEditSinkCookie = TF_INVALID_COOKIE;
		private uint dwThreadMgrEventSinkCookie = TF_INVALID_COOKIE;
		private bool fFlipKeys;
		private ITfContext pTextEditSinkContext;
		private ITfThreadMgr pThreadMgr;
		private uint tfClientId;

		public CCaseTextService()
		{
			c_rgMenuItems = new (string, Action, Func<bool>?)[]
			{
				( "Show Snoop Wnd", Menu_ShowSnoopWnd, IsSnoopWndVisible ), // must match MENU_SHOWSNOOP_INDEX
				( "Hello World", Menu_HelloWord, null),
				( "Flip Selection", Menu_FlipSel, null ),
				( "Flip Doc", Menu_FlipDoc, null ),
				( "Flip Keystrokes", Menu_FlipKeys, IsKeyFlipping ), // must match MENU_FLIPKEYS_INDEX
			};
		}

		HRESULT ITfTextInputProcessor.Activate(ITfThreadMgr pThreadMgr, uint tfClientId)
		{
			ITfDocumentMgr pFocusDoc;

			this.pThreadMgr = pThreadMgr;

			this.tfClientId = tfClientId;

			if (!InitLanguageBar())
				goto ExitError;

			if (!InitThreadMgrSink())
				goto ExitError;

			if (!InitSnoopWnd())
				goto ExitError;

			if (!InitKeystrokeSink())
				goto ExitError;

			if (!InitPreservedKey())
				goto ExitError;

			// start tracking the focus doc
			pFocusDoc = pThreadMgr.GetFocus();

			// The system will call OnSetFocus only for focus events after Activate is called.
			((ITfThreadMgrEventSink)this).OnSetFocus(pFocusDoc, default);

			return HRESULT.S_OK;

			ExitError:
			((ITfTextInputProcessor)this).Deactivate(); // cleanup any half-finished init
			return HRESULT.E_FAIL;
		}

		HRESULT ITfTextInputProcessor.Deactivate()
		{
			UninitSnoopWnd();
			UninitThreadMgrSink();
			UninitLanguageBar();
			UninitKeystrokeSink();
			UninitPreservedKey();

			// we MUST release all refs to pThreadMgr in Deactivate
			SafeReleaseClear(pThreadMgr);

			tfClientId = TF_CLIENTID_NULL;

			return HRESULT.S_OK;
		}

		HRESULT ITfTextEditSink.OnEndEdit(ITfContext pic, uint ecReadOnly, ITfEditRecord pEditRecord)
		{
			// we'll use the endedit notification to update the snoop window

			// did the selection change?
			if (pEditRecord.GetSelectionStatus())
			{
				pSnoopWnd.UpdateText(ecReadOnly, pContext, default);
				return HRESULT.S_OK;
			}

			// text modification?
			IEnumTfRanges pEnumTextChanges = pEditRecord.GetTextAndPropertyUpdates(TF_GTP.TF_GTP_INCL_TEXT);
			ITfRange pRange = IEnumFromCom<ITfRange>.Create(pEnumTextChanges).FirstOrDefault();
			if (!(pRange is null))
			{
				// arbitrary update the snoop window with the first change there may be more than one in the enumerator, but we don't care here
				pSnoopWnd.UpdateText(ecReadOnly, pContext, pRange);
			}

			// if we get here, only property values changed

			return HRESULT.S_OK;
		}

		HRESULT ITfThreadMgrEventSink.OnInitDocumentMgr(ITfDocumentMgr pdim) => HRESULT.S_OK;

		HRESULT ITfKeyEventSink.OnKeyDown(ITfContext pContext, IntPtr wParam, IntPtr lParam, out bool pfEaten)
		{
			CKeystrokeEditSession pEditSession;
			HRESULT hr = HRESULT.S_OK;

			try
			{
				pfEaten = IsKeyEaten(fFlipKeys, wParam);

				if (pfEaten)
				{
					// we'll insert a byte ourselves in place of this keystroke
					pEditSession = new CKeystrokeEditSession(pContext);

					// we need a lock to do our work
					// nb: this method is one of the few places where it is legal to use the TF_ES_SYNC flag
					pContext.RequestEditSession(tfClientId, pEditSession, TF_ES.TF_ES_SYNC | TF_ES.TF_ES_READWRITE, out _);
				}
			}
			catch
			{
				pfEaten = false;
			}
			return HRESULT.S_OK;
		}

		HRESULT ITfKeyEventSink.OnKeyUp(ITfContext pContext, IntPtr wParam, IntPtr lParam, out bool pfEaten)
		{
			pfEaten = IsKeyEaten(fFlipKeys, wParam);
			return HRESULT.S_OK;
		}

		HRESULT ITfThreadFocusSink.OnKillThreadFocus() => throw new NotImplementedException();

		HRESULT ITfThreadMgrEventSink.OnPopContext(ITfContext pic) => HRESULT.S_OK;

		HRESULT ITfKeyEventSink.OnPreservedKey(ITfContext pContext, in Guid rguid, out bool pfEaten)
		{
			if (rguid == GUID_PRESERVEDKEY_FLIPCASE)
			{
				Menu_FlipDoc();
				pfEaten = true;
			}
			else
			{
				pfEaten = false;
			}

			return HRESULT.S_OK;
		}

		HRESULT ITfThreadMgrEventSink.OnPushContext(ITfContext pic) => HRESULT.S_OK;

		HRESULT ITfThreadMgrEventSink.OnSetFocus(ITfDocumentMgr pdimFocus, ITfDocumentMgr pdimPrevFocus)
		{
			// track text changes on the focus doc we are guarenteed a final OnSetFocus(default, ..) which we use for cleanup
			InitTextEditSink(pdimFocus);

			// let's update the snoop window with text from the new focus context
			pSnoopWnd.UpdateText(default);
			return HRESULT.S_OK;
		}

		HRESULT ITfKeyEventSink.OnSetFocus(bool fForeground) => HRESULT.S_OK;

		HRESULT ITfThreadFocusSink.OnSetThreadFocus() => throw new NotImplementedException();

		HRESULT ITfKeyEventSink.OnTestKeyDown(ITfContext pContext, IntPtr wParam, IntPtr lParam, out bool pfEaten)
		{
			pfEaten = IsKeyEaten(fFlipKeys, wParam);
			return HRESULT.S_OK;
		}

		HRESULT ITfKeyEventSink.OnTestKeyUp(ITfContext pContext, IntPtr wParam, IntPtr lParam, out bool pfEaten)
		{
			pfEaten = IsKeyEaten(fFlipKeys, wParam);
			return HRESULT.S_OK;
		}

		HRESULT ITfThreadMgrEventSink.OnUninitDocumentMgr(ITfDocumentMgr pdim) => HRESULT.S_OK;

		private bool InitKeystrokeSink()
		{
			try
			{
				var pKeystrokeMgr = (ITfKeystrokeMgr)pThreadMgr;
				pKeystrokeMgr.AdviseKeyEventSink(tfClientId, this, true);
				return true;
			}
			catch { return false; }
		}

		private bool InitPreservedKey()
		{
			const string wchToggleCase = "Toggle Case";

			try
			{
				var pKeystrokeMgr = (ITfKeystrokeMgr)pThreadMgr;
				pKeystrokeMgr.PreserveKey(tfClientId, GUID_PRESERVEDKEY_FLIPCASE, c_FlipCaseKey, wchToggleCase, (uint)wchToggleCase.Length);
				return true;
			}
			catch { return false; }
		}

		private bool InitTextEditSink(ITfDocumentMgr pDocMgr)
		{
			bool fRet;

			var pSource = (ITfSource)pTextEditSinkContext;

			// clear out any previous sink first
			if (dwTextEditSinkCookie != TF_INVALID_COOKIE)
			{
				pSource.UnadviseSink(dwTextEditSinkCookie);

				pTextEditSinkContext = default;
				dwTextEditSinkCookie = TF_INVALID_COOKIE;
			}

			if (pDocMgr is null)
				return true; // caller just wanted to clear the previous sink

			// setup a new sink advised to the topmost context of the document

			pTextEditSinkContext = pDocMgr.GetTop();

			if (pTextEditSinkContext is null)
				return true; // empty document, no sink possible

			fRet = false;

			try
			{
				pSource.AdviseSink(typeof(ITfTextEditSink).GUID, this, out dwTextEditSinkCookie);
				fRet = true;
			}
			catch
			{
				dwTextEditSinkCookie = TF_INVALID_COOKIE;
			}

			if (fRet == false)
			{
				pTextEditSinkContext = default;
			}

			return fRet;
		}

		private bool InitThreadMgrSink()
		{
			var pSource = (ITfSource)pThreadMgr;
			try { pSource.AdviseSink(typeof(ITfThreadMgrEventSink).GUID, this, out dwThreadMgrEventSinkCookie); }
			catch
			{
				// make sure we don't try to Unadvise dwThreadMgrEventSinkCookie later
				dwThreadMgrEventSinkCookie = TF_INVALID_COOKIE;
				return false;
			}

			return true;
		}

		// we're only interested in VK_A - VK_Z, when the "Flip Keys" menu option is on
		private bool IsKeyEaten(bool fFlipKeys, IntPtr wParam) => fFlipKeys && (wParam.ToInt32() >= 'A') && (wParam.ToInt32() <= 'Z');

		private void Menu_FlipDoc()
		{
			// get the focus document
			ITfDocumentMgr pFocusDoc = pThreadMgr.GetFocus();

			// we want the topmost context, since the main doc context could be superceded by a modal tip context
			ITfContext pContext = pFocusDoc.GetTop();

			var pFlipEditSession = new CFlipDocEditSession(pContext);

			// we need a document write lock to insert text the CHelloEditSession will do all the work when the
			// CFlipDocEditSession::DoEditSession method is called by the context
			pContext.RequestEditSession(tfClientId, pFlipEditSession, TF_ES.TF_ES_READWRITE | TF_ES.TF_ES_ASYNCDONTCARE, out _);
		}

		private void Menu_FlipKeys() => fFlipKeys = !fFlipKeys;

		private void Menu_FlipSel()
		{
			// get the focus document
			ITfDocumentMgr pFocusDoc = pThreadMgr.GetFocus();

			// we want the topmost context, since the main doc context could be superceded by a modal tip context
			ITfContext pContext = pFocusDoc.GetTop();

			var pFlipEditSession = new CFlipEditSession(pContext);

			// we need a document write lock to insert text the CHelloEditSession will do all the work when the
			// CFlipDocEditSession::DoEditSession method is called by the context
			pContext.RequestEditSession(tfClientId, pFlipEditSession, TF_ES.TF_ES_READWRITE | TF_ES.TF_ES_ASYNCDONTCARE, out _);
		}

		private void Menu_HelloWord()
		{
			// get the focus document
			ITfDocumentMgr pFocusDoc = pThreadMgr.GetFocus();

			// we want the topmost context, since the main doc context could be superceded by a modal tip context
			ITfContext pContext = pFocusDoc.GetTop();

			var pHelloEditSession = new CHelloEditSession(pContext);

			// we need a document write lock to insert text the CHelloEditSession will do all the work when the
			// CHelloEditSession::DoEditSession method is called by the context
			pContext.RequestEditSession(tfClientId, pHelloEditSession, TF_ES.TF_ES_READWRITE | TF_ES.TF_ES_ASYNCDONTCARE, out _);
		}

		private bool RegisterCategories(bool fRegister)
		{
			var pCategoryMgr = new ITfCategoryMgr();

			foreach ((Guid pguidCategory, Guid pguid) in c_rgCategories)
			{
				try
				{
					if (fRegister)
					{
						pCategoryMgr.RegisterCategory(typeof(CCaseTextService).GUID, pguidCategory, pguid);
					}
					else
					{
						pCategoryMgr.UnregisterCategory(typeof(CCaseTextService).GUID, pguidCategory, pguid);
					}
				}
				catch { return false; }
			}
			return true;
		}

		private bool RegisterProfiles()
		{
			try
			{
				var pInputProcessProfiles = new ITfInputProcessorProfiles();
				pInputProcessProfiles.Register(typeof(CCaseTextService).GUID);

				var achIconFile = System.Reflection.Assembly.GetExecutingAssembly().Location;

				pInputProcessProfiles.AddLanguageProfile(typeof(CCaseTextService).GUID, CASE_LANGID, c_guidCaseProfile, c_szCaseDescW,
					(uint)c_szCaseDescW.Length, achIconFile, (uint)achIconFile.Length, CASE_ICON_INDEX);

				return true;
			}
			catch { return false; }
		}

		private void UninitKeystrokeSink()
		{
			var pKeystrokeMgr = (ITfKeystrokeMgr)pThreadMgr;
			pKeystrokeMgr.UnadviseKeyEventSink(tfClientId);
		}

		private void UninitPreservedKey()
		{
			var pKeystrokeMgr = (ITfKeystrokeMgr)pThreadMgr;
			pKeystrokeMgr.UnpreserveKey(GUID_PRESERVEDKEY_FLIPCASE, c_FlipCaseKey);
		}

		private void UninitThreadMgrSink()
		{
			if (dwThreadMgrEventSinkCookie == TF_INVALID_COOKIE)
				return; // never Advised

			try
			{
				var pSource = (ITfSource)pThreadMgr;
				pSource.UnadviseSink(dwThreadMgrEventSinkCookie);
			}
			catch { }

			dwThreadMgrEventSinkCookie = TF_INVALID_COOKIE;
		}

		/* 5d6d1b1e-64f2-47cd-9fe1-4e032c2dae77 */

		private void UnregisterProfiles()
		{
			var pInputProcessProfiles = new ITfInputProcessorProfiles();
			pInputProcessProfiles.Unregister(typeof(CCaseTextService).GUID);
		}
	}

	internal class CFlipDocEditSession : CEditSessionBase
	{
		public CFlipDocEditSession(ITfContext pContext) : base(pContext)
		{
		}

		public override HRESULT DoEditSession([In] uint ec)
		{
			// get the head of the doc
			ITfRange pRangeStart = pContext.GetStart(ec);

			// do the work
			ToggleCase(ec, pRangeStart, true);

			return HRESULT.S_OK;
		}
	}

	internal class CFlipEditSession : CEditSessionBase
	{
		public CFlipEditSession(ITfContext pContext) : base(pContext)
		{
		}

		public override HRESULT DoEditSession([In] uint ec)
		{
			// get the head of the doc
			var tfSelection = new TF_SELECTION[1];
			try
			{
				pContext.GetSelection(ec, TF_DEFAULT_SELECTION, 1, tfSelection, out var cFetched);
				if (cFetched == 0)
					return HRESULT.S_OK;
			}
			catch
			{
				return HRESULT.S_OK;
			}

			// do the work
			ToggleCase(ec, tfSelection[0].range, false);

			return HRESULT.S_OK;
		}
	}

	internal class CHelloEditSession : CEditSessionBase
	{
		private const string text = "Hello world!";

		public CHelloEditSession(ITfContext pContext) : base(pContext)
		{
		}

		public override HRESULT DoEditSession([In] uint ec)
		{
			InsertTextAtSelection(ec, pContext, text, text.Length);

			return HRESULT.S_OK;
		}

		private void InsertTextAtSelection(uint ec, ITfContext pContext, string pchText, int cchText)
		{
			// we need a special interface to insert text at the selection
			var pInsertAtSelection = (ITfInsertAtSelection)pContext;

			// insert the text
			ITfRange pRange = pInsertAtSelection.InsertTextAtSelection(ec, 0, pchText, cchText);

			// update the selection, we'll make it an insertion point just past the inserted text.
			pRange.Collapse(ec, TfAnchor.TF_ANCHOR_END);

			var tfSelection = new TF_SELECTION[] { new TF_SELECTION { range = pRange, style = new TF_SELECTIONSTYLE { ase = TfActiveSelEnd.TF_AE_NONE, fInterimChar = false } } };

			pContext.SetSelection(ec, 1, tfSelection);
		}
	}

	internal class CKeystrokeEditSession : CEditSessionBase
	{
		public CKeystrokeEditSession(ITfContext pContext) : base(pContext)
		{
		}

		public override HRESULT DoEditSession([In] uint ec)
		{
			if (User32.GetKeyState(VK_SHIFT) & 0x8000)
			{
				// shift-key, make it lowercase
				wc = (WCHAR)(wParam | 32);
			}
			else
			{
				// else make it capital
				wc = (WCHAR)wParam;
			}

			InsertTextAtSelection(ec, pContext, &wc, 1);

			return HRESULT.S_OK;
		}
	}
}