using System.Text;
using Vanara.InteropServices;
using Vanara.PInvoke;

using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WinINet;

namespace CacheEnumerate;

internal class Program
{
	private const int CACHESAMPLE_ACTION_DELETE = 1;
	private const int CACHESAMPLE_ACTION_ENUM_ALL = 2;
	private const int CACHESAMPLE_ACTION_DETAIL = 4;
	private const int CACHESAMPLE_ACTION_ENUM_COOKIE = 8;
	private const int CACHESAMPLE_ACTION_ENUM_HISTORY = 16;
	private const int CACHESAMPLE_ACTION_ENUM_CONTENT = 32;
	private const int CACHESAMPLE_ACTION_ENUM_MASK = CACHESAMPLE_ACTION_ENUM_ALL|CACHESAMPLE_ACTION_ENUM_COOKIE|CACHESAMPLE_ACTION_ENUM_HISTORY|CACHESAMPLE_ACTION_ENUM_CONTENT;

	private const string ALL = null; // Enumerate in all FIXED container
	private const string CONTENT = "";
	private const string COOKIE = "cookie:"; // Enumerate only in the COOKIE container Enumerate only in the CONTENT container
	private const string HISTORY = "visited:"; // Enumerate only in the HISTORY container

	private static int g_dwAction; // action type var.
	private static string g_lpszSearchPattern = default; // Search pattern
	private static string g_lpszURL = default; // Store user input URL

	public static int Main(string[] args)
	{
		// Parse out the command line and set user options
		ParseArguments(args);

		switch (g_dwAction)
		{
			// Enumerate all cache containers and delete option.
			case CACHESAMPLE_ACTION_ENUM_ALL | CACHESAMPLE_ACTION_DELETE:
				ClearCache();
				break;

			// Get entry details on a particular URL
			case CACHESAMPLE_ACTION_DETAIL:
				GetEntryDetail();
				break;

			// Enumerate entries from a particular containers.
			default:
				EnumerateCache();
				break;
		}

		return 0;
	}

	/*++
	Routine Description:
	This routine is the call that starts the clearing of WinInet cache store.
	It calls two other routines to clear the cache stores of both cache groups
	and non grouped entries. The correct way to clear the cache of its content
	is to first delete all the cache groups and their associated cache entries.
	Then, the rest of the non grouped cache entries are deleted.
	Arguments:
	None.
	Return Value:
	None.
	--*/
	private static void ClearCache()
	{
		Console.Write("\t*** Deleting all entries in cache. ***\n");

		// First delete all cache groups.
		DeleteAllCacheGroups();

		// Delete the rest of the non grouped entries.
		EnumerateCache();
	}

	/*++
	Routine Description:
	This routine is used to delete all the cache groups in the WinInet cache
	store. It utilize the "find first" and "find next" group enumeration
	routine to enumerate all the groups. The delete group routine was
	called with CACHEGROUP_FLAG_FLUSHURL_ONDELETE flag to have all the
	cache entries associated with the particular group flushed/deleted.
	Arguments:
	None.
	Return Value:
	None.
	--*/
	private static void DeleteAllCacheGroups()
	{
		foreach (var gID in FindUrlCacheGroups())
		{
			// Delete the cache group and flush all entries tag to this group.
			Console.Write("\t Deleting cache group ID: {0}\n", gID);
			if (!DeleteUrlCacheGroup(gID, // ID of the cache group to be released.
				CACHEGROUP_FLAG.CACHEGROUP_FLAG_FLUSHURL_ONDELETE)) // delete all of the cache entries associated with this group, unless the entry belongs to another group.
			{
				// Log out any errors on delete. Additional error processing may be needed deppending on condition.
				LogInetError(Win32Error.GetLastError(), "DeleteUrlCacheGroup");
			}
		}
	}

