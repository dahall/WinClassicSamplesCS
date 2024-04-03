using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static explorerdataprovider.Guids;
using static explorerdataprovider.Utils;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace explorerdataprovider;

public class CFolderViewCB : IShellFolderViewCB, IFolderViewSettings
{
	public PropSys.IPropertyDescriptionList GetColumnPropertyList(in Guid riid) => throw new NotImplementedException();

	public void GetFolderFlags(out FOLDERFLAGS pfolderMask, out FOLDERFLAGS pfolderFlags)
	{
		pfolderMask = FOLDERFLAGS.FWF_USESEARCHFOLDER;
		pfolderFlags = FOLDERFLAGS.FWF_USESEARCHFOLDER;
	}

	public void GetGroupByProperty(out PROPERTYKEY pkey, out bool pfGroupAscending) => throw new NotImplementedException();

	public uint GetGroupSubsetCount() => throw new NotImplementedException();

	public uint GetIconSize() => throw new NotImplementedException();

	public void GetSortColumns(SORTCOLUMN[] rgSortColumns, uint cColumnsIn, out uint pcColumnsOut) => throw new NotImplementedException();

	public FOLDERLOGICALVIEWMODE GetViewMode() => throw new NotImplementedException();

	public HRESULT MessageSFVCB(SFVM uMsg, IntPtr wParam, IntPtr lParam, ref IntPtr plResult) => HRESULT.E_NOTIMPL;
}

public class CFolderViewImplEnumIDList : IEnumIDList
{
	private readonly ITEMDATA[] m_aData = new ITEMDATA[MAX_OBJS];
	private readonly SHCONTF m_grfFlags;
	private int m_nItem;
	private readonly int m_nLevel;
	private readonly CFolderViewImplFolder m_pFolder;

	public CFolderViewImplEnumIDList(SHCONTF grfFlags, int nLevel, CFolderViewImplFolder pFolderViewImplShellFolder)
	{
		m_grfFlags = grfFlags; m_nLevel = nLevel; m_pFolder = pFolderViewImplShellFolder;
		Initialize();
	}

	public IEnumIDList Clone() =>
		// this method is rarely used and it's acceptable to not implement it.
		throw new NotImplementedException();

	public HRESULT Next(uint celt, IntPtr[] rgelt, out uint pceltFetched)
	{
		uint celtFetched = 0;

		HRESULT hr = celt <= 1 ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
		if (hr.Succeeded)
		{
			uint i = 0;
			while (hr.Succeeded && i < celt && m_nItem < m_aData.Length)
			{
				var fSkip = false;
				if (!m_grfFlags.IsFlagSet(SHCONTF.SHCONTF_STORAGE))
				{
					if (m_aData[m_nItem].fIsFolder)
					{
						if (!m_grfFlags.IsFlagSet(SHCONTF.SHCONTF_FOLDERS))
						{
							// this is a folder, but caller doesnt want folders
							fSkip = true;
						}
					}
					else
					{
						if (!m_grfFlags.IsFlagSet(SHCONTF.SHCONTF_NONFOLDERS))
						{
							// this is a file, but caller doesnt want files
							fSkip = true;
						}
					}
				}

				if (!fSkip)
				{
					hr = CFolderViewImplFolder.CreateChildID(m_aData[m_nItem].szName, m_nLevel, m_aData[m_nItem].nSize, m_aData[m_nItem].nSides, m_aData[m_nItem].fIsFolder, out PIDL pidl);
					if (hr.Succeeded)
					{
						rgelt[i] = pidl.DangerousGetHandle();
						celtFetched++;
						i++;
					}
				}

				m_nItem++;
			}
		}

		pceltFetched = celtFetched;

		return (celtFetched == celt) ? HRESULT.S_OK : HRESULT.S_FALSE;
	}

	public void Reset() => m_nItem = 0;

	public void Skip(uint celt) => m_nItem += (int)celt;

