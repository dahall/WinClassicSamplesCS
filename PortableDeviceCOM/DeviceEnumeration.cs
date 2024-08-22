static partial class Program
{
	const string CLIENT_NAME = "WPD Sample Application";
	const int CLIENT_MAJOR_VER = 1;
	const int CLIENT_MINOR_VER = 0;
	const int CLIENT_REVISION = 2;

	// Reads and displays the device friendly name for the specified PnPDeviceID string
	static void DisplayFriendlyName([In] IPortableDeviceManager deviceManager, string pnpDeviceID) =>
		DisplayString(deviceManager.GetDeviceFriendlyName(pnpDeviceID), "Friendly Name");

	// Reads and displays the device manufacturer for the specified PnPDeviceID string
	static void DisplayManufacturer([In] IPortableDeviceManager deviceManager, string pnpDeviceID) =>
		DisplayString(deviceManager.GetDeviceManufacturer(pnpDeviceID), "Manufacturer");

	// Reads and displays the device discription for the specified PnPDeviceID string
	static void DisplayDescription([In] IPortableDeviceManager deviceManager, string pnpDeviceID) =>
		DisplayString(deviceManager.GetDeviceDescription(pnpDeviceID), "Description");

	static void DisplayString(string? str, string msgName)
	{
		if (!string.IsNullOrEmpty(str))
			Console.Write($"{msgName}: {str}\n", str);
		else
			Console.Write($"The device did not provide a {msgName}.\n");
	}

	// Enumerates all Windows Portable Devices, displays the friendly name, manufacturer, and description of each device. This function also
	// returns the total number of devices found.
	static int EnumerateAllDevices()
	{
		// CoCreate the IPortableDeviceManager interface to enumerate portable devices and to get information about them.
		IPortableDeviceManager deviceManager = new();

		// 1) Pass default as the string array pointer to get the total number of devices found on the system.
		var pnpDeviceIDs = deviceManager.GetDevices();

		// Report the number of devices found. NOTE: we will report 0, if an error occured.

		Console.Write("\n{0} Windows Portable Device(s) found on the system\n\n", pnpDeviceIDs.Length);

		// 2) Allocate an array to hold the PnPDeviceID strings returned from the IPortableDeviceManager::GetDevices method

		// For each device found, display the devices friendly name, manufacturer, and description strings.
		for (int index = 0; index < pnpDeviceIDs.Length; index++)
		{
			Console.Write("[{0}] ", index);
			DisplayFriendlyName(deviceManager, pnpDeviceIDs[index]);
			Console.Write(" ");
			DisplayManufacturer(deviceManager, pnpDeviceIDs[index]);
			Console.Write(" ");
			DisplayDescription(deviceManager, pnpDeviceIDs[index]);
		}

		return pnpDeviceIDs.Length;
	}

	// Creates and populates an IPortableDeviceValues with information about this application. The IPortableDeviceValues is used as a
	// parameter when calling the IPortableDevice::Open() method.
	static IPortableDeviceValues? GetClientInformation()
	{
		// Client information is optional. The client can choose to identify itself, or to remain unknown to the driver. It is beneficial to
		// identify yourself because drivers may be able to optimize their behavior for known clients. (e.g. An IHV may want their bundled
		// driver to perform differently when connected to their bundled software.)

		// CoCreate an IPortableDeviceValues interface to hold the client information.
		IPortableDeviceValues clientInformation = new();

		// Attempt to set all bits of client information
		clientInformation.SetStringValue(WPD_CLIENT_NAME, CLIENT_NAME);
		clientInformation.SetUnsignedIntegerValue(WPD_CLIENT_MAJOR_VERSION, CLIENT_MAJOR_VER);
		clientInformation.SetUnsignedIntegerValue(WPD_CLIENT_MINOR_VERSION, CLIENT_MINOR_VER);
		clientInformation.SetUnsignedIntegerValue(WPD_CLIENT_REVISION, CLIENT_REVISION);

		// Some device drivers need to impersonate the caller in order to function correctly. Since our application does not need to restrict
		// its identity, specify SECURITY_IMPERSONATION so that we work with all devices.
		clientInformation.SetUnsignedIntegerValue(WPD_CLIENT_SECURITY_QUALITY_OF_SERVICE, (uint)FileFlagsAndAttributes.SECURITY_IMPERSONATION);

		return clientInformation;
	}

	// Calls EnumerateDevices() function to display devices on the system and to obtain the total number of devices found. If 1 or more
	// devices are found, this function prompts the user to choose a device using a zero-based index.
	static IPortableDevice? ChooseDevice()
	{
		// Fill out information about your application, so the device knows who they are speaking to.

		IPortableDeviceValues clientInformation = GetClientInformation()!;

		// Enumerate and display all devices.
		var pnpDeviceIDCount = EnumerateAllDevices();
		if (pnpDeviceIDCount > 0)
		{
			// Prompt user to enter an index for the device they want to choose.
			Console.Write("Enter the index of the device you wish to use.\n>");
			var selection = Console.ReadLine();

			if (!int.TryParse(selection, out var currentDeviceIndex))
				currentDeviceIndex = int.MaxValue;
			if (currentDeviceIndex >= pnpDeviceIDCount)
			{
				Console.Write("An invalid device index was specified, defaulting to the first device in the list.\n");
				currentDeviceIndex = 0;
			}

			// CoCreate the IPortableDeviceManager interface to enumerate portable devices and to get information about them.
			IPortableDeviceManager deviceManager = new();

			// Allocate an array to hold the PnPDeviceID strings returned from the IPortableDeviceManager::GetDevices method
			var pnpDeviceIDs = deviceManager.GetDevices();

			// CoCreate the IPortableDevice interface and call Open() with the chosen PnPDeviceID string.
			IPortableDevice device = new();
			try { device.Open(pnpDeviceIDs[currentDeviceIndex], clientInformation); }
			catch (UnauthorizedAccessException)
			{
				Console.Write("Failed to Open the device for Read Write access, will open it for Read-only access instead\n");
				clientInformation.SetUnsignedIntegerValue(WPD_CLIENT_DESIRED_ACCESS, ACCESS_MASK.GENERIC_READ);
				device.Open(pnpDeviceIDs[currentDeviceIndex], clientInformation);
			}
			return device;
		}

		// If no devices were found on the system, just exit this function.
		return null;
	}
}