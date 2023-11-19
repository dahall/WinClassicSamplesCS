using System.IO;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.ProjectedFSLib;

namespace ProjectedFileSystem;

[Flags]
internal enum OptionalMethods
{
	None = 0,
	Notify = 0x1,
	QueryFileName = 0x2,
	CancelCommand = 0x4
};

[ComVisible(true), Guid("1d6e9474-bee1-4f33-8ae0-6ff1f2384996"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IVirtualizationInstance
{
	void CancelCommand(in PRJ_CALLBACK_DATA CallbackData);
	HRESULT EndDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId);
	HRESULT GetDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId, [In, Optional] string? SearchExpression, [In] PRJ_DIR_ENTRY_BUFFER_HANDLE DirEntryBufferHandle);
	HRESULT GetFileData(in PRJ_CALLBACK_DATA CallbackData, ulong ByteOffset, uint Length);
	HRESULT GetPlaceholderInfo(in PRJ_CALLBACK_DATA CallbackData);
	HRESULT Notify(in PRJ_CALLBACK_DATA CallbackData, bool IsDirectory, PRJ_NOTIFICATION NotificationType, string DestinationFileName, ref PRJ_NOTIFICATION_PARAMETERS NotificationParameters);
	HRESULT QueryFileName(in PRJ_CALLBACK_DATA CallbackData);
	HRESULT Start(string rootPath, in PRJ_STARTVIRTUALIZING_OPTIONS options = default);
	HRESULT StartDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId);
	void Stop();
	HRESULT WriteFileData(in Guid streamId, IntPtr buffer, ulong byteOffset, uint length);
	HRESULT WritePlaceholderInfo(string relativePath, in PRJ_PLACEHOLDER_INFO placeholderInfo, uint length);
}

internal abstract class VirtualizationInstance : IVirtualizationInstance
{
	protected PRJ_NAMESPACE_VIRTUALIZATION_CONTEXT _instanceHandle = default;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	protected string _rootPath;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	private PRJ_CALLBACKS _callbacks = default;
	private OptionalMethods _implementedOptionalMethods = OptionalMethods.None;
	private PRJ_STARTVIRTUALIZING_OPTIONS _options = default;
	private string instanceIdFile = "\\.regfsId";

	///////////////////////////////////////////////////////////////////////////////////////////////
	// Virtualization instance control API wrappers (user mode . kernel mode). The derived provider class should not override these methods.
	///////////////////////////////////////////////////////////////////////////////////////////////

	///////////////////////////////////////////////////////////////////////////////////////////////
	// Getter/Setter methods. The derived provider class (RegfsProvider) uses SetOptionalMethods() to indicate which optional callbacks
	// it overrode. This is required because ProjFS detects whether a provider implemented an optional callback by whether or not the
	// provider supplied a pointer to the callback in the callbacks parameter of PrjStartVirtualizing(). VirtualizationInstance.Start()
	// uses GetOptionalMethods() to find out which optional callbacks were implemented, and sets the corresponding C callback into the
	// callbacks parameter.
	///////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual OptionalMethods OptionalMethods { get => _implementedOptionalMethods; set => _implementedOptionalMethods |= value; }

	// Starts a virtualization instance at rootPath
	public HRESULT Start(string rootPath, in PRJ_STARTVIRTUALIZING_OPTIONS options = default)
	{
		_rootPath = rootPath;

		_options = options;

		// Ensure we have a virtualization root directory that is stamped with an instance ID using the PrjMarkDirectoryAsPlaceholder API.
		HRESULT hr = EnsureVirtualizationRoot();

		if (hr.Failed)
		{
			return hr;
		}

		// Register the required C callbacks.
		_callbacks.StartDirectoryEnumerationCallback = StartDirEnumCallback_C;
		_callbacks.EndDirectoryEnumerationCallback = EndDirEnumCallback_C;
		_callbacks.GetDirectoryEnumerationCallback = GetDirEnumCallback_C;
		_callbacks.GetPlaceholderInfoCallback = GetPlaceholderInfoCallback_C;
		_callbacks.GetFileDataCallback = GetFileDataCallback_C;

		// Register the optional C callbacks.

		// Register Notify if the provider says it implemented it, unless the provider didn't create any notification mappings.
		if ((OptionalMethods & OptionalMethods.Notify) != OptionalMethods.None && _options.NotificationMappingsCount != 0)
		{
			_callbacks.NotificationCallback = NotificationCallback_C;
		}

		// Register QueryFileName if the provider says it implemented it.
		if ((OptionalMethods & OptionalMethods.QueryFileName) != OptionalMethods.None)
		{
			_callbacks.QueryFileNameCallback = QueryFileName_C;
		}

		// Register CancelCommand if the provider says it implemented it.
		if ((OptionalMethods & OptionalMethods.CancelCommand) != OptionalMethods.None)
		{
			_callbacks.CancelCommandCallback = CancelCommand_C;
		}

		// Start the virtualization instance. Note that we pass our 'this' pointer in the instanceContext parameter. ProjFS will send
		// this context back to us when calling our callbacks, which will allow them to fish out this instance of the
		// VirtualizationInstance class and call our methods.
		hr = PrjStartVirtualizing(_rootPath, _callbacks, Marshal.GetIUnknownForObject(this), _options, out _instanceHandle);

		return hr;
	}

