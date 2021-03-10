using System;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.Opc;
using static Vanara.PInvoke.UrlMon;

namespace MusicBundle
{
	internal static partial class Program
	{
		//-------------------------------------
		// Consumption helper methods.

		//---------------------------------
		// Function to consume the new music bundle.
		// Exposed through MusicBundle.h.
		//
		///////////////////////////////////////////////////////////////////////////////
		// Description: Deserializes the contents of a music bundle package, displays text from the Track List, Lyrics, Album Website parts.
		// The method then serializes the contents of the parts to files in specified output directory.
		///////////////////////////////////////////////////////////////////////////////
		private static void ConsumeMusicBundle(string inputPackageName,// Name of music bundle.
			string outputDirectory // Directory into which music bundle parts are written as files.
		)
		{
			// Create a new factory.
			var opcFactory = new IOpcFactory();

			// Open a read-only stream over the input package.
			opcFactory.CreateStreamOnFile(inputPackageName, OPC_STREAM_IO_MODE.OPC_STREAM_IO_READ, // Read-only.
				default, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, out IStream packageStream).ThrowIfFailed();

			// Create a package object to represent the pacakge being read, allowing package components to be accessed through Packaging API objects.
			opcFactory.ReadPackageFromStream(packageStream, OPC_READ_FLAGS.OPC_READ_DEFAULT, // Validate package component when it is accessed.
				out IOpcPackage opcPackage).ThrowIfFailed();

			// Get relationship set of package relationships.
			IOpcRelationshipSet packageRelationshipSet = opcPackage.GetRelationshipSet();

			// Get the set of parts in the package that are not Relationships parts.
			IOpcPartSet packagePartSet = opcPackage.GetPartSet();

			// Read, display and unpack the music bundle.

			// Read and display album art.
			ReadAlbumArtFromBundle(opcFactory,
				packageRelationshipSet,
				packagePartSet,
				outputDirectory);

			// Read and display album website.
			ReadAlbumWebsiteFromBundle(packageRelationshipSet);

			// Read and unpack as files in the output directory: the track list, tracks and lyrics. Display the track list and corresponding lyrics.
			ReadTrackListFromBundle(opcFactory, packageRelationshipSet, packagePartSet, outputDirectory);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Enumerates and reads all the Track parts in the Track List relationship set by the relationship type
		///////////////////////////////////////////////////////////////////////////////
		private static void EnumerateTracksFromBundle(IOpcFactory opcFactory,
			IOpcRelationshipSet trackListRelationshipSet,// Set of all relationships whose source is the Track List part.
			IOpcPartSet packagePartSet, // Set of all parts in the package.
			string outputDirectory // Write part content to a file in this directory.
		)
		{
			// Get enumerator of relationships in the set that are the track relationship type.
			IOpcRelationshipEnumerator relationshipEnumerator = trackListRelationshipSet.GetEnumeratorForType(g_trackRelationshipType);

			// For each relationship in the enumerator, get the targetted Track part, read the part and read the Lyrics part linked to the
			// current Track part.
			while (relationshipEnumerator.MoveNext(out var bNext).Succeeded && bNext)
			{
				// Get current enumerator relationship.
				if (relationshipEnumerator.GetCurrent(out IOpcRelationship trackRelationship).Succeeded)
				{
					// Get the Track part targetted by the relationship.
					GetRelationshipTargetPart(packagePartSet,
						trackRelationship,
						g_trackContentType,
						out IOpcPart trackPart);

					ReadTrackAndLyricsFromBundle(opcFactory,
						packagePartSet,
						trackPart,
						outputDirectory);
				}
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Reads the Album Art in the Music Bundle and writes the part to a file in the output directory
		///////////////////////////////////////////////////////////////////////////////
		private static void ReadAlbumArtFromBundle(IOpcFactory opcFactory,
			IOpcRelationshipSet packageRelationshipSet,// Package relationship set.
			IOpcPartSet packagePartSet, // Set of all parts in the package.
			string outputDirectory // Write part content to a file in this directory.
		) =>
			// Find the Album Art part by using the album art relationship type.
			ReadPartFromBundle(opcFactory,
				packageRelationshipSet,
				packagePartSet,
				g_albumArtRelationshipType,
				g_albumArtContentType,
				outputDirectory, null, out _);

		///////////////////////////////////////////////////////////////////////////////
		// Description: Reads the Album Website part displays its content in the console.
		///////////////////////////////////////////////////////////////////////////////
		private static void ReadAlbumWebsiteFromBundle(IOpcRelationshipSet packageRelationshipSet // Package relationship set.
		)
		{
			// Find the Album Website part by using the album website relationship type.
			GetRelationshipByType(packageRelationshipSet, g_albumWebsiteRelationshipType, out IOpcRelationship opcRelationship);

			// Get the target mode of the relationship; teh mode must be 'External' for the relationship to target an external absolute URL.
			OPC_URI_TARGET_MODE targetMode = opcRelationship.GetTargetMode();

			if (targetMode != OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_EXTERNAL)
			{
				// The target mode was 'Internal'.
				Console.Error.Write("Invalid music bundle package: relationship with type {0} must have External target mode.\n",
				g_albumWebsiteRelationshipType);

				// Set the return code to an error.
				throw new InvalidOperationException();
			}

			// Get the target URI, which is the album website URL.
			IUri targetUri = opcRelationship.GetTargetUri();

			// Get the album website URL as a string.
			var targetUriString = targetUri.GetAbsoluteUri();

			// Display the album website URL.
			Console.Write("\n++++++ Album Website ++++++\n{0}\n\n", targetUriString);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Reads a part from the Music Bundle, displays content to the console (if required), and writes the content to a file
		// in the output directory.
		///////////////////////////////////////////////////////////////////////////////
		private static void ReadPartFromBundle(IOpcFactory opcFactory,
			IOpcRelationshipSet relationshipSet,// Track relationship set.
			IOpcPartSet packagePartSet, // Set of all parts in the package.
			string relationshipType, // Relationship type of relationship targeting the part.
			string contentType, // Content type of the part.
			string outputDirectory, // Part content is serialized as the content of a file in this directory.
			string displayTitle, // Title to display with file contents. Optional; set to default if displaying content is not required.
			out IOpcPart partRead // The part to be read. Optional; caller must release the object.
		)
		{
			// Get relationships of the specified type.
			GetRelationshipByType(relationshipSet, relationshipType, out IOpcRelationship relationship);

			// Get the part targetted by the relationship.
			GetRelationshipTargetPart(packagePartSet, relationship, contentType, out partRead);

			if (displayTitle is not null)
			{
				// Get part content stream.
				IStream stream = partRead.GetContentStream();

				// Display the content to the console.
				DisplayStreamContent(displayTitle, stream);
			}

			// Write the part content to a file in the output directory.
			WritePartContentToFile(opcFactory, outputDirectory, partRead);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Method does the following:
		// 1. Reads a specified Track part in the Music Bundle and writes the part to a file in the output directory.
		// 2. Reads the Lyrics part for the Track part.
		///////////////////////////////////////////////////////////////////////////////
		private static void ReadTrackAndLyricsFromBundle(IOpcFactory opcFactory,
			IOpcPartSet packagePartSet,// Set of all parts in the package.
			IOpcPart trackPart, // Track part to read.
			string outputDirectory // Write part content to a file in this directory.
		)
		{
			// Write part content to a file in the output directory.
			WritePartContentToFile(opcFactory, outputDirectory, trackPart);

			// Get the relationship set for the Track part.
			IOpcRelationshipSet trackRelationshipSet = trackPart.GetRelationshipSet();

			// Get Lyrics for track.
			ReadPartFromBundle(opcFactory,
				trackRelationshipSet,
				packagePartSet,
				g_lyricsRelationshipType,
				g_lyricsContentType,
				outputDirectory,
				"Lyrics", out _);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Method does the following:
		// 1. Reads the Track List in the Music Bundle, displays it to the console.
		// 2. Writes the part to a file in the output directory.
		// 3. Enumerates the Track List relationships and reads all the Tracks in the Music Bundle.
		///////////////////////////////////////////////////////////////////////////////
		private static void ReadTrackListFromBundle(IOpcFactory opcFactory,
			IOpcRelationshipSet packageRelationshipSet,// Package relationship set.
			IOpcPartSet packagePartSet, // Set of all parts in the package.
			string outputDirectory // Write part content to a file in this directory.
		)
		{
			// Find the Track List part by using the track list relationship type.
			ReadPartFromBundle(opcFactory,
				packageRelationshipSet,
				packagePartSet,
				g_trackListRelationshipType,
				g_trackListContentType,
				outputDirectory,
				"TrackList",
				out IOpcPart trackListPart);

			// Get the set of relationships whose source is the Track List part.
			IOpcRelationshipSet trackListRelationshipSet = trackListPart.GetRelationshipSet();

			// Enumerate tracks in the bundle.
			EnumerateTracksFromBundle(opcFactory,
				trackListRelationshipSet,
				packagePartSet,
				outputDirectory);
		}
	}
}