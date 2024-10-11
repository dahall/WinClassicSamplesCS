using System.Media;
using Vanara.DirectoryServices;
using Vanara.Extensions;
using static Vanara.PInvoke.ActiveDS;

namespace ADQI
{
	[ADsViewer(typeof(ADsFileShare))]
	[ADsViewer(typeof(ADsGroup))]
	[ADsViewer(typeof(ADsObject))]
	[ADsViewer(typeof(ADsPrintJob))]
	[ADsViewer(typeof(ADsPrintQueue))]
	[ADsViewer(typeof(ADsResource))]
	[ADsViewer(typeof(ADsService))]
	[ADsViewer(typeof(ADsSession))]
	[ADsViewer(typeof(ADsUser))]
	public partial class ADsDlg : Form, IADsViewerForm
	{
		private const string STRING_SEPARATOR = "----------";

		public ADsDlg() => InitializeComponent();

		public ADsDlg(IADsObject obj) : this()
		{
			ADsObjectInstance = obj;
			adsPathText.Text = obj.Path;
			nameText.Text = obj.Name;
			parentText.Text = ADsObjectInstance.NativeInterface.Parent;
			classText.Text = obj.Class;
			schemaText.Text = obj.Schema?.Path;
			guidText.Text = obj.Guid == Guid.Empty ? "" : obj.Guid.ToString();
			guidBtn.Enabled = guidText.TextLength > 0;
			attrCombo.Items.Clear();
			valuesList.Items.Clear();
			attrCombo.Items.AddRange(obj.Schema?.MandatoryProperties.ToArray() ?? []);
			attrCombo.Items.Add(STRING_SEPARATOR);
			attrCombo.Items.AddRange(obj.Schema?.OptionalProperties.ToArray() ?? []);
		}

		public IADsObject? ADsObjectInstance { get; }

		private void ADs_Load(object sender, EventArgs e)
		{
			if (ADsObjectInstance is null)
			{
				MessageBox.Show("No interface was supplied", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
				Close();
			}
		}

		private void attrCombo_SelectedIndexChanged(object sender, EventArgs e) => getBtn_Click(sender, e);

		private void closeBtn_Click(object sender, EventArgs e) => Close();

		private void copyBtn_Click(object sender, EventArgs e)
		{
			if (valuesList.SelectedItem is not string curSel)
				return;
			Clipboard.SetText(curSel);
		}

		private void getBtn_Click(object sender, EventArgs e)
		{
			valuesList.Items.Clear();
			if (attrCombo.SelectedItem is not string curSel || curSel == STRING_SEPARATOR)
			{
				SystemSounds.Beep.Play();
				return;
			}

			if (ADsObjectInstance!.PropertyCache.TryGetValue(curSel, out var propVal))
			{
				valuesList.Items.AddRange(Helper.VariantToStringList(propVal));
			}
		}

		private void getExBtn_Click(object sender, EventArgs e) => getBtn_Click(sender, e);

		private void getInfoBtn_Click(object sender, EventArgs e)
		{
			try
			{
				ADsObjectInstance!.PropertyCache.Refresh();
				MessageBox.Show("Succeed");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void getInfoExBtn_Click(object sender, EventArgs e) => getInfoBtn_Click(sender, e);

		private void guidBtn_Click(object sender, EventArgs e)
		{
			try
			{
				IADsPathname pPathname = new();
				pPathname.Set(ADsObjectInstance!.Path, ADS_SETTYPE.ADS_SETTYPE_FULL);

				// Usage: PathName to find out the provider 
				var strProvider = pPathname.Retrieve(ADS_FORMAT.ADS_FORMAT_PROVIDER);
				if (strProvider != "LDAP")
				{
					MessageBox.Show("Only LDAP: (Active Directory Provider) knows about <GUID=xxxx> syntax", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}

				// Usage: PathName to find out the server
				string strServer;
				try { strServer = pPathname.Retrieve(ADS_FORMAT.ADS_FORMAT_SERVER); }
				catch { strServer = ""; }

				// Now build the LDAP://server/<GUID=xxx
				string strPath = $"{strProvider}://{strServer}{(strServer.Length == 0 ? "" : "/")}<GUID={ADsObjectInstance!.Guid}>";

				// Now Bind using GUID
				// ADsOpenObject(strPath, null, null, 0, IID_IUnknown, out var pUnk).ThrowIfFailed();
				new ADsDlg(ADsObject.GetObject(strPath)).ShowDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void parentBtn_Click(object sender, EventArgs e)
		{
			IADsObject? iadsParent = null;
			try
			{
				iadsParent = ADsObject.GetObject(parentText.Text);
			}
			catch { }
			if (iadsParent is null || !iadsParent.Schema.Container)
			{
				MessageBox.Show("Failed to get parent object.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//new ADsContainer(iadsParent).ShowDialog();
		}

		private void putBtn_Click(object sender, EventArgs e)
		{
			// TODO: Add your control notification handler code here
		}

		private void putExBtn_Click(object sender, EventArgs e)
		{
			// TODO: Add your control notification handler code here
		}

		private void schemaBtn_Click(object sender, EventArgs e)
		{
			if (ADsObjectInstance!.Schema is null || !ADsObjectInstance!.Schema.Container)
			{
				MessageBox.Show("Failed to get parent object.", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			//new ADsContainer(iadsParent).ShowDialog();
		}

		private void setInfoBtn_Click(object sender, EventArgs e)
		{
			try
			{
				ADsObjectInstance!.PropertyCache.Save();
				MessageBox.Show("Succeed");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void valuesList_DoubleClick(object sender, EventArgs e)
		{
			if (valuesList.SelectedItem is not string curSel)
				return;
			// TODO: Add handling of viewing objects
		}
	}
}
