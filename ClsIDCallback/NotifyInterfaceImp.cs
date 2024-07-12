namespace ClsIDCallback;

using Vanara.Extensions;
using Vanara.PInvoke;
using static Utils;
using static Vanara.PInvoke.BITS;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.User32;

internal class CClassFactory : IClassFactory
{
	// Shut down the application.
	public static void AttemptToTerminateServer()
	{
		if (g_lObjsInUse > 0 || g_lServerLocks > 0)
		{
		}
		else
		{
			PostThreadMessage(g_dwMainThreadID, (uint)WindowMessage.WM_QUIT);
		}
	}

	public virtual HRESULT CreateInstance(object? pUnkOuter, in Guid riid, out object? ppvObject)
	{
		ppvObject = null;
		return HRESULT.E_NOTIMPL;
	}

	public virtual HRESULT LockServer(bool fLock)
	{
		if (fLock)
		{
			Interlocked.Increment(ref g_lServerLocks);
		}
		else
		{
			Interlocked.Decrement(ref g_lServerLocks);
		}
		// If this is an out-of-proc server, check to see whether we should shut down.
		AttemptToTerminateServer(); //@local

		return HRESULT.S_OK;
	}
}

internal class CNotifyInterfaceImp : IBackgroundCopyCallback2
{
	public void FileTransferred(IBackgroundCopyJob pJob, IBackgroundCopyFile pFile) { }

	public void JobError(IBackgroundCopyJob pJob, IBackgroundCopyError pError)
	{
		Console.Write("Job Failed\n");
		pJob.Cancel();
		Console.Write("It is OK to close the command window.\n");
	}

	public void JobModification(IBackgroundCopyJob pJob, uint dwReserved) { }

	public void JobTransferred(IBackgroundCopyJob pJob)
	{
		Console.Write("Job Transfred\n");
		pJob.Complete();
		Console.Write("It is OK to close the command window.\n");
	}
}

internal class CNotifyInterfaceImp_Factory : CClassFactory
{
	public override HRESULT CreateInstance(object? pUnkOuter, in Guid riid, out object? ppvObject)
	{
		CNotifyInterfaceImp? pCExeObj01 = default;

		// Initialise the receiver.
		ppvObject = default;

		if (pUnkOuter is not null)
		{
			return HRESULT.CLASS_E_NOAGGREGATION;
		}

		// Create an instance of the component.
		pCExeObj01 = new CNotifyInterfaceImp();

		if (pCExeObj01 is null)
		{
			return HRESULT.E_OUTOFMEMORY;
		}

		pCExeObj01.QueryInterface(riid, out ppvObject);

		Console.Write("return a new callback instance\n");
		return HRESULT.S_OK;
	}
}