using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Util;
using static Vanara.PInvoke.SpellCheck;
using static Vanara.PInvoke.SpellCheckProvider;

internal class CSampleSpellCheckProvider(string languageTag) : ISpellCheckProvider
{
	private const string localizedName = "Sample Spell Checker";
	private const string spellerId = "samplespell";
	private readonly SampleEngine engine = new(languageTag);

	public HRESULT EngineCheck(string text, out CSpellingError spellingError) => engine.FindFirstError(text, out spellingError);

	HRESULT ISpellCheckProvider.Check(string text, out IEnumSpellingError? value)
	{
		value = new CEnumSpellingError(text, this);
		return HRESULT.S_OK;
	}

	HRESULT ISpellCheckProvider.get_Id(out string? value) => Set(spellerId, out value);

	HRESULT ISpellCheckProvider.get_LanguageTag(out string? value) => Set(languageTag, out value);

	HRESULT ISpellCheckProvider.get_LocalizedName(out string? value) => Set(localizedName, out value);

	HRESULT ISpellCheckProvider.get_OptionIds(out IEnumString? value)
	{
		value = null;
		HRESULT hr = OptionsStore.GetOptionIdsForLanguage(languageTag, out var optionIds);
		if (hr.Succeeded)
		{
			hr = Util.CreateEnumString(optionIds, out value);
		}
		return hr;
	}

	HRESULT ISpellCheckProvider.GetOptionDescription(string optionId, out IOptionDescription? value)
	{
		value = new COptionDescription(optionId);
		return HRESULT.S_OK;
	}

	HRESULT ISpellCheckProvider.GetOptionValue(string optionId, out byte value) => engine.GetOptionValue(optionId, out value);

	HRESULT ISpellCheckProvider.InitializeWordlist(WORDLIST_TYPE wordlistType, IEnumString? words)
	{
		WORDLIST_TYPE type = wordlistType;
		engine.ClearWordlist(type);

		HRESULT hr = HRESULT.S_OK;
		while (HRESULT.S_OK == hr && words != null)
		{
			string[] lpWord = new string[1];
			hr = words.Next(1, lpWord, default);
			if (HRESULT.S_OK == hr)
			{
				hr = engine.AddWordToWordlist(type, lpWord[0]);
			}
		}

		return hr;
	}

	HRESULT ISpellCheckProvider.SetOptionValue(string optionId, byte value) => engine.SetOptionValue(optionId, value);

	HRESULT ISpellCheckProvider.Suggest(string word, out IEnumString? value)
	{
		value = null;
		HRESULT hr = engine.GetSuggestions(word, 5, out var suggestions);
		if (hr.Succeeded)
		{
			hr = CreateEnumString(suggestions, out value);
		}
		return hr;
	}

	private static HRESULT Set<T>(T languageTag, out T? value) { value = languageTag; return HRESULT.S_OK; }
}