	private HRESULT Initialize()
	{
		HRESULT hr = HRESULT.S_OK;
		for (var i = 0; hr.Succeeded && i < m_aData.Length; i++)
		{
			hr = LoadFolderViewImplDisplayString(i, out var str);
			if (hr.Succeeded)
			{
				// Just hardcode the values here now.
				m_aData[i] = new ITEMDATA { nSize = i, nSides = 3, fIsFolder = ISFOLDERFROMINDEX(i), szName = str! };
			}
		}
		return hr;
	}
}

public class CFolderViewImplFolder : IShellFolder2, IPersistFolder2
{
	public const int g_nMaxLevel = 5;

	internal const ushort MYOBJID = 0x1234;
	private readonly uint m_nLevel;
	private PIDL? m_pidl; // where this folder is in the name space
	private string[]? m_rgNames = null;
	private readonly string m_szModuleName = string.Empty;

	public CFolderViewImplFolder(uint nLevel) => m_nLevel = nLevel;

	public HRESULT BindToObject(PIDL pidl, IBindCtx? pbc, in Guid riid, out object? ppv)
	{
		ppv = default;
		if (!pidl.IsInvalid)
		{

			// Initialize it.
			PIDL pidlFirst = ILCloneFirst((IntPtr)pidl);
			if (m_pidl is not null && pidlFirst is not null)
			{
				var pidlBind = PIDL.Combine(m_pidl, pidlFirst);
				if (pidlBind is not null)
				{
					CFolderViewImplFolder pCFolderViewImplFolder = new(m_nLevel + 1);
					PIDL pidlNext = ILNext((IntPtr)pidl);

					if (pidlNext.IsEmpty)
					{
						// If we're reached the end of the idlist, return the interfaces we support for this item.
						// Other potential handlers to return include IPropertyStore, IStream, IStorage, etc.
						return ShellUtil.QueryInterface(pCFolderViewImplFolder, riid, out ppv);
					}
					else
					{
						// Otherwise we delegate to our child folder to let it bind to the next level.
						return pCFolderViewImplFolder.BindToObject(pidlNext, pbc, riid, out ppv);
					}
				}
			}
		}
		return HRESULT.E_INVALIDARG;
	}

	public HRESULT BindToStorage(PIDL pidl, IBindCtx? pbc, in Guid riid, out object? ppv) => BindToObject(pidl, pbc, riid, out ppv);

