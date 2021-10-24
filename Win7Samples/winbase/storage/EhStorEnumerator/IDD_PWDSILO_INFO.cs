using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EhStorEnumerator
{
    public partial class IDD_PWDSILO_INFO : Form
    {
        public IDD_PWDSILO_INFO()
        {
            InitializeComponent();
        }

        public IDD_PWDSILO_INFO(CPasswordSiloInformation siloInformation) : this()
        {
            IDC_FRIENDLY_NAME.Text = siloInformation.SiloName;
            IDC_ADMIN_HINT.Text = siloInformation.AdminHint;
            if (siloInformation.SiloInfo.UserCreated)
            {
                IDC_USER_NAME.Text = siloInformation.UserName;
                IDC_USER_HINT.Text = siloInformation.UserHint;
            }
            else
            {
                IDC_USER_NAME.Text = "{No User Created}";
            }
            IDC_AUTHN_STATE.Text = siloInformation.dwAuthnState.ToString().Substring(29);
        }
    }
}
