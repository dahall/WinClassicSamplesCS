using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.Opc;
using static Vanara.PInvoke.UrlMon;

namespace MusicBundle;

internal static partial class Program
{
	//---------------------------------------
	// For the sake of simplicity, the production data that appears below is hard
	// coded. In a fully-functional application, this data would be retrieved from
	// the user of the application.

	// Album art.
	private const string g_albumArt = "\\AlbumArt\\jacqui.jpg";

	// Link to external website for the album.
	private const string g_albumWebsite = "http://www.example.com/Media/Albums/Jacqui%20Kramer/Leap%20Forward";

	// Track list.
	private const string g_trackList = "\\TrackList.wpl";

	// Lyric file names. One-to-one mapping with track names.
	private static readonly string[] g_lyricsNames = {
		"\\Lyrics\\CrystalFree.txt",
		"\\Lyrics\\Sire.txt",
		"\\Lyrics\\SmallPines.txt",
		"\\Lyrics\\Valparaiso.txt"
	};

	// Track names.
	private static readonly string[] g_trackNames = {
		"\\Tracks\\CrystalFree.wma",
		"\\Tracks\\Sire.wma",
		"\\Tracks\\SmallPines.wma",
		"\\Tracks\\Valparaiso.wma"
	};

	//-------------------------------------
	// Production helper methods.

	///////////////////////////////////////////////////////////////////////////////
	// Description: Adds the Album Art part to the package and creates a package relationship to that has the Album Art part as its target.
	///////////////////////////////////////////////////////////////////////////////
	private static void AddAlbumArtToBundle(IOpcFactory opcFactory,
		IOpcPartSet packagePartSet, // Represents the set of parts (excluding Relationships parts) in a package.
		IOpcRelationshipSet packageRelationshipSet, // Represents the Relationships part that stores package relationships.
		string inputDirectory // Directory location of the files specified in trackName and lyricsName.
	)
	{
		// Add Album Art part.
		AddPartToBundle(opcFactory, g_albumArt, packagePartSet, g_albumArtContentType, OPC_COMPRESSION_OPTIONS.OPC_COMPRESSION_NONE, inputDirectory, out IOpcPartUri albumArtPartUri, out _);

		// Add a package relationship that has the Album Art part as its target.
		packageRelationshipSet.CreateRelationship(default, // Use auto-generated relationship ID.
		g_albumArtRelationshipType,
		albumArtPartUri, // Relationship target's URI.
		OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_INTERNAL); // Relationship's target is internal.
	}

	///////////////////////////////////////////////////////////////////////////////
	// Description: Creates a package relationship that targets the album website and adds the relationship to the package relationship set.
	///////////////////////////////////////////////////////////////////////////////
	private static void AddAlbumWebsiteToBundle(IOpcRelationshipSet packageRelationshipSet) // Relationship set that has all package relationships.
	{
		// Create the URI for the Album Website.
		CreateUri(g_albumWebsite, Uri_CREATE.Uri_CREATE_CANONICALIZE, default, out IUri uri).ThrowIfFailed();

		// Add Album Website as a package relationship. Note that the target mode of the relationship is "External" because the website
		// is a resource that exists outside of the package.
		packageRelationshipSet.CreateRelationship(default, // Use auto-generated relationship ID.
			g_albumWebsiteRelationshipType,
			uri, // Relationship target's URI.
			OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_EXTERNAL); // Relationship's target is external.
	}

	///////////////////////////////////////////////////////////////////////////////
	// Description: Creates an empty part, adds the part to the package's part set and writes data to the part as part content.
	//
	// Note: This method does not add Relationships parts to the music bundle.
	///////////////////////////////////////////////////////////////////////////////
	private static void AddPartToBundle(IOpcFactory opcFactory,
		string contentFileName, // File that contains the content to be stored in the part to add.
		IOpcPartSet packagePartSet, // Represents the set of parts (excluding Relationships parts) in a package.
		string contentType, // The content type of the content to be stored in the part.
		OPC_COMPRESSION_OPTIONS compressionOptions, // Level of compression to use on the part.
		string inputDirectory, // Directory location of the file specified in contentFileName.
		out IOpcPartUri createdPartUri,// Represents the part name. The caller must release the interface.
		out IOpcPart createdPart// Optional. Represents the part to add to the package. The caller must release the interface.
	)
	{
		// Create the part name from the file name.
		opcFactory.CreatePartUri(contentFileName, out createdPartUri).ThrowIfFailed();

		// Create the part as an empty part, and add it to the set of parts in the package.
		createdPart = packagePartSet.CreatePart(createdPartUri, contentType, compressionOptions);

		// Add content to the empty part.
		WriteFileContentToPart(opcFactory, inputDirectory, contentFileName, createdPart);
	}

