using System.Diagnostics.CodeAnalysis;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Shell32;

namespace KnownFolders;

internal class Program
{
	private const int IDI_KFSAMPLE_ICON              = 129; // "handler_24bit.ico"
	private const int IDS_KFSAMPLE_TOOLTIP           = 130; // "KnownFolders offer handy tooltips!"
	private const int IDS_KFSAMPLE_LOCALIZEDNAME = 131; // "This is my Sample KnownFolder!"

	private const string SZ_REG_PATH_HISTORY = "Software\\Microsoft\\KnownFolderSample";

	// command line args
	private const string SZ_CLA_REGISTER = "/register";
	private const string SZ_CLA_ENUM = "/enum";
	private const string SZ_CLA_UNREGISTER = "/unregister";
	private const string SZ_CLA_CATEGORY = "/category:";
	private const string SZ_CLA_DEFFLAG = "/defFlag:";
	private const string SZ_CLA_ID = "/id:";
	private const string SZ_CLA_PSZNAME = "/pszName:";
	private const string SZ_CLA_PSZCREATOR = "/pszCreator:";
	private const string SZ_CLA_PSZDESCRIPTION = "/pszDescription:";
	private const string SZ_CLA_PSZRELPATH = "/pszRelativePath:";
	private const string SZ_CLA_PSZPARSENAME = "/pszParsingName:";
	private const string SZ_CLA_PSZLOCALIZEDNAME = "/pszLocalizedName:";
	private const string SZ_CLA_PSZICON = "/pszIcon:";
	private const string SZ_CLA_PSZTOOLTIP = "/pszTooltip:";
	private const string SZ_CLA_PSZSECURITY = "/pszSecurity:";
	private const string SZ_CLA_FINDFORPATH = "/pszFindForPath:";
	private const string SZ_CLA_CLEAN = "/clean";
	private const string SZ_CLA_SHOW_USAGE = "/?";

	public enum ACTION_TYPE
	{
		ACT_UNDEFINED,
		ACT_REGISTER,
		ACT_ENUM,
		ACT_UNREGISTER,
		ACT_CLEAN,
		ACT_SHOW_USAGE,
		ACT_FIND_FOR_PATH
	}

	private static void RegisterKnownFolder(in Guid kfid, in KNOWNFOLDER_DEFINITION pkfd)
	{
		using ComReleaser<IKnownFolderManager> pkfm = new(new());
		try
		{
			pkfm.Item.RegisterFolder(kfid, pkfd);
			// to make it easy to clean up everything this sample does, we'll track each added kfid
			AddRegisteredFolderToHistory(kfid);
		}
		catch (Exception ex)
		{
			Console.Write($"IKnownFolder::RegisterFolder() failed with hr = 0x{ex.HResult:X}\nMake sure this tool is run as an administrator as that is necessary to regiser a known folder");
		}
	}

	private static void EnumAndDumpKnownFolders(out uint pdwKFCount, string? pszNameSrchStr, in Guid kfidSearch)
	{
		pdwKFCount = 0;
		using ComReleaser<IKnownFolderManager> pkfm = new(new());
		foreach (var rgKFID in pkfm.Item.GetFolderIds())
		{
			// if we are searching for a specific Guid, make sure we match before going
			// any further. Guid.Empty means "show all."
			if (kfidSearch == Guid.Empty || kfidSearch == rgKFID)
			{
				var szKFIDGuid = rgKFID.ToString();
				var pkfCurrent = pkfm.Item.GetFolder(rgKFID);
				Guid kfid = pkfCurrent.GetId();

				KNOWNFOLDER_DEFINITION kfd = pkfCurrent.GetFolderDefinition();
				bool fDumpThisFolder = pszNameSrchStr is null || kfd.pszName.ToString().Contains(pszNameSrchStr);
				if (fDumpThisFolder)
				{
					++pdwKFCount;
					DumpKnownFolderDef(kfid, kfd);
					DumpKnownFolderInfo(pkfCurrent);
				}
			}
		}
	}

	private static void UnregisterFolder(in Guid kfid)
	{
		using ComReleaser<IKnownFolderManager> pkfm = new(new());
		pkfm.Item.UnregisterFolder(kfid);
	}

	private static void GetKnownFolderForPath(string pszPath, out Guid pkfid, out KNOWNFOLDER_DEFINITION pkfd)
	{
		using ComReleaser<IKnownFolderManager> pkfm = new(new());
		using ComReleaser<IKnownFolder> pkf = new(pkfm.Item.FindFolderFromPath(pszPath, FFFP_MODE.FFFP_EXACTMATCH));
		pkfid = pkf.Item.GetId();
		pkfd = pkf.Item.GetFolderDefinition();
	}

