using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.IScsiDsc;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.SetupAPI;
using static Vanara.PInvoke.Ws2_32;

namespace ISCSI
{
	internal static class ISCSICli
	{
		/// <summary>The scsi request failed.</summary>
		public const uint ISDSC_SCSI_REQUEST_FAILED = 0xEFFF001D;

		public struct VOLUMEMOREINFO
		{
			public VOLUME_DISK_EXTENTS VolumeDiskExtents;
			public string[] VolumePathNames;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct DEVICEINTERFACEENTRY
		{
			public SP_DEVICE_INTERFACE_DATA DeviceInterfaceData;
			public SP_DEVINFO_DATA DeviceInfoData;
			public string DeviceInterfaceDetailData;
			public VOLUMEMOREINFO MoreInfo;
		}

		//#define OffsetToPtr(Base, Offset) ((ref byte)((ref byte)(Base) + (Offset)))

		private static void Usage(uint Code)
		{
			if (Code == 0)
			{
				Console.Write("iscsicli\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 1))
			{
				Console.Write("iscsicli AddTarget <TargetName> <TargetAlias> <TargetPortalAddress>\n");
				Console.Write(" <TargetPortalSocket> <Target flags>\n");
				Console.Write(" <Persist> <Login Flags> <Header Digest> <Data Digest> \n");
				Console.Write(" <Max Connections> <DefaultTime2Wait>\n");
				Console.Write(" <DefaultTime2Retain> <Username> <Password> <AuthType>\n");
				Console.Write(" <Mapping Count> <Target Lun> <OS Bus> <Os Target> \n");
				Console.Write(" <OS Lun> ...\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 2))
			{
				Console.Write("iscsicli RemoveTarget <TargetName> \n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 3))
			{
				Console.Write("iscsicli AddTargetPortal <TargetPortalAddress> <TargetPortalSocket> \n");
				Console.Write(" [HBA Name] [Port Number]\n");
				Console.Write(" <Security Flags>\n");
				Console.Write(" <Login Flags> <Header Digest> <Data Digest> \n");
				Console.Write(" <Max Connections> <DefaultTime2Wait>\n");
				Console.Write(" <DefaultTime2Retain> <Username> <Password> <AuthType>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 4))
			{
				Console.Write("iscsicli RemoveTargetPortal <TargetPortalAddress> <TargetPortalSocket> [HBA Name] [Port Number]\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 5))
			{
				Console.Write("iscsicli RefreshTargetPortal <TargetPortalAddress> <TargetPortalSocket> [HBA Name] [Port Number]\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 6))
			{
				Console.Write("iscsicli ListTargets [ForceUpdate]\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 7))
			{
				Console.Write("iscsicli ListTargetPortals\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 8))
			{
				Console.Write("iscsicli TargetInfo <TargetName> [Discovery Mechanism]\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 9))
			{
				Console.Write("iscsicli LoginTarget <TargetName> <ReportToPNP>\n");
				Console.Write(" <TargetPortalAddress> <TargetPortalSocket>\n");
				Console.Write(" <InitiatorInstance> <Port number> <Security Flags>\n");
				Console.Write(" <Login Flags> <Header Digest> <Data Digest> \n");
				Console.Write(" <Max Connections> <DefaultTime2Wait>\n");
				Console.Write(" <DefaultTime2Retain> <Username> <Password> <AuthType> <Key>\n");
				Console.Write(" <Mapping Count> <Target Lun> <OS Bus> <Os Target> \n");
				Console.Write(" <OS Lun> ...\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 10))
			{
				Console.Write("iscsicli LogoutTarget <SessionId>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 11))
			{
				Console.Write("iscsicli PersistentLoginTarget <TargetName> <ReportToPNP>\n");
				Console.Write(" <TargetPortalAddress> <TargetPortalSocket>\n");
				Console.Write(" <InitiatorInstance> <Port number> <Security Flags>\n");
				Console.Write(" <Login Flags> <Header Digest> <Data Digest> \n");
				Console.Write(" <Max Connections> <DefaultTime2Wait>\n");
				Console.Write(" <DefaultTime2Retain> <Username> <Password> <AuthType> <Key>\n");
				Console.Write(" <Mapping Count> <Target Lun> <OS Bus> <Os Target> \n");
				Console.Write(" <OS Lun> ...\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 12))
			{
				Console.Write("iscsicli ListPersistentTargets\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 13))
			{
				Console.Write("iscsicli RemovePersistentTarget <Initiator Name> <TargetName> \n");
				Console.Write(" <Port Number> \n");
				Console.Write(" <Target Portal Address> \n");
				Console.Write(" <Target Portal Socket> \n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 14))
			{
				Console.Write("iscsicli AddConnection <SessionId> <Initiator Instance>\n");
				Console.Write(" <Port Number> <Target Portal Address>\n");
				Console.Write(" <Target Portal Socket> <Security Flags>\n");
				Console.Write(" <Login Flags> <Header Digest> <Data Digest> \n");
				Console.Write(" <Max Connections> <DefaultTime2Wait>\n");
				Console.Write(" <DefaultTime2Retain> <Username> <Password> <AuthType> <Key>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 15))
			{
				Console.Write("iscsicli RemoveConnection <SessionId> <ConnectionId> \n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 16))
			{
				Console.Write("iscsicli ScsiInquiry <SessionId> <LUN> <EvpdCmddt> <PageCode>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 17))
			{
				Console.Write("iscsicli ReadCapacity <SessionId> <LUN>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 18))
			{
				Console.Write("iscsicli ReportLUNs <SessionId>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 19))
			{
				Console.Write("iscsicli ReportTargetMappings\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 20))
			{
				Console.Write("iscsicli ListInitiators\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 21))
			{
				Console.Write("iscsicli AddiSNSServer <iSNS Server Address>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 22))
			{
				Console.Write("iscsicli RemoveiSNSServer <iSNS Server Address>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 23))
			{
				Console.Write("iscsicli RefreshiSNSServer <iSNS Server Address>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 24))
			{
				Console.Write("iscsicli ListiSNSServers\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 25))
			{
				Console.Write("iscsicli NodeName <node name>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 26))
			{
				Console.Write("iscsicli SessionList <Show Session Info>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 27))
			{
				Console.Write("iscsicli CHAPSecret <chap secret>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 28))
			{
				Console.Write("iscsicli TunnelAddr <Initiator Name> <InitiatorPort> <Destination Address> <Tunnel Address> <Persist>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 29))
			{
				Console.Write("iscsicli GroupKey <Key> <Persist>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 31))
			{
				Console.Write("iscsicli BindPersistentVolumes\n\n");
				Console.Write("iscsicli BindPersistentDevices\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 37))
			{
				Console.Write("iscsicli ReportPersistentDevices\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 38))
			{
				Console.Write("iscsicli AddPersistentDevice <Volume or Device Path>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 39))
			{
				Console.Write("iscsicli RemovePersistentDevice <Volume or Device Path>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 40))
			{
				Console.Write("iscsicli ClearPersistentDevices\n");
				Console.Write("\n");
			}


			if ((Code == 0) || (Code == 42))
			{
				Console.Write("iscsicli GetPSKey <Initiator Name> <initiator Port> <Id Type> <Id>\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 30))
			{
				Console.Write("iscsicli PSKey <Initiator Name> <initiator Port> <Security Flags> <Id Type> <Id> <Key> <persist>\n");
				Console.Write("\n");
			}


			if (Code == 0)
			{
				Console.Write("Quick Commands\n\n");
			}

			if ((Code == 0) || (Code == 33))
			{
				Console.Write("iscsicli QLoginTarget <TargetName> [CHAP Username] [CHAP Password]\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 34))
			{
				Console.Write("iscsicli QAddTarget <TargetName> <TargetPortalAddress>\n");
				Console.Write("\n");
			}


			if ((Code == 0) || (Code == 35))
			{
				Console.Write("iscsicli QAddTargetPortal <TargetPortalAddress>\n");
				Console.Write(" [CHAP Username] [CHAP Password]\n");
				Console.Write("\n");
			}

			if ((Code == 0) || (Code == 36))
			{
				Console.Write("iscsicli QAddConnection <SessionId> <Initiator Instance>\n");
				Console.Write(" <Target Portal Address>\n");
				Console.Write(" [CHAP Username] [CHAP Password]\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 1) ||
			(Code == 9) ||
			(Code == 11))
			{
				Console.Write("Target Mappings:\n");
				Console.Write(" <Target Lun> is the LUN value the target uses to expose the LUN.\n");
				Console.Write(" It must be in the form 0x0123456789abcdef\n");
				Console.Write(" <OS Bus> is the bus number the OS should use to surface the LUN\n");
				Console.Write(" <OS Target> is the target number the OS should use to surface the LUN\n");
				Console.Write(" <OS LUN> is the LUN number the OS should use to surface the LUN\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 30) ||
			(Code == 42))
			{
				Console.Write("Payload Id Type:\n");
				Console.Write(" ID_IPV4_ADDR is 1 - Id format is 1.2.3.4\n");
				Console.Write(" ID_FQDN is 2 - Id format is ComputerName\n");
				Console.Write(" ID_IPV6_ADDR is 5 - Id form is IPv6 Address\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 3) ||
			(Code == 9) ||
			(Code == 11) ||
			(Code == 14) ||
			(Code == 30))
			{
				Console.Write("Security Flags:\n");
				Console.Write(" TunnelMode is 0x00000040\n");
				Console.Write(" TransportMode is 0x00000020\n");
				Console.Write(" PFS Enabled is 0x00000010\n");
				Console.Write(" Aggressive Mode is 0x00000008\n");
				Console.Write(" Main mode is 0x00000004\n");
				Console.Write(" IPSEC/IKE Enabled is 0x00000002\n");
				Console.Write(" Valid Flags is 0x00000001\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 1) ||
			(Code == 3) ||
			(Code == 9) ||
			(Code == 11) ||
			(Code == 14))
			{
				Console.Write("Login Flags:\n");
				Console.Write(" ISCSI_LOGIN_FLAG_REQUIRE_IPSEC 0x00000001\n");
				Console.Write(" IPsec is required for the operation\n\n");
				Console.Write(" ISCSI_LOGIN_FLAG_MULTIPATH_ENABLED 0x00000002\n");
				Console.Write(" Multipathing is enabled for the target on this initiator\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 1) ||
			(Code == 3) ||
			(Code == 9) ||
			(Code == 11) ||
			(Code == 14))
			{
				Console.Write("AuthType:\n");
				Console.Write(" ISCSI_NO_AUTH_TYPE,\n");
				Console.Write(" No iSCSI in-band authenticiation is used\n\n");
				Console.Write(" ISCSI_CHAP_AUTH_TYPE = 1,\n");
				Console.Write(" One way CHAP (Target authenticates initiator is used)\n\n");
				Console.Write(" ISCSI_MUTUAL_CHAP_AUTH_TYPE = 2\n");
				Console.Write(" Mutual CHAP (Target and Initiator authenticate each other is used)\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 1))
			{
				Console.Write("Target Flags:\n");
				Console.Write(" ISCSI_TARGET_FLAG_HIDE_STATIC_TARGET 0x00000002\n");
				Console.Write(" If this flag is set then the target will never be reported unless it\n");
				Console.Write(" is also discovered dynamically.\n\n");

				Console.Write(" ISCSI_TARGET_FLAG_MERGE_TARGET_INFORMATION 0x00000004\n");
				Console.Write(" If this flag is set then the target information passed will be\n");
				Console.Write(" merged with any target information already statically configured for\n");
				Console.Write(" the target\n");
				Console.Write("\n");
			}

			if ((Code == 0) ||
			(Code == 1) ||
			(Code == 3) ||
			(Code == 9) ||
			(Code == 11) ||
			(Code == 14) ||
			(Code == 27) ||
			(Code == 30) ||
			(Code == 33) ||
			(Code == 35) ||
			(Code == 36))
			{
				Console.Write("CHAP secrets, CHAP passwords and IPSEC preshared keys can be specified as\n");
				Console.Write("a text string or as a sequence of hexadecimal values. The value specified on\n");
				Console.Write("the command line is always considered a string unless the first two characters\n");
				Console.Write("0x in which case it is considered a hexadecimal value.\n");
				Console.Write("\n");
				Console.Write("For example 0x12345678 specifies a 4 byte secret\n");
				Console.Write("\n");
			}

			Console.Write("All numerical values are assumed decimal unless preceeded by 0x. If\n");
			Console.Write("preceeded by 0x then value is assumed to be hex\n");
			Console.Write("\n");

			if (Code == 0)
			{
				Console.Write("iscsicli can also be run in command line mode where iscsicli commands\n");
				Console.Write("can be entered directly from the console. To enter command line\n");
				Console.Write("mode, just run iscsicli without any parameters\n");
				Console.Write("\n");
			}
		}

		private static string GetiSCSIMessageText(Win32Error Status) =>
			FormatMessage((uint)Status, null, GetModuleHandle("iscsidsc.dll"),
				FormatMessageFlags.FORMAT_MESSAGE_MAX_WIDTH_MASK | FormatMessageFlags.FORMAT_MESSAGE_FROM_SYSTEM | FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE);

		private static void PrintSecurityFlags(string Indent, ISCSI_SECURITY_FLAGS SecurityFlags)
		{
			Console.Write("{0}Security Flags : 0x{1:X}\n", Indent, SecurityFlags);

			if (SecurityFlags.IsFlagSet(ISCSI_SECURITY_FLAGS.ISCSI_SECURITY_FLAG_TUNNEL_MODE_PREFERRED))
			{
				Console.Write("{0} Tunnel Mode Preferred\n", Indent);
			}

			if (SecurityFlags.IsFlagSet(ISCSI_SECURITY_FLAGS.ISCSI_SECURITY_FLAG_TRANSPORT_MODE_PREFERRED))
			{
				Console.Write("{0} Transport Mode Preferred\n", Indent);
			}

			if (SecurityFlags.IsFlagSet(ISCSI_SECURITY_FLAGS.ISCSI_SECURITY_FLAG_PFS_ENABLED))
			{
				Console.Write("{0} PFS Enabled\n", Indent);
			}

			if (SecurityFlags.IsFlagSet(ISCSI_SECURITY_FLAGS.ISCSI_SECURITY_FLAG_AGGRESSIVE_MODE_ENABLED))
			{
				Console.Write("{0} Aggressive Mode Enabled\n", Indent);
			}

			if (SecurityFlags.IsFlagSet(ISCSI_SECURITY_FLAGS.ISCSI_SECURITY_FLAG_IKE_IPSEC_ENABLED))
			{
				Console.Write("{0} IPSEC Enabled\n", Indent);
			}

			if (SecurityFlags.IsFlagSet(ISCSI_SECURITY_FLAGS.ISCSI_SECURITY_FLAG_VALID))
			{
				Console.Write("{0} Security Flags are Valid\n", Indent);
			}
		}

		private static void PrintTargetMapping(in ISCSI_TARGET_MAPPING Mapping)
		{
			Console.Write(" Session Id : {0:X}-{1:X}\n" +
				" Target Name : {2}\n" +
				" Initiator : {3}\n" +
				" Initiator Scsi Device : {4}\n" +
				" Initiator Bus : {5}\n" +
				" Initiator Target Id : {6}\n",
				Mapping.SessionId.AdapterUnique,
				Mapping.SessionId.AdapterSpecific,
				Mapping.TargetName,
				Mapping.InitiatorName,
				Mapping.OSDeviceName,
				Mapping.OSBusNumber,
				Mapping.OSTargetNumber);

			SCSI_LUN_LIST[] LUNList = Mapping.LUNList.ToArray<SCSI_LUN_LIST>((int)Mapping.LUNCount);
			for (var j = 0; j < Mapping.LUNCount; j++)
			{
				Console.Write(" Target LUN: 0x{0:X} <-. OS Lun: 0x{1:X}\n", LUNList[j].TargetLUN, LUNList[j].OSLUN);
			}
			Console.Write("\n");
		}

		private static void PrintStringList(string Title, string Spacer, IEnumerable<string> List)
		{
			Console.Write("{0}\n", Title);
			foreach (var s in List)
				Console.Write("{0}\"{1}\"\n", Spacer, s);
		}

		private static void PrintLoginOptions(string Header, in ISCSI_LOGIN_OPTIONS LoginOptions)
		{
			Console.Write("{0}Version : {1}\n", Header, LoginOptions.Version);
			Console.Write("{0}Information Specified: 0x{1:X}\n", Header, LoginOptions.InformationSpecified);

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_HEADER_DIGEST))
			{
				Console.Write("{0}Header Digest : ", Header);
				if (LoginOptions.HeaderDigest == ISCSI_DIGEST_TYPES.ISCSI_DIGEST_TYPE_NONE)
				{
					Console.Write("None\n");
				}
				else if (LoginOptions.HeaderDigest == ISCSI_DIGEST_TYPES.ISCSI_DIGEST_TYPE_CRC32C)
				{
					Console.Write("CRC-32C\n");
				}
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_DATA_DIGEST))
			{
				Console.Write("{0}Data Digest : ", Header);
				if (LoginOptions.DataDigest == ISCSI_DIGEST_TYPES.ISCSI_DIGEST_TYPE_NONE)
				{
					Console.Write("None\n");
				}
				else if (LoginOptions.DataDigest == ISCSI_DIGEST_TYPES.ISCSI_DIGEST_TYPE_CRC32C)
				{
					Console.Write("CRC-32C\n");
				}
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_MAXIMUM_CONNECTIONS))
			{
				Console.Write("{0}Maximum Connections : {1}\n", Header, LoginOptions.MaximumConnections);
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_DEFAULT_TIME_2_WAIT))
			{
				Console.Write("{0}Default Time 2 Wait : {1}\n", Header, LoginOptions.DefaultTime2Wait);
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_DEFAULT_TIME_2_RETAIN))
			{
				Console.Write("{0}Default Time 2 Retain: {1}\n", Header, LoginOptions.DefaultTime2Retain);
			}

			Console.Write("{0}Login Flags : 0x{1:X}\n", Header, LoginOptions.LoginFlags);

			if (LoginOptions.LoginFlags.IsFlagSet(ISCSI_LOGIN_FLAGS.ISCSI_LOGIN_FLAG_REQUIRE_IPSEC))
			{
				Console.Write("{0} Require IPsec\n", Header);
			}

			if (LoginOptions.LoginFlags.IsFlagSet(ISCSI_LOGIN_FLAGS.ISCSI_LOGIN_FLAG_MULTIPATH_ENABLED))
			{
				Console.Write("{0} Multipath Enabled\n", Header);
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_AUTH_TYPE))
			{
				Console.Write("{0}Authentication Type : ", Header);
				switch (LoginOptions.AuthType)
				{
					case ISCSI_AUTH_TYPES.ISCSI_NO_AUTH_TYPE:
						Console.Write("None\n");
						break;
					case ISCSI_AUTH_TYPES.ISCSI_CHAP_AUTH_TYPE:
						Console.Write("CHAP\n");
						break;
					case ISCSI_AUTH_TYPES.ISCSI_MUTUAL_CHAP_AUTH_TYPE:
						Console.Write("Mutual CHAP\n");
						break;
					default:
						Console.Write("Unknown - {1}\n", LoginOptions.AuthType);
						break;
				}
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_USERNAME))
			{
				Console.Write("{0}Username : \n", Header);
				Console.Write(LoginOptions.Username.ToHexDumpString((int)LoginOptions.UsernameLength));
			}

			if (LoginOptions.InformationSpecified.IsFlagSet(ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_PASSWORD))
			{
				Console.Write("{0}Password : <is established>\n", Header);
			}
		}

		private static bool StringToSessionId(string str, out ISCSI_UNIQUE_SESSION_ID SessionId)
		{
			// Session id is in the form of "0x1234567812345678-0x1234567812345678"
			try
			{
				var spl = str.Split('-');
				if (spl.Length == 2)
				{
					SessionId = new ISCSI_UNIQUE_SESSION_ID
					{
						AdapterUnique = ulong.Parse(spl[0], System.Globalization.NumberStyles.HexNumber),
						AdapterSpecific = ulong.Parse(spl[1], System.Globalization.NumberStyles.HexNumber),
					};
					return true;
				}
			}
			catch { }
			SessionId = default;
			return false;
		}

		private static bool stoiDForLogicalUnit(string x, out ulong Value)
		{
			Value = 0;
			return x.Length == 18 && ulong.TryParse(x, System.Globalization.NumberStyles.HexNumber, null, out Value);
		}

		private static void ParseLoginOptions(out ISCSI_LOGIN_OPTIONS LoginOptions, string[] ArgV, uint ArgCIndex)
		{
			LoginOptions = default;
			SafeAllocatedMemoryHandle un, pwd;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.LoginFlags = (ISCSI_LOGIN_FLAGS)uint.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;


			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_HEADER_DIGEST;
				LoginOptions.HeaderDigest = (ISCSI_DIGEST_TYPES)int.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_DATA_DIGEST;
				LoginOptions.DataDigest = (ISCSI_DIGEST_TYPES)int.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_MAXIMUM_CONNECTIONS;
				LoginOptions.MaximumConnections = uint.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_DEFAULT_TIME_2_WAIT;
				LoginOptions.DefaultTime2Wait = uint.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_DEFAULT_TIME_2_RETAIN;
				LoginOptions.DefaultTime2Retain = uint.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_USERNAME;

				if (!ArgV[ArgCIndex].StartsWith('-'))
				{
					LoginOptions.Username = un = new SafeCoTaskMemString(ArgV[ArgCIndex], CharSet.Ansi);
					LoginOptions.UsernameLength = un.Size - 1U;
				}
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_PASSWORD;
				if (!ArgV[ArgCIndex].StartsWith('-'))
				{
					var Secret = ArgV[ArgCIndex].ToUpperInvariant();
					if (Secret.StartsWith("0X"))
					{
						_ = ParseHexString(Secret[2..], out pwd);
						LoginOptions.Password = pwd;
						LoginOptions.PasswordLength = pwd.Size;
					}
					else
					{
						LoginOptions.Password = pwd = new SafeCoTaskMemString(ArgV[ArgCIndex], CharSet.Ansi);
						LoginOptions.PasswordLength = pwd.Size - 1U;
					}
				}
			}
			ArgCIndex++;

			if (!ArgV[ArgCIndex].StartsWith('*'))
			{
				LoginOptions.InformationSpecified |= ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_AUTH_TYPE;
				LoginOptions.AuthType = (ISCSI_AUTH_TYPES)int.Parse(ArgV[ArgCIndex]);
			}
			ArgCIndex++;
		}

		private static Win32Error ParseHexString(string s, out SafeAllocatedMemoryHandle BufPtr)
		{
			BufPtr = SafeCoTaskMemHandle.Null;
			if (s.Length % 2 != 0)
				return Win32Error.ERROR_INVALID_PARAMETER;
			BufPtr = new SafeCoTaskMemHandle(s.Length / 2);
			for (var i = 0; i < s.Length; i += 2)
				BufPtr.DangerousGetHandle().Write(sbyte.Parse(s.Substring(i, 2), System.Globalization.NumberStyles.HexNumber), i / 2, BufPtr.Size);
			return Win32Error.ERROR_SUCCESS;
		}

		private static bool IsTrue(string s, bool Default)
		{
			s = s.ToLowerInvariant();
			if (s.StartsWith('*'))
				return (Default);

			return s.StartsWith('t');
		}

		// iscsicli TunnelAddr <Initiator Name> <InitiatorPort> <Destination Address> <Tunnel Address> <Persist>
		private static Win32Error TunnelAddress(string[] ArgV)
		{
			string Initiator, DestAddress, TunnelAddress;
			bool Persist;
			uint InitiatorPort;

			if (ArgV.Length != 6)
			{
				Usage(28);
				return (Win32Error.ERROR_SUCCESS);
			}

			Initiator = ArgV[1];
			if (Initiator.StartsWith('*'))
			{
				Initiator = default;
			}
			if (ArgV[3].StartsWith('*'))
			{
				InitiatorPort = ISCSI_ALL_INITIATOR_PORTS;
			}
			else
			{
				InitiatorPort = uint.Parse(ArgV[2]);
			}

			DestAddress = ArgV[3];
			if (DestAddress.StartsWith('*'))
			{
				DestAddress = default;
			}

			TunnelAddress = ArgV[4];
			if (TunnelAddress.StartsWith('*'))
			{
				TunnelAddress = default;
			}

			Persist = IsTrue(ArgV[5], true);

			return SetIScsiTunnelModeOuterAddress(Initiator, InitiatorPort, DestAddress, TunnelAddress, Persist);
		}

		// iscsicli GroupKey <Key> <Persist>
		private static Win32Error GroupKey(string[] ArgV)
		{
			if (ArgV.Length != 3)
			{
				Usage(29);
				return (Win32Error.ERROR_SUCCESS);
			}

			string Key = null;
			uint KeyLength = 0;

			if (!ArgV[1].StartsWith('*'))
			{
				Key = ArgV[1];
				KeyLength = (uint)Key.Length + 1;
			}

			var Persist = IsTrue(ArgV[2], true);
			return SetIScsiGroupPresharedKey(KeyLength, Key, Persist);
		}

		// iscsicli CHAPSecret <secret>
		private static Win32Error CHAPSecret(string[] ArgV)
		{
			Win32Error Status = Win32Error.ERROR_SUCCESS;
			SafeAllocatedMemoryHandle Key = SafeCoTaskMemHandle.Null;
			uint KeyLength = 0;
			string Secret;

			if (ArgV.Length != 2)
			{
				Usage(27);
				return (Status);
			}

			Secret = ArgV[1];
			if (!Secret.StartsWith('*'))
			{
				if (Secret.ToLower().StartsWith("0x"))
				{
					Status = ParseHexString(Secret[2..], out Key);
				}
				else
				{
					Key = new SafeCoTaskMemString(ArgV[1], CharSet.Ansi);
					Status = Win32Error.ERROR_SUCCESS;
				}
			}

			if (Status == Win32Error.ERROR_SUCCESS)
			{
				Status = SetIScsiInitiatorCHAPSharedSecret(KeyLength, Key);
			}

			Key?.Dispose();
			return (Status);
		}

		// Console.Write("iscsicli BindPersistentVolumes\n");
		private static Win32Error BindPeristentVolumes(string[] ArgV) => SetupPersistentIScsiVolumes();

		// Console.Write("iscsicli ClearPersistentVolumes\n");
		private static Win32Error ClearPersistentVolumes(string[] ArgV) => ClearPersistentIScsiDevices();

		// Console.Write("iscsicli AddPersistentVolume <Volume Path>\n");
		private static Win32Error AddPersistentVolume(string[] ArgV)
		{
			if (ArgV.Length != 2)
			{
				Usage(38);
				return Win32Error.ERROR_SUCCESS;
			}
			else
			{
				return AddPersistentIScsiDevice(ArgV[1]);
			}
		}

		// Console.Write("iscsicli RemovePersistentVolume <Volume Path>\n");
		private static Win32Error RemovePersistentVolume(string[] ArgV)
		{
			if (ArgV.Length != 2)
			{
				Usage(39);
				return Win32Error.ERROR_SUCCESS;
			}
			else
			{
				return RemovePersistentIScsiDevice(ArgV[1]);
			}
		}

		// Console.Write("iscsicli ReportPersistentVolumes\n");
		private static Win32Error ReportPersistentVolumes(string[] ArgV)
		{
			uint SizeNeeded = 0;
			Win32Error Status = ReportPersistentIScsiDevices(ref SizeNeeded, default);
			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				using var Buffer = new SafeHGlobalHandle(SizeNeeded * StringHelper.GetCharSize());
				Status = ReportPersistentIScsiDevices(ref SizeNeeded, Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					PrintStringList("Persistent Volumes", "", Buffer.ToStringEnum());
					Console.Write("\n");
				}
			}
			else
			{
				Status = Win32Error.ERROR_NOT_ENOUGH_MEMORY;
			}

			return (Status);
		}

		// iscsicli NodeName <node name>
		private static Win32Error NodeName(string[] ArgV)
		{
			if (ArgV.Length != 2)
			{
				Usage(25);
				return (Win32Error.ERROR_SUCCESS);
			}

			var NodeName = !ArgV[1].StartsWith('*') ? default : ArgV[1];

			return SetIScsiInitiatorNodeName(NodeName);
		}

		/*
		Description:

		This routine will determine if the disk represented by a disk
		device number is part of a volume

		Arguments:

		DeviceNumber is the disk device number

		Volume is the VOLUME_DISK_EXTENTS for the volume


		Return Values:

		Status

		*/
		private static bool DiscpIsDeviceNumberInVolume(uint DeviceNumber, in Kernel32.VOLUME_DISK_EXTENTS Volume)
		{
			for (uint i = 0; i < Volume.NumberOfDiskExtents; i++)
			{
				if (Volume.Extents[i].DiskNumber == DeviceNumber)
				{
					return (true);
				}
			}
			return (false);
		}

		/*++

		Routine Description:

		This routine will see if the volume passed or any volumes mounted
		on the volume passed are the volume that we are looking for and if
		so then add the path to the volume to the list

		Arguments:

		Name is the volume name for the volume on which to look for volume
		paths that are for VolumeNameToFind

		VolumeNameToFind is the name of the volume we are searching for 

		*VolumePath returns with a pointer to the list of volume paths.
		Each path in the list is nul terminated with the last path
		double nul terminated. The caller must free this buffer. 

		*VolumePathLen returns with the number of bytes in the
		VolumePath buffer

		Return Value:

		Status

		--*/
		private static Win32Error DiscpVolumeMountList(string Name, string VolumeNameToFind, List<string> VolumePath)
		{
			Win32Error Status = Win32Error.ERROR_SUCCESS;

			//
			// See if the mount point is for our volume name
			//
			var VolumeName = new StringBuilder(MAX_PATH);
			var b = GetVolumeNameForVolumeMountPoint(Name, VolumeName, MAX_PATH);
			if (b)
			{
				if (string.Compare(VolumeName.ToString(), VolumeNameToFind, true) == 0)
				{
					//
					// we found a mountpoint for our volume name,
					// lets add it to the list
					//
					VolumePath.Add(Name);
				}
				else
				{
					string LinkName1, LinkName2;
					if ((LinkName1 = QueryDosDevice(VolumeName.ToString()[4..48]).FirstOrDefault()) != null &&
						(LinkName2 = QueryDosDevice(VolumeNameToFind[4..48]).FirstOrDefault()) != null)
					{
						if (string.Compare(LinkName1, LinkName2) == 0)
						{
							VolumePath.Add(Name);
						}
					}
				}

				var VolumeMountPoint = new StringBuilder(MAX_PATH);
				using SafeVolumeMountPointHandle h = FindFirstVolumeMountPoint(VolumeName.ToString(), VolumeMountPoint, MAX_PATH);
				if (!h.IsInvalid)
				{
					do
					{
						var MountPointPath = Name + VolumeMountPoint;

						Status = DiscpVolumeMountList(MountPointPath, VolumeNameToFind, VolumePath);

						b = FindNextVolumeMountPoint(h, VolumeMountPoint, MAX_PATH);

					} while ((Status == Win32Error.ERROR_SUCCESS) && b);
				}
			}

			return (Status);
		}

		/*++

		Routine Description:

		My implementation of the GetVolumePathNamesForVolumeName
		functionality, but for W2K. What we need to do is to find the
		volume name for every drive letter and then the volume name for
		every mount point and if any of them match our volume name then
		we've got a mapping. Note there could be multiple paths for a
		volume name.

		Arguments:

		VolumeNameToFind is the name of the volume

		*VolumePath returns with a pointer to the list of volume paths.
		Each path in the list is nul terminated with the last path
		double nul terminated. The caller must free this buffer. 

		*VolumePathLen returns with the number of characters in the
		VolumePath buffer

		Return Value:

		Status

		--*/
		private static Win32Error DiscpGetVolumePathNamesForVolumeName(string VolumeNameToFind, out List<string> VolumePath)
		{

			//
			// Initialize output volume path list to double nul
			//
			VolumePath = new List<string>();

			//
			// Loop through all drive letters looking for mount points that
			// match our volume name
			//
			var Drive = new StringBuilder(" :\\");
			Win32Error Status = Win32Error.ERROR_SUCCESS;
			for (var c = 'C'; ((c < ('Z' + 1)) && (Status == Win32Error.ERROR_SUCCESS)); c++)
			{
				Drive[0] = c;
				Status = DiscpVolumeMountList(Drive.ToString(), VolumeNameToFind, VolumePath);
			}

			return (Status);
		}

		/*++

		Routine Description:

		This routine will map a volume name to the volume paths for it. XP
		and W2003 have a nifty function that does this easily, but it is
		not available on W2K. So this routine will figure out if we are on
		W2K or not and do it the hard way on W2K or the easy way on XP and
		W2003. This api should behave in a functionally identical way to
		the GetVolumePathNamesForVolumeName api.

		Arguments:

		VolumeName is the name of the volume

		*VolumePath returns with a pointer to the list of volume paths.
		Each path in the list is nul terminated with the last path
		double nul terminated. The caller must free this buffer. 

		*VolumePathLen returns with the number of characters in the
		VolumePath buffer

		Return Value:

		Status

		--*/
		private static Win32Error DiscpVolumeNameToVolumePath(string VolumeName, out List<string> VolumePath)
		{
			Win32Error Status;

			VolumePath = new List<string>();
			var VersionInfo = new OSVERSIONINFOEX { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OSVERSIONINFOEX>() };
			if (GetVersionEx(ref VersionInfo))
			{
				if ((VersionInfo.dwMajorVersion == 5) && (VersionInfo.dwMinorVersion == 0))
				{
					//
					// We are on W2K so we need to do the mapping from volume
					// name to VolumePath the hard way
					//
					Status = DiscpGetVolumePathNamesForVolumeName(VolumeName, out VolumePath);
				}
				else
				{
					//
					// Since we are on XP or W2003 then we can use the
					// advanced API so load it up from kernel32.dll
					//
					var b = GetVolumePathNamesForVolumeName(VolumeName, null, 0, out var CharNeeded);

					Status = b ? Win32Error.ERROR_SUCCESS : GetLastError();

					if (Status == Win32Error.ERROR_MORE_DATA)
					{
						using var p = new SafeCoTaskMemHandle(CharNeeded * StringHelper.GetCharSize());
						b = GetVolumePathNamesForVolumeName(VolumeName, p, CharNeeded, out CharNeeded);

						Status = b ? Win32Error.ERROR_SUCCESS : GetLastError();
						if (Status == Win32Error.ERROR_SUCCESS)
						{
							VolumePath = p.ToStringEnum().ToList();
						}
					}
					else
					{
						Status = GetLastError();
					}
				}
			}
			else
			{
				Status = GetLastError();
			}

			return (Status);
		}

		private delegate Win32Error ENUMDEVICEINTERFACECALLBACK(IntPtr Context, in Guid Guid, HDEVINFO DevInfo, ref DEVICEINTERFACEENTRY DevEntry);

		private static Win32Error DiscpEnumerateDeviceInterfaces(in Guid Guid, ENUMDEVICEINTERFACECALLBACK Callback, IntPtr Context,
			out List<DEVICEINTERFACEENTRY> List)
		{
			Win32Error Status = Win32Error.ERROR_SUCCESS;
			List = new List<DEVICEINTERFACEENTRY>();

			//
			// get info on all exsiting disk devices
			//
			using SafeHDEVINFO DevInfo = SetupDiGetClassDevs(Guid,
				default, // string Enumerator, OPTIONAL
				default, // HWND hwndParent, OPTIONAL
				DIGCF.DIGCF_DEVICEINTERFACE | DIGCF.DIGCF_PRESENT);

			if (!DevInfo.IsInvalid)
			{
				foreach (SP_DEVICE_INTERFACE_DATA diData in SetupDiEnumDeviceInterfaces(DevInfo, Guid))
				{
					var e = new DEVICEINTERFACEENTRY();
					var b = SetupDiGetDeviceInterfaceDetail(DevInfo, e.DeviceInterfaceData, out e.DeviceInterfaceDetailData, out e.DeviceInfoData);
					if (b)
					{
						// if we've successfully gathered all of our info, then we do the callout for the specific work
						Status = Callback?.Invoke(Context, Guid, DevInfo, ref e) ?? Win32Error.ERROR_SUCCESS;
					}
					else
					{
						Status = Win32Error.GetLastError();
					}
					if (Status != Win32Error.ERROR_SUCCESS)
						break;
				}
			}
			else
			{
				Status = Win32Error.GetLastError();
			}
			return (Status);
		}

		/*++

		Routine Description:

		This routine is the volume device interface callback. It gets
		additional information about each volume that is found. Information
		includes the disk extents and the volume path names.

		Arguments:

		Context is not used

		Guid is the guid for the volume device interface

		DevInfo is the DevInfo set for the enumeration

		DevEntry is the device interface information structure. This
		routine will insert a pointer to the VOLUME_DISK_EXTENTS for
		the device interface

		Return Value:

		Status

		--*/
		private static Win32Error DiscpVolumeDeviceInterfaceCallback(IntPtr Context, in Guid Guid, HDEVINFO DevInfo, ref DEVICEINTERFACEENTRY DevEntry)
		{
			Win32Error Status;

			var VolumeName = new StringBuilder(MAX_PATH);
			var b = GetVolumeNameForVolumeMountPoint(DevEntry.DeviceInterfaceDetailData + '\\', VolumeName, MAX_PATH);
			if (b)
			{
				//
				// Now we have the volume name, we can get the volume paths
				// that can access it. First figure out the size needed for
				// the pathname buffer
				//
				Status = DiscpVolumeNameToVolumePath(VolumeName.ToString(), out List<string> VolumePath);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					//
					// Get the disk extents for the volume
					//
					using SafeHFILE Handle = CreateFile(DevEntry.DeviceInterfaceDetailData,
						FileAccess.GENERIC_READ, // access mode
						System.IO.FileShare.ReadWrite, // share mode
						default,
						System.IO.FileMode.Open,
						FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, // file attributes
						default);

					if (!Handle.IsInvalid)
					{
						var VolumeMoreInfo = new VOLUMEMOREINFO();
						b = DeviceIoControl(Handle, IOControlCode.IOCTL_VOLUME_GET_VOLUME_DISK_EXTENTS, out VolumeMoreInfo.VolumeDiskExtents);
						if (b)
						{
							// And if all goes well, get the volume path names for the volume
							VolumeMoreInfo.VolumePathNames = VolumePath.ToArray();
							Status = Win32Error.ERROR_SUCCESS;
							DevEntry.MoreInfo = VolumeMoreInfo;
						}
						else
						{
							DevEntry.MoreInfo = default;
							Status = Win32Error.GetLastError();
						}
					}
					else
					{
						Status = Win32Error.GetLastError();
					}
				}
			}
			else
			{
				Status = Win32Error.GetLastError();
			}

			return (Status);
		}

		public static readonly Guid GUID_DEVINTERFACE_DISK = new Guid(0x53f56307, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_CDROM = new Guid(0x53f56308, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_PARTITION = new Guid(0x53f5630a, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_TAPE = new Guid(0x53f5630b, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_WRITEONCEDISK = new Guid(0x53f5630c, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_VOLUME = new Guid(0x53f5630d, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_MEDIUMCHANGER = new Guid(0x53f56310, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_FLOPPY = new Guid(0x53f56311, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_CDCHANGER = new Guid(0x53f56312, 0xb6bf, 0x11d0, 0x94, 0xf2, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_STORAGEPORT = new Guid(0x2accfe60, 0xc130, 0x11d2, 0xb0, 0x82, 0x00, 0xa0, 0xc9, 0x1e, 0xfb, 0x8b);
		public static readonly Guid GUID_DEVINTERFACE_VMLUN = new Guid(0x6f416619, 0x9f29, 0x42a5, 0xb2, 0x0b, 0x37, 0xe2, 0x19, 0xca, 0x02, 0xb0);
		public static readonly Guid GUID_DEVINTERFACE_SES = new Guid(0x1790c9ec, 0x47d5, 0x4df3, 0xb5, 0xaf, 0x9a, 0xdf, 0x3c, 0xf2, 0x3e, 0x48);
		public static readonly Guid GUID_DEVINTERFACE_SERVICE_VOLUME = new Guid(0x6ead3d82, 0x25ec, 0x46bc, 0xb7, 0xfd, 0xc1, 0xf0, 0xdf, 0x8f, 0x50, 0x37);
		public static readonly Guid GUID_DEVINTERFACE_HIDDEN_VOLUME = new Guid(0x7f108a28, 0x9833, 0x4b3b, 0xb7, 0x80, 0x2c, 0x6b, 0x5f, 0xa5, 0xc0, 0x62);
		public static readonly Guid GUID_DEVINTERFACE_UNIFIED_ACCESS_RPMB = new Guid(0x27447c21, 0xbcc3, 0x4d07, 0xa0, 0x5b, 0xa3, 0x39, 0x5b, 0xb4, 0xee, 0xe7);
		public static readonly Guid WDI_STORAGE_PREDICT_FAILURE_DPS_GUID = new Guid(0xe9f2d03a, 0x747c, 0x41c2, 0xbb, 0x9a, 0x02, 0xc6, 0x2b, 0x6d, 0x5f, 0xcb);

		private static string DeviceTypeFromGuid(in Guid Guid) => Guid switch
		{
			var g when g == GUID_DEVINTERFACE_DISK => "Disk",
			var g when g == GUID_DEVINTERFACE_TAPE => "Tape",
			var g when g == GUID_DEVINTERFACE_CDROM => "CDRom",
			var g when g == GUID_DEVINTERFACE_WRITEONCEDISK => "Write Once Disk",
			var g when g == GUID_DEVINTERFACE_CDCHANGER => "CD Changer",
			var g when g == GUID_DEVINTERFACE_MEDIUMCHANGER => "Medium Changer",
			var g when g == GUID_DEVINTERFACE_FLOPPY => "Floppy",
			_ => "Unknown"
		};

		/*
		Description:

		This routine will return the index of the volume that the device
		belongs. 

		Arguments:

		DeviceNumber has the disk device number

		VolumeCount has the number of volumes

		VolumeList has information about the voluem device interfaces

		*VoluemIndex on entry has the next index in the list to being the
		search. On return it has the index into volume list for the
		next match. 

		Return Values:

		Status

		*/
		private static Win32Error GetMountPointsFromDeviceNumber(uint DeviceNumber, IList<DEVICEINTERFACEENTRY> VolumeList, ref int VolumeIndex)
		{
			var Status = Win32Error.ERROR_INVALID_PARAMETER;

			for (var j = VolumeIndex; j < VolumeList.Count; j++)
			{
				if (VolumeList[j].MoreInfo.VolumeDiskExtents.NumberOfDiskExtents > 0)
				{
					if (DiscpIsDeviceNumberInVolume(DeviceNumber, VolumeList[j].MoreInfo.VolumeDiskExtents))
					{
						VolumeIndex = j;
						Status = Win32Error.ERROR_SUCCESS;
						break;
					}
				}
			}
			return (Status);
		}

		// iscsicli SessionList
		private static Win32Error SessionList(string[] ArgV)
		{
			ISCSI_SESSION_INFO[] SessionInfo = null;
			var ShowDeviceInfo = true;
			Win32Error StatusDontCare;

			if ((ArgV.Length < 1) || (ArgV.Length > 2))
			{
				Usage(26);
				return (Win32Error.ERROR_SUCCESS);
			}

			if (ArgV.Length == 2)
			{
				ShowDeviceInfo = IsTrue(ArgV[1], true);
			}

			var SizeNeeded = 0U;
			Win32Error Status = GetIScsiSessionList(ref SizeNeeded, out var SessionCount, default);

			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				using var mem = new SafeCoTaskMemHandle(SizeNeeded);
				Status = GetIScsiSessionList(ref SizeNeeded, out SessionCount, mem);
				if (Status.Succeeded)
					SessionInfo = mem.ToArray<ISCSI_SESSION_INFO>((int)SessionCount);
			}
			else
			{
				SessionCount = 0;
			}

			if (Status == Win32Error.ERROR_SUCCESS && SessionCount > 0)
			{
				Console.Write("Total of {0} sessions\n\n", SessionCount);

				foreach (ISCSI_SESSION_INFO si in SessionInfo)
				{
					Console.Write("Session Id : {0:X}-{1:X}\n" +
						"Initiator Node Name :{2}\n" +
						"Target Node Name : {3}\n" +
						"Target Name :{4}\n" +
						"ISID : {5:X} {6:X} {7:X} {8:X} {9:X} {10:X}\n" +
						"TSID : {11:X} {12:X}\n" +
						"Number Connections : {13}\n",
						si.SessionId.AdapterUnique,
						si.SessionId.AdapterSpecific,
						si.InitiatorName,
						si.TargetNodeName,
						si.TargetName,
						si.ISID[0],
						si.ISID[1],
						si.ISID[2],
						si.ISID[3],
						si.ISID[4],
						si.ISID[5],
						si.TSID[0],
						si.TSID[1],
						si.ConnectionCount);

					if (si.ConnectionCount > 0)
					{
						Console.Write("\n Connections:\n");
					}

					foreach (ISCSI_CONNECTION_INFO ConnectionInfo in si.Connections.ToArray<ISCSI_CONNECTION_INFO>((int)si.ConnectionCount))
					{
						Console.Write(" Connection Id : {0:X}-{1:X}\n" +
							" Initiator Portal : {2}/{3}\n" +
							" Target Portal : {4}/{5}\n" +
							" CID : {6:X} {7:X}\n",
							ConnectionInfo.ConnectionId.AdapterUnique,
							ConnectionInfo.ConnectionId.AdapterSpecific,
							ConnectionInfo.InitiatorAddress,
							ConnectionInfo.InitiatorSocket,
							ConnectionInfo.TargetAddress,
							ConnectionInfo.TargetSocket,
							ConnectionInfo.CID[0],
							ConnectionInfo.CID[1]);
					}

					Console.Write("\n");

					if (ShowDeviceInfo)
					{
						var DeviceCount = 0U;
						ISCSI_DEVICE_ON_SESSION[] DeviceList = null;
						Status = GetDevicesForIScsiSession(si.SessionId, ref DeviceCount, DeviceList);
						if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
						{
							DeviceList = new ISCSI_DEVICE_ON_SESSION[(int)DeviceCount];
							Status = GetDevicesForIScsiSession(si.SessionId, ref DeviceCount, DeviceList);
							if ((Status == Win32Error.ERROR_SUCCESS) && (DeviceCount > 0))
							{
								Status = DiscpEnumerateDeviceInterfaces(GUID_DEVINTERFACE_VOLUME,
									DiscpVolumeDeviceInterfaceCallback, default, out List<DEVICEINTERFACEENTRY> VolumeList);
								if (Status != Win32Error.ERROR_SUCCESS)
								{
									VolumeList = null;
								}

								Console.Write(" Devices:\n");
								for (var k = 0; k < DeviceCount; k++)
								{
									Console.Write(" Device Type : {0}\n", DeviceTypeFromGuid(DeviceList[k].DeviceInterfaceType));

									Console.Write(" Device Number : {0}\n", DeviceList[k].StorageDeviceNumber.DeviceNumber);

									Console.Write(" Storage Device Type : {0}\n", DeviceList[k].StorageDeviceNumber.DeviceType);

									Console.Write(" Partition Number : {0}\n", DeviceList[k].StorageDeviceNumber.PartitionNumber);

									if (DeviceList[k].DeviceInterfaceType == GUID_DEVINTERFACE_DISK)
									{
										var HeaderPrinted = false;
										var VolumeIndex = 0;
										for (; ; )
										{
											StatusDontCare = GetMountPointsFromDeviceNumber(DeviceList[k].StorageDeviceNumber.DeviceNumber,
												VolumeList, ref VolumeIndex);
											if (StatusDontCare == Win32Error.ERROR_SUCCESS)
											{
												if (!HeaderPrinted)
												{
													Console.Write(" Volume Path Names : \n");
													HeaderPrinted = true;
												}
												foreach (var p in VolumeList[VolumeIndex].MoreInfo.VolumePathNames)
												{
													Console.WriteLine(" " + p);
												}
												VolumeIndex++;
											}
											else
											{
												break;
											}
										}
									}

									Console.Write("\n");
								}
							}
						}
						Status = Win32Error.ERROR_SUCCESS;
					}
				}
			}

			return (Status);
		}

		// Console.Write("iscsicli GetPSKey <Initiator Name> <initiator Port>
		// <Id Type> <Id>\n");
		private static Win32Error GetPSKey(string[] ArgV)
		{
			byte[] Id;
			uint InitiatorPort;

			if (ArgV.Length != 5)
			{
				Usage(42);
				return (Win32Error.ERROR_SUCCESS);
			}

			var Initiator = ArgV[1];
			if (Initiator.StartsWith('*'))
			{
				Initiator = default;
			}
			if (ArgV[2].StartsWith('*'))
			{
				InitiatorPort = ISCSI_ALL_INITIATOR_PORTS;
			}
			else
			{
				InitiatorPort = uint.Parse(ArgV[2]);
			}

			var IdType = (IKE_IDENTIFICATION_PAYLOAD_TYPE)int.Parse(ArgV[3]);
			var IdText = ArgV[4];
			Win32Error Status = Win32Error.ERROR_SUCCESS;

			if (Status == Win32Error.ERROR_SUCCESS)
			{
				switch (IdType)
				{
					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_IPV4_ADDR:
						{
							using var sockaddr = new SOCKADDR(0U);
							int sockaddrlen = sockaddr.Size;
							Status = WSAStringToAddress(IdText, ADDRESS_FAMILY.AF_INET, default, sockaddr, ref sockaddrlen);
							if (Status != Win32Error.ERROR_SUCCESS)
							{
								return (Status);
							}
							Id = sockaddr.GetAddressBytes();
							break;
						}

					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_IPV6_ADDR:
						{
							using var sockaddr = new SOCKADDR(default(SOCKADDR_IN6));
							int sockaddrlen = sockaddr.Size;
							Status = WSAStringToAddress(IdText, ADDRESS_FAMILY.AF_INET6, default, sockaddr, ref sockaddrlen);
							if (Status != Win32Error.ERROR_SUCCESS)
							{
								return (Status);
							}
							Id = sockaddr.GetAddressBytes();
							break;
						}

					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_FQDN:
					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_USER_FQDN:
						{
							Id = StringHelper.GetBytes(IdText, true, CharSet.Ansi);
							break;
						}

					default:
						{
							Console.Write("Error, only id types ID_IPV4_ADDR (1), ID_FQDN (2), ID_USER_FQDN supported (3)\n");
							return (Win32Error.ERROR_SUCCESS);
						}
				}

				using var pId = new PinnedObject(Id);
				var AuthInfo = new IKE_AUTHENTICATION_INFORMATION
				{
					AuthMethod = IKE_AUTHENTICATION_METHOD.IKE_AUTHENTICATION_PRESHARED_KEY_METHOD,
					PsKey = new IKE_AUTHENTICATION_PRESHARED_KEY
					{
						IdType = IdType,
						IdLengthInBytes = (uint)Id.Length,
						Id = pId
					},
				};

				Status = GetIScsiIKEInfo(Initiator, InitiatorPort, default, ref AuthInfo);

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					PrintSecurityFlags(" ", AuthInfo.PsKey.SecurityFlags);
				}
			}
			return (Status);
		}

		// iscsicli PSKey <Initiator Name> <initiator Port> <Security Flags>
		// <Id Type> <Id> <Key> <persist>
		private static Win32Error PSKey(string[] ArgV)
		{
			Win32Error Status;
			string Initiator;
			ISCSI_SECURITY_FLAGS SecurityFlags;
			byte[] Id;
			IKE_IDENTIFICATION_PAYLOAD_TYPE IdType;
			string Key;
			uint KeyLength;
			string IdText;
			uint InitiatorPort;
			bool Persist;

			if (ArgV.Length != 8)
			{
				Usage(30);
				return (Win32Error.ERROR_SUCCESS);
			}

			Initiator = ArgV[1];
			if (Initiator.StartsWith('*'))
			{
				Initiator = default;
			}
			if (ArgV[2].StartsWith('*'))
			{
				InitiatorPort = ISCSI_ALL_INITIATOR_PORTS;
			}
			else
			{
				InitiatorPort = uint.Parse(ArgV[2]);
			}

			SecurityFlags = (ISCSI_SECURITY_FLAGS)int.Parse(ArgV[3]);
			IdType = (IKE_IDENTIFICATION_PAYLOAD_TYPE)int.Parse(ArgV[4]);
			IdText = ArgV[5];
			Status = Win32Error.ERROR_SUCCESS;

			if (Status == Win32Error.ERROR_SUCCESS)
			{
				switch (IdType)
				{
					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_IPV4_ADDR:
						{
							using var sockaddr = new SOCKADDR(0U);
							int sockaddrlen = sockaddr.Size;
							Status = WSAStringToAddress(IdText, ADDRESS_FAMILY.AF_INET, default, sockaddr, ref sockaddrlen);
							if (Status != Win32Error.ERROR_SUCCESS)
							{
								return (Status);
							}
							Id = sockaddr.GetAddressBytes();
							break;
						}

					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_IPV6_ADDR:
						{
							using var sockaddr = new SOCKADDR(default(SOCKADDR_IN6));
							int sockaddrlen = sockaddr.Size;
							Status = WSAStringToAddress(IdText, ADDRESS_FAMILY.AF_INET6, default, sockaddr, ref sockaddrlen);
							if (Status != Win32Error.ERROR_SUCCESS)
							{
								return (Status);
							}
							Id = sockaddr.GetAddressBytes();
							break;
						}

					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_FQDN:
					case IKE_IDENTIFICATION_PAYLOAD_TYPE.ID_USER_FQDN:
						{
							Id = StringHelper.GetBytes(IdText, true, CharSet.Ansi);
							break;
						}

					default:
						{
							Console.Write("Error, only id types ID_IPV4_ADDR (1), ID_FQDN (2), ID_USER_FQDN supported (3)\n");
							return (Win32Error.ERROR_SUCCESS);
						}
				}

				if (ArgV[6].StartsWith('*'))
				{
					Key = default;
					KeyLength = 0;
				}
				else
				{
					Key = ArgV[6];
					KeyLength = (uint)Key.Length;
				}

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Persist = IsTrue(ArgV[7], true);

					using var pId = new PinnedObject(Id);
					using var pKey = new SafeCoTaskMemString(Key, CharSet.Auto);
					var IKEAuthInfo = new IKE_AUTHENTICATION_INFORMATION
					{
						AuthMethod = IKE_AUTHENTICATION_METHOD.IKE_AUTHENTICATION_PRESHARED_KEY_METHOD,
						PsKey = new IKE_AUTHENTICATION_PRESHARED_KEY
						{
							SecurityFlags = SecurityFlags,
							IdType = IdType,
							IdLengthInBytes = (uint)Id.Length,
							Id = pId,
							KeyLengthInBytes = KeyLength,
							Key = pKey
						},
					};

					Status = SetIScsiIKEInfo(Initiator, InitiatorPort, IKEAuthInfo, Persist);
				}
			}

			return (Status);
		}

		// iscsicli ReportLUNs <SessionId>
		private static Win32Error ReportLUNs(string[] ArgV)
		{
			Win32Error Status;

			if (ArgV.Length != 2)
			{
				Usage(18);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				if (StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
				{
					uint SenseSize = 18, ResponseSize = 0;
					using var Sense = new SafeCoTaskMemHandle(SenseSize);
					Status = SendScsiReportLuns(SessionId, out var ScsiStatus, ref ResponseSize, default, ref SenseSize, Sense);
					if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
					{
						using var Response = new SafeCoTaskMemHandle(ResponseSize);
						Status = SendScsiReportLuns(SessionId, out ScsiStatus, ref ResponseSize, Response, ref SenseSize, Sense);
						if (Status == Win32Error.ERROR_SUCCESS)
						{
							Console.Write(" ScsiStatus : 0x{0:X}\n Response Buffer Size : 0x{1:X}\n", ScsiStatus, ResponseSize);
							Console.WriteLine(Response.GetBytes(0, (int)ResponseSize).ToHexDumpString());
						}
					}

					if (Status == ISDSC_SCSI_REQUEST_FAILED)
					{
						Console.Write(" ScsiStatus : 0x{0:X}\n Sense Buffer Size : 0x{1:X}\n", ScsiStatus, SenseSize);
						Console.WriteLine(Sense.GetBytes(0, (int)SenseSize).ToHexDumpString());
					}
				}
				else
				{
					Console.Write("Invalid sessionid: {0}\n", ArgV[1]);
					Status = Win32Error.ERROR_INVALID_PARAMETER;
				}

			}

			return (Status);
		}

		// iscsicli ReadCapacity <SessionId> <LUN>
		private static Win32Error ReadCapacity(string[] ArgV)
		{
			Win32Error Status;

			if (ArgV.Length != 3)
			{
				Usage(17);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				if (StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
				{

					if (ulong.TryParse(ArgV[2], System.Globalization.NumberStyles.HexNumber, null, out var LUN))
					{
						uint SenseSize = 18, ResponseSize = 0;
						using var Sense = new SafeCoTaskMemHandle(SenseSize);
						Status = SendScsiReadCapacity(SessionId, LUN, out var ScsiStatus, ref ResponseSize, default, ref SenseSize, Sense);
						if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
						{
							using var Response = new SafeCoTaskMemHandle(ResponseSize);
							Status = SendScsiReadCapacity(SessionId, LUN, out ScsiStatus, ref ResponseSize, Response, ref SenseSize, Sense);
							if (Status == Win32Error.ERROR_SUCCESS)
							{
								Console.Write(" ScsiStatus : 0x{0:X}\n Response Buffer Size : 0x{1:X}\n", ScsiStatus, ResponseSize);
								Console.WriteLine(Response.GetBytes(0, (int)ResponseSize).ToHexDumpString());
							}
						}

						if (Status == ISDSC_SCSI_REQUEST_FAILED)
						{
							Console.Write(" ScsiStatus : 0x{0:X}\n Sense Buffer Size : 0x{1:X}\n", ScsiStatus, SenseSize);
							Console.WriteLine(Sense.GetBytes(0, (int)SenseSize).ToHexDumpString());
						}
					}
					else
					{
						Console.Write("Invalid LUN: {0}\n", ArgV[2]);
						Status = Win32Error.ERROR_INVALID_PARAMETER;
					}
				}
				else
				{
					Console.Write("Invalid sessionid: {0}\n", ArgV[1]);
					Status = Win32Error.ERROR_INVALID_PARAMETER;
				}

			}

			return (Status);
		}

		// iscsicli ScsiInquiry <SessionId> <LUN> <EvpdCmddt> <PageCode>
		private static Win32Error DoScsiInquiry(string[] ArgV)
		{
			Win32Error Status;
			byte EvpdCmdt, PageCode;

			if (ArgV.Length != 5)
			{
				Usage(16);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				if (StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
				{
					if (ulong.TryParse(ArgV[2], System.Globalization.NumberStyles.HexNumber, null, out var LUN))
					{
						EvpdCmdt = byte.Parse(ArgV[3]);
						PageCode = byte.Parse(ArgV[4]);

						uint SenseSize = 18, ResponseSize = 0;
						using var Sense = new SafeCoTaskMemHandle(SenseSize);
						Status = SendScsiInquiry(SessionId, LUN, EvpdCmdt, PageCode, out var ScsiStatus, ref ResponseSize, default, ref SenseSize, Sense);
						if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
						{
							using var Response = new SafeCoTaskMemHandle(ResponseSize);
							Status = SendScsiInquiry(SessionId, LUN, EvpdCmdt, PageCode, out ScsiStatus, ref ResponseSize, Response, ref SenseSize, Sense);
							if (Status == Win32Error.ERROR_SUCCESS)
							{
								Console.Write(" ScsiStatus : 0x{0:X}\n Response Buffer Size : 0x{1:X}\n", ScsiStatus, ResponseSize);
								Console.WriteLine(Response.GetBytes(0, (int)ResponseSize).ToHexDumpString());
							}
						}

						if (Status == ISDSC_SCSI_REQUEST_FAILED)
						{
							Console.Write(" ScsiStatus : 0x{0:X}\n Sense Buffer Size : 0x{1:X}\n", ScsiStatus, SenseSize);
							Console.WriteLine(Sense.GetBytes(0, (int)SenseSize).ToHexDumpString());
						}
					}
					else
					{
						Console.Write("Invalid LUN: {0}\n", ArgV[2]);
						Status = Win32Error.ERROR_INVALID_PARAMETER;
					}
				}
				else
				{
					Console.Write("Invalid sessionid: {0}\n", ArgV[1]);
					Status = Win32Error.ERROR_INVALID_PARAMETER;
				}
			}

			return (Status);
		}

		// iscsicli LogoutTarget <SessionId>
		private static Win32Error DoLogoutTarget(string[] ArgV)
		{
			Win32Error Status;

			if (ArgV.Length != 2)
			{
				Usage(10);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				if (StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
				{
					Console.Write("Logout Target 0x{0:X}-0x{1:X}\n", SessionId.AdapterUnique, SessionId.AdapterSpecific);
					Status = LogoutIScsiTarget(SessionId);
				}
				else
				{
					Console.Write("Invalid sessionid: {0}\n", ArgV[1]);
					Status = Win32Error.ERROR_INVALID_PARAMETER;
				}
			}

			return (Status);
		}

		private static Win32Error DoLoginToTarget(string[] ArgV, bool IsPersistent)
		{
			Win32Error Status;
			bool ReportToPNP;
			string TargetName;
			string TargetPortalAddress;
			ushort TargetPortalSocket;
			string InitiatorInstance;
			ISCSI_TARGET_PORTAL TargetPortal = default;
			ISCSI_SECURITY_FLAGS SecurityFlags;
			uint MappingCount;
			uint ArgCIndex;
			uint PortNumber;
			int ArgCExpected;
			string Key;
			uint KeyLength;
			uint x;
			bool b;
			SafeCoTaskMemStruct<ISCSI_TARGET_MAPPING> Mapping = SafeCoTaskMemStruct<ISCSI_TARGET_MAPPING>.Null;

			if (ArgV.Length < 19)
			{
				Usage(IsPersistent ? 11 : 9);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				TargetName = ArgV[1];
				ReportToPNP = true;
				TargetPortalAddress = default;
				TargetPortalSocket = 0;
				InitiatorInstance = default;

				ReportToPNP = IsTrue(ArgV[2], true);

				if (!ArgV[3].StartsWith('*'))
				{
					TargetPortalAddress = ArgV[3];
					if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
					{
						return (Win32Error.ERROR_INVALID_PARAMETER);
					}
				}

				if ((TargetPortalAddress is not null) && (!ArgV[4].StartsWith('*')))
				{
					TargetPortalSocket = ushort.Parse(ArgV[4]);
					TargetPortal.Address = TargetPortalAddress;
					TargetPortal.Socket = TargetPortalSocket;
					TargetPortal.SymbolicName = string.Empty;
				}
				else if (!((TargetPortalAddress is null) && ArgV[4].StartsWith('*')))
				{
					Console.Write("Portal address and socket must be specified\n");
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}

				if (!ArgV[5].StartsWith('*'))
				{
					InitiatorInstance = ArgV[5];
				}

				if (!ArgV[6].StartsWith('*'))
				{
					PortNumber = uint.Parse(ArgV[6]);
				}
				else
				{
					PortNumber = ISCSI_ANY_INITIATOR_PORT;
				}

				SecurityFlags = (ISCSI_SECURITY_FLAGS)int.Parse(ArgV[7]);

				ParseLoginOptions(out ISCSI_LOGIN_OPTIONS LoginOptions, ArgV, 9);

				if (ArgV[17].StartsWith('*'))
				{
					Key = default;
					KeyLength = 0;
				}
				else
				{
					Key = ArgV[17];
					KeyLength = (uint)Key.Length + 1;
				}

				MappingCount = uint.Parse(ArgV[18]);
				ArgCExpected = 20 + ((int)MappingCount * 4);
				if (ArgV.Length != ArgCExpected)
				{
					Usage(IsPersistent ? 11 : 9);
					return (Win32Error.ERROR_SUCCESS);
				}

				if (MappingCount != 0)
				{
					ArgCIndex = 19;
					var sMapping = new ISCSI_TARGET_MAPPING
					{
						OSBusNumber = uint.Parse(ArgV[ArgCIndex + 1]),
						OSTargetNumber = uint.Parse(ArgV[ArgCIndex + 2]),
						LUNCount = MappingCount
					};
					var LUNList = new SCSI_LUN_LIST[(int)MappingCount];
					for (var i = 0; i < MappingCount; i++)
					{
						b = stoiDForLogicalUnit(ArgV[ArgCIndex], out LUNList[i].TargetLUN);
						if (b == false)
						{
							Console.Write("Target LUN must be in 0x0123456789abcdef format\n");
							return (Win32Error.ERROR_INVALID_PARAMETER);
						}

						ArgCIndex++; // bus

						x = uint.Parse(ArgV[ArgCIndex]);
						if (x != sMapping.OSBusNumber)
						{
							Console.Write("OSBus number must be the same for all LUNs\n");
							return (Win32Error.ERROR_INVALID_PARAMETER);
						}

						ArgCIndex++; // target
						x = uint.Parse(ArgV[ArgCIndex]);
						if (x != sMapping.OSTargetNumber)
						{
							Console.Write("OSTarget number must be the same for all LUNs\n");
							return (Win32Error.ERROR_INVALID_PARAMETER);
						}

						ArgCIndex++;
						LUNList[i].OSLUN = uint.Parse(ArgV[ArgCIndex]);
						ArgCIndex++;
					}

					SizeT SizeNeeded = Marshal.SizeOf<ISCSI_TARGET_MAPPING>() + MappingCount * Marshal.SizeOf<SCSI_LUN_LIST>();
					Mapping = new SafeCoTaskMemStruct<ISCSI_TARGET_MAPPING>(sMapping, SizeNeeded);
					IntPtr ptr = Mapping.DangerousGetHandle().Offset(Marshal.SizeOf<ISCSI_TARGET_MAPPING>());
					ptr.Write(LUNList);
					Mapping.DangerousGetHandle().Write(ptr, (int)Marshal.OffsetOf<ISCSI_TARGET_MAPPING>(nameof(ISCSI_TARGET_MAPPING.LUNList)));
				}
				else
				{
					Mapping = default;
				}

				Console.Write("LoginTarget to {0} on {1} to {2}/{3}\n",
					TargetName,
					InitiatorInstance ?? "<no init instance>",
					TargetPortalAddress is not null ? TargetPortal.Address : "<no portal>",
					TargetPortalAddress is not null ? TargetPortal.Socket : 0);

				using SafeCoTaskMemStruct<ISCSI_TARGET_PORTAL> pTargetPortal = TargetPortalAddress is not null ? new SafeCoTaskMemStruct<ISCSI_TARGET_PORTAL>(TargetPortal) : SafeCoTaskMemStruct<ISCSI_TARGET_PORTAL>.Null;
				Status = LoginIScsiTarget(TargetName, !ReportToPNP, InitiatorInstance, PortNumber,
					pTargetPortal, SecurityFlags, Mapping, LoginOptions, KeyLength, Key, IsPersistent, out ISCSI_UNIQUE_SESSION_ID SessionId, out ISCSI_UNIQUE_SESSION_ID ConnectionId);

				if (!IsPersistent)
				{
					if (Status == Win32Error.ERROR_SUCCESS)
					{
						Console.Write("Session Id is 0x{0:X}-0x{1:X}\n", SessionId.AdapterUnique, SessionId.AdapterSpecific);
						Console.Write("Connection Id is 0x{0:X}-0x{1:X}\n", ConnectionId.AdapterUnique, ConnectionId.AdapterSpecific);
					}
				}

				Mapping?.Dispose();
			}

			return (Status);
		}

		// iscsicli LoginTarget <TargetName> <ReportToPNP>
		// <TargetPortalAddress> <TargetPortalSocket>
		// <InitiatorInstance> <Port number> <Security Flags>
		// <Header Digest> <Data Digest> 
		// <Max Connections> <DefaultTime2Wait>
		// <DefaultTime2Retain> <Username> <Password>
		// <AuthType> <Key>
		// <Mapping Count> <Target Lun> <OS Bus> <Os Target> 
		// <OS Lun> ...
		// 
		private static Win32Error TryLoginToTarget(string[] ArgV) => DoLoginToTarget(ArgV, false);

		// iscsicli PersistentLoginTarget <TargetName> <ReportToPNP>
		// <TargetPortalAddress> <TargetPortalSocket>
		// <InitiatorInstance> <Port number> <Security Flags>
		// <Header Digest> <Data Digest> 
		// <Max Connections> <DefaultTime2Wait>
		// <DefaultTime2Retain> <Username> <Password>
		// <AuthType> <Key>
		// <Mapping Count> <Target Lun> <OS Bus> <Os Target> 
		// <OS Lun> ...
		// 
		private static Win32Error PersistentLoginTarget(string[] ArgV) => DoLoginToTarget(ArgV, true);

		// Console.Write("iscsicli RemovePersistentTarget <Initiator Name> <TargetName>\n"
		// Console.Write(" <Port Number> \n");
		// Console.Write(" <Target Portal Address> \n");
		// Console.Write(" <Target Portal Socket> \n");
		private static Win32Error RemovePersistentTarget(string[] ArgV)
		{
			Win32Error Status;
			ISCSI_TARGET_PORTAL TargetPortal = default;

			if (ArgV.Length != 6)
			{
				Usage(13);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				var I = ArgV[1].StartsWith('*') ? default : ArgV[1];

				var PortNumber = ArgV[3].StartsWith('*') ? ISCSI_ALL_INITIATOR_PORTS : uint.Parse(ArgV[3]);

				if (!ArgV[4].StartsWith('*'))
				{
					if (ArgV[4].Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
					{
						return (Win32Error.ERROR_INVALID_PARAMETER);
					}

					TargetPortal.Address = ArgV[4];
					TargetPortal.Socket = (ushort)int.Parse(ArgV[5]);
				}

				Status = RemoveIScsiPersistentTarget(I, PortNumber, ArgV[2], TargetPortal);
			}

			return (Status);
		}


		// Console.Write("iscsicli ListPersistentTargets\n");
		private static Win32Error ListPersistentTarget(string[] ArgV)
		{
			uint SizeNeeded = 0;
			Win32Error Status = ReportIScsiPersistentLogins(out var Count, default, ref SizeNeeded);
			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				Console.Write("Total of {0} peristent targets\n", Count);
				using var LoginInfoArray = new SafeCoTaskMemHandle(SizeNeeded);
				Status = ReportIScsiPersistentLogins(out Count, LoginInfoArray, ref SizeNeeded);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					foreach (PERSISTENT_ISCSI_LOGIN_INFO LoginInfo in LoginInfoArray.ToArray<PERSISTENT_ISCSI_LOGIN_INFO>((int)Count))
					{
						Console.Write(" Target Name : {0}\n" +
							" Address and Socket : {1} {2}\n" +
							" Session Type : {3}\n" +
							" Initiator Name : {4}\n",
							LoginInfo.TargetName,
							LoginInfo.TargetPortal.Address ?? "*",
							LoginInfo.TargetPortal.Socket,
							LoginInfo.IsInformationalSession ? "Informational" : "Data",
							LoginInfo.InitiatorInstance);

						if (LoginInfo.InitiatorPortNumber == ISCSI_ANY_INITIATOR_PORT)
						{
							Console.Write(" Port Number : <Any Port>\n");
						}
						else
						{
							Console.Write(" Port Number : {0}\n", LoginInfo.InitiatorPortNumber);
						}

						PrintSecurityFlags(" ", LoginInfo.SecurityFlags);

						PrintLoginOptions(" ", LoginInfo.LoginOptions);
						if (LoginInfo.Mappings != default)
						{
							PrintTargetMapping(LoginInfo.Mappings.ToStructure<ISCSI_TARGET_MAPPING>());
						}

						Console.Write("\n");
					}
				}
			}

			return (Status);
		}

		private static Win32Error GetTargetInfo(string TargetName, string DiscoveryMechanism, TARGET_INFORMATION_CLASS InfoClass, out ISafeMemoryHandle Buffer)
		{
			Buffer = SafeCoTaskMemHandle.Null;
			uint SizeNeeded = 0;
			Win32Error Status = GetIScsiTargetInformation(TargetName, DiscoveryMechanism, InfoClass, ref SizeNeeded, default);
			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				var b = new SafeCoTaskMemHandle(SizeNeeded);
				Status = GetIScsiTargetInformation(TargetName, DiscoveryMechanism, InfoClass, ref SizeNeeded, b);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Buffer = b;
				}
				else
				{
					b.Dispose();
				}
			}
			return (Status);
		}

		private static void PrintPortalGroups(ISCSI_TARGET_PORTAL_GROUP[] PortalGroups, uint Size)
		{
			var i = 0;
			foreach (ISCSI_TARGET_PORTAL_GROUP p in PortalGroups)
			{
				Console.Write(" Group {0} has {1} portals\n", i++, p.Count);

				for (var j = 0; j < p.Count; j++)
				{
					Console.Write(" Address and Socket : {0} {1}\n Symbolic Name : {2}\n", p.Portals[j].Address, p.Portals[j].Socket, p.Portals[j].SymbolicName);
				}
			}
		}

		// iscsicli TargetInfo <TargetName> [DiscoveryMechanism]
		private static Win32Error TargetInfo(string[] ArgV)
		{
			string DiscoveryMechanism;
			uint Count;
			Win32Error Status;

			if ((ArgV.Length != 2) && (ArgV.Length != 3))
			{
				Usage(8);
				return (Win32Error.ERROR_SUCCESS);
			}
			else
			{
				if (ArgV.Length == 2)
				{
					DiscoveryMechanism = default;
				}
				else if (ArgV[2][0] == '*')
				{
					DiscoveryMechanism = default;
				}
				else
				{
					DiscoveryMechanism = ArgV[2];
				}
			}

			Console.Write("Get Target information for {0} discovered by {1}\n", ArgV[1], DiscoveryMechanism ?? "<all mechanisms>");

			Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.DiscoveryMechanisms, out ISafeMemoryHandle Buffer);
			if (Status == Win32Error.ERROR_SUCCESS)
			{
				if (Buffer.Size > 0)
				{
					PrintStringList(" Discovery Mechanisms :", " ", Buffer.ToStringEnum());
				}
				else
				{
					Console.Write(" Discovery Mechanisms: <This List is Empty>\n");
				}
				Buffer.Dispose();
			}

			if (DiscoveryMechanism != default)
			{
				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.ProtocolType, out Buffer);

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					var ProtocolList = new[] { "iSCSI TCP Protocol", "Unknown" };
					TARGETPROTOCOLTYPE ProtocolType = Buffer.ToStructure<TARGETPROTOCOLTYPE>();

					if (ProtocolType != TARGETPROTOCOLTYPE.ISCSI_TCP_PROTOCOL_TYPE)
					{
						ProtocolType = TARGETPROTOCOLTYPE.ISCSI_TCP_PROTOCOL_TYPE;
					}

					Console.Write(" Protocol Type : {0}\n", ProtocolList[(int)ProtocolType]);

					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Protocol Type Failed : {0}\n", GetiSCSIMessageText(Status));
				}

				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.TargetAlias, out Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Console.Write(" Target Alias : {0}\n", Buffer.ToString(-1, CharSet.Auto));
					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Target Alias Failed : {0}\n", GetiSCSIMessageText(Status));
				}

				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.PortalGroups, out Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					if (Buffer.Size > 0)
					{
						Count = Buffer.ToStructure<uint>();
						Console.Write(" PortalGroups : {0} portal groups\n", Count);
						PrintPortalGroups(Buffer.ToArray<ISCSI_TARGET_PORTAL_GROUP>((int)Count, sizeof(uint)), Buffer.Size);
					}
					else
					{
						Console.Write(" PortalGroups : <This List Is Empty>\n");
					}
					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Portal Groups Failed : {0}\n", GetiSCSIMessageText(Status));
				}

				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.InitiatorName, out Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					if (Buffer.Size > 0)
					{
						Console.Write(" Initiator Name : {0}\n", Buffer.ToString(-1, CharSet.Auto));
					}
					else
					{
						Console.Write(" Initiator Name : <Empty>\n");
					}
					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Initiator List Failed: {0}\n", GetiSCSIMessageText(Status));
				}

				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.TargetFlags, out Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					if (Buffer.Size == Marshal.SizeOf<ISCSI_TARGET_FLAGS>())
					{
						ISCSI_TARGET_FLAGS TargetFlags = Buffer.ToStructure<ISCSI_TARGET_FLAGS>();
						Console.Write(" Target Flags : {0}\n", TargetFlags);

						if (TargetFlags.IsFlagSet(ISCSI_TARGET_FLAGS.ISCSI_TARGET_FLAG_HIDE_STATIC_TARGET))
						{
							Console.Write(" Target is hidden until dynamically discovered\n");
						}
					}

					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Target Flags Failed : {0}\n", GetiSCSIMessageText(Status));
				}

				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.LoginOptions, out Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					if (Buffer.Size >= Marshal.SizeOf<ISCSI_LOGIN_OPTIONS>())
					{
						ISCSI_LOGIN_OPTIONS LoginOptions = Buffer.ToStructure<ISCSI_LOGIN_OPTIONS>();
						PrintLoginOptions(" ", LoginOptions);
					}

					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Login Options Failed : {0}\n", GetiSCSIMessageText(Status));
				}


				Status = GetTargetInfo(ArgV[1], DiscoveryMechanism, TARGET_INFORMATION_CLASS.PersistentTargetMappings, out Buffer);
				if (Status == Win32Error.ERROR_SUCCESS)
				{
					if (Buffer.Size >= Marshal.SizeOf<ISCSI_TARGET_MAPPING>())
					{
						ISCSI_TARGET_MAPPING Mapping = Buffer.ToStructure<ISCSI_TARGET_MAPPING>();
						PrintTargetMapping(Mapping);
					}

					Buffer.Dispose();
				}
				else
				{
					Console.Write(" Target Mappings Failed : {0}\n", GetiSCSIMessageText(Status));
				}

				Status = Win32Error.ERROR_SUCCESS;
			}
			Console.Write("\n");

			return (Status);
		}

		// iscsicli AddConnection <SessionId> <initiator instance>
		// <Port Number> <Target Portal Address>
		// <Target Portal Socket> <Security Flags>
		// <Header Digest> <Data Digest>
		// <Max Connections> <DefaultTime2Wait>
		// <DefaultTime2Retain> <Username> <Password>
		// <AuthType> <Key>
		private static Win32Error DoAddConnection(string[] ArgV)
		{
			Win32Error Status;
			ISCSI_TARGET_PORTAL TargetPortal = default;
			string TargetPortalAddress;
			ushort TargetPortalSocket;
			string Key;
			uint KeyLength;

			if (ArgV.Length != 17)
			{
				Usage(14);
				return (Win32Error.ERROR_SUCCESS);
			}

			if (!StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
			{
				Usage(14);
				return (Win32Error.ERROR_SUCCESS);
			}

			var InitiatorName = !ArgV[2].StartsWith('*') ? ArgV[2] : default;

			var PortNumber = ArgV[3].StartsWith('*') ? ISCSI_ANY_INITIATOR_PORT : (uint)int.Parse(ArgV[3]);

			if (!ArgV[4].StartsWith('*'))
			{
				TargetPortalAddress = ArgV[4];
				if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
				{
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}
			}
			else
			{
				TargetPortalAddress = default;
			}

			if ((TargetPortalAddress != default) && ((!ArgV[5].StartsWith('*'))))
			{
				TargetPortalSocket = (ushort)int.Parse(ArgV[5]);
				TargetPortal.Address = TargetPortalAddress;
				TargetPortal.Socket = TargetPortalSocket;
			}
			else
			{
				TargetPortalAddress = default;
			}

			var SecurityFlags = (ISCSI_SECURITY_FLAGS)int.Parse(ArgV[6]);

			ParseLoginOptions(out ISCSI_LOGIN_OPTIONS LoginOptions, ArgV, 8);
			if (ArgV[16].StartsWith('*'))
			{
				Key = default;
				KeyLength = 0;
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				Key = ArgV[16];
				KeyLength = (uint)Key.Length + 1;
			}

			using var pTargetPortal = SafeCoTaskMemHandle.CreateFromStructure(TargetPortal);
			using var pLoginOptions = SafeCoTaskMemHandle.CreateFromStructure(LoginOptions);
			Status = AddIScsiConnection(SessionId, InitiatorName, PortNumber, TargetPortalAddress is not null ? pTargetPortal : IntPtr.Zero,
				SecurityFlags, pLoginOptions, KeyLength, Key, out _);

			return (Status);
		}

		// Console.Write("iscsicli RemoveConnection <SessionId> <ConnectionId> \n");
		private static Win32Error DoRemoveConnection(string[] ArgV)
		{
			if (ArgV.Length != 3)
			{
				Usage(15);
				return (Win32Error.ERROR_SUCCESS);
			}

			if (!StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
			{
				Usage(15);
				return (Win32Error.ERROR_SUCCESS);
			}

			if (!StringToSessionId(ArgV[2], out ISCSI_UNIQUE_SESSION_ID ConnectionId))
			{
				Usage(15);
				return (Win32Error.ERROR_SUCCESS);
			}

			return RemoveIScsiConnection(SessionId, ConnectionId);
		}

		// iscsicli ListInitiators 
		private static Win32Error DoReportInitiatorList(string[] ArgV)
		{
			Win32Error Status;
			IEnumerable<string> b = Enumerable.Empty<string>();

			if (ArgV.Length != 1)
			{
				Usage(20);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				var BufferSize = 0U;
				Status = ReportIScsiInitiatorList(ref BufferSize, default);

				if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
				{
					using var Buffer = new SafeCoTaskMemHandle(BufferSize * StringHelper.GetCharSize());
					Status = ReportIScsiInitiatorList(ref BufferSize, Buffer);
					b = Buffer.ToStringEnum().ToArray();
				}

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Console.Write("Initiators List:\n");
					foreach (var s in b)
						Console.Write(" {0}\n", s);
				}
			}
			return (Status);
		}

		// iscsicli ReportTargetMappings
		private static Win32Error DoReportActiveIScsiTargetMappings(string[] ArgV)
		{
			uint BufferSize = 0;
			Win32Error Status = ReportActiveIScsiTargetMappings(ref BufferSize, out _, default);
			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				using var MappingX = new SafeCoTaskMemHandle(BufferSize);
				if (!MappingX.IsInvalid)
				{
					Status = ReportActiveIScsiTargetMappings(ref BufferSize, out var MappingCount, MappingX);
					if (Status == Win32Error.ERROR_SUCCESS)
					{
						Console.Write("Total of {0} mappings returned\n", MappingCount);
						foreach (ISCSI_TARGET_MAPPING Mapping in MappingX.ToArray<ISCSI_TARGET_MAPPING>((int)MappingCount))
						{
							PrintTargetMapping(Mapping);
						}
					}
				}
			}
			else if (Status == Win32Error.ERROR_SUCCESS)
			{
				Console.Write("No mappings\n");
			}

			return (Status);
		}

		// iscsicli ListTargets <ForceUpdate>
		private static Win32Error ListTargets(string[] ArgV)
		{
			Win32Error Status;

			if ((ArgV.Length != 2) && (ArgV.Length != 3))
			{
				Usage(6);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				var ForceUpdate = ArgV.Length == 2 ? IsTrue(ArgV[1], false) : false;

				var BufferSize = 0U;
				using var Buffer = new SafeCoTaskMemHandle(StringHelper.GetCharSize());
				Status = ReportIScsiTargets(ForceUpdate, ref BufferSize, default);
				if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
				{
					Buffer.Size = BufferSize * StringHelper.GetCharSize();
					Status = ReportIScsiTargets(ForceUpdate, ref BufferSize, Buffer);
				}

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Console.Write("Targets List:\n");
					foreach (var s in Buffer.ToStringEnum())
					{
						Console.Write(" {0}\n", s);
					}
				}
			}

			return (Status);
		}

		// iscsicli RefreshTargetPortal <TargetPortalAddress>
		// <TargetPortalSocket> [HBAName]
		// [PortNumber]
		private static Win32Error RefreshTargetPortal(string[] ArgV)
		{
			Win32Error Status;
			string TargetPortalAddress;
			string TargetPortalSocket;
			string HBAName;
			uint PortNumber;

			if ((ArgV.Length < 3) || (ArgV.Length > 5))
			{
				Usage(5);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				TargetPortalAddress = ArgV[1];
				if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
				{
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}
				TargetPortalSocket = ArgV[2];

				if (ArgV.Length > 3)
				{
					HBAName = ArgV[3];
					if (HBAName.StartsWith('*'))
					{
						HBAName = default;
					}

					if (ArgV.Length > 4 && !ArgV[4].StartsWith('*'))
					{
						PortNumber = uint.Parse(ArgV[4]);
					}
					else
					{
						PortNumber = ISCSI_ALL_INITIATOR_PORTS;
					}
				}
				else
				{
					HBAName = default;
					PortNumber = ISCSI_ALL_INITIATOR_PORTS;
				}

				var TargetPortal = new ISCSI_TARGET_PORTAL
				{
					Address = TargetPortalAddress,
					Socket = (ushort)int.Parse(TargetPortalSocket)
				};
				Status = RefreshIScsiSendTargetPortal(HBAName, PortNumber, TargetPortal);
			}

			return (Status);
		}


		// iscsicli ListTargetPortals
		private static Win32Error ListTargetPortals(string[] ArgV)
		{
			Win32Error Status;

			uint SizeNeeded = 0;
			Status = ReportIScsiSendTargetPortalsEx(out var Count, ref SizeNeeded, default);
			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				using var PortalInfoArray = new SafeCoTaskMemHandle(SizeNeeded);
				if (!PortalInfoArray.IsInvalid)
				{
					Status = ReportIScsiSendTargetPortalsEx(out Count, ref SizeNeeded, PortalInfoArray);
					if (Status == Win32Error.ERROR_SUCCESS)
					{
						Console.Write("Total of {0} portals are persisted:\n\n", Count);
						foreach (ISCSI_TARGET_PORTAL_INFO_EX PortalInfo in PortalInfoArray.ToArray<ISCSI_TARGET_PORTAL_INFO_EX>((int)Count))
						{
							Console.Write(" Address and Socket : {0} {1}\n" +
								" Symbolic Name : {2}\n" +
								" Initiator Name : {3}\n",
								PortalInfo.Address,
								PortalInfo.Socket,
								PortalInfo.SymbolicName,
								PortalInfo.InitiatorName);

							if (PortalInfo.InitiatorPortNumber == ISCSI_ANY_INITIATOR_PORT)
							{
								Console.Write(" Port Number : <Any Port>\n");
							}
							else
							{
								Console.Write(" Port Number : {0}\n", PortalInfo.InitiatorPortNumber);
							}

							PrintSecurityFlags(" ", PortalInfo.SecurityFlags);
							PrintLoginOptions(" ", PortalInfo.LoginOptions);
							Console.Write("\n");
						}
					}
				}
			}

			return (Status);
		}


		// iscsicli RemoveTargetPortal <TargetPortalAddress> <TargetPortalSocket>
		// [HBA Name] [PortNumber]
		//
		private static Win32Error RemoveTargetPortal(string[] ArgV)
		{
			Win32Error Status;
			string TargetPortalAddress;
			string TargetPortalSocket;
			string HBAName;
			uint PortNumber;

			if ((ArgV.Length < 3) || (ArgV.Length > 5))
			{
				Usage(4);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				TargetPortalAddress = ArgV[1];
				if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
				{
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}
				TargetPortalSocket = ArgV[2];

				if (ArgV.Length > 3)
				{
					HBAName = ArgV[3];
					if (HBAName.StartsWith('*'))
					{
						HBAName = default;
					}

					if (ArgV.Length > 4)
					{
						if (ArgV[4].StartsWith('*'))
						{
							PortNumber = ISCSI_ALL_INITIATOR_PORTS;
						}
						else
						{
							PortNumber = uint.Parse(ArgV[4]);
						}
					}
					else
					{
						PortNumber = ISCSI_ALL_INITIATOR_PORTS;
					}
				}
				else
				{
					HBAName = default;
					PortNumber = ISCSI_ALL_INITIATOR_PORTS;
				}

				var TargetPortal = new ISCSI_TARGET_PORTAL
				{
					Address = TargetPortalAddress,
					Socket = ushort.Parse(TargetPortalSocket)
				};
				Status = RemoveIScsiSendTargetPortal(HBAName, PortNumber, TargetPortal);
			}

			return (Status);
		}

		// iscsicli AddTargetPortal <TargetPortalAddress> <TargetPortalSocket>
		// [HBA Name] [PortNumber]
		// Console.Write(" <Security Flags>\n");
		// Console.Write(" <Login Flags> <Header Digest> <Data Digest> \n");
		// Console.Write(" <Max Connections> <DefaultTime2Wait>\n");
		// Console.Write(" <DefaultTime2Retain> <Username>
		// <Password> <AuthType>\n");
		private static Win32Error AddTargetPortal(string[] ArgV)
		{
			Win32Error Status;
			string TargetPortalAddress, TargetPortalSocket, HBAName;
			uint PortNumber;
			ISCSI_LOGIN_OPTIONS LoginOptions = default;
			ISCSI_SECURITY_FLAGS SecurityFlags = 0;

			if ((ArgV.Length != 3) && (ArgV.Length < 15))
			{
				Usage(3);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				TargetPortalAddress = ArgV[1];
				if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
				{
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}

				TargetPortalSocket = ArgV[2];

				if (ArgV.Length > 3)
				{
					HBAName = ArgV[3];
					if (HBAName.StartsWith('*'))
					{
						HBAName = default;
					}

					if (ArgV.Length > 4)
					{
						if (ArgV[4].StartsWith('*'))
						{
							PortNumber = ISCSI_ALL_INITIATOR_PORTS;
						}
						else
						{
							PortNumber = uint.Parse(ArgV[4]);
						}

						SecurityFlags = (ISCSI_SECURITY_FLAGS)int.Parse(ArgV[5]);

						if (ArgV.Length > 6)
						{
							ParseLoginOptions(out LoginOptions, ArgV, 7);
						}
					}
					else
					{
						PortNumber = ISCSI_ALL_INITIATOR_PORTS;
					}
				}
				else
				{
					HBAName = default;
					PortNumber = ISCSI_ALL_INITIATOR_PORTS;
				}

				var TargetPortal = new ISCSI_TARGET_PORTAL
				{
					Address = TargetPortalAddress,
					Socket = ushort.Parse(TargetPortalSocket)
				};
				Status = AddIScsiSendTargetPortal(HBAName, PortNumber, LoginOptions, SecurityFlags, TargetPortal);
			}

			return (Status);
		}

		// iscsicli RemoveTarget <TargetName>
		private static Win32Error RemoveTarget(string[] ArgV)
		{
			string TargetName;

			if (ArgV.Length != 2)
			{
				Usage(2);
				return Win32Error.ERROR_SUCCESS;
			}
			else
			{
				TargetName = ArgV[1];
			}

			return RemoveIScsiStaticTarget(TargetName);
		}


		// iscsicli AddTarget <TargetName> <TargetAlias> <TargetPortalAddress>
		// <TargetPortalSocket> <Target flags>
		// <Persist> <Header Digest> <Data Digest> 
		// <Max Connections> <DefaultTime2Wait>
		// <DefaultTime2Retain> <Username> <Password>
		// <AuthType>
		// <Mapping Count> <Target Lun> <OS Bus> <Os Target> 
		// <OS Lun> ...
		private static Win32Error AddTarget(string[] ArgV)
		{
			Win32Error Status;

			if (ArgV.Length < 17)
			{
				Usage(1);
				Status = Win32Error.ERROR_SUCCESS;
			}
			else
			{
				var TargetName = ArgV[1];
				var TargetAlias = ArgV[2].StartsWith('*') ? default : ArgV[2];

				ISCSI_TARGET_PORTAL_GROUP PortalGroup = default;
				if ((!ArgV[3].StartsWith('*')) && (!ArgV[4].StartsWith('*')))
				{
					var TargetPortalAddress = ArgV[3];
					if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
					{
						return (Win32Error.ERROR_INVALID_PARAMETER);
					}
					var TargetPortalSocket = ArgV[4];

					PortalGroup.Count = 1;
					PortalGroup.Portals = new[] { new ISCSI_TARGET_PORTAL { Address = TargetPortalAddress, Socket = ushort.Parse(TargetPortalSocket) } };
				}
				else
				{
					PortalGroup.Count = 0;
				}

				var TargetFlags = (ISCSI_TARGET_FLAGS)int.Parse(ArgV[5]);
				var Persist = IsTrue(ArgV[6], true);

				ParseLoginOptions(out ISCSI_LOGIN_OPTIONS LoginOptions, ArgV, 8);

				var MappingCount = uint.Parse(ArgV[16]);
				var ArgCExpected = 17 + ((int)MappingCount * 4);
				if (ArgV.Length != ArgCExpected)
				{
					Usage(1);
					return (Win32Error.ERROR_SUCCESS);
				}

				var SizeNeeded = (uint)Marshal.SizeOf<ISCSI_TARGET_MAPPING>() + MappingCount * (uint)Marshal.SizeOf<SCSI_LUN_LIST>();
				var ArgCIndex = 17;
				var m = new ISCSI_TARGET_MAPPING
				{
					OSBusNumber = uint.Parse(ArgV[ArgCIndex + 1]),
					OSTargetNumber = uint.Parse(ArgV[ArgCIndex + 2]),
					LUNCount = MappingCount,
				};
				using SafeAllocatedMemoryHandle Mapping = MappingCount > 0 ? new SafeCoTaskMemStruct<ISCSI_TARGET_MAPPING>(m, SizeNeeded) : SafeCoTaskMemHandle.Null;
				if (MappingCount > 0)
				{
					IntPtr ptr = Mapping.DangerousGetHandle().Offset(Marshal.OffsetOf<ISCSI_TARGET_MAPPING>(nameof(ISCSI_TARGET_MAPPING.LUNList)).ToInt64());
					ptr.Write(Mapping.DangerousGetHandle().Offset(Marshal.SizeOf<ISCSI_TARGET_MAPPING>()));

					var LUNList = new SCSI_LUN_LIST[(int)MappingCount];
					for (var i = 0; i < MappingCount; i++)
					{
						var b = stoiDForLogicalUnit(ArgV[ArgCIndex], out LUNList[i].TargetLUN);
						if (b == false)
						{
							Console.Write("Target LUN must be in 0x0123456789abcdef format\n");
							return (Win32Error.ERROR_INVALID_PARAMETER);
						}
						ArgCIndex++;

						var x = uint.Parse(ArgV[ArgCIndex]);
						if (x != m.OSBusNumber)
						{
							Console.Write("OSBus number must be the same for all LUNs\n");
							return (Win32Error.ERROR_INVALID_PARAMETER);
						}

						ArgCIndex++; // target
						x = uint.Parse(ArgV[ArgCIndex]);
						if (x != m.OSTargetNumber)
						{
							Console.Write("OSTarget number must be the same for all LUNs\n");
							return (Win32Error.ERROR_INVALID_PARAMETER);
						}

						ArgCIndex++;
						LUNList[i].OSLUN = uint.Parse(ArgV[ArgCIndex]);
						ArgCIndex++;
					}

					Mapping.DangerousGetHandle().Write(LUNList, Marshal.SizeOf<ISCSI_TARGET_MAPPING>());
				}

				using var pLoginOptions = SafeCoTaskMemHandle.CreateFromStructure(LoginOptions);
				using SafeCoTaskMemHandle pPortalGroup = PortalGroup.Count == 0 ? SafeCoTaskMemHandle.Null : SafeCoTaskMemHandle.CreateFromStructure(PortalGroup);
				Status = AddIScsiStaticTarget(TargetName, TargetAlias, TargetFlags, Persist, Mapping,
					pLoginOptions, pPortalGroup);
			}
			return (Status);
		}

		// iscsicli AddiSNSServer <Server name>
		private static Win32Error AddiSNSServerX(string[] ArgV)
		{
			string ServerName;

			if (ArgV.Length != 2)
			{
				Usage(21);
				return (Win32Error.ERROR_SUCCESS);
			}

			ServerName = ArgV[1];

			return AddISNSServer(ServerName);
		}


		// iscsicli RemoveiSNSServer <Server name>
		private static Win32Error RemoveiSNSServerX(string[] ArgV)
		{
			string ServerName;

			if (ArgV.Length != 2)
			{
				Usage(22);
				return (Win32Error.ERROR_SUCCESS);
			}

			ServerName = ArgV[1];

			return RemoveISNSServer(ServerName);
		}

		// iscsicli RefreshiSNSServer <Server name>
		private static Win32Error RefreshiSNSServer(string[] ArgV)
		{
			if (ArgV.Length != 2)
			{
				Usage(23);
				return (Win32Error.ERROR_SUCCESS);
			}

			var ServerName = ArgV[1].StartsWith('*') ? default : ArgV[1];

			return RefreshISNSServer(ServerName);
		}

		private static Win32Error ListiSNSServers(string[] ArgV)
		// Console.Write("iscsicli ListiSNSServer\n");
		{
			var SizeNeeded = 0U;
			Win32Error Status = ReportISNSServerList(ref SizeNeeded, default);
			if (Status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
			{
				using var Buffer = new SafeCoTaskMemHandle(SizeNeeded * StringHelper.GetCharSize());
				if (!Buffer.IsInvalid)
				{
					Status = ReportISNSServerList(ref SizeNeeded, Buffer);
					if (Status == Win32Error.ERROR_SUCCESS)
					{
						foreach (var b in Buffer.ToStringEnum())
							Console.Write(" {0}\n", b); ;
					}
				}
			}
			else if (Status == Win32Error.ERROR_SUCCESS)
			{
				Console.Write("No SNS Servers\n");
			}

			return (Status);
		}

		/*++

		Routine Description:

		This routine will allocate a login options structure and build it
		to contain the CHAP username and password needed for one way CHAP

		Arguments:

		CHAPUsername is the chap username to use

		CHAPPassword is the chap password to use

		*LoginOptionsPtr returns with the filled in login option structure

		Return Value:

		Win32Error.ERROR_SUCCESS or error code

		--*/
		private static Win32Error BuildLoginOptionsForCHAP(string CHAPUsername, string CHAPPassword, out SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS> LoginOptions)
		{
			Win32Error Status = Win32Error.ERROR_SUCCESS;

			LoginOptions = new ISCSI_LOGIN_OPTIONS
			{
				Version = ISCSI_LOGIN_OPTIONS_VERSION,
				InformationSpecified = ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_USERNAME | ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_PASSWORD | ISCSI_LOGIN_OPTIONS_INFO_SPECIFIED.ISCSI_LOGIN_OPTIONS_AUTH_TYPE,
				AuthType = ISCSI_AUTH_TYPES.ISCSI_CHAP_AUTH_TYPE
			};

			if (!CHAPUsername.StartsWith('-') && !CHAPUsername.StartsWith('*'))
			{
				LoginOptions.Size += CHAPUsername.Length + 1;
				LoginOptions.AsRef().Username = LoginOptions.DangerousGetHandle().Offset(Marshal.SizeOf<ISCSI_LOGIN_OPTIONS>());
				LoginOptions.AsRef().UsernameLength = (uint)CHAPUsername.Length;
				StringHelper.Write(CHAPUsername, LoginOptions.DangerousGetHandle(), out _, true, CharSet.Ansi);
			}

			if (!CHAPPassword.StartsWith('-') && !CHAPPassword.StartsWith('*'))
			{
				var Secret = CHAPPassword;
				if ((Secret[0] == '0') && ((Secret[1] == 'X') || (Secret[1] == 'x')))
				{
					Status = ParseHexString(Secret[2..], out var pPassword);
					var offset = LoginOptions.DangerousGetHandle().Offset(LoginOptions.Size);
					LoginOptions.Size += pPassword.Size;
					LoginOptions.AsRef().Password = offset;
					LoginOptions.AsRef().PasswordLength = (uint)pPassword.Size;
					offset.Write(pPassword.GetBytes(0, pPassword.Size));
				}
				else
				{
					var offset = LoginOptions.DangerousGetHandle().Offset(LoginOptions.Size);
					LoginOptions.Size += CHAPPassword.Length + 1;
					LoginOptions.AsRef().Password = offset;
					LoginOptions.AsRef().PasswordLength = (uint)CHAPPassword.Length;
					StringHelper.Write(CHAPPassword, offset, out _, true, CharSet.Ansi);
				}
			}

			return (Status);
		}

		// Console.Write("iscsicli QLoginTarget <TargetName> [CHAP Username] [CHAP Password]\n");
		private static Win32Error QLoginTarget(string[] ArgV)
		{
			Win32Error Status;
			string TargetName, CHAPUsername, CHAPPassword;
			SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS> LoginOptions;

			if ((ArgV.Length != 2) && (ArgV.Length != 4))
			{
				Usage(33);
				return (Win32Error.ERROR_SUCCESS);
			}

			TargetName = ArgV[1];

			if (ArgV.Length == 4)
			{
				CHAPUsername = ArgV[2];
				CHAPPassword = ArgV[3];

				Status = BuildLoginOptionsForCHAP(CHAPUsername, CHAPPassword, out LoginOptions);
			}
			else
			{
				LoginOptions = SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS>.Null;
				Status = Win32Error.ERROR_SUCCESS;
			}

			if (Status == Win32Error.ERROR_SUCCESS)
			{
				Status = LoginIScsiTarget(TargetName,
					false, // IsInformationalSession
					default, // InitiatorInstance,
					ISCSI_ANY_INITIATOR_PORT,
					default, // TargetPortal
					0, // SecurityFlags,
					default, // Mappings,
					LoginOptions,
					0, // KeySize,
					default, // Key
					false, // IsPersistent,
					out ISCSI_UNIQUE_SESSION_ID SessionId,
					out ISCSI_UNIQUE_SESSION_ID ConnectionId);

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Console.Write("Session Id is 0x{0:X}-0x{1:X}\n", SessionId.AdapterUnique, SessionId.AdapterSpecific);
					Console.Write("Connection Id is 0x{0:X}-0x{1:X}\n", ConnectionId.AdapterUnique, ConnectionId.AdapterSpecific);
				}
			}

			return (Status);
		}

		// Console.Write("iscsicli QAddTarget <TargetName> <TargetPortalAddress>\n");
		private static Win32Error QAddTarget(string[] ArgV)
		{
			Win32Error Status;
			ISCSI_TARGET_PORTAL_GROUP PortalGroup = default;
			string TargetName, TargetPortalAddress;

			if (ArgV.Length != 3)
			{
				Usage(34);
				return (Win32Error.ERROR_SUCCESS);
			}

			TargetName = ArgV[1];

			if (!ArgV[2].StartsWith('*'))
			{
				TargetPortalAddress = ArgV[2];
				if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
				{
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}

				PortalGroup.Count = 1;
				PortalGroup.Portals = new ISCSI_TARGET_PORTAL[] { new ISCSI_TARGET_PORTAL { Address = TargetPortalAddress, Socket = 3260 } };
			}
			else
			{
				PortalGroup.Count = 0;
				PortalGroup.Portals = new ISCSI_TARGET_PORTAL[] { default };
			}

			Status = AddIScsiStaticTarget(TargetName,
				default, // TargetAlias,
				0, // TargetFlags
				true, // Persist
				default, // Mappings
				default, // LoginOptions
				PortalGroup);

			return (Status);
		}

		// Console.Write("iscsicli QAddTargetPortal <TargetPortalAddress> 
		// Console.Write(" [CHAP Username] [CHAP Password]\n"
		private static Win32Error QAddTargetPortal(string[] ArgV)
		{
			Win32Error Status;
			ISCSI_TARGET_PORTAL TargetPortal;
			string TargetPortalAddress;
			string CHAPUsername, CHAPPassword;
			SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS> LoginOptions;

			if ((ArgV.Length != 2) && (ArgV.Length != 4))
			{
				Usage(35);
				return (Win32Error.ERROR_SUCCESS);
			}

			TargetPortalAddress = ArgV[1];
			if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
			{
				return (Win32Error.ERROR_INVALID_PARAMETER);
			}

			TargetPortal.Address = TargetPortalAddress;
			//TargetPortal.Address[MAX_ISCSI_PORTAL_ADDRESS_LEN - 1] = 0;
			TargetPortal.Socket = 3260;

			if (ArgV.Length == 4)
			{
				CHAPUsername = ArgV[2];
				CHAPPassword = ArgV[3];

				Status = BuildLoginOptionsForCHAP(CHAPUsername, CHAPPassword, out LoginOptions);
			}
			else
			{
				LoginOptions = SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS>.Null;
				Status = Win32Error.ERROR_SUCCESS;
			}

			if (Status == Win32Error.ERROR_SUCCESS)
			{
				Status = AddIScsiSendTargetPortal(default, // InitiatorInstance,
					ISCSI_ANY_INITIATOR_PORT,
					LoginOptions,
					0, // SecurityFlags,
					TargetPortal);
			}

			return (Status);
		}

		// Console.Write("iscsicli QAddConnection <SessionId> <Initiator Instance>\n");
		// Console.Write(" <Target Portal Address>
		// Console.Write(" [CHAP Username] [CHAP Password]\n");
		private static Win32Error QAddConnection(string[] ArgV)
		{
			Win32Error Status;
			ISCSI_TARGET_PORTAL TargetPortal;
			string TargetPortalAddress;
			string InitiatorName;
			SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS> LoginOptions;
			string CHAPUsername, CHAPPassword;

			if ((ArgV.Length != 4) && (ArgV.Length != 6))
			{
				Usage(36);
				return (Win32Error.ERROR_SUCCESS);
			}

			if (!StringToSessionId(ArgV[1], out ISCSI_UNIQUE_SESSION_ID SessionId))
			{
				Usage(36);
				return (Win32Error.ERROR_SUCCESS);
			}

			InitiatorName = ArgV[2];

			if (!ArgV[3].StartsWith('*'))
			{
				TargetPortalAddress = ArgV[3];
				if (TargetPortalAddress.Length > (MAX_ISCSI_PORTAL_ADDRESS_LEN - 1))
				{
					return (Win32Error.ERROR_INVALID_PARAMETER);
				}

				TargetPortal.Address = TargetPortalAddress;
				TargetPortal.Socket = 3260;
			}
			else
			{
				TargetPortalAddress = "";
			}

			if (ArgV.Length == 6)
			{
				CHAPUsername = ArgV[4];
				CHAPPassword = ArgV[5];

				Status = BuildLoginOptionsForCHAP(CHAPUsername, CHAPPassword, out LoginOptions);
			}
			else
			{
				LoginOptions = SafeCoTaskMemStruct<ISCSI_LOGIN_OPTIONS>.Null;
				Status = Win32Error.ERROR_SUCCESS;
			}

			if (Status == Win32Error.ERROR_SUCCESS)
			{
				Status = AddIScsiConnection(SessionId,
					InitiatorName,
					ISCSI_ANY_INITIATOR_PORT,
					string.IsNullOrEmpty(TargetPortalAddress) ? default : TargetPortal,
					0, // SecurityFlags,
					LoginOptions,
					0, // KeySize,
					default, // Key
					out ISCSI_UNIQUE_SESSION_ID ConnectionId);

				if (Status == Win32Error.ERROR_SUCCESS)
				{
					Console.Write("Connection Id is 0x{0:X}-0x{1:X}\n", ConnectionId.AdapterUnique, ConnectionId.AdapterSpecific);
				}
			}

			return (Status);
		}

		private static Win32Error PerformCommandLine(string[] ArgV)
		{
			Win32Error Status = Win32Error.ERROR_SUCCESS;

			if (ArgV != default)
			{
				if (ArgV.Length > 0)
				{
					// AddIScsiStaticTarget
					if (string.Compare(ArgV[0], "AddTarget", true) == 0)
					{
						Status = AddTarget(ArgV);

						// RemoveIScsiStaticTarget 
					}
					else if (string.Compare(ArgV[0], "RemoveTarget", true) == 0)
					{
						Status = RemoveTarget(ArgV);

						// AddIScsiSendTargetPortal 
					}
					else if (string.Compare(ArgV[0], "AddTargetPortal", true) == 0)
					{
						Status = AddTargetPortal(ArgV);

						// RemoveIScsiSendTargetPortal 
					}
					else if (string.Compare(ArgV[0], "RemoveTargetPortal", true) == 0)
					{
						Status = RemoveTargetPortal(ArgV);

						// RefreshIScsiSendTargetPortal 
					}
					else if (string.Compare(ArgV[0], "RefreshTargetPortal", true) == 0)
					{
						Status = RefreshTargetPortal(ArgV);

						// ReportIScsiSendTargetPortals 
					}
					else if (string.Compare(ArgV[0], "ListTargetPortals", true) == 0)
					{
						Status = ListTargetPortals(ArgV);

						// ReportTargets 
					}
					else if (string.Compare(ArgV[0], "ListTargets", true) == 0)
					{
						Status = ListTargets(ArgV);

						// GetTargetInformation 
					}
					else if (string.Compare(ArgV[0], "TargetInfo", true) == 0)
					{
						Status = TargetInfo(ArgV);

						// LoginTarget 
					}
					else if (string.Compare(ArgV[0], "LoginTarget", true) == 0)
					{
						Status = TryLoginToTarget(ArgV);

						// PersistentLoginTarget 
					}
					else if (string.Compare(ArgV[0], "PersistentLoginTarget", true) == 0)
					{
						Status = PersistentLoginTarget(ArgV);

						// RemovePersistentTarget
					}
					else if (string.Compare(ArgV[0], "RemovePersistentTarget", true) == 0)
					{
						Status = RemovePersistentTarget(ArgV);

						// ListPersistentTarget
					}
					else if (string.Compare(ArgV[0], "ListPersistentTargets", true) == 0)
					{
						Status = ListPersistentTarget(ArgV);

						// LogoutTarget 
					}
					else if (string.Compare(ArgV[0], "LogoutTarget", true) == 0)
					{
						Status = DoLogoutTarget(ArgV);

						// ReportInitiatorList 
					}
					else if (string.Compare(ArgV[0], "ListInitiators", true) == 0)
					{
						Status = DoReportInitiatorList(ArgV);

						// ReportActiveIScsiTargetMappings
					}
					else if (string.Compare(ArgV[0], "ReportTargetMappings", true) == 0)
					{
						Status = DoReportActiveIScsiTargetMappings(ArgV);

						// AddConnection 
					}
					else if (string.Compare(ArgV[0], "AddConnection", true) == 0)
					{
						Status = DoAddConnection(ArgV);

						// RemoveConnection
					}
					else if (string.Compare(ArgV[0], "RemoveConnection", true) == 0)
					{
						Status = DoRemoveConnection(ArgV);

						// SendScsiInquiry 
					}
					else if (string.Compare(ArgV[0], "ScsiInquiry", true) == 0)
					{
						Status = DoScsiInquiry(ArgV);

						// SendScsiReadCapacity 
					}
					else if (string.Compare(ArgV[0], "ReadCapacity", true) == 0)
					{
						Status = ReadCapacity(ArgV);

						// SendScsiReportLuns 
					}
					else if (string.Compare(ArgV[0], "ReportLUNs", true) == 0)
					{
						Status = ReportLUNs(ArgV);

						// AddiSNSServer
					}
					else if (string.Compare(ArgV[0], "AddiSNSServer", true) == 0)
					{
						Status = AddiSNSServerX(ArgV);

						// RemoveiSNSServer
					}
					else if (string.Compare(ArgV[0], "RemoveiSNSServer", true) == 0)
					{
						Status = RemoveiSNSServerX(ArgV);

						// ListiSNSServers
					}
					else if (string.Compare(ArgV[0], "ListiSNSServers", true) == 0)
					{
						Status = ListiSNSServers(ArgV);

						// RefreshiSNSServer
					}
					else if (string.Compare(ArgV[0], "RefreshiSNSServer", true) == 0)
					{
						Status = RefreshiSNSServer(ArgV);

						// TunnelAddr
					}
					else if (string.Compare(ArgV[0], "TunnelAddr", true) == 0)
					{
						Status = TunnelAddress(ArgV);

						// GroupKey
					}
					else if (string.Compare(ArgV[0], "GroupKey", true) == 0)
					{
						Status = GroupKey(ArgV);

						// PSKey
					}
					else if (string.Compare(ArgV[0], "PSKey", true) == 0)
					{
						Status = PSKey(ArgV);

						// CHAPSecret
					}
					else if (string.Compare(ArgV[0], "CHAPSecret", true) == 0)
					{
						Status = CHAPSecret(ArgV);

					}
					else if (string.Compare(ArgV[0], "NodeName", true) == 0)
					{
						Status = NodeName(ArgV);

					}
					else if (string.Compare(ArgV[0], "SessionList", true) == 0)
					{
						Status = SessionList(ArgV);

					}
					else if (string.Compare(ArgV[0], "BindPersistentVolumes", true) == 0)
					{
						Status = BindPeristentVolumes(ArgV);

					}
					else if (string.Compare(ArgV[0], "BindPersistentDevices", true) == 0)
					{
						Status = BindPeristentVolumes(ArgV);

					}
					else if (string.Compare(ArgV[0], "AddPersistentDevice", true) == 0)
					{
						Status = AddPersistentVolume(ArgV);

					}
					else if (string.Compare(ArgV[0], "RemovePersistentDevice", true) == 0)
					{
						Status = RemovePersistentVolume(ArgV);

					}
					else if (string.Compare(ArgV[0], "ClearPersistentDevices", true) == 0)
					{
						Status = ClearPersistentVolumes(ArgV);

					}
					else if (string.Compare(ArgV[0], "ReportPersistentDevices", true) == 0)
					{
						Status = ReportPersistentVolumes(ArgV);

					}
					else if (string.Compare(ArgV[0], "GetPSKey", true) == 0)
					{
						Status = GetPSKey(ArgV);

					}
					else if (string.Compare(ArgV[0], "QLoginTarget", true) == 0)
					{
						Status = QLoginTarget(ArgV);

					}
					else if (string.Compare(ArgV[0], "QAddTarget", true) == 0)
					{
						Status = QAddTarget(ArgV);

					}
					else if (string.Compare(ArgV[0], "QAddTargetPortal", true) == 0)
					{
						Status = QAddTargetPortal(ArgV);

					}
					else if (string.Compare(ArgV[0], "QAddConnection", true) == 0)
					{
						Status = QAddConnection(ArgV);
					}
					else
					{
						Usage(0);
						Status = Win32Error.ERROR_SUCCESS;
					}
				}
				else
				{
					Usage(0);
					Status = Win32Error.ERROR_SUCCESS;
				}

			}
			else
			{
				Status = Win32Error.ERROR_NOT_ENOUGH_MEMORY;
			}
			Console.Write("{0}\n", GetiSCSIMessageText(Status));
			return (Status);
		}

		private const int INPUT_BUFFER_SIZE = 4096;

		private static int Main(string[] argv)
		{
			Win32Error Status = GetIScsiVersionInformation(out ISCSI_VERSION_INFO iSCSIVer);
			if (Status == Win32Error.ERROR_SUCCESS)
			{
				Console.Write("Microsoft iSCSI Initiator Version {0}.{1}\n\n", iSCSIVer.MajorVersion, iSCSIVer.MinorVersion);
			}

			Status = WSAStartup(Macros.MAKEWORD(1, 1), out _);
			if (Status == Win32Error.ERROR_SUCCESS)
			{
				try
				{
					if (argv.Length == 0)
					{
						Usage(0);
						var NodeName = new StringBuilder(MAX_ISCSI_NAME_LEN + 1);
						Status = GetIScsiInitiatorNodeName(NodeName);
						if (Status == Win32Error.ERROR_SUCCESS)
						{
							Console.Write("Running on node name {0}\n", NodeName.ToString());
						}
					}
					else
					{
						Status = PerformCommandLine(argv);
					}

				}
				finally
				{
					WSACleanup();
				}
			}
			else
			{
				Console.Write("Error setting up Windows sockets: {0}\n", GetiSCSIMessageText(Status));
			}

			return unchecked((int)(uint)Status);
		}
	}
}