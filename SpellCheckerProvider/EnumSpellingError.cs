using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheck;

class CEnumSpellingError(string text, CSampleSpellCheckProvider spellcheckProvider) : IEnumSpellingError
{
	SizeT currentTextPosition = 0;

	public HRESULT Next(out ISpellingError? value)
	{
		value = null;
		HRESULT hr = spellcheckProvider.EngineCheck(text.Substring(currentTextPosition), out var spellingError);
		if (hr == HRESULT.S_FALSE) // no more spelling errors left
		{
			return hr;
		}

		CSpellingError? returnedError = null;
		if (hr == HRESULT.S_OK)
		{
			SizeT indexInOriginal = currentTextPosition;
			returnedError = new CSpellingError(spellingError.CorrectiveAction, spellingError.Replacement, indexInOriginal + spellingError.StartIndex, spellingError.Length);
			currentTextPosition += spellingError.StartIndex + spellingError.Length;
		}

		if (hr == HRESULT.S_OK)
		{
			value = returnedError;
		}

		return hr;
	}
}