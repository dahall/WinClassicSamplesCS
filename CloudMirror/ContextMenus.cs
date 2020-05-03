using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.Storage.Provider;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace CloudMirror
{
	[ComVisible(true), Guid("165cd069-d9c8-42b4-8e37-b6971afa4494")]
	public class TestExplorerCommandHandler : IExplorerCommand, IObjectWithSite, IExplorerCommandState
	{
		private object _site;

		public HRESULT EnumSubCommands(out IEnumExplorerCommand ppEnum)
		{
			ppEnum = null; return HRESULT.E_NOTIMPL;
		}

		public HRESULT GetCanonicalName(out Guid pguidCommandName)
		{
			pguidCommandName = Guid.Empty; return HRESULT.E_NOTIMPL;
		}

		public HRESULT GetFlags(out EXPCMDFLAGS pFlags)
		{
			pFlags = EXPCMDFLAGS.ECF_DEFAULT; return HRESULT.S_OK;
		}

		public HRESULT GetIcon(IShellItemArray psiItemArray, out string ppszIcon)
		{
			ppszIcon = null; return HRESULT.E_NOTIMPL;
		}

		public HRESULT GetSite(in Guid riid, out object ppvSite)
		{
			var myriid = riid;
			HRESULT hr = Marshal.QueryInterface(Marshal.GetIUnknownForObject(_site), ref myriid, out var ppv);
			ppvSite = hr.Succeeded ? Marshal.GetObjectForIUnknown(ppv) : null;
			return hr;
		}

		public HRESULT GetState(IShellItemArray psiItemArray, bool fOkToBeSlow, out EXPCMDSTATE pCmdState)
		{
			pCmdState = EXPCMDSTATE.ECS_ENABLED; return HRESULT.S_OK;
		}

		public HRESULT GetTitle(IShellItemArray psiItemArray, out string ppszName)
		{
			ppszName = "TestCommand"; return HRESULT.S_OK;
		}

		public HRESULT GetToolTip(IShellItemArray psiItemArray, out string ppszInfotip)
		{
			ppszInfotip = null; return HRESULT.E_NOTIMPL;
		}

		public HRESULT Invoke(IShellItemArray selection, IBindCtx pbc)
		{
			try
			{
				var hwnd = HWND.NULL;

				if (_site != null)
				{
					// Get the HWND of the browser from the site to parent our message box to
					IUnknown_QueryService(_site, SID_STopLevelBrowser, new Guid("00000000-0000-0000-C000-000000000046") /* IID_IUnknown */, out var browser).ThrowIfFailed();
					IUnknown_GetWindow(browser, out hwnd);
				}

				Console.Write("Cloud Provider Command received\n");

				// Set a new custom state on the selected files
				var prop = new StorageProviderItemProperty
				{
					Id = 3,
					Value = "Value3",
					// This icon is just for the sample. You should provide your own branded icon here
					IconResource = "shell32.dll,-259"
				};

				for (uint i = 0; i < selection.GetCount(); i++)
				{
					using var pShellItem = ComReleaserFactory.Create(selection.GetItemAt(i));

					using var fullPath = pShellItem.Item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

					var item = (IStorageItem)StorageFile.GetFileFromPathAsync(fullPath);
					StorageProviderItemProperties.SetAsync(item, new[] { prop }).GetResults();

					SHChangeNotify(SHCNE.SHCNE_UPDATEITEM, SHCNF.SHCNF_PATHW, fullPath);
				}
			}
			catch (Exception ex)
			{
				return ex.HResult;
			}

			return HRESULT.S_OK;
		}

		public HRESULT SetSite(object pUnkSite)
		{
			_site = pUnkSite; return HRESULT.S_OK;
		}
	}
}