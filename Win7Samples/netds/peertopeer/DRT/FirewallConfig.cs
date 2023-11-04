using System.Runtime.InteropServices;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.FirewallApi;
using static Vanara.PInvoke.Kernel32;

namespace DrtSdkSample
{
	static class FirewallConfig
	{
		public static HRESULT OpenFirewallForDrtSdkSample(bool fOpenPortForPNRP)
		{
			var hr = WindowsFirewallInitialize(out var fwProfile);
			if (hr.Failed)
			{
				Console.Write("WindowsFirewallInitialize failed: {0}\n", hr);
				goto error;
			}

			hr = WindowsFirewallIsOn(fwProfile, out bool fFirewallOn);
			if (hr.Failed)
			{
				Console.Write("WindowsFirewallIsOn failed: {0}\n", hr);
				goto error;
			}

			if (fFirewallOn)
			{
				var lpFilename = GetModuleFileName(default);

				// Add DrtSdkSample to the authorized application collection.
				hr = WindowsFirewallAddApp(fwProfile, lpFilename, "DrtSdkSample");
				if (hr.Failed)
				{
					Console.Write("WindowsFirewallAddApp failed: {0}\n", hr);
					goto error;
				}

				if (fOpenPortForPNRP)
				{
					// Add PNRP Port to list of globally open ports.
					hr = WindowsFirewallPortAdd(fwProfile, 3540, NET_FW_IP_PROTOCOL.NET_FW_IP_PROTOCOL_UDP, "PNRP");
					if (hr.Failed)
					{
						Console.Write("WindowsFirewallPortAdd failed: {0}\n", hr);
						goto error;
					}
				}

			}

			error:

			// Release the firewall profile.
			WindowsFirewallCleanup(fwProfile);

			return hr;
		}

		static HRESULT WindowsFirewallInitialize(out INetFwProfile fwProfile)
		{
			using var pMgr = ComReleaserFactory.Create(new INetFwMgr());
			using var pPolicy = ComReleaserFactory.Create(pMgr.Item.LocalPolicy);
			fwProfile = pPolicy.Item.CurrentProfile;
			return HRESULT.S_OK;
		}

		static void WindowsFirewallCleanup(INetFwProfile fwProfile)
		{
			Marshal.FinalReleaseComObject(fwProfile);
		}

		static HRESULT WindowsFirewallIsOn(INetFwProfile fwProfile, out bool fwOn)
		{
			fwOn = fwProfile.FirewallEnabled;
			return HRESULT.S_OK;
		}

		static HRESULT WindowsFirewallTurnOn(INetFwProfile fwProfile, bool value = true)
		{
			if (fwProfile.FirewallEnabled != value)
			{
				fwProfile.FirewallEnabled = value;
				Console.WriteLine($"The firewall is now {(value ? "on" : "off")}.");
			}
			return HRESULT.S_OK;
		}

		static HRESULT WindowsFirewallTurnOff(INetFwProfile fwProfile) => WindowsFirewallTurnOn(fwProfile, false);

		static HRESULT WindowsFirewallAppIsEnabled(INetFwProfile fwProfile, string fwProcessImageFileName, out bool fwAppEnabled)
		{
			_ = fwProfile ?? throw new ArgumentNullException(nameof(fwProfile));
			_ = fwProcessImageFileName ?? throw new ArgumentNullException(nameof(fwProcessImageFileName));

			// Retrieve the authorized application collection.
			using var fwApps = ComReleaserFactory.Create(fwProfile.AuthorizedApplications);

			// Attempt to retrieve the authorized application.
			using var fwApp = ComReleaserFactory.Create(fwApps.Item.Item(fwProcessImageFileName));

			// Find out if the authorized application is enabled.
			fwAppEnabled = fwApp.Item.Enabled;

			return HRESULT.S_OK;
		}

