using static Vanara.PInvoke.MSCTF;

namespace tsfapp
{
	internal static class Program
	{
		internal static readonly Lazy<ITfThreadMgr> g_pThreadMgr = new(() => { TF_CreateThreadMgr(out var m).ThrowIfFailed(); return m; });
		internal static Lazy<uint> g_clientId = new(() => g_pThreadMgr.Value.Activate());

		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		private static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new CTSFMainWnd());
			if (g_pThreadMgr.IsValueCreated) g_pThreadMgr.Value.Deactivate();
		}
	}
}