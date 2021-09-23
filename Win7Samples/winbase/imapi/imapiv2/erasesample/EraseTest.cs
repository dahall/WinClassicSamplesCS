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
using System.ComponentModel;
using Vanara.Storage;

OpticalStorageEraseMediaOperation op = new();
op.EraseProgress += Device_EraseProgress;
op.Execute();

static void Device_EraseProgress(object sender, ProgressChangedEventArgs e)
{
	// each block is 2%
	// ----=----1----=----2----=----3----=----4----=----5----=----6----=----7----=----8
	// ±.....................

	for (var i = 1; i < 100; i += 2)
	{
		if (i < e.ProgressPercentage)
			Console.Write((char)178);
		else if (i == e.ProgressPercentage)
			Console.Write((char)177);
		else
			Console.Write((char)176);
	}
	Console.Write(" {0}%", e.ProgressPercentage);
}