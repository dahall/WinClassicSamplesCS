using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using Vanara.PInvoke.InteropServices;

internal static partial class Util
{
	public static HRESULT CreateEnumString(IEnumerable<string> begin, out IEnumString value)
	{
		value = new ComEnumString(begin);
		return HRESULT.S_OK;
	}
}