using Vanara.Extensions;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.Ole32;

namespace PropertyEdit;

internal class Program
{
	private static void EnumerateProperties(string pszFilename)
	{
		// Call the helper to get the property store for the initialized item Note that as long as you have the property store, you are
		// keeping the file open So always release it once you are done.
		using PropertyStore pps = GetPropertyStore(pszFilename);

		// Retrieve the number of properties stored in the item.
		foreach (var prop in pps)
		{
			// Get the property key at a given index.
			PrintProperty(prop.Key, prop.Value);
		}
	}

	private static void GetPropertyDescription(string pszCanonicalName)
	{
		// Get the property description for the given property. Property description contains meta information on the property itself.
		PropertyDescription ppd = new(pszCanonicalName);
		Console.Write("Property {0} has label : {1}\n", pszCanonicalName, ppd.DisplayName);
	}

	private static PropertyStore GetPropertyStore(string pszFilename) => new(pszFilename);

	private static void GetPropertyValue(string pszFilename, string pszCanonicalName)
	{
		// Convert the Canonical name of the property to PROPERTYKEY
		PROPERTYKEY key = new(pszCanonicalName);

		// Call the helper to get the property store for the initialized item
		using var pps = GetPropertyStore(pszFilename);
		PrintProperty(key, pps[key], pszCanonicalName);
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

	private static void PrintProperty(PROPERTYKEY key, object? propvarValue, string? pszCanonicalName = null)
	{
		pszCanonicalName ??= key.GetCanonicalName() ?? key.ToString();
		string s = propvarValue switch
		{
			null => "null",
			string v => $"\"{v}\"",
			FILETIME ft => ft.ToDateTime().ToString(),
			string[] v => string.Join(",", v),
			object[] v => string.Join(",", v),
			_ => propvarValue.ToString()!,
		};
		Console.Write("{0} = {1}\n", pszCanonicalName, s);
	}

	private static void SetPropertyValue(string pszFilename, string pszCanonicalName, string pszValue)
	{
		// Convert the Canonical name of the property to PROPERTYKEY
		PROPERTYKEY key = new(pszCanonicalName);

		// Call the helper to get the property store for the initialized item
		using var pps = GetPropertyStore(pszFilename);
		// Set the value to the property store of the item.
		pps[key] = pszValue;
		// Commit does the actual writing back to the file stream.
		pps.Commit();
		Console.Write("Property {0} value {1} written successfully \n", pszCanonicalName, pps[key]);
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