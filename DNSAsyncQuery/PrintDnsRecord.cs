using static Vanara.PInvoke.DnsApi;

namespace DNSAsyncQuery
{
	static partial class DnsQueryEx
	{
		private static PRINT_DNS_RECORD_DATA_FUNCTION[] PrintDataTable = new PRINT_DNS_RECORD_DATA_FUNCTION[]
		{
			null, // ZERO
			ARecordPrint, // A
			PtrRecordPrint, // NS
			PtrRecordPrint, // MD
			PtrRecordPrint, // MF
			PtrRecordPrint, // CNAME
			SoaRecordPrint, // SOA
			PtrRecordPrint, // MB
			PtrRecordPrint, // MG
			PtrRecordPrint, // MR
			null, // null
			null, // WKS
			PtrRecordPrint, // PTR
			TxtRecordPrint, // HINFO
			null, // MINFO
			MxRecordPrint, // MX
			TxtRecordPrint, // TXT
			null, // RP
			MxRecordPrint, // AFSDB
			TxtRecordPrint, // X25
			TxtRecordPrint, // ISDN
			MxRecordPrint, // RT
			null, // NSAP
			null, // NSAPPTR
			SigRecordPrint, // SIG
			KeyRecordPrint, // KEY
			null, // PX
			null, // GPOS
			AaaaRecordPrint, // AAAA
			null, // LOC
			null, // NXT
			null, // EID
			null, // NIMLOC
			SrvRecordPrint, // SRV
			null, // ATMA
			null, // NAPTR
			null, // KX
			null, // CERT
			null, // A6
			null, // DNAME
			null, // SINK
			null, // OPT
			null, // 42
			DsRecordPrint, // DS
			null, // 44
			null, // 45
			SigRecordPrint, // RRSIG
			NsecRecordPrint, // NSEC
			KeyRecordPrint, // DNSKEY
			null, // DHCID
			Nsec3RecordPrint, // NSEC3
			Nsec3ParamRecordPrint // NSEC3PARAM
		};

		private delegate void PRINT_DNS_RECORD_DATA_FUNCTION(in DNS_RECORD DnsRecord);

		private static void AaaaRecordPrint(in DNS_RECORD DnsRecord)
		{
			var aaaa = (DNS_AAAA_DATA)DnsRecord.Data;

			Console.Write("\tIP address = {0}\n", aaaa.Ip6Address);
		}

		private static void ARecordPrint(in DNS_RECORD DnsRecord)
		{
			var Ipv4address = ((DNS_A_DATA)DnsRecord.Data).IpAddress;

			Console.Write("\tIP address = {0}\n", Ipv4address.ToString());
		}

		private static void DsRecordPrint(in DNS_RECORD DnsRecord)
		{
			var ds = (DNS_DS_DATA)DnsRecord.Data;
			Console.Write("\tKey Tag = {0}\n" +
				"\tAlgorithm = {1}\n" +
				"\tDigest Type = {2}\n",
				ds.wKeyTag,
				ds.chAlgorithm,
				ds.chDigestType);
		}

		private static void KeyRecordPrint(in DNS_RECORD DnsRecord)
		{
			var key = (DNS_KEY_DATA)DnsRecord.Data;
			Console.Write("\tFlags = 0x{0:X4}\n" +
				"\tProtocol = {1}\n" +
				"\tAlgorithm = {2}\n",
				key.wFlags,
				key.chProtocol,
				key.chAlgorithm);
		}

		private static void MxRecordPrint(in DNS_RECORD DnsRecord)
		{
			var mx = (DNS_MX_DATA)DnsRecord.Data;
			Console.Write("\tPreference = {0}\n\tExchange = {1}\n", mx.wPreference, mx.pNameExchange);
		}

		private static void Nsec3ParamRecordPrint(in DNS_RECORD DnsRecord)
		{
			var ns3p = (DNS_NSEC3PARAM_DATA)DnsRecord.Data;
			Console.Write("\tHashAlgorithm = {0}\n" +
				"\tFlags = 0x{1:X2}\n" +
				"\tIterations = {2}\n",
				ns3p.chAlgorithm,
				ns3p.bFlags,
				ns3p.wIterations);
		}

