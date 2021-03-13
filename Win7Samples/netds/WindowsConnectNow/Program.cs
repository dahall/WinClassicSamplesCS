using CommandLine;
using System;
using Vanara.PInvoke;
using Vanara.Extensions;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WcnApi;
using static Vanara.PInvoke.WlanApi;
using System.Text;

namespace WindowsConnectNow
{
	static class Program
	{
		private const int DOT11_SSID_MAX_LENGTH = 32;
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
			public string wszDeviceName;
			public string wszManufacturerName;
			public string wszModelName;
			public string wszModelNumber;
			public string wszSerialNumber;
			public uint uConfigMethods;
		}

		public class CONFIGURATION_PARAMETERS
		{
			public WCN_PASSWORD_TYPE enumConfigType;
			public Guid? pDeviceUUID;
			public string pDevicePin;
			public ConfigurationScenario enumConfigScenario;
			public string pSearchSSID;
			public string pProfilePassphrase;
			public bool bTurnOnSoftAP;
			public string pProfileSSID;

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
			public string Guid { get; set; }

			[Option]
			public string SEARCHSSID { get; set; }

			[Option]
			public string PIN { get; set; }

			[Option]
			public string PROFILESSID { get; set; }

			[Option]
			public string PROFILEPASSPHRASE { get; set; }
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

			HRESULT GetStringAttribute(WCN_ATTRIBUTE_TYPE at, out string str) => FunctionHelper.CallMethodWithStrBuf((StringBuilder sb, ref uint s) => pDevice.GetStringAttribute(at, s, sb), out str);

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
			uint status = Win32Error.ERROR_SUCCESS;
			HRESULT hr = HRESULT.S_OK;
			uint pinLen = Pin_Length_8;

			//pin needs to be a null terminated ascii byte[] for the IWCNDevice::SetPassword function 
			byte pin[Pin_Length_8 + 1] = { 0 };


			int result;

			//WCN declarations
			CComPtr<IWCNDevice> pDevice;
			CComObject<WcnConnectNotification>* pWcnConNotif = default;
			CComObject<CWcnFdDiscoveryNotify>* wcnFdDiscoveryNotify = default;

			//Wlan variable declarations
			ushort profileBuffer[WCN_API_MAX_BUFFER_SIZE] = { 0 };
			HANDLE wlanHandle;
			uint negVersion;
			Guid interfaceGuid = { 0 };
			WLAN_INTERFACE_INFO_LIST* pInterfaceList;
			uint wlanResult;
			WLAN_CONNECTION_PARAMETERS connParams;
			ZeroMemory(&connParams, sizeof(connParams));
			WCN_DEVICE_INFO_PARAMETERS WCNDeviceInformation;
			StringBuilder pWlanProfileXml = default;
			uint dwFlags = WLAN_PROFILE_GET_PLAINTEXT_KEY;


			//The following wlan profile xml is used to configure an unconfigured WCN enabled Router or device.
			//See http://msdn.microsoft.com/en-us/library/bb525370(VS.85).aspx on how to generate a wlan profile.
			//Alternatively, you can read an existing network profile by calling WlanGetProfile.
			const string WCNConnectionProfileTemplate =
				"<?xml version=\"1.0\" ?>" +
				"" +
				"<WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\">" +
				" <name>%s</name>" +
				"" +
				" <SSIDConfig>" +
				" <SSID>" +
				" <name>%s</name>" +
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
				" <keyMaterial>%s</keyMaterial>" +
				" </sharedKey>" +
				"" +
				" </security>" +
				" </MSM>" +
				"</WLANProfile>";


			std::wstring profileXML;

			//open a wlan handle - this will be used later for saving the profile to the system
			status = WlanOpenHandle(WLAN_API_VERSION_2_0,
									default,
									&negVersion,
									&wlanHandle);

			if (status != Win32Error.ERROR_SUCCESS)
			{
				Console.Write("\nERROR: WlanOpenHandle failed with the following error code [%d]", status);
				hr = HRESULT.S_FALSE;
				goto cleanup;
			}

