using System;
using Vanara.PInvoke;

namespace MusicBundle
{
	internal static partial class Program
	{
		// Content types for parts in a music bundle, as defined in the music bundle
		// specification, which is in the MusicBundle.h header file.
		private const string g_trackContentType = "audio/x-ms-wma";
		private const string g_lyricsContentType = "text/plain";
		private const string g_albumArtContentType = "image/jpeg";
		private const string g_trackListContentType = "application/vnd.ms-wpl";

		// Relationship types for relationships in music bundle, as defined in the
		// music bundle specification, which is in the MusicBundle.h header file.
		// As described in the OPC, the format creator can create custom relationship
		// types that identify what kinds of resources that are the targets of 
		// relationships in the custom package format.
		private const string g_trackRelationshipType = "http://schemas.example.com/package/2008/relationships/media-bundle/playlist-song";
		private const string g_lyricsRelationshipType = "http://schemas.example.com/package/2008/relationships/media-bundle/song-lryic";
		private const string g_trackListRelationshipType = "http://schemas.example.com/package/2008/relationships/media-bundle/tracklist";
		private const string g_albumWebsiteRelationshipType = "http://schemas.example.com/package/2008/relationships/media-bundle/album-website";

		// The thumbnails relationship type is defined in the OPC specification.
		private const string g_albumArtRelationshipType = "http://schemas.openxmlformats.org/package/2006/relationships/metadata/thumbnail";

		//============================================================
		// Main entry point of the sample.
		//============================================================
		private static int Main(string[] args)
		{
			var bShowHelp = false; // Indicates whether parameter help should be shown.

			HRESULT hr = HRESULT.S_OK;

			// Check if consuming or producing Music Bundle.
			if (args.Length == 3)
			{
				if (string.Equals("-p", args[0], StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						// Produce Music Bundle.
						ProduceMusicBundle(
								args[1], // Input directory.
								args[2]  // Output package name.
								);
					}
					catch (Exception ex)
					{
						Console.Error.Write("Production failed with error : 0x{0:X}\n", ex.HResult);
					}
				}
				else if (string.Equals("-c", args[0], StringComparison.OrdinalIgnoreCase))
				{
					try
					{
						// Consume Music Bundle.
						ConsumeMusicBundle(
								args[1], // Name of package to consume.
								args[2]  // Output directory.
								);
					}
					catch (Exception ex)
					{
						Console.Error.Write("Consumption failed with error : 0x{0:X}\n", ex.HResult);
					}
				}
				else
				{
					// Neither production or consumption were indicated.
					bShowHelp = true;
				}
			}
			else
			{
				// Wrong number of aruguments.
				bShowHelp = true;
			}

			if (bShowHelp)
			{
				// Input arguments are invalid; show help text.
				Console.Write("Music Bundle Sample:\n");
				Console.Write("To Produce Bundle : MusicBundle.exe -p <Input Directory> <Output Package Path>\n");
				Console.Write("To Consume Bundle : MusicBundle.exe -c <Input Package Path> <Output Directoy>\n");
			}

			return hr.Succeeded ? 0 : 1;
		}
	}
}
