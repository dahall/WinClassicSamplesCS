using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.MSCTF;

namespace tsfcase;

internal class CEditSessionBase : ITfEditSession
{
	protected ITfContext pContext;

	public CEditSessionBase(ITfContext pContext) => this.pContext = pContext;

	public virtual HRESULT DoEditSession([In] uint ec) => HRESULT.S_OK;
}