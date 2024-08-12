using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.FMApi;
using static Vanara.PInvoke.Kernel32;

namespace FMAPI;

internal static class Program
{
	// Define program specific values
	private const string VOLUME = @"\\.\D:";

	//
	//Program entry point
	//
	private static void Main()
	{
		HeapSetInformation(default, HEAP_INFORMATION_CLASS.HeapEnableTerminationOnCorruption, default, 0);

		//Read the first BOOT_SECTOR_SIZE bytes on the volume
		if (!ReadVolumeBytes(VOLUME, out var bootSector))
		{
			Console.Write("Error reading from volume, Error: {0}\n", Win32Error.GetLastError());
			return;
		}

		//Detect the boot sector information
		if (DetectBootSector(bootSector, out var info))
		{
			//Determine file system type
			string fileSystem = info.FileSystem.ToString().Substring(10);

			//Display the boot sector information
			Console.Write("File System Type: {0}\n", fileSystem);
			Console.Write("Bytes Per Sector: {0}\n", info.BytePerSector);
			Console.Write("Sectors Per Cluster: {0}\n", info.SectorPerCluster);
			Console.Write("Total Sectors: {0}\n", info.TotalSectors);
		}
		else
		{
			Console.Write("Boot sector not recognized.");
		}
	}

	//
	//Read the first BOOT_SECTOR_SIZE bytes of a given volume
	//
	private static bool ReadVolumeBytes(string vol, out byte[] buffer)
	{
		//Initialize buffer with nulls
		buffer = new byte[BOOT_SECTOR_SIZE];

		//Open the volume
		using SafeHFILE fileHandle = CreateFile(vol, FileAccess.GENERIC_READ, FILE_SHARE.FILE_SHARE_READ, default,
			CreationOption.OPEN_EXISTING, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, default);
		if (!fileHandle)
			return false;

		//Read the fist BOOT_SECTOR_SIZE bytes
		bool success = ReadFile(fileHandle, buffer, BOOT_SECTOR_SIZE, out var bytesRead, default);

		return success && BOOT_SECTOR_SIZE == bytesRead;
	}

	// From https://github.com/gtworek/PSBits/tree/master/FMAPI
	private static void Scan()
	{
		using SimulateWinPE ctx = new();

		Win32Error.ThrowLastErrorIfFalse(CreateFileRestoreContext(VOLUME, RESTORE_CONTEXT_FLAGS.ContextFlagVolume | RESTORE_CONTEXT_FLAGS.FlagScanRemovedFiles | RESTORE_CONTEXT_FLAGS.FlagScanIncludeRemovedDirectories,
			0, 0, FILE_RESTORE_VERSION_2, out var context));
		try
		{
			bool success = true;
			while (success)
			{
				if (ScanRestorableFiles(context, @"\", out var fileInfo))
				{
					Console.WriteLine($"File found: {fileInfo.Value.FileName}");
				}
				else
				{
					Win32Error err = Win32Error.GetLastError();
					if (err == Win32Error.ERROR_INSUFFICIENT_BUFFER)
						continue;
					success = false;
					if (err == Win32Error.ERROR_NO_MORE_FILES)
						Console.WriteLine($"No more files.");
					else
						Console.WriteLine($"Unknown error: {err}");
				}
			}
		}
		finally
		{
			CloseFileRestoreContext(context);
		}
	}
}

public class SimulateWinPE : IDisposable
{
	private const string miniNTKey = @"SYSTEM\CurrentControlSet\Control\MiniNT";
	private readonly bool keyCreated = false;
	public SimulateWinPE() => keyCreated = RegOpenKey(HKEY.HKEY_LOCAL_MACHINE, miniNTKey, out _).Failed && RegCreateKey(HKEY.HKEY_LOCAL_MACHINE, miniNTKey, out _).Succeeded;
	void IDisposable.Dispose() { if (keyCreated) RegDeleteKey(HKEY.HKEY_LOCAL_MACHINE, miniNTKey); }
}