using Vanara.DirectoryServices;

////////////////////////////////
// Bind to a domain object
//////////////////////////////////
var pCont = (ADsComputer)ADsObject.GetObject($"WinNT://{Environment.MachineName}");

/////////////////////////////////
// Enumerate
/////////////////////////////////
foreach (IADsObject pChild in pCont.Children)
{
	Console.WriteLine($"{pChild.Name}\t({pChild.Class})");
}

return 0;