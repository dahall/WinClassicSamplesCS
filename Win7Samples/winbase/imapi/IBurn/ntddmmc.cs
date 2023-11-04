using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;

namespace Vanara.PInvoke
{
	[PInvokeData("scsi.h")]
	[StructLayout(LayoutKind.Sequential)]
	public struct OPC_TABLE_ENTRY
	{
		private ulong bytes;

		public ushort Speed { get => (ushort)BitHelper.GetBits(bytes, 0, 16); set => BitHelper.SetBits(ref bytes, 0, 16, value); }
		public ulong OPCValue { get => (ushort)BitHelper.GetBits(bytes, 16, 48); set => BitHelper.SetBits(ref bytes, 16, 48, value); }
	}

	[VanaraMarshaler(typeof(SafeAnysizeStructMarshaler<DISC_INFORMATION>), nameof(NumberOPCEntries))]
	[PInvokeData("scsi.h")]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DISC_INFORMATION
	{
		public ushort Length;
		private byte bits1;
		public byte DiscStatus { get => BitHelper.GetBits(bits1, 0, 2); set => BitHelper.SetBits(ref bits1, 0, 2, value); }
		public byte LastSessionStatus { get => BitHelper.GetBits(bits1, 2, 2); set => BitHelper.SetBits(ref bits1, 2, 2, value); }
		public bool Erasable { get => BitHelper.GetBit(bits1, 4); set => BitHelper.SetBit(ref bits1, 4, value); }
		public byte FirstTrackNumber;
		public byte NumberOfSessionsLsb;
		public byte LastSessionFirstTrackLsb;
		public byte LastSessionLastTrackLsb;
		private byte bits2;
		public byte MrwStatus { get => BitHelper.GetBits(bits2, 0, 2); set => BitHelper.SetBits(ref bits2, 0, 2, value); }
		public bool MrwDirtyBit { get => BitHelper.GetBit(bits2, 2); set => BitHelper.SetBit(ref bits2, 2, value); }
		public bool URU { get => BitHelper.GetBit(bits2, 5); set => BitHelper.SetBit(ref bits2, 5, value); }
		public bool DBC_V { get => BitHelper.GetBit(bits2, 6); set => BitHelper.SetBit(ref bits2, 6, value); }
		public bool DID_V { get => BitHelper.GetBit(bits2, 7); set => BitHelper.SetBit(ref bits2, 7, value); }
		public byte DiscType;
		public byte NumberOfSessionsMsb;
		public byte LastSessionFirstTrackMsb;
		public byte LastSessionLastTrackMsb;
		public uint DiskIdentification;
		public uint LastSessionLeadIn; // HMSF
		public uint LastPossibleLeadOutStartTime; // HMSF
		public ulong DiskBarCode;
		public byte Reserved4;
		public byte NumberOPCEntries;

		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
		public OPC_TABLE_ENTRY[] OPCTable; // can be many of these here....
	}
	
	/// <summary>The GET_CONFIGURATION_HEADER structure is used to format the output data retrieved by the IOCTL_CDROM_GET_CONFIGURATION request.</summary>
	// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddmmc/ns-ntddmmc-_get_configuration_header
	// typedef struct _GET_CONFIGURATION_HEADER { UCHAR DataLength[4]; UCHAR Reserved[2]; UCHAR CurrentProfile[2]; UCHAR Data[0]; } GET_CONFIGURATION_HEADER, *PGET_CONFIGURATION_HEADER;
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._GET_CONFIGURATION_HEADER")]
	[StructLayout(LayoutKind.Sequential, Pack = 2)]
	public struct GET_CONFIGURATION_HEADER
	{
		private uint dataLength;

		/// <summary>
		/// Indicates the amount of data, in bytes, that is being returned in the buffer area pointed to by the <c>Data</c> member. If the
		/// data length is greater than 65,530 bytes, multiple GET CONFIGURATION commands will be required for the Initiator to read all
		/// configuration data. The bytes in this array are arranged in big-endian order. <c>DataLength</c>[0] has the most significant
		/// byte, and <c>DataLength</c>[3] has the least significant byte.
		/// </summary>
		public uint DataLength { get => BinaryPrimitives.ReverseEndianness(dataLength); set => dataLength = BinaryPrimitives.ReverseEndianness(value); }

		/// <summary>Reserved.</summary>
		public ushort Reserved;

		private ushort currentProfile;

		/// <summary>
		/// Contains an enumerator value of type FEATURE_PROFILE_TYPE that indicates the device's current profile. The bytes in this array
		/// are arranged in big-endian order. <c>CurrentProfile</c>[0] has the most significant byte, and <c>CurrentProfile</c>[3] has the
		/// least significant byte.
		/// </summary>
		public FEATURE_PROFILE_TYPE CurrentProfile { get => (FEATURE_PROFILE_TYPE)BinaryPrimitives.ReverseEndianness(currentProfile); set => currentProfile = BinaryPrimitives.ReverseEndianness((ushort)value); }
	}

	/// <summary>The FEATURE_HEADER structure is used in conjunction with the IOCTL_CDROM_GET_CONFIGURATION request to report header information for both feature and profile descriptors.</summary>
	// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddmmc/ns-ntddmmc-_feature_header
	// typedef struct _FEATURE_HEADER { UCHAR FeatureCode[2]; UCHAR Current : 1; UCHAR Persistent : 1; UCHAR Version : 4; UCHAR Reserved0 : 2; UCHAR AdditionalLength; } FEATURE_HEADER, *PFEATURE_HEADER;
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._FEATURE_HEADER")]
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FEATURE_HEADER
	{
		private ushort featureCode;

		/// <summary>
		/// Contains a value between zero and 0xffff that indicates a feature. The FEATURE_NUMBER enumeration provides a list of currently
		/// supported feature numbers. <c>FeatureCode</c>[0] contains the most significant byte of the feature number. <c>FeatureCode</c>[1]
		/// contains the least significant byte.
		/// </summary>
		public FEATURE_NUMBER FeatureCode { get => (FEATURE_NUMBER)BinaryPrimitives.ReverseEndianness(featureCode); set => featureCode = BinaryPrimitives.ReverseEndianness((ushort)value); }

		private byte flags;

		/// <summary>
		/// Indicates, when set to 1, that this feature is currently active and the data reported for the feature is valid. When set to
		/// zero, this bit indicates that the feature is not currently active and that the data reported for the feature might not be valid.
		/// </summary>
		public bool Current { get => BitHelper.GetBit(flags, 0); set => BitHelper.SetBit(ref flags, 0, value); }

		/// <summary>Indicates, when set to 1, that the feature is always active. When set to zero, this bit indicates that the feature is not always active.</summary>
		public bool Persistent { get => BitHelper.GetBit(flags, 1); set => BitHelper.SetBit(ref flags, 1, value); }

		/// <summary>Must be set to zero unless otherwise specified within the description for a particular feature.</summary>
		public byte Version { get => BitHelper.GetBits(flags, 2, 4); set => BitHelper.SetBits(ref flags, 2, 4, value); }

		/// <summary>
		/// Indicates the number of bytes of feature information that follow this header. This member must be an integral multiple of 4. The
		/// total size of the data related to this feature will be <c>AdditionalLength</c> + <c>sizeof</c>(FEATURE_HEADER).
		/// </summary>
		public byte AdditionalLength;
	}

