using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace CloudMirror
{
	internal class DirectoryWatcher
	{
		private static readonly SizeT c_bufferSize = Marshal.SizeOf<FILE_NOTIFY_INFORMATION>() * 100;

		private Action<IEnumerable<string>> _callback;
		private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
		private SafeHFILE _dir;
		private string _path;

		public void Cancel()
		{
			Console.Write("Canceling watcher\n");
			_cancellationTokenSource.Cancel();
		}

		public void Initalize(string path, Action<IEnumerable<string>> callback)
		{
			_path = path;
			_callback = callback;

			_dir = CreateFile(path, Kernel32.FileAccess.FILE_LIST_DIRECTORY, FileShare.ReadWrite | FileShare.Delete, default,
				FileMode.Open, FileFlagsAndAttributes.FILE_FLAG_BACKUP_SEMANTICS | FileFlagsAndAttributes.FILE_FLAG_OVERLAPPED);
			if (_dir.IsInvalid)
			{
				Win32Error.ThrowLastError();
			}
		}

		public async Task ReadChangesAsync()
		{
			var token = _cancellationTokenSource.Token;
			await Task.Run(() =>
			{
				unsafe
				{
					using var _notify = new SafeHGlobalHandle(c_bufferSize);
					NativeOverlapped _overlapped = default;
					while (true)
					{
						uint returned = 0;
						if (!ReadDirectoryChanges(_dir, _notify, c_bufferSize, true, FILE_NOTIFY_CHANGE.FILE_NOTIFY_CHANGE_ATTRIBUTES, &returned, &_overlapped, null))
							throw Win32Error.GetLastError().GetException();

						if (GetOverlappedResultEx(_dir, &_overlapped, out var transferred, 1000, false))
						{
							var result = new List<string>();
							var ptrOffset = 0L;
							foreach (var next in _notify.DangerousGetHandle().LinkedListToIEnum<FILE_NOTIFY_INFORMATION>(fn => fn.NextEntryOffset == 0 ? IntPtr.Zero : _notify.DangerousGetHandle().Offset(ptrOffset += fn.NextEntryOffset)))
							{
								result.Add(Path.Combine(_path, next.FileName));
							}
							_callback?.Invoke(result);
						}
						else if (Win32Error.GetLastError() != Win32Error.WAIT_TIMEOUT)
						{
							throw Win32Error.GetLastError().GetException();
						}
						else if (token.IsCancellationRequested)
						{
							Console.Write("watcher cancel received\n");
							return;
						}
					}
				}
			}, token);
		}
	}
}