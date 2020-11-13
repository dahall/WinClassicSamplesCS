using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GroupChat
{
	public partial class IDD_DELETEGROUP : Form
	{
		public IDD_DELETEGROUP()
		{
			InitializeComponent();
			GroupChat.RefreshIdentityCombo(IDC_CB_IDENTITY, true);
		}

		private void IDOK_Click(object sender, EventArgs e)
		{
			GroupChat.Main.SetStatus("Group Deleted");
			Close();
		}

		private void IDC_CB_IDENTITY_SelectedIndexChanged(object sender, EventArgs e)
		{
			GroupChat.RefreshGroupCombo(IDC_CB_GROUP, GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY));
		}
	}
}
