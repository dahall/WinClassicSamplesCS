using CommandLine;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WcnApi;
using static Vanara.PInvoke.WlanApi;

namespace WindowsConnectNow;

static class Program
{
	internal const int DOT11_SSID_MAX_LENGTH = 32;
	const uint UUID_LENGTH = 36; // number of chars in a Guid string
	const  uint Pin_Length_8 = 8; //valid max wcn pin length
	const  uint Pin_Length_4 = 4; //valid min wcn pin length
	const  uint dwCharsToGenerate = 15; // 14 chars for the passsphrase xxxx-xxxx-xxxx 1 for the null terminator
	const string PassphraseCharacterSet = "012345678790abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMONPQRSTUVWXYZ";
	const  uint PASSPHRASE_MAX_LENGTH = 63; // max length for wpa2 ascii
	const  uint PASSPHRASE_MIN_LENGTH = 8; // min length for wpa2 ascii
	const  uint Discovery_Event_Wait_Time_MS = 90000;
	const  uint WINDOWS7_MAJOR_VERSION = 6;
	const  uint WINDOWS7_MINOR_VERSION = 1;

	public enum ConfigurationScenario
	{
		DeviceConfigPushButton,
		DeviceConfigPin,
		RouterConfig,
		PCConfigPushButton,
		PCConfigPin,
	};

	public class WCN_DEVICE_INFO_PARAMETERS
	{
		public string? wszDeviceName;
		public string? wszManufacturerName;
		public string? wszModelName;
		public string? wszModelNumber;
		public string? wszSerialNumber;
		public uint uConfigMethods;
	}

	public class CONFIGURATION_PARAMETERS
	{
		public WCN_PASSWORD_TYPE enumConfigType;
		public Guid? pDeviceUUID;
		public string? pDevicePin;
		public ConfigurationScenario enumConfigScenario;
		public string? pSearchSSID;
		public string? pProfilePassphrase;
		public bool bTurnOnSoftAP;
		public string? pProfileSSID;

		public CONFIGURATION_PARAMETERS(Options o)
		{
			enumConfigScenario = o.Scenario;
			enumConfigType = o.Scenario switch
			{
				ConfigurationScenario.DeviceConfigPushButton => WCN_PASSWORD_TYPE.WCN_PASSWORD_TYPE_PUSH_BUTTON,
				ConfigurationScenario.DeviceConfigPin => WCN_PASSWORD_TYPE.WCN_PASSWORD_TYPE_PIN,
				ConfigurationScenario.RouterConfig => WCN_PASSWORD_TYPE.WCN_PASSWORD_TYPE_PIN,
				ConfigurationScenario.PCConfigPushButton => WCN_PASSWORD_TYPE.WCN_PASSWORD_TYPE_PUSH_BUTTON,
				ConfigurationScenario.PCConfigPin => WCN_PASSWORD_TYPE.WCN_PASSWORD_TYPE_PIN,
				_ => throw new ArgumentException("The supplied option for Scenairo is not valid"),
			};
			if (o.Guid is not null)
				pDeviceUUID = Guid.TryParse(o.Guid, out var guid) ? guid : throw new ArgumentException("Invalid GUID format.");
			if (o.PIN is not null)
				pDevicePin = o.PIN.Length == Pin_Length_4 || o.PIN.Length == Pin_Length_8 ? o.PIN : throw new ArgumentException("Invalid PIN format.");
			if (o.SEARCHSSID is not null)
				pSearchSSID = o.SEARCHSSID.Length <= DOT11_SSID_MAX_LENGTH ? o.SEARCHSSID : throw new ArgumentException("SID is too long.");
			if (o.PROFILESSID is not null)
				pProfileSSID = o.PROFILESSID.Length <= DOT11_SSID_MAX_LENGTH + 1 ? o.PROFILESSID : throw new ArgumentException("SID is too long.");
			if (o.PROFILEPASSPHRASE is not null)
				pProfilePassphrase = o.PROFILEPASSPHRASE.Length >= PASSPHRASE_MIN_LENGTH && o.PROFILEPASSPHRASE.Length <= PASSPHRASE_MAX_LENGTH ? o.PROFILEPASSPHRASE : throw new ArgumentException("Passphrase must be between 8 and 63 characters long");
			bTurnOnSoftAP = o.Scenario == ConfigurationScenario.DeviceConfigPin || o.Scenario == ConfigurationScenario.DeviceConfigPushButton;
		}
	}

