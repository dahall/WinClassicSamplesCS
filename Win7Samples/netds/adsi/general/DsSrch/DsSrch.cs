using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ActiveDS;

internal class Program
{
	//
	// Globals representing the parameters
	//
	static string? pszSearchBase, pszSearchFilter, pszUserName = default, pszPassword = default;
	static readonly List<string> pszAttrNames = [];
	static readonly List<ADS_SEARCHPREF_INFO> pSearchPref = [];
	static int dwMaxRows = int.MaxValue;
	static ADS_AUTHENTICATION dwAuthFlags = 0;

	//+---------------------------------------------------------------------------
	//
	// Function: main
	//
	// Synopsis:
	//
	//----------------------------------------------------------------------------
	internal static int Main(string[] args)
	{
#if EXT
		// Enable if you want to test binary values in filters and send
		// pszBinaryFilter instead of pszSearchFilter in ExecuteSearch

		string pszBinaryFilter = "objectSid=";

		byte[] column = [ 0x01, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x15, 0x00,
			0x00, 0x00, 0x59, 0x51, 0xb8, 0x17, 0x66, 0x72, 0x5d, 0x25,
			0x64, 0x63, 0x3b, 0x0b, 0x29, 0x99, 0x21, 0x00 ];

		ADsEncodeBinaryData(column, out var pszDest).ThrowIfFailed();

		pszBinaryFilter += pszDest;
#endif

		//
		// Sets the global variables with the parameters
		//
		if (ProcessArgs(args).Failed)
		{
			Util.PrintUsage();
			return HRESULT.E_FAIL;
		}

		IDirectorySearch? pDSSearch;
		if (dwAuthFlags != 0)
			ADsOpenObject(pszSearchBase!, out pDSSearch, dwAuthFlags, pszUserName, pszPassword).ThrowIfFailed();
		else
			ADsGetObject(pszSearchBase!, out pDSSearch).ThrowIfFailed();

		uint cErr = 0;
		if (pSearchPref.Count > 0 && pDSSearch!.SetSearchPreference(pSearchPref.ToArray()) != HRESULT.S_OK)
		{
			foreach (var p in pSearchPref.Where(sp => sp.dwStatus != ADS_STATUS.ADS_STATUS_S_OK))
			{
				Console.WriteLine($"Error in setting the preference {p.dwSearchPref}: status = {p.dwStatus}");
				cErr++;
			}
		}

		SafeADS_SEARCH_HANDLE hSearchHandle = pDSSearch!.ExecuteSearch(pszSearchFilter!, pszAttrNames.ToArray());

		uint nRows = 0;
		foreach (var row in pDSSearch!.GetRowData(hSearchHandle).Take(dwMaxRows))
		{
			nRows++;
			foreach (var col in row)
				Util.PrintColumn(col!, col.pszAttrName);
		}

		Console.WriteLine($"Total Rows: {nRows}");

		if (cErr != 0)
			Console.WriteLine($"{cErr} warning(s) ignored");

		return 0;
	}

