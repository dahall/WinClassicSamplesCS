using DirectShowLib;
using System;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.CoreAudio;

namespace DuckingMediaPlayer
{
	internal class CMediaPlayer : IAudioVolumeDuckNotification, IAudioSessionEvents
	{
		private readonly IWin32Window AppWindow;
		private bool DuckingRegistered;
		private string FileName;
		private IGraphBuilder GraphBuilder;
		private IMediaEventEx MediaEvent;

		// Event context used for media player volume changes. This allows us to determine if a volume change request was initiated by this
		// media player application or some other application.
		private Guid MediaPlayerEventContext = Guid.NewGuid();

		private long MediaPlayerTime;
		private IMediaSeeking MediaSeeking;
		private IAudioSessionControl2 SessionControl2;
		private IAudioSessionManager2 SessionManager2;
		private bool SessionNotificationRegistered;
		private ISimpleAudioVolume SimpleVolume;

		public CMediaPlayer(IWin32Window handle) => AppWindow = handle;

		// Get or set the mute state for the current audio session.
		//
		// We set a specific event context on the SetMasterVolume call - when we receive the simple volume changed notification we can use
		// this event context to determine if the volume change call came from our application or another application.
		public bool Mute
		{
			get
			{
				HRESULT hr = GetSimpleVolume();
				if (hr.Succeeded)
				{
					try
					{
						var mute = SimpleVolume.GetMute();
						return (mute);
					}
					catch
					{
						MessageBox.Show(AppWindow, "Unable to retrieve mute for current session", "Get Mute Error", MessageBoxButtons.OK);
					}
				}
				else
				{
					MessageBox.Show(AppWindow, "Unable to retrieve simple volume control for current session", "Get Mute Error", MessageBoxButtons.OK);
				}
				return false;
			}

			set
			{
				HRESULT hr = GetSimpleVolume();
				if (hr.Succeeded)
				{
					try
					{
						SimpleVolume.SetMute(value, MediaPlayerEventContext);
					}
					catch
					{
						MessageBox.Show(AppWindow, "Unable to set mute for current session", "Set Mute Error", MessageBoxButtons.OK);
					}
				}
				else
				{
					MessageBox.Show(AppWindow, "Unable to retrieve simple volume control for current session", "Set Mute Error", MessageBoxButtons.OK);
				}
			}
		}

		// Get or set the volume on the current audio session.
		//
		// We set a specific event context on the SetMasterVolume call - when we receive the simple volume changed notification we can use
		// this event context to determine if the volume change call came from our application or another application.
		public float Volume
		{
			get
			{
				HRESULT hr = GetSimpleVolume();
				if (hr.Succeeded)
				{
					try
					{
						var volume = SimpleVolume.GetMasterVolume();
						return volume;
					}
					catch
					{
						MessageBox.Show(AppWindow, "Unable to retrieve volume for current session", "Get Volume Error", MessageBoxButtons.OK);
					}
				}
				else
				{
					MessageBox.Show(AppWindow, "Unable to retrieve simple volume control for current session", "Get Volume Error", MessageBoxButtons.OK);
				}
				return 0.0f;
			}

			set
			{
				HRESULT hr = GetSimpleVolume();
				if (hr.Succeeded)
				{
					try
					{
						SimpleVolume.SetMasterVolume(value, MediaPlayerEventContext);
					}
					catch
					{
						MessageBox.Show(AppWindow, "Unable to retrieve volume for current session", "Set Volume Error", MessageBoxButtons.OK);
					}
				}
				else
				{
					MessageBox.Show(AppWindow, "Unable to retrieve simple volume control for current session", "Set Volume Error", MessageBoxButtons.OK);
				}
			}
		}

		// Continues media playback if it is currently paused.
		public bool Continue()
		{
			var isContinued = false;
			var mediaControl = (IMediaControl)GraphBuilder;
			if (mediaControl is not null)
			{
				mediaControl.Run();
				isContinued = true;
			}
			return isContinued;
		}

