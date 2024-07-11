using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

class Program
{
	[STAThread]
	public static void Main()
	{
		using Vanara.Windows.Forms.ComCtl32v6Context ccc = new();

		new CAmbientLightAwareDlg().DoModal();
	}
}