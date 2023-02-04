global using System.Runtime.InteropServices;
global using Vanara.PInvoke;
global using static Vanara.PInvoke.CoreAudio;
global using static Vanara.PInvoke.Ole32;
global using static Vanara.PInvoke.PropSys;
using System.CommandLine;

PROPERTYKEY PKEY_Device_FriendlyName = new(new(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0), 14);

RootCommand rootCmd = new()
{
	new Option<uint>("-f", () => 440, "Sine wave frequency (Hz)") { IsRequired = true },
	new Option<uint>("-l", () => 30, "Audio Render Latency (ms)") { IsRequired = true },
	new Option<uint>("-d", () => 10, "Sine Wave Duration (s)") { IsRequired = true },
	new Option<bool>("-console", "Use the default console device"),
	new Option<bool>("-communications", "Use the default communications device"),
	new Option<bool>("-multimedia", "Use the default multimedia device"),
	new Option<string>("-endpoint", "Use the specified endpoint ID"),
	new Option<bool>("-w", "Enable call to AudioViewManagerService"),
};
rootCmd.SetHandler(Main);
rootCmd.InvokeAsync(args);

void Main(System.CommandLine.Invocation.InvocationContext ctx) // uint TargetFrequency, uint TargetLatency, uint TargetDurationInSec, bool UseConsoleDevice, bool UseCommunicationsDevice, bool UseMultimediaDevice, string OutputEndpoint, bool EnableAudioViewManagerService)
{
	uint TargetFrequency = (uint)(ctx.ParseResult.GetValueForOption(rootCmd.Options[0]) ?? 440U);
	uint TargetLatency = (uint)(ctx.ParseResult.GetValueForOption(rootCmd.Options[1]) ?? 30U);
	uint TargetDurationInSec = (uint)(ctx.ParseResult.GetValueForOption(rootCmd.Options[2]) ?? 10U);
	bool UseConsoleDevice = (bool)(ctx.ParseResult.GetValueForOption(rootCmd.Options[3]) ?? false);
	bool UseCommunicationsDevice = (bool)(ctx.ParseResult.GetValueForOption(rootCmd.Options[4]) ?? false);
	bool UseMultimediaDevice = (bool)(ctx.ParseResult.GetValueForOption(rootCmd.Options[5]) ?? false);
	string? OutputEndpoint = (string?)ctx.ParseResult.GetValueForOption(rootCmd.Options[6]);
	bool EnableAudioViewManagerService = (bool)(ctx.ParseResult.GetValueForOption(rootCmd.Options[7]) ?? false);

	Console.Write("WASAPI Render Shared Event Driven Sample\nCopyright (c) Microsoft. All Rights Reserved\n\n");

	//
	// A GUI application should use COINIT_APARTMENTTHREADED instead of COINIT_MULTITHREADED.
	//
	CoInitializeEx(default, COINIT.COINIT_MULTITHREADED).ThrowIfFailed("Unable to initialize COM");

	//
	// Now that we've parsed our command line, pick the device to render.
	//
	PickDevice(out var device, out var isDefaultDevice, out var role);

	Console.Write("Render a {0} hz Sine wave for {1} seconds\n", TargetFrequency, TargetDurationInSec);

	//
	// Instantiate a renderer and play a sound for TargetDuration seconds
	//
	// Configure the renderer to enable stream switching on the specified role if the user specified one of the default devices.
	//
	CWASAPIRenderer renderer = new(device, isDefaultDevice, role, EnableAudioViewManagerService, TargetLatency);

	//
	// We've initialized the renderer. Once we've done that, we know some information about the
	// mix format and we can allocate the buffer that we're going to render.
	//
	// The buffer is going to contain "TargetDuration" seconds worth of PCM data. That means
	// we're going to have ref TargetDuration samples/second frames multiplied by the frame size.
	//
	uint renderBufferSizeInBytes = renderer.BufferSizePerPeriod * renderer.FrameSize;
	ulong renderDataLength = renderer.SamplesPerSecond * TargetDurationInSec * renderer.FrameSize + (renderBufferSizeInBytes - 1);
	ulong renderBufferCount = renderDataLength / renderBufferSizeInBytes;

	//
	// Build the render buffer queue.
	//
	LinkedList<RenderBuffer> renderQueue = new();

	double theta = 0;

	for (ulong i = 0; i < renderBufferCount; i++)
	{
		RenderBuffer renderBuffer;
		try
		{
			// Append another buffer to the queue.
			renderBuffer = renderQueue.AddLast(new RenderBuffer(renderBufferSizeInBytes)).Value;
		}
		catch
		{
			Console.Write("Unable to allocate render buffer\n");
			ctx.ExitCode = -1;
			return;
		}

		//
		// Generate tone data in the buffer.
		//
		switch (renderer.SampleType)
		{
			case CWASAPIRenderer.RenderSampleType.Float:
				GenerateSineSamples<float>(renderBuffer.buffer, TargetFrequency, renderer.ChannelCount, renderer.SamplesPerSecond, ref theta);
				break;
			case CWASAPIRenderer.RenderSampleType.Pcm16Bit:
				GenerateSineSamples<short>(renderBuffer.buffer, TargetFrequency, renderer.ChannelCount, renderer.SamplesPerSecond, ref theta);
				break;
		}
	}

	if (renderer.Start(renderQueue).Succeeded)
	{
		do
		{
			Console.Write(".");
			Thread.Sleep(1000);
		} while (--TargetDurationInSec > 0);
		Console.Write("\n");
		renderer.Stop();
	}

	renderer.Shutdown();

	//
	// Based on the input switches, pick the specified device to use.
	//
	void PickDevice(out IMMDevice DeviceToUse, out bool IsDefaultDevice, out ERole DefaultDeviceRole)
	{
		IMMDevice? deviceToUse = null;
		DefaultDeviceRole = 0;
		IsDefaultDevice = false; // Assume we're not using the default device.

		IMMDeviceEnumerator deviceEnumerator = new();

		//
		// First off, if none of the console switches was specified, use the console device.
		//
		if (!UseConsoleDevice && !UseCommunicationsDevice && !UseMultimediaDevice && OutputEndpoint == null)
		{
			//
			// The user didn't specify an output device, prompt the user for a device and use that.
			//
			IMMDeviceCollection deviceCollection = deviceEnumerator.EnumAudioEndpoints(EDataFlow.eRender, DEVICE_STATE.DEVICE_STATE_ACTIVE);

			Console.Write("Select an output device:\n");
			Console.Write(" 0: Default Console Device\n");
			Console.Write(" 1: Default Communications Device\n");
			Console.Write(" 2: Default Multimedia Device\n");
			uint deviceCount = deviceCollection.GetCount();

			for (uint i = 0; i < deviceCount; i += 1)
			{
				Console.Write($" {i + 3}: {GetDeviceName(deviceCollection, i)}\n");
			}
			var choice = Console.ReadLine();
			if (uint.TryParse(choice, out var deviceIndex))
			{
				Console.Write("unrecognized device index: {0}\n", choice);
				throw new HRESULT(HRESULT.E_UNEXPECTED).GetException();
			}
			switch (deviceIndex)
			{
				case 0:
					UseConsoleDevice = true;
					break;
				case 1:
					UseCommunicationsDevice = true;
					break;
				case 2:
					UseMultimediaDevice = true;
					break;
				default:
					deviceCollection.Item(deviceIndex - 3, out deviceToUse).ThrowIfFailed();
					break;
			}
		}
		else if (OutputEndpoint is not null)
		{
			deviceToUse = deviceEnumerator.GetDevice(OutputEndpoint);
		}

		if (deviceToUse is null)
		{
			ERole deviceRole = ERole.eConsole; // Assume we're using the console role.
			if (UseConsoleDevice)
			{
				deviceRole = ERole.eConsole;
			}
			else if (UseCommunicationsDevice)
			{
				deviceRole = ERole.eCommunications;
			}
			else if (UseMultimediaDevice)
			{
				deviceRole = ERole.eMultimedia;
			}
			deviceToUse = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, deviceRole);
			IsDefaultDevice = true;
			DefaultDeviceRole = deviceRole;
		}

		DeviceToUse = deviceToUse;
	}
}

