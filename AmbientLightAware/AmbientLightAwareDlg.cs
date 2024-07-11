using Vanara;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;

internal class CAmbientLightAwareDlg : IDisposable
{
	private const int IDC_STATIC_LUX = 1001;
	private const int IDC_STATIC_SAMPLE = 1002;
	private const int IDC_STATIC_LUX_SENSORS = 1003;
	private const int IDC_STATIC_SENSORS = 1003;

	private LOGFONT m_lfLogFont = new(); // font to be adjusted for current brightness
	private static Lazy<SafeHICON> m_hIcon = new(() => LoadIcon(default, "AmbientLightAware.ico"));
	private CAmbientLightAwareSensorManagerEvents? m_pSensorManagerEvents; // events class
	private SafeHFONT hf = SafeHFONT.Null;
	private DLGTEMPLATEEX_MGD dlgTmpl;
	private HWND Handle;

	public CAmbientLightAwareDlg()
	{
		dlgTmpl = new()
		{
			cx = 207, cy = 88,
			style = (WindowStyles)(DialogBoxStyles.DS_SETFONT | DialogBoxStyles.DS_MODALFRAME | DialogBoxStyles.DS_FIXEDSYS) | WindowStyles.WS_POPUP | WindowStyles.WS_VISIBLE | WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU,
			exStyle = WindowStylesEx.WS_EX_APPWINDOW,
			title = "Ambient Light Aware SDK Sample",
			pointsize = 8,
			typeface = "MS Shell Dlg",
			controls = [
				DLGTEMPLATEEX_MGD.MakeButton("Done", (ushort)MB_RESULT.IDOK, 7, 65, 50, 16, WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE | WindowStyles.WS_TABSTOP | (WindowStyles)ButtonStyle.BS_DEFPUSHBUTTON),
				DLGTEMPLATEEX_MGD.MakeStatic("Ambient light level: lux", IDC_STATIC_LUX, 7, 7, 193, 12),
				DLGTEMPLATEEX_MGD.MakeStatic("Sensors: 0", IDC_STATIC_SENSORS, 7, 18, 193, 9),
				DLGTEMPLATEEX_MGD.MakeStatic("Sample Optimized Text", IDC_STATIC_SAMPLE, 7, 33, 193, 32),
			]
		};
	}

	public int DoModal() => DialogBoxIndirectParam(default, dlgTmpl, GetDesktopWindow(), DlgProc).ToInt32() > 0 ? 1 : throw Win32Error.GetLastError().GetException()!;

	// Clean up function called by parent winapp
	void IDisposable.Dispose()
	{
		m_pSensorManagerEvents?.Dispose();
		m_pSensorManagerEvents = default;
	}

