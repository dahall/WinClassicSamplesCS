using Vanara.PInvoke;
using static Vanara.PInvoke.IpHlpApi;

IP_INTERFACE_INFO pInterfaceInfo;
Win32Error Err;
int Index = -1;
bool OptList = false;
bool OptRelease = false;
bool OptRenew = false;

if (args.Length < 1)
{
	Usage();
	return;
}
for (int i = 0; i < args.Length; i++)
{
	if (args[i][0] is '-'or '/')
	{
		switch (char.ToLower(args[i][1]))
		{
			case 'l':
				OptList = true;
				break;
			case 'r':
				OptRelease = true;
				if (args[i].Length > 2)
					Index = args[i][2];
				break;
			case 'n':
				OptRenew = true;
				if (args[i].Length > 2)
					Index = args[i][2];
				break;
			default:
				Usage();
				return;
		}
	}
	else
	{
		Usage();
		return;
	}
}

// Check options
if ((OptRelease && Index == -1) || (OptRenew && Index == -1))
{
	Usage();
	return;
}

// Get actual adapter information
if (OptList)
{
	try
	{
		Console.Write("Index     Adapter\n");
		foreach (var adapt in GetAdaptersAddresses().OrderBy(a => a.IfIndex))
		{
			Console.Write("{0:10}{1}\n", adapt.IfIndex, adapt.Description);
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine("GetAdaptersAddresses failed with error {0}\n", ex.Message);
		return;
	}
}

// Get actual adapter information
try
{
	pInterfaceInfo = GetInterfaceInfo();
}
catch (Exception ex)
{
	Console.Write("GetInterfaceInfo failed with error {0}\n", ex.Message);
	return;
}

if (OptRelease)
{
	for (int i = 0; i < pInterfaceInfo.NumAdapters; i++)
	{
		if (Index == pInterfaceInfo.Adapter[i].Index)
		{
			if ((Err = IpReleaseAddress(ref pInterfaceInfo.Adapter[i])) != 0)
			{
				Console.Write("IpReleaseAddress failed with error {0}\n", Err);
				return;
			}
			break;
		}
	}
}


if (OptRenew)
{
	for (int i = 0; i < pInterfaceInfo.NumAdapters; i++)
	{
		if (Index == pInterfaceInfo.Adapter[i].Index)
		{
			if ((Err = IpRenewAddress(ref pInterfaceInfo.Adapter[i])) != 0)
			{
				Console.Write("IpRenewAddress failed with error {0}\n", Err);
				return;
			}
			break;
		}
	}
}

static void Usage() => Console.Write("Usage: Iprenew [ -l ] [ -r<index id> ] [ -n<index id>]\n\n" +
			"\t -l List adapters with corresponding index ID information\n" +
			"\t -r Release IP address for adapter index ID\n" +
			"\t -n Renew IP address for adapter index ID\n");