	/// <summary>
	/// The FEATURE_PROFILE_TYPE enumeration provides a list of the profile names that are defined by the SCSI Multimedia - 4 (MMC-4) specification.
	/// </summary>
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._FEATURE_PROFILE_TYPE")]
	public enum FEATURE_PROFILE_TYPE : ushort
	{
		/// <summary>
		/// Does not indicate a valid profile.
		/// </summary>
		ProfileInvalid = 0x0000,
		/// <summary>
		/// Indicates the profile named "Nonremovable disk" by the SCSI-3 Multimedia (MMC-3) specification. This profile is used with devices that manage rewritable media and are capable of changing behavior.
		/// </summary>
		ProfileNonRemovableDisk = 0x0001,
		/// <summary>
		/// Indicates the profile named "Removable disk" by the MMC-3 specification. This profile is used with devices that manage rewritable, removable media.
		/// </summary>
		ProfileRemovableDisk = 0x0002,
		/// <summary>
		/// Indicates the profile named "MO Erasable" by the MMC-3 specification. This profile is used with devices that manage magneto-optical media and that have a sector-erase capability.
		/// </summary>
		ProfileMOErasable = 0x0003,
		/// <summary>
		/// Indicates the profile named "MO Write Once" by the MMC-3 specification. This profile is used with devices that manage magneto-optical write-once media.
		/// </summary>
		ProfileMOWriteOnce = 0x0004,
		/// <summary>
		/// Indicates the profile named "AS-MO" by the MMC-3 specification. This profile is used with devices that implement Advance Storage technology and manage magneto-optical media.
		/// </summary>
		ProfileAS_MO = 0x0005,
		// Reserved 0x0006 - 0x0007
		/// <summary>
		/// Indicates the profile named "CD-ROM" by the MMC-3 specification. This profile is used with devices that manage read-only compact disc media.
		/// </summary>
		ProfileCdrom = 0x0008,
		/// <summary>
		/// Indicates the profile named "CD-R" by the MMC-3 specification. This profile is used with devices that manage write-once compact disc media.
		/// </summary>
		ProfileCdRecordable = 0x0009,
		/// <summary>
		/// Indicates the profile named "CD-RW" by the MMC-3 specification. This profile is used with devices that manage rewritable compact disc media.
		/// </summary>
		ProfileCdRewritable = 0x000a,
		// Reserved 0x000b - 0x000f
		/// <summary>
		/// Indicates the profile named "DVD-ROM" by the MMC-3 specification. This profile is used with devices that manage read-only DVD media.
		/// </summary>
		ProfileDvdRom = 0x0010,
		/// <summary>
		/// Indicates the profile named "DVD-R" by the MMC-3 specification. This profile is used with devices that manage write-once DVD media and operate in sequential recording mode.
		/// </summary>
		ProfileDvdRecordable = 0x0011,
		/// <summary>
		/// Indicates the profile named "DVD-RAM or DVD+RW" by the MMC-3 specification. This profile is used with devices that manage rewritable DVD media.
		/// </summary>
		ProfileDvdRam = 0x0012,
		/// <summary>
		/// Indicates the profile named "DVD-RW Restricted Overwrite" by the MMC-3 specification. This profile is used with devices that manage rerecordable DVD media and operate in packet-writing mode.
		/// </summary>
		ProfileDvdRewritable = 0x0013, // restricted overwrite
		/// <summary>
		/// Indicates the profile named "DVD-RW Sequential Recording" by the MMC-3 specification. This profile is used with devices that implement a series of features associated with sequential recording, such as the features "Incremental Streaming Writable" and "Real-Time Streaming". For a full list of the features supported with this profile, see the MMC-3 specification.
		/// </summary>
		ProfileDvdRWSequential = 0x0014,
		/// <summary/>
		ProfileDvdDashRDualLayer = 0x0015,
		/// <summary/>
		ProfileDvdDashRLayerJump = 0x0016,
		// Reserved 0x0017 - 0x0019
		/// <summary>
		/// Indicates the profile named "DVD+RW" by the MMC-3 specification. This profile is used with devices that implement a series of features required to manage DVD media that is both readable and writable. For a full list of the features supported with this profile, see the MMC-3 specification.
		/// </summary>
		ProfileDvdPlusRW = 0x001A,
		/// <summary/>
		ProfileDvdPlusR = 0x001B,
		// Reserved 0x001C - 001F
		/// <summary>
		/// Indicates the profile named "DDCD-ROM" by the MMC-3 specification. This profile is used with devices that can read "DDCD specific structure." For a full list of the features supported with this profile, see the MMC-3 specification.
		/// </summary>
		ProfileDDCdrom = 0x0020, // obsolete
		/// <summary>
		/// Indicates the profile named "DDCD-R" by the MMC-3 specification. This profile is used with devices that can read "DDCD-R specific structure." For a full list of the features supported with this profile, see the MMC-3 specification.
		/// </summary>
		ProfileDDCdRecordable = 0x0021, // obsolete
		/// <summary>
		/// Indicates the profile named "DDCD-RW" by the MMC-3 specification. This profile is used with devices that can read "DDCD-RW specific structure." For a full list of the features supported with this profile, see the MMC-3 specification.
		/// </summary>
		ProfileDDCdRewritable = 0x0022, // obsolete
										// Reserved 0x0023 - 0x0029
		/// <summary/>
		ProfileDvdPlusRWDualLayer = 0x002A,
		/// <summary/>
		ProfileDvdPlusRDualLayer = 0x002B,
		// Reserved 0x002C - 0x003F
		/// <summary/>
		ProfileBDRom = 0x0040,
		/// <summary>
		/// BD-R 'SRM'
		/// </summary>
		ProfileBDRSequentialWritable = 0x0041, // BD-R 'SRM'
		/// <summary>
		/// BD-R 'RRM'
		/// </summary>
		ProfileBDRRandomWritable = 0x0042, // BD-R 'RRM'
		/// <summary/>
		ProfileBDRewritable = 0x0043,
		// Reserved 0x0044 - 0x004F
		/// <summary/>
		ProfileHDDVDRom = 0x0050,
		/// <summary/>
		ProfileHDDVDRecordable = 0x0051,
		/// <summary/>
		ProfileHDDVDRam = 0x0052,
		/// <summary/>
		ProfileHDDVDRewritable = 0x0053,
		// Reserved 0x0054 - 0x0057
		/// <summary/>
		ProfileHDDVDRDualLayer = 0x0058,
		// Reserved 0x0059 - 0x0059
		/// <summary/>
		ProfileHDDVDRWDualLayer = 0x005A,
		// Reserved 0x005B - 0xfffe
		/// <summary>
		/// Indicates that the device does not conform to any profile.
		/// </summary>
		ProfileNonStandard = 0xffff
	}

