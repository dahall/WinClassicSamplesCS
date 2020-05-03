using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Shell32;
using static Vanara.PInvoke.ShlwApi;

namespace CloudMirror
{
	[ComVisible(true), Guid("3d781652-78c5-4038-87a4-ec5940ab560a")]
	public class ThumbnailProvider : IInitializeWithItem, IThumbnailProvider
	{
		private IShellItem2 _itemDest, _itemSrc;

		public HRESULT GetThumbnail(uint cx, out HBITMAP phbmp, out WTS_ALPHATYPE pdwAlpha)
		{
			// Retrieve thumbnails of the placeholders on demand by delegating to the thumbnail of the source items.
			try
			{
				using var thumbnailProviderSource = ComReleaserFactory.Create(_itemSrc.BindToHandler<IThumbnailProvider>(default, BHID.BHID_ThumbnailHandler.Guid()));
				thumbnailProviderSource.Item.GetThumbnail(cx, out phbmp, out pdwAlpha).ThrowIfFailed();
			}
			catch (Exception ex)
			{
				phbmp = HBITMAP.NULL;
				pdwAlpha = WTS_ALPHATYPE.WTSAT_UNKNOWN;
				return ex.HResult;
			}

			return HRESULT.S_OK;
		}

		public HRESULT Initialize(IShellItem item, STGM mode)
		{
			try
			{
				_itemDest = (IShellItem2)item;

				// We want to identify the original item in the source folder that we're mirroring, based on the placeholder item that we
				// get initialized with. There's probably a way to do this based on the file identity blob but this just uses path manipulation.
				string destPathItem = _itemDest.GetDisplayName(SIGDN.SIGDN_FILESYSPATH);

				Console.Write("Thumbnail requested for {0}\n", destPathItem);

				// Verify the item is underneath the root as we expect.
				if (!PathIsPrefix(ProviderFolderLocations.GetClientFolder(), destPathItem))
				{
					return HRESULT.E_UNEXPECTED;
				}

				// Find the relative segment to the sync root.
				var relativePath = new StringBuilder(MAX_PATH);
				if (!PathRelativePathTo(relativePath, ProviderFolderLocations.GetClientFolder(), FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY, destPathItem, FileFlagsAndAttributes.FILE_ATTRIBUTE_NORMAL))
					Win32Error.ThrowLastError();

				// Now combine that relative segment with the original source folder, which results in the path to the source item that
				// we're mirroring.
				var sourcePathItem = Path.Combine(ProviderFolderLocations.GetServerFolder(), relativePath.ToString());

				_itemSrc = SHCreateItemFromParsingName<IShellItem2>(sourcePathItem);
			}
			catch (Exception ex)
			{
				return ex.HResult;
			}

			return HRESULT.S_OK;
		}
	}
}