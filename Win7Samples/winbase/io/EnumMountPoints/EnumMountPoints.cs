using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace EnumMountPoints
{
	static class Program
	{
		static void Main()
		{
			EnumVolumes();
		}

		/*-----------------------------------------------------------------------------
		EnumVolumes()

		Parameters
		None

		Return Value
		None

		Notes
		FindFirstVolume/FindNextVolume returns the unique volume name for each.
		Since unique volume names aren't very user friendly, PrintDosDeviceNames
		prints out the Dos device name(s) that refer to the volume.
		-----------------------------------------------------------------------------*/
		static void EnumVolumes()
		{
			try
			{
				// Find the rest of the unique volumes and enumerate each of their
				// mount points.
				foreach (var szVolumeName in Kernel32.EnumVolumes())
				{
					Console.Write("\nUnique vol name: ");
					Console.WriteLine(szVolumeName);
					PrintDosDeviceNames(szVolumeName);
					EnumMountPoints(szVolumeName);
				}
			}
			catch (Exception ex)
			{
				// If we can't even find one volume, just print an error and return.
				Console.Write("FindFirstVolume failed. Error = {0}\n", ex.Message);
			}
		}

		/*-----------------------------------------------------------------------------
		EnumMountPoints(string szVolume)

		Parameters
		szVolume
		Unique volume name of the volume to enumerate mount points for.

		Return Value
		None

		Notes
		Enumerates and prints the volume mount points (if any) for the unique
		volume name passed in.
		-----------------------------------------------------------------------------*/
		static void EnumMountPoints(string szVolume)
		{
			try
			{
				// Find and print the rest of the mount points
				foreach (var szMountPoint in EnumVolumeMountPoints(szVolume))
				{
					PrintMountPoint(szVolume, szMountPoint);
				}
			}
			catch (Exception ex)
			{
				Console.Write("No mount points: {0}\n", ex.Message);
			}
		}

		/*-----------------------------------------------------------------------------
		PrintMountPoint(string szVolume, string szMountPoint)

		Parameters
		szVolume
		Unique volume name the mount point is located on

		szMountPoint
		Name of the mount point to print

		Return Value
		None

		Notes
		Prints out both the mount point and the unique volume name of the volume
		mounted at the mount point.
		-----------------------------------------------------------------------------*/
		static void PrintMountPoint(string szVolume, string szMountPoint)
		{
			var szVolumeName = new StringBuilder(MAX_PATH);

			Console.Write(" * Mount point: ");

			// Print out the mount point
			Console.WriteLine(szMountPoint);
			Console.Write(" ...is a mount point for...\n");

			// Append the mount point name to the unique volume name to get the
			// complete path name for the mount point
			var szMountPointPath = szVolume + szMountPoint;

			// Get and print the unique volume name for the volume mounted at the
			// mount point
			if (!GetVolumeNameForVolumeMountPoint(szMountPointPath, szVolumeName, MAX_PATH))
			{
				Console.Write("GetVolumeNameForVolumeMountPoint failed. Error = {0}\n", GetLastError());
			}
			else
			{
				Console.Write(" {0}\n", szVolumeName);
			}
		}

		/*-----------------------------------------------------------------------------
		PrintDosDeviceNames(string szVolume)

		Parameters
		szVolume
		Unique volume name to get the Dos device names for

		Return Value
		None

		Notes
		Prints out the Dos device name(s) for the unique volume name
		-----------------------------------------------------------------------------*/
		static void PrintDosDeviceNames(string szVolume)
		{
			var szVolumeName = new StringBuilder(MAX_PATH);

			// Get all logical drive strings
			Console.Write("Dos drive names: ");

			// Get the unique volume name for each logical drive string. If the volume
			// drive string matches the passed in volume, print out the Dos drive name
			Console.WriteLine(string.Join(" ", GetLogicalDriveStrings().Where(s => string.Equals(szVolume, GetMtPtVolumeName(s), StringComparison.OrdinalIgnoreCase))));

			string GetMtPtVolumeName(string szDrive) =>
				GetVolumeNameForVolumeMountPoint(szDrive, szVolumeName, MAX_PATH) ? szVolumeName.ToString() : null;
		}
	}
}