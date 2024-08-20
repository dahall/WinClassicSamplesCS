using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;

namespace Decomp;

internal static class Program
{
	/******************************************************************
	* Global Variables 
	******************************************************************/
	static INPUT_MESSAGE_SOURCE g_inputSource = new();

	/******************************************************************
	* WinMain 
	* Application entrypoint 
	******************************************************************/
	internal static void Main()
	{
		SetProcessDPIAware();
		VisibleWindow.Run(WndProc, "Input Source Identification Sample", new SIZE(640, 480));
	}

	static HRESULT OnRender(HDC hdc, in RECT rcPaint)
	{
		FillRect(hdc, rcPaint, (HBRUSH)GetStockObject(StockObjectType.WHITE_BRUSH));

		string wzText = "Source: ";

		switch (g_inputSource.deviceType)
		{
			case INPUT_MESSAGE_DEVICE_TYPE.IMDT_UNAVAILABLE:
				wzText += "Unavailable\n";
				break;

			case INPUT_MESSAGE_DEVICE_TYPE.IMDT_KEYBOARD:
				wzText += "Keyboard\n";
				break;

			case INPUT_MESSAGE_DEVICE_TYPE.IMDT_MOUSE:
				wzText += "Mouse\n";
				break;

			case INPUT_MESSAGE_DEVICE_TYPE.IMDT_TOUCH:
				wzText += "Touch\n";
				break;

			case INPUT_MESSAGE_DEVICE_TYPE.IMDT_PEN:
				wzText += "Pen\n";
				break;
		}

		wzText += "Origin: ";

		switch (g_inputSource.originId)
		{
			case INPUT_MESSAGE_ORIGIN_ID.IMO_UNAVAILABLE:
				wzText += "Unavailable\n";
				break;

			case INPUT_MESSAGE_ORIGIN_ID.IMO_HARDWARE:
				wzText += "Hardware\n";
				break;

			case INPUT_MESSAGE_ORIGIN_ID.IMO_INJECTED:
				wzText += "Injected\n";
				break;

			case INPUT_MESSAGE_ORIGIN_ID.IMO_SYSTEM:
				wzText += "System\n";
				break;
		}

		DrawText(hdc, wzText, wzText.Length, rcPaint, DrawTextFlags.DT_TOP | DrawTextFlags.DT_LEFT);

		return HRESULT.S_OK;
	}


	/******************************************************************
	* WndProc 
	* This static method handles our app's window ref ref ref messages 
	******************************************************************/
	static IntPtr WndProc(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		WindowMessage message = (WindowMessage)msg;

		if (message is >= WindowMessage.WM_MOUSEFIRST and <= WindowMessage.WM_MOUSELAST or
			>= WindowMessage.WM_KEYFIRST and <= WindowMessage.WM_KEYLAST or
			>= WindowMessage.WM_TOUCH and <= WindowMessage.WM_POINTERWHEEL)
		{
			GetCurrentInputMessageSource(ref g_inputSource);
			InvalidateRect(hwnd, default, false);
		}

		switch (message)
		{
			case WindowMessage.WM_PAINT:
			case WindowMessage.WM_DISPLAYCHANGE:
				HDC hdc = BeginPaint(hwnd, out var ps);
				OnRender(hdc, ps.rcPaint);
				EndPaint(hwnd, ps);
				return default;

			case WindowMessage.WM_DESTROY:
				PostQuitMessage(0);
				return (IntPtr)1;
		}

		return DefWindowProc(hwnd, msg, wParam, lParam);
	}
}