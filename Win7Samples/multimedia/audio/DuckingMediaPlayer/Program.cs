namespace DuckingMediaPlayer;

internal static class Program
{
	/// <summary>The main entry point for the application.</summary>
	[MTAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		if (Environment.OSVersion.Version < new Version(6, 1))
			MessageBox.Show("This sample requires Windows 7 or later", "Incompatible OS Version", MessageBoxButtons.OK);
		else
			Application.Run(new DuckingMediaPlayerSample());
	}
}