using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.IMAPI;
using static Vanara.PInvoke.ShlwApi;

namespace Vanara.Storage
{
	public interface IOpticalStorageImage
	{
		IStream GetImageStream();
	}

	public interface IOpticalStorageMediaOperation
	{
		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="OpticalStorageDevice"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		OpticalStorageDevice Device { get; set; }

		/// <summary>Executes the operation on media in the device identified by <see cref="Device"/>.</summary>
		void Execute();

		/// <summary>Determines if the current media in a supported operation supports the given format.</summary>
		/// <param name="device">A device to test.</param>
		/// <returns>
		/// <para>Is <see langword="true"/> if the media in the operation supports the given format; otherwise, <see langword="false"/>.</para>
		/// <para><c>Note</c><see langword="true"/> also implies that the result from IsDiscoperationSupported is <see langword="true"/>.</para>
		/// </returns>
		bool SupportsCurrentMediaInDevice(OpticalStorageDevice device);

		/// <summary>Determines if the operation supports the given format.</summary>
		/// <param name="device">A device to test.</param>
		/// <returns>Is <see langword="true"/> if the operation supports the given format; otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// When implemented by the IDiscFormat2RawCD interface, this method will return E_IMAPI_DF2RAW_MEDIA_IS_NOT_SUPPORTED in the event
		/// the operation does not support the given format. It is important to note that in this specific scenario the value does not
		/// indicate that an error has occurred, but rather the result of a successful operation.
		/// </remarks>
		bool SupportsDevice(OpticalStorageDevice device);
	}

	/// <summary>Static class that enumerates the CD and DVD devices installed on the local machine.</summary>
	public static class OpticalStorageManager
	{
		private static readonly ComConnectionPoint connPt;
		private static readonly IDiscMaster2 diskMaster = new();
		private static readonly Lazy<bool> isSupported = new(() => new IDiscMaster2().IsSupportedEnvironment);
		private static OpticalStorageDevice userDefDevice = null;

		static OpticalStorageManager() => connPt = new ComConnectionPoint(diskMaster, new DDiscMaster2EventsSink(Added, Removed));

		/// <summary>Receives notification when an optical media device is added to the computer.</summary>
		public static event Action<OpticalStorageDevice> DeviceAdded;

		/// <summary>Receives notification when an optical media device is removed from the computer.</summary>
		public static event Action<OpticalStorageDevice> DeviceRemoved;

		public static OpticalStorageDevice DefaultDevice
		{
			get => userDefDevice is null ? (diskMaster.Count > 0 ? Devices[0] : null) : userDefDevice;
			set => userDefDevice = value;
		}

		/// <summary>Retrieves a list of the CD and DVD devices installed on the computer.</summary>
		/// <returns>A list of the CD and DVD devices installed on the computer.</returns>
		/// <remarks>
		/// The enumeration is a snapshot of the devices on the computer at the time of the call and will not reflect devices that are added
		/// and removed. To receive notification when a device is added or removed from the computer, implement the <see
		/// cref="DeviceAdded"/> and <see cref="DeviceRemoved"/> events.
		/// </remarks>
		public static OpticalStorageDeviceCollection Devices => new(diskMaster);

		/// <summary>
		/// Retrieves a value that determines if the environment contains one or more optical devices and the execution context has
		/// permission to access the devices.
		/// </summary>
		/// <value>
		/// Is <see langword="true"/> if the environment contains one or more optical devices and the execution context has permission to
		/// access the devices; otherwise, false.
		/// </value>
		/// <remarks>The environment must contain at least one type-5 optical device.</remarks>
		public static bool IsSupportedEnvironment => isSupported.Value;

		private static void Added(IDiscMaster2 o, string uid) => DeviceAdded?.Invoke(new OpticalStorageDevice(uid));

		private static void Removed(IDiscMaster2 o, string uid)
		{
			if (userDefDevice is not null && uid == userDefDevice.UID) userDefDevice = null;
			DeviceRemoved?.Invoke(new OpticalStorageDevice(uid));
		}
	}

	/// <summary>
	/// Receives progress notification of the current write operation. The notifications are sent when copying the content of a file or
	/// while adding directories or files to the file system image.
	/// </summary>
	public class FileSystemImageUpdateEventArgs : EventArgs
	{
		/// <summary>Initializes a new instance of the <see cref="FileSystemImageUpdateEventArgs"/> class.</summary>
		/// <param name="currentFile">String that contains the full path of the file being written.</param>
		/// <param name="copiedSectors">Number of sectors copied.</param>
		/// <param name="totalSectors">Total number of sectors in the file.</param>
		public FileSystemImageUpdateEventArgs(string currentFile, long copiedSectors, long totalSectors)
		{
			CurrentFile = currentFile;
			CopiedSectors = copiedSectors;
			TotalSectors = totalSectors;
		}

		/// <summary>Gets the number of sectors copied.</summary>
		/// <value>The copied sectors.</value>
		public long CopiedSectors { get; }

		/// <summary>Gets a string that contains the full path of the file being written.</summary>
		/// <value>The current file.</value>
		public string CurrentFile { get; }

		/// <summary>Gets the total number of sectors in the file.</summary>
		/// <value>The total sectors.</value>
		public long TotalSectors { get; }
	}

	/// <summary>
	/// Use this class to specify the boot image to add to the optical disc. A boot image contains one or more sectors of code used to start
	/// the computer.
	/// </summary>
	/// <remarks>This interface supports the "El Torito" Bootable CD-ROM format specification.</remarks>
	public class OpticalStorageBootOptions
	{
		internal readonly IBootOptions options;
		internal readonly ComFileStream bootImage;

		/// <summary>Initializes a new instance of the <see cref="OpticalStorageBootOptions"/> class.</summary>
		/// <param name="bootImage">An <c>IStream</c> interface of the data stream that contains the boot image.</param>
		/// <remarks>
		/// <para>
		/// If the size of the newly assigned boot image is either 1.2, 1.44. or 2.88 MB, this method will automatically adjust the
		/// EmulationType value to the respective "floppy" type value. It is, however, possible to override the default or previously
		/// assigned <c>EmulationType</c> value by setting the Emulation property.
		/// </para>
		/// <para>The additional specification of the platform on which to use thme boot image requires the call to the PlatformId property.</para>
		/// <para>IMAPI does not include any boot images; developers must provide their own boot images.</para>
		/// </remarks>
		public OpticalStorageBootOptions(IStream bootImage)
		{
			options = new();
			options.AssignBootImage(bootImage);
		}

		/// <summary>Initializes a new instance of the <see cref="OpticalStorageBootOptions"/> class.</summary>
		/// <param name="bootImageFile">A file that contains the boot image.</param>
		/// <remarks>
		/// <para>
		/// If the size of the newly assigned boot image is either 1.2, 1.44. or 2.88 MB, this method will automatically adjust the
		/// EmulationType value to the respective "floppy" type value. It is, however, possible to override the default or previously
		/// assigned <c>EmulationType</c> value by setting the Emulation property.
		/// </para>
		/// <para>The additional specification of the platform on which to use the boot image requires the call to the PlatformId property.</para>
		/// <para>IMAPI does not include any boot images; developers must provide their own boot images.</para>
		/// </remarks>
		public OpticalStorageBootOptions(string bootImageFile)
		{
			options = new();
			bootImage = new(bootImageFile, true, 0);
			options.AssignBootImage(bootImage.NativeInterface);
		}

		/// <summary>Sets the media type that the boot image is intended to emulate.</summary>
		/// <value>
		/// Media type that the boot image is intended to emulate. For possible values, see the EmulationType enumeration type. The default
		/// value is <c>EmulationNone</c>, which means the BIOS will not emulate any device type or special sector size for the CD during
		/// boot from the CD.
		/// </value>
		[DefaultValue(EmulationType.EmulationNone)]
		public EmulationType Emulation { get => options.Emulation; set => options.Emulation = value; }

		/// <summary>Sets an identifier that identifies the manufacturer or developer of the CD.</summary>
		/// <value>
		/// Identifier that identifies the manufacturer or developer of the CD. This is an ANSI string that is limited to 24 bytes. The
		/// string does not need to include a NULL character; however, you must set unused bytes to 0x00.
		/// </value>
		[DefaultValue(null)]
		public string Manufacturer { get => options.Manufacturer; set => options.Manufacturer = value; }

		/// <summary>Gets the underlying native COM interface.</summary>
		/// <value>The native interface.</value>
		public IBootOptions NativeInterface => options;

		/// <summary>Sets the platform identifier that identifies the operating system architecture that the boot image supports.</summary>
		/// <value>
		/// Identifies the operating system architecture that the boot image supports. For possible values, see the PlatformId enumeration
		/// type. The default value is <c>PlatformX86</c> for Intel x86–based platforms.
		/// </value>
		[DefaultValue(PlatformId.PlatformX86)]
		public PlatformId PlatformId { get => options.PlatformId; set => options.PlatformId = value; }

		/// <summary>Retrieves a pointer to the boot image data stream.</summary>
		/// <value>Pointer to the <c>IStream</c> interface associated with the boot image data stream.</value>
		public IStream BootImage => options.BootImage;

		/// <summary>Retrieves the size of the boot image.</summary>
		/// <value>Size, in bytes, of the boot image.</value>
		public uint ImageSize => options.ImageSize;
	}

	/// <summary>
	/// This class represents a physical device. You use this class to retrieve information about a CD and DVD device installed on the
	/// computer and to perform operations such as closing the tray or ejecting the media.
	/// </summary>
	public class OpticalStorageDevice
	{
		internal readonly IDiscFormat2Data mediaImage;
		internal readonly IDiscRecorder2 recorder;
		internal IDiscRecorder2Ex recorderEx;

		/// <summary>Initializes a new instance of the <see cref="OpticalStorageDevice"/> class.</summary>
		/// <param name="uid">String that contains the unique identifier for the device.</param>
		public OpticalStorageDevice(string uid) : this() => recorder.InitializeDiscRecorder(uid);

		private OpticalStorageDevice()
		{
			recorder = new();
			mediaImage = new() { Recorder = recorder };
		}

		/// <summary>Retrieves the adapter descriptor for the device.</summary>
		/// <value>The adapter descriptor.</value>
		public Kernel32.STORAGE_ADAPTER_DESCRIPTOR AdapterDescriptor
		{
			get
			{
				NativeInterfaceEx.GetAdapterDescriptor(out var data, out var size);
				return data.DangerousGetHandle().ToStructure<Kernel32.STORAGE_ADAPTER_DESCRIPTOR>(size);
			}
		}

		/// <summary>Retrieves the byte alignment mask for the device.</summary>
		/// <value>
		/// Byte alignment mask that you use to determine if the buffer is aligned to the correct byte boundary for the device. The byte
		/// alignment value is always a number that is a power of 2.
		/// </value>
		public uint ByteAlignmentMask => NativeInterfaceEx.GetByteAlignmentMask();

		/// <summary>Gets the type of the current physical media.</summary>
		/// <value>The type of the current physical media.</value>
		public IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalType => NativeInterfaceEx.GetCurrentPhysicalMediaType();

		/// <summary>Retrieves the device descriptor for the device.</summary>
		/// <value>The device descriptor.</value>
		public Kernel32.STORAGE_ADAPTER_DESCRIPTOR DeviceDescriptor
		{
			get
			{
				NativeInterfaceEx.GetDeviceDescriptor(out var data, out var size);
				return data.DangerousGetHandle().ToStructure<Kernel32.STORAGE_ADAPTER_DESCRIPTOR>(size);
			}
		}

		/// <summary>Determines if the device can eject and subsequently reload media.</summary>
		/// <value>
		/// <para>
		/// Is <see langword="true"/> if the device can eject and subsequently reload media. If <see langword="false"/>, loading media must
		/// be done manually.
		/// </para>
		/// <para>
		/// <c>Note</c> For slim drives or laptop drives, which utilize a manual tray-loading mechanism, this parameter can indicate an
		/// incorrect value of <see langword="true"/>.
		/// </para>
		/// </value>
		public bool CanLoadMedia => recorder.DeviceCanLoadMedia;

		///// <summary>
		///// Gets the <see cref="OpticalStorageMedia"/> instance for the current media. This value is <see langword="null"/> if no media is loaded.
		///// </summary>
		///// <value>The <see cref="OpticalStorageMedia"/> instance for the current media..</value>
		//public OpticalStorageMedia Media => OpticalStorageMedia.Create(recorder, mediaImage);

		/// <summary>Gets the underlying native COM interface.</summary>
		/// <value>The native interface.</value>
		public IDiscRecorder2 NativeInterface => recorder;

		/// <summary>Gets the extended underlying native COM interface.</summary>
		/// <value>The extended native interface.</value>
		public IDiscRecorder2Ex NativeInterfaceEx => recorderEx ??= (IDiscRecorder2Ex)recorder;

		/// <summary>Retrieves the product revision code of the device.</summary>
		/// <value>String that contains the product revision code of the device.</value>
		public string ProductVersion => recorder.ProductRevision;

		/// <summary>Retrieves the media types that are supported.</summary>
		/// <value>
		/// List of media types supported by the device. For a list of media types, see the IMAPI_MEDIA_PHYSICAL_TYPE enumeration type.
		/// </value>
		public IMAPI_MEDIA_PHYSICAL_TYPE[] SupportedMediaTypes
		{
			get
			{
				var media = new IDiscFormat2Data { Recorder = recorder };
				return media.SupportedMediaTypes;
			}
		}

		/// <summary>Retrieves the unique identifier used to initialize the disc device.</summary>
		/// <value>Unique identifier for the device. This is the identifier you specified in the constructor.</value>
		public string UID => recorder.ActiveDiscRecorder;

		/// <summary>Retrieves the list of feature pages of the device that are marked as current.</summary>
		/// <value>
		/// List of supported feature pages that are marked as current for the device. For possible values, see the IMAPI_FEATURE_PAGE_TYPE enumeration.
		/// </value>
		public IMAPI_FEATURE_PAGE_TYPE[] CurrentFeaturePages => recorder.CurrentFeaturePages;

		/// <summary>Retrieves all MMC profiles of the device that are marked as current.</summary>
		/// <value>
		/// List of supported profiles that are marked as current for the device. For possible values, see the IMAPI_PROFILE_TYPE enumeration.
		/// </value>
		public IMAPI_PROFILE_TYPE[] CurrentProfiles => recorder.CurrentProfiles;

		/// <summary>Retrieves the name of the client application that has exclusive access to the device.</summary>
		/// <value>String that contains the name of the client application that has exclusive access to the device.</value>
		/// <remarks>
		/// This property returns the current exclusive access owner of the device. This value comes directly from CDROM.SYS and should be
		/// queried anytime an operation fails with error E_IMAPI_RECORDER_LOCKED.
		/// </remarks>
		public string ExclusiveAccessOwner => recorder.ExclusiveAccessOwner;

		/// <summary>Retrieves the product ID of the device.</summary>
		/// <value>String that contains the product ID of the device.</value>
		public string ProductId => recorder.ProductId;

		/// <summary>Retrieves the list of features that the device supports.</summary>
		/// <value>List of features that the device supports. For possible values, see the IMAPI_FEATURE_PAGE_TYPE enumeration type.</value>
		public IMAPI_FEATURE_PAGE_TYPE[] SupportedFeaturePages => recorder.SupportedFeaturePages;

		/// <summary>Retrieves the list of MMC mode pages that the device supports.</summary>
		/// <value>List of MMC mode pages that the device supports. For possible values, see the IMAPI_MODE_PAGE_TYPE enumeration type.</value>
		public IMAPI_MODE_PAGE_TYPE[] SupportedModePages => recorder.SupportedModePages;

		/// <summary>Retrieves the list of MMC profiles that the device supports.</summary>
		/// <value>List of MMC profiles that the device supports. For possible values, see the IMAPI_PROFILE_TYPE enumeration type.</value>
		public IMAPI_PROFILE_TYPE[] SupportedProfiles => recorder.SupportedProfiles;

		/// <summary>Retrieves the vendor ID for the device.</summary>
		/// <value>String that contains the vendor ID for the device.</value>
		public string VendorId => recorder.VendorId;

		/// <summary>Retrieves the unique volume name associated with the device.</summary>
		/// <value>String that contains the unique volume name associated with the device.</value>
		/// <remarks>To retrieve the drive letter assignment, use the <see cref="VolumePathNames"/> property.</remarks>
		public string VolumeName => recorder.VolumeName;

		/// <summary>Retrieves a list of drive letters and NTFS mount points for the device.</summary>
		/// <value>List of drive letters and NTFS mount points for the device.</value>
		public string[] VolumePathNames => recorder.VolumePathNames;

