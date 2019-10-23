using System;
using System.Windows.Forms;

using static Vanara.PInvoke.Shell32;

using Vanara.PInvoke;

namespace ExecInExplorer
{
	internal static class ExecInExplorer
	{
		private static readonly Guid IID_IDispatch = new Guid("{00020400-0000-0000-C000-000000000046}");
		private static readonly Guid IID_IShellDispatch2 = new Guid("A4C6892C-3BA9-11d2-9DEA-00C04FB16162");
		private static readonly Guid SID_STopLevelBrowser = new Guid(0x4C96BE40, 0x915C, 0x11CF, 0x99, 0xD3, 0x00, 0xAA, 0x00, 0x4A, 0xE8, 0x37);

		// From a shell view object gets its automation interface and from that gets the shell application object that implements
		// IShellDispatch2 and related interfaces.
		private static object GetShellDispatchFromView(IShellView psv)
		{
			var psfvd = (IShellFolderViewDual)psv.GetItemObject(SVGIO.SVGIO_BACKGROUND, IID_IDispatch);
			return psfvd.Application;
		}

		// use the shell view for the desktop using the shell windows automation to find the desktop web browser and then grabs its view
		//
		// returns: IShellView, IFolderView and related interfaces
		private static T GetShellViewForDesktop<T>() where T : class
		{
			var psw = new IShellWindows();
			object vEmpty = null;
			var pdisp = psw.FindWindowSW(ref vEmpty, default, ShellWindowTypeConstants.SWC_DESKTOP, out var hwnd, ShellWindowFindWindowOptions.SWFO_NEEDDISPATCH);
			var psb = ShlwApi.IUnknown_QueryService<IShellBrowser>(pdisp, SID_STopLevelBrowser);
			return (T)psb.QueryActiveShellView();
		}

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			ShellExecInExplorerProcess("http://www.msn.com");
		}

		private static void ShellExecInExplorerProcess(string pszFile)
		{
			var psv = GetShellViewForDesktop<IShellView>();
			dynamic psd = GetShellDispatchFromView(psv);
			psd.ShellExecuteW(pszFile, null, null, null, null);
		}
	}
}
