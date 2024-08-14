using Vanara.PInvoke;
using static Vanara.PInvoke.FMApi;
using static Vanara.PInvoke.Kernel32;

// Define program specific values
const string VOLUME = @"\\.\D:";

HeapSetInformation(default, HEAP_INFORMATION_CLASS.HeapEnableTerminationOnCorruption, default, 0);

//Call CreateFileRestoreContext with the FMAPI version number we are expecting to use
RESTORE_CONTEXT_FLAGS flags = RESTORE_CONTEXT_FLAGS.ContextFlagVolume | RESTORE_CONTEXT_FLAGS.FlagScanRemovedFiles;
if (CreateFileRestoreContext(VOLUME, flags, 0, 0, FILE_RESTORE_VERSION_2, out var context))
{
	Console.Write("Version Check Succeeded.");
}
else
{
	Win32Error err = GetLastError();
	if (Win32Error.ERROR_INVALID_PARAMETER == err)
	{
		Console.Write("Version Check Failed.");
	}
	else
	{
		Console.Write("Failed to Create FileRestoreContext, Error: {0}.\n", err);
	}
}

//Close the context
if (!context.IsNull)
{
	CloseFileRestoreContext(context);
}