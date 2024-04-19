using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.Ole32;

namespace PRSample;

public static partial class Program
{
	public static readonly Lazy<HWND> hwnd = new(User32.GetDesktopWindow);

	/////////////////////////////////////////////////////////////////
	// myAddProperty
	//
	// This function initializes the property structure pProp
	/////////////////////////////////////////////////////////////////
	private static DBPROP myAddProperty(DBPROPENUM dwPropertyID, object? lValue = null, bool required = true) => new()
	{
		// Set up the property structure
		dwPropertyID = dwPropertyID,
		dwOptions = required ? DBPROPOPTIONS.DBPROPOPTIONS_REQUIRED : DBPROPOPTIONS.DBPROPOPTIONS_OPTIONAL,
		dwStatus = DBPROPSTATUS.DBPROPSTATUS_OK,
		colid = DB_NULLID,
		vValue = lValue
	};

	/////////////////////////////////////////////////////////////////
	// myCreateDataSource
	//
	// This function creates an OLE DB DataSource object for a provider selected by the user, sets initialization properties for the
	// DataSource, and initializes the DataSource. The function returns a pointer to the DataSource object's IUnknown ref in ppUnkDataSource.
	/////////////////////////////////////////////////////////////////
	private static void myCreateDataSource(out IDBInitialize ppUnkDataSource)
	{
		Guid clsid = CLSID_MSDASQL;
		object? pIDBInitialize = null;

		// Use the Microsoft Data Links UI to create the DataSource object; this will allow the user to select the provider to connect to and
		// to set the initialization properties for the DataSource object, which will be created by the Data Links UI.
		if (g_dwFlags.IsFlagSet(Flags.USE_PROMPTDATASOURCE))
		{
			// Create the Data Links UI object and obtain the IDBPromptInitialize interface from it
			IDBPromptInitialize pIDBPromptInitialize = new();

			// Invoke the Data Links UI to allow the user to select the provider and set initialization properties for the DataSource object
			// that this will create
			pIDBPromptInitialize.PromptDataSource(default, //pUnkOuter
				hwnd.Value, //hWndParent
				DBPROMPTOPTIONS.DBPROMPTOPTIONS_PROPERTYSHEET, //dwPromptOptions
				0, //cSourceTypeFilter
				default, //rgSourceTypeFilter
				default, //pwszszzProviderFilter
				typeof(IDBInitialize).GUID, //riid
				ref pIDBInitialize //ppDataSource
			).ThrowIfFailed();

			// We've obtained a DataSource object from the Data Links UI. This object has had its initialization properties set, so all we
			// need to do is Initialize it
			((IDBInitialize)pIDBInitialize!).Initialize().ThrowIfFailed();

			ppUnkDataSource = (IDBInitialize)pIDBInitialize;
		}
		// We are not using the Data Links UI to create the DataSource object. Instead, we will enumerate the providers installed on this
		// system through the OLE DB Enumerator and will allow the user to select the ProgID of the provider for which we will create a
		// DataSource object.
		else
		{
			// Use the OLE DB Enumerator to obtain a rowset of installed providers, then allow the user to select a provider from this rowset
			myCreateEnumerator(CLSID_OLEDB_ENUMERATOR, out clsid);

			// We will create the DataSource object through the OLE DB service component IDataInitialize interface, so we need to create an
			// instance of the data initialization object
			IDataInitialize pIDataInitialize = new();

			// Use IDataInitialize::CreateDBInstance to create an uninitialized DataSource object for the chosen provider. By using this
			// service component method, the service component manager can provide additional functionality beyond what is natively supported
			// by the provider if the consumer requests that functionality
			pIDataInitialize.CreateDBInstance(clsid, //clsid -- provider
				default, //pUnkOuter
				CLSCTX.CLSCTX_INPROC_SERVER, //dwClsContext
				default, //pwszReserved
				typeof(IDBInitialize).GUID, //riid
				out pIDBInitialize //ppDataSource
			);

			// Initialize the DataSource object by setting any required initialization properties and calling IDBInitialize::Initialize
			myDoInitialization(pIDBInitialize!);

			ppUnkDataSource = (IDBInitialize)pIDBInitialize!;
		}
	}

