using System.Reflection;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.ProjectedFSLib;

namespace ProjectedFileSystem;

internal class RegfsProvider : VirtualizationInstance
{
	// An enumeration session starts when StartDirEnum is invoked and ends when EndDirEnum is invoked. This tracks the active
	// enumeration sessions.
	private Dictionary<Guid, DirInfo> _activeEnumSessions = new();

	// If this flag is set to true, RegFS will block file content modifications for placeholder files.
	//private bool _readOnlyFileContent = true;

	// If this flag is set to true, RegFS will block the following namespace-altering operations that take place under virtualization root:
	// 1) file or directory deletion
	// 2) file or directory rename
	//
	// New file or folder create cannot be easily blocked due to limitations in ProjFS.
	private bool _readonlyNamespace = true;

	public RegfsProvider()
	{
		OptionalMethods = OptionalMethods.Notify;
	}

	public override HRESULT EndDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId)
	{
		Console.Write("\n---. {0}\n", MethodBase.GetCurrentMethod()!.Name);

		// Get rid of the DirInfo object we created in StartDirEnum.
		_activeEnumSessions.Remove(EnumerationId);

		Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, HRESULT.S_OK);

		return HRESULT.S_OK;
	}

	public override HRESULT GetDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId, [In, Optional] string? SearchExpression, [In] PRJ_DIR_ENTRY_BUFFER_HANDLE DirEntryBufferHandle)
	{
		Console.Write("\n---. {0}: Path [{1}] SearchExpression [{2}]\n", MethodBase.GetCurrentMethod()!.Name, CallbackData.FilePathName, SearchExpression);

		HRESULT hr = HRESULT.S_OK;

		// Get the correct enumeration session from our map.
		if (!_activeEnumSessions.TryGetValue(EnumerationId, out var dirInfo))
		{
			// We were asked for an enumeration we don't know about.
			hr = HRESULT.E_INVALIDARG;

			Console.Write("<---- {0}: Unknown enumeration ID\n", MethodBase.GetCurrentMethod()!.Name);

			return hr;
		}

		// If the enumeration is restarting, reset our bookkeeping information.
		if ((CallbackData.Flags & PRJ_CALLBACK_DATA_FLAGS.PRJ_CB_DATA_FLAG_ENUM_RESTART_SCAN) != 0)
		{
			dirInfo.Reset();
		}

		if (!dirInfo.EntriesFilled)
		{
			// The DirInfo associated with the current session hasn't been initialized yet. This method will enumerate the subkeys and
			// values in the registry key corresponding to CallbackData.FilePathName. For each one that matches SearchExpression it will
			// create an entry to return to ProjFS and store it in the DirInfo object.
			hr = PopulateDirInfoForPath(CallbackData.FilePathName, dirInfo, SearchExpression);

			if (hr.Failed)
			{
				Console.Write("<---- {0}: Failed to populate dirInfo: 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, hr);
				return hr;
			}

			// This will ensure the entries in the DirInfo are sorted the way the file system expects.
			dirInfo.SortEntriesAndMarkFilled();
		}

		// Return our directory entries to ProjFS.
		while (dirInfo.CurrentIsValid)
		{
			// ProjFS allocates a fixed size buffer then invokes this callback. The callback needs to call PrjFillDirEntryBuffer to fill
			// as many entries as possible until the buffer is full.
			if (HRESULT.S_OK != PrjFillDirEntryBuffer(dirInfo.CurrentFileName, dirInfo.CurrentBasicInfo, DirEntryBufferHandle))
			{
				break;
			}

			// Only move the current entry cursor after the entry was successfully filled, so that we can start from the correct index
			// in the next GetDirEnum callback for this enumeration session.
			dirInfo.MoveNext();
		}

		Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, hr);

		return hr;
	}

	public override HRESULT GetFileData(in PRJ_CALLBACK_DATA CallbackData, ulong ByteOffset, uint Length)
	{
		Console.Write("\n---. {0}: Path [{1}] triggered by [{2}]\n", MethodBase.GetCurrentMethod()!.Name, CallbackData.FilePathName, CallbackData.TriggeringProcessImageFileName);

		// We're going to need alignment information that is stored in the instance to service this callback.
		var hr = PrjGetVirtualizationInstanceInfo(_instanceHandle, out _);
		if (hr.Failed)
		{
			Console.Write("<---- {0}: PrjGetVirtualizationInstanceInfo: 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, hr);
			return hr;
		}

		// Allocate a buffer that adheres to the machine's memory alignment. We have to do this in case the caller who caused this
		// callback to be invoked is performing non-cached I/O. For more details, see the topic "Providing File Data" in the ProjFS documentation.
		var writeBuffer = PrjAllocateAlignedBuffer(_instanceHandle, Length);

		if (writeBuffer == default)
		{
			Console.Write("<---- {0}: Could not allocate write buffer.\n", MethodBase.GetCurrentMethod()!.Name);
			return HRESULT.E_OUTOFMEMORY;
		}

		// Read the data out of the registry.
		if (!RegOps.ReadValue(CallbackData.FilePathName, out var value))
		{
			hr = (HRESULT)(Win32Error)Win32Error.ERROR_FILE_NOT_FOUND;

			PrjFreeAlignedBuffer(writeBuffer);
			Console.Write("<---- {0}: Failed to read from registry.\n", MethodBase.GetCurrentMethod()!.Name);

			return hr;
		}

		// Call ProjFS to write the data we read from the registry into the on-disk placeholder.
		using var pWriteBuffer = new SafeHGlobalHandle(writeBuffer, Length, false);
		pWriteBuffer.Write(value!);
		hr = WriteFileData(CallbackData.DataStreamId, pWriteBuffer, ByteOffset, Length);

		if (hr.Failed)
		{
			// If this callback returns an error, ProjFS will return this error code to the thread that issued the file read, and the
			// target file will remain an empty placeholder.
			Console.Write("{0}: failed to write file for [{1}]: 0x{2:X8}\n", MethodBase.GetCurrentMethod()!.Name, CallbackData.FilePathName, hr);
		}

		// Free the memory-aligned buffer we allocated.
		PrjFreeAlignedBuffer(writeBuffer);

		Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, hr);

		return hr;
	}

	public override HRESULT GetPlaceholderInfo(in PRJ_CALLBACK_DATA CallbackData)
	{
		Console.Write("\n---. {0}: Path [{1}] triggered by [{2}] \n",
		MethodBase.GetCurrentMethod()!.Name, CallbackData.FilePathName, CallbackData.TriggeringProcessImageFileName);

		bool isKey;
		int valSize = 0;

		// Find out whether the specified path exists in the registry, and whether it is a key or a value.
		if (RegOps.DoesKeyExist(CallbackData.FilePathName))
		{
			isKey = true;
		}
		else if (RegOps.DoesValueExist(CallbackData.FilePathName))
		{
			isKey = false;
		}
		else
		{
			Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, Win32Error.ERROR_FILE_NOT_FOUND);
			return (HRESULT)(Win32Error)Win32Error.ERROR_FILE_NOT_FOUND;
		}

		// Format the PRJ_PLACEHOLDER_INFO structure. For registry keys we create directories on disk, for values we create files.
		PRJ_PLACEHOLDER_INFO placeholderInfo = default;
		placeholderInfo.FileBasicInfo.IsDirectory = isKey;
		placeholderInfo.FileBasicInfo.FileSize = valSize;

		// Create the on-disk placeholder.
		HRESULT hr = WritePlaceholderInfo(CallbackData.FilePathName, placeholderInfo, (uint)Marshal.SizeOf(placeholderInfo));

		Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, hr);

		return hr;
	}

	public override HRESULT Notify(in PRJ_CALLBACK_DATA CallbackData, bool IsDirectory, PRJ_NOTIFICATION NotificationType, string DestinationFileName, ref PRJ_NOTIFICATION_PARAMETERS NotificationParameters)
	{
		HRESULT hr = HRESULT.S_OK;

		Console.Write("\n---. {0}: Path [{1}] triggered by [{2}]", MethodBase.GetCurrentMethod()!.Name, CallbackData.FilePathName, CallbackData.TriggeringProcessImageFileName);
		Console.Write("\n---- Notification: 0x{0}\n", NotificationType);

		switch (NotificationType)
		{
			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_FILE_OPENED:

				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_FILE_HANDLE_CLOSED_FILE_MODIFIED:
			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_FILE_OVERWRITTEN:

				Console.Write("\n ----- [{0}] was modified\n", CallbackData.FilePathName);
				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_NEW_FILE_CREATED:

				Console.Write("\n ----- [{0}] was created\n", CallbackData.FilePathName);
				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_FILE_RENAMED:

				Console.Write("\n ----- [{0}] . [{1}]\n", CallbackData.FilePathName, DestinationFileName);
				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_FILE_HANDLE_CLOSED_FILE_DELETED:

				Console.Write("\n ----- [{0}] was deleted\n", CallbackData.FilePathName);
				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_PRE_RENAME:

				if (_readonlyNamespace)
				{
					// Block file renames.
					hr = (HRESULT)(Win32Error)Win32Error.ERROR_ACCESS_DENIED;
					Console.Write("\n ----- rename request for [{0}] was rejected \n", CallbackData.FilePathName);
				}
				else
				{
					Console.Write("\n ----- rename request for [{0}] \n", CallbackData.FilePathName);
				}
				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_PRE_DELETE:

				if (_readonlyNamespace)
				{
					// Block file deletion. We must return a particular NTStatus to ensure the file system properly recognizes that this
					// is a deny-delete.
					hr = (HRESULT)(NTStatus)NTStatus.STATUS_CANNOT_DELETE;
					Console.Write("\n ----- delete request for [{0}] was rejected \n", CallbackData.FilePathName);
				}
				else
				{
					Console.Write("\n ----- delete request for [{0}] \n", CallbackData.FilePathName);
				}
				break;

			case PRJ_NOTIFICATION.PRJ_NOTIFICATION_FILE_PRE_CONVERT_TO_FULL:

				break;

			default:

				Console.Write("{0}: Unexpected notification\n", MethodBase.GetCurrentMethod()!.Name);
				break;
		}

		Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, hr);
		return hr;
	}

	public override HRESULT StartDirEnum(in PRJ_CALLBACK_DATA CallbackData, in Guid EnumerationId)
	{
		Console.Write("\n---. {0}: Path [{1}] triggerred by [{2}]\n", MethodBase.GetCurrentMethod()!.Name, CallbackData.FilePathName, CallbackData.TriggeringProcessImageFileName);

		// For each dir enum session, ProjFS sends: one StartEnumCallback one or more GetEnumCallbacks one EndEnumCallback These
		// callbacks will use the same value for EnumerationId for the same session. Here we map the EnumerationId to a new DirInfo object.
		_activeEnumSessions[EnumerationId] = new DirInfo(CallbackData.FilePathName);

		Console.Write("<---- {0}: return 0x{1:X8}\n", MethodBase.GetCurrentMethod()!.Name, HRESULT.S_OK);

		return HRESULT.S_OK;
	}

	private HRESULT PopulateDirInfoForPath(string relativePath, in DirInfo dirInfo, string? searchExpression)
	{
		// Get a list of the registry keys and values under the given key.
		HRESULT hr = RegOps.EnumerateKey(relativePath, out var entries);
		if (hr.Failed)
		{
			Console.Write("{0}: Could not enumerate key: 0x{0:X8}", MethodBase.GetCurrentMethod()!.Name, hr);
			return hr;
		}

		// Store each registry key that matches searchExpression as a directory entry.
		foreach (var subKey in entries.SubKeys)
		{
			if (PrjFileNameMatch(subKey, searchExpression))
			{
				dirInfo.FillDirEntry(subKey);
			}
		}

		// Store each registry value that matches searchExpression as a file entry.
		foreach (var val in entries.Values)
		{
			if (PrjFileNameMatch(val, searchExpression))
			{
				dirInfo.FillFileEntry(val);
			}
		}

		return hr;
	}
}