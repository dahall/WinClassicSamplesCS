using System;
using System.ComponentModel;
using System.Windows.Forms;
using static Vanara.PInvoke.CoreAudio;

namespace DuckingMediaPlayer
{
	public partial class DuckingMediaPlayerSample : Form
	{
		private CMediaPlayer g_MediaPlayer;

		public DuckingMediaPlayerSample() => InitializeComponent();

		protected override void OnClosing(CancelEventArgs e)
		{
			g_MediaPlayer.Shutdown();
			g_MediaPlayer = default;
			base.OnClosing(e);
		}

		protected override void WndProc(ref Message m)
		{
			switch ((uint)m.Msg)
			{
				// Let the media player know about the DShow graph event. If we come to the end of the track, reset the slider to the
				// beginning of the track.
				case WM_APP_GRAPHNOTIFY:
					{
						if (g_MediaPlayer.HandleGraphEvent())
						{
							// Reset the slider and timer, we're at the end of the track.
							IDC_SLIDER_PLAYBACKPOS.Value = 0;

							progressTimer.Stop();
							IDC_BUTTON_PLAY.Enabled = true;
							IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = false;
						}
						break;
					}

				// Called when the media player receives a ducking notification. Lets the media player know that the session has been ducked
				// and pauses the player.
				case WM_APP_SESSION_DUCKED:
					if (g_MediaPlayer.Pause())
					{
						IDC_BUTTON_PAUSE.Text = "Continue";
						progressTimer.Stop();
					}
					break;
				// Called when the media player receives an unduck notification. Lets the media player know that the session has been
				// unducked and continues the player.
				case WM_APP_SESSION_UNDUCKED:
					if (g_MediaPlayer.Continue())
					{
						IDC_BUTTON_PAUSE.Text = "Pause";
						progressTimer.Start();
					}
					break;
				// Process a session volume changed notification. Sync the UI elements with the values in the notification.
				//
				// The caller passes the new Mute state in wParam and the new volume value in lParam.
				case WM_APP_SESSION_VOLUME_CHANGED:
					{
						var newMute = m.WParam != IntPtr.Zero;
						var newVolume = LPARAM2FLOAT(m.LParam);
						var volumePos = (int)(newVolume * 100);
						IDC_SLIDER_VOLUME.Value = volumePos;
						IDC_CHECK_MUTE.Checked = newMute;
						break;
					}

				default:
					// If the current chat transport is going to handle this message, pass the message to the transport.
					//
					// Otherwise just let our caller know that they need to handle it.
					base.WndProc(ref m);
					break;
			}
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			g_MediaPlayer = new CMediaPlayer(this);
			try
			{
				g_MediaPlayer.Initialize();
			}
			catch
			{
				MessageBox.Show(this, "Unable to initialize media player", "Initialization Failure", MessageBoxButtons.OK);
				Close();
			}

			IDC_SLIDER_VOLUME.Value = (int)(g_MediaPlayer.Volume * 100.0f);

			IDC_CHECK_MUTE.Checked = g_MediaPlayer.Mute;
		}

		private void IDC_BUTTON_BROWSE_Click(object sender, EventArgs e)
		{
			// If the user hit the "Browse" button, bring up the file common dialog box.
			//
			// If the user hit "OK" to the dialog then update the edit control to include the filename and load the file into the player.
			var openFileDlg = new OpenFileDialog
			{
				FileName = IDC_EDIT_FILENAME.Text,
				CheckFileExists = true
			};

			if (openFileDlg.ShowDialog(this) == DialogResult.OK)
			{
				IDC_EDIT_FILENAME.Text = openFileDlg.FileName;
				// If we're playing (the stop button is enabled), stop playing.
				if (IDC_BUTTON_STOP.Enabled)
				{
					g_MediaPlayer.Stop();
					progressTimer.Stop();
					IDC_BUTTON_PLAY.Enabled = true;
					IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = false;
				}

				g_MediaPlayer.SetFileName(openFileDlg.FileName);
				IDC_BUTTON_PLAY.Enabled = true;
				IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = false;
			}
		}