	/*++
	Routine Description:
	This routine is used to enumerate the WinInet cache store. It utilizes
	the Ex versions of both the "find first" and "find next" pair of cache
	enumeration routines. It uses the default all inclusive flag for
	cache entry enumeration.
	Arguments:
	None.
	Return Value:
	None.
	--*/
	private static void EnumerateCache()
	{
		// decide which search pattern to use based on the options set.
		switch (g_dwAction & CACHESAMPLE_ACTION_ENUM_MASK)
		{
			case CACHESAMPLE_ACTION_ENUM_ALL:
				g_lpszSearchPattern = ALL;
				break;

			case CACHESAMPLE_ACTION_ENUM_COOKIE:
				g_lpszSearchPattern = COOKIE;
				break;

			case CACHESAMPLE_ACTION_ENUM_HISTORY:
				g_lpszSearchPattern = HISTORY;
				break;

			case CACHESAMPLE_ACTION_ENUM_CONTENT:
				g_lpszSearchPattern = CONTENT;
				break;

			// Unknown search pattern.
			default:
				Console.Write("Unknown search pattern: 0x{0:X}\n\n", g_dwAction & CACHESAMPLE_ACTION_ENUM_MASK);
				return;
		}

		foreach (INTERNET_CACHE_ENTRY_INFO_MGD lpCacheEntryInfo in FindUrlCacheEntries(g_lpszSearchPattern))
		{
			// We will delete the entry if the CACHESAMPLE_ACTION_DELETE flag is set; otherwise, simply display the source URL.
			if (lpCacheEntryInfo.lpszSourceUrlName is not null)
			{
				Console.Write("The cache entry's source URL is: {0}\n", lpCacheEntryInfo.lpszSourceUrlName);

				// Delete the entry if delete flag is set.
				if ((g_dwAction & CACHESAMPLE_ACTION_DELETE) != 0)
				{
					Console.Write("Deleting the cache entry with URL: {0}\n", lpCacheEntryInfo.lpszSourceUrlName);

					// Check the success status of delete. Addition error handling may be needed if situation warrant it.
					if (!DeleteUrlCacheEntry(lpCacheEntryInfo.lpszSourceUrlName))
					{
						var dwErr = Win32Error.GetLastError();

						// This error is returned when a delete is attempt on an entry that has been locked. In this case, the entry will be
						// marked for delete in the next process startup in WinInet.
						if (Win32Error.ERROR_SHARING_VIOLATION == dwErr)
						{
							Console.Write("The entry is locked and will be deleted in the next process startup.\n");
						}
						else
						{
							LogInetError(dwErr, "DeleteUrlCacheEntry");
						}
					}
				}
			}
		}
	}

	/*++
	Routine Description:
	This routine is used to retrieve cache entry detail on a specific URL. The
	advantage of this routine is that if one knows the specific URL for a cache
	entry, the detail of that particular entry is easily retrieve this call
	rather than using the "find first" and "find next" pair of cache apis to
	enum to the correct entry.
	Arguments:
	lpszURL - Source URL for a particular cache entry.
	Return Value:
	None.
	--*/
	private static void GetEntryDetail()
	{
		uint cbEntrySize = 0;

		// 1st call to GetUrlCacheEntryInfo with default buffer to get the buffer size needed.
		using SafeCoTaskMemStruct<INTERNET_CACHE_ENTRY_INFO> lpCacheEntryInfo = new(0);
		if (!GetUrlCacheEntryInfo(g_lpszURL, // The URL to the entry
			default, // Buffer
			ref cbEntrySize)) // Buffer size
		{
			var dwErr = (uint)Win32Error.GetLastError();
			switch (dwErr)
			{
				case Win32Error.ERROR_FILE_NOT_FOUND:
					// No entry found with the specified URL. We're done.
					Console.Write("There is no cache entry associated with the requested URL : {0}\n", g_lpszURL);
					return;

				case Win32Error.ERROR_INSUFFICIENT_BUFFER:
					// allocate buffer with the correct size.
					lpCacheEntryInfo.Size = cbEntrySize;
					lpCacheEntryInfo.InitializeSizeField("dwStructSize");
					break;

				default:
					// We shouldn't have any other errors
					LogInetError(dwErr, "GetUrlCacheEntryInfo");
					return;
			}
		}

		// Called GetUrlCacheEntryInfo with the correct buffer size.
		if (!GetUrlCacheEntryInfo(g_lpszURL, // The URL to the entry.
			lpCacheEntryInfo, // Buffer.
			ref cbEntrySize)) // Buffer size.
		{
			// If the entry is not in th cache. The previous call will resulted in Win32Error.ERROR_FILE_NOT_FOUND even with a default
			// buffer. We shouldn't have any other errors.
			LogInetError(Win32Error.GetLastError(), "GetUrlCacheEntrInfo");
		}
		else
		{
			// Got the entry info. dump out some details for demonstration purpose.
			Console.Write("The returned cache entry contains the following detail:\n");
			Console.Write("The source URL is: {0}\n", lpCacheEntryInfo.AsRef().lpszSourceUrlName);
			Console.Write("The local file name is: {0}\n", lpCacheEntryInfo.AsRef().lpszLocalFileName);
		}
	}

	/*++
	Routine Description:
	This routine is used to log WinInet errors in human readable form.
	Arguments:
	err - Error number obtained from GetLastError()
	str - String pointer holding caller-context information 
	Return Value:
	None.
	--*/
	static void LogInetError(Win32Error err, string str)
	{
		StringBuilder msgBuffer = new(512);
		var dwResult = FormatMessage(FormatMessageFlags.FORMAT_MESSAGE_FROM_HMODULE, GetModuleHandle("wininet.dll"),
			(uint)err, LANGID.LANG_USER_DEFAULT, msgBuffer, (uint)msgBuffer.Capacity, default);
		if (dwResult != 0)
			Console.Error.Write("{0}: {1}\n", str, msgBuffer);
		else
			Console.Error.Write("Error {0} while formatting message for {1} in {2}\n", GetLastError(), err, str);
	}

