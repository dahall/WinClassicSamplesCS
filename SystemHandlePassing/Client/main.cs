using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;

try
{
	// Get a file name.
	Console.WriteLine("Enter a file name: " );

	string fileName = Console.ReadLine() ?? throw new InvalidOperationException("You must enter a file name.");

	// Open the file
	using var fileHandle = Win32Error.ThrowLastErrorIfInvalid(CreateFile(fileName, // File name
		Kernel32.FileAccess.GENERIC_READ | Kernel32.FileAccess.GENERIC_WRITE, // Read/write access
		FileShare.ReadWrite, // Share with other processes
		default, // Default security
		FileMode.Open, // Create if not exists, open otherwise.
		0, // No flags/attributes
		default // No template
	));

	// Make our side of the event
	// Auto reset, initially unset, no name, default security, throws on fail
	using SafeEventHandle startEvent = SafeEventHandle.Create();

	// Prepare COM
	CoInitializeEx(default, COINIT.COINIT_MULTITHREADED).ThrowIfFailed();

	// Gain access to a sparkle finisher. COM will start the server if it's not running.
	Type type = Marshal.GetTypeFromCLSID(new Guid("EA27C73A-48C2-4714-9D20-A9D2C4F6AED3")) ?? throw new NullReferenceException();

	dynamic sparkleFinisher = Activator.CreateInstance(type) ?? throw new NullReferenceException();

	// Tell the sparkle finisher to work on our file when we signal the start event.
	// It returns an event that is signaled when the work is finished.
	sparkleFinisher.AddSparkleFinishToFile((HFILE)fileHandle, (HEVENT)startEvent, out HEVENT fev);
	using var finishedEvent = new SafeEventHandle((IntPtr)fev, true);

	// Build up our message.
	StringBuilder message = new();
	message.AppendLine("Loading dishes.");
	message.AppendLine("Loading detergent.");
	message.AppendLine("Pre-washing.");
	message.AppendLine("Rinsing.");
	message.AppendLine("Washing.");
	message.AppendLine("Rinsing.");

	// Finalize into string.
	var completeMessage = message.ToString();

	// Write out to file.
	var bytesToWrite = Encoding.Unicode.GetByteCount(completeMessage);
	Win32Error.ThrowLastErrorIfFalse(WriteFile(fileHandle, Encoding.Unicode.GetBytes(completeMessage), (uint)bytesToWrite, out var bytesWritten));

	// If we didn't write the whole thing... detect it and set error.
	if (bytesWritten != bytesToWrite) throw new HRESULT(HRESULT.E_UNEXPECTED).GetException()!;

	// Signal the sparkle finisher server to do its work.
	startEvent.Set();

	// Wait for the server to say that it is finished.
	finishedEvent.Wait(); // Wait infinite.

	// Cleanup of all handles and resources is automatic when using WIL types!
}
catch (Exception ex)
{
	Console.WriteLine($"Sparkle Finish Client Failed with error: '{ex.Message}'");
}