	[STAThread]
	private static void Main(string[] ppszArgs)
	{
		if (ParseAndValidateCommandLine(ppszArgs, out var at, out var kfid, out var kfd, out var pszFindForPath))
		{
			switch (at)
			{
				case ACTION_TYPE.ACT_REGISTER:
					{
						if (Guid.Empty == kfid)
						{
							kfid = Guid.NewGuid();
						}

						CompleteKnownFolderDef(ref kfd);
						RegisterKnownFolder(kfid, kfd);
						// we create our knownfolder with SHGetKnownFolderPath() so that the shell will write
						// the desktop.ini file in the folder. This is how our customizations
						// (i.e.: pszIcon, pszTooltip and pszLocalizedName) get picked up by explorer.
						try
						{
							SHGetKnownFolderPath(kfid, KNOWN_FOLDER_FLAG.KF_FLAG_CREATE | KNOWN_FOLDER_FLAG.KF_FLAG_INIT, default, out var pszPath).ThrowIfFailed();
							DumpKnownFolderDef(kfid, kfd);
						}
						catch (Exception ex)
						{
							Console.Write("SHGetKnownFolderPath(KF_FLAG_CREATE | KF_FLAG_INIT) returned hr=0x{0:x}\nThe KnownFolder was not registered.\n", ex.HResult);
							UnregisterFolder(kfid);
							Console.Write("The KnownFolder was not registered.\n");
						}
					}
					break;

				case ACTION_TYPE.ACT_ENUM:
					Console.Write("Enumerating all registered KnownFolders \n");
					if (kfd.pszName is not null)
					{
						Console.Write(" matching pszName '{0}'...\n", kfd.pszName);
					}
					else if (Guid.Empty != kfid)
					{
						Console.Write(" matching Guid {0}...\n", kfid);
					}

					EnumAndDumpKnownFolders(out var dwCKF, kfd.pszName, kfid);
					Console.Write("Finished enumerating {0} registered KnownFolders enumerated.\n", dwCKF);
					break;

				case ACTION_TYPE.ACT_UNREGISTER:
					if (Guid.Empty == kfid)
					{
						DumpUsage();
					}
					else
					{
						try { UnregisterFolder(kfid); }
						catch (Exception ex) { Console.Write("IKnownFolderManager::UnregisterFolder returned hr=0x{0:x}\n", ex.HResult); }
					}
					break;

				case ACTION_TYPE.ACT_CLEAN:
					Console.Write("Unregistering all KnownFolders registered by this tool\n");
					UnregisterAllKFsAddedByThisTool(out var dw);
					Console.Write("Unregistered {0} KnownFolders\n", dw);
					break;

				case ACTION_TYPE.ACT_FIND_FOR_PATH:
					try
					{
						GetKnownFolderForPath(pszFindForPath!, out kfid, out kfd);
						DumpKnownFolderDef(kfid, kfd);
					}
					catch
					{
						Console.Write("Failed to find KnownFolder for path: {0}\n", pszFindForPath);
					}
					break;

				case ACTION_TYPE.ACT_SHOW_USAGE:
				default:
					DumpUsage();
					break;
			}

			//FreeKnownFolderDefinitionFields(&kfd);
		}
		else
		{
			DumpUsage();
		}
	}

	private static bool ExtractParam(string pszPrefix, string pszArg, [NotNullWhen(true)] out string? ppszParam)
	{
		ppszParam = default;

		bool fSuccess = false;
		SizeT cchPrefix = pszPrefix.Length;
		SizeT cchParam = pszArg.Length - cchPrefix;
		if (cchParam > 0)
		{
			ppszParam = pszArg.Substring(cchPrefix, cchParam + 1);
			fSuccess = true;
		}
		return fSuccess;
	}

	private static readonly (KF_DEFINITION_FLAGS flags, string pszFlagName)[] c_rgKFFlagMap =
	[
		(KF_DEFINITION_FLAGS.KFDF_LOCAL_REDIRECT_ONLY, "redirectonly"),
		(KF_DEFINITION_FLAGS.KFDF_ROAMABLE, "roamable"),
		(KF_DEFINITION_FLAGS.KFDF_PRECREATE, "precreate"),
		(KF_DEFINITION_FLAGS.KFDF_STREAM, "streamable"),
		(KF_DEFINITION_FLAGS.KFDF_PUBLISHEXPANDEDPATH, "expandedpath"),
	];

