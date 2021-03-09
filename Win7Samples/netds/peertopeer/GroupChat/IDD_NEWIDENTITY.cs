using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Vanara.PInvoke.P2P;

namespace GroupChat
{
	public partial class IDD_NEWIDENTITY : Form
	{
		public IDD_NEWIDENTITY()
		{
			InitializeComponent();
		}

		//-----------------------------------------------------------------------------
		// Function: HandleCreateIdentity
		//
		// Purpose:  Extracts the friendly name from the dialog and
		//           creates a new identity.
		//
		// Returns:  HRESULT
		//
		private void IDOK_Click(object sender, EventArgs e)
		{
			if (0 == IDC_EDT_FRIENDLYNAME.TextLength)
			{
				GroupChat.DisplayError("Please type a name for the identity.");
				return;
			}

			var hr = PeerIdentityCreate("GroupChatMember", IDC_EDT_FRIENDLYNAME.Text, default, out var pwzIdentity);
			if (hr.Failed)
			{
				GroupChat.DisplayHrError("Failed to create identity", hr);
			}
			else
			{
				Identity = pwzIdentity;
				Close();
			}
		}

		public string Identity { get; private set; }
	}
}
