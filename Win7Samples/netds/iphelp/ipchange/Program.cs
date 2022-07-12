using System;
using static Vanara.PInvoke.IpHlpApi;
using static Vanara.PInvoke.Ws2_32;
using System.Linq;
using System.Collections.Generic;

namespace recvmsg;

static class Program
{
	const uint INVALID = unchecked((uint)-1);

	public static void Main(string[] args)
	{
		uint Context = INVALID, Index = INVALID;
		string NewIPStr = string.Empty;//[IPADDR_BUF_SIZE];
		string NewMaskStr = string.Empty; // [IPADDR_BUF_SIZE];
		bool OptList = false;
		bool OptAdd = false;
		bool OptDel = false;

		for (int i = 0; i < args.Length; i++)
		{
			if (args[i][0] is '-' or '/')
			{
				switch (char.ToLower(args[i][1]))
				{
					case 'l':
						OptList = true;
						break;
					case 'a':
						OptAdd = true;
						break;
					case 'c':
						if (args[i].Length > 2)
							Context = Convert.ToUInt32(args[i].Substring(2));
						break;
					case 'd':
						OptDel = true;
						break;
					case 'i':
						if (args[i].Length > 2)
							NewIPStr = args[i].Substring(2);
						break;
					case 'm':
						if (args[i].Length > 2)
							NewMaskStr = args[i].Substring(2);
						break;
					case 'n':
						if (args[i].Length > 2)
							Index = Convert.ToUInt32(args[i].Substring(2));
						break;
					default:
						Console.Write("default\n");
						Usage();
						return;
				}
			}
			else
			{
				Console.Write("else\n");
				Usage();
				return;
			}
		}

		// Check options
		if (OptAdd && (Index == INVALID || string.IsNullOrEmpty(NewIPStr) || string.IsNullOrEmpty(NewMaskStr))
			|| OptDel && Context == INVALID)
		{
			Usage();
			return;
		}

		if (OptList)
		{
			// Get sizing information about all adapters
			List<IP_ADAPTER_INFO> pAdapterInfo;
			try
			{
#pragma warning disable CS0618 // Type or member is obsolete
				pAdapterInfo = GetAdaptersInfo().ToList();
#pragma warning restore CS0618 // Type or member is obsolete
			}
			catch (Exception ex)
			{
				Console.WriteLine($"GetAdaptersInfo failed with error {ex.Message}");
				return;
			}

			Console.Write("MAC Address - Adapter\n" +
				"Index Context Ip Address Subnet Mask\n" +
				"--------------------------------------------------------------\n");

			foreach (var pAdapt in pAdapterInfo)
			{
				for (uint i = 0; i < pAdapt.AddressLength; i++)
				{
					if (i == pAdapt.AddressLength - 1)
						Console.Write("{0:X2} - ", pAdapt.Address[i]);
					else
						Console.Write("{0:X2}-", pAdapt.Address[i]);
				}
				Console.WriteLine(pAdapt.AdapterDescription);

				foreach (IP_ADDR_STRING pAddrStr in pAdapt.IpAddresses)
				{
					Console.WriteLine($"{pAdapt.Index,-10}{pAddrStr.Context,-10}{pAddrStr.IpAddress,-20}{pAddrStr.IpMask}");
				}
			}
		}

		if (OptAdd)
		{
			var NewIP = inet_addr(NewIPStr);
			var NewMask = inet_addr(NewMaskStr);
			var Err = AddIPAddress(NewIP, NewMask, Index, out var NTEContext, out _);
			if (Err.Failed)
			{
				Console.WriteLine($"AddIPAddress failed with error {NTEContext}, {Err}");
				return;
			}
		}

		if (OptDel)
		{
			var Err = DeleteIPAddress(Context);
			if (Err.Failed)
				Console.WriteLine($"DeleteIPAddress failed {Err}");
		}
	}

	static void Usage() =>
		Console.Write("Usage: Ipchange [ -l ] [ -a -n<index id> -i<ip address> -m<subnet mask> ] " +
			"[ -d -c<context id>]\n\n" +
			"\t -l List adapter index IDs and IP Address context ID information\n" +
			"\t -a Add IP Address option\n" +
			"\t -d Delete IP Address option\n" +
			"\t -i IP Address to specify with -a option\n" +
			"\t -m Subnet Mask to specify with -a option\n" +
			"\t -c IP context ID for an existing IP address\n" +
			"\t -n Index ID of an existing network adapter\n");
}