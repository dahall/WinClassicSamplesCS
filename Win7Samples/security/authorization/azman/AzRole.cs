/****************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzRole.cpp

Abstract:

	Routines performing the migration for the AzRole object

 History:

****************************************************************************/
namespace AzMigrate;

class CAzRole(IAzRole pNative, bool pisNew) : CAzBase<IAzRole>(pNative, pisNew)
{
	/*++

	Routine description:

		This method copies properties from the source role to *this* 
		role

	Arguments: srcRole - Source role

	Return Value:

		Returns success, appropriate failure value 

	--*/
	private HRESULT CopyUserData(CAzRole srcRole) {

		CAzLogging.Entering("CAzRole.CopyUserData");

		HRESULT hr = Catch(() => srcRole.m_native.Operations, out var @var);

		CAzLogging.Log(hr, "Getting operations for role", Name);

		if (hr.Succeeded) {

			hr = InitializeUsingSafeArray(var, m_native.AddOperation);

			CAzLogging.Log(hr, "Adding Operations", Name);
		}

		hr = Catch(() => srcRole.m_native.AppMembers, out @var);

		CAzLogging.Log(hr, "Getting app members for role", Name);

		if (hr.Succeeded) {

			hr = InitializeUsingSafeArray(var, m_native.AddAppMember);

			CAzLogging.Log(hr, "Setting app members for role", Name);
		}

		if (!CAzGlobalOptions.m_bIgnoreMembers) {

			hr = Catch(() => srcRole.m_native.Members, out @var);

			CAzLogging.Log(hr, "Getting Members for role", Name);

			if (hr.Succeeded) {

				hr = InitializeUsingSafeArray(var, m_native.AddMember);

				CAzLogging.Log(hr, "Setting Members for role", Name);
			}
		}

		hr = Catch(() => srcRole.m_native.Tasks, out @var);

		CAzLogging.Log(hr, "Getting Tasks for role", Name);

		if (hr.Succeeded) {

			hr = InitializeUsingSafeArray(var, m_native.AddTask);

			CAzLogging.Log(hr, "Setting Tasks for role", Name);

		}

		CAzLogging.Exiting("CAzRole.CopyUserData");

		return hr;
	}

	/*++

	Routine description:

	This method copies properties from the source role ref to ref this 
	role

	Arguments: srcRole - Source role

	Return Value:

	Returns success, appropriate failure value 

	--*/

	public HRESULT Copy(CAzRole srcRole) {

		CAzLogging.Entering("CAzRole.Copy");

		HRESULT hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting for role", Name);

		if (hr.Failed)
			goto lError1;

		hr = Catch(() => srcRole.m_native.Description, out var bstrData);

		CAzLogging.Log(hr, "Getting Description for role", Name);

		if (hr.Succeeded) {

			hr = Catch(() => m_native.Description = bstrData!);

			CAzLogging.Log(hr, "Setting Description for role", Name);
		}

		hr = Catch(() => srcRole.m_native.ApplicationData, out bstrData);

		CAzLogging.Log(hr, "Getting ApplicationData for role", Name);

		if (hr.Succeeded) {

			hr = Catch(() => m_native.ApplicationData = bstrData!);

			CAzLogging.Log(hr, "Setting ApplicationData for role", Name);

		}

		hr = CopyUserData(srcRole);

		CAzLogging.Log(hr, "CopyUserData for role", Name);

		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting for role", Name);

lError1:
		CAzLogging.Exiting("CAzRole.Copy");

		return hr;
	}
}