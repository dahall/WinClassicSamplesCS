using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace SparkleFinisherLib;

/*internal class Program
{
	private static void Main(string[] args)
	{
		try
		{
			using var s_exitEvent = SafeEventHandle.Create();

			// Prepare COM
			CoInitializeEx(default, COINIT.COINIT_MULTITHREADED).ThrowIfFailed();

			// Leverage WRL to create basic factories for our objects and get them ready to accept calls.
			RegistrationServices
	auto & mod = Module < OutOfProc >::Create(&s_releaseNotifier);

			THROW_IF_FAILED(mod.RegisterObjects());

			// Wait to hear back from the notifier.
			s_exitEvent.Wait(); // infinite wait

			THROW_IF_FAILED(mod.UnregisterObjects());

			// Cleanup of all handles and resources is automatic when using WIL types!
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Sparkle Finish Client Failed with error: '{ex.Message}'");
		}
	}
}*/

[ComVisible(true), Guid("EA27C73A-48C2-4714-9D20-A9D2C4F6AED3"), ClassInterface(ClassInterfaceType.None)]
public class SparkleFinisher : ISparkleFinisher
{
	public HRESULT AddSparkleFinishToFile([In] HFILE file, [In] HEVENT startEvent, out HEVENT finishedEvent)
	{
		try
		{
			// Create a SparkleWork to record the work we are doing for this client. The parameters are valid only for the lifetime of this
			// call, so the SparkleWork constructor duplicates the handles so that we can continue to use them after we return to the caller.
			SparkleWork work = new(file, startEvent);

			// As with all output parameters, COM takes ownership of the handle we return. Since we want to retain access to the handle for
			// ourselves, give the caller a duplicate.
			finishedEvent = work.DuplicateFinishedEventHandle();

			// Schedule the work to continue on the threadpool when the start event is signaled.
			work.ScheduleWork();

			// The threadpool work item owns the work now.

			return HRESULT.S_OK;
		}
		catch (Exception ex) { finishedEvent = default; return ex.HResult; }
	}
}

internal class SparkleWork : IDisposable
{
	private readonly SafeHFILE file;
	private readonly SafeEventHandle startEvent, finishedEvent = SafeEventHandle.Create();
	private SafePTP_WAIT? threadpoolWait;

	internal SparkleWork(HFILE file, HEVENT startEvent)
	{
		this.file = new(Duplicate(file));
		this.startEvent = new(Duplicate(startEvent));
	}

	void IDisposable.Dispose() => finishedEvent.Set();

	internal SafeEventHandle DuplicateFinishedEventHandle() => new(Duplicate(finishedEvent));

	internal void ScheduleWork()
	{
		// Create a threadpool wait that will continue the work.
		GCHandle h = GCHandle.Alloc(this);
		threadpoolWait = CreateThreadpoolWait(WaitCallback, (IntPtr)h);
		h.Free();

		// Schedule the wait to run when the m_startEvent is signaled.
		SetThreadpoolWait(threadpoolWait, startEvent);
	}

	private static IntPtr Duplicate<T>(T h) where T : IHandle
	{
		Win32Error.ThrowLastErrorIfFalse(DuplicateHandle(GetCurrentProcess(), h.DangerousGetHandle(), GetCurrentProcess(), out IntPtr o, 0, false, DUPLICATE_HANDLE_OPTIONS.DUPLICATE_SAME_ACCESS));
		return o;
	}

	private void WaitCallback(PTP_CALLBACK_INSTANCE _, IntPtr Context, PTP_WAIT __, uint WaitResult)
	{
		try
		{
			// Re-attach work pointer to a unique_ptr to ensure it is cleaned up even if an error occurs.
			using SparkleWork work = (SparkleWork)GCHandle.FromIntPtr(Context).Target;

			// If the wait ended for any reason except being signaled properly by the caller, we need to abort.
			if (WAIT_STATUS.WAIT_OBJECT_0 != (WAIT_STATUS)WaitResult) throw ((HRESULT)HRESULT.E_ABORT).GetException();

			// Finalize into string.
			string completeMessage = "Dispensing sparkle finisher.\nRinsing.\nDrying.\nSparkly!\n";

			// Write out to file.
			int bytesToWrite = Encoding.Unicode.GetByteCount(completeMessage);
			Win32Error.ThrowLastErrorIfFalse(WriteFile(work.file, Encoding.Unicode.GetBytes(completeMessage), (uint)bytesToWrite, out uint bytesWritten));

			// On scope exit, the unique_ptr will free the SparkleWork which will signal the m_finishedEvent (to tell the client that we are
			// done) and then clean up the handles.
		}
		// Since this is an anonymous work callback, capture and log errors as there's no one really listening. (A fancier version of this
		// COM method might use some mechanism to report an error to the client, but that is outside the scope of this sample.)
		catch { }
	}
}