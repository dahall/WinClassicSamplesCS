using Vanara.PInvoke;

internal partial class Program
{
	static void PrintVariant(object? varPropData)
	{
		if (varPropData is null)
			Console.WriteLine("<null>");
		else if (varPropData.GetType().IsArray)
			PrintVariantArray(varPropData);
		else if (varPropData.GetType().IsCOMObject)
			Console.WriteLine("<Object>");
		else
			Console.WriteLine(varPropData);
	}

	static void PrintVariantArray(object var) => Console.WriteLine(string.Join(", ", (object[])var));

	static void PrintProperty(string bstrPropName, HRESULT hRetVal, object? varPropData)
	{
		switch ((int)hRetVal)
		{
			case 0:
				Console.Write($"{bstrPropName,-32}: ");
				PrintVariant(varPropData);
				break;

			case HRESULT.E_ADS_CANT_CONVERT_DATATYPE:
				Console.Write($"{bstrPropName,-32}: ");
				Console.Write("<Data could not be converted for display>\n");
				break;

			default:
				Console.Write($"{bstrPropName,-32}: ");
				Console.Write("<Data not available>\n");
				break;
		}
	}

	static void PrintUsage() => Console.Write("usage: adscmd [list|dump] <ADsPath>\n");
}
