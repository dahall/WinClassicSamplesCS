using System;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.WinMm;

namespace DuckingCaptureSample
{
	internal class CWaveChat : CChatTransport
	{
		public const uint WAVE_MAPPER = unchecked((uint)-1);
		private IntPtr waveBuffer1, waveBuffer2;
		private SafeHWAVEIN waveHandle;
		private WAVEHDR waveHeader1, waveHeader2;

		public CWaveChat(HWND appWin) : base(appWin)
		{
		}

		public override ChatTransportType TransportType => ChatTransportType.ChatTransportWave;

		public override bool HandlesMessage(uint msg) => (MultimediaMessage)msg switch
		{
			MultimediaMessage.MM_WIM_OPEN or MultimediaMessage.MM_WIM_CLOSE or MultimediaMessage.MM_WIM_DATA => true,
			_ => false,
		};

		public override bool Initialize(bool UseInputDevice)
		{
			if (!UseInputDevice)
			{
				MessageBox(AppWindow, "Wave Chat can only run on the input device", "Failed to initialize chat");
				return false;
			}
			if (waveInGetNumDevs() == 0)
			{
				MessageBox(default, "No Capture Devices found", "Failed to initialize chat");
				return false;
			}
			return true;
		}

		public override bool MessageHandler(HWND hWnd, uint message, IntPtr wParam, IntPtr lParam)
		{
			switch (message)
			{
				case (uint)MultimediaMessage.MM_WIM_OPEN:
					return true;

				case (uint)MultimediaMessage.MM_WIM_CLOSE:
					return true;

				case (uint)MultimediaMessage.MM_WIM_DATA:
					// Process the capture data since we've received a buffers worth of data.
					//
					// In real life, we'd copy the capture data out of the waveHeader that just completed and process it, but since this is
					// a sample, we discard the data and simply re-submit the buffer.
					MMRESULT mmr;
					HWAVEIN waveHandle = wParam;
					if (!waveHandle.IsNull)
					{
						ref WAVEHDR waveHeader = ref lParam.AsRef<WAVEHDR>();
						mmr = waveInAddBuffer(waveHandle, ref waveHeader, (uint)Marshal.SizeOf<WAVEHDR>());
						if (mmr != MMRESULT.MMSYSERR_NOERROR)
						{
							MessageBox(hWnd, "Failed to add buffer");
						}
					}
					return true;
			}
			return false;
		}

		public override void Shutdown()
		{
			if (waveHandle is not null && !waveHandle.IsInvalid)
			{
				MMRESULT mmr = waveInStop(waveHandle);
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to start");
				}

				mmr = waveInReset(waveHandle);
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to reset");
				}

				mmr = waveInUnprepareHeader(waveHandle, ref waveHeader1, (uint)Marshal.SizeOf(waveHeader1));
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to unprepare wave header 1");
				}
				Marshal.FreeCoTaskMem(waveBuffer1);

				mmr = waveInUnprepareHeader(waveHandle, ref waveHeader2, (uint)Marshal.SizeOf(waveHeader2));
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to unprepare wave header 2");
				}
				Marshal.FreeCoTaskMem(waveBuffer2);

				waveHandle.Dispose();
				waveHandle = default;
			}
		}

		public override bool StartChat(bool HideFromVolumeMixer)
		{
			WAVEFORMATEX waveFormat = new()
			{
				nSamplesPerSec = 44100,
				nChannels = 2,
				wBitsPerSample = 16,
				wFormatTag = WAVE_FORMAT.WAVE_FORMAT_PCM
			};
			waveFormat.nBlockAlign = (ushort)(waveFormat.wBitsPerSample / 8 * waveFormat.nChannels);
			waveFormat.nAvgBytesPerSec = waveFormat.nSamplesPerSec * waveFormat.nBlockAlign;

			MMRESULT mmr = waveInOpen(out waveHandle, WAVE_MAPPER, waveFormat,
				(IntPtr)AppWindow, default,
				WAVE_OPEN.CALLBACK_WINDOW | WAVE_OPEN.WAVE_MAPPED_DEFAULT_COMMUNICATION_DEVICE);
			if (mmr != MMRESULT.MMSYSERR_NOERROR)
			{
				MessageBox(AppWindow, "Failed to open wave in");
				return false;
			}

			waveBuffer1 = Marshal.AllocCoTaskMem((int)waveFormat.nAvgBytesPerSec);
			waveHeader1.dwBufferLength = waveFormat.nAvgBytesPerSec;
			waveHeader1.lpData = waveBuffer1;

			mmr = waveInPrepareHeader(waveHandle, ref waveHeader1, (uint)Marshal.SizeOf(waveHeader1));
			if (mmr != MMRESULT.MMSYSERR_NOERROR)
			{
				MessageBox(AppWindow, "Failed to prepare header 1");
				return false;
			}

			mmr = waveInAddBuffer(waveHandle, ref waveHeader1, (uint)Marshal.SizeOf(waveHeader1));
			if (mmr != MMRESULT.MMSYSERR_NOERROR)
			{
				MessageBox(AppWindow, "Failed to add buffer 1");
				return false;
			}

			waveBuffer2 = Marshal.AllocCoTaskMem((int)waveFormat.nAvgBytesPerSec);
			waveHeader2.dwBufferLength = waveFormat.nAvgBytesPerSec;
			waveHeader2.lpData = waveBuffer2;

			mmr = waveInPrepareHeader(waveHandle, ref waveHeader2, (uint)Marshal.SizeOf(waveHeader2));
			if (mmr != MMRESULT.MMSYSERR_NOERROR)
			{
				MessageBox(default, "Failed to prepare header 2");
				return false;
			}

			mmr = waveInAddBuffer(waveHandle, ref waveHeader2, (uint)Marshal.SizeOf(waveHeader2));
			if (mmr != MMRESULT.MMSYSERR_NOERROR)
			{
				MessageBox(AppWindow, "Failed to add buffer 2");
				return false;
			}

			mmr = waveInStart(waveHandle);
			if (mmr != MMRESULT.MMSYSERR_NOERROR)
			{
				MessageBox(AppWindow, "Failed to start");
				return false;
			}
			return true;
		}

		public override void StopChat()
		{
			if (waveHandle is not null && !waveHandle.IsInvalid)
			{
				MMRESULT mmr = waveInStop(waveHandle);
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to start");
				}

				mmr = waveInReset(waveHandle);
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to reset");
				}
				mmr = waveInUnprepareHeader(waveHandle, ref waveHeader1, (uint)Marshal.SizeOf(waveHeader1));
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to unprepare wave header 1");
				}
				Marshal.FreeCoTaskMem(waveBuffer1);

				mmr = waveInUnprepareHeader(waveHandle, ref waveHeader2, (uint)Marshal.SizeOf(waveHeader2));
				if (mmr != MMRESULT.MMSYSERR_NOERROR)
				{
					MessageBox(default, "Failed to unprepare wave header 2");
				}
				Marshal.FreeCoTaskMem(waveBuffer2);

				waveHandle.Dispose();
				waveHandle = default;
			}
		}
	}
}