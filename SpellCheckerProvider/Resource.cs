static class ResId
{
	public const int IDS_IGNOREREPEATED_HEADING = 500;
	public const int IDS_IGNOREREPEATED_DESCRIPTION = 501;
	public const int IDS_IGNOREREPEATED_LABEL = 502;
	public const int IDS_OKLETTER_HEADING = 503;
	public const int IDS_OKLETTER_DESCRIPTION = 504;
	public const int IDS_OKLETTER_LABEL_A = 505;
	public const int IDS_OKLETTER_LABEL_B = 506;
	public const int IDS_OKLETTER_LABEL_F = 507;

	public static readonly Dictionary<int, string> Resources = new()
	{
		{ IDS_IGNOREREPEATED_DESCRIPTION, "Do not consider repeated consecutive words as misspelled." },
		{ IDS_IGNOREREPEATED_HEADING, "" },
		{ IDS_IGNOREREPEATED_LABEL, "Ignore repeated words" },
		{ IDS_OKLETTER_HEADING, "" },
		{ IDS_OKLETTER_DESCRIPTION, "Letter required to be present for a word to be considered correctly spelled." },
		{ IDS_OKLETTER_LABEL_A, "Letter a" },
		{ IDS_OKLETTER_LABEL_B, "Letter b" },
		{ IDS_OKLETTER_LABEL_F, "Letter f" },
	};
}