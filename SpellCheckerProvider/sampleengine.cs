using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheck;
using static Vanara.PInvoke.SpellCheckProvider;

class SampleEngine
{
	public const int MAX_WORD_SIZE = 128;
	public const int MAX_WORDLIST_SIZE = 10;
	public const int NUM_WORDLIST_TYPES = 4;
	static readonly char[] okletterset = { 'a', 'b', 'f' };
	private string languageTag;
	private byte[] optionValues = new byte[OptionsStore.MAX_LANGUAGE_OPTIONS];
	private readonly Dictionary<WORDLIST_TYPE, List<string>> wordlists = [];

	public SampleEngine(string languageTag)
	{
		this.languageTag = languageTag;
		InitializeOptionValuesToDefault();
	}

	public HRESULT AddWordToWordlist([In] WORDLIST_TYPE wordlistType, string word)
	{
		if (!wordlists.TryGetValue(wordlistType, out var words))
			wordlists.Add(wordlistType, words = []);
		words.Add(word);
		return HRESULT.S_OK;
	}

	public HRESULT ClearWordlist([In] WORDLIST_TYPE wordlistType)
	{

		HRESULT hr = Enum.IsDefined(wordlistType) ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
		if (hr.Succeeded && wordlists.TryGetValue(wordlistType, out var words))
			words.Clear();
		return hr;
	}

	public unsafe HRESULT FindFirstError(string text, out CSpellingError result)
	{
		HRESULT hr = HRESULT.S_OK;
		result = new(default);

		fixed (char* ptext = text)
		{
			char* currentPosition = ptext;
			while (currentPosition[0] != '\0')
			{
				char* wordStart = FindFirstNonDelimiter(currentPosition);
				if (*wordStart == '\0')
				{
					currentPosition = wordStart;
					break;
				}

				char* wordEnd = FindFirstDelimiter(wordStart);

				result.CorrectiveAction = CheckWord(wordStart, wordEnd);

				if (CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE == result.CorrectiveAction)
				{
					char* nextWordStart = FindFirstNonDelimiter(wordEnd);
					char* nextWordEnd = FindFirstDelimiter(nextWordStart);

					if (ShouldIgnoreRepeatedWord() && (0 == Kernel32.CompareStringOrdinal(new(wordStart), (int)(wordEnd - wordStart), new(nextWordStart), (int)(nextWordEnd - nextWordStart), false)))
					{
						result.CorrectiveAction = CORRECTIVE_ACTION.CORRECTIVE_ACTION_DELETE;
						result.StartIndex = (uint)(nextWordStart - ptext);
						result.Length = (uint)(nextWordEnd - nextWordStart);
						break;
					}

					currentPosition = wordEnd;
				}
				else
				{
					result.StartIndex = (uint)(wordStart - ptext);
					result.Length = (uint)(wordEnd - wordStart);
					if (CORRECTIVE_ACTION.CORRECTIVE_ACTION_REPLACE == result.CorrectiveAction)
					{
						hr = GetReplacement(wordStart, wordEnd, out var repl);
						if (hr.Succeeded)
							result.Replacement = repl;
					}

					break;
				}
			}

			if (*currentPosition == '\0')
			{
				hr = HRESULT.S_FALSE;
			}
		}
		return hr;
	}

	public HRESULT GetOptionValue(string optionId, out byte optionValue)
	{
		int optionIndex = OptionsStore.GetOptionIndexInLanguage(optionId);
		HRESULT hr = (optionIndex < 0) ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
		optionValue = hr.Succeeded ? optionValues[optionIndex] : (byte)0;
		return hr;
	}

	public HRESULT GetSuggestions(string word, int maxSuggestions, out IReadOnlyList<string> suggestionList)
	{
		HRESULT hr = HRESULT.S_OK;

		char okLetter = GetOkLetter();
		List<string> suggestions = [];
		unsafe
		{
			fixed (char* wordPtr = word)
			{
				for (char* p = wordPtr; hr.Succeeded && (*p != '\0'); ++p)
				{
					if (char.IsUpper(*p))
					{
						break;
					}

					if (suggestions.Count < maxSuggestions)
					{
						SizeT index = p - wordPtr;
						StringBuilder sb = new(word);
						sb[index] = okLetter;
						suggestions.Add(sb.ToString());
					}
				}
			}
		}
		suggestionList = suggestions;

		return hr;
	}

	public HRESULT SetOptionValue(string optionId, [In] byte optionValue)
	{
		int optionIndex = OptionsStore.GetOptionIndexInLanguage(optionId);
		HRESULT hr = (optionIndex < 0) ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
		if (hr.Succeeded)
		{
			optionValues[optionIndex] = optionValue;
		}
		return hr;
	}

