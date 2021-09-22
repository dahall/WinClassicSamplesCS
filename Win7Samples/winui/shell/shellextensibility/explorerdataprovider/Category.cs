using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static explorerdataprovider.Guids;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;

namespace explorerdataprovider
{
	[ComVisible(true)]
	public class CFolderViewImplCategoryProvider : ICategoryProvider
	{
		private readonly IShellFolder2 m_psf;

		public CFolderViewImplCategoryProvider(IShellFolder2 psf) => m_psf = psf;

		public HRESULT CanCategorizeOnSCID(in Ole32.PROPERTYKEY pscid) => pscid == PROPERTYKEY.System.ItemNameDisplay ||
			pscid == PKEY_Microsoft_SDKSample_AreaSize || pscid == PKEY_Microsoft_SDKSample_DirectoryLevel || pscid == PKEY_Microsoft_SDKSample_NumberOfSides ?
			HRESULT.S_OK : HRESULT.S_FALSE;

		public HRESULT CreateCategory(in Guid pguid, in Guid riid, out object ppv)
		{
			switch (pguid)
			{
				case var p when p == CAT_GUID_NAME:
					ppv = new CFolderViewImplCategorizer_Name(m_psf);
					break;

				case var p when p == CAT_GUID_SIZE:
					ppv = new CFolderViewImplCategorizer_Size(m_psf);
					break;

				case var p when p == CAT_GUID_LEVEL:
					ppv = new CFolderViewImplCategorizer_Level(m_psf);
					break;

				case var p when p == CAT_GUID_SIDES:
					ppv = new CFolderViewImplCategorizer_Sides(m_psf);
					break;

				case var p when p == CAT_GUID_VALUE:
					ppv = new CFolderViewImplCategorizer_Value(m_psf);
					break;

				default:
					ppv = default;
					return HRESULT.E_INVALIDARG;
			}
			return HRESULT.S_OK;
		}

		public HRESULT EnumCategories(out Ole32.IEnumGUID penum)
		{
			penum = new CFolderViewImplEnumGUID();
			return HRESULT.S_OK;
		}

		public HRESULT GetCategoryForSCID(in Ole32.PROPERTYKEY pscid, out Guid pguid)
		{
			switch (pscid)
			{
				case var p when p == PROPERTYKEY.System.ItemNameDisplay:
					pguid = CAT_GUID_NAME;
					break;

				case var p when p == PKEY_Microsoft_SDKSample_AreaSize:
					pguid = CAT_GUID_SIZE;
					break;

				case var p when p == PKEY_Microsoft_SDKSample_DirectoryLevel:
					pguid = CAT_GUID_LEVEL;
					break;

				case var p when p == PKEY_Microsoft_SDKSample_NumberOfSides:
					pguid = CAT_GUID_SIDES;
					break;

				default:
					if (pscid.fmtid == Guid.Empty)
					{
						pguid = CAT_GUID_VALUE;
					}
					else
					{
						pguid = Guid.Empty;
						return HRESULT.E_INVALIDARG;
					}
					break;
			}
			return HRESULT.S_OK;
		}

		public HRESULT GetCategoryName(in Guid pguid, StringBuilder pszName, uint cch) => pguid == CAT_GUID_VALUE
				? Utils.StringCchCopy(pszName, (int)cch, Properties.Resources.ResourceManager.GetString("IDS_VALUE"))
				: HRESULT.E_FAIL;

		public HRESULT GetDefaultCategory(out Guid pguid, out Ole32.PROPERTYKEY pscid)
		{
			pguid = CAT_GUID_LEVEL;
			pscid = default;
			return HRESULT.S_OK;
		}
	}

	internal abstract class CFolderViewImplCategorizerBase : ICategorizer
	{
		private static readonly Dictionary<uint, string> strLookup = new()
		{
			{ 103, "IDS_EXPLORE" },
			{ 104, "IDS_OPEN" },
			{ 105, "IDS_GROUPBYSIDES" },
			{ 106, "IDS_EXPLORE_HELP" },
			{ 107, "IDS_OPEN_HELP" },
			{ 108, "IDS_1" },
			{ 109, "IDS_2" },
			{ 110, "IDS_3" },
			{ 111, "IDS_4" },
			{ 112, "IDS_5" },
			{ 113, "IDS_6" },
			{ 114, "IDS_7" },
			{ 115, "IDS_8" },
			{ 116, "IDS_9" },
			{ 117, "IDS_0" },
			{ 118, "IDS_GROUPBYSIZE" },
			{ 119, "IDS_GROUPBYALPHA" },
			{ 120, "IDS_VALUE" },
			{ 121, "IDS_SMALL" },
			{ 122, "IDS_MEDIUM" },
			{ 123, "IDS_LARGE" },
			{ 124, "IDS_LESSTHAN5" },
			{ 125, "IDS_5ORGREATER" },
			{ 126, "IDS_GROUPBYVALUE" },
			{ 127, "IDS_GROUPBYLEVEL" },
			{ 128, "IDS_RECTANGLE" },
			{ 129, "IDS_TRIANGLE" },
			{ 130, "IDS_CIRCLE" },
			{ 131, "IDS_POLYGON" },
			{ 132, "IDS_STRING132" },
			{ 132, "IDS_TITLE" },
			{ 133, "IDS_DISPLAY" },
			{ 134, "IDS_UNSPECIFIED" },
			{ 135, "IDS_SETTINGS" },
			{ 136, "IDS_SETTING1" },
			{ 137, "IDS_SETTING2" },
			{ 138, "IDS_SETTING3" },
			{ 139, "IDS_DISPLAY_TT" },
			{ 140, "IDS_SETTINGS_TT" },
			{ 141, "IDS_SETTING1_TT" },
			{ 142, "IDS_SETTING2_TT" },
			{ 143, "IDS_SETTING3_TT" },
		};

