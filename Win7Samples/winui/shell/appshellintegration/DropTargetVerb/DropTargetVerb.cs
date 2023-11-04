using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.PInvoke;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

// This demonstrates how implement a shell verb using the DropTarget method this method is prefered for verb implementations that need to
// work on Windows XP as it provides the most flexibility, it is simple, and supports out of process activation.
//
// This sample implements a stand alone local server COM object but it is expected that the verb implementation will be integreated into
// existing applications. To do that have your main application object register a class factory for itself and have that object implement
// IDropTarget for the verbs of your application. Note that COM will launch your application if it is not already running and will connect to
// an already running instance of your application if it is already running. These are features of the COM based verb implementation methods
//
// It is also possible (but not recomended) to create in process implementations of this object. To do that follow this sample but replace
// the local server COM object with an inproc server.
//
// version note. the DropTarget method works on Windows XP and above this sample demonstrates how to use the APIs on XP as well as those o
// Vista or above depending ont he version you compile with...

namespace DropTargetVerb
{
	[Guid("4f0ecd66-5b4d-4821-a73b-aaa64023e19c")]
	public class CDropTargetVerb : CAppMessageLoop, IDropTarget, IObjectWithSite
	{
		// Each verb has to have a unique COM object, run UUIDGEN to create new CLSID values for your new verbs
		private const string c_szVerbDisplayName = "DropTarget Verb Sample";

		private HWND _hwnd;
		private IDataObject _pdtobj;   // selected items on XP
		private object _punkSite;    // optional site, supports IObjectWithSite impl

		public CDropTargetVerb()
		{
		}

		// a CIDA structure represents a set of shell items, create the Nth item from that set in the form of an IShellItem
		//
		// this uses XP SP1 APIs so it works downlevel
		public static HRESULT CreateShellItemFromHIDA(ref CIDA pida, int iItem, out IShellItem ppsi)
		{
			ppsi = null;
			HRESULT hr = HRESULT.E_FAIL;
			if (iItem < pida.cidl)
			{
				// cast needed due to overload of the type of the 3rd param, when the first 2 params are null this is an absolute IDList
				hr = SHCreateShellItem(pida.GetFolderPIDL(), null, pida.GetItemRelativePIDL(iItem), out ppsi);
			}
			return hr;
		}

		// this sample is a local server drop target so it must enter a message loop and wait for COM to make calls on the registered object
		public void Run()
		{
			var classFactory = new CStaticClassFactory<CDropTargetVerb>(this);
			classFactory.Register(CLSCTX.CLSCTX_LOCAL_SERVER, REGCLS.REGCLS_SINGLEUSE);
			MessageLoop();
		}

		HRESULT IDropTarget.DragEnter(IDataObject pDataObj, MouseButtonState grfKeyState, POINT pt, ref DROPEFFECT pdwEffect) => HRESULT.S_OK;

		HRESULT IDropTarget.DragLeave() => HRESULT.S_OK;

		// IDropTarget this is the required interface for a verb implemeting the DropTarget method DragEnter is called to enable the
		// implementaiton to zero the output dwEffect value, indicating that the verb does not accept the input data object. this is rarely used.
		HRESULT IDropTarget.DragOver(MouseButtonState grfKeyState, POINT pt, ref DROPEFFECT pdwEffect) => HRESULT.S_OK;

		// this method is what is called to invoke the verb and is typically the only method that needs to be implemented this is the method
		// that is called to invoke the verb the data object represents the selection and is converted into a shell item array to address the
		// items being acted on.
		HRESULT IDropTarget.Drop(IDataObject pDataObj, MouseButtonState grfKeyState, POINT pt, ref DROPEFFECT pdwEffect)
		{
			_pdtobj = pDataObj;

			// capture state from the site, the HWND of the parent (that should not be used for a modal
			// window) but might be useful for positioning the UI of this verb
			IUnknown_GetWindow(_punkSite, out _hwnd);

			QueueAppCallback();

			pdwEffect = DROPEFFECT.DROPEFFECT_NONE;   // didn't move or copy the file
			return HRESULT.S_OK;
		}

		HRESULT IObjectWithSite.GetSite(in Guid riid, out object ppvSite)
		{
			ppvSite = null;
			return _punkSite != null ? ShellHelpers.QueryInterface(_punkSite, riid, out ppvSite) : HRESULT.E_FAIL;
		}

		// IObjectWithSite the IObjectWithSite implementation for a verb is optional, it provides access to the invoking code, in the case of
		// verbs activated in the context of the explorer the programming model of the explorer can be accessed via the site
		HRESULT IObjectWithSite.SetSite(object pUnkSite)
		{
			_punkSite = pUnkSite;
			return HRESULT.S_OK;
		}

		protected override void OnAppCallback()
		{
			// on Vista SHCreateShellItemArrayFromDataObject is supported and will convert the data object into the set of shell items
			// directly, use that if you work on Vista and above
			var hr = SHCreateShellItemArrayFromDataObject(_pdtobj, typeof(IShellItemArray).GUID, out var psia);
			if (hr.Succeeded)
			{
				var count = psia.GetCount();
				if (count > 0 && psia.GetItemAt(0) is IShellItem2 psi)
				{
					var pszName = psi.GetDisplayName(SIGDN.SIGDN_PARENTRELATIVEPARSING);
					var szMsg = $"{count} item(s), first item is named {pszName}";
					System.Windows.Forms.MessageBox.Show(szMsg, c_szVerbDisplayName);
				}
			}
		}

		[STAThread]
		private static void Main(string[] args)
		{
			const string c_szProgID = "txtfile";
			const string c_szVerbName = "DropTargetVerb";

			if (args.Length > 0)
			{
				if (string.Equals(args[0], "-Embedding", StringComparison.OrdinalIgnoreCase))
				{
					new CDropTargetVerb().Run();
				}
				else if (string.Equals(args[0], "-Unregister", StringComparison.OrdinalIgnoreCase))
				{
					var re = new CRegisterExtension(typeof(CDropTargetVerb).GUID);
					re.UnRegisterObject();
					re.UnRegisterVerb(c_szProgID, c_szVerbName);
					System.Windows.Forms.MessageBox.Show("Uninstalled DropTarget Verb Sample for .txt files", c_szVerbDisplayName);
				}
			}
			else
			{
				var re = new CRegisterExtension(typeof(CDropTargetVerb).GUID);

				var hr = re.RegisterAppAsLocalServer(c_szVerbDisplayName);
				if (hr.Succeeded)
				{
					// register this verb on .txt files ProgID
					hr = re.RegisterDropTargetVerb(c_szProgID, c_szVerbName, c_szVerbDisplayName);
					if (hr.Succeeded)
					{
						hr = re.RegisterVerbAttribute(c_szProgID, c_szVerbName, "NeverDefault");
						if (hr.Succeeded)
						{
							System.Windows.Forms.MessageBox.Show("Installed DropTarget Verb Sample for .txt files\n\nright click on a .txt file and choose 'DropTarget Verb Sample' to see this in action",
								c_szVerbDisplayName);
						}
					}
				}
			}
		}
	}
}
