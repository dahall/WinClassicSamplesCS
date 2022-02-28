using System;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.PInvoke.VssApi;

namespace vshadow
{
	internal partial class VssClient
	{
		// Query the properties of the given shadow copy
		public void GetSnapshotProperties(Guid snapshotID)
		{
			// Get the shadow copy properties
			VSS_SNAPSHOT_PROP Snap = m_pVssObject.GetSnapshotProperties(snapshotID);

			// Print the properties of this shadow copy
			PrintSnapshotProperties(Snap);
		}

		// Query all the shadow copies in the given set If snapshotSetID is NULL, just query all shadow copies in the system
		public void QuerySnapshotSet(Guid snapshotSetID = default)
		{
			TextWriter ft = Console.Out;

			if (snapshotSetID == Guid.Empty)
				ft.WriteLine("\nQuerying all shadow copies in the system ...\n");
			else
				ft.WriteLine("\nQuerying all shadow copies with the SnapshotSetID {0} ...\n", snapshotSetID);

			// Get list all shadow copies.
			IVssEnumObject pIEnumSnapshots = null;
			try
			{
				pIEnumSnapshots = m_pVssObject.Query(Guid.Empty, VSS_OBJECT_TYPE.VSS_OBJECT_NONE, VSS_OBJECT_TYPE.VSS_OBJECT_SNAPSHOT);
			}
			// If there are no shadow copies, just return
			catch (COMException c) when (c.HResult == HRESULT.S_FALSE)
			{
				if (snapshotSetID == Guid.Empty)
					ft.WriteLine("\nThere are no shadow copies in the system\n");
				return;
			}
			catch (Exception ex)
			{
				ft.WriteLine(ex);
			}

			var ie = new Vanara.Collections.IEnumFromCom<VSS_OBJECT_PROP>(pIEnumSnapshots.Next, pIEnumSnapshots.Reset);
			foreach (var Prop in ie)
			{
				VSS_SNAPSHOT_PROP Snap = Prop.Obj.Snap;

				// Print the shadow copy (if not filtered out)
				if ((snapshotSetID == Guid.Empty) || (Snap.m_SnapshotSetId == snapshotSetID))
					PrintSnapshotProperties(Snap);
			}
		}

		// Print the properties for the given snasphot
		private void PrintSnapshotProperties(in VSS_SNAPSHOT_PROP prop)
		{
			TextWriter ft = Console.Out;

			var lAttributes = prop.m_lSnapshotAttributes;

			ft.WriteLine("* SNAPSHOT ID = {0} ...", prop.m_SnapshotId);
			ft.WriteLine(" - Shadow copy Set: {0}", prop.m_SnapshotSetId);
			ft.WriteLine(" - Original count of shadow copies = {0}", prop.m_lSnapshotsCount);
			ft.WriteLine(" - Original Volume name: {0} [{1}]", prop.m_pwszOriginalVolumeName, Util.GetDisplayNameForVolume(prop.m_pwszOriginalVolumeName));
			ft.WriteLine(" - Creation Time: {0}", prop.m_tsCreationTimestamp.ToDateTime());
			ft.WriteLine(" - Shadow copy device name: {0}", prop.m_pwszSnapshotDeviceObject);
			ft.WriteLine(" - Originating machine: {0}", prop.m_pwszOriginatingMachine);
			ft.WriteLine(" - Service machine: {0}", prop.m_pwszServiceMachine);

			if ((prop.m_lSnapshotAttributes & VSS_VOLUME_SNAPSHOT_ATTRIBUTES.VSS_VOLSNAP_ATTR_EXPOSED_LOCALLY) != 0)
				ft.WriteLine(" - Exposed locally as: {0}", prop.m_pwszExposedName);
			else if ((prop.m_lSnapshotAttributes & VSS_VOLUME_SNAPSHOT_ATTRIBUTES.VSS_VOLSNAP_ATTR_EXPOSED_REMOTELY) != 0)
			{
				ft.WriteLine(" - Exposed remotely as {0}", prop.m_pwszExposedName);
				if (!string.IsNullOrEmpty(prop.m_pwszExposedPath))
					ft.WriteLine(" - Path exposed: {0}", prop.m_pwszExposedPath);
			}
			else
				ft.WriteLine(" - Not Exposed");

			ft.WriteLine(" - Provider id: {0}", prop.m_ProviderId);

			// Display the attributes
			ft.WriteLine(" - Attributes: {0}", lAttributes.ToString().Replace("VSS_VOLSNAP_ATTR_", ""));

			ft.WriteLine("");
		}
	}
}