		protected IShellFolder2 m_psf;

		protected CFolderViewImplCategorizerBase(IShellFolder2 psf) => m_psf = psf;

		protected abstract string Description { get; }

		public virtual HRESULT CompareCategory(CATSORT_FLAGS csfFlags, uint dwCategoryId1, uint dwCategoryId2) => Utils.ResultFromShort((short)(dwCategoryId1 - dwCategoryId2));

		public abstract HRESULT GetCategory(uint cidl, IntPtr[] apidl, uint[] rgCategoryIds);

		public virtual HRESULT GetCategoryInfo(uint dwCategoryId, out CATEGORY_INFO pci)
		{
			pci = new() { wszName = GetCategoryInfo(dwCategoryId) };
			return HRESULT.S_OK;
		}

		public virtual HRESULT GetDescription(StringBuilder pszDesc, uint cch) => Utils.StringCchCopy(pszDesc, (int)cch, Description);

		protected virtual string GetCategoryInfo(uint dwCategoryId) => LookupStrId(dwCategoryId);

		protected static string LookupStrId(uint id) => strLookup.TryGetValue(id, out var s) ? Properties.Resources.ResourceManager.GetString(s) : id.ToString();

		protected static uint RevLookupStrId(string id) => strLookup.FirstOrDefault(kv => kv.Value == id).Key;
	}

	[ComVisible(true)]
	internal class CFolderViewImplCategorizer_Level : CFolderViewImplCategorizerBase
	{
		public CFolderViewImplCategorizer_Level(IShellFolder2 psf) : base(psf) { }

		protected override string Description => Properties.Resources.IDS_GROUPBYLEVEL;

		public override HRESULT GetCategory(uint cidl, IntPtr[] apidl, uint[] rgCategoryIds)
		{
			HRESULT hr = HRESULT.E_INVALIDARG;  // cidl == 0
			for (uint i = 0; i < cidl; i++)
			{
				hr = m_psf.GetDetailsEx(apidl[0], PKEY_Microsoft_SDKSample_DirectoryLevel, out var v);
				if (hr.Succeeded)
				{
					rgCategoryIds[0] = v is int vi ? (uint)vi : 0;
				}
				else
				{
					break;
				}
			}
			return hr;
		}

		protected override string GetCategoryInfo(uint dwCategoryId) => dwCategoryId.ToString();
	}

	[ComVisible(true)]
	internal class CFolderViewImplCategorizer_Name : CFolderViewImplCategorizerBase
	{
		public CFolderViewImplCategorizer_Name(IShellFolder2 psf) : base(psf) { }

		protected override string Description => Properties.Resources.IDS_GROUPBYALPHA;

		public override HRESULT GetCategory(uint cidl, IntPtr[] apidl, uint[] rgCategoryIds)
		{
			HRESULT hr = HRESULT.E_INVALIDARG; // cidl == 0
			for (uint i = 0; i < cidl; i++)
			{
				hr = m_psf.GetDetailsEx(apidl[i], PROPERTYKEY.System.ItemNameDisplay, out var v);
				if (hr.Succeeded)
				{
					rgCategoryIds[i] = (v as string)?[0] ?? '\0';
				}
				else
				{
					break;
				}
			}
			return hr;
		}

		protected override string GetCategoryInfo(uint dwCategoryId) => new(new[] { Convert.ToChar(dwCategoryId) });
	}

	[ComVisible(true)]
	internal class CFolderViewImplCategorizer_Sides : CFolderViewImplCategorizerBase
	{
		public CFolderViewImplCategorizer_Sides(IShellFolder2 psf) : base(psf) { }

		protected override string Description => Properties.Resources.IDS_GROUPBYSIDES;

