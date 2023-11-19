using System.Data;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Magnification;
using static Vanara.PInvoke.User32;

namespace MagnificationFullscreen;

public partial class FullscreenMagnifierSample : Form
{
	// Global variables and strings.
	private const string g_pszAppTitle = "Fullscreen Magnifier Sample";

	private static readonly MAGCOLOREFFECT g_MagEffectGrayscale = new(new[,] { {0.3f, 0.3f, 0.3f, 0.0f, 0.0f },
		{ 0.6f, 0.6f, 0.6f, 0.0f, 0.0f },
		{ 0.1f, 0.1f, 0.1f, 0.0f, 0.0f },
		{ 0.0f, 0.0f, 0.0f, 1.0f, 0.0f },
		{ 0.0f, 0.0f, 0.0f, 0.0f, 1.0f }});

	public FullscreenMagnifierSample()
	{
		InitializeComponent();
		IDC_RADIO_100.Tag = 1.0f;
		IDC_RADIO_200.Tag = 2.0f;
		IDC_RADIO_300.Tag = 3.0f;
		IDC_RADIO_400.Tag = 4.0f;
	}

	private static bool Equals(in MAGCOLOREFFECT a, in MAGCOLOREFFECT b) => a.transform.Cast<float>().SequenceEqual(b.transform.Cast<float>());

	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		// Initialize the magnification functionality for this process.
		if (MagInitialize())
		{
			// Present a dialog box to allow the user to control fullscreen magnification.
			Application.Run(new FullscreenMagnifierSample());

			// Any current magnification and color effects will be turned off as a result of calling MagUninitialize().
			MagUninitialize();
		}
		else
		{
			MessageBox(default, "Failed to initialize magnification.", g_pszAppTitle, MB_FLAGS.MB_OK);
		}
	}

	// FUNCTION: GetSettings()
	//
	// PURPOSE: Query all the related settings, and present them to the user.
	private void GetSettings()
	{
		string? pszColorStatus = default;
		var fInputTransformEnabled = false;

		// If any unexpected errors occur trying to get the current settings, present no settings data.
		var fSuccess = true;

		// Get the current magnification level and offset.
		if (!MagGetFullscreenTransform(out var magnificationLevel, out _, out _))
		{
			fSuccess = false;
		}

		if (fSuccess)
		{
			// Get the current color effect.
			if (MagGetFullscreenColorEffect(out var magEffect))
			{
				// Present friendly text relating to the color effect.
				if (magEffect.IsIdentity)
				{
					pszColorStatus = "Identity";
				}
				else if (Equals(g_MagEffectGrayscale, magEffect))
				{
					pszColorStatus = "Grayscale";
				}
				else
				{
					// This would be an unexpected result from MagGetDesktopColorEffect() given that the sample only sets the identity
					// or grayscale effects.
					pszColorStatus = "Unknown";
				}
			}
			else
			{
				fSuccess = false;
			}
		}

		// Get the current input transform.
		if (fSuccess && !MagGetInputTransform(out fInputTransformEnabled, out _, out _))
		{
			fSuccess = false;
		}

		// Present the results of all the calls above.
		var szMessage = fSuccess
			? string.Format("The current settings are:\r\n\r\nMagnification level: {0}%\r\nColor effect: {1}\r\nInput transform state: {2}",
				magnificationLevel * 100, pszColorStatus, fInputTransformEnabled)
			: string.Format("Failed to get magnification setting. Error was {0}", Win32Error.GetLastError());

		MessageBox(Handle, szMessage, g_pszAppTitle, MB_FLAGS.MB_OK);
	}

	// FUNCTION: HandleCommand()
	//
	// PURPOSE: Take action in response to user action at the dialog box's controls.
	private void HandleCommand(object sender, EventArgs e)
	{
		switch (((Control)sender).Name)
		{
			case "IDC_CLOSE":

				// Close the sample dialog box.
				Close();

				break;

			case "IDC_RADIO_100":
			case "IDC_RADIO_200":
			case "IDC_RADIO_300":
			case "IDC_RADIO_400":

				// The user clicked one of the radio button to apply some fullscreen magnification. (We know the control ids are
				// sequential here.)
				SetZoom((float)((Control)sender).Tag);

				break;

			case "IDC_CHECK_SETGRAYSCALE":
				// The user clicked the checkbox to apply grayscale to the colors on the screen.
				SetColorGrayscaleState(IDC_CHECK_SETGRAYSCALE.Checked);

				break;

			case "IDC_CHECK_SETINPUTTRANSFORM":
				// The user clicked the checkbox to apply an input transform for touch and pen input.
				SetInputTransform(IDC_CHECK_SETINPUTTRANSFORM.Checked);

				break;

			case "IDC_BUTTON_GETSETTINGS":

				// The user wants to retrieve the current magnification settings.
				GetSettings();

				break;
		}
	}

	// FUNCTION: SetColorGrayscaleState()
	//
	// PURPOSE: Either apply grayscale to all colors on the screen, or restore the original colors.
	private void SetColorGrayscaleState([In] bool fGrayscaleOn)
	{
		// Apply the color matrix required to either invert the screen colors or to show the regular colors.
		MagSetFullscreenColorEffect(fGrayscaleOn ? g_MagEffectGrayscale : MAGCOLOREFFECT.Identity);
	}

	// FUNCTION: SetInputTransform()
	//
	// PURPOSE: Apply an input transform to allow touch and pen input to account for the current fullscreen or lens magnification.
	private void SetInputTransform(bool fSetInputTransform)
	{
		var fContinue = true;

		RECT rcSource = default;
		RECT rcDest = default;

		// MagSetInputTransform() is used to adjust pen and touch input to account for the current magnification. The "Source" and
		// "Destination" rectangles supplied to MagSetInputTransform() are from the perspective of the currently magnified visuals. The
		// source rectangle is the portion of the screen that is currently being magnified, and the destination rectangle is the area on
		// the screen which shows the magnified results.

		// If we're setting an input transform, base the transform on the current fullscreen magnification.
		if (fSetInputTransform)
		{
			// Assume here the touch and pen input is going to the primary monitor.
			rcDest.right = GetSystemMetrics(SystemMetric.SM_CXSCREEN);
			rcDest.bottom = GetSystemMetrics(SystemMetric.SM_CYSCREEN);

			// Get the currently active magnification.
			if (MagGetFullscreenTransform(out var magnificationFactor, out var xOffset, out var yOffset))
			{
				// Determine the area of the screen being magnified.
				rcSource.left = xOffset;
				rcSource.top = yOffset;
				rcSource.right = rcSource.left + (int)(rcDest.right / magnificationFactor);
				rcSource.bottom = rcSource.top + (int)(rcDest.bottom / magnificationFactor);
			}
			else
			{
				// An unexpected error occurred trying to get the current magnification.
				fContinue = false;

				var szError = string.Format("Failed to get current magnification. Error was {0}", Win32Error.GetLastError());
				MessageBox(Handle, szError, g_pszAppTitle, MB_FLAGS.MB_OK);
			}
		}

		// Now set the input transform as required.
		if (fContinue && !MagSetInputTransform(fSetInputTransform, rcSource, rcDest))
		{
			// If the last error is E_ACCESSDENIED, then this may mean that the process is not running with UIAccess privileges.
			// UIAccess is required in order for MagSetInputTransform() to success.

			var szError = string.Format("Failed to set input transform. Error was {0}", Win32Error.GetLastError());
			MessageBox(Handle, szError, g_pszAppTitle, MB_FLAGS.MB_OK);
			IDC_CHECK_SETINPUTTRANSFORM.Checked = false;
		}
	}

	// FUNCTION: SetZoom()
	//
	// PURPOSE: Apply fullscreen magnification.
	private void SetZoom(float magnificationFactor)
	{
		// Attempts to apply a magnification of less than 100% will fail.
		if (magnificationFactor >= 1.0)
		{
			// The offsets supplied to MagSetFullscreenTransform() are relative to the top left corner of the primary monitor, in
			// unmagnified coordinates. To position the top left corner of some window of interest at the top left corner of the
			// magnified view, call GetWindowRect() to get the window rectangle, and pass the rectangle's left and top values as the
			// offsets supplied to MagSetFullscreenTransform().

			// If the top left corner of the window of interest is to be positioned at the top left corner of the monitor furthest to
			// the left of the primary monitor, then the top left corner of the desktop would be adjusted by the current magnification,
			// as follows: int xOffset = rcTargetWindow.left - (int)(rcVirtualDesktop.left / magnificationFactor); int yOffset =
			// rcTargetWindow.top - (int)(rcVirtualDesktop.top / magnificationFactor);

			// For this sample, keep the sample's UI at the center of the magnified view on the primary monitor. In order to do this, it
			// is nececessary to adjust the offsets supplied to MagSetFullscreenTransform() based on the magnification being applied.

			// Note that the calculations in this file which use GetSystemMetrics(SM_C*SCREEN) assume that the values returned from that
			// function are unaffected by the current DPI setting. In order to ensure this, the manifest for this app declares the app
			// to be DPI aware.

			var xDlg = (int)(GetSystemMetrics(SystemMetric.SM_CXSCREEN) * (1.0 - (1.0 / magnificationFactor)) / 2.0);
			var yDlg = (int)(GetSystemMetrics(SystemMetric.SM_CYSCREEN) * (1.0 - (1.0 / magnificationFactor)) / 2.0);

			var fSuccess = MagSetFullscreenTransform(magnificationFactor, xDlg, yDlg);
			// If an input transform for pen and touch is currently applied, update the transform to account for the new magnification.
			if (fSuccess && MagGetInputTransform(out var fInputTransformEnabled, out _, out _) && fInputTransformEnabled)
			{
				SetInputTransform(fInputTransformEnabled);
			}
		}
	}
}