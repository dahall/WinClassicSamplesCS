global using Vanara.PInvoke;
global using static Vanara.PInvoke.OleDb;
using System.Diagnostics.CodeAnalysis;

namespace PRSample;

public static partial class Program
{
	public const int MAX_COL_SIZE = 5000;
	public const int MAX_DISPLAY_SIZE = 20;
	public const int MAX_NAME_LEN = 256;
	public const int MAX_ROWS = 10;
	public const int MIN_DISPLAY_SIZE = 3;
	public const uint ROUNDUP_AMOUNT = 8;

	private static Flags g_dwFlags = 0;

	[Flags]
	public enum Flags : uint
	{
		//Connecting
		USE_PROMPTDATASOURCE = 0x0001,

		USE_ENUMERATOR = 0x0002,

		//Rowset
		USE_COMMAND = 0x0010,

		//Storage Objects
		USE_ISEQSTREAM = 0x0100,

		// Display options
		DISPLAY_METHODCALLS = 0x1000,

		DISPLAY_INSTRUCTIONS = 0x2000,
	}

	public static int Main(string[] args)
	{
		try
		{
			// Parse command line arguments, if any; this will update the value of g_dwFlags as appropriate for the arguments
			if (!myParseCommandLine(args))
				return 1;

			// Display instructions for the given command line arguments
			myDisplayInstructions();

			// Create the Data Source object using the OLE DB service components
			myCreateDataSource(out var pUnkDataSource);

			// Create a Session object from the Data Source object
			myCreateSession(pUnkDataSource, out var pUnkSession);

			// Create a Rowset object from the Session object, either directly from the Session or through a Command object
			myCreateRowset(pUnkSession, out var pUnkRowset);

			// Display the Rowset object data to the user
			myDisplayRowset(pUnkRowset!, default, out _);
		}
		catch { return 1; }
		finally
		{
			//if (pUnkRowset is not null)
			//	pUnkRowset.Release();
			//if (pUnkSession is not null)
			//	pUnkSession.Release();
			//if (pUnkDataSource is not null)
			//	pUnkDataSource.Release();
		}

		return 0;
	}

	//ROUNDUP on all platforms pointers must be aligned properly
	public static uint ROUNDUP(uint size, uint amount = ROUNDUP_AMOUNT) => amount == 0 ? size : (uint)(((ulong)size + amount - 1) & ~((ulong)amount - 1));

	/////////////////////////////////////////////////////////////////
	// myDisplayInstructions
	//
	// This function asks the user whether they would like instructions displayed for the application. If so, it displays the instructions
	// appropriate to the flags set in g_dwFlags.
	/////////////////////////////////////////////////////////////////
	private static void myDisplayInstructions()
	{
		char ch;

		// Display header and ask the user if they want instructions
		Console.Write("\nOLE DB Programmer's Reference Sample\n" +
			"====================================\n\n");
		Console.Write("Display instructions [Y or N]? ");
		do
		{
			ch = myGetChar();
		}
		while (ch is not 'y' and not 'n');
		Console.Write("{0}\n\n", ch);

		// No instructions, so we're done
		if (ch == 'n')
			return;

		// Display basic instructions
		Console.Write("This application is a simple OLE DB sample that will display\n" +
			"a rowset and will allow basic navigation of that rowset by\n" +
			"the user. The application will perform the following steps:\n\n");

		// Display DataSource creation instructions
		if ((g_dwFlags & Flags.USE_PROMPTDATASOURCE) != 0)
		{
			Console.Write(" - Creates a DataSource object through the Microsoft Data\n" +
				" Links UI. This allows the user to select the OLE DB\n" +
				" provider to use and to set connection properties.\n");
		}
		else
		{
			Console.Write(" - Creates a DataSource object through IDataInitialize::\n" +
				" CreateDBInstance, which allows the OLE DB service\n" +
				" component manager to add additional functionality to\n" +
				" the provider as requested. The user will select the\n" +
				" provider to use from a rowset obtained from the OLE DB\n" +
				" enumerator.\n");
		}

		// Display Session creation and table-selection instructions
		Console.Write(" - Creates a Session object from the DataSource object.\n");
		Console.Write(" - If the provider supports the schema rowset interface,\n" +
			" creates a TABLES schema rowset and allows the user to\n" +
			" select a table name from this rowset.\n");

		// Display Rowset creation instructions
		if ((g_dwFlags & Flags.USE_COMMAND) != 0)
		{
			Console.Write(" - Creates a Command object from the Session object and\n" +
				" allows the user to specify command text for this Command,\n" +
				" then executes the command to create the final rowset.\n");
		}
		else
		{
			Console.Write(" - Creates the final rowset over the table specified by the\n" +
				" user.\n");
		}

		Console.Write(" - Displays this rowset and allows the user to perform basic\n" +
			" navigation of that rowset.\n\n");

		// Wait for the user to press a key before continuing
		Console.Write("Press a key to continue...");
		myGetChar();
		Console.Write("\n\n");
	}

