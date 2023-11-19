using Vanara.PInvoke;
using static Vanara.PInvoke.FunDisc;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WcnApi;

namespace WindowsConnectNow;

class CWcnFdDiscoveryNotify : IFunctionDiscoveryNotification
{
	IFunctionDiscovery? m_pFunctionDiscovery;
	IFunctionInstanceCollectionQuery? m_pFunctionInstanceCollectionQuery;
	IFunctionInstanceCollection? m_pFiCollection;
	IFunctionInstance? m_pFunctionInstance;

	string? wszUUID = null;
	string? wszSSIDInHex = null;
	bool bUseSSID;
	SafeEventHandle? anySearchEvent;       // set on _any_ event (device added, removed, etc)

	//when function discovery finds device during its search look at the device to see if its the one you want
	public HRESULT OnUpdate([In] QueryUpdateAction enumQueryUpdateAction, [In] ulong fdqcQueryContext, [In] IFunctionInstance pIFunctionInstance)
	{
		string? ssid = null;
		HRESULT hr = HRESULT.S_OK;

		switch (enumQueryUpdateAction)
		{
			case QueryUpdateAction.QUA_ADD:
				//QUA_ADD is called whenever a new device appears on the network.
				//("New" is defined as "new to you", so at the start of a query, you may
				// see a bunch of QUA_ADDs for all the existing devices).
				//
				//You can inspect the Function Instance's properties by calling
				//IFunctionInstance::OpenPropertyStore. You can also get an IWCNDevice
				//interface from the IFunctionInstance interface pointer by calling
				//IFunctionInstance::QueryService.
				//
				//The IFunctionInstance interface pointer that is passed into this OnUpdate
				//callback doesn't belong to you, so if you want to save it for use beyond
				//the lifetime of this callback, you need to add a reference to it (and
				//possibly marshal it over to your thread). Since this sample uses
				//CComPtr smart pointers, the additional reference count is automatic.

				//open the property store of this function instance to extract the Guid of the device
				hr = pIFunctionInstance.OpenPropertyStore(STGM.STGM_READ, out var pPropStore);
				if (hr.Failed)
				{
					Console.Write("\nERROR: Failed to open PropertyStore for FI.");
					goto cleanup;
				}

				//We need to decide if this IFunctionInstance belongs to the device
				//that we are interested in. This sample demonstrates two different
				//ways of matching an IFunctionInstance:
				// 1. Match an AP's SSID; or
				// 2. Match any device's WPS Guid.
				if (bUseSSID) // 1. Match by SSID
				{
					try
					{
						ssid = (string?)pPropStore!.GetValue(PKEY_WCN_SSID);
					}
					catch (Exception ex)
					{
						Console.Write(ex.Message);
						hr = ex.HResult;
						goto cleanup;
					}

					if (string.Equals(ssid, wszSSIDInHex, StringComparison.OrdinalIgnoreCase))
					{
						// The SSID matches the one we computed earlier, so save the
						// function instance and tell the main thread it can continue.
						//
						// Note that if you are using an apartment threaded main thread
						// (e.g., it is a GUI thread), you'll have to marshal this pointer
						// so that it isn't smuggled across an apartment boundary.
						// This sample has all threads in the MTA, so we don't need 
						// to marshal anything.
						//
						// If you aren't using smart pointers, make sure to do AddRef here.
						m_pFunctionInstance = pIFunctionInstance;
						anySearchEvent?.Set();
					}
				}
				else // 2. Match by WPS Guid
				{
					try
					{
						ssid = (string?)pPropStore!.GetValue(PKEY_PNPX_GlobalIdentity);
					}
					catch (Exception ex)
					{
						Console.Write(ex.Message);
						hr = ex.HResult;
						goto cleanup;
					}

					// put curly braces around Guid so that you can compare it.
					//Compare extracted Guid with the desired Guid
					if (string.Equals($"{{{ssid}}}", wszUUID, StringComparison.OrdinalIgnoreCase))
					{
						// The Guid matches, so save the interface. See the comments
						// above about threading and reference counting.
						m_pFunctionInstance = pIFunctionInstance;
						anySearchEvent?.Set();
					}
				}

				break;

			case QueryUpdateAction.QUA_CHANGE:
				// The function instance may have changed one or more of its properties.
				// If you are displaying the properties to the user, you should mark the
				// display as dirty and refresh them soon.
				//
				// This sample code does not display device icons in a GUI, so it doesn't
				// handle QUA_CHANGE.
				break;

			case QueryUpdateAction.QUA_REMOVE:
				// The function instance is about to be removed. You shouldn't take a
				// reference to the IFunctionInstance here, because it points to a device
				// that no longer exists anyway. Instead, check its Guid and see if you
				// were displaying any device with that Guid to the user. If so, remove
				// the device from your display. Make sure to release any COM interfaces
				// that point to the device being removed.
				//
				// This sample code does not display device icons in a GUI, so it doesn't
				// handle QUA_REMOVE.
				break;

			default:
				break;
		}

		cleanup:

		return hr;
	}