			// Get the first wlan device
			// ideally you would want to be able to choose the wireless device you want to use
			status = WlanEnumInterfaces(wlanHandle,
										default,
										&pInterfaceList);

			if (status != Win32Error.ERROR_SUCCESS)
			{
				Console.Write("\nERROR: WlanEnumInterfaces failed with the following error code [0x%d]", status);
				hr = HRESULT.S_FALSE;
				goto cleanup;
			}

			//Make sure there is at least one wlan interface on the system
			if (pInterfaceList == 0 || pInterfaceList.dwNumberOfItems == 0)
			{
				Console.Write("\nERROR: No wireless network adapters on the system");
				hr = HRESULT.S_FALSE;
				goto cleanup;
			}

			//get the wlan interface Guid
			interfaceGuid = pInterfaceList.InterfaceInfo[0].InterfaceGuid;

			//Create an instance of the IWCNConnectNotify Interface
			hr = CComObject < WcnConnectNotification >::CreateInstance(&pWcnConNotif);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: Creating an instance of WcnConnectNotification failed with the following error hr=[0x%x]", hr);
				goto cleanup;
			}
			pWcnConNotif.AddRef();

			hr = CComObject < CWcnFdDiscoveryNotify >::CreateInstance(&wcnFdDiscoveryNotify);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: Creating an instance of CWcnFdDiscoveryNotify failed with the following error hr=[0x%x]", hr);
				goto cleanup;
			}
			wcnFdDiscoveryNotify.AddRef();

			//initialize WcnConnectNotification
			hr = pWcnConNotif.Init();
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: Creating a connection notification event failed with the following error hr=[0x%x]", hr);
				goto cleanup;
			}

			//initialize CWcnFdDiscoveryNotify 
			hr = wcnFdDiscoveryNotify.Init(configParams.bTurnOnSoftAP);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: Initializing Function Discovery notify failed with the following error hr=[0x%x].", hr);
				goto cleanup;
			}

			//Search for WCN device with function discovery
			hr = wcnFdDiscoveryNotify.WcnFDSearchStart(&configParams.pDeviceUUID, configParams.pSearchSSID);
			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: Function Discovery search failed to start with the following error hr=[0x%x].", hr);
				goto cleanup;
			}

			//Wait for Function Discovery to complete
			wcnFdDiscoveryNotify.WaitForAnyDiscoveryEvent(Discovery_Event_Wait_Time_MS);

			//Attempt to get the IWCNDevice instance
			if (wcnFdDiscoveryNotify.GetWCNDeviceInstance(&pDevice))
			{
				//get information about the device from the IWCNDevice instance
				Console.Write("\nINFO: The following Device was found by Function Discovery.");
				hr = GetWCNDeviceInformation(pDevice, &WCNDeviceInformation);
				if (hr != HRESULT.S_OK)
				{
					Console.Write("\nERROR: Failed to get the Device information from the IWCNDevice Instance, hr=[0x%x]", hr);
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
			if (configParams.enumConfigScenario != PCConfigPin
				&& configParams.enumConfigScenario != PCConfigPushButton)
			{
				//add the profiles ssid and passphrase to the wlan profile template
				swprintf_s(profileBuffer,
						WCNConnectionProfileTemplate,
						configParams.pProfileSSID,
						configParams.pProfileSSID,
						configParams.pProfilePassphrase);


				//Add the created profile to the wlan store
				status = WlanSetProfile(wlanHandle,
										&interfaceGuid,
										0, //all-user profile
										profileBuffer,
										default, // Default Security - All user profile
										true, // Overwrite profile
										default, // reserved
										&wlanResult);

				if (status != Win32Error.ERROR_SUCCESS)
				{
					Console.Write("\nERROR: Failed to save the profile return code was [0x%x]", wlanResult);
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
				status = WlanGetProfile(wlanHandle,
										&interfaceGuid,
										configParams.pProfileSSID,
										default, //reserved
										&pWlanProfileXml,
										&dwFlags, // Flags - get profile in plain text 
										default); // GrantedAccess - none

				if (status != Win32Error.ERROR_SUCCESS)
				{
					Console.Write("\nERROR: WlanGetprofile Failed to get profile [%s] with error code [0x%x]", configParams.pProfileSSID, status);
					hr = HRESULT.S_FALSE;
					goto cleanup;
				}
				else
				{
					Console.Write("\nINFO: Successfully retrieved profile [%s] from the wlan store.", configParams.pProfileSSID);
				}

				//check to make sure the profile from the wlan store is not a Group Policy profile
				if (WLAN_PROFILE_GROUP_POLICY & dwFlags)
				{
					Console.Write("\nERROR: Profile [%s] is a group policy WLAN profile which is not supported by WCN", configParams.pProfileSSID);
					hr = HRESULT.S_FALSE;
					goto cleanup;
				}


				//The IWCNDevice::SetNetworkProfile method queues an XML WLAN profile to be 
				//provisioned to the device. This method may only be called prior to IWCNDevice::Connect.
				hr = pDevice.SetNetworkProfile(pWlanProfileXml);
				if (hr != HRESULT.S_OK)
				{
					Console.Write("\nERROR: IWCNDevice::SetNetworkProfile failed with error code [0x%x]", hr);
					goto cleanup;
				}
				else
				{
					Console.Write("\nINFO: IWCNDevice::SetNetworkProfile() succeeded with result [0x%x]", hr);
				}
			}

			switch (configParams.enumConfigScenario)
			{
				case DeviceConfigPushButton:

					pinLen;
					break;

				case DeviceConfigPin:
				case RouterConfig:
					if (configParams.pDevicePin == 0)
					{
						Console.Write("\nERROR: Pin must not be 0 when doing a pin configuration");
						hr = HRESULT.S_FALSE;
						goto cleanup;
					}


					result = WideCharToMultiByte(CP_UTF8,
								 0,
								 configParams.pDevicePin,
								 -1,
								 ([MarshalAs(UnmanagedType.LPStr)] StringBuilder)pin,
								 sizeof(pin),
								 default,
								 default);
					if (result == 0)
					{
						Console.Write("\nERROR: Failed to convert the pin to multibyte.");
						goto cleanup;
					}


					pinLen = sizeof(pin) - 1;
					break;

				case PCConfigPushButton:
					//check to make sure the device supports push button before doing the push button configuration
					if (WCNDeviceInformation.uConfigMethods & WCN_VALUE_CM_PUSHBUTTON)
					{
						//set the pin length to 0 this is necessary for a Push button configuration scenario				
						pinLen;
					}
					else
					{
						Console.Write("ERROR: The [%s] device does not support the Push Button Method", WCNDeviceInformation.wszDeviceName);
						hr = HRESULT.S_FALSE;
						goto cleanup;
					}
					break;

				case PCConfigPin:
					//check to make sure the device supports pin before doing the pin configuration
					if ((WCNDeviceInformation.uConfigMethods & WCN_VALUE_CM_LABEL) ||
						(WCNDeviceInformation.uConfigMethods & WCN_VALUE_CM_DISPLAY))
					{
						if (configParams.pDevicePin == 0)
						{
							Console.Write("\nERROR: Pin must not be 0 when doing a pin configuration");
							hr = HRESULT.S_FALSE;
							goto cleanup;
						}

						result = WideCharToMultiByte(CP_UTF8, //CodePage
									0, //Unmapped character flags
									configParams.pDevicePin,
									-1, //null terminated string
									([MarshalAs(UnmanagedType.LPStr)] StringBuilder)pin,
									sizeof(pin),
									default, //lpDefaultChar - use system default value
									default); //lpUsedDefaultChar ignored
						if (result == 0)
						{
							Console.Write("\nERROR: Failed to convert the pin to multibyte.");
							goto cleanup;
						}

						pinLen = sizeof(pin) - 1;

					}
					else
					{
						Console.Write("\nERROR: The [%s] device does not supprot the pin method", WCNDeviceInformation.wszDeviceName);
						hr = HRESULT.S_FALSE;
						goto cleanup;
					}
					break;

				default:
					break;
			}

			//The IWCNDevice::SetPassword method configures the authentication method value, and if required, 
			//a password used for the pending session. This method may only be called prior to IWCNDevice::Connect.
			hr = pDevice.SetPassword(configParams.enumConfigType,
										pinLen,
										(byte*)pin);

			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nERROR: IWCNDevice::SetPassword failed with error code [0x%x]", hr);
				goto cleanup;
			}
			else
			{
				Console.Write("\nINFO: IWCNDevice::SetPassword succeeded with result [0x%x]", hr);
			}


			//The IWCNDevice::Connect method initiates the session.
			hr = pDevice.Connect(pWcnConNotif);
			if (hr != HRESULT.S_OK)
			{
				//Device Push button configuration is only supported on SoftAP capable wireless Nics 
				if (hr == HRESULT _FROM_WIN32(ERROR_CONNECTION_UNAVAIL)
					&& configParams.enumConfigScenario == DeviceConfigPushButton)
				{
					Console.Write("\nERROR: PushButton Configuration of non AP devices is only supported on");
					Console.Write("\n SoftAP capable wireless network cards.");
				}
				else
				{
					Console.Write("\nERROR: IWCNDevice::Connect failed with error code [0x%x]", hr);
				}
				goto cleanup;
			}
			else
			{
				Console.Write("\nINFO: IWCNDevice::Connect succeeded with result [0x%x]", hr);
			}

			//wait for the configuration result
			hr = pWcnConNotif.WaitForConnectionResult();
			if (hr != HRESULT.S_OK)
			{
				Console.Write("ERROR: WaitforconnectionResult returned the following error [ox%x]", hr);
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
			if (configParams.enumConfigScenario == PCConfigPushButton || configParams.enumConfigScenario == PCConfigPin)
			{
				//The IWCNDevice::GetNetworkProfile method gets a network profile from the device.
				hr = pDevice.GetNetworkProfile(ARRAYSIZE(profileBuffer), profileBuffer);
				if (hr != HRESULT.S_OK)
				{
					Console.Write("\nERROR: IWCNDevice::GetNetworkProfile failed with [0x%x]", hr);
					goto cleanup;
				}

				//save the profile to the system if doing a RouterConfig or a pushbutton scenario
				//The SoftapConfig and DeviceConfig scenarios will generally use a profile that is already on the system
				//save the profile to the wlan interface			
				status = WlanSetProfile(wlanHandle,
										&interfaceGuid,
										0, //Flags - none
										profileBuffer,
										default, // Default Security - All user profile
										true, // Overwrite profile
										default, // reserved
										&wlanResult);

				if (status != Win32Error.ERROR_SUCCESS)
				{
					Console.Write("\nERROR: Failed to save the profile to the WLAN store, return code was [0x%x]", wlanResult);
					hr = HRESULT.S_FALSE;
				}
				else
				{
					Console.Write("\nINFO: Successfully saved the profile to the WLAN store");
				}
			}

			//Display the SSID and passphrase used to configure the Router or device
			if (configParams.enumConfigScenario != PCConfigPin && configParams.enumConfigScenario != PCConfigPushButton)
			{
				Console.Write("\nINFO: Profile SSID Used: [%s]", configParams.pProfileSSID);
				Console.Write("\nINFO: Profile Passphrase Used: [%s]", configParams.pProfilePassphrase);
			}

			cleanup:

			if (pWcnConNotif)
			{
				pWcnConNotif.Release();
				pWcnConNotif;
			}

			if (wcnFdDiscoveryNotify)
			{
				wcnFdDiscoveryNotify.Release();
				wcnFdDiscoveryNotify;
			}

			if (wlanHandle != default)
			{
				WlanCloseHandle(wlanHandle, default);
			}

			if (pInterfaceList != default)
			{
				WlanFreeMemory(pInterfaceList);
			}

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
			dwMajorVersion = (uint)(Macros.LOBYTE(Macros.LOWORD(dwVersion)));
			dwMinorVersion = (uint)(Macros.HIBYTE(Macros.LOWORD(dwVersion)));

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
}
