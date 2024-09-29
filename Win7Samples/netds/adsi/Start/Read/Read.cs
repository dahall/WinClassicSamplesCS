using Vanara.PInvoke;
using Vanara.DirectoryServices;
using static Vanara.PInvoke.ActiveDS;

////////////////////////////////////
// Bind to a directory object
/////////////////////////////////////
var pUsr = (ADsUser)ADsObject.GetObject($"WinNT://{Environment.MachineName}/{Environment.UserName},user");

///////////////////////////////////
// Get a single value attribute
////////////////////////////////////
Console.WriteLine($"FullName: {pUsr.PropertyCache["FullName"]}");

///////////////////////////////////////////////////////
// Get a multi value attribute from a service object
/////////////////////////////////////////////////////////
var pSvc = (ADsService)ADsObject.GetObject($"WinNT://{Environment.MachineName}/BITS,service");
var var = pSvc.PropertyCache["Dependencies"];
if (var is string[] sa)
{
	Console.WriteLine("Getting service dependencies using IADs :");
	Console.WriteLine(string.Join(" ", sa));
}

///////////////////////////////////////////////////////////
// Using IDirectoryObject to get a multivalue attribute
// Note: NOT all providers support this interface
////////////////////////////////////////////////////////////
ADsGetObject("LDAP://CN=Administrator,CN=Users,DC=testDom1,DC=testDom2,DC=microsoft,DC=com", out IDirectoryObject? pDirObject).ThrowIfFailed();
// Now get the attribute
var pAttrInfo = pDirObject!.GetObjectAttributes("objectClass");
Console.WriteLine("Getting the objectClass multivalue attribute using IDirectoryObject :");
foreach (var attr in pAttrInfo)
	foreach (var v in attr.pADsValues)
		Console.WriteLine($"  {v.value}");