	//this callback is invoked if there is a fatal error and the search could not be completed
	public HRESULT OnError([In] HRESULT hrFD, [In] ulong fdqcQueryContext, [In] string pszProvider)
	{
		anySearchEvent?.Set();

		Console.Write("\nERROR: Provider [{0}] returned error code [0x{1:X}]", pszProvider, (int)hrFD);

		return HRESULT.S_OK;
	}

	//used to signal the end of the Function Discovery Search
	public HRESULT OnEvent([In] FD_EVENTID dwEventID, [In] ulong fdqcQueryContext, [In] string pszProvider)
	{
		if (dwEventID == FD_EVENTID.FD_EVENTID_SEARCHCOMPLETE)
		{
			Console.Write("\nINFO: FD_EVENTID_SEARCHCOMPLETE");
			anySearchEvent?.Set();
		}

		return HRESULT.S_OK;
	}

	// create the Function discovery instance and setup an event to be used to signal the end of the Search 
	public HRESULT Init([In]bool bTurnOnSoftAP)
	{
		anySearchEvent = CreateEvent(default, true, false, default);
		bUseSSID = false;
		HRESULT hr;
		if (anySearchEvent.IsInvalid)
		{
			Console.Write("ERROR: Failed to create the search event");
			hr = HRESULT.E_FAIL;
			goto cleanup;
		}

		//This interface is used by client programs to discover function instances, get the default function
		//instance for a category, and create advanced Function Discovery query objects that enable registering 
		//Function Discovery defaults, among other things.
		m_pFunctionDiscovery = new IFunctionDiscovery();

		//create an WCN instance collection Query for Function Discovery to use
		//This interface implements the asynchronous query for a collection of function instances based on category 
		//and subcategory. A pointer to this interface is returned when the collection query is created by the 
		//client program.
		var ctx = 0UL;
		hr = m_pFunctionDiscovery.CreateInstanceCollectionQuery(FCTN_CATEGORY_WCN, default, false, this, ref ctx,
			out m_pFunctionInstanceCollectionQuery);

		if (hr != HRESULT.S_OK)
		{
			Console.Write("\nFailed to create Function Discovery query.");
			goto cleanup;
		}

		if (bTurnOnSoftAP)
		{
			// WCN can optionally turn on the SoftAP (aka WLAN Hosted Network) if the PC has a wireless
			// adapter that supports it. You should only turn on SoftAP if you expect a wireless device
			// (like a wireless printer or a wireless picture frame) to connect to the hosted network.
			//
			// This API will return S_OK regardless if the computer supports SoftAP or not. If you really
			// need to know if SoftAP is not supported, call WlanHostedNetworkQueryStatus and check
			// if the returned HostedNetworkState is wlan_hosted_network_unavailable.
			hr = m_pFunctionInstanceCollectionQuery!.AddQueryConstraint(WCN_QUERY_CONSTRAINT_USE_SOFTAP, FD_CONSTRAINTVALUE_TRUE);

			if (hr != HRESULT.S_OK)
			{
				Console.Write("\nFailed to add the softap query constraint hr=[0x{0}]", hr);
				goto cleanup;
			}
		}

		cleanup:

		return hr;
	}

