using Vanara.PInvoke;
using static Vanara.PInvoke.CoreAudio;
using static Vanara.PInvoke.Kernel32;

namespace DuckingCaptureSample;

internal class CWasapiChat : CChatTransport, IDisposable
{
	private readonly GCHandleProvider hPtr;
	private IAudioClient AudioClient;
	private SafeEventHandle AudioSamplesReadyEvent;
	private IAudioCaptureClient CaptureClient;
	private IMMDevice ChatEndpoint;
	private SafeHTHREAD ChatThread;
	private EDataFlow Flow;
	private IAudioRenderClient RenderClient;
	private SafeEventHandle ShutdownEvent;

	public CWasapiChat(HWND appWin) : base(appWin)
	{
		Flow = EDataFlow.eRender;
		hPtr = new GCHandleProvider(this);
	}

	public override ChatTransportType TransportType => ChatTransportType.ChatTransportWasapi;

	public void Dispose()
	{
		ChatEndpoint = null;
		RenderClient = null;
		CaptureClient = null;
		AudioClient = null;

		if (ChatThread is not null)
		{
			ChatThread.Dispose();
			ChatThread = default;
		}
		if (ShutdownEvent is not null)
		{
			ShutdownEvent.Dispose();
			ShutdownEvent = null;
		}
		if (AudioSamplesReadyEvent is not null)
		{
			AudioSamplesReadyEvent.Dispose();
			AudioSamplesReadyEvent = null;
		}
	}

	public override bool Initialize(bool UseInputDevice)
	{
		IMMDeviceEnumerator deviceEnumerator = new();

		Flow = UseInputDevice ? EDataFlow.eCapture : EDataFlow.eRender;

		ChatEndpoint = deviceEnumerator.GetDefaultAudioEndpoint(Flow, ERole.eCommunications);
		deviceEnumerator = null;

		// Create our shutdown event - we want an auto reset event that starts in the not-signaled state.
		ShutdownEvent = CreateEventEx(default, default, 0, ACCESS_MASK.SYNCHRONIZE | (uint)SynchronizationObjectAccess.EVENT_MODIFY_STATE);
		if (ShutdownEvent.IsNull)
		{
			MessageBox(AppWindow, "Unable to create shutdown event.", "WASAPI Transport Initialize Failure");
			return false;
		}

		AudioSamplesReadyEvent = CreateEventEx(default, default, 0, ACCESS_MASK.SYNCHRONIZE | (uint)SynchronizationObjectAccess.EVENT_MODIFY_STATE);
		if (ShutdownEvent.IsNull)
		{
			MessageBox(AppWindow, "Unable to create samples ready event.", "WASAPI Transport Initialize Failure");
			return false;
		}

		return true;
	}

	public override void Shutdown()
	{
		if (ChatThread is not null)
		{
			SetEvent(ShutdownEvent);
			WaitForSingleObject(ChatThread, INFINITE);
			ChatThread.Dispose();
			ChatThread = default;
		}
		if (ShutdownEvent is not null)
		{
			ShutdownEvent.Dispose();
			ShutdownEvent = default;
		}
		if (AudioSamplesReadyEvent is not null)
		{
			AudioSamplesReadyEvent.Dispose();
			AudioSamplesReadyEvent = default;
		}
		ChatEndpoint = null;
		AudioClient = null;
		RenderClient = null;
		CaptureClient = null;
	}