	public HRESULT CompareIDs(IntPtr lParam, PIDL pidl1, PIDL pidl2)
	{
		HRESULT hr;
		var shc = (SHCIDS)unchecked((uint)lParam.ToInt32());
		if ((shc & (SHCIDS.SHCIDS_CANONICALONLY | SHCIDS.SHCIDS_ALLFIELDS)) != 0)
		{
			// First do a "canonical" comparison, meaning that we compare with the intent to determine item
			// identity as quickly as possible. The sort order is arbitrary but it must be consistent.
			hr = GetName(pidl1, out var psz1);
			if (hr.Succeeded)
			{
				hr = GetName(pidl2, out var psz2);
				if (hr.Succeeded)
				{
					hr = ResultFromShort(StrCmp(psz1, psz2));
				}
			}

			// If we've been asked to do an all-fields comparison, test for any other fields that
			// may be different in an item that shares the same identity. For example if the item
			// represents a file, the identity may be just the filename but the other fields contained
			// in the idlist may be file size and file modified date, and those may change over time.
			// In our example let's say that "level" is the data that could be different on the same item.
			if ((ResultFromShort(0) == hr) && shc.IsFlagSet(SHCIDS.SHCIDS_ALLFIELDS))
			{
				hr = GetLevel(pidl1, out var cLevel1);
				if (hr.Succeeded)
				{
					hr = GetLevel(pidl2, out var cLevel2);
					if (hr.Succeeded)
					{
						hr = ResultFromShort(cLevel1 - cLevel2);
					}
				}
			}
		}
		else
		{
			// Compare child ids by column data (lParam & SHCIDS_COLUMNMASK).
			hr = ResultFromShort(0);
			switch ((uint)(shc & SHCIDS_COLUMNMASK))
			{
				case 0: // Column one, Name.
						// Load the strings that represent the names
					if (m_rgNames is null)
					{
						hr = LoadFolderViewImplDisplayStrings(out m_rgNames);
					}
					if (hr.Succeeded)
					{
						hr = GetName(pidl1, out var psz1);
						if (hr.Succeeded)
						{
							hr = GetName(pidl2, out var psz2);
							if (hr.Succeeded)
							{
								// Find their place in the array.
								// This is a display sort so we want to sort by "one" "two" "three" instead of alphabetically.
								int nPidlOne = 0, nPidlTwo = 0;
								for (var i = 0; i < m_rgNames.Length; i++)
								{
									if (0 == StrCmp(psz1, m_rgNames[i]))
									{
										nPidlOne = i;
									}

									if (0 == StrCmp(psz2, m_rgNames[i]))
									{
										nPidlTwo = i;
									}
								}

								hr = ResultFromShort(nPidlOne - nPidlTwo);
							}
						}
					}
					break;

				case 1: // Column two, Size.
					hr = GetSize(pidl1, out var nSize1);
					if (hr.Succeeded)
					{
						hr = GetSize(pidl2, out var nSize2);
						if (hr.Succeeded)
						{
							hr = ResultFromShort(nSize1 - nSize2);
						}
					}
					break;
				case 2: // Column Three, Sides.
					hr = GetSides(pidl1, out var nSides1);
					if (hr.Succeeded)
					{
						hr = GetSides(pidl2, out var nSides2);
						if (hr.Succeeded)
						{
							hr = ResultFromShort(nSides1 - nSides2);
						}
					}
					break;
				case 3: // Column four, Level.
					hr = GetLevel(pidl1, out var cLevel1);
					if (hr.Succeeded)
					{
						hr = GetLevel(pidl2, out var cLevel2);
						if (hr.Succeeded)
						{
							hr = ResultFromShort(cLevel1 - cLevel2);
						}
					}
					break;
				default:
					hr = ResultFromShort(1);
					break;
			}
		}

		if (ResultFromShort(0) == hr)
		{
			// Continue on by binding to the next level.
			hr = ILCompareRelIDs(this, pidl1, pidl2, shc);
		}
		return hr;
	}

	public HRESULT CreateViewObject(HWND hwndOwner, in Guid riid, out object? ppv)
	{
		ppv = default;

		HRESULT hr = HRESULT.E_NOINTERFACE;
		if (riid == typeof(IShellView).GUID)
		{
			SFV_CREATE csfv = new() { cbSize = (uint)Marshal.SizeOf(typeof(SFV_CREATE)), pshf = this, psfvcb = new CFolderViewCB() };
			hr = SHCreateShellFolderView(csfv, out IShellView? shv);
			if (hr.Succeeded) ppv = shv;
		}
		else if (riid == typeof(ICategoryProvider).GUID)
		{
			ppv = new CFolderViewImplCategoryProvider(this);
		}
		else if (riid == typeof(IContextMenu).GUID)
		{
			// This is the background context menu for the folder itself, not the context menu on items within it.
			DEFCONTEXTMENU dcm = new() { hwnd = hwndOwner, pidlFolder = (IntPtr)(m_pidl ?? IntPtr.Zero), psf = this };
			hr = SHCreateDefaultContextMenu(dcm, riid, out ppv);
		}
		else if (riid == typeof(IExplorerCommandProvider).GUID)
		{
			ppv = new CFolderViewCommandProvider();
		}
		return hr;
	}

	public HRESULT EnumObjects(HWND hwnd, SHCONTF grfFlags, out IEnumIDList? ppenumIDList)
	{
		ppenumIDList = m_nLevel >= g_nMaxLevel ? default : new CFolderViewImplEnumIDList(grfFlags, (int)m_nLevel + 1, this);
		return ppenumIDList is null ? HRESULT.S_FALSE : HRESULT.S_OK;
	}

