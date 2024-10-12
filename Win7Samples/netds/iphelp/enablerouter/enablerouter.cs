using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.IpHlpApi;

const int RETURN_SUCCESS = 0;
const int RETURN_FAILURE = 1;

// Step through one iteration if there are no command line switches

NativeOverlapped overlapped;
Win32Error Error;

if (args.Length < 1)
{
	using var hEvent = CreateEvent();
	if (hEvent.IsInvalid)
	{
		Console.Write("CreateEvent: {0}\n", GetLastError());
		return RETURN_FAILURE;
	}
	else unsafe
	{
		overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
		Error = EnableRouter(out var Handle, &overlapped);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			Console.Write("EnableRouter: {0}\n", Error);
			return RETURN_FAILURE;
		}
		else
		{
			Console.Write("Press <Enter> to disable routing");
			Console.ReadLine();
			Error = UnenableRouter(&overlapped, out var Count);
			if (Error.Failed)
			{
				Console.Write("UnenableRouter: {0}\n", Error);
				return RETURN_FAILURE;
			}
			else
			{
				Console.Write("UnenableRouter: {0} references left\n", Count);
			}
		}
	}
	return RETURN_SUCCESS;
}

// Loop continuously for the 'stress' command line switch
if (char.ToLower(args[1][0]) == 's')
{
	while (true)
	{
		using var hEvent = CreateEvent();
		if (hEvent.IsInvalid)
		{
			Console.Write("CreateEvent: {0}\n", GetLastError());
			return RETURN_FAILURE;
		}
		else unsafe
		{
			overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
			Error = EnableRouter(out _, &overlapped);
			if (Error != Win32Error.ERROR_IO_PENDING)
			{
				Console.Write("EnableRouter: {0}\n", Error);
			}
			else
			{
				Error = UnenableRouter(&overlapped, out var Count);
				if (Error.Failed)
				{
					Console.Write("UnenableRouter: {0}\n", Error);
				}
				else
				{
					int i = 1;
					if ((i % 100) == 0)
					{
						Console.Write("Enable/UnenableRouter Stress: {0} iterations\n", i);
					}
					i++;
				}
			}
		}
	}
	//return RETURN_SUCCESS;
}

// Run the API regressions for the 'regress' command line switch

