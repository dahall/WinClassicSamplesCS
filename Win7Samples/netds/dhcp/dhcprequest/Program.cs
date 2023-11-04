using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Dhcp;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;

// initial buffer size for options buffer
const int INITIAL_BUFFER_SIZE = 256;

string wszAdapter; // the adapter name in wide chars 

if (args.Length > 0)
	wszAdapter = args[0];
else
	wszAdapter = DetermineAdapter();

// initialize the DHCP Client Options API
DhcpCApiInitialize(out var dwVersion).ThrowIfFailed();
try
{
	Console.Write("DHCP Client Options API version {0}\n", dwVersion);

	//
	// Here the request is set up - since this is an easy example, the request
	// is set up statically, however in a real-world scenario this may require
	// building the request array in a more 'dynamic' way
	//
	// Also for this sample we are using two items that are almost always in a 
	// DHCP configuration. Hence this information is retrieved from the local 
	// DHCP cache. 
	//
	// the DHCP Client Options API arrays for getting the options 
	SafeNativeArray<DHCPAPI_PARAMS> requests = new(new[] {
		new DHCPAPI_PARAMS { OptionId = DHCP_OPTION_ID.OPTION_SUBNET_MASK }, // subnet mask
		new DHCPAPI_PARAMS { OptionId = DHCP_OPTION_ID.OPTION_ROUTER_ADDRESS }
	}); // gateway address

	// set-up the actual arrays
	DHCPCAPI_PARAMS_ARRAY sendarray = new(); // we aren't sending anything
	DHCPCAPI_PARAMS_ARRAY requestarray = new() { nParams = (uint)requests.Count, Params = requests };

	// buffer variables
	uint dwSize = INITIAL_BUFFER_SIZE; // size of buffer for options
	using SafeCoTaskMemHandle buffer = new(dwSize); // buffer for options 

	Console.Write("Getting DHCP Options on Adapter [{0}]\n", wszAdapter);

	// loop until buffer is big enough to get the data and then make request
	Win32Error dwResult;
	do
	{
		buffer.Size = dwSize; // allocate the buffer
	}
	while ((dwResult = DhcpRequestParams(DHCPCAPI_REQUEST.DHCPCAPI_REQUEST_SYNCHRONOUS, default, wszAdapter,
		default, sendarray, requestarray, buffer, ref dwSize, default)) == Win32Error.ERROR_MORE_DATA);

	dwResult.ThrowIfFailed();

	// parse out results

	// first check subnet
	if (requests[0].nBytesData >= Marshal.SizeOf<IN_ADDR>())
		Console.Write("Subnet Mask: {0}\n", requests[0].Data.ToStructure<IN_ADDR>());
	else
		Console.Write("Subnet Mask NOT present!\n");

	// check for router address
	if (requests[1].nBytesData >= Marshal.SizeOf<IN_ADDR>())
		Console.Write("Gateway Address: {0}\n", requests[1].Data.ToStructure<IN_ADDR>());
	else
		Console.Write("Gateway Address NOT present!\n");
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