using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheck;

namespace SpellCheckerClient;

internal static partial class Program
{
	static void RunCommandLoop([In] ISpellChecker spellChecker)
	{
		Console.Write("Commands:\n");
		Console.Write("quit - Quit\n");
		Console.Write("add <word> - Add word\n");
		Console.Write("ac <word> <word> - Add autocorrect pair\n");
		Console.Write("ign <word> - Ignore word\n");
		Console.Write("chkb <text> - Check text (batch - pasted text or file open)\n");
		Console.Write("chk <text> - Check text (as you type)\n");

		while (true)
		{
			Console.Write("> ");
			string? line = Console.ReadLine();
			if (line is not null)
			{
				if (!ReadSingleWord(line, 5, out var command))
					command = "";
				var buffer = line.Substring(command.Length).TrimStart();
				switch (command)
				{
					case "quit":
						return;
					case "add":
						AddCommand(spellChecker, buffer);
						break;
					case "ac":
						AutoCorrectCommand(spellChecker, buffer);
						break;
					case "ign":
						IgnoreCommand(spellChecker, buffer);
						break;
					case "chkb":
						AddCommand(spellChecker, buffer);
						break;
					case "chk":
						CheckCommand(spellChecker, buffer);
						break;
					default:
						Console.Write("Invalid command\n");
						break;
				}
			}
		}
	}

	static void RunSpellCheckingLoop([In] ISpellChecker spellChecker)
	{
		PrintInfoAndOptions(spellChecker);
		OnSpellCheckerChanged.StartListeningToChangeEvents(spellChecker, out var eventListener);
		try
		{
			RunCommandLoop(spellChecker);
		}
		finally
		{
			OnSpellCheckerChanged.StopListeningToChangeEvents(spellChecker, eventListener);
		}
	}

	static void StartSpellCheckingSession([In] ISpellCheckerFactory spellCheckerFactory, string languageTag)
	{
		bool isSupported = spellCheckerFactory.IsSupported(languageTag);
		if (!isSupported)
		{
			Console.Write("Language tag {0} is not supported.\n", languageTag);
		}
		else
		{
			ISpellChecker spellChecker = spellCheckerFactory.CreateSpellChecker(languageTag);
			try
			{
				RunSpellCheckingLoop(spellChecker);
			}
			finally
			{
				Marshal.ReleaseComObject(spellChecker);
			}
		}
	}

	[MTAThread]
	static void Main(string[] args)
	{
		ISpellCheckerFactory spellCheckerFactory = new();
		try
		{
			if (args.Length == 0)
			{
				PrintAvailableLanguages(spellCheckerFactory);
			}
			else if (args.Length == 1)
			{
				string languageTag = args[0];
				StartSpellCheckingSession(spellCheckerFactory, languageTag);
			}
			else
			{
				Console.Write("Usage:\n");
				Console.Write("\"SampleClient\" - lists all the available languages\n");
				Console.Write("\"SampleClient <language tag>\" - initiates an interactive spell checking session in the language, if supported\n");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error:\n{ex}");
		}
		finally
		{
			Marshal.ReleaseComObject(spellCheckerFactory);
		}
	}
}