	public HRESULT EnumSearches(out IEnumExtraSearch? ppenum) { ppenum = null; return HRESULT.E_NOINTERFACE; }

	public HRESULT GetAttributesOf(uint cidl, IntPtr[] apidl, ref SFGAO rgfInOut)
	{
		// If SFGAO_FILESYSTEM is returned, GetDisplayNameOf(SHGDN_FORPARSING) on that item MUST
		// return a filesystem path.
		HRESULT hr = HRESULT.E_INVALIDARG;
		if (1 == cidl)
		{
			hr = GetLevel(apidl[0], out var nLevel);
			if (hr.Succeeded)
			{
				hr = GetFolderness(apidl[0], out var fIsFolder);
				if (hr.Succeeded)
				{
					SFGAO dwAttribs = 0;
					if (fIsFolder)
					{
						dwAttribs |= SFGAO.SFGAO_FOLDER;
					}
					if (nLevel < g_nMaxLevel)
					{
						dwAttribs |= SFGAO.SFGAO_HASSUBFOLDER;
					}
					rgfInOut &= dwAttribs;
				}
			}
		}
		return hr;
	}

	public Guid GetClassID() => CLSID_FolderViewImpl;

	public HRESULT GetCurFolder(ref PIDL ppidl)
	{
		HRESULT hr = m_pidl is not null ? HRESULT.S_OK : HRESULT.E_FAIL;
		if (hr.Succeeded)
		{
			ppidl = ILClone((IntPtr)m_pidl!);
			hr = ppidl is not null ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
		}
		return hr;
	}

	public HRESULT GetDefaultColumn(uint dwRes, out uint pSort, out uint pDisplay)
	{
		pSort = 0;
		pDisplay = 0;
		return HRESULT.S_OK;
	}

	public HRESULT GetDefaultColumnState(uint iColumn, out SHCOLSTATE pcsFlags)
	{
		HRESULT hr = (iColumn < 3) ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
		pcsFlags = hr.Succeeded ? SHCOLSTATE.SHCOLSTATE_ONBYDEFAULT | SHCOLSTATE.SHCOLSTATE_TYPE_STR : 0;
		return hr;
	}

	public HRESULT GetDefaultSearchGUID(out Guid pguid)
	{
		pguid = default;
		return HRESULT.E_NOTIMPL;
	}

	public HRESULT GetDetailsEx(PIDL pidl, in PROPERTYKEY pscid, out object? pv)
	{
		HRESULT hr = GetFolderness(pidl, out var pfIsFolder);
		if (hr.Succeeded)
		{
			if (!pfIsFolder && pscid == PROPERTYKEY.System.PropList.PreviewDetails)
			{
				// This proplist indicates what properties are shown in the details pane at the bottom of the explorer browser.
				pv = "prop:Microsoft.SDKSample.AreaSize;Microsoft.SDKSample.NumberOfSides;Microsoft.SDKSample.DirectoryLevel";
				hr = HRESULT.S_OK;
			}
			else
			{
				hr = GetColumnDisplayName(pidl, pscid, out _, default, 0);
			}
		}

		pv = null;
		return hr;
	}