	//+---------------------------------------------------------------------------
	//
	// Function: ProcessArgs
	//
	// Synopsis:
	//
	//----------------------------------------------------------------------------
	static HRESULT ProcessArgs(string[] args)
	{
		bool fVal;
		uint dwVal;
		int currArg = 0;

		while (currArg < args.Length)
		{
			if (args.Length - currArg < 2 || args[currArg][0] is not '/' and not '-')
				return HRESULT.E_FAIL;
			switch (args[currArg][1])
			{
				case 'b':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;
					pszSearchBase = args[currArg];
					if (pszSearchBase is null) return HRESULT.E_FAIL;
					break;

				case 'f':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;
					pszSearchFilter = args[currArg];
					if (pszSearchFilter is null) return HRESULT.E_FAIL;
					break;

				case 'a':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;
					var pszAttrList = args[currArg];
					if (pszAttrList is null) return HRESULT.E_FAIL;

					pszAttrNames.AddRange(pszAttrList.Split(','));
					for (int i = pszAttrNames.Count - 1; i >= 0; i--)
					{
						pszAttrNames[i] = RemoveWhiteSpaces(pszAttrNames[i]) ?? "";
						if (pszAttrNames[i].Length == 0)
							pszAttrNames.RemoveAt(i);
					}
					break;

				case 'u':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;
					pszUserName = args[currArg];
					if (pszUserName is null) return HRESULT.E_FAIL;
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;
					pszPassword = args[currArg];
					if (pszPassword is null) return HRESULT.E_FAIL;
					break;

				case 't':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;

					var pszCurrPref = args[currArg].Split('=');
					if (pszCurrPref.Length != 2 || string.IsNullOrEmpty(pszCurrPref[0] = pszCurrPref[0].Trim()) || string.IsNullOrEmpty(pszCurrPref[1] = pszCurrPref[1].Trim()))
						return HRESULT.E_FAIL;

					if (string.Equals(pszCurrPref[0], "SecureAuth", StringComparison.InvariantCultureIgnoreCase))
					{
						if (!TryParseBool(pszCurrPref[1], out var b)) return HRESULT.E_FAIL;
						dwAuthFlags = dwAuthFlags.SetFlags(ADS_AUTHENTICATION.ADS_SECURE_AUTHENTICATION, b);
					}
					else if (string.Equals(pszCurrPref[0], "UseEncrypt", StringComparison.InvariantCultureIgnoreCase))
					{
						if (!TryParseBool(pszCurrPref[1], out var b)) return HRESULT.E_FAIL;
						dwAuthFlags = dwAuthFlags.SetFlags(ADS_AUTHENTICATION.ADS_USE_ENCRYPTION, b);
					}
					else
						return HRESULT.E_FAIL;
					break;

				case 'p':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;

					pszCurrPref = args[currArg].Split('=');
					if (pszCurrPref.Length != 2 || string.IsNullOrEmpty(pszCurrPref[0] = pszCurrPref[0].Trim()) || string.IsNullOrEmpty(pszCurrPref[1] = pszCurrPref[1].Trim()))
						return HRESULT.E_FAIL;

					switch (pszCurrPref[0].ToLowerInvariant())
					{
						case "asynchronous":
							if (!TryParseBool(pszCurrPref[1], out fVal)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_ASYNCHRONOUS, fVal));
							break;
						case "attrtypesonly":
							if (!TryParseBool(pszCurrPref[1], out fVal)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_ATTRIBTYPES_ONLY, fVal));
							break;
						case "derefaliases":
							if (!TryParseBool(pszCurrPref[1], out fVal)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_DEREF_ALIASES, fVal));
							break;
						case "timeout":
							dwVal = uint.Parse(pszCurrPref[1]);
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_TIMEOUT, dwVal));
							break;
						case "timelimit":
							dwVal = uint.Parse(pszCurrPref[1]);
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_TIME_LIMIT, dwVal));
							break;
						case "sizelimit":
							dwVal = uint.Parse(pszCurrPref[1]);
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_SIZE_LIMIT, dwVal));
							break;
						case "pagesize":
							dwVal = uint.Parse(pszCurrPref[1]);
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_PAGESIZE, dwVal));
							break;
						case "pagedtimelimit":
							dwVal = uint.Parse(pszCurrPref[1]);
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_PAGED_TIME_LIMIT, dwVal));
							break;
						case "searchscope":
							ADS_SCOPE dwSearchScope;
							if (!TryParseEnum(pszCurrPref[1], out dwSearchScope)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_SEARCH_SCOPE, dwSearchScope));
							break;
						case "chasereferrals":
							ADS_CHASE_REFERRALS dwChaseReferrals;
							if (!TryParseEnum(pszCurrPref[1], out dwChaseReferrals)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_CHASE_REFERRALS, dwChaseReferrals));
							break;
						case "sorton":
							var sk = RemoveWhiteSpaces(pszCurrPref[1]);
							if (string.IsNullOrEmpty(sk)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_SORT_ON, new ADS_SORTKEY(sk!)));
							break;
						case "cacheresults":
							if (!TryParseBool(pszCurrPref[1], out fVal)) return HRESULT.E_FAIL;
							pSearchPref.Add(new(ADS_SEARCHPREF.ADS_SEARCHPREF_CACHE_RESULTS, fVal));
							break;
						default:
							return HRESULT.E_FAIL;
					}
					break;

				case 'n':
					if (++currArg >= args.Length)
						return HRESULT.E_FAIL;
					dwMaxRows = int.Parse(args[currArg]);
					break;

				default:
					return HRESULT.E_FAIL;
			}

			currArg++;
		}

		//
		// Check for Mandatory arguments;
		//
		if (pszSearchBase is null || pszSearchFilter is null)
			return HRESULT.E_FAIL;

		if (pszAttrNames.Count == 0)
		{
			//
			// Get all the attributes
			//
			pszAttrNames.Add("*");
		}

		return HRESULT.S_OK;

		static bool TryParseBool(string s, out bool b)
		{
			if (bool.TryParse(s, out b))
				return true;
			if (string.Equals(s, "yes", StringComparison.InvariantCultureIgnoreCase))
			{
				b = true;
				return true;
			}
			if (string.Equals(s, "no", StringComparison.InvariantCultureIgnoreCase))
			{
				b = false;
				return true;
			}
			return false;
		}

		static bool TryParseEnum<T>(string s, out T t) where T : struct, Enum => Enum.TryParse(s, true, out t) || Enum.TryParse($"{typeof(T).Name}_{s}", true, out t);
	}

	static string? RemoveWhiteSpaces(string? pszText) => pszText?.Trim();
}