	private static bool ArgToFlag(string? pszCat, out KF_DEFINITION_FLAGS pFlags)
	{
		var (flags, pszFlagName) = c_rgKFFlagMap.FirstOrDefault(p => string.Equals(p.pszFlagName, pszCat, StringComparison.InvariantCultureIgnoreCase));
		pFlags = flags;
		return pszFlagName is not null;
	}

	private static readonly (KF_CATEGORY category, string pszCategoryName)[] c_rgKFCategoryMap =
	[
		(KF_CATEGORY.KF_CATEGORY_VIRTUAL, ""),
		(KF_CATEGORY.KF_CATEGORY_FIXED, "fixed"),
		(KF_CATEGORY.KF_CATEGORY_COMMON, "common"),
		(KF_CATEGORY.KF_CATEGORY_PERUSER, "user"),
	];

	private static bool ArgToCategory(string? pszCat, out KF_CATEGORY pCategory)
	{
		var (category, pszCategoryName) = c_rgKFCategoryMap.FirstOrDefault(p => string.Equals(p.pszCategoryName, pszCat, StringComparison.InvariantCultureIgnoreCase));
		pCategory = category;
		return pszCategoryName is not null;
	}

	private static string KFCategoryToString(KF_CATEGORY category)
	{
		var (cat, name) = c_rgKFCategoryMap.FirstOrDefault(p => category == p.category);
		return name;
	}

