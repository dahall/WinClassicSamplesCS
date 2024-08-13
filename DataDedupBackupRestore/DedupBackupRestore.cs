/* Choosing to not finish this project as it involves converting all Wbem calls over to .NET and can only be executed on Windows Server, which I don't have acess to. */

using System.Management;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.DdpBackup;
using static Vanara.PInvoke.Macros;
using Vanara.InteropServices;

namespace Dedpu;

/////////////////////////////////////////////////////////////////////
//
// This class is for selective restore from optimized backup
//
/////////////////////////////////////////////////////////////////////
[ComVisible(true)]
class CBackupStore([In] string backupLocation) : IDedupReadFileCallback
{
	private readonly Dictionary<string, long> m_dataStreamMap = [];

	public HRESULT ReadBackupFile(string FileFullPath, long FileOffset, uint SizeToRead, byte[] FileBuffer, out uint ReturnedSize, uint Flags = 0)
	{
		// This method is called by the backup support COM object to read the backup database from the backup medium
		HRESULT hr = HRESULT.S_OK;
		string filePath = backupLocation + FileFullPath;
		ReturnedSize = 0;

		// FileBuffer contents can be uninitialized after ref byte ReturnedSize
		using var hFile = CreateFile(filePath, FileAccess.GENERIC_READ, FILE_SHARE.FILE_SHARE_READ, default, CreationOption.OPEN_EXISTING, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS);
		if (!hFile)
		{
			hr = GetLastError().ToHRESULT();
			Console.WriteLine($"Cannot open file {filePath}. Did we back it up? Error: {hr}");
		}
		else
		{
			// This file was saved with BackupRead, so its contents are actually a series of WIN32_STREAM_ID structures that describe the
			// original file To read data as we would read from the original file, we first need to find the DATA stream. If the file was not
			// backed up with BackupRead you would not need this
			hr = FindDataStream(hFile, filePath, out var dataStreamOffset);
			if (hr.Succeeded)
			{
				long actualFileOffset = FileOffset + dataStreamOffset;
				NativeOverlapped overlapped = new() { OffsetLow = unchecked((int)actualFileOffset.LowPart()), OffsetHigh = actualFileOffset.HighPart() };
				if (!ReadFile(hFile, FileBuffer, (int)SizeToRead, out var retSize, ref overlapped))
				{
					Console.WriteLine($"Cannot read from file {filePath}. Did we back it up? gle= {GetLastError()}");
					hr = GetLastError().ToHRESULT();
				}
				ReturnedSize = (uint)retSize;
			}
		}
		return hr;
	}

	public HRESULT OrderContainersRestore(uint NumberOfContainers, string[] ContainerPaths, out uint ReadPlanEntries, out DEDUP_CONTAINER_EXTENT[] ReadPlan)
	{
		// If you backed up to multiple tapes and parts of every file are split between the tapes
		// you want avoid switching tapes back and forth as the restore engine the backup database files
		// To implement this, you need to return the order in which the restore engine should read the files
		// In this example, we tell the restore engine to read first 64k of every file first, then
		// the remainder of the file. In a real tape backup application you would need to read the backup 
		// catalog, then return an array containing the file ranges on the first tape, 
		// then the files on the second tape, and so on
		// If you are backing up on fixed disk you don't need to implement this, just uncomment the following 2 
		// lines and delete the rest:
		//*ReadPlanEntries;
		//*ReadPlan = default;

		uint fileFragments = 2 * NumberOfContainers;
		ReadPlan = new DEDUP_CONTAINER_EXTENT[fileFragments];
		ReadPlanEntries = fileFragments;

		for (uint i = 0; i < fileFragments; i++)
		{
			ReadPlan[i].ContainerIndex = i % NumberOfContainers;

			string filePath = backupLocation;
			string relativePath = ContainerPaths[i % NumberOfContainers];
			filePath += relativePath;

			// Chop the file at % of file size, to demonstrate extents
			long fileSize = default;
			GetFileSize(filePath, fileSize);

			uint extentBoundary = (fileSize.LowPart() * 10) / 100;

			if (i < NumberOfContainers)
			{
				ReadPlan[i].StartOffset = 0;
				ReadPlan[i].Length = extentBoundary;
			}
			else
			{
				ReadPlan[i].StartOffset = extentBoundary;
				ReadPlan[i].Length = long.MaxValue; // this just says "until the end of the file"
			}
		}
		return HRESULT.S_OK;
	}

	public HRESULT PreviewContainerRead(string FileFullPath, uint NumberOfReads, DDP_FILE_EXTENT[] ReadOffsets)
	{
		// This will be called before the actual read, so you can optimize and do bigger reads instead
		// of smaller ones. If you decide you want to do this, examine the ReadOffsets and do a bigger read 
		// into a big buffer, then satisfy the next reads from the buffer you allocated.

		return HRESULT.S_OK;
	}

	private HRESULT FindDataStream([In] HFILE hFile, [In] string filePath, out long result)
	{
		uint nameOffset = (uint)Marshal.OffsetOf<WIN32_STREAM_ID>(nameof(WIN32_STREAM_ID.cStreamName)).ToInt32();

		// Cache the results per file path so we don't have to do this for every read
		if (!m_dataStreamMap.TryGetValue(filePath, out result))
		{
			SafeHGlobalStruct<WIN32_STREAM_ID> streamId = new();
			while (streamId.Value.dwStreamId != BACKUP_STREAM_ID.BACKUP_DATA)
			{
				SetFilePointerEx(hFile, streamId.Size, out _, System.IO.SeekOrigin.Current);
				// The Size field is the actual size starting from the cStreamName field, so we only want to read the header
				if (!ReadFile(hFile, streamId, nameOffset, out var bytesRead) || bytesRead != nameOffset)
				{
					Console.WriteLine($"Cannot find the data stream in file. Did you use something other than BackupRead? Error: {GetLastError()}");
					return HRESULT.E_UNEXPECTED;
				}
			}

			// Get the current position
			SetFilePointerEx(hFile, default, out result, System.IO.SeekOrigin.Current);
			m_dataStreamMap[filePath] = result;
		}
		return HRESULT.S_OK;
	}
}

internal static class Program
{
	// Backup/restore constants
	const string SYSTEM_VOLUME_INFORMATION = "\\System Volume Information";
	const string DEDUP_FOLDER = "\\Dedup";
	const string BACKUP_METADATA_FILE_NAME = "dedupBackupMetadata.{741309a8-a42a-4830-b530-fad823933e6d}";
	const string BACKUP_METADATA_FORMAT = "{0}\r\n{1}\r\n";
	const string LONG_PATH_PREFIX = "\\\\?\\";
	const string DIRECTORY_BACKUP_FILE = "directoryBackup.{741309a8-a42a-4830-b530-fad823933e6d}";
	const uint DEDUP_STORE_FOLDERS_COUNT = 3;
	static readonly string[] DEDUP_STORE_FOLDERS = ["\\ChunkStore", "\\Settings", "\\State"];

	// WMI constants from Data Deduplication MOF schema
	const string CIM_V2_NAMESPACE = "root\\cimv2";
	const string CIM_DEDUP_NAMESPACE = "root\\Microsoft\\Windows\\Deduplication";
	const string CIM_DEDUP_CLASS_VOLUME = "MSFT_DedupVolume";
	const string CIM_DEDUP_CLASS_VOLUME_METADATA = "MSFT_DedupVolumeMetadata";
	const string CIM_DEDUP_CLASS_JOB = "MSFT_DedupJob";
	const string CIM_DEDUP_METHOD_ENABLE = "Enable";
	const string CIM_DEDUP_METHOD_DISABLE = "Disable";
	const string CIM_DEDUP_METHOD_STOP = "Stop";
	const string CIM_DEDUP_METHOD_START = "Start";
	const string CIM_DEDUP_PROP_STOREID = "StoreId";
	const string CIM_DEDUP_PROP_DATAACCESS = "DataAccess";
	const string CIM_DEDUP_PROP_VOLUME = "Volume";
	const string CIM_DEDUP_PROP_VOLUMEID = "VolumeId";
	const string CIM_DEDUP_PROP_RETURNVALUE = "ReturnValue";
	const string CIM_DEDUP_PROP_TYPE = "Type";
	const string CIM_DEDUP_PROP_TIMESTAMP = "Timestamp";
	const string CIM_DEDUP_PROP_WAIT = "Wait";
	const string CIM_DEDUP_PROP_JOB = "DedupJob";
	const string CIM_DEDUP_PROP_ID = "Id";
	const string CIM_DEDUP_PROP_ERRORCODE = "error_Code";
	const string CIM_DEDUP_PROP_ERRORMESSAGE = "Message";
	const string CIM_DEDUP_PROP_PATH = "__PATH";
	const uint CIM_DEDUP_JOB_TYPE_UNOPT = 4;
	const uint CIM_DEDUP_JOB_TYPE_GC = 2;

	const string DEDUP_OPERATIONAL_EVENT_CHANNEL_NAME = "Microsoft-Windows-Deduplication/Operational";

	static readonly HRESULT DDP_E_NOT_FOUND = ((HRESULT)0x80565301);
	static readonly HRESULT DDP_E_PATH_NOT_FOUND = ((HRESULT)0x80565304);
	static readonly HRESULT DDP_E_VOLUME_DEDUP_DISABLED = ((HRESULT)0x80565323);

	// Enums, classes
	enum Action
	{
		BackupAction,
		RestoreStubAction,
		RestoreDataAction,
		RestoreFileAction,
		RestoreVolumeAction,
		RestoreFilesAction
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Selective restore
	//
	/////////////////////////////////////////////////////////////////////

