// See https://aka.ms/new-console-template for more information
using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.WinMm;

internal readonly struct RenderBuffer
{
	public readonly byte[] buffer;

	public RenderBuffer(uint length) => buffer = new byte[length];
}

internal class CWASAPIRenderer : IMMNotificationClient, IAudioSessionEvents
{
	private readonly bool enableAudioViewManagerService, enableStreamSwitch, isDefaultDevice;
	private readonly ERole endpointRole;
	private readonly ERole role;
	private IAudioClient audioClient;
	private SafeEventHandle audioSamplesReadyEvent, shutdownEvent, streamSwitchEvent;
	private IAudioSessionControl? audioSessionControl;
	private IMMDevice? device;
	private IMMDeviceEnumerator deviceEnumerator;
	private IMMDevice endpoint;
	private uint engineLatencyInMS;
	private bool inStreamSwitch;
	private SafeCoTaskMemStruct<WAVEFORMATEX> mixFormat;
	private LinkedList<RenderBuffer>? renderBufferQueue;
	private IAudioRenderClient? renderClient;
	private SafeHTHREAD? renderThread;
	private SafeEventHandle? streamSwitchCompleteEvent;
	public CWASAPIRenderer(IMMDevice EndPoint, bool EnableStreamSwitch, ERole EndpointRole, bool EnableAudioViewManagerService, uint EngineLatency)
	{
		endpoint = EndPoint;
		enableStreamSwitch = EnableStreamSwitch;
		endpointRole = EndpointRole;
		enableAudioViewManagerService = EnableAudioViewManagerService;

		if (EngineLatency < 30)
		{
			throw new ArgumentOutOfRangeException(nameof(EngineLatency), "Engine latency in shared mode event driven cannot be less than 30ms");
		}

		// Create our shutdown and samples ready events- we want auto reset events that start in the not-signaled state.
		shutdownEvent = Win32Error.ThrowLastErrorIfInvalid(CreateEvent(default, false, false));

		audioSamplesReadyEvent = Win32Error.ThrowLastErrorIfInvalid(CreateEvent(default, false, false));

		// Create our stream switch event- we want auto reset events that start in the not-signaled state. Note that we create this event
		// even if we're not going to stream switch - that's because the event is used in the main loop of the renderer and thus it has
		// to be set.
		streamSwitchEvent = Win32Error.ThrowLastErrorIfInvalid(CreateEvent(default, false, false));

		// Now activate an IAudioClient object on our preferred endpoint and retrieve the mix format for that endpoint.
		audioClient = endpoint.Activate<IAudioClient>(CLSCTX.CLSCTX_INPROC_SERVER);

		deviceEnumerator = new IMMDeviceEnumerator();

		// Load the MixFormat. This may differ depending on the shared mode used
		audioClient.GetMixFormat(out SafeCoTaskMemHandle? mem).ThrowIfFailed();
		SizeT sz = mem.Size;
		mixFormat = new(mem.TakeOwnership(), true, sz);

		FrameSize = mixFormat.Value.nBlockAlign;
		CalculateMixFormatType().ThrowIfFailed();

		// Remember our configured latency in case we'll need it for a stream switch later.
		engineLatencyInMS = EngineLatency;

		InitializeAudioEngine();

		if (enableStreamSwitch)
		{
			audioSessionControl = audioClient.GetService<IAudioSessionControl>();

			// Create the stream switch complete event- we want a manual reset event that starts in the not-signaled state.
			streamSwitchCompleteEvent = Win32Error.ThrowLastErrorIfInvalid(CreateEvent(default, false, false));

			// Register for session and endpoint change notifications.
			//
			// A stream switch is initiated when we receive a session disconnect notification or we receive a default device changed notification.
			audioSessionControl.RegisterAudioSessionNotification(this);

			deviceEnumerator.RegisterEndpointNotificationCallback(this);
		}
	}

	public enum RenderSampleType
	{
		Float,
		Pcm16Bit,
	}

