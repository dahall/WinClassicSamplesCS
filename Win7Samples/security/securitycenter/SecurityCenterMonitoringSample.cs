using Vanara.PInvoke;
using static Vanara.PInvoke.WscApi;

var hr = WscRegisterForChanges(default, out var hWscCallbackRegistration, OnSecurityCenterHealthChange, default);
if (hr.Failed)
{
	Console.Write("Failed to Register for Security Center change notifications: Error 0x{0:X}\n", (int)hr);
	return;
}

if (hr.Succeeded)
{
	Console.Write("Monitoring Security Center for health changes. Press Enter to stop...\n");
	Console.ReadKey();
}

if (hr.Succeeded)
{
	hr = WscUnRegisterChanges(hWscCallbackRegistration);
	if (hr.Failed)
	{
		Console.Write("Failed to UnRegister Security Center change notifications: Error 0x{0:X}\n", (int)hr);
	}
}

uint OnSecurityCenterHealthChange(IntPtr lpParameter)
{
	HRESULT hr = HRESULT.S_OK;
	WSC_SECURITY_PROVIDER_HEALTH health = WSC_SECURITY_PROVIDER_HEALTH.WSC_SECURITY_PROVIDER_HEALTH_GOOD;

	if (hr.Succeeded)
	{
		hr = WscGetSecurityProviderHealth(WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_INTERNET_SETTINGS, out health);
		if (hr.Succeeded)
		{
			Console.Write("Internet Settings are {0}, The Security Center service is {1}\n",
			(WSC_SECURITY_PROVIDER_HEALTH.WSC_SECURITY_PROVIDER_HEALTH_GOOD == health) ? "OK" : "Not OK",
			(HRESULT.S_FALSE == hr) ? "Not Running" : "Running");
		}
	}
	if (hr.Succeeded)
	{
		hr = WscGetSecurityProviderHealth(WSC_SECURITY_PROVIDER.WSC_SECURITY_PROVIDER_ALL, out health);
		if (hr.Succeeded)
		{
			Console.Write("Security Center says the machines security health is {0}, The Security Center service is {0}\n",
			(WSC_SECURITY_PROVIDER_HEALTH.WSC_SECURITY_PROVIDER_HEALTH_GOOD == health) ? "OK" : "Not OK",
			(HRESULT.S_FALSE == hr) ? "Not Running" : "Running");
		}
	}
	if (hr.Failed)
	{
		Console.Write("Failed to get health status from Security Center: Error: 0x{0:X}\n", (int)hr);
	}
	return 0;
}