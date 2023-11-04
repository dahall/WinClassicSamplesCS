using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.IMAPI;

namespace Vanara.Storage
{
	internal static class IMAPIv2Extensions
	{
		/// <summary>Burns data files to disc in a single session using files from a single directory tree.</summary>
		/// <param name="recorder">Burning Device. Must be initialized.</param>
		/// <param name="path">Directory of files to burn.</param>
		public static void BurnDirectory(this IDiscRecorder2 recorder, string path, string imageName = "IMAPI Sample")
		{
			// Define the new disc format and set the recorder
			var dataWriterImage = new IDiscFormat2Data
			{
				Recorder = recorder
			};

			if (!dataWriterImage.IsRecorderSupported(recorder))
			{
				Console.WriteLine("The recorder is not supported");
				return;
			}

			if (!dataWriterImage.IsCurrentMediaSupported(recorder))
			{
				Console.WriteLine("The current media is not supported");
				return;
			}

			dataWriterImage.ClientName = imageName;

			// Create an image stream for a specified directory.

			// Create a new file system image and retrieve the root directory
			var fsi = new IFileSystemImage
			{
				// Set the media size
				FreeMediaBlocks = dataWriterImage.FreeSectorsOnMedia,

				// Use legacy ISO 9660 Format
				FileSystemsToCreate = FsiFileSystems.FsiFileSystemUDF
			};

			// Add the directory to the disc file system
			IFsiDirectoryItem dir = fsi.Root;
			Console.WriteLine();
			Console.Write("Adding files to image:".PadRight(80));
			using (var eventDisp = new ComConnectionPoint(fsi, new DFileSystemImageEventsSink(AddTreeUpdate)))
				dir.AddTree(path, false);
			Console.WriteLine();

			// Create an image from the file system
			Console.WriteLine("\nWriting content to disc...");
			IFileSystemImageResult result = fsi.CreateResultImage();

			// Data stream sent to the burning device
			IStream stream = result.ImageStream;

			// Write the image stream to disc using the specified recorder.
			using (var eventDisp = new ComConnectionPoint(dataWriterImage, new DDiscFormat2DataEventsSink(WriteUpdate)))
				dataWriterImage.Write(stream);   // Burn the stream to disc

			Console.WriteLine("----- Finished writing content -----");

			static void AddTreeUpdate(IFileSystemImage @object, string currentFile, long copiedSectors, long totalSectors)
			{
				Console.Write(new string('\b', 80));
				Console.Write(Path.GetFileName(currentFile).PadRight(80));
			}

			static void WriteUpdate(IDiscFormat2Data @object, IDiscFormat2DataEventArgs progress)
			{
				switch (progress.CurrentAction)
				{
					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_VALIDATING_MEDIA:
						Console.Write("Validating media. ");
						break;

					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_FORMATTING_MEDIA:
						Console.Write("Formatting media. ");
						break;

					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_INITIALIZING_HARDWARE:
						Console.Write("Initializing Hardware. ");
						break;

					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_CALIBRATING_POWER:
						Console.Write("Calibrating Power (OPC). ");
						break;

					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_WRITING_DATA:
						var totalSectors = progress.SectorCount;
						var writtenSectors = progress.LastWrittenLba - progress.StartLba;
						var percentDone = writtenSectors / totalSectors;
						Console.Write("Progress: {0} - ", percentDone);
						break;

					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_FINALIZATION:
						Console.Write("Finishing the writing. ");
						break;

					case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_COMPLETED:
						Console.Write("Completed the burn. ");
						break;

					default:
						Console.Write("Error!!!! Unknown Action: 0x{0:X} ", progress.CurrentAction);
						break;
				}
				Console.WriteLine($"Time: {progress.ElapsedTime} / {progress.TotalTime} ({progress.ElapsedTime * 100 / progress.TotalTime}%)");
			}
		}

