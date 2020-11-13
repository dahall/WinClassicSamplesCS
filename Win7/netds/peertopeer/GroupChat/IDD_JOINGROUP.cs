using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.P2P;

namespace GroupChat
{
	public partial class IDD_JOINGROUP : Form
	{
		public IDD_JOINGROUP()
		{
			InitializeComponent();
			GroupChat.RefreshIdentityCombo(IDC_CB_IDENTITY, true);
		}

		//-----------------------------------------------------------------------------
		// Function: HandleJoinGroup
		//
		// Purpose:  Extracts the information from the dialog and calls
		//           JoinGroup to do the actual work.
		//
		// Returns:  HRESULT
		//
		private HRESULT HandleJoinGroup()
		{
			var wzPassword = IDC_CHECK_PASSWORD.Checked ? IDC_EDIT_PASSWORD.Text : string.Empty;
			var wzInvitation = IDC_EDT_LOCATION.Text;
			return GroupChat.Main.JoinGroup(GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY), wzInvitation, wzPassword, IDC_CHECK_PASSWORD.Checked);
		}

		private void IDC_BTN_BROWSE_Click(object sender, EventArgs e)
		{
			GroupChat.BrowseHelper(this, IDC_EDT_LOCATION, "Group Invitation", GroupChat.c_wzFileExtInv, true);
		}

		private void IDC_CHECK_PASSWORD_CheckedChanged(object sender, EventArgs e)
		{
			IDC_EDIT_PASSWORD.Enabled = IDC_STATIC_PASSWORD.Enabled = IDC_CHECK_PASSWORD.Checked;
		}

		private void IDOK_Click(object sender, EventArgs e)
		{
			if (HandleJoinGroup().Succeeded)
			{
				GroupChat.Main.SetStatus("Joined group");
				Close();
			}
		}
	}
}