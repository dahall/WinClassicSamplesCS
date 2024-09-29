using Vanara.PInvoke;
using Vanara.DirectoryServices;

string adsPath = $"WinNT://{Environment.UserDomainName}/{Environment.UserName},user";

/////////////////////////////////////////
// Binding as currently logged on user
////////////////////////////////////////
using (IADsObject pADs = ADsObject.GetObject(adsPath))
{
	//... do some operations here
}

/////////////////////////////////////////
// Binding with alternate credentials
////////////////////////////////////////
using (IADsObject pADs = ADsObject.OpenObject(adsPath, ActiveDS.ADS_AUTHENTICATION.ADS_SECURE_AUTHENTICATION, "Administrator", "secret"))
{
}

return 0;