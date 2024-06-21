using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;

namespace PropertySchema;

internal class Program
{
	[MTAThread]
	static void Main(string[] args)
	{
		//Must specify at least a flag, and at least one other argument depending on the flag.
		//See DisplayUsage for more information
		if (args.Length > 0)
		{
			//Check the first argument (-register, -unregister, -refresh, -dump, or -showprops)
			string pszAction = args[0];
			string pszArgument = args.Length > 1 ? args[1] : "";

			if (!StrCmpICW(pszAction, "-register") && args.Length > 1)
			{
				//attempt to register the schema
				RegisterSchema(pszArgument);
			}
			else if (!StrCmpICW(pszAction, "-unregister") && args.Length > 1)
			{
				// attempt to unregister the schema
				UnregisterSchema(pszArgument);
			}
			else if (!StrCmpICW(pszAction, "-dump") && args.Length > 1)
			{
				// iterate through each property name listed and dump property description info
				DisplayDumpInformation(args);
			}
			else if (!StrCmpICW(pszAction, "-refresh"))
			{
				//refresh the schema, should be called after registering or unregistering
				RefreshSchema();
			}
			else if (!StrCmpICW(pszAction, "-showprops") && args.Length > 1)
			{
				//check to see if the next argument is one we can accept
				ParsePropFlag(pszArgument);
			}
			else if (args.Length <= 1)
			{
				DisplayUsage();
			}
			else
			{
				// only -register, -unregister, -dump, refresh, and -showprops are supported
				Console.Write("Unrecognized flag: {0}\n", pszAction);
			}
		}
		else
		{
			DisplayUsage();
		}
	}

	static bool StrCmpICW(string a, string b) => string.Compare(a, b, StringComparison.OrdinalIgnoreCase) != 0;

	static void DumpProperties(PROPDESC_ENUMFILTER pdefFilter)
	{
		//get the list of properties that fall into the filter
		HRESULT hr = PSEnumeratePropertyDescriptions(pdefFilter, typeof(IPropertyDescriptionList).GUID, out var ppdl);
		if (hr.Succeeded)
		{
			//GetCount returns the number of properties that fall under the filter
			//specified in call to PSEnumeratePropertyDescriptions
			var cuProps = ppdl.GetCount();
			if (hr.Succeeded)
			{
				switch (pdefFilter)
				{
					//These enums are part of the PROPDESC_ENUMFILTER enum type
					case PROPDESC_ENUMFILTER.PDEF_ALL:
						Console.Write("\nThere are {0} properties in the system.\n", cuProps);
						break;
					case PROPDESC_ENUMFILTER.PDEF_SYSTEM:
						Console.Write("\nThere are {0} System properties.\n", cuProps);
						break;
					case PROPDESC_ENUMFILTER.PDEF_NONSYSTEM:
						Console.Write("\nThere are {0} NonSystem properties.\n", cuProps);
						break;
					case PROPDESC_ENUMFILTER.PDEF_VIEWABLE:
						Console.Write("\nThere are {0} Viewable properties.\n", cuProps);
						break;
					case PROPDESC_ENUMFILTER.PDEF_QUERYABLE:
						Console.Write("\nThere are {0} Queryable properties.\n", cuProps);
						break;
					case PROPDESC_ENUMFILTER.PDEF_INFULLTEXTQUERY:
						Console.Write("\nThere are {0} InFullTextQuery properties.\n", cuProps);
						break;
				}

				if (cuProps <= 0)
				{
					Console.Write("No Properties to display\n");
				}
				else
				{
					Console.Write("Printing the Canonical Name of the properties:\n");
					IPropertyDescription? ppd;
					string pszCanonicalName;
					for (uint iIndex = 0; iIndex < cuProps; iIndex++)
					{
						//iterates through each item in the description list and
						//displays the canonical name. Information in addition to Canonical Name
						//is displayed via the -dump flag
						ppd = ppdl.GetAt(iIndex, typeof(IPropertyDescription).GUID);
						if (hr.Succeeded)
						{
							pszCanonicalName = ppd.GetCanonicalName();
							Marshal.ReleaseComObject(ppd);
							Console.Write($"{pszCanonicalName}\n");
						}
						else
						{
							Console.Write("IPropertyDescriptionList::GetAt failed with error: 0x{0:x}\n", hr);
						}
					}
				}
			}
			else
			{
				Console.Write("IPropertyDescriptionList::GetCount failed with error: 0x{0:x}\n", hr);
			}
		}
		else
		{
			Console.Write("PSEnumeratePropertyDescriptions failed with error: 0x{0:x}\n", hr);
		}
	}

	static void DumpPropertyDescription(string pszPropertyName)
	{
		HRESULT hr = PSGetPropertyDescriptionByName(pszPropertyName, typeof(IPropertyDescription).GUID, out var ppv);
		if (hr.Succeeded)
		{
			IPropertyDescription ppdDump = (IPropertyDescription)ppv;
			Console.Write("{0}\n", pszPropertyName);
			Console.Write("----------------------------\n");

			DumpPropertyKey(ppdDump);
			DumpCanonicalName(ppdDump);
			DumpDisplayName(ppdDump);
			DumpEditInvitation(ppdDump);
			DumpDefaultColumnWidth(ppdDump);
			DumpSortDescriptionLabel(ppdDump);

			Marshal.ReleaseComObject(ppv);
		}
		else
		{
			//This Error condition is a common one and should be handled specifically
			if (HRESULT.TYPE_E_ELEMENTNOTFOUND == hr)
			{
				Console.Write("{0} - Property Not found\n", pszPropertyName);
			}
			else
			{
				Console.Write("{0} - Error 0x{1:x} obtaining Property Description\n", pszPropertyName, hr);
			}
		}
	}

