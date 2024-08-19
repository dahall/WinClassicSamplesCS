using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.ElsCore;

namespace Decomp;

internal static class Program
{
	[MTAThread]
	internal static void Main()
	{
		// create Hangul Decomposition Transliteration service.
		using var pGuid = new PinnedObject(ELS_GUID_TRANSLITERATION_HANGUL_DECOMPOSITION);
		MAPPING_ENUM_OPTIONS enumOptions = new() { Size = Marshal.SizeOf(typeof(MAPPING_ENUM_OPTIONS)), pGuid = (IntPtr)pGuid };
		int testCount = 1;

		if (MappingGetServices(enumOptions, out var mappingServiceInfo).Succeeded)
		{
			bool succeeded;

			// Hangul syllable is decomposed into Korean 2beolsik keyboard keystrokes. Decomposed syllable string is represented by
			// Compatibility Jamo.
			succeeded = TestRecognizeMappingText(mappingServiceInfo[0], "\xAC00\xAC01", "\x3131\x314F\x3131\x314F\x3131");
			Console.Write($"test {testCount++}: {(succeeded ? "succeeded" : "failed")}\n");

			// A twin consonant is treated as a basic consonants. Because 2beolsik Keyboard defines keys for twin consonants.
			succeeded = TestRecognizeMappingText(mappingServiceInfo[0], "\xAE4C\xC600", "\x3132\x314F\x3147\x3155\x3146");
			Console.Write($"test {testCount++}: {(succeeded ? "succeeded" : "failed")}\n");

			// A single syllable can be decomposed in 2 to 5 jamos.
			succeeded = TestRecognizeMappingText(mappingServiceInfo[0], "\xAC00\xB220\xB400\xB923", "\x3131\x314F\x3134\x315C\x3153\x3137\x3157\x3150\x3134\x3139\x315C\x3154\x3131\x3145");
			Console.Write($"test {testCount++}: {(succeeded ? "succeeded" : "failed")}\n");

			// Modern compatibility jamos are also decomposed, but not for old jamos
			succeeded = TestRecognizeMappingText(mappingServiceInfo[0], "\x313A\x3165", "\x3139\x3131\x3165");
			Console.Write($"test {testCount++}: {(succeeded ? "succeeded" : "failed")}\n");

			// Decomposing is not applied to other characters.
			succeeded = TestRecognizeMappingText(mappingServiceInfo[0], "1A@\xAC00*", "1A@\x3131\x314F*");
			Console.Write($"test {testCount++}: {(succeeded ? "succeeded" : "failed")}\n");
		}
		else
		{
			Console.Write("Failed to create a transliteration service\n");
		}
	}

	private static bool TestRecognizeMappingText(in MAPPING_SERVICE_INFO mappingServiceInfo, [In] string queryValue, [In] string? expectedValue)
	{
		bool succeeded = false;
		MAPPING_PROPERTY_BAG mappingPropertyBag = new();
		MappingRecognizeText(mappingServiceInfo, queryValue, queryValue.Length + 1, 0, default, ref mappingPropertyBag).ThrowIfFailed();
		if (mappingPropertyBag.dwRangesCount > 0)
		{
			string? actualValue = StringHelper.GetString(mappingPropertyBag.rgResultRanges![0].pData, CharSet.Unicode, mappingPropertyBag.rgResultRanges![0].dwDataSize);
			succeeded = expectedValue == actualValue;
		}
		MappingFreePropertyBag(mappingPropertyBag);
		return succeeded;
	}
}