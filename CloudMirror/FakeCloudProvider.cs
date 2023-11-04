using static Vanara.PInvoke.CldApi;

namespace CloudMirror
{
	static class FakeCloudProvider
	{
		static CF_CONNECTION_KEY s_transferCallbackConnectionKey;
		static CF_CALLBACK_REGISTRATION[] s_MirrorCallbackTable =
		{
			new CF_CALLBACK_REGISTRATION { Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_FETCH_DATA, Callback = new CF_CALLBACK(OnFetchData) },
			new CF_CALLBACK_REGISTRATION { Type = CF_CALLBACK_TYPE.CF_CALLBACK_TYPE_CANCEL_FETCH_DATA, Callback = new CF_CALLBACK(OnCancelFetchData) },
			CF_CALLBACK_REGISTRATION.CF_CALLBACK_REGISTRATION_END
		};

		public static bool Start(string serverFolder = "", string clientFolder = "")
		{
			var result = false;

			if (ProviderFolderLocations.Init(serverFolder, clientFolder))
			{
				var onStop  = new CancellationTokenSource();

				try
				{
					// Stage 1: Setup
					//--------------------------------------------------------------------------------------------
					// The client folder (syncroot) must be indexed in order for states to properly display
					Utilities.AddFolderToSearchIndexer(ProviderFolderLocations.ClientFolder);
					// Start up the task that registers and hosts the services for the shell (such as custom states, menus, etc)
					ShellServices.InitAndStartServiceTask(onStop.Token);
					// Register the provider with the shell so that the Sync Root shows up in File Explorer
					CloudProviderRegistrar.RegisterWithShell();
					// Hook up callback methods (in this class) for transferring files between client and server
					ConnectSyncRootTransferCallbacks();
					// Create the placeholders in the client folder so the user sees something
					Placeholders.Create(ProviderFolderLocations.ServerFolder, "", ProviderFolderLocations.ClientFolder);

					// Stage 2: Running
					//--------------------------------------------------------------------------------------------
					// The file watcher loop for this sample will run until the user presses Ctrl-C.
					// The file watcher will look for any changes on the files in the client (syncroot) in order
					// to let the cloud know.
					CloudProviderSyncRootWatcher.WatchAndWait();

					// And if we got here, then this was a normally run test versus crash-o-rama
					result = true;
				}
				finally
				{
					// Stage 3: Done Running-- caused by CTRL-C
					//--------------------------------------------------------------------------------------------
					// Unhook up those callback methods
					onStop.Cancel();

					DisconnectSyncRootTransferCallbacks();

					// A real sync engine should NOT unregister the sync root upon exit.
					// This is just to demonstrate the use of StorageProviderSyncRootManager.Unregister.
					CloudProviderRegistrar.Unregister();
				}
			}

			return result;
		}

		static void ConnectSyncRootTransferCallbacks()
		{
			try
			{
				// Connect to the sync root using Cloud File API
				CfConnectSyncRoot(ProviderFolderLocations.ClientFolder, s_MirrorCallbackTable, default,
					CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_PROCESS_INFO | CF_CONNECT_FLAGS.CF_CONNECT_FLAG_REQUIRE_FULL_FILE_PATH,
					out s_transferCallbackConnectionKey).ThrowIfFailed();
			}
			catch (Exception ex)
			{
				// winrt.to_hresult() will eat the exception if it is a result of winrt.check_hresult,
				// otherwise the exception will get rethrown and this method will crash out as it should
				Console.Write("Could not connect to sync root, hr {0}\n", (Vanara.PInvoke.HRESULT)ex.HResult);
				throw;
			}
		}

		static void DisconnectSyncRootTransferCallbacks()
		{
			Console.Write("Shutting down\n");
			try
			{
				CfDisconnectSyncRoot(s_transferCallbackConnectionKey).ThrowIfFailed();
			}
			catch (Exception ex)
			{
				// winrt.to_hresult() will eat the exception if it is a result of winrt.check_hresult,
				// otherwise the exception will get rethrown and this method will crash out as it should
				Console.Write("Could not disconnect the sync root, hr {0}\n", (Vanara.PInvoke.HRESULT)ex.HResult);
			}
		}

		static void OnFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			FileCopierWithProgress.CopyFromServerToClient(callbackInfo, callbackParameters, ProviderFolderLocations.ServerFolder);
		}

		static void OnCancelFetchData(in CF_CALLBACK_INFO callbackInfo, in CF_CALLBACK_PARAMETERS callbackParameters)
		{
			FileCopierWithProgress.CancelCopyFromServerToClient(callbackInfo, callbackParameters);
		}
	}
}