	public HRESULT GetDetailsOf(PIDL pidl, uint iColumn, out SHELLDETAILS pDetails)
	{
		HRESULT hr = MapColumnToSCID(iColumn, out var key);
		pDetails = new() { cxChar = 24 };
		var szRet = new StringBuilder(Kernel32.MAX_PATH);

		if (pidl is null)
		{
			// No item means we're returning information about the column itself.
			switch (iColumn)
			{
				case 0:
					pDetails.fmt = ComCtl32.ListViewColumnFormat.LVCFMT_LEFT;
					hr = StringCchCopy(szRet, szRet.Length, "Name");
					break;
				case 1:
					pDetails.fmt = ComCtl32.ListViewColumnFormat.LVCFMT_CENTER;
					hr = StringCchCopy(szRet, szRet.Length, "Size");
					break;
				case 2:
					pDetails.fmt = ComCtl32.ListViewColumnFormat.LVCFMT_CENTER;
					hr = StringCchCopy(szRet, szRet.Length, "Sides");
					break;
				case 3:
					pDetails.fmt = ComCtl32.ListViewColumnFormat.LVCFMT_CENTER;
					hr = StringCchCopy(szRet, szRet.Length, "Level");
					break;
				default:
					// GetDetailsOf is called with increasing column indices until failure.
					hr = HRESULT.E_FAIL;
					break;
			}
		}
		else if (hr.Succeeded)
		{
			hr = GetColumnDisplayName(pidl, key, out _, szRet, (uint)szRet.Length);
		}

		if (hr.Succeeded)
		{
			hr = StringToStrRet(szRet.ToString(), out pDetails.str);
		}

		return hr;
	}

	public HRESULT GetDisplayNameOf(PIDL pidl, SHGDNF uFlags, out STRRET pName)
	{
		HRESULT hr = HRESULT.S_OK;
		pName = default;
		if ((uFlags & SHGDNF.SHGDN_FORPARSING) != 0)
		{
			string szDisplayName = "";
			if ((uFlags & SHGDNF.SHGDN_INFOLDER) != 0)
			{
				// This form of the display name needs to be handled by ParseDisplayName.
				hr = GetName(pidl, out szDisplayName);
			}
			else
			{
				hr = SHGetNameFromIDList(m_pidl!, (uFlags & SHGDNF.SHGDN_FORADDRESSBAR) != 0 ? SIGDN.SIGDN_DESKTOPABSOLUTEEDITING : SIGDN.SIGDN_DESKTOPABSOLUTEPARSING, out var pszThisFolder);
				if (hr.Succeeded)
				{
					szDisplayName = pszThisFolder + '\\';
					hr = GetName(pidl, out var szName);
					if (hr.Succeeded)
					{
						szDisplayName += szName;
					}
				}
			}
			if (hr.Succeeded)
			{
				hr = StringToStrRet(szDisplayName, out pName);
			}
		}
		else
		{
			hr = GetName(pidl, out var pszName);
			if (hr.Succeeded)
			{
				hr = StringToStrRet(pszName, out pName);
			}
		}
		return hr;
	}

	public HRESULT GetUIObjectOf(HWND hwndOwner, uint cidl, IntPtr[] apidl, in Guid riid, IntPtr rgfReserved, out object? ppv)
	{
		ppv = default;
		HRESULT hr;

		if (riid == typeof(IContextMenu).GUID)
		{
			// The default context menu will call back for IQueryAssociations to determine the
			// file associations with which to populate the menu.
			using var pi = new PinnedObject(apidl);
			DEFCONTEXTMENU dcm = new() { hwnd = hwndOwner, pidlFolder = (IntPtr)m_pidl!, psf = this, cidl = cidl, apidl = pi };
			hr = SHCreateDefaultContextMenu(dcm, riid, out ppv);
		}
		else if (riid == typeof(IExtractIconW).GUID)
		{
			hr = SHCreateDefaultExtractIcon(typeof(IDefaultExtractIconInit).GUID, out var pdxi);
			if (hr.Succeeded)
			{
				hr = GetFolderness(apidl[0], out var fIsFolder);
				if (hr.Succeeded)
				{
					// This refers to icon indices in shell32. You can also supply custom icons or
					// register IExtractImage to support general images.
					pdxi.SetNormalIcon("shell32.dll", fIsFolder ? 4 : 1);
				}
				if (hr.Succeeded)
				{
					hr = ShellUtil.QueryInterface(pdxi, riid, out ppv);
				}
			}
		}
		else if (riid == typeof(IDataObject).GUID)
		{
			hr = SHCreateDataObject(m_pidl!, cidl, apidl, default, riid, out var ido);
			ppv = ido;
		}
		else if (riid == typeof(IQueryAssociations).GUID)
		{
			hr = GetFolderness(apidl[0], out var fIsFolder);
			if (hr.Succeeded)
			{
				// the type of the item can be determined here. we default to "FolderViewSampleType", which has
				// a context menu registered for it.
				if (fIsFolder)
				{
					ASSOCIATIONELEMENT[] rgAssocFolder = new ASSOCIATIONELEMENT[]
					{
						new() { ac = ASSOCCLASS.ASSOCCLASS_PROGID_STR, pszClass = "FolderViewSampleType"},
						new() { ac = ASSOCCLASS.ASSOCCLASS_FOLDER },
					};
					hr = AssocCreateForClasses(rgAssocFolder, (uint)rgAssocFolder.Length, riid, out ppv);
				}
				else
				{
					ASSOCIATIONELEMENT[] rgAssocItem = new ASSOCIATIONELEMENT[]
					{
						new() { ac = ASSOCCLASS.ASSOCCLASS_PROGID_STR, pszClass = "FolderViewSampleType"},
					};
					hr = AssocCreateForClasses(rgAssocItem, (uint)rgAssocItem.Length, riid, out ppv);
				}
			}
		}
		else
		{
			hr = HRESULT.E_NOINTERFACE;
		}
		return hr;
	}

