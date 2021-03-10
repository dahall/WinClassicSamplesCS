using System;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Opc;
using static Vanara.PInvoke.UrlMon;

namespace MusicBundle
{
	internal static partial class Program
	{
		private const int MAX_BUFFER_SIZE = 1024;

		///////////////////////////////////////////////////////////////////////////////
		// Description: Creates and retrieves a full file name by combining the part name with the base directory path. The part name is
		// converted to a file name by replacing the leading '/' character with '\'. Method creates the directory structure to the full file
		// name. Memory for the full file name is allocated by calling CoTaskMemAlloc, and the caller free the memory by calling CoTaskMemFree.
		///////////////////////////////////////////////////////////////////////////////
		private static void CreateDirectoryFromPartName(string filePath, // The base directory path.
			string partName, // Part name to use to create full file name.
			out string fullFileName // The resultant full file name. The caller must free the memory allocated
									// for this string buffer.
		)
		{
			fullFileName = System.IO.Path.Combine(filePath, partName.TrimStart('/').Replace('/', '\\'));
			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullFileName));
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Displays stream content in the console.
		///////////////////////////////////////////////////////////////////////////////
		private static void DisplayStreamContent(string title, // The title to be displayed above the stream contents.
			IStream stream // The stream whose contents are displayed.
		)
		{
			var buffer = new byte[MAX_BUFFER_SIZE];
			int bytesRead = 1;

			// Display title.
			Console.Write("++++++ {0} ++++++\n", title);

			// Read and display data from screen.
			unsafe
			{
				while (bytesRead > 0)
				{
					// Pass in 'size - 1' so the buffer is always null-terminated when passed in to the Console.Write method.
					stream.Read(buffer, buffer.Length - 1, (IntPtr)(void*)&bytesRead);

					if (bytesRead > 0)
					{
						// Display data.
						Console.WriteLine(Encoding.ASCII.GetString(buffer));
					}
				}
			}

			// Display end deliminator.
			Console.Write("++++++ End {0} ++++++\n\n", title);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Forms and retrieves a full file name by combining the file name with the base directory path. Memory for the full
		// file name is allocated by calling CoTaskMemAlloc, and the caller free the memory by calling CoTaskMemFree.
		///////////////////////////////////////////////////////////////////////////////
		private static void GetFullFileName(string filePath, // The base directory path.
			string fileName, // Name of the file.
			out string fullFileName // The resultant full file name. The caller must free the memory allocated
									// for this string buffer.
		) => fullFileName = System.IO.Path.Combine(filePath, fileName.TrimStart('\\'));

		///////////////////////////////////////////////////////////////////////////////
		// Description: Gets the relationship of the specified type from a specified relationship set.
		// Note: Method expects exactly one relationship of the specified type. This limitation is described in the Music Bundle Package
		// specification--and is not imposed by the Packaging APIs or the OPC.
		///////////////////////////////////////////////////////////////////////////////
		private static void GetRelationshipByType(IOpcRelationshipSet relsSet, // Relationship set that contains the relationship.
			string relationshipType, // The relationship type of the relationship.
			out IOpcRelationship targetRelationship // Recieves the relationship. Method may return a valid
													// pointer even on failure, and the caller must always release if a non-default value is returned.
		)
		{
			targetRelationship = null;
			HRESULT hr = HRESULT.S_OK;

			// Get an enumerator of all relationships of the required type.
			IOpcRelationshipEnumerator relsEnumerator = relsSet.GetEnumeratorForType(relationshipType);

			// Enumerate through relationships, ensuring that there is exactly one relationship that has the specified type.
			var count = 0;
			while (hr.Succeeded && (hr = relsEnumerator.MoveNext(out var hasNext)).Succeeded && hasNext)
			{
				count++;

				if (count > 1)
				{
					// There is more than one relationship of the specified type.
					Console.Error.Write("Invalid music bundle package: cannot have more than 1 relationship with type: {0}.\n", relationshipType);

					// Set the return code to an error.
					hr = HRESULT.E_FAIL;

					// An error was encountered; stop enumerating.
					break;
				}

				if (hr.Succeeded)
				{
					// Get the relationship at the current position of the enumerator.
					hr = relsEnumerator.GetCurrent(out targetRelationship);
				}
			}

			if (hr.Succeeded)
			{
				if (count == 0)
				{
					// There were no relationships in the set that had the specified type.
					Console.Error.Write("Invalid music bundle package: relationship with type {0} does not exist.\n", relationshipType);

					throw new InvalidOperationException();
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Gets the target part of a relationship with the 'Internal' target mode.
		///////////////////////////////////////////////////////////////////////////////
		private static void GetRelationshipTargetPart(IOpcPartSet partSet, // Set of the parts in the package.
			IOpcRelationship relationship,// Relationship that targets the required part.
			string expectedContentType, // Content type expected for the target part.
			out IOpcPart targetPart // Recieves pointer to target part. Method may return a valid
									// pointer even on failure, and the caller must always release if a non-default value is returned.
		)
		{
			targetPart = null;

			OPC_URI_TARGET_MODE targetMode = relationship.GetTargetMode();
			if (targetMode != OPC_URI_TARGET_MODE.OPC_URI_TARGET_MODE_INTERNAL)
			{
				// The relationship's target is not a part.
				var relationshipType = relationship.GetRelationshipType();

				Console.Error.Write("Invalid music bundle package: relationship with type {0} must have Internal target mode.\n", relationshipType);

				// Set the return code to an error.
				throw new InvalidOperationException();
			}

			// Relationship's target is a part; the target mode is 'Internal'.

			// Get the URI of the relationship source.
			IOpcUri sourceUri = relationship.GetSourceUri();

			// Get the URI of the relationship target.
			IUri targetUri = relationship.GetTargetUri();

			// Resolve the target URI to the part name of the target part.
			IOpcPartUri targetPartUri = sourceUri.CombinePartUri(targetUri);

			// Check that a part with the resolved part name exists in the part set.
			var partExists = partSet.PartExists(targetPartUri);

			if (!partExists)
			{
				// The part does not exist in the part set.
				Console.Error.Write("Invalid music bundle package: the target part of relationship does not exist.\n");

				// Set the return code to an error.
				throw new InvalidOperationException();
			}

			// Get the part.
			targetPart = partSet.GetPart(targetPartUri);

			// Get the content type of the part.
			var contentType = targetPart.GetContentType();

			if (contentType != expectedContentType)
			{
				// Content type of the part did not match the expected content type.
				Console.Error.Write("Invalid music bundle package: the target part does not have correct content type.\n");

				// Set the return code to an error.
				throw new InvalidOperationException();
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Opens a stream to the path and file name specified, and deserializes the file's content as the content of the part.
		///////////////////////////////////////////////////////////////////////////////
		private static void WriteFileContentToPart(IOpcFactory opcFactory,
			string filePath, // Directory where the file is located.
			string fileName, // Name of file whose content will be deserialized.
			IOpcPart opcPart // Part into which the file content is deserialized.
		)
		{
			// Get the full file name of the file.
			GetFullFileName(filePath, fileName, out var fullFileName);

			// Create a read-only stream over the file.
			opcFactory.CreateStreamOnFile(fullFileName, OPC_STREAM_IO_MODE.OPC_STREAM_IO_READ, default,
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, out IStream fileStream).ThrowIfFailed();

			// Get the content stream of the part.
			IStream partStream = opcPart.GetContentStream();

			// Part content is overwritten; truncate the size of the stream to zero.
			partStream.SetSize(0);

			// Copy the content from the file stream to the part content stream.
			fileStream.CopyTo(partStream, long.MaxValue, default, default);
		}

		///////////////////////////////////////////////////////////////////////////////
		// Description: Reads and serializes part content to a specified file. Also creates the required directory structure that contains
		// the file.
		///////////////////////////////////////////////////////////////////////////////
		private static void WritePartContentToFile(IOpcFactory opcFactory,
			string filePath, // Base directory path where the file is created.
			IOpcPart opcPart // Part whose content is serialized.
		)
		{
			// Get the part name of this part.
			IOpcPartUri opcPartUri = opcPart.GetName();

			// Get the part name as string.
			var partUriString = opcPartUri.GetAbsoluteUri();

			// Create the full file name and the directory structure.
			CreateDirectoryFromPartName(filePath, partUriString, out var fullFileName);

			// Create a write-only stream over the file where part content will be serialized.
			opcFactory.CreateStreamOnFile(fullFileName, OPC_STREAM_IO_MODE.OPC_STREAM_IO_WRITE, default,
				FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, out IStream fileStream).ThrowIfFailed();

			// Get the part content stream.
			IStream partStream = opcPart.GetContentStream();

			// Copy the part content stream to the file stream.
			partStream.CopyTo(fileStream, long.MaxValue, default, default);
		}
	}
}