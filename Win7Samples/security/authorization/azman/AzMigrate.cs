/****************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzMigrate.cpp

Abstract:

	Defines the entry point for the console application.

 History:

****************************************************************************/

global using Vanara.PInvoke;
global using static Vanara.PInvoke.AzRoles;

namespace AzMigrate;

public class Program
{
	[STAThread]
	public static int Main(string[] args)
	{
		int iReturnValue = 0;
		int iStatus = ParseCommandLine(args);

		if (iStatus < 0)
		{
			Console.WriteLine("Invalid command line arguments");

			DisplayUsage();

			iReturnValue = -1;

			goto Done;
		}

		if (iStatus == 0)
		{

			Run();

			iReturnValue = CAzLogging.MIGRATE_SUCCESS ? 0 : -1;

			if (CAzLogging.MIGRATE_SUCCESS)
				Console.WriteLine("Successful Migration");
			else
				Console.WriteLine("Errors in Migration");
		}

Done:
		return iReturnValue;
	}

	/*++

	Routine description:

	This method runs most of the migration code

	Arguments: NONE

	Return Value:NONE


	--*/
	static void Run()
	{
		CAzMStore sourceStore = new(CAzGlobalOptions.m_bstrSourceStoreName), destStore = new(CAzGlobalOptions.m_bstrDestStoreName);

		var hr = sourceStore.InitializeStore(false);

		if (hr.Failed)
			goto lError1;

		hr = destStore.InitializeStore(true);

		if (hr.Failed) {

			if (CAzGlobalOptions.m_bOverWrite == true) {

				hr = destStore.OverWriteStore();

				CAzLogging.Log(hr, "Overwriting AzMan Authornization Store", CAzGlobalOptions.m_bstrDestStoreName);

				if (hr.Failed)
					goto lError1;

			} else {

				goto lError1;
			}
		}

		if (CAzGlobalOptions.m_bSpecificApp == false)
			hr = sourceStore.OpenAllApps();
		else {
			foreach (string appName in CAzGlobalOptions.m_bstrAppNames)
				hr = sourceStore.OpenApp(appName);

			if (hr.Failed)
				goto lError1;
		}

		if (hr.Failed)
			goto lError1;

		destStore.Copy(sourceStore);

lError1:
		CAzLogging.Close();
	}


	/*++

	Routine description:

	This method parses the command line arguments and sets the appropriate
	members in the CAzGlobalOptions class

	Return Value:

	Returns success - 0
	failure - -1
	help - 1

	--*/
	static int ParseCommandLine(string[] rgCmdArgs)
	{
		int iRet;
		int iNumArgs = rgCmdArgs.Length;
		int iNumToBeProcessed;

		// To take care of no command line arguments
		if (iNumArgs <= 0) {
			iRet = -1;
			goto Cleanup;
		}

		// Check if one argument only and that is the /? argument
		if (0 == string.Compare(rgCmdArgs[0], 0, CAzGlobalOptions.HELPTAG, 0, CAzGlobalOptions.HELPTAG_LEN, true)) {
			DisplayUsage();
			iRet = 1;
			goto Cleanup;
		}

		// if more than one argument but not the required 2 arguments
		if (iNumArgs < 3) {
			iRet = -1;
			goto Cleanup;
		}

		CAzGlobalOptions.m_bstrDestStoreName = rgCmdArgs[0];

		CAzGlobalOptions.m_bstrSourceStoreName = rgCmdArgs[1];

		//CAzGlobalOptions.setDefaults();

		iNumToBeProcessed = iNumArgs - 2;

		for (int i = 2; i < iNumArgs; i++) {

			// Checking for /logfile
			if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.LOGFILETAG, 0, CAzGlobalOptions.LOGFILETAG_LEN, true)) {

				var strRightPart = rgCmdArgs[i].IndexOf('=') is int index && index >= 0 ? rgCmdArgs[i][index..] : null;

				if (default == strRightPart) {
					iRet = -1;
					goto Cleanup;
				}

				CAzLogging.Initialize(LogLevel.LOG_LOGFILE, strRightPart.TrimStart('='));

				iNumToBeProcessed--;

			} else if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.APPNAMETAG, 0, CAzGlobalOptions.APPNAMETAG_LEN, true)) {

				//Checking for /application flag

				var strRightPart = rgCmdArgs[i].IndexOf('=') is int index && index >= 0 ? rgCmdArgs[i][index..] : null;

				if (strRightPart == null) {
					iRet = -1;
					goto Cleanup;
				}
				CAzGlobalOptions.m_bSpecificApp = true;

				string[] strAppNames = strRightPart!.TrimStart('=').Split(',');

				foreach (string appName in strAppNames)
					CAzGlobalOptions.m_bstrAppNames.Add(appName);

				iNumToBeProcessed--;

			} else if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.OVERWRITETAG, 0, CAzGlobalOptions.OVERWRITETAG_LEN, true)) {

				//Checking for /overwrite flag

				CAzGlobalOptions.m_bOverWrite = true;

				iNumToBeProcessed--;

			} else if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.IGNOREMEMBERSTAG, 0, CAzGlobalOptions.IGNOREMEMBERSTAG_LEN, true)) {

				//Checking for /IGNOREMEMBERS flag

				CAzGlobalOptions.m_bIgnoreMembers = true;

				iNumToBeProcessed--;

			} else if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.IGNOREPOLICYADMINSTAG, 0, CAzGlobalOptions.IGNOREPOLICYADMINSTAG_LEN, true)) {

				//Checking for /IGNOREPOLICYADMIN flag

				CAzGlobalOptions.m_bIgnorePolicyAdmins = true;

				iNumToBeProcessed--;

			} else if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.VERBOSETAG, 0, CAzGlobalOptions.VERBOSETAG_LEN, true)) {

				CAzGlobalOptions.m_bVerbose = true;

				CAzLogging.Initialize(LogLevel.LOG_TRACE);

				iNumToBeProcessed--;

			} else if (0 == string.Compare(rgCmdArgs[i], 0, CAzGlobalOptions.HELPTAG, 0, CAzGlobalOptions.HELPTAG_LEN, true)) {

				DisplayUsage();

				iNumToBeProcessed--;
				iRet = 1;
				goto Cleanup;
			}
		}
		// Some additional parameters exist which donot match
		// hence these are invalid flags.
		if (0 != iNumToBeProcessed)
		{
			iRet = -1;
			goto Cleanup;
		}

		iRet = 0;

Cleanup:

		return iRet;
	}

	/*++

	Routine description:

	This method displays the usage details of the tool

	Arguments: NONE

	Return Value: NONE


	--*/
	static void DisplayUsage()
	{
		Console.WriteLine("\tAzMigrate <destination store> <source store> [flags]\n");
		Console.WriteLine("\tFlags:" + Environment.NewLine + "\t/o : If destination store exists, it would be overwritten");
		Console.WriteLine("\t/a=[application name1,application name2,....] : Migrate specified applications only to destination store");
		Console.WriteLine("\t/l=[log file name] : Log all the operations performed during migration into specified log file");
		Console.WriteLine("\t/ip : Ignore all Policy assignments");
		Console.WriteLine("\t/im : Ignore all members");
		Console.WriteLine("\t/v : Verbose mode");
		Console.WriteLine("\t/? : Help");
	}
}