if (char.ToLower(args[1][0]) == 'r')
{
	// Regression test EnableRouter()

	Console.Write("\n\n\tRegression tests for EnableRouter()\n\n");

	// Test 1: Do not zero memory for the Overlapped structure

	Console.Write("\nRegression Test 1: Do not zero memory for the Overlapped structure\n");

	Console.Write("\nRegression Test 1: ZeroMemory(&Overlapped, Marshal.SizeOf(typeof(Overlapped))) NOT called\n");

	using (var hEvent = CreateEvent())
	if (hEvent.IsInvalid)
	{
		FAIL_PRINT("Regression Test 1", "CreateEvent", GetLastError());
	}
	else unsafe
	{
		SUCCESS_PRINT("Regression Test 1", "CreateEvent", GetLastError());
		overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
		Error = EnableRouter(out _, &overlapped);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			FAIL_PRINT("Regression Test 1", "EnableRouter", Error);
		}
		else
		{
			SUCCESS_PRINT("Regression Test 1", "EnableRouter", Error);
			Error = UnenableRouter(&overlapped, out var Count);
			if (Error.Failed)
			{
				FAIL_PRINT("Regression Test 1", "UnenableRouter", Error);
			}
			else
			{
				SUCCESS_PRINT("Regression Test 1", "UnenableRouter", Error);
				Console.Write("Regression Test 1: UnenableRouter: {0} references left\n", Count);
			}
		}
	}

	// Test 2: Overlapped.hEvent is zero

	Console.Write("\nRegression Test 2: Set Overlapped.hEvent equal to zero\n");

	Console.Write("\nRegression Test 2: Overlapped.hEvent\n");

	unsafe
	{
		overlapped = new();
		Error = EnableRouter(out _, &overlapped);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			FAIL_PRINT("Regression Test 2", "EnableRouter", Error);
		}
		else
		{
			SUCCESS_PRINT("Regression Test 2", "EnableRouter", Error);
			Error = UnenableRouter(&overlapped, out var Count);
			if (Error.Failed)
			{
				FAIL_PRINT("Regression Test 2", "UnenableRouter", Error);
			}
			else
			{
				SUCCESS_PRINT("Regression Test 2", "UnenableRouter", Error);
				Console.Write("Regression Test 2: UnenableRouter: {0} references left\n", Count);
			}
		}
	}

	// Test 3: Pass default parameters to EnableRouter()

	Console.Write("\nRegression Test 3: Pass default parameters to EnableRouter()\n");

	Console.Write("\nRegression Test 3.1: EnableRouter(default, &Overlapped)\n");

	using (var hThread = CreateThread(default, 0, NullHandleCase, default, CREATE_THREAD_FLAGS.CREATE_SUSPENDED, out var dwThreadId))
	if (hThread.IsInvalid)
	{
		FAIL_PRINT("Regression Test 3.1", "CreateThread", GetLastError());
	}
	else
	{
		Error = ResumeThread(hThread);
		if (Error == 0xFFFFFFFF)
		{
			FAIL_PRINT("Regression Test 3.1", "ResumeThread", GetLastError());
		}
		else
		{
			var status = WaitForSingleObject(hThread, 10000); // wait 10 seconds for thread to complete
			if (status is WAIT_STATUS.WAIT_FAILED or WAIT_STATUS.WAIT_ABANDONED)
			{
				FAIL_PRINT("Regression Test 3.1", "WaitForSingleObject", GetLastError());
			}
			if (status == WAIT_STATUS.WAIT_OBJECT_0)
			{
				FAIL_PRINT("Regression Test 3.1", "WaitForSingleObject", GetLastError());
				Console.Write("Regression Test 3.1: WAIT_OBJECT_0 was unexpected\n");
			}
			if (status == WAIT_STATUS.WAIT_TIMEOUT)
			{
				SUCCESS_PRINT("Regression Test 3.1", "WaitForSingleObject", Error);
				Console.Write("Regression Test 3.1: WAIT_TIMEOUT was expected\n");
			}
		}
	}

	Console.Write("\nRegression Test 3.2: EnableRouter(&Handle, default)\n");

	using (var hThread = CreateThread(default, 0, NullOverlappedCase, default, CREATE_THREAD_FLAGS.CREATE_SUSPENDED, out var dwThreadId))
	if (hThread.IsInvalid)
	{
		FAIL_PRINT("Regression Test 3.2", "CreateThread", GetLastError());
	}
	else
	{
		Error = ResumeThread(hThread);
		if (Error == 0xFFFFFFFF)
		{
			FAIL_PRINT("Regression Test 3.2", "ResumeThread", GetLastError());
		}
		else
		{
			var status = WaitForSingleObject(hThread, 10000); // wait 10 seconds for thread to complete
			if (status is WAIT_STATUS.WAIT_FAILED or WAIT_STATUS.WAIT_ABANDONED)
			{
				FAIL_PRINT("Regression Test 3.2", "WaitForSingleObject", GetLastError());
			}
			if (status == WAIT_STATUS.WAIT_OBJECT_0)
			{
				FAIL_PRINT("Regression Test 3.2", "WaitForSingleObject", GetLastError());
				Console.Write("Regression Test 3.2: WAIT_OBJECT_0 was unexpected\n");
			}
			if (status == WAIT_STATUS.WAIT_TIMEOUT)
			{
				SUCCESS_PRINT("Regression Test 3.2", "WaitForSingleObject", Error);
				Console.Write("Regression Test 3.2: WAIT_TIMEOUT was expected\n");
			}
		}
	}

	Console.Write("\nRegression Test 3.3: EnableRouter(default, default)\n");

	using (var hThread = CreateThread(default, 0, AllNullParamCase, default, CREATE_THREAD_FLAGS.CREATE_SUSPENDED, out var dwThreadId))
	if (hThread.IsInvalid)
	{
		FAIL_PRINT("Regression Test 3.3", "CreateThread", GetLastError());
	}
	else
	{
		Error = ResumeThread(hThread);
		if (Error == 0xFFFFFFFF)
		{
			FAIL_PRINT("Regression Test 3.3", "ResumeThread", GetLastError());
		}
		else
		{
			var status = WaitForSingleObject(hThread, 10000); // wait 10 seconds for thread to complete
			if (status is WAIT_STATUS.WAIT_FAILED or WAIT_STATUS.WAIT_ABANDONED)
			{
				FAIL_PRINT("Regression Test 3.3", "WaitForSingleObject", GetLastError());
			}
			if (status == WAIT_STATUS.WAIT_OBJECT_0)
			{
				FAIL_PRINT("Regression Test 3.3", "WaitForSingleObject", GetLastError());
				Console.Write("Regression Test 3.3: WAIT_OBJECT_0 was unexpected\n");
			}
			if (status == WAIT_STATUS.WAIT_TIMEOUT)
			{
				SUCCESS_PRINT("Regression Test 3.3", "WaitForSingleObject", Error);
				Console.Write("Regression Test 3.3: WAIT_TIMEOUT was expected\n");
			}
		}
	}

	// Regression test UnenableRouter()

	Console.Write("\n\n\tRegression tests for UnenableRouter()\n\n");

	// Test 4: Use an Overlapped that is different from the one used for EnableRouter

	Console.Write("\nRegression Test 4: Use a different Overlapped than the one used for EnableRouter\n");

	Console.Write("\nRegression Test 4: Declare OVERLAPPED OverlappedUnenableRouter\n");

	using (var hEvent = CreateEvent())
	if (hEvent.IsInvalid)
	{
		FAIL_PRINT("Regression Test 4", "CreateEvent", GetLastError());
	}
	else unsafe
	{
		SUCCESS_PRINT("Regression Test 4", "CreateEvent", GetLastError());
		overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
		Error = EnableRouter(out _, &overlapped);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			FAIL_PRINT("Regression Test 4", "EnableRouter", Error);
		}
		else
		{
			SUCCESS_PRINT("Regression Test 4", "EnableRouter", Error);
			using var hUnEvent = CreateEvent(default, false, false, default);
			if (hUnEvent.IsInvalid)
			{
				FAIL_PRINT("Regression Test 4", "CreateEvent", GetLastError());
			}
			else
			{
				SUCCESS_PRINT("Regression Test 4", "CreateEvent", GetLastError());
				NativeOverlapped overlappedUnenableRouter = new() { EventHandle = hUnEvent.DangerousGetHandle() };
				Error = UnenableRouter(&overlappedUnenableRouter, out var Count);
				if (Error.Failed)
				{
					FAIL_PRINT("Regression Test 4", "UnenableRouter", Error);
				}
				else
				{
					SUCCESS_PRINT("Regression Test 4", "UnenableRouter", Error);
					Console.Write("Regression Test 4: UnenableRouter: {0} references left\n", Count);
				}
			}
		}
	}

	return RETURN_SUCCESS;
}

