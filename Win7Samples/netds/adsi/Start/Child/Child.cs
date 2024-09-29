using Vanara.DirectoryServices;

string adsPath = $"LDAP://DC=mydomain2,DC=mydomain1,DC=microsoft,DC=com";

using (IADsObject pObj = ADsObject.GetObject(adsPath))
{
	var pCont = (IADsContainerObject<IADsObject>)pObj;

	/////////////////////////////////////////////////////////////
	// Get the child from the container 
	// Note in the LDAP provider you can go down more than one level
	///////////////////////////////////////////////////////////////
	IADsObject pDisp = pCont.Children["user", "CN=Mike Smith, OU=myou1"];

	var pADs = (ADsUser)pDisp;
}

return 0;