		static HRESULT WindowsFirewallAddApp(INetFwProfile fwProfile, string fwProcessImageFileName, string fwName)
		{
			_ = fwProfile ?? throw new ArgumentNullException(nameof(fwProfile));
			_ = fwProcessImageFileName ?? throw new ArgumentNullException(nameof(fwProcessImageFileName));
			_ = fwName ?? throw new ArgumentNullException(nameof(fwName));

			// First check to see if the application is already authorized.
			var hr = WindowsFirewallAppIsEnabled(fwProfile, fwProcessImageFileName, out var fwAppEnabled);
			if (hr.Failed)
			{
				Console.Write("WindowsFirewallAppIsEnabled failed: {0}\n", hr);
				goto error;
			}

			// Only add the application if it isn't already authorized.
			if (!fwAppEnabled)
			{
				Console.Write("Adding firewall exception for {0}\n", fwName);

				// Retrieve the authorized application collection.
				using var fwApps = ComReleaserFactory.Create(fwProfile.AuthorizedApplications);

				// Create an instance of an authorized application.
				using var fwApp = ComReleaserFactory.Create(new INetFwAuthorizedApplication());

				// Set the process image file name.
				fwApp.Item.ProcessImageFileName = fwProcessImageFileName;

				// Set the application friendly name.
				fwApp.Item.Name = fwName;

				// Add the application to the collection.
				fwApps.Item.Add(fwApp.Item);

				// Check if the app is still not added
				hr = WindowsFirewallAppIsEnabled(fwProfile, fwProcessImageFileName, out fwAppEnabled);
				if (hr.Failed)
				{
					Console.Write("WindowsFirewallAppIsEnabled failed: {0}\n", hr);
					goto error;
				}

				if (!fwAppEnabled)
				{
					Console.Write("ERROR: Could not enable firewall application exception, try running as administrator.\n");
				}
			}

			error:

			return hr;
		}

		static HRESULT WindowsFirewallPortIsEnabled(INetFwProfile fwProfile, int portNumber, NET_FW_IP_PROTOCOL ipProtocol, out bool fwPortEnabled)
		{
			_ = fwProfile ?? throw new ArgumentNullException(nameof(fwProfile));

			fwPortEnabled = false;

			// Retrieve the globally open ports collection.
			using var fwOpenPorts = ComReleaserFactory.Create(fwProfile.GloballyOpenPorts);

			try
			{
				// Attempt to retrieve the globally open port.
				using var fwOpenPort = ComReleaserFactory.Create(fwOpenPorts.Item.Item(portNumber, ipProtocol));

				// Find out if the globally open port is enabled.
				fwPortEnabled = fwOpenPort.Item.Enabled;
			}
			catch { }

			return HRESULT.S_OK;
		}

		static HRESULT WindowsFirewallPortAdd(INetFwProfile fwProfile, int portNumber, NET_FW_IP_PROTOCOL ipProtocol, string name)
		{
			_ = fwProfile ?? throw new ArgumentNullException(nameof(fwProfile));
			_ = name ?? throw new ArgumentNullException(nameof(name));

			// First check to see if the port is already added.
			var hr = WindowsFirewallPortIsEnabled(fwProfile, portNumber, ipProtocol, out var fwPortEnabled);
			if (hr.Failed)
			{
				Console.Write("WindowsFirewallPortIsEnabled failed: {0}\n", hr);
				goto error;
			}

			// Only add the port if it isn't already added.
			if (!fwPortEnabled)
			{
				Console.Write("Adding firewall exception for {0}\n", name);
				// Retrieve the collection of globally open ports.
				using var fwOpenPorts = ComReleaserFactory.Create(fwProfile.GloballyOpenPorts);

				// Create an instance of an open port.
				using var fwOpenPort = ComReleaserFactory.Create(new INetFwOpenPort());

				// Set the port number.
				fwOpenPort.Item.Port = portNumber;

				// Set the IP protocol.
				fwOpenPort.Item.Protocol = ipProtocol;

				// Set the friendly name of the port.
				fwOpenPort.Item.Name = name;

				// Opens the port and adds it to the collection.
				fwOpenPorts.Item.Add(fwOpenPort.Item);

				hr = WindowsFirewallPortIsEnabled(fwProfile, portNumber, ipProtocol, out fwPortEnabled);
				if (hr.Failed)
				{
					Console.Write("WindowsFirewallPortIsEnabled failed: {0}\n", hr);
					goto error;
				}

				if (!fwPortEnabled)
				{
					Console.Write("ERROR: Could not enable firewall port exception, try running as administrator.\n");
				}

			}

			error:

			return hr;
		}
	}
}