	public override bool StartChat(bool HideFromVolumeMixer)
	{
		try
		{
			AudioClient = ChatEndpoint.Activate<IAudioClient>(Ole32.CLSCTX.CLSCTX_INPROC_SERVER);
			HRESULT hr = AudioClient.GetMixFormat(out var mixFormat);
			if (hr.Failed)
			{
				MessageBox(AppWindow, "Unable to get mix format on audio client.", "WASAPI Transport Start Failure");
				return false;
			}

			// Initialize the chat transport - Initialize WASAPI in event driven mode, associate the audio client with our samples ready
			// event handle, retrieve a capture/render client for the transport, create the chat thread and start the audio engine.
			var chatGuid = Guid.NewGuid();

			hr = AudioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
				(AUDCLNT_STREAMFLAGS)(HideFromVolumeMixer ? AUDCLNT_SESSIONFLAGS.AUDCLNT_SESSIONFLAGS_DISPLAY_HIDE : 0)
				| AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_EVENTCALLBACK | AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_NOPERSIST,
				500000, 0, mixFormat, chatGuid);

			if (hr.Failed)
			{
				MessageBox(AppWindow, "Unable to initialize audio client.", "WASAPI Transport Start Failure");
				return false;
			}

			AudioClient.SetEventHandle(AudioSamplesReadyEvent);

			if (Flow == EDataFlow.eRender)
			{
				RenderClient = AudioClient.GetService<IAudioRenderClient>();
			}
			else
			{
				CaptureClient = AudioClient.GetService<IAudioCaptureClient>();
			}

			// Now create the thread which is going to drive the "Chat".
			ChatThread = CreateThread(default, 0, WasapiChatThread, hPtr, 0, out _);
			if (ChatThread?.IsInvalid ?? true)
			{
				MessageBox(AppWindow, "Unable to create transport thread.", "WASAPI Transport Start Failure");
				return false;
			}

			// For render, we want to pre-roll a frames worth of silence into the pipeline. That way the audio engine won't glitch on startup.
			if (Flow == EDataFlow.eRender)
			{
				var framesAvailable = AudioClient.GetBufferSize();
				IntPtr pData = RenderClient.GetBuffer(framesAvailable);
				RenderClient.ReleaseBuffer(framesAvailable, AUDCLNT_BUFFERFLAGS.AUDCLNT_BUFFERFLAGS_SILENT);
			}

			// We're ready to go, start the chat!
			AudioClient.Start();

			return true;
		}
		catch
		{
			return false;
		}
	}

	public override void StopChat()
	{
		// Tell the chat thread to shut down, wait for the thread to complete then clean up all the stuff we allocated in StartChat().
		if (ShutdownEvent is not null)
		{
			ShutdownEvent.Set();
		}
		if (ChatThread is not null)
		{
			WaitForSingleObject(ChatThread, INFINITE);

			ChatThread.Dispose();
			ChatThread = default;
		}

		RenderClient = null;
		CaptureClient = null;
		AudioClient = null;
	}

	private static uint WasapiChatThread(IntPtr Context)
	{
		var stillPlaying = true;
		CWasapiChat chat = GCHandleProvider.GetTarget<CWasapiChat>(Context);
		ISyncHandle[] waitArray = { chat.ShutdownEvent, chat.AudioSamplesReadyEvent };

		while (stillPlaying)
		{
			WAIT_STATUS waitResult = WaitForMultipleObjects(waitArray, false, INFINITE);
			switch (waitResult)
			{
				case WAIT_STATUS.WAIT_OBJECT_0:
					stillPlaying = false; // We're done, exit the loop.
					break;

				case WAIT_STATUS.WAIT_OBJECT_0 + 1:
					// Either stream silence to the audio client or ignore the audio samples.
					//
					// Note that we don't check for errors here. This is because (a) there's no way of reporting the failure (b) once
					// the streaming engine has started there's really no way for it to fail.
					try
					{
						if (chat.Flow == EDataFlow.eRender)
						{
							var framesAvailable = chat.AudioClient.GetCurrentPadding();
							chat.RenderClient.GetBuffer(framesAvailable);
							chat.RenderClient.ReleaseBuffer(framesAvailable, AUDCLNT_BUFFERFLAGS.AUDCLNT_BUFFERFLAGS_SILENT);
						}
						else
						{
							var framesAvailable = chat.AudioClient.GetCurrentPadding();
							chat.CaptureClient.GetBuffer(out IntPtr pData, out framesAvailable, out AUDCLNT_BUFFERFLAGS flags, out _, out _);
							chat.CaptureClient.ReleaseBuffer(framesAvailable);
						}
					}
					catch { }
					break;
			}
		}
		return 0;
	}
}