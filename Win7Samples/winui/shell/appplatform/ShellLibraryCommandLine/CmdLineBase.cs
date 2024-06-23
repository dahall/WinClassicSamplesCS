#pragma warning disable IDE1006 // Naming Styles

using Vanara.PInvoke;

namespace CmdLine;

public struct ARGENTRY<ARGVALUE>(string arg, ARGVALUE val, string use)
{
	public string szArg = arg;
	public string szUsage = use;
	public ARGVALUE value = val;
}

// Base class for a single command
public abstract class CCmdBase
{
	private readonly CStringMap<COptionHandler> mapOptionHandlers = new();
	private readonly string szName, szDescription, szArgTemplate;
	private bool fError = false;
	private string szPrefixedName;

	public CCmdBase(string szName, string szDescription, string szArgTemplate)
	{
		this.szName = szPrefixedName = szName;
		this.szDescription = szDescription;
		this.szArgTemplate = szArgTemplate;
	}

	// toggles debug output for all classes derived from CCmdBase
	public static bool DebugMode { get; set; } = false;

	public string ArgTemplate => szArgTemplate;

	public string Description => szDescription;

	public string Name => GetName(false);

	public void AddPrefix(string pszPrefix) => szPrefixedName = $"{pszPrefix} {szPrefixedName}";

	// methods for executing the command, and obtaining usage information
	public HRESULT Execute(string[] ppszArgs)
	{
		HRESULT hr;
		string? pszFirstArg = ppszArgs.FirstOrDefault();
		if (pszFirstArg is not null && pszFirstArg[0] is '-' or '/' &&
			pszFirstArg.Substring(1).ToLower() is "help" or "h" or "?")
		{
			hr = HRESULT.S_FALSE;
			PrintUsage();
		}
		else
		{
			hr = ProcessOptions(ref ppszArgs);
			if (hr.Succeeded)
			{
				hr = v_ProcessArguments(ppszArgs);
				if (hr.Succeeded)
				{
					hr = v_ExecuteCommand();
				}
			}
		}
		return hr;
	}

	public string GetName(bool fIncludePrefix = false) => fIncludePrefix ? szPrefixedName : szName;

	public void PrintUsage()
	{
		bool fHaveOptions = !mapOptionHandlers.IsEmpty();
		Output("Usage: {0} {1}{2}\n\n", GetName(true), fHaveOptions ? "[OPTIONS] " : "", ArgTemplate);
		if (!string.IsNullOrEmpty(szDescription))
		{
			Output("{0}\n\n", Description);
		}
		if (fHaveOptions)
		{
			Output("Options:\n");
			mapOptionHandlers.ForEachValue(PrintUsageForOption);
			Output("\n");
		}
		v_PrintInstructions();
	}

	protected void AddEnumOptionHandler<ARGVALUE>(string pszOption, string pszDescription, string pszUsage,
			Func<ARGVALUE, HRESULT> pmfnSetOption, ARGENTRY<ARGVALUE>[] pLookupTable)
	{
		COptionHandler pHandler = new CEnumeratedOptionHandler<ARGVALUE>(pmfnSetOption, pszOption, pszDescription, pszUsage, pLookupTable);
		mapOptionHandlers.Add(pszOption, pHandler);
	}

	// methods to add specify option handlers; call these from the subclass constructor
	protected void AddStringOptionHandler(string pszOption, string pszUsage, Func<string, HRESULT> pmfnSetOption)
	{
		COptionHandler pHandler = new CStringOptionHandler(pmfnSetOption, pszOption, pszUsage);
		mapOptionHandlers.Add(pszOption, pHandler);
	}

	// methods for generating output (use instead of Console.Write)
	protected void Output(string pszFormat, params object?[]? args) => Console.Write(pszFormat, args);

	protected void ParseError(string pszFormat, params object?[]? args)
	{
		if (!fError)
		{
			fError = true;
			Console.Write("ERROR - " + pszFormat, args);
			Console.Write("\n");
			PrintUsage();
		}
	}

