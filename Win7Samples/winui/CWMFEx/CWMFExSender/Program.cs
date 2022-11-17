using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

// Globals
const string WNDCLASSNAME = "CWMFExTestWindow";
const uint CWMFEX_CONTROL = WM_APP + 1;
const int CWMFEX_ACK = 0x0f0f0f0f;

// Find the target window
HWND hwnd = FindWindow(WNDCLASSNAME, WNDCLASSNAME);
if (hwnd.IsNull)
{
	Console.Write("\nFindWindow failed with {0}", Win32Error.GetLastError()) ;
}

// Receiver has taken no action so the message should not make it through
Send(hwnd, WindowMessage.WM_UNDO, default, default, false);

// Notify receiver to allow this message
// Note that the receiver allows the control message CWMFEX_CONTROL
SendMessage(hwnd, CWMFEX_CONTROL, MessageFilterExAction.MSGFLT_ALLOW, (IntPtr)WindowMessage.WM_UNDO);

// Receiver has allowed this message, so the message should make it through
Send(hwnd, WindowMessage.WM_UNDO, default, default, true);

// Notify receiver to disallow this message
SendMessage(hwnd, CWMFEX_CONTROL, MessageFilterExAction.MSGFLT_DISALLOW, (IntPtr)WindowMessage.WM_UNDO);

// Receiver has disallowed this message, so the message should not make it through
Send(hwnd, WindowMessage.WM_UNDO, default, default, false);

// Notify receiver to allow this message
SendMessage(hwnd, CWMFEX_CONTROL, MessageFilterExAction.MSGFLT_ALLOW, (IntPtr)WindowMessage.WM_UNDO);

// Receiver has allowed this message, so the message should make it through
Send(hwnd, WindowMessage.WM_UNDO, default, default, true);

// Notify receiver to reset its message filter
SendMessage(hwnd, CWMFEX_CONTROL, MessageFilterExAction.MSGFLT_RESET, (IntPtr)WindowMessage.WM_UNDO);

// Receiver's message filter has been reset, so the message should not make it through
// Note that, after the rest, the control message CWMFEX_CONTROL, will also not make it through
Send(hwnd, WindowMessage.WM_UNDO, default, default, false);

static void Send(HWND hwnd, WindowMessage uMsg, IntPtr wParam, IntPtr lParam, bool fExpectedSuccess)
{
	// Attempt to post the message

	bool fResult = PostMessage(hwnd, uMsg, wParam, lParam);
	if (fExpectedSuccess)
	{
		if (!fResult)
		{
			Console.Write("\nUnexpected: PostMessage failed with {0}", Win32Error.GetLastError());
		}
	}
	else
	{
		if (fResult)
		{
			Console.Write("\nUnexpected: PostMessage succeeded");
		}
	}

	// Attempt to send the message

	IntPtr lResult = SendMessage(hwnd, uMsg, wParam, lParam);
	if (fExpectedSuccess)
	{
		if (CWMFEX_ACK != lResult.ToInt32())
		{
			Console.Write("\nUnexpected: SendMessage failed");
		}
	}
	else
	{
		if (CWMFEX_ACK == lResult.ToInt32())
		{
			Console.Write("\nUnexpected: SendMessage succeeded");
		}
	}
}