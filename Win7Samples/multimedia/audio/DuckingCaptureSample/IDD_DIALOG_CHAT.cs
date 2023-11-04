using Vanara.PInvoke;

namespace DuckingCaptureSample
{
	public partial class IDD_DIALOG_CHAT : Form
	{
		private CChatTransport g_CurrentChat;
		private int g_WasapiComboBoxIndex = -1;
		private int g_WaveComboBoxIndex;

		public IDD_DIALOG_CHAT() => InitializeComponent();

		private enum ChatState
		{
			ChatStatePlaying,      // We're currently playing/capturing
			ChatStateNotPlaying,
		}

		public override bool PreProcessMessage(ref Message msg)
		{
			if (g_CurrentChat is not null)
			{
				if (g_CurrentChat.HandlesMessage(unchecked((uint)msg.Msg)) &&
					g_CurrentChat.MessageHandler(msg.HWnd, unchecked((uint)msg.Msg), msg.WParam, msg.LParam))
					return true;
			}
			return base.PreProcessMessage(ref msg);
		}

		private void IDC_CHATSTART_Click(object sender, EventArgs e)
		{
			if (g_CurrentChat.StartChat(IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Checked))
				SyncUIState(Handle, ChatState.ChatStatePlaying);
		}

		private void IDC_CHATSTOP_Click(object sender, EventArgs e)
		{
			g_CurrentChat.StopChat();
			SyncUIState(Handle, ChatState.ChatStateNotPlaying);
		}

		private void IDC_COMBO_CHAT_TRANSPORT_SelectedIndexChanged(object sender, EventArgs e)
		{
			var currentSel = IDC_COMBO_CHAT_TRANSPORT.SelectedIndex;

			// The user modified the chat transport. Delete the existing chat transport and create a new one.
			g_CurrentChat.Shutdown();
			g_CurrentChat = null;

			if (currentSel == g_WasapiComboBoxIndex)
			{
				// Instantiate the WASAPI transport.
				g_CurrentChat = new CWasapiChat(Handle);
			}
			else if (currentSel == g_WaveComboBoxIndex)
			{
				// Instantiate the wave transport.
				g_CurrentChat = new CWaveChat(Handle);
			}

			// Sync the UI to the transport choice
			SyncUIState(Handle, ChatState.ChatStateNotPlaying);

			// Initialize the chat object
			var useInputDevice = IDC_RADIO_CAPTURE.Checked;
			if (g_CurrentChat.Initialize(useInputDevice))
			{
				// Sync the UI to the state again - we're not playing but after initializing the state might change.
				SyncUIState(Handle, ChatState.ChatStateNotPlaying);
			}
			else
			{
				MessageBox.Show(this, "Unable to initialize chat", "Error", MessageBoxButtons.OK);
			}
		}

		private void IDC_RADIO_CAPTURE_CheckedChanged(object sender, EventArgs e)
		{
			var currentSel = IDC_COMBO_CHAT_TRANSPORT.SelectedIndex;

			// The radio button selection may change when the transport is changed to Wave because render is not an option for Wave. We
			// detect that here and only rebuild the transport for Wasapi
			if ((currentSel == g_WasapiComboBoxIndex) && (g_CurrentChat.TransportType == ChatTransportType.ChatTransportWasapi))
			{
				// The user switched between render and capture. Delete the existing chat transport and create a new one.
				g_CurrentChat.Shutdown();

				// Reinstantiate the WASAPI transport.
				//
				// Also update the state of the rendering options since the WASAPI transport supports them.
				g_CurrentChat = new CWasapiChat(Handle);
				g_CurrentChat.Initialize(IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Checked);
			}
			SyncUIState(Handle, ChatState.ChatStateNotPlaying);
		}

		private void IDD_DIALOG_CHAT_Load(object sender, EventArgs e)
		{
			// Start by using the wave transport for "chat".

			// Allocate the WAVE chat transport. If we failed to startup, we're done.
			g_CurrentChat = new CWaveChat(Handle);
			if (!g_CurrentChat.Initialize(true))
				Close();

			// Set up the combobox and initialize the chat options to reflect that we've set the Wave chat transport by default.
			g_WaveComboBoxIndex = IDC_COMBO_CHAT_TRANSPORT.Items.Add("WAVE API Transport");
			g_WasapiComboBoxIndex = IDC_COMBO_CHAT_TRANSPORT.Items.Add("WASAPI API Transport");
			IDC_COMBO_CHAT_TRANSPORT.SelectedIndex = g_WaveComboBoxIndex;

			// Simulate a "stop" event to get the UI in sync.
			SyncUIState(Handle, ChatState.ChatStateNotPlaying);
		}

		private void IDOK_Click(object sender, EventArgs e)
		{
			g_CurrentChat?.StopChat();
			g_CurrentChat?.Shutdown();
			g_CurrentChat = null;

			Close();
		}

		// Makes all of the dialog controls consistent with the current transport and specified chat state
		private void SyncUIState(HWND hWnd, ChatState State)
		{
			if (State == ChatState.ChatStatePlaying)
			{
				// Sync the UI to the state - Since we're playing, the only thing we can do is to hit the "Stop" button.
				IDC_CHATSTART.Enabled = false;
				IDC_CHATSTOP.Enabled = true;
				IDC_COMBO_CHAT_TRANSPORT.Enabled = false;
				IDC_RADIO_CAPTURE.Enabled = false;
				IDC_RADIO_RENDER.Enabled = false;
				IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Enabled = false;
			}
			else if (State == ChatState.ChatStateNotPlaying)
			{
				// Sync the UI to the state - since we're not playing all the options except stop become available.
				IDC_CHATSTART.Enabled = true;
				IDC_CHATSTOP.Enabled = false;
				IDC_COMBO_CHAT_TRANSPORT.Enabled = true;
				IDC_RADIO_CAPTURE.Enabled = true;

				// Now sync the transport options - the wave transport doesn't support output, so disable output device option when the the
				// current transport is the wave transport.
				//
				// Otherwise enable the "Use Output" and "hide from volume mixer" options
				//
				// Note that the "Hide from volume mixer" option is only valid if the "Use Output Device" box is checked.
				if (g_CurrentChat?.TransportType == ChatTransportType.ChatTransportWave)
				{
					IDC_RADIO_RENDER.Enabled = false;
					IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Enabled = false;
					IDC_RADIO_CAPTURE.Checked = true;
					IDC_RADIO_RENDER.Checked = false;
					IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Checked = false;
				}
				else
				{
					IDC_RADIO_RENDER.Enabled = true;
					IDC_CHECK_HIDE_FROM_VOLUME_MIXER.Enabled = IDC_RADIO_RENDER.Checked;
				}
			}
		}
	}
}