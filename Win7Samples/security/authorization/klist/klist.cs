using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Secur32;

namespace KList;

internal static class KList
{
	private static int Main(string[] args)
	{
		if (args.Length < 1)
		{
			Console.Write("Usage: klist.exe <tickets | tgt | purge | get> [service principal name(for get)]\n");
			return 1;
		}

		//
		// Get the logon handle and package ID from the
		// Kerberos package
		//
		if (!PackageConnectLookup(out var LogonHandle, out var PackageId))
			return 1;

		switch (args[0].ToLower())
		{
			case "tickets":
				ShowTickets(LogonHandle, PackageId, false);
				break;
			case "tgt":
				ShowTgt(LogonHandle, PackageId);
				break;
			case "purge":
				ShowTickets(LogonHandle, PackageId, true);
				break;
			case "get":
				if (args.Length < 2)
					Console.Write("Provide service principal name (SPN) of encoded ticket to retrieve\n");
				else
					GetEncodedTicket(LogonHandle, PackageId, args[1]);
				break;
			default:
				Console.Write("Usage: klist.exe <tickets | tgt | purge | get> [service principal name(for get)]\n");
				break;
		}

		return 0;
	}

	public static IEnumerable<string> GetNames(this KERB_EXTERNAL_NAME Name)
	{
		if (Name.NameCount == 0)
			yield break;
		using var pin = new PinnedObject(Name);
		foreach (var us in ((IntPtr)pin).ToIEnum<LSA_UNICODE_STRING>(Name.NameCount, 8))
			yield return us.ToString();
	}

	static void PrintKerbName(IntPtr Name) => PrintKerbName(Name.ToStructure<KERB_EXTERNAL_NAME>());

	static void PrintKerbName(in KERB_EXTERNAL_NAME Name)
	{
		Console.WriteLine(string.Join("/", Name.GetNames()));
	}

	static void PrintTime(string Comment, in FILETIME ConvertTime)
	{
		Console.Write(Comment);

		//
		// If the time is infinite, just say so.
		//
		if (ConvertTime.ToUInt64() == 0x7FFFFFFFFFFFFFFF)
			Console.Write("Infinite\n");

		//
		// Otherwise print it more clearly
		//
		else
			Console.WriteLine(ConvertTime.ToDateTime().ToString("U"));
	}

	static void PrintEType(KERB_ETYPE etype)
	{
		Console.Write("KerbTicket Encryption Type: {0}\n", etype);
	}

	static void PrintTktFlags(KERB_TICKET_FLAGS flags)
	{
		const string baseName = "KERB_TICKET_FLAGS_";
		Console.WriteLine(string.Join(" ", flags.GetFlags().Select(f => f.ToString()).Select(f => f.StartsWith(baseName) ? f.Substring(baseName.Length) : f)));
	}

	static bool PackageConnectLookup(out SafeLsaConnectionHandle pLogonHandle, out uint pPackageId)
	{
		var Status = LsaConnectUntrusted(out pLogonHandle);

		if (Status.Failed)
		{
			ShowNTError("LsaConnectUntrusted", Status);
			pPackageId = 0;
			return false;
		}

		Status = LsaLookupAuthenticationPackage(pLogonHandle, MICROSOFT_KERBEROS_NAME, out pPackageId);

		if (Status.Failed)
		{
			ShowNTError("LsaLookupAuthenticationPackage", Status);
			return false;
		}

		return true;
	}

