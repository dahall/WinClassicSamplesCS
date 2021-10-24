using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Vanara.PInvoke.EnhancedStorage;

namespace EhStorEnumerator
{
    public partial class IDD_SET_CERTIFICATE : Form
    {
        public CCertProperties g_newCertProps = new();

        public IDD_SET_CERTIFICATE(CCertificate g_Certificate)
        {
            InitializeComponent();
            var (_, nMaxCertCount) = g_DeviceCertData.m_iDevice.CertGetCertificatesCount();

            IDC_CERT_SIGNER_INDEX.Items.Add(Make("None", -1));

            for (uint nCertIndex = 0; nCertIndex < nMaxCertCount; nCertIndex++)
            {
                if (CertificateExists(nCertIndex))
                    IDC_CERT_SIGNER_INDEX.Items.Add(Make($"{nCertIndex}", nCertIndex));
                else
                    IDC_CERT_INDEX.Items.Add(Make($"{nCertIndex}", nCertIndex));
            }

            IDC_VALIDATION_POLICY.Items.Add(Make("None", CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_NONE));
            IDC_VALIDATION_POLICY.Items.Add(Make("Basic", CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_BASIC));
            IDC_VALIDATION_POLICY.Items.Add(Make("Extended", CERT_VALIDATION_POLICY.CERT_VALIDATION_POLICY_EXTENDED));

            IDC_CERT_TYPE.Items.Add(Make("Provisioning Certificate (PCp)", CERT_TYPE.CERT_TYPE_PCp));
            IDC_CERT_TYPE.Items.Add(Make("Auth. Silo Certificate (ASCh)", CERT_TYPE.CERT_TYPE_ASCh));
            IDC_CERT_TYPE.Items.Add(Make("Host Certificate (HCh)", CERT_TYPE.CERT_TYPE_HCh));
            IDC_CERT_TYPE.Items.Add(Make("Signer Certificate (SCh)", CERT_TYPE.CERT_TYPE_SIGNER));

            IDC_DEVICE_ID.Text = g_DeviceCertData.m_szDevicePNPID;
            IDC_CERT_SUBJECT.Text = g_Certificate.Subject;
        }

        private static bool CertificateExists(uint nIndex) => g_DeviceCertData.m_parCertificates.Any(c => c.nIndex == nIndex);

        static CBItem<T> Make<T>(string text, T item) => new() { Text = text, Value = item };

        private void IDOK_Click(object sender, EventArgs e)
        {
            g_newCertProps.nCertType = ((CBItem<CERT_TYPE>)IDC_CERT_TYPE.SelectedItem).Value;
            g_newCertProps.nValidationPolicy = ((CBItem<CERT_VALIDATION_POLICY>)IDC_VALIDATION_POLICY.SelectedItem).Value;
            g_newCertProps.nSignerCertIndex = ((CBItem<uint>)IDC_CERT_SIGNER_INDEX.SelectedItem).Value;
            g_newCertProps.nIndex = 0;
        }
    }

    public class CBItem<TItem>
    {
        public string Text { get; set; }
        public TItem Value { get; set; }
        public override string ToString() => Text ?? "";
    }
}
