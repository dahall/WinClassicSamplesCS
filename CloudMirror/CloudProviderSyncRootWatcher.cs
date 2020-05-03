using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.CldApi;
using static Vanara.PInvoke.Kernel32;

namespace CloudMirror
{
    static class CloudProviderSyncRootWatcher
    {
        static DirectoryWatcher s_directoryWatcher = new DirectoryWatcher();
        static bool s_shutdownWatcher;

        public static void WatchAndWait()
        {
            // Main loop - wait for Ctrl+C or our named event to be signaled
            SetConsoleCtrlHandler(Stop, true);
            InitDirectoryWatcher();

            while (true)
            {
                try
                {
                    var task = s_directoryWatcher.ReadChangesAsync();

                    while (!task.IsCompleted)
                    {
                        Sleep(1000);

                        if (s_shutdownWatcher)
                        {
                            s_directoryWatcher.Cancel();
                            task.Wait();
                        }
                    }

                    if (s_shutdownWatcher)
                    {
                        break;
                    }
                }
                catch
                {
                    Console.Write("CloudProviderSyncRootWatcher watcher failed.\n");
                    throw;
                }
            }
        }

        public static bool Stop(CTRL_EVENT reason)
        {
            s_shutdownWatcher = true;
            return true;
        }

        static void InitDirectoryWatcher()
        {
            // Set up a Directory Watcher on the client side to handle user's changing things there
            try
            {
                s_directoryWatcher.Initalize(ProviderFolderLocations.GetClientFolder(), OnSyncRootFileChanges);
            }
            catch
            {
                Console.Write("Could not init directory watcher.\n");
                throw;
            }
        }

        static void OnSyncRootFileChanges(IEnumerable<string> changes)
        {
            foreach (var path in changes)
            {
                Console.Write("Processing change for {0}\n", path);

                var attrib = GetFileAttributes(path);
                if ((attrib & FileFlagsAndAttributes.FILE_ATTRIBUTE_DIRECTORY) == 0)
                {
                    using var placeholder = CreateFile(path, 0, FileShare.Read, null, FileMode.Open, 0);

                    var offset = 0L;
                    var length = -1L;

                    if ((attrib & FileFlagsAndAttributes.FILE_ATTRIBUTE_PINNED) != 0)
                    {
                        Console.Write("Hydrating file {0}\n", path);
                        CfHydratePlaceholder(placeholder, offset, length, CF_HYDRATE_FLAGS.CF_HYDRATE_FLAG_NONE);
                    }
                    else if ((attrib & FileFlagsAndAttributes.FILE_ATTRIBUTE_UNPINNED) != 0)
                    {
                        Console.Write("Dehydrating file {0}\n", path);
                        CfDehydratePlaceholder(placeholder, offset, length, CF_DEHYDRATE_FLAGS.CF_DEHYDRATE_FLAG_NONE);
                    }
                }
            }
        }

    }
}