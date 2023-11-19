using std;
using Vanara.PInvoke;
using static Vanara.PInvoke.SearchApi;

namespace CrawlScopeCommandLine;

internal static class Program
{
	// Global objects used for command line parsing.
	private static readonly CSetValueParam g_AddRootParam = new("add_root");
	private static readonly CSetValueParam g_AddRuleParam = new("add_rule");
	private static readonly CFlagParam g_AltHelpParam = new("?");
	private static readonly CExclFlagParam g_DefaultParam = new("default", "user");
	private static readonly CFlagParam g_EnumRootsParam = new("enumerate_roots");
	private static readonly CFlagParam g_EnumRulesParam = new("enumerate_rules");
	private static readonly CFlagParam g_HelpParam = new("help");
	private static readonly CExclFlagParam g_IncludeParam = new("include", "exclude");
	private static readonly CFlagParam g_OverrideParam = new("override_children");
	private static readonly CFlagParam g_Reindex = new("reindex");
	private static readonly CSetValueParam g_RemoveRootParam = new("remove_root");
	private static readonly CSetValueParam g_RemoveRuleParam = new("remove_rule");
	private static readonly CFlagParam g_Reset = new("reset");
	private static readonly CFlagParam g_RevertParam = new("revert");

	// List of alternative options corresponding to CSM operations.
	private static readonly CParamBase[] ExclusiveParams =
	{
		g_EnumRootsParam,
		g_EnumRulesParam,
		g_AddRootParam,
		g_RemoveRootParam,
		g_AddRuleParam,
		g_RemoveRuleParam,
		g_RevertParam,
		g_Reset,
		g_Reindex,
		g_HelpParam,
		g_AltHelpParam,
	};

	// List of all supported command line options.
	private static readonly CParamBase[] Params =
	{
		g_IncludeParam,
		g_DefaultParam,
		g_EnumRootsParam,
		g_EnumRulesParam,
		g_AddRootParam,
		g_RemoveRootParam,
		g_AddRuleParam,
		g_RemoveRuleParam,
		g_RevertParam,
		g_Reset,
		g_Reindex,
		g_HelpParam,
		g_AltHelpParam
	};

	// Command line options help text
	private static readonly string[] rgParamsHelp =
	{
		"/enumerate_roots",
		"/enumerate_rules",
		"/add_root <new root path>",
		"/remove_root <root path to remove>",
		"/add_rule <rule URL> /[DEFAULT|USER] /[INCLUDE|EXCLUDE]",
		"/remove_rule <rule URL> /[DEFAULT|USER]",
		"/revert",
		"/reset",
		"/reindex",
		"/help or /? "
	};

	public static int Main(string[] args)
	{
		// Parsing command line
		Win32Error iRes = CParamBase.ParseParams(Params, args);
		if (Win32Error.ERROR_SUCCESS == iRes)
		{
			// Check that only one CSM operation parameter was referred
			iRes = CheckExcusiveParameters();
			if (Win32Error.ERROR_SUCCESS == iRes)
			{
				// Default catalog name will be used if /CATALOG option doesn't specify otherwise
				if (g_HelpParam.Exists || g_AltHelpParam.Exists)
				{
					iRes = PrintHelp();
				}
				else if (g_EnumRootsParam.Exists)
				{
					iRes = EnumRoots();
				}
				else if (g_EnumRulesParam.Exists)
				{
					iRes = EnumRules();
				}
				else if (g_AddRootParam.Exists && g_AddRootParam.Get() is not null)
				{
					iRes = AddRoots(g_AddRootParam.Get()!);
				}
				else if (g_RemoveRootParam.Exists && g_RemoveRootParam.Get() is not null)
				{
					iRes = RemoveRoots(g_RemoveRootParam.Get()!);
				}
				else if (g_AddRuleParam.Exists && g_AddRuleParam.Get() is not null)
				{
					iRes = AddRule(g_DefaultParam.Exists && g_DefaultParam.Get(),
						g_IncludeParam.Exists && g_IncludeParam.Get(),
						g_OverrideParam.Exists,
						g_AddRuleParam.Get()!);
				}
				else if (g_RemoveRuleParam.Exists && g_RemoveRuleParam.Get() is not null)
				{
					iRes = RemoveRule(g_DefaultParam.Exists && g_DefaultParam.Get(),
						g_RemoveRuleParam.Get()!);
				}
				else if (g_RevertParam.Exists)
				{
					iRes = Revert();
				}
				else if (g_Reset.Exists)
				{
					iRes = Reset();
				}
				else if (g_Reindex.Exists)
				{
					iRes = Reindex();
				}
				else
				{
					System.Diagnostics.Debug.Write("Required parameter is missing!");
				}
			}
			else
			{
				iRes = PrintHelp();
			}
		}
		return 0;
	}