		// Returns the position in the song being played in units 0..1000
		public int GetPosition()
		{
			if (MediaSeeking is not null && MediaPlayerTime != 0)
			{
				if (HRESULT.S_OK == MediaSeeking.GetCurrentPosition(out var position))
				{
					var sliderTick = (int)((position * 1000) / MediaPlayerTime);
					return sliderTick;
				}
			}
			return 0;
		}

		// Handles DirectShow graph events.
		//
		// Returns true if the player should be stopped.
		public bool HandleGraphEvent()
		{
			var stopped = false;
			// Disregard if we don't have an IMediaEventEx pointer.
			if (MediaEvent is null)
			{
				return stopped;
			}

			// Get all the events
			while (MediaEvent.GetEvent(out EventCode evCode, out IntPtr param1, out IntPtr param2, 0) == HRESULT.S_OK)
			{
				MediaEvent.FreeEventParams(evCode, param1, param2);
				switch (evCode)
				{
					case EventCode.Complete:
						{
							// Stop playback, we're done.
							{
								var mediaControl = (IMediaControl)GraphBuilder;
								mediaControl.Stop();
							}

							DsLong timeBegin = new(0);
							MediaSeeking.SetPositions(timeBegin, AMSeekingSeekingFlags.AbsolutePositioning, default, AMSeekingSeekingFlags.NoPositioning);
							stopped = true;
						}
						break;

					case EventCode.UserAbort: // Fall through.
					case EventCode.ErrorAbort:
						stopped = false;
						break;
				}
			}
			return stopped;
		}

		// Initialize the media player. Instantiates DShow, retrieves the session control for the current audio session and registers for
		// notifications on that session control.
		public void Initialize()
		{
			GraphBuilder = (IGraphBuilder)new FilterGraph();
			MediaEvent = (IMediaEventEx)GraphBuilder;
			MediaEvent.SetNotifyWindow(AppWindow.Handle, (int)WM_APP_GRAPHNOTIFY, default);
			MediaSeeking = (IMediaSeeking)GraphBuilder;
			GetSessionControl2();
			SessionControl2.RegisterAudioSessionNotification(this);
			SessionNotificationRegistered = true;
		}

		// Pauses media playback if it is currently running.
		public bool Pause()
		{
			var isPaused = false;
			var mediaControl = (IMediaControl)GraphBuilder;
			if (mediaControl is not null)
			{
				mediaControl.Pause();
				isPaused = true;
			}
			return isPaused;
		}

		// Starts the media player.
		public void Play()
		{
			var mediaControl = (IMediaControl)GraphBuilder;

			if (mediaControl is not null)
			{
				DsLong timeBegin = new(0);
				MediaSeeking.SetPositions(timeBegin, AMSeekingSeekingFlags.AbsolutePositioning, default, AMSeekingSeekingFlags.NoPositioning);

				mediaControl.Run();
			}
		}

		// Removes any filters in the audio graph - called before rebuilding the audio graph.
		public void RemoveAllFilters()
		{
			HRESULT hr = GraphBuilder.EnumFilters(out IEnumFilters enumFilters);

			if (hr.Succeeded)
			{
				var filter = new IBaseFilter[1];
				hr = enumFilters.Next(1, filter, default);
				while (hr == HRESULT.S_OK)
				{
					// Remove the filter from the graph.
					GraphBuilder.RemoveFilter(filter[0]);
					filter[0] = null;

					// Reset the enumeration since we removed the filter (which invalidates the enumeration).
					enumFilters.Reset();

					hr = enumFilters.Next(1, filter, default);
				}
			}
		}

		// Sets the file we're going to play.
		//
		// Returns true if the file can be played, false otherwise.
		public bool SetFileName(string FileName)
		{
			RemoveAllFilters();

			this.FileName = FileName;

			// Ask DirectShow to build a render graph for this file.
			HRESULT hr = GraphBuilder.RenderFile(FileName, default);
			if (hr.Failed)
			{
				MessageBox.Show(AppWindow, "Unable to build graph for media file", "Set Filename Error", MessageBoxButtons.OK);
				return false;
			}

			// If we can figure out the length of this track retrieve it.
			AMSeekingSeekingCapabilities caps = AMSeekingSeekingCapabilities.CanGetDuration;
			var canSeek = (HRESULT.S_OK == MediaSeeking.CheckCapabilities(ref caps));
			if (canSeek)
			{
				MediaSeeking.GetDuration(out MediaPlayerTime);
			}
			return true;
		}

