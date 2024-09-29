using static Vanara.PInvoke.ActiveDS;

////////////////////////////////////////////
// First bind to the destination container
////////////////////////////////////////////
var hr = ADsGetObject("LDAP://OU=trOU,DC=domain1,DC=domain2,DC=microsoft,DC=com", out IADsContainer? pCont);
if (hr.Failed)
{
	return 0;
}

/////////////////////////////////////////////////
// Now, move the object to the bound container
///////////////////////////////////////////////////
object pDisp = pCont!.MoveHere("LDAP://CN=Mike Smith,OU=srOU,DC=domain1,DC=domain2,DC=microsoft,DC=com", null);

// You may do other operation here, such as updating attributes

return 1;