using Vanara.PInvoke;
using static Vanara.PInvoke.FunDisc;

namespace fdsample
{
	internal class Program
	{
		private static void Main()
		{
			Console.Write("This sample will list all PnP devices on your system.\n");
			Console.Write("Then, it will demonstrate Function Discovery's ADD and\n");
			Console.Write("REMOVE notifications.\nPress any key to continue.\n");
			//Console.ReadKey();

			// Instantiate the Function Discovery wrapper object
			var pFD = new CMyFDHelper();

			// Enumerate all PnP Devices
			pFD.ListFunctionInstances(FCTN_CATEGORY_PNP);

			// Wait for device to be added
			Console.Write("Waiting 30 seconds for you to plug in a PnP device.\n");
			Console.Write("For example, a USB mouse.\n");
			var hr = pFD.WaitForChange(30000, FCTN_CATEGORY_PNP, QueryUpdateAction.QUA_ADD);

			if (hr == HRESULT.S_OK)
			{
				// Wait for device to be removed
				Console.Write("Waiting 30 seconds for you to remove a device...\n");

				pFD.WaitForChange(30000, FCTN_CATEGORY_PNP, QueryUpdateAction.QUA_REMOVE);
			}
			else if (HRESULT.RPC_S_CALLPENDING == hr)
			{
				Console.Write("No device was added.  ");
				Console.Write("Try running the sample again.");
			}
		}
	}
}