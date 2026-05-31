/* **************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzAppGroup.cpp

Abstract:

	Routines performing the migration for the AppGroup object

 History:

 ***************************************************************************/

using Vanara.PInvoke;
using static Vanara.PInvoke.AzRoles;

namespace AzMigrate;

internal class CAzAppGroup(IAzApplicationGroup pNative, bool pisNew) : CAzBase<IAzApplicationGroup>(pNative, pisNew)
{
	/*++

	Routine description:

	This method copies properties from the source app group ref to ref this app
	Group which are specific to version 1.2

	Arguments: Source App Group

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within
	this method

	--*/
	public HRESULT Copy(CAzAppGroup srcAppGroup)
	{
		CAzLogging.Entering("Copy");

		HRESULT hr = Catch(() => srcAppGroup.m_native.Description, out string? bstrData);

		CAzLogging.Log(hr, "Getting Description for App Group", srcAppGroup.Name);

		if (hr.Succeeded && bstrData is not null)
		{
			hr = Catch(() => m_native.Description = bstrData);

			CAzLogging.Log(hr, "Setting Description for App Group", Name);
		}

		hr = Catch(() => srcAppGroup.m_native.Type, out var lType);

		CAzLogging.Log(hr, "Getting Type for App Group", srcAppGroup.Name);

		if (hr.Succeeded)
		{
			hr = Catch(() => m_native.Type = lType);

			CAzLogging.Log(hr, "Setting Type for App Group", Name);
		}

		hr = Catch(() => srcAppGroup.m_native.LdapQuery, out bstrData);

		CAzLogging.Log(hr, "Getting LDAPQuery for App Group", srcAppGroup.Name);

		// THe length check is reqd below as adding an empty string LDAP Query returns an error

		if (hr.Succeeded && bstrData is not null)
		{
			hr = Catch(() => m_native.LdapQuery = bstrData);

			CAzLogging.Log(hr, "Setting LDAPQuery for App Group", Name);
		}

		if (CAzGlobalOptions.m_bVersionTwo)
		{
			hr = CopyVersion2Constructs(srcAppGroup);

			CAzLogging.Log(hr, "Copying bizrule properties for App Group", srcAppGroup.Name);
		}

		if (!CAzGlobalOptions.m_bIgnoreMembers)
		{
			hr = Catch(() => srcAppGroup.m_native.Members, out object? cVVar);

			CAzLogging.Log(hr, "Getting Members for App Group", srcAppGroup.Name);

			if (hr.Succeeded && cVVar is not null)
			{
				hr = InitializeUsingSafeArray(cVVar, m_native.AddMember);

				CAzLogging.Log(hr, "Setting Members for App Group", Name);
			}

			hr = Catch(() => srcAppGroup.m_native.NonMembers, out cVVar);

			CAzLogging.Log(hr, "Getting Non Members for App Group", srcAppGroup.Name);

			if (hr.Succeeded && cVVar is not null)
			{
				hr = InitializeUsingSafeArray(cVVar, m_native.AddNonMember);

				CAzLogging.Log(hr, "Setting Non Members for App Group", Name);
			}
		}

		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting App Group", Name);

		CAzLogging.Exiting("Copy");

		return hr;
	}

	/*++

	Routine description:

	This method copies links found in the source app group ref to ref this app
	Group

	Arguments: Source App Group

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within
	this method

	--*/
	public HRESULT CopyLinks(CAzAppGroup srcAppGroup)
	{
		CAzLogging.Entering("CopyLinks");

		HRESULT hr = Catch(() => srcAppGroup.m_native.AppMembers, out object? cVVar);

		CAzLogging.Log(hr, "Getting App Members for App Group", srcAppGroup.Name);

		if (hr.Succeeded)
		{
			hr = InitializeUsingSafeArray(cVVar, m_native.AddAppMember);

			CAzLogging.Log(hr, "Setting App Members for App Group", Name);
		}

		hr = Catch(() => srcAppGroup.m_native.AppNonMembers, out cVVar);

		CAzLogging.Log(hr, "Getting App Non Members for App Group", srcAppGroup.Name);

		if (hr.Succeeded)
		{
			hr = InitializeUsingSafeArray(cVVar, m_native.AddAppNonMember);

			CAzLogging.Log(hr, "Setting App Non Members for App Group", Name);
		}

		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting for App Group", Name);

		CAzLogging.Exiting("CopyLinks");

		return hr;
	}

	/*++

	Routine description:

	This method copies properties from the source app group ref to ref this app
	Group

	Arguments: Source App Group

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within
	this method

	--*/
	protected HRESULT CopyVersion2Constructs(CAzAppGroup srcAppGroup)
	{
		HRESULT hr = HRESULT.S_OK;

		if (!CAzGlobalOptions.m_bVersionTwo)
			goto lDone;

		AZ_PROP_CONSTANTS[] rgProperties = [AZ_PROP_CONSTANTS.AZ_PROP_GROUP_BIZRULE, AZ_PROP_CONSTANTS.AZ_PROP_GROUP_BIZRULE_LANGUAGE, AZ_PROP_CONSTANTS.AZ_PROP_GROUP_BIZRULE_IMPORTED_PATH];
		foreach (var p in rgProperties)
		{
			hr = Catch(() => srcAppGroup.m_native.GetProperty(p), out object? cVVar);

			CAzLogging.Log(hr, "Getting IAzApplicationGroup Property ID:", srcAppGroup.Name, p);

			if (hr.Succeeded && cVVar is string s && s.Length != 0)
			{
				hr = Catch(() => m_native.SetProperty(p, cVVar));

				CAzLogging.Log(hr, "Setting IAzApplicationGroup Property ID:", Name, p);
			}
		}
lDone:
		return hr;
	}
}