		// Shut down the media player - releases all the resources associated with the media player.
		public void Shutdown()
		{
			MediaEvent = null;
			MediaSeeking = null;
			GraphBuilder = null;

			if (SessionManager2 is not null)
			{
				if (DuckingRegistered)
				{
					SessionManager2.UnregisterDuckNotification(this);
					DuckingRegistered = false;
				}
				SessionManager2 = default;
			}

			SimpleVolume = null;

			if (SessionControl2 is not null)
			{
				if (SessionNotificationRegistered)
				{
					SessionControl2.UnregisterAudioSessionNotification(this);
					SessionNotificationRegistered = false;
				}
				SessionControl2 = default;
			}

			FileName = default;
		}

		// Stops media playback.
		public void Stop()
		{
			var mediaControl = (IMediaControl)GraphBuilder;
			if (mediaControl is not null)
			{
				mediaControl.Stop();
			}
		}

		// Sync the "Ducking Opt Out" state with the UI - either enable or disable ducking for this session.
		public void SyncDuckingOptOut(bool DuckingOptOutChecked)
		{
			HRESULT hr = GetSessionControl2();

			// Sync our ducking state to the UI.
			if (hr.Succeeded)
			{
				try
				{
					SessionControl2.SetDuckingPreference(DuckingOptOutChecked);
				}
				catch
				{
					MessageBox.Show(AppWindow, "Unable to update the ducking preference", "Sync Ducking State Error", MessageBoxButtons.OK);
				}
			}
		}

		// Sync's the "Pause On Duck" state for the media player.
		//
		// Either registers or unregisters for ducking notification.
		public void SyncPauseOnDuck(bool PauseOnDuckChecked)
		{
			HRESULT hr = GetSessionManager2();

			// Retrieve the current session ID. We'll use that to request that the ducking manager filter our notifications (so we only see
			// ducking notifications for our session).
			if (hr.Succeeded)
			{
				hr = GetCurrentSessionId(out var sessionId);

				// And either register or unregister for ducking notifications based on whether or not the Pause On Duck state is checked.
				if (hr.Succeeded)
				{
					if (PauseOnDuckChecked)
					{
						if (!DuckingRegistered)
						{
							try
							{
								SessionManager2.RegisterDuckNotification(sessionId, this);
								DuckingRegistered = true;
							}
							catch { }
						}
					}
					else
					{
						if (DuckingRegistered)
						{
							try
							{
								SessionManager2.UnregisterDuckNotification(this);
								DuckingRegistered = false;
							}
							catch { }
						}
					}

					if (hr.Failed)
					{
						MessageBox.Show(AppWindow, "Unable to register or unregister for ducking notifications", "Sync Ducking Pause Error", MessageBoxButtons.OK);
					}
				}
			}
		}

		// Toggle the pause state for the media player. Returns true if the media player pauses, false if it runs.
		public bool TogglePauseState()
		{
			var isPaused = false;
			var mediaControl = (IMediaControl)GraphBuilder;
			if (mediaControl is not null)
			{
				mediaControl.GetState(unchecked((int)Kernel32.INFINITE), out FilterState filterState);
				if (filterState == FilterState.Running)
				{
					mediaControl.Pause();
					isPaused = true;
				}
				else if (filterState == FilterState.Paused)
				{
					mediaControl.Run();
					isPaused = false;
				}
			}
			return isPaused;
		}

		HRESULT IAudioSessionEvents.OnChannelVolumeChanged(uint ChannelCount, float[] NewChannelVolumeArray, uint ChangedChannel, in Guid EventContext) => HRESULT.S_OK;

		HRESULT IAudioSessionEvents.OnDisplayNameChanged(string NewDisplayName, in Guid EventContext) => HRESULT.S_OK;

		HRESULT IAudioSessionEvents.OnGroupingParamChanged(in Guid NewGroupingParam, in Guid EventContext) => HRESULT.S_OK;

		HRESULT IAudioSessionEvents.OnIconPathChanged(string NewIconPath, in Guid EventContext) => HRESULT.S_OK;