CommandLineUsage();

return RETURN_FAILURE;

static void FAIL_PRINT(string Test, string API, IErrorProvider Error) => Console.Write("{0}: {1} FAILED: Error {2}\n", Test, API, Error);

static void SUCCESS_PRINT(string Test, string API, IErrorProvider Error) => Console.Write("{0}: {1} SUCCEEDED: Error {2}\n", Test, API, Error);

static void CommandLineUsage()
{
	Console.Error.Write("\nEnableRouter [stress, regress]\n\n");
	Console.Error.Write(" 'enablerouter' with no parameters will step through one iteration of\n");
	Console.Error.Write(" enabling and disabling the router\n\n");
	Console.Error.Write(" 'enablerouter stress' will enable and disable the router in an infinite loop\n\n");
	Console.Error.Write(" 'enablerouter regress' will perform the EnableRouter() and UnenableRouter()\n");
	Console.Error.Write(" regression tests\n\n");
	Console.Error.Write(" Any other parameter will display this help message\n\n");
}

static uint NullHandleCase(IntPtr lpNotUsed)
{
	using var hEvent = CreateEvent();
	if (hEvent.IsInvalid)
	{
		FAIL_PRINT("Regression Test 3.1", "CreateEvent", GetLastError());
	}
	else unsafe
	{
		SUCCESS_PRINT("Regression Test 3.1", "CreateEvent", GetLastError());
		Console.Write("Regression Test 3.1: Call to EnableRouter(default, &Overlapped) is expected to block\n");
		NativeOverlapped overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
		var Error = EnableRouter(out _, &overlapped);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			FAIL_PRINT("Regression Test 3.1", "EnableRouter", Error);
		}
		else
		{
			SUCCESS_PRINT("Regression Test 3.1", "EnableRouter", Error);
			Error = UnenableRouter(&overlapped, out var Count);
			if (Error.Failed)
			{
				FAIL_PRINT("Regression Test 3.1", "UnenableRouter", Error);
			}
			else
			{
				SUCCESS_PRINT("Regression Test 3.1", "UnenableRouter", Error);
				Console.Write("Regression Test 3.1: UnenableRouter: {0} references left\n", Count);
			}
		}
	}
	return RETURN_FAILURE;
}

