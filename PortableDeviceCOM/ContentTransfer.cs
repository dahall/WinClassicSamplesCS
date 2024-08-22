using System.IO;
using Vanara.InteropServices;
using static Vanara.PInvoke.ComDlg32;
using static Vanara.PInvoke.ShlwApi;

static partial class Program
{
	// Reads a string property from the IPortableDeviceProperties interface and returns it
	static string GetStringValue([In] IPortableDeviceProperties properties, string objectID, in PROPERTYKEY key)
	{
		// 1) CoCreate an IPortableDeviceKeyCollection interface to hold the the property key we wish to read.
		IPortableDeviceKeyCollection propertiesToRead = new();

		// 2) Populate the IPortableDeviceKeyCollection with the keys we wish to read.
		// NOTE: We are not handling any special error cases here so we can proceed with adding as many of the target properties as we can.
		propertiesToRead.Add(key);

		// 3) Call GetValues() passing the collection of specified PROPERTYKEYs.
		IPortableDeviceValues objectProperties = properties.GetValues(objectID, // The object whose properties we are reading
			propertiesToRead); // The properties we want to read

		// 4) Extract the string value from the returned property collection
		return objectProperties.GetStringValue(key);
	}

	// Copies data from a source stream to a destination stream using the specified transferSizeBytes as the temporary buffer size.
	static int StreamCopy([In] IStream destStream, [In] IStream sourceStream, int transferSizeBytes)
	{
		// Allocate a temporary buffer (of Optimal transfer size) for the read results to be written to.
		byte[] objectData = new byte[transferSizeBytes];

		int totalBytesRead = 0, totalBytesWritten = 0, bytesWritten = 0, bytesRead = 0;
		using PinnedObject pWritten = new(bytesWritten);
		using PinnedObject pRead = new(bytesRead);

		// Read until the number of bytes returned from the source stream is 0, or an error occured during transfer.
		do
		{
			// Read object data from the source stream
			sourceStream.Read(objectData, transferSizeBytes, pRead);

			// Write object data to the destination stream
			totalBytesRead += bytesRead; // Calculating total bytes read from device for debugging purposes only

			destStream.Write(objectData, bytesRead, pWritten);

			totalBytesWritten += bytesWritten; // Calculating total bytes written to the file for debugging purposes only

			// Output Read/Write operation information only if we have received data and if no error has occured so far.
			if ((bytesRead > 0))
			{
				Console.Write("Read {0} bytes from the source stream...Wrote {1} bytes to the destination stream...\n", bytesRead, bytesWritten);
			}

		} while ((bytesRead > 0));

		// If we are successful, set bytesWrittenOut before exiting.
		return totalBytesWritten;
	}

