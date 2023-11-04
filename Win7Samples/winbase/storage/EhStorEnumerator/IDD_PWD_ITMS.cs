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
