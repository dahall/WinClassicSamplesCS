using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.Storage;
using static imapi2sample.ConsoleUtil;
using static Vanara.PInvoke.IMAPI;

namespace imapi2sample;

internal static class Program
{
	private const string clientName = "Imapi2Sample";

	public static int Main(string[] args)
	{
		if (!ParseCommandLine(args, out PROGRAM_OPTIONS options))
		{
			PrintHelp(Path.GetFileName(Environment.CommandLine));
			return HRESULT.E_INVALIDARG;
		}

		// Get start time for total time
		var ticker = System.Diagnostics.Stopwatch.StartNew();

		try
		{
			if (options.ListWriters)
			{
				ListAllRecorders();
			}
			if (options.Erase)
			{
				EraseMedia(options.WriterIndex, options.FullErase);
			}
			if (options.Write)
			{
				DataWriter(options);
			}
			if (options.Image)
			{
				ImageWriter(options);
			}
			if (options.Audio)
			{
				AudioWriter(options);
			}
			if (options.Raw)
			{
				RawWriter(options);
			}
			if (options.Eject)
			{
				EjectClose(options, false);
			}
			if (options.Close)
			{
				EjectClose(options, true);
			}
		}
		catch (Exception ex)
		{
			Console.Write(ex);
			return 1;
		}
		finally
		{
			ticker.Stop();
			Console.Write(" - Total Time: {0}\n", ticker.Elapsed);
		}

		return 0;
	}

	// Write wav files to CD
	private static void AudioWriter(in PROGRAM_OPTIONS options)
	{
		OpticalStorageWriteAudioOperation op = new(clientName) { Device = OpticalStorageManager.Devices[options.WriterIndex] };
		op.AudioTrackPaths.AddRange(Directory.EnumerateFiles(options.FileName, "*.wav"));
		var timer = System.Diagnostics.Stopwatch.StartNew();
		// hookup event sink
		op.WriteAudioTrackProgress += WriteTrackProgress;
		op.Execute();
		op.WriteAudioTrackProgress -= WriteTrackProgress;
		timer.Stop();
		Console.Write(" - Time to write: {0}\n", timer.Elapsed);

		Console.Write("AudioWriter succeeded for drive index {0}\n", options.WriterIndex);

		static void WriteTrackProgress(object? sender, OpticalStorageWriteAudioTrackEventArgs progress)
		{
			var currentTrack = progress.CurrentTrackNumber;
			var elapsedTime = progress.ElapsedTime;
			var remainingTime = progress.RemainingTime;
			var currentAction = progress.CurrentAction;
			var startLba = progress.StartLba;
			var sectorCount = progress.SectorCount;
			var lastReadLba = progress.LastReadLba;
			var lastWrittenLba = progress.LastWrittenLba;
			var totalSystemBuffer = progress.TotalSystemBuffer;
			var usedSystemBuffer = progress.UsedSystemBuffer;
			var freeSystemBuffer = progress.FreeSystemBuffer;

			if (currentAction == IMAPI_FORMAT2_TAO_WRITE_ACTION.IMAPI_FORMAT2_TAO_WRITE_ACTION_PREPARING)
			{
				DeleteCurrentLine();
				Console.Write("Preparing ... ");
			}
			else if (currentAction == IMAPI_FORMAT2_TAO_WRITE_ACTION.IMAPI_FORMAT2_TAO_WRITE_ACTION_FINISHING)
			{
				DeleteCurrentLine();
				Console.Write("Finishing ... ");
			}
			else if (currentAction == IMAPI_FORMAT2_TAO_WRITE_ACTION.IMAPI_FORMAT2_TAO_WRITE_ACTION_WRITING)
			{
				DeleteCurrentLine();
				Console.Write("T {0} [{1:X}..{2:X}] ", currentTrack, startLba, startLba + sectorCount);
				UpdatePercentageDisplay(lastWrittenLba - startLba, sectorCount);
			}
		}
	}