		/// <summary>Acquires exclusive access to the device.</summary>
		/// <param name="clientName">
		/// String that contains the friendly name of the client application requesting exclusive access. Cannot be <see langword="null"/>
		/// or a zero-length string. The string must conform to the restrictions for the IOCTL_CDROM_EXCLUSIVE_ACCESS control code found in
		/// the DDK.
		/// </param>
		/// <param name="force">
		/// Set to <see langword="true"/> to gain exclusive access to the volume whether the file system volume can or cannot be dismounted.
		/// If <see langword="false"/>, this method gains exclusive access only when there is no file system mounted on the volume.
		/// </param>
		/// <remarks>
		/// <para>
		/// You should not have to call this method to acquire the lock yourself because the write operations acquire the lock for you.
		/// </para>
		/// <para>
		/// Each device has a lock count. The first call to a device locks the device for exclusive access. Applications can use the
		/// <c>AcquireExclusiveAccess</c> method multiple times to apply multiple locks on a device. Each call increments the lock count by one.
		/// </para>
		/// <para>
		/// When unlocking a recorder, the lock count must reach zero to free the device for other clients. Calling the <see
		/// cref="ReleaseExclusiveAccess"/> method decrements the lock count by one.
		/// </para>
		/// <para>
		/// An equal number of calls to the <c>AcquireExclusiveAccess</c> and ReleaseExclusiveAccess methods are needed to free a device.
		/// Should the application exit unexpectedly or crash while holding the exclusive access, the CDROM.SYS driver will automatically
		/// release these exclusive locks.
		/// </para>
		/// <para>
		/// If the device is already locked, you can call <see cref="ExclusiveAccessOwner"/> to retrieve the name of the client application
		/// that currently has exclusive access.
		/// </para>
		/// </remarks>
		public void AcquireExclusiveAccess(string clientName, bool force = true) => recorder.AcquireExclusiveAccess(force, clientName);

		/// <summary>Closes the media tray.</summary>
		/// <remarks>
		/// <c>Note</c> Some drives, such as those with slot-loading mechanisms, do not support this method. To determine if the device
		/// supports this method, call the IDiscRecorder2.DeviceCanLoadMedia property.
		/// </remarks>
		public void CloseTray() => recorder.CloseTray();

		/// <summary>Disables Media Change Notification (MCN) for the device.</summary>
		/// <remarks>
		/// <para>
		/// MCN is the CD-ROM device driver's method of detecting media change and state changes in the CD-ROM device. For example, when you
		/// change the media in a CD-ROM device, a MCN message is sent to trigger media features, such as Autoplay. To disable the features,
		/// call this method.
		/// </para>
		/// <para>
		/// To enable notifications, call the <see cref="EnableMediaChangeNotifications"/> method. If the application crashes or closes
		/// unexpectedly, then MCN is automatically re-enabled by the driver.
		/// </para>
		/// <para>
		/// Note that <see cref="DisableMediaChangeNotifications"/> increments a reference count each time it is called. The <see
		/// cref="EnableMediaChangeNotifications"/> method decrements the count. The device is enabled when the reference count is zero.
		/// </para>
		/// </remarks>
		public void DisableMediaChangeNotifications() => recorder.DisableMcn();

		/// <summary>Enables Media Change Notification (MCN) for the device.</summary>
		/// <remarks>
		/// <para>
		/// MCN is the CD-ROM device driver's method of detecting media change and state changes in the CD-ROM device. For example, when you
		/// change the media in a CD-ROM device, a MCN message is sent to trigger media features, such as Autoplay. MCN is enabled by
		/// default. Call this method to enable notifications when the notifications have been disabled using <see cref="DisableMediaChangeNotifications"/>.
		/// </para>
		/// <para>
		/// Note that <see cref="DisableMediaChangeNotifications"/> increments a reference count each time it is called. The <see
		/// cref="EnableMediaChangeNotifications"/> method decrements the count. The device is enabled when the reference count is zero.
		/// </para>
		/// </remarks>
		public void EnableMediaChangeNotifications() => recorder.EnableMcn();

		public void EraseMedia(bool fullErase = true, string clientName = null)
		{
			// Setup erasure
			OpticalStorageEraseMediaOperation op = new(fullErase, clientName) { Device = this };
			// Execute
			op.Execute();
		}

		public void Execute(IOpticalStorageMediaOperation operation)
		{
			operation.Device = this; operation.Execute();
		}

		/// <summary>Ejects media from the device.</summary>
		public void OpenTray() => recorder.EjectMedia();

		/// <summary>Releases exclusive access to the device.</summary>
		/// <remarks>
		/// <para>
		/// Each device has a lock count. The first call to a device locks the device for exclusive access. Applications can use the
		/// <c>AcquireExclusiveAccess</c> method multiple times to apply multiple locks on a device. Each call increments the lock count by one.
		/// </para>
		/// <para>
		/// When unlocking a recorder, the lock count must reach zero to free the device for other clients. Calling the <see
		/// cref="ReleaseExclusiveAccess"/> method decrements the lock count by one.
		/// </para>
		/// <para>
		/// An equal number of calls to the <c>AcquireExclusiveAccess</c> and ReleaseExclusiveAccess methods are needed to free a device.
		/// Should the application exit unexpectedly or crash while holding the exclusive access, the CDROM.SYS driver will automatically
		/// release these exclusive locks.
		/// </para>
		/// </remarks>
		public void ReleaseExclusiveAccess() => recorder.ReleaseExclusiveAccess();

		public void WriteAudioTracksToMedia(IEnumerable<string> audioFilePaths, string clientName = null)
		{
			OpticalStorageWriteAudioOperation op = new(clientName) { Device = this };
			op.AudioTrackPaths.AddRange(audioFilePaths);
			op.Execute();
		}

		public void WriteDirectoryToMedia(string directoryPath, FsiFileSystems fileSystems = FsiFileSystems.FsiFileSystemJoliet | FsiFileSystems.FsiFileSystemISO9660,
					string volumeName = null, OpticalStorageBootOptions bootOptions = null, bool forceMediaToBeClosed = false, string clientName = null)
		{
			OpticalStorageWriteOperation op = new(clientName)
			{
				Device = this,
				ForceMediaToBeClosed = forceMediaToBeClosed,
			};
			OpticalStorageFileSystemImage image = new(directoryPath, fileSystems, volumeName, bootOptions)
			{
				FreeMediaBlocks = op.FreeSectorsOnMedia,
				MultisessionInterfaces = op.MultisessionInterfaces,
			};
			op.Data = image.GetImageStream();
			op.Execute();
		}

		public void WriteRawImageToMedia(OpticalStorageRawImage image, IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE sectorType = IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE.IMAPI_FORMAT2_RAW_CD_SUBCODE_IS_COOKED, string clientName = null)
		{
			OpticalStorageWriteRawOperation op = new(clientName)
			{
				Data = image.GetImageStream(),
				Device = this,
				RequestedSectorType = sectorType,
			};
			op.Execute();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Validate(IDiscFormat2 fmtr)
		{
			//try { _ = fmtr.GetCurrentPhysicalMediaType(); } catch { throw new InvalidOperationException("Media is not present."); }
			//if (!fmtr.IsCurrentMediaSupported(fmtr.GetRecorder())) throw new InvalidOperationException("Media is not supported for this operation.");
		}
	}

	/// <summary>Retrieves a list of the CD and DVD devices installed on the computer.</summary>
	public class OpticalStorageDeviceCollection : IReadOnlyList<OpticalStorageDevice>
	{
		private readonly IDiscMaster2 diskMaster;

		/// <summary>Initializes a new instance of the <see cref="OpticalStorageDeviceCollection"/> class.</summary>
		public OpticalStorageDeviceCollection() => diskMaster = new();

		internal OpticalStorageDeviceCollection(IDiscMaster2 master) => diskMaster = master;

		/// <summary>Gets the number of elements in the collection.</summary>
		public int Count => diskMaster.Count;

		/// <summary>Gets the <see cref="OpticalStorageDevice"/> at the specified index.</summary>
		/// <value>The <see cref="OpticalStorageDevice"/>.</value>
		/// <param name="index">The index.</param>
		/// <returns>The <see cref="OpticalStorageDevice"/> at the specified index.</returns>
		public OpticalStorageDevice this[int index] => this.ElementAt(index);

		/// <summary>Gets the <see cref="OpticalStorageDevice"/> at the specified index.</summary>
		/// <value>The <see cref="OpticalStorageDevice"/>.</value>
		/// <param name="uidOrVolPath">String that contains the unique identifier, the drive letter, or NTFS mount point for the device.</param>
		/// <returns>The <see cref="OpticalStorageDevice"/> at the specified index.</returns>
		public OpticalStorageDevice this[string uidOrVolPath]
		{
			get
			{
				try { return new OpticalStorageDevice(uidOrVolPath); }
				catch
				{
					uidOrVolPath = uidOrVolPath.ToLowerInvariant();
					OpticalStorageDevice ret = this.FirstOrDefault(d => d.VolumePathNames.Any(p => p.ToLowerInvariant().Equals(uidOrVolPath)));
					return ret ?? throw new ArgumentOutOfRangeException(nameof(uidOrVolPath), "Invalid device UID or volume.");
				}
			}
		}

		/// <summary>Returns an enumerator that iterates through the collection.</summary>
		/// <returns>An enumerator that can be used to iterate through the collection.</returns>
		public IEnumerator<OpticalStorageDevice> GetEnumerator() => diskMaster.Cast<string>().Select(id => new OpticalStorageDevice(id)).GetEnumerator();

		/// <summary>Returns an enumerator that iterates through a collection.</summary>
		/// <returns>An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.</returns>
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>Represents an erase operation on an optical storage device.</summary>
	public class OpticalStorageEraseMediaOperation : OpticalStorageMediaOperation<IDiscFormat2Erase>
	{
		/// <summary>Initializes a new instance of the <see cref="OpticalStorageEraseMediaOperation"/> class.</summary>
		/// <param name="fullErase">
		/// <para>Set to <see langword="true"/> to fully erase the disc by overwriting the entire medium at least once.</para>
		/// <para>
		/// Set to <see langword="false"/> to overwrite the directory tracks, but not the entire disc. This option requires less time to
		/// perform than the full erase option.
		/// </para>
		/// </param>
		/// <param name="clientName">
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock. If <see
		/// langword="null"/>, the name is pulled from the app domain.
		/// </param>
		public OpticalStorageEraseMediaOperation(bool fullErase = false, string clientName = null) : base(new(), clientName) => op.FullErase = fullErase;

		/// <summary>Occurs during <see cref="EraseMedia"/> to indicate the progress.</summary>
		public event EventHandler<ProgressChangedEventArgs> EraseProgress;

		/// <summary>Sets the friendly name of the client.</summary>
		/// <value>Name of the client application.</value>
		/// <remarks>
		/// <para>
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock.
		/// </para>
		/// <para>
		/// Because any application with read/write access to the device during the erase operation can use the IOCTL_CDROM_EXCLUSIVE_ACCESS
		/// (query) control code (see the Microsoft Windows Driver Development Kit (DDK)) to access the name, it is important that the name
		/// identify the program that is using this interface to erase to the media. The name is restricted to the same character set as
		/// required by the IOCTL_CDROM_EXCLUSIVE_ACCESS control code.
		/// </para>
		/// </remarks>
		public override string ClientName { get => op.ClientName; set => op.ClientName = value; }

		/// <summary>Determines the quality of the disc erasure.</summary>
		/// <value>
		/// <para>Set to <see langword="true"/> to fully erase the disc by overwriting the entire medium at least once.</para>
		/// <para>
		/// Set to <see langword="false"/> to overwrite the directory tracks, but not the entire disc. This option requires less time to
		/// perform than the full erase option.
		/// </para>
		/// <para>The default is <see langword="false"/>.</para>
		/// </value>
		public bool FullErase { get => op.FullErase; set => op.FullErase = value; }

		/// <summary>Retrieves the type of media in the disc device.</summary>
		/// <value>Type of media in the disc device. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPEenumeration type.</value>
		public override IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalMediaType => op.CurrentPhysicalMediaType;

		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="IDiscRecorder2"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		protected override IDiscRecorder2 Recorder { get => op.Recorder; set => op.Recorder = value; }

		/// <summary>Executes the operation on media in the specified device.</summary>
		/// <param name="device">The device on which to execute the operation.</param>
		public override void Execute()
		{
			if (Device is null)
				throw new InvalidOperationException("No optical device can be found.");

			if (string.IsNullOrEmpty(op.ClientName))
				op.ClientName = System.AppDomain.CurrentDomain.FriendlyName;

			using (var cp = new ComConnectionPoint(op, new DDiscFormat2EraseEventsSink(EraseUpdate)))
				op.EraseMedia();

			void EraseUpdate(IDiscFormat2Erase @object, int elapsedSeconds, int estimatedTotalSeconds)
			{
				if (estimatedTotalSeconds != 0)
					EraseProgress?.Invoke(this, new ProgressChangedEventArgs(elapsedSeconds * 100 / estimatedTotalSeconds, null));
			}
		}
	}

	public class OpticalStorageFileSystemImage : IOpticalStorageImage
	{
		private readonly IFileSystemImage fsi;

		public OpticalStorageFileSystemImage(string directoryPath, FsiFileSystems fileSystems, string volumeName, OpticalStorageBootOptions bootOptions)
		{
			// Make sure directory exists
			if (!Directory.Exists(directoryPath))
				throw new DirectoryNotFoundException();

			// Create a new file system image and retrieve the root directory
			fsi = new()
			{
				//FreeMediaBlocks = mediaImage.FreeSectorsOnMedia,
				FileSystemsToCreate = fileSystems
			};

			// Get the root dir
			IFsiDirectoryItem dir = fsi.Root;

			// create the BootImageOptions object
			if (bootOptions is not null)
				fsi.BootImageOptions = bootOptions.options;

			// ImportFileSystem - Import file data from disc
			//if (multiSession)
			//{
			//	// Set the multisession interface in the image
			//	fsi.MultisessionInterfaces = mediaImage.MultisessionInterfaces;
			//	fsi.ImportFileSystem();
			//}

			// Set the volume name
			if (volumeName is not null)
				fsi.VolumeName = volumeName;

			// Add a dir to the image
			using (var eventDisp = new ComConnectionPoint(fsi, new DFileSystemImageEventsSink(AddTreeUpdate)))
				dir.AddTree(directoryPath, false);

			// Report back what file systems are being used
			fileSystems = fsi.FileSystemsToCreate;

			void AddTreeUpdate(IFileSystemImage @object, string currentFile, long copiedSectors, long totalSectors) =>
				FileAdded?.Invoke(this, new FileSystemImageUpdateEventArgs(currentFile, copiedSectors, totalSectors));
		}

		internal OpticalStorageFileSystemImage(IDiscFormat2Data mediaImage)
		{
			fsi = new()
			{
				FreeMediaBlocks = mediaImage.FreeSectorsOnMedia,
				MultisessionInterfaces = mediaImage.MultisessionInterfaces,
			};
			fsi.ImportFileSystem();
		}

		public event EventHandler<FileSystemImageUpdateEventArgs> FileAdded;

		/// <summary>Retrieves a property value that specifies if the UDF Metadata will be redundant in the file system image.</summary>
		/// <value>
		/// Pointer to a value that specifies if the UDF metadata is redundant in the resultant file system image. A value of <c><see
		/// langword="true"/></c> indicates that UDF metadata will be redundant; otherwise, <c><see langword="false"/></c>.
		/// </value>
		/// <remarks>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </remarks>
		public bool CreateRedundantUdfMetadataFiles { get => fsi is IFileSystemImage3 i && i.CreateRedundantUdfMetadataFiles; set { if (fsi is IFileSystemImage3 i) i.CreateRedundantUdfMetadataFiles = value; } }

		/// <summary>Gets a string that identifies a disc and the sessions recorded on the disc.</summary>
		/// <returns>
		/// String that contains a signature that identifies the disc and the sessions on it. This string is not guaranteed to be unique
		/// between discs.
		/// </returns>
		/// <remarks>
		/// <para>
		/// When layering sessions on a disc, the signature acts as a key that the client can use to ensure the session order, and to
		/// distinguish sessions on disc from session images that will be laid on the disc.
		/// </para>
		/// <para>You must call IFileSystemImage::put_MultisessionInterfaces prior to calling <c>CalculateDiscIdentifier</c>.</para>
		/// </remarks>
		public string DiscIdentifier => fsi.CalculateDiscIdentifier();

		/// <summary>Gets or sets the types of file systems to create when generating the result stream.</summary>
		/// <value>
		/// One or more file system types to create when generating the result stream. For possible values, see the FsiFileSystems
		/// enumeration type.
		/// </value>
		/// <remarks>
		/// <para>
		/// To specify the file system types, call the IFileSystemImage::put_FileSystemsToCreate method. You could also call either
		/// IFilesystemImage::ChooseImageDefaults or IFilesystemImage::ChooseImageDefaultsForMediaType to have IMAPI choose the file system
		/// for you.
		/// </para>
		/// <para>To retrieve a list of supported file system types, call the IFileSystemImage.FileSystemsSupported method.</para>
		/// </remarks>
		public FsiFileSystems FileSystemsToCreate { get => fsi.FileSystemsToCreate; set => fsi.FileSystemsToCreate = value; }

