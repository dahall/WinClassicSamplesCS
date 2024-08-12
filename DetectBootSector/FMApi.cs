#pragma warning disable CA1401 // P/Invokes should not be visible
using System.Runtime.Versioning;
using Vanara.InteropServices;

namespace Vanara.PInvoke;

/// <summary>Functions, constants, and structures for File Management API.</summary>
public static partial class FMApi
{
	/// <summary>The size of the boot sector array supplied to <see cref="DetectBootSector"/>.</summary>
	public const int BOOT_SECTOR_SIZE = 512;

	/// <summary>Used to define the major version number of the file restore functions.</summary>
	public const int FILE_RESTORE_MAJOR_VERSION_1 = 0x0001;

	/// <summary>Used to define the major version number of the file restore functions.</summary>
	public const int FILE_RESTORE_MAJOR_VERSION_2 = 0x0002;

	/// <summary>Used to define the minor version number of the file restore functions.</summary>
	public const int FILE_RESTORE_MINOR_VERSION_1 = 0x0001;

	/// <summary>Used to define the minor version number of the file restore functions.</summary>
	public const int FILE_RESTORE_MINOR_VERSION_2 = 0x0000;

	/// <summary>Used to define the complete version number of the file restore functions.</summary>
	public const int FILE_RESTORE_VERSION_1 = (FILE_RESTORE_MAJOR_VERSION_1 << 16) | FILE_RESTORE_MINOR_VERSION_1;

	/// <summary>Used to define the complete version number of the file restore functions.</summary>
	public const int FILE_RESTORE_VERSION_2 = (FILE_RESTORE_MAJOR_VERSION_2 << 16) | FILE_RESTORE_MINOR_VERSION_2;

	/// <summary>Used to define the current version number for file restore functions.</summary>
	public const int FILE_RESTORE_VERSION_CURRENT = FILE_RESTORE_VERSION_2;

	private const string Lib_Fmapi = "fmapi.dll";

	/// <summary>
	/// <para>
	/// The <c>FILE_RESTORE_CALLBACK</c> function describes a callback function that is used to report the progress status or finished status
	/// of the file restoration process.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="PacketType">The message type that is sent. For more information, see the <c>FILE_RESTORE_PACKET_TYPE</c> enumeration.</param>
	/// <param name="PacketLength">The length of the information in the packet, in bytes.</param>
	/// <param name="PacketData">The progress status or finished status of the file restoration process.</param>
	/// <returns>If the callback returns <c>FALSE</c>, the file restore operation is canceled.</returns>
	/// <remarks>Note that there is no associated header file for this callback function.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/file-restore-callback typedef BOOLEAN (
	// *FILE_RESTORE_CALLBACK)( _In_ FILE_RESTORE_PACKET_TYPE PacketType, _In_ ULONG PacketLength, _In_ PVOID PacketData );
	[PInvokeData("")]
	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	[return: MarshalAs(UnmanagedType.U1)]
	public delegate bool FILE_RESTORE_CALLBACK([In] FILE_RESTORE_PACKET_TYPE PacketType, uint PacketLength, [In] IntPtr PacketData);

	/// <summary>
	/// <para>Defines the valid file system types that can be recognized by the <c>DetectBootSector</c> function.</para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>Note that there is no associated header file for this enumeration.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/boot-sector-file-system-type typedef enum {
	// FileSystemUnknown, FileSystemFAT12, FileSystemFAT16, FileSystemFat32, FileSystemNTFS } BOOT_SECTOR_FILE_SYSTEM_TYPE;
	[PInvokeData("")]
	public enum BOOT_SECTOR_FILE_SYSTEM_TYPE
	{
		/// <summary>The file system is not known.</summary>
		FileSystemUnknown,

		/// <summary>The file system is FAT12.</summary>
		FileSystemFAT12,

		/// <summary>The file system is FAT16.</summary>
		FileSystemFAT16,

		/// <summary>The file system is FAT32.</summary>
		FileSystemFAT32,

		/// <summary>The file system is NTFS.</summary>
		FileSystemNTFS,
	}

	/// <summary>
	/// <para>Defines the message types that can be sent by the <c>RestoreFile</c> function.</para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>Note that there is no associated header file for this enumeration.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/file-restore-packet-type typedef enum {
	// FileRestoreProgressInfo = 100, FileRestoreFinished = 101 } FILE_RESTORE_PACKET_TYPE, *PFILE_RESTORE_PACKET_TYPE;
	[PInvokeData("")]
	public enum FILE_RESTORE_PACKET_TYPE
	{
		/// <summary>
		/// The progress of the restoration process is reported by the callback function. The structure of the progress information is
		/// defined in <c>FILE_RESTORE_PROGRESS_INFORMATION</c>.
		/// </summary>
		FileRestoreProgressInfo = 100,

		/// <summary>
		/// The final status of the restoration process is reported by the callback function. The structure of the status information is
		/// defined in <c>FILE_RESTORE_FINISHED_INFORMATION</c>.
		/// </summary>
		FileRestoreFinished,
	}

	/// <summary>
	/// <para>
	/// Defines values that are used in the <c>CreateFileRestoreContext</c> function to specify the file restore context for the volume or
	/// for the physical drive. These enumeration values are used in the Flags argument to the <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>Note that there is no associated header file for this enumeration.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/restore-context-flags typedef enum { ContextFlagVolume =
	// 0x00000001, ContextFlagDisk = 0x00000002, FlagScanRemovedFiles = 0x00000004, FlagScanRegularFiles = 0x00000008,
	// FlagScanIncludeRemovedDirectories = 0x00000010 } RESTORE_CONTEXT_FLAGS;
	[PInvokeData("")]
	[Flags]
	public enum RESTORE_CONTEXT_FLAGS : uint
	{
		/// <summary>
		/// <para>
		/// Indicates that the string in the Volume parameter to the <c>CreateFileRestoreContext</c> function represents a path of a volume
		/// </para>
		/// <para>This flag cannot be combined with <c>ContextFlagDisk.</c></para>
		/// <para>.</para>
		/// </summary>
		ContextFlagVolume = 0x01,