	/////////////////////////////////////////////////////////////////
	// myGetChar
	//
	// This function gets a character from the keyboard and converts it to lowercase before returning it.
	/////////////////////////////////////////////////////////////////
	private static char myGetChar()
	{
		// Get a character from the keyboard
		var ch = Console.ReadKey();

		// Re-read for the actual key value if necessary
		if (ch.KeyChar is (char)0 or (char)0xE0)
			ch = Console.ReadKey();

		return char.ToLower(ch.KeyChar);
	}

	/////////////////////////////////////////////////////////////////
	// myGetInputFromUser
	//
	// This function prompts the user with the contents of pwszFmt and any accompanying variable arguments, then gets a string as input from
	// the user. If the string is non-empty, it is copied into pwszInput and the function returns true; otherwise this function returns false.
	/////////////////////////////////////////////////////////////////
	private static bool myGetInputFromUser([NotNullWhen(true)] out string? pwszInput, string pwszFmt, params object?[] args)
	{
		// Create the string with variable arguments...
		string? wszBuffer = string.Format(pwszFmt, args);

		// Output the string...
		Console.Write(wszBuffer);

		// Now get the Input from the user...
		wszBuffer = Console.ReadLine();
		if (!string.IsNullOrEmpty(wszBuffer))
		{
			pwszInput = wszBuffer;
			return true;
		}

		pwszInput = null;
		return false;
	}

	/////////////////////////////////////////////////////////////////
	// myParseCommandLine
	//
	// This function parses the application's command line arguments and sets the appropriate bits in g_dwFlags. If an invalid argument is
	// encountered, a usage message is displayed and the function returns false; otherwise true is returned.
	/////////////////////////////////////////////////////////////////
	private static bool myParseCommandLine(string[] args)
	{
		// Set the locale for all C runtime functions
		//setlocale(LC_ALL, ".ACP");

		// Go through each command line argument and set the appropriate bits in g_dwFlags, depending on the chosen options
		for (var iArg = 0; iArg < args.Length; iArg++)
		{
			// Inspect the current argument string
			var psz = args[iArg];

			// Valid options begin with '-' or '/'
			if (psz[0] is '-' or '/')
			{
				// The next character is the option
				switch (char.ToLower(psz[1]))
				{
					case 'u':
						// Use the service components UI to prompt for and create the DataSource object; the enumerator is not used
						g_dwFlags |= Flags.USE_PROMPTDATASOURCE;
						g_dwFlags &= ~Flags.USE_ENUMERATOR;
						continue;
					case 'e':
						// Use the enumerator to select the provider, then use IDataInitialize to create the DataSource object; don't use the
						// UI to prompt for the DataSource
						g_dwFlags |= Flags.USE_ENUMERATOR;
						g_dwFlags &= ~Flags.USE_PROMPTDATASOURCE;
						continue;
					case 'c':
						// Use ICommand instead of IOpenRowset
						g_dwFlags |= Flags.USE_COMMAND;
						continue;
					case 'b':
						// Use ISequentialStream to fetch BLOB column data
						g_dwFlags |= Flags.USE_ISEQSTREAM;
						continue;
					case 'n':
						// Don't display method call strings as part of the extended error checking macro
						g_dwFlags &= ~Flags.DISPLAY_METHODCALLS;
						continue;
				}
			}

			// Invalid argument; show the usage flags to the user
			Console.Error.Write("Usage: prsample.exe [-u] [-e] [-c] [-b] [-n]\n\nWhere:\n\t" +
				"u = Use the Microsoft Data Links UI " +
				"to create the DataSource\n\t" +
				"e = Use the Enumerator and IDataInitialize " +
				"to create the DataSource\n\t" +
				"c = Use ICommand instead of IOpenRowset to create the Rowset\n\t" +
				"b = Use ISequentialStream for BLOB columns\n\t" +
				"n = Don't display method call strings\n");

			return false;
		}

		return true;
	}
}