	static void DumpPropertyKey(IPropertyDescription ppdDump)
	{
		//the unique property identifier
		PROPERTYKEY key = ppdDump.GetPropertyKey();
		Console.Write("Property Key:\t\t{0}\n", key);
	}

	static void DumpCanonicalName(IPropertyDescription ppdDump)
	{
		string pszName = ppdDump.GetCanonicalName();
		Console.Write("Canonical Name:\t\t{0}\n", pszName);
	}

	static void DumpDisplayName(IPropertyDescription ppdDump)
	{
		//the name as displayed in, for example, the Search Pane
		ppdDump.GetDisplayName(out var pszName);
		Console.Write("Display Name:\t\t{0}\n", pszName);
	}

	static void DumpEditInvitation(IPropertyDescription ppdDump)
	{
		//the way in which the property is edited
		string pszInvite = ppdDump.GetEditInvitation();
		Console.Write("Edit Invitation:\t{0}\n", pszInvite);
	}

	static void DumpDefaultColumnWidth(IPropertyDescription ppdDump)
	{
		//the column width as displayed in the listview
		uint cchWidth = ppdDump.GetDefaultColumnWidth();
		Console.Write("Default Column Width:\t{0}\n", cchWidth);
	}

	static void DumpSortDescriptionLabel(IPropertyDescription ppdDump)
	{
		//the manner in which this property is sorted
		string pszAscending = ppdDump.GetSortDescriptionLabel(false);
		string pszDescending = ppdDump.GetSortDescriptionLabel(true);
		Console.Write("Sort Description Label:\t{0}/{1}\n", pszAscending, pszDescending);
	}

	static void RegisterSchema(string pszFileName)
	{
		HRESULT hr = PSRegisterPropertySchema(pszFileName);
		if (hr.Succeeded)
		{
			Console.Write("PSRegisterPropertySchema succeeded.\n");
		}
		else
		{
			Console.Write("PSRegisterPropertySchema failed for schema file {0} with error: 0x{1:x}\n", pszFileName, hr);
		}
	}

	static void UnregisterSchema(string pszFileName)
	{
		HRESULT hr = PSUnregisterPropertySchema(pszFileName);
		if (hr.Succeeded)
		{
			Console.Write("PSUnregisterPropertySchema succeeded.\n");
		}
		else
		{
			Console.Write("PSUnregisterPropertySchema failed for schema file {0} with error: 0x{1:x}\n", pszFileName, hr);
		}
	}

	static void DisplayDumpInformation(string[] pszArgList)
	{
		for (int argi = 1; argi < pszArgList.Length; argi++)
		{
			Console.Write("\n");
			DumpPropertyDescription(pszArgList[argi]);
		}
	}

	static void RefreshSchema()
	{
		// NOTE: Currently this API is not supported, although it may be in the future. In order to refresh an already registered schema, please use PSRegisterPropertySchema instead and pass in the same path which was used to register the schema initially.
		HRESULT hr = PSRefreshPropertySchema();
		if (hr.Succeeded)
		{
			Console.Write("PSRefreshPropertySchema succeeded.\n");
		}
		else
		{
			Console.Write("PSRefreshPropertySchema failed with error 0x{0:x}\n", hr);
		}
	}

	static void ParsePropFlag(string pszFlagToUse)
	{
		if (!StrCmpICW(pszFlagToUse, "All"))
		{
			DumpProperties(PROPDESC_ENUMFILTER.PDEF_ALL);
		}
		else if (!StrCmpICW(pszFlagToUse, "System"))
		{
			DumpProperties(PROPDESC_ENUMFILTER.PDEF_SYSTEM);
		}
		else if (!StrCmpICW(pszFlagToUse, "NonSystem"))
		{
			DumpProperties(PROPDESC_ENUMFILTER.PDEF_NONSYSTEM);
		}
		else if (!StrCmpICW(pszFlagToUse, "Viewable"))
		{
			DumpProperties(PROPDESC_ENUMFILTER.PDEF_VIEWABLE);
		}
		else if (!StrCmpICW(pszFlagToUse, "Queryable"))
		{
			DumpProperties(PROPDESC_ENUMFILTER.PDEF_QUERYABLE);
		}
		else if (!StrCmpICW(pszFlagToUse, "InFullTextQuery"))
		{
			DumpProperties(PROPDESC_ENUMFILTER.PDEF_INFULLTEXTQUERY);
		}
		else
		{
			DisplayUsage();
		}
	}

	static void DisplayUsage()
	{
		Console.Write("Usage: [OPTIONS] [ARGUMENTS]\n\n");
		Console.Write("Options:\n");
		Console.Write(" -register <filename>\t\t\t\tRegisters the provided property schema\n");
		Console.Write(" -unregister <filename>\t\t\t\tUnregisters the provided property schema\n");
		Console.Write(" -refresh\t\t\t\t\tRefreshes the property schema\n");
		Console.Write(" -dump <PropertyName1> <PropertyName2> ...\tShows information about each provided PropertyName\n");
		Console.Write(" -showprops <PropertyType>\t\t\tShows properties for the given property type\n");
		Console.Write("Arguments:\n");
		Console.Write(" filename\t\t\t\t\tProperty Schema to be registered\\unregistered\n");
		Console.Write(" PropertyName:\t\t\t\t\tThe Canonical Name of the property\n");
		Console.Write(" Property Type:\t\t\t\tThe type of filter to be applied with what properties are shown\n");
		Console.Write("\t\t\t\t\t\tAll\n\t\t\t\t\t\tSystem\n\t\t\t\t\t\tNonSytem\n\t\t\t\t\t\tViewable\n\t\t\t\t\t\tQueryable\n\t\t\t\t\t\tInFullTextQuery\n");
	}
}