	// Function for writing a dir to disc
	private static void DataWriter(in PROGRAM_OPTIONS options)
	{
		// create a DiscRecorder object
		OpticalStorageWriteOperation op = new(clientName) { Device = OpticalStorageManager.Devices[options.WriterIndex], ForceMediaToBeClosed = options.CloseDisc };

		// Set the filesystems to use if specified
		var fileSystem = FsiFileSystems.FsiFileSystemNone;
		if (options.Iso)
			fileSystem |= FsiFileSystems.FsiFileSystemISO9660;
		if (options.Joliet)
			fileSystem |= FsiFileSystems.FsiFileSystemJoliet | FsiFileSystems.FsiFileSystemISO9660;
		if (options.UDF)
			fileSystem |= FsiFileSystems.FsiFileSystemUDF;

		// Check if media is blank
		var isBlank = op.IsMediaHeuristicallyBlank;
		if (!options.Multi && !isBlank)
		{
			Console.Write("*** WRITING TO NON-BLANK MEDIA WITHOUT IMPORT! ***\n");
		}

		// Check what file systems are being used
		OpticalStorageBootOptions? bootOp = options.BootFileName is null ? null : new(options.BootFileName);
		OpticalStorageFileSystemImage image = new(options.FileName, fileSystem, options.VolumeName, bootOp);
		Console.WriteLine("Supported file systems: " + image.FileSystemsToCreate.ToString().Replace("FsiFileSystem", ""));

		// Get count
		Console.Write("Number of Files: {0}\n", image.FileCount);
		Console.Write("Number of Directories: {0}\n", image.DirectoryCount);

		// Create the result image
		// Get the stream
		op.Data = image.GetImageStream();
		Console.Write("Image ready to write\n");

		var timer = System.Diagnostics.Stopwatch.StartNew();
		// hookup event sink
		op.WriteProgress += DiscRecorder_WriteProgress;
		op.Execute();
		op.WriteProgress -= DiscRecorder_WriteProgress;
		timer.Stop();
		Console.Write(" - Time to write: {0}\n", timer.Elapsed);

		// verify the WriteProtectStatus property gets
		_ = op.WriteProtectStatus;

		// verify that clearing the disc recorder works
		//dataWriter.Recorder = null;

		Console.Write("DataWriter succeeded for drive index {0}\n", options.WriterIndex);
	}

	private static void DiscRecorder_WriteProgress(object? sender, OpticalStorageWriteEventArgs progress)
	{
		var currentAction = progress.CurrentAction;
		var elapsedTime = progress.ElapsedTime;
		var remainingTime = progress.RemainingTime;
		var totalTime = progress.TotalTime;
		var startLba = progress.StartLba;
		var sectorCount = progress.SectorCount;
		var lastReadLba = progress.LastReadLba;
		var lastWrittenLba = progress.LastWrittenLba;
		var totalSystemBuffer = progress.TotalSystemBuffer;
		var usedSystemBuffer = progress.UsedSystemBuffer;
		var freeSystemBuffer = progress.FreeSystemBuffer;

		switch (currentAction)
		{
			case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_WRITING_DATA:
				DeleteCurrentLine();
				Console.Write("[{0:x}..{1:x}] ", startLba, startLba + sectorCount);
				UpdatePercentageDisplay(lastWrittenLba - startLba, sectorCount);
				break;
			case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_FINALIZATION:
				DeleteCurrentLine();
				OverwriteCurrentLine();
				goto default;
			case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_COMPLETED:
				Console.Write(currentAction.GetDescription());
				break;
			default:
				DeleteCurrentLine();
				Console.Write(currentAction.GetDescription());
				break;
		}
	}

	private static void EjectClose(in PROGRAM_OPTIONS options, bool close)
	{
		var device = OpticalStorageManager.Devices[options.WriterIndex];

		try
		{
			// Try and prevent shell pop-ups
			device.DisableMediaChangeNotifications();

			var canLoad = device.CanLoadMedia;
			if (canLoad && close)
				device.CloseTray();
			else if (canLoad)
				device.OpenTray();
		}
		finally
		{
			// re-enable MCN
			device.EnableMediaChangeNotifications();
		}
	}

