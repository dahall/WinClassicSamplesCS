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
    public partial class IDD_PWD_ITMS : Form
    {
        public IDD_PWD_ITMS(bool sidEnabled)
        {
            InitializeComponent();
            IDC_DEVICE_SID.Enabled = sidEnabled;
        }

        public string SID => IDC_DEVICE_SID.Text;
    }
}
