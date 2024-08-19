using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;

namespace SpellCheckerClient;

internal static partial class Program
{
	private static bool HasSingleString([In, Out] IEnumString enumString) => enumString.Enum().ElementAtOrDefault(1) is null;

	private static void PrintEnumString([In] IEnumString enumString, [In, Optional] string? prefixText)
	{
		foreach (var s in enumString.Enum())
			Console.WriteLine(prefixText is null ? s : $"{s} {prefixText}");
	}
}