	private static void EraseMedia(int index, bool full)
	{
		OpticalStorageEraseMediaOperation op = new(full, clientName) { Device = OpticalStorageManager.Devices[index] };

		// Set the app name for use with exclusive access THIS IS REQUIRED
		op.EraseProgress += Device_EraseProgress;
		// erase the disc
		var timer = System.Diagnostics.Stopwatch.StartNew();
		op.Execute();
		op.EraseProgress -= Device_EraseProgress;
		timer.Stop();
		Console.WriteLine();
		Console.WriteLine(" - Time to erase: {0}", timer.Elapsed);

		Console.Write("EraseMedia succeeded for drive index {0}\n", index);

		static void Device_EraseProgress(object? sender, ProgressChangedEventArgs progress)
		{
			DeleteCurrentLine();
			DeleteCurrentLine();

			// each block is 2%
			// ----=----1----=----2----=----3----=----4----=----5----=----6----=----7----=----8
			// ±.....................

			for (var i = 1; i < 100; i += 2)
			{
				if (i < progress.ProgressPercentage)
					Console.Write((char)178);
				else if (i == progress.ProgressPercentage)
					Console.Write((char)177);
				else
					Console.Write((char)176);
			}
			Console.Write($" {progress.ProgressPercentage}%");
		}

	}

	private static IStream GetIsoStreamForDataWriting(out uint sectors2, string shortStreamFilename)
	{
		sectors2 = 0;

		// open an ISO image for the stream
		ShlwApi.SHCreateStreamOnFile(shortStreamFilename, STGM.STGM_READ | STGM.STGM_SHARE_DENY_WRITE, out IStream data).ThrowIfFailed();

		// validate size and save # of blocks for use later
		data.Stat(out STATSTG stat, 1 /*STATFLAG_NONAME*/);

		// validate size and save # of blocks for use later
		if (stat.cbSize % 2048 != 0)
		{
			throw new InvalidDataException(string.Format("File is not multiple of 2048 bytes. File size is {0} ({0:X}).", stat.cbSize));
		}
		else if (stat.cbSize / 2048 > 0x7FFFFFFF)
		{
			throw new InvalidDataException(string.Format("File is too large, # of sectors won't fit a int. File size is {0} ({0:X}).", stat.cbSize));
		}
		else
		{
			sectors2 = (uint)(stat.cbSize / 2048);
		}

		return data;
	}

	private static void ImageWriter(in PROGRAM_OPTIONS options)
	{
		OpticalStorageWriteOperation op = new(clientName)
		{
			// get a stream to write to the disc
			Data = GetIsoStreamForDataWriting(out _, options.FileName),
			// create a DiscRecorder object
			Device = OpticalStorageManager.Devices[options.WriterIndex]
		};

		// Create the event sink
		op.WriteProgress += DiscRecorder_WriteProgress;
		// write the stream
		op.Execute();
		op.WriteProgress -= DiscRecorder_WriteProgress;

		Console.Write("ImageWriter succeeded for drive index {0}\n", options.WriterIndex);
	}

	private static void ListAllRecorders()
	{
		// Print each recorder's ID
		var i = 0;
		foreach (var discRecorder in OpticalStorageManager.Devices)
		{
			// Get the device strings
			Console.Write($"{i++:2}) {discRecorder.VendorId} {discRecorder.ProductId}");

			// Get the mount point
			var mountPoints = discRecorder.VolumePathNames;
			if (mountPoints.Length == 0)
				Console.Write(" (*** NO MOUNT POINTS ***)");
			else
				Console.Write(" (" + string.Join(" ", mountPoints) + ")");

			try
			{
				OpticalStorageWriteOperation op = new() { Device = discRecorder };
				// get the current media in the recorder
				Console.WriteLine($" ({op.CurrentPhysicalMediaType})");
			}
			catch
			{
				Console.WriteLine($" (No or invalid media.)");
			}
		}
	}

