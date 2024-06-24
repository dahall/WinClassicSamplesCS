using Vanara.InteropServices;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.Shell32;

namespace PropertyEdit;

internal class Program
{
	private static void EnumerateProperties(string pszFilename)
	{
		// Call the helper to get the property store for the initialized item Note that as long as you have the property store, you are
		// keeping the file open So always release it once you are done.
		using ComReleaser<IPropertyStore> pps = new(GetPropertyStore(pszFilename, GETPROPERTYSTOREFLAGS.GPS_DEFAULT));

		// Retrieve the number of properties stored in the item.
		for (uint i = 0; i < pps.Item.GetCount(); i++)
		{
			// Get the property key at a given index.
			PROPERTYKEY key = pps.Item.GetAt(i);
			// Get the canonical name of the property
			string pszCanonicalName = key.GetCanonicalName() ?? key.ToString();
			PrintProperty(pps.Item, key, pszCanonicalName);
		}
	}

	private static void GetPropertyDescription(string pszCanonicalName)
	{
		// Get the property description for the given property. Property description contains meta information on the property itself.
		PSGetPropertyDescriptionByName(pszCanonicalName, typeof(IPropertyDescription).GUID, out var ppd).ThrowIfFailed();
		((IPropertyDescription)ppd).GetDisplayName(out var pszPropertyLabel).ThrowIfFailed();
		Console.Write("Property {0} has label : {1}\n", pszCanonicalName, pszPropertyLabel);
	}

	private static IPropertyStore GetPropertyStore(string pszFilename, GETPROPERTYSTOREFLAGS gpsFlags)
	{
		string szAbsPath = System.IO.Path.GetFullPath(Environment.ExpandEnvironmentVariables(pszFilename));
		SHGetPropertyStoreFromParsingName(szAbsPath, default, gpsFlags, typeof(IPropertyStore).GUID, out var ppps).ThrowIfFailed();
		return (IPropertyStore)ppps!;
	}

	private static void GetPropertyValue(string pszFilename, string pszCanonicalName)
	{
		// Convert the Canonical name of the property to PROPERTYKEY
		PSGetPropertyKeyFromName(pszCanonicalName, out var key).ThrowIfFailed();

		// Call the helper to get the property store for the initialized item
		using ComReleaser<IPropertyStore> pps = new(GetPropertyStore(pszFilename, GETPROPERTYSTOREFLAGS.GPS_DEFAULT));
		PrintProperty(pps.Item, key, pszCanonicalName);
	}

	[STAThread]
	private static void Main(string[] args)
	{
		int argc = -1;
		string? CONSUME_NEXT_ARG() => args.Length > ++argc ? args[argc] : null;

		string pszAppName = System.IO.Path.GetFileName(Environment.ProcessPath ?? "PropertyEdit.exe");
		string? pszOp = CONSUME_NEXT_ARG();
		if (!string.IsNullOrEmpty(pszOp) && (pszOp[0] == '-' || pszOp[0] == '/'))
		{
			/* skip - or / */
			pszOp = pszOp.TrimStart('-', '/').ToLowerInvariant();
			if (pszOp == "?")
			{
				Usage(pszAppName);
			}
			else if (pszOp == "get")
			{
				string? pszPropertyName = CONSUME_NEXT_ARG();
				if (pszPropertyName is not null)
				{
					string? pszFileName = CONSUME_NEXT_ARG();
					if (pszFileName is not null)
					{
						GetPropertyValue(pszFileName, pszPropertyName);
					}
					else
					{
						Console.Write("No file name specified.\n");
					}
				}
				else
				{
					Console.Write("No property canonical name specified.\n");
				}
			}
			else if (pszOp == "enum")
			{
				string? pszFileName = CONSUME_NEXT_ARG();
				if (pszFileName is not null)
				{
					EnumerateProperties(pszFileName);
				}
				else
				{
					Console.Write("No file name specified.\n");
				}
			}
			else if (pszOp == "set")
			{
				string? pszPropertyName = CONSUME_NEXT_ARG();
				if (pszPropertyName is not null)
				{
					string? pszPropertyValue = CONSUME_NEXT_ARG();
					if (pszPropertyValue is not null)
					{
						string? pszFileName = CONSUME_NEXT_ARG();
						if (pszFileName is not null)
						{
							SetPropertyValue(pszFileName, pszPropertyName, pszPropertyValue);
						}
						else
						{
							Console.Write("No file name specified.\n");
						}
					}
					else
					{
						Console.Write("No property value specified.\n");
					}
				}
				else
				{
					Console.Write("No property canonical name specified.\n");
				}
			}
			else if (pszOp == "info")
			{
				string? pszPropertyName = CONSUME_NEXT_ARG();
				if (pszPropertyName is not null)
				{
					GetPropertyDescription(pszPropertyName);
				}
				else
				{
					Console.Write("No property canonical name specified.\n");
				}
			}
			else
			{
				Console.Write("Unrecognized operation specified: -{0}\n", pszOp);
				Usage(pszAppName);
			}
		}
		else
		{
			Console.Write("No operation specified.\n");
			Usage(pszAppName);
		}
	}

	private static void PrintProperty(IPropertyStore pps, PROPERTYKEY key, string pszCanonicalName)
	{
		PROPVARIANT propvarValue = new();
		pps.GetValue(key, propvarValue);
		Console.Write("{0} = {1}\n", pszCanonicalName, propvarValue);
	}

	private static void SetPropertyValue(string pszFilename, string pszCanonicalName, string pszValue)
	{
		// Convert the Canonical name of the property to PROPERTYKEY
		PSGetPropertyKeyFromName(pszCanonicalName, out var key).ThrowIfFailed();

		// Call the helper to get the property store for the initialized item
		using ComReleaser<IPropertyStore> pps = new(GetPropertyStore(pszFilename, GETPROPERTYSTOREFLAGS.GPS_DEFAULT));
		PROPVARIANT propvarValue = new(pszValue);
		PSCoerceToCanonicalValue(key, propvarValue).ThrowIfFailed();
		// Set the value to the property store of the item.
		pps.Item.SetValue(key, propvarValue);
		// Commit does the actual writing back to the file stream.
		pps.Item.Commit();
		Console.Write("Property {0} value {1} written successfully \n", pszCanonicalName, pszValue);
	}

	private static void Usage(string pszAppName)
	{
		Console.Write("Usage: {0} [OPTIONS] [Filename] \n", pszAppName);
		Console.Write("\n");
		Console.Write("Options:\n");
		Console.Write(" -get <PropertyName> Get the value for the property defined\n");
		Console.Write(" by its Canonical Name in <propertyName>\n");
		Console.Write(" -set <PropertyName> Set the value for the property defined\n");
		Console.Write(" <PropertyValue> by <PropertyName> with value <PropertyValue>\n");
		Console.Write(" -enum Enumerate all the properties.\n");
		Console.Write(" -info <PropertyName> Get schema information on property.\n");
		Console.Write("\n");
		Console.Write("Examples:\n");
		Console.Write("PropertyEdit -get System.Author foo.jpg\n");
		Console.Write("PropertyEdit -set System.Author \"John Doe\" foo.jpg\n");
		Console.Write("PropertyEdit -enum foo.jpg\n");
		Console.Write("PropertyEdit -info System.Author \n");
	}
}