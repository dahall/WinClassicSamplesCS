using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.OleAut32;
using static Vanara.PInvoke.Shell32;

namespace ExecInExplorer
{
	internal static class ExecInExplorer
	{
		private static readonly Guid SID_STopLevelBrowser = new Guid(0x4C96BE40, 0x915C, 0x11CF, 0x99, 0xD3, 0x00, 0xAA, 0x00, 0x4A, 0xE8, 0x37);

		// From a shell view object gets its automation interface and from that gets the shell application object that implements
		// IShellDispatch2 and related interfaces.
		private static IShellDispatch2 GetShellDispatchFromView(IShellView psv)
		{
			var io = psv.GetItemObject(SVGIO.SVGIO_BACKGROUND, typeof(IDispatch).GUID);
			var psfvd = (IShellFolderViewDual)io;
			return (IShellDispatch2)psfvd.Application;
		}

		// use the shell view for the desktop using the shell windows automation to find the desktop web browser and then grabs its view
		//
		// returns: IShellView, IFolderView and related interfaces
		private static T GetShellViewForDesktop<T>() where T : class
		{
			var psw = new IShellWindows();
			var pdisp = psw.FindWindowSW(0 /* CSIDL_Desktop */, default, ShellWindowTypeConstants.SWC_DESKTOP, out _, ShellWindowFindWindowOptions.SWFO_NEEDDISPATCH);
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
			var psd = GetShellDispatchFromView(psv);
			var empty = new object();
			psd.ShellExecute(pszFile, empty, empty, empty, empty);
		}
	}
}