	public class Options
	{
		[Option(Required = true)]
		public ConfigurationScenario Scenario { get; set; }

		[Option]
		public string? Guid { get; set; }

		[Option]
		public string? SEARCHSSID { get; set; }

		[Option]
		public string? PIN { get; set; }

		[Option]
		public string? PROFILESSID { get; set; }

		[Option]
		public string? PROFILEPASSPHRASE { get; set; }
	}

	static HRESULT GetWCNDeviceInformation(IWCNDevice pDevice, WCN_DEVICE_INFO_PARAMETERS pWCNDeviceInformation)
	{
		HRESULT hr = Win32Error.ERROR_SUCCESS;

		//A WCN device can have a variety of attributes. (These attributes generally correspond
		//to TLVs in the WPS specification, although not all WPS TLVs are available as WCN attributes).
		//You can use the IWCNDevice::Get*Attribute to read these attributes. Not all devices send 
		//all attributes -- if the device did not send a particular attribute, the Get*Attribute API 
		//will return HRESULT _FROM_WIN32(ERROR_NOT_FOUND).
		//
		//This sample demonstrates how to get the most common attributes that would be useful for
		//displaying in a user interface.

		//
		// WCN_TYPE_DEVICE_NAME
		//

		//The IWCNDevice::GetStringAttribute method gets a cached attribute from the device as a string.
		hr = GetStringAttribute(WCN_ATTRIBUTE_TYPE.WCN_TYPE_DEVICE_NAME, out pWCNDeviceInformation.wszDeviceName);
		if (hr != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: Failed to get the Device Name from the IWCNDevice instance. hr=[0x{0:X}]", (uint)hr);
			goto cleanup;
		}

		Console.Write("\nINFO: Device Name: [{0}]", pWCNDeviceInformation.wszDeviceName);

		//
		// WCN_TYPE_MANUFACTURER
		//

		hr = GetStringAttribute(WCN_ATTRIBUTE_TYPE.WCN_TYPE_MANUFACTURER, out pWCNDeviceInformation.wszManufacturerName);
		if (hr != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: Failed to get the device manufacturer from the ICWNDevice instance, hr=[0x{0:X}]", (uint)hr);
			goto cleanup;
		}

		Console.Write("\nINFO: Manufacturer Name: [{0}]", pWCNDeviceInformation.wszManufacturerName);

		HRESULT GetStringAttribute(WCN_ATTRIBUTE_TYPE at, out string? str) => FunctionHelper.CallMethodWithStrBuf((StringBuilder? sb, ref uint s) => pDevice.GetStringAttribute(at, s, sb!), out str);

		//
		// WCN_TYPE_MODEL_NAME
		//

		hr = GetStringAttribute(WCN_ATTRIBUTE_TYPE.WCN_TYPE_MODEL_NAME, out pWCNDeviceInformation.wszModelName);
		if (hr != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: Failed to get the device model name from the ICWNDevice instance, hr=[0x{0:X}]", (uint)hr);
			goto cleanup;
		}

		Console.Write("\nINFO: Model Name: [{0}]", pWCNDeviceInformation.wszModelName);

		//
		// WCN_TYPE_MODEL_NUMBER
		// Note that the Model Number is actually a string. Most devices have alpha-numeric
		// model numbers, like "AB1234CD".

		hr = GetStringAttribute(WCN_ATTRIBUTE_TYPE.WCN_TYPE_MODEL_NUMBER, out pWCNDeviceInformation.wszModelNumber);
		if (hr != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: Failed to get the device model name from the ICWNDevice instance, hr=[0x{0:X}]", (uint)hr);
			goto cleanup;
		}

		Console.Write("\nINFO: Model Number: [{0}]", pWCNDeviceInformation.wszModelNumber);

		//
		// WCN_TYPE_SERIAL_NUMBER
		// Note that the Serial Number is actually a string. Some devices send strings that
		// aren't meaningful, like "(none)" or just the empty string.

		hr = GetStringAttribute(WCN_ATTRIBUTE_TYPE.WCN_TYPE_SERIAL_NUMBER, out pWCNDeviceInformation.wszSerialNumber);
		if (hr != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: Failed to get the device model name from the ICWNDevice instance, hr=[0x{0:X}]", (uint)hr);
			goto cleanup;
		}

		Console.Write("\nINFO: Serial Number: [{0}]", pWCNDeviceInformation.wszSerialNumber);

		//
		// WCN_TYPE_CONFIG_METHODS
		// This is a bit mask of the values from WCN_VALUE_TYPE_CONFIG_METHODS.
		// For example, a devices indicates support for pushbutton if its Config
		// Methods value includes the WCN_VALUE_CM_PUSHBUTTON flag.

		//The GetIntegerAttribute method gets a cached attribute from the device as an integer.
		hr = pDevice.GetIntegerAttribute(WCN_ATTRIBUTE_TYPE.WCN_TYPE_CONFIG_METHODS, out pWCNDeviceInformation.uConfigMethods);
		if (hr != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: Failed to get the device model name from the ICWNDevice instance, hr=[0x{0:X}]", (uint)hr);
			goto cleanup;
		}

		cleanup:
		return hr;
	}