	/*++
	Routine Description:
	This routine is used to Parse command line arguments. Flags are
	case sensitive.
	Arguments:
	args - Pointer to the argument vector
	Return Value:
	None.
	--*/
	private static void ParseArguments(string[] args)
	{
		var bError = true;

		// If there are either no option or more than two options, print the usage block and exit.
		if (args.Length is 0 or >2)
		{
			PrintUsageBlock();
			Environment.Exit(1);
		}

		for (var i = 0; i < args.Length; ++i)
		{
			// Make sure all option starts with '-'. Otherwise, print the usage block and exit.
			if (args[i][0] != '-')
			{
				Console.Write("Invalid switch {0}\n", args[i]);
				goto done;
			}

			switch (args[i][1])
			{
				// Enumerate entries in all the fix containers.
				case 'a':
					// This must be the 1st and only option.
					if (i > 1)
					{
						goto done;
					}

					g_dwAction |= CACHESAMPLE_ACTION_ENUM_ALL;
					break;

				// Enumerate entries in the content container.
				case 'c':
					// This must be the 1st option.
					if (i > 1)
					{
						goto done;
					}

					g_dwAction |= CACHESAMPLE_ACTION_ENUM_CONTENT;
					break;

				// Delete option for found entries.
				case 'd':
					// Don't allow -d option as the first option. If it is the first option, print usage block and exit.
					if (i == 1)
					{
						goto done;
					}

					g_dwAction |= CACHESAMPLE_ACTION_DELETE;
					break;

				// Get entry detail on a particular cached URL.
				case 'e':
					// This option must be the first option. And it must have a URL param ONLY. Otherwise, print usage and exit.
					if (i != 1 || args.Length != 2)
					{
						goto done;
					}

					// Grab the URL param.
					g_lpszURL = args[++i];

					// Check again to be conservative.
					if (string.IsNullOrEmpty(g_lpszURL))
					{
						Console.Write("You must specify an URL for detail search!.");
						goto done;
					}

					g_dwAction |= CACHESAMPLE_ACTION_DETAIL;
					break;

				// Enumerate history entries only.
				case 'h':
					// This must be the 1st option.
					if (i > 1)
					{
						goto done;
					}

					g_dwAction |= CACHESAMPLE_ACTION_ENUM_HISTORY;
					break;

				// Enumerate entries in the cookie container.
				case 'k':
					// This must be the 1st option.
					if (i > 1)
					{
						goto done;
					}

					g_dwAction |= CACHESAMPLE_ACTION_ENUM_COOKIE;
					break;

				// Display usage block.
				case '?':
					// This must be the 1st option.
					if (i > 1)
					{
						goto done;
					}

					PrintUsageBlock();
					Environment.Exit(0);
					break;

				// Unknown option print the usage block.
				default:
					Console.Write("Unknown option: {0}\n\n", args[i]);
					goto done;
			}

			bError = false;
		}

		done:

		// If we have any errors, print the usage block and exit.
		if (bError)
		{
			PrintUsageBlock();
			Environment.Exit(1);
		}
	}

	/*++
	Routine Description:
	This routine is used to print out the usage and option
	messages.
	Arguments:
	None.
	Return Value:
	None.
	--*/
	private static void PrintUsageBlock()
	{
		Console.Write("Usage: CacheEnumerate [-?] | [[-a | -c | -h | -k] -d] | [-e <URL>] \n\n");
		Console.Write("Flag Semantics: \n");
		Console.Write("-a : Enumerate entries in all fixed containers\n");
		Console.Write("-c : Enumerate entries in the content container\n");
		Console.Write("-d : Delete entries option \n");
		Console.Write("-e : Get details of a entry for a specific URL \n");
		Console.Write("-h : Enumerate entries in the history container\n");
		Console.Write("-k : Enumerate entries in the cookie container\n");
		Console.Write("-? : Display usage info.\n");
		Console.Write("\n");
		Console.Write("E.g.\n");
		Console.Write("\t CacheEnumerate.exe -a - Enumerate all entries in all the fixed containers\n");
		Console.Write("\t CacheEnumerate.exe -e http://www.microsoft.com/ - get detail on an entry associated a URL\n");
		Console.Write("\t CacheEnumerate.exe -h -d - Enumerate all the entries in the ");
		Console.Write("history container and delete each found entry.\n");
		Console.Write("\t CacheEnumerate.exe -a -d - Enumerate all entries in the fixed containers");
		Console.Write("and delete each found entry.\n");
	}
}