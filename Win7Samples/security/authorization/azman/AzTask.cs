/****************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzTask.cpp

Abstract:

	Routines performing the migration for the CAzTask object

 History:

****************************************************************************/

using Vanara.PInvoke;
using static Vanara.PInvoke.AzRoles;

namespace AzMigrate;

internal class CAzTask(IAzTask pNative, bool pisNew) : CAzBase<IAzTask>(pNative, pisNew)
{
	private static readonly AZ_PROP_CONSTANTS[] m_props =
	[
		AZ_PROP_CONSTANTS.AZ_PROP_APPLICATION_DATA,
		AZ_PROP_CONSTANTS.AZ_PROP_TASK_BIZRULE_LANGUAGE,
		AZ_PROP_CONSTANTS.AZ_PROP_TASK_BIZRULE,
		AZ_PROP_CONSTANTS.AZ_PROP_DESCRIPTION,
		AZ_PROP_CONSTANTS.AZ_PROP_TASK_IS_ROLE_DEFINITION
	];

	/*++

	Routine description:

	This method copies properties from the source task ref to ref this
	task

	Arguments: srcTask - Source Task

	Return Value:

	Returns success, appropriate failure value

	--*/
	public HRESULT Copy(CAzTask srcTask)
	{
		CAzLogging.Exiting("Copy");

		HRESULT hr;
		for (long i = 0; i < m_props.Length; i++)
		{
			hr = Catch(() => srcTask.m_native.GetProperty(m_props[i]), out object? cVVari);

			CAzLogging.Log(hr, "Getting IAzTask Property ID:", srcTask.Name, m_props[i]);

			if (hr.Succeeded)
			{
				hr = Catch(() => m_native.SetProperty(m_props[i], cVVari!));

				CAzLogging.Log(hr, "Setting IAzTask Property ID:", Name, m_props[i]);
			}
		}

		hr = Catch(() => srcTask.m_native.BizRuleImportedPath, out string? bstrData);

		CAzLogging.Log(hr, "Getting BizRuleImportedPath for Task", Name);

		if (hr.Succeeded)
		{
			hr = Catch(() => m_native.BizRuleImportedPath = bstrData!);

			CAzLogging.Log(hr, "Setting BizRuleImportedPath for App Group", Name);
		}

		hr = Catch(() => srcTask.m_native.Operations, out object? cVVar);

		CAzLogging.Log(hr, "Getting Operations for Task", Name);

		if (hr.Failed)
			goto lError1;

		hr = InitializeUsingSafeArray(cVVar, m_native.AddOperation);

		CAzLogging.Log(hr, "Setting Operations for Task", Name);

		if (hr.Failed)
			goto lError1;

		if (CAzGlobalOptions.m_bVersionTwo)
		{
			hr = CopyVersion2Constructs(srcTask);
			CAzLogging.Log(hr, "Copying bizrule properties for Task", srcTask.Name);
		}

		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting Task ", Name);

lError1:
		CAzLogging.Exiting("Copy");

		return hr;
	}

	/*++

	Routine description:

	This method copies links from the source task ref to ref this
	task

	Arguments: srcTask - Source Task

	Return Value:

	Returns success, appropriate failure value

	--*/
	public HRESULT CopyLinks(CAzTask srcTask)
	{
		CAzLogging.Entering("CopyLinks");

		HRESULT hr = Catch(() => srcTask.m_native.Tasks, out object? cVVar);

		CAzLogging.Log(hr, "Getting Tasks for Task", Name);

		if (hr.Succeeded)
		{
			hr = InitializeUsingSafeArray(cVVar, srcTask.m_native.AddTask);

			CAzLogging.Log(hr, "Setting Task links for role", Name);
		}

		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting Task links", Name);

		CAzLogging.Exiting("CopyLinks");

		return hr;
	}

	/*++

	Routine description:

	This method copies properties from the source task ref to ref this task
	which are specific to version 1.2

	Arguments: Source Task

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within
	this method

	--*/
	protected HRESULT CopyVersion2Constructs(CAzTask srcTask)
	{
		AZ_PROP_CONSTANTS[] rgProperties = [AZ_PROP_CONSTANTS.AZ_PROP_TASK_BIZRULE, AZ_PROP_CONSTANTS.AZ_PROP_TASK_BIZRULE_LANGUAGE, AZ_PROP_CONSTANTS.AZ_PROP_TASK_BIZRULE_IMPORTED_PATH];

		HRESULT hr = HRESULT.S_OK;

		if (!CAzGlobalOptions.m_bVersionTwo)
			goto lDone;

		for (long i = 0; i < 3; i++)
		{
			hr = Catch(() => srcTask.m_native.GetProperty(rgProperties[i]), out object? cVVar);

			CAzLogging.Log(hr, "Getting IAzTask Property ID:", srcTask.Name, rgProperties[i]);

			if (hr.Succeeded && cVVar is string s && s.Length != 0)
			{
				hr = Catch(() => m_native.SetProperty(rgProperties[i], cVVar));

				CAzLogging.Log(hr, "Setting IAzTask Property ID:", Name, rgProperties[i]);
			}
		}
lDone:
		return hr;
	}
}