using Vanara;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.OleAut32;
using static Vanara.PInvoke.OleDb;

namespace RowsetViewer;

//-----------------------------------------------------------------------------
// Microsoft OLE DB RowsetViewer
// Copyright (C) 1994 - 1999 By Microsoft Corporation.
//
// @doc
//
// @module CBASE.H
//
//-----------------------------------------------------------------------------------

///////////////////////////////////////////////////////////////
// Defines
//
///////////////////////////////////////////////////////////////
enum SOURCE
{
	eInvalid = 0,

	//Object Source
	eCUnknown = 1,
	eCDataSource = 2,
	eCSession = 3,
	eCCommand = 4,
	eCMultipleResults = 5,
	eCRowset = 6,
	eCRow = 7,
	eCStream = 8,
	eCEnumerator = 9,
	eCBinder = 10,
	eCServiceComp = 11,
	eCDataLinks = 12,
	eCDataset = 13,
	eCTransaction = 14,
	eCTransactionOptions = 15,
	eCError = 16,
	eCCustomError = 17,
	eCRowPosition = 18,
	eCConnectionPoint = 19,
};


enum BASE_CLASS
{
	eCBase = 0x0001000,
	eCContainerBase = 0x0002000,
	eCAsynchBase = 0x0004000,
	eCPropertiesBase = 0x0008000,
	eCDataAccess = 0x0010000
};


//Use to quickly implement GetInterfaceAddress
//#define HANDLE_GETINTERFACE(interface)			\
//	if(riid == IID_##interface)					\
//		return (IUnknown**)&m_p##interface

//#define OBTAIN_INTERFACE(interface)				\
//	if(!m_p##interface)							\
//		TRACE_QI(m_pIUnknown, IID_##interface, (IUnknown**)&m_p##interface, GetObjectName())

//#define RELEASE_INTERFACE(interface)			\
//	if(m_p##interface)							\
//		TRACE_RELEASE(m_p##interface,	WIDESTRING(#interface))

//#define SOURCE_GETINTERFACE(pObject, type)		\
//	((pObject)? (type*)(pObject)->GetInterface(IID_##type) : NULL)

//#define SOURCE_GETOBJECT(pObject, source)		\
//	(((pObject) && (((pObject)->GetObjectType() == e##source) || ((pObject)->GetBaseType() & e##source))) ? (source*)(pObject) : NULL)

//#define SOURCE_GETPARENT(pObject, source)		\
//	((pObject)? (source*)(pObject)->GetParent(e##source) : NULL)


///////////////////////////////////////////////////////////////
// Functions
//
///////////////////////////////////////////////////////////////
static SOURCE GuidToSourceType(REFGUID guidType);
static SOURCE DetermineObjectType(object pIUnknown, SOURCE eSource);


///////////////////////////////////////////////////////////////
// Interface
//
///////////////////////////////////////////////////////////////
[ComVisible(true), Guid(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAggregate
{
}

/////////////////////////////////////////////////////////////////
// CBase class
//
/////////////////////////////////////////////////////////////////
[ComVisible(true), Guid()]
abstract class CBase(SOURCE eObjectType, CMainWindow pCMainWindow, CMDIChild? pCMDIChild = null) : IDisposable
{
	public HRESULT CreateObject(ref CBase pCSource, in Guid riid, object pIUnknown, uint dwCreateOpts = uint.MaxValue);
	public HRESULT ReleaseObject(uint ulExpectedRefCount);
	public HRESULT ReleaseChildren();

	public HRESULT SetInterface(in Guid riid, object pIUnknown);
	public object GetInterface(in Guid riid);

	public bool IsSameObject(object pIUnkObject);
	public CBase GetParent(SOURCE eSource);

	//Derived Object helpers (Devired Class implements this)
	public HRESULT AutoQI(uint dwCreateOpts);
	public HRESULT AutoRelease();
	public object GetInterfaceAddress(in Guid riid);

	public string GetObjectName();
	public uint GetObjectMenu();
	public int GetObjectImage();
	public Guid GetDefaultInterface();
	public void OnDefOperation();

	//UI - Helpers
	public HRESULT DisplayObject();
	public virtual string ObjectDesc { get; set; }

	//Inlines
	public SOURCE ObjectType { get; } = eObjectType;
	public BASE_CLASS BaseType { get; } = BASE_CLASS.eCBase;

	//Interface
	public COptionsSheet GetOptions();

	//Common OLE DB Interfaces
	object m_pIUnknown;
	ISupportErrorInfo m_pISupportErrorInfo;
	IAggregate m_pIAggregate;
	IService m_pIService;

	//Data
	HTREEITEM m_hTreeItem;
	CLSCTX m_dwCLSCTX = CLSCTX.CLSCTX_INPROC_SERVER;

	//Parent Info
	CBase m_pCParent;
	Guid m_guidSource;

	//BackPointers
	CMainWindow m_pCMainWindow = pCMainWindow ?? pCMDIChild.Parent;
	CMDIChild m_pCMDIChild = pCMDIChild;

	//IUnknown
	private bool disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				//Make sure this item is removed from the tree...
				//CObjTree* pCObjTree = m_pCMainWindow->m_pCMDIObjects->m_pCObjTree;
				//if (pCObjTree && m_hTreeItem)
				//{
				//	//NOTE: The object (after this desctructor) is no longer available,
				//	//so make sure that even if the node cannot be removed (child nodes),
				//	//we need to still remove the object reference so it doesn't try and access it anymore...
				//	pCObjTree->RemoveObject(this);
				//	pCObjTree->SetItemParam(m_hTreeItem, NULL);
				//}
			}

			// TODO: free unmanaged resources (unmanaged objects) and override finalizer
			// TODO: set large fields to null
			disposedValue = true;
		}
	}

	// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
	// ~CBase()
	// {
	//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
	//     Dispose(disposing: false);
	// }

	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}