	static HRESULT RunScenario(CONFIGURATION_PARAMETERS configParams)
	{
		//common declarations
		int pinLen = (int)Pin_Length_8;

		//pin needs to be a null terminated ascii byte[] for the IWCNDevice::SetPassword function 
		var pin = new StringBuilder(pinLen + 1);

		//Wlan variable declarations
		StringBuilder? profileBuffer = null;
		WCN_DEVICE_INFO_PARAMETERS WCNDeviceInformation = new();

		//The following wlan profile xml is used to configure an unconfigured WCN enabled Router or device.
		//See http://msdn.microsoft.com/en-us/library/bb525370(VS.85).aspx on how to generate a wlan profile.
		//Alternatively, you can read an existing network profile by calling WlanGetProfile.
		const string WCNConnectionProfileTemplate =
			"<?xml version=\"1.0\" ?>" +
			"" +
			"<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
			" <name>{0}</name>" +
			"" +
			" <SSIDConfig>" +
			" <SSID>" +
			" <name>{1}</name>" +
			" </SSID>" +
			" </SSIDConfig>" +
			" " +
			" <connectionType>ESS</connectionType>" +
			" <connectionMode>auto</connectionMode>" +
			"" +
			" <MSM>" +
			" <security>" +
			" <authEncryption>" +
			" <authentication>WPA2PSK</authentication>" +
			" <encryption>AES</encryption>" +
			" </authEncryption>" +
			"" +
			"" +
			" <sharedKey>" +
			" <keyType>passPhrase</keyType>" +
			" <protected>false</protected>" +
			" <keyMaterial>{2}</keyMaterial>" +
			" </sharedKey>" +
			"" +
			" </security>" +
			" </MSM>" +
			"</WLANProfile>";

		//open a wlan handle - this will be used later for saving the profile to the system
		var status = WlanOpenHandle(WLAN_API_VERSION_2_0, default, out var _, out var wlanHandle);
		HRESULT hr;
		if (status != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: WlanOpenHandle failed with the following error code [{0}]", status);
			hr = HRESULT.S_FALSE;
			goto cleanup;
		}

		// Get the first wlan device
		// ideally you would want to be able to choose the wireless device you want to use
		status = WlanEnumInterfaces(wlanHandle, default, out var pInterfaceList);
		if (status != Win32Error.ERROR_SUCCESS)
		{
			Console.Write("\nERROR: WlanEnumInterfaces failed with the following error code [0x{0:X}]", (uint)status);
			hr = HRESULT.S_FALSE;
			goto cleanup;
		}

		//Make sure there is at least one wlan interface on the system
		if (pInterfaceList.dwNumberOfItems == 0)
		{
			Console.Write("\nERROR: No wireless network adapters on the system");
			hr = HRESULT.S_FALSE;
			goto cleanup;
		}

		//get the wlan interface Guid
		var interfaceGuid = pInterfaceList.InterfaceInfo[0].InterfaceGuid;

		//Create an instance of the IWCNConnectNotify Interface
		var pWcnConNotif = new WcnConnectNotification();

		var wcnFdDiscoveryNotify = new CWcnFdDiscoveryNotify();

		//initialize WcnConnectNotification
		hr = pWcnConNotif.Init();
		if (hr != HRESULT.S_OK)
		{
			Console.Write("\nERROR: Creating a connection notification event failed with the following error hr=[{0}]", hr);
			goto cleanup;
		}

		//initialize CWcnFdDiscoveryNotify 
		hr = wcnFdDiscoveryNotify.Init(configParams.bTurnOnSoftAP);
		if (hr != HRESULT.S_OK)
		{
			Console.Write("\nERROR: Initializing Function Discovery notify failed with the following error hr=[{0}].", hr);
			goto cleanup;
		}

		//Search for WCN device with function discovery
		hr = wcnFdDiscoveryNotify.WcnFDSearchStart(configParams.pDeviceUUID, configParams.pSearchSSID);
		if (hr != HRESULT.S_OK)
		{
			Console.Write("\nERROR: Function Discovery search failed to start with the following error hr=[{0}].", hr);
			goto cleanup;
		}

		//Wait for Function Discovery to complete
		wcnFdDiscoveryNotify.WaitForAnyDiscoveryEvent(Discovery_Event_Wait_Time_MS);

		//Attempt to get the IWCNDevice instance
		if (wcnFdDiscoveryNotify.GetWCNDeviceInstance(out var pDevice))
		{
			//get information about the device from the IWCNDevice instance
			Console.Write("\nINFO: The following Device was found by Function Discovery.");
			hr = GetWCNDeviceInformation(pDevice!, WCNDeviceInformation);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: Failed to get the Device information from the IWCNDevice Instance, hr=[{0}]", hr);
				goto cleanup;
			}
		}
		else
		{
			Console.Write("\nERROR: Device was NOT found by Function Discovery.");
			hr = HRESULT.S_FALSE;
			goto cleanup;
		}

