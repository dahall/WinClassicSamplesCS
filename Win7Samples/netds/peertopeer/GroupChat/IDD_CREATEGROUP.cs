using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.P2P;

namespace GroupChat
{
	public partial class IDD_CREATEGROUP : Form
	{
		public IDD_CREATEGROUP()
		{
			InitializeComponent();
			RefreshIdentityCombo();
		}

		private void IDC_BTN_NEW_IDENTITY_Click(object sender, EventArgs e)
		{
			if (new IDD_NEWIDENTITY().ShowDialog(this) == DialogResult.OK)
			{
				RefreshIdentityCombo();
			}
		}

		private void RefreshIdentityCombo() => GroupChat.RefreshIdentityCombo(IDC_CB_IDENTITY, true);

		private void IDOK_Click(object sender, EventArgs e)
		{
			if (GroupChat.Main.CreateGroup(IDC_EDT_GROUPNAME.Text, GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY),
				IDC_EDIT_PASSWORD.Text, IDC_RADIO_AUTH_PASSW.Checked, IDC_RADIO_GLOBAL_SCOPE.Checked).Succeeded)
			{
				GroupChat.Main.SetStatus("Group created");
				Close();
			}
		}

		private void AuthRadioCheckChanged(object sender, EventArgs e)
		{
			IDC_EDIT_PASSWORD.Enabled = IDC_STATIC_PASSWORD.Enabled = IDC_RADIO_AUTH_PASSW.Checked;
		}
	}
}