	///////////////////////////////////////////////////////////////////////////////
	// Description: Adds a Track part and a Lyrics part to the set.
	// 1. Track part:
	// + Adds a Track part to the music bundle
	// + Creates a relationship where the source is the Track List part and the target is the Tark part by creating the relationship
	// from the Track List part's relationship set.
	// 2. Lyrics part:
	// + Adds a Lryics part to the music bundle.
	// + Creates a relationship where the source is the Track part that was added to the music bundle and the target is the Tark part by
	// creating the relationship from the Track part's relationship set.
	///////////////////////////////////////////////////////////////////////////////
	private static void AddTrackAndLyricsToBundle(IOpcFactory opcFactory,
		IOpcPartSet packagePartSet, // Represents the set of parts (excluding Relationships parts) in a package.
		IOpcRelationshipSet trackListRelationshipSet, // Represents the Relationships part containing relationships that have the Track List part as their target.
		string inputDirectory, // Directory location of the files specified in trackName and lyricsName.
		string trackName, // Name of file that contains the track data.
		string lyricsName // Name of tile that contains lyrics data.
	)
	{
		// Add a Track part that contains track data to the package.
		AddPartToBundle(opcFactory, trackName, packagePartSet, g_trackContentType, OPC_COMPRESSION_OPTIONS.OPC_COMPRESSION_NONE,
			inputDirectory, out IOpcPartUri trackPartUri, out IOpcPart trackPart);

		// Add a relationship that has the Track part as the target to the relationship set that represents the Relationships part
		// containing relationships that have the Track List part as their source.
		trackListRelationshipSet.CreateRelationship(default, // Use auto-generated relationship ID.
			g_trackRelationshipType,
			trackPartUri, // Relationship target's URI.
			OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_INTERNAL // Relationship's target is internal.
		);

		// Add a Lyrics part that contains lyrics for added track.
		AddPartToBundle(opcFactory,
		lyricsName,
		packagePartSet,
		g_lyricsContentType,
		OPC_COMPRESSION_OPTIONS.OPC_COMPRESSION_NORMAL,
		inputDirectory,
		out IOpcPartUri lyricsPartUri, out _);

		// Get relationship set for Track part. The relationship set represents the Relationship part that stores relationships that
		// have the Track part as their source.
		IOpcRelationshipSet trackRelationshipSet = trackPart.GetRelationshipSet();

		// Add a relationship to the Track part's Relationships part, represented as a relationship object in the relationship object set.
		trackRelationshipSet.CreateRelationship(default, // Use auto-generated relationship ID.
			g_lyricsRelationshipType,
			lyricsPartUri, // Relationship target's URI.
			OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_INTERNAL); // Relationship's target is internal.
	}

	///////////////////////////////////////////////////////////////////////////////
	// Description: Adds the Track List part to the package, and creates and adds a package relationship to the package relationship set
	// that has the Track List part as its target, and then adds the track and lyrics to the music bundle.
	///////////////////////////////////////////////////////////////////////////////
	private static void AddTrackListToBundle(IOpcFactory opcFactory,
		IOpcPartSet packagePartSet, // Represents the set of parts (excluding Relationships parts) in a package.
		IOpcRelationshipSet packageRelationshipSet, // The relationship set that stores package relationships.
		string inputDirectory // Directory location of the files specified in trackName and lyricsName.
	)
	{
		// Add Track List part.
		AddPartToBundle(opcFactory,
			g_trackList,
			packagePartSet,
			g_trackListContentType,
			OPC_COMPRESSION_OPTIONS.OPC_COMPRESSION_NORMAL,
			inputDirectory,
			out IOpcPartUri trackListPartUri,
			out IOpcPart trackListPart);

		// Add a package relationship that has the Track List part as its target.
		packageRelationshipSet.CreateRelationship(default, // Use auto-generated relationship ID.
			g_trackListRelationshipType,
			trackListPartUri, // Relationship target's URI.
			OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_INTERNAL); // Relationship's target is internal.

		// Get the relationship set that represents the Relationships part that stores the relationships that have the Track List part
		// as their source.
		IOpcRelationshipSet trackListRelationshipSet = trackListPart.GetRelationshipSet();

		// Add all track and lyric files to the music bundle as Track parts and Lyric parts, respectively.
		for (var i = 0; i < g_trackNames.Length; i++)
		{
			AddTrackAndLyricsToBundle(opcFactory, packagePartSet, trackListRelationshipSet,
				inputDirectory, g_trackNames[i], g_lyricsNames[i]);
		}
	}

	//---------------------------------
	// Function to create the new music bundle.
	// Exposed through MusicBundle.h.
	//
	///////////////////////////////////////////////////////////////////////////////
	// Description: Creates a package that is a music bundle, in compliance with both the OPC specification and the Music Bundle
	// specification, which can be found in MusicBundle.h. Given the directory that contains all the files needed, this method creates
	// all parts and relationships for the new music bundle and saves resultant package.
	//
	// Note: Relationships parts are not created explicitly. Relationship sets are serialized as Relationships parts when the package is saved.
	///////////////////////////////////////////////////////////////////////////////
	private static void ProduceMusicBundle(string inputDirectory, // Parent directory that contains files to add to the music bundle.
		string outputPackageName // Name of the music bundle package to create.
	)
	{
		var opcFactory = new IOpcFactory();

		IOpcPackage opcPackage = opcFactory.CreatePackage();

		// Get the set of parts in the package. Parts (that are not Relationships parts) to be included in the music bundle to be
		// created will be created in this set.
		IOpcPartSet packagePartSet = opcPackage.GetPartSet();

		// Get the set of package relationships. All package relationships specific to the music bundle to be created will be created in
		// this set.
		IOpcRelationshipSet packageRelationshipSet = opcPackage.GetRelationshipSet();

		// Populate the music bundle.

		// Add the Album Art part to the package.
		AddAlbumArtToBundle(opcFactory, packagePartSet, packageRelationshipSet, inputDirectory);

		// Add the Track List part, and Track and Lyrics parts to package.
		AddTrackListToBundle(opcFactory, packagePartSet, packageRelationshipSet, inputDirectory);

		// Add a package relationship that targets the album website to the package.
		AddAlbumWebsiteToBundle(packageRelationshipSet);

		// Save the music bundle.

		// Create a writable stream over the name of the package to be created.
		opcFactory.CreateStreamOnFile(outputPackageName, OPC_STREAM_IO_MODE.OPC_STREAM_IO_WRITE, default,
			FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, out IStream fileStream);

		// Serialize package content to the writable stream.
		opcFactory.WritePackageToStream(opcPackage, OPC_WRITE_FLAGS.OPC_WRITE_DEFAULT, fileStream);
	}
}