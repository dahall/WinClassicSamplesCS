using Vanara.PInvoke;
using static Vanara.PInvoke.ActiveDS;

///////////////////////////////////////////////
// Bind to Object, it serves as a base search
///////////////////////////////////////////////
ADsGetObject("LDAP://DC=testDom1,DC=testDom2,DC=microsoft,DC=com", out IDirectorySearch? pSearch).ThrowIfFailed();

///////////////////////////////////////
// We want a subtree search
/////////////////////////////////////////
ADS_SEARCHPREF_INFO[] prefInfo = [new(ADS_SEARCHPREF.ADS_SEARCHPREF_SEARCH_SCOPE, ADS_SCOPE.ADS_SCOPE_SUBTREE)];
pSearch!.SetSearchPreference(prefInfo, 1).ThrowIfFailed();

//////////////////////////////////////////
// Search for all groups in a domain
/////////////////////////////////////////////
string[] pszAttr = ["Name"];
using var hSearch = pSearch.ExecuteSearch("(objectCategory=Group)", pszAttr);

//////////////////////////////////////////
// Now enumerate the result
/////////////////////////////////////////////
while (pSearch.GetNextRow(hSearch) != HRESULT.S_ADS_NOMORE_ROWS)
{
	// Get 'Name' attribute
	var col = pSearch.GetColumn(hSearch, pszAttr[0]);
	if (col.HasValue)
	{
		Console.WriteLine(col.Value.pADsValues[0].Value);
	}
}

////////////////////
// Clean-up
////////////////////////

return 0;