		private static void Nsec3RecordPrint(in DNS_RECORD DnsRecord)
		{
			var nsec3 = (DNS_NSEC3_DATA)DnsRecord.Data;
			Console.Write("\tHashAlgorithm = {0}\n" +
				"\tFlags = 0x{1:X2}\n" +
				"\tIterations = {2}\n",
				nsec3.chAlgorithm,
				nsec3.bFlags,
				nsec3.wIterations);
		}

		private static void NsecRecordPrint(in DNS_RECORD DnsRecord)
		{
			Console.Write("\tNext Domain Name = {0}\n",
				((DNS_NSEC_DATA)DnsRecord.Data).pNextDomainName);
		}

		private static void PrintDnsRecordList(SafeDnsRecordList DnsRecord)
		{
			foreach (var rec in DnsRecord)
			{
				PrintRecord(rec);
			}
		}

		// This sample prints record data for most commonly used DNS TYPES.
		private static void PrintRecord(in DNS_RECORD DnsRecord)
		{
			Console.Write(" Record:\n" +
			"\tOwner = {0}\n" +
			"\tType = {1}\n" +
			"\tFlags = {2:X8}\n" +
			"\t\tSection = {3}\n" +
			"\t\tDelete = {4}\n" +
			"\t\tCharSet = {5}\n" +
			"\tTTL = {6}\n" +
			"\tReserved = {7}\n" +
			"\tDataLength = {8}\n",
			DnsRecord.pName,
			DnsRecord.wType,
			DnsRecord.Flags.DW,
			DnsRecord.Flags.Section,
			DnsRecord.Flags.Delete,
			DnsRecord.Flags.CharSet,
			DnsRecord.dwTtl,
			DnsRecord.dwReserved,
			DnsRecord.wDataLength);

			if ((int)DnsRecord.wType < PrintDataTable.Length)
			{
				PrintDataTable[(int)DnsRecord.wType]?.Invoke(DnsRecord);
			}
		}

		private static void PtrRecordPrint(in DNS_RECORD DnsRecord)
		{
			Console.Write("\tHostName = {0}\n", ((DNS_PTR_DATA)DnsRecord.Data).pNameHost);
		}

		private static void SigRecordPrint(in DNS_RECORD DnsRecord)
		{
			var sig = (DNS_SIG_DATA)DnsRecord.Data;
			Console.Write("\tType Covered = {0}\n" +
				"\tAlgorithm = {1}\n" +
				"\tLabels = {2}\n" +
				"\tOriginal TTL = {3}\n" +
				"\tSignature Expiration = {4}\n" +
				"\tSignature Inception = {5}\n" +
				"\tKey Tag = {6}\n" +
				"\tSigner's Name = {7}\n",
				sig.wTypeCovered,
				sig.chAlgorithm,
				sig.chLabelCount,
				sig.dwOriginalTtl,
				sig.dwExpiration,
				sig.dwTimeSigned,
				sig.wKeyTag,
				sig.pNameSigner);
		}

		private static void SoaRecordPrint(in DNS_RECORD DnsRecord)
		{
			var soa = (DNS_SOA_DATA)DnsRecord.Data;
			Console.Write("n\tPrimary = {0}\n" +
				"\tAdmin = {0}\n" +
				"\tSerial = {0}\n" +
				"\tRefresh = {0}\n" +
				"\tRetry = {0}\n" +
				"\tExpire = {0}\n" +
				"\tDefault TTL = {0}\n",
				soa.pNamePrimaryServer,
				soa.pNameAdministrator,
				soa.dwSerialNo,
				soa.dwRefresh,
				soa.dwRetry,
				soa.dwExpire,
				soa.dwDefaultTtl);
		}

		private static void SrvRecordPrint(in DNS_RECORD DnsRecord)
		{
			var srv = (DNS_SRV_DATA)DnsRecord.Data;
			Console.Write("\tPriority = {0}\n" +
				"\tWeight = {1}\n" +
				"\tPort = {2}\n" +
				"\tTarget Host = {3}\n",
				srv.wPriority,
				srv.wWeight,
				srv.wPort,
				srv.pNameTarget);
		}

		private static void TxtRecordPrint(in DNS_RECORD DnsRecord)
		{
			var txt = (DNS_TXT_DATA)DnsRecord.Data;
			var Count = txt.dwStringCount;
			var StringArray = txt.pStringArray;

			for (var Index = 1; Index <= Count; Index++)
			{
				Console.Write("\tString[{0}] = {1}\n", Index, StringArray[Index]);
			}
		}
	}
}