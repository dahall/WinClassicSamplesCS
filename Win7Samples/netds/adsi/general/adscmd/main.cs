internal partial class Program
{
	//+---------------------------------------------------------------------------
	//
	// Function: main
	//
	//----------------------------------------------------------------------------
	internal static void Main(string[] args)
	{
		if (args.Length != 2)
		{
			PrintUsage();
			Environment.Exit(1);
		}

		if (string.Compare(args[0], "list", true) == 0)
		{
			//
			// Call the List helper function
			//
			Environment.Exit(DoList(args[1]));
		}
		else if (string.Compare(args[0], "dump", true) == 0)
		{
			//
			// Call the Dump helper function
			//
			Environment.Exit(DoDump(args[1]));
		}
		else
		{
			//
			// Unknown command
			//
			PrintUsage();
			Environment.Exit(1);
		}
	}
}