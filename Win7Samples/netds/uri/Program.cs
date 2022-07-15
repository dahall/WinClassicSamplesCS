using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.UrlMon;

namespace uri;

internal class Program
{
	public static void Main(string[] args)
	{
		if (1 <= args.Length)
		{
			HRESULT hr = CreateUri(args[0], Uri_CREATE.Uri_CREATE_ALLOW_RELATIVE, default, out IUri pBaseIUri);

			if (hr.Succeeded)
			{
				if (1 == args.Length)
				{
					Console.Write("The result of creating \"{0}\": \n", args[0]);
					DisplayIUri(pBaseIUri);
				}
				else
				{
					hr = CreateUri(args[1], Uri_CREATE.Uri_CREATE_ALLOW_RELATIVE, default, out IUri pRelativeIUri);

					if (hr.Succeeded)
					{
						hr = CoInternetCombineIUri(pBaseIUri, pRelativeIUri, 0, out IUri pCombinedIUri);

						if (hr.Succeeded)
						{
							Console.Write("The result of combining \"{0}\" and \"{1}\": \n", args[0], args[1]);
							DisplayIUri(pCombinedIUri);
						}
						else
						{
							Console.Write("CoInternetCombineIUri failed with {0}\n", hr);
						}
					}
					else
					{
						Console.Write("CreateUri of Relative URI failed with {0}\n", hr);
					}
				}
			}
			else
			{
				Console.Write("CreateUri of Base URI failed with {0}\n", hr);
			}
		}
		else
		{
			DisplayHelp(System.IO.Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location));
		}
	}

	private static void DisplayHelp(string pwzApplicationName) => Console.Write("Displays the properties of an IUri object.\n" +
			"\n" +
			"{0} uri1 [uri2]\n" +
			"\n" +
			"When one URI is specified the displayed result is the URI.\n" +
			"When two URIs are specified the displayed result is the two URIs combined.\n" +
			"\n",
			pwzApplicationName is null ? "iuri" : pwzApplicationName);

	private static void DisplayIUri(IUri pUri)
	{
		for (uint dwProperty = (uint)Uri_PROPERTY.Uri_PROPERTY_STRING_START; dwProperty <= (uint)Uri_PROPERTY.Uri_PROPERTY_STRING_LAST; ++dwProperty)
		{
			try
			{
				pUri.GetPropertyBSTR((Uri_PROPERTY)dwProperty, out var bstrProperty);
				Console.Write("\t{0,-27} == \"{1}\"\n", GetPropertyName((Uri_PROPERTY)dwProperty), bstrProperty);
			}
			catch (Exception ex)
			{
				if (ex.HResult == HRESULT.S_FALSE)
					Console.Write("\t{0,-27} not set\n", GetPropertyName((Uri_PROPERTY)dwProperty));
				else
					Console.Write("\t{0,-27} GetPropertyBSTR failed with 0x{1:X}\n", GetPropertyName((Uri_PROPERTY)dwProperty), ex.HResult);
			}
		}

		for (uint dwProperty = (uint)Uri_PROPERTY.Uri_PROPERTY_DWORD_START; dwProperty <= (uint)Uri_PROPERTY.Uri_PROPERTY_DWORD_LAST; ++dwProperty)
		{
			try
			{
				pUri.GetPropertyDWORD((Uri_PROPERTY)dwProperty, out var dwValue);
				Console.Write("\t{0,-27} == {1}\n", GetPropertyName((Uri_PROPERTY)dwProperty), dwValue);
			}
			catch (Exception ex)
			{
				if (ex.HResult == HRESULT.S_FALSE)
					Console.Write("\t{0,-27} not set\n", GetPropertyName((Uri_PROPERTY)dwProperty));
				else
					Console.Write("\t{0,-27} GetPropertyDWORD failed with 0x{1:X}\n", GetPropertyName((Uri_PROPERTY)dwProperty), ex.HResult);
			}
		}
	}

	private static string GetPropertyName(Uri_PROPERTY property) => Enum.IsDefined(property) ? property.ToString() : "ERROR: Unknown Uri_PROPERTY value.";
}