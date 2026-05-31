/****************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzScope.cpp

Abstract:

    Routines performing the migration for the CAzScope objects

 History:

****************************************************************************/
namespace AzMigrate;

class CAzScope(IAzScope pNative, bool pisNew) : CAzBase<IAzScope>(pNative, pisNew)
{
    /*++

    Routine description:

    This method copies properties from the source scope ref to ref this 
    roscope

    Arguments: srcScope - Source scope

    Return Value:

    Returns success, appropriate failure value 

    --*/

    public HRESULT Copy(CAzScope srcScope) {

        CAzLogging.Entering("CAzScope.Copy");

        HRESULT hr = Catch(() => srcScope.m_native.Description, out var bstrData);

        CAzLogging.Log(hr, "Getting Description for scope", Name);

        if (hr.Succeeded) {

            hr = Catch(() => m_native.Description = bstrData!);

            CAzLogging.Log(hr, "Setting Description for scope", Name);
        }

        hr = Catch(() => srcScope.m_native.ApplicationData, out bstrData);

        CAzLogging.Log(hr, "Getting ApplicationData for scope", Name);


        if (hr.Succeeded) {

            hr = Catch(() => m_native.ApplicationData = bstrData!);

            CAzLogging.Log(hr, "Setting ApplicationData for scope", Name);
        }

        if (!CAzGlobalOptions.m_bIgnorePolicyAdmins) {

            hr = Catch(() => srcScope.m_native.PolicyAdministrators, out var cVVar);

            CAzLogging.Log(hr, "Getting PolicyAdministrators for scope", Name);

            if (hr.Succeeded) {

                hr = InitializeUsingSafeArray(cVVar, m_native.AddPolicyAdministrator);

                CAzLogging.Log(hr, "Setting PolicyAdministrators for scope", Name);
            }

            hr = Catch(() => srcScope.m_native.PolicyReaders, out cVVar);

            CAzLogging.Log(hr, "Getting PolicyReaders for scope", Name);

            if (hr.Succeeded) {

                hr = InitializeUsingSafeArray(cVVar, m_native.AddPolicyReader);

                CAzLogging.Log(hr, "Setting PolicyReaders for scope", Name);
            }

        }

        hr = Catch(() => m_native.Submit());

        CAzLogging.Log(hr, "Submitting property changes for scope", Name);

        if (hr.Failed)
            goto lError1;

        hr = CAzHelper.CreateAppGroups(srcScope.m_native, m_native);

        CAzLogging.Log(hr, "Creating App Groups for scope", Name);

        hr = CAzHelper.CreateTasks(srcScope.m_native, m_native);

        CAzLogging.Log(hr, "Creating Tasks for scope", Name);

        hr = CAzHelper.CreateRoles(srcScope.m_native, m_native);

        CAzLogging.Log(hr, "Creating Roles for scope", Name);

        hr = Catch(() => m_native.Submit());

        CAzLogging.Log(hr, "Submitting child object addition changes for scope", Name);

lError1:
        CAzLogging.Exiting("CAzScope.Copy");

        return hr;
    }
}