		/// <summary>Gets or sets the maximum number of blocks available for the image.</summary>
		/// <value>Number of blocks to use in creating the file system image.</value>
		public int FreeMediaBlocks { get => fsi.FreeMediaBlocks; set => fsi.FreeMediaBlocks = value; }

		/// <summary>Gets or sets the ISO9660 compatibility level to use when creating the result image.</summary>
		/// <value>Identifies the interchange level of the ISO9660 file system.</value>
		public int ISO9660InterchangeLevel { get => fsi.ISO9660InterchangeLevel; set => fsi.ISO9660InterchangeLevel = value; }

		/// <summary>Indicates if the files being added to the file system image should be staged before the burn.</summary>
		/// <value>
		/// <c><see langword="true"/></c> if the files being added to the file system image are required to be stageded in one or more stage
		/// files before burning. Otherwise, <c><see langword="false"/></c> is returned if IMAPI is permitted to optimize the image creation
		/// process by not staging the files being added to the file system image.
		/// </value>
		/// <remarks>
		/// <para>
		/// "Staging" is a process in which an image is created on the hard-drive, containing all files to be burned, prior to the
		/// initiation of the burn operation.
		/// </para>
		/// <para>
		/// Setting this this property to <c><see langword="true"/></c> via IFileSystemImage::put_StageFiles will only affect files that are
		/// added after the property is set: those files will always be staged. Files that were not staged prior to a specified property
		/// value of <c><see langword="true"/></c>, will not be staged.
		/// </para>
		/// <para>By specifying <c><see langword="false"/></c>, the file system image creation process is optimized in two ways:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>Less time is required for image generation</term>
		/// </item>
		/// <item>
		/// <term>Less space is consumed on a local disk by IMAPI</term>
		/// </item>
		/// </list>
		/// <para>
		/// However, in order to avoid buffer underrun problems during burning, a certain minimum throughput is required for read operations
		/// on non-staged files. In the event that file accessibility or throughput may not meet the requirements of the burner, IMAPI
		/// enforces file staging regardless of the specified property value. For example, file staging is enforced for source files from
		/// removable storage devices, such as USB Flash Disk.
		/// </para>
		/// </remarks>
		public bool StageFiles { get => fsi.StageFiles; set => fsi.StageFiles = value; }

		/// <summary>Determines the compliance level for creating and developing the file-system image.</summary>
		/// <value>
		/// <para>Is <see langword="true"/> if the file system images are created in strict compliance with applicable standards.</para>
		/// <para>Is <see langword="false"/> if the compliance standards are relaxed to be compatible with IMAPI version 1.0.</para>
		/// </value>
		public bool StrictFileSystemCompliance { get => fsi.StrictFileSystemCompliance; set => fsi.StrictFileSystemCompliance = value; }

		/// <summary>Gets or sets the UDF revision level of the imported file system image.</summary>
		/// <value>UDF revision level of the imported file system image.</value>
		/// <remarks>
		/// The value is encoded according to the UDF specification, except the variable size is <see cref="int"/>. For example, revision
		/// level 1.02 is represented as 0x102.
		/// </remarks>
		public int UDFRevision { get => fsi.UDFRevision; set => fsi.UDFRevision = value; }

		/// <summary>Determines if the file and directory names use a restricted character.</summary>
		/// <value>
		/// Is <see langword="true"/> if the file and directory names to add to the file system image must consist of characters that map
		/// directly to CP_ANSI (code points 32 through 127). Otherwise, <see langword="false"/>.
		/// </value>
		public bool UseRestrictedCharacterSet { get => fsi.UseRestrictedCharacterSet; set => fsi.UseRestrictedCharacterSet = value; }

		/// <summary>Gets or sets the volume name for this file system image.</summary>
		/// <value>String that contains the volume name for this file system image.</value>
		public string VolumeName { get => fsi.VolumeName; set => fsi.VolumeName = value; }

		/// <summary>Gets or sets the temporary directory in which stash files are built.</summary>
		/// <value>String that contains the path to the temporary directory.</value>
		public string WorkingDirectory { get => fsi.WorkingDirectory; set => fsi.WorkingDirectory = value; }

		/// <summary>Retrieves the boot option array that will be utilized to generate the file system image.</summary>
		/// <value>
		/// A boot option array that contains a list of IBootOptions interfaces of boot images used to generate the file system image.
		/// </value>
		/// <remarks>If a boot image is not specified, a zero-sized array will be returned.</remarks>
		internal IBootOptions[] BootImageOptionsArray => fsi is IFileSystemImage2 i ? i.BootImageOptionsArray : Array.Empty<IBootOptions>();

		/// <summary>Gets the change point identifier.</summary>
		/// <value>Change point identifier. The identifier is a count of the changes to the file system image since its inception.</value>
		/// <remarks>
		/// <para>
		/// An application can store the value of this property prior to making a change to the file system, then at a later point pass the
		/// value to the IFileSystemImage::RollbackToChangePoint method to revert changes since that point in development.
		/// </para>
		/// <para>
		/// An application can call the IFileSystemImage::LockInChangePoint method to lock the state of a file system image at any point in
		/// its development. Once a lock is set, you cannot call RollbackToChangePoint to revert the file system image to its earlier state.
		/// </para>
		/// </remarks>
		public int ChangePoint => fsi.ChangePoint;

		/// <summary>Gets the number of directories in the file system image.</summary>
		/// <value>Number of directories in the file system image.</value>
		public int DirectoryCount => fsi.DirectoryCount;

		/// <summary>Gets the number of files in the file system image.</summary>
		/// <value>Number of files in the file system image.</value>
		public int FileCount => fsi.FileCount;

		/// <summary>Gets the list of file system types that a client can use to build a file system image.</summary>
		/// <value>
		/// One or more file system types that a client can use to build a file system image. For possible values, see the FsiFileSystems
		/// enumeration type.
		/// </value>
		public FsiFileSystems FileSystemsSupported => fsi.FileSystemsSupported;

		/// <summary>Gets the volume name provided from an imported file system.</summary>
		/// <value>String that contains the volume name provided from an imported file system. Is <c>NULL</c> until a file system is imported.</value>
		/// <remarks>
		/// The imported volume name is provided for user information and is not automatically carried forward to subsequent sessions.
		/// </remarks>
		public string ImportedVolumeName => fsi.ImportedVolumeName;

		/// <summary>Retrieves the supported ISO9660 compatibility levels.</summary>
		/// <value>
		/// List of supported ISO9660 compatibility levels. Each item in the list is a VARIANT that identifies one supported interchange
		/// level. The variant type is <c>VT_UI4</c>. The <c>ulVal</c> member of the variant contains the compatibility level.
		/// </value>
		public uint[] ISO9660InterchangeLevelsSupported => fsi.ISO9660InterchangeLevelsSupported;

		/// <summary>Gets the starting block address for the recording session.</summary>
		/// <value>Starting block address for the recording session.</value>
		/// <remarks>
		/// <para>The session starting block can be set in the following ways:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>Importing a file system automatically sets the session starting block.</term>
		/// </item>
		/// <item>
		/// <term>If the previous session is not imported, the client can call IFileSystemImage::put_SessionStartBlock to set this property.</term>
		/// </item>
		/// </list>
		/// </remarks>
		public int SessionStartBlock => fsi.SessionStartBlock;

		/// <summary>Gets a list of supported UDF revision levels.</summary>
		/// <value>List of supported UDF revision levels.</value>
		/// <remarks>
		/// The value is encoded according to the UDF specification, except the variable size is <see cref="int"/>. For example, revision
		/// level 1.02 is represented as 0x102.
		/// </remarks>
		public int[] UDFRevisionsSupported => fsi.UDFRevisionsSupported;

		/// <summary>Gets the number of blocks in use.</summary>
		/// <value>Estimated number of blocks used in the file-system image.</value>
		public int UsedBlocks => fsi.UsedBlocks;

		/// <summary>Retrieves the volume name for the ISO9660 system image.</summary>
		/// <value>String that contains the volume name for the ISO9660 system image.</value>
		public string VolumeNameISO9660 => fsi.VolumeNameISO9660;

		/// <summary>Retrieves the volume name for the Joliet system image.</summary>
		/// <value>String that contains the volume name for the Joliet system image.</value>
		public string VolumeNameJoliet => fsi.VolumeNameJoliet;

		/// <summary>Retrieves the volume name for the UDF system image.</summary>
		/// <value>String that contains the volume name for the UDF system image.</value>
		public string VolumeNameUDF => fsi.VolumeNameUDF;

		/// <summary>Gets or sets the boot image that you want to add to the file system image.</summary>
		/// <value>An IBootOptions interface of the boot image to add to the disc. Is <c>NULL</c> if a boot image has not been specified.</value>
		internal IBootOptions BootImageOptions { get => fsi.BootImageOptions; set => fsi.BootImageOptions = value; }

		/// <summary>Retrieves the list of multi-session interfaces for the optical media.</summary>
		/// <value>
		/// List of multi-session interfaces for the optical media. Each element of the list is a <c>VARIANT</c> of type <c>VT_Dispatch</c>.
		/// Query the <c>pdispVal</c> member of the variant for the IMultisession interface.
		/// </value>
		/// <remarks>
		/// Query the IMultisession interface for a derived <c>IMultisession</c> interface, for example, the IMultisessionSequential interface.
		/// </remarks>
		internal IMultisession[] MultisessionInterfaces { get => fsi.MultisessionInterfaces; set => fsi.MultisessionInterfaces = value; }

		/// <summary>Sets the default file system types and the image size based on the specified media type.</summary>
		/// <param name="value">
		/// Identifies the physical media type that will receive the burn image. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPE
		/// enumeration type.
		/// </param>
		public void ChooseImageDefaultsForMediaType(IMAPI_MEDIA_PHYSICAL_TYPE value) => fsi.ChooseImageDefaultsForMediaType(value);

		/// <summary>Checks for the existence of a given file or directory.</summary>
		/// <param name="fullPath">String that contains the fully qualified path of the directory or file to check.</param>
		/// <returns>
		/// Indicates if the item is a file, a directory, or does not exist. For possible values, see the FsiItemType enumeration type.
		/// </returns>
		public FsiItemType Exists(string fullPath) => fsi.Exists(fullPath);

		/// <summary>Retrieves the file system to import by default.</summary>
		/// <param name="fileSystems">One or more file system values. For possible values, see the FsiFileSystems enumeration type.</param>
		/// <returns>
		/// A single file system value that identifies the default file system. The value is one of the file systems specified in fileSystems
		/// </returns>
		/// <remarks>
		/// <para>Use this method to identify the default file system to use with IFileSystemImage::ImportFileSystem.</para>
		/// <para>To identify the supported file systems, call the IFileSystemImage.FileSystemsSupported method.</para>
		/// </remarks>
		public FsiFileSystems GetDefaultFileSystemForImport(FsiFileSystems fileSystems) => fsi.GetDefaultFileSystemForImport(fileSystems);

		public IStream GetImageStream()
		{
			// Create an image from the file system
			IFileSystemImageResult result = fsi.CreateResultImage();

			// Data stream sent to the burning device
			return result.ImageStream;
		}

		/// <summary>Imports the default file system on the current disc.</summary>
		/// <returns>Identifies the imported file system. For possible values, see the FsiFileSystems enumeration type.</returns>
		/// <remarks>
		/// <para>
		/// You must call IFileSystemImage::put_MultisessionInterfaces prior to calling <c>IFileSystemImage::ImportFileSystem</c>.
		/// Additionally, it is recommended that IDiscFormat2.MediaHeuristicallyBlank is called before
		/// <c>IFileSystemImage::put_MultisessionInterfaces</c> to verify that the media is not blank.
		/// </para>
		/// <para>
		/// If the disc contains more than one file system, only one file system is imported. This method chooses the file system to import
		/// in the following order: UDF, Joliet, ISO 9660. The import includes transferring directories and files to the in-memory file
		/// system structure.
		/// </para>
		/// <para>
		/// You may call this method at any time during the construction of the in-memory file system. If, during import, a file or
		/// directory already exists in the in-memory copy, the in-memory version will be retained; the imported file will be discarded.
		/// </para>
		/// <para>
		/// To determine which file system is the default file system for the disc, call the IFileSystemImage::GetDefaultFileSystemForImport method.
		/// </para>
		/// <para>
		/// This method only reads the file information. If the item is a file, the file data is copied when calling
		/// IFsiDirectoryItem::AddFile, IFsiDirectoryItem::AddTree, or IFsiDirectoryItem::Add method.
		/// </para>
		/// <para>
		/// This method returns <c>IMAPI_E_NO_SUPPORTED_FILE_SYSTEM</c> if a supported file system is not found in the last session.
		/// Additionally, this method returns <c>IMAPI_E_INCOMPATIBLE_PREVIOUS_SESSION</c> if the layout of the file system in the last
		/// session is incompatible with the layout used by IMAPI for the creation of requested file systems for the result image. For more
		/// details see the IFileSystemImage::put_FileSystemsToCreate method documention.
		/// </para>
		/// </remarks>
		public FsiFileSystems ImportFileSystem() => fsi.ImportFileSystem();

		/// <summary>Import a specific file system from disc.</summary>
		/// <param name="fileSystemToUse">
		/// Identifies the file system to import. For possible values, see the FsiFileSystems enumeration type.
		/// </param>
		/// <remarks>
		/// <para>
		/// You must call IFileSystemImage::put_MultisessionInterfaces prior to calling <c>IFileSystemImage::ImportSpecificFileSystem</c>.
		/// Additionally, it is recommended that IDiscFormat2.MediaHeuristicallyBlank is called before
		/// <c>IFileSystemImage::put_MultisessionInterfaces</c> to verify that the media is not blank.
		/// </para>
		/// <para>
		/// You may call this method at any time during the construction of the in-memory file system. If, during import, a file or
		/// directory already exists in the in-memory copy, the in-memory version will be retained; the imported file will be discarded.
		/// </para>
		/// <para>
		/// On re-writable media (DVD+/-RW, DVDRAM, BD-RE), import or burning a second session is not support if the first session has an
		/// ISO9660 file system, due to file system limitations.
		/// </para>
		/// <para>
		/// This method only reads the file information. If the item is a file, the file data is copied when calling
		/// IFsiDirectoryItem::AddFile, IFsiDirectoryItem::AddTree, or IFsiDirectoryItem::Add method.
		/// </para>
		/// <para>
		/// this method returns <c>IMAPI_E_INCOMPATIBLE_PREVIOUS_SESSION</c> if the layout of the file system in the last session is
		/// incompatible with the layout used by IMAPI for the creation of requested file systems for the result image. For more details see
		/// the IFileSystemImage::put_FileSystemsToCreate method documention. If the file system specified by fileSystemToUse has not been
		/// found, this method returns <c>IMAPI_E_FILE_SYSTEM_NOT_FOUND</c>.
		/// </para>
		/// </remarks>
		public void ImportSpecificFileSystem(FsiFileSystems fileSystemToUse) => fsi.ImportSpecificFileSystem(fileSystemToUse);

		/// <summary>Locks the file system information at the current change-point level.</summary>
		/// <remarks>
		/// <para>Once the change point is locked, rollback to earlier change points is not permitted.</para>
		/// <para>Locking the change point does not change the IFileSystemImage.ChangePoint property.</para>
		/// </remarks>
		public void LockInChangePoint() => fsi.LockInChangePoint();

		/// <summary>Determines if a specific file system on the current media is appendable through the IMAPI.</summary>
		/// <param name="fileSystemToProbe">The file system on the current media to probe.</param>
		/// <returns>A <c>bool</c> value specifying if the specified file system is appendable.</returns>
		/// <remarks>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </remarks>
		public bool ProbeSpecificFileSystem(FsiFileSystems fileSystemToProbe) => (fsi as IFileSystemImage3)?.ProbeSpecificFileSystem(fileSystemToProbe) ?? throw new NotSupportedException();

		/// <summary>Reverts the image back to the specified change point.</summary>
		/// <param name="changePoint">Change point that identifies the target state for rollback.</param>
		/// <remarks>
		/// <para>
		/// Typically, an application calls the IFileSystemImage.ChangePoint method and stores the change point value prior to making a
		/// change to the file system. If necessary, you can pass the change point value to this method to revert changes since that point
		/// in development.
		/// </para>
		/// <para>
		/// An application can call the IFileSystemImage::LockInChangePoint method to lock the state of a file system image at any point in
		/// its development. After a lock is set, you cannot call this method to revert the file system image to its earlier state.
		/// </para>
		/// </remarks>
		public void RollbackToChangePoint(int changePoint) => fsi.RollbackToChangePoint(changePoint);