		private void IDC_BUTTON_PAUSE_Click(object sender, EventArgs e)
		{
			// The user hit the "Pause/Continue" button.
			//
			// Toggle the "Pause" state in the player and update the button text as appropriate.
			if (g_MediaPlayer.TogglePauseState())
			{
				IDC_BUTTON_PAUSE.Text = "Continue";
				progressTimer.Stop();
			}
			else
			{
				IDC_BUTTON_PAUSE.Text = "Pause";
				progressTimer.Start();
			}
		}

		private void IDC_BUTTON_PLAY_Click(object sender, EventArgs e)
		{
			// The user hit the "Play" button.
			//
			// Sync the "Pause On Duck" and "Ducking Opt Out" buttons with the player and then start playback.
			//
			//
			// Then disable the "Play" button and enable the "Pause" and "Stop" buttons.
			g_MediaPlayer.SyncPauseOnDuck(IDC_CHECK_PAUSE_ON_DUCK.Checked);
			g_MediaPlayer.SyncDuckingOptOut(IDC_CHECK_DUCKING_OPT_OUT.Checked);
			g_MediaPlayer.Play();
			progressTimer.Start();
			IDC_BUTTON_PLAY.Enabled = false;
			IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = true;
		}

		private void IDC_BUTTON_STOP_Click(object sender, EventArgs e)
		{
			// The user hit the "Stop" button.
			//
			// Stop the player and stop the progress timer and enable the "Play" button.
			g_MediaPlayer.Stop();
			progressTimer.Stop();
			IDC_BUTTON_PLAY.Enabled = true;
			IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = false;
		}

		private void IDC_CHECK_DUCKING_OPT_OUT_CheckedChanged(object sender, EventArgs e) =>
			// The user checked the "Opt Out" option - ask the media player to sync to that state.
			g_MediaPlayer.SyncDuckingOptOut(IDC_CHECK_DUCKING_OPT_OUT.Checked);

		private void IDC_CHECK_MUTE_CheckedChanged(object sender, EventArgs e) => g_MediaPlayer.Mute = IDC_CHECK_MUTE.Checked;

		private void IDC_CHECK_PAUSE_ON_DUCK_CheckedChanged(object sender, EventArgs e) => g_MediaPlayer.SyncPauseOnDuck(IDC_CHECK_PAUSE_ON_DUCK.Checked);

		private void IDC_EDIT_FILENAME_Leave(object sender, EventArgs e)
		{
			// See if the user navigated away from the filename edit control - if so, load the file in the filename edit control.
			//
			//
			// If we're playing (the stop button is enabled), stop playing.
			if (IDC_BUTTON_STOP.Enabled)
			{
				g_MediaPlayer.Stop();
				progressTimer.Stop();
				IDC_BUTTON_PLAY.Enabled = true;
				IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = false;
			}

			if (g_MediaPlayer.SetFileName(IDC_EDIT_FILENAME.Text))
			{
				IDC_BUTTON_PLAY.Enabled = true;
				IDC_BUTTON_PAUSE.Enabled = IDC_BUTTON_STOP.Enabled = false;
			}
		}

		private void IDC_SLIDER_VOLUME_Scroll(object sender, EventArgs e)
		{
			var volumePosition = IDC_SLIDER_VOLUME.Value;
			g_MediaPlayer.Volume = volumePosition / 100.0f;
		}

		private void IDOK_Click(object sender, EventArgs e) => Close();

		private float LPARAM2FLOAT(IntPtr lParam) => (float)(lParam.ToInt64() / 100000.0f);

		private void progressTimer_Tick(object sender, EventArgs e)
		{
			// Update the progress slider to match the current playback position.
			var position = g_MediaPlayer.GetPosition();
			IDC_SLIDER_PLAYBACKPOS.Value = position;
		}
	}
}