	protected void RuntimeError(string pszFormat, params object?[]? args)
	{
		fError = true;
		Console.Write("ERROR - " + pszFormat, args);
	}

	// methods to be overridden by the command implementation
	protected abstract HRESULT v_ExecuteCommand();

	protected virtual void v_PrintInstructions() { }

	protected virtual HRESULT v_ProcessArguments(string[] pppszArgs) => HRESULT.S_OK;

	private void PrintUsageForOption(COptionHandler pOption) => pOption.v_PrintUsage(this);

	private HRESULT ProcessOptions(ref string[] ppszArgs)
	{
		HRESULT hr = HRESULT.E_INVALIDARG;

		if (ppszArgs is not null)
		{
			// process prefixed options using registered handlers
			hr = HRESULT.S_OK;
			int iArg = 0, idx = 0;
			while (hr.Succeeded && iArg < ppszArgs.Length && !string.IsNullOrEmpty(ppszArgs[iArg]) && ppszArgs[iArg][0] is '-' or '/')
			{
				string[] splits = ppszArgs[iArg].Substring(1).Split(':');
				string pszOptionName = splits[0];
				string pszOptionArgs = splits.Length <= 1 ? "" : splits[1];

				hr = mapOptionHandlers.Find(pszOptionName, out var pHandler);
				if (hr.Succeeded)
				{
					hr = pHandler!.v_SetOption(this, pszOptionArgs);
				}
				else
				{
					ParseError($"Unrecognized option: {ppszArgs[iArg]}{(pszOptionArgs.Length > 0 ? ":" : "")}{pszOptionArgs}\n");
				}
				iArg++;
			}

			if (hr.Succeeded)
			{
				// leave remaining arguments for the derived ref class pppszArgs = ppszArgs + iArg;
				ppszArgs = ppszArgs.Skip(iArg).ToArray();
			}
		}

		return hr;
	}

	// option handling framework
	public abstract class COptionHandler
	{
		public virtual void v_PrintUsage(CCmdBase pCmd) { }

		public abstract HRESULT v_SetOption(CCmdBase pCmd, string pszValue);
	}

	public abstract class COptionHandlerBase<ARGVALUE>(Func<ARGVALUE, HRESULT> pmfnSetOption, string pszName, string pszUsage) : COptionHandler
	{
		public Func<ARGVALUE, HRESULT> pmfnSetOption = pmfnSetOption;
		protected string szName = $"{pszName}[:ARG]";
		protected string szUsage = pszUsage;
	}

	public class CEnumeratedOptionHandler<ARGVALUE> : COptionHandlerBase<ARGVALUE>
	{
		private ARGENTRY<ARGVALUE>[] pLookupTable;
		private string szDescription;

		public CEnumeratedOptionHandler(Func<ARGVALUE, HRESULT> pmfnSetOption, string pszName, string pszDescription, string pszUsage,
			ARGENTRY<ARGVALUE>[] pLookupTable) : base(pmfnSetOption, pszName, pszUsage)
		{
			szDescription = pszDescription;
			this.pLookupTable = (ARGENTRY<ARGVALUE>[])pLookupTable.Clone();
		}

		public override void v_PrintUsage(CCmdBase pCmd)
		{
			pCmd.Output(" -{0,-18} {1}\n", szName, szUsage);
			for (uint iEntry = 0; iEntry < pLookupTable.Length; iEntry++)
			{
				string pszArg = pLookupTable[iEntry].szArg;
				pCmd.Output("   {0,-19} {1}\n", !string.IsNullOrEmpty(pszArg) ? pszArg : "<none>", pLookupTable[iEntry].szUsage);
			}
		}

		public override HRESULT v_SetOption(CCmdBase pCmd, string pszValue)
		{
			HRESULT hr = HRESULT.E_INVALIDARG;
			for (uint iEntry = 0; hr.Failed && iEntry < pLookupTable.Length; iEntry++)
			{
				if (string.Equals(pszValue, pLookupTable[iEntry].szArg, StringComparison.InvariantCultureIgnoreCase))
				{
					hr = pmfnSetOption(pLookupTable[iEntry].value);
				}
			}

			if (hr.Failed)
			{
				pCmd.ParseError("Unrecognized {0}: {1}\n", szDescription, pszValue);
			}
			return hr;
		}
	}