	private static Win32Error AddRoots(string pszURL)
	{
		Console.Write($"Adding new root {pszURL}\n");

		try
		{
			// Crawl scope manager for that catalog
			CreateCatalogManager(out ISearchCatalogManager? pCatalogManager).ThrowIfFailed();
			// Crawl scope manager for that catalog
			ISearchCrawlScopeManager pSearchCrawlScopeManager = pCatalogManager!.GetCrawlScopeManager();
			var pISearchRoot = new ISearchRoot();
			pISearchRoot.RootURL = pszURL;
			pSearchCrawlScopeManager.AddRoot(pISearchRoot);
			pSearchCrawlScopeManager.SaveAll();
			pCatalogManager.ReindexSearchRoot(pszURL);
			Console.Write($"Reindexing was started for root {pszURL}\n");
			return Win32Error.ERROR_SUCCESS;
		}
		catch (Exception ex)
		{
			return ReportHRESULTError("AddRoots()", ex.HResult);
		}
	}

	private static Win32Error AddRule(bool fDefault, bool fInclude, bool fOverride, string pszURL)
	{
		Console.Write("Adding new " + (fDefault ? "default " : "user ") + (fInclude ? "inclusion " : "exclusion ") + "rule " + pszURL + ((!fDefault && fOverride) ? "overriding cildren rules" : "") + "\n");

		// Crawl scope manager for that catalog
		HRESULT hr = CreateCrawlScopeManager(out ISearchCrawlScopeManager? pSearchCrawlScopeManager);
		if (hr.Succeeded)
		{
			try
			{
				if (fDefault)
				{
					pSearchCrawlScopeManager!.AddDefaultScopeRule(pszURL, fInclude, FOLLOW_FLAGS.FF_INDEXCOMPLEXURLS);
				}
				else
				{
					pSearchCrawlScopeManager!.AddUserScopeRule(pszURL, fInclude, fOverride, FOLLOW_FLAGS.FF_INDEXCOMPLEXURLS);
				}
				pSearchCrawlScopeManager.SaveAll();
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
		}
		return ReportHRESULTError("AddRule()", hr);
	}

	// CheckExcusiveParameters function validates that only one option from ExclusiveParams list is present in argument list
	private static Win32Error CheckExcusiveParameters()
	{
		var iRes = Win32Error.ERROR_SUCCESS;
		uint dwCount = 0;
		for (var i = 0; i < ExclusiveParams.Length; i++)
		{
			if (ExclusiveParams[i].Exists)
			{
				dwCount++;
			}
		}

		if (0 == dwCount)
		{
			Console.Write("Error: CSM operation parameter is expected!" + "\n");
			iRes = Win32Error.ERROR_INVALID_PARAMETER;
		}
		else if (1 < dwCount)
		{
			Console.Write("Error: Duplicated CSM operation parameters!" + "\n");
			iRes = Win32Error.ERROR_INVALID_PARAMETER;
		}

		return iRes;
	}

	private static HRESULT CreateCatalogManager(out ISearchCatalogManager? ppSearchCatalogManager)
	{
		try
		{
			var pSearchManager = new ISearchManager();
			ppSearchCatalogManager = pSearchManager.GetCatalog("SystemIndex");
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			ppSearchCatalogManager = null;
			return ex.HResult;
		}
	}

	private static HRESULT CreateCrawlScopeManager(out ISearchCrawlScopeManager? ppSearchCrawlScopeManager)
	{
		ppSearchCrawlScopeManager = default;

		HRESULT hr = CreateCatalogManager(out ISearchCatalogManager? pCatalogManager);
		if (hr.Succeeded)
		{
			try
			{
				// Crawl scope manager for that catalog
				ppSearchCrawlScopeManager = pCatalogManager!.GetCrawlScopeManager();
			}
			catch (Exception ex)
			{
				return ex.HResult;
			}
		}
		return hr;
	}

	private static HRESULT DisplayRootInfo(ISearchRoot pSearchRoot)
	{
		try
		{
			Console.WriteLine("\t" + pSearchRoot.RootURL);
			Console.WriteLine($"\t\tAuthenticationType={pSearchRoot.AuthenticationType}");
			Console.WriteLine($"\t\tEnumerationDepth={pSearchRoot.EnumerationDepth}");
			Console.WriteLine($"\t\tFollowDirectories={pSearchRoot.FollowDirectories}");
			Console.WriteLine($"\t\tHostDepth={pSearchRoot.HostDepth}");
			Console.WriteLine($"\t\tIsHierarchical={pSearchRoot.IsHierarchical}");
			Console.WriteLine($"\t\tProvidesNotifications={pSearchRoot.ProvidesNotifications}");
			Console.WriteLine($"\t\tUseNotificationsOnly={pSearchRoot.UseNotificationsOnly}");
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
	}

	private static HRESULT DisplayRule(ISearchScopeRule pSearchScopeRule)
	{
		try
		{
			Console.Write("\t" + pSearchScopeRule.PatternOrURL);
			Console.Write((pSearchScopeRule.IsDefault ? " " : " NOT ") + "DEFAULT");
			Console.Write((pSearchScopeRule.IsIncluded ? " INCLUDED" : " EXCLUDED "));
			Console.WriteLine();
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
	}

	private static Win32Error EnumRoots()
	{
		// Crawl scope manager for that catalog
		HRESULT hr = CreateCrawlScopeManager(out ISearchCrawlScopeManager? pSearchCrawlScopeManager);
		if (hr.Succeeded)
		{
			// Search roots on that crawl scope
			hr = pSearchCrawlScopeManager!.EnumerateRoots(out IEnumSearchRoots pSearchRoots);
			if (hr.Succeeded)
			{
				var pSearchRoot = new ISearchRoot[1];
				while (hr.Succeeded && HRESULT.S_OK == (hr = pSearchRoots.Next(1, pSearchRoot, out var fetched)) && fetched == 1)
				{
					hr = DisplayRootInfo(pSearchRoot[0]);
				}
			}
		}

		return ReportHRESULTError("EnumRoots()", hr);
	}

	private static Win32Error EnumRules()
	{
		// Crawl scope manager for that catalog
		HRESULT hr = CreateCrawlScopeManager(out ISearchCrawlScopeManager? pSearchCrawlScopeManager);
		if (hr.Succeeded)
		{
			// Search roots on that crawl scope
			hr = pSearchCrawlScopeManager!.EnumerateScopeRules(out IEnumSearchScopeRules pScopeRules);
			if (hr.Succeeded)
			{
				var pSearchScopeRule = new ISearchScopeRule[1];
				uint fetched = 0;
				while (hr.Succeeded && HRESULT.S_OK == (hr = pScopeRules.Next(1, pSearchScopeRule, ref fetched)) && fetched == 1)
				{
					hr = DisplayRule(pSearchScopeRule[0]);
				}
			}
		}

		return ReportHRESULTError("EnumRules()", hr);
	}

	private static Win32Error PrintHelp()
	{
		Console.Write("NOTE: you must run this tool as an admin to perform functions" + "\n");
		Console.Write(" that change the state of the index" + "\n");

		Console.Write("List of availible options:" + "\n");

		foreach (var str in rgParamsHelp)
		{
			Console.Write($"\t{str}\n");
		}
		return Win32Error.ERROR_SUCCESS;
	}

	private static Win32Error Reindex()
	{
		Console.Write("Reindexing catalog." + "\n");

		HRESULT hr = CreateCatalogManager(out ISearchCatalogManager? pCatalogManager);
		if (hr.Succeeded)
		{
			try { pCatalogManager!.Reindex(); }
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
		}
		return ReportHRESULTError("Reindex()", hr);
	}

	private static Win32Error RemoveRoots(string pszURL)
	{
		Console.Write("Removing root " + pszURL + "\n");

		try
		{
			// Crawl scope manager for that catalog
			CreateCrawlScopeManager(out ISearchCrawlScopeManager? pSearchCrawlScopeManager).ThrowIfFailed();
			pSearchCrawlScopeManager!.RemoveRoot(pszURL);
			pSearchCrawlScopeManager.SaveAll();
			return Win32Error.ERROR_SUCCESS;
		}
		catch (Exception ex)
		{
			return ReportHRESULTError("RemoveRoots()", ex.HResult);
		}
	}

	private static Win32Error RemoveRule(bool fDefault, string pszURL)
	{
		Console.Write("Removing " + (fDefault ? "default" : "user") + " rule " + pszURL + "\n");

		// Crawl scope manager for that catalog
		HRESULT hr = CreateCrawlScopeManager(out ISearchCrawlScopeManager? pSearchCrawlScopeManager);
		if (hr.Succeeded)
		{
			try
			{
				if (fDefault)
				{
					pSearchCrawlScopeManager!.RemoveDefaultScopeRule(pszURL);
				}
				else
				{
					pSearchCrawlScopeManager!.RemoveScopeRule(pszURL);
				}
				pSearchCrawlScopeManager.SaveAll();
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
		}
		return ReportHRESULTError("RemoveRule()", hr);
	}

	private static Win32Error ReportHRESULTError(string pszOpName, HRESULT hr)
	{
		uint iErr = 0;

		if (hr.Failed)
		{
			if (hr.Facility == HRESULT.FacilityCode.FACILITY_WIN32)
			{
				iErr = unchecked((uint)hr.Code);
				var pMsgBuf = Kernel32.FormatMessage(iErr);
				Console.Write($"\nError: {pszOpName} failed with error {iErr}: {pMsgBuf}\n");
			}
			else
			{
				Console.Write($"\nError: {pszOpName} failed with error 0x{(uint)hr:X}\n");
				iErr = uint.MaxValue;
			}
		}

		return iErr;
	}

	private static Win32Error Reset()
	{
		Console.Write("Resetting catalog." + "\n");

		HRESULT hr = CreateCatalogManager(out ISearchCatalogManager? pCatalogManager);
		if (hr.Succeeded)
		{
			try { pCatalogManager!.Reset(); }
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
		}
		return ReportHRESULTError("Reset()", hr);
	}

	private static Win32Error Revert()
	{
		Console.Write("Reverting catalog to its default state." + "\n");

		// Crawl scope manager for that catalog
		HRESULT hr = CreateCrawlScopeManager(out ISearchCrawlScopeManager? pSearchCrawlScopeManager);
		if (hr.Succeeded)
		{
			try
			{
				pSearchCrawlScopeManager!.RevertToDefaultScopes();
				pSearchCrawlScopeManager.SaveAll();
			}
			catch (Exception ex)
			{
				hr = ex.HResult;
			}
		}

		return ReportHRESULTError("Revert()", hr);
	}
}