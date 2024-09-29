using Vanara.DirectoryServices;
using static Vanara.PInvoke.ActiveDS;
using Vanara.InteropServices;

///////////////////////////////////////////////////
// Create a user using IADsContainer::Create
////////////////////////////////////////////////////
var pCont = (ADsComputer)ADsObject.GetObject($"WinNT://{Environment.MachineName}");

var pIADsUser = (ADsUser)pCont.Children.Add("user", "AliceW");

//do NOT hard code your password into the source code in production code
//take user input instead
pIADsUser.SetPassword("MysEcret1");
pIADsUser.PropertyCache.Save(); // Commit

Console.Write("User created successfully\n");

return 0;

#pragma warning disable CS8321 // Local function is declared but never used
static unsafe void CreateUserIDirectoryObject()
{
	///////////////////////////////////////////////////////////
	// Use IDirectoryObject to create an object
	////////////////////////////////////////////////////////////

	// Create this user in an organizational unit
	var hr = ADsGetObject("LDAP://OU=MyOU,DC=fabrikam,DC=com", out IDirectoryObject? pDirObject);
	if (hr.Succeeded)
	{
		ADSVALUE sAMValue = new() { dwType = ADSTYPE.ADSTYPE_CASE_IGNORE_STRING, CaseIgnoreString = new SafeLPWSTR("user") };
		ADSVALUE uPNValue = new() { dwType = ADSTYPE.ADSTYPE_CASE_IGNORE_STRING, CaseIgnoreString = new SafeLPWSTR("mikes") };
		ADSVALUE classValue = new() { dwType = ADSTYPE.ADSTYPE_CASE_IGNORE_STRING, CaseIgnoreString = new SafeLPWSTR("mikes@fabrikam.com") };

		ADS_ATTR_INFO[] attrInfo = [
			new() { pszAttrName = "objectClass", dwControlCode = ADS_ATTR.ADS_ATTR_UPDATE, dwADsType = ADSTYPE.ADSTYPE_CASE_IGNORE_STRING, pADsValues = &classValue, dwNumValues = 1 },
			new() { pszAttrName = "sAMAccountName", dwControlCode = ADS_ATTR.ADS_ATTR_UPDATE, dwADsType = ADSTYPE.ADSTYPE_CASE_IGNORE_STRING, pADsValues = &sAMValue, dwNumValues = 1 },
			new() { pszAttrName = "userPrincipalName", dwControlCode = ADS_ATTR.ADS_ATTR_UPDATE, dwADsType = ADSTYPE.ADSTYPE_CASE_IGNORE_STRING, pADsValues = &uPNValue, dwNumValues = 1 },
		];

		pDirObject!.CreateDSObject("CN=Mike Smith", attrInfo, (uint)attrInfo.Length, out var pDisp);
	}
}
#pragma warning restore CS8321 // Local function is declared but never used