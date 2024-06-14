using System;
using System.Collections.Generic;
using Vanara.PInvoke;
using static Vanara.PInvoke.Shell32;

namespace ImageFactorySample;

internal class Program
{
	// The possbile sizes for the image that is requested
	private static readonly Dictionary<string, int> sizeTable = new() { { "small", 16 }, { "medium", 48 }, { "large", 96 }, { "extralarge", 256 } };

	private static int LookUp(string pwszArg) => sizeTable.TryGetValue(pwszArg, out int val) ? val : 0;

	private static void DisplayUsage()
	{
		Console.Write("Usage:\n");
		Console.Write("IShellItemImageFactory.exe <size> <Absolute Path to file>\n");
		Console.Write("size - small, medium, large, extralarge\n");
		Console.Write("e.g. ImageFactorySample.exe medium c:\\HelloWorld.jpg \n");
	}

	[STAThread]
	private static void Main(string[] args)
	{
		if (args.Length != 2)
		{
			DisplayUsage();
		}
		else
		{
			int nSize = LookUp(args[0]);
			if (nSize == 0)
			{
				DisplayUsage();
			}
			else
			{
				try
				{
					// Getting the IShellItemImageFactory interface pointer for the file.
					IShellItemImageFactory pImageFactory = SHCreateItemFromParsingName<IShellItemImageFactory>(args[1])!;
					SIZE size = new(nSize, nSize);

					//sz - Size of the image, SIIGBF_BIGGERSIZEOK - GetImage will stretch down the bitmap (preserving aspect ratio)
					pImageFactory.GetImage(size, SIIGBF.SIIGBF_BIGGERSIZEOK, out var hbmp).ThrowIfFailed();
					var dlg = new ImageFactoryDlg();
					dlg.IDC_STATIC1.Image = hbmp.ToBitmap();
					dlg.ShowDialog();
				}
				catch (Exception ex)
				{
					System.Windows.Forms.MessageBox.Show(ex.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
				}
			}
		}
	}
}