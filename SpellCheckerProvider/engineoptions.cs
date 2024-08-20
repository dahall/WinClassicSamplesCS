using Vanara.PInvoke;
using static ResId;
using static Util;
using static Vanara.PInvoke.User32;

internal static class OptionsStore
{
	private static readonly int[] enusIgnoreRepeatedLabelIds = [IDS_IGNOREREPEATED_LABEL];

	private static readonly int[] enusOkletterLabelIds = [IDS_OKLETTER_LABEL_A, IDS_OKLETTER_LABEL_B, IDS_OKLETTER_LABEL_F];

	private static readonly OptionDeclaration[] enusOptions = [
		new("samplespell:en-US:ignorerepeated", IDS_IGNOREREPEATED_HEADING, IDS_IGNOREREPEATED_DESCRIPTION, 0, enusIgnoreRepeatedLabelIds),
		new("samplespell:en-US:okletter", IDS_OKLETTER_HEADING, IDS_OKLETTER_DESCRIPTION, 2, enusOkletterLabelIds)
	];

	private static readonly LanguageOptions[] spellingOptions = [new("en-US", enusOptions)];

	public static readonly int MAX_LANGUAGE_OPTIONS = enusOptions.Length;
	public static readonly int MAX_LABELS = enusOkletterLabelIds.Length;

	public static HRESULT GetDefaultOptionValue(string optionId, out byte optionValue)
	{
		OptionDeclaration? declaration = GetOptionDeclarationFromId(optionId);
		optionValue = declaration?.defaultValue ?? 0;
		return declaration is null ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
	}

	public static HRESULT GetOptionDescription(string optionId, out string optionDesc) =>
		GetOptionStringFromResource(optionId, d => d.descriptionRid, out optionDesc);

	public static HRESULT GetOptionHeading(string optionId, out string optionHeading) =>
		GetOptionStringFromResource(optionId, d => d.headingRid, out optionHeading);

	public static HRESULT GetOptionIdsForLanguage(string languageTag, out string[] optionIds)
	{
		LanguageOptions? optionsList = GetLanguageOptionsList(languageTag);
		optionIds = optionsList is null ? [] : optionsList.declarations.Select(d => d.optionId).ToArray();
		return optionsList is null ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
	}

	public static int GetOptionIndexInLanguage(string optionId)
	{
		for (int i = 0; i < spellingOptions.Length; ++i)
			for (int j = 0; j < spellingOptions[i].declarations.Length; j++)
				if (CaseInsensitiveIsEqual(spellingOptions[i].declarations[j].optionId, optionId))
					return j;
		return -1;
	}

	public static HRESULT GetOptionLabels(string optionId, out string[] labels)
	{
		OptionDeclaration? declaration = GetOptionDeclarationFromId(optionId);
		labels = declaration is null ? [] : Array.ConvertAll(declaration.labelRids, i => LoadStringFromResource(i, out var s).Succeeded ? s! : "");
		return declaration is null ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
	}

	private static LanguageOptions? GetLanguageOptionsList(string languageTag) =>
							spellingOptions.FirstOrDefault(o => CaseInsensitiveIsEqual(o.languageTag, languageTag));

	private static OptionDeclaration? GetOptionDeclarationFromId(string optionId) =>
		spellingOptions.SelectMany(l => l.declarations).FirstOrDefault(d => CaseInsensitiveIsEqual(d.optionId, optionId));

	private static OptionDeclaration? GetOptionDeclarationFromId(string optionId, [In] LanguageOptions optionsList) =>
		optionsList.declarations.FirstOrDefault(d => CaseInsensitiveIsEqual(d.optionId, optionId));

	private static HRESULT GetOptionStringFromResource(string optionId, Func<OptionDeclaration, int> g, out string optionString)
	{
		OptionDeclaration? declaration = GetOptionDeclarationFromId(optionId);
		HRESULT hr = declaration is null ? HRESULT.E_INVALIDARG : HRESULT.S_OK;
		if (hr.Failed)
			optionString = "";
		else
		{
			hr = LoadStringFromResource(g(declaration!), out var s);
			optionString = hr.Succeeded ? s! : "";
		}
		return hr;
	}

	private static HRESULT LoadStringFromResource([In] int resourceIndex, out string? stringResource) =>
		Resources.TryGetValue(resourceIndex, out stringResource) ? HRESULT.S_OK : HRESULT.E_INVALIDARG;

	private class LanguageOptions(string languageTag, OptionDeclaration[] declarations)
	{
		public OptionDeclaration[] declarations { get; } = declarations;
		public string languageTag { get; } = languageTag;
	}

	private class OptionDeclaration(string optionId, int headingRid, int descriptionRid, byte defaultValue, int[] labelRids)
	{
		public byte defaultValue { get; } = defaultValue;
		public int descriptionRid { get; } = descriptionRid;
		public int headingRid { get; } = headingRid;
		public int[] labelRids { get; } = labelRids;
		public string optionId { get; } = optionId;
	}
}