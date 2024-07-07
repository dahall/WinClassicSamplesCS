using Vanara.PInvoke;
using static Vanara.PInvoke.CoreAudio;
using static Vanara.PInvoke.PropSys;

namespace AEC;

public class Program
{
	[MTAThread]
	public static void Main()
	{
		// Print diagnostic messages to the console for developer convenience.
		GetRenderEndpointId(out string? endpointId).ThrowIfFailed();

		using CAECCapture aecCapture = new();
		aecCapture.SetEchoCancellationRenderEndpoint(endpointId!);

		// Capture for 10 seconds.
		Console.Write("Capturing for 10 seconds...\n");
		Thread.Sleep(10000);
		Console.Write("Finished.\n");
	}

	private static HRESULT GetRenderEndpointId(out string? endpointId)
	{
		// default means "use the system default"
		endpointId = default;

		IMMDeviceEnumerator deviceEnumerator = new();
		IMMDeviceCollection? spDeviceCollection = deviceEnumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE);
		if (spDeviceCollection is null)
			return HRESULT.S_FALSE;

		uint deviceCount = spDeviceCollection.GetCount();
		IMMDevice? device;

		Console.Write("0: system default\n");

		for (uint i = 0; i < deviceCount; i++)
		{
			// Get the device from the collection.
			spDeviceCollection.Item(i, out device).ThrowIfFailed();

			// Get the device friendly name.
			IPropertyStore? properties = device!.OpenPropertyStore(STGM.STGM_READ);
			object? variant = properties?.GetValue(FunDisc.PKEY_Device_FriendlyName);

			Console.Write("{0}: {1}\n", i + 1, variant);
		}

		Console.Write("Choose a device to use as the acoustic echo cancellation render endpoint: ");
		Console.Out.Flush();

		if (!uint.TryParse(Console.ReadLine(), out var index))
		{
			return HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_CANCELLED);
		}
		if (index == 0)
		{
			return HRESULT.S_OK;
		}

		// Convert from 1-based index to 0-based index.
		index--;
		if (index > deviceCount)
		{
			Console.Write("Invalid choice.\n");
			return HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_CANCELLED);
		}

		// Get the chosen device from the collection.
		spDeviceCollection.Item(index, out device).ThrowIfFailed();

		// Get and return the endpoint ID for that device.
		endpointId = device!.GetId();

		return HRESULT.S_OK;
	}
}