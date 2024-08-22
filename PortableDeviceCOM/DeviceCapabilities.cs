static partial class Program
{
	// Displays a friendly name for a passed in functional category. If the category is not known by this function the Guid will be displayed
	// in string form.
	static void DisplayFunctionalCategory(in Guid functionalCategory)
	{
		if (WPD_FUNCTIONAL_CATEGORY_STORAGE.Equals(functionalCategory))
		{
			Console.Write("WPD_FUNCTIONAL_CATEGORY_STORAGE");
		}
		else if (WPD_FUNCTIONAL_CATEGORY_STILL_IMAGE_CAPTURE.Equals(functionalCategory))
		{
			Console.Write("WPD_FUNCTIONAL_CATEGORY_STILL_IMAGE_CAPTURE");
		}
		else if (WPD_FUNCTIONAL_CATEGORY_AUDIO_CAPTURE.Equals(functionalCategory))
		{
			Console.Write("WPD_FUNCTIONAL_CATEGORY_AUDIO_CAPTURE");
		}
		else if (WPD_FUNCTIONAL_CATEGORY_SMS.Equals(functionalCategory))
		{
			Console.Write("WPD_FUNCTIONAL_CATEGORY_SMS");
		}
		else if (WPD_FUNCTIONAL_CATEGORY_RENDERING_INFORMATION.Equals(functionalCategory))
		{
			Console.Write("WPD_FUNCTIONAL_CATEGORY_RENDERING_INFORMATION");
		}
		else
		{
			Console.Write(functionalCategory);
		}
	}

	// Displays a friendly name for a passed in evt If the evt is not known by this function the Guid will be displayed in string form.
	static void DisplayEvent(in Guid evt)
	{
		if (WPD_EVENT_OBJECT_ADDED.Equals(evt))
		{
			Console.Write("WPD_EVENT_OBJECT_ADDED");
		}
		else if (WPD_EVENT_OBJECT_REMOVED.Equals(evt))
		{
			Console.Write("WPD_EVENT_OBJECT_REMOVED");
		}
		else if (WPD_EVENT_OBJECT_UPDATED.Equals(evt))
		{
			Console.Write("WPD_EVENT_OBJECT_UPDATED");
		}
		else if (WPD_EVENT_DEVICE_RESET.Equals(evt))
		{
			Console.Write("WPD_EVENT_DEVICE_RESET");
		}
		else if (WPD_EVENT_DEVICE_CAPABILITIES_UPDATED.Equals(evt))
		{
			Console.Write("WPD_EVENT_DEVICE_CAPABILITIES_UPDATED");
		}
		else if (WPD_EVENT_STORAGE_FORMAT.Equals(evt))
		{
			Console.Write("WPD_EVENT_STORAGE_FORMAT");
		}
		else
		{
			Console.Write(evt);
		}
	}

	// Displays a friendly name for a passed in content type If the content type is not known by this function the Guid will be displayed in
	// string form.
	static void DisplayContentType(in Guid contentType)
	{
		if (WPD_CONTENT_TYPE_FUNCTIONAL_OBJECT.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_FUNCTIONAL_OBJECT");
		}
		else if (WPD_CONTENT_TYPE_FOLDER.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_FOLDER");
		}
		else if (WPD_CONTENT_TYPE_IMAGE.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_IMAGE");
		}
		else if (WPD_CONTENT_TYPE_DOCUMENT.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_DOCUMENT");
		}
		else if (WPD_CONTENT_TYPE_CONTACT.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_CONTACT");
		}
		else if (WPD_CONTENT_TYPE_AUDIO.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_AUDIO");
		}
		else if (WPD_CONTENT_TYPE_VIDEO.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_VIDEO");
		}
		else if (WPD_CONTENT_TYPE_TASK.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_TASK");
		}
		else if (WPD_CONTENT_TYPE_APPOINTMENT.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_APPOINTMENT");
		}
		else if (WPD_CONTENT_TYPE_EMAIL.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_EMAIL");
		}
		else if (WPD_CONTENT_TYPE_MEMO.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_MEMO");
		}
		else if (WPD_CONTENT_TYPE_UNSPECIFIED.Equals(contentType))
		{
			Console.Write("WPD_CONTENT_TYPE_UNSPECIFIED");
		}
		else
		{
			Console.Write(contentType);
		}
	}

	// Display the basic evt options for the passed in evt.
	static void DisplayEventOptions([In] IPortableDeviceCapabilities capabilities, in Guid evt)
	{
		IPortableDeviceValues evtOptions = capabilities.GetEventOptions(evt);
		Console.Write("Event Options:\n");
		// Read the WPD_EVENT_OPTION_IS_BROADCAST_EVENT value to see if the evt is a broadcast evt. If the read fails, assume false
		bool isBroadcastEvent = evtOptions.GetBoolValue(WPD_EVENT_OPTION_IS_BROADCAST_EVENT);
		Console.Write("\tWPD_EVENT_OPTION_IS_BROADCAST_EVENT = {0}\n", isBroadcastEvent ? "true" : "false");
	}

	// Display all functional object identifiers contained in an IPortableDevicePropVariantCollection
	// NOTE: These values are assumed to be in VT_LPWSTR VarType format.
	static void DisplayFunctionalObjectIDs([In] IPortableDevicePropVariantCollection functionalObjectIDs)
	{
		// Get the total number of object identifiers in the collection.
		uint numObjectIDs = functionalObjectIDs.GetCount();

		// Loop through the collection and displaying each object identifier found. This loop prints a comma-separated list of the object identifiers.
		for (uint objectIDIndex = 0; objectIDIndex < numObjectIDs; objectIDIndex++)
		{
			using PROPVARIANT objectID = new();
			functionalObjectIDs.GetAt(objectIDIndex, objectID);

			// We have a functional object identifier. It is assumed that object identifiers are returned as VT_LPWSTR varTypes.
			if (objectID.vt == VARTYPE.VT_LPWSTR && objectID.pwszVal != default)
			{
				// Display the object identifiers separated by commas
				Console.Write(objectID.pwszVal);
				if ((objectIDIndex + 1) < numObjectIDs)
				{
					Console.Write(", ");
				}
			}
			else
			{
				Console.Write("! Invalid functional object identifier found\n");
			}
		}
	}

	// List all functional objects on the device
	static void ListFunctionalObjects([In] IPortableDevice device)
	{
		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get all functional categories supported by the device. We will use these categories to enumerate functional objects that fall
		// within them.
		IPortableDevicePropVariantCollection functionalCategories = capabilities.GetFunctionalCategories();

		// Get the number of functional categories found on the device.
		uint numFunctionalCategories = functionalCategories.GetCount();

		Console.Write("\n{0} Functional Categories Found on the device\n\n", numFunctionalCategories);

		// Loop through each functional category and get the list of functional object identifiers associated with a particular category.
		for (uint index = 0; index < numFunctionalCategories; index++)
		{
			using PROPVARIANT pv = new();
			functionalCategories.GetAt(index, pv);

			// We have a functional category. It is assumed that functional categories are returned as VT_CLSID varTypes.
			if (pv.vt == VARTYPE.VT_CLSID && pv.puuid != default)
			{
				// Display the functional category name
				Console.Write("Functional Category: ");
				DisplayFunctionalCategory(pv.puuid!.Value);
				Console.Write("\n");

				// Display the object identifiers for all functional objects within this category
				IPortableDevicePropVariantCollection functionalObjectIDs = capabilities.GetFunctionalObjects(pv.puuid!.Value);
				Console.Write("Functional Objects: ");
				DisplayFunctionalObjectIDs(functionalObjectIDs);
				Console.Write("\n\n");
			}
		}
	}

	// Display all content types contained in an IPortableDevicePropVariantCollection
	// NOTE: These values are assumed to be in VT_CLSID VarType format.
	static void DisplayContentTypes([In] IPortableDevicePropVariantCollection contentTypes)
	{
		if (contentTypes is null)
		{
			Console.Write("! A default IPortableDevicePropVariantCollection interface pointer was received\n");
			return;
		}

		// Get the total number of content types in the collection.
		uint numContentTypes = contentTypes.GetCount();

		// Loop through the collection and displaying each content type found. This loop prints a comma-separated list of the content types.
		for (uint contentTypeIndex = 0; contentTypeIndex < numContentTypes; contentTypeIndex++)
		{
			using PROPVARIANT contentType = new();
			contentTypes.GetAt(contentTypeIndex, contentType);

			// We have a content type. It is assumed that content types are returned as VT_CLSID varTypes.
			if (contentType.vt == VARTYPE.VT_CLSID && contentType.puuid != default)
			{
				// Display the content types separated by commas
				DisplayContentType(contentType.puuid!.Value);

				if ((contentTypeIndex + 1) < numContentTypes)
				{
					Console.Write(", ");
				}
			}
			else
			{
				Console.Write("! Invalid content type found\n");
			}
		}
	}

	// List all functional categories on the device
	static void ListFunctionalCategories([In] IPortableDevice device)
	{
		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get all functional categories supported by the device.
		IPortableDevicePropVariantCollection functionalCategories = capabilities.GetFunctionalCategories();

		// Get the number of functional categories found on the device.
		uint numCategories = functionalCategories.GetCount();

		Console.Write("\n{0} Functional Categories Found on the device\n\n", numCategories);

		// Loop through each functional category and display its name
		for (uint index = 0; index < numCategories; index++)
		{
			using PROPVARIANT pv = new();
			functionalCategories.GetAt(index, pv);

			// We have a functional category. It is assumed that functional categories are returned as VT_CLSID varTypes.
			if (pv.vt == VARTYPE.VT_CLSID && pv.puuid != default)
			{
				// Display the functional category name
				DisplayFunctionalCategory(pv.puuid!.Value);
				Console.Write("\n");
			}
		}
	}

	// List supported content types the device supports
	static void ListSupportedContentTypes([In] IPortableDevice device)
	{
		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get all functional categories supported by the device. We will use these categories to enumerate functional objects that fall
		// within them.
		IPortableDevicePropVariantCollection functionalCategories = capabilities.GetFunctionalCategories();

		// Get the number of functional categories found on the device.
		uint numCategories = functionalCategories.GetCount();

		Console.Write("\n{0} Functional Categories Found on the device\n\n", numCategories);

		// Loop through each functional category and display its name and supported content types.
		for (uint index = 0; index < numCategories; index++)
		{
			using PROPVARIANT pv = new();
			functionalCategories.GetAt(index, pv);

			// We have a functional category. It is assumed that functional categories are returned as VT_CLSID varTypes.
			if (pv.vt == VARTYPE.VT_CLSID && pv.puuid != default)
			{
				// Display the functional category name
				Console.Write("Functional Category: ");
				DisplayFunctionalCategory(pv.puuid!.Value);
				Console.Write("\n");

				// Display the content types supported for this category
				IPortableDevicePropVariantCollection contentTypes = capabilities.GetSupportedContentTypes(pv.puuid!.Value);
				Console.Write("Supported Content Types: ");
				DisplayContentTypes(contentTypes);
				Console.Write("\n\n");
			}
		}
	}

	// Determines if a device supports a particular functional category.
	static bool SupportsFunctionalCategory([In] IPortableDevice device, in Guid functionalCategory)
	{
		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get all functional categories supported by the device. We will use these categories to search for a particular functional
		// category. There is typically only 1 of these types of functional categories.
		IPortableDevicePropVariantCollection functionalCategories = capabilities.GetFunctionalCategories();

		// Get the number of functional categories found on the device.
		uint numCategories = functionalCategories.GetCount();

		// Loop through each functional category and find the passed in category
		for (uint dwIndex = 0; dwIndex < numCategories; dwIndex++)
		{
			using PROPVARIANT pv = new();
			functionalCategories.GetAt(dwIndex, pv);

			// We have a functional category. It is assumed that functional categories are returned as VT_CLSID varTypes.
			if (pv.vt == VARTYPE.VT_CLSID && functionalCategory == pv.puuid!.Value)
				return true;
		}

		return false;
	}

	// Determines if a device supports a particular command.
	static bool SupportsCommand([In] IPortableDevice device, in PROPERTYKEY command)
	{
		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get all commands supported by the device. We will use these commands to search for a particular functional category.
		IPortableDeviceKeyCollection commands = capabilities.GetSupportedCommands();

		// Get the number of supported commands found on the device.
		uint numCommands = commands.GetCount();

		// Loop through each functional category and find the passed in category
		for (uint index = 0; index < numCommands; index++)
		{
			if (commands.GetAt(index) == command)
				return true;
		}

		return false;
	}

	// Reads the WPD_RENDERING_INFORMATION_PROFILES properties on the device.
	static IPortableDeviceValuesCollection ReadProfileInformationProperties([In] IPortableDevice device, string functionalObjectID)
	{
		// Get an IPortableDeviceContent interface from the IPortableDevice interface to access the content-specific methods.
		IPortableDeviceContent content = device.Content();

		// Get an IPortableDeviceProperties interface from the IPortableDeviceContent interface to access the property-specific methods.
		IPortableDeviceProperties properties = content.Properties();

		// CoCreate an IPortableDeviceKeyCollection interface to hold the the property keys we wish to read WPD_RENDERING_INFORMATION_PROFILES)
		IPortableDeviceKeyCollection propertiesToRead = new();

		// Populate the IPortableDeviceKeyCollection with the keys we wish to read.
		// NOTE: We are not handling any special error cases here so we can proceed with adding as many of the target properties as we can.
		propertiesToRead.Add(WPD_RENDERING_INFORMATION_PROFILES);

		// Call GetValues() passing the collection of specified PROPERTYKEYs.
		IPortableDeviceValues objectProperties = properties.GetValues(functionalObjectID, // The object whose properties we are reading
			propertiesToRead); // The properties we want to read

		// Read the WPD_RENDERING_INFORMATION_PROFILES
		return objectProperties.GetIPortableDeviceValuesCollectionValue(WPD_RENDERING_INFORMATION_PROFILES);
	}

	static void DisplayExpectedValues([In] IPortableDeviceValues expectedValues)
	{
		// 1) Determine what type of valid values should be displayed by reading the WPD_PROPERTY_ATTRIBUTE_FORM property.
		WpdAttributeForm formAttribute = (WpdAttributeForm)expectedValues.GetUnsignedIntegerValue(WPD_PROPERTY_ATTRIBUTE_FORM);

		// 2) Switch on the attribute form to determine what expected value properties to read.
		switch (formAttribute)
		{
			case WpdAttributeForm.WPD_PROPERTY_ATTRIBUTE_FORM_RANGE:
				uint rangeMin = expectedValues.GetUnsignedIntegerValue(WPD_PROPERTY_ATTRIBUTE_RANGE_MIN);
				uint rangeMax = expectedValues.GetUnsignedIntegerValue(WPD_PROPERTY_ATTRIBUTE_RANGE_MAX);
				uint rangeStep = expectedValues.GetUnsignedIntegerValue(WPD_PROPERTY_ATTRIBUTE_RANGE_STEP);
				Console.Write("MIN: {0}, MAX: {1}, STEP: {2}\n", rangeMin, rangeMax, rangeStep);
				break;

			default:
				Console.Write("* DisplayExpectedValues helper function did not display attributes for form {0}", formAttribute);
				break;
		}
	}

	// Displays a rendering profile.
	static void DisplayRenderingProfile([In] IPortableDeviceValues profile)
	{
		try
		{
			// Display WPD_MEDIA_TOTAL_BITRATE
			uint totalBitrate = profile.GetUnsignedIntegerValue(WPD_MEDIA_TOTAL_BITRATE);
		}
		catch
		{
			// If we fail to read the total bitrate as a single value, then it must be a valid value set. (i.e. returning
			// IPortableDeviceValues as the value which contains properties describing the valid values for this property.)
			IPortableDeviceValues expectedValues = profile.GetIPortableDeviceValuesValue(WPD_MEDIA_TOTAL_BITRATE);
			Console.Write("Total Bitrate: ");
			DisplayExpectedValues(expectedValues);
		}

		// Display WPD_AUDIO_CHANNEL_COUNT
		uint channelCount = profile.GetUnsignedIntegerValue(WPD_AUDIO_CHANNEL_COUNT);
		Console.Write("Channel Count: {0}\n", channelCount);

		// Display WPD_AUDIO_FORMAT_CODE
		uint audioFormatCode = profile.GetUnsignedIntegerValue(WPD_AUDIO_FORMAT_CODE);
		Console.Write("Audio Format Code: {0}\n", audioFormatCode);

		// Display WPD_OBJECT_FORMAT
		Guid objectFormat = profile.GetGuidValue(WPD_OBJECT_FORMAT);
		Console.Write("Object Format: {0}\n", objectFormat);
	}

	// List rendering capabilities the device supports
	static void ListRenderingCapabilityInformation([In] IPortableDevice device)
	{
		if (SupportsFunctionalCategory(device, WPD_FUNCTIONAL_CATEGORY_RENDERING_INFORMATION) == false)
		{
			Console.Write("This device does not support device rendering information to display\n");
			return;
		}

		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get the functional object identifier for the rendering information object
		IPortableDevicePropVariantCollection renderingInfoObjects = capabilities.GetFunctionalObjects(WPD_FUNCTIONAL_CATEGORY_RENDERING_INFORMATION);

		// Assume the device only has one rendering information object for this example. We are going to request the first Object Identifier
		// found in the collection.
		using PROPVARIANT pv = new();
		renderingInfoObjects.GetAt(0, pv);
		if (pv.vt == VARTYPE.VT_LPWSTR && pv.pwszVal != default)
		{
			IPortableDeviceValuesCollection renderingInfoProfiles = ReadProfileInformationProperties(device, pv.pwszVal);

			// Error output statements are performed by the helper function, so they are omitted here.

			// Display all rendering profiles

			// Get the number of profiles supported by the device
			uint numProfiles = renderingInfoProfiles.GetCount();
			Console.Write("{0} Rendering Profiles are supported by this device\n", numProfiles);
			for (uint index = 0; index < numProfiles; index++)
			{
				IPortableDeviceValues profile = renderingInfoProfiles.GetAt(index);
				Console.Write("\nProfile #{0}:\n", index);
				DisplayRenderingProfile(profile);
				Console.Write("\n\n");
			}
		}
	}

	// List all supported evts on the device
	static void ListSupportedEvents([In] IPortableDevice device)
	{
		// Get an IPortableDeviceCapabilities interface from the IPortableDevice interface to access the device capabilities-specific methods.
		IPortableDeviceCapabilities capabilities = device.Capabilities();

		// Get all evts supported by the device.
		IPortableDevicePropVariantCollection evts = capabilities.GetSupportedEvents();

		// Get the number of supported evts found on the device.
		uint numEvents = evts.GetCount();

		Console.Write("\n{0} Supported Events Found on the device\n\n", numEvents);

		// Loop through each evt and display its name
		for (uint index = 0; index < numEvents; index++)
		{
			using PROPVARIANT pv = new();
			evts.GetAt(index, pv);
			// We have an evt. It is assumed that evts are returned as VT_CLSID varTypes.
			if (pv.vt == VARTYPE.VT_CLSID && pv.puuid != default)
			{
				// Display the evt name
				DisplayEvent(pv.puuid!.Value);
				Console.Write("\n");
				// Display the evt options
				DisplayEventOptions(capabilities, pv.puuid!.Value);
				Console.Write("\n");
			}
		}
	}
}