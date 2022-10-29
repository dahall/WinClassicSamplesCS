using Vanara.PInvoke;
using static Vanara.PInvoke.WscApi;

Dictionary<string, WSC_SECURITY_PROVIDER> match = new() {
	{ "-av", WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_ANTIVIRUS },
	{ "-as", WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_ANTISPYWARE },
	{ "-fw", WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_FIREWALL },
};
List<WSC_SECURITY_PROVIDER> providers = new();

foreach (var arg in args)
{
	if (match.TryGetValue(arg.ToLower(), out var p))
		providers.Add(p);
}
if (providers.Count == 0)
	PrintUsage();
else
	foreach (var provider in providers)
		GetSecurityProducts(provider);

HRESULT GetSecurityProducts(WSC_SECURITY_PROVIDER provider)
{
	if (!Enum.IsDefined(provider))
	{
		return HRESULT.E_INVALIDARG;
	}

	//
	// Initialize the product list with the type of security product you're 
	// interested in.
	//
	IWscProductList PtrProductList = new();
	PtrProductList.Initialize(provider);

	//
	// Get the number of security products of that type.
	//
	var ProductCount = PtrProductList.Count;

	if (provider == WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_FIREWALL)
	{
		Console.Write("\n\nFirewall Products:\n");
	}
	else if (provider == WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_ANTIVIRUS)
	{
		Console.Write("\n\nAntivirus Products:\n");
	}
	else
	{
		Console.Write("\n\nAntispyware Products:\n");
	}

	//
	// Loop over each product, querying the specific attributes.
	//
	for (uint i = 0; i < ProductCount; i++)
	{
		//
		// Get the next security product
		//
		var PtrProduct = PtrProductList[i];

		//
		// Get the product name
		//
		Console.Write("\nProduct name: {0}\n", PtrProduct.ProductName);
		Console.Write("\nProduct GUID: {0}\n", PtrProduct.ProductGuid);
		Console.Write("\nProduct is default: {0}\n", PtrProduct.ProductIsDefault);

		//
		// Get the product state
		//
		string pszState = PtrProduct.ProductState switch
		{
			WSC_SECURITY_PRODUCT_STATE.WSC_SECURITY_PRODUCT_STATE_ON => "On",
			WSC_SECURITY_PRODUCT_STATE.WSC_SECURITY_PRODUCT_STATE_OFF => "Off",
			WSC_SECURITY_PRODUCT_STATE.WSC_SECURITY_PRODUCT_STATE_SNOOZED => "Snoozed",
			_ => "Expired"
		};
		Console.Write("Product state: {0}\n", pszState);

		//
		// Get the signature status (not applicable to firewall products)
		//
		if (provider != WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_FIREWALL)
		{
			string pszStatus = (PtrProduct.SignatureStatus == WSC_SECURITY_SIGNATURE_STATUS.WSC_SECURITY_PRODUCT_UP_TO_DATE) ? "Up-to-date" : "Out-of-date";
			Console.Write("Product status: {0}\n", pszStatus);
		}
		else
		{
			var p3 = (IWscProduct3)PtrProduct;
			Console.WriteLine($"Status: Dom={p3.FirewallDomainProfileSubstatus}, Pub={p3.FirewallPublicProfileSubstatus}, Prv={p3.FirewallPrivateProfileSubstatus}");
		}

		//
		// Get the remediation path for the security product
		//
		Console.Write("Product remediation path: {0}\n", PtrProduct.RemediationPath);

		//
		// Get the product state timestamp (updated when product changes its 
		// state), and only applicable for AV products (default is returned for
		// AS and FW products)
		//
		if (provider == WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_ANTIVIRUS)
		{
			Console.Write("Product state timestamp: {0}\n", PtrProduct.ProductStateTimestamp);
			var p3 = (IWscProduct3)PtrProduct;
			Console.WriteLine($"Status: Scan={p3.AntivirusScanSubstatus}, Set={p3.AntivirusSettingsSubstatus}, Upd={p3.AntivirusProtectionUpdateSubstatus}");
			Console.WriteLine($"Days to Exp: {p3.AntivirusDaysUntilExpired}");
		}

		PtrProduct = null;
	}

	return HRESULT.S_OK;
}

void PrintUsage()
{
	Console.Write("Usage: WscApiSample.exe [-av | -as | -fw]\n" +
		"   av: Query Antivirus programs\n" +
		"   as: Query Antispyware programs\n" +
		"   fw: Query Firewall programs\n\n");
}