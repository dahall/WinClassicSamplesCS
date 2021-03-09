using System.Linq;

using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;

namespace Vanara.PInvoke
{
	// CPropertyStoreReader is a helper class that holds onto the PropertyStore of a ShellItem. Property Stores consist of <PROPERTYKEY,
	// PROPVARIANT> pairs. Given a PROPERTYKEY we get the PROPVARIANT value from the Property Store and convert it to the appropriate value
	// (string, int, uint, bool, etc.) before returning it to the caller.
	internal class CPropertyStoreReader
	{
		private IPropertyStore _pps;

		public CPropertyStoreReader(IPropertyStore pps = null) => _pps = pps;

		public CPropertyStoreReader(IShellItem psi, GETPROPERTYSTOREFLAGS flags, PROPERTYKEY[] rgKeys = null, uint cKeys = 0) => _pps = (psi as IShellItem2)?.GetPropertyStoreForKeys(rgKeys, cKeys, flags, typeof(IPropertyStore).GUID);

		~CPropertyStoreReader()
		{
			_pps = null;
		}

		public bool GetBool(in PROPERTYKEY key) => GetValue(key).boolVal;

		public byte[] GetBytes(in PROPERTYKEY key) => GetValue(key).caub.ToArray();

		public int GetInt32(in PROPERTYKEY key) => GetValue(key).lVal;

		public string GetString(in PROPERTYKEY key) => GetValue(key).pwszVal;

		public uint GetUInt32(in PROPERTYKEY key) => GetValue(key).ulVal;

		public ulong GetUInt64(in PROPERTYKEY key) => GetValue(key).ulVal;

		// add more type accessor methods here as needed

		public PROPVARIANT GetValue(in PROPERTYKEY key)
		{
			var propvar = new PROPVARIANT();
			_pps.GetValue(key, propvar);
			return propvar;
		}
	}
}
