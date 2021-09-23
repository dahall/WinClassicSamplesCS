using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.IMAPI;
using Vanara.PInvoke;

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
	}
}