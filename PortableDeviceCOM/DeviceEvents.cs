static partial class Program
{
	// IPortableDeviceEventCallback implementation for use with device events.
	class CPortableDeviceEventsCallback : IPortableDeviceEventCallback
	{
		void IPortableDeviceEventCallback.OnEvent(IPortableDeviceValues? eventParameters)
		{
			if (eventParameters is not null)
			{
				Console.Write("***************************\n** Device event received **\n***************************\n");
				DisplayStringProperty(eventParameters, WPD_EVENT_PARAMETER_PNP_DEVICE_ID, "WPD_EVENT_PARAMETER_PNP_DEVICE_ID");
				DisplayGuidProperty(eventParameters, WPD_EVENT_PARAMETER_EVENT_ID, "WPD_EVENT_PARAMETER_EVENT_ID");
			}
		}
	}

	static void RegisterForEventNotifications([In] IPortableDevice device, [In, Out] ref string? eventCookie)
	{
		HRESULT hr = HRESULT.S_OK;
		string? tempEventCookie = default;

		// Check to see if we already have an event registration cookie. If so, then avoid registering again.
		// NOTE: An application can register for events as many times as they want. This sample only keeps a single registration cookie
		// around for simplicity.
		if (eventCookie != default)
		{
			Console.Write("This application has already registered to receive device events.\n");
			return;
		}

		eventCookie = default;

		// Create an instance of the callback object. This will be called when events are received.
		CPortableDeviceEventsCallback callback = new();

		// Call Advise to register the callback and receive events.
		device.Advise(0, callback, default, out tempEventCookie);

		// Save the event registration cookie if event registration was successful.
		eventCookie = tempEventCookie;
		tempEventCookie = default; // relinquish memory to the caller
		Console.Write("This application has registered for device event notifications and was returned the registration cookie '{0}'", eventCookie);
	}

	static void UnregisterForEventNotifications([In, Optional] IPortableDevice? device, [In, Optional] string? eventCookie)
	{
		if (device is null || eventCookie is null)
			return;

		device.Unadvise(eventCookie);
		Console.Write("This application used the registration cookie '{0}' to unregister from receiving device event notifications", eventCookie);
	}
}