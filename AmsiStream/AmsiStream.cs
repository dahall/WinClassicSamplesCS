using System.IO;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AMSI;

const string AppName = "Contoso Script Engine v3.4.9999.0";

CStreamScanner scanner = new();
HRESULT hr = 0;
if (args.Length < 1)
{
	// Scan a single memory stream.
	Console.Write("Creating memory stream object\n");

	AmsiStream stream = new(new SafeLPSTR("Hello, world"), false) { AppName = AppName, ContentName = "Sample content.txt" };
	hr = scanner.ScanStream(stream);
}
else
{
	// Scan the files passed on the command line.
	for (int i = 0; i < args.Length; i++)
	{
		string fileName = args[i];

		Console.Write("Creating stream object with file name: {0}\n", fileName);
		AmsiStream stream = new(new FileInfo(fileName), false) { AppName = AppName, ContentName = fileName };
		hr = scanner.ScanStream(stream);
	}
}
Console.Write("Leaving with hr = 0x{0:x}\n{1}\n", (int)hr, hr);

return 0;

internal class CStreamScanner
{
	private readonly IAntimalware m_antimalware = new();

	public HRESULT ScanStream([In] IAmsiStream stream)
	{
		Console.Write("Calling antimalware.Scan() ...\n");
		HRESULT hr = m_antimalware.Scan(stream, out var r, out var provider);
		if (hr.Failed)
		{
			return hr;
		}

		Console.Write("Scan result is {0}. IsMalware: {1}\n", r, AmsiResultIsMalware(r));

		if (provider is not null)
		{
			hr = provider.DisplayName(out var name);
			if (hr.Succeeded)
			{
				Console.Write("Provider display name: {0}\n", name);
			}
			else
			{
				Console.Write("DisplayName failed with 0x{0:x}", (int)hr);
			}
		}

		return HRESULT.S_OK;
	}
}