	public class CStringOptionHandler : COptionHandlerBase<string>
	{
		public CStringOptionHandler(Func<string, HRESULT> pmfnSetOption, string pszName, string pszUsage) :
			base(pmfnSetOption, pszName, pszUsage) { }

		public override void v_PrintUsage(CCmdBase pCmd) => pCmd.Output(" -{0,-18} {1}\n", szName, szUsage);

		public override HRESULT v_SetOption(CCmdBase pCmd, string pszValue) => pmfnSetOption(pszValue);
	}

	// Simple associative array implementation
	protected class CStringMap<VALUE> where VALUE : class
	{
		private readonly Dictionary<string, VALUE> map = [];

		public HRESULT Add(string pszKey, VALUE ppValue) { map.Add(pszKey.ToLower(), ppValue); return 0; }

		public HRESULT Find(string pszKey, out VALUE? ppValue) => map.TryGetValue(pszKey.ToLower(), out ppValue) ? HRESULT.S_OK : HRESULT.E_FAIL;

		public void ForEachValue(Action<VALUE> pmfnDo)
		{
			foreach (var pValue in map.Values)
			{
				pmfnDo(pValue);
			}
		}

		public bool IsEmpty() => map.Count == 0;
	}
}

// command implementation that server as a wrapper for a set of sub-commands, ala netsh.exe
public class CMetaCommand : CCmdBase
{
	private CStringMap<CCmdBase> mapCmds = new();

	private string[]? _ppszArgs = null;

	private CCmdBase? pSpecifiedCmd;

	// weak reference
	public CMetaCommand(string pszName, string pszDescription, PFNCREATECOMMAND[] pCmds) : base(pszName, pszDescription, "SUBCOMMAND")
	{
		pCmds.All(p => AddCommand(p).Succeeded);
	}

	public delegate HRESULT PFNCREATECOMMAND(out CCmdBase ppCmd, string pszPrefix);

	public static HRESULT Create<CCmdType>(out CCmdBase ppCmd, string pszPrefix) where CCmdType : CCmdBase, new()
	{
		ppCmd = new CCmdType();
		ppCmd.AddPrefix(pszPrefix);
		return HRESULT.S_OK;
	}

	protected HRESULT AddCommand(PFNCREATECOMMAND pfnCreate)
	{
		HRESULT hr = pfnCreate(out var pCmd, GetName(true));
		if (hr.Succeeded)
		{
			hr = mapCmds.Add(pCmd.GetName(), pCmd);
		}
		return hr;
	}

	protected override HRESULT v_ExecuteCommand() => pSpecifiedCmd?.Execute(_ppszArgs ?? []) ?? HRESULT.E_UNEXPECTED;

	protected override void v_PrintInstructions()
	{
		Output("Supported commands:\n");
		mapCmds.ForEachValue(PrintDescriptionForCommand);
	}

	protected override HRESULT v_ProcessArguments(string[] ppszArgs)
	{
		HRESULT hr = HRESULT.E_UNEXPECTED;

		if (pSpecifiedCmd is null)
		{
			hr = HRESULT.E_INVALIDARG;
			if (ppszArgs.Length >= 1)
			{
				// look up the command name
				hr = mapCmds.Find(ppszArgs[0], out pSpecifiedCmd);
				if (hr.Succeeded)
				{
					// save the remaining arguments for the specified command this assumes that the lifetime of ppszArgs and cArgs is tied to
					// the calling function, CCmdBase::Execute
					_ppszArgs = ppszArgs.Skip(1).ToArray();
				}
				else
				{
					ParseError("Command not recognized: {0}\n", ppszArgs[0]);
				}
			}
			else
			{
				ParseError("No command specified.\n");
			}
		}

		return hr;
	}

	private void PrintDescriptionForCommand(CCmdBase pCmd) => Output(" {0,-12}{1}\n", pCmd.GetName(), pCmd.Description);
}