	// The Event Driven renderer will be woken up every defaultDevicePeriod hundred-nano-seconds. Convert that time into a number of frames.
	public uint BufferSizePerPeriod
	{
		get
		{
			long defaultDevicePeriod = 0;
			try { audioClient?.GetDevicePeriod(out defaultDevicePeriod, out _); }
			catch
			{
				Console.Write("Unable to retrieve device period\n");
				return 0;
			}
			double devicePeriodInSeconds = defaultDevicePeriod / (10000.0 * 1000.0);
			return (uint)(mixFormat.Value.nSamplesPerSec * devicePeriodInSeconds + 0.5);
		}
	}

	public ushort BytesPerSample => (ushort)(mixFormat.Value.wBitsPerSample / 8);
	public ushort ChannelCount => mixFormat.Value.nChannels;
	public uint FrameSize { get; private set; }
	public uint SamplesPerSecond => mixFormat.Value.nSamplesPerSec;
	public RenderSampleType SampleType { get; private set; } = RenderSampleType.Pcm16Bit;
	private uint BufferSize { get; set; }

	// Shut down the render code and free all the resources.
	public void Shutdown()
	{
		if (renderThread is not null && !renderThread.IsClosed)
		{
			shutdownEvent.Set();
			renderThread.Wait();
			renderThread.Dispose();
		}

		if (enableStreamSwitch)
		{
			TerminateStreamSwitch();
		}
	}

	// Start rendering - Create the render thread and start rendering the buffer.
	public HRESULT Start(LinkedList<RenderBuffer> RenderBufferQueue)
	{
		renderBufferQueue = RenderBufferQueue;

		// We want to pre-roll the first buffer's worth of data into the pipeline. That way the audio engine won't glitch on startup.
		if (renderClient is null) throw new InvalidOperationException();

		IntPtr pData;

		if (renderBufferQueue.Count == 0)
		{
			pData = renderClient.GetBuffer(BufferSize);
			renderClient.ReleaseBuffer(BufferSize, AUDCLNT_BUFFERFLAGS.AUDCLNT_BUFFERFLAGS_SILENT);
		}
		else
		{
			RenderBuffer renderBuffer = renderBufferQueue.First();
			// Remove the buffer from the queue.
			renderBufferQueue.RemoveFirst();
			uint bufferLengthInFrames = (uint)renderBuffer.buffer.Length / FrameSize;

			pData = renderClient.GetBuffer(bufferLengthInFrames);
			pData.Write(renderBuffer.buffer);
			renderClient.ReleaseBuffer(bufferLengthInFrames, 0);
		}

		// Now create the thread which is going to drive the renderer.
		GCHandle h = GCHandle.Alloc(this);
		try
		{
			renderThread = CreateThread(default, 0, WASAPIRenderThread, (IntPtr)h, 0, out _);
			if (renderThread.IsInvalid)
				return Win32Error.GetLastError().ToHRESULT();
		}
		finally { h.Free(); }

		// We're ready to go, start rendering!
		audioClient.Start();

		return HRESULT.S_OK;
	}

	// Stop the renderer.
	public void Stop()
	{
		// Tell the render thread to shut down, wait for the thread to complete then clean up all the stuff we allocated in Start().
		if (shutdownEvent?.IsInvalid ?? false)
		{
			shutdownEvent.Set();
		}

		try { audioClient.Stop(); }
		catch { Console.Write("Unable to stop audio client\n"); }

		if (renderThread?.IsInvalid ?? false)
		{
			WaitForSingleObject(renderThread, INFINITE);
			renderThread.Dispose();
			renderThread = null;
		}

		// Drain the buffers in the render buffer queue.
		renderBufferQueue?.Clear();
	}

	HRESULT IAudioSessionEvents.OnChannelVolumeChanged(uint ChannelCount, float[] NewChannelVolumeArray, uint ChangedChannel, in Guid EventContext) => HRESULT.S_OK;

	// Called when the default render device changed. We just want to set an event which lets the stream switch logic know that it's ok to
	// continue with the stream switch.
	HRESULT IMMNotificationClient.OnDefaultDeviceChanged(EDataFlow Flow, ERole Role, string _)
	{
		if (Flow == EDataFlow.eRender && Role == endpointRole)
		{
			// The default render device for our configured role was changed.
			//
			// If we're not in a stream switch already, we want to initiate a stream switch event. We also we want to set the stream switch
			// complete event. That will signal the render thread that it's ok to re-initialize the audio renderer.
			if (!inStreamSwitch)
			{
				inStreamSwitch = true;
				streamSwitchEvent.Set();
			}
			streamSwitchCompleteEvent?.Set();
		}
		return HRESULT.S_OK;
	}

