using Vanara.InteropServices;
using static Vanara.PInvoke.Tdh;

Console.Write("TdhEnumerateProviders output:\n");
TdhEnumerateProvidersSample();

Console.Write("TdhEnumerateProvidersForDecodingSource output:\n");
TdhEnumerateProvidersForDecodingSourceSample();

string GetSchemaSourceName(uint schemaSource) => schemaSource switch
{
	0 => "XML manifest",
	1 => "WMI MOF class",
	_ => "unknown",
};

void ShowProviderInfo(SafeCoTaskMemStruct<PROVIDER_ENUMERATION_INFO>? pEnum)
{
	if (pEnum is null)
	{
		return;
	}

	for (int i = 0; i < pEnum.Value.NumberOfProviders; i++)
	{
		TRACE_PROVIDER_INFO pInfo = pEnum.Value.TraceProviderInfoArray[i];
		// Provider information is in pEnum.TraceProviderInfoArray[i].
		Console.Write("Provider name: {0}\nProvider Guid: {1}\nSource: {2} ({3})\n\n",
			PEI_PROVIDER_NAME(pEnum, pInfo),
			pInfo.ProviderGuid, pInfo.SchemaSource, GetSchemaSourceName(pInfo.SchemaSource));
	}
}

void TdhEnumerateProvidersForDecodingSourceSample()
{
	// Available in Windows 10 build 20348 or later. Retrieve providers registered via manifest files with
	// TdhEnumerateProvidersForDecodingSource. Allocate the required buffer and call TdhEnumerateProvidersForDecodingSource. The list of
	// providers can change between the time you retrieved the required buffer size and the time you enumerated the providers, so call
	// TdhEnumerateProvidersForDecodingSource in a loop until the function does not return Win32Error.ERROR_INSUFFICIENT_BUFFER.

	// Note that the only supported decoding sources are DecodingSourceXMLFile and DecodingSourceWbem. This sample uses DecodingSourceXMLFile.
	Vanara.PInvoke.Win32Error status = TdhEnumerateProvidersForDecodingSource(DECODING_SOURCE.DecodingSourceXMLFile,
		out SafeCoTaskMemStruct<PROVIDER_ENUMERATION_INFO>? manifestProvidersBuffer);

	if (status.Failed)
	{
		Console.Write("TdhEnumerateProvidersForDecodingSource failed with error {0}.\n", status);
		return;
	}
	else
	{
		ShowProviderInfo(manifestProvidersBuffer);
	}
}

void TdhEnumerateProvidersSample()
{
	// Available in Windows Vista or later. Retrieve providers registered via manifest files and via MOF class with TdhEnumerateProviders.
	// Allocate the required buffer and call TdhEnumerateProviders. The list of providers can change between the time you retrieved the
	// required buffer size and the time you enumerated the providers, so call TdhEnumerateProviders in a loop until the function does not
	// return Win32Error.ERROR_INSUFFICIENT_BUFFER.
	Vanara.PInvoke.Win32Error status = TdhEnumerateProviders(out SafeCoTaskMemStruct<PROVIDER_ENUMERATION_INFO>? providerBuffer);

	if (status.Failed)
	{
		Console.Write("TdhEnumerateProviders failed with error {0}.\n", status);
		return;
	}
	else
	{
		ShowProviderInfo(providerBuffer);
	}
}