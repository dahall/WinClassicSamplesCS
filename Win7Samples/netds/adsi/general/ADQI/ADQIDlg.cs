using System.Media;
using System.Reflection;
using Vanara.DirectoryServices;

namespace ADQI;

public partial class ADQIDlg : Form
{
	private readonly Dictionary<Type, Type> formMap = [];
	private IADsObject? obj;

	public ADQIDlg() => InitializeComponent();

	private void ADQIDlg_Load(object sender, EventArgs e)
	{
		adspathText.DataBindings.Add(new Binding("Text", Properties.Settings.Default, "adsPath", true, DataSourceUpdateMode.OnPropertyChanged));
		//adspathText.Text = Properties.Settings.Default.adsPath;
		foreach (var t in GetType().Assembly.GetTypes().Where(t => typeof(IADsViewerForm).IsAssignableFrom(t)))
		{
			foreach (var attr in t.GetCustomAttributes<ADsViewerAttribute>(true))
				formMap.Add(attr.SupportedObjectType, t);
		}
		adspathText_TextChanged(sender, e);
	}

	private void adspathText_TextChanged(object sender, EventArgs e) => okBtn.Enabled = adspathText.TextLength > 0;

	private void cancelBtn_Click(object sender, EventArgs e) => Close();

	private void EnumerateInterfaces()
	{
		interfaceList.Items.Clear();

		interfaceList.Items.AddRange(typeof(IADsObject).Assembly.GetExportedTypes().Where(t => typeof(IADsObject).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract).ToArray());
	}

	private void interfaceList_DoubleClick(object sender, EventArgs e)
	{
		if (interfaceList.SelectedItem is not Type curSel)
		{
			SystemSounds.Beep.Play();
			return;
		};

		if (formMap.TryGetValue(curSel, out Type? formType))
		{
			using Form form = (Form)Activator.CreateInstance(formType, obj)!;
			form.ShowDialog();
		}
		else
		{
			MessageBox.Show($"No form available for '{curSel.Name}'.", null, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}

	private void okBtn_Click(object sender, EventArgs e)
	{
		try
		{
			obj = ADsObject.GetObject(adspathText.Text);
			EnumerateInterfaces();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message, null, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void ADQIDlg_FormClosing(object sender, FormClosingEventArgs e) => Properties.Settings.Default.Save();
}

[System.AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
internal sealed class ADsViewerAttribute(Type adsObjType) : Attribute
{
	public Type SupportedObjectType { get; } = adsObjType;
}

internal interface IADsViewerForm
{
	IADsObject? ADsObjectInstance { get; }
}