static uint NullOverlappedCase(IntPtr lpNotUsed)
{
	using var hEvent = CreateEvent();
	if (hEvent.IsInvalid)
	{
		FAIL_PRINT("Regression Test 3.2", "CreateEvent", GetLastError());
	}
	else unsafe
	{
		SUCCESS_PRINT("Regression Test 3.2", "CreateEvent", GetLastError());
		Console.Write("Regression Test 3.2: Call to EnableRouter(&Handle, default) is expected to block\n");
		NativeOverlapped overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
		var Error = EnableRouter(out _, null);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			FAIL_PRINT("Regression Test 3.2", "EnableRouter", Error);
		}
		else
		{
			SUCCESS_PRINT("Regression Test 3.2", "EnableRouter", Error);
			Error = UnenableRouter(&overlapped, out var Count);
			if (Error.Failed)
			{
				FAIL_PRINT("Regression Test 3.2", "UnenableRouter", Error);
			}
			else
			{
				SUCCESS_PRINT("Regression Test 3.2", "UnenableRouter", Error);
				Console.Write("Regression Test 3.2: UnenableRouter: {0} references left\n", Count);
			}
		}
	}
	return RETURN_FAILURE;
}

static uint AllNullParamCase(IntPtr lpNotUsed)
{
	using var hEvent = CreateEvent();
	if (hEvent.IsInvalid)
	{
		FAIL_PRINT("Regression Test 3.3", "CreateEvent", GetLastError());
	}
	else unsafe
	{
		SUCCESS_PRINT("Regression Test 3.3", "CreateEvent", GetLastError());
		Console.Write("Regression Test 3.3: Call to EnableRouter(default, default) is expected to block\n");
		NativeOverlapped overlapped = new() { EventHandle = hEvent.DangerousGetHandle() };
		var Error = EnableRouter(out _, null);
		if (Error != Win32Error.ERROR_IO_PENDING)
		{
			FAIL_PRINT("Regression Test 3.3", "EnableRouter", Error);
		}
		else
		{
			SUCCESS_PRINT("Regression Test 3.3", "EnableRouter", Error);
			Error = UnenableRouter(&overlapped, out var Count);
			if (Error.Failed)
			{
				FAIL_PRINT("Regression Test 3.3", "UnenableRouter", Error);
			}
			else
			{
				SUCCESS_PRINT("Regression Test 3.3", "UnenableRouter", Error);
				Console.Write("Regression Test 3.3: UnenableRouter: {0} references left\n", Count);
			}
		}
	}
	return RETURN_FAILURE;
}