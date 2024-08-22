global using System.Runtime.InteropServices.ComTypes;
global using Vanara.PInvoke;
global using static Vanara.PInvoke.PortableDeviceApi;
global using static Vanara.PInvoke.Kernel32;
global using static Vanara.PInvoke.Ole32;

static partial class Program
{
	const int SELECTION_BUFFER_SIZE = 81;

	static void DoMenu()
	{
		var device = ChooseDevice();
		if (device is null)
		{
			// No device was selected, so exit immediately.
			return;
		}

		uint selectionIndex = 0;
		string? eventCookie = null;
		while (selectionIndex != 99)
		{
			Console.Write("\n\n");
			Console.Write("WPD Sample Application \n");
			Console.Write("=======================================\n\n");
			Console.Write("0.  Enumerate all Devices\n");
			Console.Write("1.  Choose a Device\n");
			Console.Write("2.  Enumerate all content on the device\n");
			Console.Write("3.  Transfer content from the device\n");
			Console.Write("4.  Delete content from the device\n");
			Console.Write("5.  Move content already on the device to another location on the device\n");
			Console.Write("6.  Transfer Image content to the device\n");
			Console.Write("7.  Transfer Music content to the device\n");
			Console.Write("8.  Transfer Contact (VCARD file) content to the device\n");
			Console.Write("9.  Transfer Contact (Defined by Properties Only) to the device\n");
			Console.Write("10. Create a folder on the device\n");
			Console.Write("11. Add a Contact Photo resource to an object\n");
			Console.Write("12. Read properties on a content object\n");
			Console.Write("13. Write properties on a content object\n");
			Console.Write("14. Get an object identifier from a Persistent Unique Identifier (PUID)\n");
			Console.Write("15. List all functional categories supported by the device\n");
			Console.Write("16. List all functional objects on the device\n");
			Console.Write("17. List all content types supported by the device\n");
			Console.Write("18. List rendering capabilities supported by the device\n");
			Console.Write("19. Register to receive device event notifications\n");
			Console.Write("20. Unregister from receiving device event notifications\n");
			Console.Write("21. List all events supported by the device\n");
			Console.Write("22. List all hint locations supported by the device\n");
			Console.Write("====(Advanced BULK property operations)====\n");
			Console.Write("23. Read properties on multiple content objects\n");
			Console.Write("24. Write properties on multiple content objects\n");
			Console.Write("25. Read properties on multiple content objects using object format\n");
			Console.Write("====(Update content operations)====\n");
			Console.Write("26. Update Image content (properties and data) on the device\n");
			Console.Write("27. Update Music content (properties and data) on the device\n");
			Console.Write("28. Update Contact content (properties and data) on the device\n");
			Console.Write("99. Exit\n");
			if (!uint.TryParse(Console.ReadLine(), out selectionIndex))
				selectionIndex = 98;
			try
			{
				switch (selectionIndex)
				{
					case 0:
						EnumerateAllDevices();
						break;
					case 1:
						// Unregister any device event registrations before creating a new IPortableDevice
						UnregisterForEventNotifications(device, eventCookie);

						// Release the old IPortableDevice interface before obtaining a new one.
						device = ChooseDevice();
						break;
					case 2:
						EnumerateAllContent(device!);
						break;
					case 3:
						TransferContentFromDevice(device!);
						break;
					case 4:
						DeleteContentFromDevice(device!);
						break;
					case 5:
						MoveContentAlreadyOnDevice(device!);
						break;
					case 6:
						TransferContentToDevice(device!, WPD_CONTENT_TYPE_IMAGE, "JPEG (*.JPG)\0*.JPG\0JPEG (*.JPEG)\0*.JPEG\0JPG (*.JPE)\0*.JPE\0JPG (*.JFIF)\0*.JFIF\0\0", "JPG");
						break;
					case 7:
						TransferContentToDevice(device!, WPD_CONTENT_TYPE_AUDIO, "MP3 (*.MP3)\0*.MP3\0\0", "MP3");
						break;
					case 8:
						TransferContentToDevice(device!, WPD_CONTENT_TYPE_CONTACT, "VCARD (*.VCF)\0*.VCF\0\0", "VCF");
						break;
					case 9:
						TransferContactToDevice(device!);
						break;
					case 10:
						CreateFolderOnDevice(device!);
						break;
					case 11:
						CreateContactPhotoResourceOnDevice(device!);
						break;
					case 12:
						ReadContentProperties(device!);
						break;
					case 13:
						WriteContentProperties(device!);
						break;
					case 14:
						GetObjectIdentifierFromPersistentUniqueIdentifier(device!);
						break;
					case 15:
						ListFunctionalCategories(device!);
						break;
					case 16:
						ListFunctionalObjects(device!);
						break;
					case 17:
						ListSupportedContentTypes(device!);
						break;
					case 18:
						ListRenderingCapabilityInformation(device!);
						break;
					case 19:
						RegisterForEventNotifications(device!, ref eventCookie);
						break;
					case 20:
						UnregisterForEventNotifications(device, eventCookie);
						eventCookie = default;
						break;
					case 21:
						ListSupportedEvents(device!);
						break;
					case 22:
						ReadHintLocations(device!);
						break;
					case 23:
						ReadContentPropertiesBulk(device!);
						break;
					case 24:
						WriteContentPropertiesBulk(device!);
						break;
					case 25:
						ReadContentPropertiesBulkFilteringByFormat(device!);
						break;
					case 26:
						UpdateContentOnDevice(device!, WPD_CONTENT_TYPE_IMAGE, "JPEG (*.JPG)\0*.JPG\0JPEG (*.JPEG)\0*.JPEG\0JPG (*.JPE)\0*.JPE\0JPG (*.JFIF)\0*.JFIF\0\0", "JPG");
						break;
					case 27:
						UpdateContentOnDevice(device!, WPD_CONTENT_TYPE_AUDIO, "MP3 (*.MP3)\0*.MP3\0\0", "MP3");
						break;
					case 28:
						UpdateContentOnDevice(device!, WPD_CONTENT_TYPE_CONTACT, "VCARD (*.VCF)\0*.VCF\0\0", "VCF");
						break;
					default:
						break;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error:\n{ex}");
			}
		}
	}

	[MTAThread]
	internal static void Main()
	{
		// Enable the heap manager to terminate the process on heap error.
		HeapSetInformation(default, HEAP_INFORMATION_CLASS.HeapEnableTerminationOnCorruption, default, 0);

		// Enter the menu processing loop
		DoMenu();
	}
}