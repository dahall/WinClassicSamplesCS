using static Vanara.PInvoke.HttpApi;

if (args.Length != 1)
{
	Console.Write("Usage: IdnSample <url>\n");
	return 0;
}

// Initialize HTTPAPI to use server APIs.
using SafeHttpInitialize init = new();

// Prepare the input url.

HttpPrepareUrl(args[0], out var PreparedUrl).ThrowIfFailed();

Console.Write($"{args[0]} prepared is: {PreparedUrl}\n");

return 0;