	unsafe CORRECTIVE_ACTION CheckWord(char* begin, char* end)
	{
		CORRECTIVE_ACTION result = CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE;
		if (begin == end)
		{
			return result;
		}

		if (IsWordInWordlist(begin, end, WORDLIST_TYPE.WORDLIST_TYPE_IGNORE))
		{
			return CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE;
		}
		else if (IsWordInWordlist(begin, end, WORDLIST_TYPE.WORDLIST_TYPE_AUTOCORRECT))
		{
			return CORRECTIVE_ACTION.CORRECTIVE_ACTION_REPLACE;
		}
		else if (IsWordInWordlist(begin, end, WORDLIST_TYPE.WORDLIST_TYPE_EXCLUDE))
		{
			return CORRECTIVE_ACTION.CORRECTIVE_ACTION_DELETE;
		}
		else if (IsWordInWordlist(begin, end, WORDLIST_TYPE.WORDLIST_TYPE_ADD))
		{
			return CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE;
		}

		bool hasOkLetter = HasOkLetter(begin, end);
		bool hasUpper = HasUpperChar(begin, end);
		if (hasOkLetter && !hasUpper)
		{
			result = CORRECTIVE_ACTION.CORRECTIVE_ACTION_NONE;
		}
		else if (hasOkLetter && hasUpper)
		{
			result = CORRECTIVE_ACTION.CORRECTIVE_ACTION_REPLACE;
		}
		else if (!hasOkLetter)
		{
			result = CORRECTIVE_ACTION.CORRECTIVE_ACTION_GET_SUGGESTIONS; //if there's any uppercase, the suggestion list will be empty
		}

		return result;
	}

	unsafe char* FindFirstDelimiter(char* text)
	{
		char* p;
		for (p = text; *p != '\0'; ++p)
		{
			if (IsDelimiter(*p))
			{
				break;
			}
		}
		return p;
	}

	unsafe char* FindFirstNonDelimiter(char* text)
	{
		char* p;
		for (p = text; *p != '\0'; ++p)
		{
			if (!IsDelimiter(*p))
			{
				break;
			}
		}
		return p;
	}

	HRESULT GetLanguageTag([In] uint maxOutput, out string languageTag)
	{
		languageTag = this.languageTag;
		return HRESULT.S_OK;
	}

	char GetOkLetter() => okletterset[(optionValues[1] <= 2) ? optionValues[1] : 2];

	unsafe HRESULT GetReplacement([In] char* begin, char* end, out string replacement)
	{
		string? autoCorrectPair = GetWordIfInWordlist(begin, end, WORDLIST_TYPE.WORDLIST_TYPE_AUTOCORRECT);
		replacement = autoCorrectPair is null ? new string(begin, 0, (int)(end - begin)).ToLower() : autoCorrectPair.Substring((int)(end - begin + 1));
		return HRESULT.S_OK;
	}

	unsafe string? GetWordIfInWordlist([In] char* begin, char* end, [In] WORDLIST_TYPE wordlistType)
	{
		int comparisonSize = (int)(end - begin);
		return wordlists[wordlistType].FirstOrDefault(w => Util.CaseInsensitiveIsEqual(w, new(begin), comparisonSize, comparisonSize));
	}

	unsafe bool HasOkLetter([In] char* begin, char* end)
	{
		char okLetter = GetOkLetter();
		char upperOkLetter = char.ToUpper(okLetter);
		for (char* p = begin; p != end; ++p)
		{
			if ((okLetter == *p) || (upperOkLetter == *p))
			{
				return true;
			}
		}
		return false;
	}

	unsafe bool HasUpperChar([In] char* begin, char* end)
	{
		for (char* p = begin; p != end; ++p)
		{
			if (char.IsUpper(*p))
			{
				return true;
			}
		}
		return false;
	}

	void InitializeOptionValuesToDefault()
	{
		HRESULT hr = OptionsStore.GetOptionIdsForLanguage(languageTag, out var optionIds);
		for (int i = 0; hr.Succeeded && (i < optionIds.Length); ++i)
		{
			hr = OptionsStore.GetDefaultOptionValue(optionIds[i], out optionValues[i]);
		}
	}
	
	bool IsDelimiter(char c) => (c is ' ' or '\n' or '\t');

	unsafe bool IsWordInWordlist([In] char* begin, char* end, [In] WORDLIST_TYPE wordlistType) => (null != GetWordIfInWordlist(begin, end, wordlistType));

	bool ShouldIgnoreRepeatedWord() => (optionValues[0] == 0);
}