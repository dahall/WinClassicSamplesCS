using Vanara.DirectoryServices;

internal partial class Program
{
	static int DoList(string pszADsPath)
	{
		try
		{
			var pADsContainer = ADsObject.GetObject(pszADsPath) as IADsContainerObject<IADsObject> ?? throw new ArgumentException("Invalid path", nameof(pszADsPath));
			foreach (var i in pADsContainer.Children)
				Console.WriteLine($"  {i.Name}({i.Class})");
			return 0;
		}
		catch (Exception ex)
		{
			Console.Write("Failed to enumerate objects of: {0}\n", pszADsPath);
			return ex.HResult;
		}
	}
}