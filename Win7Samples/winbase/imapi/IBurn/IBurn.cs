/*-----------------------------------------------------------------------*
 * This file is part of the Microsoft IMAPIv2 Code Samples.              *
 *                                                                       *
 * Copyright (C) Microsoft Corporation.  All rights reserved.            *
 *                                                                       *
 * This source code is intended only as a supplement to Microsoft IMAPI2 *
 * help and/or on-line documentation.  See these other materials for     *
 * detailed information regarding Microsoft code samples.                *
 *                                                                       *
 * THIS CODE AND INFORMATION ARE PROVIDED AS IS WITHOUT WARRANTY OF ANY  *
 * KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE   *
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR *
 * PURPOSE.                                                              *
 *-----------------------------------------------------------------------*/

using System;
using Vanara.Storage;
using static Vanara.PInvoke.IMAPI;

OpticalStorageWriteOperation op = new()
{
	Device = OpticalStorageManager.DefaultDevice,
};
op.WriteProgress += Device_WriteProgress;
OpticalStorageFileSystemImage image = new(args[0], FsiFileSystems.FsiFileSystemJoliet | FsiFileSystems.FsiFileSystemISO9660, null, null)
{
	FreeMediaBlocks = op.FreeSectorsOnMedia,
	MultisessionInterfaces = op.MultisessionInterfaces,
};
op.Data = image.GetImageStream();
op.Execute();

static void Device_WriteProgress(object sender, OpticalStorageWriteEventArgs progress)
{
	string timeStatus = $"Time: {progress.ElapsedTime} / {progress.TotalTime} ({progress.ElapsedTime / progress.TotalTime})";

	switch (progress.CurrentAction)
	{
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_VALIDATING_MEDIA:
			{
				Console.Write("Validating media. ");
			}
			break;
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_FORMATTING_MEDIA:
			{
				Console.Write("Formatting media. ");
			}
			break;
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_INITIALIZING_HARDWARE:
			{
				Console.Write("Initializing Hardware. ");
			}
			break;
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_CALIBRATING_POWER:
			{
				Console.Write("Calibrating Power (OPC). ");
			}
			break;
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_WRITING_DATA:
			{
				int totalSectors = progress.SectorCount;
				int writtenSectors = progress.LastWrittenLba - progress.StartLba;
				int percentDone = writtenSectors / totalSectors;
				Console.Write("Progress: {0} - ", percentDone);
			}
			break;
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_FINALIZATION:
			{
				Console.Write("Finishing the writing. ");
			}
			break;
		case IMAPI_FORMAT2_DATA_WRITE_ACTION.IMAPI_FORMAT2_DATA_WRITE_ACTION_COMPLETED:
			{
				Console.Write("Completed the burn.");
			}
			break;
		default:
			{
				Console.Write("Error!!!! Unknown Action: 0x{0:X}", progress.CurrentAction);
			}
			break;
	}
	Console.WriteLine(timeStatus);
}