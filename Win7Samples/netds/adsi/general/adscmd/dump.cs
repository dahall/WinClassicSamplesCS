using Vanara.DirectoryServices;
using Vanara.PInvoke;

internal partial class Program
{
	static int DoDump(string pszADsPath)
	{
		//
		// Bind to the object.
		//
		try
		{
			var pADs = ADsObject.GetObject(pszADsPath);
			// Dump the object
			DumpObject(pADs);
			return 0;
		}
		catch (Exception ex)
		{
			Console.Write("Unable to read properties of: {0}\n", pszADsPath);
			return ex.HResult;
		}
	}

	//
	// Given an ADs pointer, dump the contents of the object
	//
	static void DumpObject(IADsObject pADs)
	{
		//
		// Access the schema for the object
		//
		var parray = GetPropertyList(pADs);

		//
		// List the Properties
		//
		foreach (var varProperty in parray)
		{
			//
			// Get a property and print it out. The HRESULT is passed to
			// PrintProperty.
			//
			PrintProperty(varProperty, HRESULT.S_OK, pADs.PropertyCache[varProperty]);
		}
	}

	static IReadOnlyCollection<string> GetPropertyList(IADsObject pADs) => pADs.Schema.MandatoryProperties;
}