	static bool PurgeTicket(SafeLsaConnectionHandle LogonHandle, uint PackageId, string Server, string Realm)
	{
		var pServer = new SafeLSA_UNICODE_STRING(Server);
		var pRealm = new SafeLSA_UNICODE_STRING(Realm);
		var CacheRequest = new KERB_PURGE_TKT_CACHE_REQUEST { MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbPurgeTicketCacheMessage, ServerName = pServer, RealmName = pRealm };

		Console.Write("\tDeleting ticket: \n");
		Console.Write("\t   ServerName = {0}\n", CacheRequest.ServerName);
		Console.Write("\t   RealmName  = {0}\n", CacheRequest.RealmName);

		using var pin = new PinnedObject(CacheRequest);
		var Status = LsaCallAuthenticationPackage(LogonHandle, PackageId, pin, (uint)Marshal.SizeOf(typeof(KERB_PURGE_TKT_CACHE_REQUEST)), out _, out _, out var SubStatus);

		if (Status.Failed || SubStatus.Failed)
		{
			ShowNTError("LsaCallAuthenticationPackage(purge)", Status);
			Console.Write("Substatus: 0x{0:X}\n", SubStatus);
			ShowNTError("LsaCallAuthenticationPackage(purge SubStatus)", SubStatus);
			return false;
		}
		else
		{
			Console.Write("\tTicket purged!\n");
			return true;
		}

	}

	static bool ShowTickets(SafeLsaConnectionHandle LogonHandle, uint PackageId, bool interactive)
	{
		var CacheRequest = new KERB_QUERY_TKT_CACHE_REQUEST { MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbQueryTicketCacheMessage };

		var pin = new PinnedObject(CacheRequest);
		var Status = LsaCallAuthenticationPackage(LogonHandle, PackageId, pin, (uint)Marshal.SizeOf<KERB_QUERY_TKT_CACHE_REQUEST>(), out var mem, out _, out var SubStatus);
		if (Status.Failed || SubStatus.Failed)
		{
			ShowNTError("LsaCallAuthenticationPackage", Status);
			Console.Write("Substatus: 0x{0:X}\n", SubStatus);
			return false;
		}
		//mem.Size = bufLen;
		var CacheResponse = mem.ToStructure<KERB_QUERY_TKT_CACHE_RESPONSE>();

		Console.Write("\nCached Tickets: ({0})\n", CacheResponse.CountOfTickets);
		for (var Index = 0; Index < CacheResponse.CountOfTickets; Index++)
		{
			Console.Write("\n   Server: {0}@{1}\n", CacheResponse.Tickets[Index].ServerName, CacheResponse.Tickets[Index].RealmName);
			Console.Write("      ");
			PrintEType(CacheResponse.Tickets[Index].EncryptionType);
			PrintTime("      End Time: ", CacheResponse.Tickets[Index].EndTime);
			PrintTime("      Renew Time: ", CacheResponse.Tickets[Index].RenewTime);
			Console.Write("      TicketFlags: (0x{0:X}) ", CacheResponse.Tickets[Index].TicketFlags);
			PrintTktFlags(CacheResponse.Tickets[Index].TicketFlags);
			Console.WriteLine();

			if (interactive)
			{
				Console.Write("Purge? (y/n/q) : ");
				var ch = Console.Read();
				if (ch == 'y' || ch == 'Y')
				{
					Console.WriteLine();
					PurgeTicket(LogonHandle, PackageId, CacheResponse.Tickets[Index].ServerName.ToString(), CacheResponse.Tickets[Index].RealmName.ToString());
				}
				else if (ch == 'q' || ch == 'Q')
					break;
				else
					Console.Write("\n\n");
			}
		}

		mem.Dispose();
		return true;
	}