	/// <summary>
	/// The FEATURE_NUMBER enumeration provides a list of the features that are defined by the SCSI Multimedia - 4 (MMC-4) specification.
	/// </summary>
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._FEATURE_NUMBER")]
	public enum FEATURE_NUMBER : ushort
	{
		/// <summary>
		/// Indicates the feature named "Profile List" by the MMC-3 specification. This feature provides a list of all profiles supported by the device.
		/// </summary>
		FeatureProfileList = 0x0000,
		/// <summary>
		/// Indicates the feature named "Core" by the MMC-3 specification. This feature encompasses the basic functionality which is mandatory for all devices that support the MMC-3 standard. See the MMC-3 specification for a description of the capabilities included in the Core feature.
		/// </summary>
		FeatureCore = 0x0001,
		/// <summary>
		/// Indicates the feature named "Morphing" by the MMC-3 specification. Devices that support this feature can notify the initiator of operational changes and allow the initiator to prevent operational changes.
		/// </summary>
		FeatureMorphing = 0x0002,
		/// <summary>
		/// Indicates the feature named "Removable Medium" by the MMC-3 specification. Devices that support this feature allow the medium to be removed from the device. They also can communicate to the initiator that the user wants to eject the medium or has inserted a new medium.
		/// </summary>
		FeatureRemovableMedium = 0x0003,
		/// <summary>
		/// Indicates the feature named "Write Protect" by the MMC-3 specification. Devices that support this feature allow the initiator to change the write-protection state of the media programmatically.
		/// </summary>
		FeatureWriteProtect = 0x0004,
		/// <summary>
		/// Indicates the feature named "Random Readable" by the MMC-3 specification. Devices that support this feature allow the initiator to read blocks of data on the disk at random locations. These devices do not require that the initiator address disk locations in any particular order.
		/// </summary>
		FeatureRandomReadable = 0x0010,
		/// <summary>
		/// Indicates the feature named "MultiRead," originally defined by the Optical Storage Technology Association (OSTA) and incorporated into the MMC-3 specification. Devices that support this feature can read all CD media types.
		/// </summary>
		FeatureMultiRead = 0x001D,
		/// <summary>
		/// Indicates the feature named "CD Read" by the MMC-3 specification. Devices that support this feature can read CD-specific information from the media and can read user data from all types of CD blocks.
		/// </summary>
		FeatureCdRead = 0x001E,
		/// <summary>
		/// Indicates the feature named "DVD Read" by the MMC-3 specification. Devices that support this feature can read DVD-specific information from the media.
		/// </summary>
		FeatureDvdRead = 0x001F,
		/// <summary>
		/// Indicates the feature named "Random Writable" by the MMC-3 specification. Devices that support this feature can write blocks of data to random locations on the disk. These devices do not require that the initiator address disk locations in any particular order.
		/// </summary>
		FeatureRandomWritable = 0x0020,
		/// <summary>
		/// Indicates the feature named "Incremental Streaming Writable" by the MMC-3 specification. Devices that support this feature can append data to a limited number of locations on the media.
		/// </summary>
		FeatureIncrementalStreamingWritable = 0x0021,
		/// <summary>
		/// Indicates the feature named "Sector Erasable" by the MMC-3 specification. Devices that support this feature require an erase pass before overwriting existing data.
		/// </summary>
		FeatureSectorErasable = 0x0022,
		/// <summary>
		/// Indicates the feature named "Formattable" by the MMC-3 specification. Devices that support this feature can format media into logical blocks.
		/// </summary>
		FeatureFormattable = 0x0023,
		/// <summary>
		/// Indicates the feature named "Defect Management" by the MMC-3 specification. Devices that support this feature are able to provide contiguous address space that is guaranteed to be defect free.
		/// </summary>
		FeatureDefectManagement = 0x0024,
		/// <summary>
		/// Indicates the feature named "Write Once" by the MMC-3 specification. Devices that support this feature can write to any previously unused logical block.
		/// </summary>
		FeatureWriteOnce = 0x0025,
		/// <summary>
		/// Indicates the feature named "Restricted Overwrite" by the MMC-3 specification. Devices that support this feature are limited in regard to which logical blocks they can overwrite at any given time.
		/// </summary>
		FeatureRestrictedOverwrite = 0x0026,
		/// <summary>
		/// Indicates the feature named "CD-RW CAV Write" by the MMC-3 specification. Devices that support this feature can perform writes on CD-RW media in CAV mode.
		/// </summary>
		FeatureCdrwCAVWrite = 0x0027,
		/// <summary>
		/// Indicates the feature named "MRW" by the MMC-3 specification. Devices that support this feature can recognize, read and optionally write MRW formatted media.
		/// </summary>
		FeatureMrw = 0x0028,
		/// <summary/>
		FeatureEnhancedDefectReporting = 0x0029,
		/// <summary>
		/// Indicates the feature named "DVD+RW" by the MMC-3 specification. Devices that support this feature can recognize, read and optionally write DVD+RW media.
		/// </summary>
		FeatureDvdPlusRW = 0x002A,
		/// <summary/>
		FeatureDvdPlusR = 0x002B,
		/// <summary>
		/// Indicates the feature named "DVD-RW Restricted Overwrite" by the MMC-3 specification. Devices that support this feature can only write on block boundaries. These devices cannot perform read or write operations that transfer less than a block of data.
		/// </summary>
		FeatureRigidRestrictedOverwrite = 0x002C,
		/// <summary>
		/// Indicates the feature named "CD Track at Once" by the MMC-3 specification. Devices that support this feature can write data to a CD track.
		/// </summary>
		FeatureCdTrackAtOnce = 0x002D,
		/// <summary>
		/// Indicates the feature named "CD Mastering" by the MMC-3 specification. Devices that support this feature can write to a CD in either "Session-at-Once" mode or raw mode.
		/// </summary>
		FeatureCdMastering = 0x002E,
		/// <summary>
		/// Indicates the feature named "DVD-R Write" by the MMC-3 specification. Devices that support this feature can write data to a write-once DVD media in "Disc-at-Once" mode.
		/// </summary>
		FeatureDvdRecordableWrite = 0x002F, // both -R and -RW
		/// <summary>
		/// Indicates the feature named "DDCD Read" by the MMC-3 specification. Devices that support this feature can read user data from DDCD blocks.
		/// </summary>
		FeatureDDCDRead = 0x0030, // obsolete
		/// <summary>
		/// Indicates the feature named "DDCD-R Write" by the MMC-3 specification. Devices that support this feature can read and write DDCD-R media.
		/// </summary>
		FeatureDDCDRWrite = 0x0031, // obsolete
		/// <summary>
		/// Indicates the feature named "DDCD-RW Write" by the MMC-3 specification. Devices that support this feature can read and write DDCD-RW media.
		/// </summary>
		FeatureDDCDRWWrite = 0x0032, // obsolete
		/// <summary/>
		FeatureLayerJumpRecording = 0x0033,
		/// <summary/>
		FeatureCDRWMediaWriteSupport = 0x0037,
		/// <summary/>
		FeatureBDRPseudoOverwrite = 0x0038,
		/// <summary/>
		FeatureDvdPlusRWDualLayer = 0x003A,
		/// <summary/>
		FeatureDvdPlusRDualLayer = 0x003B,
		/// <summary/>
		FeatureBDRead = 0x0040,
		/// <summary/>
		FeatureBDWrite = 0x0041,
		/// <summary/>
		FeatureTSR = 0x0042,
		/// <summary/>
		FeatureHDDVDRead = 0x0050,
		/// <summary/>
		FeatureHDDVDWrite = 0x0051,
		/// <summary/>
		FeatureHybridDisc = 0x0080,
		/// <summary>
		/// Indicates the feature named "Power Management" by the MMC-3 specification. Devices that support this feature can perform both initiator and logical-unit directed power management.
		/// </summary>
		FeaturePowerManagement = 0x0100,
		/// <summary>
		/// Indicates the feature named "S.M.A.R.T." by the MMC-3 specification. Devices that support this feature support Self-Monitoring Analysis and Reporting Technology (SMART).
		/// </summary>
		FeatureSMART = 0x0101,
		/// <summary>
		/// Indicates the feature named "Embedded Changer" by the MMC-3 specification. Devices that support this feature can move media back and forth between a media storage area and the mechanism that actually accesses the media.
		/// </summary>
		FeatureEmbeddedChanger = 0x0102,
		/// <summary>
		/// Indicates the feature named "CD Audio External Play" by the MMC-3 specification. Devices that support this feature can play CD audio data and channel it directly to an external output.
		/// </summary>
		FeatureCDAudioAnalogPlay = 0x0103, // obsolete
		/// <summary>
		/// Indicates the feature named "Microcode Upgrade" by the MMC-3 specification. Devices that support this feature can upgrade their internal microcode by means of a published interface.
		/// </summary>
		FeatureMicrocodeUpgrade = 0x0104,
		/// <summary>
		/// Indicates the feature named "Time-Out" by the MMC-3 specification. Devices that have this feature must respond to commands within a set time period. When these devices cannot complete commands in the allotted time, they complete the commands with an error.
		/// </summary>
		FeatureTimeout = 0x0105,
		/// <summary>
		/// Indicates the feature named "DVD-CSS" by the MMC-3 specification. Devices that support this feature can perform DVD Content Scrambling System (DVD-CSS) authentication and key management.
		/// </summary>
		FeatureDvdCSS = 0x0106,
		/// <summary>
		/// Indicates the feature named "Real Time Streaming" by the MMC-3 specification. Devices that support this feature allow the initiator to specify the performance level of the device within certain limits allowed by the device. These devices must also indicate to the initiator whether they support stream playback operations.
		/// </summary>
		FeatureRealTimeStreaming = 0x0107,
		/// <summary>
		/// Indicates the feature named "Device Serial Number" by the MMC-3 specification. Devices that support this feature can furnish the initiator with a serial number that uniquely identifies the device.
		/// </summary>
		FeatureLogicalUnitSerialNumber = 0x0108,
		/// <summary/>
		FeatureMediaSerialNumber = 0x0109,
		/// <summary>
		/// Indicates the feature named "Disc Control Blocks" by the MMC-3 specification. Devices that support this feature can read or write Disc Control Blocks.
		/// </summary>
		FeatureDiscControlBlocks = 0x010A,
		/// <summary>
		/// Indicates the feature named "DVD CPRM" by the MMC-3 specification. Devices that support this feature can perform DVD Content Protection for Recordable Media (CPRM) authentication and key management.
		/// </summary>
		FeatureDvdCPRM = 0x010B,
		/// <summary/>
		FeatureFirmwareDate = 0x010C,
		/// <summary/>
		FeatureAACS = 0x010D,
		/// <summary/>
		FeatureVCPS = 0x0110,
	}