		/// <summary>Sets the default file system types and the image size based on the current media.</summary>
		/// <param name="discRecorder">An IDiscRecorder2 the identifies the device that contains the current media.</param>
		internal void ChooseImageDefaults(IDiscRecorder2 discRecorder) => fsi.ChooseImageDefaults(discRecorder);

		/// <summary>Create a directory item with the specified name.</summary>
		/// <param name="name">String that contains the name of the directory item to create.</param>
		/// <returns>
		/// An IFsiDirectoryItem interface of the new directory item. When done, call the <c>IFsiDirectoryItem::Release</c> method to
		/// release the interface.
		/// </returns>
		/// <remarks>
		/// After setting properties on the IFsiDirectoryItem interface, call the IFsiDirectoryItem::Add method on the parent directory item
		/// to add it to the file system image.
		/// </remarks>
		internal IFsiDirectoryItem CreateDirectoryItem(string name) => fsi.CreateDirectoryItem(name);

		/// <summary>Create a file item with the specified name.</summary>
		/// <param name="name">String that contains the name of the file item to create.</param>
		/// <returns>
		/// An IFsiFileItem interface of the new file item. When done, call the <c>IFsiFileItem::Release</c> method to release the interface.
		/// </returns>
		/// <remarks>
		/// After setting properties on the IFsiFileItem interface, call the IFsiDirectoryItem::Add method on the parent directory item to
		/// add it to the file system image.
		/// </remarks>
		internal IFsiFileItem CreateFileItem(string name) => fsi.CreateFileItem(name);

		/// <summary>Create the result object that contains the file system and file data.</summary>
		/// <returns>
		/// <para>An IFileSystemImageResult interface of the image result.</para>
		/// <para>Client applications can stream the image to media or other long-term storage devices, such as disk drives.</para>
		/// </returns>
		/// <remarks>
		/// <para>
		/// Currently, <c>IFileSystemImage::CreateResultImage</c> will require disc media access as a result of a previous
		/// IFileSystemImage::IdentifyFileSystemsOnDisc method call. To resolve this issue, it is recommended that another IFileSystemImage
		/// object be created specifically for the <c>IFileSystemImage::IdentifyFileSystemsOnDisc</c> operation.
		/// </para>
		/// <para>
		/// The resulting stream can be saved as an ISO file if the file system is generated in a single session and has a start address of zero.
		/// </para>
		/// </remarks>
		internal IFileSystemImageResult CreateResultImage() => fsi.CreateResultImage();

		/// <summary>Retrieves a list of the different types of file systems on the optical media.</summary>
		/// <param name="discRecorder">
		/// An IDiscRecorder2 interface that identifies the recording device that contains the media. If this parameter is <c>NULL</c>, the
		/// discRecorder specified in IMultisession will be used.
		/// </param>
		/// <returns>One or more files systems on the disc. For possible values, see FsiFileSystems enumeration type.</returns>
		/// <remarks>
		/// Client applications can call IFileSystemImage::GetDefaultFileSystemForImport with the value returned by this method to determine
		/// the type of file system to import.
		/// </remarks>
		internal FsiFileSystems IdentifyFileSystemsOnDisc(IDiscRecorder2 discRecorder) => fsi.IdentifyFileSystemsOnDisc(discRecorder);

		/// <summary>Set maximum number of blocks available based on the capabilities of the recorder.</summary>
		/// <param name="discRecorder">
		/// An IDiscRecorder2 interface that identifies the recording device from which you want to set the maximum number of blocks available.
		/// </param>
		internal void SetMaxMediaBlocksFromDevice(IDiscRecorder2 discRecorder) => fsi.SetMaxMediaBlocksFromDevice(discRecorder);
	}

	//public class OpticalStorageMedia
	//{
	//	private IDiscFormat2 mediaImage;

	//	/// <summary>Gets the type of media in the disc device.</summary>
	//	/// <value>Type of media in the disc device. For possible values, see the <see cref="IMAPI_MEDIA_PHYSICAL_TYPE"/> enumeration type.</value>
	//	public IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalType => mediaImage.GetCurrentPhysicalMediaType();

	//	/// <summary>Attempts to determine if the media is blank using heuristics (mainly for DVD+RW and DVD-RAM media).</summary>
	//	/// <value>Is <see langword="true"/> if the disc is likely to be blank; otherwise; <see langword="false"/>.</value>
	//	/// <remarks>
	//	/// <para>
	//	/// This method checks, for example, for a mounted file system on the device, verifying the first and last 2MB of the disc are
	//	/// filled with zeros, and other media-specific checks. These checks can help to determine if the media may have files on it for
	//	/// media that cannot be erased physically to a blank status.
	//	/// </para>
	//	/// <para>For a positive check that a disc is blank, call the IsMediaPhysicallyBlank property.</para>
	//	/// </remarks>
	//	public bool IsMediaHeuristicallyBlank => mediaImage.MediaHeuristicallyBlank;

	//	/// <summary>Determines if the current media is reported as physically blank by the drive.</summary>
	//	/// <value>Is <see langword="true"/> if the disc is physically blank; otherwise, <see langword="false"/>.</value>
	//	public bool IsMediaPhysicallyBlank => mediaImage.MediaPhysicallyBlank;

	//	/*
	//	/// <summary>Gets the current state of the media in the device.</summary>
	//	/// <value>
	//	/// State of the media in the disc device. For possible values, see the IMAPI_FORMAT2_DATA_MEDIA_STATE enumeration type. Note that
	//	/// more than one state can be set.
	//	/// </value>
	//	public IMAPI_FORMAT2_DATA_MEDIA_STATE Status => mediaImage.CurrentMediaStatus;

	//	/// <summary>Determines if Buffer Underrun Free recording is enabled.</summary>
	//	/// <value>
	//	/// Set to <see langword="true"/> to disable Buffer Underrun Free recording; otherwise, <see langword="false"/>. The default is <see
	//	/// langword="false"/> (enabled).
	//	/// </value>
	//	/// <remarks>
	//	/// Buffer underrun can be an issue if the data stream does not enter the buffer fast enough to keep the device continuously
	//	/// writing. In turn, the stop and start action of writing can cause data on the disc to be unusable. Buffer Underrun Free (BUF)
	//	/// recording allows the laser to start and stop without damaging data already written to the disc. Disabling of BUF recording is
	//	/// possible only on CD-R/RW media.
	//	/// </remarks>
	//	public bool BufferUnderrunFreeDisabled => mediaImage.BufferUnderrunFreeDisabled;

	//	/// <summary>Gets the current write protect state of the media in the device.</summary>
	//	/// <value>
	//	/// <para>
	//	/// The current write protect state of the media in the device. For possible values, see the IMAPI_MEDIA_WRITE_PROTECT_STATE
	//	/// enumeration type.
	//	/// </para>
	//	/// <para>Note that more than one state can be set.</para>
	//	/// </value>
	//	public IMAPI_MEDIA_WRITE_PROTECT_STATE WriteProtectStatus => mediaImage.WriteProtectStatus;
	//	*/

	//	internal static OpticalStorageMedia Create(IDiscRecorder2 recorder, IDiscFormat2 formatter, string clientName = null, bool throwOnFail = false)
	//	{
	//		// Validate recorder for formatter
	//		if (!formatter.IsRecorderSupported(recorder))
	//			return !throwOnFail ? null : throw new ArgumentException($"Current recorder is not supported.", nameof(recorder));

	//		// Set Recorder property on IDiscFormat2
	//		formatter.SetRecorder(recorder);

	//		// Set ClientName property on IDiscFormat2, if null, pull from app domain
	//		formatter.SetClientName(string.IsNullOrEmpty(clientName) ? System.AppDomain.CurrentDomain.FriendlyName : clientName);

	//		// Validate formatter media
	//		if (!formatter.IsCurrentMediaSupported(recorder))
	//			return !throwOnFail ? null : throw new InvalidOperationException($"Current media is not supported.");

	//		return new OpticalStorageMedia { mediaImage = formatter };
	//	}
	//}

	public abstract class OpticalStorageMediaOperation<T> : IOpticalStorageMediaOperation where T : IDiscFormat2
	{
		internal readonly T op;

		/// <summary>Initializes a new instance of the <see cref="OpticalStorageMediaOperation{T}"/> class.</summary>
		/// <param name="clientName">The name is used when the write operation requests exclusive access to the device.</param>
		protected OpticalStorageMediaOperation(T opInst, string clientName)
		{
			op = opInst;
			ClientName = string.IsNullOrEmpty(clientName) ? System.AppDomain.CurrentDomain.FriendlyName : clientName;
		}

		/// <summary>Sets the friendly name of the client.</summary>
		/// <value>Name of the client application.</value>
		/// <remarks>
		/// <para>
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock.
		/// </para>
		/// <para>
		/// Because any application with read/write access to the device during the erase operation can use the IOCTL_CDROM_EXCLUSIVE_ACCESS
		/// (query) control code (see the Microsoft Windows Driver Development Kit (DDK)) to access the name, it is important that the name
		/// identify the program that is using this interface to erase to the media. The name is restricted to the same character set as
		/// required by the IOCTL_CDROM_EXCLUSIVE_ACCESS control code.
		/// </para>
		/// </remarks>
		public abstract string ClientName { get; set; }

		/// <summary>Retrieves the type of media in the disc device.</summary>
		/// <value>Type of media in the disc device. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPEenumeration type.</value>
		public abstract IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalMediaType { get; }

		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="OpticalStorageDevice"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		public OpticalStorageDevice Device
		{
			get
			{
				if (Recorder?.ActiveDiscRecorder is null)
					return Device = OpticalStorageManager.DefaultDevice;
				return new OpticalStorageDevice(Recorder.ActiveDiscRecorder);
			}
			set
			{
				if (value is not null)
				{
					if (!SupportsDevice(value))
						throw new NotSupportedException("Current device does not support this operation.");
					if (!SupportsCurrentMediaInDevice(value))
						throw new NotSupportedException("Current device's media does not support this operation.");
				}
				Recorder = value?.NativeInterface;
			}
		}

		/// <summary>Attempts to determine if the media is blank using heuristics (mainly for DVD+RW and DVD-RAM media).</summary>
		/// <value>Is <see langword="true"/> if the disc is likely to be blank; otherwise; <see langword="false"/>.</value>
		/// <remarks>
		/// <para>
		/// This method checks, for example, for a mounted file system on the device, verifying the first and last 2MB of the disc are
		/// filled with zeros, and other media-specific checks. These checks can help to determine if the media may have files on it for
		/// media that cannot be erased physically to a blank status.
		/// </para>
		/// <para>For a positive check that a disc is blank, call the IsMediaPhysicallyBlank property.</para>
		/// </remarks>
		public bool IsMediaHeuristicallyBlank => op.MediaHeuristicallyBlank;

		/// <summary>Determines if the current media is reported as physically blank by the drive.</summary>
		/// <value>Is <see langword="true"/> if the disc is physically blank; otherwise, <see langword="false"/>.</value>
		public bool IsMediaPhysicallyBlank => op.MediaPhysicallyBlank;

		/// <summary>Retrieves the media types that are supported by the current operation.</summary>
		/// <value>
		/// List of media types supported by the current operation. For a list of media types, see the IMAPI_MEDIA_PHYSICAL_TYPE enumeration type.
		/// </value>
		public IMAPI_MEDIA_PHYSICAL_TYPE[] SupportedMediaTypes => op.SupportedMediaTypes;

		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="IDiscRecorder2"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		protected abstract IDiscRecorder2 Recorder { get; set; }

		/// <summary>Executes the operation on media in the device identified by <see cref="Device"/>.</summary>
		public abstract void Execute();

		/// <summary>Determines if the current media in a supported operation supports the given format.</summary>
		/// <param name="device">A device to test.</param>
		/// <returns>
		/// <para>Is <see langword="true"/> if the media in the operation supports the given format; otherwise, <see langword="false"/>.</para>
		/// <para><c>Note</c><see langword="true"/> also implies that the result from IsDiscoperationSupported is <see langword="true"/>.</para>
		/// </returns>
		public bool SupportsCurrentMediaInDevice(OpticalStorageDevice device) => op.IsCurrentMediaSupported(device.NativeInterface);

		/// <summary>Determines if the operation supports the given format.</summary>
		/// <param name="device">A device to test.</param>
		/// <returns>Is <see langword="true"/> if the operation supports the given format; otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// When implemented by the IDiscFormat2RawCD interface, this method will return E_IMAPI_DF2RAW_MEDIA_IS_NOT_SUPPORTED in the event
		/// the operation does not support the given format. It is important to note that in this specific scenario the value does not
		/// indicate that an error has occurred, but rather the result of a successful operation.
		/// </remarks>
		public bool SupportsDevice(OpticalStorageDevice device) => op.IsRecorderSupported(device.NativeInterface);

		protected TRet EnsureDevice<TRet>(Func<TRet> func)
		{
			if (Device is null)
				throw new InvalidOperationException("No optical device can be found.");
			return func();
		}
	}

	public class OpticalStorageRawImage : IOpticalStorageImage
	{
		private readonly IRawCDImageCreator image;

		public OpticalStorageRawImage(IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE sectorType = IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE.IMAPI_FORMAT2_RAW_CD_SUBCODE_IS_RAW)
		{
			image = new();
			Tracks = new OpticalStorageRawImageTrackList(image);
		}

		/// <summary>
		/// Gets or sets the value that specifies if "Gapless Audio" recording is disabled. This property defaults to a value of <c><see
		/// langword="false"/></c>, which disables the use of "gapless" recording between consecutive audio tracks.
		/// </summary>
		/// <value>
		/// A <c>VARIANT_BOOL</c> value that specifies if "Gapless Audio" is disabled. Setting a value of <c><see langword="false"/></c>
		/// disables "Gapless Audio", while <c><see langword="true"/></c> enables it.
		/// </value>
		/// <remarks>
		/// <para>
		/// When disabled, by default, the audio tracks will have the standard 2-second (150 sector) silent gap between tracks. When
		/// enabled, the last 2 seconds of audio data from the previous audio track are encoded in the pregap area of the next audio track,
		/// enabling seamless transitions between tracks.
		/// </para>
		/// <para>
		/// It is recommended that this property value is only set before the process of adding tracks to the image has begun as any changes
		/// afterwards could result in adverse effects to other image properties.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public bool DisableGaplessAudio { get => image.DisableGaplessAudio; set => image.DisableGaplessAudio = value; }

		/// <summary>Gets or sets the Media Catalog Number (MCN) for the entire audio disc.</summary>
		/// <value>A <c>BSTR</c> value that represents the MCN to associate with the audio disc.</value>
		/// <remarks>
		/// <para>
		/// The returned MCN is formatted as a 13-digit decimal number and must also be provided in the same form. Additionally, the
		/// provided MCN value must have a valid checksum digit (least significant digit), or it will be rejected. For improved
		/// compatibility with scripting, leading zeros may be excluded. For example, "0123456789012" can be expressed as "123456789012".
		/// </para>
		/// <para>Please refer to the MMC specification for details regarding the MCN value.</para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public string MediaCatalogNumber { get => image.MediaCatalogNumber; set => image.MediaCatalogNumber = value; }

		/// <summary>Gets or sets the value that defines the type of image file that will be generated.</summary>
		/// <value>An IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE enumeration that defines the type of image file.</value>
		/// <remarks>
		/// <para>
		/// If the value set via IRawCDImageCreator::AddSubcodeRWGenerator is not <c>NULL</c>, then the <c>PQ_ONLY</c> type defined by
		/// IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE is not a valid choice, as subcode would not be generated by the resulting image.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE ResultingImageType { get => image.ResultingImageType; set => image.ResultingImageType = value; }

		/// <summary>Gets or sets the starting track number.</summary>
		/// <value>A <c>LONG</c> value that represents the starting track number.</value>
		/// <remarks>
		/// <para>
		/// This property value can only be set before the addition of tracks. If this property is set to a value other than 1, all tracks
		/// added to the image must be audio tracks.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public int StartingTrackNumber { get => image.StartingTrackNumber; set => image.StartingTrackNumber = value; }

		/// <summary>
		/// Gets or sets the StartOfLeadoutLimit property value. This value specifies if the resulting image is required to fit on a piece
		/// of media with a <c>StartOfLeadout</c> greater than or equal to the LBA specified.
		/// </summary>
		/// <value>Pointer to a <c>LONG</c> value that represents the current StartOfLeadoutLimit.</value>
		/// <remarks>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </remarks>
		public int StartOfLeadoutLimit { get => image.StartOfLeadoutLimit; set => image.StartOfLeadoutLimit = value; }

		public OpticalStorageRawImageTrackList Tracks { get; }

