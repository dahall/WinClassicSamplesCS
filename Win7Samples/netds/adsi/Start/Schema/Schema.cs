using static Vanara.PInvoke.ActiveDS;

ADsGetObject($"WinNT://{Environment.MachineName}/Schema", out IADsContainer? pSchema).ThrowIfFailed();

// Better way
foreach (var pChild in pSchema!.Cast<IADs>())
	Console.Write("{0}\t\t({1})\n", pChild.Name, pChild.Class);

////////////// Enumerate Schema objects ///////////////////////////////////
ADsBuildEnumerator(pSchema!, out var pEnum).ThrowIfFailed();
Marshal.ReleaseComObject(pSchema!); // no longer needed, since we have the enumerator already

object[] var = [new object()];
while (ADsEnumerateNext(pEnum, 1, var, out var lFetch).Succeeded && lFetch == 1)
{
	var pChild = (IADs)var[0];
	// Get more information on the child classes
	string bstrName = pChild.Name;
	string bstrClass = pChild.Class;

	Console.Write("{0}\t\t({1})\n", bstrName, bstrClass);

	// Clean-up
	Marshal.ReleaseComObject(pChild);
}

//Release the enumerator.
if (pEnum is not null)
{
	ADsFreeEnumerator(pEnum);
}

return 0;