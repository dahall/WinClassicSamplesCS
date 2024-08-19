using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheck;

namespace SpellCheckerClient;
internal static partial class Program
{
	static void PrintAvailableLanguages([In] ISpellCheckerFactory spellCheckerFactory)
	{
		IEnumString enumLanguages = spellCheckerFactory.SupportedLanguages;
		try
		{
			Console.Write("Available languages:\n");
			PrintEnumString(enumLanguages, null);
		}
		finally
		{
			Marshal.ReleaseComObject(enumLanguages);
		}
	}

	static void PrintSpellingError([In] ISpellChecker spellChecker, string text, [In] ISpellingError spellingError)
	{
		CORRECTIVE_ACTION correctiveAction = CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE;

		var startIndex = spellingError.StartIndex;
		var errorLength = spellingError.Length;
		correctiveAction = spellingError.CorrectiveAction;
		string misspelled = text.Substring((int)startIndex, (int)errorLength);
		Console.Write($"{misspelled} [{startIndex}, {startIndex + errorLength - 1}] is misspelled. ");

		if (CORRECTIVE_ACTION.CORRECTIVE_ACTION_GET_SUGGESTIONS == correctiveAction)
		{
			Console.Write("Suggestions:\n");
			IEnumString enumSuggestions = spellChecker.Suggest(misspelled);
			try
			{
				PrintEnumString(enumSuggestions, "\t");
				Console.Write("\n");
			}
			finally
			{
				Marshal.ReleaseComObject(enumSuggestions);
			}
		}
		else if (CORRECTIVE_ACTION.CORRECTIVE_ACTION_REPLACE == correctiveAction)
		{
			Console.Write("It should be autocorrected to:\n");
			string replacement = spellingError.Replacement;
			Console.Write("\t{0}\n\n", replacement);
		}
		else if (CORRECTIVE_ACTION.CORRECTIVE_ACTION_DELETE == correctiveAction)
		{
			Console.Write("It should be deleted.\n\n");
		}
		else
		{
			Console.Write("Invalid corrective action.\n\n");
		}
	}

	static void PrintSpellingErrors([In] ISpellChecker spellChecker, string text, [In, Out] IEnumSpellingError enumSpellingError)
	{
		HRESULT hr = HRESULT.S_OK;
		int numErrors = 0;
		foreach (var spellingError in enumSpellingError.Enum())
		{
			++numErrors;
			PrintSpellingError(spellChecker, text, spellingError);
			Marshal.ReleaseComObject(spellingError);
		}

		if (0 == numErrors)
		{
			Console.Write("No errors.\n\n");
		}
	}

	static void PrintLanguage([In] ISpellChecker spellChecker) => Console.Write($"Language: {spellChecker.LanguageTag}\n\n");

	static void PrintSpellCheckerIdAndName([In] ISpellChecker spellChecker) => Console.Write($"Provider: {spellChecker.Id} ({spellChecker.LocalizedName})\n\n");

	static void PrintOptionHeading([In] IOptionDescription optionDescription)
	{
		if (!string.IsNullOrEmpty(optionDescription.Heading))
			Console.Write("\t{0}\n", optionDescription.Heading);
	}

	static void PrintOptionDescription([In] IOptionDescription optionDescription)
	{
		if (!string.IsNullOrEmpty(optionDescription.Description))
			Console.Write("\t{0}\n", optionDescription.Description);
	}

	static void PrintSingleLabel([In, Out] IEnumString enumString, byte optionValue)
	{		
		string? label = enumString.Enum().FirstOrDefault();
		if (label is not null)
			Console.Write($"\t{label} (current {((optionValue == 1) ? "on" : "off")})\n");
	}

	static void PrintMultipleLabels([In, Out] IEnumString enumString, byte optionValue)
	{
		byte i = 0;
		foreach (var label in enumString.Enum())
		{
			string currentText = optionValue == i ? "(current)" : "";
			Console.Write($"\t[{i++}] {label} {currentText}\n");
		}
	}

	static void PrintOptionLabels([In] ISpellChecker spellChecker, string optionId, [In] IOptionDescription optionDescription)
	{
		byte optionValue = spellChecker.GetOptionValue(optionId);
		IEnumString enumLabels = optionDescription.Labels;
		try
		{
			if (HasSingleString(enumLabels))
				PrintSingleLabel(enumLabels, optionValue);
			else
				PrintMultipleLabels(enumLabels, optionValue);
		}
		finally
		{
			Marshal.ReleaseComObject(enumLabels);
		}
	}

	static void PrintOption([In] ISpellChecker spellChecker, string optionId)
	{
		Console.Write($"\t{optionId}\n");

		IOptionDescription optionDescription = spellChecker.GetOptionDescription(optionId);
		try
		{
			PrintOptionHeading(optionDescription);
			PrintOptionDescription(optionDescription);
			PrintOptionLabels(spellChecker, optionId, optionDescription);
		}
		finally
		{
			Marshal.ReleaseComObject(optionDescription);
		}
	}

	static void PrintSpellingOptions([In] ISpellChecker spellChecker)
	{
		Console.Write("Options:\n");
		IEnumString enumOptionIds = spellChecker.OptionIds;
		try
		{
			foreach (var optionId in enumOptionIds.Enum())
			{
				PrintOption(spellChecker, optionId);
				Console.WriteLine();
			}
			Console.WriteLine();
		}
		finally
		{
			Marshal.ReleaseComObject(enumOptionIds);
		}
	}

	static void PrintInfoAndOptions([In] ISpellChecker spellChecker)
	{
		PrintLanguage(spellChecker);
		PrintSpellCheckerIdAndName(spellChecker);
		PrintSpellingOptions(spellChecker);
	}
}