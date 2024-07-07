using Vanara.PInvoke;
using static Vanara.PInvoke.Avrt;
using static Vanara.PInvoke.CoreAudio;
using static Vanara.PInvoke.Ole32;

namespace AEC;

internal class CAECCapture : IDisposable
{
	private const int REFTIMES_PER_SEC = 10000000;

	private static readonly Guid AUDIO_EFFECT_TYPE_ACOUSTIC_ECHO_CANCELLATION = new("6f64adbe-8211-11e2-8c70-2c27d7f001fa");
	private readonly Thread captureThread;
	private readonly AutoResetEvent terminationEvent;
	private readonly IAudioClient2 audioClient;
	private readonly IAudioCaptureClient? captureClient;

	public CAECCapture()
	{
		terminationEvent = new(false);

		IMMDeviceEnumerator enumerator = new();
		IMMDevice device = enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eCommunications) ?? throw new InvalidOperationException();
		audioClient = device!.Activate<IAudioClient2>(CLSCTX.CLSCTX_INPROC_SERVER);

		// Set the category as communications.
		AudioClientProperties clientProperties = new()
		{
			cbSize = (uint)Marshal.SizeOf(typeof(AudioClientProperties)),
			eCategory = AUDIO_STREAM_CATEGORY.AudioCategory_Communications
		};
		audioClient.SetClientProperties(clientProperties);

		audioClient.GetMixFormat(out var wfxCapture);

		long hnsRequestedDuration = REFTIMES_PER_SEC;
		audioClient.Initialize(AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
			AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
			hnsRequestedDuration,
			0,
			wfxCapture,
			default).ThrowIfFailed();

		bool aecEffectPresent = IsAcousticEchoCancellationEffectPresent();
		if (!aecEffectPresent)
		{
			Console.Write("Warning: Capture stream is not echo cancelled.\n");

			// An APO vendor can add code here to insert an in-app acoustic echo cancellation APO before starting the capture stream.
		}

		var deviceId = device!.GetId();
		Console.Write("Created communications stream on capture endpoint {0}\n", deviceId);

		captureClient = audioClient.GetService<IAudioCaptureClient>();

		captureThread = new(RecordCommunicationsStream);
		captureThread.Start();
	}

	public void Dispose()
	{
		terminationEvent.Set();
		captureThread.Join();
	}

	public void SetEchoCancellationRenderEndpoint(string aecReferenceEndpointId)
	{
		try
		{
			IAcousticEchoCancellationControl? aecControl = audioClient.GetService<IAcousticEchoCancellationControl>();

			// Call SetEchoCancellationRenderEndpoint to change the endpoint of the auxiliary input stream.
			aecControl!.SetEchoCancellationRenderEndpoint(aecReferenceEndpointId).ThrowIfFailed();
		}
		catch
		{
			// For this app, we ignore any failure to to control acoustic echo cancellation. (Treat as best effort.)
			Console.Write("Warning: Acoustic echo cancellation control is not available.\n");
		}
	}

	private bool IsAcousticEchoCancellationEffectPresent()
	{
		try
		{
			// IAudioEffectsManager requires build 22000 or higher.
			IAudioEffectsManager? audioEffectsManager = audioClient.GetService<IAudioEffectsManager>();
			if (audioEffectsManager is null)
			{
				// Audio effects manager is not supported, so clearly not present.
				return false;
			}

			audioEffectsManager.GetAudioEffects(out var peffects, out var numEffects);
			AUDIO_EFFECT[] effects = peffects.ToArray<AUDIO_EFFECT>((int)numEffects);

			for (uint i = 0; i < numEffects; i++)
			{
				// Check for acoustic echo cancellation Audio Processing Object (APO)
				if (effects[i].id == AUDIO_EFFECT_TYPE_ACOUSTIC_ECHO_CANCELLATION)
				{
					return true;
				}
			}
		}
		catch { }
		return false;
	}

	private void RecordCommunicationsStream()
	{
		uint mmcssTaskIndex = 0;
		using var mmcssTaskHandle = Win32Error.ThrowLastErrorIfInvalid(AvSetMmThreadCharacteristics("Audio", ref mmcssTaskIndex));

		AutoResetEvent bufferComplete = new(false);
		audioClient.SetEventHandle(bufferComplete.SafeWaitHandle.DangerousGetHandle());

		audioClient.Start();

		Console.Write("Started communications capture stream.\n");

		AutoResetEvent[] events = [terminationEvent, bufferComplete];

		while (!WaitHandle.WaitAll(events))
		{
			uint packetLength = 0;
			while (captureClient!.GetNextPacketSize(out packetLength).Succeeded && packetLength > 0)
			{
				captureClient.GetBuffer(out var buffer, out var numFramesRead, out var flags, out _, out _).ThrowIfFailed();

				// At this point, the app can send the buffer to the capture pipeline. This program just discards the buffer without
				// processing it.
				captureClient.ReleaseBuffer(numFramesRead);
			}
		}

		audioClient.Stop();
	}
}