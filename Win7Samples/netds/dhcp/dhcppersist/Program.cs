using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Dhcp;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

const int INITIAL_BUFFER_SIZE = 256;
const string DHCP_PERSISTENT_APP_STRING = "DhcpRequestSample";

/*
* main
*
* this is where it all happens
*/
bool bAddPersist = false, bRemovePersist = false;
string? ptr = null;

// check for persist options and adapter ID
foreach (var p in args)
{
	if (p[0] is '/' or '-')
	{
		if (char.ToUpper(p[1]) == 'P')
			bAddPersist = true;
		else if (char.ToUpper(p[1]) == 'R')
			bRemovePersist = true;
	}
	else
		ptr = p;
}

var wszAdapter = ptr ?? DetermineAdapter(); // the adapter name in wide chars

// initialize the DHCP Client Options API
DhcpCApiInitialize(out var dwVersion).ThrowIfFailed();
try
{
	Console.Write("DHCP Client Options API version {0}\n", dwVersion);

	Console.Write("Setting DHCP Options on Adapter [{0}]\n", wszAdapter);

	// remove persistence if required
	if (bRemovePersist)
	{
		Console.Write("Removing Option Persistence...\n");
		DhcpUndoRequestParams(0, default, wszAdapter, DHCP_PERSISTENT_APP_STRING).ThrowIfFailed();
	}
	else
	{
		//
		// Here the request is set up - since this is an easy example, the request
		// is set up statically, however in a real-world scenario this may require
		// building the request array in a more 'dynamic' way
		//
		// the DHCP Client Options API arrays for getting the options 
		SafeNativeArray<DHCPAPI_PARAMS> requests = new(new[] { new DHCPAPI_PARAMS { OptionId = DHCP_OPTION_ID.OPTION_TIME_SERVERS } }); // gateway address

		// set-up the actual arrays
		DHCPCAPI_PARAMS_ARRAY sendarray = new(); // we aren't sending anything
		DHCPCAPI_PARAMS_ARRAY requestarray = new() { nParams = (uint)requests.Count, Params = requests };

		// buffer variables
		uint dwSize = INITIAL_BUFFER_SIZE; // size of buffer for options
		using SafeCoTaskMemHandle buffer = new(dwSize); // buffer for options 

		var dwFlags = DHCPCAPI_REQUEST.DHCPCAPI_REQUEST_SYNCHRONOUS;
		if (bAddPersist)
		{
			dwFlags = DHCPCAPI_REQUEST.DHCPCAPI_REQUEST_PERSISTENT;
			Console.Write("Making the request persistent...\n");
		}

		// loop until buffer is big enough to get the data and then make request
		Win32Error dwResult;
		do
		{
			buffer.Size = dwSize; // allocate the buffer
		}
		// make the request on the adapter
		while ((dwResult = DhcpRequestParams(dwFlags, default, wszAdapter, default, sendarray,
			requestarray, buffer, ref dwSize, bAddPersist ? DHCP_PERSISTENT_APP_STRING : null)) == Win32Error.ERROR_MORE_DATA);

		dwResult.ThrowIfFailed();

		// parse out results only if we made the synchronous request
		if (!bAddPersist)
		{
			// check for time server
			if (requests[0].nBytesData > 0)
				Console.Write("Time Server: {0}\n", requests[0].Data.ToStructure<IN_ADDR>());
			else
				Console.Write("Time Server NOT present!\n");
		}
	}
}
finally
{
	// de-init the api
	DhcpCApiCleanup();
}

return 0;

/*
* DetermineAdapter
*
* NOTE:
*
* This code retrieves the Adapter Name to use for the DHCP Client API
* using the IPHelper API.
*
* NT has a name for the adapter that through this API has device
* information in front of it followed by a {Guid}, 98 does not and
* the Index is used instead. So if the string is set to ?? (what it is
* in 98) we revert to using the string representation of the index.
*
*/
static string DetermineAdapter()
{
	IP_INTERFACE_INFO pInfo = GetInterfaceInfo();

	// convert, parse, and convert back
	var szAdapter = pInfo.Adapter[0].Name;
	if (szAdapter[0] == '?')
		// use index if the pointer is not set
		szAdapter = pInfo.Adapter[0].Index.ToString();
	var idx = szAdapter.IndexOf('{');
	if (idx >= 0)
		szAdapter = szAdapter.Remove(0, idx);

	return szAdapter;
}