	/////////////////////////////////////////////////////////////////
	// myDoInitialization
	//
	// This function sets initialization properties that tell the provider to prompt the user for any information required to initialize the
	// provider, then calls the provider's initialization function.
	/////////////////////////////////////////////////////////////////
	private static void myDoInitialization(object pIUnknown)
	{
		// In order to initialize the DataSource object most providers require some initialization properties to be set by the consumer. For
		// instance, these might include the data source to connect to and the user ID and password to use to establish identity. We will ask
		// the provider to prompt the user for this required information by setting the following properties:
		DBPROP[] rgProperties =
		[
			myAddProperty(DBPROPENUM.DBPROP_INIT_PROMPT, DBPROMPT_COMPLETE),
			myAddProperty(DBPROPENUM.DBPROP_INIT_HWND, (IntPtr)hwnd.Value)
		];
		DBPROPSET[] rgPropSets = [new DBPROPSET() { rgProperties = rgProperties, guidPropertySet = DBPROPSET_DBINIT }];

		// Obtain the needed interfaces
		var pIDBProperties = (IDBProperties)pIUnknown;
		var pIDBInitialize = (IDBInitialize)pIUnknown;

		// If a provider requires initialization properties, it must support the properties that we are setting (_PROMPT and HWND). However,
		// some providers do not need initialization properties and may therefore not support the PROMPT and HWND properties. Because of
		// this, we will not check the return value from SetProperties
		pIDBProperties.SetProperties(1, rgPropSets);

		// Now that we've set our properties, initialize the provider
		pIDBInitialize.Initialize().ThrowIfFailed();
	}

	/////////////////////////////////////////////////////////////////
	// myGetProperty
	//
	// This function gets the bool value for the specified property and returns the result ref in pbValue.
	/////////////////////////////////////////////////////////////////
	private static void myGetProperty(object pIUnknown, in Guid riid, DBPROPENUM dwPropertyID, in Guid guidPropertySet, out bool pbValue)
	{
		// Initialize the output
		pbValue = false;

		// Set up the Property ID Set
		SafeNativeArray<DBPROPENUM> rgPropertyIDs = new([dwPropertyID]);
		DBPROPIDSET[] rgPropertyIDSets = [new() { rgPropertyIDs = rgPropertyIDs, cPropertyIDs = 1, guidPropertySet = guidPropertySet }];

		// Get the property value for this property from the provider, but don't try to display extended error information, since this may
		// not be a supported property: a failure is, in fact, expected if the property is not supported
		DBPROPSET[] rgPropSets;
		if (riid == typeof(IDBProperties).GUID)
		{
			IDBProperties pIDBProperties = (IDBProperties)pIUnknown;
			pIDBProperties.GetProperties(
				rgPropertyIDSets, //rgPropertyIDSets
				out rgPropSets //prgPropSets
			).ThrowIfFailed();
		}
		else if (riid == typeof(ISessionProperties).GUID)
		{
			ISessionProperties pISesProps = (ISessionProperties)pIUnknown;
			pISesProps.GetProperties(
			rgPropertyIDSets, //rgPropertyIDSets
			out rgPropSets //prgPropSets
			).ThrowIfFailed();
		}
		else if (riid == typeof(ICommandProperties).GUID)
		{
			ICommandProperties pICmdProps = (ICommandProperties)pIUnknown;
			pICmdProps.GetProperties(
				rgPropertyIDSets, //rgPropertyIDSets
				out rgPropSets //prgPropSets
			).ThrowIfFailed();
		}
		else
		{
			IRowsetInfo pIRowsetInfo = (IRowsetInfo)pIUnknown;
			pIRowsetInfo.GetProperties(
				rgPropertyIDSets, //rgPropertyIDSets
				out rgPropSets //prgPropSets
			).ThrowIfFailed();
		}

		// Return the value for this property to the caller if it's a VT_BOOL type value, as expected
		if (rgPropSets[0].rgProperties[0].vValue is bool b)
			pbValue = b;
	}
}

internal static class User32
{
	[DllImport("user32.dll", SetLastError = true)]
	public static extern HWND GetDesktopWindow();
}