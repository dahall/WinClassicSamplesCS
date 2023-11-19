using Vanara.PInvoke;
using static Vanara.PInvoke.EnhancedStorage;

namespace EhStorEnumerator;

public partial class IDD_CERT_SILO_INFO : Form
    {
        public IDD_CERT_SILO_INFO()
        {
            InitializeComponent();
        }

        public IDD_CERT_SILO_INFO(PortableDeviceApi.IPortableDevice device) : this()
        {
            IDC_FRIENDLY_NAME.Text = device.CertGetSiloFriendlyName();
            string? guid = null;
            try { guid = device.CertGetSiloGUID(); } catch { }
            IDC_SILO_GUID.Text = guid ?? "Cannot retrieve, device not trusted";
            (uint nStoredCertCount, uint nMaxCertCount)? cnt = null;
            try { cnt = device.CertGetCertificatesCount(); } catch { }
            IDC_CERT_COUNT.Text = cnt.HasValue ? $"Stored: {cnt.Value.nStoredCertCount}, Max: {cnt.Value.nMaxCertCount}" : "{Unable to retrieve}";
            IDC_AUTHN_STATE.Text = device.CertGetState();
            foreach (var cap in device.CertGetSiloCapablity(CERT_CAPABILITY.CERT_CAPABILITY_ASYMMETRIC_KEY_CRYPTOGRAPHY))
                IDC_ASYMM_KEY.Items.Add(cap);
            foreach (var cap in device.CertGetSiloCapablity(CERT_CAPABILITY.CERT_CAPABILITY_HASH_ALG))
                IDC_HASH_ALGS.Items.Add(cap);
            foreach (var cap in device.CertGetSiloCapablity(CERT_CAPABILITY.CERT_CAPABILITY_SIGNATURE_ALG))
                IDC_SIGNING_ALGS.Items.Add(cap);
        }
    }