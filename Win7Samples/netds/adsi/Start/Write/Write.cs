using Vanara.PInvoke;
using static Vanara.PInvoke.ActiveDS;

string pszADsPath = "LDAP://CN=Jane Johnson,OU=testOU,DC=testDom1,DC=testDom2,DC=microsoft,DC=com";

///////////////////////////////////
// Modifying attributes via IADs
////////////////////////////////////
ADsGetObject(pszADsPath, out IADs? pADs).ThrowIfFailed();

// we omit checking result for brevity..

// First Name
pADs!.Put("givenName", "Janet");

// Last Name
pADs!.Put("sn", "Johns");

// Other Telephones
string[] pszPhones = { "425 844 1234", "425 924 4321" };
pADs.Put("otherTelephone", pszPhones);

pADs.SetInfo();

/////////////////////////////////////////////////
// Alternatively, you can use IDirectoryObject
//////////////////////////////////////////////////
ADsGetObject(pszADsPath, out IDirectoryObject? pDir).ThrowIfFailed();

ADS_ATTR_INFO[] attrInfo = [
	new("givenName", "Janet"),
	new("sn", "Johns"),
	new("otherTelephone", pszPhones),
];

pDir!.SetObjectAttributes(attrInfo);

return 0;