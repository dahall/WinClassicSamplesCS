using System.Data;
using static Vanara.PInvoke.EnhancedStorage;

namespace EhStorEnumerator
{
	public partial class IDD_CERTIFICATES : Form
    {
        private readonly ColumnHeader[] deviceHeaders = new[]
        {
            new ColumnHeader() { Text = "Index" },
            new ColumnHeader() { Text = "Type" },
            new ColumnHeader() { Text = "V. Policy" },
            new ColumnHeader() { Text = "Signer" },
        };

        private readonly ColumnHeader[] storeHeaders = new[]
        {
            new ColumnHeader() { Text = "Subject" },
            new ColumnHeader() { Text = "Version" },
            new ColumnHeader() { Text = "Issuer" },
        };

        public IDD_CERTIFICATES()
        {
            InitializeComponent();
            IDC_CERTTAB.SelectedIndex = 0;
        }

        public CCertificate SelectedCertificate =>
            IDC_CERT_LIST.SelectedItems.Count != 1 ? null : (CCertificate)IDC_CERT_LIST.SelectedItems[0].Tag;

        public uint SelectedCertIndex => IDC_CERT_LIST.SelectedItems.Count != 1 ? uint.MaxValue : (uint)IDC_CERT_LIST.SelectedIndices[0];

        private void CheckEnableButtons()
        {
            IDC_ADD_TO_DEVICE.Enabled = IDC_CERTTAB.SelectedIndex > 0 && SelectedCertificate is not null;
            IDC_DELETE.Enabled = IDC_CERTTAB.SelectedIndex == 0 && SelectedCertificate is not null;
        }

        private void FillDeviceList()
        {
            IDC_CERT_LIST.Clear();
            IDC_CERT_LIST.Columns.AddRange(deviceHeaders);

            g_DeviceCertData.m_parCertificates = new();
            (uint nStoredCertCount, uint nMaxCertCount) = g_DeviceCertData.m_iDevice.CertGetCertificatesCount();
            if (nStoredCertCount == 0)
            {
                return;
            }

            uint nNextCertIndex = 0U;
            do
            {
                CCertProperties certProperties = g_DeviceCertData.m_iDevice.CertGetCertificate(nNextCertIndex);
                g_DeviceCertData.m_parCertificates.Add(certProperties);
                nNextCertIndex = certProperties.nNextCertIndex;
                IDC_CERT_LIST.Items.Add(new ListViewItem(new string[] { $"{certProperties.nIndex}", certProperties.CertType, certProperties.ValidationPolicy, $"{certProperties.nSignerCertIndex}" }));
            } while (nNextCertIndex > 0);

            IDC_CERT_LIST.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void FillStoreList(SYSTEM_STORE_NAMES store)
        {
            IDC_CERT_LIST.Clear();
            IDC_CERT_LIST.Columns.AddRange(storeHeaders);

            CLocalCertStoreImp localStore = new(store);
            IDC_CERT_LIST.Items.AddRange(localStore.GetCertificatesList().
                Select(c => new ListViewItem(new string[] { c.Subject, $"{c.Version}", c.Issuer }) { Tag = c }).ToArray());

            IDC_CERT_LIST.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void IDC_ADD_TO_DEVICE_Click(object sender, EventArgs e)
        {
            CCertificate g_Certificate = SelectedCertificate;
            if (g_Certificate is not null)
            {
                IDD_SET_CERTIFICATE dlg = new(g_Certificate);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    bool bProcessAdding = true;
                    dlg.g_newCertProps.CertificateData = g_Certificate.GetEncodedData();

                    // add certificate...
                    if ((dlg.g_newCertProps.nCertType == CERT_TYPE.CERT_TYPE_PCp) && (dlg.g_newCertProps.nIndex != 1))
                    {
                        bProcessAdding = MessageBox.Show(this, "Warning!\nPCp certificate should be placed to slot 1 only.\nAre you sure?",
                            "Confirm...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
                    }

                    if ((dlg.g_newCertProps.nCertType != CERT_TYPE.CERT_TYPE_PCp) && (dlg.g_newCertProps.nIndex == 1))
                    {
                        bProcessAdding = MessageBox.Show(this, "Warning!\nSlot 1 reserved for PCp certificate.\nAre you sure?",
                            "Confirm...", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
                    }

                    if (!bProcessAdding)
                    {
                        try
                        {
                            g_DeviceCertData.m_iDevice.CertSetCertificate(dlg.g_newCertProps.nIndex, dlg.g_newCertProps);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }

        private void IDC_CERT_LIST_SelectedIndexChanged(object sender, EventArgs e) => CheckEnableButtons();

        private void IDC_CERTTAB_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckEnableButtons();

            switch (IDC_CERTTAB.SelectedIndex)
            {
                case 0:
                    FillDeviceList();
                    break;

                case 1:
                    FillStoreList(SYSTEM_STORE_NAMES.SYSTEM_STORE_CA);
                    break;

                case 2:
                    FillStoreList(SYSTEM_STORE_NAMES.SYSTEM_STORE_MY);
                    break;

                case 3:
                    FillStoreList(SYSTEM_STORE_NAMES.SYSTEM_STORE_ROOT);
                    break;

                case 4:
                    FillStoreList(SYSTEM_STORE_NAMES.SYSTEM_STORE_SPC);
                    break;

                default:
                    break;
            }
        }

        private void IDC_DELETE_Click(object sender, EventArgs e)
        {
            uint nCertIndex = SelectedCertIndex;
            if (nCertIndex == uint.MaxValue)
            {
                return;
            }

            try
            {
                g_DeviceCertData.m_iDevice.CertRemoveCertificate(nCertIndex);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            FillDeviceList();
        }
    }
}