using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.Storage.Provider;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.SearchApi;
using static Vanara.PInvoke.Shell32;

namespace CloudMirror
{
	internal static class Utilities
	{
		private const string MSSEARCH_INDEX = "SystemIndex";
		private static readonly PROPERTYKEY PKEY_StorageProviderTransferProgress = new PROPERTYKEY(new Guid(0xE77E90DF, 0x6271, 0x4F5B, 0x83, 0x4F, 0x2D, 0xD1, 0xF2, 0x45, 0xDD, 0xA4), 4);

		public static void AddFolderToSearchIndexer(string folder)
		{
			var url = "file:///" + folder;

			try
			{
				using var searchManager = ComReleaserFactory.Create(new ISearchManager());
				using var searchCatalogManager = ComReleaserFactory.Create(searchManager.Item.GetCatalog(MSSEARCH_INDEX));
				using var searchCrawlScopeManager = ComReleaserFactory.Create(searchCatalogManager.Item.GetCrawlScopeManager());
				searchCrawlScopeManager.Item.AddDefaultScopeRule(url, true, FOLLOW_FLAGS.FF_INDEXCOMPLEXURLS);
				searchCrawlScopeManager.Item.SaveAll();

				Console.Write("Succesfully called AddFolderToSearchIndexer on \"{0}\"\n", url);
			}
			catch (Exception ex)
			{
				// winrt.to_hresult() will eat the exception if it is a result of winrt.check_hresult, otherwise the exception will get
				// rethrown and this method will crash out as it should
				Console.Write("Failed on call to AddFolderToSearchIndexer for \"{0}\" with {1:X8}\n", url, ex.HResult);
				throw;
			}
		}

		public static async void ApplyCustomStateToPlaceholderFile(string path, string filename, StorageProviderItemProperty prop)
		{
			try
			{
				var fullPath = Path.Combine(path, filename);

				var customProperties = new StorageProviderItemProperty[] { prop };

				var item = await StorageFile.GetFileFromPathAsync(fullPath);
				await StorageProviderItemProperties.SetAsync(item, customProperties);
			}
			catch (Exception ex)
			{
				// winrt.to_hresult() will eat the exception if it is a result of winrt.check_hresult, otherwise the exception will get
				// rethrown and this method will crash out as it should
				Console.Write("Failed to set custom state with {0:X8}\n", ex.HResult);
			}
		}

		public static void ApplyTransferStateToFile(string fullPath, in CF_CALLBACK_INFO callbackInfo, long total, long completed)
		{
			// Tell the Cloud File API about progress so that toasts can be displayed
			CfReportProviderProgress(callbackInfo.ConnectionKey, callbackInfo.TransferKey, Convert.ToInt64(total), Convert.ToInt64(completed)).ThrowIfFailed();
			Console.Write("Succesfully called CfReportProviderProgress \"{0}\" with {1}/{2}\n", fullPath, completed, total);

			// Tell the Shell so File Explorer can display the progress bar in its view
			try
			{
				// First, get the Volatile property store for the file. That's where the properties are maintained.
				using var shellItem = ComReleaserFactory.Create(SHCreateItemFromParsingName<IShellItem2>(fullPath));

				using var propStoreVolatile = ComReleaserFactory.Create(shellItem.Item.GetPropertyStore(GETPROPERTYSTOREFLAGS.GPS_READWRITE | GETPROPERTYSTOREFLAGS.GPS_VOLATILEPROPERTIESONLY, typeof(IPropertyStore).GUID));

				// The PKEY_StorageProviderTransferProgress property works with a ulong array that is two elements, with element 0 being the
				// amount of data transferred, and element 1 being the total amount that will be transferred.
				propStoreVolatile.Item.SetValue(PKEY_StorageProviderTransferProgress, new long[] { completed, total }, false);

				// Set the sync transfer status accordingly
				propStoreVolatile.Item.SetValue(PROPERTYKEY.System.SyncTransferStatus, (completed < total) ? SYNC_TRANSFER_STATUS.STS_TRANSFERRING : SYNC_TRANSFER_STATUS.STS_NONE, false);

				// Without this, all your hard work is wasted.
				propStoreVolatile.Item.Commit();

				// Broadcast a notification that something about the file has changed, so that apps who subscribe (such as File Explorer)
				// can update their UI to reflect the new progress
				using var ptr = new SafeCoTaskMemString(fullPath);
				SHChangeNotify(SHCNE.SHCNE_UPDATEITEM, SHCNF.SHCNF_PATHW, ptr);

				Console.Write("Succesfully Set Transfer Progress on \"{0}\" to {1}/{2}\n", fullPath, completed, total);
			}
			catch (Exception ex)
			{
				// winrt.to_hresult() will eat the exception if it is a result of winrt.check_hresult, otherwise the exception will get
				// rethrown and this method will crash out as it should
				Console.Write("Failed to Set Transfer Progress on \"{0}\" with {1:X8}\n", fullPath, ex.HResult);
			}
		}

		public static string ConvertSidToStringSid(PSID sid) => sid.ToString("D");

		public static ulong FileTimeToLargeInteger(FILETIME fileTime) => fileTime.ToUInt64();

		public static ulong LongLongToLargeInteger(long longlong) => Convert.ToUInt64(longlong);

		public static CF_OPERATION_INFO ToOperationInfo(in CF_CALLBACK_INFO info, [In] CF_OPERATION_TYPE operationType) =>
			new CF_OPERATION_INFO { StructSize = (uint)Marshal.SizeOf<CF_OPERATION_INFO>(), Type = operationType, ConnectionKey = info.ConnectionKey, TransferKey = info.TransferKey };
	}
}