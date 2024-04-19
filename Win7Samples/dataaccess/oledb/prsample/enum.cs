using static Vanara.PInvoke.Ole32;

namespace PRSample;

public static partial class Program
{
	/////////////////////////////////////////////////////////////////
	// myCreateEnumerator
	//
	// This function creates an enumerator, obtains a sources rowset from it, displays the rowset to the user, and allows the user to specify
	// the ProgID of a provider. The Guid that matches this ProgID is retuned to the caller ref in pCLSID.
	/////////////////////////////////////////////////////////////////
	private static void myCreateEnumerator(in Guid clsidEnumerator, out Guid pCLSID)
	{
		//HRESULT hr;
		//object pIUnkEnumerator = default;
		//ISourcesRowset pISourcesRowset = default;
		//IRowset pIRowset = default;
		//ref IDBInitialize pIDBInitialize = default;
		//ushort[] wszProgID = new[] ushort = new[] new = new[] new = new[] new = new new[MAX_NAME_LEN + 1] = default;

		// Create the Enumerator object. We ask for IUnknown when creating the enumerator because some enumerators may require initialization
		// before we can obtain a sources rowset from the enumerator. This is indicated by whether the enumerator object exposes
		// IDBInitialize or not (we don't want to ask for IDBInitialize, since enumerators that don't require initialization will cause the
		// CoCreateInstance to fail)
		CoCreateInstance(clsidEnumerator, //clsid -- enumerator
			default, //pUnkOuter
			CLSCTX.CLSCTX_INPROC_SERVER, //dwClsContext
			IID_IUnknown, //riid
			out var pIUnkEnumerator //ppvObj
		).ThrowIfFailed();

		// If the enumerator exposes IDBInitialize, we need to initialize it
		if (pIUnkEnumerator is IDBInitialize pIDBInitialize)
		{
			myDoInitialization(pIUnkEnumerator);
		}

		// Set properties on the rowset, to request additional functionality
		myAddRowsetProperties(out var rgPropSets);

		// Obtain a sources rowset from the enumerator. This rowset contains all of the OLE DB providers that this enumerator is able to list
		ISourcesRowset pISourcesRowset = (ISourcesRowset)pIUnkEnumerator;
		pISourcesRowset.GetSourcesRowset(default, //pUnkOuter
			typeof(IRowset).GUID, //riid
			1, //cPropSets
			rgPropSets, //rgPropSets
			out var pIRowset //ppRowset
		).ThrowIfFailed();

		// Display the rowset to the user; this will allow the user to perform basic navigation of the rowset and will allow the user to
		// select a row containing a desired provider.
		myDisplayRowset(pIRowset!, "SOURCES_NAME", out var wszProgID);

		// Obtain the ProgID for the provider to use from the user; the default value for this is the value of the SOURCES_NAME column in the
		// row selected by the user previously
		myGetInputFromUser(out var wszProgID2, "\nType the ProgID of a provider to use [Enter = `{0}`]: ", wszProgID);
		CLSIDFromProgID(wszProgID2!, out pCLSID).ThrowIfFailed();
	}
}