using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.MSCTF;

namespace tsfcase
{
	internal class CLangBarItemButton : ITfLangBarItemButton, ITfSource
	{
		private static readonly int CONNECT_E_FIRST = (int)HRESULT.Make(true, HRESULT.FacilityCode.FACILITY_ITF, 0x0200);

		// this implementation's limit for advisory connections has been reached
		public static readonly int CONNECT_E_ADVISELIMIT = (int)CONNECT_E_FIRST + 1;

		// connection attempt failed
		public static readonly int CONNECT_E_CANNOTCONNECT = (int)CONNECT_E_FIRST + 2;

		// there is no connection for this connection id
		public static readonly int CONNECT_E_NOCONNECTION = CONNECT_E_FIRST;

		// must use a derived interface to connect
		public static readonly int CONNECT_E_OVERRIDDEN = (int)CONNECT_E_FIRST + 3;

		const string LANGBAR_ITEM_DESC = "Case Menu";
		CCaseTextService pCase;
		ITfLangBarItemSink pLangBarItemSink;
		TF_LANGBARITEMINFO tfLangBarItemInfo;

		public CLangBarItemButton(CCaseTextService pCase)
		{
			tfLangBarItemInfo = new TF_LANGBARITEMINFO
			{
				clsidService = new Guid("6565d455-5030-4c0f-8871-83f6afde514f"),
				guidItem = new Guid("01679c88-5141-4ee5-a47f-c8d586ff37e1"),
				dwStyle = TF_LBI_STYLE.TF_LBI_STYLE_BTN_MENU,
				szDescription = LANGBAR_ITEM_DESC
			};
			this.pCase = pCase;
			pLangBarItemSink = default;
		}

		HRESULT ITfLangBarItemButton.GetInfo(out TF_LANGBARITEMINFO pInfo)
		{
			pInfo = tfLangBarItemInfo;
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItemButton.GetStatus(out TF_LBI_STATUS pdwStatus)
		{
			pdwStatus = 0;
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItemButton.Show(bool fShow) => HRESULT.E_NOTIMPL;

		HRESULT ITfLangBarItemButton.GetTooltipString(out string pbstrToolTip)
		{
			pbstrToolTip = LANGBAR_ITEM_DESC;
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItemButton.OnClick(TfLBIClick click, POINT pt, in RECT prcArea) => HRESULT.S_OK;

		HRESULT ITfLangBarItemButton.InitMenu(ITfMenu pMenu)
		{
			uint i = 0;
			foreach ((string s, Action a, Func<bool> p) in pCase.c_rgMenuItems)
			{
				TF_LBMENUF dwFlags = 0;
				if (!(p is null))
					dwFlags = p.Invoke() ? TF_LBMENUF.TF_LBMENUF_CHECKED : 0;

				pMenu.AddMenuItem(i++, dwFlags, default, default, s, (uint)s.Length, out _);
			}

			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItemButton.OnMenuSelect(uint wID)
		{
			pCase.c_rgMenuItems[(int)wID].Item2.Invoke();
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItemButton.GetIcon(out HICON phIcon)
		{
			phIcon = default;
			return HRESULT.E_NOTIMPL;
		}

		HRESULT ITfLangBarItemButton.GetText(out string pbstrText)
		{
			pbstrText = LANGBAR_ITEM_DESC;
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItem.GetInfo(out TF_LANGBARITEMINFO pInfo)
		{
			pInfo = tfLangBarItemInfo;
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItem.GetStatus(out TF_LBI_STATUS pdwStatus)
		{
			pdwStatus = 0;
			return HRESULT.S_OK;
		}

		HRESULT ITfLangBarItem.Show(bool fShow) => HRESULT.E_NOTIMPL;

		HRESULT ITfLangBarItem.GetTooltipString(out string pbstrToolTip)
		{
			pbstrToolTip = LANGBAR_ITEM_DESC;
			return HRESULT.S_OK;
		}

		const uint CASE_LANGBARITEMSINK_COOKIE = 0x0fab0fab;

		void ITfSource.AdviseSink(in Guid riid, object punk, out uint pdwCookie)
		{
			if (!Guid.Equals(typeof(ITfLangBarItemSink).GUID, riid))
				throw new COMException(null, CONNECT_E_CANNOTCONNECT);

			if (!(pLangBarItemSink is null))
				throw new COMException(null, CONNECT_E_ADVISELIMIT);

			pLangBarItemSink = (ITfLangBarItemSink)punk;

			pdwCookie = CASE_LANGBARITEMSINK_COOKIE;
		}

		void ITfSource.UnadviseSink(uint dwCookie)
		{
			if (dwCookie != CASE_LANGBARITEMSINK_COOKIE)
				throw new COMException(null, CONNECT_E_NOCONNECTION);

			if (pLangBarItemSink is null)
				throw new COMException(null, CONNECT_E_NOCONNECTION);

			pLangBarItemSink = default;
		}
	}
}