using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.FunDisc;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;

namespace fdsample
{
	public class CMyFDHelper : IFunctionDiscoveryNotification
	{
		private static readonly PROPERTYKEY[] g_Keys = {
			PROPERTYKEY.System.ComputerName,
			PKEY_FD_Visibility,
			PKEY_Device_InstanceId,
			PKEY_Device_Interface,
			PKEY_Device_HardwareIds,
			PKEY_Device_CompatibleIds,
			PKEY_Device_Service,
			PKEY_Device_Class,
			PKEY_Device_ClassGuid,
			PKEY_Device_Driver,
			PKEY_Device_ConfigFlags,
			PKEY_Device_Manufacturer,
			PKEY_Device_FriendlyName,
			PKEY_Device_LocationInfo,
			PKEY_Device_PDOName,
			PKEY_Device_Capabilities,
			PKEY_Device_UINumber,
			PKEY_Device_UpperFilters,
			PKEY_Device_LowerFilters,
			PKEY_Device_BusTypeGuid,
			PKEY_Device_LegacyBusType,
			PKEY_Device_BusNumber,
			PKEY_Device_EnumeratorName,
			PKEY_Device_QueueSize,
			PKEY_Device_Status,
			PKEY_Device_Comment,
			PKEY_Device_Model,
			PKEY_Device_DeviceDesc,
			PKEY_Device_BIOSVersion,
			PKEY_PNPX_DomainName,
			PKEY_PNPX_ShareName,
			PKEY_PNPX_GlobalIdentity,
			PKEY_PNPX_Types,
			PKEY_PNPX_Scopes,
			PKEY_PNPX_XAddrs,
			PKEY_PNPX_MetadataVersion,
			PKEY_DeviceDisplay_Manufacturer,
			PKEY_PNPX_ManufacturerUrl,
			PKEY_DeviceDisplay_ModelName,
			PKEY_DeviceDisplay_ModelNumber,
			PKEY_PNPX_ModelUrl,
			PKEY_PNPX_Upc,
			PKEY_PNPX_PresentationUrl,
			PKEY_DeviceDisplay_FriendlyName,
			PKEY_PNPX_FirmwareVersion,
			PKEY_PNPX_SerialNumber,
			PKEY_PNPX_DeviceCategory,
			PKEY_PNPX_PhysicalAddress,
			PKEY_PNPX_NetworkInterfaceGuid,
			PKEY_PNPX_NetworkInterfaceLuid,
			PKEY_PNPX_Installable,
			PKEY_PNPX_Associated,
			PKEY_PNPX_IpAddress,
			PKEY_PNPX_CompatibleTypes,
			PKEY_PNPX_ServiceId,
			PKEY_PNPX_ServiceTypes,
			PKEY_PNPX_ServiceAddress,
			PKEY_WNET_Scope,
			PKEY_WNET_Type,
			PKEY_WNET_DisplayType,
			PKEY_WNET_Usage,
			PKEY_WNET_LocalName,
			PKEY_WNET_RemoteName,
			PKEY_WNET_Comment,
			PKEY_WNET_Provider,
			PKEY_DrvPkg_VendorWebSite
		};

		private readonly AutoResetEvent m_hAdd, m_hRemove, m_hChange;
		private readonly IFunctionDiscovery m_pFunDisc;

		public CMyFDHelper()
		{
			// Create events for signaling notifications
			m_hAdd = new AutoResetEvent(false);
			m_hRemove = new AutoResetEvent(false);
			m_hChange = new AutoResetEvent(false);

			// CoCreate function discovery
			m_pFunDisc = new IFunctionDiscovery();
		}