	private static bool ParseAndValidateCommandLine(string[] ppszArgs, out ACTION_TYPE at, out Guid pkfid, out KNOWNFOLDER_DEFINITION pkfd, out string? ppszFindForPath)
	{
		bool fSuccess = true;
		at = ACTION_TYPE.ACT_UNDEFINED;
		ppszFindForPath = default;
		pkfd = default;
		pkfid = default;

		for (int i = 0; fSuccess && i < ppszArgs.Length; ++i)
		{
			if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_REGISTER))
			{
				at = ACTION_TYPE.ACT_REGISTER;
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_ENUM))
			{
				at = ACTION_TYPE.ACT_ENUM;
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_UNREGISTER))
			{
				at = ACTION_TYPE.ACT_UNREGISTER;
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_CLEAN))
			{
				at = ACTION_TYPE.ACT_CLEAN;
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_SHOW_USAGE))
			{
				at = ACTION_TYPE.ACT_SHOW_USAGE;
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_CATEGORY))
			{
				fSuccess = ExtractParam(SZ_CLA_CATEGORY, ppszArgs[i], out var pszCat);
				if (fSuccess)
				{
					fSuccess = ArgToCategory(pszCat, out pkfd.category);
				}
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_DEFFLAG))
			{
				fSuccess = ExtractParam(SZ_CLA_DEFFLAG, ppszArgs[i], out var pszCat);
				if (fSuccess)
				{
					fSuccess = ArgToFlag(pszCat, out var flags);
					if (fSuccess)
					{
						pkfd.kfdFlags |= flags;
					}
				}
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_ID))
			{
				fSuccess = ExtractParam(SZ_CLA_ID, ppszArgs[i], out var pszKFID);
				if (fSuccess)
				{
					pkfid = Guid.Parse(pszKFID!);
				}
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZNAME))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZNAME, ppszArgs[i], out pkfd.pszName!);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZDESCRIPTION))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZDESCRIPTION, ppszArgs[i], out pkfd.pszDescription!);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZRELPATH))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZRELPATH, ppszArgs[i], out pkfd.pszRelativePath);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZPARSENAME))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZPARSENAME, ppszArgs[i], out var name);
				if (fSuccess) pkfd.pszParsingName = name!;
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZLOCALIZEDNAME))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZLOCALIZEDNAME, ppszArgs[i], out pkfd.pszLocalizedName);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZICON))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZICON, ppszArgs[i], out pkfd.pszIcon);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZTOOLTIP))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZICON, ppszArgs[i], out pkfd.pszTooltip);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_PSZSECURITY))
			{
				fSuccess = ExtractParam(SZ_CLA_PSZSECURITY, ppszArgs[i], out pkfd.pszSecurity);
			}
			else if (string.Equals(ppszArgs[i].ToLower(), SZ_CLA_FINDFORPATH))
			{
				at = ACTION_TYPE.ACT_FIND_FOR_PATH;
				fSuccess = ExtractParam(SZ_CLA_FINDFORPATH, ppszArgs[i], out ppszFindForPath);
			}
		}

		return fSuccess;
	}

	private static void GenerateResourcePath(out string ppsz, uint dwRID, bool fUseAtSign)
	{
		ppsz = string.Format(fUseAtSign ? "@{0},-{1}" : "{0},-{1}", Environment.ProcessPath ?? "", dwRID);
	}

	// here we'll fill out all fields not entered by the user
	private static void CompleteKnownFolderDef(ref KNOWNFOLDER_DEFINITION pkfd)
	{
		if (0 == pkfd.category)
		{
			pkfd.category = KF_CATEGORY.KF_CATEGORY_PERUSER;
		}

		if (default == pkfd.pszName)
		{
			pkfd.pszName = "SDK Sample KnownFolder";
		}

		if (Guid.Empty == pkfd.fidParent)
		{
			// by default we root under user profile
			pkfd.fidParent = KNOWNFOLDERID.FOLDERID_Profile.Guid();
		}

		if (default == pkfd.pszDescription)
		{
			pkfd.pszDescription = "This folder is a sample known folder";
		}

		if (default == pkfd.pszRelativePath)
		{
			pkfd.pszRelativePath = "SDKSampleFolder";
		}

		if (default == pkfd.pszParsingName)
		{
			pkfd.pszParsingName = Guid.NewGuid().ToString();
		}

		if (default == pkfd.pszTooltip)
		{
			GenerateResourcePath(out pkfd.pszTooltip, IDS_KFSAMPLE_TOOLTIP, true);
		}

		if (default == pkfd.pszLocalizedName)
		{
			GenerateResourcePath(out pkfd.pszLocalizedName, IDS_KFSAMPLE_LOCALIZEDNAME, true);
		}

		if (default == pkfd.pszIcon)
		{
			GenerateResourcePath(out pkfd.pszIcon, IDI_KFSAMPLE_ICON, false);
		}
	}

	private static void DumpUsage()
	{
		Console.Write("kfexplorer.exe /[register | enum | unregister | clean] </arg:param> ...\n\n");
		Console.Write("\t/register may be combined with any of the <args> below.\n");
		Console.Write("\t\t\tkfexplorer.exe /register\n");
		Console.Write("\t\t\tkfexplorer.exe /register \"/pszName:Sample KnownFolder\" /category:user /pszRelativePath:SampleFolder \"/pszDescription:This KnownFolder is for samples!\"\n\n");
		Console.Write("\t/enum may be combined with /pszName. If /pszName is specified only KnownFolders with names containing /pszName will be enumerated\n");
		Console.Write("\t\t\tkfexplorer.exe /enum\n");
		Console.Write("\t\t\tkfexplorer.exe /enum /pszName:Fonts\n\n");
		Console.Write("\t/unregister requires either /id or /pszPath\n");
		Console.Write("\t\t\tkfexplorer.exe /unregister /id:{7B396E54-9EC5-4300-BE0A-2482EBAE1A26}\n\n");
		Console.Write("\t<arg> may be any number of the following:\n");
		Console.Write("\t\t/pszName\tNon-localized human readable name of KnownFolder\n");
		Console.Write("\t\t/pszDescription\tDescription and purpose of the KnownFolder\n");
		Console.Write("\t\t/fidParent\tKNOWNFOLDERID (Guid) of parent knownfolder\n");
		Console.Write("\t\t/pszRelativePath\tPath of KnownFolder relative to pfidParent. If this folder does not exist it will be created.\n");
		Console.Write("\t\t/pszTooltip\tResource path for tooltip string (i.e.: c:\\kf.dll,-119)\n");
		Console.Write("\t\t/pszLocalizedName\tResource path for default localized name (i.e.: c:\\kf.dll,-119)\n");
		Console.Write("\t\t/pszIcon\tResource path for custom folder icon (i.e.: c:\\kf.dll,-119)\n");
		Console.Write("\t\t/pszSecurity\tSSDL formatted string describing default security descriptor\n");
		Console.Write("\t\t/dwAttributes\tFolder attributes\n");
		Console.Write("\t\t/defFlag\tUse this arg multiple times to build dwDefinitionFlags.\n");
		Console.Write("\t\t\tpersonalize - Can display a personalized name for this folder\n");
		Console.Write("\t\t\tlocal - Can redirect to local disk only\n");
		Console.Write("\t\t\troam - Can be synched to another machine\n");
		Console.Write("\t\t/category\tSpecify the KnownFolder category\n");
		Console.Write("\t\t\tvirtual - shell folders appear in the namespace but do not represent a physical folder (i.e.: Control Panel)\n");
		Console.Write("\t\t\tfixed - folders not managed by shell and not redirectable (i.e.: C:\\Windows)\n");
		Console.Write("\t\t\tcommon - folders used for sharing data between users (i.e.: C:\\users\\public\\desktop)\n");
		Console.Write("\t\t\tuser - per user folders rooted in the user profile (i.e.: C:\\users\\<user>\\Pictures)\n");
	}

	private static void DumpKnownFolderDef(in Guid kfid, in KNOWNFOLDER_DEFINITION kfd)
	{
		Console.Write("KNOWNFOLDER_DEFINITION for: {0}\n", kfd.pszName);
		Console.Write("\tCategory: 0x{0:x} ({1})\n", kfd.category, KFCategoryToString(kfd.category));
		Console.Write("\tKNOWNFOLDERID : {0}\n", kfid.KnownFolderId()?.ToString() ?? kfid.ToString());
		Console.Write("\tpszName : {0}\n", kfd.pszName);
		Console.Write("\tpszDescription : {0}\n", kfd.pszDescription);
		Console.Write("\tfidParent : {0}\n", kfd.fidParentEnum.HasValue ? kfd.fidParentEnum.Value : kfd.fidParent);
		Console.Write("\tpszRelativePath : {0}\n", kfd.pszRelativePath);
		Console.Write("\tpszParsingName : {0}\n", kfd.pszParsingName);
		Console.Write("\tpszTooltip : {0}\n", kfd.pszTooltip);
		Console.Write("\tpszLocalizedName : {0}\n", kfd.pszLocalizedName);
		Console.Write("\tpszIcon : {0}\n", kfd.pszIcon);
		Console.Write("\tpszSecurity : {0}\n", kfd.pszSecurity);
		Console.Write("\tdwAttributes : {0}\n", kfd.dwAttributes);
		Console.Write("\tkfdFlags : {0}\n", kfd.kfdFlags);
	}

	// You can get some information from IKnownFolder that you cannot get from
	// the KNOWNFOLDER_DEFINITION.
	private static void DumpKnownFolderInfo(IKnownFolder pkf)
	{
		Guid kfid = pkf.GetId();
		KNOWNFOLDER_DEFINITION kfd = pkf.GetFolderDefinition();
		Console.Write("IKnownFolder info for {0} ({1})\n", kfd.pszName, kfid);

		try
		{
			string pszPath = pkf.GetPath(0);
			Console.Write("\tCurrent Path : {0}\n", pszPath);
		}
		catch { }

		try
		{
			using ComReleaser<IShellItem> psi = new(pkf.GetShellItem<IShellItem>());
			string psz = psi.Item.GetDisplayName(SIGDN.SIGDN_DESKTOPABSOLUTEPARSING);
			Console.Write("\tCurrent Location: {0}\n", psz);
		}
		catch { }
	}

	private static void RemovePhysicalFolder(in Guid kfid)
	{
		using ComReleaser<IKnownFolderManager> pkfm = new(new());
		using ComReleaser<IKnownFolder> pkf = new(pkfm.Item.GetFolder(kfid));
		string pszPath = pkf.Item.GetPath(0);
		SHFILEOPSTRUCT fos = new()
		{
			wFunc = ShellFileOperation.FO_DELETE,
			pFrom = [pszPath],
			fFlags = FILEOP_FLAGS.FOF_NOCONFIRMATION | FILEOP_FLAGS.FOF_NOERRORUI | FILEOP_FLAGS.FOF_SILENT
		};
		if (0 != SHFileOperation(ref fos))
			throw new InvalidOperationException();
	}

	private static void AddRegisteredFolderToHistory(Guid kfid)
	{
		if (RegCreateKeyEx(HKEY.HKEY_LOCAL_MACHINE, SZ_REG_PATH_HISTORY, 0, default, RegOpenOptions.REG_OPTION_NON_VOLATILE,
			REGSAM.KEY_ALL_ACCESS, default, out var hKey, out _).Succeeded)
		{
			using (hKey)
				ShlwApi.SHSetValue(hKey, default, kfid.ToString(), REG_VALUE_TYPE.REG_SZ);
		}
	}

	private static void UnregisterAllKFsAddedByThisTool(out uint pdwKFs)
	{
		pdwKFs = 0;
		if (RegOpenKeyEx(HKEY.HKEY_LOCAL_MACHINE, SZ_REG_PATH_HISTORY, 0, REGSAM.KEY_ALL_ACCESS, out var hKey).Succeeded)
		{
			using (hKey)
			{
				foreach (var (valueName, type, data) in RegEnumValue(hKey, false))
				{
					Guid kfid = Guid.Parse(valueName);
					RemovePhysicalFolder(kfid);
					try
					{
						UnregisterFolder(kfid);
						++pdwKFs;
					}
					catch (Exception ex)
					{
						Console.Write("Failed to UnregisterFolder {0} hr=0x{1:x}\n", valueName, ex.HResult);
					}
				}
				RegDeleteTree(hKey, default);
			}
		}
	}
}