/////////////////////////////////////////////////////////////////
// CUnknown class
//
/////////////////////////////////////////////////////////////////
class CUnknown : CBase
{
	//Constructors
	CUnknown(CMainWindow pCMainWindow, CMDIChild? pCMDIChild = default) : base(SOURCE.eCUnknown, pCMainWindow, pCMDIChild)
	{
	}

	//Derived Object helpers
	//Devired Class implements this...
	HRESULT AutoQI(uint dwCreateOpts) { return base.AutoQI(dwCreateOpts); }
	HRESULT AutoRelease() { return base.AutoRelease(); }
	object GetInterfaceAddress(in Guid riid) { return base.GetInterfaceAddress(riid); }

	string GetObjectName() { return "Unknown"; }
	uint GetObjectMenu() { return IDM_UNKNOWNMENU; }
	int GetObjectImage() { return IMAGE_QUESTION; }
	Guid GetDefaultInterface() { return IID_IUnknown; }
}


/////////////////////////////////////////////////////////////////
// CContainerBase class
//
/////////////////////////////////////////////////////////////////
class CContainerBase : CBase
{
	//Constructors
	CContainerBase(SOURCE eObjectType, CMainWindow pCMainWindow, CMDIChild? pCMDIChild = default);
	~CContainerBase();

	//IUnknown Helpers
	HRESULT AutoQI(uint dwCreateOpts);
	HRESULT AutoRelease();
	object GetInterfaceAddress(in Guid riid);

	//Members
	HRESULT FindConnectionPoint(in Guid riid, out IConnectionPoint ppIConnectionPoint);
	HRESULT AdviseListener(in Guid riid, ref uint pdwCookie);
	HRESULT UnadviseListener(in Guid riid, ref uint pdwCookie);

	IConnectionPointContainer? m_pIConnectionPointContainer;
}


/////////////////////////////////////////////////////////////////
// CConnectionPoint class
//
/////////////////////////////////////////////////////////////////
class CConnectionPoint : CBase
{
	//Constructors
	CConnectionPoint(CMainWindow pCMainWindow, CMDIChild? pCMDIChild = default);
	~CConnectionPoint();

	//IUnknown Helpers
	HRESULT AutoQI(uint dwCreateOpts);
	HRESULT AutoRelease();
	object GetInterfaceAddress(in Guid riid);

	//Derived Class
	string GetObjectName() { return "ConnectionPoint"; }
	uint GetObjectMenu() { return IDM_CONNECTIONPOINTMENU; }
	int GetObjectImage() { return IMAGE_FORM; }
	Guid GetDefaultInterface() { return typeof(IConnectionPoint).GUID; }
	string GetObjectDesc();

	//Members
	HRESULT GetConnectionInterface(in Guid pIID);

	//OLE DB Interfaces
	//[MANADATORY]
	IConnectionPoint m_pIConnectionPoint;

	//[OPTIONAL]

	////Data
	uint m_dwCookie;
}


/////////////////////////////////////////////////////////////////
// CAsynchBase class
//
/////////////////////////////////////////////////////////////////
class CAsynchBase : CContainerBase
{
	//Constructors
	CAsynchBase(SOURCE eObjectType, CMainWindow pCMainWindow, CMDIChild? pCMDIChild = default);
	~CAsynchBase();

	//IUnknown Helpers
	HRESULT AutoQI(uint dwCreateOpts);
	HRESULT AutoRelease();
	object GetInterfaceAddress(in Guid riid);

	//Members
	bool IsInitialized() { return m_fInitialized; }
	HRESULT Initialize();
	HRESULT Uninitialize();

	HRESULT Abort(HCHAPTER hChapter, DBASYNCHOP eOperation);
	HRESULT GetStatus(HCHAPTER hChapter, DBASYNCHOP eOperation, ref DBCOUNTITEM pulProgress, ref DBCOUNTITEM pulProgressMax, ref DBASYNCHPHASE peAsynchPhase, ref string ppwszStatusText);

	//OLE DB Interfaces
	//[MANADATORY]

	//[OPTIONAL]
	IDBInitialize? m_pIDBInitialize; //OLE DB interface
	IDBAsynchStatus? m_pIDBAsynchStatus; //OLE DB interface

	//Extra interfaces

	////Data
	uint m_dwCookieAsynchNotify;
	bool m_fInitialized;
}


/////////////////////////////////////////////////////////////////
// CPropertiesBase class
//
/////////////////////////////////////////////////////////////////
class CPropertiesBase : CAsynchBase
{
	//Constructors
	CPropertiesBase(SOURCE eObjectType, ref CMainWindow pCMainWindow, ref CMDIChild pCMDIChild = default);
	~CPropertiesBase();

	//IUnknown Helpers
	HRESULT AutoQI(uint dwCreateOpts);
	HRESULT AutoRelease();
	object GetInterfaceAddress(in Guid riid);

	//Members
	HRESULT SetProperties(uint cPropSets, ref DBPROPSET rgPropSets);

	//OLE DB Interfaces
	//[MANADATORY]
	IDBProperties m_pIDBProperties; //OLE DB interface

	//[OPTIONAL]

	//Data
}


///////////////////////////////////////////////////////////////////////////////
// Class CAggregate
// 
///////////////////////////////////////////////////////////////////////////////
class CAggregate : IAggregate
{
	CAggregate();
	~CAggregate();

	//Helpers
	HRESULT HandleAggregation(in Guid riid, out object ppIUnknown);
	HRESULT SetInner(object pIUnkInner);
	HRESULT ReleaseInner();

	//Data
	uint m_cRef;
	object m_spUnkInner;
}