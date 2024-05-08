using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using Vanara.PInvoke.NetListMgr;

namespace NetworkListManager;

public class Program
{
	[MTAThread]
	public static int Main(string[] args)
	{
		if (args.Length == 0)
		{
			Console.Write("Enter action: ");
			args = [Console.ReadKey().KeyChar.ToString()];
		}
		int scenario = args.Length == 1 ? int.Parse(args[0]) : 0;
		switch (scenario)
		{
			case 1:
			case 2:
				QueryCurrentNetworkConnectivitySample(scenario == 2);
				break;

			case 3:
				QueryCurrentNetworkCostSample();
				break;

			case 4:
			case 5:
				ListenToNetworkConnectivityChangesSample(scenario == 5);
				break;

			case 6:
				ListenToNetworkCostChangesSample();
				break;

			default:
				Console.WriteLine("NetworkListManager.exe sample demonstrates how to use network list manager APIs." +
				"\n [parameter]" +
				"\n 1: Query current network connectivity: User has not opted into possible network usage charges." +
				"\n 2: Query current network connectivity: User has opted into possible network usage charges." +
				"\n 3: Query current network cost information." +
				"\n 4: Listen to network connectivity changes: User has not opted into possible network usage charges." +
				"\n 5: Listen to network connectivity changes: User has opted into possible network usage charges." +
				"\n 6: Listen to network cost changes.");
				break;
		}
		return 0;
	}

	private static UniqueConnectionpointToken FindConnectionPointAndAdvise(in Guid itf, object source, object sink)
	{
		IConnectionPointContainer container = (IConnectionPointContainer)source;
		Guid g = itf;
		container.FindConnectionPoint(ref g, out var connectionPoint);
		connectionPoint!.Advise(sink, out var token);
		return new UniqueConnectionpointToken(connectionPoint, token);
	}

	// typename T is the connection point interface we are connecting to.
	private static UniqueConnectionpointToken FindConnectionPointAndAdvise<T>(object source, object sink) =>
		FindConnectionPointAndAdvise(typeof(T).GUID, source, sink);

	private static void ListenToNetworkConnectivityChangesSample(bool optedIn)
	{
		Console.WriteLine("Listening to network connectivity changes.");
		if (optedIn)
		{
			Console.WriteLine("User has opted into possible network usage charges.");
		}
		else
		{
			Console.WriteLine("User has not opted into possible network usage charges.");
		}

		INetworkListManager networkListManager = new();

		using UniqueConnectionpointToken token = FindConnectionPointAndAdvise<INetworkListManagerEvents>(networkListManager, new NetworkConnectivityListener(optedIn, networkListManager));

		Console.WriteLine("Press Enter to stop.");
		Console.ReadKey(true);
	}

	private static void ListenToNetworkCostChangesSample()
	{
		Console.WriteLine("Listening to network cost changes.");

		INetworkCostManager networkCostManager = new();

		using UniqueConnectionpointToken token = FindConnectionPointAndAdvise<INetworkCostManagerEvents>(networkCostManager, new NetworkCostListener());

		Console.WriteLine("Press Enter to stop.");
		Console.ReadKey(true);
	}

	private static void QueryCurrentNetworkConnectivitySample(bool optedIn)
	{
		Console.WriteLine("Querying current network connectivity.");
		if (optedIn)
		{
			Console.WriteLine("User has opted into possible network usage charges.");
		}
		else
		{
			Console.WriteLine("User has not opted into possible network usage charges.");
		}

		INetworkListManager networkListManager = new();
		// Checks machine level connectivity via ipv4 or ipv6 or both.
		NLM_CONNECTIVITY connectivity = networkListManager.GetConnectivity();
		Utility.EvaluateAndReportConnectivity(optedIn, connectivity, networkListManager);
	}

	private static void QueryCurrentNetworkCostSample()
	{
		Console.WriteLine("Querying current network cost information.");

		// Use INetworkCostManager to query machine-wide cost associated with a network connection used for machine-wide Internet connectivity.
		INetworkCostManager networkCostManager = new();

		networkCostManager.GetCost(out var connectionCost);
		Utility.EvaluateAndReportConnectionCost(connectionCost);
	}

	internal class NetworkConnectivityListener(bool m_optedIn, INetworkListManager m_networkListManager) : INetworkListManagerEvents
	{
		public HRESULT ConnectivityChanged(NLM_CONNECTIVITY newConnectivity)
		{
			Console.WriteLine("INetworkListManagerEvents::ConnectivityChanged");
			Utility.EvaluateAndReportConnectivity(m_optedIn, newConnectivity, m_networkListManager);
			return HRESULT.S_OK;
		}
	}

	internal abstract class UniqueComToken<T, TTok>(T value, TTok token, Action<T, TTok> onclose) : IDisposable where T : class where TTok : struct
	{
		public T Instance { get; private set; } = value;

		public void Dispose()
		{
			onclose(Instance, token);
			Marshal.ReleaseComObject(Instance);
		}
	}

	internal class UniqueConnectionpointToken(IConnectionPoint value, int token) : UniqueComToken<IConnectionPoint, int>(value, token, (cp, t) => cp.Unadvise(t))
	{
	}

	private class NetworkCostListener : INetworkCostManagerEvents
	{
		public HRESULT CostChanged(NLM_CONNECTION_COST connectionCost, NLM_SOCKADDR? pDestAddr)
		{
			Console.WriteLine("INetworkCostManagerEvents::CostChanged");
			Utility.EvaluateAndReportConnectionCost(connectionCost);
			return HRESULT.S_OK;
		}

		public HRESULT DataPlanStatusChanged(NLM_SOCKADDR? pDestAddr) =>
			// This event is not used by the sample.
			HRESULT.S_OK;
	}
}