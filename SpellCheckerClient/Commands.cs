using System.Text.RegularExpressions;
using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheck;

namespace SpellCheckerClient;

internal static partial class Program
{
	private static void AddCommand([In] ISpellChecker spellChecker, string buffer)
	{
		if (ReadSingleWord(buffer, 256, out var word))
			spellChecker.Add(word);
		else
			throw new InvalidOperationException("Unable to parse input.");
	}

	private static void AutoCorrectCommand([In] ISpellChecker spellChecker, string buffer)
	{
		if (ReadTwoWords(buffer, 256, out var from, 256, out var to))
			spellChecker.AutoCorrect(from, to);
		else
			throw new InvalidOperationException("Unable to parse input.");
	}

	private static void CheckAsYouTypeCommand([In] ISpellChecker spellChecker, string buffer)
	{
		if (ReadText(buffer, 4096, out var text))
		{
			var enumSpellingError = spellChecker.ComprehensiveCheck(text);
			try { PrintSpellingErrors(spellChecker, text, enumSpellingError); }
			finally { Marshal.ReleaseComObject(enumSpellingError); }
		}
		else
			throw new InvalidOperationException("Unable to parse input.");
	}

	private static void CheckCommand([In] ISpellChecker spellChecker, string buffer)
	{
		if (ReadText(buffer, 1024, out var text))
		{
			var enumSpellingError = spellChecker.Check(text);
			try { PrintSpellingErrors(spellChecker, text, enumSpellingError); }
			finally { Marshal.ReleaseComObject(enumSpellingError); }
		}
		else
			throw new InvalidOperationException("Unable to parse input.");
	}

	private static void IgnoreCommand([In] ISpellChecker spellChecker, string buffer)
	{
		if (ReadSingleWord(buffer, 256, out var word))
			spellChecker.Ignore(word);
		else
			throw new InvalidOperationException("Unable to parse input.");
	}

	private static bool ReadInteger(string buffer, out int integer)
	{
		var m = Regex.Match(buffer, $@"\b(\d{{1,10}})\b");
		if (2 != m.Groups.Count) { integer = 0; return false; }
		return int.TryParse(m.Groups[1].Value, out integer);
	}

	private static bool ReadSingleWord(string buffer, SizeT maxWordSize, out string word)
	{
		var m = Regex.Match(buffer, $@"\b(\w{{1,{maxWordSize}}})\b");
		if (2 != m.Groups.Count) { word = ""; return false; }
		word = m.Groups[1].Value;
		return true;
	}

	private static bool ReadText(string buffer, SizeT maxTextSize, out string text)
	{
		var m = Regex.Match(buffer, $@" ([^\n]{{1,{maxTextSize}}})");
		if (2 != m.Groups.Count) { text = ""; return false; }
		text = m.Groups[1].Value;
		return true;
	}

	private static bool ReadTwoWords(string buffer, SizeT maxFirstSize, out string first, SizeT maxSecondSize, out string second)
	{
		var m = Regex.Match(buffer, $@"\b(\w{{1,{maxFirstSize}}})\s+(\w{{1,{maxSecondSize}}})\b");
		if (3 != m.Groups.Count) { first = second = ""; return false; }
		first = m.Groups[1].Value;
		second = m.Groups[2].Value;
		return true;
	}
}