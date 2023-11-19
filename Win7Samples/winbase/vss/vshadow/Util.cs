using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

namespace vshadow;

internal static class Util
{
	// Get the displayable root path for the given volume name
	public static string GetDisplayNameForVolume(string volumeName)
	{
		if (GetVolumePathNamesForVolumeName(volumeName, null, 0, out var dwRequired) || dwRequired <= 2)
			return "";

		Win32Error.ThrowLastErrorUnless(Win32Error.ERROR_MORE_DATA);

		using var mem = new SafeCoTaskMemHandle(dwRequired);
		Win32Error.ThrowLastErrorIfFalse(GetVolumePathNamesForVolumeName(volumeName, mem, mem.Size, out dwRequired));

		// compute the smallest mount point by enumerating the returned MULTI_SZ
		return mem.DangerousGetHandle().ToStringEnum(allocatedBytes: dwRequired).Aggregate((s, best) => s.Length < best.Length ? s : best);
	}
}