	public void Initialize(PIDL pidl)
	{
		m_pidl = ILClone((IntPtr)pidl);
		if (m_pidl is null) throw new COMException(null, HRESULT.E_FAIL);
	}

	public HRESULT MapColumnToSCID(uint iColumn, out PROPERTYKEY pkey)
	{
		// The property keys returned here are used by the categorizer.
		HRESULT hr = HRESULT.S_OK;
		switch (iColumn)
		{
			case 0:
				pkey = PROPERTYKEY.System.ItemNameDisplay;
				break;
			case 1:
				pkey = PKEY_Microsoft_SDKSample_AreaSize;
				break;
			case 2:
				pkey = PKEY_Microsoft_SDKSample_NumberOfSides;
				break;
			case 3:
				pkey = PKEY_Microsoft_SDKSample_DirectoryLevel;
				break;
			default:
				pkey = default;
				hr = HRESULT.E_FAIL;
				break;
		}
		return hr;
	}

	public HRESULT ParseDisplayName(HWND hwnd, IBindCtx? pbc, string pszName, out uint pchEaten, out PIDL ppidl, ref SFGAO pdwAttributes)
	{
		HRESULT hr = HRESULT.E_INVALIDARG;
		pchEaten = 0;
		ppidl = PIDL.Null;

		if (null != pszName)
		{
			var szNameComponent = new StringBuilder(Kernel32.MAX_PATH);

			// extract first component of the display name
			StrPtrAuto pszNext = PathFindNextComponent(pszName);
			if (!pszNext.IsNullOrEmpty)
			{
				hr = StringCchCopy(szNameComponent, pszName.Length - pszNext.ToString().Length, pszName);
			}
			else
			{
				hr = StringCchCopy(szNameComponent, szNameComponent.Length, pszName);
			}

			if (hr.Succeeded)
			{
				PathRemoveBackslash(szNameComponent);

				hr = GetIndexFromDisplayString(szNameComponent.ToString(), out var uIndex);
				if (hr.Succeeded)
				{
					var fIsFolder = ISFOLDERFROMINDEX((int)uIndex);
					hr = CreateChildID(szNameComponent.ToString(), (int)m_nLevel + 1, (int)uIndex, 3, fIsFolder, out var pidlCurrent);
					if (hr.Succeeded)
					{
						// If there are more components to parse, delegate to the child folder to handle the rest.
						if (!pszNext.IsNullOrEmpty)
						{
							// Bind to current item
							hr = BindToObject(pidlCurrent, pbc, typeof(IShellFolder).GUID, out var psf);
							if (hr.Succeeded)
							{
								hr = ((IShellFolder)psf!).ParseDisplayName(hwnd, pbc, pszNext!, out pchEaten, out var pidlNext, ref pdwAttributes);
								if (hr.Succeeded)
								{
									ppidl = ILCombine((IntPtr)pidlCurrent, (IntPtr)pidlNext);
								}
							}
						}
						else
						{
							// transfer ownership to caller
							ppidl = pidlCurrent;
						}
					}
				}
			}
		}

		return hr;
	}

