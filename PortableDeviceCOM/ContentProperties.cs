static partial class Program
{
	static SafeEventHandle? g_bulkPropertyOperationEvent = null;

	// Displays a property assumed to be in error code form.
	static void DisplayErrorResultProperty([In] IPortableDeviceValues properties, in PROPERTYKEY key, string keyName)
	{
		HRESULT error = properties.GetErrorValue(key);
		Console.Write("{0}: HRESULT = (0x{1:X})\n", keyName, (int)error);
	}

	// Displays a property assumed to be in string form.
	static void DisplayStringProperty([In] IPortableDeviceValues properties, in PROPERTYKEY key, string keyName)
	{
		string? value = properties.GetStringValue(key);
		// Get the length of the string value so we can output <empty string value> if one is encountered.
		if (!string.IsNullOrEmpty(value))
		{
			Console.Write("{0}: {1}\n", keyName, value);
		}
		else
		{
			Console.Write("{0}: <empty string value>\n", keyName);
		}
	}

	// Displays a property assumed to be in Guid form.
	static void DisplayGuidProperty([In] IPortableDeviceValues properties, in PROPERTYKEY key, string keyName)
	{
		Guid value = properties.GetGuidValue(key);
		Console.Write("{0}: {1}\n", keyName, value);
	}

	// Reads properties for the user specified object.
	static void ReadContentProperties([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier on the device to read properties from.
		Console.Write("Enter the identifier of the object you wish to read properties from.\n>");
		var selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceProperties interface from the IPortableDeviceContent interface to access the property-specific methods.
		IPortableDeviceProperties properties = content.Properties();

		// 3) CoCreate an IPortableDeviceKeyCollection interface to hold the the property keys we wish to read.
		IPortableDeviceKeyCollection propertiesToRead = new();

		// 4) Populate the IPortableDeviceKeyCollection with the keys we wish to read.
		// NOTE: We are not handling any special error cases here so we can proceed with adding as many of the target properties as we can.
		propertiesToRead.Add(WPD_OBJECT_PARENT_ID);
		propertiesToRead.Add(WPD_OBJECT_NAME);
		propertiesToRead.Add(WPD_OBJECT_PERSISTENT_UNIQUE_ID);
		propertiesToRead.Add(WPD_OBJECT_FORMAT);
		propertiesToRead.Add(WPD_OBJECT_CONTENT_TYPE);

		// 5) Call GetValues() passing the collection of specified PROPERTYKEYs.
		IPortableDeviceValues objectProperties = properties.GetValues(
			selection ?? "", // The object whose properties we are reading
			propertiesToRead); // The properties we want to read

		// 6) Display the returned property values to the user
		DisplayStringProperty(objectProperties, WPD_OBJECT_PARENT_ID, "WPD_OBJECT_PARENT_ID");
		DisplayStringProperty(objectProperties, WPD_OBJECT_NAME, "WPD_OBJECT_NAME");
		DisplayStringProperty(objectProperties, WPD_OBJECT_PERSISTENT_UNIQUE_ID, "WPD_OBJECT_PERSISTENT_UNIQUE_ID");
		DisplayGuidProperty(objectProperties, WPD_OBJECT_CONTENT_TYPE, "WPD_OBJECT_CONTENT_TYPE");
		DisplayGuidProperty(objectProperties, WPD_OBJECT_FORMAT, "WPD_OBJECT_FORMAT");
	}

	// Writes properties on the user specified object.
	static void WriteContentProperties([In] IPortableDevice device)
	{
		// Prompt user to enter an object identifier on the device to write properties on.
		Console.Write("Enter the identifier of the object you wish to write properties on.\n>");
		var selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceProperties interface from the IPortableDeviceContent interface to access the property-specific methods.
		IPortableDeviceProperties properties = content.Properties();

		// 3) Check the property attributes to see if we can write/change the WPD_OBJECT_NAME property.
		IPortableDeviceValues attributes = properties.GetPropertyAttributes(selection!, WPD_OBJECT_NAME);
		bool canWrite = attributes.GetBoolValue(WPD_PROPERTY_ATTRIBUTE_CAN_WRITE);
		if (canWrite)
		{
			Console.Write("The attribute WPD_PROPERTY_ATTRIBUTE_CAN_WRITE for the WPD_OBJECT_NAME reports true\nThis means that the property can be changed/updated\n\n");
		}
		else
		{
			Console.Write("The attribute WPD_PROPERTY_ATTRIBUTE_CAN_WRITE for the WPD_OBJECT_NAME reports false\nThis means that the property cannot be changed/updated\n\n");
		}

		// 4) Prompt the user for the new value of the WPD_OBJECT_NAME property only if the property attributes report that it can be changed/updated.
		if (canWrite)
		{
			Console.Write("Enter the new WPD_OBJECT_NAME for the object '{0}'.\n>", selection);
			var newObjectName = Console.ReadLine();

			// 5) CoCreate an IPortableDeviceValues interface to hold the the property values we wish to write.
			IPortableDeviceValues objectPropertiesToWrite = new();
			objectPropertiesToWrite.SetStringValue(WPD_OBJECT_NAME, newObjectName!);

			// 6) Call SetValues() passing the collection of specified PROPERTYKEYs.
			IPortableDeviceValues propertyWriteResults = properties.SetValues(
				selection!, // The object whose properties we are reading
				objectPropertiesToWrite); // The properties we want to read
			Console.Write("The WPD_OBJECT_NAME property on object '{0}' was written successfully (Read the properties again to see the updated value)\n", selection);
		}
	}

	// Retreives the object identifier for the persistent unique identifier
	static void GetObjectIdentifierFromPersistentUniqueIdentifier([In] IPortableDevice device)
	{
		// Prompt user to enter an unique identifier to convert to an object idenifier.
		Console.Write("Enter the Persistant Unique Identifier of the object you wish to convert into an object identifier.\n>");
		var selection = Console.ReadLine();

		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) CoCreate an IPortableDevicePropVariantCollection interface to hold the the Unique Identifiers to query for Object Identifiers.
		//
		// NOTE: This is a collection interface so more than 1 identifier can be requested at a time. This sample only requests a single
		// unique identifier.
		IPortableDevicePropVariantCollection persistentUniqueIDs = new();

		// Initialize a PROPVARIANT structure with the persistent unique identifier string that the user selected above. This memory will be
		// freed when PropVariantClear() is called below.
		using PROPVARIANT persistentUniqueID = new(selection);
		// Add the object identifier to the objects-to-delete list (We are only deleting 1 in this example)
		persistentUniqueIDs.Add(persistentUniqueID);

		// 3) Attempt to get the unique idenifier for the object from the device
		IPortableDevicePropVariantCollection objectIDs = content.GetObjectIDsFromPersistentUniqueIDs(persistentUniqueIDs);
		using PROPVARIANT objectID = new();
		objectIDs.GetAt(0, objectID);
		Console.Write("The persistent unique identifier '{0}' relates to object identifier '{1}' on the device.\n", selection, objectID.pwszVal);
	}

	// IPortableDevicePropertiesBulkCallback implementation for use with IPortableDevicePropertiesBulk operations.
	class CGetBulkValuesCallback : IPortableDevicePropertiesBulkCallback
	{
		HRESULT IPortableDevicePropertiesBulkCallback.OnStart(in Guid pContext)
		{
			Console.Write("** BULK Property operation starting, context = {0} **\n", pContext);
			return HRESULT.S_OK;
		}

		HRESULT IPortableDevicePropertiesBulkCallback.OnProgress(in Guid pContext, IPortableDeviceValuesCollection values)
		{
			uint numValues = values.GetCount();

			// Display the returned properties to the user.
			// NOTE: We are reading for expected properties, which were setup in the QueueGetXXXXXX bulk operation call.
			Console.Write("Received next batch of {0} object value elements..., context = {1}\n", numValues, pContext);

			for (uint index = 0; index < numValues; index++)
			{
				IPortableDeviceValues objectProperties = values.GetAt(index);
				DisplayStringProperty(objectProperties, WPD_OBJECT_PARENT_ID, "WPD_OBJECT_PARENT_ID");
				DisplayStringProperty(objectProperties, WPD_OBJECT_NAME, "WPD_OBJECT_NAME");
				DisplayStringProperty(objectProperties, WPD_OBJECT_PERSISTENT_UNIQUE_ID, "WPD_OBJECT_PERSISTENT_UNIQUE_ID");
				DisplayGuidProperty(objectProperties, WPD_OBJECT_CONTENT_TYPE, "WPD_OBJECT_CONTENT_TYPE");
				DisplayGuidProperty(objectProperties, WPD_OBJECT_FORMAT, "WPD_OBJECT_FORMAT");
				Console.Write("\n\n");
			}
			return HRESULT.S_OK;
		}

		void IPortableDevicePropertiesBulkCallback.OnEnd(in Guid pContext, HRESULT hrStatus)
		{
			Console.Write("** BULK Property operation ending, status = 0x{0:X}, context = {1} **\n", (int)hrStatus, pContext);

			// This assumes that we are only performing a single operation at a time, so no check is needed on the context when setting the
			// operation complete event.
			g_bulkPropertyOperationEvent?.Set();
		}
	}

	// IPortableDevicePropertiesBulkCallback implementation for use with IPortableDevicePropertiesBulk operations.
	class CSetBulkValuesCallback : IPortableDevicePropertiesBulkCallback
	{
		HRESULT IPortableDevicePropertiesBulkCallback.OnStart(in Guid pContext)
		{
			Console.Write("** BULK Property operation starting, context = {0} **\n", pContext);
			return HRESULT.S_OK;
		}

		HRESULT IPortableDevicePropertiesBulkCallback.OnProgress(in Guid pContext, IPortableDeviceValuesCollection values)
		{
			uint numValues = values.GetCount();

			// Display the returned properties set operation results to the user.
			// NOTE: We are reading for expected properties, which were setup in the QueueSetXXXXXX bulk operation call. The values returned
			// are in the form VT_ERROR holding the HRESULT for the set operation.
			Console.Write("Received next batch of {0} object value elements..., context = {1}\n", numValues, pContext);

			for (uint index = 0; index < numValues; index++)
			{
				IPortableDeviceValues objectProperties = values.GetAt(index);
				DisplayStringProperty(objectProperties, WPD_OBJECT_ID, "WPD_OBJECT_ID");
				DisplayErrorResultProperty(objectProperties, WPD_OBJECT_NAME, "WPD_OBJECT_NAME");
				Console.Write("\n\n");
			}
			return HRESULT.S_OK;
		}

		void IPortableDevicePropertiesBulkCallback.OnEnd(in Guid pContext, HRESULT hrStatus)
		{
			Console.Write("** BULK Property operation ending, status = 0x{0:X}, context = {1} **\n", (int)hrStatus, pContext);

			// This assumes that we are only performing a single operation at a time, so no check is needed on the context when setting the
			// operation complete event.
			g_bulkPropertyOperationEvent?.Set();
		}
	}

	// Reads a set of properties for all objects.
	static void ReadContentPropertiesBulk([In] IPortableDevice device)
	{
		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceProperties interface from the IPortableDeviceContent interface to access the property-specific methods.
		IPortableDeviceProperties properties = content.Properties();

		// 3) Check to see if the driver supports BULK property operations by call QueryInterface on the IPortableDeviceProperties interface
		// for IPortableDevicePropertiesBulk
		IPortableDevicePropertiesBulk propertiesBulk = (IPortableDevicePropertiesBulk)properties;

		// 4) CoCreate an IPortableDeviceKeyCollection interface to hold the the property keys we wish to read.
		IPortableDeviceKeyCollection propertiesToRead = new();

		// 5) Populate the IPortableDeviceKeyCollection with the keys we wish to read.
		// NOTE: We are not handling any special error cases here so we can proceed with adding as many of the target properties as we can.
		propertiesToRead.Add(WPD_OBJECT_PARENT_ID);
		propertiesToRead.Add(WPD_OBJECT_NAME);
		propertiesToRead.Add(WPD_OBJECT_PERSISTENT_UNIQUE_ID);
		propertiesToRead.Add(WPD_OBJECT_FORMAT);
		propertiesToRead.Add(WPD_OBJECT_CONTENT_TYPE);

		// 6) Create an instance of the IPortableDevicePropertiesBulkCallback object.
		CGetBulkValuesCallback callback = new();

		// 7) Call our helper function CreateIPortableDevicePropVariantCollectionWithAllObjectIDs to enumerate and create an
		// IPortableDevicePropVariantCollection with the object identifiers needed to perform the bulk operation on.
		IPortableDevicePropVariantCollection objectIDs = CreateIPortableDevicePropVariantCollectionWithAllObjectIDs(content);

		// 8) Call QueueGetValuesByObjectList to initialize the Asynchronous property operation.
		propertiesBulk.QueueGetValuesByObjectList(objectIDs, propertiesToRead, callback, out var context);

		// 9) Call Start() to actually begin the property operation

		// In order to create a simpler to follow example we create and wait infinitly for the bulk property operation to complete and ignore
		// any errors. Production code should be written in a more robust manner. Create the global event handle to wait on for the bulk
		// operation to complete.
		g_bulkPropertyOperationEvent = CreateEvent(default, false, false, default);
		if (!g_bulkPropertyOperationEvent.IsNull)
		{
			// Call Start() to actually begin the Asynchronous bulk operation.
			propertiesBulk.Start(context);
		}
		else
		{
			Console.Write("! Failed to create the global event handle to wait on for the bulk operation. Aborting operation.\n");
		}

		// In order to create a simpler to follow example we will wait infinitly for the operation to complete and ignore any errors.
		// Production code should be written in a more robust manner.
		g_bulkPropertyOperationEvent?.Wait();

		// Cleanup any created global event handles before exiting..
		g_bulkPropertyOperationEvent = null;
	}

	// Writes a set of properties for all objects.
	static void WriteContentPropertiesBulk(IPortableDevice device)
	{
		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceProperties interface from the IPortableDeviceContent interface to access the property-specific methods.
		IPortableDeviceProperties properties = content.Properties();

		// 3) Check to see if the driver supports BULK property operations by call QueryInterface on the IPortableDeviceProperties interface
		// for IPortableDevicePropertiesBulk
		IPortableDevicePropertiesBulk propertiesBulk = (IPortableDevicePropertiesBulk)properties;

		// 4) CoCreate an IPortableDeviceValuesCollection interface to hold the the properties we wish to write.
		IPortableDeviceValuesCollection propertiesToWrite = new();

		// 6) Create an instance of the IPortableDevicePropertiesBulkCallback object.
		CSetBulkValuesCallback callback = new();

		// 7) Call our helper function CreateIPortableDevicePropVariantCollectionWithAllObjectIDs to enumerate and create an
		// IPortableDevicePropVariantCollection with the object identifiers needed to perform the bulk operation on.
		IPortableDevicePropVariantCollection objectIDs = CreateIPortableDevicePropVariantCollectionWithAllObjectIDs(content);

		uint numObjectIDs = objectIDs.GetCount();

		// 8) Iterate through object list and add appropriate IPortableDeviceValues to collection
		for (uint index = 0; (index < numObjectIDs); index++)
		{
			IPortableDeviceValues newvalues = new();

			// Get the Object ID whose properties we will set
			using PROPVARIANT objectID = new();
			objectIDs.GetAt(index, objectID);

			// Save them into the IPortableDeviceValues so the driver knows which object this proeprty set belongs to
			newvalues.SetStringValue(WPD_OBJECT_ID, objectID.pwszVal!);

			// Set the new values. In this sample, we attempt to set the name property.
			string newName = $"NewName{index}";
			newvalues.SetStringValue(WPD_OBJECT_NAME, newName);

			// Add this property set to the collection
			propertiesToWrite.Add(newvalues);
		}

		// 9) Call QueueSetValuesByObjectList to initialize the Asynchronous property operation.
		propertiesBulk.QueueSetValuesByObjectList(propertiesToWrite, callback, out var context);

		// 10) Call Start() to actually begin the property operation

		// In order to create a simpler to follow example we create and wait infinitly for the bulk property operation to complete and ignore
		// any errors. Production code should be written in a more robust manner. Create the global event handle to wait on for the bulk
		// operation to complete.
		g_bulkPropertyOperationEvent = CreateEvent(default, false, false, default);
		if (!g_bulkPropertyOperationEvent.IsNull)
		{
			// Call Start() to actually begin the Asynchronous bulk operation.
			propertiesBulk.Start(context);
		}
		else
		{
			Console.Write("! Failed to create the global event handle to wait on for the bulk operation. Aborting operation.\n");
		}

		// In order to create a simpler to follow example we will wait infinitly for the operation to complete and ignore any errors.
		// Production code should be written in a more robust manner.
		g_bulkPropertyOperationEvent?.Wait();

		// Cleanup any created global event handles before exiting..
		g_bulkPropertyOperationEvent = default;
	}

	// Reads a set of properties for all objects of a particular format.
	static void ReadContentPropertiesBulkFilteringByFormat([In] IPortableDevice device)
	{
		// 1) Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// 2) Get an IPortableDeviceProperties interface from the IPortableDeviceContent interface to access the property-specific methods.
		IPortableDeviceProperties properties = content.Properties();

		// 3) Check to see if the driver supports BULK property operations by call QueryInterface on the IPortableDeviceProperties interface
		// for IPortableDevicePropertiesBulk
		IPortableDevicePropertiesBulk propertiesBulk = (IPortableDevicePropertiesBulk)properties;

		// 4) CoCreate an IPortableDeviceKeyCollection interface to hold the the property keys we wish to read.
		IPortableDeviceKeyCollection propertiesToRead = new();

		// 5) Populate the IPortableDeviceKeyCollection with the keys we wish to read.
		// NOTE: We are not handling any special error cases here so we can proceed with adding as many of the target properties as we can.
		propertiesToRead.Add(WPD_OBJECT_PARENT_ID);
		propertiesToRead.Add(WPD_OBJECT_NAME);
		propertiesToRead.Add(WPD_OBJECT_PERSISTENT_UNIQUE_ID);
		propertiesToRead.Add(WPD_OBJECT_FORMAT);
		propertiesToRead.Add(WPD_OBJECT_CONTENT_TYPE);

		// 6) Create an instance of the IPortableDevicePropertiesBulkCallback object.
		CGetBulkValuesCallback callback = new();

		// 7) Call QueueGetValuesByObjectFormat to initialize the Asynchronous property operation.
		const uint DEPTH = 100;
		propertiesBulk.QueueGetValuesByObjectFormat(WPD_OBJECT_FORMAT_MP3,
			WPD_DEVICE_OBJECT_ID,
			DEPTH,
			propertiesToRead,
			callback,
			out var context);

		// 9) Call Start() to actually begin the property operation

		// In order to create a simpler to follow example we create and wait infinitly for the bulk property operation to complete and ignore
		// any errors. Production code should be written in a more robust manner. Create the global event handle to wait on for the bulk
		// operation to complete.
		g_bulkPropertyOperationEvent = CreateEvent(default, false, false, default);
		if (!g_bulkPropertyOperationEvent.IsNull)
		{
			// Call Start() to actually begin the Asynchronous bulk operation.
			propertiesBulk.Start(context);
		}
		else
		{
			Console.Write("! Failed to create the global event handle to wait on for the bulk operation. Aborting operation.\n");
		}

		// In order to create a simpler to follow example we will wait infinitly for the operation to complete and ignore any errors.
		// Production code should be written in a more robust manner.
		g_bulkPropertyOperationEvent?.Wait();

		// Cleanup any created global event handles before exiting..
		g_bulkPropertyOperationEvent = default;
	}
}