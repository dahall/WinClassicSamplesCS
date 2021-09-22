using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static explorerdataprovider.Guids;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.User32;

namespace explorerdataprovider
{
	internal delegate HRESULT PFN_ExplorerCommandExecute(IShellItemArray psiItemArray, object pv);

	[ComVisible(true)]
	public class CFolderViewCommandProvider : IExplorerCommandProvider
	{
		private static readonly FVCOMMANDITEM[] c_FVTasks = new FVCOMMANDITEM[]
		{
			// Icon reference should be replaced by absolute reference to own icon resource.
			new(GUID_Display,  Properties.Resources.IDS_DISPLAY,  Properties.Resources.IDS_DISPLAY_TT,  "shell32.dll,-42",    0,                  s_OnDisplay ),
			new(GUID_Settings, Properties.Resources.IDS_SETTINGS, Properties.Resources.IDS_SETTINGS_TT, "shell32.dll,-16710", EXPCMDFLAGS.ECF_HASSUBCOMMANDS, null, c_FVTaskSettings )
		};

		private static readonly FVCOMMANDITEM[] c_FVTaskSettings = new FVCOMMANDITEM[]
		{
			// Icon reference should be replaced by absolute reference to own icon resource.
			new(GUID_Setting1, Properties.Resources.IDS_SETTING1, Properties.Resources.IDS_SETTING1_TT, "shell32.dll,-16710", 0, s_OnSetting1),
			new(GUID_Setting2, Properties.Resources.IDS_SETTING2, Properties.Resources.IDS_SETTING2_TT, "shell32.dll,-16710", 0, s_OnSetting2),
			new(GUID_Setting3, Properties.Resources.IDS_SETTING3, Properties.Resources.IDS_SETTING3_TT, "shell32.dll,-16710", 0, s_OnSetting3)
		};

		public HRESULT GetCommand(in Guid rguidCommandId, in Guid riid, out object ppv)
		{
			ppv = null; return HRESULT.E_NOTIMPL;
		}

		public HRESULT GetCommands(object punkSite, in Guid riid, out object ppv)
		{
			CFolderViewCommandEnumerator pFVCommandEnum = new(c_FVTasks);
			return ShellUtil.QueryInterface(pFVCommandEnum, riid, out ppv);
		}

		private static HRESULT s_OnDisplay(IShellItemArray psiItemArray, object pv) => psiItemArray.DisplayItem();

		private static HRESULT s_OnSetting1(IShellItemArray psiItemArray, object pv)
		{
			MessageBox(default, Properties.Resources.IDS_SETTING1, Properties.Resources.IDS_SETTING1, MB_FLAGS.MB_OK);
			return HRESULT.S_OK;
		}

		private static HRESULT s_OnSetting2(IShellItemArray psiItemArray, object pv)
		{
			MessageBox(default, Properties.Resources.IDS_SETTING2, Properties.Resources.IDS_SETTING2, MB_FLAGS.MB_OK);
			return HRESULT.S_OK;
		}

		private static HRESULT s_OnSetting3(IShellItemArray psiItemArray, object pv)
		{
			MessageBox(default, Properties.Resources.IDS_SETTING3, Properties.Resources.IDS_SETTING3, MB_FLAGS.MB_OK);
			return HRESULT.S_OK;
		}
	}

	internal class CFolderViewCommand : IExplorerCommand
	{
		private readonly FVCOMMANDITEM pfvci;

		public CFolderViewCommand(FVCOMMANDITEM pfvci) => this.pfvci = pfvci;

		public HRESULT EnumSubCommands(out IEnumExplorerCommand ppEnum)
		{
			ppEnum = new CFolderViewCommandEnumerator(pfvci.pFVCIChildren); return HRESULT.S_OK;
		}

		public HRESULT GetCanonicalName(out Guid pguidCommandName)
		{
			pguidCommandName = pfvci.pguidCanonicalName; return HRESULT.S_OK;
		}

		public HRESULT GetFlags(out EXPCMDFLAGS pFlags)
		{
			pFlags = pfvci.ecFlags; return HRESULT.S_OK;
		}

		public HRESULT GetIcon(IShellItemArray psiItemArray, out string ppszIcon)
		{
			ppszIcon = pfvci.pszIcon; return HRESULT.S_OK;
		}

		public HRESULT GetState(IShellItemArray psiItemArray, bool fOkToBeSlow, out EXPCMDSTATE pCmdState)
		{
			pCmdState = pfvci.pguidCanonicalName != GUID_Display || (psiItemArray?.GetCount() ?? 0) > 0 ? EXPCMDSTATE.ECS_ENABLED : EXPCMDSTATE.ECS_DISABLED;
			return HRESULT.S_OK;
		}

		public HRESULT GetTitle(IShellItemArray psiItemArray, out string ppszName)
		{
			ppszName = pfvci.dwTitleID; return HRESULT.S_OK;
		}

		public HRESULT GetToolTip(IShellItemArray psiItemArray, out string ppszInfotip)
		{
			ppszInfotip = pfvci.dwToolTipID; return HRESULT.S_OK;
		}

		public HRESULT Invoke(IShellItemArray psiItemArray, IBindCtx pbc) => pfvci.pfnInvoke?.Invoke(psiItemArray, pbc) ?? HRESULT.S_OK;
	}

	internal class CFolderViewCommandEnumerator : IEnumExplorerCommand
	{
		private readonly FVCOMMANDITEM[] apfvci;
		private uint uCurrent = 0;

		public CFolderViewCommandEnumerator(FVCOMMANDITEM[] c_FVTasks) => apfvci = c_FVTasks;

		public IEnumExplorerCommand Clone() => throw new NotImplementedException();

		public HRESULT Next(uint celt, IExplorerCommand[] pUICommand, out uint pceltFetched)
		{
			HRESULT hr = HRESULT.S_FALSE;
			pceltFetched = 0;
			if (uCurrent <= pUICommand.Length)
			{
				uint uIndex = 0;
				HRESULT hrLocal = HRESULT.S_OK;
				while (uIndex < celt && uCurrent < pUICommand.Length && hrLocal.Succeeded)
				{
					pUICommand[uIndex] = new CFolderViewCommand(apfvci[uCurrent]);
					uIndex++;
					uCurrent++;
				}

				pceltFetched = uIndex;

				if (uIndex == celt)
				{
					hr = HRESULT.S_OK;
				}
			}
			return hr;
		}

		public void Reset() => uCurrent = 0;

		public HRESULT Skip(uint celt)
		{
			uCurrent += celt;

			HRESULT hr = HRESULT.S_OK;
			if (uCurrent > apfvci.Length)
			{
				uCurrent = (uint)apfvci.Length;
				hr = HRESULT.S_FALSE;
			}
			return hr;
		}
	}

	internal class FVCOMMANDITEM
	{
		public string dwTitleID;
		public string dwToolTipID;
		public EXPCMDFLAGS ecFlags;
		public PFN_ExplorerCommandExecute pfnInvoke;
		public FVCOMMANDITEM[] pFVCIChildren;
		public Guid pguidCanonicalName;
		public string pszIcon;

		public FVCOMMANDITEM(Guid guid, string title, string tooltip, string icon, EXPCMDFLAGS flags, PFN_ExplorerCommandExecute invoke, FVCOMMANDITEM[] children = null)
		{
			pguidCanonicalName = guid;
			dwTitleID = title;
			dwToolTipID = tooltip;
			pszIcon = icon;
			pfnInvoke = invoke;
			pFVCIChildren = children;
		}
	}
}