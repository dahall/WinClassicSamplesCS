using Vanara.DirectoryServices;
using static Vanara.PInvoke.ActiveDS;

using var pCont = (ADsComputer)ADsObject.GetObject($"WinNT://{Environment.MachineName}");

///////////////////////////////////////////////////
// Using IADsContainer::Delete to delete a user
//////////////////////////////////////////////////
pCont.Children.Remove("user", "AliceW");

/////////////////////////////////////////////////////////////
// Using IDirectoryObject::DeleteDSObject to delete a user
//////////////////////////////////////////////////////////////
var hr = ADsGetObject("LDAP://OU=testOU,DC=testDom1,DC=testDom2,DC=microsoft,DC=com", out IDirectoryObject? pDirObject);
if (hr.Succeeded)
{
	pDirObject!.DeleteDSObject("CN=Mike Smith");
}

return 0;