	/// <summary>The FEATURE_DATA_PROFILE_LIST_EX structure contains information corresponding to a profile list element in a profile list descriptor.</summary>
	// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddmmc/ns-ntddmmc-_feature_data_profile_list_ex
	// typedef struct _FEATURE_DATA_PROFILE_LIST_EX { UCHAR ProfileNumber[2]; UCHAR Current : 1; UCHAR Reserved1 : 7; UCHAR Reserved2; } FEATURE_DATA_PROFILE_LIST_EX, *PFEATURE_DATA_PROFILE_LIST_EX;
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._FEATURE_DATA_PROFILE_LIST_EX")]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct FEATURE_DATA_PROFILE_LIST_EX
	{
		private ushort profileNumber;
		/// <summary>Contains the profile number. This number must be one of the values defined by the FEATURE_PROFILE_TYPE enumeration. <c>ProfileNumber</c>[0] must contain the most significant byte of the profile number. <c>ProfileNumber</c>[1] must contain the least significant byte.</summary>
		public FEATURE_PROFILE_TYPE ProfileNumber { get => (FEATURE_PROFILE_TYPE)BinaryPrimitives.ReverseEndianness(profileNumber); set => profileNumber = BinaryPrimitives.ReverseEndianness((ushort)value); }

		private byte flags;

		/// <summary>Indicates, when set to 1, that this feature is currently active and the feature data is valid. When set to zero, this bit indicates that the feature is not currently active and that the feature data might not be valid.</summary>
		public bool Current { get => BitHelper.GetBit(flags, 0); set => BitHelper.SetBit(ref flags, 0, value); }

		/// <summary>Reserved.</summary>
		public byte Reserved2;
	}

	/// <summary>The FEATURE_DATA_PROFILE_LIST structure contains the data for a profile list descriptor.</summary>
	/// <remarks>This structure holds data for the feature named "Profile List" by the MMC-3 specification. This feature provides a list of all profiles supported by the device.</remarks>
	// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddmmc/ns-ntddmmc-_feature_data_profile_list
	// typedef struct _FEATURE_DATA_PROFILE_LIST { FEATURE_HEADER Header; FEATURE_DATA_PROFILE_LIST_EX Profiles[0]; } FEATURE_DATA_PROFILE_LIST, *PFEATURE_DATA_PROFILE_LIST;
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._FEATURE_DATA_PROFILE_LIST")]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct FEATURE_DATA_PROFILE_LIST
	{
		/// <summary>
		/// Contains a header that indicates how many profiles are reported in the profile list descriptor. The FEATURE_HEADER structure is
		/// used to describe both feature and profile list descriptors. When FEATURE_HEADER is used with a profile list descriptor the
		/// <c>FeatureCode</c> member of FEATURE_HEADER must be set to zero, the <c>Current</c> member must be set to 1, the <c>Version</c>
		/// member must be set to zero, and the <c>Persistent</c> member must be set to 1. The <c>Persistent</c> member is set to 1, because
		/// all devices that are compliant with the SCSI Multimedia - 4 (MMC-4) standard must support reporting of the profile list. The
		/// <c>AdditionalLength</c> member must be set to ((number of profile descriptors) * 4). See the MMC-3 specification For more
		/// information about the values assigned to these members.
		/// </summary>
		public FEATURE_HEADER Header;
		/// <summary>Contains a variable length array of FEATURE_DATA_PROFILE_LIST_EX structures that describe all the profiles supported by the device.</summary>
		//public FEATURE_DATA_PROFILE_LIST_EX Profiles[0];
	}

	/// <summary>The DVD_LAYER_DESCRIPTOR structure is used in conjunction with the IOCTL_DVD_READ_STRUCTURE request to retrieve a DVD layer descriptor.</summary>
	/// <remarks>For more information, see the SCSI Multimedia Commands - 3 (MMC-3) specification.</remarks>
// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddcdvd/ns-ntddcdvd-_dvd_layer_descriptor
// typedef struct _DVD_LAYER_DESCRIPTOR { UCHAR BookVersion : 4; UCHAR BookType : 4; UCHAR MinimumRate : 4; UCHAR DiskSize : 4; UCHAR LayerType : 4; UCHAR TrackPath : 1; UCHAR NumberOfLayers : 2;
// UCHAR Reserved1 : 1; UCHAR TrackDensity : 4; UCHAR LinearDensity : 4; ULONG StartingDataSector; ULONG EndDataSector; ULONG EndLayerZeroSector; UCHAR Reserved5 : 7; UCHAR BCAFlag : 1; } DVD_LAYER_DESCRIPTOR, *PDVD_LAYER_DESCRIPTOR;
[PInvokeData("ntddcdvd.h", MSDNShortId = "NS:ntddcdvd._DVD_LAYER_DESCRIPTOR")]
	[StructLayout(LayoutKind.Sequential, Size = 17)]
	public struct DVD_LAYER_DESCRIPTOR
	{
		private uint bits;
		/// <summary>Specifies the version of the specified book that this media complies with.</summary>
		public byte BookVersion { get => (byte)BitHelper.GetBits(bits, 0, 4); set => BitHelper.SetBits(ref bits, 0, 4, value); }
		/// <summary>
		///   <para>Specifies the DVD book this media complies with. This member can have one of the following values:</para>
		///   <list type="table">
		///     <listheader>
		///       <term>Value</term>
		///       <term>Meaning</term>
		///     </listheader>
		///     <item>
		///       <term>0</term>
		///       <term>DVD-ROM</term>
		///     </item>
		///     <item>
		///       <term>1</term>
		///       <term>DVD-RAM</term>
		///     </item>
		///     <item>
		///       <term>2</term>
		///       <term>DVD-R</term>
		///     </item>
		///     <item>
		///       <term>3</term>
		///       <term>DVD-RW</term>
		///     </item>
		///     <item>
		///       <term>9</term>
		///       <term>DVD+RW</term>
		///     </item>
		///   </list>
		/// </summary>
		public byte BookType { get => (byte)BitHelper.GetBits(bits, 4, 4); set => BitHelper.SetBits(ref bits, 4, 4, value); }
		/// <summary>
		///   <para>Specifies the read rate to use for the media. This member can have one of the following values:</para>
		///   <list type="table">
		///     <listheader>
		///       <term>Value</term>
		///       <term>Meaning</term>
		///     </listheader>
		///     <item>
		///       <term>0</term>
		///       <term>DVD-ROM</term>
		///     </item>
		///     <item>
		///       <term>1</term>
		///       <term>DVD-RAM</term>
		///     </item>
		///     <item>
		///       <term>2</term>
		///       <term>DVD-R</term>
		///     </item>
		///     <item>
		///       <term>3</term>
		///       <term>DVD-RW</term>
		///     </item>
		///     <item>
		///       <term>9</term>
		///       <term>DVD+RW</term>
		///     </item>
		///   </list>
		/// </summary>
		public byte MinimumRate { get => (byte)BitHelper.GetBits(bits, 8, 4); set => BitHelper.SetBits(ref bits, 8, 4, value); }
		/// <summary>Specifies the physical size of the media. A value of zero indicates 120 mm. A value of 1 indicates a size of 80 mm.</summary>
		public byte DiskSize { get => (byte)BitHelper.GetBits(bits, 12, 4); set => BitHelper.SetBits(ref bits, 12, 4, value); }
		/// <summary>
		///   <para>Indicates the type of layer. This member can have one of the following values:</para>
		///   <list type="table">
		///     <listheader>
		///       <term>Value</term>
		///       <term>Meaning</term>
		///     </listheader>
		///     <item>
		///       <term>1</term>
		///       <term>Read-only layer</term>
		///     </item>
		///     <item>
		///       <term>2</term>
		///       <term>Recordable layer</term>
		///     </item>
		///     <item>
		///       <term>4</term>
		///       <term>Rewritable layer</term>
		///     </item>
		///   </list>
		/// </summary>
		public byte LayerType { get => (byte)BitHelper.GetBits(bits, 16, 4); set => BitHelper.SetBits(ref bits, 16, 4, value); }
		/// <summary>Specifies the direction of the layers when more than one layer is used. If the <c>TrackPath</c> member is zero, this media uses a parallel track path (PTP). With PTP, each layer is independent and has its own lead-in and lead-out areas. If <c>TrackPath</c> is 1, the media uses opposite track path (OTP). With opposite track path, the two layers are united, and there is only one lead-in and lead-out area. For further details, see the SCSI Multimedia Commands - 3 (MMC-3) specification.</summary>
		public bool TrackPath { get => BitHelper.GetBit(bits, 20); set => BitHelper.SetBit(ref bits, 20, value); }
		/// <summary>Specifies the number of layers present on the side of the media being read. A value of zero indicates that the media has one layer. A value of 1 indicates that the media has two layers.</summary>
		public byte NumberOfLayers { get => (byte)BitHelper.GetBits(bits, 21, 2); set => BitHelper.SetBits(ref bits, 21, 2, value); }
		/// <summary>
		///   <para>Indicates the track width used for this media in units of micrometers per track. This member can have one of the following values:</para>
		///   <list type="table">
		///     <listheader>
		///       <term>Value</term>
		///       <term>Meaning</term>
		///     </listheader>
		///     <item>
		///       <term>0</term>
		///       <term>0.74 m/track</term>
		///     </item>
		///     <item>
		///       <term>1</term>
		///       <term>0.80 m/track</term>
		///     </item>
		///     <item>
		///       <term>2</term>
		///       <term>0.615 m/track</term>
		///     </item>
		///   </list>
		/// </summary>
		public byte TrackDensity { get => (byte)BitHelper.GetBits(bits, 24, 4); set => BitHelper.SetBits(ref bits, 24, 4, value); }
		/// <summary>
		///   <para>Indicates the minimum/maximum pit length used for this layer in units of micrometers per bit. This member can have one of the following values:</para>
		///   <list type="table">
		///     <listheader>
		///       <term>Value</term>
		///       <term>Meaning</term>
		///     </listheader>
		///     <item>
		///       <term>0</term>
		///       <term>0.267 m/bit</term>
		///     </item>
		///     <item>
		///       <term>1</term>
		///       <term>0.293 m/bit</term>
		///     </item>
		///     <item>
		///       <term>2</term>
		///       <term>0.409 to 0.435 m/bit</term>
		///     </item>
		///     <item>
		///       <term>4</term>
		///       <term>0.280 to 0.291 m/bit</term>
		///     </item>
		///     <item>
		///       <term>8</term>
		///       <term>0.353 m/bit</term>
		///     </item>
		///   </list>
		/// </summary>
		public byte LinearDensity { get => (byte)BitHelper.GetBits(bits, 28, 4); set => BitHelper.SetBits(ref bits, 28, 4, value); }
		/// <summary>
		///   <para>Specifies the first block that contains user data. This member can have one of the following values:</para>
		///   <list type="table">
		///     <listheader>
		///       <term>Value</term>
		///       <term>Meaning</term>
		///     </listheader>
		///     <item>
		///       <term>0x30000</term>
		///       <term>An initial block value of 0x30000 indicates that the media type is DVD-ROM or DVD-R/-RW</term>
		///     </item>
		///     <item>
		///       <term>0x31000</term>
		///       <term>An initial block value of 0x30000 indicates that the media type is DVD-RAM or DVD+RW</term>
		///     </item>
		///   </list>
		/// </summary>
		public uint StartingDataSector;
		/// <summary>Specifies the last sector of the user data in the last layer of the media.</summary>
		public uint EndDataSector;
		/// <summary>Specifies the last sector of the user data in layer zero. If this media does not use the opposite track path method and contains multiple layers, this value is set to zero.</summary>
		public uint EndLayerZeroSector;
		private byte flags;
		/// <summary>Indicates, if set to 1, the presence of data in the burst cutting area (BCA). If set to zero, it indicates that there is no BCA data.</summary>
		public bool BCAFlag { get => BitHelper.GetBit(flags, 7); set => BitHelper.SetBit(ref flags, 7, value); }
	}