		/// <summary>Burns a boot image and data files to disc in a single session using files from a single directory tree.</summary>
		/// <param name="recorder">Burning Device. Must be initialized.</param>
		/// <param name="path">Directory of files to burn. \\winbuilds\release\winmain\latest.tst\amd64fre\en-us\skus.cmi\staged\windows</param>
		/// <param name="bootFile">Path and filename of boot image. \\winbuilds\release\winmain\latest.tst\x86fre\bin\etfsboot.com</param>
		public static void CreateBootDisc(this IDiscRecorder2 recorder, string path, string bootFile)
		{
			// -------- Adding Boot Image Code -----
			Console.WriteLine("Creating BootOptions");
			var bootOptions = new IBootOptions
			{
				Manufacturer = "Microsoft",
				PlatformId = PlatformId.PlatformX86,
				Emulation = EmulationType.EmulationNone
			};

			// Need stream for boot image file
			Console.WriteLine("Creating IStream for file {0}", bootFile);
			var iStream = new ComStream(new FileStream(bootFile, FileMode.Open, FileAccess.Read, FileShare.Read));
			bootOptions.AssignBootImage(iStream);

			// Create disc file system image (ISO9660 in this example)
			var fsi = new IFileSystemImage
			{
				FreeMediaBlocks = -1, // Enables larger-than-CD image
				FileSystemsToCreate = FsiFileSystems.FsiFileSystemISO9660 | FsiFileSystems.FsiFileSystemJoliet | FsiFileSystems.FsiFileSystemUDF,

				// Hooking bootStream to FileSystemObject
				BootImageOptions = bootOptions
			};

			// Hooking content files FileSystemObject
			fsi.Root.AddTree(path, false);

			IFileSystemImageResult result = fsi.CreateResultImage();
			IStream stream = result.ImageStream;

			// Create and write stream to disc using the specified recorder.
			var dataWriterBurn = new IDiscFormat2Data
			{
				Recorder = recorder,
				ClientName = "IMAPI Sample"
			};
			dataWriterBurn.Write(stream);

			Console.WriteLine("----- Finished writing content -----");
		}

		/// <summary>Examines and reports the media characteristics.</summary>
		/// <param name="recorder">Burning Device. Must be initialized.</param>
		public static void DisplayMediaCharacteristics(this IDiscRecorder2 recorder)
		{
			// Define the new disc format and set the recorder
			var mediaImage = new IDiscFormat2Data { Recorder = recorder };

			// *** Validation methods inherited from IMAPI2.MsftDiscFormat2
			var boolResult = mediaImage.IsRecorderSupported(recorder);
			Console.WriteLine($"--- Current recorder {(boolResult ? "IS" : "IS NOT")} supported. ---");

			boolResult = mediaImage.IsCurrentMediaSupported(recorder);
			Console.WriteLine($"--- Current media {(boolResult ? "IS" : "IS NOT")} supported. ---");

			Console.WriteLine("ClientName = {0}", mediaImage.ClientName);

			if (boolResult)
			{
				// Check a few CurrentMediaStatus possibilities. Each status is associated with a bit and some combinations are legal.
				var curMediaStatus = mediaImage.CurrentMediaStatus;
				Console.WriteLine("Current Media Status:");

				if (curMediaStatus == IMAPI_FORMAT2_DATA_MEDIA_STATE.IMAPI_FORMAT2_DATA_MEDIA_STATE_UNKNOWN)
				{
					Console.WriteLine("\tMedia state is unknown.");
				}
				else
				{
					foreach (var state in curMediaStatus.GetFlags().Where(s => s != 0))
						Console.WriteLine("\t" + state.GetDescription());
				}

				Console.WriteLine("Current Media Type\t" + mediaImage.CurrentPhysicalMediaType.GetDescription());
			}
			Console.Write("\n----- Finished -----");
		}

