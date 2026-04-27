using Vanara.InteropServices;
using Vanara.PInvoke;
using static Common;
using static Vanara.PInvoke.Kernel32;

internal partial class Program
{
	const string pwd = "Pa$$w0rd";
	static readonly ManualResetEventSlim svrExit = new(false);
	static readonly Dictionary<string, ACCESS_FUND> ExTypes = new(StringComparer.InvariantCultureIgnoreCase)
	{
		["Personal"] = ACCESS_FUND.ACCESS_FUND_PERSONAL,
		["Corporate"] = ACCESS_FUND.ACCESS_FUND_CORPORATE,
		["Transfer"] = ACCESS_FUND.ACCESS_FUND_TRANSFER,
	};

	private static void Main()
	{
		string szServerName = "\\\\.";
		EX_BUF exBuf = default;

		Console.Write("\nRun client as 1) Joe (Employee), 2) Martha (Manager), 3) Bob (VP): ");
		var key = Console.ReadKey();
		var userId = key.KeyChar switch 
		{
			'1' => "Joe",
			'2' => "Martha",
			'3' => "Bob",
			_ => null
		};

		Console.Write("\nRun server as 1) Joe (Employee), 2) Martha (Manager), 3) Bob (VP): ");
		key = Console.ReadKey();
		SafeLPWSTR serverId = key.KeyChar switch 
		{
			'1' => "Joe",
			'2' => "Martha",
			'3' => "Bob",
			_ => ""
		};
		Console.WriteLine();

		if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(serverId))
		{
			Console.WriteLine("Invalid selection. Exiting.");
			return;
		}

		CreateLocalAcct(userId, pwd);
		CreateLocalAcct(serverId!, pwd);

		if (!Impersonate(userId, pwd, out var hClientToken))
			HandleError(GetLastError(), "Impersonate", true, true);

		using var svr = SafeHTHREAD.Create(AuthzSvr, serverId, out _);

		Usage();
		string? input;
		while (!string.IsNullOrEmpty(input = Console.ReadLine()))
		{
			var args = input.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			try
			{
				//
				// Verify expnese input
				//
				if (args.Length < 2 || !ExTypes.TryGetValue(args[0], out exBuf.dwType) || !int.TryParse(args[1], out int amt) || amt == 0)
				{
					Usage();
					continue;
				}

				Console.Write($"expense: {ExNames[(int)exBuf.dwType]} Ammount: {exBuf.dwAmmount}\n");

				var szPipeName = $"{szServerName}\\pipe\\AuthzSamplePipe";

				// Wait for an instance of the pipe
				if (!WaitNamedPipe(szPipeName, NMPWAIT_WAIT_FOREVER))
					HandleError(GetLastError(), "WaitNamedPipe", true, true);

				// Connect to pipe
				using var hPipe = CreateFile(szPipeName, FileAccess.GENERIC_READ | FileAccess.GENERIC_WRITE, 0,
					default, CreationOption.OPEN_EXISTING, 0, default);
				if (hPipe.IsInvalid)
					HandleError(GetLastError(), "CreateFile", true, true);

				// Send off request
				WriteToPipe(hPipe, exBuf);

				// wait till server responds with one uint
				if (ReadFromPipe(hPipe, out uint dwResponse) == 0)
				{
					Console.Write("Error reading form Svr\n");
					return;
				}

				switch (dwResponse)
				{
					case EXPENSE_APPROVED:
						Console.Write("Expense Approved.\n");
						break;

					case Win32Error.ERROR_ACCESS_DENIED:
						Console.Write("Expense denied: Access denied.\n");
						break;

					case ERROR_INSUFFICIENT_FUNDS:
						Console.Write("Expense denied: Insufficient funds.\n");
						break;

					default:
						Console.Write("Expense failed: unexpected error.\n");
						break;
				}
			}
			catch { }
		}
		svrExit.Set();
		svr.Wait();

		static void Usage() => Console.Write("Usage: <Personal|Corporate|Transfer> <Ammount (cents)>\n");
	}
}