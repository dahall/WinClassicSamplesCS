namespace vshadow;

internal class Program
{
	private static void Main(string[] args)
	{
		VssClient client = new(Vanara.PInvoke.VssApi.VSS_SNAPSHOT_CONTEXT.VSS_CTX_ALL);
		client.QuerySnapshotSet();
	}
}