		/// <summary>Examines and reports the burn device characteristics such as Product ID, Revision Level, Feature Set and Profiles.</summary>
		/// <param name="recorder">Burning Device. Must be initialized.</param>
		public static void DisplayCharacteristics(this OpticalStorageDevice recorder)
		{
			//*** - Formating the way to display info on the supported recoders
			Console.WriteLine("--------------------------------------------------------------------------------");
			Console.WriteLine("ActiveRecorderId: {0}".PadLeft(22), recorder.UID);
			Console.WriteLine("Vendor Id: {0}".PadLeft(22), recorder.VendorId);
			Console.WriteLine("Product Id: {0}".PadLeft(22), recorder.ProductId);
			Console.WriteLine("Product Revision: {0}".PadLeft(22), recorder.ProductVersion);
			Console.WriteLine("VolumeName: {0}".PadLeft(22), recorder.VolumeName);
			Console.WriteLine("Can Load Media: {0}".PadLeft(22), recorder.CanLoadMedia);

			foreach (var mountPoint in recorder.VolumePathNames)
			{
				Console.WriteLine("Mount Point: {0}".PadLeft(22), mountPoint);
			}

			Console.WriteLine("SupportedMediaTypes in the device: ");
			foreach (var supportedMediaType in recorder.SupportedMediaTypes)
			{
				Console.WriteLine('\t' + supportedMediaType.GetDescription());
			}

			foreach (var supportedFeature in recorder.SupportedFeaturePages)
			{
				Console.WriteLine("Feature: {0}".PadLeft(22), supportedFeature.ToString("F"));
			}

			Console.WriteLine("Current Features");
			foreach (var currentFeature in recorder.CurrentFeaturePages)
			{
				Console.WriteLine("Feature: {0}".PadLeft(22), currentFeature.ToString("F"));
			}

			Console.WriteLine("Supported Profiles");
			foreach (var supportedProfile in recorder.SupportedProfiles)
			{
				Console.WriteLine("Profile: {0}".PadLeft(22), supportedProfile.ToString("F"));
			}

			Console.WriteLine("Current Profiles");
			foreach (var currentProfile in recorder.CurrentProfiles)
			{
				Console.WriteLine("Profile: {0}".PadLeft(22), currentProfile.ToString("F"));
			}

			Console.WriteLine("Supported Mode Pages");
			foreach (var supportedModePage in recorder.SupportedModePages)
			{
				Console.WriteLine("Mode Page: {0}".PadLeft(22), supportedModePage.ToString("F"));
			}

			Console.WriteLine("\n----- Finished content -----");
		}

		public static HRESULT GetPhysicalDvdStructure(this IDiscRecorder2Ex recorder, out DVD_LAYER_DESCRIPTOR dvdStructureInformation)
		{
			dvdStructureInformation = default;

			try
			{
				// Read the DVD structure
				recorder.ReadDvdStructure(0, 0, 0, 0, out var tmpDescriptor, out var tmpDescriptorSize);
				if (tmpDescriptorSize < Marshal.SizeOf(typeof(DVD_LAYER_DESCRIPTOR)))
					return HRESULT.E_IMAPI_RECORDER_INVALID_RESPONSE_FROM_DEVICE;

				// save the results
				dvdStructureInformation = tmpDescriptor.DangerousGetHandle().ToStructure<DVD_LAYER_DESCRIPTOR>(tmpDescriptorSize);

				return HRESULT.S_OK;
			}
			catch (Exception ex)
			{
				return ex.HResult;
			}
		}