	// Transfers a selected object's data (WPD_RESOURCE_DEFAULT) to a temporary file.
	static void TransferContentFromDevice([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier on the device to transfer.
		Console.Write("Enter the identifier of the object you wish to transfer.\n>");
		string? selection = Console.ReadLine();

		// 1) get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceResources interface from the IPortableDeviceContent interface to access the resource-specific methods.
		IPortableDeviceResources resources = content.Transfer();

		// 3) Get the IStream (with READ access) and the optimal transfer buffer size to begin the transfer.
		IStream objectDataStream = resources.GetStream(selection!, // Identifier of the object we want to transfer
			WPD_RESOURCE_DEFAULT, // We are transferring the default resource (which is the entire object's data)
			STGM.STGM_READ, // Opening a stream in READ mode, because we are reading data from the device.
			out var optimalTransferSizeBytes); // Driver supplied optimal transfer size

		// 4) Read the WPD_OBJECT_ORIGINAL_FILE_NAME property so we can properly name the transferred object. Some content objects may not
		// have this property, so a fall-back case has been provided below. (i.e. Creating a file named <objectID>.data )
		IPortableDeviceProperties properties = content.Properties();

		string? originalFileName = null;
		try { originalFileName = GetStringValue(properties, selection!, WPD_OBJECT_ORIGINAL_FILE_NAME); }
		catch (Exception ex) { Console.Write("! Failed to read WPD_OBJECT_ORIGINAL_FILE_NAME on object '{0}', hr = 0x{1:X}\n", selection, ex.HResult); }
		if (originalFileName is null)
		{
			// Create a temporary file name
			originalFileName = $"{selection}.data";
			Console.Write("* Creating a filename '{0}' as a default.\n", originalFileName);
		}

		// 5) Create a destination for the data to be written to. In this example we are creating a temporary file which is named the same as
		// the object identifier string.
		SHCreateStreamOnFileEx(originalFileName, STGM.STGM_CREATE | STGM.STGM_WRITE, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, false, default, out var finalFileStream).ThrowIfFailed();

		// 6) Read on the object's data stream and write to the final file's data stream using the driver supplied optimal transfer buffer size.

		// Since we have IStream-compatible interfaces, call our helper function that copies the contents of a source stream into a
		// destination stream.
		var totalBytesWritten = StreamCopy(finalFileStream!, // Destination (The Final File to transfer to)
			objectDataStream, // Source (The Object's data to transfer from)
			(int)optimalTransferSizeBytes); // The driver specified optimal transfer buffer size
		if (totalBytesWritten == 0)
		{
			Console.Write("! Failed to transfer object from device.\n");
		}
		else
		{
			Console.Write("* Transferred object '{0}' to '{1}'.\n", selection, originalFileName);
		}
	}

	// Deletes a selected object from the device.
	static void DeleteContentFromDevice([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier on the device to delete.
		Console.Write("Enter the identifier of the object you wish to delete.\n>");
		string? selection = Console.ReadLine();

		// 1) get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) CoCreate an IPortableDevicePropVariantCollection interface to hold the the object identifiers to delete.
		//
		// NOTE: This is a collection interface so more than 1 object can be deleted at a time. This sample only deletes a single object.
		IPortableDevicePropVariantCollection objectsToDelete = new();

		// Initialize a PROPVARIANT structure with the object identifier string that the user selected above. Notice we are allocating memory
		// for the string value. This memory will be freed when PropVariantClear() is called below.
		using PROPVARIANT pv = new(selection!);

		// Add the object identifier to the objects-to-delete list (We are only deleting 1 in this example)
		objectsToDelete.Add(pv);

		// Attempt to delete the object from the device
		content.Delete(DELETE_OBJECT_OPTIONS.PORTABLE_DEVICE_DELETE_NO_RECURSION, // Deleting with no recursion
		objectsToDelete); // Object(s) to delete

		// An HRESULT.S_OK return lets the caller know that the deletion was successful
		Console.Write("The object '{0}' was deleted from the device.\n", selection);
	}

	// Moves a selected object (which is already on the device) to another location on the device.
	static void MoveContentAlreadyOnDevice([In] IPortableDevice device)
	{
		// Check if the device supports the move command needed to perform this operation
		if (SupportsCommand(device, WPD_COMMAND_OBJECT_MANAGEMENT_MOVE_OBJECTS) == false)
		{
			Console.Write("! This device does not support the move operation (i.e. The WPD_COMMAND_OBJECT_MANAGEMENT_MOVE_OBJECTS command)\n");
			return;
		}

		// Prompt user to enter an object identifier on the device to move.
		Console.Write("Enter the identifier of the object you wish to move.\n>");
		string? selection = Console.ReadLine();

		// Prompt user to enter an object identifier on the device to move.
		Console.Write("Enter the identifier of the object you wish to move '{0}' to.\n>", selection);
		string? destinationFolderObjectID = Console.ReadLine();

		// 1) get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) CoCreate an IPortableDevicePropVariantCollection interface to hold the the object identifiers to move.
		//
		// NOTE: This is a collection interface so more than 1 object can be moved at a time. This sample only moves a single object.
		IPortableDevicePropVariantCollection objectsToMove = new();

		// Initialize a PROPVARIANT structure with the object identifier string that the user selected above. Notice we are allocating memory
		// for the string value. This memory will be freed when PropVariantClear() is called below.
		using PROPVARIANT pv = new(selection);

		// Add the object identifier to the objects-to-move list (We are only moving 1 in this example)
		objectsToMove.Add(pv);

		// Attempt to move the object on the device
		content.Move(objectsToMove, // Object(s) to move
			destinationFolderObjectID!); // Folder to move to

		// An HRESULT.S_OK return lets the caller know that the deletion was successful
		Console.Write("The object '{0}' was moved on the device.\n", selection);
	}

	// Fills out the required properties for ALL content types...
	static void GetRequiredPropertiesForAllContentTypes([In] IPortableDeviceValues objectProperties, string parentObjectID,
		string filePath, [In] IStream fileStream)
	{
		// Set the WPD_OBJECT_PARENT_ID
		objectProperties.SetStringValue(WPD_OBJECT_PARENT_ID, parentObjectID);

		// Set the WPD_OBJECT_SIZE by requesting the total size of the data stream.
		fileStream.Stat(out var statstg, (int)STATFLAG.STATFLAG_NONAME);
		objectProperties.SetUnsignedLargeIntegerValue(WPD_OBJECT_SIZE, (ulong)statstg.cbSize);

		// Set the WPD_OBJECT_ORIGINAL_FILE_NAME by splitting the file path into a separate filename.
		string originalFileName = Path.GetFileName(filePath);
		objectProperties.SetStringValue(WPD_OBJECT_ORIGINAL_FILE_NAME, originalFileName);

		// Set the WPD_OBJECT_NAME. We are using the file name without its file extension in this example for the object's name. The object
		// name could be a more friendly name like "This Cool Song" or "That Cool Picture".
		objectProperties.SetStringValue(WPD_OBJECT_NAME, Path.GetFileNameWithoutExtension(filePath));
	}

	// Fills out the required properties for WPD_CONTENT_TYPE_IMAGE
	static void GetRequiredPropertiesForImageContentTypes([In] IPortableDeviceValues pObjectProperties)
	{
		// Set the WPD_OBJECT_CONTENT_TYPE to WPD_CONTENT_TYPE_IMAGE because we are creating/transferring image content to the device.
		pObjectProperties.SetGuidValue(WPD_OBJECT_CONTENT_TYPE, WPD_CONTENT_TYPE_IMAGE);

		// Set the WPD_OBJECT_FORMAT to WPD_OBJECT_FORMAT_EXIF because we are creating/transferring image content to the device.
		pObjectProperties.SetGuidValue(WPD_OBJECT_FORMAT, WPD_OBJECT_FORMAT_EXIF);
	}

	// Fills out the required properties for WPD_CONTENT_TYPE_AUDIO
	static void GetRequiredPropertiesForMusicContentTypes([In] IPortableDeviceValues objectProperties)
	{
		// Set the WPD_OBJECT_CONTENT_TYPE to WPD_CONTENT_TYPE_AUDIO because we are creating/transferring music content to the device.
		objectProperties.SetGuidValue(WPD_OBJECT_CONTENT_TYPE, WPD_CONTENT_TYPE_AUDIO);

		// Set the WPD_OBJECT_FORMAT to WPD_OBJECT_FORMAT_MP3 because we are creating/transferring music content to the device.
		objectProperties.SetGuidValue(WPD_OBJECT_FORMAT, WPD_OBJECT_FORMAT_MP3);
	}

	// Fills out the required properties for WPD_CONTENT_TYPE_CONTACT
	static void GetRequiredPropertiesForContactContentTypes([In] IPortableDeviceValues objectProperties)
	{
		// Set the WPD_OBJECT_CONTENT_TYPE to WPD_CONTENT_TYPE_CONTACT because we are creating/transferring contact content to the device.
		objectProperties.SetGuidValue(WPD_OBJECT_CONTENT_TYPE, WPD_CONTENT_TYPE_CONTACT);

		// Set the WPD_OBJECT_FORMAT to WPD_OBJECT_FORMAT_VCARD2 because we are creating/transferring contact content to the device. (This is
		// Version 2 of the VCARD file. If you have Version 3, use WPD_OBJECT_FORMAT_VCARD3 as the format)
		objectProperties.SetGuidValue(WPD_OBJECT_FORMAT, WPD_OBJECT_FORMAT_VCARD2);
	}

	// Fills out the required properties for specific WPD content types.
	static IPortableDeviceValues GetRequiredPropertiesForContentType(in Guid contentType, string parentObjectID, string filePath,
		[In] IStream fileStream)
	{
		// CoCreate an IPortableDeviceValues interface to hold the the object information
		IPortableDeviceValues objectPropertiesTemp = new();

		// Fill out required properties for ALL content types
		GetRequiredPropertiesForAllContentTypes(objectPropertiesTemp, parentObjectID, filePath, fileStream);

		// Fill out required properties for specific content types.
		// NOTE: If the content type is unknown to this function then only the required properties will be written. This is enough for
		// transferring most generic content types.
		if (contentType.Equals(WPD_CONTENT_TYPE_IMAGE))
		{
			GetRequiredPropertiesForImageContentTypes(objectPropertiesTemp);
		}
		else if (contentType.Equals(WPD_CONTENT_TYPE_AUDIO))
		{
			GetRequiredPropertiesForMusicContentTypes(objectPropertiesTemp);
		}
		else if (contentType.Equals(WPD_CONTENT_TYPE_CONTACT))
		{
			GetRequiredPropertiesForContactContentTypes(objectPropertiesTemp);
		}

		return objectPropertiesTemp;
	}

	// Transfers a user selected file to the device
	static void TransferContentToDevice([In] IPortableDevice device, in Guid contentType, string fileTypeFilter, string defaultFileExtension)
	{
		// Prompt user to enter an object identifier for the parent object on the device to transfer.
		Console.Write("Enter the identifier of the parent object which the file will be transferred under.\n>");
		string? selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Present the user with a File Open dialog. Our sample is restricting the types to user-specified forms.
		SafeLPTSTR filePath = new(MAX_PATH);
		SafeLPTSTR defFileExt = new(defaultFileExtension);
		OPENFILENAME openFileNameInfo = new()
		{
			lStructSize = (uint)Marshal.SizeOf(typeof(OPENFILENAME)),
			hwndOwner = default,
			lpstrFile = filePath,
			nMaxFile = (uint)filePath.Capacity,
			lpstrFilter = fileTypeFilter,
			nFilterIndex = 1,
			Flags = OFN.OFN_PATHMUSTEXIST | OFN.OFN_FILEMUSTEXIST,
			lpstrDefExt = defFileExt
		};

		Win32Error.ThrowLastErrorIfFalse(GetOpenFileName(ref openFileNameInfo), "The transfer operation was cancelled.\n");

		// 3) Open the image file and add required properties about the file being transferred

		// Open the selected file as an IStream. This will simplify reading the data and writing to the device.
		SHCreateStreamOnFileEx(filePath!, STGM.STGM_READ, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, false, default, out var fileStream).ThrowIfFailed();

		// Get the required properties needed to properly describe the data being transferred to the device.
		IPortableDeviceValues finalObjectProperties = GetRequiredPropertiesForContentType(contentType, // Content type of the data
				selection!, // Parent to transfer the data under
				filePath!, // Full file path to the data file
				fileStream); // Open IStream that contains the data

		// 4) Transfer for the content to the device
		uint optimalTransferSizeBytes = 0;
		content.CreateObjectWithPropertiesAndData(finalObjectProperties, // Properties describing the object data
			out var tempStream, // Returned object data stream (to transfer the data to)
			ref optimalTransferSizeBytes); // Returned optimal buffer size to use during transfer

		// Once we have a the IStream returned from CreateObjectWithPropertiesAndData, QI for IPortableDeviceDataStream so we can use the
		// additional methods to get more information about the object (i.e. The newly created object identifier on the device)
		IPortableDeviceDataStream finalObjectDataStream = (IPortableDeviceDataStream)tempStream;

		// Since we have IStream-compatible interfaces, call our helper function that copies the contents of a source stream into a
		// destination stream.
		int totalBytesWritten = StreamCopy(finalObjectDataStream, // Destination (The Object to transfer to)
			fileStream, // Source (The File data to transfer from)
			(int)optimalTransferSizeBytes); // The driver specified optimal transfer buffer size
		if (totalBytesWritten == 0)
		{
			Console.Write("! Failed to transfer object to device.");
		}

		// After transferring content to the device, the client is responsible for letting the driver know that the transfer is complete by
		// calling the Commit() method on the IPortableDeviceDataStream interface.
		finalObjectDataStream.Commit((int)STGC.STGC_DEFAULT);

		// Some clients may want to know the object identifier of the newly created object. This is done by calling GetObjectID() method on
		// the IPortableDeviceDataStream interface.
		string newlyCreatedObject = finalObjectDataStream.GetObjectID();
		Console.Write("The file '{0}' was transferred to the device.\nThe newly created object's ID is '{1}'\n", filePath, newlyCreatedObject);
	}

	// Transfers a user selected file to the device as a new WPD_RESOURCE_CONTACT_PHOTO resource
	static void CreateContactPhotoResourceOnDevice([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier for the object to which we will add a Resource.
		Console.Write("Enter the identifier of the object to which we will add a photograph.\n>");
		string? selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceResources interface from the IPortableDeviceContent to access the resource-specific methods.
		IPortableDeviceResources resources = content.Transfer();

		// 3) Present the user with a File Open dialog. Our sample is restricting the types to user-specified forms.
		SafeLPTSTR filePath = new(MAX_PATH);
		SafeLPTSTR defFileExt = new("JPG");
		OPENFILENAME openFileNameInfo = new()
		{
			lStructSize = (uint)Marshal.SizeOf(typeof(OPENFILENAME)),
			hwndOwner = default,
			lpstrFile = filePath,
			nMaxFile = (uint)filePath.Capacity,
			lpstrFilter = "JPEG (*.JPG)\ref 0 .JPG\0JPEG (*.JPEG)\ref 0 .JPEG\0JPG (*.JPE)\ref 0 .JPE\0JPG (*.JFIF)\ref 0 .JFIF\0\0",
			nFilterIndex = 1,
			Flags = OFN.OFN_PATHMUSTEXIST | OFN.OFN_FILEMUSTEXIST,
			lpstrDefExt = defFileExt
		};

		Win32Error.ThrowLastErrorIfFalse(GetOpenFileName(ref openFileNameInfo), "The transfer operation was cancelled.\n");

		// 4) Open the file and add required properties about the resource being transferred

		// Open the selected file as an IStream. This will simplify reading the data and writing to the device.
		SHCreateStreamOnFileEx(filePath!, STGM.STGM_READ, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, false, default, out var fileStream).ThrowIfFailed();

		// CoCreate the IPortableDeviceValues to hold the resource attributes
		IPortableDeviceValues resourceAttributes = new();

		// Fill in the necessary information regarding this resource

		// Set the WPD_OBJECT_ID. This informs the driver which object this request is intended for.
		resourceAttributes.SetStringValue(WPD_OBJECT_ID, selection!);

		// Set the WPD_RESOURCE_ATTRIBUTE_RESOURCE_KEY to WPD_RESOURCE_CONTACT_PHOTO
		resourceAttributes.SetKeyValue(WPD_RESOURCE_ATTRIBUTE_RESOURCE_KEY, WPD_RESOURCE_CONTACT_PHOTO);

		// Set the WPD_RESOURCE_ATTRIBUTE_TOTAL_SIZE by requesting the total size of the data stream.
		fileStream.Stat(out var statstg, (int)STATFLAG.STATFLAG_NONAME);
		resourceAttributes.SetUnsignedLargeIntegerValue(WPD_RESOURCE_ATTRIBUTE_TOTAL_SIZE, (ulong)statstg.cbSize);

		// Set the WPD_RESOURCE_ATTRIBUTE_FORMAT to WPD_OBJECT_FORMAT_EXIF because we are creating a contact photo resource with JPG image data.
		resourceAttributes.SetGuidValue(WPD_RESOURCE_ATTRIBUTE_FORMAT, WPD_OBJECT_FORMAT_EXIF);

		// 5) Transfer for the content to the device
		resources.CreateResource(resourceAttributes, // Properties describing this resource
			out var resourceStream, // Returned resource data stream (to transfer the data to)
			out var optimalTransferSizeBytes, // Returned optimal buffer size to use during transfer
			out _);

		// Since we have IStream-compatible interfaces, call our helper function that copies the contents of a source stream into a
		// destination stream.
		int totalBytesWritten = StreamCopy(resourceStream, // Destination (The resource to transfer to)
			fileStream, // Source (The File data to transfer from)
			(int)optimalTransferSizeBytes); // The driver specified optimal transfer buffer size

		// After transferring content to the device, the client is responsible for letting the driver know that the transfer is complete by
		// calling the Commit() method on the IPortableDeviceDataStream interface.
		resourceStream.Commit((int)STGC.STGC_DEFAULT);
	}

	// Fills out the required properties for a properties-only contact named "John Kane". This is a hard-coded contact.
	static IPortableDeviceValues GetRequiredPropertiesForPropertiesOnlyContact(string parentObjectID)
	{
		// CoCreate an IPortableDeviceValues interface to hold the the object information
		IPortableDeviceValues objectPropertiesTemp = new();

		// Set the WPD_OBJECT_PARENT_ID
		objectPropertiesTemp.SetStringValue(WPD_OBJECT_PARENT_ID, parentObjectID);

		// Set the WPD_OBJECT_NAME.
		objectPropertiesTemp.SetStringValue(WPD_OBJECT_NAME, "John Kane");

		// Set the WPD_OBJECT_CONTENT_TYPE to WPD_CONTENT_TYPE_CONTACT because we are creating contact content on the device.
		objectPropertiesTemp.SetGuidValue(WPD_OBJECT_CONTENT_TYPE, WPD_CONTENT_TYPE_CONTACT);

		// Set the WPD_CONTACT_DISPLAY_NAME to "John Kane"
		objectPropertiesTemp.SetStringValue(WPD_CONTACT_DISPLAY_NAME, "John Kane");

		// Set the WPD_CONTACT_PRIMARY_PHONE to "425-555-0123"
		objectPropertiesTemp.SetStringValue(WPD_CONTACT_PRIMARY_PHONE, "425-555-0123");

		return objectPropertiesTemp;
	}

	static IPortableDeviceValues GetRequiredPropertiesForFolder(string parentObjectID, string folderName)
	{
		// CoCreate an IPortableDeviceValues interface to hold the the object information
		IPortableDeviceValues objectPropertiesTemp = new();

		// Set the WPD_OBJECT_PARENT_ID
		objectPropertiesTemp.SetStringValue(WPD_OBJECT_PARENT_ID, parentObjectID);

		// Set the WPD_OBJECT_NAME.
		objectPropertiesTemp.SetStringValue(WPD_OBJECT_NAME, folderName);

		// Set the WPD_OBJECT_CONTENT_TYPE to WPD_CONTENT_TYPE_FOLDER because we are creating contact content on the device.
		objectPropertiesTemp.SetGuidValue(WPD_OBJECT_CONTENT_TYPE, WPD_CONTENT_TYPE_FOLDER);

		return objectPropertiesTemp;
	}

	// Creates a properties-only object on the device which is WPD_CONTENT_TYPE_CONTACT specific.
	// NOTE: This function creates a hard-coded contact for "John Kane" always.
	static void TransferContactToDevice([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier for the parent object on the device to transfer.
		Console.Write("Enter the identifier of the parent object which the contact will be transferred under.\n>");
		string? selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get the properties that describe the object being created on the device
		IPortableDeviceValues finalObjectProperties = GetRequiredPropertiesForPropertiesOnlyContact(selection!); // Parent to transfer the data under

		// 3) Transfer the content to the device by creating a properties-only object
		string newlyCreatedObject = content.CreateObjectWithPropertiesOnly(finalObjectProperties); // Properties describing the object data
		Console.Write("The contact was transferred to the device.\nThe newly created object's ID is '{0}'\n", newlyCreatedObject);
	}

	// Creates a properties-only object on the device which is WPD_CONTENT_TYPE_FOLDER specific.
	static void CreateFolderOnDevice([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier for the parent object on the device to transfer.
		Console.Write("Enter the identifier of the parent object which the folder will be created under.\n>");
		string? selection = Console.ReadLine();

		// Prompt user to enter an object identifier for the parent object on the device to transfer.
		Console.Write("Enter the name of the the folder to create.\n>");
		string? folderName = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get the properties that describe the object being created on the device
		IPortableDeviceValues finalObjectProperties = GetRequiredPropertiesForFolder(selection!, // Parent to create the folder under
			folderName!); // Folder Name

		// 3) Transfer the content to the device by creating a properties-only object
		string newlyCreatedObject = content.CreateObjectWithPropertiesOnly(finalObjectProperties); // Properties describing the object data
		Console.Write("The folder was created on the device.\nThe newly created object's ID is '{0}'\n", newlyCreatedObject);
	}

	// Fills out properties that accompany updating an object's data...
	static IPortableDeviceValues GetPropertiesForUpdateData(string filePath, [In] IStream fileStream)
	{
		// CoCreate an IPortableDeviceValues interface to hold the the object information
		IPortableDeviceValues objectPropertiesTemp = new();

		// Set the WPD_OBJECT_SIZE by requesting the total size of the data stream.
		fileStream.Stat(out var statstg, (int)STATFLAG.STATFLAG_NONAME);
		objectPropertiesTemp.SetUnsignedLargeIntegerValue(WPD_OBJECT_SIZE, (ulong)statstg.cbSize);

		// Set the WPD_OBJECT_ORIGINAL_FILE_NAME by splitting the file path into a separate filename.
		string originalFileName = Path.GetFileName(filePath);
		objectPropertiesTemp.SetStringValue(WPD_OBJECT_ORIGINAL_FILE_NAME, originalFileName);

		// Set the WPD_OBJECT_NAME. We are using the file name without its file extension in this example for the object's name. The object
		// name could be a more friendly name like "This Cool Song" or "That Cool Picture".
		objectPropertiesTemp.SetStringValue(WPD_OBJECT_NAME, Path.GetFileNameWithoutExtension(filePath));

		return objectPropertiesTemp;
	}

	// Updates a selected object's properties and data (WPD_RESOURCE_DEFAULT).
	static void UpdateContentOnDevice([In] IPortableDevice device, in Guid contentType, string fileTypeFilter, string defaultFileExtension)
	{
		// Prompt user to enter an object identifier for the object on the device to update.
		Console.Write("Enter the identifier of the object to update.\n>");
		string? selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent2 interface from the IPortableDevice interface to access the UpdateObjectWithPropertiesAndData method.
		IPortableDeviceContent content = device.Content();
		IPortableDeviceContent2 content2 = (IPortableDeviceContent2)content;

		// 2) (Optional) Check if the object is of the correct content type. This also ensures the user-specified object ID is valid.
		IPortableDeviceProperties properties = content2.Properties();
		IPortableDeviceValues objectProperties = properties.GetValues(selection!, default);
		Guid objectContentType = objectProperties.GetGuidValue(WPD_OBJECT_CONTENT_TYPE);
		if (objectContentType != contentType)
			throw new ArgumentException($"! Object ({selection}) is not of the correct content type.", nameof(contentType));

		// 3) Present the user with a File Open dialog. Our sample is restricting the types to user-specified forms.
		SafeLPTSTR filePath = new(MAX_PATH);
		SafeLPTSTR defFileExt = new(defaultFileExtension);
		OPENFILENAME openFileNameInfo = new()
		{
			lStructSize = (uint)Marshal.SizeOf(typeof(OPENFILENAME)),
			hwndOwner = default,
			lpstrFile = filePath,
			nMaxFile = (uint)filePath.Capacity,
			lpstrFilter = fileTypeFilter,
			nFilterIndex = 1,
			Flags = OFN.OFN_PATHMUSTEXIST | OFN.OFN_FILEMUSTEXIST,
			lpstrDefExt = defFileExt
		};

		Win32Error.ThrowLastErrorIfFalse(GetOpenFileName(ref openFileNameInfo), "The transfer operation was cancelled.\n");

		// 4) Open the file and add required properties about the file being transferred

		// Open the selected file as an IStream. This will simplify reading the data and writing to the device.
		SHCreateStreamOnFileEx(filePath!, STGM.STGM_READ, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL, false, default, out var fileStream).ThrowIfFailed();

		// Get the required properties needed to properly describe the data being transferred to the device.
		IPortableDeviceValues finalObjectProperties = GetPropertiesForUpdateData(filePath!, // Full file path to the data file
			fileStream); // Open IStream that contains the data

		// 5) Transfer for the content to the device
		content2.UpdateObjectWithPropertiesAndData(selection!,
			finalObjectProperties, // Properties describing the object data
			out var tempStream, // Returned object data stream (to transfer the data to)
			out var optimalTransferSizeBytes); // Returned optimal buffer size to use during transfer

		// Once we have a the IStream returned from UpdateObjectWithPropertiesAndData, QI for IPortableDeviceDataStream so we can use the
		// additional methods to get more information about the object (i.e. The newly created object identifier on the device)
		IPortableDeviceDataStream finalObjectDataStream = (IPortableDeviceDataStream)tempStream;

		// Since we have IStream-compatible interfaces, call our helper function that copies the contents of a source stream into a
		// destination stream.
		int totalBytesWritten = StreamCopy(finalObjectDataStream, // Destination (The resource to transfer to)
			fileStream, // Source (The File data to transfer from)
			(int)optimalTransferSizeBytes); // The driver specified optimal transfer buffer size

		// After transferring content to the device, the client is responsible for letting the driver know that the transfer is complete by
		// calling the Commit() method on the IPortableDeviceDataStream interface.
		finalObjectDataStream.Commit((int)STGC.STGC_DEFAULT);
	}
}