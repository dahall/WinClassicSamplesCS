using System.Diagnostics.CodeAnalysis;

static partial class Program
{
	// This number controls how many object identifiers are requested during each call to IEnumPortableDeviceObjectIDs::Next()
	const int NUM_OBJECTS_TO_REQUEST = 10;

	// Recursively called function which enumerates using the specified object identifier as the parent.
	static void RecursiveEnumerate(string objectID, [In] IPortableDeviceContent content)
	{
		// Print the object identifier being used as the parent during enumeration.
		Console.Write("{0}\n", objectID);

		// Get an IEnumPortableDeviceObjectIDs interface by calling EnumObjects with the specified parent object identifier.
		IEnumPortableDeviceObjectIDs enumObjectIDs = content.EnumObjects(0, // Flags are unused
			objectID, // Starting from the passed in object
			default); // Filter is unused

		// Loop calling Next() while HRESULT.S_OK is being returned.
		HRESULT hr;
		do
		{
			string[] objectIDArray = new string[NUM_OBJECTS_TO_REQUEST];
			hr = enumObjectIDs.Next(NUM_OBJECTS_TO_REQUEST, // Number of objects to request on each NEXT call
				objectIDArray, // Array of string array which will be populated on each NEXT call
				out var numFetched); // Number of objects written to the string array
			if (hr.Succeeded)
			{
				// Traverse the results of the Next() operation and recursively enumerate Remember to free all returned object identifiers
				// using CoTaskMemFree()
				for (uint index = 0; (index < numFetched) && (objectIDArray[index] != default); index++)
				{
					RecursiveEnumerate(objectIDArray[index], content);
				}
			}
		} while (hr == HRESULT.S_OK);
	}

	// Enumerate all content on the device starting with the "DEVICE" object
	static void EnumerateAllContent([In] IPortableDevice device)
	{
		// Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// Enumerate content starting from the "DEVICE" object.
		Console.Write("\n");
		RecursiveEnumerate(WPD_DEVICE_OBJECT_ID, content);
	}

	// Recursively called function which enumerates using the specified object identifier as the parent and populates the returned object
	// identifiers into an IPortableDevicePropVariantCollection object.
	static void RecursiveEnumerateAndCopyToCollection(string objectID, [In] IPortableDeviceContent content, [In] IPortableDevicePropVariantCollection objectIDs)
	{
		// Add the object identifier being used as the parent during enumeration to the collection.
		using PROPVARIANT pv = new(objectID);
		{
			// Add the object identifier...
			objectIDs.Add(pv);
		}

		// Get an IEnumPortableDeviceObjectIDs interface by calling EnumObjects with the specified parent object identifier.
		IEnumPortableDeviceObjectIDs enumObjectIDs = content.EnumObjects(0, // Flags are unused
			objectID, // Starting from the passed in object
			default); // Filter is unused

		// Loop calling Next() while HRESULT. HRESULT. HRESULT. HRESULT.S_OK is being returned.
		HRESULT hr;
		do
		{
			string[] objectIDArray = new string[NUM_OBJECTS_TO_REQUEST];
			hr = enumObjectIDs.Next(NUM_OBJECTS_TO_REQUEST, // Number of objects to request on each Next() call
				objectIDArray, // string array which will be populated on each Next() call
				out var numFetched); // Number of objects written to the string array
			if (hr.Succeeded)
			{
				// Traverse the results of the Next() operation and recursively enumerate Remember to free all returned object identifiers
				// using CoTaskMemFree()
				for (uint index = 0; (index < numFetched) && (objectIDArray[index] != default); index++)
				{
					RecursiveEnumerateAndCopyToCollection(objectIDArray[index], content, objectIDs);
				}
			}
		} while (hr == HRESULT.S_OK);
	}

	// Enumerate all content on the device starting with the "DEVICE" object and populates the returned object identifiers into an IPortableDevicePropVariantCollection
	static IPortableDevicePropVariantCollection CreateIPortableDevicePropVariantCollectionWithAllObjectIDs([In] IPortableDeviceContent content)
	{
		// CoCreate an IPortableDevicePropVariantCollection interface to hold the the object identifiers
		IPortableDevicePropVariantCollection objectIDsTemp = new();
		RecursiveEnumerateAndCopyToCollection(WPD_DEVICE_OBJECT_ID, content, objectIDsTemp);
		return objectIDsTemp;
	}

	static bool SendHintsCommand([In] IPortableDevice device, in Guid contentType, [NotNullWhen(true)] out IPortableDeviceValues? results)
	{
		results = default;
		try
		{
			// Create and initialize the command parameters
			IPortableDeviceValues vals = new();
			vals.SetGuidValue(WPD_PROPERTY_COMMON_COMMAND_CATEGORY, WPD_COMMAND_DEVICE_HINTS_GET_CONTENT_LOCATION.fmtid);
			vals.SetUnsignedIntegerValue(WPD_PROPERTY_COMMON_COMMAND_ID, WPD_COMMAND_DEVICE_HINTS_GET_CONTENT_LOCATION.pid);
			vals.SetGuidValue(WPD_PROPERTY_DEVICE_HINTS_CONTENT_TYPE, contentType);

			// Send the command
			results = device.SendCommand(0, vals);
			return true;
		}
		catch { return false; }
	}

	static void ReadHintsResults([In] IPortableDeviceProperties properties, [In] IPortableDeviceValues results)
	{
		// Get the collection
		IPortableDevicePropVariantCollection folderIDs = results.GetIPortableDevicePropVariantCollectionValue(WPD_PROPERTY_DEVICE_HINTS_CONTENT_LOCATIONS);

		// Get the count of folders
		uint numFolderIDs = folderIDs.GetCount();
		// Loop through each of the folders
		for (uint index = 0; index < numFolderIDs; ++index)
		{
			// Get the folder id
			using PROPVARIANT folderID = new();

			folderIDs.GetAt(index, folderID);
			if (folderID.vt == VARTYPE.VT_LPWSTR)
			{
				// Get the properties for this item
				IPortableDeviceValues folderProperties = properties.GetValues(folderID.pwszVal!, default);
				// Get the persistent unique object id
				string folderPersistentUniqueID = folderProperties.GetStringValue(WPD_OBJECT_PERSISTENT_UNIQUE_ID);
				Console.Write(" '{0}' ({1})\n", folderID.pwszVal, folderPersistentUniqueID);
			}
			else
			{
				Console.Write("Driver returned unexpected PROVARIANT Type: {0}\n", folderID.vt);
			}
		}
	}

	static void ReadHintLocations([In] IPortableDevice device)
	{
		// Get the device content
		IPortableDeviceContent content = device.Content();
		// Get the device properties
		IPortableDeviceProperties properties = content.Properties();
		// Loop through some typical content types supported by Portable Devices
		Guid[] formatTypes = [WPD_CONTENT_TYPE_IMAGE, WPD_CONTENT_TYPE_VIDEO];
		string[] formatTypeStrings = ["WPD_CONTENT_TYPE_IMAGE", "WPD_CONTENT_TYPE_VIDEO"];
		for (uint index = 0; index < formatTypes.Length; ++index)
		{
			Console.Write("Folders for content type '{0}':\n", formatTypeStrings[index]);
			if (SendHintsCommand(device, formatTypes[index], out var results))
			{
				ReadHintsResults(properties, results);
			}
		}
	}
}