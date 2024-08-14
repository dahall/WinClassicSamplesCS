using System.IO;
using Vanara;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;

namespace PlugInDll;

public static class PlugInDll
{
	public const string HWND_NAME_EXTERNAL = "External Content";
	public const string PLUGINWINDOWCLASSNAME = "Plugin Window Class";
	private const int DEFAULT_CHAR_BUFFER = 200;
	private const int DEFAULT_PADDING96 = 20;
	private const string PROP_FONTSET = "FONT_SET";
	private static WindowClass? wcex;

	// This method will create an HWND tree that is scaled to the system DPI ("System DPI" is a global DPI that is based off of the scale
	// factor of the primary display). When the process that this code is running in is has a DPI_AWARENESS_CONTEXT of
	// DPI_AWARENESS_CONTEXT_UNAWARE, the system DPI will be 96
	public static HWND CreateContentHwnd(HINSTANCE hInstance, int nWidth, int nHeight)
	{
		// Register the window class
		ClassRegistration(hInstance);

		// Get the "System DPI" Don't do this in per-monitor aware code as this will either return 96 or the system DPI but will not return
		// the per-monitor DPI
		uint mainMonitorDPI = GetDpiForSystem();

		// Create an HWND tree that is parented to the message window (HWND_MESSAGE)
		SafeHWND hWndExternalContent = CreateWindowEx(0L, PLUGINWINDOWCLASSNAME, HWND_NAME_EXTERNAL, WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD,
			0, 0, nWidth, nHeight, HWND.HWND_MESSAGE, default, hInstance, default);

		// Add some child controls
		HWND hWndStatic = CreateWindowEx(WindowStylesEx.WS_EX_LEFT, "STATIC", "External content static (text) control",
			(WindowStyles)StaticStyle.SS_LEFT | WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE,
			ScaleToSystemDPI(DEFAULT_PADDING96, mainMonitorDPI),
			ScaleToSystemDPI(DEFAULT_PADDING96, mainMonitorDPI),
			ScaleToSystemDPI(nWidth - 2 * DEFAULT_PADDING96, mainMonitorDPI),
			ScaleToSystemDPI(75, mainMonitorDPI),
			hWndExternalContent, default, hInstance, default).ReleaseOwnership();

		// Subclass the static control so that we can ignore WM_SETFONT from the host
		SetWindowSubclass(hWndStatic, SubclassProc, 0, default);

		// Set the font for the static control
		var hFontOld = GetWindowFont(hWndStatic);
		SafeHGlobalStruct<LOGFONT> lfText = new();
		SystemParametersInfoForDpi(SPI.SPI_GETICONTITLELOGFONT, lfText.Size, lfText, 0, mainMonitorDPI);
		HFONT hFontNew = CreateFontIndirect(lfText.Value);
		if (!hFontNew.IsNull)
		{
			SendMessage(hWndStatic, WindowMessage.WM_SETFONT, (IntPtr)hFontNew, Macros.MAKELPARAM(1, 0));
		}

		// Convert DPI awareness context to a string
		DPI_AWARENESS_CONTEXT dpiAwarenessContext = GetThreadDpiAwarenessContext();
		DPI_AWARENESS dpiAwareness = GetAwarenessFromDpiAwarenessContext(dpiAwarenessContext);

		// Convert DPI awareness to a string
		string awarenessContext = dpiAwareness switch
		{
			DPI_AWARENESS.DPI_AWARENESS_SYSTEM_AWARE => "DPI_AWARENESS_CONTEXT_SYSTEM_AWARE",
			DPI_AWARENESS.DPI_AWARENESS_PER_MONITOR_AWARE => AreDpiAwarenessContextsEqual(dpiAwarenessContext, DPI_AWARENESS_CONTEXT.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
								? "DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2"
								: "DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE",
			_ => "DPI_AWARENESS_CONTEXT_UNAWARE",
		};

		// Build the output string
		string result = $"HWND content from an external source. The thread that created this content had a thread context of {awarenessContext}, with a DPI of: {mainMonitorDPI}";
		SetWindowText(hWndStatic, result);

		// Load a bitmap
		using var bmpStream = System.Reflection.Assembly.GetAssembly(typeof(PlugInDll))!.GetManifestResourceStream("PluginDLL.PC.bmp")!;
		System.Drawing.Bitmap bmp = new(bmpStream);

		// Create a static control to put the image in to
		GetParentRelativeWindowRect(hWndStatic, out var rcClient);
		HWND hWndImage = CreateWindowEx(WindowStylesEx.WS_EX_LEFT, "STATIC", "External content static (bitmap) control",
			(WindowStyles)StaticStyle.SS_BITMAP | WindowStyles.WS_CHILD | WindowStyles.WS_VISIBLE,
			ScaleToSystemDPI(DEFAULT_PADDING96, mainMonitorDPI),
			rcClient.bottom + ScaleToSystemDPI(DEFAULT_PADDING96, mainMonitorDPI),
			ScaleToSystemDPI(nWidth - 2 * DEFAULT_PADDING96, mainMonitorDPI),
			ScaleToSystemDPI(200, mainMonitorDPI),
			hWndExternalContent, default, hInstance, default).ReleaseOwnership();
		SendMessage(hWndImage, StaticMessage.STM_SETIMAGE, (IntPtr)LoadImageType.IMAGE_BITMAP, bmp.GetHbitmap());

		return hWndExternalContent.ReleaseOwnership();
	}

	private static void ClassRegistration(HINSTANCE hInstance) => wcex ??= new(PLUGINWINDOWCLASSNAME, hInstance, WndProc, WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW);

	public static bool GetParentRelativeWindowRect(HWND hWnd, out RECT childBounds)
	{
		if (!GetWindowRect(hWnd, out childBounds))
			return false;
		MapWindowRect(GetDesktopWindow(), GetAncestor(hWnd, GetAncestorFlag.GA_PARENT), ref childBounds);
		return true;
	}

	private static int ScaleToSystemDPI(int @in, uint mainMonitorDPI) => (int)(@in * mainMonitorDPI / 96);

	private static IntPtr SubclassProc(HWND hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
	{
		switch ((WindowMessage)uMsg)
		{
			// Store a flag indicating that the font has been set, then don't let the font be set after that
			case WindowMessage.WM_SETFONT:
				BOOL bFontSet = GetProp(hWnd, PROP_FONTSET);
				if (!bFontSet)
				{
					// Allow the font set to happen
					SetProp(hWnd, PROP_FONTSET, (IntPtr)1);
					return DefSubclassProc(hWnd, uMsg, wParam, lParam);
				}
				else
				{
					return default;
				}
		}

		return DefSubclassProc(hWnd, uMsg, wParam, lParam);
	}

	private static IntPtr WndProc(HWND hWnd, uint message, IntPtr wParam, IntPtr lParam)
	{
		switch ((WindowMessage)message)
		{
			case WindowMessage.WM_CLOSE:
				DestroyWindow(hWnd);
				return default;

			case WindowMessage.WM_DESTROY:
				return default;
		}
		return DefWindowProc(hWnd, message, wParam, lParam);
	}
}