	HRESULT IMMNotificationClient.OnDeviceAdded(string pwstrDeviceId) => HRESULT.S_OK;

	HRESULT IMMNotificationClient.OnDeviceRemoved(string pwstrDeviceId) => HRESULT.S_OK;

	HRESULT IMMNotificationClient.OnDeviceStateChanged(string pwstrDeviceId, DEVICE_STATE dwNewState) => HRESULT.S_OK;

	HRESULT IAudioSessionEvents.OnDisplayNameChanged(string NewDisplayName, in Guid EventContext) => HRESULT.S_OK;

	HRESULT IAudioSessionEvents.OnGroupingParamChanged(in Guid NewGroupingParam, in Guid EventContext) => HRESULT.S_OK;

	HRESULT IAudioSessionEvents.OnIconPathChanged(string NewIconPath, in Guid EventContext) => HRESULT.S_OK;

	HRESULT IMMNotificationClient.OnPropertyValueChanged(string pwstrDeviceId, PROPERTYKEY key) => HRESULT.S_OK;

	// Called when an audio session is disconnected.
	//
	// When a session is disconnected because of a device removal or format change event, we just want to let the render thread know that the
	// session's gone away
	HRESULT IAudioSessionEvents.OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason)
	{
		if (DisconnectReason == AudioSessionDisconnectReason.DisconnectReasonDeviceRemoval)
		{
			// The stream was disconnected because the device we're rendering to was removed.
			//
			// We want to reset the stream switch complete event (so we'll block when the HandleStreamSwitchEvent function waits until the
			// default device changed event occurs).
			//
			// Note that we don't_ set the streamSwitchCompleteEvent - that will be set when the OnDefaultDeviceChanged event occurs.
			inStreamSwitch = true;
			streamSwitchEvent.Set();
		}
		if (DisconnectReason == AudioSessionDisconnectReason.DisconnectReasonFormatChanged)
		{
			// The stream was disconnected because the format changed on our render device.
			//
			// We want to flag that we're in a stream switch and then set the stream switch event (which breaks out of the renderer). We also
			// want to set the streamSwitchCompleteEvent because we're not going to see a default device changed event after this.
			inStreamSwitch = true;
			streamSwitchEvent.Set();
			streamSwitchCompleteEvent?.Set();
		}
		return HRESULT.S_OK;
	}

	HRESULT IAudioSessionEvents.OnSimpleVolumeChanged(float NewVolume, bool NewMute, in Guid EventContext) => HRESULT.S_OK;

	HRESULT IAudioSessionEvents.OnStateChanged(AudioSessionState NewState) => HRESULT.S_OK;

	// Crack open the mix format and determine what kind of samples are being rendered.
	private HRESULT CalculateMixFormatType()
	{
		if (mixFormat.Value.wFormatTag == WAVE_FORMAT.WAVE_FORMAT_PCM || mixFormat.Value.wFormatTag == WAVE_FORMAT.WAVE_FORMAT_EXTENSIBLE &&
			mixFormat.ToType<WAVEFORMATEXTENSIBLE>().SubFormat == KSDATAFORMAT_SUBTYPE_PCM)
		{
			if (mixFormat.Value.wBitsPerSample == 16)
			{
				SampleType = RenderSampleType.Pcm16Bit;
			}
			else
			{
				Console.Write("Unknown PCM integer sample type\n");
				return HRESULT.E_UNEXPECTED;
			}
		}
		else if (mixFormat.Value.wFormatTag == WAVE_FORMAT.WAVE_FORMAT_IEEE_FLOAT || mixFormat.Value.wFormatTag == WAVE_FORMAT.WAVE_FORMAT_EXTENSIBLE &&
			mixFormat.ToType<WAVEFORMATEXTENSIBLE>().SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT)
		{
			SampleType = RenderSampleType.Float;
		}
		else
		{
			Console.Write("unrecognized device format.\n");
			return HRESULT.E_UNEXPECTED;
		}
		return HRESULT.S_OK;
	}

	static readonly Guid KSDATAFORMAT_SUBTYPE_PCM = new(0x00000001, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);
	static readonly Guid KSDATAFORMAT_SUBTYPE_IEEE_FLOAT = new(0x00000003, 0x0000, 0x0010, 0x80, 0x00, 0x00, 0xaa, 0x00, 0x38, 0x9b, 0x71);

	private uint DoRenderThread()
	{
		using BeginEndEventContext coInit = new(() => CoInitializeEx(default, COINIT.COINIT_MULTITHREADED).Succeeded, () => { CoUninitialize(); return true; });
		if (!coInit.BeginSucceeded)
		{
			Console.Write("Unable to initialize COM in render thread\n");
		}

		SafeEventHandle?[] waitArray = new[] { shutdownEvent, streamSwitchEvent, audioSamplesReadyEvent };
		bool stillPlaying = true;
		while (stillPlaying)
		{
			stillPlaying = WaitForMultipleObjects(waitArray, false, INFINITE) switch
			{
				// We're done, exit the loop.
				WAIT_STATUS.WAIT_OBJECT_0 + 0 => false,
				// We've received a stream switch request.
				//
				// We need to stop the renderer, tear down the _audioClient and _renderClient objects and re-create them on the new. endpoint
				// if possible. If this fails, abort the thread.
				WAIT_STATUS.WAIT_OBJECT_0 + 1 => HandleStreamSwitchEvent().Succeeded,
				WAIT_STATUS.WAIT_OBJECT_0 + 2 => ProduceAudioFrames().Succeeded,
				_ => true
			};
		}

		return 0;
	}

	// Handle the stream switch.
	//
	// When a stream switch happens, we want to do several things in turn:
	//
	// 1) Stop the current renderer.
	// 2) Release any resources we have allocated (the audioClient, audioSessionControl (after unregistering for notifications) and renderClient).
	// 3) Wait until the default device has changed (or 500ms has elapsed). If we time out, we need to abort because the stream switch can't happen.
	// 4) Retrieve the new default endpoint for our role.
	// 5) Re-instantiate the audio client on that new endpoint.
	// 6) Retrieve the mix format for the new endpoint. If the mix format doesn't match the old endpoint's mix format, we need to abort
	//    because the stream switch can't happen.
	// 7) Re-initialize the audioClient.
	// 8) Re-register for session disconnect notifications and reset the stream switch complete event.
	private HRESULT HandleStreamSwitchEvent()
	{
		inStreamSwitch = false;

		try
		{
			// Step 1. Stop rendering.
			audioClient?.Stop();

			// Step 2. Release our resources. Note that we don't release the mix format, we need it for step 6.
			audioSessionControl?.UnregisterAudioSessionNotification(this);

			audioSessionControl = null;
			renderClient = null;

			// Step 3. Wait for the default device to change.
			//
			// There is a race between the session disconnect arriving and the new default device arriving (if applicable). Wait the shorter
			// of 500 milliseconds or the arrival of the new default device, then attempt to switch to the default device. In the case of a
			// format change (i.e. the default device does not change), we artificially generate a new default device notification so the
			// code will not needlessly wait 500ms before re-opening on the new format. (However, note below in step 6 that in this SDK
			// sample, we are unlikely to actually successfully absorb a format change, but a real audio application implementing stream
			// switching would re-format their pipeline to deliver the new format).
			if (!streamSwitchCompleteEvent?.Wait(500) ?? false)
			{
				Console.Write("Stream switch timeout - aborting...\n");
				return HRESULT.E_UNEXPECTED;
			}

			// Step 4. If we can't get the new endpoint, we need to abort the stream switch. If there IS a new device, we should be able to
			// retrieve it.
			endpoint = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, endpointRole);

			// Step 5 - Re-instantiate the audio client on the new endpoint.
			audioClient = endpoint.Activate<IAudioClient>(CLSCTX.CLSCTX_INPROC_SERVER);

			// Step 6 - Retrieve the new mix format.
			audioClient.GetMixFormat(out var wfxNew).ThrowIfFailed();

			// Note that this is an intentionally naive comparison. A more sophisticated comparison would compare the sample rate, channel
			// count and format and apply the appropriate conversions into the render pipeline.
			if (!mixFormat.Equals(wfxNew))
			{
				Console.Write("New mix format doesn't match old mix format. Aborting.\n");
				return HRESULT.E_UNEXPECTED;
			}

			// Step 7: Re-initialize the audio client.
			InitializeAudioEngine();

			// Step 8: Re-register for session disconnect notifications.
			audioSessionControl = audioClient.GetService<IAudioSessionControl>();
			audioSessionControl.RegisterAudioSessionNotification(this);

			// Reset the stream switch complete event because it's a manual reset event.
			streamSwitchCompleteEvent?.Reset();
			// And we're done. Start rendering again.
			audioClient.Start();
		}
		catch (Exception ex) { return ex.HResult; }
		return HRESULT.S_OK;
	}

	private void InitializeAudioEngine()
	{
		audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_NOPERSIST,
		engineLatencyInMS * 10000, 0, mixFormat).ThrowIfFailed();

		// Retrieve the buffer size for the audio client.
		BufferSize = audioClient.GetBufferSize();
		audioClient.SetEventHandle(audioSamplesReadyEvent);
		renderClient = audioClient.GetService<IAudioRenderClient>();

		if (enableAudioViewManagerService)
		{
			try
			{
				IAudioViewManagerService audioViewManagerService = audioClient.GetService<IAudioViewManagerService>();
				// Pass the window that this audio stream is associated with. This is used by the system for purposes such as rendering
				// spatial audio in Mixed Reality scenarios.
				audioViewManagerService.SetAudioStreamWindow(GetConsoleWindow());
				Console.Write("Audio stream has been associated with the console window\n");
			}
			catch
			{
				Console.Write("Unable to associate the audio stream with a window\n");
				throw;
			}
		}
	}

	private HRESULT ProduceAudioFrames()
	{
		// We need to provide the next buffer of samples to the audio renderer.
		try
		{
			// We want to find out how much of the ref buffer isn'ref t available (is padding).
			uint padding = audioClient.GetCurrentPadding();

			// Calculate the number of frames available. We'll render that many frames or the number of frames left in the buffer, whichever
			// is smaller.
			uint framesAvailable = BufferSize - padding;

			// Stop if we have nothing more to render.
			if (renderBufferQueue is null || renderBufferQueue.Count == 0)
			{
				return HRESULT.HRESULT_FROM_WIN32(Win32Error.ERROR_HANDLE_EOF);
			}

			// If the buffer at the head of the render buffer queue does not fit in the frames available, then skip this pass. We will have
			// more room on the next pass.
			if (renderBufferQueue.First().buffer.Length > framesAvailable * FrameSize)
			{
				return HRESULT.S_OK;
			}

			// Remove the first buffer from the head of the queue.
			RenderBuffer renderBuffer = renderBufferQueue.First();
			renderBufferQueue.RemoveFirst();

			// Copy data from the render buffer to the output buffer and bump our render pointer.
			uint framesToWrite = (uint)renderBuffer.buffer.Length / FrameSize;
			if (renderClient is not null)
			{
				IntPtr pData = renderClient.GetBuffer(framesToWrite);
				pData.Write(renderBuffer.buffer);
				renderClient.ReleaseBuffer(framesToWrite, 0);
			}
		}
		catch (Exception ex) { return ex.HResult; }
		return HRESULT.S_OK;
	}
	private void TerminateStreamSwitch()
	{
		// Unregistration can fail if InitializeStreamSwitch failed to register.
		audioSessionControl?.UnregisterAudioSessionNotification(this);

		// Unregistration can fail if InitializeStreamSwitch failed to register.
		deviceEnumerator?.UnregisterEndpointNotificationCallback(this);

		streamSwitchCompleteEvent?.Reset();
	}

	// Render thread - processes samples from the audio engine
	private uint WASAPIRenderThread(IntPtr Context)
	{
		CWASAPIRenderer? renderer = GCHandle.FromIntPtr(Context).Target as CWASAPIRenderer;
		return renderer?.DoRenderThread() ?? 0;
	}
}