		public override HRESULT GetCategory(uint cidl, IntPtr[] apidl, uint[] rgCategoryIds)
		{
			HRESULT hr = HRESULT.E_INVALIDARG; //cidl == 0
			for (uint i = 0; i < cidl; i++)
			{
				hr = m_psf.GetDetailsEx(apidl[0], PKEY_Microsoft_SDKSample_NumberOfSides, out var v);
				if (hr.Succeeded)
				{
					rgCategoryIds[i] = RevLookupStrId("IDS_UNSPECIFIED");
					if (v is int vi)
					{
						switch (vi)
						{
							case 0:
								rgCategoryIds[i] = RevLookupStrId("IDS_CIRCLE");
								break;

							case 3:
								rgCategoryIds[i] = RevLookupStrId("IDS_TRIANGLE");
								break;

							case 4:
								rgCategoryIds[i] = RevLookupStrId("IDS_RECTANGLE");
								break;

							case >= 5:
								rgCategoryIds[i] = RevLookupStrId("IDS_POLYGON");
								break;

							default:
								break;
						}
					}
				}
				else
				{
					break;
				}
			}
			return hr;
		}
	}

	[ComVisible(true)]
	internal class CFolderViewImplCategorizer_Size : CFolderViewImplCategorizerBase
	{
		public CFolderViewImplCategorizer_Size(IShellFolder2 psf) : base(psf) { }

		protected override string Description => Properties.Resources.IDS_GROUPBYSIZE;

		public override HRESULT GetCategory(uint cidl, IntPtr[] apidl, uint[] rgCategoryIds)
		{
			HRESULT hr = HRESULT.E_INVALIDARG; //cidl == 0
			for (uint i = 0; i < cidl; i++)
			{
				hr = m_psf.GetDetailsEx(apidl[0], PKEY_Microsoft_SDKSample_AreaSize, out var v);
				if (hr.Succeeded)
				{
					rgCategoryIds[i] = RevLookupStrId("IDS_UNSPECIFIED");

					if (v is string s)
					{
						var nSize = int.Parse(s);
						if (nSize < byte.MaxValue / 3)
						{
							rgCategoryIds[i] = RevLookupStrId("IDS_SMALL");
						}
						else if (nSize < 2 * byte.MaxValue / 3)
						{
							rgCategoryIds[i] = RevLookupStrId("IDS_MEDIUM");
						}
						else
						{
							rgCategoryIds[i] = RevLookupStrId("IDS_LARGE");
						}
					}
				}
				else
				{
					break;
				}
			}
			return hr;
		}
	}

	[ComVisible(true)]
	internal class CFolderViewImplCategorizer_Value : CFolderViewImplCategorizerBase
	{
		public CFolderViewImplCategorizer_Value(IShellFolder2 psf) : base(psf) { }

		protected override string Description => Properties.Resources.IDS_GROUPBYVALUE;

		public override HRESULT GetCategory(uint cidl, IntPtr[] apidl, uint[] rgCategoryIds)
		{
			HRESULT hr = HRESULT.S_OK;
			for (uint i = 0; i < cidl; i++)
			{
				hr = m_psf.GetDetailsEx(apidl[i], PROPERTYKEY.System.ItemNameDisplay, out var v);
				if (hr.Succeeded)
				{
					hr = Utils.LoadFolderViewImplDisplayStrings(out var rgNames);
					if (hr.Succeeded)
					{
						// Find their place in the array.
						int p = Array.IndexOf(rgNames, v as string);
						rgCategoryIds[i] = RevLookupStrId(p < rgNames.Length / 2 ? "IDS_LESSTHAN5" : "IDS_5ORGREATER");
					}
				}
				else
				{
					break;
				}
			}
			return hr;
		}
	}

	[ComVisible(true)]
	internal class CFolderViewImplEnumGUID : IEnumGUID
	{
		public const int MAX_CATEGORIES = 1;  // These are additional categories beyond the columns

		private uint m_ulCurrentIndex;

		public IEnumGUID Clone() => throw new NotImplementedException();

		public HRESULT Next(uint celt, Guid[] rgelt, out uint pceltFetched)
		{
			HRESULT hr = (celt != 1) ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
			if (hr.Succeeded)
			{
				hr = (m_ulCurrentIndex < MAX_CATEGORIES) ? HRESULT.S_OK : HRESULT.S_FALSE;
				if (HRESULT.S_OK == hr)
				{
					switch (m_ulCurrentIndex++)
					{
						case 0:
							rgelt = new[] { CAT_GUID_VALUE };
							break;
					}
				}
			}
			pceltFetched = (HRESULT.S_OK == hr) ? 1U : 0;
			return hr;
		}

		public void Reset() => m_ulCurrentIndex = 0;

		public HRESULT Skip(uint celt)
		{
			m_ulCurrentIndex += celt;
			return HRESULT.S_OK;
		}
	}
}