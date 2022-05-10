global using System;
global using System.Globalization;
global using System.Linq;
global using System.Management;
global using System.Reflection;
global using Vanara.PInvoke;
global using Vanara.IO;

if (args.Length == 1)
{
	if (string.Equals(args[0], "GetAllAttachedVirtualDiskPhysicalPaths", StringComparison.OrdinalIgnoreCase))
	{
		var paths = VirtualDisk.GetAllAttachedVirtualDiskPaths().ToArray();
		if (paths.Length == 0)
			Console.WriteLine("There are no loopback mounted virtual disks.");
		else
			Console.WriteLine(string.Join("\r\n", paths.Select(s => $"Path = '{s}'")));
	}
	else
	{
		ShowUsage();
	}
}
else if (args.Length == 2)
{
	string virtualHardDiskPath = args[1];
	if (string.Equals(args[0], "GetVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		using var vdisk = VirtualDisk.Open(virtualHardDiskPath, true, true, true);
		Console.WriteLine($"driveType = {vdisk.ProviderSubtype}");
		Console.WriteLine($"driveFormat = {vdisk.DiskType}");
		Console.WriteLine($"physicalSize = {vdisk.PhysicalSize}");
		Console.WriteLine($"virtualSize = {vdisk.VirtualSize}");
		Console.WriteLine($"sectorSize = {vdisk.SectorSize}");
		Console.WriteLine($"blockSize = {vdisk.BlockSize}");
		Console.WriteLine($"physicalSectorSize = {vdisk.PhysicalSectorSize}");
		Console.WriteLine($"identifier = {vdisk.Identifier}");
		if (vdisk.DiskType == VirtualDisk.DeviceType.VhdSet)
		{
			Console.Write("parentPath = ");
			Console.WriteLine(string.Join("\r\n\t", vdisk.ParentPaths));
			Console.WriteLine($"parentIdentifier = {vdisk.ParentIdentifier}");
		}
		Console.WriteLine($"minInternalSize = {vdisk.SmallestSafeVirtualSize}");
		Console.WriteLine($"fragmentationPercentage = {vdisk.FragmentationPercentage}%");
	}
	else if (string.Equals(args[0], "ValidateVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		VirtualDisk.ValidateAsync(virtualHardDiskPath).Wait();
	}
	else if (string.Equals(args[0], "DetachVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		using var vdisk = VirtualDisk.Open(virtualHardDiskPath, true, false);
		vdisk.Detach();
	}
	else if (string.Equals(args[0], "CreateVirtualFloppyDisk", StringComparison.OrdinalIgnoreCase))
	{
		CallWmi("CreateVirtualFloppyDisk", ("Path", virtualHardDiskPath));
	}
	else if (string.Equals(args[0], "MergeVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		using var vdisk = VirtualDisk.Open(virtualHardDiskPath, false);
		vdisk.MergeWithParent();
	}
	else
	{
		ShowUsage();
	}
}
else if (args.Length == 3)
{
	string virtualHardDiskPath = args[1];
	if (string.Equals(args[0], "CompactVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		using var vdisk = VirtualDisk.Open(virtualHardDiskPath, false);
		VirtualDisk.CompactionMode mode = (VirtualDisk.CompactionMode)ushort.Parse(args[2]);

		if (mode == VirtualDisk.CompactionMode.Retrim)
			vdisk.Attach();
		try
		{
			vdisk.CompactAsync(mode).Wait();
		}
		finally
		{
			if (vdisk.Attached)
				vdisk.Detach();
		}
	}
	else if (string.Equals(args[0], "ResizeVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		using var vdisk = VirtualDisk.Open(virtualHardDiskPath, true, false);
		vdisk.Resize(ulong.Parse(args[2], CultureInfo.CurrentCulture));
	}
	else if (string.Equals(args[0], "SetVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
	{
		//int sectorSize;
		//string parentPath = args[2];

		//if (int.TryParse(args[2], NumberStyles.None, CultureInfo.CurrentCulture, out sectorSize))
		//{
		//	StorageSetSample.SetVirtualHardDisk(
		//		serverName,
		//		virtualHardDiskPath,
		//		null,
		//		sectorSize);
		//}
		//else
		//{
		//	StorageSetSample.SetVirtualHardDisk(
		//		serverName,
		//		virtualHardDiskPath,
		//		parentPath,
		//		0);
		//}
	}
	else
	{
		ShowUsage();
	}
}
//else if (args.Length == 4)
//{
//	if (string.Equals(args[0], "AttachVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
//	{
//		string virtualHardDiskPath = args[1];
//		string assignDriveLetter = args[2];
//		string readOnly = args[3];

//		StorageAttachSample.AttachVirtualHardDisk(
//			serverName,
//			virtualHardDiskPath,
//			assignDriveLetter,
//			readOnly);
//	}
//	else if (string.Equals(args[0], "CreateDifferencingVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
//	{
//		string virtualHardDiskPath = args[1];
//		string parentPath = args[2];
//		VirtualHardDiskType type = VirtualHardDiskType.Differencing;
//		VirtualHardDiskFormat format;

//		if (string.Equals(args[3], "vhdx", StringComparison.OrdinalIgnoreCase))
//		{
//			format = VirtualHardDiskFormat.Vhdx;
//		}
//		else
//		{
//			format = VirtualHardDiskFormat.Vhd;
//		}

//		StorageCreateSample.CreateVirtualHardDisk(
//			serverName,
//			virtualHardDiskPath,
//			parentPath,
//			type,
//			format,
//			0,
//			0,
//			0,
//			0);
//	}
//	else if (string.Equals(args[0], "ConvertVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
//	{
//		string sourcePath = args[1];
//		string destinationPath = args[2];

//		VirtualHardDiskFormat format;
//		if (string.Equals(args[3], "vhdx", StringComparison.OrdinalIgnoreCase))
//		{
//			format = VirtualHardDiskFormat.Vhdx;
//		}
//		else
//		{
//			format = VirtualHardDiskFormat.Vhd;
//		}

//		StorageConvertSample.ConvertVirtualHardDisk(
//			serverName,
//			sourcePath,
//			destinationPath,
//			format);
//	}
//	else
//	{
//		ShowUsage();
//	}
//}
//else if (args.Length == 5)
//{
//	if (string.Equals(args[0], "SetParentVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
//	{
//		string ChildPath = args[1];
//		string ParentPath = args[2];
//		string LeafPath = args[3];
//		string IgnoreIDMismatch = args[4];

//		if (string.Equals(LeafPath, "null", StringComparison.OrdinalIgnoreCase))
//		{
//			// Only valid if VHD is not online.
//			LeafPath = null;
//		}

//		StorageSetParentSample.SetParentVirtualHardDisk(
//			serverName,
//			ChildPath,
//			ParentPath,
//			LeafPath,
//			IgnoreIDMismatch);
//	}
//	else
//	{
//		ShowUsage();
//	}
//}
//else if (args.Length == 7)
//{
//	if (string.Equals(args[0], "CreateFixedVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
//	{
//		string virtualHardDiskPath = args[1];
//		string parentPath = null;
//		VirtualHardDiskType type = VirtualHardDiskType.FixedSize;
//		VirtualHardDiskFormat format;

//		if (string.Equals(args[2], "vhdx", StringComparison.OrdinalIgnoreCase))
//		{
//			format = VirtualHardDiskFormat.Vhdx;
//		}
//		else
//		{
//			format = VirtualHardDiskFormat.Vhd;
//		}

//		long fileSize = long.Parse(args[3], CultureInfo.CurrentCulture);
//		int blockSize = int.Parse(args[4], CultureInfo.CurrentCulture);
//		int logicalSectorSize = int.Parse(args[5], CultureInfo.CurrentCulture);
//		int physicalSectorSize = int.Parse(args[6], CultureInfo.CurrentCulture);

//		StorageCreateSample.CreateVirtualHardDisk(
//			serverName,
//			virtualHardDiskPath,
//			parentPath,
//			type,
//			format,
//			fileSize,
//			blockSize,
//			logicalSectorSize,
//			physicalSectorSize);
//	}
//	else if (string.Equals(args[0], "CreateDynamicVirtualHardDisk", StringComparison.OrdinalIgnoreCase))
//	{
//		string virtualHardDiskPath = args[1];
//		string parentPath = null;
//		VirtualHardDiskType type = VirtualHardDiskType.DynamicallyExpanding;
//		VirtualHardDiskFormat format;

//		if (string.Equals(args[2], "vhdx", StringComparison.OrdinalIgnoreCase))
//		{
//			format = VirtualHardDiskFormat.Vhdx;
//		}
//		else
//		{
//			format = VirtualHardDiskFormat.Vhd;
//		}

//		long fileSize = long.Parse(args[3], CultureInfo.CurrentCulture);
//		int blockSize = int.Parse(args[4], CultureInfo.CurrentCulture);
//		int logicalSectorSize = int.Parse(args[5], CultureInfo.CurrentCulture);
//		int physicalSectorSize = int.Parse(args[6], CultureInfo.CurrentCulture);

//		StorageCreateSample.CreateVirtualHardDisk(
//			serverName,
//			virtualHardDiskPath,
//			parentPath,
//			type,
//			format,
//			fileSize,
//			blockSize,
//			logicalSectorSize,
//			physicalSectorSize);
//	}
//	else
//	{
//		ShowUsage();
//	}
//}
else
{
	ShowUsage();
}

/// <summary>
/// Displays the command line usage for the program.
/// </summary>
static void ShowUsage()
{
	string moduleName = Assembly.GetExecutingAssembly().GetModules()[0].Name;

	Console.WriteLine("\nUsage:\t{0} <SampleName> <Arguments>\n", moduleName);

	Console.WriteLine("Supported SampleNames and Arguments:\n");
	Console.WriteLine("   GetVirtualHardDisk <server> <path>");
	Console.WriteLine("   SetVirtualHardDisk <server> <path> [<parent> | <physical sector size>]");
	Console.WriteLine("   ValidateVirtualHardDisk <server> <path>");
	Console.WriteLine("   CreateFixedVirtualHardDisk <server> <path> <file size> <block size> <logical sector size> <physical sector size>");
	Console.WriteLine("   CreateDynamicVirtualHardDisk <server> <path> <file size> <block size> <logical sector size> <physical sector size>");
	Console.WriteLine("   CreateDifferencingVirtualHardDisk <server> <path> <parent path>");
	Console.WriteLine("   CreateVirtualFloppyDisk <server> <path>");
	Console.WriteLine("   AttachVirtualHardDisk <server> <path> <assign drive letter> <read only>");
	Console.WriteLine("   DetachVirtualHardDisk <server> <path>");
	Console.WriteLine("   SetParentVirtualHardDisk <child> <parent> <leaf> <ignore id mismatch>");
	Console.WriteLine("   ConvertVirtualHardDisk <server> <source path> <destination path> <format>");
	Console.WriteLine("   MergeVirtualHardDisk <server> <source path> <destination path>");
	Console.WriteLine("   CompactVirtualHardDisk <server> <path> <mode>");
	Console.WriteLine("   ResizeVirtualHardDisk <server> <path> <file size>");
	Console.WriteLine("\n");

	Console.WriteLine("Examples:\n");
	Console.WriteLine("   {0} GetVirtualHardDisk . c:\\fixed.vhd", moduleName);
	Console.WriteLine("   {0} SetVirtualHardDisk . c:\\diff.vhdx c:\\dynamic.vhdx", moduleName);
	Console.WriteLine("   {0} SetVirtualHardDisk . c:\\diff.vhdx 512", moduleName);
	Console.WriteLine("   {0} ValidateVirtualHardDisk . c:\\fixed.vhd", moduleName);
	Console.WriteLine("   {0} CreateFixedVirtualHardDisk . c:\\fixed.vhd vhd 1073741824 0 0 0", moduleName);
	Console.WriteLine("   {0} CreateDynamicVirtualHardDisk . c:\\dynamic.vhd vhd 1073741824 0 0 0", moduleName);
	Console.WriteLine("   {0} CreateDifferencingVirtualHardDisk . c:\\diff.vhd c:\\dynamic.vhd vhd", moduleName);
	Console.WriteLine("   {0} CreateVirtualFloppyDisk . c:\\floppy.vfd", moduleName);
	Console.WriteLine("   {0} AttachVirtualHardDisk . c:\\fixed.vhd true true", moduleName);
	Console.WriteLine("   {0} DetachVirtualHardDisk . c:\\fixed.vhd", moduleName);
	Console.WriteLine("   {0} SetParentVirtualHardDisk . c:\\diff.vhd c:\\fixed.vhd null false", moduleName);
	Console.WriteLine("   {0} ConvertVirtualHardDisk . c:\\dynamic.vhd c:\\fixed.vhd vhd", moduleName);
	Console.WriteLine("   {0} MergeVirtualHardDisk . c:\\diff.vhd c:\\dynamic.vhd", moduleName);
	Console.WriteLine("   {0} CompactVirtualHardDisk . c:\\dynamic.vhd 0", moduleName);
	Console.WriteLine("   {0} ResizeVirtualHardDisk . c:\\dynamic.vhd 2147483648", moduleName);
	Console.WriteLine("\n");
	Console.WriteLine("\n");
}

/// <summary>Gets the image management service.</summary>
/// <param name="scope">The ManagementScope to use to connect to WMI.</param>
/// <returns>The image management object.</returns>
static ManagementObject GetImageManagementService(ManagementScope scope)
{
	using ManagementClass imageManagementServiceClass = new("Msvm_ImageManagementService") { Scope = scope };
	return imageManagementServiceClass.GetInstances().Cast<ManagementObject>().FirstOrDefault();
}

static void CallWmi(string method, params (string, object)[] values)
{
	ManagementScope scope = new(@"\\.\root\virtualization\v2");
	using var imgMgmtSvc = GetImageManagementService(scope);
	using var inParams = imgMgmtSvc.GetMethodParameters(method);
	foreach (var kv in values)
		inParams[kv.Item1] = kv.Item2;
	using var outParams = imgMgmtSvc.InvokeMethod(method, inParams, null);
	Utils.Wmi.ValidateOutput(outParams, scope);
}