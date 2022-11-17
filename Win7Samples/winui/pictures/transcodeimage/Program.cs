using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

// Transcoded image will fit in a box of the requested height and width
const int REQUESTED_WIDTH = 100;
const int REQUESTED_HEIGHT = 100;
// Requested format may be TI_BITMAP or TI_JPEG. 
const TI_FLAGS REQUESTED_FORMAT = TI_FLAGS.TI_BITMAP;

//command line arguments
string pwszCommandLineArg1;
string pwszCommandLineArg2;

if (args.Length == 2)
{
	pwszCommandLineArg1 = args[0];
	pwszCommandLineArg2 = args[1];
}
else // TODO: accept size arguments
{
	Console.Write("Wrong # of arguments. Usage: TranscodeImage.exe sourceimagepath destinationimagepath");
	return -1;
}

// ITranscodeImage object
ITranscodeImage pTransImg = new();

// Used for creating an IShellItem object representing the image.
PIDL pItemIdList;
// Get a pointer to an ITEMIDLIST by parsing the first command line argument
SHParseDisplayName(pwszCommandLineArg1, default, out pItemIdList, 0, out _).ThrowIfFailed();

// IShellItem representing the image to pass to TranscodeImage
IShellItem pItemToTranscode;
// Create an IShellItem object with the ITEMIDLIST you created above.
SHCreateShellItem(default, default, pItemIdList, out pItemToTranscode).ThrowIfFailed();

// A stream to hold the transcoded image
IStream pImgStream;
// Create a stream on a file to pass to TranscodeImage later.
SHCreateStreamOnFile(pwszCommandLineArg2, STGM.STGM_READWRITE, out pImgStream);

// Transcode the image to the indicated format,
// which is either TI_BITMAP, or TI_JPEG
// and resize it to REQUESTED_WIDTH x REQUESTED_WIDTH.
pTransImg.TranscodeImage(pItemToTranscode, REQUESTED_WIDTH, REQUESTED_HEIGHT, REQUESTED_FORMAT, pImgStream, out _, out _);

// Write the stream containing the transcoded image to the destination file.
pImgStream.Commit((int)STGC.STGC_DEFAULT);

return 0;