//
// Retrieves the device friendly name for a particular device in a device collection.
//
string GetDeviceName(IMMDeviceCollection DeviceCollection, uint DeviceIndex)
{
	DeviceCollection.Item(DeviceIndex, out var device).ThrowIfFailed();
	IPropertyStore propertyStore = device.OpenPropertyStore(STGM.STGM_READ);
	return $"{(propertyStore.GetValue(PKEY_Device_FriendlyName) as string) ?? "Unknown"} ({device.GetId()})";
}

// Convert from [-1.0...+1.0] to sample format.
T Convert<T>(double Value)
{
	if (typeof(T) != typeof(double) && typeof(T) != typeof(float))
		return unchecked((T)(object)-1);
	else
		// Floating point types all use the range [-1.0 .. +1.0]
		return (T)System.Convert.ChangeType(Value, typeof(T));
}

//
// Generate samples which represent a sine wave that fits into the specified buffer. 
//
// T: Type of data holding the sample (short, int, byte, float)
// Buffer - Buffer to hold the samples
// BufferLength - Length of the buffer.
// ChannelCount - Number of channels per audio frame.
// SamplesPerSecond - Samples/Second for the output data.
// InitialTheta - Initial theta value - start at 0, modified in this function.
//
void GenerateSineSamples<T>(byte[] Buffer, uint Frequency, ushort ChannelCount, uint SamplesPerSecond, ref double InitialTheta) where T : unmanaged, IConvertible
{
	double sampleIncrement = Frequency * Math.PI * 2.0 / SamplesPerSecond;
	unsafe
	{
		fixed (byte* pBuffer = Buffer)
		{
			T* dataBuffer = (T*)pBuffer;
			double theta = InitialTheta;

			for (int i = 0; i < Buffer.Length / Marshal.SizeOf(typeof(T)); i += ChannelCount)
			{
				double sinValue = Math.Sin(theta);
				for (ushort j = 0; j < ChannelCount; j++)
				{
					dataBuffer[i + j] = Convert<T>(sinValue);
				}
				theta += sampleIncrement;
			}

			InitialTheta = theta;
		}
	}
}