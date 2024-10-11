using Vanara.DirectoryServices;
using Vanara.Windows.Forms;

namespace ADQI;

[ADsViewer(typeof(ADsComputer))]
[ADsViewer(typeof(ADsContainer))]
[ADsViewer(typeof(ADsDomain))]
[ADsViewer(typeof(ADsFileService))]
[ADsViewer(typeof(ADsSchemaClass))]
public partial class ADsContainerDlg : Form, IADsViewerForm
{
	private IADsContainerObject? icont;

	public ADsContainerDlg(IADsObject obj)
	{
		InitializeComponent();
		ADsObjectInstance = obj;
		icont = ADsObjectInstance as IADsContainerObject;
		if (icont is null)
		{
			MessageBox.Show("Fatal Error! QI for IADsContainer failed", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			Close();
		}
		EnumerateChildren();
	}

	public IADsObject? ADsObjectInstance { get; }
	public IADsObject? SelectedChild { get; internal set; }

	private void childList_DoubleClick(object sender, EventArgs e) => viewBtn_Click(sender, e);

	private void childList_SelectedIndexChanged(object sender, EventArgs e)
	{
		SelectedChild = (childList.SelectedItem as ListItem)?.Object;
		viewBtn.Enabled = renameBtn.Enabled = deleteBtn.Enabled = SelectedChild is not null;
	}

	private void closeBtn_Click(object sender, EventArgs e) => Close();

	private void deleteBtn_Click(object sender, EventArgs e)
	{
		if (SelectedChild is null)
			return;
		if (icont!.Children.Remove(SelectedChild))
			EnumerateChildren();
		else
			MessageBox.Show("Failed to delete child object", null, MessageBoxButtons.OK, MessageBoxIcon.Error);
	}

	private void EnumerateChildren()
	{
		childList.Items.Clear();
		childList.Items.AddRange(icont!.Children.Select(o => new ListItem(o)).ToList().ToArray());
	}

	private void moveBtn_Click(object sender, EventArgs e)
	{
		object? destPath = string.Empty;
		if (InputDialog.Show(this, "Object you want to move to this container (ADsPath):", "Move", ref destPath) == DialogResult.OK)
		{
			try
			{
				_ = icont!.Children.MoveHere(destPath?.ToString() ?? "", null);
				EnumerateChildren();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	private void renameBtn_Click(object sender, EventArgs e)
	{
		if (SelectedChild is null)
			return;
		object? newName = string.Empty;
		if (InputDialog.Show(this, "New Name:", $"Rename {SelectedChild.Name}", ref newName) == DialogResult.OK)
		{
			try
			{
				_ = icont!.Children.MoveHere(SelectedChild, newName?.ToString());
				EnumerateChildren();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}

	private void setBtn_Click(object sender, EventArgs e)
	{
		icont!.Children.Filter = filterText.Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		EnumerateChildren();
	}

	private void viewBtn_Click(object sender, EventArgs e)
	{
		if (SelectedChild is null)
			return;
		new ADsDlg(SelectedChild).ShowDialog(this);
	}

	private class ListItem(IADsObject obj)
	{
		public IADsObject Object { get; } = obj;
		public override string ToString() => Object.Name;
	}
}