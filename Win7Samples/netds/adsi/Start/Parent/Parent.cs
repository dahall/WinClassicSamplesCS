using static Vanara.PInvoke.ActiveDS;

/////////////////////////////////////////
//Bind to an object
/////////////////////////////////////////
var hr = ADsGetObject("WinNT://INDEPENDENCE/JJohnson", out IADs? pADs);
if (hr.Failed)
{
	return (int)hr;
}

//////////////////////////////
// Get the ADs Parent's Path
//////////////////////////////
var bstrParent = pADs!.Parent;

////////////////////////////////
// Bind to the Parent
////////////////////////////////
hr = ADsGetObject(bstrParent, out IADs? pParent);

if (hr.Succeeded)
{
	// do something with pParent...
}

return 0;