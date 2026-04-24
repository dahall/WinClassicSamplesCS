using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using System.Data;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Windows.Forms;
[assembly: SupportedOSPlatform("windows")]

namespace Sampler;

public partial class NuGetPackages : Form
{
	const string framework = "net8.0";
	private const string Prefix = "Vanara";
	readonly List<IPackageSearchMetadata> packages = [];
	static readonly ILogger logger = NullLogger.Instance; // TODO: Replace with actual logger if needed
	static readonly CancellationToken cancellationToken = CancellationToken.None; // TODO: Replace with actual cancellation token if needed

	public NuGetPackages() => InitializeComponent();

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);
		listBox1.Format += (s, args) => args.Value = args.ListItem is IPackageSearchMetadata r ? r.Title : args.ListItem?.ToString() ?? string.Empty;
		Task.Factory.StartNew(async () =>
		{
			await foreach (var package in NuGetUtils.LoadNuGetPackageListAsync(Prefix, logger, cancellationToken))
				if (package.Identity.Id.StartsWith(Prefix + '.', StringComparison.OrdinalIgnoreCase))
					packages.Add(package);
			listBox1.Invoke(() => { listBox1.Items.Clear(); listBox1.DataSource = packages; });
		}, cancellationToken);
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		try { Directory.Delete(Path.Combine(Path.GetTempPath(), "NuGetDownloads"), true); } catch { }
		base.OnFormClosing(e);
	}

	private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
	{
		listBox2.Items.Clear();
		if (listBox1.SelectedItem is IPackageSearchMetadata metadata)
			Task.Factory.StartNew(() =>
			{
				string[] items = NuGetUtils.LoadPackageVersionsAsync(metadata, logger, cancellationToken).Result;
				listBox2.Invoke(() => listBox2.Items.AddRange(items));
			}, cancellationToken);
	}

	private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
	{
		listBox3.Items.Clear();
		if (listBox1.SelectedItem is IPackageSearchMetadata metadata && listBox2.SelectedItem is string ver)
		{
			listBox3.Items.Add("Getting dependencies...");
			Task.Factory.StartNew(async () =>
			{
				HashSet<SourcePackageDependencyInfo> dependencies = [];
				await NuGetUtils.ResolveDependenciesAsync(metadata.Identity, NuGetFramework.Parse(framework), dependencies, null, logger, cancellationToken);
				listBox3.Invoke(() => listBox3.Items[0] = "Downloading packages...");
				string downloadDir = Path.Combine(Path.GetTempPath(), "NuGetDownloads");
				await NuGetUtils.DownloadPackagesAsync(dependencies.Select(dep => new PackageIdentity(dep.Id, dep.Version)).Where(id => id.Id.StartsWith(Prefix + '.', StringComparison.OrdinalIgnoreCase)), downloadDir, logger, cancellationToken);
				listBox3.Invoke(() => listBox3.Items[0] = "Extracting files...");
				string extractDir = Path.Combine(downloadDir, "extracted");
				await NuGetUtils.ExtractAssembliesFromPacakgesAsync(downloadDir, framework, extractDir, logger, cancellationToken);
				listBox3.Invoke(() => { listBox3.Items.Clear(); listBox3.Items.AddRange(Array.ConvertAll(Directory.GetFiles(extractDir), Path.GetFileName)!); });
			}, cancellationToken);
		}
	}
}