		/// <summary>
		/// <para>
		/// Indicates that the string in the Volume parameter to the <c>CreateFileRestoreContext</c> function represents a path of a physical disk.
		/// </para>
		/// <para>This flag cannot be combined with <c>ContextFlagVolume.</c></para>
		/// </summary>
		ContextFlagDisk = 0x02,

		/// <summary>Indicates that the <c>ScanRestorableFiles</c> function will search for files that have been deleted.</summary>
		FlagScanRemovedFiles = 0x04,

		/// <summary>
		/// Indicates that the that <c>ScanRestorableFiles</c> function will search for files that have not been deleted (also called
		/// "regular files").
		/// </summary>
		FlagScanRegularFiles = 0x08,

		/// <summary>
		/// Indicates that the that the <c>ScanRestorableFiles</c> function will search for directories that have been deleted. This flag is
		/// only used with the <c>FlagScanRemovedFiles</c> flag.
		/// </summary>
		FlagScanIncludeRemovedDirectories = 0x10,
	}

	/// <summary>A bitmask of flags that indicate the status of the volume.</summary>
	[PInvokeData("")]
	[Flags]
	public enum VOLUME_INFO : uint
	{
		/// <summary>Used to define the encrypted status of a volume.</summary>
		VOLUME_INFO_ENCRYPTED = 0x0001,

		/// <summary>Used to define the locked status of a volume.</summary>
		VOLUME_INFO_LOCKED = 0x0002
	}