	private IntPtr DlgProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)msg)
		{
			case WindowMessage.WM_INITDIALOG:
				Handle = hwnd;
				// Set the icon for this dialog.
				SendMessage(hwnd, WindowMessage.WM_SETICON, 1, m_hIcon.Value.DangerousGetHandle()); // Set big icon
				SendMessage(hwnd, WindowMessage.WM_SETICON, 0, m_hIcon.Value.DangerousGetHandle()); // Set small icon
				InitAmbientLightAware();
				break;

			case WindowMessage.WM_COMMAND:
				if (wParam.ToInt32() is (int)MB_RESULT.IDOK or (int)MB_RESULT.IDCANCEL)
				{
					EndDialog(hwnd, wParam);
					return (IntPtr)1;
				}
				break;

			case WindowMessage.WM_PAINT:
				if (IsIconic(hwnd))
				{
					using var dc = GetDC(hwnd);

					SendMessage(hwnd, WindowMessage.WM_ICONERASEBKGND, dc.DangerousGetHandle(), IntPtr.Zero);

					// Center icon in client rectangle
					int cxIcon = GetSystemMetrics(SystemMetric.SM_CXICON);
					int cyIcon = GetSystemMetrics(SystemMetric.SM_CYICON);
					GetClientRect(hwnd, out var rect);
					int x = (rect.Width - cxIcon + 1) / 2;
					int y = (rect.Height - cyIcon + 1) / 2;

					// Draw the icon
					DrawIcon(dc, x, y, m_hIcon.Value);
				}
				else
				{
					//base.WndProc(hwnd, msg, wParam, lParam);

					// ********************************************************************
					// Change the font size for the optimzed text.
					// ********************************************************************
					using var fontOptimzed = CreateFontIndirect(m_lfLogFont);
					if (!fontOptimzed.IsInvalid)
					{
						var pStaticTextSample = GetDlgItem(hwnd, IDC_STATIC_SAMPLE);
						if (!pStaticTextSample.IsNull)
						{
							SetWindowFont(pStaticTextSample, fontOptimzed, true);
						}
					}
				}
				return default;

			default:
				break;
		}
		return IntPtr.Zero;
	}

	// helper function
	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareDlg::InitAmbientLightAware
	//
	// Description of function/method: Helper function, initializes sensor events class
	//
	// Parameters: none
	//
	// Return Values: HRESULT.S_OK on success, else an error
	///////////////////////////////////////////////////////////////////////////////
	private HRESULT InitAmbientLightAware()
	{
		HRESULT hr;

		// save the font so we can easily change the text size in UpdateLux
		var pWnd = GetDlgItem(Handle, IDC_STATIC_SAMPLE);
		if (!pWnd.IsNull)
		{
			var pFont = GetWindowFont(pWnd);
			if (!pFont.IsNull)
			{
				m_lfLogFont = GetObject<LOGFONT>(pFont);
				m_pSensorManagerEvents = new CAmbientLightAwareSensorManagerEvents(this);
				hr = m_pSensorManagerEvents.Initialize();
			}
			else
			{
				hr = HRESULT.E_POINTER;
			}
		}
		else
		{
			hr = HRESULT.E_POINTER;
		}

		return hr;
	}

	///////////////////////////////////////////////////////////////////////////////
	// CAmbientLightAwareDlg::UpdateLux
	//
	// Description of function/method: Callback function. This function is called by the events class when new data has been received. This
	// function then uses this information to change the font size to be optimized for the current brightness (lux).
	//
	// This sample is not meant to be an ideal implementation, but just showing how sensor data can be collected and processed.
	//
	// Parameters:
	// lux: The average lux value for all sensors
	// numSensors: The number of sensors reporting data
	//
	// Return Values: true on success, false on failure
	///////////////////////////////////////////////////////////////////////////////
	internal HRESULT UpdateLux(float lux, int numSensors)
	{
		HRESULT hr = HRESULT.S_OK;
		System.Diagnostics.Debug.WriteLine($"Lux: {lux}");

		if (lux < 10.0)
		{
			// Darkness
			m_lfLogFont.lfHeight = 10; // A sample font size for dark environments
		}
		else if (lux < 300)
		{
			// Dim Indoors
			m_lfLogFont.lfHeight = 12; // A sample font size for dim indoor environments
		}
		else if (lux < 800)
		{
			// Normal Indoors
			m_lfLogFont.lfHeight = 14; // A sample font size for indoor environments
		}
		else if (lux < 10000)
		{
			// Bright Indoors
			m_lfLogFont.lfHeight = 16; // A sample font size for bright indoor environments
		}
		else if (lux < 30000)
		{
			// Overcast Outdoors
			m_lfLogFont.lfHeight = 20; // A sample font size for overcast environments
		}
		else
		{
			// Direct Sunlight
			m_lfLogFont.lfHeight = 30; // A sample font size for sunny environments
		}

		SetDlgItemText(Handle, IDC_STATIC_LUX, $"Ambient light level: {lux} lux");
		SetDlgItemText(Handle, IDC_STATIC_SENSORS, $"Sensors: {numSensors}");

		// Force OnPaint which changes the text font to be optimized for this lux
		InvalidateRect(GetDlgItem(Handle, IDC_STATIC_SAMPLE), default, true);
		UpdateWindow(Handle);

		return hr;
	}
}