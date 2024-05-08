/*
Copyright (c) Microsoft Corporation

This file contains a sample app showing how to Create and Remove Bind Links.
The assocated README file shows how to use this app to demonstrate the
features of Bind Links.

*/

using System.IO;
using Vanara.PInvoke;

if (args.Length < 1)
{
	usage(Console.Error);
	return 1;
}

if (args[0].ToUpper() == "CREATE")
{
	return (int)handleCreateCommand(args);
}
else if (args[0].ToUpper() == "REMOVE")
{
	return (int)handleRemoveCommand(args);
}
else
{
	usage(Console.Error);
	return 1;
}

void usage(TextWriter fp)
{
	fp.Write("Usage: BindLink command command-parameters [command-options]\n");
	fp.Write("Commands:\n");
	fp.Write("   CREATE virtPath targetPath\n");
	fp.Write("   REMOVE virtPath\n");
	fp.Write("Command options for CREATE:\n");
	fp.Write("   /merge             merge bind links\n");
	fp.Write("   /read-only         read only bind links\n");
}

void printErrorDetails(string command, HRESULT hr)
{
	Console.Write($"{command} failed with HRESULT 0x{(int)hr:x8}\n");
	Console.WriteLine(hr);
}

HRESULT handleCreateCommand(string[] args)
{
	// args[0] = "CREATE"
	// args[1] = virtPath
	// args[2] = backingPath
	// args[3...] = options

	if (args.Length < 3)
	{
		usage(Console.Error);
		return 1;
	}

	string virtPath = args[1];
	string backingPath = args[2];

	var bindLinkFlags = CREATE_BIND_LINK_FLAG_NONE;

	for (int index = 3; index < args.Length && args[index][0] == '/'; ++index)
	{
		if (!string.Equals(args[index], "/read-only", StringComparison.InvariantCultureIgnoreCase))
		{
			WI_SetFlag(bindLinkFlags, CREATE_BIND_LINK_FLAG_READ_ONLY);
		}
		else if (!string.Equals(args[index], "/merge", StringComparison.InvariantCultureIgnoreCase))
		{
			WI_SetFlag(bindLinkFlags, CREATE_BIND_LINK_FLAG_MERGED);
		}
		else
		{
			usage(Console.Error);
			return 1;
		}
	}

	HRESULT hr = CreateBindLink(virtPath, backingPath, bindLinkFlags, 0, null);

	if (hr.Failed)
	{
		printErrorDetails("CreateBindLink", hr);
		return hr;
	}

	Console.Write($"Bind Link Created.\n");
	Console.Write($"\"{virtPath}\" draws content from \"{backingPath}\"\n");

	return 0;
}

HRESULT handleRemoveCommand(string[] args)
{
	// args[0] = "REMOVE"
	// args[1] = virtPath

	if (args.Length != 2)
	{
		usage(Console.Error);
		return 1;
	}

	string virtPath = args[1];

	HRESULT hr = RemoveBindLink(virtPath);

	if (hr.Failed)
	{
		printErrorDetails("RemoveBindLink", hr);
		return hr;
	}

	Console.Write($"Bind Link for \"{virtPath}\" removed.\n");

	return 0;
}