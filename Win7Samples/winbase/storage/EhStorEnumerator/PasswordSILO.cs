using Vanara.Extensions;
using static Vanara.PInvoke.EnhancedStorage;
using static Vanara.PInvoke.PortableDeviceApi;

namespace EhStorEnumerator;

public class CPasswordSiloInformation
    {
        public CPasswordSiloInformation(IPortableDeviceValues results)
        {
            results.GetBufferValue(ENHANCED_STORAGE_PROPERTY_PASSWORD_SILO_INFO, out Vanara.InteropServices.SafeCoTaskMemHandle pbBuffer, out int cbBuffer);
            SiloInfo = pbBuffer.DangerousGetHandle().ToStructure<ENHANCED_STORAGE_PASSWORD_SILO_INFORMATION>(cbBuffer);
            dwAuthnState = (ENHANCED_STORAGE_AUTHN_STATE)results.GetUnsignedIntegerValue(ENHANCED_STORAGE_PROPERTY_AUTHENTICATION_STATE);
            UserHint = results.GetStringValue(ENHANCED_STORAGE_PROPERTY_USER_HINT);
            UserName = results.GetStringValue(ENHANCED_STORAGE_PROPERTY_USER_NAME);
            AdminHint = results.GetStringValue(ENHANCED_STORAGE_PROPERTY_ADMIN_HINT);
            SiloName = results.GetStringValue(ENHANCED_STORAGE_PROPERTY_SILO_NAME);
        }

        public string AdminHint { get; }
        public ENHANCED_STORAGE_AUTHN_STATE dwAuthnState { get; }
        public ENHANCED_STORAGE_PASSWORD_SILO_INFORMATION SiloInfo { get; }
        public string SiloName { get; }
        public string UserHint { get; }
        public string UserName { get; }
    }

    public partial class EhStorEnumerator2 : Form
    {
        private void OnPasswordInittomanufacturerstate(object sender, EventArgs e)
        {
            IPortableDevice? dev = SelectedDevice;
            if (dev is null)
            {
                return;
            }

            CPasswordSiloInformation siloInformation = dev.PasswordQueryInformation();
            IDD_PWD_ITMS dlg = new(siloInformation.SiloInfo.SecurityIDAvailable);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                dev.PasswordInitializeToManufacturerState(dlg.SID);
            }
        }

        private void OnPasswordQueryInformation(object sender, EventArgs e)
        {
            IPortableDevice? dev = SelectedDevice;
            if (dev is null)
            {
                return;
            }

            CPasswordSiloInformation siloInformation = dev.PasswordQueryInformation();
            new IDD_PWDSILO_INFO(siloInformation).ShowDialog(this);
        }

        private void OnPasswordSet(object sender, EventArgs e)
        {
            IPortableDevice? dev = SelectedDevice;
            if (dev is null)
            {
                return;
            }

            CPasswordSiloInformation siloInformation = dev.PasswordQueryInformation();
            IDD_SET_PASSWORD dlg = new(siloInformation);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                dev.PasswordChangePassword(dlg.IsAdmin, dlg.OldPassword, dlg.NewPassword, dlg.PasswordHint, dlg.SID);
            }
        }
    }