	private static bool ParseCommandLine(string[] Arguments, out PROGRAM_OPTIONS Options)
	{
		var goodEnough = false;
		var Count = Arguments.Length;
		// initialize with defaults
		Options = default;

		for (uint i = 0; i < Arguments.Length; i++)
		{
			if ((Arguments[i][0] == '/') || (Arguments[i][0] == '-'))
			{
				var validArgument = false;

				// If the first character of the argument is a - or a / then treat it as an option.
				switch (Arguments[i].ToLower().TrimStart('/', '-'))
				{
					case "write":
						Options.Write = true;
						validArgument = true;
						break;

					case "image":
						Options.Image = true;
						validArgument = true;
						break;

					case "audio":
						Options.Audio = true;
						validArgument = true;
						break;

					case "raw":
						Options.Raw = true;
						validArgument = true;
						break;

					case "close":
						Options.CloseDisc = true;
						validArgument = true;
						break;

					case "multi":
						Options.Multi = true;
						validArgument = true;
						break;

					case "iso":
						Options.Iso = true;
						validArgument = true;
						break;

					case "joliet":
						Options.Joliet = true;
						validArgument = true;
						break;

					case "udf":
						Options.UDF = true;
						validArgument = true;
						break;

					case "free":
						Options.FreeSpace = true;
						validArgument = true;
						break;

					case "inject":
						Options.Close = true;
						validArgument = true;
						break;

					case "eject":
						Options.Eject = true;
						validArgument = true;
						break;

					case "drive":
						// requires second argument
						if (i + 1 < Count)
						{
							i++; // advance argument index
							if (int.TryParse(Arguments[i], out var tmp))
							{
								// Let's do this zero based for now
								Options.WriterIndex = tmp;
								validArgument = true;
							}
						}

						if (!validArgument)
						{
							Console.Write("Need a second argument after drive," +
								" which is the one-based index to the\n" +
								"writer to use in decimal format. To" +
								"get a list of available drives and" +
								"their indexes, use \"-list\" option\n");
						}
						break;

					case "boot":
						// requires second argument
						if (i + 1 < Count)
						{
							i++; // advance argument index
							if (Arguments[i] != default)
							{
								// Let's do this zero based for now
								Options.BootFileName = Arguments[i];
								validArgument = true;
							}
						}

						if (!validArgument)
						{
							Console.Write("Need a second argument after boot," +
								" which is the boot file the\n" +
								"writer will use\n");
						}
						break;

					case "vol":
						// requires second argument
						if (i + 1 < Count)
						{
							i++; // advance argument index
							if (Arguments[i] != default)
							{
								// Let's do this zero based for now
								Options.VolumeName = Arguments[i];
								validArgument = true;
							}
						}

						if (!validArgument)
						{
							Console.Write("Need a second argument after vol," +
								" which is the volume name for\n" +
								"the disc\n");
						}
						break;

					case "list":
						Options.ListWriters = true;
						validArgument = true;
						break;

					case "erase":
						Options.Erase = true;
						Options.FullErase = false;
						validArgument = true;
						break;

					case "fullerase":
						Options.Erase = true;
						Options.FullErase = true;
						validArgument = true;
						break;

					case "?":
						Console.Write("Requesting help\n");
						break;

					default:
						Console.Write("Unknown option -- {0}\n", Arguments[i]);
						break;
				}

				if (!validArgument)
				{
					return false;
				}
			}
			else if (Options.FileName is null)
			{
				// The first non-flag argument is the ISO to write name.
				Options.FileName = Arguments[i];
			}
			else
			{
				// Too many non-flag arguments provided. This must be an error.
				Console.Write("Error: extra argument {0} not expected\n", Arguments[i]);
				return false;
			}
		}

		// Validate the command-line arguments.
		if (Options.ListWriters)
		{
			// listing the Writers stands alone
			if (!(Options.Write || Options.Erase))
			{
				goodEnough = true;
			}
			else
			{
				Console.Write("Error: Listing writers must be used alone\n");
			}
		}
		else if (Options.Write)
		{
			// Write allows erase, but not self-test Write requires at least a filename
			if (Options.FileName != default)
			{
				goodEnough = true;
			}
			else
			{
				Console.Write("Error: Write requires directory\n");
			}

			// validate erase options?
		}
		else if (Options.Image)
		{
			// Write allows erase, but not self-test Write requires at least a filename
			if (Options.FileName != default)
			{
				goodEnough = true;
			}
			else
			{
				Console.Write("Error: Image requires filename\n");
			}
		}
		else if (Options.Audio)
		{
			// Write allows erase, but not self-test Write requires at least a filename
			if (Options.FileName != default)
			{
				goodEnough = true;
			}
			else
			{
				Console.Write("Error: Audio requires directory\n");
			}
		}
		else if (Options.Raw)
		{
			// Write allows erase, but not self-test Write requires at least a filename
			if (Options.FileName != default)
			{
				goodEnough = true;
			}
			else
			{
				Console.Write("Error: Raw requires directory\n");
			}
		}
		else if (Options.Erase)
		{
			// validate erase options?
			goodEnough = true;
		}

		// These are not stand alone options.
		//if (Options.CloseDisc )
		//{
		// //Console.Write("Option 'DiscOpen' is not yet implemented\n");
		// goodEnough = true;
		//}
		//if (Options.Multi )
		//{
		// goodEnough = true;
		//}

		if (Options.FreeSpace)
		{
			goodEnough = true;
		}
		if (Options.Eject)
		{
			goodEnough = true;
		}
		if (Options.Close)
		{
			goodEnough = true;
		}

		if (!goodEnough)
		{
			Options = default;
			return false;
		}

		return true;
	}