		/// <summary>Gets the SCSI-form table of contents for the resulting disc.</summary>
		/// <value>
		/// The SCSI-form table of contents for the resulting disc. Accuracy of this value depends on
		/// <c>IRawCDImageCreator::get_ExpectedTableOfContents</c> being called after all image properties have been set.
		/// </value>
		/// <remarks>
		/// <para>This method can only be called after at least one track has been added to the image.</para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public object[] ExpectedTableOfContents => image.ExpectedTableOfContents;

		/// <summary>Retrieves the number of total used sectors on the current media, including any overhead between existing tracks.</summary>
		/// <value>Pointer to a <c>LONG</c> value that indicates the number of total used sectors on the media.</value>
		/// <remarks>
		/// <para>
		/// This value represents the LBA of the last sector with data that is considered part of a track, and does not include the overhead
		/// of the leadin, leadout, or the two-seconds between MSF 00:00:00 and MSF 00:02:00.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public int LastUsedUserSectorInImage => image.LastUsedUserSectorInImage;

		/// <summary>
		/// Retrieves the value that defines the LBA for the start of the Leadout. This method can be utilized to determine if the image can
		/// be written to a piece of media by comparing it against the <c>LastPossibleStartOfLeadout</c> for the media.
		/// </summary>
		/// <value>Pointer to a <c>LONG</c> value that represents the LBA for the start of the Leadout.</value>
		/// <remarks>
		/// <para>Use of this method requires that at least 1 track has been added to the image.</para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public int StartOfLeadout => image.StartOfLeadout;

		/// <summary>
		/// Accepts the provided <c>IStream</c> object and saves the associated pointer to be used as data for the pre-gap for track 1.
		/// </summary>
		/// <param name="data">Pointer to the provided <c>IStream</c> object.</param>
		/// <remarks>
		/// <para>
		/// This method can only be called prior to adding any tracks to the image. The data stream must be at least 2 seconds (or 150
		/// sectors) long.
		/// </para>
		/// <para>
		/// The data stream should not result final sector exceeding LBA 397,799 (MSF 88:25:74), as the minimal-sized track plus leadout
		/// would then exceed the MSF 89:59:74 maximum. Additionally, it is recommended that the IMAPI_CD_SECTOR_TYPE value for the first
		/// track is implicitly defined as "Audio". The resulting audio can then only be heard by playing the first track and "rewinding"
		/// back to the start of the audio disc.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public void AddSpecialPregap(IStream data) => image.AddSpecialPregap(data);

		/// <summary>
		/// Allows the addition of custom R-W subcode, provided by the <c>IStream</c>. The provided object must have a size equal to the
		/// number of sectors in the raw disc image * 96 bytes when the final image is created.
		/// </summary>
		/// <param name="subcode">
		/// The subcode data (with 96 bytes per sector), where the 2 most significant bits must always be zero (as they are the P/Q bits).
		/// </param>
		/// <remarks>
		/// <para>
		/// May be added anytime prior to calling IRawCDImageCreator::CreateResultImage. If IRawCDImageCreator::put_ResultingImageType is
		/// set to return PQ only, then this call will fail as no RW subcode will be used in the resulting image.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public void AddSubcodeRWGenerator(IStream subcode) => image.AddSubcodeRWGenerator(subcode);

		/// <summary>Creates the final <c>IStream</c> object based on the current settings.</summary>
		/// <returns>Pointer to the finalized IStream object.</returns>
		/// <remarks>
		/// <para>
		/// <c>IRawCDImageCreator::CreateResultImage</c> can only be called once, and will result in the object becoming read-only. All
		/// properties associated with this object can be read but not modified. The resulting <c>IStream</c> object will be a disc image
		/// which starts at MSF 95:00:00, to allow writing of a single image to media with multiple starting addresses.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public IStream GetImageStream() => image.CreateResultImage();
	}

	public class OpticalStorageRawImageTrack
	{
		private readonly IRawCDImageTrackInfo track;

		internal OpticalStorageRawImageTrack(IRawCDImageTrackInfo rawCDImageTrackInfo) => track = rawCDImageTrackInfo;

		/// <summary>
		/// Sets the value that specifies if an audio track has an additional pre-emphasis added to the audio data prior to being written to CD.
		/// </summary>
		/// <value>Value that specifies if an audio track has an additional pre-emphasis added to the audio data.</value>
		/// <remarks>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </remarks>
		public bool AudioHasPreemphasis { get => track.AudioHasPreemphasis; set => track.AudioHasPreemphasis = value; }

		/// <summary>
		/// Sets the digital audio copy "Allowed" bit to one of three values on the resulting media. Please see the
		/// IMAPI_CD_TRACK_DIGITAL_COPY_SETTING enumeration for additional information on each possible value.
		/// </summary>
		/// <value>The digital audio copy setting value to assign.</value>
		/// <remarks>
		/// <para>This property may only be set for tracks containing audio data.</para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public IMAPI_CD_TRACK_DIGITAL_COPY_SETTING DigitalAudioCopySetting { get => track.DigitalAudioCopySetting; set => track.DigitalAudioCopySetting = value; }

		/// <summary>
		/// Gets or sets the International Standard Recording Code (ISRC) currently associated with the track. This property value defaults
		/// to <c>NULL</c> (or a zero-length string) and may only be set for tracks containing audio data.
		/// </summary>
		/// <value>The ISRC to associate with the track.</value>
		/// <remarks>
		/// <para>
		/// The format of the ISRC is provided to the caller formatted per ISRC standards (DIN-31-621) recommendations, such as
		/// "US-K7Y-98-12345". When set, the provided string may optionally exclude all the '-' characters.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public bool ISRC { get => track.ISRC; set => track.ISRC = value; }

		/// <summary>Retrieves the number of user sectors in this track.</summary>
		/// <value>The number of user sectors in this track.</value>
		/// <remarks>
		/// <para>
		/// The end of the track is typically the <c>StartingLBA</c> plus the <c>SectorCount</c>. The start of the next track includes both
		/// of these properties plus any required pregap or postgap.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public int SectorCount => track.SectorCount;

		/// <summary>
		/// Retrieves the type of data provided for the sectors in this track. For more detail on the possible sector types, see IMAPI_CD_SECTOR_TYPE.
		/// </summary>
		/// <value>A pointer to a IMAPI_CD_SECTOR_TYPE enumeration that specifies the type of data provided for the sectors on the track.</value>
		/// <remarks>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </remarks>
		public IMAPI_CD_SECTOR_TYPE SectorType => track.SectorType;

		/// <summary>Retrieves the LBA of the first user sectors in this track.</summary>
		/// <value>The LBA of the first user sectors in this track.</value>
		/// <remarks>
		/// <para>Most tracks also include a pregap and postgap, which are not included in this value.</para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public int StartingLba => track.StartingLba;

		/// <summary>Retrieves the one-based index of the tracks on the disc.</summary>
		/// <value>The one-based index associated with this track.</value>
		/// <remarks>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </remarks>
		public int[] TrackIndexes => track.TrackIndexes;

		/// <summary>Retrieves the track number for this track.</summary>
		/// <value>The track number for this track.</value>
		/// <remarks>
		/// <para>
		/// While this value is often identical to the <c>TrackIndex</c> property, it is possible for pure audio discs to start with a track
		/// other than track number 1. This means that the more general formula is that this value is ( TrackIndex + FirstTrackNumber - 1).
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public int TrackNumber => track.TrackNumber;

		/// <summary>Add the specified LBA (relative to the start of the track) as an index.</summary>
		/// <param name="lbaOffset">The LBA to add. This must be a value in the range of 0 and 0x7FFFFFFF.</param>
		public void AddTrackIndex(int lbaOffset) => track.AddTrackIndex(lbaOffset);

		/// <summary>Removes the specified LBA (relative to the start of the track) as an index.</summary>
		/// <param name="lbaOffset">The LBA to remove. This must be a value in the range of 0 and 0x7FFFFFFF.</param>
		public void ClearTrackIndex(int lbaOffset) => track.ClearTrackIndex(lbaOffset);
	}

	public class OpticalStorageRawImageTrackList : IReadOnlyCollection<OpticalStorageRawImageTrack>
	{
		private readonly IRawCDImageCreator image;

		internal OpticalStorageRawImageTrackList(IRawCDImageCreator img) => image = img;

		/// <summary>Retrieves the number of existing audio tracks on the media.</summary>
		/// <value>A value that indicates the number of audio tracks that currently exist on the media.</value>
		public int Count => image.NumberOfExistingTracks;

		/// <summary>Accepts the provided <c>IStream</c> object and saves the interface pointer as the next track in the image.</summary>
		/// <param name="dataType">
		/// A value, defined by IMAPI_CD_SECTOR_TYPE, that indicates the type of data. <c>IMAPI_CD_SECTOR_AUDIO</c> is the only value
		/// supported by the <c>IRawCDImageCreator::AddTrack</c> method.
		/// </param>
		/// <param name="data">Pointer to the provided <c>IStream</c> object.</param>
		/// <returns>A <see cref="OpticalStorageRawImageTrack"/> instance associated with the new track.</returns>
		/// <remarks>
		/// <para>
		/// Any additional tracks must be compatible with all existing tracks. See the IMAPI_CD_SECTOR_TYPE enumeration for information on limitations.
		/// </para>
		/// <para>
		/// The data stream must be at least 4 seconds (300 sectors) long. Data stream may not cause final sector to exceed LBA 398,099 (MSF
		/// 88:29:74), as leadout would then exceed the MSF 89:59:74 maximum.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public OpticalStorageRawImageTrack Add(IStream data)
		{
			// Add the track to the stream
			var idx = image.AddTrack(IMAPI_CD_SECTOR_TYPE.IMAPI_CD_SECTOR_AUDIO, data);
			return new OpticalStorageRawImageTrack(image.get_TrackInfo(idx));
		}

		/// <summary>Accepts the provided <c>IStream</c> object and saves the interface pointer as the next track in the image.</summary>
		/// <param name="dataType">
		/// A value, defined by IMAPI_CD_SECTOR_TYPE, that indicates the type of data. <c>IMAPI_CD_SECTOR_AUDIO</c> is the only value
		/// supported by the <c>IRawCDImageCreator::AddTrack</c> method.
		/// </param>
		/// <param name="filePath">Full path of a file to add as a track.</param>
		/// <returns>A <see cref="OpticalStorageRawImageTrack"/> instance associated with the new track.</returns>
		/// <remarks>
		/// <para>
		/// Any additional tracks must be compatible with all existing tracks. See the IMAPI_CD_SECTOR_TYPE enumeration for information on limitations.
		/// </para>
		/// <para>
		/// The data stream must be at least 4 seconds (300 sectors) long. Data stream may not cause final sector to exceed LBA 398,099 (MSF
		/// 88:29:74), as leadout would then exceed the MSF 89:59:74 maximum.
		/// </para>
		/// <para>
		/// This method is supported in Windows Server 2003 with Service Pack 1 (SP1), Windows XP with Service Pack 2 (SP2), and Windows
		/// Vista via the Windows Feature Pack for Storage. All features provided by this update package are supported natively in Windows 7
		/// and Windows Server 2008 R2.
		/// </para>
		/// </remarks>
		public OpticalStorageRawImageTrack Add(string filePath)
		{
			using var f = new AudioFile(filePath);
			return Add(f.NativeInterface);
		}

