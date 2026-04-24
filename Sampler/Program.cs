using System.Windows.Forms;

namespace Sampler;


static class Program
{
	/// <summary>The main entry point for the application.</summary>
	static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.Run(new NuGetPackages());
	}
}