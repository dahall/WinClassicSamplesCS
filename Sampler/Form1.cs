using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.PInvoke;
using Vanara.Windows.Shell;
using static Vanara.PInvoke.ComCtl32;
using static Vanara.PInvoke.Shell32;

namespace Sampler;

public partial class Form1 : Form
{
	private const string folderKey = "5EEB255733234c4dBECF9A128E896A1E";
	private const char sep = '\\';

	private DirectoryInfo[] topDirs = [];

	public Form1() => InitializeComponent();

	private static DirectoryInfo RootPath => new(Environment.ExpandEnvironmentVariables(Properties.Settings.Default.Root));

	private static void AddLeaf(TreeNode root, FileInfo leaf, ImageList imageList)
	{
		var fullLeafPath = leaf.FullName;
		var relLeafPath = fullLeafPath.Substring(root.Name.Length).TrimStart(sep);
		var leafPathParts = relLeafPath.Split(sep);

		var curParent = root;
		for (int i = 0; i < leafPathParts.Length - 1; i++)
		{
			var childKey = Path.Combine(curParent.Name, leafPathParts[i]);
			var childIdx = curParent.Nodes.IndexOfKey(childKey);
			curParent = childIdx == -1 ? AddSystemNode(curParent.Nodes, childKey, imageList) : curParent.Nodes[childIdx];
		}
	}

	private static TreeNode AddSystemNode(TreeNodeCollection parent, string systemItemPath, ImageList imageList)
	{
		string ext, ilkey;
		if (File.Exists(systemItemPath))
		{
			ext = Path.GetExtension(systemItemPath);
			if (ext.Equals(".exe", StringComparison.InvariantCultureIgnoreCase) || ext.Equals(".lnk", StringComparison.InvariantCultureIgnoreCase))
				ext = systemItemPath;
			ilkey = ext;
		}
		else
		{
			ext = ilkey = folderKey;
		}

		if (!imageList.Images.ContainsKey(ilkey))
		{
			try
			{
				if (ilkey == folderKey)
					imageList.Images.Add(ilkey, GetSystemIcon()!);
				else
					imageList.Images.Add(ilkey, IconExtension.GetFileIcon(ext, IconSize.Small)!.ToBitmap());
			}
			catch (ArgumentException ex)
			{
				throw new ArgumentException($"File \"{systemItemPath}\" does" + " not exist!", ex);
			}
		}
		return parent.Add(systemItemPath, Path.GetFileName(systemItemPath), ilkey, ilkey);
	}

	private static Bitmap? GetSystemIcon()
	{
		var shfi = new SHFILEINFO();
		HIMAGELIST hSystemImageList = SHGetFileInfo("", 0, ref shfi, SHFILEINFO.Size, SHGFI.SHGFI_SYSICONINDEX | SHGFI.SHGFI_SMALLICON);
		return hSystemImageList.IsNull ? null : ImageList_GetIcon(hSystemImageList, shfi.iIcon, IMAGELISTDRAWFLAGS.ILD_TRANSPARENT)?.ToBitmap();
	}

	private void explorerBrowser_SelectionChanged(object sender, EventArgs e)
	{
	}

	private void Form1_Load(object sender, EventArgs e)
	{
		//treeLoader.RunWorkerAsync();
		projectView.BeginUpdate();
		projectView.Nodes.Clear();
		LoadTree();
		projectView.EndUpdate();
	}

	private void LoadTree()
	{
		var root = projectView.Nodes.Add(RootPath.FullName, "Samples");
		root.Expand();
		foreach (var fi in RootPath.EnumerateFiles("*.csproj", SearchOption.AllDirectories))
		{
			if (!fi.DirectoryName!.EndsWith("\\Sampler"))
				AddLeaf(root, fi, projectView.ImageList);
		}
	}

	private void projectView_AfterSelect(object sender, TreeViewEventArgs e)
	{
		var di = new DirectoryInfo(e.Node!.Name);
		var prj = di.Exists ? di.EnumerateFiles("*.csproj").FirstOrDefault() : null;
		explorerBrowser.Navigate(new ShellFolder(e.Node.Name));
		if (prj != null)
		{
			titleLabel.Text = Path.GetFileNameWithoutExtension(prj.Name);
		}
		else
		{
			titleLabel.Text = "[None]";
		}
	}

	private void runButton_Click(object sender, EventArgs e)
	{
		MessageBox.Show("This is a placeholder for running the selected sample project. Implementing this functionality remains to do.");
		//var session = conEmuControl1.Start(new ConEmuStartInfo()
		//{
		//	AnsiStreamChunkReceivedEventSink = (sender, args) => sbText.Append(args.GetMbcsText()),
		//	ConsoleProcessCommandLine = "ping 8.8.8.8",
		//	LogLevel = ConEmuStartInfo.LogLevels.Basic
		//});
		//session.ConsoleProcessExited += delegate
		//{
		//	var match = Regex.Match(sbText.ToString(), @"\(.*\b(?<pc>\d+)%.*?\)", RegexOptions.Multiline);
		//	if (!match.Success)
		//	{
		//		labelWaitOrResult.Text = "Ping execution completed, failed to parse the result.";
		//		labelWaitOrResult.BackColor = Color.PaleVioletRed;
		//	}
		//	else
		//	{
		//		labelWaitOrResult.Text = $"Ping execution completed, lost {match.Groups["pc"].Value} per cent of packets.";
		//		labelWaitOrResult.BackColor = Color.Lime;
		//	}
		//};
		//session.ConsoleEmulatorClosed += delegate { form.Close(); };
	}

	private void treeLoader_DoWork(object sender, DoWorkEventArgs e)
	{
	}

	private void treeLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
	}
}