	private static HRESULT RestoreStub([In] string source, [In] string destination) => RestoreFile(source, destination);

	private static HRESULT RestoreData([In] string source, [In] string destination)
	{
		// Source is a file name, but we need the directory name so we can read
		// the backup database inside the backup store
		int lastSeparator = source.LastIndexOf('\\');
		if (lastSeparator == -1)
		{
			Console.WriteLine($"source is not a file path");
			return HRESULT.E_UNEXPECTED;
		}
		string backupLocation = source.Substring(0, lastSeparator);
		CBackupStore pStore = new(backupLocation);

		IDedupBackupSupport backupSupport = new();
		// NOTE: destination was already created by RestoreStub
		HRESULT hr = backupSupport.RestoreFiles(1, [destination], pStore, DEDUP_BACKUP_SUPPORT_PARAM_TYPE.DEDUP_RECONSTRUCT_UNOPTIMIZED, default);
		if (hr.Failed)
		{
			Console.WriteLine($"Restore failed, hr = {hr}");
			// We need to clean up on failure
			DeleteFile(destination);
		}

		if (hr == HRESULT.S_FALSE)
		{
			Console.WriteLine($"Destination file is not a Data Deduplication file");
		}
		return hr;
	}

	private static HRESULT RestoreFilesData([In] string source, [In] string[] restoredFiles)
	{
		SizeT fileCount = restoredFiles.Length;
		if (fileCount == 0)
		{
			Console.WriteLine($"No files to restore");
			return HRESULT.S_OK;
		}

		string[] bstrFiles = (string[])restoredFiles.Clone();
		IDedupBackupSupport backupSupport = new();
		HRESULT[] hrRestoreResults = new HRESULT[fileCount];
		CBackupStore pStore = new(source);

		// NOTE: destination stubs were already created by RestoreFiles
		var hr = backupSupport.RestoreFiles((uint)fileCount, bstrFiles, pStore, DEDUP_BACKUP_SUPPORT_PARAM_TYPE.DEDUP_RECONSTRUCT_UNOPTIMIZED, hrRestoreResults);
		if (hr.Failed)
		{
			Console.WriteLine($"Restore failed, hr = {hr}");

			// Files not restored successfully will have deduplication reparse points
			// Non-dedup files were completely restored when restoring stubs
			// When error code is DDP_E_JOB_COMPLETED_PARTIAL_SUCCESS some deduplicated files were also fully restored
			// Cleanup failed restores
			for (int index = 0; index < fileCount; ++index)
			{
				if (hrRestoreResults[index].Failed)
				{
					Console.WriteLine($"Failed to restore file {bstrFiles[index]}, hr = {hr}");
					DeleteFile(bstrFiles[index]);
				}
			}
		}

		if (hr == HRESULT.E_OUTOFMEMORY)
		{
			Console.WriteLine($"Not enough resources");
		}
		return hr;
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Backup full or selective volume
	//
	/////////////////////////////////////////////////////////////////////

	private static void DoBackup([In] string source, [In] string destination)
	{
		// Recursivelly back up the specified directory
		BackupDirectoryTree(source, destination);

		StringBuilder volumePathName = new(MAX_PATH);
		GetVolumePathName(source, volumePathName, MAX_PATH);

		if (source != volumePathName.ToString())
		{
			// Important: you must always ensure you back up the deduplication database, which is under
			// System Volume Information\Dedup and contains the actual file data
			// since we didn't back up the whole volume we need to back it up now

			string dedupDatabase = volumePathName.ToString().TrimEnd('\\') + SYSTEM_VOLUME_INFORMATION;
			string databaseDestination = destination + SYSTEM_VOLUME_INFORMATION;

			// Backup SVI folder
			BackupDirectory(dedupDatabase, databaseDestination);

			databaseDestination += DEDUP_FOLDER;
			dedupDatabase += DEDUP_FOLDER;

			// Backup the deduplication store
			BackupDirectoryTree(dedupDatabase, databaseDestination);
		}

		WriteBackupMetadata(source, destination);
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Full volume restore core
	//
	/////////////////////////////////////////////////////////////////////

	private static HRESULT RestoreVolume([In] string source, [In] string destination)
	{
		Console.WriteLine($"Restoring files from '{source}' to volume '{destination}'");

		string? sourceDedupStoreId = null, destinationDedupStoreId = null, backupTime = null;

		HRESULT hr = GetVolumeGuidNameForPath(destination, out var destinationVolumeGuidName);

		// Get the chunk store ID and backup timestamp from the backup metadata
		if (hr.Succeeded)
		{
			hr = ReadBackupMetadata(source, out sourceDedupStoreId, out backupTime);
		}

		// Check for deduplication metadata
		bool destinationHasDedupMetadata = false;
		if (hr.Succeeded)
		{
			hr = VolumeHasDedupMetadata(destinationVolumeGuidName, out destinationHasDedupMetadata, out destinationDedupStoreId);
		}

		if (hr.Succeeded && destinationHasDedupMetadata)
		{
			if (string.Equals(sourceDedupStoreId, destinationDedupStoreId, StringComparison.OrdinalIgnoreCase))
			{
				Console.WriteLine($"Restore is unsupported. Source deduplication store ID '{sourceDedupStoreId}' does not match destination ID '{destinationDedupStoreId}'.");
				hr = HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_NOT_SUPPORTED);
			}

			if (hr.Succeeded)
			{
				// Disable deduplication jobs on the volume to avoid disruption during restore
				hr = ToggleDedupJobs(destinationVolumeGuidName, false);
			}

			// Cancel deduplication jobs running/queued on the volume
			if (hr.Succeeded)
			{
				hr = CancelDedupJobs(destinationVolumeGuidName);
			}

			// Unoptimize files that changed since backup timestamp
			if (hr.Succeeded)
			{
				hr = UnoptimizeSinceTimestamp(destinationVolumeGuidName, backupTime!);
			}

			// Disable deduplication data access on the volume during restore
			// NOTE: this operation causes the destination volume to dismount/remount
			if (hr.Succeeded)
			{
				hr = ToggleDedupDataAccess(destinationVolumeGuidName, false);
			}

			// Delete the deduplication store
			if (hr.Succeeded)
			{
				hr = DeleteDedupStore(destinationVolumeGuidName);
			}
		}

		// Restore deduplication store
		if (hr.Succeeded)
		{
			hr = RestoreDedupStore(source, destination);
		}

		if (hr.Succeeded && !destinationHasDedupMetadata)
		{
			// Deduplication store is restored to a fresh volume
			// Jobs and data access need to be disabled to avoid disruption during restore

			// Disable deduplication jobs on the volume
			hr = ToggleDedupJobs(destinationVolumeGuidName, false);

			// Disable deduplication data access on the volume
			// NOTE: this operation causes the destination volume to dismount/remount
			if (hr.Succeeded)
			{
				hr = ToggleDedupDataAccess(destinationVolumeGuidName, false);
			}

			// Set state such that deduplication is reenabled after restore
			destinationHasDedupMetadata = true;
		}

		// Restore files on the volume
		if (hr.Succeeded)
		{
			// This sample restores files with source priority in cases where a file/directory
			// exists in both the backup store and the target volume
			// A real backup application might offer the option of target volume priority semantics
			hr = RestoreFiles(source, destination, true, out _);
		}

		// Reenable deduplication jobs and data access
		if (hr.Succeeded && destinationHasDedupMetadata)
		{
			// Enable deduplication data access on the volume
			// NOTE: this operation causes the destination volume to dismount/remount
			hr = ToggleDedupDataAccess(destinationVolumeGuidName, true);

			if (hr.Succeeded)
			{
				// Enable deduplication jobs on the volume
				hr = ToggleDedupJobs(destinationVolumeGuidName, true);
			}
		}

		// Run GC job
		if (hr.Succeeded)
		{
			hr = RunGarbageCollection(destinationVolumeGuidName);
		}

		if (hr.Succeeded)
		{
			Console.WriteLine($"Restore completed");
		}
		else
		{
			Console.WriteLine($"Restore completed with error {hr}");
		}

		return hr;
	}

	/*
	private static HRESULT GetDedupChunkStoreId([In] string volumeGuidName, [Out] string chunkStoreId)
	{
		HRESULT hr = HRESULT.S_OK;
		CComPtr<IWbemClassObject> spInstance;

		chunkStoreId.clear();

		// Returns S_FALSE if not found
		hr = WmiGetDedupInstanceByVolumeId(CIM_DEDUP_CLASS_VOLUME_METADATA, volumeGuidName, spInstance);

		if (hr == HRESULT.S_OK)
		{
			_variant_t var;

			// Get the value of the StoreId property
			hr = spInstance.Get(CIM_DEDUP_PROP_STOREID, 0, &var, 0, 0);
			if (hr.Failed)
			{
				Console.WriteLine($"IWbemClassObjectGet for property StoreId failed with error {hr}");
			}
			else
			{
				chunkStoreId = var.bstrVal;
			}
		}

		return hr;
	}

	private static HRESULT ToggleDedupJobs([In] string volumeGuidName, bool enableJobs)
	{
		string methodName = enableJobs ? CIM_DEDUP_METHOD_ENABLE : CIM_DEDUP_METHOD_DISABLE;

		// Setup for WMI method call - get WBEM services, input parameter object

		CComPtr<IWbemServices> spWmi;
		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		CComPtr<IWbemClassObject> spInParams;
		if (hr.Succeeded)
		{
			hr = WmiGetMethodInputParams(spWmi, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), spInParams);
		}

		if (hr.Succeeded)
		{
			hr = WmiAddVolumeInputParameter(spInParams, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), volumeGuidName);
		}

		if (hr.Succeeded)
		{
			CComPtr<IWbemClassObject> spOutParams;
			hr = WmiExecuteMethod(spWmi, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), spInParams, spOutParams, "Jobs");
		}

		return hr;
	}

	private static HRESULT ToggleDedupDataAccess([In] string volumeGuidName, bool enableDataAccess)
	{
		string methodName = enableDataAccess ? CIM_DEDUP_METHOD_ENABLE : CIM_DEDUP_METHOD_DISABLE;

		// Setup for WMI method call - get WBEM services, input parameter object
		CComPtr<IWbemServices> spWmi;
		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		CComPtr<IWbemClassObject> spInParams;
		if (hr.Succeeded)
		{
			hr = WmiGetMethodInputParams(spWmi, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), spInParams);
		}

		// Volume name parameter
		if (hr.Succeeded)
		{
			hr = WmiAddVolumeInputParameter(spInParams, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), volumeGuidName);
		}

		// DataAccess parameter
		if (hr.Succeeded)
		{
			variant_t dataAccessTrigger = true; ;

			hr = WmiAddInputParameter(spInParams, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), CIM_DEDUP_PROP_DATAACCESS, dataAccessTrigger);
		}

		// Execute method
		if (hr.Succeeded)
		{
			CComPtr<IWbemClassObject> spOutParams;
			hr = WmiExecuteMethod(spWmi, CIM_DEDUP_CLASS_VOLUME, methodName.c_str(), spInParams, spOutParams, "DataAccess");
		}

		return hr;
	}

	private static HRESULT CancelDedupJobs([In] string volumeGuidName)
	{
		CComPtr<IWbemServices> spWmi;
		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		// Get the job instance queued/running for the specified volume
		CComPtr<IEnumWbemClassObject> spInstances;
		if (hr.Succeeded)
		{
			hr = WmiQueryDedupInstancesByVolumeId(spWmi, CIM_DEDUP_CLASS_JOB, volumeGuidName.c_str(), spInstances);
		}

		// Stop each job
		while (hr.Succeeded)
		{

			CComPtr<IWbemClassObject> spInstance;
			uint uReturn;

			// Get a job instance
			hr = spInstances.Next(WBEM_INFINITE, 1, &spInstance, &uReturn);
			if (hr.Failed)
			{
				Console.WriteLine($"IEnumWbemClassObjectNext failed with error 0x{hex}" << hr);
				break;
			}

			if (uReturn == 0)
			{
				// All done
				break;
			}

			// Get the _PATH property (used as input to ExecMethod)
			variant_t varObjectPath;
			hr = spInstance.Get(CIM_DEDUP_PROP_PATH, 0, &varObjectPath, 0, 0);
			if (hr.Failed)
			{
				Console.WriteLine($"IWbemClassObjectGet for property {CIM_DEDUP_PROP_PATH} failed with error 0x{hex}" << hr);
				break;
			}

			// Call the Stop method
			if (hr.Succeeded)
			{
				// NOTE: the MSFT_DedupJob.Stop method takes no input parameters
				CComPtr<IWbemClassObject> spOutParams;
				hr = WmiExecuteMethod(spWmi, varObjectPath.bstrVal, CIM_DEDUP_METHOD_STOP, default, spOutParams);
				if (hr.Failed)
				{
					break;
				}
			}
		}

		return hr;
	}

	private static HRESULT RestoreDedupStoreDirectories([In] string source, [In] string destination)
	{
		string sourcePath = source;
		string destinationPath = destination;

		sourcePath += SYSTEM_VOLUME_INFORMATION;
		destinationPath += SYSTEM_VOLUME_INFORMATION;

		HRESULT hr = RestoreDirectory(sourcePath, destinationPath);

		if (hr.Succeeded || hr == HRESULT_FROM_WIN32(Win32Error.ERROR_FILE_EXISTS))
		{
			sourcePath += DEDUP_FOLDER;
			destinationPath += DEDUP_FOLDER;

			hr = RestoreDirectory(sourcePath, destinationPath);
		}

		if (hr == HRESULT_FROM_WIN32(Win32Error.ERROR_FILE_EXISTS))
		{
			hr = HRESULT.S_OK;
		}

		return hr;
	}

	private static HRESULT RestoreDedupStore([In] string source, [In] string destination)
	{
		HRESULT hr = RestoreDedupStoreDirectories(source, destination);

		if (hr.Succeeded)
		{
			string sourceDedupStorePath = BuildDedupStorePath(source);
			string destinationDedupStore = BuildDedupStorePath(destination);

			for (int index; hr.Succeeded && index < DEDUP_STORE_FOLDERS_COUNT; index++)
			{
				string sourceDedupPath = sourceDedupStorePath;
				sourceDedupPath.append(DEDUP_STORE_FOLDERS[index]);

				string destinationDedupPath = destinationDedupStore;
				destinationDedupPath.append(DEDUP_STORE_FOLDERS[index]);

				vector<string> excludePaths;
				hr = RestoreDirectoryTree(sourceDedupPath, destinationDedupPath, excludePaths, default);
			}
		}

		return hr;
	}

	private static HRESULT RestoreFiles([In] string source, [In] string destination, [In] bool isVolumeRestore, out string[] pRestoredFiles)
	{
		HRESULT hr = HRESULT.S_OK;

		// Restore files (exclude deduplication store)

		vector<string> excludePaths;
		excludePaths.push_back(BuildDedupStorePath(source));

		if (hr.Succeeded)
		{
			hr = RestoreDirectoryTree(source, destination, excludePaths, pRestoredFiles);
		}

		if (hr.Succeeded && !isVolumeRestore)
		{
			// NOTE: RestoreDirectoryTree also restores SVI folder
			// If the backed up directory was a volume root there may be files to restore under SVI folder
			// If the backed up directory was a folder then SVI should be cleaned up
			// Directory deletion failures ignored intentionally
			string dedupStorePath = TrimTrailingSeparator(destination, '\\');
			dedupStorePath.append(SYSTEM_VOLUME_INFORMATION);
			RemoveDirectory(dedupStorePath.c_str());
		}

		return hr;
	}

	private static HRESULT GetJobInstanceId([In] IWbemClassObject startJobOutParams, [Out] string jobId)
	{
		UNREFERENCED_PARAMETER(jobId);

		_variant_t varArray;

		jobId.clear();

		// Get the value of the StoreId property
		HRESULT hr = startJobOutParams.Get(CIM_DEDUP_PROP_JOB, 0, &varArray, 0, 0);
		if (hr.Failed)
		{
			Console.WriteLine($"IWbemClassObjectGet for property {CIM_DEDUP_PROP_JOB} failed with error 0x{hex}" << hr);
		}

		if (varArray.vt != (VT_ARRAY | VT_UNKNOWN))
		{
			Console.WriteLine($"VT Type is unexpected for property {CIM_DEDUP_PROP_JOB}");
			hr = HRESULT.E_FAIL;
		}

		if (varArray.ppunkVal is null || *(varArray.ppunkVal) is null)
		{
			Console.WriteLine($"Property {CIM_DEDUP_PROP_JOB} has a default value");
			hr = HRESULT.E_FAIL;
		}

		object unknown = default;

		if (hr.Succeeded)
		{
			long index;
			ref SAFEARRAY sa = varArray.parray;
			hr = SafeArrayGetElement(sa, &index, &unknown);
			if (hr.Failed)
			{
				Console.WriteLine($"SafeArrayGetElement failed with error 0x{hex}" << hr);
			}

			if (unknown is null)
			{
				Console.WriteLine($"Property {CIM_DEDUP_PROP_JOB} has a default value");
				hr = HRESULT.E_FAIL;
			}
		}

		if (hr.Succeeded)
		{
			CComPtr<IWbemClassObject> spJob;

			CComPtr<IUnknown> spUnknown = unknown;

			hr = spUnknown.QueryInterface(&spJob);
			if (hr.Failed)
			{
				Console.WriteLine($"IUnknownQueryInterface for IWbemClassObject failed with error 0x{hex}" << hr);
			}

			if (hr.Succeeded)
			{
				variant_t varJobId;

				HRESULT hr = spJob.Get(CIM_DEDUP_PROP_ID, 0, &varJobId, 0, 0);
				if (hr.Failed)
				{
					Console.WriteLine($"IWbemClassObjectGet for property {CIM_DEDUP_PROP_ID} failed with error 0x{hex}" << hr);
				}

				if (varJobId.vt != VT_BSTR)
				{
					Console.WriteLine($"VT Type is unexpected for property {CIM_DEDUP_PROP_JOB}");
					hr = HRESULT.E_FAIL;
				}

				if (hr.Succeeded)
				{
					jobId = varJobId.bstrVal;
				}
			}

		}

		return hr;
	}

	private static HRESULT GetEventData([In] EVT_HANDLE _event, string dataName, out object varData)
	{
		HRESULT hr = HRESULT.S_OK;

		StringBuilder eventDataXPath = new("Event/EventData/Data[@Name='");
		eventDataXPath.Append(dataName);
		eventDataXPath.Append("']");

		string[] values = [eventDataXPath.ToString()];

		EVT_HANDLE renderContext = EvtCreateRenderContext(values.Length, values, EvtRenderContextValues);

		if (renderContext is null)
		{
			hr = GetLastError().ToHRESULT();
			Console.WriteLine($"EvtCreateRenderContext failed with error {GetLastError()}");
		}

		if (hr.Succeeded)
		{
			PEVT_VARIANT properties = default;
			bool result = EvtRender(renderContext, _event, EvtRenderEventValues, 0, default, out var bufferUsed, out var propertyCount);

			if (!result)
			{
				var status = GetLastError();

				if (status == Win32Error.ERROR_INSUFFICIENT_BUFFER)
				{
					ushort[] eventDataBuffer = new ushort[1024]; // production code should use dynamic memory

					properties = (PEVT_VARIANT)eventDataBuffer;

					result = EvtRender(renderContext, _event, EvtRenderEventValues, Marshal.SizeOf(typeof(eventDataBuffer)), properties, out bufferUsed, out propertyCount);
				}
			}

			if (!result)
			{
				hr = GetLastError().ToHRESULT();
				Console.WriteLine($"EvtRender failed with error {GetLastError()}");
			}

			if (hr.Succeeded)
			{
				if (properties[0].Type == EvtVarTypeString)
				{
					varData = properties[0].StringVal;
				}
				else if (properties[0].Type == EvtVarTypeUInt32)
				{
					varData.ulVal = properties[0].UInt32Val;
					varData.vt = VT_UI4;
				}
				else
				{
					Console.WriteLine($"Conversion from {properties}"[0].Type << " not implemented");
					hr = HRESULT.E_FAIL;
				}
			}

			if (!(renderContext is null))
			{
				EvtClose(renderContext);
			}

		}

		return hr;
	}

	private static HRESULT DisplayUnoptimizationFileError([In] EVT_HANDLE _event)
	{
		string parentPath = "";
		string fileName = "";
		string errorMessage = "";
		uint errorCode = 0;

		HRESULT hr = GetEventData(_event, "ParentDirectoryPath", out var var);
		if (hr.Succeeded)
		{
			parentPath = (string)var;
		}

		hr = GetEventData(_event, "FileName", out var);
		if (hr.Succeeded)
		{
			fileName = (string)var;
		}

		hr = GetEventData(_event, "ErrorMessage", out var);
		if (hr.Succeeded)
		{
			errorMessage = (string)var;
		}

		hr = GetEventData(_event, "ErrorCode", out var);
		if (hr.Succeeded)
		{
			errorCode = (uint)var;
		}

		string filePath = parentPath + fileName;
		Console.WriteLine($"Unoptimization file error\nFile path: {filePath}\nError code: 0x{errorCode:X}\nError message: {errorMessage}");

		return hr;
	}

	private static HRESULT CheckForUnoptimizationFileErrors([In] string jobId, out bool foundErrors)
	{
		HRESULT hr = HRESULT.S_OK;
		foundErrors = false;

		// Query for errors from the specified job ID

		// Build the query string
		string eventQueryPrefix = "<QueryList> <Query Id=\"0\" Path=\"Microsoft-Windows-Deduplication/Operational\"> " +
			"<Select Path=\"Microsoft-Windows-Deduplication/Operational\">*[System[(EventID=6144)] " +
			"and[] EventData = new[] and = new new[Data[(@Name=\"JobInstanceId\")]=\"";

		string eventQuerySuffix = "\"]] </Select> </Query> </QueryList>";

		StringBuilder queryXml = new(eventQueryPrefix);
		queryXml.Append(jobId);
		queryXml.Append(eventQuerySuffix);

		// Execute the query
		EVT_HANDLE queryResult = EvtQuery(default,
		DEDUP_OPERATIONAL_EVENT_CHANNEL_NAME,
		queryXml.c_str(),
		//"Event/System[EventID=6144]",
		EvtQueryChannelPath | EvtQueryReverseDirection);

		if (queryResult is null)
		{
			var error = GetLastError();
			hr = error.ToHRESULT();
			Console.WriteLine($"EvtQuery for unoptimization file errors failed with error {error}");
		}

		// Process the returned events, if any
		while (hr.Succeeded)
		{
			uint eventsReturned;
			EVT_HANDLE eventHandle = default;

			bool bStatus = EvtNext(queryResult, 1, &eventHandle, INFINITE, 0, &eventsReturned);

			if (!bStatus)
			{
				uint error = GetLastError();

				if (error != Win32Error.ERROR_NO_MORE_ITEMS)
				{
					hr = error.ToHRESULT();
					Console.WriteLine($"EvtNext for unoptimization file error failed with error {error}");
				}
				else
				{
					break;
				}
			}
			else if (eventsReturned == 1)
			{
				DisplayUnoptimizationFileError(eventHandle);
				EvtClose(eventHandle);
				foundErrors = true;
			}
		}

		if (!(queryResult is null))
		{
			EvtClose(queryResult);
		}

		return hr;
	}

	private static HRESULT UnoptimizeSinceTimestamp([In] string volumeGuidName, [In] string backupTime)
	{
		// Setup for WMI method call - get WBEM services, input parameter object
		CComPtr<IWbemServices> spWmi;
		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		CComPtr<IWbemClassObject> spInParams;
		if (hr.Succeeded)
		{
			hr = WmiGetMethodInputParams(spWmi, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, spInParams);
		}

		// Volume parameter
		if (hr.Succeeded)
		{
			hr = WmiAddVolumeInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, volumeGuidName);
		}

		// Job type parameter
		if (hr.Succeeded)
		{
			// Unoptimization job is type=4 (from Data Deduplication MOF schema)
			variant_t var;
			var.lVal = CIM_DEDUP_JOB_TYPE_UNOPT;
			var.vt = VT_I4;
			hr = WmiAddInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, CIM_DEDUP_PROP_TYPE, var);
		}

		// Timestamp parameter
		if (hr.Succeeded)
		{
			variant_t var = backupTime.c_str();
			hr = WmiAddInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, CIM_DEDUP_PROP_TIMESTAMP, var);
		}

		// Job wait parameter for synchronous job
		// NOTE: real backup applications might run optimization job asynchronously, track progress, etc.
		if (hr.Succeeded)
		{
			variant_t var = (bool)true;
			hr = WmiAddInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, CIM_DEDUP_PROP_WAIT, var);
		}

		// Execute the unoptimization job
		string jobId;
		if (hr.Succeeded)
		{
			CComPtr<IWbemClassObject> spOutParams;
			hr = WmiExecuteMethod(spWmi, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, spInParams, spOutParams, "Unoptimization");

			// Get the job id for error tracking
			if (hr.Succeeded)
			{
				hr = GetJobInstanceId(spOutParams, jobId);
			}
		}

		// Check for file errors
		if (hr.Succeeded)
		{
			bool foundErrors = false;
			hr = CheckForUnoptimizationFileErrors(jobId, foundErrors);

			if (foundErrors is not null)
			{
				Console.WriteLine($"One or more files failed to unoptimize. Resolve the problems and retry the restore.");
				hr = HRESULT.E_FAIL;
			}
		}

		return hr;
	}

	private static HRESULT RunGarbageCollection([In] string volumeGuidName)
	{
		// Setup for WMI method call - get WBEM services, input parameter object
		CComPtr<IWbemServices> spWmi;
		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		CComPtr<IWbemClassObject> spInParams;
		if (hr.Succeeded)
		{
			hr = WmiGetMethodInputParams(spWmi, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, spInParams);
		}

		// Volume parameter
		if (hr.Succeeded)
		{
			hr = WmiAddVolumeInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, volumeGuidName);
		}

		// Job type parameter
		if (hr.Succeeded)
		{
			// GC job is type=2 (from Data Deduplication MOF schema)
			variant_t var;
			var.lVal = CIM_DEDUP_JOB_TYPE_GC;
			var.vt = VT_I4;
			hr = WmiAddInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, CIM_DEDUP_PROP_TYPE, var);
		}

		// Job wait parameter for synchronous job
		// NOTE: Backup applications are not required to wait for GC completion
		if (hr.Succeeded)
		{
			variant_t var = (bool)true;
			hr = WmiAddInputParameter(spInParams, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, CIM_DEDUP_PROP_WAIT, var);
		}


		// Execute the method
		// Job will be queued and execute asynchronously
		if (hr.Succeeded)
		{
			CComPtr<IWbemClassObject> spOutParams;
			hr = WmiExecuteMethod(spWmi, CIM_DEDUP_CLASS_JOB, CIM_DEDUP_METHOD_START, spInParams, spOutParams, "GarbageCollection");
		}
		return hr;
	}

	private static HRESULT DeleteDedupStore([In] string volume)
	{
		HRESULT hr = HRESULT.S_OK;

		string dedupStorePath = BuildDedupStorePath(volume);

		hr = DeleteDirectoryTree(dedupStorePath);

		return hr;
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Program main
	//
	/////////////////////////////////////////////////////////////////////

	int _cdecl tmain(int args.Length, [In] reads_(args.Length) ref TCHAR args[])
	{
		string source, destination;
		Action action;

		if (!ParseCommandLine(args.Length, args, &action, &source, &destination))
		{
			PrintUsage(args[0]);
			return 1;
		}

		HRESULT hr = CoInitializeEx(default, COINIT_MULTITHREADED);
		if (hr.Succeeded)
		{
			hr = CoInitializeSecurity(default,
			-1, // COM authentication
			default, // Authentication services
			default, // Reserved
			RPC_C_AUTHN_LEVEL_DEFAULT, // Default authentication 
			RPC_C_IMP_LEVEL_IMPERSONATE, // Default Impersonation 
			default, // Authentication info
			EOAC_NONE, // Additional capabilities 
			default // Reserved);

if (hr.Succeeded)
			{
				hr = ModifyPrivilege(SE_BACKUP_NAME, true);
			}

			if (hr.Succeeded)
			{
				hr = ModifyPrivilege(SE_RESTORE_NAME, true);
			}

			// Permit paths longer than MAX_PATH
			source = string(LONG_PATH_PREFIX) + source;
			destination = string(LONG_PATH_PREFIX) + destination;

			if (hr.Succeeded)
			{
				switch (action)
				{
					case BackupAction:
						DoBackup(source, destination);
						break;
					case RestoreStubAction:
						hr = RestoreStub(source, destination);
						break;
					case RestoreDataAction:
						hr = RestoreData(source, destination);
						break;
					case RestoreFileAction:
						hr = RestoreStub(source, destination);
						if (hr.Succeeded)
						{
							hr = RestoreData(source, destination);
						}
						break;
					case RestoreVolumeAction:
						hr = RestoreVolume(source, destination);
						break;
					case RestoreFilesAction:
						vector<string> restoredFiles;
						hr = RestoreFiles(source, destination, false, &restoredFiles);
						if (hr.Succeeded)
						{
							hr = RestoreFilesData(source, restoredFiles);
						}
						restoredFiles.clear();
						break;
				}
			}

			CoUninitialize();
		}

		if (hr.Failed) return 1;
		return 0;
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Backup/restore utilities
	//
	/////////////////////////////////////////////////////////////////////

	private static string BuildDedupStorePath([In] string volume)
	{
		string dedupStorePath = TrimTrailingSeparator(volume, '\\');

		dedupStorePath.append(SYSTEM_VOLUME_INFORMATION);
		dedupStorePath.append(DEDUP_FOLDER);

		return dedupStorePath;
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Backup related methods
	//
	/////////////////////////////////////////////////////////////////////

	private static HRESULT BackupFile([In] string source, [In] string destination)
	{
		// Open the source file
		HANDLE hSourceFile = CreateFile(source.c_str(),
		GENERIC_READ,
		FILE_SHARE_READ,
		default,
		OPEN_EXISTING,
		FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
		default);
		if (hSourceFile == INVALID_HANDLE_VALUE)
		{
			Console.WriteLine($"CreateFile({source} failed with error {GetLastError()}");
			return GetLastError().ToHRESULT();
		}

		// Open the backup medium
		// in this example the medium is another file, but it could be tape, network server, etc...
		HANDLE hDestinationFile = CreateFile(destination.c_str(),
		GENERIC_WRITE,
		FILE_SHARE_READ,
		default,
		CREATE_ALWAYS,
		FILE_FLAG_BACKUP_SEMANTICS,
		default);
		if (hDestinationFile == INVALID_HANDLE_VALUE)
		{
			Console.WriteLine($"CreateFile({destination}) failed with error {GetLastError()}");
			return GetLastError().ToHRESULT();
		}

		// 4k is the default NTFS cluster size, and small enough to fit into the stack
		// we could also readk 8k if we can afford the extra stack cost or allocate a heap buffer
		// and read 64k or more
		uint DEFAULT_BUFFER_SIZE = 4086;
		uint bytesRead, bytesWritten;
		byte[] buffer = new[] byte = new new[DEFAULT_BUFFER_SIZE];
		IntPtr context = default;
		HRESULT hr = HRESULT.S_OK;

		// Read 4k at a time. BackupRead will return attributes, security, and reparse point information
		while (BackupRead(hSourceFile, buffer, DEFAULT_BUFFER_SIZE, &bytesRead, false, true, &context) && bytesRead > 0)
		{
			// Save the data describing the source file to the destination medium.
			// we do a write file here, but if this would be a network server you could send on a socket
			if (!WriteFile(hDestinationFile, buffer, bytesRead, &bytesWritten, default))
			{
				Console.WriteLine($"WriteFile({destination}) failed with error {GetLastError()}");
				hr = GetLastError().ToHRESULT();
				break;
			}

			if (bytesRead != bytesWritten)
			{
				Console.WriteLine($"WriteFile({destination}) unexpectedly wrote less bytes than expected (expected:{bytesRead} written:{bytesWritten})");
				hr = HRESULT.E_UNEXPECTED;
				break;
			}
		}

		// Call BackupRead one more time to clean up the context
		BackupRead(hSourceFile, default, 0, default, true, true, &context);

		// Close the source file
		CloseHandle(hSourceFile);

		// Close the backup medium
		CloseHandle(hDestinationFile);

		return hr;
	}

	private static HRESULT BackupDirectory([In] string source, [In] string destination)
	{
		HRESULT hr = HRESULT.S_OK;

		// Create the corresponding directory in the destination
		if (!CreateDirectory(destination.c_str(), default))
		{
			var error = GetLastError();
			if (error != Win32Error.ERROR_ALREADY_EXISTS)
			{
				Console.WriteLine($"CreateDirectory({destination}) failed with error {GetLastError()}");
				hr = error.ToHRESULT();
			}
		}

		if (hr.Succeeded)
		{
			// Backup the directory
			StringBuilder directoryBackupPath = new(destination);
			directoryBackupPath.Append("\\");
			directoryBackupPath.Append(DIRECTORY_BACKUP_FILE);

			hr = BackupFile(source, directoryBackupPath);
		}

		return hr;
	}

	private static void BackupDirectoryTree([In] string source, [In] string destination)
	{
		// Backup the directory
		HRESULT hr = BackupDirectory(source, destination);
		if (hr.Failed)
		{
			return;
		}

		// Walk through all the files and subdirectories
		WIN32_FIND_DATA findData;
		string pattern = source;
		pattern += "\\*";
		HANDLE hFind = FindFirstFile(pattern.c_str(), &findData);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			do
			{
				// If not . or ..
				if (findData.cFileName[0] != '.')
				{
					string newSource = source;
					newSource += '\\';
					newSource += findData.cFileName;
					string newDestination = destination;
					newDestination += '\\';
					newDestination += findData.cFileName;
					// Backup the source file or directory
					if (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
					{
						// NOTE: This code is using recursion for simplicity, however a real backup application
						// should avoid recursion since it will overflow the stack for a deep directory tree
						BackupDirectoryTree(newSource, newDestination);
					}
					else
					{
						// Do BackupRead and backup the file
						hr = BackupFile(newSource, newDestination);
						if (hr.Failed)
						{
							// NOTE: This code ignores BackupFile errors,
							// but a backup app might handle it differently
							Console.WriteLine($"BackupFile failed, hr = 0x{hex}" << hr);
						}
					}
				}
			} while (FindNextFile(hFind, &findData));

			FindClose(hFind);
		}
	}

	private static HRESULT VolumeHasDedupMetadata([In] string volumeGuidName, out bool hasDedupMetadata, out string chunkStoreId)
	{
		chunkStoreId.clear();
		hasDedupMetadata = false;

		HRESULT hr = GetDedupChunkStoreId(volumeGuidName, chunkStoreId);

		if (hr.Succeeded && !chunkStoreId.empty())
		{
			hasDedupMetadata = true;
		}

		return hr;
	}

	private static void WriteBackupMetadata([In] string source, [In] string destination)
	{
		string volumeGuidName;
		string chunkStoreId;

		HRESULT hr = GetVolumeGuidNameForPath(source, volumeGuidName);

		// Get the deduplication chunk store ID
		if (hr.Succeeded)
		{
			hr = GetDedupChunkStoreId(volumeGuidName.c_str(), chunkStoreId);
		}

		// Write the store ID and backup timestamp to the backup metadata file
		if (hr.Succeeded && !chunkStoreId.empty())
		{
			string filePath = destination;
			filePath.append("\\");
			filePath.append(BACKUP_METADATA_FILE_NAME);

			ref FILE metadataFile = default;
			errno_t err = wfopen_s(&metadataFile, filePath.c_str(), "w");
			if (err != 0)
			{
				Console.WriteLine($"Unable to create backup metadata file: {filePath}");
			}
			else
			{
				// A real backup application would use the VSS snapshot timestamp
				FILETIME ftNow = default;
				GetSystemTimeAsFileTime(&ftNow);

				// Convert time to wbem time string
				WBEMTime backupTime(ftNow);
				bstr_t bstrTime = backupTime.GetDMTF();

				_ftprintf(metadataFile, BACKUP_METADATA_FORMAT, chunkStoreId.c_str(), (string)bstrTime);
				fclose(metadataFile);
			}
		}
	}

	private static HRESULT ReadBackupMetadata([In] string source, out string chunkStoreId, out string backupTime)
	{
		HRESULT hr = HRESULT.S_OK;
		string filePath = source;

		backupTime.clear();
		chunkStoreId.clear();

		filePath.append("\\");
		filePath.append(BACKUP_METADATA_FILE_NAME);

		ref FILE metadataFile = default;
		errno_t err = wfopen_s(&metadataFile, filePath.c_str(), "r");
		if (err != 0)
		{
			Console.WriteLine($"Unable to open backup metadata file: {filePath}");
			hr = HRESULT.E_FAIL;
		}
		else
		{
			ushort storeId[] = "{00000000-0000-0000-0000-000000000000}";
			ushort timestamp[] = "yyyymmddhhmmss.nnnnnn-ggg";

			int converted = fwscanf_s(metadataFile, BACKUP_METADATA_FORMAT, storeId, ARRAY_LEN(storeId), timestamp, ARRAY_LEN(timestamp));
			if (converted != 2)
			{
				Console.WriteLine($"Unable to read chunk store ID and backup time from backup metadata file: {filePath}");
			}
			else
			{
				chunkStoreId = storeId;
				backupTime = timestamp;
			}
			fclose(metadataFile);
		}

		return hr;
	}

	/////////////////////////////////////////////////////////////////////
	//
	// Restore related methods
	//
	/////////////////////////////////////////////////////////////////////

	private static HRESULT RestoreFile([In] string source, [In] string destination, [In, Optional] bool overWriteExisting)
	{
		// Create destination dir if it doesn't exist
		SizeT lastSeparator = destination.rfind('\\');
		if (lastSeparator == stringnpos)
		{
			Console.WriteLine($"destination is not a file path");
			return HRESULT.E_UNEXPECTED;
		}
		string destinationLocation = destination.substr(0, lastSeparator);

		CreateDirectory(destinationLocation.c_str(), default);

		// Open the backup medium
		// In this example the medium is another file, but it could be tape, network server, etc...
		HANDLE hSourceFile = CreateFile(source.c_str(),
		GENERIC_READ,
		FILE_SHARE_READ,
		default,
		OPEN_EXISTING,
		FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
		default);
		if (hSourceFile == INVALID_HANDLE_VALUE)
		{
			Console.WriteLine($"CreateFile({source}) failed with error {GetLastError()}");
			return GetLastError().ToHRESULT();
		}

		// Open the file to be restored
		HANDLE hDestinationFile = CreateFile(destination.c_str(),
		GENERIC_WRITE | WRITE_OWNER | WRITE_DAC,
		FILE_SHARE_READ,
		default,
		overWriteExisting ? OPEN_EXISTING : CREATE_ALWAYS,
		FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
		default);
		if (hDestinationFile == INVALID_HANDLE_VALUE)
		{
			Console.WriteLine($"CreateFile({destination}) failed with error {GetLastError()}");
			return GetLastError().ToHRESULT();
		}

		uint DEFAULT_BUFFER_SIZE = 4086;
		uint bytesRead, bytesWritten;
		byte[] buffer = new[] byte = new new[DEFAULT_BUFFER_SIZE];
		IntPtr context = default;
		HRESULT hr = HRESULT.S_OK;

		// Read 4k at a time from the backup medium. BackupRead will return attributes, security, and reparse point information
		while (ReadFile(hSourceFile, buffer, DEFAULT_BUFFER_SIZE, &bytesRead, default) && bytesRead > 0)
		{
			// Call BackupWrite to restore the file, including security, attributes and reparse point
			if (!BackupWrite(hDestinationFile, buffer, bytesRead, &bytesWritten, false, true, &context))
			{
				Console.WriteLine($"BackupWrite({destination}) failed with error {GetLastError()}");
				hr = GetLastError().ToHRESULT();
				break;
			}

			if (bytesRead != bytesWritten)
			{
				Console.WriteLine($"BackupWrite({destination}) unexpectedly wrote less bytes than expected (expected:{bytesRead} written:{bytesWritten})");
				hr = HRESULT.E_UNEXPECTED;
				break;
			}
		}

		// Call BackupWrite one more time to clean up the context
		BackupWrite(hDestinationFile, default, 0, default, true, true, &context);

		// Close the backup medium
		CloseHandle(hSourceFile);

		// Close the destination file
		CloseHandle(hDestinationFile);

		return hr;
	}

	private static HRESULT RestoreDirectory([In] string source, [In] string destination)
	{
		HRESULT hr = HRESULT.S_OK;

		string directorySourcePath = source;
		directorySourcePath.append("\\");
		directorySourcePath.append(DIRECTORY_BACKUP_FILE);

		// Create the corresponding directory in the destination, if not already present
		if (!CreateDirectory(destination.c_str(), default))
		{
			uint error = GetLastError();
			// Access denied error can occur for volume root directory
			// The sample code also exempts access denied error for all other directories
			// A real backup application may handle this condition differently
			if (error != Win32Error.ERROR_ALREADY_EXISTS && error != Win32Error.ERROR_ACCESS_DENIED)
			{
				Console.WriteLine($"CreateDirectory({destination}) failed with error {GetLastError()}");
				hr = error.ToHRESULT();
			}
		}

		if (hr.Succeeded)
		{
			// Restore the directory
			RestoreFile(directorySourcePath, destination, true);
		}

		return hr;
	}

	private static HRESULT DeleteDirectoryTree([In] string directory)
	{
		HRESULT hr = HRESULT.S_OK;

		// Walk through all the files and subdirectories
		WIN32_FIND_DATA findData;
		string pattern = directory;
		pattern += "\\*";
		HANDLE hFind = FindFirstFile(pattern.c_str(), &findData);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			do
			{
				// If not . or ..
				if (findData.cFileName[0] != '.')
				{
					string newPath = directory;
					newPath += '\\';
					newPath += findData.cFileName;

					// Backup the source file or directory
					if (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
					{
						// NOTE: This code is using recursion for simplicity, however a real backup application
						// should avoid recursion since it will overflow the stack for a deep directory tree
						hr = DeleteDirectoryTree(newPath);
						if (hr.Failed)
						{
							break;
						}

						// NOTE: This code is ignoring directory deletion failures since the same tree will be restored anyway.
						// real backup applications migh choose to handle this differently.
						RemoveDirectory(newPath.c_str());
					}
					else
					{
						// Do BackupRead and backup the file
						bool result = DeleteFile(newPath.c_str());
						if (!result)
						{
							hr = GetLastError().ToHRESULT();
							Console.WriteLine($"DeleteFile failed, hr = 0x{hex}" << hr);
							break;
						}
					}
				}
			} while (FindNextFile(hFind, &findData));

			FindClose(hFind);
		}

		return hr;
	}

	private static HRESULT RestoreDirectoryTree([In] string source, [In] string destination, [In] string[] sourceExcludePaths, [Out] opt_ vector<string>* pRestoredFiles)
	{
		HRESULT hr = HRESULT.S_OK;

		if (!(pRestoredFiles is null))
		{
			pRestoredFiles.clear();
		}

		// Check for exclusion
		for (SizeT index; index < sourceExcludePaths.Length; index++)
		{
			string excludePath = sourceExcludePaths[index];
			if (wcscmp(source.c_str(), excludePath.c_str()) == 0)
			{
				return hr = S_FALSE;
			}
		}

		// Restore the directory
		hr = RestoreDirectory(source, destination);
		if (hr.Failed && hr != HRESULT_FROM_WIN32(Win32Error.ERROR_FILE_EXISTS) && hr != HRESULT_FROM_WIN32(Win32Error.ERROR_ACCESS_DENIED))
		{
			return hr;
		}
		hr = HRESULT.S_OK;

		string trimmedSource = TrimTrailingSeparator(source, '\\');

		// Walk through all the files and subdirectories
		WIN32_FIND_DATA findData;
		string pattern = trimmedSource;
		pattern += "\\*";
		HANDLE hFind = FindFirstFile(pattern.c_str(), &findData);
		if (hFind != INVALID_HANDLE_VALUE)
		{
			do
			{
				// If not . or ..
				if (findData.cFileName[0] != '.')
				{
					string newSource = trimmedSource;
					newSource += '\\';
					newSource += findData.cFileName;
					string newDestination = destination;
					newDestination += '\\';
					newDestination += findData.cFileName;
					// Restore the file or directory
					if (findData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
					{

						// NOTE: This code is using recursion for simplicity, however a real backup application
						// should avoid recursion since it will overflow the stack for a deep directory tree
						hr = RestoreDirectoryTree(newSource, newDestination, sourceExcludePaths, pRestoredFiles);
						if (hr.Failed)
						{
							break;
						}
					}
					else if ((_wcsicmp(findData.cFileName, DIRECTORY_BACKUP_FILE) == 0) ||
					(_wcsicmp(findData.cFileName, BACKUP_METADATA_FILE_NAME) == 0))
					{
						// This file is backup metadata, not original volume data
						continue;
					}
					else
					{
						// Restore the file
						hr = RestoreFile(newSource, newDestination);
						if (hr.Failed)
						{
							if (hr == HRESULT_FROM_WIN32(Win32Error.ERROR_ACCESS_DENIED) || hr == HRESULT_FROM_WIN32(Win32Error.ERROR_SHARING_VIOLATION))
							{
								// Some files may be in busy, protected by a filter, etc.
								// An online restore may not be able to replace every file
								Console.WriteLine($"Warning: continuing restore after RestoreFile failed, hr = 0x{hex}" << hr);
								hr = HRESULT.S_OK;
							}
							else
							{
								Console.WriteLine($"RestoreFile failed, hr = 0x{hex}" << hr);
								break;
							}
						}
						if (pRestoredFiles is not null)
						{
							pRestoredFiles.push_back(newDestination);
						}
					}
				}
			} while (FindNextFile(hFind, &findData));

			FindClose(hFind);
		}

		return hr;
	}

	/////////////////////////////////////////////////////////////////////
	//
	// WMI utilities
	//
	/////////////////////////////////////////////////////////////////////

	private static HRESULT WmiGetWbemServices(string wmiNamespace, [Out] CComPtr<IWbemServices>& spWmi)
	{
		CComPtr<IWbemLocator> spLocator = default;

		bstr_t bstrNamespace = wmiNamespace;

		HRESULT hr = CoCreateInstance(CLSID_WbemLocator,
		0,
		CLSCTX_INPROC_SERVER,
		typeof(IWbemLocator).Guid,
		(ref IntPtr) & spLocator);
		if (hr.Failed)
		{
			Console.WriteLine($"CoCreateInstance(IWbemLocator) failed, hr = 0x{hex}" << hr);
		}

		if (hr.Succeeded)
		{
			hr = spLocator.ConnectServer(bstrNamespace,
			default,
			default,
			default,
			0L,
			default,
			default,
			&spWmi);
			if (hr.Failed)
			{
				Console.WriteLine($"unable to connect to WMI; namespace {CIM_DEDUP_NAMESPACE}, hr = 0x{hex}" << hr);
			}
		}

		if (hr.Succeeded)
		{
			hr = CoSetProxyBlanket(spWmi,
			RPC_C_AUTHN_WINNT,
			RPC_C_AUTHZ_NONE,
			default,
			RPC_C_AUTHN_LEVEL_PKT,
			RPC_C_IMP_LEVEL_IMPERSONATE,
			default,
			EOAC_NONE);

			if (hr.Failed)
			{
				Console.WriteLine($"CoSetProxyBlanket failed, hr = 0x{hex}" << hr);
			}
		}

		return hr;
	}

	private static HRESULT WmiGetMethodInputParams([In] IWbemServices pWmi, string className, string methodName, [Out] CComPtr<IWbemClassObject>& spInParams)
	{
		CComPtr<IWbemClassObject> spClass;

		spInParams = default;

		HRESULT hr = pWmi.GetObject(bstr_t(className), 0, default, &spClass, default);
		if (hr.Failed)
		{
			Console.WriteLine($"WMI query for class {className} failed with error 0x{hex}" << hr);
		}

		CComPtr<IWbemClassObject> spInParamsDefinition;
		if (hr.Succeeded)
		{
			hr = spClass.GetMethod(methodName, 0, &spInParamsDefinition, default);
			if (hr.Failed)
			{
				Console.WriteLine($"WMI query for method {className}." << (string)methodName << " failed with error 0x{hex}" << hr);
			}

			// GetMethod returns default for method that takes no input parameters.
		}

		if (hr.Succeeded && (!(spInParamsDefinition is null)))
		{
			hr = spInParamsDefinition.SpawnInstance(0, &spInParams);
			if (hr.Failed)
			{
				Console.WriteLine($"WMI input parameter creation for method {className}." << (string)methodName << " failed with error 0x{hex}" << hr);
			}
		}
		else if (spInParamsDefinition is null)
		{
			hr = S_FALSE;
		}

		return hr;
	}

	private static HRESULT WmiAddVolumeInputParameter([In, Out] IWbemClassObject pParams, string className, string methodName, [In] string volume)
	{
		HRESULT hr = HRESULT.S_OK;
		ref SAFEARRAY psa = default;
		SAFEARRAYBOUND saBound = default;
		saBound.lLbound;
		saBound.cElements = 1;

		psa = SafeArrayCreate(VT_BSTR, 1, &saBound);
		if (psa is null)
		{
			Console.WriteLine($"Out of memory creating safe-array");
			hr = HRESULT.E_OUTOFMEMORY;
		}

		if (hr.Succeeded)
		{

			long index;
			hr = SafeArrayPutElement(psa, &index, ([MarshalAs(UnmanagedType.BStr)] string)bstr_t(volume.c_str()));
			if (hr.Failed)
			{
				Console.WriteLine($"SafeArrayPutElement(0, {volume}) failed with error 0x{hex}" << hr);
			}
		}

		VARIANT volumeArray;
		VariantInit(&volumeArray);
		volumeArray.vt = VT_ARRAY | VT_BSTR;
		volumeArray.parray = psa;

		if (hr.Succeeded)
		{
			hr = pParams.Put(bstr_t(CIM_DEDUP_PROP_VOLUME), 0, &volumeArray, 0);
			if (hr.Failed)
			{
				Console.WriteLine($"Setting property {className}.{methodName}.{CIM_DEDUP_PROP_VOLUME} failed with error 0x{hex}" << hr);
			}
		}

		if (!(psa is null))
		{
			SafeArrayDestroy(psa);
		}

		return hr;
	}

	private static HRESULT WmiAddInputParameter([In, Out] IWbemClassObject pParams, string className, string methodName, string propertyName, [In] variant_t& var)
	{
		HRESULT hr = HRESULT.S_OK;

		if (hr.Succeeded)
		{
			hr = pParams.Put(bstr_t(propertyName), 0, &var, 0);
			if (hr.Failed)
			{
				Console.WriteLine($"Setting property {className}.{methodName}.{propertyName} failed with error 0x{hex}" << hr);
			}
		}

		return hr;
	}

	private static HRESULT WmiGetErrorInfo([Out] HRESULT& hrOperation, [Out] string errorMessageOperation)
	{
		CComPtr<IErrorInfo> spErrorInfo;
		CComPtr<IWbemClassObject> spWmiError;

		HRESULT hr = GetErrorInfo(0, &spErrorInfo);
		if (hr.Failed)
		{
			Console.WriteLine($"GetErrorInfo failed with error 0x{hex}" << hr);
		}

		if (hr.Succeeded)
		{
			hr = spErrorInfo.QueryInterface(&spWmiError);
			if (hr.Failed)
			{
				Console.WriteLine($"IErrorInfoQueryInterface failed with error 0x{hex}" << hr);
			}
		}

		if (hr.Succeeded)
		{
			variant_t var;

			// Get the value of the ErrorCode property
			hr = spWmiError.Get(CIM_DEDUP_PROP_ERRORCODE, 0, &var, 0, 0);
			if (hr.Failed)
			{
				Console.WriteLine($"IWbemClassObjectGet for property {CIM_DEDUP_PROP_ERRORCODE} failed with error 0x{hex}" << hr);
			}
			else
			{
				// This is the root cause error
				hrOperation = var.ulVal;

				// Get the error message
				hr = spWmiError.Get(CIM_DEDUP_PROP_ERRORMESSAGE, 0, &var, 0, 0);
				if (hr.Failed)
				{
					Console.WriteLine($"IWbemClassObjectGet for property {CIM_DEDUP_PROP_ERRORMESSAGE} failed with error 0x{hex}" << hr);
				}
				else
				{
					errorMessageOperation = var.bstrVal;
				}
			}
		}

		return hr;
	}

	private static HRESULT WmiExecuteMethod([In] IWbemServices pWmi, string className, string methodName, [In, Optional] IWbemClassObject pInParams,
	[Out] CComPtr<IWbemClassObject>& spOutParams, [In, Optional] string context)
	{
		// Make the method call
		HRESULT hr = pWmi.ExecMethod(bstr_t(className), bstr_t(methodName), 0, default, pInParams, &spOutParams, default);

		// Evaluate the output parameter object
		if (hr.Succeeded)
		{
			variant_t var;
			hr = spOutParams.Get(bstr_t(CIM_DEDUP_PROP_RETURNVALUE), 0, &var, default, 0);
			if (hr.Failed)
			{
				Console.WriteLine($"Get method return value for {className}.{methodName}({context})" << " failed with error 0x{hex}" << hr);
			}

			if (hr.Succeeded && FAILED(var.ulVal))
			{
				hr = var.ulVal;
				Console.WriteLine($"WMI method {className}.{methodName}({context})" << " failed with error 0x{hex}" << hr);
			}
		}
		else
		{
			// Get the root cause error
			HRESULT hrLocal = HRESULT.S_OK;
			HRESULT hrOperation = HRESULT.S_OK;
			string errorMessage;

			hrLocal = WmiGetErrorInfo(hrOperation, errorMessage);

			if (hrLocal.Succeeded && hrOperation.Failed)
			{
				hr = hrOperation;
				Console.WriteLine($"WMI method execution {className}.{methodName}({context})" << " failed with error 0x{hex}" << hr);
				Console.WriteLine($" Error message: {errorMessage}");
			}
		}

		return hr;
	}

	private static HRESULT WmiGetDedupInstanceByVolumeId(string className, [In] string volumeGuidName, out IWbemClassObject instance)
	{
		ManagementPath spWmi = new(className)
		CComPtr<IWbemClassObject> spInstance;

		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		if (hr.Succeeded)
		{
			string objectPath = className;
			objectPath.append(".VolumeId='");
			objectPath.append(volumeGuidName);
			objectPath.append("'");

			// Get the specified object
			hr = spWmi.GetObject(bstr_t(objectPath.c_str()),
			WBEM_FLAG_RETURN_WBEM_COMPLETE,
			default, // context
			&spInstance,
			default // synchronous call; not needed);

if (hr.Failed)
			{
				// Get the root cause error
				HRESULT hrLocal = HRESULT.S_OK;
				HRESULT hrOperation = HRESULT.S_OK;
				string errorMessage;

				hrLocal = WmiGetErrorInfo(hrOperation, errorMessage);

				if (hrLocal.Succeeded && hrOperation.Failed)
				{
					if (hrOperation == DDP_E_NOT_FOUND || hrOperation == DDP_E_PATH_NOT_FOUND || hrOperation == DDP_E_VOLUME_DEDUP_DISABLED)
					{
						// The object is not found
						hr = S_FALSE;
					}
					else
					{
						hr = hrOperation;
						Console.WriteLine($"WMI query for {objectPath} failed with error 0x{hex}" << hr);
						Console.WriteLine($" Error message: {errorMessage}");
					}
				}
			}
		}

		if (hr.Succeeded)
		{
			instance = spInstance;
		}

		return hr;
	}

	private static HRESULT WmiQueryDedupInstancesByVolumeId([In] IWbemServices pWmi, [In] string className, [In] string volumeGuidName, [Out] CComPtr<IEnumWbemClassObject>& instances)
	{
		CComPtr<IWbemServices> spWmi;

		HRESULT hr = WmiGetWbemServices(CIM_DEDUP_NAMESPACE, spWmi);

		// Backslash chars need to be escaped in WMI queries
		string quotedVolumeGuidName = volumeGuidName;
		StringReplace(quotedVolumeGuidName, "\\", "\\\\");

		string query = "ref select from ";
		query.append(className);
		query.append(" where VolumeId='");
		query.append(quotedVolumeGuidName);
		query.append("'");

		hr = pWmi.ExecQuery(bstr_t("WQL"),
		bstr_t(query.c_str()),
		WBEM_FLAG_FORWARD_ONLY | WBEM_FLAG_RETURN_IMMEDIATELY,
		default,
		&instances);

		if (hr.Failed)
		{
			Console.WriteLine($"WMI query failed with error 0x{hex}" << hr << ", query: {query}");
		}

		return hr;
	}


	/////////////////////////////////////////////////////////////////////
	//
	// Common utilities
	//
	/////////////////////////////////////////////////////////////////////

	private static void StringReplace([In, Out] string stringValue, [In] string matchValue, [In] string replaceValue)
	{
		if (!stringValue.empty())
		{
			if (matchValue.compare(replaceValue) != 0)
			{
				stringsize_type pos = stringValue.find(matchValue, 0);

				while (pos != stringnpos)
				{

					stringValue.replace(pos,
					matchValue.length(),
					replaceValue);

					pos = stringValue.find(matchValue,
					pos + replaceValue.length());
				}
			}
			else
			{
				// If the two values are the same, we don't need to replace anything
			}
		}
	}

	private static string TrimTrailingSeparator([In] string str, ushort separator)
	{
		string returnString = str;

		stdstringsize_type pos = returnString.find_last_not_of(separator);

		if (pos != stdstringnpos)
		{
			++pos;
		}
		else
		{
			pos;
		}

		if (pos < returnString.length())
		{
			returnString.erase(pos);
		}

		return returnString;
	}

	private static bool IsRootPath([In] string path)
	{

		ushort[] volumePath = new[] ushort = new new[MAX_PATH];
		bool isRootPath = false;

		if (GetVolumePathName(path.c_str(), volumePath, MAX_PATH))
		{
			// For volume root, input length will equal output length (ignoring 
			// trailing backslash if any)

			SizeT cchPath = wcslen(path.c_str());
			SizeT cchRoot = wcslen(volumePath);

			if (volumePath[cchRoot - 1] == '\\')
				cchRoot--;
			if (path.c_str()[cchPath - 1] == '\\')
				cchPath--;

			if (cchPath == cchRoot)
			{
				isRootPath = true;
			}
		}

		return isRootPath;
	}

	private static void PrintUsage(string programName)
	{
		Console.WriteLine($"BACKUP");
		Console.WriteLine($"Backup a directory to a destination directory:") <<
		"\t{programName} -backup <directory-or-volume-path> -destination <directory-path>{endl}" << endl;
		Console.WriteLine($"EXAMPLE: {programName} -backup d:\\mydirectory -destination f:\\mydirectorybackup");
		Console.WriteLine($"EXAMPLE: {programName} -backup d:\\ -destination f:\\mydirectorybackup");

		Console.WriteLine(endl << "RESTORE");
		Console.WriteLine($"Restore a backed up directory to a destination directory:") <<
		"\t{programName} -restore <backup-directory-path> -destination <directory-path>{endl}" << endl;
		Console.WriteLine($"EXAMPLE: {programName} -restore f:\\mydirectorybackup -destination d:\\mydirectory");

		Console.WriteLine(endl << "SINGLE FILE RESTORE");
		Console.WriteLine($"Restore the reparse point to the destination:") <<
		"\t{programName} -restorestub <backup-file-path> -destination <file-path>{endl}" << endl;
		Console.WriteLine($"Restore the data for the reparse point restored with the -restorestub option above:") <<
		"\t{programName} -restoredata <backup-file-path> -destination <stub-path>{endl}" << endl;
		Console.WriteLine($"Restore the reparse point and data in one operation (-restorestub + -restoredata):") <<
		"\t{programName} -restorefile <backup-file-path> -destination <file-path>{endl}" << endl;
		Console.WriteLine($"EXAMPLE: {programName} -restorefile f:\\mydirectorybackup\\myfile -destination d:\\temp\\myfile");

		Console.WriteLine(endl << "VOLUME RESTORE");
		Console.WriteLine($"Restore the entire volume to the destination:") <<
		"\t{programName} -restorevolume <backup-directory-path> -destination <volume-path>{endl}" << endl;
		Console.WriteLine($"EXAMPLE: {programName} -restorevolume f:\\mydirectorybackup -destination d:\\");
	}

	private static bool ParseCommandLine(int args.Length, [In] reads_(args.Length) ref TCHAR args[], out Action action, out string source, out string destination)
	{

		if (action is null || source is null || destination is null)
		{
			return false;
		}

		*action = BackupAction;
		*source = "";
		*destination = "";

		if (args.Length < 5) return false;
		// args[0] is the program name, skip it

		// args[1] is the command, must be one of the following
		string actionString = args[1];
		if (actionString == "-backup")
		{
			*action = BackupAction;
		}

		else if (actionString == "-restorestub")
		{
			*action = RestoreStubAction;
		}

		else if (actionString == "-restoredata")
		{
			*action = RestoreDataAction;
		}

		else if (actionString == "-restorefile")
		{
			*action = RestoreFileAction;
		}

		else if (actionString == "-restorevolume")
		{
			*action = RestoreVolumeAction;
		}

		else if (actionString == "-restore")
		{
			*action = RestoreFilesAction;
		}

		else
		{
			return false;
		}

		// args[2] is the ref source source = args[2];
		// args[4] is the ref destination destination = args[4];

		// args[3] is always "-destination", check it for completeness
		string arg3 = args[3];
		if (arg3 != "-destination") return false;

		return true;
	}

	private static HRESULT ModifyPrivilege(string szPrivilege, bool fEnable)
	{
		HRESULT hr = HRESULT.S_OK;
		TOKEN_PRIVILEGES NewState = default;
		LUID luid = default;
		HANDLE hToken = default;

		// Open the process token for this process.
		if (!OpenProcessToken(GetCurrentProcess(),
		TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY,
		&hToken))
		{
			Console.WriteLine($"Failed OpenProcessToken");
			return Win32Error.ERROR_FUNCTION_FAILED;
		}

		// Get the local unique ID for the privilege.
		if (!LookupPrivilegeValue(default,
		szPrivilege,
		&luid))
		{
			CloseHandle(hToken);
			Console.WriteLine($"Failed LookupPrivilegeValue");
			return Win32Error.ERROR_FUNCTION_FAILED;
		}

		// Assign values to the TOKEN_PRIVILEGE structure.
		NewState.PrivilegeCount = 1;
		NewState.Privileges[0].Luid = luid;
		NewState.Privileges[0].Attributes =
		(fEnable ? SE_PRIVILEGE_ENABLED : 0);

		// Adjust the token privilege.
		if (!AdjustTokenPrivileges(hToken,
		false,
		&NewState,
		0,
		default,
		default))
		{
			Console.WriteLine($"Failed AdjustTokenPrivileges");
			hr = Win32Error.ERROR_FUNCTION_FAILED;
		}

		// Close the handle.
		CloseHandle(hToken);

		return hr;
	}

	private static HRESULT GetVolumeGuidNameForPath([In] string path, out string volumeGuidName)
	{
		HRESULT hr = HRESULT.S_OK;

		ushort[] volumePathName = new[] ushort = new new[MAX_PATH];
		bool result = GetVolumePathName(path.c_str(), volumePathName, MAX_PATH);
		if (!result)
		{
			Console.WriteLine($"GetVolumePathName({path}) failed with error {GetLastError()}");
			hr = GetLastError().ToHRESULT();
		}

		if (hr.Succeeded)
		{
			ushort[] tempVolumeGuidName = new[] ushort = new new[MAX_PATH];
			result = GetVolumeNameForVolumeMountPoint(volumePathName, tempVolumeGuidName, MAX_PATH);
			if (!result)
			{
				Console.WriteLine($"GetVolumeNameForVolumePathName({volumePathName}) failed with error {GetLastError()}");
				hr = GetLastError().ToHRESULT();
			}

			volumeGuidName = tempVolumeGuidName;
		}

		return hr;
	}

	private static HRESULT GetFileSize([In] string FilePath, out long FileSize)
	{
		HRESULT hr = HRESULT.S_OK;

		HANDLE fileHandle =

		CreateFile(FilePath.c_str(),
		FILE_READ_ATTRIBUTES,
		FILE_SHARE_READ,
		default,
		OPEN_EXISTING,
		FILE_ATTRIBUTE_NORMAL | FILE_FLAG_BACKUP_SEMANTICS,
		default);

		if (fileHandle == INVALID_HANDLE_VALUE)
		{
			hr = HRESULT_FROM_WIN32(GetLastError());

			Console.WriteLine($"CreateFile(%s{FilePath}) failed with error {GetLastError()}");
		}
		else
		{
			if (!GetFileSizeEx(fileHandle, &FileSize))
			{
				hr = HRESULT_FROM_WIN32(GetLastError());

				Console.WriteLine($"GetFileSizeEx(%s{FilePath}) failed with error {GetLastError()}");
			}
		}

		return hr;
	}

	*/
}