		public void ListFunctionInstances(string pszCategory)
		{
			// Create an instance collection query. This CMyFDHelper class implements CFunctionDiscoveryNotificationWrapper and therefore
			// IFunctionDiscoveryNotification. One of the parameters to CreateInstanceCollectionQuery is a IFunctionDiscoveryNotification
			// pointer. This object is sent query events.
			m_pFunDisc.CreateInstanceCollectionQuery(pszCategory, default, true, this, default, out IFunctionInstanceCollectionQuery pQuery).ThrowIfFailed();

			// Execute the query. If it's a local query, for example PnP or the registry, hr will be set to HRESULT.S_OK and the collection
			// will be populated with instances. If it's a network query hr will be set to HRESULT.E_PENDING and the collection will be
			// empty. All instances from a network query are returned via notifications.
			HRESULT hr = pQuery.Execute(out IFunctionInstanceCollection pCollection);

			// If this is a local query, we expect HRESULT.S_OK
			uint dwCount = 0;
			if (HRESULT.S_OK == hr)
				hr = pCollection.GetCount(out dwCount);

			// Loop through all instances returned in the collection This is done only in the case of a local query
			IFunctionInstance pInstance = default;
			IPropertyStore pStore = default;
			for (uint i = 0; (HRESULT.S_OK == hr && (i < dwCount)); i++)
			{
				if (HRESULT.S_OK == hr)
					hr = pCollection.Item(i, out pInstance);

				if (HRESULT.S_OK == hr)
					hr = pInstance.OpenPropertyStore(STGM.STGM_READ, out pStore);

				if (HRESULT.S_OK == hr)
					hr = DisplayProperties(pStore);
			}

			if (HRESULT.S_OK == hr)
				Console.Write("Found {0} instances on your system.\n\n", dwCount);

			// If it's a network query, we expected HRESULT.E_PENDING from the above call to pQuery.Execute(&pCollection ) Instances will be
			// returned via notifications, return HRESULT.S_OK
			if (HRESULT.E_PENDING == hr)
				hr = HRESULT.S_OK;

			hr.ThrowIfFailed();
		}

		public HRESULT OnError(HRESULT hr, ulong fdqcQueryContext, [MarshalAs(UnmanagedType.LPWStr)] string pszProvider)
		{
			if (pszProvider is not null)
				Console.Write("{0} encountered '{1}'.", pszProvider, hr);

			return HRESULT.S_OK;
		}

		public HRESULT OnEvent(FD_EVENTID dwEventID, ulong fdqcQueryContext, [MarshalAs(UnmanagedType.LPWStr)] string pszProvider)
		{
			if (pszProvider is not null)
				Console.Write("{0} sent OnEvent notification.", pszProvider);

			return HRESULT.S_OK;
		}

		public HRESULT OnUpdate(QueryUpdateAction eAction, ulong fdqcQueryContext, IFunctionInstance pInstance)
		{
			if (pInstance is null)
				return HRESULT.E_INVALIDARG;

			// Open the property store
			HRESULT hr = pInstance.OpenPropertyStore(STGM.STGM_READ, out IPropertyStore pStore);

			// In PnP the device's friendly name could be in one of the following property keys. Check each.
			var pvName = (string)pStore.GetValue(PKEY_Device_DeviceDesc) ?? (string)pStore.GetValue(PKEY_Device_FriendlyName);

			// If there is no friendly name, get the provider instance ID instead. This ID is unique to the provider and is used to identify
			// the instance.
			if (pvName is null)
			{
				hr = pInstance.GetProviderInstanceID(out pvName);
			}

			switch (eAction)
			{
				case QueryUpdateAction.QUA_ADD:
					Console.Write("Added: {0}\n\n", pvName);
					SetEvent(m_hAdd);
					break;

				case QueryUpdateAction.QUA_REMOVE:
					Console.Write("Removed: {0}\n\n", pvName);
					SetEvent(m_hRemove);
					break;

				case QueryUpdateAction.QUA_CHANGE:
					Console.Write("Changed: {0}\n\n", pvName);
					SetEvent(m_hChange);
					break;
			}

			return hr;
		}