	public HRESULT SetNameOf(HWND hwnd, PIDL pidl, string pszName, SHGDNF uFlags, out PIDL ppidlOut)
	{
		ppidlOut = PIDL.Null;
		return HRESULT.E_NOTIMPL;
	}

	internal static HRESULT CreateChildID(string pszName, int nLevel, int nSize, int nSides, bool fIsFolder, out PIDL ppidl)
	{
		var myObj = new FVITEMID
		{
			data = new ITEMDATA
			{
				nLevel = (byte)nLevel,
				nSize = (byte)nSize,
				nSides = (byte)nSides,
				fIsFolder = fIsFolder,
				szName = pszName
			}
		};
		// Allocate and zero the memory.
		using var ptr = SafeCoTaskMemHandle.CreateFromStructure(myObj);
		ppidl = new PIDL(ptr.GetBytes(0, ptr.Size));
		return HRESULT.S_OK;
	}

	internal static int StrCmp(string a, string b) => string.Compare(a, b, StringComparison.CurrentCulture);

	private static HRESULT ILCompareRelIDs(IShellFolder psfParent, PIDL pidl1, PIDL pidl2, SHCIDS lParam)
	{
		HRESULT hr;
		IntPtr pidlRel1 = ILNext((IntPtr)pidl1);
		IntPtr pidlRel2 = ILNext((IntPtr)pidl2);

		if (ILIsEmpty(pidlRel1))
		{
			if (ILIsEmpty(pidlRel2))
			{
				hr = ResultFromShort(0); // Both empty
			}
			else
			{
				hr = ResultFromShort(-1); // 1 is empty, 2 is not.
			}
		}
		else
		{
			if (ILIsEmpty(pidlRel2))
			{
				hr = ResultFromShort(1); // 2 is empty, 1 is not
			}
			else
			{
				// pidlRel1 and pidlRel2 point to something, so:
				// (1) Bind to the next level of the IShellFolder
				// (2) Call its CompareIDs to let it compare the rest of IDs.
				PIDL pidlNext = ILCloneFirst((IntPtr)pidl1); // pidl2 would work as well
				hr = pidlNext is not null ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
				if (hr.Succeeded)
				{
					// We do not want to pass the lParam is IShellFolder2 isn't supported.
					// Although it isn't important for this example it shoud be considered
					// if you are implementing this for other situations.
					try
					{
						IShellFolder psf2 = psfParent.BindToObject<IShellFolder2>(pidlNext!)!;
						// Also, the column mask will not be relevant and should never be passed.
						hr = psf2.CompareIDs((IntPtr)((uint)(lParam & ~SHCIDS_COLUMNMASK)), pidlRel1, pidlRel2);
					}
					catch (Exception ex) { hr = ex.HResult; }
				}
			}
		}
		return hr;
	}

	private static FVITEMID? IsValid(PIDL pidl)
	{
		FVITEMID? pidmine = default;
		if (pidl is not null)
		{
			pidmine = (FVITEMID)GCHandle.FromIntPtr((IntPtr)pidl).Target!;
			if (pidmine is null || !(pidmine.cb > 0 && MYOBJID == pidmine.MyObjID && pidmine.data.nLevel <= g_nMaxLevel))
			{
				pidmine = default;
			}
		}
		return pidmine;
	}

