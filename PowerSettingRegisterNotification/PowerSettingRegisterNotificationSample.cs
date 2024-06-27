/*++

Routine Description:

Main method which runs when exe is launched. It registers for the GUID_ENERGY_SAVER_STATUS notification
and waits for notifications until the user terminates the program by entering any input and hitting "enter".
While the program is running, the callback method will print out to the console the notification is triggered
(according to the callback method).

Return Value:

Returns error status.

--*/

using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.PowrProf;


//
// Register for GUID_ENERGY_SAVER_STATUS to receive notifications whenever setting updates
//

DEVICE_NOTIFY_SUBSCRIBE_PARAMETERS pparams = new() { Callback = EnergySaverPowerSettingCallback };
var error = PowerSettingRegisterNotification(GUID_ENERGY_SAVER_STATUS, DEVICE_NOTIFY.DEVICE_NOTIFY_CALLBACK,
	pparams, out var registrationHandle);

if (error != Win32Error.ERROR_SUCCESS)
{
	Console.Write("Error registering for GUID_ENERGY_SAVER_STATUS: {0}. Terminating program...\n\n", error);
	return (int)(uint)error;
}

Console.Write("Registered for GUID_ENERGY_SAVER_STATUS notifications\n\n");

//
// Wait for user input before unregistering... 
//

Console.Write("Waiting for GUID_ENERGY_SAVER_STATUS notifications... \n\n");
Console.Write("You can toggle Energy Saver in Quick Settings or Settings > Power & Battery to trigger the notification.\n\n");
Console.Write("Press any key to end the program...\n\n");
Console.ReadKey(true);

//
// Unregister from GUID_ENERGY_SAVER_STATUS notifications for cleanup
//

registrationHandle.Dispose();
Console.Write("Unregistered for GUID_ENERGY_SAVER_STATUS notifications\n\n");
return 0;

/*++

Routine Description:

This is the callback function for when GUID_ENERGY_SAVER_STATUS power setting
notification is triggered. It shows an example of how an App can
adjust behavior depending on the energy saver status.

Arguments:

Context - The context provided when registering for the power notification.

Type - The type of power event that caused this notification (e.g.
GUID_ENERGY_SAVER_STATUS)

Setting - The data associated with the power event. For
GUID_ENERGY_SAVER_STATUS, this is a value of type ENERGY_SAVER_STATUS.

Return Value:

Returns error status.

--*/
Win32Error EnergySaverPowerSettingCallback([In, Optional] IntPtr Context, uint Type, [In] IntPtr Setting)
{
	POWERBROADCAST_SETTING powerSetting = Setting.ToStructure<POWERBROADCAST_SETTING>();
	Guid settingId = powerSetting.PowerSetting;

	//
	// Check the data size is expected
	//
	if (settingId != GUID_ENERGY_SAVER_STATUS || powerSetting.DataLength != Marshal.SizeOf(typeof(ENERGY_SAVER_STATUS)))
	{
		return Win32Error.ERROR_INVALID_PARAMETER;
	}

	var status = powerSetting.GetEnumData<ENERGY_SAVER_STATUS>();

	//
	// Change app behavior depending on energy saver status.
	// For example, an app that does data synchronization might reduce its
	// synchronization when under standard energy saver mode and pause it
	// entirely when under extreme energy saver mode.
	//

	switch (status)
	{
		case ENERGY_SAVER_STATUS.ENERGY_SAVER_STANDARD:
			Console.Write("Standard energy saver mode: Reduce activities.\n");
			break;

		case ENERGY_SAVER_STATUS.ENERGY_SAVER_HIGH_SAVINGS:
			Console.Write("Extreme energy saver mode: Pause all non-essential activities.\n");
			break;

		default:
			Console.Write("Energy saver not active: Run normally.\n");
			break;
	}

	return Win32Error.ERROR_SUCCESS;
}