using System;
using System.Collections.Generic;
using System.Linq;
using Vanara.Extensions;

namespace ADQI;
internal static class Helper
{
	public static string[] VariantToStringList(object? var) => var switch {
		null => [],
		string[] arr => arr,
		string str => [str],
		byte[] bytes => bytes.ToHexDumpString(16, 4, 0).Split('\n', StringSplitOptions.RemoveEmptyEntries),
		object?[] obs => Array.ConvertAll(obs, o => o?.ToString() ?? ""),
		_ => [var.ToString()],
	};
}
