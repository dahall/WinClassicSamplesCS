using static Vanara.PInvoke.ActiveDS;

internal static class Util
{
	public static void PrintUsage()
	{
		Console.Write("\nUsage: dssrch /b <baseObject> /f <search_filter> [/f <attrlist>] [/p <preference>=value>] ");
		Console.Write(" [/u <UserName> <Password>] [/t <flagName>=<value> [/n <maxRowsToFetch>\n");
		Console.Write("\n where:\n");
		Console.Write(" baseObject = ADsPath of the base of the search\n");
		Console.Write(" search_filter = search filter string in LDAP format\n");
		Console.Write(" attrlist = list of the attributes to display\n");
		Console.Write(" preference could be one of:\n");
		Console.Write(" Asynchronous, AttrTypesOnly, DerefAliases, SizeLimit, TimeLimit,\n");
		Console.Write(" TimeOut, PageSize, SearchScope, SortOn, CacheResults\n");
		Console.Write(" flagName could be one of:\n");
		Console.Write(" SecureAuth or UseEncrypt\n");
		Console.Write(" value is yes/no/true/false for a Boolean and the respective integer for integers\n");
		Console.Write(" scope is one of \"Base\", \"OneLevel\", or \"Subtree\"\n");
		Console.Write("\nFor Example: dssrch /b NDS://ntmarst/ms /f \"(object Class=*)\" ");
		Console.Write(" /a \"ADsPath, name, description\" /p searchScope=onelevel\n\n OR \n");
		Console.Write("\n dssrch /b \"LDAP://test.mysite.com/");
		Console.Write("OU=testOU,DC=test,DC=mysite,DC=com\" /f \"(objectClass=*)\" /a \"ADsPath, name, usnchanged\" ");
		Console.Write(" /u \"CN=user1,CN=Users,DC=test,DC=mysite,DC=COM\" \"secret~1\" ");
		Console.Write("/p searchScope=onelevel /t secureauth=yes /p SortOn=name /p CacheResults=no\n");
	}

	//
	// Print the data depending on its type.
	//
	public static void PrintColumn(in ADS_SEARCH_COLUMN pColumn, string pszColumnName) =>
		Console.WriteLine($"{pszColumnName} = {string.Join("# ", pColumn.pADsValues)}");
}