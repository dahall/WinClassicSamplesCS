using static Vanara.PInvoke.ActiveDS;

var hr = ADsGetObject("LDAP://CN=Users,DC=Microsoft,DC=COM", out IADsContainer? pCont);
if (hr.Failed)
{
	return 0;
}

var pDisp = pCont!.MoveHere("LDAP://CN=Jeff Smith,CN=Users,DC=Microsoft,DC=COM", "CN=Jeffrey Smith");

return 0;