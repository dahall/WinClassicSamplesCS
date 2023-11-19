using Microsoft.Win32;
using System.Reflection;
using Vanara.PInvoke;

namespace ProjectedFileSystem;

// Stores RegEntry items for the entries within a registry key, separated into lists of subkeys and values.
internal struct RegEntries
{
	public string[] SubKeys;
	public string[] Values;
}

internal static class RegOps
{
	public static readonly Dictionary<string, RegistryKey> _regRootKeyMap = new()
	{
		{ "HKEY_CLASSES_ROOT", Registry.ClassesRoot },
		{ "HKEY_CURRENT_USER", Registry.CurrentUser },
		{ "HKEY_LOCAL_MACHINE", Registry.LocalMachine },
		{ "HKEY_USERS", Registry.Users },
		{ "HKEY_CURRENT_CONFIG", Registry.CurrentConfig }
	};

	// Returns true if the given path corresponds to a key that exists in the registry.
	public static bool DoesKeyExist(string path)
	{
		OpenKeyByPath(path, out var subkey);
		if (subkey is null)
		{
			Console.Write("{0}: key [{1}] doesn't exist \n", MethodBase.GetCurrentMethod()!.Name, path);
			return false;
		}

		subkey.Dispose();
		return true;
	}

	// Returns true if the given path corresponds to a value that exists in the registry, and tells you how big it is.
	public static bool DoesValueExist(string path)
	{
		var pos = path.LastIndexOf('\\');
		if (pos == -1)
		{
			// There are no '\' characters in the path. The only paths with no '\' are the predefined keys, so this can't be a value.
			return false;
		}
		else
		{
			OpenKeyByPath(path.Substring(0, pos), out var subkey);
			if (subkey is null)
			{
				Console.Write("{0}: value [{1}] doesn't exist \n", MethodBase.GetCurrentMethod()!.Name, path.Substring(0, pos));
				return false;
			}

			var valPathStr = path.Substring(pos + 1);
			var res = subkey.GetValue(valPathStr);
			if (res is null)
			{
				Console.Write("{0}: Could not get value [{1}] at key [{2}]: {3}\n", MethodBase.GetCurrentMethod()!.Name, valPathStr, path.Substring(0, pos), res);
				return false;
			}

			subkey.Dispose();
		}

		return true;
	}

	// Returns a RegEntries struct populated with the subkeys and values in the registry key whose path is specified.
	public static HRESULT EnumerateKey(string path, out RegEntries entries)
	{
		HRESULT hr = HRESULT.S_OK;

		if (PathUtils.IsVirtualizationRoot(path))
		{
			entries = new RegEntries { SubKeys = _regRootKeyMap.Keys.ToArray(), Values = new string[0] };
		}
		else
		{
			// The path is somewhere below the root, so try opening the key.
			hr = OpenKeyByPath(path, out var subKey);

			// If the path corresponds to a registry key, enumerate it.
			if (subKey != null)
			{
				hr = EnumerateKey(subKey, out entries);
				subKey.Dispose();
			}
			else
				entries = default;
		}
		return hr;
	}

	// Reads a value from the registry.
	public static bool ReadValue(string path, out object? data)
	{
		var lastPos = path.LastIndexOf('\\');
		if (lastPos == -1)
		{
			// There are no '\' characters in the path. The only paths with no '\' are the predefined keys, so this can't be a value.
			data = null;
			return false;
		}

		// Split the path into <key>\<value>
		var keyPath = path.Substring(0, lastPos);
		var valName = path.Substring(lastPos + 1);

		// Open the key path to get a HKEY handle to it.
		OpenKeyByPath(keyPath, out var subkey);

		// Read the value's content from the registry.
		data = subkey?.GetValue(valName);
		if (data is null)
		{
			Console.Write("{0}: RegQueryValueEx [{1}]\n", MethodBase.GetCurrentMethod()!.Name, valName);
			subkey?.Dispose();
			return false;
		}

		subkey?.Dispose();
		return true;
	}

	// Returns a RegEntries struct populated with the subkeys and values in the specified registry key.
	private static HRESULT EnumerateKey(RegistryKey hKey, out RegEntries entries)
	{
		entries = new RegEntries { SubKeys = hKey.GetSubKeyNames().ToArray(), Values = hKey.GetValueNames().ToArray() };
		return HRESULT.S_OK;
	}

	// Gets the HKEY for a registry key given the path, if it exists.
	private static HRESULT OpenKeyByPath(string path, out RegistryKey? hKey)
	{
		HRESULT hr = HRESULT.S_OK;

		hKey = default;

		var pos = path.IndexOf('\\');
		if (pos == -1)
		{
			if (!_regRootKeyMap.TryGetValue(path, out hKey))
			{
				Console.Write("{0}: root key [{1}] doesn't exist\n", MethodBase.GetCurrentMethod()!.Name, path);
				hr = (HRESULT)(Win32Error)Win32Error.ERROR_PATH_NOT_FOUND;
			}
		}
		else
		{
			// The first component of the path should be a predefined key, so get its HKEY value and try opening the rest of the key
			// relative to it.
			var rootKeyStr = path.Substring(0, pos);
			var rootKey = _regRootKeyMap[rootKeyStr];
			try
			{
				hKey = rootKey.OpenSubKey(path.Substring(pos + 1), false);
			}
			catch (Exception ex)
			{
				Console.Write("{0}: failed to open key [{1}]: {2}", MethodBase.GetCurrentMethod()!.Name, path, ex.HResult);
			}
		}

		return hr;
	}
}