	static bool ShowTgt(SafeLsaConnectionHandle LogonHandle, uint PackageId)
	{
		var CacheRequest = new KERB_QUERY_TKT_CACHE_REQUEST { MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveTicketMessage };

		var pin = new PinnedObject(CacheRequest);
		var Status = LsaCallAuthenticationPackage(LogonHandle, PackageId, pin, (uint)Marshal.SizeOf<KERB_QUERY_TKT_CACHE_REQUEST>(), out var mem, out _, out var SubStatus);
		var TicketEntry = mem.ToStructure<KERB_RETRIEVE_TKT_RESPONSE>();

		if (Status.Failed || SubStatus.Failed)
		{
			ShowNTError("LsaCallAuthenticationPackage", Status);
			Console.Write("Substatus: 0x{0:X}\n", SubStatus);
			return false;
		}

		Console.Write("\nCached TGT:\n\n");

		Console.Write("ServiceName: "); PrintKerbName(TicketEntry.Ticket.ServiceName);

		Console.Write("TargetName: "); PrintKerbName(TicketEntry.Ticket.TargetName);

		Console.Write("FullServiceName: "); PrintKerbName(TicketEntry.Ticket.ClientName);

		Console.Write("DomainName: {0}\n", TicketEntry.Ticket.DomainName);

		Console.Write("TargetDomainName: {0}\n", TicketEntry.Ticket.TargetDomainName);

		Console.Write("AltTargetDomainName: {0}\n", TicketEntry.Ticket.AltTargetDomainName);

		Console.Write("TicketFlags: (0x%x) ", TicketEntry.Ticket.TicketFlags);
		PrintTktFlags(TicketEntry.Ticket.TicketFlags);
		PrintTime("KeyExpirationTime: ", TicketEntry.Ticket.KeyExpirationTime);
		PrintTime("StartTime: ", TicketEntry.Ticket.StartTime);
		PrintTime("EndTime: ", TicketEntry.Ticket.EndTime);
		PrintTime("RenewUntil: ", TicketEntry.Ticket.RenewUntil);
		PrintTime("TimeSkew: ", TicketEntry.Ticket.TimeSkew);
		PrintEType((KERB_ETYPE)TicketEntry.Ticket.SessionKey.KeyType);

		mem.Dispose();
		return true;
	}

	static bool GetEncodedTicket(SafeLsaConnectionHandle LogonHandle, uint PackageId, string Server)
	{
		bool Success = false;

		var Target = new SafeLSA_UNICODE_STRING(Server);
		var Target2 = new SafeLSA_UNICODE_STRING(Server);
		var CacheRequest = new KERB_RETRIEVE_TKT_REQUEST { MessageType = KERB_PROTOCOL_MESSAGE_TYPE.KerbRetrieveEncodedTicketMessage, TargetName = Target };

		using var pin = new PinnedObject(CacheRequest);
		var Status = LsaCallAuthenticationPackage(LogonHandle, PackageId, pin, (uint)Marshal.SizeOf(typeof(KERB_RETRIEVE_TKT_REQUEST)), out var mem, out var ResponseSize, out var SubStatus);

		if (Status.Failed || SubStatus.Failed)
		{
			ShowNTError("LsaCallAuthenticationPackage", Status);
			Console.Write("Substatus: 0x{0:X}\n", SubStatus);
			ShowNTError("Substatus:", SubStatus);
		}
		else
		{
			var Ticket = mem.ToStructure<KERB_RETRIEVE_TKT_RESPONSE>().Ticket;

			Console.Write("\nEncoded Ticket:\n\n");

			Console.Write("ServiceName: "); PrintKerbName(Ticket.ServiceName);

			Console.Write("TargetName: "); PrintKerbName(Ticket.TargetName);

			Console.Write("ClientName: "); PrintKerbName(Ticket.ClientName);

			Console.Write("DomainName: {0}\n", Ticket.DomainName);

			Console.Write("TargetDomainName: {0}\n", Ticket.TargetDomainName);

			Console.Write("AltTargetDomainName: {0}\n", Ticket.AltTargetDomainName);

			Console.Write("TicketFlags: (0x%x) ", Ticket.TicketFlags);
			PrintTktFlags(Ticket.TicketFlags);
			PrintTime("KeyExpirationTime: ", Ticket.KeyExpirationTime);
			PrintTime("StartTime: ", Ticket.StartTime);
			PrintTime("EndTime: ", Ticket.EndTime);
			PrintTime("RenewUntil: ", Ticket.RenewUntil);
			PrintTime("TimeSkew: ", Ticket.TimeSkew);
			PrintEType((KERB_ETYPE)Ticket.SessionKey.KeyType);

			Success = true;

		}

		return Success;
	}

	static void ShowLastError(string szAPI, Win32Error dwError) => Console.WriteLine("Error calling function {0}: {1}\n{2}", szAPI, dwError, dwError.FormatMessage());

	// 
	// Convert the NTSTATUS to Winerror. Then call ShowLastError().     
	// 
	static void ShowNTError(string szAPI, NTStatus Status) => ShowLastError(szAPI, LsaNtStatusToWinError(Status));
}