		public static IMAPI_MEDIA_PHYSICAL_TYPE GetCurrentPhysicalMediaType(this IDiscRecorder2Ex recorder)
		{
			if (recorder is null) throw new ArgumentNullException(nameof(recorder));

			bool supportsGetConfiguration = true; // avoid legacy checks by default
			bool readDvdStructureCurrent = false;
			bool readDvdStructureSupported = false;

			recorder.GetDiscInformation(out var mem, out var memSz);
			var discInfo = mem.DangerousGetHandle().ToStructure<DISC_INFORMATION>(memSz);

			// determine if READ_DVD_STRUCTURE is a supported command
			try
			{
				recorder.GetFeaturePage(IMAPI_FEATURE_PAGE_TYPE.IMAPI_FEATURE_PAGE_TYPE_DVD_READ, false, out var dvdRead, out var dvdReadSize);

				// the feature page is supported
				readDvdStructureSupported = true;

				// check if DvdRead feature is current
				// (Data is guaranteed to be the right size by GetFeaturePage)
				readDvdStructureCurrent = dvdRead.ToStructure<FEATURE_HEADER>().Current;

				dvdRead?.Dispose();
			}
			catch (COMException tmpHr) when (tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_GET_CONFIGURATION_NOT_SUPPORTED)
			{
			}
			catch
			{
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN;
			}

			// discInfo is either initialized or mediaType has already been determined
			if ((discInfo.DiscStatus == 0x02) && // complete, non-appendable
			!discInfo.Erasable)
			{
				return readDvdStructureCurrent ? IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDROM : IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDROM;
			}

			// check for DVD+RW media
			try
			{
				recorder.GetFeaturePage(IMAPI_FEATURE_PAGE_TYPE.IMAPI_FEATURE_PAGE_TYPE_DVD_PLUS_RW, true, out var pfeature, out var featureSize);
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSRW;
			}
			catch (COMException tmpHr) when (tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_GET_CONFIGURATION_NOT_SUPPORTED ||
				tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_FEATURE_IS_NOT_CURRENT || tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_NO_SUCH_FEATURE)
			{
			}
			catch
			{
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN;
			}

			// check for DVD+R dual-layer media
			try
			{
				recorder.GetFeaturePage(IMAPI_FEATURE_PAGE_TYPE.IMAPI_FEATURE_PAGE_TYPE_DVD_PLUS_R_DUAL_LAYER, true, out var pfeature, out var featureSize);
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR_DUALLAYER;
			}
			catch (COMException tmpHr) when (tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_GET_CONFIGURATION_NOT_SUPPORTED ||
				tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_FEATURE_IS_NOT_CURRENT || tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_NO_SUCH_FEATURE)
			{
			}
			catch
			{
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN;
			}

			// check for DVD+R media
			try
			{
				recorder.GetFeaturePage(IMAPI_FEATURE_PAGE_TYPE.IMAPI_FEATURE_PAGE_TYPE_DVD_PLUS_R, true, out var pfeature, out var featureSize);
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR;
			}
			catch (COMException tmpHr) when (tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_GET_CONFIGURATION_NOT_SUPPORTED ||
				tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_FEATURE_IS_NOT_CURRENT || tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_NO_SUCH_FEATURE)
			{
			}
			catch
			{
				return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN;
			}

			// Use ReadDvdStructure (ignore errors)
			if (readDvdStructureSupported)
			{
				HRESULT tmpHr = GetPhysicalDvdStructure(recorder, out var descriptor);

				if (tmpHr.Failed)
				{
					// ignore this error, since it's possibly not even DVD media
				}
				else
				{
					switch (descriptor.BookType)
					{
						case 0x0:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDROM;
						case 0x1:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDRAM;
						case 0x2:
							return descriptor.NumberOfLayers == 0x1
								? IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHR_DUALLAYER
								: IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHR;
						case 0x3:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHRW;
						case 0x9:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSRW;
						case 0xA:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR;
						case 0xE:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR_DUALLAYER;
					}
				}
			}

			// use profiles to allow CD-R, CD-RW, randomly writable media to be detected
			try
			{
				recorder.GetSupportedProfiles(true, out var pprofiles, out var profileCount);
				IMAPI_PROFILE_TYPE[] profiles = pprofiles.ToArray<IMAPI_PROFILE_TYPE>((int)profileCount);

				// according to the specs, the features shall be listed in order
				// of drive's usage preference.
				foreach (IMAPI_PROFILE_TYPE profile in profiles)
				{
					switch (profile)
					{
						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_NON_REMOVABLE_DISK:
						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_REMOVABLE_DISK:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DISK;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_CDROM:
						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DDCDROM:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDROM;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_CD_RECORDABLE:
						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DDCD_RECORDABLE:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDR;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_CD_REWRITABLE:
						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DDCD_REWRITABLE:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDRW;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVDROM:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDROM;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVD_RAM:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDRAM;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVD_PLUS_R:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVD_PLUS_RW:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSRW;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVD_DASH_RECORDABLE:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHR;

						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVD_DASH_REWRITABLE:
						case IMAPI_PROFILE_TYPE.IMAPI_PROFILE_TYPE_DVD_DASH_RW_SEQUENTIAL:
							return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHRW;

						default:
							break;
					}
				} // end of loop through all profiles
			}
			catch (COMException tmpHr) when (tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_GET_CONFIGURATION_NOT_SUPPORTED)
			{
				supportsGetConfiguration = false;
			}
			catch (COMException tmpHr) when (tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_FEATURE_IS_NOT_CURRENT || tmpHr.HResult == HRESULT.E_IMAPI_RECORDER_NO_SUCH_FEATURE)
			{
			}
			catch
			{ }

			// For the final, last-ditch attempt for legacy drives
			if (!supportsGetConfiguration)
			{
				// NOTE: this works because -ROM media was determined earlier discInfo is either initialized or_ mediaType has already been determined
				return discInfo.Erasable ? IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDRW : IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDR;
			}

			return IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN;
		}
	}
}