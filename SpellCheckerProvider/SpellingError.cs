using static Vanara.PInvoke.SpellCheck;

class CSpellingError([In] CORRECTIVE_ACTION correctiveAction, string? replacement = null, uint startIndex = 0, uint errorLength = 0) : ISpellingError
{
	public uint StartIndex { get; set; } = startIndex;
	public uint Length { get; set; } = errorLength;
	public CORRECTIVE_ACTION CorrectiveAction { get; set; } = correctiveAction;
	public string Replacement { get; set; } = replacement ?? "";
}