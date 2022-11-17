using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.User32;

// Globals
const string WNDCLASSNAME = "CWMFExTestWindow";
const uint CWMFEX_CONTROL = WM_APP + 1;
const int CWMFEX_ACK = 0x0f0f0f0f;
const MessageFilterInformation MSGFLTINFO_ERROR = unchecked((MessageFilterInformation)(uint)-1);

using var hwnd = CreateTheWindow(GetModuleHandle());
if (hwnd.IsInvalid)
{
	return;
}

// Allow control message CWMFEX_CONTROL so that sender can control this window

if (MSGFLTINFO_ERROR == ChangeFilter(hwnd, CWMFEX_CONTROL, MessageFilterExAction.MSGFLT_ALLOW))
{
	return;
}

while (GetMessage(out var msg))
{
	TranslateMessage(msg);
	DispatchMessage(msg);
}

MessageFilterInformation ChangeFilter(HWND hwnd, uint uMsg, MessageFilterExAction dwMsgFlt)
{
	CHANGEFILTERSTRUCT ChangeFilterStruct = CHANGEFILTERSTRUCT.Default;
	bool fSuccess = ChangeWindowMessageFilterEx(hwnd, uMsg, dwMsgFlt, ref ChangeFilterStruct);
	MessageFilterInformation dwMsgFltInfo = ChangeFilterStruct.ExtStatus;

	if (!fSuccess)
	{
		Console.Write("\nChangeWindowMessageFilterEx failed with {0}", Win32Error.GetLastError());
		dwMsgFltInfo = MSGFLTINFO_ERROR;
	}

	return dwMsgFltInfo;
}

SafeHWND CreateTheWindow(HINSTANCE hInstance)
{
	WindowClass wndclass = new(WNDCLASSNAME, hInstance, CWMFExWindowProc, hbrBkgd: (HBRUSH)SystemColorIndex.COLOR_WINDOW);

	SafeHWND hwnd = CreateWindow(WNDCLASSNAME, WNDCLASSNAME, WindowStyles.WS_BORDER | WindowStyles.WS_VISIBLE | WindowStyles.WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, 500, 500, default, default, hInstance, default);
	if (hwnd.IsInvalid)
	{
		Console.Write("\nCreateWindow failed with {0}", Win32Error.GetLastError());
	}
	return hwnd;
}

IntPtr CWMFExWindowProc(HWND hwnd, uint uMsg, IntPtr wParam, IntPtr lParam)
{
	switch (uMsg)
	{
		case CWMFEX_CONTROL:
			return (IntPtr)ChangeFilter(hwnd, (uint)lParam.ToInt32(), (MessageFilterExAction)wParam.ToInt32());
		case (uint)WindowMessage.WM_CLOSE:
			PostQuitMessage(0);
			break;
		default:
			break;
	}
	DefWindowProc(hwnd, uMsg, wParam, lParam);
	return (IntPtr)CWMFEX_ACK;
}