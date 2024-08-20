static partial class Util
{
	public static bool CaseInsensitiveIsEqual(string first, string second, int firstSize = -1, int secondSize = -1)
	{
		if (firstSize >= 0) first = first.Substring(0, Math.Min(firstSize, first.Length));
		if (secondSize >= 0) second = second.Substring(0, Math.Min(secondSize, second.Length));
		return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
	}
}