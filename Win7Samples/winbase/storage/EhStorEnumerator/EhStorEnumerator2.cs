using static Vanara.PInvoke.PortableDeviceApi;

namespace EhStorEnumerator;

public partial class EhStorEnumerator2 : Form
{
	private static readonly Lazy<IPortableDeviceValues> clientInfo = new(InitClientInfo);
	private IPortableDeviceManager? g_DevManager;

	public EhStorEnumerator2() => InitializeComponent();

	private IPortableDevice? SelectedDevice
	{
		get
		{
			var devName = SelectedDeviceName;
			if (devName is null)
				return null;

			IPortableDevice dev = new();
			dev.Open(devName, clientInfo.Value);
			return dev;
		}
	}

	private string? SelectedDeviceName =>
		IDC_DEVLIST.SelectedItems.Count == 0 ? null : IDC_DEVLIST.SelectedItems[0].SubItems[2].Text;

	private static IPortableDeviceValues InitClientInfo()
	{
		IPortableDeviceValues vals = new();
		vals.SetStringValue(WPD_CLIENT_NAME, "EhStorEnumerator");
		vals.SetUnsignedIntegerValue(WPD_CLIENT_MAJOR_VERSION, 1);
		vals.SetUnsignedIntegerValue(WPD_CLIENT_MINOR_VERSION, 1);
		vals.SetUnsignedIntegerValue(WPD_CLIENT_REVISION, 1);
		vals.SetUnsignedIntegerValue(WPD_CLIENT_SECURITY_QUALITY_OF_SERVICE, (uint)Vanara.PInvoke.FileFlagsAndAttributes.SECURITY_IMPERSONATION);
		return vals;
	}

	private void EhStorEnumerator2_Load(object sender, EventArgs e)
	{
		g_DevManager = new IPortableDeviceManager();

		IDC_REFRESH_Click(this, EventArgs.Empty);
	}

	private void IDC_DEVLIST_MouseClick(object sender, MouseEventArgs e)
	{
		if (e.Button != MouseButtons.Right)
		{
			return;
		}

		ListViewItem item = IDC_DEVLIST.GetItemAt(e.X, e.Y);
		if (item is null)
		{
			return;
		}

		if (item.Text == "Microsoft WPD Enhanced Storage Password Driver")
		{
			IDR_POPUPMENU_PASSWORD.Show(IDC_DEVLIST, e.Location);
		}
		else if (item.Text == "Microsoft WPD Enhanced Storage Certificate Driver")
		{
			IDR_POPUPMENU_CERT.Show(IDC_DEVLIST, e.Location);
		}
	}

	private void IDC_REFRESH_Click(object sender, EventArgs e)
	{
		IDC_DEVLIST.Items.Clear();
		foreach (string device in g_DevManager!.GetDevices(true))
		{
			IDC_DEVLIST.Items.Add(new ListViewItem(new[] { g_DevManager!.GetDeviceDescription(device), g_DevManager!.GetDeviceManufacturer(device), device }));
		}
		IDC_DEVLIST.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
	}
}