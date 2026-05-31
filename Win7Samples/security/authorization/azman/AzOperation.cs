/****************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzOperation.cpp

Abstract:

    Routines performing the migration for the CAzOperation object

 History:

****************************************************************************/
namespace AzMigrate;

class CAzOperation(IAzOperation pNative, bool pisNew) : CAzBase<IAzOperation>(pNative, pisNew)
{
    /*++

    Routine description:

        This method copies properties from the source operation to *this* 
        operation

    Arguments: srcOp - Source Operation

    Return Value:

        Returns success, appropriate failure value 

    --*/
    public HRESULT Copy(CAzOperation srcOp) {

        CAzLogging.Entering("Copy");

        HRESULT hr = Catch(() => srcOp.m_native.ApplicationData, out var data);

        CAzLogging.Log(hr, "Getting Application Data for Operation", Name);

        hr = Catch(() => m_native.ApplicationData = data!);

        CAzLogging.Log(hr, "Setting Application Data for Operation", Name);

        hr = Catch(() => srcOp.m_native.OperationID, out var lopID);

        CAzLogging.Log(hr, "Getting OperationID for Operation", Name);

        hr = Catch(() => m_native.OperationID = lopID);

        CAzLogging.Log(hr, "Setting Operation ID for Operation", Name);

        hr = Catch(() => srcOp.m_native.Description, out var description);

        CAzLogging.Log(hr, "Getting Description for Operation", Name);

        hr = Catch(() => m_native.Description = description!);

        CAzLogging.Log(hr, "Setting Description for Operation", Name);

        hr = Catch(() => m_native.Submit());

        CAzLogging.Log(hr, "Submitting changes for Operation", Name);

        CAzLogging.Exiting("Copy");

        return hr;
    }
}