		//The following segment generates a WLAN profile from the template above then saves it to the
		//WLAN store. It the retrieves the profile from the WLAN store for use in configuring a router
		//or device.
		if (configParams.enumConfigScenario != ConfigurationScenario.PCConfigPin && configParams.enumConfigScenario != ConfigurationScenario.PCConfigPushButton)
		{
			//add the profiles ssid and passphrase to the wlan profile template
			profileBuffer = new StringBuilder(string.Format(WCNConnectionProfileTemplate, configParams.pProfileSSID, configParams.pProfileSSID, configParams.pProfilePassphrase));

			//Add the created profile to the wlan store
			status = WlanSetProfile(wlanHandle,
									interfaceGuid,
									0, //all-user profile
									profileBuffer.ToString(),
									default, // Default Security - All user profile
									true, // Overwrite profile
									default, // reserved
									out var wlanResult);

			if (status != Win32Error.ERROR_SUCCESS)
			{
				Console.Write("\nERROR: Failed to save the profile return code was [{0}]", wlanResult);
				hr = HRESULT.S_FALSE;
				goto cleanup;
			}
			else
			{
				Console.Write("\nINFO: Successfully saved the profile to the wlan store");
			}

			//Here is where the profile is retrieved from the wlan store to be used in the configuration
			//of the device. 
			//If so desired a list of available profiles could be presented to the user so that 
			//they could decied which profile will be used to configure the device
			//The wlan profile must be retrieved in plain text inorder for the IWCNDEVICE::SetNetWorkProfile
			// method to succeede. In order to do this you need to be elevated to get the wlan profile
			// in plain text.
			WLAN_PROFILE_FLAGS dwFlags = WLAN_PROFILE_FLAGS.WLAN_PROFILE_GET_PLAINTEXT_KEY;
			status = WlanGetProfile(wlanHandle,
									interfaceGuid,
									configParams.pProfileSSID!,
									default, //reserved
									out var pWlanProfileXml,
									ref dwFlags, // Flags - get profile in plain text 
									out _); // GrantedAccess - none

			if (status != Win32Error.ERROR_SUCCESS)
			{
				Console.Write("\nERROR: WlanGetprofile Failed to get profile [{0}] with error code [{1}]", configParams.pProfileSSID, status);
				hr = HRESULT.S_FALSE;
				goto cleanup;
			}
			else
			{
				Console.Write("\nINFO: Successfully retrieved profile [{0}] from the wlan store.", configParams.pProfileSSID);
			}

			//check to make sure the profile from the wlan store is not a Group Policy profile
			if (dwFlags.IsFlagSet(WLAN_PROFILE_FLAGS.WLAN_PROFILE_GROUP_POLICY))
			{
				Console.Write("\nERROR: Profile [{0}] is a group policy WLAN profile which is not supported by WCN", configParams.pProfileSSID);
				hr = HRESULT.S_FALSE;
				goto cleanup;
			}

			//The IWCNDevice::SetNetworkProfile method queues an XML WLAN profile to be 
			//provisioned to the device. This method may only be called prior to IWCNDevice::Connect.
			hr = pDevice!.SetNetworkProfile(pWlanProfileXml);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: IWCNDevice::SetNetworkProfile failed with error code [{0}]", hr);
				goto cleanup;
			}
			else
			{
				Console.Write("\nINFO: IWCNDevice::SetNetworkProfile() succeeded with result [{0}]", hr);
			}
		}

		switch (configParams.enumConfigScenario)
		{
			case ConfigurationScenario.DeviceConfigPushButton:
				pinLen = 0;
				break;

			case ConfigurationScenario.DeviceConfigPin:
			case ConfigurationScenario.RouterConfig:
				if (configParams.pDevicePin is null)
				{
					Console.Write("\nERROR: Pin must not be 0 when doing a pin configuration");
					hr = HRESULT.S_FALSE;
					goto cleanup;
				}

				var result = WideCharToMultiByte(CP_UTF8, 0, configParams.pDevicePin, -1, pin, pin.Capacity);
				if (result == 0)
				{
					Console.Write("\nERROR: Failed to convert the pin to multibyte.");
					goto cleanup;
				}

				pinLen = pin.Length;
				break;

			case ConfigurationScenario.PCConfigPushButton:
				//check to make sure the device supports push button before doing the push button configuration
				if ((WCNDeviceInformation.uConfigMethods & (uint)WCN_VALUE_TYPE_CONFIG_METHODS.WCN_VALUE_CM_PUSHBUTTON) != 0)
				{
					//set the pin length to 0 this is necessary for a Push button configuration scenario				
					pinLen = 0;
				}
				else
				{
					Console.Write("ERROR: The [{0}] device does not support the Push Button Method", WCNDeviceInformation.wszDeviceName);
					hr = HRESULT.S_FALSE;
					goto cleanup;
				}
				break;

			case ConfigurationScenario.PCConfigPin:
				//check to make sure the device supports pin before doing the pin configuration
				if ((WCNDeviceInformation.uConfigMethods & (uint)(WCN_VALUE_TYPE_CONFIG_METHODS.WCN_VALUE_CM_LABEL | WCN_VALUE_TYPE_CONFIG_METHODS.WCN_VALUE_CM_DISPLAY)) != 0)
				{
					if (configParams.pDevicePin is null)
					{
						Console.Write("\nERROR: Pin must not be 0 when doing a pin configuration");
						hr = HRESULT.S_FALSE;
						goto cleanup;
					}

					result = WideCharToMultiByte(CP_UTF8, //CodePage
								0, //Unmapped character flags
								configParams.pDevicePin,
								-1, //null terminated string
								pin,
								pin.Capacity);
					if (result == 0)
					{
						Console.Write("\nERROR: Failed to convert the pin to multibyte.");
						goto cleanup;
					}

					pinLen = pin.Length;

				}
				else
				{
					Console.Write("\nERROR: The [{0}] device does not supprot the pin method", WCNDeviceInformation.wszDeviceName);
					hr = HRESULT.S_FALSE;
					goto cleanup;
				}
				break;

			default:
				break;
		}

		//The IWCNDevice::SetPassword method configures the authentication method value, and if required, 
		//a password used for the pending session. This method may only be called prior to IWCNDevice::Connect.
		hr = pDevice!.SetPassword(configParams.enumConfigType, (uint)pinLen, StringHelper.GetBytes(pin.ToString(), false, CharSet.Ansi));

		if (hr != HRESULT.S_OK)
		{
			Console.Write("\nERROR: IWCNDevice::SetPassword failed with error code [{0}]", hr);
			goto cleanup;
		}
		else
		{
			Console.Write("\nINFO: IWCNDevice::SetPassword succeeded with result [{0}]", hr);
		}

		//The IWCNDevice::Connect method initiates the session.
		hr = pDevice.Connect(pWcnConNotif);
		if (hr != HRESULT.S_OK)
		{
			//Device Push button configuration is only supported on SoftAP capable wireless Nics 
			if (hr == Win32Error.ERROR_CONNECTION_UNAVAIL && configParams.enumConfigScenario == ConfigurationScenario.DeviceConfigPushButton)
			{
				Console.Write("\nERROR: PushButton Configuration of non AP devices is only supported on");
				Console.Write("\n SoftAP capable wireless network cards.");
			}
			else
			{
				Console.Write("\nERROR: IWCNDevice::Connect failed with error code [{0}]", hr);
			}
			goto cleanup;
		}
		else
		{
			Console.Write("\nINFO: IWCNDevice::Connect succeeded with result [{0}]", hr);
		}

		//wait for the configuration result
		hr = pWcnConNotif.WaitForConnectionResult();
		if (hr != HRESULT.S_OK)
		{
			Console.Write("ERROR: WaitforconnectionResult returned the following error [{0}]", hr);
			goto cleanup;
		}

		//check to see which connection callbacks were called
		if (pWcnConNotif.connectSucceededCallBackInvoked)
		{
			Console.Write("\nINFO: IWCNConnectNotify::ConnectSucceeded was invoked");
		}
		else if (pWcnConNotif.connectFailedCallBackInvoked)
		{
			Console.Write("\nERROR: IWCNConnectNotify::ConnectFailed was invoked");
			hr = HRESULT.S_FALSE;
			goto cleanup;
		}

		//save the profile from the IWCNDevice instance to the WLAN store if doing a PCConfigPushButton 
		//or a PCConfigPin scenario

		// this is the profile that was received from the router
		if (configParams.enumConfigScenario == ConfigurationScenario.PCConfigPushButton || configParams.enumConfigScenario == ConfigurationScenario.PCConfigPin)
		{
			//The IWCNDevice::GetNetworkProfile method gets a network profile from the device.
			hr = pDevice.GetNetworkProfile((uint)profileBuffer!.Capacity, profileBuffer);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: IWCNDevice::GetNetworkProfile failed with [{0}]", hr);
				goto cleanup;
			}

			//save the profile to the system if doing a RouterConfig or a pushbutton scenario
			//The SoftapConfig and DeviceConfig scenarios will generally use a profile that is already on the system
			//save the profile to the wlan interface			
			status = WlanSetProfile(wlanHandle,
									interfaceGuid,
									0, //Flags - none
									profileBuffer.ToString(),
									default, // Default Security - All user profile
									true, // Overwrite profile
									default, // reserved
									out var wlanResult);

			if (status != Win32Error.ERROR_SUCCESS)
			{
				Console.Write("\nERROR: Failed to save the profile to the WLAN store, return code was [{0}]", wlanResult);
				hr = HRESULT.S_FALSE;
			}
			else
			{
				Console.Write("\nINFO: Successfully saved the profile to the WLAN store");
			}
		}

		//Display the SSID and passphrase used to configure the Router or device
		if (configParams.enumConfigScenario != ConfigurationScenario.PCConfigPin && configParams.enumConfigScenario != ConfigurationScenario.PCConfigPushButton)
		{
			Console.Write("\nINFO: Profile SSID Used: [{0}]", configParams.pProfileSSID);
			Console.Write("\nINFO: Profile Passphrase Used: [{0}]", configParams.pProfilePassphrase);
		}

		cleanup:

		return hr;
	}

	static void printUsage()
	{
		Console.Write("\nUSAGE:");
		Console.Write("\n WCNConfigure.exe");
		Console.Write("\n Scenario=[DeviceConfigPin | DeviceConfigPushButton | RouterConfig |");
		Console.Write("\n PCConfigPushButton | PCConfigPin ]");
		Console.Write("\n [Guid=<uuid of device> | SEARCHSSID=<ssid of device to find>]");
		Console.Write("\n [PIN=<pin of device>]");
		Console.Write("\n [PROFILESSID=<ssid to use in profile>]");
		Console.Write("\n [PROFILEPASSPHRASE=<passphrase to use in profile>]");
		Console.Write("\n");
		Console.Write("\nParameters:");
		Console.Write("\n Scenario - choose the operation you wish to perform ");
		Console.Write("\n DeviceConfigPushButton - Configure a WCN enabled device, such as a picture");
		Console.Write("\n frame using the button on the device");
		Console.Write("\n DeviceConfigPin - Configure a WCN enabled device, such as a picture frame");
		Console.Write("\n using the device supplied pin");
		Console.Write("\n RouterConfig - Configure a WCN enabled Wireless Router");
		Console.Write("\n PCConfigPushButton - Get the wireless profile from a WCN enabled router");
		Console.Write("\n using the Push Button on the device.");
		Console.Write("\n PCConfigPin - Get the wireless profile from a WCN enabled rotuer using the");
		Console.Write("\n supplied pin.");
		Console.Write("\n");
		Console.Write("\n Guid - Enter a device Guid in the following format xxxx-xxxx-xxxx-xxxxxxxxxxxx");
		Console.Write("\n Guid is necessary for the DeviceConfigPushButton and DeviceConfigPin");
		Console.Write("\n scenarios. Use either Guid or SEARCHSSID for the RouterConfig, PCConfigPin");
		Console.Write("\n and PCConfigPushButton scenarios.");
		Console.Write("\n");
		Console.Write("\n SEARCHSSID - Enter in the SSID for the Router you are looking to configure.");
		Console.Write("\n SEARCHSSID is only valid in the RouterConfig, PCConfigPushButton and ");
		Console.Write("\n PCConfigPin scenarios. Use either Guid or SEARCHSSID for the these");
		Console.Write("\n scenarios. NOTE: Using SSID will return the first device");
		Console.Write("\n found with that ssid. If there is more than one device with the");
		Console.Write("\n same ssid use the Guid instead");
		Console.Write("\n");
		Console.Write("\n PIN - Enter the pin of the device");
		Console.Write("\n PIN is only valid when using the RouterConfig and DeviceConfigPIN");
		Console.Write("\n Scenarios.");
		Console.Write("\n");
		Console.Write("\n PROFILESSID - When present this SSID will be used in the WLAN profile that is");
		Console.Write("\n pushed to the router/device otherwise a default SSID of WCNSSID ");
		Console.Write("\n will be used");
		Console.Write("\n");
		Console.Write("\n PROFILEPASSPHRASE - when present this passphrase will be used in the wlan");
		Console.Write("\n profile that is pushed to the router/device. Otherwise, a");
		Console.Write("\n random default passphrase will be used\n\n");
	}

	static bool validateParameters(CONFIGURATION_PARAMETERS configParams)
	{
		bool bReturnValue = false;

		switch (configParams.enumConfigScenario)
		{
			//DeviceConfig and RouterConfig require both the uuid and the device pin
			case ConfigurationScenario.DeviceConfigPin:
				if (configParams.pDeviceUUID != Guid.Empty && configParams.pDevicePin is not null)
				{
					bReturnValue = true;
				}
				break;

			case ConfigurationScenario.DeviceConfigPushButton:
				if (configParams.pDeviceUUID != Guid.Empty)
				{
					bReturnValue = true;
				}
				break;

			case ConfigurationScenario.RouterConfig:
				//uuid or searchssid must be present in order to continue
				if ((configParams.pDeviceUUID != Guid.Empty || configParams.pSearchSSID is not null)
					&& configParams.pDevicePin is not null)
				{
					bReturnValue = true;
				}
				break;

			case ConfigurationScenario.PCConfigPushButton:
				if (configParams.pDeviceUUID != Guid.Empty || configParams.pSearchSSID is not null)
				{
					bReturnValue = true;
				}
				break;

			case ConfigurationScenario.PCConfigPin:
				if ((configParams.pDeviceUUID != Guid.Empty || configParams.pSearchSSID is not null)
					&& configParams.pDevicePin is not null)
				{
					bReturnValue = true;
				}
				break;

			default:
				break;
		}

		return bReturnValue;
	}

	[MTAThread]
	static int Main(string[] args)
	{
		HRESULT hr = Win32Error.ERROR_SUCCESS;
		HRESULT status = Win32Error.ERROR_SUCCESS;
		uint dwVersion;
		uint dwMajorVersion;
		uint dwMinorVersion;

		//check to make sure we are running on Windows 7 or later
		dwVersion = GetVersion();
		dwMajorVersion = Macros.LOBYTE(Macros.LOWORD(dwVersion));
		dwMinorVersion = Macros.HIBYTE(Macros.LOWORD(dwVersion));

		//dwMajorVersion must be 6 or greater and dwMinorVersion must be 1 or greater (Vista is 6.0)
		if (dwMajorVersion <= WINDOWS7_MAJOR_VERSION)
		{
			if ((dwMajorVersion == WINDOWS7_MAJOR_VERSION && dwMinorVersion < WINDOWS7_MINOR_VERSION)
				|| dwMajorVersion < WINDOWS7_MAJOR_VERSION)
			{
				Console.Write("\nERROR: This Application requires Windows 7 or later\n\n");
				goto cleanup;
			}

		}

		//get the parameters from the command line
		Parser.Default.ParseArguments<Options>(args).WithNotParsed(o => printUsage()).WithParsed(opts =>
		{
			CONFIGURATION_PARAMETERS configParameters;
			try
			{
				if (!validateParameters(configParameters = new CONFIGURATION_PARAMETERS(opts)))
				{
					printUsage();
					Console.Write("\nERROR: Invalid parameters");
					return;
				}
			}
			catch (Exception ex)
			{
				Console.Write("ERROR: " + ex.Message);
				return;
			}

			//select the scenario you wish to run
			switch (configParameters.enumConfigScenario)
			{
				//configure a wireless device using the device supplied pin code
				case ConfigurationScenario.DeviceConfigPin:
					status = RunScenario(configParameters);

					if (status != Win32Error.ERROR_SUCCESS)
					{
						Console.Write("\nERROR: Configuration of the wireless Device with a PIN Failed\n\n");
					}
					else
					{
						Console.Write("\nINFO: Configuration of the wireless Device with a PIN Succeeded\n\n");
					}
					break;

				//configure a wireless device with by pushing the configuration button on the device 
				case ConfigurationScenario.DeviceConfigPushButton:
					status = RunScenario(configParameters);
					if (status != Win32Error.ERROR_SUCCESS)
					{
						Console.Write("\nERROR: Configuration of the Wireless device with push button Failed\n\n");
					}
					else
					{
						Console.Write("\nINFO: Configuration of the Wireless device with push button Succeeded\n\n");
					}
					break;

				//configure a router using the router supplied pin code
				case ConfigurationScenario.RouterConfig:
					status = RunScenario(configParameters);
					if (status != Win32Error.ERROR_SUCCESS)
					{
						Console.Write("\nERROR: Configuration of the Wireless Router Failed\n\n");
					}
					else
					{
						Console.Write("\nINFO: Configuration of the Wireless Router Succeeded\n\n");
					}
					break;

				// get the wireless profile from the router using the configuration button on the router
				case ConfigurationScenario.PCConfigPushButton:
					Console.Write("\n\nINFO: Please push the 'WCN Configure Button' on the router and then hit enter.\n");
					if (Console.ReadKey().KeyChar == '\n')
					{
						Console.Write("\nINFO: Attempting to get the Wireless profile from the router");
					}

					status = RunScenario(configParameters);
					if (status != Win32Error.ERROR_SUCCESS)
					{
						Console.Write("\nERROR: PC Configuration with the Push Button failed\n\n");
					}
					else
					{
						Console.Write("\nINFO: PC Configuration with the Push Button succeeded\n\n");
					}
					break;

				case ConfigurationScenario.PCConfigPin:
					status = RunScenario(configParameters);
					if (status != Win32Error.ERROR_SUCCESS)
					{
						Console.Write("\nERROR: PC Configuration with a pin Failed\n\n");
					}
					else
					{
						Console.Write("\nINFO: PC Configuration with a pin Succeeded\n\n");
					}
					break;

				default:
					break;
			}

		});

		cleanup:

		return 0;
	}
}
