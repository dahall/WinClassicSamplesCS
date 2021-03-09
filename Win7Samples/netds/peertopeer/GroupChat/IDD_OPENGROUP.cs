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
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.P2P;

namespace GroupChat
{
    public partial class IDD_OPENGROUP : Form
    {
        public IDD_OPENGROUP()
        {
            InitializeComponent();
            GroupChat.RefreshIdentityCombo(IDC_CB_IDENTITY, true);
        }

        private void IDOK_Click(object sender, EventArgs e)
        {
            if (HandleOpenGroup().Succeeded)
            {
                GroupChat.Main.SetStatus("Group opened");
                Close();
            }
        }

        private void IDC_CB_IDENTITY_SelectedIndexChanged(object sender, EventArgs e)
        {
            GroupChat.RefreshGroupCombo(IDC_CB_GROUP, GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY));
        }

        //-----------------------------------------------------------------------------
        // Function: HandleOpenGroup
        //
        // Purpose:  Extracts the information from the dialog and calls
        //           OpenGroup to do the actual work.
        //
        // Returns:  HRESULT
        //
        HRESULT HandleOpenGroup()
        {
            var pwzGroup = IDC_CB_GROUP.SelectedIndex != -1 ? ((PEER_NAME_PAIR)IDC_CB_GROUP.SelectedItem).pwzPeerName : null;
            var pwzIdentity = GroupChat.GetSelectedIdentity(IDC_CB_IDENTITY);
            return GroupChat.Main.OpenGroup(pwzIdentity, pwzGroup);
        }
    }
}