	private static void PrintHelp(string selfName)
	{
		Console.Write("{0}: {1}\n" +
			"Usage:\n" +
			"{0} -list\n" +
			"{0} -write <dir> [-multi] [-close] [-drive <#>] [-boot <file>]\n" +
			"{0} -audio <dir> [-close] [-drive <#>]\n" +
			"{0} -raw <dir> [-close] [-drive <#>]\n" +
			"{0} -image <file>[-close] [-drive <#>] [-bufe | -bufd]\n" +
			"{0} -erase [-drive <#>]\n" +
			"\n" +
			"\tlist -- list the available writers and their index.\n" +
			"\terase -- quick erases the chosen recorder.\n" +
			"\tfullerase -- full erases the chosen recorder.\n" +
			"\twrite -- Writes a directory to the disc.\n" +
			"\t <dir> -- Directory to be written.\n" +
			"\t [-SAO] -- Use Cue Sheet recording.\n" +
			"\t [-close] -- Close disc (not appendable).\n" +
			"\t [-drive <#>] -- choose the given recorder index.\n" +
			"\t [-iso, -udf, -joliet] -- specify the file system to write.\n" +
			"\t [-multi] -- Add an additional session to disc.\n" +
			"\t [-boot <file>] -- Create a boot disk. File is a boot image.\n" +
			"\teject -- Eject the CD tray\n" +
			"\tinject -- Close the CD tray\n",
			selfName, Environment.OSVersion.Version);
		return;
	}

	// Write audio to disc using disc at once
	private static void RawWriter(in PROGRAM_OPTIONS options)
	{
		// set the image type
		OpticalStorageRawImage raw = new(IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE.IMAPI_FORMAT2_RAW_CD_SUBCODE_IS_RAW);
		// Add tracks
		foreach (var fileName in Directory.EnumerateFiles(options.FileName, "*.wav"))
		{
			// We have a file, let's add it
			Console.Write("Attempting to add {0}\n", fileName);

			//Add the track to the stream
			raw.Tracks.Add(fileName);
		}

		// Set the app name for use with exclusive access THIS IS REQUIRED
		OpticalStorageWriteRawOperation op = new(clientName)
		{
			Data = raw.GetImageStream(),
			// create a DiscRecorder object
			Device = OpticalStorageManager.Devices[options.WriterIndex],
			// NOTE: this will change later to put a different mode when it's fully implemented
			RequestedSectorType = IMAPI_FORMAT2_RAW_CD_DATA_SECTOR_TYPE.IMAPI_FORMAT2_RAW_CD_SUBCODE_IS_RAW
		};

		// burn stream
		op.Execute();
	}

	private struct PROGRAM_OPTIONS
	{
		// defaults should all be logical if all are set to false / default / 0

		public bool Audio;

		public string BootFileName;

		public bool Close;

		public bool CloseDisc;

		public bool Eject;

		// erase details
		public bool Erase;

		public string FileName;

		// Test Options
		public bool FreeSpace;

		public bool FullErase;

		public bool Image;

		public bool Iso;

		public bool Joliet;

		// list the Writers
		public bool ListWriters;

		public bool Multi;

		public bool Raw;

		//public bool SessionAtOnceWrite;

		public bool UDF;

		public string VolumeName;

		// Write details
		public bool Write;

		public int WriterIndex; // store as zero-based index, print as 1-based
	}
}