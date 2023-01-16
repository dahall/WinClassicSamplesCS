using Vanara.PInvoke;
using static Vanara.PInvoke.User32;

if (RegisterHotKey(default, 1, HotKeyModifiers.MOD_ALT | HotKeyModifiers.MOD_NOREPEAT, 0x42)) //0x42 is 'b'
{
	Console.Write("Hotkey 'alt+b' registered, using MOD_NOREPEAT flag\n");
}

MSG msg;
while (GetMessage(out msg) != 0)
{
	if (msg.message == (uint)WindowMessage.WM_HOTKEY)
	{
		Console.Write("WM_HOTKEY received\n");
	}
}