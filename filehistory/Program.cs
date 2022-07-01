using System;
using Vanara.PInvoke;
using static Vanara.PInvoke.FhSvcCtl;

namespace filehistory
{
	internal class Program
	{
		public static int Main(string[] args)
		/*++
		Routine Description:
		This is the main entry point of the console application.
		Arguments:
		Argc - the number of command line arguments
		Argv - command line arguments
		Return Value:
		exit code
		--*/
		{
			Console.Write("\nFile History Sample Setup Tool\n");
			Console.Write("Copyright (C) Microsoft Corporation. All rights reserved.\n\n");

			// If there are fewer than 2 command-line arguments, print the correct usage and exit
			if (args.Length < 1)
			{
				Console.Write("Usage: fhsetup <path>\n\n");
				Console.Write("Examples:\n");
				Console.Write(" fhsetup D:\\\n");
				Console.Write(" fhsetup \\\\server\\share\\\n\n");
				return 1;
			}

			ConfigureFileHistory(args[0]);

			return 0;
		}

		private static void ConfigureFileHistory(string TargetPath)
		/*++
		Routine Description:
		This function configures a target for File History.
		It will only succeed if the user has never configured File History
		before and there is no File History data on the target.
		Arguments:
		TargetPath -
		The path of the File History target
		--*/
		{
			IFhConfigMgr configMgr;

			// TargetPath must not be default
			if (TargetPath is null)
				throw new ArgumentNullException(nameof(TargetPath));

			// Copy the target path into a local variable and set the target name to an empty string to allow the config manager to set a name
			var targetPath = TargetPath;
			var targetName = "";

			// The configuration manager is used to create and load configuration files, get/set the backup status, validate a target, etc
			configMgr = new();

			// Create a new default configuration file - do not overwrite if one already exists
			Console.Write("Creating default configuration\n");
			configMgr.LoadConfiguration();
			//configMgr.CreateDefaultConfiguration(false);

			// Check the backup status If File History is disabled by group policy, quit
			Console.Write("Getting backup status\n");
			FH_BACKUP_STATUS backupStatus = configMgr.GetBackupStatus();

			if (backupStatus == FH_BACKUP_STATUS.FH_STATUS_DISABLED_BY_GP)
			{
				Console.Write("Error: File History is disabled by group policy\n");
				throw new Exception();
			}

			// Make sure the target is valid to be used for File History
			Console.Write("Validating target\n");
			FH_DEVICE_VALIDATION_RESULT validationResult = configMgr.ValidateTarget(targetPath);

			if (validationResult is not FH_DEVICE_VALIDATION_RESULT.FH_VALID_TARGET and not FH_DEVICE_VALIDATION_RESULT.FH_CURRENT_DEFAULT)
			{
				// If the target is inaccessible, read-only, an invalid drive type (such as a CD), already being used for File History, or
				// part of the protected namespace - don't enable File History
				Console.Write("Error: {0} is not a valid target\n", targetPath);
				throw new Exception();
			}

			// Provision the target to be used for File History and set it as the default target
			Console.Write("Provisioning and setting target\n");
			//configMgr.ProvisionAndSetNewTarget(targetPath, targetName);

			// Enable File History
			Console.Write("Enabling File History\n");
			configMgr.SetBackupStatus(FH_BACKUP_STATUS.FH_STATUS_ENABLED);

			// Save the configuration to disk
			Console.Write("Saving configuration\n");
			configMgr.SaveConfiguration();

			// Tell the File History service to schedule backups
			Console.Write("Scheduling regular backups\n");
			_=ScheduleBackups();

			// Recommend the target to other Homegroup members
			Console.Write("Recommending target to Homegroup\n");
			configMgr.ChangeDefaultTargetRecommendation(true);

			Console.Write("Success! File History is now enabled\n");
		}

		private static HRESULT ScheduleBackups()
		/*++
		Routine Description:
		This function starts the File History service if it is stopped
		and schedules regular backups.
		Arguments:
		None
		Return Value:
		HRESULT.S_OK if successful
		HRESULT from underlying functions
		--*/
		{
			HRESULT backupHr = HRESULT.S_OK;

			HRESULT pipeHr = FhServiceOpenPipe(true, out SafeFH_SERVICE_PIPE_HANDLE pipe);
			if (pipeHr.Succeeded)
			{
				backupHr = FhServiceReloadConfiguration(pipe);
				pipeHr = FhServiceClosePipe(pipe);
			}

			// The HRESULT from the backup operation is more important than the HRESULT from pipe operations
			return backupHr.Failed ? backupHr : pipeHr;
		}
	}
}