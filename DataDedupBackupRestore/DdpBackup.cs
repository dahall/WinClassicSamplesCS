namespace Vanara.PInvoke;

public static partial class DdpBackup
{
	/// <summary>Indicates whether Data Deduplication should perform an unoptimized or optimized restore.</summary>
	// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/ne-ddpbackup-dedup_backup_support_param_type typedef enum
	// _DEDUP_BACKUP_SUPPORT_PARAM_TYPE { DEDUP_RECONSTRUCT_UNOPTIMIZED = 1, DEDUP_RECONSTRUCT_OPTIMIZED = 2 } DEDUP_BACKUP_SUPPORT_PARAM_TYPE;
	[PInvokeData("ddpbackup.h", MSDNShortId = "NE:ddpbackup._DEDUP_BACKUP_SUPPORT_PARAM_TYPE")]
	public enum DEDUP_BACKUP_SUPPORT_PARAM_TYPE
	{
		/// <summary>
		/// <para>Value:</para>
		/// <para>1</para>
		/// <para>Perform an unoptimized restore.</para>
		/// </summary>
		DEDUP_RECONSTRUCT_UNOPTIMIZED,

		/// <summary>
		/// <para>Value:</para>
		/// <para>2</para>
		/// <para>Reserved for future use. Do not use.</para>
		/// </summary>
		DEDUP_RECONSTRUCT_OPTIMIZED,
	}

