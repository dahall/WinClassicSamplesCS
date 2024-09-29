using Vanara.DirectoryServices;

//----------------------------------------------------------
//--- This script enumerates ADSI objects in a computer 
//---------------------------------------------------------
var comp = (ADsComputer)ADsObject.GetObject("WinNT://" + Environment.MachineName + ",computer");

foreach (var obj in comp.Children)
	Console.WriteLine(obj.Name);

//----------------------------------------------------------
//--- This script enumerates ADSI objects in a domain
//---------------------------------------------------------
var dom = (ADsDomain)ADsObject.GetObject("WinNT://" + Environment.UserDomainName);

foreach (var obj in dom.Children)
	Console.WriteLine(obj.Name);