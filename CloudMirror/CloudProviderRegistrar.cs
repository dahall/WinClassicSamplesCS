using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Provider;
using static Vanara.PInvoke.Kernel32;

namespace CloudMirror
{
	internal static class CloudProviderRegistrar
	{
		private const string STORAGE_PROVIDER_ACCOUNT = "TestAccount1";
		private const string STORAGE_PROVIDER_ID = "TestStorageProvider";

		public static async Task RegisterWithShell()
		{
			try
			{
				var info = new StorageProviderSyncRootInfo();
				info.Id = GetSyncRootId();
				info.Path = await StorageFolder.GetFolderFromPathAsync(ProviderFolderLocations.GetClientFolder());
				info.DisplayNameResource = "TestStorageProviderDisplayName";
				// This icon is just for the sample. You should provide your own branded icon here
				info.IconResource = "%SystemRoot%\\system32\\charmap.exe,0";
				info.HydrationPolicy = StorageProviderHydrationPolicy.Full;
				info.HydrationPolicyModifier = StorageProviderHydrationPolicyModifier.None;
				info.PopulationPolicy = StorageProviderPopulationPolicy.AlwaysFull;
				info.InSyncPolicy = StorageProviderInSyncPolicy.FileCreationTime | StorageProviderInSyncPolicy.DirectoryCreationTime;
				info.Version = "1.0.0";
				info.ShowSiblingsAsGroup = false;
				info.HardlinkPolicy = StorageProviderHardlinkPolicy.None;
				info.RecycleBinUri = new Uri("http://cloudmirror.example.com/recyclebin");

				// Context
				var syncRootIdentity = ProviderFolderLocations.GetServerFolder() + "->" + ProviderFolderLocations.GetClientFolder();

				//var contextString = "TestProviderContextString";
				var contextBuffer = CryptographicBuffer.ConvertStringToBinary(syncRootIdentity, BinaryStringEncoding.Utf8);
				info.Context = contextBuffer;

				var customStates = info.StorageProviderItemPropertyDefinitions;
				AddCustomState(customStates, "CustomStateName1", 1);
				AddCustomState(customStates, "CustomStateName2", 2);
				AddCustomState(customStates, "CustomStateName3", 3);

				StorageProviderSyncRootManager.Register(info);

				// Give the cache some time to invalidate
				Sleep(1000);
			}
			catch (Exception ex)
			{
				// to_hresult() will eat the exception if it is a result of check_hresult, otherwise the exception will get rethrown and
				// this method will crash out as it should
				Console.Write("Could not register the sync root, hr {0:X8}\n", ex.HResult);
				throw;
			}
		}

		public static void Unregister()
		{
			try
			{
				StorageProviderSyncRootManager.Unregister(GetSyncRootId());
			}
			catch (Exception ex)
			{
				// to_hresult() will eat the exception if it is a result of check_hresult, otherwise the exception will get rethrown and
				// this method will crash out as it should
				Console.Write("Could not unregister the sync root, hr {0:X8}\n", ex.HResult);
			}
		}

		private static void AddCustomState(ICollection<StorageProviderItemPropertyDefinition> customStates, string displayNameResource, int id) =>
			customStates.Add(new StorageProviderItemPropertyDefinition { DisplayNameResource = displayNameResource, Id = id });

		private static string GetSyncRootId() => $"{STORAGE_PROVIDER_ID}!{WindowsIdentity.GetCurrent().User}!{STORAGE_PROVIDER_ACCOUNT}";
	}
}