		HRESULT IAudioSessionEvents.OnSessionDisconnected(AudioSessionDisconnectReason DisconnectReason) => HRESULT.S_OK;

		HRESULT IAudioSessionEvents.OnSimpleVolumeChanged(float NewSimpleVolume, bool NewMute, in Guid EventContext)
		{
			if (EventContext != MediaPlayerEventContext)
			{
				PostMessage(AppWindow, WM_APP_SESSION_VOLUME_CHANGED, NewMute ? (IntPtr)1 : default, FLOAT2LPARAM(NewSimpleVolume));
			}
			return HRESULT.S_OK;
		}

		HRESULT IAudioSessionEvents.OnStateChanged(AudioSessionState NewState) => HRESULT.S_OK;

		// When we receive a duck notification, post a "Session Ducked" message to the application window.
		HRESULT IAudioVolumeDuckNotification.OnVolumeDuckNotification(string sessionID, uint countCommunicationSessions)
		{
			PostMessage(AppWindow, WM_APP_SESSION_DUCKED);
			return 0;
		}

		// When we receive an unduck notification, post a "Session Unducked" message to the application window.
		HRESULT IAudioVolumeDuckNotification.OnVolumeUnduckNotification(string sessionID)
		{
			PostMessage(AppWindow, WM_APP_SESSION_UNDUCKED);
			return 0;
		}

		private IntPtr FLOAT2LPARAM(float value) => (IntPtr)(int)(value * 100000.0f);

		// Utility function to retrieve the Session ID for the current audio session.
		private HRESULT GetCurrentSessionId(out string SessionId)
		{
			SessionId = null;
			HRESULT hr = GetSessionControl2();
			if (hr.Succeeded)
			{
				using Vanara.InteropServices.SafeCoTaskMemString sessId = SessionControl2.GetSessionInstanceIdentifier();
				SessionId = sessId;
			}
			return hr;
		}

		// Utility function to retrieve the session control interface for the current audio session.
		//
		// We assume that DirectShow uses the default session Guid and doesn't specify any session specific flags.
		private HRESULT GetSessionControl2()
		{
			HRESULT hr = HRESULT.S_OK;
			if (SessionControl2 is null)
			{
				hr = GetSessionManager2();
				if (hr.Succeeded)
				{
					try
					{
						IAudioSessionControl sessionControl = SessionManager2.GetAudioSessionControl(default, 0);
						SessionControl2 = (IAudioSessionControl2)sessionControl;
					}
					catch
					{
						MessageBox.Show(AppWindow, "Unable to QI for SessionControl2", "Get SessionControl Error", MessageBoxButtons.OK);
					}
				}
			}
			return hr;
		}

		// Utility function to retrieve the session manager for the default audio endpoint.
		private HRESULT GetSessionManager2()
		{
			HRESULT hr = HRESULT.S_OK;
			if (SessionManager2 is null)
			{
				var deviceEnumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();

				// Start with the default endpoint.
				IMMDevice endpoint = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole);

				SessionManager2 = endpoint.Activate<IAudioSessionManager2>(Ole32.CLSCTX.CLSCTX_INPROC_SERVER);
			}
			return hr;
		}

		// Utility function to retrieve the simple volume control interface for the current audio session.
		//
		// We assume that DirectShow uses the default session Guid and doesn't specify any session specific flags.
		private HRESULT GetSimpleVolume()
		{
			HRESULT hr = HRESULT.S_OK;
			if (SimpleVolume is null)
			{
				hr = GetSessionManager2();
				if (hr.Succeeded)
				{
					try { SimpleVolume = SessionManager2.GetSimpleAudioVolume(default, 0); }
					catch
					{
						MessageBox.Show(AppWindow, "Unable to get Simple Volume", "Get Simple Volume Error", MessageBoxButtons.OK);
					}
				}
			}
			return hr;
		}

		private void PostMessage<T>(IWin32Window appWindow, T msg, IntPtr wParam = default, IntPtr lParam = default) where T : IConvertible =>
			User32.PostMessage(appWindow.Handle, msg.ToUInt32(null), wParam, lParam);
	}
}