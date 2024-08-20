using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.SpellCheckProvider;

[ComVisible(true), Guid("9AEC2879-1A82-4FEA-AA4F-60B98D3AC293")]
public class CSampleSpellCheckProviderFactory : ISpellCheckProviderFactory
{
	internal static readonly string[] supportedLanguages = ["en-us"];

	HRESULT ISpellCheckProviderFactory.CreateSpellCheckProvider(string languageTag, out ISpellCheckProvider? value)
	{
		HRESULT hr = ((ISpellCheckProviderFactory)this).IsSupported(languageTag, out var isSupported);
		if (hr.Succeeded && !isSupported)
		{
			hr = HRESULT.E_INVALIDARG;
		}

		CSampleSpellCheckProvider? spellProvider = null;
		if (hr.Succeeded)
		{
			spellProvider = new CSampleSpellCheckProvider(languageTag);
		}

		value = spellProvider;
		return hr;
	}

	HRESULT ISpellCheckProviderFactory.get_SupportedLanguages(out IEnumString? value) => Util.CreateEnumString(supportedLanguages, out value);

	HRESULT ISpellCheckProviderFactory.IsSupported(string languageTag, out bool value)
	{
		value = supportedLanguages.Any(s => string.Compare(languageTag, s, true) == 0);
		return HRESULT.S_OK;
	}
}