		/// <summary>Gets the enumerator.</summary>
		/// <returns></returns>
		public IEnumerator<OpticalStorageRawImageTrack> GetEnumerator()
		{
			var start = image.StartingTrackNumber;
			for (var i = 0; i < Count; i++)
				yield return new OpticalStorageRawImageTrack(image.get_TrackInfo(i + start));
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>Represents an operation to write audio to blank CD-R or CD-RW media on an optical storage device.</summary>
	public class OpticalStorageWriteAudioOperation : OpticalStorageMediaOperation<IDiscFormat2TrackAtOnce>
	{
		/// <summary>Initializes a new instance of the <see cref="OpticalStorageWriteAudioOperation"/> class.</summary>
		/// <param name="clientName">
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock. If <see
		/// langword="null"/>, the name is pulled from the app domain.
		/// </param>
		public OpticalStorageWriteAudioOperation(string clientName = null) : base(new(), clientName)
		{
		}

		/// <summary>Occurs during <see cref="WriteAudioTracksToMedia"/> to indicate the progress.</summary>
		public event EventHandler<OpticalStorageWriteAudioTrackEventArgs> WriteAudioTrackProgress;

		/// <summary>Gets the list of audio track paths.</summary>
		/// <value>The audio track paths.</value>
		public List<string> AudioTrackPaths { get; } = new List<string>();

		/// <summary>Determines if Buffer Underrun Free Recording is enabled.</summary>
		/// <value>
		/// Set to <see langword="true"/> to disable Buffer Underrun Free Recording; otherwise, <see langword="false"/>. The default is <see
		/// langword="false"/> (enabled).
		/// </value>
		/// <remarks>
		/// Buffer underrun can be an issue if the data stream does not enter the buffer fast enough to keep the device continuously
		/// writing. In turn, the stop and start action of writing can cause data on the disc to be unusable. Buffer Underrun Free (BUF)
		/// recording allows the laser to start and stop without damaging data already written to the disc. Disabling of BUF recording is
		/// possible only on CD-R/RW media.
		/// </remarks>
		[DefaultValue(false)]
		public bool BufferUnderrunFreeDisabled { get => op.BufferUnderrunFreeDisabled; set => op.BufferUnderrunFreeDisabled = value; }

		/// <summary>Sets the friendly name of the client.</summary>
		/// <value>Name of the client application.</value>
		/// <remarks>
		/// <para>
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock.
		/// </para>
		/// <para>
		/// Because any application with read/write access to the device during the erase operation can use the IOCTL_CDROM_EXCLUSIVE_ACCESS
		/// (query) control code (see the Microsoft Windows Driver Development Kit (DDK)) to access the name, it is important that the name
		/// identify the program that is using this interface to erase to the media. The name is restricted to the same character set as
		/// required by the IOCTL_CDROM_EXCLUSIVE_ACCESS control code.
		/// </para>
		/// </remarks>
		public override string ClientName { get => op.ClientName; set => op.ClientName = value; }

		/// <summary>Retrieves the requested rotational-speed control type.</summary>
		/// <value>
		/// Requested rotational-speed control type. Is <see langword="true"/> for constant angular velocity (CAV) rotational-speed control
		/// type. Otherwise, is <see langword="false"/> for any other rotational-speed control type that the recorder supports.
		/// </value>
		/// <remarks>This is the value specified in the most recent call to the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</remarks>
		[DefaultValue(false)]
		public bool RequestedRotationTypeIsPureCAV
		{
			get => op.RequestedRotationTypeIsPureCAV;
			set => op.SetWriteSpeed(RequestedWriteSpeed, value);
		}

		/// <summary>Retrieves the requested write speed.</summary>
		/// <value>
		/// <para>Requested write speed measured in disc sectors per second.</para>
		/// <para>A value of 0xFFFFFFFF (-1) requests that the write occurs using the fastest supported speed for the media.</para>
		/// </value>
		/// <remarks>This is the value specified in the most recent call to the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</remarks>
		[DefaultValue(-1)]
		public int RequestedWriteSpeed
		{
			get => op.RequestedWriteSpeed;
			set => op.SetWriteSpeed(value, RequestedRotationTypeIsPureCAV);
		}

		/// <summary>Retrieves the type of media in the disc device.</summary>
		/// <value>Type of media in the disc device. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPEenumeration type.</value>
		public override IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalMediaType => op.CurrentPhysicalMediaType;

		/// <summary>Retrieves the current rotational-speed control used by the recorder.</summary>
		/// <value>
		/// Is <see langword="true"/> if constant angular velocity (CAV) rotational-speed control is in use. Otherwise, <see
		/// langword="false"/> to indicate that another rotational-speed control that the recorder supports is in use.
		/// </value>
		/// <remarks>
		/// <para>
		/// To retrieve the requested rotational-speed control, call the IDiscFormat2TrackAtOnce::get_RequestedRotationTypeIsPureCAV method.
		/// </para>
		/// <para>Rotational-speed control types include the following:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>CLV (Constant Linear Velocity). The disc is written at a constant speed. Standard rotational control.</term>
		/// </item>
		/// <item>
		/// <term>CAV (Constant Angular Velocity). The disc is written at a constantly increasing speed.</term>
		/// </item>
		/// <item>
		/// <term>
		/// ZCAV (Zone Constant Angular Velocity). The disc is divided into zones. After each zone, the write speed increases. This is an
		/// impure form of CAV.
		/// </term>
		/// </item>
		/// <item>
		/// <term>
		/// PCAV (Partial Constant Angular Velocity). The disc speed increases up to a specified velocity. Once reached, the disc spins at
		/// the specified velocity for the duration of the disc writing.
		/// </term>
		/// </item>
		/// </list>
		/// </remarks>
		public bool CurrentRotationTypeIsPureCAV => op.CurrentRotationTypeIsPureCAV;

		/// <summary>Retrieves the drive's current write speed.</summary>
		/// <value>The write speed of the current media, measured in sectors per second.</value>
		/// <remarks>
		/// <para>To retrieve the requested write speed, call the IDiscFormat2TrackAtOnce::get_RequestedWriteSpeed method.</para>
		/// <para>
		/// To retrieve a list of the write speeds that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeeds method.
		/// </para>
		/// <para>
		/// Note that the write speed is based on the media write speeds. The value of this property can change when a media change occurs.
		/// </para>
		/// </remarks>
		public int CurrentWriteSpeed => op.CurrentWriteSpeed;

		/// <summary>Determines if the media is left open for writing after writing the audio track.</summary>
		/// <value>
		/// Set to <see langword="true"/> to leave the media open for writing after writing the audio track; otherwise, <see
		/// langword="false"/>. The default is <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// <para>
		/// You can set this property before calling the IDiscFormat2TrackAtOnce::PrepareMedia method or after calling the
		/// IDiscFormat2TrackAtOnce::ReleaseMedia method; you cannot set it during a track-writing session.
		/// </para>
		/// <para>This property is useful to create a multi-session CD with audio in the first session and data in the second session.</para>
		/// </remarks>
		public bool DoNotFinalizeMedia => op.DoNotFinalizeMedia;

		/// <summary>Retrieves the table of content for the audio tracks that were laid on the media within the track-writing session.</summary>
		/// <value>
		/// Table of contents for the audio tracks that were laid on the media within the track-writing session. Each element of the list is
		/// a <c>VARIANT</c> of type <c>VT_BYREF|VT_UI1</c>. The <c>pbVal</c> member of the variant contains a binary blob. For details of
		/// the blob, see the READ TOC command at ftp://ftp.t10.org/t10/drafts/mmc5/mmc5r03.pdf.
		/// </value>
		/// <remarks>The property is not accessible outside a track-writing session. Nor is the property accessible if the disc is blank.</remarks>
		public IntPtr[] ExpectedTableOfContents => op.ExpectedTableOfContents;

		/// <summary>Retrieves the number of sectors available for adding a new track to the media.</summary>
		/// <value>Number of available sectors on the media that can be used for writing audio.</value>
		/// <remarks>
		/// If called during an AddAudioTrack operation, the available sectors do not reflect the sectors used in writing the current audio
		/// track. Instead, the reported value is the number of available sectors immediately preceding the call to AddAudioTrack.
		/// </remarks>
		public int FreeSectorsOnMedia => op.FreeSectorsOnMedia;

		/// <summary>Retrieves the number of existing audio tracks on the media.</summary>
		/// <value>Number of completed tracks written to disc, not including the track currently being added.</value>
		/// <remarks>
		/// <para>The value is zero if:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>The media is blank</term>
		/// </item>
		/// <item>
		/// <term>You call this method outside a writing session</term>
		/// </item>
		/// </list>
		/// </remarks>
		public int NumberOfExistingTracks => op.NumberOfExistingTracks;

		/// <summary>Retrieves a list of the detailed write configurations supported by the disc recorder and current media.</summary>
		/// <value>
		/// List of the detailed write configurations supported by the disc recorder and current media. Each element of the list is a
		/// <c>VARIANT</c> of type <c>VT_Dispatch</c>. Query the <c>pdispVal</c> member of the variant for the IWriteSpeedDescriptor
		/// interface, which contains the media type, write speed, rotational-speed control type.
		/// </value>
		/// <remarks>
		/// To retrieve a list of the write speeds that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeeds method.
		/// </remarks>
		public WriteSpeedDescriptor[] SupportedWriteSpeedDescriptors => Array.ConvertAll(op.SupportedWriteSpeedDescriptors, i => new WriteSpeedDescriptor(i));

		/// <summary>Retrieves a list of the write speeds supported by the disc recorder and current media.</summary>
		/// <value>
		/// List of the write speeds supported by the disc recorder and current media. Each element of the list is a <c>VARIANT</c> of type
		/// <c>VT_UI4</c>. The <c>ulVal</c> member of the variant contains the number of sectors written per second.
		/// </value>
		/// <remarks>
		/// <para>You can use a speed from the list to set the write speed when calling the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</para>
		/// <para>
		/// To retrieve a list of the write configurations that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeedDescriptors method.
		/// </para>
		/// </remarks>
		public uint[] SupportedWriteSpeeds => op.SupportedWriteSpeeds;

		/// <summary>Retrieves the total sectors available on the media if writing one continuous audio track.</summary>
		/// <value>Number of all sectors on the media that can be used for audio if one continuous audio track was recorded.</value>
		/// <remarks>This property can be retrieved at any time; however, during writing, the value is cached.</remarks>
		public int TotalSectorsOnMedia => op.TotalSectorsOnMedia;

		/// <summary>Retrieves the total number of used sectors on the media.</summary>
		/// <value>Number of used sectors on the media, including audio tracks and overhead that exists between tracks.</value>
		/// <remarks>If you call this method from your event handler, the number reflects the sectors used before the write began.</remarks>
		public int UsedSectorsOnMedia => op.UsedSectorsOnMedia;

		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="IDiscRecorder2"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		protected override IDiscRecorder2 Recorder { get => op.Recorder; set => op.Recorder = value; }

		/// <summary>Executes the operation on media in the specified device.</summary>
		/// <param name="device">The device on which to execute the operation.</param>
		public override void Execute()
		{
			if (Device is null)
				throw new InvalidOperationException("No optical device can be found.");

			if (string.IsNullOrEmpty(op.ClientName))
				op.ClientName = System.AppDomain.CurrentDomain.FriendlyName;

			try
			{
				// Prepare the media
				op.PrepareMedia();

				// hookup the event
				using var cp = new ComConnectionPoint(op, new DDiscFormat2TrackAtOnceEventsSink(Audio_Update));

				// Add each track
				foreach (var fileName in AudioTrackPaths)
				{
					// get a stream to write to the disc
					using var audioStream = new AudioFile(fileName);

					// Write the stream
					op.AddAudioTrack(audioStream.NativeInterface);
				}
			}
			finally
			{
				// Release the media now that we are done
				op.ReleaseMedia();
			}

			void Audio_Update(IDiscFormat2TrackAtOnce @object, IDiscFormat2TrackAtOnceEventArgs progress) =>
				WriteAudioTrackProgress?.Invoke(this, new OpticalStorageWriteAudioTrackEventArgs(progress));
		}
	}

	public class OpticalStorageWriteAudioTrackEventArgs : EventArgs
	{
		private readonly IDiscFormat2TrackAtOnceEventArgs progress;

		public OpticalStorageWriteAudioTrackEventArgs(IDiscFormat2TrackAtOnceEventArgs progress) => this.progress = progress;

		/// <summary>Retrieves the current write action being performed.</summary>
		/// <value>
		/// Current write action being performed. For a list of possible actions, see the IMAPI_FORMAT2_TAO_WRITE_ACTION enumeration type.
		/// </value>
		public IMAPI_FORMAT2_TAO_WRITE_ACTION CurrentAction => progress.CurrentAction;

		/// <summary>Retrieves the current track number being written to the media.</summary>
		/// <value>Track number, ranging from 1 through 99.</value>
		public int CurrentTrackNumber => progress.CurrentTrackNumber;

		/// <summary>Retrieves the total elapsed time of the write operation.</summary>
		/// <value>Elapsed time, in seconds, of the write operation.</value>
		public TimeSpan ElapsedTime => TimeSpan.FromSeconds(progress.ElapsedTime);

		/// <summary>Retrieves the number of unused bytes in the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the unused portion of the internal data buffer that is used for writing to disc.</value>
		/// <remarks>This method returns the same value as if you subtracted IWriteEngine2EventArgs.UsedSystemBuffer from IWriteEngine2EventArgs.TotalSystemBuffer.</remarks>
		public int FreeSystemBuffer => progress.FreeSystemBuffer;

		/// <summary>Retrieves the address of the sector most recently read from the burn image.</summary>
		/// <value>Logical block address of the sector most recently read from the input data stream.</value>
		public int LastReadLba => progress.LastReadLba;

		/// <summary>Retrieves the address of the sector most recently written to the device.</summary>
		/// <value>Logical block address of the sector most recently written to the device.</value>
		public int LastWrittenLba => progress.LastWrittenLba;

		/// <summary>Retrieves the estimated remaining time of the write operation.</summary>
		/// <value>Estimated time, in seconds, needed for the remainder of the write operation.</value>
		/// <remarks>
		/// The estimate for a single write operation can vary as the operation progresses. The drive provides updated information that can
		/// affect the projected duration of the write operation.
		/// </remarks>
		public TimeSpan RemainingTime => TimeSpan.FromSeconds(progress.RemainingTime);

		/// <summary>Retrieves the number of sectors to write to the device in the current write operation.</summary>
		/// <value>The number of sectors to write in the current write operation.</value>
		/// <remarks>This is the same value passed to the IWriteEngine2::WriteSection method.</remarks>
		public int SectorCount => progress.SectorCount;

		/// <summary>Retrieves the starting logical block address (LBA) of the current write operation.</summary>
		/// <value>Starting logical block address of the write operation. Negative values for LBAs are supported.</value>
		/// <remarks>This is the same value passed to the IWriteEngine2::WriteSection method.</remarks>
		public int StartLba => progress.StartLba;

		/// <summary>Retrieves the size of the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the internal data buffer that is used for writing to disc.</value>
		public int TotalSystemBuffer => progress.TotalSystemBuffer;

		/// <summary>Retrieves the number of used bytes in the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the used portion of the internal data buffer that is used for writing to disc.</value>
		/// <remarks>This value increases as data is read into the buffer and decreases as data is written to disc.</remarks>
		public int UsedSystemBuffer => progress.UsedSystemBuffer;
	}

	/// <summary>Use retrieve information about the current write operation.</summary>
	public class OpticalStorageWriteEventArgs : EventArgs
	{
		private readonly IDiscFormat2DataEventArgs progress;

		public OpticalStorageWriteEventArgs(IDiscFormat2DataEventArgs progress) => this.progress = progress;

		/// <summary>Retrieves the current write action being performed.</summary>
		/// <value>
		/// Current write action being performed. For a list of possible actions, see the IMAPI_FORMAT2_DATA_WRITE_ACTION enumeration type.
		/// </value>
		public IMAPI_FORMAT2_DATA_WRITE_ACTION CurrentAction => progress.CurrentAction;

		/// <summary>Retrieves the total elapsed time of the write operation.</summary>
		/// <value>Elapsed time, in seconds, of the write operation.</value>
		public TimeSpan ElapsedTime => TimeSpan.FromSeconds(progress.ElapsedTime);

		/// <summary>Retrieves the number of unused bytes in the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the unused portion of the internal data buffer that is used for writing to disc.</value>
		/// <remarks>This method returns the same value as if you subtracted IWriteEngine2EventArgs.UsedSystemBuffer from IWriteEngine2EventArgs.TotalSystemBuffer.</remarks>
		public int FreeSystemBuffer => progress.FreeSystemBuffer;

		/// <summary>Retrieves the address of the sector most recently read from the burn image.</summary>
		/// <value>Logical block address of the sector most recently read from the input data stream.</value>
		public int LastReadLba => progress.LastReadLba;

		/// <summary>Retrieves the address of the sector most recently written to the device.</summary>
		/// <value>Logical block address of the sector most recently written to the device.</value>
		public int LastWrittenLba => progress.LastWrittenLba;

		/// <summary>Retrieves the estimated remaining time of the write operation.</summary>
		/// <value>Estimated time, in seconds, needed for the remainder of the write operation.</value>
		/// <remarks>
		/// The estimate for a single write operation can vary as the operation progresses. The drive provides updated information that can
		/// affect the projected duration of the write operation.
		/// </remarks>
		public TimeSpan RemainingTime => TimeSpan.FromSeconds(progress.RemainingTime);

		/// <summary>Retrieves the number of sectors to write to the device in the current write operation.</summary>
		/// <value>The number of sectors to write in the current write operation.</value>
		/// <remarks>This is the same value passed to the IWriteEngine2::WriteSection method.</remarks>
		public int SectorCount => progress.SectorCount;

		/// <summary>Retrieves the starting logical block address (LBA) of the current write operation.</summary>
		/// <value>Starting logical block address of the write operation. Negative values for LBAs are supported.</value>
		/// <remarks>This is the same value passed to the IWriteEngine2::WriteSection method.</remarks>
		public int StartLba => progress.StartLba;

		/// <summary>Retrieves the size of the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the internal data buffer that is used for writing to disc.</value>
		public int TotalSystemBuffer => progress.TotalSystemBuffer;

		/// <summary>Retrieves the estimated total time for write operation.</summary>
		/// <value>Estimated time, in seconds, for write operation.</value>
		/// <remarks>
		/// The estimate for a single write operation can vary as the operation progresses. The drive provides updated information that can
		/// affect the projected duration of the write operation.
		/// </remarks>
		public TimeSpan TotalTime => TimeSpan.FromSeconds(progress.TotalTime);

		/// <summary>Retrieves the number of used bytes in the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the used portion of the internal data buffer that is used for writing to disc.</value>
		/// <remarks>This value increases as data is read into the buffer and decreases as data is written to disc.</remarks>
		public int UsedSystemBuffer => progress.UsedSystemBuffer;
	}

	/// <summary>Represents an erase operation on an optical storage device.</summary>
	public class OpticalStorageWriteOperation : OpticalStorageMediaOperation<IDiscFormat2Data>
	{
		/// <summary>Initializes a new instance of the <see cref="OpticalStorageEraseMediaOperation"/> class.</summary>
		/// <param name="clientName">
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock. If <see
		/// langword="null"/>, the name is pulled from the app domain.
		/// </param>
		public OpticalStorageWriteOperation(string clientName = null) : base(new(), clientName)
		{
		}

		/// <summary>Occurs during erase operations to indicate the progress.</summary>
		public event EventHandler<OpticalStorageWriteEventArgs> WriteProgress;

		/// <summary>Determines if Buffer Underrun Free Recording is enabled.</summary>
		/// <value>
		/// Set to <see langword="true"/> to disable Buffer Underrun Free Recording; otherwise, <see langword="false"/>. The default is <see
		/// langword="false"/> (enabled).
		/// </value>
		/// <remarks>
		/// Buffer underrun can be an issue if the data stream does not enter the buffer fast enough to keep the device continuously
		/// writing. In turn, the stop and start action of writing can cause data on the disc to be unusable. Buffer Underrun Free (BUF)
		/// recording allows the laser to start and stop without damaging data already written to the disc. Disabling of BUF recording is
		/// possible only on CD-R/RW media.
		/// </remarks>
		[DefaultValue(false)]
		public bool BufferUnderrunFreeDisabled { get => op.BufferUnderrunFreeDisabled; set => op.BufferUnderrunFreeDisabled = value; }

		/// <summary>Sets the friendly name of the client.</summary>
		/// <value>Name of the client application.</value>
		/// <remarks>
		/// <para>
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock.
		/// </para>
		/// <para>
		/// Because any application with read/write access to the device during the erase operation can use the IOCTL_CDROM_EXCLUSIVE_ACCESS
		/// (query) control code (see the Microsoft Windows Driver Development Kit (DDK)) to access the name, it is important that the name
		/// identify the program that is using this interface to erase to the media. The name is restricted to the same character set as
		/// required by the IOCTL_CDROM_EXCLUSIVE_ACCESS control code.
		/// </para>
		/// </remarks>
		public override string ClientName { get => op.ClientName; set => op.ClientName = value; }

		/// <summary>Gets or sets an <c>IStream</c> interface of the data stream to write.</summary>
		/// <value>The data to write.</value>
		public IStream Data { get; set; }

		/// <summary>Determines if a DVD recording session includes tasks that can increase the chance that a device can play the DVD.</summary>
		/// <value>
		/// <para>
		/// Set to <see langword="true"/> to skip the tasks that allow the disc to play on more consumer devices. Removing compatibility
		/// reduces the recording session time and the need for less free space on disc.
		/// </para>
		/// <para>Set to <see langword="false"/> to increase the chance that a device can play the DVD. The default is <see langword="false"/>.</para>
		/// </value>
		/// <remarks>
		/// <para>This property has no affect on CD media and DVD dash media.</para>
		/// <para>For DVD+R and DVD+DL media, this property will also affect the media closing operation.</para>
		/// <list type="table">
		/// <listheader>
		/// <term>Value of DisableConsumerDvdCompatibilityMode</term>
		/// <term>Value of ForceMediaToBeClosed</term>
		/// <term>Closure operation</term>
		/// </listheader>
		/// <item>
		/// <term>False</term>
		/// <term>True</term>
		/// <term>Closes the disc in compatible mode</term>
		/// </item>
		/// <item>
		/// <term>Fale</term>
		/// <term>False</term>
		/// <term>Closes the disc in compatible mode</term>
		/// </item>
		/// <item>
		/// <term>True</term>
		/// <term>True</term>
		/// <term>Closes the disc normally</term>
		/// </item>
		/// <item>
		/// <term>True</term>
		/// <term>False</term>
		/// <term>Closes the session for DVD+RCloses disc normally for DVD+R DL</term>
		/// </item>
		/// </list>
		/// </remarks>
		[DefaultValue(false)]
		public bool DisableConsumerDvdCompatibilityMode { get => op.DisableConsumerDvdCompatibilityMode; set => op.DisableConsumerDvdCompatibilityMode = value; }

		/// <summary>Determines if further additions to the file system are prevented.</summary>
		/// <value>
		/// <para>Set to <see langword="true"/> to mark the disc as closed to prohibit additional writes when the next write session ends.</para>
		/// <para>Set to <see langword="false"/> to keep the disc open for subsequent write sessions. The default is <see langword="false"/>.</para>
		/// </value>
		/// <remarks>
		/// <para>
		/// When the free space on a disc reaches 2% or less, the write process marks the disc closed, regardless of the value of this
		/// property. This action ensures that a disc has enough free space to record a file system in a write session.
		/// </para>
		/// <para>You can erase a rewritable disc that is marked closed.</para>
		/// <para>
		/// Note that the IDiscFormat2Data::put_DisableConsumerDvdCompatibilityMode property may supersede this property. Please refer to
		/// <c>put_DisableConsumerDvdCompatibilityMode</c> for details.
		/// </para>
		/// </remarks>
		[DefaultValue(false)]
		public bool ForceMediaToBeClosed { get => op.ForceMediaToBeClosed; set => op.ForceMediaToBeClosed = value; }

		/// <summary>Determines if the data writer must overwrite the disc on overwritable media types.</summary>
		/// <value>
		/// Is <see langword="true"/> if the data writer must overwrite the disc on overwritable media types; otherwise, <see
		/// langword="false"/>. The default is <see langword="false"/>.
		/// </value>
		[DefaultValue(false)]
		public bool ForceOverwrite { get => op.ForceOverwrite; set => op.ForceOverwrite = value; }

		/// <summary>Determines if the data stream contains post-writing gaps.</summary>
		/// <value>
		/// Set to <see langword="true"/> if the data stream contains post-writing gaps; otherwise, <see langword="false"/>. The default is
		/// <see langword="false"/>.
		/// </value>
		/// <remarks>
		/// Note that writing to CD-R/RW media will automatically append a post-gap of 150 sectors, unless this property is explicitly disabled.
		/// </remarks>
		[DefaultValue(false)]
		public bool PostgapAlreadyInImage { get => op.PostgapAlreadyInImage; set => op.PostgapAlreadyInImage = value; }

		/// <summary>Retrieves the requested rotational-speed control type.</summary>
		/// <value>
		/// Requested rotational-speed control type. Is <see langword="true"/> for constant angular velocity (CAV) rotational-speed control
		/// type. Otherwise, is <see langword="false"/> for any other rotational-speed control type that the recorder supports.
		/// </value>
		/// <remarks>This is the value specified in the most recent call to the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</remarks>
		[DefaultValue(false)]
		public bool RequestedRotationTypeIsPureCAV
		{
			get => op.RequestedRotationTypeIsPureCAV;
			set => op.SetWriteSpeed(RequestedWriteSpeed, value);
		}

		/// <summary>Retrieves the requested write speed.</summary>
		/// <value>
		/// <para>Requested write speed measured in disc sectors per second.</para>
		/// <para>A value of 0xFFFFFFFF (-1) requests that the write occurs using the fastest supported speed for the media.</para>
		/// </value>
		/// <remarks>This is the value specified in the most recent call to the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</remarks>
		[DefaultValue(-1)]
		public int RequestedWriteSpeed
		{
			get => op.RequestedWriteSpeed;
			set => op.SetWriteSpeed(value, RequestedRotationTypeIsPureCAV);
		}

		/// <summary>Retrieves the current state of the media in the device.</summary>
		/// <value>
		/// State of the media in the disc device. For possible values, see the IMAPI_FORMAT2_DATA_MEDIA_STATE enumeration type. Note that
		/// more than one state can be set.
		/// </value>
		/// <remarks>For an example that uses this property, see Checking Media Support.</remarks>
		public IMAPI_FORMAT2_DATA_MEDIA_STATE CurrentMediaStatus => op.CurrentMediaStatus;

		/// <summary>Retrieves the type of media in the disc device.</summary>
		/// <value>Type of media in the disc device. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPEenumeration type.</value>
		public override IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalMediaType => op.CurrentPhysicalMediaType;

		/// <summary>Retrieves the current rotational-speed control used by the recorder.</summary>
		/// <value>
		/// Is <see langword="true"/> if constant angular velocity (CAV) rotational-speed control is in use. Otherwise, <see
		/// langword="false"/> to indicate that another rotational-speed control that the recorder supports is in use.
		/// </value>
		/// <remarks>
		/// <para>
		/// To retrieve the requested rotational-speed control, call the IDiscFormat2TrackAtOnce::get_RequestedRotationTypeIsPureCAV method.
		/// </para>
		/// <para>Rotational-speed control types include the following:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>CLV (Constant Linear Velocity). The disc is written at a constant speed. Standard rotational control.</term>
		/// </item>
		/// <item>
		/// <term>CAV (Constant Angular Velocity). The disc is written at a constantly increasing speed.</term>
		/// </item>
		/// <item>
		/// <term>
		/// ZCAV (Zone Constant Angular Velocity). The disc is divided into zones. After each zone, the write speed increases. This is an
		/// impure form of CAV.
		/// </term>
		/// </item>
		/// <item>
		/// <term>
		/// PCAV (Partial Constant Angular Velocity). The disc speed increases up to a specified velocity. Once reached, the disc spins at
		/// the specified velocity for the duration of the disc writing.
		/// </term>
		/// </item>
		/// </list>
		/// </remarks>
		public bool CurrentRotationTypeIsPureCAV => op.CurrentRotationTypeIsPureCAV;

		/// <summary>Retrieves the drive's current write speed.</summary>
		/// <value>The write speed of the current media, measured in sectors per second.</value>
		/// <remarks>
		/// <para>To retrieve the requested write speed, call the IDiscFormat2TrackAtOnce::get_RequestedWriteSpeed method.</para>
		/// <para>
		/// To retrieve a list of the write speeds that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeeds method.
		/// </para>
		/// <para>
		/// Note that the write speed is based on the media write speeds. The value of this property can change when a media change occurs.
		/// </para>
		/// </remarks>
		public int CurrentWriteSpeed => op.CurrentWriteSpeed;

		/// <summary>Retrieves the number of sectors available for adding a new track to the media.</summary>
		/// <value>Number of available sectors on the media that can be used for writing audio.</value>
		/// <remarks>
		/// If called during an AddAudioTrack operation, the available sectors do not reflect the sectors used in writing the current audio
		/// track. Instead, the reported value is the number of available sectors immediately preceding the call to AddAudioTrack.
		/// </remarks>
		public int FreeSectorsOnMedia => op.FreeSectorsOnMedia;

		/// <summary>Retrieves the last sector of the previous write session.</summary>
		/// <value>
		/// <para>Address where the previous write operation ended.</para>
		/// <para>
		/// The value is -1 if the media is blank or does not support multi-session writing (indicates that no previous session could be detected).
		/// </para>
		/// </value>
		/// <remarks>
		/// <c>Note</c> This property should not be used. Instead, you should use an interface derived from IMultisession, such as
		/// IMultisessionSequential, for importing file data from the previous session.
		/// </remarks>
		public int LastWrittenAddressOfPreviousSession => op.LastWrittenAddressOfPreviousSession;

		/// <summary>Retrieves a list of available multi-session interfaces.</summary>
		/// <value>
		/// List of available multi-session interfaces. Each element of the array is a <c>VARIANT</c> of type <c>VT_DISPATCH</c>. Query the
		/// <c>pdispVal</c> member of the variant for any interface that inherits from IMultisession interface, for example, IMultisessionSequential.
		/// </value>
		/// <remarks>The array will always contain at least one element.</remarks>
		internal IMultisession[] MultisessionInterfaces => op.MultisessionInterfaces;

		/// <summary>Retrieves the location for the next write operation.</summary>
		/// <value>Address where the next write operation begins.</value>
		/// <remarks>
		/// <para>Blank media begin writing at location zero.</para>
		/// <para>In multi-session writing, the next writable address is useful for setting up a correct file system.</para>
		/// </remarks>
		public int NextWritableAddress => op.NextWritableAddress;

		/// <summary>Retrieves the first sector of the previous write session.</summary>
		/// <value>
		/// <para>Address where the previous write operation began.</para>
		/// <para>
		/// The value is -1 if the media is blank or does not support multi-session writing (indicates that no previous session could be detected).
		/// </para>
		/// </value>
		/// <remarks>
		/// <c>Note</c> This property should not be used. Instead, you should use an interface derived from IMultisession, such as
		/// IMultisessionSequential, for importing file data from the previous session.
		/// </remarks>
		public int StartAddressOfPreviousSession => op.StartAddressOfPreviousSession;

		/// <summary>Retrieves a list of the detailed write configurations supported by the disc recorder and current media.</summary>
		/// <value>
		/// List of the detailed write configurations supported by the disc recorder and current media. Each element of the list is a
		/// <c>VARIANT</c> of type <c>VT_Dispatch</c>. Query the <c>pdispVal</c> member of the variant for the IWriteSpeedDescriptor
		/// interface, which contains the media type, write speed, rotational-speed control type.
		/// </value>
		/// <remarks>
		/// To retrieve a list of the write speeds that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeeds method.
		/// </remarks>
		public WriteSpeedDescriptor[] SupportedWriteSpeedDescriptors => Array.ConvertAll(op.SupportedWriteSpeedDescriptors, i => new WriteSpeedDescriptor(i));

		/// <summary>Retrieves a list of the write speeds supported by the disc recorder and current media.</summary>
		/// <value>
		/// List of the write speeds supported by the disc recorder and current media. Each element of the list is a <c>VARIANT</c> of type
		/// <c>VT_UI4</c>. The <c>ulVal</c> member of the variant contains the number of sectors written per second.
		/// </value>
		/// <remarks>
		/// <para>You can use a speed from the list to set the write speed when calling the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</para>
		/// <para>
		/// To retrieve a list of the write configurations that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeedDescriptors method.
		/// </para>
		/// </remarks>
		public uint[] SupportedWriteSpeeds => op.SupportedWriteSpeeds;

		/// <summary>Retrieves the total sectors available on the media if writing one continuous audio track.</summary>
		/// <value>Number of all sectors on the media that can be used for audio if one continuous audio track was recorded.</value>
		/// <remarks>This property can be retrieved at any time; however, during writing, the value is cached.</remarks>
		public int TotalSectorsOnMedia => op.TotalSectorsOnMedia;

		/// <summary>Retrieves the current write protect state of the media in the device.</summary>
		/// <value>
		/// <para>
		/// The current write protect state of the media in the device. For possible values, see the IMAPI_MEDIA_WRITE_PROTECT_STATE
		/// enumeration type.
		/// </para>
		/// <para>Note that more than one state can be set.</para>
		/// </value>
		public IMAPI_MEDIA_WRITE_PROTECT_STATE WriteProtectStatus => op.WriteProtectStatus;

		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="IDiscRecorder2"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		protected override IDiscRecorder2 Recorder { get => op.Recorder; set => op.Recorder = value; }

		/// <summary>Executes the operation on media in the specified device.</summary>
		/// <param name="device">The device on which to execute the operation.</param>
		public override void Execute()
		{
			if (Device is null)
				throw new InvalidOperationException("No optical device can be found.");

			if (Data is null)
				throw new InvalidOperationException("The Data property must be set with a valid stream.");

			if (string.IsNullOrEmpty(op.ClientName))
				op.ClientName = System.AppDomain.CurrentDomain.FriendlyName;

			// Write the image stream to disc using the specified recorder.
			using (var eventDisp = new ComConnectionPoint(op, new DDiscFormat2DataEventsSink(WriteUpdate)))
				op.Write(Data);   // Burn the stream to disc

			// verify the WriteProtectStatus property gets (IS THIS NEEDED?)
			_ = op.WriteProtectStatus;

			void WriteUpdate(IDiscFormat2Data @object, IDiscFormat2DataEventArgs progress) =>
				WriteProgress?.Invoke(this, new OpticalStorageWriteEventArgs(progress));
		}
	}

	/// <summary>Use retrieve information about the current write operation.</summary>
	public class OpticalStorageWriteRawEventArgs : EventArgs
	{
		private readonly IDiscFormat2RawCDEventArgs progress;

		public OpticalStorageWriteRawEventArgs(IDiscFormat2RawCDEventArgs progress) => this.progress = progress;

		/// <summary>Retrieves the current write action being performed.</summary>
		/// <value>
		/// Current write action being performed. For a list of possible actions, see the IMAPI_FORMAT2_RAW_CD_WRITE_ACTION enumeration type.
		/// </value>
		public IMAPI_FORMAT2_RAW_CD_WRITE_ACTION CurrentAction => progress.CurrentAction;

		/// <summary>Retrieves the total elapsed time of the write operation.</summary>
		/// <value>Elapsed time, in seconds, of the write operation.</value>
		public TimeSpan ElapsedTime => TimeSpan.FromSeconds(progress.ElapsedTime);

		/// <summary>Retrieves the number of unused bytes in the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the unused portion of the internal data buffer that is used for writing to disc.</value>
		/// <remarks>This method returns the same value as if you subtracted IWriteEngine2EventArgs.UsedSystemBuffer from IWriteEngine2EventArgs.TotalSystemBuffer.</remarks>
		public int FreeSystemBuffer => progress.FreeSystemBuffer;

		/// <summary>Retrieves the address of the sector most recently read from the burn image.</summary>
		/// <value>Logical block address of the sector most recently read from the input data stream.</value>
		public int LastReadLba => progress.LastReadLba;

		/// <summary>Retrieves the address of the sector most recently written to the device.</summary>
		/// <value>Logical block address of the sector most recently written to the device.</value>
		public int LastWrittenLba => progress.LastWrittenLba;

		/// <summary>Retrieves the estimated remaining time of the write operation.</summary>
		/// <value>Estimated time, in seconds, needed for the remainder of the write operation.</value>
		/// <remarks>
		/// The estimate for a single write operation can vary as the operation progresses. The drive provides updated information that can
		/// affect the projected duration of the write operation.
		/// </remarks>
		public TimeSpan RemainingTime => TimeSpan.FromSeconds(progress.RemainingTime);

		/// <summary>Retrieves the number of sectors to write to the device in the current write operation.</summary>
		/// <value>The number of sectors to write in the current write operation.</value>
		/// <remarks>This is the same value passed to the IWriteEngine2::WriteSection method.</remarks>
		public int SectorCount => progress.SectorCount;

		/// <summary>Retrieves the starting logical block address (LBA) of the current write operation.</summary>
		/// <value>Starting logical block address of the write operation. Negative values for LBAs are supported.</value>
		/// <remarks>This is the same value passed to the IWriteEngine2::WriteSection method.</remarks>
		public int StartLba => progress.StartLba;

		/// <summary>Retrieves the size of the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the internal data buffer that is used for writing to disc.</value>
		public int TotalSystemBuffer => progress.TotalSystemBuffer;

		/// <summary>Retrieves the number of used bytes in the internal data buffer that is used for writing to disc.</summary>
		/// <value>Size, in bytes, of the used portion of the internal data buffer that is used for writing to disc.</value>
		/// <remarks>This value increases as data is read into the buffer and decreases as data is written to disc.</remarks>
		public int UsedSystemBuffer => progress.UsedSystemBuffer;
	}

	/// <summary>
	/// Represents an operation on an optical storage device to write raw images to a disc device using Disc At Once (DAO) mode (also known
	/// as uninterrupted recording). For information on DAO mode, see the latest revision of the MMC specification at ftp://ftp.t10.org/t10/drafts/mmc5.
	/// </summary>
	public class OpticalStorageWriteRawOperation : OpticalStorageMediaOperation<IDiscFormat2RawCD>
	{
		/// <summary>Initializes a new instance of the <see cref="OpticalStorageWriteRawOperation"/> class.</summary>
		/// <param name="clientName">
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock. If <see
		/// langword="null"/>, the name is pulled from the app domain.
		/// </param>
		public OpticalStorageWriteRawOperation(string clientName = null) : base(new(), clientName)
		{
		}

		/// <summary>Occurs during write operations to indicate the progress.</summary>
		public event EventHandler<OpticalStorageWriteRawEventArgs> WriteProgress;

		/// <summary>Determines if Buffer Underrun Free Recording is enabled.</summary>
		/// <value>
		/// Set to <see langword="true"/> to disable Buffer Underrun Free Recording; otherwise, <see langword="false"/>. The default is <see
		/// langword="false"/> (enabled).
		/// </value>
		/// <remarks>
		/// Buffer underrun can be an issue if the data stream does not enter the buffer fast enough to keep the device continuously
		/// writing. In turn, the stop and start action of writing can cause data on the disc to be unusable. Buffer Underrun Free (BUF)
		/// recording allows the laser to start and stop without damaging data already written to the disc. Disabling of BUF recording is
		/// possible only on CD-R/RW media.
		/// </remarks>
		[DefaultValue(false)]
		public bool BufferUnderrunFreeDisabled { get => op.BufferUnderrunFreeDisabled; set => op.BufferUnderrunFreeDisabled = value; }

		/// <summary>Sets the friendly name of the client.</summary>
		/// <value>Name of the client application.</value>
		/// <remarks>
		/// <para>
		/// The name is used when the write operation requests exclusive access to the device. The <see
		/// cref="OpticalStorageDevice.ExclusiveAccessOwner"/> property contains the name of the client that has the lock.
		/// </para>
		/// <para>
		/// Because any application with read/write access to the device during the erase operation can use the IOCTL_CDROM_EXCLUSIVE_ACCESS
		/// (query) control code (see the Microsoft Windows Driver Development Kit (DDK)) to access the name, it is important that the name
		/// identify the program that is using this interface to erase to the media. The name is restricted to the same character set as
		/// required by the IOCTL_CDROM_EXCLUSIVE_ACCESS control code.
		/// </para>
		/// </remarks>
		public override string ClientName { get => op.ClientName; set => op.ClientName = value; }

		/// <summary>Retrieves the current rotational-speed control used by the recorder.</summary>
		/// <value>
		/// Is <see langword="true"/> if constant angular velocity (CAV) rotational-speed control is in use. Otherwise, <see
		/// langword="false"/> to indicate that another rotational-speed control that the recorder supports is in use.
		/// </value>
		/// <remarks>
		/// <para>
		/// To retrieve the requested rotational-speed control, call the IDiscFormat2TrackAtOnce::get_RequestedRotationTypeIsPureCAV method.
		/// </para>
		/// <para>Rotational-speed control types include the following:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>CLV (Constant Linear Velocity). The disc is written at a constant speed. Standard rotational control.</term>
		/// </item>
		/// <item>
		/// <term>CAV (Constant Angular Velocity). The disc is written at a constantly increasing speed.</term>
		/// </item>
		/// <item>
		/// <term>
		/// ZCAV (Zone Constant Angular Velocity). The disc is divided into zones. After each zone, the write speed increases. This is an
		/// impure form of CAV.
		/// </term>
		/// </item>
		/// <item>
		/// <term>
		/// PCAV (Partial Constant Angular Velocity). The disc speed increases up to a specified velocity. Once reached, the disc spins at
		/// the specified velocity for the duration of the disc writing.
		/// </term>
		/// </item>
		/// </list>
		/// </remarks>
		public bool CurrentRotationTypeIsPureCAV => op.CurrentRotationTypeIsPureCAV;

		/// <summary>Retrieves the drive's current write speed.</summary>
		/// <value>The write speed of the current media, measured in sectors per second.</value>
		/// <remarks>
		/// <para>To retrieve the requested write speed, call the IDiscFormat2TrackAtOnce::get_RequestedWriteSpeed method.</para>
		/// <para>
		/// To retrieve a list of the write speeds that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeeds method.
		/// </para>
		/// <para>
		/// Note that the write speed is based on the media write speeds. The value of this property can change when a media change occurs.
		/// </para>
		/// </remarks>
		public int CurrentWriteSpeed => op.CurrentWriteSpeed;

		/// <summary>Gets or sets an <c>IStream</c> interface of the data stream to write.</summary>
		/// <value>The data to write.</value>
		public IStream Data { get; set; }

		/// <summary>Retrieves the last possible starting position for the leadout area.</summary>
		/// <value>Sector address of the starting position for the leadout area.</value>
		public int LastPossibleStartOfLeadout => op.LastPossibleStartOfLeadout;

		/// <summary>Retrieves the requested rotational-speed control type.</summary>
		/// <value>
		/// Requested rotational-speed control type. Is <see langword="true"/> for constant angular velocity (CAV) rotational-speed control
		/// type. Otherwise, is <see langword="false"/> for any other rotational-speed control type that the recorder supports.
		/// </value>
		/// <remarks>This is the value specified in the most recent call to the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</remarks>
		[DefaultValue(false)]
		public bool RequestedRotationTypeIsPureCAV
		{
			get => op.RequestedRotationTypeIsPureCAV;
			set => op.SetWriteSpeed(RequestedWriteSpeed, value);
		}

		/// <summary>Sets the requested data sector to use for writing the stream.</summary>
		/// <value>
		/// Data sector to use for writing the stream. For possible values, see the IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE enumeration type.
		/// The default is <c>IMAPI_FORMAT2_RAW_CD_SUBCODE_IS_COOKED</c>.
		/// </value>
		/// <remarks>For a list of supported data sector types, call the IDiscFormat2RawCD::get_SupportedSectorTypes method.</remarks>
		public IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE RequestedSectorType { get => op.RequestedSectorType; set => op.RequestedSectorType = value; }

		/// <summary>Retrieves the requested write speed.</summary>
		/// <value>
		/// <para>Requested write speed measured in disc sectors per second.</para>
		/// <para>A value of 0xFFFFFFFF (-1) requests that the write occurs using the fastest supported speed for the media.</para>
		/// </value>
		/// <remarks>This is the value specified in the most recent call to the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</remarks>
		[DefaultValue(-1)]
		public int RequestedWriteSpeed
		{
			get => op.RequestedWriteSpeed;
			set => op.SetWriteSpeed(value, RequestedRotationTypeIsPureCAV);
		}

		/// <summary>Retrieves the first sector of the next session.</summary>
		/// <value>Sector number for the start of the next write operation. This value can be negative for blank media.</value>
		/// <remarks>
		/// The client application that creates an image must provide appropriately sized lead-in and lead-out data. The application
		/// developer using the IDiscFormat2RawCD interface must understand the formats of lead-in and lead-out for the first and subsequent
		/// sessions. Note that lead-in LBA for the first session is negative.
		/// </remarks>
		public int StartOfNextSession => op.StartOfNextSession;

		/// <summary>Retrieves the supported data sector types for the current recorder.</summary>
		/// <value>
		/// <para>
		/// List of data sector types for the current recorder. Each element of the list is a <c>VARIANT</c> of type <c>VT_UI4</c>. The
		/// <c>ulVal</c> member of the variant contains the data sector type.
		/// </para>
		/// <para>For a list of values of supported sector types, see IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE.</para>
		/// </value>
		public IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE[] SupportedSectorTypes => op.SupportedSectorTypes;

		/// <summary>Retrieves a list of the detailed write configurations supported by the disc recorder and current media.</summary>
		/// <value>
		/// List of the detailed write configurations supported by the disc recorder and current media. Each element of the list is a
		/// <c>VARIANT</c> of type <c>VT_Dispatch</c>. Query the <c>pdispVal</c> member of the variant for the IWriteSpeedDescriptor
		/// interface, which contains the media type, write speed, rotational-speed control type.
		/// </value>
		/// <remarks>
		/// To retrieve a list of the write speeds that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeeds method.
		/// </remarks>
		public WriteSpeedDescriptor[] SupportedWriteSpeedDescriptors => Array.ConvertAll(op.SupportedWriteSpeedDescriptors, i => new WriteSpeedDescriptor(i));

		/// <summary>Retrieves a list of the write speeds supported by the disc recorder and current media.</summary>
		/// <value>
		/// List of the write speeds supported by the disc recorder and current media. Each element of the list is a <c>VARIANT</c> of type
		/// <c>VT_UI4</c>. The <c>ulVal</c> member of the variant contains the number of sectors written per second.
		/// </value>
		/// <remarks>
		/// <para>You can use a speed from the list to set the write speed when calling the IDiscFormat2TrackAtOnce::SetWriteSpeed method.</para>
		/// <para>
		/// To retrieve a list of the write configurations that the recorder and current media supports, call the
		/// IDiscFormat2TrackAtOnce::get_SupportedWriteSpeedDescriptors method.
		/// </para>
		/// </remarks>
		public uint[] SupportedWriteSpeeds => op.SupportedWriteSpeeds;

		/// <summary>Retrieves the type of media in the disc device.</summary>
		/// <value>Type of media in the disc device. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPEenumeration type.</value>
		public override IMAPI_MEDIA_PHYSICAL_TYPE CurrentPhysicalMediaType => op.CurrentPhysicalMediaType;

		/// <summary>Gets or sets the recording device to use for the operation.</summary>
		/// <value>An <see cref="IDiscRecorder2"/> instance that identifies the recording device to use in the write operation.</value>
		/// <remarks>
		/// The recorder must be compatible with the format defined by this interface. To determine compatibility, call the <see
		/// cref="SupportsDevice"/> method.
		/// </remarks>
		protected override IDiscRecorder2 Recorder { get => op.Recorder; set => op.Recorder = value; }

		/// <summary>Executes the operation on media in the specified device.</summary>
		/// <param name="device">The device on which to execute the operation.</param>
		public override void Execute()
		{
			if (Device is null)
				throw new InvalidOperationException("No optical device can be found.");

			if (Data is null)
				throw new InvalidOperationException("The Data property must be set with a valid stream.");

			if (string.IsNullOrEmpty(op.ClientName))
				op.ClientName = System.AppDomain.CurrentDomain.FriendlyName;

			// prepare media
			op.PrepareMedia();

			// Write the image stream to disc using the specified recorder.
			using (var eventDisp = new ComConnectionPoint(op, new DDiscFormat2RawCDEventsSink(WriteUpdate)))
				op.WriteMedia(Data);   // Burn the stream to disc

			// release media (even if the burn failed)
			op.ReleaseMedia();

			void WriteUpdate(IDiscFormat2RawCD @object, IDiscFormat2RawCDEventArgs progress) =>
				WriteProgress?.Invoke(this, new OpticalStorageWriteRawEventArgs(progress));
		}
	}

	/// <summary>
	/// Use this class to retrieve detailed write configurations supported by the disc recorder and current media, for example, the media
	/// type, write speed, rotational-speed control type.
	/// </summary>
	public class WriteSpeedDescriptor
	{
		internal WriteSpeedDescriptor(IWriteSpeedDescriptor desc)
		{
			MediaType = desc.MediaType;
			RotationTypeIsPureCAV = desc.RotationTypeIsPureCAV;
			WriteSpeed = desc.WriteSpeed;
		}

		/// <summary>Retrieves type of media in the current drive.</summary>
		/// <value>Type of media in the current drive. For possible values, see the IMAPI_MEDIA_PHYSICAL_TYPE enumeration type.</value>
		public IMAPI_MEDIA_PHYSICAL_TYPE MediaType { get; }

		/// <summary>Retrieves the supported rotational-speed control used by the recorder for the current media.</summary>
		/// <value>
		/// Is <see langword="true"/> if constant angular velocity (CAV) rotational-speed control is in use. Otherwise, <see
		/// langword="false"/> to indicate that another rotational-speed control that the recorder supports is in use.
		/// </value>
		/// <remarks>
		/// <para>Rotational-speed control types include the following:</para>
		/// <list type="bullet">
		/// <item>
		/// <term>CLV (Constant Linear Velocity). The disc is written at a constant speed. Standard rotational control.</term>
		/// </item>
		/// <item>
		/// <term>CAV (Constant Angular Velocity). The disc is written at a constantly increasing speed.</term>
		/// </item>
		/// <item>
		/// <term>
		/// ZCAV (Zone Constant Linear Velocity). The disc is divided into zones. After each zone, the write speed increases. This is an
		/// impure form of CAV.
		/// </term>
		/// </item>
		/// <item>
		/// <term>
		/// PCAV (Partial Constant Angular Velocity). The disc speed increases up to a specified velocity. Once reached, the disc spins at
		/// the specified velocity for the duration of the disc writing.
		/// </term>
		/// </item>
		/// </list>
		/// </remarks>
		public bool RotationTypeIsPureCAV { get; }

		/// <summary>Retrieves the supported write speed for writing to the media.</summary>
		/// <value>Write speed of the current media, measured in sectors per second.</value>
		/// <remarks>The write speed is based on the media write speeds. The value of this property can change when a media change occurs.</remarks>
		public int WriteSpeed { get; }
	}

	internal class AudioFile : ComFileStream
	{
		private static readonly int hdrSize = Marshal.SizeOf(typeof(WAV_HEADER));
		private const long SECTOR_SIZE = 2352;

		public AudioFile(string fileName)
		{
			if (!File.Exists(fileName))
				throw new FileNotFoundException("File not found.", fileName);

			using (var hFile = Kernel32.CreateFile(fileName, Kernel32.FileAccess.FILE_GENERIC_READ, FileShare.Read, null, FileMode.Open, 0))
			{
				if (hFile.IsInvalid || !Kernel32.GetFileSizeEx(hFile, out var fileLen))
					throw new ArgumentException("File is unavailable or empty.", nameof(fileName));

				if (!IsValidIMAPIFormat(hFile))
					throw new InvalidOperationException("The file does not match the required 44.1KHz, Stereo, Uncompressed WAV format.");

				var sz = (int)fileLen - hdrSize;
				var hMem = Marshal.AllocHGlobal(sz);
				if (!Kernel32.SetFilePointerEx(hFile, hdrSize, default, SeekOrigin.Begin) || !Kernel32.ReadFile(hFile, hMem, (uint)sz, out _))
					throw new ArgumentException("Unable to read file.", nameof(fileName));
				Ole32.CreateStreamOnHGlobal(hMem, true, out stream).ThrowIfFailed();
			}

			// Mod the size so that it is byte aligned
			stream.Stat(out STATSTG stat, 1 /*STATFLAG_DEFAULT*/);
			var newSize = ((stat.cbSize / SECTOR_SIZE) + 1) * SECTOR_SIZE;
			stream.SetSize(newSize);

			// Skip header
			stream.Seek(hdrSize, 0, default);
		}

		public static bool IsValidIMAPIFormat(HFILE hFile)
		{
			using var pwavHeader = new SafeHGlobalStruct<WAV_HEADER>();
			if (Kernel32.SetFilePointerEx(hFile, 0, default, SeekOrigin.Begin) && Kernel32.ReadFile(hFile, pwavHeader, pwavHeader.Size, out _))
			{
				var wavHeader = pwavHeader.Value;
				return (wavHeader.chunkID == 0x46464952) && // "RIFF"
				  (wavHeader.format == 0x45564157) && // "WAVE"
				  (wavHeader.formatChunkId == 0x20746d66) && // "fmt "
				  (wavHeader.audioFormat == 1) && // 1 = PCM (uncompressed)
				  (wavHeader.numChannels == 2) && // 2 = Stereo
				  (wavHeader.sampleRate == 44100); // 44.1 KHz
			}
			return false;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct WAV_HEADER
		{
			public uint chunkID;
			public uint chunkSize;
			public uint format;
			public uint formatChunkId;
			public uint formatChunkSize;
			public ushort audioFormat;
			public ushort numChannels;
			public uint sampleRate;
			public uint byteRate;
			public ushort blockAlign;
			public ushort bitsPerSample;
			public uint dataChunkId;
			public uint dataChunkSize;
		}
	}

	internal class ComFileStream : IDisposable
	{
		protected IStream stream;

		public ComFileStream(string fileName, bool readOnly = true, long align = 0)
		{
			STGM mode = readOnly ? STGM.STGM_READ | STGM.STGM_SHARE_DENY_NONE : STGM.STGM_READWRITE;
			SHCreateStreamOnFile(fileName, mode, out stream).ThrowIfFailed();

			// Mod the size so that it is byte aligned
			if (align > 0)
			{
				stream.Stat(out STATSTG stat, 1 /*STATFLAG_DEFAULT*/);
				var newSize = ((stat.cbSize / align) + 1) * align;
				stream.SetSize(newSize);
			}
		}

		protected ComFileStream() { }

		public IStream NativeInterface => stream;

		public IStream Release() { IStream s = stream; stream = null; return s; }

		public void Dispose()
		{
			if (stream is null) return;
#pragma warning disable CA1416 // Validate platform compatibility
			System.Runtime.InteropServices.Marshal.ReleaseComObject(stream);
#pragma warning restore CA1416 // Validate platform compatibility
			stream = null;
			GC.SuppressFinalize(this);
		}
	}
}