using System.Runtime.InteropServices;
using static Vanara.PInvoke.Rpc;

namespace hello
{
	public interface ihello
	{
		void HelloProc([In] RPC_BINDING_HANDLE h1, [In] string pszString);

		void Shutdown([In] RPC_BINDING_HANDLE h1);
	}
}