	/// <summary>
	/// Provides a method for restoring a file from a backup store containing copies of Data Deduplication reparse points, metadata, and
	/// container files.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A backup application uses the <c>IDedupBackupSupport</c> interface to drive the restore process for a select file from a backup store
	/// that contains the fully optimized version of the file (reparse point) and the Data Deduplication store.
	/// </para>
	/// <para>This interface is not useful when the backup store contains a copy of the original, non-optimized file.</para>
	/// <para>Applications that use the <c>IDedupBackupSupport</c> interface must also implement the IDedupReadFileCallback interface.</para>
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/nn-ddpbackup-idedupbackupsupport
	[PInvokeData("ddpbackup.h", MSDNShortId = "NN:ddpbackup.IDedupBackupSupport")]
	[ComImport, Guid("C719D963-2B2D-415E-ACF7-7EB7CA596FF4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), CoClass(typeof(DedupBackupSupport))]
	public interface IDedupBackupSupport
	{
		/// <summary>
		/// <para>
		/// Reconstructs a set of files from a backup store that contains the fully optimized version of the files (reparse points) and the
		/// Data Deduplication store.
		/// </para>
		/// <para>
		/// Applications that call the RestoreFiles method must also implement the IDedupReadFileCallback interface. Before calling the
		/// <c>RestoreFiles</c> method, the application must have previously restored the Data Deduplication reparse points for the files to
		/// the location specified by the <c>FileFullPaths</c> parameter. Metadata located in the reparse points will be utilized by Data
		/// Deduplication to further drive the restore process.
		/// </para>
		/// <para>
		/// After calling this method, applications can expect to receive two calls to IDedupReadFileCallback::OrderContainersRestore (one
		/// for stream map containers and one for data containers) and two or more calls to IDedupReadFileCallback::ReadBackupFile. The
		/// application will also receive one call to IDedupReadFileCallback::PreviewContainerRead before each call to <c>ReadBackupFile</c>
		/// that is directed to a container file.
		/// </para>
		/// </summary>
		/// <param name="NumberOfFiles">
		/// The number of files to restore. If this exceeds 10,000 then the method will fail with <c>E_INVALIDARG</c> (0x80070057).
		/// </param>
		/// <param name="FileFullPaths">
		/// For each file, this parameter contains the full path from the root directory of the volume to the reparse point previously
		/// restored by the application.
		/// </param>
		/// <param name="Store">
		/// IDedupReadFileCallback interface pointer for the backup store. This parameter is required and cannot be <c>NULL</c>.
		/// </param>
		/// <param name="Flags">
		/// This parameter must be <c>DEDUP_RECONSTRUCT_UNOPTIMIZED</c> on input. For more information, see the
		/// DEDUP_BACKUP_SUPPORT_PARAM_TYPE enumeration.
		/// </param>
		/// <param name="FileResults">
		/// <para>
		/// For each file, this parameter contains the results of the restore operation for that file. This parameter is optional and can be
		/// <c>NULL</c> if the application doesn't need to know the results for each individual file.
		/// </para>
		/// <para>S_OK (0x00000000L)</para>
		/// <para>The file was restored successfully.</para>
		/// <para>S_FALSE (0x00000001L)</para>
		/// <para>The specified file is not a deduplicated file.</para>
		/// <para>DDP_E_FILE_CORRUPT (0x80565355L)</para>
		/// <para>Data deduplication encountered a file corruption error.</para>
		/// <para>DDP_E_FILE_SYSTEM_CORRUPT (0x8056530EL)</para>
		/// <para>Data deduplication encountered a file system corruption error.</para>
		/// <para>DDP_E_INVALID_DATA (0x8056531DL)</para>
		/// <para>The data is not valid.</para>
		/// <para>DDP_E_JOB_COMPLETED_PARTIAL_SUCCESS (0x80565356L)</para>
		/// <para>The operation completed with some errors. Check the event logs for more details.</para>
		/// <para><c>Windows Server 2012:  </c> This value is not supported before Windows Server 2012 R2.</para>
		/// <para>DDP_E_NOT_FOUND (0x80565301L)</para>
		/// <para>The requested object was not found.</para>
		/// <para>DDP_E_PATH_NOT_FOUND (0x80565304L)</para>
		/// <para>A specified container path was not found in the backup store.</para>
		/// <para>DDP_E_UNEXPECTED (0x8056530CL)</para>
		/// <para>Data deduplication encountered an unexpected error. Check the Data Deduplication Operational event log for more information.</para>
		/// <para>DDP_E_VOLUME_DEDUP_DISABLED (0x80565323L)</para>
		/// <para>The specified volume is not enabled for deduplication.</para>
		/// <para>DDP_E_VOLUME_UNSUPPORTED (0x8056530bL)</para>
		/// <para>The specified volume type is not supported. Deduplication is supported on fixed, write-enabled NTFS data volumes.</para>
		/// <para><c>Windows Server 2012:  </c> This value is not supported before Windows Server 2012 R2.</para>
		/// </param>
		/// <returns>
		/// <para>
		/// This method can return standard <c>HRESULT</c> values, such as <c>S_OK</c>. It can also return converted system error codes using
		/// the HRESULT_FROM_WIN32 macro. You can test for success or failure <c>HRESULT</c> values by using the SUCCEEDED and FAILED macros
		/// defined in Winerror.h. Possible return values include the following.
		/// </para>
		/// <para>
		/// If no file was restored successfully, the result is the first file error encountered. This will be one of the "DDP_E_ <c>XXX</c>"
		/// error codes above.
		/// </para>
		/// </returns>
		/// <remarks>
		/// The <c>Store</c> parameter is required because the restore engine (implemented by Data Deduplication) can read data from the
		/// backup media only by calling the IDedupReadFileCallback::ReadBackupFile method.
		/// </remarks>
		// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/nf-ddpbackup-idedupbackupsupport-restorefiles HRESULT RestoreFiles(
		// [in] ULONG NumberOfFiles, [in] BSTR *FileFullPaths, [in] IDedupReadFileCallback *Store, [in] DWORD Flags, [out] HRESULT
		// *FileResults );
		[PreserveSig]
		HRESULT RestoreFiles(uint NumberOfFiles,
			[In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] FileFullPaths,
			[In, Optional] IDedupReadFileCallback? Store, DEDUP_BACKUP_SUPPORT_PARAM_TYPE Flags,
			[Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] HRESULT[]? FileResults);
	}

	/// <summary>
	/// A callback interface, implemented by backup applications, that enables Data Deduplication to read content from metadata and container
	/// files residing in a backup store and optionally improve restore efficiency.
	/// </summary>
	/// <remarks>
	/// The <c>IDedupReadFileCallback</c> interface is implemented by a backup application and passed as a parameter to the
	/// IDedupBackupSupport::RestoreFiles method. The callback is used by Data Deduplication to read data from Data Duplication store
	/// containers in the backup store. <c>IDedupReadFileCallback</c> also includes methods that applications can optionally implement to
	/// increase the efficiency of the Data Deduplication file restore process.
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/nn-ddpbackup-idedupreadfilecallback
	[PInvokeData("ddpbackup.h", MSDNShortId = "NN:ddpbackup.IDedupReadFileCallback")]
	[ComImport, Guid("7BACC67A-2F1D-42D0-897E-6FF62DD533BB"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IDedupReadFileCallback
	{
		/// <summary>Reads data from a Data Deduplication store metadata or container file located in the backup store.</summary>
		/// <param name="FileFullPath">The full path from the root directory of the volume to the container file.</param>
		/// <param name="FileOffset">The offset, in bytes, from the beginning of the file to the beginning of the data to be read.</param>
		/// <param name="SizeToRead">The number of bytes to read from the file.</param>
		/// <param name="FileBuffer">
		/// A pointer to a buffer that receives the data that is read from the file. The size of the buffer must be greater than or equal to
		/// the number specified in the <c>SizeToRead</c> parameter.
		/// </param>
		/// <param name="ReturnedSize">
		/// Pointer to a ULONG variable that receives the number of bytes that were read from the backup store. If the call to
		/// <c>ReadBackupFile</c> is successful, this number is equal to the value that was specified in the <c>SizeToRead</c> parameter.
		/// </param>
		/// <param name="Flags">This parameter is reserved for future use.</param>
		/// <returns>
		/// This method can return standard <c>HRESULT</c> values, such as <c>S_OK</c>. It can also return converted system error codes using
		/// the HRESULT_FROM_WIN32 macro. Possible return values include the following.
		/// </returns>
		// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/nf-ddpbackup-idedupreadfilecallback-readbackupfile HRESULT
		// ReadBackupFile( [in] BSTR FileFullPath, [in] hyper FileOffset, [in] ULONG SizeToRead, [out] BYTE *FileBuffer, [out] ULONG
		// *ReturnedSize, [in] DWORD Flags );
		[PreserveSig]
		HRESULT ReadBackupFile([In, MarshalAs(UnmanagedType.BStr)] string FileFullPath, long FileOffset,
			uint SizeToRead, [Out] byte[] FileBuffer, out uint ReturnedSize, uint Flags = 0);

		/// <summary>
		/// <para>
		/// This method provides the application with the ability to influence the order of the pending reads that are required to retrieve
		/// the target file.
		/// </para>
		/// <para>
		/// Given a list of container files that hold data for the restore target file, generates a list of container file extents in a
		/// sorted order that results in an efficient cross-container read plan from the backup store.
		/// </para>
		/// <para>Implementation of this method by the application is optional.</para>
		/// </summary>
		/// <param name="NumberOfContainers">Number of container paths in the <c>ContainerPaths</c> array.</param>
		/// <param name="ContainerPaths">
		/// Array of paths to container files that must be read in order to restore the file specified in the
		/// IDedupBackupSupport::RestoreFiles call. Each element is a full path from the root directory of the volume to a container file.
		/// </param>
		/// <param name="ReadPlanEntries">
		/// Pointer to a ULONG variable that receives the number of DEDUP_CONTAINER_EXTENT structures in the array that the <c>ReadPlan</c>
		/// parameter points to.
		/// </param>
		/// <param name="ReadPlan">Pointer to a buffer that receives an array of DEDUP_CONTAINER_EXTENT structures.</param>
		/// <returns>
		/// This method can return standard <c>HRESULT</c> values, such as <c>S_OK</c>. It can also return converted system error codes using
		/// the HRESULT_FROM_WIN32 macro. Possible return values include the following.
		/// </returns>
		/// <remarks>
		/// <para>
		/// Given a list of container files that hold data for the restore target file, the application optionally generates a list of
		/// container store file extents in a sorted order that results in an efficient cross-container read plan. For a backup store located
		/// on tape, this would normally be in tape order.
		/// </para>
		/// <para>
		/// In the case where a container is stored in multiple extents in the backup store—for example, as a result of an incremental backup
		/// sequence—the application may also return multiple container extents for each logical container file.
		/// </para>
		/// <para>
		/// The application may return <c>S_OK</c> and <c>NULL</c> output parameters to skip the read plan optimizations. In this case,
		/// container read order will be chosen by Data Deduplication.
		/// </para>
		/// </remarks>
		// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/nf-ddpbackup-idedupreadfilecallback-ordercontainersrestore HRESULT
		// OrderContainersRestore( [in] ULONG NumberOfContainers, [in] BSTR *ContainerPaths, [out] ULONG *ReadPlanEntries, [out]
		// DEDUP_CONTAINER_EXTENT **ReadPlan );
		[PreserveSig]
		HRESULT OrderContainersRestore(uint NumberOfContainers,
			[In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.BStr, SizeParamIndex = 0)] string[] ContainerPaths,
			out uint ReadPlanEntries, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] out DEDUP_CONTAINER_EXTENT[] ReadPlan);

		/// <summary>Provides the application with a preview of the sequence of reads that are pending for a given container file extent.</summary>
		/// <param name="FileFullPath">The full path from the root directory of the volume to the container file.</param>
		/// <param name="NumberOfReads">Number of DDP_FILE_EXTENT structures in the array that the <c>ReadOffsets</c> parameter points to.</param>
		/// <param name="ReadOffsets">Pointer to an array of DDP_FILE_EXTENT structures.</param>
		/// <returns>
		/// This method can return standard <c>HRESULT</c> values, such as <c>S_OK</c>. It can also return converted system error codes using
		/// the HRESULT_FROM_WIN32 macro. Possible return values include the following.
		/// </returns>
		/// <remarks>
		/// <c>PreviewContainerRead</c> is called for each container file extent reported by IDedupReadFileCallback::OrderContainersRestore.
		/// The application may use this preview as a per-container extent read plan to increase the efficiency of the pending reads. For
		/// example, the application may choose to perform read-ahead to improve throughput or to cache read buffers to improve overall
		/// performance across parallel file restore operations.
		/// </remarks>
		// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/nf-ddpbackup-idedupreadfilecallback-previewcontainerread HRESULT
		// PreviewContainerRead( [in] BSTR FileFullPath, [in] ULONG NumberOfReads, [in] DDP_FILE_EXTENT *ReadOffsets );
		[PreserveSig]
		HRESULT PreviewContainerRead([MarshalAs(UnmanagedType.BStr)] string FileFullPath, uint NumberOfReads,
			[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] DDP_FILE_EXTENT[] ReadOffsets);
	}

	/// <summary><c>DDP_FILE_EXTENT</c> represents the extent of data in a file that is to be read in a pending call to ReadBackupFile.</summary>
	/// <remarks>Data Deduplication needs to read only the portions of a container file that back the restore target file.</remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/ns-ddpbackup-ddp_file_extent typedef struct _DDP_FILE_EXTENT { hyper
	// Length; hyper Offset; } DDP_FILE_EXTENT;
	[PInvokeData("ddpbackup.h", MSDNShortId = "NS:ddpbackup._DDP_FILE_EXTENT")]
	[StructLayout(LayoutKind.Sequential)]
	public struct DDP_FILE_EXTENT
	{
		/// <summary>Length, in bytes, of the extent.</summary>
		public long Length;

		/// <summary>Offset, in bytes, from the beginning of the file to the beginning of the extent.</summary>
		public long Offset;
	}

	/// <summary>
	/// A logical container file may be stored in a single segment or multiple segments in the backup store. <c>DEDUP_CONTAINER_EXTENT</c>
	/// represents a single extent of a specific container file as stored in the backup store. The extent may be the full container file or a
	/// portion of the file.
	/// </summary>
	/// <remarks>
	/// For example, in an incremental backup scheme, the container may reside in the store either as one complete file generated in a full
	/// backup, or as multiple incremental files that contain changes in the file since the previous backup.
	/// </remarks>
	// https://learn.microsoft.com/en-us/windows/win32/api/ddpbackup/ns-ddpbackup-dedup_container_extent typedef struct
	// _DEDUP_CONTAINER_EXTENT { ULONG ContainerIndex; hyper StartOffset; hyper Length; } DEDUP_CONTAINER_EXTENT;
	[PInvokeData("ddpbackup.h", MSDNShortId = "NS:ddpbackup._DEDUP_CONTAINER_EXTENT")]
	[StructLayout(LayoutKind.Sequential)]
	public struct DEDUP_CONTAINER_EXTENT
	{
		/// <summary>
		/// The index in the container list passed to IDedupReadFileCallback::OrderContainersRestore to which this container extent structure corresponds.
		/// </summary>
		public uint ContainerIndex;

		/// <summary>Offset, in bytes, from the beginning of the container to the beginning of the extent.</summary>
		public long StartOffset;

		/// <summary>Length, in bytes, of the extent.</summary>
		public long Length;
	}

	/// <summary>CLSID_DedupBackupSupport</summary>
	[ComImport, Guid("73D6B2AD-2984-4715-B2E3-924C149744DD"), ClassInterface(ClassInterfaceType.None)]
	public class DedupBackupSupport { }
}