	/*

	/// <summary>The FEATURE_DATA_CORE structure holds data for the Core feature descriptor.</summary>
	/// <remarks>Indicates the feature named "Core" by the MMC-3 specification. This feature encompasses the basic functionality which is mandatory for all devices that support the MMC-3 standard. See the MMC-3 specification for a description of the capabilities included in the Core feature.</remarks>
	// https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddmmc/ns-ntddmmc-_feature_data_core
	// typedef struct _FEATURE_DATA_CORE { FEATURE_HEADER Header; UCHAR PhysicalInterface[4]; UCHAR DeviceBusyEvent : 1; UCHAR INQUIRY2 : 1; UCHAR Reserved1 : 6; UCHAR Reserved2[3]; } FEATURE_DATA_CORE, *PFEATURE_DATA_CORE;
	[PInvokeData("ntddmmc.h", MSDNShortId = "NS:ntddmmc._FEATURE_DATA_CORE")]
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct FEATURE_DATA_CORE
	{
		/// <summary>Contains a FEATURE_HEADER structure with header information for this feature descriptor.</summary>
		public FEATURE_HEADER Header;
		/// <summary>Must be set to the current communication path between initiator and device, as defined in the SCSI Multimedia - 4 (MMC-4) specification. The bytes of this array are arranged in big-endian order. <c>PhysicalInterface</c>[0] contains the most significant byte, and <c>PhysicalInterface</c>[3] contains the least significant byte.</summary>
		public byte PhysicalInterface[4];
		/// <summary />
		public byte DeviceBusyEvent : 1;
		/// <summary />
		public byte INQUIRY2 : 1;
		/// <summary />
		public byte Reserved1 : 6;
		/// <summary />
		public byte Reserved2[3];
	}

	// 0x0002 - FeatureMorphing
	public struct FEATURE_DATA_MORPHING
	{
	FEATURE_HEADER Header;
	byte Asynchronous : 1;
	byte OCEvent : 1;
	byte Reserved01 : 6;
	byte Reserved2[3];
	}
	FEATURE_DATA_MORPHING, * PFEATURE_DATA_MORPHING;

	// 0x0003 - FeatureRemovableMedium
	public struct FEATURE_DATA_REMOVABLE_MEDIUM
	{
	FEATURE_HEADER Header;
	byte Lockable : 1;
	byte DBML : 1; // MMC 6 rev 2g
	byte DefaultToPrevent : 1;
	byte Eject : 1;
	byte Load : 1; // MMC 6 rev 2
	byte LoadingMechanism : 3;
	byte Reserved3[3];
	}
	FEATURE_DATA_REMOVABLE_MEDIUM, * PFEATURE_DATA_REMOVABLE_MEDIUM;

	// 0x0004 - FeatureWriteProtect
	public struct FEATURE_DATA_WRITE_PROTECT
	{
	FEATURE_HEADER Header;
	byte SupportsSWPPBit : 1;
	byte SupportsPersistentWriteProtect : 1;
	byte WriteInhibitDCB : 1;
	byte DiscWriteProtectPAC : 1;
	byte Reserved01 : 4;
	byte Reserved2[3];
	}
	FEATURE_DATA_WRITE_PROTECT, * PFEATURE_DATA_WRITE_PROTECT;

	// 0x0005 - 0x000f are Reserved

	// 0x0010 - FeatureRandomReadable
	public struct FEATURE_DATA_RANDOM_READABLE
	{
	FEATURE_HEADER Header;
	byte LogicalBlockSize[4];
	byte Blocking[2];
	byte ErrorRecoveryPagePresent : 1;
	byte Reserved1 : 7;
	byte Reserved2;
	}
	FEATURE_DATA_RANDOM_READABLE, * PFEATURE_DATA_RANDOM_READABLE;

	// 0x0011 - 0x001c are Reserved

	// 0x001D - FeatureMultiRead
	public struct FEATURE_DATA_MULTI_READ
	{
	FEATURE_HEADER Header;
	}
	FEATURE_DATA_MULTI_READ, * PFEATURE_DATA_MULTI_READ;

	// 0x001E - FeatureCdRead
	public struct FEATURE_DATA_CD_READ
	{
	FEATURE_HEADER Header;
	byte CDText : 1;
	byte C2ErrorData : 1;
	byte Reserved01 : 5;
	byte DigitalAudioPlay : 1;
	byte Reserved2[3];
	}
	FEATURE_DATA_CD_READ, * PFEATURE_DATA_CD_READ;

	// 0x001F - FeatureDvdRead
	public struct FEATURE_DATA_DVD_READ
	{
	FEATURE_HEADER Header;
	byte Multi110 : 1;
	byte Reserved1 : 7;
	byte Reserved2;
	byte DualDashR : 1;
	byte Reserved3 : 7;
	byte Reserved4;
	}
	FEATURE_DATA_DVD_READ, * PFEATURE_DATA_DVD_READ;

	// 0x0020 - FeatureRandomWritable
	public struct FEATURE_DATA_RANDOM_WRITABLE
	{
	FEATURE_HEADER Header;
	byte LastLBA[4];
	byte LogicalBlockSize[4];
	byte Blocking[2];
	byte ErrorRecoveryPagePresent : 1;
	byte Reserved1 : 7;
	byte Reserved2;
	}
	FEATURE_DATA_RANDOM_WRITABLE, * PFEATURE_DATA_RANDOM_WRITABLE;

	// 0x0021 - FeatureIncrementalStreamingWritable
	public struct FEATURE_DATA_INCREMENTAL_STREAMING_WRITABLE
	{
	FEATURE_HEADER Header;
	byte DataTypeSupported[2]; // [0] == MSB, [1] == LSB // see also FeatureCdTrackAtOnce
	byte BufferUnderrunFree : 1;
	byte AddressModeReservation : 1;
	byte TrackRessourceInformation : 1;
	byte Reserved01 : 5;
	byte NumberOfLinkSizes;
	#if !defined(_midl)
	byte LinkSize[0];
	#endif
	}
	FEATURE_DATA_INCREMENTAL_STREAMING_WRITABLE, * PFEATURE_DATA_INCREMENTAL_STREAMING_WRITABLE;

	// 0x0022 - FeatureSectorErasable
	public struct FEATURE_DATA_SECTOR_ERASABLE
	{
	FEATURE_HEADER Header;
	}
	FEATURE_DATA_SECTOR_ERASABLE, * PFEATURE_DATA_SECTOR_ERASABLE;

	// 0x0023 - FeatureFormattable
	public struct FEATURE_DATA_FORMATTABLE
	{
	FEATURE_HEADER Header;
	byte FullCertification : 1;
	byte QuickCertification : 1;
	byte SpareAreaExpansion : 1;
	byte RENoSpareAllocated : 1;
	byte Reserved1 : 4;
	byte Reserved2[3];
	byte RRandomWritable : 1;
	byte Reserved3 : 7;
	byte Reserved4[3];
	}
	FEATURE_DATA_FORMATTABLE, * PFEATURE_DATA_FORMATTABLE;

	// 0x0024 - FeatureDefectManagement
	public struct FEATURE_DATA_DEFECT_MANAGEMENT
	{
	FEATURE_HEADER Header;
	byte Reserved1 : 7;
	byte SupplimentalSpareArea : 1;
	byte Reserved2[3];
	}
	FEATURE_DATA_DEFECT_MANAGEMENT, * PFEATURE_DATA_DEFECT_MANAGEMENT;

	// 0x0025 - FeatureWriteOnce
	public struct FEATURE_DATA_WRITE_ONCE
	{
	FEATURE_HEADER Header;
	byte LogicalBlockSize[4];
	byte Blocking[2];
	byte ErrorRecoveryPagePresent : 1;
	byte Reserved1 : 7;
	byte Reserved2;
	}
	FEATURE_DATA_WRITE_ONCE, * PFEATURE_DATA_WRITE_ONCE;

	// 0x0026 - FeatureRestrictedOverwrite
	public struct FEATURE_DATA_RESTRICTED_OVERWRITE
	{
	FEATURE_HEADER Header;
	}
	FEATURE_DATA_RESTRICTED_OVERWRITE, * PFEATURE_DATA_RESTRICTED_OVERWRITE;

	// 0x0027 - FeatureCdrwCAVWrite
	public struct FEATURE_DATA_CDRW_CAV_WRITE
	{
	FEATURE_HEADER Header;
	byte Reserved1[4];
	}
	FEATURE_DATA_CDRW_CAV_WRITE, * PFEATURE_DATA_CDRW_CAV_WRITE;

	// 0x0028 - FeatureMrw
	public struct FEATURE_DATA_MRW
	{
	FEATURE_HEADER Header;
	byte Write : 1; // Cd Write
	byte DvdPlusRead : 1;
	byte DvdPlusWrite : 1;
	byte Reserved01 : 5;
	byte Reserved2[3];
	}
	FEATURE_DATA_MRW, * PFEATURE_DATA_MRW;

	// 0x0029 - Enhanced Defect Reporting
	public struct FEATURE_ENHANCED_DEFECT_REPORTING
	{
	FEATURE_HEADER Header;
	byte DRTDMSupported : 1;
	byte Reserved0 : 7;
	byte NumberOfDBICacheZones;
	byte NumberOfEntries[2];
	}
	FEATURE_ENHANCED_DEFECT_REPORTING, * PFEATURE_ENHANCED_DEFECT_REPORTING;

	// 0x002A - FeatureDvdPlusRW
	public struct FEATURE_DATA_DVD_PLUS_RW
	{
	FEATURE_HEADER Header;
	byte Write : 1;
	byte Reserved1 : 7;
	byte CloseOnly : 1;
	byte QuickStart : 1;
	byte Reserved02 : 6;
	byte Reserved03[2];
	}
	FEATURE_DATA_DVD_PLUS_RW, * PFEATURE_DATA_DVD_PLUS_RW;

	// 0x002B - FeatureDvdPlusR
	public struct FEATURE_DATA_DVD_PLUS_R
	{
	FEATURE_HEADER Header;
	byte Write : 1;
	byte Reserved1 : 7;
	byte Reserved2[3];
	}
	FEATURE_DATA_DVD_PLUS_R, * PFEATURE_DATA_DVD_PLUS_R;

	// 0x002C - FeatureDvdRwRestrictedOverwrite
	public struct FEATURE_DATA_DVD_RW_RESTRICTED_OVERWRITE
	{
	FEATURE_HEADER Header;
	byte Blank : 1;
	byte Intermediate : 1;
	byte DefectStatusDataRead : 1;
	byte DefectStatusDataGenerate : 1;
	byte Reserved0 : 4;
	byte Reserved1[3];
	}
	FEATURE_DATA_DVD_RW_RESTRICTED_OVERWRITE, * PFEATURE_DATA_DVD_RW_RESTRICTED_OVERWRITE;

	// 0x002D - FeatureCdTrackAtOnce
	public struct FEATURE_DATA_CD_TRACK_AT_ONCE
	{
	FEATURE_HEADER Header;
	byte RWSubchannelsRecordable : 1;
	byte CdRewritable : 1;
	byte TestWriteOk : 1;
	byte RWSubchannelPackedOk : 1; // MMC 3 +
	byte RWSubchannelRawOk : 1; // MMC 3 +
	byte Reserved1 : 1;
	byte BufferUnderrunFree : 1; // MMC 3 +
	byte Reserved3 : 1;
	byte Reserved2;
	byte DataTypeSupported[2]; // [0] == MSB, [1] == LSB // see also FeatureIncrementalStreamingWritable
	}
	FEATURE_DATA_CD_TRACK_AT_ONCE, * PFEATURE_DATA_CD_TRACK_AT_ONCE;

	// 0x002E - FeatureCdMastering
	public struct FEATURE_DATA_CD_MASTERING
	{
	FEATURE_HEADER Header;
	byte RWSubchannelsRecordable : 1;
	byte CdRewritable : 1;
	byte TestWriteOk : 1;
	byte RawRecordingOk : 1;
	byte RawMultiSessionOk : 1;
	byte SessionAtOnceOk : 1;
	byte BufferUnderrunFree : 1;
	byte Reserved1 : 1;
	byte MaximumCueSheetLength[3]; // [0] == MSB, [2] == LSB
	}
	FEATURE_DATA_CD_MASTERING, * PFEATURE_DATA_CD_MASTERING;

	// 0x002F - FeatureDvdRecordableWrite
	public struct FEATURE_DATA_DVD_RECORDABLE_WRITE
	{
	FEATURE_HEADER Header;
	byte Reserved1 : 1;
	byte DVD_RW : 1;
	byte TestWrite : 1;
	byte RDualLayer : 1;
	byte Reserved02 : 2;
	byte BufferUnderrunFree : 1;
	byte Reserved3 : 1;
	byte Reserved4[3];
	}
	FEATURE_DATA_DVD_RECORDABLE_WRITE, * PFEATURE_DATA_DVD_RECORDABLE_WRITE;

	// 0x0030 - FeatureDDCDRead
	public struct FEATURE_DATA_DDCD_READ
	{
	FEATURE_HEADER Header;
	}
	FEATURE_DATA_DDCD_READ, * PFEATURE_DATA_DDCD_READ;

	// 0x0031 - FeatureDDCDRWrite (obsolete)
	public struct FEATURE_DATA_DDCD_R_WRITE
	{
	FEATURE_HEADER Header;
	byte Reserved1 : 2;
	byte TestWrite : 1;
	byte Reserved2 : 5;
	byte Reserved3[3];
	}
	FEATURE_DATA_DDCD_R_WRITE, * PFEATURE_DATA_DDCD_R_WRITE;

	// 0x0032 - FeatureDDCDRWWrite (obsolete)
	public struct FEATURE_DATA_DDCD_RW_WRITE
	{
	FEATURE_HEADER Header;
	byte Blank : 1;
	byte Intermediate : 1;
	byte Reserved1 : 6;
	byte Reserved2[3];
	}
	FEATURE_DATA_DDCD_RW_WRITE, * PFEATURE_DATA_DDCD_RW_WRITE;

	// 0x0033 - FeatureLayerJumpRecording
	public struct FEATURE_DATA_LAYER_JUMP_RECORDING
	{
	FEATURE_HEADER Header;
	byte Reserved0[3];
	byte NumberOfLinkSizes;
	#if !defined(_midl)
	byte LinkSizes[0];
	#endif
	}
	FEATURE_DATA_LAYER_JUMP_RECORDING, * PFEATURE_DATA_LAYER_JUMP_RECORDING;

	// 0x0034 - 0x0036 are Reserved

	// 0x0037 - FeatureCDRWMediaWriteSupport
	public struct FEATURE_CD_RW_MEDIA_WRITE_SUPPORT
	{
	FEATURE_HEADER Header;
	byte Reserved1;
	struct{
	byte Subtype0 :1;
	byte Subtype1 :1;
	byte Subtype2 :1;
	byte Subtype3 :1;
	byte Subtype4 :1;
	byte Subtype5 :1;
	byte Subtype6 :1;
	byte Subtype7 :1;
	}
	CDRWMediaSubtypeSupport;
	byte Reserved2[2];
	}
	FEATURE_CD_RW_MEDIA_WRITE_SUPPORT, *PFEATURE_CD_RW_MEDIA_WRITE_SUPPORT;

	// 0x0038 - FeatureBDRPseudoOverwrite
	public struct FEATURE_BD_R_PSEUDO_OVERWRITE
	{
	FEATURE_HEADER Header;
	byte Reserved[4];
	}
	FEATURE_BD_R_PSEUDO_OVERWRITE, *PFEATURE_BD_R_PSEUDO_OVERWRITE;

	// 0x0039 is Reserved

	// 0x003A - FeatureDvdPlusRWDualLayer
	public struct FEATURE_DATA_DVD_PLUS_RW_DUAL_LAYER
	{
	FEATURE_HEADER Header;
	byte Write : 1;
	byte Reserved1 : 7;
	byte CloseOnly : 1;
	byte QuickStart : 1;
	byte Reserved2 : 6;
	byte Reserved3[2];
	}
	FEATURE_DATA_DVD_PLUS_RW_DUAL_LAYER, *PFEATURE_DATA_DVD_PLUS_RW_DUAL_LAYER;

	// 0x003B - FeatureDvdPlusRDualLayer
	public struct FEATURE_DATA_DVD_PLUS_R_DUAL_LAYER
	{
	FEATURE_HEADER Header;
	byte Write : 1;
	byte Reserved1 : 7;
	byte Reserved2[3];
	}
	FEATURE_DATA_DVD_PLUS_R_DUAL_LAYER, *PFEATURE_DATA_DVD_PLUS_R_DUAL_LAYER;

	// 0x003C - 0x0039 are Reserved

	// 0x0040 - FeatureBDRead

	public struct BD_CLASS_SUPPORT_BITMAP
	{
	byte Version8 :1;
	byte Version9 :1;
	byte Version10 :1;
	byte Version11 :1;
	byte Version12 :1;
	byte Version13 :1;
	byte Version14 :1;
	byte Version15 :1;
	byte Version0 :1;
	byte Version1 :1;
	byte Version2 :1;
	byte Version3 :1;
	byte Version4 :1;
	byte Version5 :1;
	byte Version6 :1;
	byte Version7 :1;
	}
	BD_CLASS_SUPPORT_BITMAP, *PBD_CLASS_SUPPORT_BITMAP;

	public struct FEATURE_BD_READ
	{
	FEATURE_HEADER Header;
	byte Reserved[4];
	BD_CLASS_SUPPORT_BITMAP Class0BitmapBDREReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class1BitmapBDREReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class2BitmapBDREReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class3BitmapBDREReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class0BitmapBDRReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class1BitmapBDRReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class2BitmapBDRReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class3BitmapBDRReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class0BitmapBDROMReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class1BitmapBDROMReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class2BitmapBDROMReadSupport;
	BD_CLASS_SUPPORT_BITMAP Class3BitmapBDROMReadSupport;
	}
	FEATURE_BD_READ, *PFEATURE_BD_READ;

	// 0x0041 - FeatureBDWrite
	public struct FEATURE_BD_WRITE
	{
	FEATURE_HEADER Header;
	byte SupportsVerifyNotRequired :1;
	byte Reserved1 :7;
	byte Reserved2[3];
	BD_CLASS_SUPPORT_BITMAP Class0BitmapBDREWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class1BitmapBDREWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class2BitmapBDREWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class3BitmapBDREWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class0BitmapBDRWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class1BitmapBDRWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class2BitmapBDRWriteSupport;
	BD_CLASS_SUPPORT_BITMAP Class3BitmapBDRWriteSupport;
	}
	FEATURE_BD_WRITE, *PFEATURE_BD_WRITE;

	// 0x0042 - FeatureTSR
	public struct FEATURE_TSR
	{
	FEATURE_HEADER Header;
	}
	FEATURE_TSR, *PFEATURE_TSR;

	// 0x0043 - 0x004F are Reserved

	// 0x0050 - FeatureHDDVDRead
	public struct FEATURE_DATA_HDDVD_READ
	{
	FEATURE_HEADER Header;
	byte Recordable : 1;
	byte Reserved0 : 7;
	byte Reserved1;
	byte Rewritable : 1; // Stands for HD DVD-RAM
	byte Reserved2 : 7;
	byte Reserved3;
	}
	FEATURE_DATA_HDDVD_READ, *PFEATURE_DATA_HDDVD_READ;

	// 0x0051 - FeatureHDDVDWrite
	public struct FEATURE_DATA_HDDVD_WRITE
	{
	FEATURE_HEADER Header;
	byte Recordable : 1;
	byte Reserved0 : 7;
	byte Reserved1;
	byte Rewritable : 1; // Stands for HD DVD-RAM
	byte Reserved2 : 7;
	byte Reserved3;
	}
	FEATURE_DATA_HDDVD_WRITE, *PFEATURE_DATA_HDDVD_WRITE;

	// 0x0052 - 0x007F are Reserved

	// 0x0080 - FeatureHybridDisc
	public struct FEATURE_HYBRID_DISC
	{
	FEATURE_HEADER Header;
	byte ResetImmunity : 1;
	byte Reserved1 : 7;
	byte Reserved2[3];
	}
	FEATURE_HYBRID_DISC, *PFEATURE_HYBRID_DISC;

	// 0x0081 - 0x00FF are Reserved

	// 0x0100 - FeaturePowerManagement
	public struct FEATURE_DATA_POWER_MANAGEMENT
	{
	FEATURE_HEADER Header;
	}
	FEATURE_DATA_POWER_MANAGEMENT, *PFEATURE_DATA_POWER_MANAGEMENT;

	// 0x0101 - FeatureSMART (not in MMC 2)
	public struct FEATURE_DATA_SMART
	{
	FEATURE_HEADER Header;
	byte FaultFailureReportingPagePresent : 1;
	byte Reserved1 : 7;
	byte Reserved02[3];
	}
	FEATURE_DATA_SMART, *PFEATURE_DATA_SMART;

	// 0x0102 - FeatureEmbeddedChanger
	public struct FEATURE_DATA_EMBEDDED_CHANGER
	{
	FEATURE_HEADER Header;
	byte Reserved1 : 2;
	byte SupportsDiscPresent : 1;
	byte Reserved2 : 1;
	byte SideChangeCapable : 1;
	byte Reserved3 : 3;
	byte Reserved4[2];
	byte HighestSlotNumber : 5;
	byte Reserved : 3;
	}
	FEATURE_DATA_EMBEDDED_CHANGER, *PFEATURE_DATA_EMBEDDED_CHANGER;

	// 0x0103 - FeatureCDAudioAnalogPlay (obsolete)
	public struct FEATURE_DATA_CD_AUDIO_ANALOG_PLAY
	{
	FEATURE_HEADER Header;
	byte SeperateVolume : 1;
	byte SeperateChannelMute : 1;
	byte ScanSupported : 1;
	byte Reserved1 : 5;
	byte Reserved2;
	byte NumerOfVolumeLevels[2]; // [0] == MSB, [1] == LSB
	}
	FEATURE_DATA_CD_AUDIO_ANALOG_PLAY, *PFEATURE_DATA_CD_AUDIO_ANALOG_PLAY;

	// 0x0104 - FeatureMicrocodeUpgrade
	public struct FEATURE_DATA_MICROCODE_UPDATE
	{
	FEATURE_HEADER Header;
	byte M5 : 1;
	byte Reserved1 : 7;
	byte Reserved2[3];
	}
	FEATURE_DATA_MICROCODE_UPDATE, *PFEATURE_DATA_MICROCODE_UPDATE;

	// 0x0105 - FeatureTimeout
	public struct FEATURE_DATA_TIMEOUT
	{
	FEATURE_HEADER Header;
	byte Group3 : 1;
	byte Reserved1 : 7;
	byte Reserved2;
	byte UnitLength[2];
	}
	FEATURE_DATA_TIMEOUT, *PFEATURE_DATA_TIMEOUT;

	// 0x0106 - FeatureDvdCSS
	public struct FEATURE_DATA_DVD_CSS
	{
	FEATURE_HEADER Header;
	byte Reserved1[3];
	byte CssVersion;
	}
	FEATURE_DATA_DVD_CSS, *PFEATURE_DATA_DVD_CSS;

	// 0x0107 - FeatureRealTimeStreaming
	public struct FEATURE_DATA_REAL_TIME_STREAMING
	{
	FEATURE_HEADER Header;
	byte StreamRecording : 1;
	byte WriteSpeedInGetPerf : 1;
	byte WriteSpeedInMP2A : 1;
	byte SetCDSpeed : 1;
	byte ReadBufferCapacityBlock : 1;
	byte Reserved1 : 3;
	byte Reserved2[3];
	}
	FEATURE_DATA_REAL_TIME_STREAMING, *PFEATURE_DATA_REAL_TIME_STREAMING;

	// 0x0108 - FeatureLogicalUnitSerialNumber
	public struct FEATURE_DATA_LOGICAL_UNIT_SERIAL_NUMBER
	{
	FEATURE_HEADER Header;
	#if !defined(_midl)
	byte SerialNumber[0];
	#endif
	}
	FEATURE_DATA_LOGICAL_UNIT_SERIAL_NUMBER, *PFEATURE_DATA_LOGICAL_UNIT_SERIAL_NUMBER;

	// 0x0109 - FeatureMediaSerialNumber
	public struct FEATURE_MEDIA_SERIAL_NUMBER
	{
	FEATURE_HEADER Header;
	}
	FEATURE_MEDIA_SERIAL_NUMBER, *PFEATURE_MEDIA_SERIAL_NUMBER;

	// 0x010A - FeatureDiscControlBlocks
	// an integral multiple of the EX structures are returned for page 010A
	public struct FEATURE_DATA_DISC_CONTROL_BLOCKS_EX
	{
	byte ContentDescriptor[4];
	}
	FEATURE_DATA_DISC_CONTROL_BLOCKS_EX, *PFEATURE_DATA_DISC_CONTROL_BLOCKS_EX;
	// use a zero-sized array for this....
	public struct FEATURE_DATA_DISC_CONTROL_BLOCKS
	{
	FEATURE_HEADER Header;
	#if !defined(_midl)
	FEATURE_DATA_DISC_CONTROL_BLOCKS_EX Data[0];
	#endif
	}
	FEATURE_DATA_DISC_CONTROL_BLOCKS, *PFEATURE_DATA_DISC_CONTROL_BLOCKS;

	// 0x010B - FeatureDvdCPRM
	public struct FEATURE_DATA_DVD_CPRM
	{
	FEATURE_HEADER Header;
	byte Reserved0[3];
	byte CPRMVersion;
	}
	FEATURE_DATA_DVD_CPRM, *PFEATURE_DATA_DVD_CPRM;

	// 0x010C - FeatureFirmwareDate
	public struct FEATURE_DATA_FIRMWARE_DATE
	{
	FEATURE_HEADER Header;
	byte Year[4];
	byte Month[2];
	byte Day[2];
	byte Hour[2];
	byte Minute[2];
	byte Seconds[2];
	byte Reserved[2];
	}
	FEATURE_DATA_FIRMWARE_DATE, *PFEATURE_DATA_FIRMWARE_DATE;

	// 0x010D - FeatureAACS
	public struct FEATURE_DATA_AACS
	{
	FEATURE_HEADER Header;
	byte BindingNonceGeneration : 1;
	byte Reserved0 : 7;
	byte BindingNonceBlockCount;
	byte NumberOfAGIDs : 4;
	byte Reserved1 : 4;
	byte AACSVersion;
	}
	FEATURE_DATA_AACS, *PFEATURE_DATA_AACS;

	// 0x010E - 0x010F are Reserved

	// 0x0110 - FeatureVCPS
	public struct FEATURE_VCPS
	{
	FEATURE_HEADER Header;
	byte Reserved[4];
	}
	FEATURE_VCPS, *PFEATURE_VCPS;

	// 0x0111 - 0xFEFF are Reserved
	public struct FEATURE_DATA_RESERVED
	{
	FEATURE_HEADER Header;
	#if !defined(_midl)
	byte Data[0];
	#endif
	}
	FEATURE_DATA_RESERVED, *PFEATURE_DATA_RESERVED;

	// 0xff00 - 0xffff are Vendor Specific
	public struct FEATURE_DATA_VENDOR_SPECIFIC
	{
	FEATURE_HEADER Header;
	#if !defined(_midl)
	byte VendorSpecificData[0];
	#endif
	}
	FEATURE_DATA_VENDOR_SPECIFIC, *PFEATURE_DATA_VENDOR_SPECIFIC;


	//
	// NOTE: All FEATURE_* structures may be extended. use of these structures
	// requires verification that the FeatureHeader.AdditionLength field
	// contains AT LEAST enough data to cover the data fields being accessed.
	// This is due to the design, which allows extending the size of the
	// various structures, which will result in these structures sizes
	// being changed over time.
	// A 0-element array is however not declared in the variable size
	// structures. Such array is declared on some structures to preserve
	// legacy, yet it is deprecated. To access variable size structures,
	// as they are always at the end of the fixed size structure, use a sizeof
	// of the declared fixed size structure as an offset.
	// *** Programmers beware! ***
	//

	//
	// NOTE: This is based on MMC 3, extended to MMC 5 rev 3
	// Further revisions will maintain backward compatibility
	// with the non-reserved fields listed here. If you need
	// to access a new field, please typecast to FEATURE_DATA_RESERVED
	// and access the appropriate bits there.
	//

	//
	// IOCTL_CDROM_GET_CONFIGURATION returns a FEATURE_* struct, which always
	// starts with a FEATURE_HEADER structure.
	//

	//
	// these are to be used for the request type
	//

	#define SCSI_GET_CONFIGURATION_REQUEST_TYPE_ALL 0x0
	#define SCSI_GET_CONFIGURATION_REQUEST_TYPE_CURRENT 0x1
	#define SCSI_GET_CONFIGURATION_REQUEST_TYPE_ONE 0x2


	public struct GET_CONFIGURATION_IOCTL_INPUT
	{
	FEATURE_NUMBER Feature;
	uint RequestType; // SCSI_GET_CONFIGURATION_REQUEST_TYPE_*
	IntPtr Reserved[2];
	}
	*/
}