		public HRESULT WaitForChange(int dwTimeout, string pszCategory, QueryUpdateAction eAction)
		{
			// Reset each event incase a notification was received by FD before this sample was interested.
			m_hAdd?.Reset();
			m_hRemove?.Reset();
			m_hChange?.Reset();

			// Create a query to recieve notifications
			HRESULT hr = m_pFunDisc.CreateInstanceCollectionQuery(pszCategory, default, true, this, default, out IFunctionInstanceCollectionQuery pQuery);

			// Add a query constraint

			// If we're querying the PnP provider category this constraint will tell the PnP provider not to populate the collection with
			// current PnP devices. We will only get notifications as devices are added and removed

			// If this method is called with a different category, the provider will ignore the constraint. It is PnP provider specific.
			if (HRESULT.S_OK == hr)
				hr = pQuery.AddQueryConstraint(PROVIDERPNP_QUERYCONSTRAINT_NOTIFICATIONSONLY, "true");

			// Execute the query. As long as the query exists we will recieve notifications.
			if (HRESULT.S_OK == hr)
				hr = pQuery.Execute(out _);

			// If it's a network query, we expect the HRESULT.E_PENDING
			if (HRESULT.E_PENDING == hr)
				hr = HRESULT.S_OK;

			AutoResetEvent hEvent = eAction switch
			{
				QueryUpdateAction.QUA_ADD => m_hAdd,
				QueryUpdateAction.QUA_REMOVE => m_hRemove,
				QueryUpdateAction.QUA_CHANGE => m_hChange,
				_ => default,
			};

			// Block and wait for a notification
			if ((HRESULT.S_OK == hr) && (hEvent is not null))
			{
				if (!hEvent.WaitOne(dwTimeout))
					Console.Write("Timeout!\n");
			}

			// One device may correspond to multiple function instances This sleep allows the OnUpdate call to output information about each
			// Function Instance.

			// THIS SLEEP IS MERELY FOR DISPLAY PURPOSES
			Sleep(1000);

			return hr;
		}

		// Outputs all properties in a property store
		private static HRESULT DisplayProperties(IPropertyStore pPStore)
		{
			if (pPStore is null)
				return HRESULT.E_INVALIDARG;

			HRESULT hr = HRESULT.S_OK;

			// Get the number of properties in the store and loop through them all
			var cProps = pPStore.GetCount();
			for (uint p = 0; (HRESULT.S_OK == hr && p < cProps); p++)
			{
				try
				{
					PROPERTYKEY key = pPStore.GetAt(p);

					// Loop through that table defined at the beginning of this file to find the key name
					var pszKeyName = key.ToString();
					if (pszKeyName is null || pszKeyName.StartsWith('{'))
					{
						var fi = typeof(FunDisc).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).FirstOrDefault(f => f.FieldType == typeof(PROPERTYKEY) && (PROPERTYKEY)f.GetValue(null) == key);
						if (fi is not null)
							pszKeyName = fi.Name;
					}

					object val = null;
					try
					{
						using PROPVARIANT pv = new PROPVARIANT();
						pPStore.GetValue(key, pv);
						val = pv.Value;
					}
					catch (Exception ex)
					{
						Console.Write("{0,-30} = Failure: {1}\n", pszKeyName, ex.Message);
						continue;
					}
					switch (val)
					{
						case null:
							Console.Write("{0,-30} = (empty)\n", pszKeyName);
							break;

						case string s:
							Console.Write("{0,-30} = {1}\n", pszKeyName, s);
							break;

						case uint u:
							Console.Write("{0,-30} = {1:X8}\n", pszKeyName, u);
							break;

						case int i:
							Console.Write("{0,-30} = {1:X8}\n", pszKeyName, i);
							break;

						case bool b:
							Console.Write("{0,-30} = {1}\n", pszKeyName, b);
							break;

						case string[] sa:
							foreach (var s in sa)
							{
								Console.Write("{0,-30} = {1}\n", pszKeyName, s);
								pszKeyName = "";
							}
							break;

						case Guid g:
							Console.Write("{0,-30} = {1:B}\n", pszKeyName, g);
							break;

						default:
							Console.Write("{0,-30} = Variant Type {1:X8} Unknown\n", pszKeyName, val);
							break;
					}
				}
				catch (Exception ex)
				{
					hr = ex.HResult;
				}
			}
			return hr;
		}
	}
}