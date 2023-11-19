using System.Runtime.InteropServices.ComTypes;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Vanara.PInvoke.NetListMgr;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;

namespace NetworkCost;

static partial class NetCostSample
{
	[ComVisible(true), ClassInterface(ClassInterfaceType.None)]
	public class CNetCostEventSink : INetworkCostManagerEvents, INetworkConnectionCostEvents, IDisposable
	{
		public int m_dwCookie;
		public IConnectionPoint? m_pConnectionPoint;
		public Guid m_riid;
		public DESTINATION_INFO m_destSockAddr = new();
		public static ComReleaser<INetworkCostManager> s_pCostManager = ComReleaserFactory.Create(new INetworkCostManager());
		public static INetworkCostManager m_pCostManager => s_pCostManager.Item;

		//Default InterfaceGuid, stored to detect change in interface, when there is a dataplan status change.
		private Guid m_defaultInterfaceGuid = Guid.Empty;

		//********************************************************************************************
		// Function:  CNetCostEventSink() & ~CNetCostEventSink()
		//
		// Description: Constructor and destructor for CNetCostEventSink class
		//
		//********************************************************************************************
		public CNetCostEventSink(DESTINATION_INFO? pDestAddress, in Guid riid)
		{
			m_riid = riid;
			if (pDestAddress != null)
			{
				m_destSockAddr.addrString = pDestAddress.addrString;
				m_destSockAddr.ipAddr = pDestAddress.ipAddr;
			}
			else
				m_destSockAddr.addrString = string.Empty;
		}

		//********************************************************************************************
		// Function: StartListeningForEvents
		//
		// Description: Creates the CNetCostEventSink object, and start a thread to perform an advise on it
		//
		//********************************************************************************************
		public static CNetCostEventSink StartListeningForEvents(in Guid riid, [Optional] DESTINATION_INFO? pDestAddress)
		{
			// Create our CNetCostEventSink object that will be used to advise to the Connection point
			var ppSinkCostMgr = new CNetCostEventSink(pDestAddress, riid);

			//If register for destination cost notifications, call SetDestinationAddresses to register the requested Destination IP addresses
			if (riid == typeof(INetworkCostManagerEvents).GUID && pDestAddress?.addrString?.Length > 0)
			{
				m_pCostManager.SetDestinationAddresses(1, new[] { pDestAddress.ipAddr! }, true);
			}

			var pCpc = (IConnectionPointContainer)m_pCostManager;
			var wr_riid = riid;
			pCpc.FindConnectionPoint(ref wr_riid, out ppSinkCostMgr.m_pConnectionPoint);
			ppSinkCostMgr.m_pConnectionPoint?.Advise(ppSinkCostMgr, out ppSinkCostMgr.m_dwCookie);

			return ppSinkCostMgr;
		}

		//********************************************************************************************
		// Function: CostChanged
		//
		// Description: Callback function to display new machine cost
		//
		//********************************************************************************************
		public HRESULT CostChanged(NLM_CONNECTION_COST newCost, NLM_SOCKADDR? pDestAddr)
		{
			GetLocalTime(out _);
			Console.Write("\n***********************************\n");
			if (pDestAddr != null)
			{
				Console.Write("Cost Change for Destination address : {0}\n", g_pSinkDestCostMgr?.m_destSockAddr.addrString);
			}

			else
			{
				Console.Write("Machine Cost changed\n");
			}

			DisplayCostDescription(newCost);
			return HRESULT.S_OK;
		}

		//********************************************************************************************
		// Function: DataPlanStatusChanged
		//
		// Description: Callback function to display new machine data plan status.
		//
		//********************************************************************************************
		public HRESULT DataPlanStatusChanged(NLM_SOCKADDR? pDestAddr)
		{
			GetLocalTime(out _);
			Console.Write("\n***********************************\n");

			if (pDestAddr != null)
			{
				Console.Write("New Data Plan Status for Destination address : {0}\n", m_destSockAddr.addrString);
			}
			else
			{
				Console.Write("Machine Data Plan Status Changed\n");
			}

			try
			{
				m_pCostManager.GetDataPlanStatus(out var dataPlanStatus, pDestAddr);
				//If there is an interface change, applications should disconnect and reconnect to the new interface
				if (!IsEqualGUID(dataPlanStatus.InterfaceGuid, m_defaultInterfaceGuid))
				{
					Console.Write("There is an interface change. Please disconnect and reconnect to the new interface \n");
					m_defaultInterfaceGuid = dataPlanStatus.InterfaceGuid;
				}
				DisplayDataPlanStatus(dataPlanStatus);
			}
			catch (Exception ex)
			{
				DisplayError(ex.HResult);
				return ex.HResult;
			}
			return HRESULT.S_OK;
		}

		//********************************************************************************************
		// Function: ConnectionCostChanged
		//
		// Description: Callback function to display new connection cost
		//
		//********************************************************************************************
		public HRESULT ConnectionCostChanged(Guid connectionId, NLM_CONNECTION_COST newCost)
		{
			GetLocalTime(out _);
			Console.Write("\n***********************************\n");
			Console.Write("Connection Cost Changed\n");

			//get connection ID
			Console.Write("Connection ID    :    {0}\n", connectionId);
			DisplayCostDescription(newCost);
			return HRESULT.S_OK;
		}

		//********************************************************************************************
		// Function: ConnectionDataPlanStatusChanged
		//
		// Description: Callback function to display new connection data plan status.
		//
		//********************************************************************************************
		public HRESULT ConnectionDataPlanStatusChanged(Guid connectionId)
		{
			GetLocalTime(out _);
			Console.Write("\n***********************************\n");
			Console.Write("Connection data plan status changed\n");
			//get connection ID
			Console.Write("Connection ID    :    {0}\n", connectionId);
			try
			{
				using var pLocalNLM = ComReleaserFactory.Create(new INetworkListManager());
				GetConnectionFromGUID(pLocalNLM.Item, connectionId, out var ppConnection).ThrowIfFailed();
				using var pConnection = ComReleaserFactory.Create(ppConnection!);
				var pConnectionCost = (INetworkConnectionCost)pConnection.Item;
				var dataPlanStatus = pConnectionCost.GetDataPlanStatus();
				DisplayDataPlanStatus(dataPlanStatus);
			}
			catch (Exception ex)
			{
				DisplayError(ex.HResult);
				return ex.HResult;
			}
			return HRESULT.S_OK;
		}

		public void Dispose()
		{
			m_pConnectionPoint?.Unadvise(m_dwCookie);
		}
	}
}