	// Stops a virtualization instance
	public void Stop()
	{
		PrjStopVirtualizing(_instanceHandle);
	}

	// Send file contents to ProjFS, ProjFS will write the data into the target placeholder file and convert it to hydrated placeholder state.
	public HRESULT WriteFileData(in Guid streamId, IntPtr buffer, ulong byteOffset, uint length)
	{
		return PrjWriteFileData(_instanceHandle, streamId, buffer, byteOffset, length);
	}

	// Send file meta data information to ProjFS, ProjFS will create an on-disk placeholder for the path.
	public HRESULT WritePlaceholderInfo(string relativePath, in PRJ_PLACEHOLDER_INFO placeholderInfo, uint length)
	{
		return PrjWritePlaceholderInfo(_instanceHandle, relativePath, placeholderInfo, length);
	}

	///////////////////////////////////////////////////////////////////////////////////////////////
	// Virtualization instance callbacks (kernel mode . user mode). These are the methods the derived provider class overrides.
	///////////////////////////////////////////////////////////////////////////////////////////////

	public virtual void CancelCommand(in PRJ_CALLBACK_DATA CallbackData) => throw new NotImplementedException();

	// [Mandatory] Inform the provider a directory enumeration is over. It corresponds to PRJ_END_DIRECTORY_ENUMERATION_CB in projectedfslib.h
	public abstract HRESULT EndDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId);

	// [Mandatory] Request directory enumeration information from the provider. It corresponds to PRJ_GET_DIRECTORY_ENUMERATION_CB in projectedfslib.h
	public abstract HRESULT GetDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId, [In, Optional] string? SearchExpression, [In] PRJ_DIR_ENTRY_BUFFER_HANDLE DirEntryBufferHandle);

	// [Mandatory] Request the contents of a file's primary data stream. It corresponds to PRJ_GET_FILE_DATA_CB in projectedfslib.h
	public abstract HRESULT GetFileData(in PRJ_CALLBACK_DATA CallbackData, ulong ByteOffset, uint Length);
	// [Mandatory] Request meta data information for a file or directory. It corresponds to PRJ_GET_PLACEHOLDER_INFO_CB in projectedfslib.h
	public abstract HRESULT GetPlaceholderInfo(in PRJ_CALLBACK_DATA CallbackData);

	// [Optional] Deliver notifications to the provider that it has specified it wishes to receive. It corresponds to
	// PRJ_NOTIFICATION_CB in projectedfslib.h
	public virtual HRESULT Notify(in PRJ_CALLBACK_DATA CallbackData, bool IsDirectory, PRJ_NOTIFICATION NotificationType, string DestinationFileName, ref PRJ_NOTIFICATION_PARAMETERS NotificationParameters) => throw new NotImplementedException();

	public virtual HRESULT QueryFileName(in PRJ_CALLBACK_DATA CallbackData) => throw new NotImplementedException();

	// [Mandatory] Inform the provider a directory enumeration is starting. It corresponds to PRJ_START_DIRECTORY_ENUMERATION_CB in projectedfslib.h
	public abstract HRESULT StartDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId);

	///////////////////////////////////////////////////////////////////////////////////////////////
	// Prototypes of the ProjFS C callbacks. ProjFS will call these, and they in turn will call the VirtualizationInstance class methods.
	///////////////////////////////////////////////////////////////////////////////////////////////

	private static void CancelCommand_C(in PRJ_CALLBACK_DATA CallbackData)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		instance.CancelCommand(CallbackData);
	}

	private static HRESULT EndDirEnumCallback_C(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.EndDirEnum(CallbackData, EnumerationId);
	}

	private static HRESULT GetDirEnumCallback_C(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId, string? SearchExpression, PRJ_DIR_ENTRY_BUFFER_HANDLE DirEntryBufferHandle)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.GetDirEnum(CallbackData, EnumerationId, SearchExpression, DirEntryBufferHandle);
	}

	private static HRESULT GetFileDataCallback_C(in PRJ_CALLBACK_DATA CallbackData, ulong ByteOffset, uint Length)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.GetFileData(CallbackData, ByteOffset, Length);
	}

	private static HRESULT GetPlaceholderInfoCallback_C(in PRJ_CALLBACK_DATA CallbackData)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.GetPlaceholderInfo(CallbackData);
	}

	private static HRESULT NotificationCallback_C(in PRJ_CALLBACK_DATA CallbackData, bool IsDirectory, PRJ_NOTIFICATION NotificationType, string DestinationFileName, ref PRJ_NOTIFICATION_PARAMETERS NotificationParameters)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.Notify(CallbackData, IsDirectory, NotificationType, DestinationFileName, ref NotificationParameters);
	}

	private static HRESULT QueryFileName_C(in PRJ_CALLBACK_DATA CallbackData)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.QueryFileName(CallbackData);
	}

	private static HRESULT StartDirEnumCallback_C(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId)
	{
		var instance = (IVirtualizationInstance)Marshal.GetObjectForIUnknown(CallbackData.InstanceContext);
		return instance.StartDirEnum(CallbackData, EnumerationId);
	}

	private HRESULT EnsureVirtualizationRoot()
	{
		Win32Error win32error;
		Guid instanceId = default;
		using var pInstanceId = new PinnedObject(instanceId);
		var guidSz = (uint)Marshal.SizeOf<Guid>();

		// Try creating our virtualization root.
		if (!CreateDirectory(_rootPath, default))
		{
			win32error = Win32Error.GetLastError();

			if (win32error == Win32Error.ERROR_ALREADY_EXISTS)
			{
				// The virtualization root already exists. Check for the stored virtualization instance ID.
				using var idFileHandle = CreateFile2(_rootPath + instanceIdFile, Kernel32.FileAccess.GENERIC_READ, FileShare.ReadWrite, FileMode.Open);
				if (idFileHandle.IsInvalid)
				{
					return (HRESULT)Win32Error.GetLastError();
				}

				if (!ReadFile(idFileHandle, pInstanceId, guidSz, out var bytesRead))
				{
					return (HRESULT)Win32Error.GetLastError();
				}

				// If we didn't read sizeof(Guid) bytes then this might not be our directory.
				if (bytesRead != guidSz)
				{
					return (HRESULT)(Win32Error)Win32Error.ERROR_BAD_CONFIGURATION;
				}
			}
			else
			{
				return (HRESULT)win32error;
			}
		}
		else
		{
			// We created a new directory. Create a virtualization instance ID.
			instanceId = Guid.NewGuid();

			// Store the ID in the directory as a way for us to detect that this is our directory in the future.
			using var idFileHandle = CreateFile2(_rootPath + instanceIdFile, Kernel32.FileAccess.GENERIC_WRITE, FileShare.ReadWrite, FileMode.CreateNew);

			if (idFileHandle.IsInvalid)
			{
				return (HRESULT)Win32Error.GetLastError();
			}

			if (!WriteFile(idFileHandle, pInstanceId, guidSz, out var bytesWritten))
			{
				return (HRESULT)Win32Error.GetLastError();
			}

			// Mark the directory as the virtualization root.
			var hr = PrjMarkDirectoryAsPlaceholder(_rootPath, default, default, instanceId);
			if (hr.Failed)
			{
				// Let's do a best-effort attempt to clean up the directory.
				DeleteFile(_rootPath + instanceIdFile);
				RemoveDirectory(_rootPath);

				return hr;
			}
		}

		return HRESULT.S_OK;
	}
}