	/// <summary>
	/// <para>
	/// Closes the context that is used to restore files. If the file system volume is exclusively locked, this function removes the lock.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">A pointer to the file restore context created by the <c>CreateFileRestoreContext</c> function.</param>
	/// <returns>
	/// <para>If the function succeeds, the return value is nonzero.</para>
	/// <para>If the function fails, the return value is zero. To get extended error information, call <c>GetLastError</c>.</para>
	/// </returns>
	/// <remarks>
	/// <para>This function cancels an ongoing restoration process that was initiated with the <c>RestoreFile</c> function.</para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/closefilerestorecontext BOOL WINAPI CloseFileRestoreContext(
	// _In_ PFILE_RESTORE_CONTEXT Context );
	[PInvokeData("")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CloseFileRestoreContext([In] PFILE_RESTORE_CONTEXT Context);

	/// <summary>
	/// <para>
	/// Initializes the context that is used to restore files. The context can be created for an existing recognized volume or for a lost
	/// (unrecognized) volume.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Volume">
	/// <para>The path of the volume or physical drive to be used.</para>
	/// <para>
	/// To specify a physical drive, use the following syntax: "\\.\PhysicalDriveN", where N is a valid drive number, for example, "\\.\PhysicalDrive0".
	/// </para>
	/// <para>To specify a mounted volume, use the following syntax: "\\.\N:", where N is a valid drive letter, for example, "\\.\C:".</para>
	/// <para>
	/// If Flags is set to <c>ContextFlagVolume</c>, this parameter identifies a volume. If Flags is set to <c>ContextFlagDisk</c>, this
	/// parameter identifies a physical drive.
	/// </para>
	/// </param>
	/// <param name="Flags">
	/// The type of context that is created. The value of this parameter can be a combination of constants from the
	/// <c>RESTORE_CONTEXT_FLAGS</c> enumeration.
	/// </param>
	/// <param name="StartSector">
	/// If Flags contains <c>ContextFlagDisk</c>, this parameter specifies the first sector offset of the lost volume. If Flags does not
	/// contain <c>ContextFlagDisk</c>, this parameter is ignored.
	/// </param>
	/// <param name="BootSector">
	/// If Flags contains <c>ContextFlagDisk</c>, this parameter specifies the boot sector offset of the lost volume. The value of this
	/// parameter can be the same as the value of StartSector or it can be the last volume sector.
	/// </param>
	/// <param name="Version">
	/// <para>
	/// The major and minor version number. This parameter must match the version of FMAPI that is being used, according to the following table.
	/// </para>
	/// <list type="table">
	/// <listheader>
	/// <description>Value</description>
	/// <description>Meaning</description>
	/// </listheader>
	/// <item>
	/// <description><c>FILE_RESTORE_VERSION_1</c></description>
	/// <description>Windows 7, Windows Server 2008 R2, Windows Server 2008, and Windows Vista</description>
	/// </item>
	/// <item>
	/// <description><c>FILE_RESTORE_VERSION_2</c></description>
	/// <description>Windows 8 and Windows Server 2012</description>
	/// </item>
	/// </list>
	/// </param>
	/// <param name="Context">A <c>PFILE_RESTORE_CONTEXT</c> pointer to save the file restore context.</param>
	/// <returns>
	/// <para>If the function succeeds, the return value is TRUE.</para>
	/// <para>If the function fails, the return value is FALSE. To get extended error information, call <c>GetLastError</c>.</para>
	/// <para>
	/// If the Version parameter does not match the version of FMAPI that is being used, this function returns FALSE, and <c>GetLastError</c>
	/// returns <c>ERROR_INVALID_PARAMETER</c>.
	/// </para>
	/// </returns>
	/// <remarks>
	/// <para>
	/// If Flags contains ContextFlagVolume, the <c>CreateFileRestoreContext</c> function uses the <c>FSCTL_LOCK_VOLUME</c> control code to
	/// lock the volume.
	/// </para>
	/// <para>If the files to be restored are encrypted with BitLocker, the <c>CreateFileRestoreContext</c> function does one of the following:</para>
	/// <list type="bullet">
	/// <item>
	/// <description>On Windows 8 and Windows Server 2012 (FMAPI version 2),</description>
	/// </item>
	/// <item>
	/// <description>
	/// On Windows 7, Windows Server 2008 R2, and Windows Server 2008, if Flags contains <c>ContextFlagDisk</c>, the
	/// <c>CreateFileRestoreContext</c> function tries to retrieve all of the required BitLocker key information from the disk and the
	/// Trusted Platform Module (TPM). If the necessary information for BitLocker decryption is not available, the file restore context is
	/// created with minimal validation. In such cases, you must call the <c>SupplyDecryptionInfo</c> function to provide the restore context
	/// with the keys that are needed to perform data decryption. Supplying this information is required only if BitLocker setup was
	/// completed with an external startup-key file on a USB drive without using a TPM. The call to the <c>SupplyDecryptionInfo</c> function
	/// must be completed before any call to <c>ScanRestorableFiles</c> or <c>RestoreFile</c>.
	/// </description>
	/// </item>
	/// <item>
	/// <description>
	/// On Windows Vista, if Flags contains <c>ContextFlagVolume</c>, the function succeeds only if the volume is unlocked by BitLocker. If
	/// the volume is not unlocked, the function returns an error.
	/// </description>
	/// </item>
	/// </list>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/createfilerestorecontext BOOL WINAPI
	// CreateFileRestoreContext( _In_ PCWSTR Volume, _In_ RESTORE_CONTEXT_FLAGS Flags, _In_opt_ LONGLONG StartSector, _In_ LONGLONG
	// BootSector, _In_ DWORD Version, _Out_ PFILE_RESTORE_CONTEXT Context );
	[PInvokeData("")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool CreateFileRestoreContext([MarshalAs(UnmanagedType.LPWStr)] string Volume,
		[In] RESTORE_CONTEXT_FLAGS Flags, [In, Optional] long StartSector, long BootSector, uint Version,
		out PFILE_RESTORE_CONTEXT Context);

	/// <summary>
	/// <para>
	/// Validates the correctness of the volume boot sector and provides data that is used to access the internal on-disk structures of the
	/// file system.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="BootSector">
	/// A pointer to the first 512 bytes of the boot sector. The size of this buffer should always be 512 bytes, even if the sector size of
	/// the drive is not 512 bytes.
	/// </param>
	/// <param name="BootSectorParams">
	/// A pointer to a <c>BOOT_SECTOR_INFO</c> structure that receives the boot sector information if a boot sector is detected.
	/// </param>
	/// <returns>
	/// <para>If a boot sector is detected, the return value is nonzero.</para>
	/// <para>If a boot sector is not detected, the return value is zero.</para>
	/// </returns>
	/// <remarks>
	/// <para>The <c>DetectBootSector</c> function supports FAT and NTFS file systems.</para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/detectbootsector BOOL WINAPI DetectBootSector( _In_ CONST
	// UCHAR* BootSector, _Out_ PBOOT_SECTOR_INFO BootSectorParams );
	[PInvokeData("")]
	[DllImport("fmapi.dll", SetLastError = false, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DetectBootSector([In, MarshalAs(UnmanagedType.LPArray, SizeConst = BOOT_SECTOR_SIZE)] byte[] BootSector, out BOOT_SECTOR_INFO BootSectorParams);

	/// <summary>
	/// <para>
	/// This function is obsolete in Windows 8, Windows Server 2012, and later. Determines whether the volume is encrypted with BitLocker
	/// technology. If the volume is encrypted, the function determines whether it is unlocked.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">
	/// A <c>PFILE_RESTORE_CONTEXT</c> pointer to the file restore context that was created by calling the <c>CreateFileRestoreContext</c> function.
	/// </param>
	/// <param name="VolumeEncryptionInfo">
	/// A bitmask of flags that indicate the status of the volume. This value can be any combination of <c>VOLUME_INFO_ENCRYPTED</c> and
	/// <c>VOLUME_INFO_LOCKED</c>. For more information about these values, see File Management Constants.
	/// </param>
	/// <returns>
	/// <para>If the function succeeds, the return value is TRUE.</para>
	/// <para>If the function fails, the return value is FALSE. To get extended error information, call <c>GetLastError</c>.</para>
	/// <para>In Windows 8, Windows Server 2012, and later, this function always returns FALSE, and <c>GetLastError</c> returns ERROR_NOT_SUPPORTED.</para>
	/// </returns>
	/// <remarks>
	/// <para>This function is obsolete in Windows 8, Windows Server 2012, and later.</para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/detectencryptedvolume BOOL WINAPI DetectEncryptedVolume(
	// _In_ PFILE_RESTORE_CONTEXT Context, _Out_ PDWORD VolumeEncryptionInfo );
	[PInvokeData("")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[UnsupportedOSPlatform("windows6.2")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DetectEncryptedVolume([In] PFILE_RESTORE_CONTEXT Context, out VOLUME_INFO VolumeEncryptionInfo);

	/// <summary>
	/// <para>
	/// This function is obsolete in Windows 8, Windows Server 2012, and later. Determines whether the volume is encrypted with BitLocker
	/// technology. If the volume is encrypted, the function determines whether it is unlocked. This function is identical to the
	/// <c>DetectEncryptedVolume</c> function, except that it has an additional VolumeSize parameter.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">
	/// A <c>PFILE_RESTORE_CONTEXT</c> pointer to the file restore context that was created by calling the <c>CreateFileRestoreContext</c> function.
	/// </param>
	/// <param name="VolumeEncryptionInfo">
	/// A bitmask of flags that indicate the status of the volume. This value can be any combination of <c>VOLUME_INFO_ENCRYPTED</c> and
	/// <c>VOLUME_INFO_LOCKED</c>. For more information about these values, see File Management Constants.
	/// </param>
	/// <param name="VolumeSize">
	/// <para>Receives the size, in bytes, of the volume. For partially encrypted volumes, VolumeSize is zero on return.</para>
	/// <para>**Windows Server 2008 and Windows Vista: **VolumeSize is not zero on return for partially encrypted volumes.</para>
	/// </param>
	/// <returns>
	/// <para>If the function succeeds, the return value is TRUE.</para>
	/// <para>If the function fails, the return value is FALSE. To get extended error information, call <c>GetLastError</c>.</para>
	/// <para>In Windows 8, Windows Server 2012, and later, this function always returns FALSE, and <c>GetLastError</c> returns ERROR_NOT_SUPPORTED.</para>
	/// </returns>
	/// <remarks>
	/// <para>This function is obsolete in Windows 8, Windows Server 2012, and later.</para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/detectencryptedvolumeex BOOL WINAPI DetectEncryptedVolumeEx(
	// _In_ PFILE_RESTORE_CONTEXT Context, _Out_ PDWORD VolumeEncryptionInfo, _Out_ PULONGLONG VolumeSize );
	[PInvokeData("")]
	[UnsupportedOSPlatform("windows6.2")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool DetectEncryptedVolumeEx([In] PFILE_RESTORE_CONTEXT Context, out VOLUME_INFO VolumeEncryptionInfo, out ulong VolumeSize);

	/// <summary>
	/// <para>
	/// Copies a deleted or regular file from a volume that is defined in the file restore context to a new file on an available logical
	/// drive. The volume path is set in the Volume parameter of the <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">A pointer to the file restore context that was created by calling the <c>CreateFileRestoreContext</c> function.</param>
	/// <param name="RestorableFile">
	/// A pointer to a <c>RESTORABLE_FILE_INFO</c> structure that specifies the file to restore that was discovered by the
	/// <c>ScanRestorableFiles</c> function.
	/// </param>
	/// <param name="DstFile">The path of the destination file where the restored data will be saved.</param>
	/// <param name="Callback">The <c>FILE_RESTORE_CALLBACK</c> callback function to use for reporting the progress of the copying process.</param>
	/// <param name="ClbkArg">The argument to be returned with the callback function.</param>
	/// <returns>
	/// <para>If the function succeeds, the return value is nonzero.</para>
	/// <para>If the function fails, the return value is zero. To get extended error information, call <c>GetLastError</c>.</para>
	/// </returns>
	/// <remarks>
	/// <para>
	/// For removed files, the <c>RestoreFile</c> function tries to restore the file. It is possible that some of the file data might not be
	/// restored. Only data streams of the restorable file are copied, all other file information is not restored. The <c>RestoreFile</c>
	/// function can only be used to restore files; directories cannot be restored.
	/// </para>
	/// <para>
	/// The <c>RestoreFile</c> function can call a callback function to report the progress of the copy process and the final status of the
	/// operation. For more information about the parameters that are used by the <c>RestoreFile</c> function to send information to a
	/// callback function, see the <c>FILE_RESTORE_PACKET_TYPE</c> enumeration.
	/// </para>
	/// <para>The restore process can be canceled by returning <c>FALSE</c> in the callback function.</para>
	/// <para>
	/// In general, FMAPI cannot guarantee the success of the restore. It uses a best-effort approach. Files that have been overwritten may
	/// not be restorable, or may be only partially restorable. In addition, limitations of the FAT file system make FMAPI inefficient in
	/// restoring fragmented files in their entirety. FMAPI will attempt to restore as much of the file as possible. In some cases, FMAPI's
	/// restored version of a file may be fully or partially corrupted (compared to the original), if the file contents and meta data were
	/// overwritten by some other file. FMAPI may not be successful in restoring very large files, due to file system restrictions.
	/// </para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/restorefile BOOL WINAPI RestoreFile( _In_
	// PFILE_RESTORE_CONTEXT Context, _In_ PRESTORABLE_FILE_INFO RestorableFile, _In_ PCWSTR DstFile, _In_opt_ FILE_RESTORE_CALLBACK
	// Callback, _In_opt_ PVOID ClbkArg );
	[PInvokeData("")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool RestoreFile([In] PFILE_RESTORE_CONTEXT Context, [In] IntPtr RestorableFile,
		[MarshalAs(UnmanagedType.LPWStr)] string DstFile, [In, Optional] FILE_RESTORE_CALLBACK? Callback, [In, Optional] IntPtr ClbkArg);

	/// <summary>
	/// <para>
	/// Searches for files that are available to be restored. The behavior of this function depends on the Flags parameter of the
	/// <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">A pointer to the file restore context created using the <c>CreateFileRestoreContext</c> function.</param>
	/// <param name="Path">The initial path of where to begin the scan for files to restore.</param>
	/// <param name="FileInfoSize">
	/// The size of the buffer that contains the <c>RESTORABLE_FILE_INFO</c> structure to which the FileInfo parameter points, in bytes.
	/// </param>
	/// <param name="FileInfo">
	/// A pointer to a <c>RESTORABLE_FILE_INFO</c> structure that contains information about the file or directory to be restored.
	/// </param>
	/// <param name="FileInfoUsed">
	/// A pointer to a variable that contains the length of the <c>RESTORABLE_FILE_INFO</c> structure or specifies the required space for the structure.
	/// </param>
	/// <returns>
	/// <para>If the function succeeds, returns <c>TRUE</c>.</para>
	/// <para>If the function fails, returns <c>FALSE</c>. To get extended error information, call the <c>GetLastError</c> function.</para>
	/// <para>
	/// If <c>TRUE</c> is returned, the <c>RESTORABLE_FILE_INFO</c> structure that is pointed to by the FileInfo parameter contains
	/// information about the file or directory to be restored.
	/// </para>
	/// <para>
	/// If <c>FALSE</c> is returned and the value of FileInfoSize is less than the value that the FileInfoUsed parameter points to, the
	/// buffer is too small. <c>GetLastError</c> returns <c>ERROR_INSUFFICIENT_BUFFER</c>.
	/// </para>
	/// <para>When scanning is complete, <c>FALSE</c> is returned and <c>GetLastError</c> returns <c>ERROR_NO_MORE_FILES</c>.</para>
	/// </returns>
	/// <remarks>
	/// <para>
	/// Typically, scanning is performed to discover files that have been removed. To discover files that have been removed, the
	/// FlagScanRemovedFiles flag must be set in the Flags parameter of the <c>CreateFileRestoreContext</c> function. If the
	/// FlagScanIncludeRemovedDirectories flag is also set, the scan includes both files and directories.
	/// </para>
	/// <para>
	/// Sometimes it is necessary to restore regular files on lost volumes. To scan for regular files, the FlagScanRegularFiles flag must be
	/// set in the Flags parameter of the <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <para>
	/// The <c>ScanRestorableFiles</c> function populates the <c>RESTORABLE_FILE_INFO</c> structure for one file or directory that is found
	/// on the volume. To restore multiple files or directories, the <c>ScanRestorableFiles</c> function must be called for each file or
	/// directory that you want to discover.
	/// </para>
	/// <para>
	/// The <c>RESTORABLE_FILE_INFO</c> structure can be variable in size. To determine the size that is required for the structure, the
	/// <c>ScanRestorableFiles</c> function should be called the first time with the FileInfoSize parameter set to zero. The
	/// <c>ScanRestorableFiles</c> function returns the required size of the <c>RESTORABLE_FILE_INFO</c> structure in the FileInfoUsed
	/// parameter. If subsequent calls to the <c>ScanRestorableFiles</c> function return <c>FALSE</c>, the value in the FileInfoUsed
	/// parameter is greater than the value in the FileInfoSize parameter, and <c>GetLastError</c> returns <c>ERROR_INSUFFICIENT_BUFFER</c>,
	/// this means that there is not enough space and the FileInfo buffer must be expanded. After the buffer has been expanded, the scan can
	/// be resumed with the same file restore context without the loss of data.
	/// </para>
	/// <para>
	/// <c>ScanRestorableFiles</c>'s scanning process uses a find-first, find-next mechanism for finding files. The first time
	/// <c>ScanRestorableFiles</c> is called, the Path parameter is used to find the first restorable file. Each subsequent call to
	/// <c>ScanRestorableFiles</c> ignores the Path parameter and searches for the next file. The scan process cannot be restarted from the
	/// beginning with an existing file restore context. To restart the scan process, call the <c>CloseFileRestoreContext</c> function, and
	/// then create a new file restore context using the <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <para>
	/// If a restorable file is found, each call to <c>ScanRestorableFiles</c> returns a <c>RESTORABLE_FILE_INFO</c> in the FileInfo
	/// parameter that references the restorable file or a partial path to the restorable file. Check the <c>IsRemoved</c> member of the
	/// <c>RESTORABLE_FILE_INFO</c> structure to determine whether the file returned is restorable.
	/// </para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/scanrestorablefiles BOOL WINAPI ScanRestorableFiles( _In_
	// PFILE_RESTORE_CONTEXT Context, _In_ PCWSTR Path, _In_ ULONG FileInfoSize, _Out_ PRESTORABLE_FILE_INFO FileInfo, _Out_ PULONG
	// FileInfoUsed );
	[PInvokeData("")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool ScanRestorableFiles([In] PFILE_RESTORE_CONTEXT Context, [MarshalAs(UnmanagedType.LPWStr)] string Path,
		uint FileInfoSize, IntPtr FileInfo, out uint FileInfoUsed);

	/// <summary>
	/// <para>
	/// Searches for files that are available to be restored. The behavior of this function depends on the Flags parameter of the
	/// <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">A pointer to the file restore context created using the <c>CreateFileRestoreContext</c> function.</param>
	/// <param name="Path">The initial path of where to begin the scan for files to restore.</param>
	/// <param name="FileInfo">
	/// A pointer to a <c>RESTORABLE_FILE_INFO</c> structure that contains information about the file or directory to be restored.
	/// </param>
	/// <returns>
	/// <para>If the function succeeds, returns <c>TRUE</c>.</para>
	/// <para>If the function fails, returns <c>FALSE</c>. To get extended error information, call the <c>GetLastError</c> function.</para>
	/// <para>
	/// If <c>TRUE</c> is returned, the <c>RESTORABLE_FILE_INFO</c> structure that is pointed to by the FileInfo parameter contains
	/// information about the file or directory to be restored.
	/// </para>
	/// <para>
	/// If <c>FALSE</c> is returned and the value of FileInfoSize is less than the value that the FileInfoUsed parameter points to, the
	/// buffer is too small. <c>GetLastError</c> returns <c>ERROR_INSUFFICIENT_BUFFER</c>.
	/// </para>
	/// <para>When scanning is complete, <c>FALSE</c> is returned and <c>GetLastError</c> returns <c>ERROR_NO_MORE_FILES</c>.</para>
	/// </returns>
	/// <remarks>
	/// <para>
	/// Typically, scanning is performed to discover files that have been removed. To discover files that have been removed, the
	/// FlagScanRemovedFiles flag must be set in the Flags parameter of the <c>CreateFileRestoreContext</c> function. If the
	/// FlagScanIncludeRemovedDirectories flag is also set, the scan includes both files and directories.
	/// </para>
	/// <para>
	/// Sometimes it is necessary to restore regular files on lost volumes. To scan for regular files, the FlagScanRegularFiles flag must be
	/// set in the Flags parameter of the <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <para>
	/// The <c>ScanRestorableFiles</c> function populates the <c>RESTORABLE_FILE_INFO</c> structure for one file or directory that is found
	/// on the volume. To restore multiple files or directories, the <c>ScanRestorableFiles</c> function must be called for each file or
	/// directory that you want to discover.
	/// </para>
	/// <para>
	/// The <c>RESTORABLE_FILE_INFO</c> structure can be variable in size. To determine the size that is required for the structure, the
	/// <c>ScanRestorableFiles</c> function should be called the first time with the FileInfoSize parameter set to zero. The
	/// <c>ScanRestorableFiles</c> function returns the required size of the <c>RESTORABLE_FILE_INFO</c> structure in the FileInfoUsed
	/// parameter. If subsequent calls to the <c>ScanRestorableFiles</c> function return <c>FALSE</c>, the value in the FileInfoUsed
	/// parameter is greater than the value in the FileInfoSize parameter, and <c>GetLastError</c> returns <c>ERROR_INSUFFICIENT_BUFFER</c>,
	/// this means that there is not enough space and the FileInfo buffer must be expanded. After the buffer has been expanded, the scan can
	/// be resumed with the same file restore context without the loss of data.
	/// </para>
	/// <para>
	/// <c>ScanRestorableFiles</c>'s scanning process uses a find-first, find-next mechanism for finding files. The first time
	/// <c>ScanRestorableFiles</c> is called, the Path parameter is used to find the first restorable file. Each subsequent call to
	/// <c>ScanRestorableFiles</c> ignores the Path parameter and searches for the next file. The scan process cannot be restarted from the
	/// beginning with an existing file restore context. To restart the scan process, call the <c>CloseFileRestoreContext</c> function, and
	/// then create a new file restore context using the <c>CreateFileRestoreContext</c> function.
	/// </para>
	/// <para>
	/// If a restorable file is found, each call to <c>ScanRestorableFiles</c> returns a <c>RESTORABLE_FILE_INFO</c> in the FileInfo
	/// parameter that references the restorable file or a partial path to the restorable file. Check the <c>IsRemoved</c> member of the
	/// <c>RESTORABLE_FILE_INFO</c> structure to determine whether the file returned is restorable.
	/// </para>
	/// </remarks>
	public static bool ScanRestorableFiles([In] PFILE_RESTORE_CONTEXT Context, [MarshalAs(UnmanagedType.LPWStr)] string Path,
		out SafeCoTaskMemStruct<RESTORABLE_FILE_INFO> FileInfoUsed)
	{
		FileInfoUsed = SafeCoTaskMemStruct<RESTORABLE_FILE_INFO>.Null;
		Win32Error err = 0;
		if (!ScanRestorableFiles(Context, Path, 0, default, out var sz))
			err = Win32Error.GetLastError();
		if (err.Succeeded || err == Win32Error.ERROR_INSUFFICIENT_BUFFER)
		{
			FileInfoUsed = new(sz);
			return ScanRestorableFiles(Context, Path, FileInfoUsed.Size, FileInfoUsed, out _);
		}
		return false;
	}

	/// <summary>
	/// <para>
	/// This function is obsolete in Windows 8, Windows Server 2012, and later. Provides the full path for a file that contains a decryption
	/// key and provides a BitLocker information block that is stored in Active Directory. The data provided with this function is required
	/// when using the <c>ScanRestorableFiles</c> function and the <c>RestoreFile</c> function to access data that is encrypted on a disk.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <param name="Context">A pointer to the file restore context that was created by calling the <c>CreateFileRestoreContext</c> function.</param>
	/// <param name="RecoveryKeyFilePath">
	/// The full path to the .bek file that contains the recovery key. Only one of RecoveryKeyFilePath or RecoveryPassword is required.
	/// </param>
	/// <param name="RecoveryPassword">The recovery password string.</param>
	/// <param name="KeyPackage">
	/// The Binary Large Object (BLOB) that contains the BitLocker key package (metadata). The key package is usually backed up to the Active
	/// Directory during the BitLocker setup. If the metadata is not readable on the disk, this parameter is required.
	/// </param>
	/// <param name="KeyPackageSize">The size of the BitLocker key package, in bytes.</param>
	/// <returns>
	/// <para>If the function succeeds, the return value is TRUE.</para>
	/// <para>If the function fails, the return value is FALSE. To get extended error information, call <c>GetLastError</c>.</para>
	/// <para>In Windows 8, Windows Server 2012, and later, this function always returns FALSE, and <c>GetLastError</c> returns ERROR_NOT_SUPPORTED.</para>
	/// </returns>
	/// <remarks>
	/// <para>This function is obsolete in Windows 8, Windows Server 2012, and later.</para>
	/// <para>You must use the <c>SupplyDecryptionInfo</c> function for the following scenarios:</para>
	/// <list type="bullet">
	/// <item>
	/// <description>The volume was encrypted with the BitLocker technology.</description>
	/// </item>
	/// <item>
	/// <description>
	/// BitLocker indicates that the metadata on the disk is corrupted. The metadata must be retrieved from Active Directory and provided
	/// using the KeyPackage parameter.
	/// </description>
	/// </item>
	/// </list>
	/// <para>
	/// If required, the <c>SupplyDecryptionInfo</c> function must be called before any call to the <c>ScanRestorableFiles</c> function or
	/// the <c>RestoreFile</c> function.
	/// </para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/supplydecryptioninfo BOOL WINAPI SupplyDecryptionInfo( _In_
	// PFILE_RESTORE_CONTEXT Context, _In_opt_ PCWSTR RecoveryKeyFilePath, _In_opt_ PVOID RecoveryPassword, _In_opt_ PVOID KeyPackage,
	// _In_opt_ ULONG KeyPackageSize );
	[PInvokeData("")]
	[UnsupportedOSPlatform("windows6.2")]
	[DllImport(Lib_Fmapi, SetLastError = true, ExactSpelling = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SupplyDecryptionInfo([In] PFILE_RESTORE_CONTEXT Context, [Optional, MarshalAs(UnmanagedType.LPWStr)] string? RecoveryKeyFilePath,
		[In, Optional] IntPtr RecoveryPassword, [In, Optional] IntPtr KeyPackage, [In, Optional] uint KeyPackageSize);

	/// <summary>
	/// <para>Provides information about the boot sector. This structure is used by the <c>DetectBootSector</c> function.</para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>Note that there is no associated header file for this structure.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/boot-sector-info typedef struct _BOOT_SECTOR_INFO { LONGLONG
	// TotalSectors; BOOT_SECTOR_FILE_SYSTEM_TYPE FileSystem; ULONG BytePerSector; ULONG SectorPerCluster; BOOL IsEncrypted; }
	// BOOT_SECTOR_INFO, *PBOOT_SECTOR_INFO;
	[PInvokeData("")]
	[StructLayout(LayoutKind.Sequential)]
	public struct BOOT_SECTOR_INFO
	{
		/// <summary>The total number of sectors on the detected volume of the file system.</summary>
		public long TotalSectors;

		/// <summary>The type of the file system.</summary>
		public BOOT_SECTOR_FILE_SYSTEM_TYPE FileSystem;

		/// <summary>The number of bytes per sector.</summary>
		public uint BytePerSector;

		/// <summary>The number of sectors per cluster.</summary>
		public uint SectorPerCluster;

		/// <summary>
		/// <para>This member is not used.</para>
		/// <para>
		/// <c>Windows 7, Windows Server 2008 R2, Windows Vista and Windows Server 2008: TRUE</c> if the volume is BitLocker encrypted;
		/// otherwise, <c>FALSE</c>.
		/// </para>
		/// </summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool IsEncrypted;
	}

	/// <summary>
	/// <para>
	/// Provides information about the final status of the restored file. This structure is used in <c>RestoreFile</c> and defines the format
	/// of the callback buffer for the <c>FileRestoreFinished</c> message type.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>Note that there is no associated header file for this structure.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/file-restore-finished-information typedef struct
	// _FILE_RESTORE_FINISHED_INFORMATION { BOOL Success; ULONG FinalResult; PVOID ClbkArg; } FILE_RESTORE_FINISHED_INFORMATION, *PFILE_RESTORE_FINISHED_INFORMATION;
	[PInvokeData("")]
	[StructLayout(LayoutKind.Sequential)]
	public struct FILE_RESTORE_FINISHED_INFORMATION
	{
		/// <summary><c>TRUE</c> if the file was successfully restored; otherwise, <c>FALSE</c>.</summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool Success;

		/// <summary>The final error code that was returned by <c>RestoreFile</c>.</summary>
		public uint FinalResult;

		/// <summary>The callback arguments that are passed with <c>RestoreFile</c>.</summary>
		public IntPtr ClbkArg;
	}

	/// <summary>
	/// <para>
	/// Provides information about the progress of the restoration of a file. This structure is used in the <c>RestoreFile</c> function and
	/// defines the format of the callback buffer for the <c>FileRestoreProgressInfo</c> message type.
	/// </para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>Note that there is no associated header file for this structure.</remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/file-restore-progress-information typedef struct
	// _FILE_RESTORE_PROGRESS_INFORMATION { LONGLONG TotalFileSize; LONGLONG TotalBytesCompleted; LONGLONG StreamSize; LONGLONG
	// StreamBytesCompleted; PVOID ClbkArg; } FILE_RESTORE_PROGRESS_INFORMATION, *PFILE_RESTORE_PROGRESS_INFORMATION;
	[PInvokeData("")]
	[StructLayout(LayoutKind.Sequential)]
	public struct FILE_RESTORE_PROGRESS_INFORMATION
	{
		/// <summary>The total size of the restorable file, in bytes.</summary>
		public long TotalFileSize;

		/// <summary>The total number of bytes that have been restored.</summary>
		public long TotalBytesCompleted;

		/// <summary>The size of the current stream, in bytes.</summary>
		public long StreamSize;

		/// <summary>The number of bytes in the stream that have been restored.</summary>
		public long StreamBytesCompleted;

		/// <summary>The callback arguments that are passed to <c>RestoreFile</c>.</summary>
		public IntPtr ClbkArg;
	}

	/// <summary>Provides a handle to a file restore context.</summary>
	/// <remarks>Initializes a new instance of the <see cref="PFILE_RESTORE_CONTEXT"/> struct.</remarks>
	/// <param name="preexistingHandle">An <see cref="IntPtr"/> object that represents the pre-existing handle to use.</param>
	[StructLayout(LayoutKind.Sequential)]
	public readonly struct PFILE_RESTORE_CONTEXT(IntPtr preexistingHandle) : IHandle
	{
		private readonly IntPtr handle = preexistingHandle;

		/// <summary>Returns an invalid handle by instantiating a <see cref="PFILE_RESTORE_CONTEXT"/> object with <see cref="IntPtr.Zero"/>.</summary>
		public static PFILE_RESTORE_CONTEXT NULL => new(IntPtr.Zero);

		/// <summary>Gets a value indicating whether this instance is a null handle.</summary>
		public readonly bool IsNull => handle == IntPtr.Zero;

		/// <summary>Implements the operator !.</summary>
		/// <param name="h1">The handle.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !(PFILE_RESTORE_CONTEXT h1) => h1.IsNull;

		/// <summary>Performs an explicit conversion from <see cref="PFILE_RESTORE_CONTEXT"/> to <see cref="IntPtr"/>.</summary>
		/// <param name="h">The handle.</param>
		/// <returns>The result of the conversion.</returns>
		public static explicit operator IntPtr(PFILE_RESTORE_CONTEXT h) => h.handle;

		/// <summary>Performs an implicit conversion from <see cref="IntPtr"/> to <see cref="PFILE_RESTORE_CONTEXT"/>.</summary>
		/// <param name="h">The pointer to a handle.</param>
		/// <returns>The result of the conversion.</returns>
		public static implicit operator PFILE_RESTORE_CONTEXT(IntPtr h) => new(h);

		/// <summary>Implements the operator !=.</summary>
		/// <param name="h1">The first handle.</param>
		/// <param name="h2">The second handle.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator !=(PFILE_RESTORE_CONTEXT h1, PFILE_RESTORE_CONTEXT h2) => !(h1 == h2);

		/// <summary>Implements the operator ==.</summary>
		/// <param name="h1">The first handle.</param>
		/// <param name="h2">The second handle.</param>
		/// <returns>The result of the operator.</returns>
		public static bool operator ==(PFILE_RESTORE_CONTEXT h1, PFILE_RESTORE_CONTEXT h2) => h1.Equals(h2);

		/// <inheritdoc/>
		public override readonly bool Equals(object? obj) => obj is PFILE_RESTORE_CONTEXT h && handle == h.handle;

		/// <inheritdoc/>
		public override readonly int GetHashCode() => handle.GetHashCode();

		/// <inheritdoc/>
		public readonly IntPtr DangerousGetHandle() => handle;
	}

	/// <summary>
	/// <para>Provides information about a restorable file. This structure is used when calling <c>ScanRestorableFiles</c>.</para>
	/// <note type="note">FMAPI can only be used in the Windows Preinstallation Environment (WinPE) for Windows Vista, Windows Server 2008,
	/// and later. Applications that use FMAPI must license WinPE.</note>
	/// </summary>
	/// <remarks>
	/// <para>Note that there is no associated header file for this structure.</para>
	/// <para>
	/// The <c>FileName</c> member is variable in length. An additional implementation-specific BLOB is added after the name of the file. The
	/// location of the BLOB is determined by <c>RestoreDataOffset</c>.
	/// </para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/previous-versions/windows/desktop/fmapi/restorable-file-info typedef struct _RESTORABLE_FILE_INFO {
	// ULONG Size; DWORD Version; ULONGLONG FileSize; FILETIME CreationTime; FILETIME LastAccessTime; FILETIME LastWriteTime; DWORD
	// Attributes; BOOL IsRemoved; LONGLONG ClustersUsedByFile; LONGLONG ClustersCurrentlyInUse; ULONG RestoreDataOffset; WCHAR FileName[1];
	// } RESTORABLE_FILE_INFO, *PRESTORABLE_FILE_INFO;
	[PInvokeData("")]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	[VanaraMarshaler(typeof(AnySizeStringMarshaler<RESTORABLE_FILE_INFO>), "*")]
	public struct RESTORABLE_FILE_INFO
	{
		/// <summary>The size of the structure, in bytes.</summary>
		public uint Size;

		/// <summary>The major and minor version of the file.</summary>
		public uint Version;

		/// <summary>The size of the file.</summary>
		public ulong FileSize;

		/// <summary>The time the file was created. See <c>FILETIME</c>.</summary>
		public FILETIME CreationTime;

		/// <summary>The time the file was last accessed.</summary>
		public FILETIME LastAccessTime;

		/// <summary>The time the file was last modified.</summary>
		public FILETIME LastWriteTime;

		/// <summary>
		/// <para>The attributes for the file. This member can be a combination of one or more of the following values.</para>
		/// <list type="table">
		/// <listheader>
		/// <description>Attribute</description>
		/// <description>Meaning</description>
		/// </listheader>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_ARCHIVE</c></description>
		/// <description>The file or directory is an archive file. Applications use this attribute to mark files for backup or removal.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_COMPRESSED</c></description>
		/// <description>
		/// The file or directory is compressed. For a file, this means that all of the data in the file is compressed. For a directory, this
		/// means that compression is the default for newly created files and subdirectories.
		/// </description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_DIRECTORY</c></description>
		/// <description>The handle identifies a directory.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_ENCRYPTED</c></description>
		/// <description>
		/// The file or directory is encrypted. For a file, this means that all data in the file is encrypted. For a directory, this means
		/// that encryption is the default for newly created files and subdirectories.
		/// </description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_HIDDEN</c></description>
		/// <description>The file or directory is hidden. It is not included in an ordinary directory listing.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_NORMAL</c></description>
		/// <description>The file does not have other attributes. This attribute is valid only if used alone.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_OFFLINE</c></description>
		/// <description>
		/// The file data is not available immediately. This attribute indicates that the file data is physically moved to offline storage.
		/// This attribute is used by Remote Storage, the hierarchical storage management software. Applications should not arbitrarily
		/// change this attribute.
		/// </description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_READONLY</c></description>
		/// <description>
		/// The file or directory is read-only. Applications can read the file, but cannot write to it or delete it. If it is a directory,
		/// applications cannot delete it.
		/// </description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_REPARSE_POINT</c></description>
		/// <description>The file or directory has an associated reparse point.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_SPARSE_FILE</c></description>
		/// <description>The file is a sparse file.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_SYSTEM</c></description>
		/// <description>The file or directory is part of the operating system or is used exclusively by the operating system.</description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_TEMPORARY</c></description>
		/// <description>
		/// The file is being used for temporary storage. File systems avoid writing data back to mass storage if sufficient cache memory is
		/// available, because often the application deletes the temporary file after the handle is closed. In that case, the system can
		/// entirely avoid writing the data. Otherwise, the data will be written after the handle is closed.
		/// </description>
		/// </item>
		/// <item>
		/// <description><c>FILE_ATTRIBUTE_VIRTUAL</c></description>
		/// <description>A file is a virtual file.</description>
		/// </item>
		/// </list>
		/// </summary>
		public FileFlagsAndAttributes Attributes;

		/// <summary><c>TRUE</c> if the file has been removed; otherwise, <c>FALSE</c>.</summary>
		[MarshalAs(UnmanagedType.Bool)]
		public bool IsRemoved;

		/// <summary>
		/// The number of clusters that the file allocates. This member is used in conjunction with <c>ClustersCurrentlyInUse</c> to
		/// determine the percentage of file data that can be recovered.
		/// </summary>
		public long ClustersUsedByFile;

		/// <summary>The number of clusters that are marked as used for the file in the volume bitmap.</summary>
		public long ClustersCurrentlyInUse;

		/// <summary>The offset from the beginning of the structure to the Binary Large Object (BLOB) that is used by <c>RestoreFile</c>.</summary>
		public uint RestoreDataOffset;

		/// <summary>The full path of the file.</summary>
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1)]
		public string FileName;
	}
}