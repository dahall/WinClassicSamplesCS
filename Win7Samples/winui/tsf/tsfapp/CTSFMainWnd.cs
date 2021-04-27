using System;
using System.Collections;
using System.Linq;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.MSCTF;

namespace tsfapp
{
	/// <summary>The form.</summary>
	public partial class CTSFMainWnd : Form
	{
		// get the keystroke manager interfce
		private readonly ITfKeystrokeMgr pKeyMgr = (ITfKeystrokeMgr)Program.g_pThreadMgr.Value;
		// get the message pump wrapper interface
		private readonly ITfMessagePump pMsgPump = (ITfMessagePump)Program.g_pThreadMgr.Value;


		/// <summary>Initializes a new instance of the <see cref="CTSFMainWnd"/> class.</summary>
		public CTSFMainWnd()
		{
			InitializeComponent();
			//Controls.Add(m_pTSFEditWnd = new CTSFEditWnd() { Dock = DockStyle.Fill, TfClientID = clientId, Text = "Helllo" });
			m_pTSFEditWnd.StatusUpdate += OnEditStatusUpdate;

			IDM_EXIT.Click += (s, e) => Close();
			IDM_ABOUT.Click += (s, e) => ShowMsg();
			IDM_GETPRESERVEDKEY.Click += (s, e) => m_pTSFEditWnd.OnGetPreservedKey();
			IDM_GETDISPATTR.Click += (s, e) => ShowMsg(string.Join(",", m_pTSFEditWnd.DisplayAttributes));
			IDM_GET_TEXTOWNER.Click += (s, e) => ShowMsg(string.Join(",", m_pTSFEditWnd.TextOwner));
			IDM_GET_READING.Click += (s, e) => ShowMsg(string.Join(",", m_pTSFEditWnd.ReadingText));
			IDM_GET_COMPOSING.Click += (s, e) => ShowMsg(m_pTSFEditWnd.Composing);
			IDM_TERMINATE_COMPOSITION.Click += (s, e) => m_pTSFEditWnd.TerminateAllCompositions();
			IDM_RECONVERT.Click += (s, e) => m_pTSFEditWnd.Reconvert();
			IDM_PLAYBACK.Click += (s, e) => m_pTSFEditWnd.Playback();
			IDM_LOAD.Click += (s, e) => { if (GetFileName(true, out var fn)) m_pTSFEditWnd.LoadFromFile(fn); };
			IDM_SAVE.Click += (s, e) => { if (GetFileName(false, out var fn)) m_pTSFEditWnd.SaveToFile(fn); };
			//IDM_TEST.Click += (s, e) => m_pTSFEditWnd.OnTest();
		}

		/// <summary>Pres the process message.</summary>
		/// <param name="m">The m.</param>
		/// <returns></returns>
		public override bool PreProcessMessage(ref Message m)
		{
			/*
			Get the next message in the queue. fResult receives false if WM_QUIT is encountered
			*/
			try
			{
				var fResult = pMsgPump.PeekMessageW(out var pumpMsg, m_pTSFEditWnd.Handle, (uint)User32.WindowMessage.WM_KEYDOWN, (uint)User32.WindowMessage.WM_KEYUP, User32.PM.M_NOREMOVE);
				switch (pumpMsg.message)
				{
					case (uint)User32.WindowMessage.WM_KEYDOWN:
						// does an ime want it?
						if (pKeyMgr.TestKeyDown(pumpMsg.wParam, pumpMsg.lParam) && pKeyMgr.KeyDown(pumpMsg.wParam, pumpMsg.lParam))
							return true;
						break;
					case (uint)User32.WindowMessage.WM_KEYUP:
						// does an ime want it?
						if (pKeyMgr.TestKeyUp(pumpMsg.wParam, pumpMsg.lParam) && pKeyMgr.KeyUp(pumpMsg.wParam, pumpMsg.lParam))
							return true;
						break;
				}
			}
			catch
			{
			}

			return base.PreProcessMessage(ref m);
		}

		private bool GetFileName(bool open, out string szFile)
		{
			szFile = null;
			if (open)
			{
				var ofd = new OpenFileDialog { CheckFileExists = true, CheckPathExists = true, DefaultExt = "tsf", Filter = "TSFApp Files (*.tsf)|*.tsf" };
				if (ofd.ShowDialog(this) == DialogResult.OK)
				{
					szFile = ofd.FileName;
					return true;
				}
			}
			else
			{
				var sfd = new SaveFileDialog { CheckPathExists = true, DefaultExt = "tsf", Filter = "TSFApp Files (*.tsf)|*.tsf" };
				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					szFile = sfd.FileName;
					return true;
				}
			}
			return false;
		}

		private void IDR_MAIN_MENU_MenuActivate(object sender, EventArgs e)
		{
			IDM_TERMINATE_COMPOSITION.Enabled = m_pTSFEditWnd.m_cCompositions > 0;
			IDM_RECONVERT.Enabled = m_pTSFEditWnd.CanReconvertSelection;
			IDM_PLAYBACK.Enabled = m_pTSFEditWnd.CanPlaybackSelection;
			IDM_LOAD.Enabled = !m_pTSFEditWnd.IsLocked();
		}

		private void OnEditStatusUpdate(string arg1, string arg2)
		{
			IDC_STATUSBAR.Items[0].Text = arg1;
			IDC_STATUSBAR.Items[1].Text = arg2;
		}

		private void ShowMsg(object text = null)
		{
			MessageBox.Show(this, text?.ToString() ?? "", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}