	private static string ConvertStringToHex(byte[] bytes)
	{
		char[] c = new char[bytes.Length * 2];
		byte b;
		for (int i = 0; i < bytes.Length; i++)
		{
			b = ((byte)(bytes[i] >> 4));
			c[i * 2] = (char)(b > 9 ? b + 0x37 : b + 0x30);
			b = ((byte)(bytes[i] & 0xF));
			c[i * 2 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
		}
		return new string(c);
	}

	//start the Function Discovery search based on the supplied Guid
	public HRESULT WcnFDSearchStart(Guid? pUUID, string? pSearchSSID)
	{
		HRESULT hr = HRESULT.S_OK;

		if (m_pFunctionInstanceCollectionQuery == default)
		{
			Console.Write("\nERROR: FD Instance query is default.");
			hr = HRESULT.E_FAIL;
			goto cleanup;
		}

		//prefer the uuid over the SSID
		if (pUUID.HasValue)
		{
			//convert the Guid to a string 
			wszUUID = pUUID.Value.ToString("B");
			bUseSSID = false;
		}
		//USE the SSID to find the Router, NOTE: this will return the first SSID it finds which may not be 
		//the rotuer you want.
		else if (pSearchSSID is not null)
		{
			//A SSID is an array of octets. Since Function Discovery cannot filter searches based on
			//a raw byte array, the SSID is actually encoded in hexadecimal before being saved in the
			//FD property bag. If you plan to use the PKEY_WCN_SSID, you should first convert the
			//raw octets to Unicode, then convert the Unicode to hexadecimal.
			
			wszSSIDInHex = ConvertStringToHex(Encoding.GetEncoding(1252).GetBytes(pSearchSSID));

			bUseSSID = true;

		}
		else
		{
			Console.Write("\nERROR: Search Guid and Search SSID are blank.");
			goto cleanup;
		}

		//Start the search
		Console.Write("\nINFO: Stating the Function Discovery Search...");

		//Performs the query defined by IFunctionDiscovery::CreateInstanceCollectionQuery.
		hr = m_pFunctionInstanceCollectionQuery.Execute(out m_pFiCollection);

		// We expect asynchronous results.
		if (hr == HRESULT.E_PENDING)
		{
			hr = HRESULT.S_OK;
		}

		if (hr.Failed)
		{
			Console.Write("\nERROR: Function Discovery query failed to run with the following error hr = {0}.", hr);
			goto cleanup;
		}

		cleanup:

		return hr;
	}

	//monitor when the Function Discovery events
	public bool WaitForAnyDiscoveryEvent(uint Timeout_ms)
	{
		if (WaitForSingleObject(anySearchEvent!, Timeout_ms) == WAIT_STATUS.WAIT_OBJECT_0)
		{
			ResetEvent(anySearchEvent!);
			return true;
		}
		else
		{
			Console.Write("\nERROR: Discovery timeout (after waiting {0}ms).", Timeout_ms);
			return false;
		}
	}

	//once Function Discovery finds the instance we are looking for set the WCN Device instance
	public bool GetWCNDeviceInstance(out IWCNDevice? ppWcnDevice)
	{
		bool returnValue = false;

		ppWcnDevice = default;

		//The instance we were looking for was not found.
		if (m_pFunctionInstance is null)
		{
			goto cleanup;
		}

		//Attempt to get IWCNDevice
		//Acts as the factory method for any services exposed through an implementation of 
		//IFunctionInstance. QueryService creates and initializes instances of a requested interface 
		//if the service from which the interface was requested supports the interface.
		var hr = m_pFunctionInstance.QueryService(SID_WcnProvider, typeof(IWCNDevice).GUID, out var pDev);
		if (hr != HRESULT.S_OK)
		{
			Console.Write("\nERROR: Failed to get IWCNDevice from the Function Instance hr={0}.", hr);
			returnValue = false;
			goto cleanup;
		}
		else
			ppWcnDevice = (IWCNDevice?)pDev;

		returnValue = true;

		cleanup:

		return returnValue;
	}
}