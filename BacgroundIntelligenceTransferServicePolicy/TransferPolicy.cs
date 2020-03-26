using System;
using Vanara.InteropServices;
using Vanara.IO;
using static Vanara.PInvoke.BITS;
using static Vanara.PInvoke.Ole32;

namespace BacgroundIntelligenceTransferServicePolicy
{
	static class TransferPolicy
	{
		[STAThread]
		static void Main()
		{
			//The impersonation level must be at least RPC_C_IMP_LEVEL_IMPERSONATE.
			CoInitializeSecurity(default, -1, default, default, RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_CONNECT, RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IMPERSONATE, default, EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE).ThrowIfFailed();

			using var pQueueMgr = ComReleaserFactory.Create(new IBackgroundCopyManager());

			// Create a Job
			Console.Write("Creating Job...\n");

			pQueueMgr.Item.CreateJob("TransferPolicy", BG_JOB_TYPE.BG_JOB_TYPE_DOWNLOAD, out var guidJob, out var pBackgroundCopyJob);
			using var pppBackgroundCopyJob = ComReleaserFactory.Create(pBackgroundCopyJob);

			Console.Write(" Job is succesfully created ...\n");

			// Set Transfer Policy for the job
			var propval = new BITS_JOB_PROPERTY_VALUE { Dword = (uint)(BITS_COST_STATE.BITS_COST_STATE_USAGE_BASED | BITS_COST_STATE.BITS_COST_STATE_OVERCAP_THROTTLED | BITS_COST_STATE.BITS_COST_STATE_BELOW_CAP | BITS_COST_STATE.BITS_COST_STATE_CAPPED_USAGE_UNKNOWN | BITS_COST_STATE.BITS_COST_STATE_UNRESTRICTED) };

			var pBackgroundCopyJob5 = (IBackgroundCopyJob5)pBackgroundCopyJob;

			pBackgroundCopyJob5.SetProperty(BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_ID_COST_FLAGS, propval);

			// get Transfer Policy for the new job
			Console.Write("Getting TransferPolicy Property ...\n");

			var actual_propval = pBackgroundCopyJob5.GetProperty(BITS_JOB_PROPERTY_ID.BITS_JOB_PROPERTY_ID_COST_FLAGS);

			var job_transferpolicy = (BITS_COST_STATE)actual_propval.Dword;
			Console.Write("get TransferPolicy Property returned {0}\n", job_transferpolicy);

			pBackgroundCopyJob.Cancel();
		}

		[STAThread]
		static void AlternateMain()
		{
			//The impersonation level must be at least RPC_C_IMP_LEVEL_IMPERSONATE.
			CoInitializeSecurity(default, -1, default, default, RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_CONNECT, RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IMPERSONATE, default, EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE).ThrowIfFailed();

			// Create a Job
			Console.Write("Creating Job...\n");

			var job = Vanara.IO.BackgroundCopyManager.Jobs.Add("TransferPolicy");

			Console.Write(" Job is succesfully created ...\n");

			// Set Transfer Policy for the job
			job.TransferBehavior = BackgroundCopyCost.OstStateOvercapThrottled | BackgroundCopyCost.OstStateUsageBased | BackgroundCopyCost.BelowCap | BackgroundCopyCost.CappedUsageUnknown | BackgroundCopyCost.Unrestricted;

			// get Transfer Policy for the new job
			Console.Write("Getting TransferPolicy Property ...\n");

			Console.Write("get TransferPolicy Property returned {0}\n", job.TransferBehavior);

			job.Cancel();
		}
	}
}