	private static HRESULT GetColumnDisplayName(PIDL pidl, in PROPERTYKEY pkey, out object? pv, StringBuilder? pszRet, uint cch)
	{
		pv = null;

		HRESULT hr = GetFolderness(pidl, out var fIsFolder);
		if (hr.Succeeded)
		{
			if (pkey == PROPERTYKEY.System.ItemNameDisplay)
			{
				hr = GetName(pidl, out var pszName);
				if (hr.Succeeded)
				{
					if (pszRet is null)
					{
						pv = pszName;
						hr = pv is not null ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
					}
					else
					{
						hr = StringCchCopy(pszRet, (int)cch, pszName);
					}
				}
			}
			else if (pkey == PKEY_Microsoft_SDKSample_AreaSize && !fIsFolder)
			{
				hr = GetSize(pidl, out var nSize);
				if (hr.Succeeded)
				{
					//This property is declared as "String" type. See: ExplorerDataProvider.propdesc
					var szFormattedSize = $"{nSize} Sq. Ft.";
					if (pszRet is not null)
					{
						hr = StringCchCopy(pszRet, cch, szFormattedSize);
					}
					else
					{
						pv = szFormattedSize;
						hr = pv is not null ? HRESULT.S_OK : HRESULT.E_OUTOFMEMORY;
					}
				}
			}
			else if (pkey == PKEY_Microsoft_SDKSample_NumberOfSides && !fIsFolder)
			{
				hr = GetSides(pidl, out var nSides);
				if (hr.Succeeded)
				{
					if (pszRet is not null)
					{
						hr = StringCchCopy(pszRet, cch, nSides.ToString());
					}
					else
					{
						pv = nSides;
					}
				}
			}
			else if (pkey == PKEY_Microsoft_SDKSample_DirectoryLevel)
			{
				hr = GetLevel(pidl, out var nLevel);
				if (hr.Succeeded)
				{
					if (pszRet is not null)
					{
						hr = StringCchCopy(pszRet, cch, nLevel.ToString());
					}
					else
					{
						pv = nLevel;
					}
				}
			}
			else
			{
				if (pszRet is not null)
				{
					pszRet.Clear();
				}
			}
		}
		return hr;
	}

	private static HRESULT GetFolderness(PIDL pidl, out bool pfIsFolder)
	{
		FVITEMID? pMyObj = IsValid(pidl);
		pfIsFolder = pMyObj?.data.fIsFolder ?? false;
		return pMyObj is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
	}

	private static HRESULT GetLevel(PIDL pidl, out int pLevel)
	{
		FVITEMID? pMyObj = IsValid(pidl);
		pLevel = pMyObj?.data.nLevel ?? 0;
		return pMyObj is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
	}

	private static HRESULT GetName(PIDL pidl, out string pszName)
	{
		FVITEMID? pMyObj = IsValid(pidl);
		pszName = pMyObj?.data.szName ?? string.Empty;
		return pMyObj is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
	}

	private static HRESULT GetSides(PIDL pidl, out int pSides)
	{
		FVITEMID? pMyObj = IsValid(pidl);
		pSides = pMyObj?.data.nSides ?? 0;
		return pMyObj is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
	}

	private static HRESULT GetSize(PIDL pidl, out int pSize)
	{
		FVITEMID? pMyObj = IsValid(pidl);
		pSize = pMyObj?.data.nSize ?? 0;
		return pMyObj is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
	}

	private static HRESULT ValidatePidl(PIDL pidl)
	{
		FVITEMID? pMyObj = IsValid(pidl);
		return pMyObj is not null ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
	}
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public class FVITEMID
{
	public static readonly ushort Size = (ushort)Marshal.SizeOf<FVITEMID>();

	public ushort cb = Size;
	public ushort MyObjID = CFolderViewImplFolder.MYOBJID;
	public ITEMDATA data;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct ITEMDATA
{
	public int nLevel;
	public int nSize;
	public int nSides;
	[MarshalAs(UnmanagedType.U1)]
	public bool fIsFolder;
	[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Kernel32.MAX_PATH)]
	public string szName;
}