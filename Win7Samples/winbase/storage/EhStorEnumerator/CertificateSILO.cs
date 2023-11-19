using static Vanara.PInvoke.PortableDeviceApi;

namespace EhStorEnumerator;

public static class g_DeviceCertData
{
	public static string? m_szDevicePNPID;
	public static IPortableDevice? m_iDevice;
	public static List<CCertProperties>? m_parCertificates;
}

public partial class EhStorEnumerator2 : Form
{
	private void OnCertificateCertificates(object sender, EventArgs e)
	{
		g_DeviceCertData.m_szDevicePNPID = SelectedDeviceName;
		g_DeviceCertData.m_iDevice = SelectedDevice;
		if (g_DeviceCertData.m_szDevicePNPID is null) return;

		new IDD_CERTIFICATES().ShowDialog(this);
	}

	private void OnCertificateDeviceauthentication(object sender, EventArgs e)
	{
		var device = SelectedDevice;
		if (device is null) return;

		device.CertDeviceAuthentication(IDC_DEVLIST.SelectedIndices[0]);
	}

	private void OnCertificateHostauthentication(object sender, EventArgs e)
	{
		var device = SelectedDevice;
		if (device is null) return;

		device.CertHostAuthentication();
	}

	private void OnCertificateInittomanufacturerstate(object sender, EventArgs e)
	{
		var device = SelectedDevice;
		if (device is null) return;

		device.CertInitializeToManufacturedState();
	}

	private void OnCertificateQueryinformation(object sender, EventArgs e)
	{
		var device = SelectedDevice;
		if (device is null) return;

		new IDD_CERT_SILO_INFO(device).ShowDialog(this);
	}

	private void OnCertificateUnauthentication(object sender, EventArgs e)
	{
		var device = SelectedDevice;
		if (device is null) return;

		device.CertUnAuthentication();
	}
}