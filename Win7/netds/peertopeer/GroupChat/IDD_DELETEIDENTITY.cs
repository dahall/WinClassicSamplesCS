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
	public partial class IDD_DELETEIDENTITY : Form
	{
		public IDD_DELETEIDENTITY()
		{
			InitializeComponent();
			GroupChat.RefreshIdentityCombo(IDC_CB_IDENTITY, false);
		}

		private void IDOK_Click(object sender, EventArgs e)
		{
			if (DeleteIdentity(GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY)).Succeeded)
				Close();
		}

		//-----------------------------------------------------------------------------
		// Function: DeleteIdentity
		//
		// Purpose: Deletes an identity
		//
		// Parameters:
		// pwzIdentity : The peer identity to delete
		//
		// Returns: HRESULT //
		HRESULT DeleteIdentity(string pwzIdentity)
		{
			HRESULT hr = HRESULT.S_OK;

			if (string.IsNullOrEmpty(pwzIdentity))
			{
				GroupChat.DisplayHrError("Please select an identity.", hr = HRESULT.E_INVALIDARG);
			}

			if (hr.Succeeded)
			{
				GroupChat.Main.CleanupGroup();

				hr = PeerIdentityDelete(pwzIdentity);

				if (hr.Failed)
				{
					GroupChat.DisplayHrError("Failed to delete identity.", hr);
				}
			}

			if (hr.Succeeded)
			{
				GroupChat.Main.SetStatus("Deleted identity");
			}

			return hr;
		}
	}
}
