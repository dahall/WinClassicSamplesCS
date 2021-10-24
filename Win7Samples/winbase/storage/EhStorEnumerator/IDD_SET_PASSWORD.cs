using System;
using System.Windows.Forms;
using Vanara.PInvoke;

namespace EhStorEnumerator
{
    public partial class IDD_SET_PASSWORD : Form
    {
        private readonly CPasswordSiloInformation siloInformation;

        public IDD_SET_PASSWORD() => InitializeComponent();

        public IDD_SET_PASSWORD(CPasswordSiloInformation siloInformation) : this()
        {
            this.siloInformation = siloInformation;
            IDC_INDICATOR_ADMIN.Checked = true;
            IDC_INDICATOR_USER.Enabled = siloInformation.SiloInfo.UserCreated;
            IDC_DEVICE_SID.Enabled = siloInformation.SiloInfo.SecurityIDAvailable;
            IDC_ODL_PASSWORD.Enabled = siloInformation.dwAuthnState != EnhancedStorage.ENHANCED_STORAGE_AUTHN_STATE.ENHANCED_STORAGE_AUTHN_STATE_NOT_AUTHENTICATED;
            IDC_PASSWORD_HINT.Text = siloInformation.AdminHint;
        }

        public bool IsAdmin => IDC_INDICATOR_ADMIN.Checked;

        public string NewPassword => IDC_NEW_PASSWORD.Text;

        public string OldPassword => IDC_ODL_PASSWORD.Text;

        public string PasswordHint => IDC_PASSWORD_HINT.Text;

        public string SID => IDC_DEVICE_SID.Text;

        private void IDOK_Click(object sender, EventArgs e) => Close();

        private void OnPwdIndicatorCheckChanged(object sender, EventArgs e) => IDC_PASSWORD_HINT.Text = IDC_INDICATOR_ADMIN.Checked ? siloInformation.AdminHint : siloInformation.UserHint;
    }
}