using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace DuckingCaptureSample
{
	public enum MultimediaMessage
	{
		MM_JOY1MOVE         = 0x3A0,  /* joystick */
		MM_JOY2MOVE         = 0x3A1,
		MM_JOY1ZMOVE        = 0x3A2,
		MM_JOY2ZMOVE        = 0x3A3,
		MM_JOY1BUTTONDOWN   = 0x3B5,
		MM_JOY2BUTTONDOWN   = 0x3B6,
		MM_JOY1BUTTONUP     = 0x3B7,
		MM_JOY2BUTTONUP     = 0x3B8,
		MM_MCINOTIFY        = 0x3B9,  /* MCI */
		MM_WOM_OPEN         = 0x3BB,  /* waveform output */
		MM_WOM_CLOSE        = 0x3BC,
		MM_WOM_DONE         = 0x3BD,
		MM_WIM_OPEN         = 0x3BE,  /* waveform input */
		MM_WIM_CLOSE        = 0x3BF,
		MM_WIM_DATA         = 0x3C0,
		MM_MIM_OPEN         = 0x3C1,  /* MIDI input */
		MM_MIM_CLOSE        = 0x3C2,
		MM_MIM_DATA         = 0x3C3,
		MM_MIM_LONGDATA     = 0x3C4,
		MM_MIM_ERROR        = 0x3C5,
		MM_MIM_LONGERROR    = 0x3C6,
		MM_MOM_OPEN         = 0x3C7,  /* MIDI output */
		MM_MOM_CLOSE        = 0x3C8,
		MM_MOM_DONE         = 0x3C9,
		MM_DRVM_OPEN        = 0x3D0,  /* installable drivers */
		MM_DRVM_CLOSE       = 0x3D1,
		MM_DRVM_DATA        = 0x3D2,
		MM_DRVM_ERROR       = 0x3D3,
		MM_STREAM_OPEN      = 0x3D4,
		MM_STREAM_CLOSE     = 0x3D5,
		MM_STREAM_DONE      = 0x3D6,
		MM_STREAM_ERROR     = 0x3D7,
		MM_MOM_POSITIONCB   = 0x3CA,  /* Callback for MEVT_POSITIONCB */
		MM_MCISIGNAL        = 0x3CB,
		MM_MIM_MOREDATA     = 0x3CC, /* MIM_DONE w/ pending events */
		MM_MIXM_LINE_CHANGE     = 0x3D0,       /* mixer line change notify */
		MM_MIXM_CONTROL_CHANGE  = 0x3D1,       /* mixer control change notify */
	}

	enum ChatTransportType
	{
		ChatTransportWave,
		ChatTransportWasapi
	}
	
	abstract class CChatTransport
	{
		public HWND AppWindow { get; }
		protected CChatTransport(HWND appWin)
		{
			AppWindow = appWin;
		}

		public virtual bool HandlesMessage(uint msg) => false;
		public virtual bool MessageHandler(HWND hwnd, uint msg, IntPtr wParam, IntPtr lParam) => false;
		public abstract bool Initialize(bool UseCaptureDevice);
		public abstract void Shutdown();
		public abstract bool StartChat(bool HideFromVolumeMixer);
		public abstract void StopChat();
		public abstract ChatTransportType TransportType { get; }

		protected void MessageBox(HWND hwnd, string msg, string caption = "Error") =>
			User32.MessageBox(hwnd, msg, caption, User32.MB_FLAGS.MB_OK);

	}
}
