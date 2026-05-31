/****************************************************************************

// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
// Copyright (c) Microsoft Corporation. All rights reserved.

 File:  AzMStore.cpp

Abstract:

	Routines performing the migration for the AzMStore object


 History:

****************************************************************************/
using Vanara.PInvoke;
using static Vanara.PInvoke.AzRoles;
using static AzMigrate.CAzHelper;

namespace AzMigrate;

internal class CAzMStore
{
	private readonly Stack<CAzApplication> m_apps = [];
	private bool m_isNew;
	private string m_storeName = "";
	private IAzAuthorizationStore? m_native;
	protected static readonly AZ_PROP_CONSTANTS[] m_props = [
		AZ_PROP_CONSTANTS.AZ_PROP_AZSTORE_DOMAIN_TIMEOUT,
		AZ_PROP_CONSTANTS.AZ_PROP_AZSTORE_MAX_SCRIPT_ENGINES,
		AZ_PROP_CONSTANTS.AZ_PROP_AZSTORE_SCRIPT_ENGINE_TIMEOUT,
		AZ_PROP_CONSTANTS.AZ_PROP_APPLICATION_DATA,
		AZ_PROP_CONSTANTS.AZ_PROP_APPLY_STORE_SACL,
		AZ_PROP_CONSTANTS.AZ_PROP_DESCRIPTION,
		AZ_PROP_CONSTANTS.AZ_PROP_GENERATE_AUDITS
	];

	public CAzMStore(string pStoreName)
	{
		CAzLogging.Entering("CAzMStore");

		m_storeName = pStoreName;

		CAzLogging.Exiting("CAzMStore");
	}

	~CAzMStore()
	{
		CAzLogging.Entering("~CAzMStore");

		while (m_apps.Count > 0)
			m_apps.Pop();

		CAzLogging.Exiting("~CAzMStore");
	}

	public IReadOnlyCollection<CAzApplication> Apps => m_apps;

	/*++

	Routine description:

	This method overwrites the destination store

	Arguments: None

	Return Value:

	Returns success, appropriate failure value of the native interface methods 

	--*/
	public HRESULT OverWriteStore() {

		CAzLogging.Entering("OverWriteStore");

		HRESULT hr = InitializeStore(false, true);

		CAzLogging.Log(hr, "Initializing existing destination store for deletion", m_storeName);

		if (hr.Failed)
			goto lError1;

		if (hr.Succeeded) {

			hr = Catch(() => m_native?.Delete());

			CAzLogging.Log(hr, "Deleting existing store", m_storeName);

			if (hr.Failed)
				goto lError1;

			if (hr.Succeeded) {

				hr = InitializeStore(true, true);
			}

			CAzLogging.Log(hr, "Initializing new destination store", m_storeName);

		}
lError1:
		CAzLogging.Exiting("OverWriteStore");

		return hr;
	}

	/*++

	Routine description:

	This method initializes the store

	Arguments: pbcreateStore - Flag to create Store, or manage store
	pbOverWritten - Flag if current store initialize called from OverWriteStore()

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/
	public HRESULT InitializeStore(bool pbcreateStore, bool pbOverWritten = false)
	{
		CAzLogging.Entering("InitializeStore");

		HRESULT hr;

		//Donot create another instance if pbOverWritten==true as instance
		//already created 

		if (!pbOverWritten) {

			hr = Catch(() => new IAzAuthorizationStore(), out m_native);

			CAzLogging.Log(hr, "Creating instance of AzMan interface", m_storeName);

			if (hr.Failed)
				goto lError1;

		}

		IAzAuthorizationStore3? newStore = m_native as IAzAuthorizationStore3;

		CAzGlobalOptions.m_bVersionTwo = newStore is not null;

		AZ_PROP_CONSTANTS flags = 0;

		if (pbcreateStore == true) {

			flags = AZ_PROP_CONSTANTS.AZ_AZSTORE_FLAG_CREATE;

			if (CAzGlobalOptions.m_bVersionTwo)
				flags |= AZ_PROP_CONSTANTS.AZ_AZSTORE_NT6_FUNCTION_LEVEL;

		} else
			flags = AZ_PROP_CONSTANTS.AZ_AZSTORE_FLAG_MANAGE_STORE_ONLY;


		// long flags=((pbcreateStore==true) ? AZ_AZSTORE_FLAG_CREATE : AZ_AZSTORE_FLAG_MANAGE_STORE_ONLY);

		hr = Catch(() => m_native!.Initialize(flags, m_storeName));

		if (!(CAzGlobalOptions.m_bOverWrite && pbOverWritten == false))

			CAzLogging.Log(hr, "Initialize AzMan Authorization Store", m_storeName);

		m_isNew = pbcreateStore;

		if (hr.Succeeded && pbcreateStore) {

			m_native!.Submit();

			CAzLogging.Log(hr, "Submitting changes on AzMan Authornization Store", m_storeName);
		}

lError1:

		CAzLogging.Exiting("InitializeStore");

		return hr;
	}


	public HRESULT OpenApps(IEnumerable<string> pAppNames)
	{
		CAzLogging.Entering("OpenApp");

		HRESULT hr = HRESULT.S_OK;

		foreach (var appName in pAppNames) {

			hr = Catch(() => m_native?.OpenApplication(appName), out var spApp);

			CAzLogging.Log(hr, "Opening Application", appName);

			if (hr.Succeeded) {

				CAzApplication app = new(spApp!, false);

				m_apps.Push(app);
			}
		}

		CAzLogging.Exiting("OpenApp");
		return hr;
	}

	/*++

	Routine description:

	This method Opens the specified application and adds it to the internal vector 
	of applications

	Arguments: pAppName - Name of the app that needs to be opened

	Return Value:

	Returns success, appropriate failure value 

	--*/
	public HRESULT OpenApp(string pAppName) {

		CAzLogging.Entering("OpenApp");

		if (m_isNew)
			return HRESULT.E_FAIL;

		HRESULT hr = Catch(() => m_native?.OpenApplication(pAppName), out var spApp);

		CAzLogging.Log(hr, "Opening Application", pAppName);

		if (hr.Succeeded && spApp is not null) {

			CAzApplication app = new(spApp, false);

			m_apps.Push(app);

		}

		CAzLogging.Exiting("OpenApp");
		return hr;
	}

	/*++

	Routine description:

	This method Opens all applications in the specified store

	Arguments: NONE

	Return Value:

	Returns success, appropriate failure value 

	--*/
	public HRESULT OpenAllApps()
	{
		CAzLogging.Entering("OpenAllApps");

		if (m_isNew)
			return HRESULT.E_FAIL;

		HRESULT hr = Catch(() => m_native?.Applications, out var spAzApplications);

		CAzLogging.Log(hr, "Getting all applications", m_storeName);

		if (hr.Failed || spAzApplications is null)
			goto lError1;

		hr = Catch(() => spAzApplications.Count, out var lCount);

		CAzLogging.Log(hr, "Getting application count", m_storeName);

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;

		for (int i = 1; i <= lCount; i++) {

			hr = Catch(() => (IAzApplication)spAzApplications[i], out var spApp);

			CAzLogging.Log(hr, "Getting application item", m_storeName);

			if (hr.Failed || spApp is null)
				goto lError1;

			CAzApplication app = new(spApp, false);

			m_apps.Push(app);

		}

lError1:

		CAzLogging.Exiting("OpenAllApps");

		return hr;
	}

	/*++

	Routine description:

	This method copies all store properties from source store ref to ref this store

	Arguments: sourceStore - Name of the source store 

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/
	protected HRESULT CopyStoreProperties(CAzMStore sourceStore) {

		CAzLogging.Entering("CopyStoreProperties");

		HRESULT hr = HRESULT.S_OK;

		foreach (AZ_PROP_CONSTANTS p in m_props) {

			hr = Catch(() => sourceStore.m_native?.GetProperty(p), out var cVVar);

			CAzLogging.Log(hr, "Getting IAzAuthorizationStore Property ID:", sourceStore.m_storeName, p);

			if (hr.Succeeded && cVVar is not null) {

				hr = Catch(() => m_native?.SetProperty(p, cVVar));

				CAzLogging.Log(hr, "Setting IAzAuthorizationStore Property ID:", m_storeName, p);
			}
		}

		if (!CAzGlobalOptions.m_bIgnorePolicyAdmins) {

			hr = Catch(() => sourceStore.m_native?.DelegatedPolicyUsers, out var cVVar);

			CAzLogging.Log(hr, "Getting IAzAuthorizationStore Delegated Policy Users", m_storeName);

			if (hr.Succeeded && cVVar is not null) {

				hr = InitializeUsingSafeArray(cVVar, m_native!.AddDelegatedPolicyUser);

				CAzLogging.Log(hr, "Setting IAzAuthorizationStore Delegated Policy Users", m_storeName);

			}

			hr = Catch(() => sourceStore.m_native?.PolicyAdministrators, out cVVar);

			CAzLogging.Log(hr, "Getting IAzAuthorizationStore Policy Admins", m_storeName);

			if (hr.Succeeded && cVVar is not null) {

				hr = InitializeUsingSafeArray(cVVar, m_native!.AddPolicyAdministrator);

				CAzLogging.Log(hr, "Setting IAzAuthorizationStore Policy Admins", m_storeName);
			}

			hr = Catch(() => sourceStore.m_native?.PolicyReaders, out cVVar);

			CAzLogging.Log(hr, "Getting IAzAuthorizationStore Policy Readers", m_storeName);

			if (hr.Succeeded && cVVar is not null) {

				hr = InitializeUsingSafeArray(cVVar, m_native!.AddPolicyReader);

				CAzLogging.Log(hr, "Setting IAzAuthorizationStore Delegated Policy Readers", m_storeName);
			}

			hr = Catch(() => m_native?.Submit());

			CAzLogging.Log(hr, "Submitting for IAzAuthorizationStore", m_storeName);
		}

		CAzLogging.Exiting("CopyStoreProperties");

		return hr;
	}


	/*++

	Routine description:

	This method copies all app groups from source store ref to ref this store
	It also copies all applications in the vector in the source store
	ref to ref this store

	Arguments: sourceStore - Name of the source store 

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/
	public HRESULT Copy(CAzMStore sourceStore)
	{
		CAzLogging.Entering("Copy");

		if (!m_isNew)
			return HRESULT.E_FAIL;

		HRESULT hr = CopyStoreProperties(sourceStore);

		CAzLogging.Log(hr, "Copying Store Properties ", m_storeName);

		hr = CreateAppGroups(sourceStore, false);

		CAzLogging.Log(hr, "Creating AppGroups under Store", m_storeName);

		if (hr.Failed)
			goto lError1;

		hr = CreateAppGroups(sourceStore, true);

		CAzLogging.Log(hr, "Creating AppGroup Links under Store", m_storeName);

		if (hr.Failed)
			goto lError1;

		var sourceApps = sourceStore.Apps;

		foreach (var app in sourceApps) {

			hr = Catch(() => m_native?.CreateApplication(app.Name), out var iazApp);

			CAzLogging.Log(hr, "Creating Application under Store", m_storeName);

			CAzApplication newApp = new(app.Interface, true);

			hr = newApp.Copy(app);

			if (hr.Failed)
				goto lError1;

			if (hr.Succeeded) {

				m_apps.Push(newApp);

				m_native?.CloseApplication(app.Name, 0);

			}
		}
		goto lDone;

lError1:
		CAzLogging.MIGRATE_SUCCESS = false;

lDone:

		CAzLogging.Exiting("Copy");

		return hr;
	}

	/*++

	Routine description:

	This method creates all app groups from source store ref into ref this store

	Arguments: sourceStore - Name of the source store 
	bCreateLinks - If links between app groups need to be created or not

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/
	protected HRESULT CreateAppGroups(CAzMStore sourceStore, bool bCreateLinks) {

		CAzLogging.Entering("CreateAppGroups");

		HRESULT hr = Catch(() => sourceStore.m_native?.ApplicationGroups, out var spAzAppGroups);

		CAzLogging.Log(hr, "Getting ApplicationGroups for Store", m_storeName);

		if (hr.Failed || spAzAppGroups is null || m_native is null)
			goto lError1;

		hr = Catch(() => spAzAppGroups.Count, out var lCount);

		CAzLogging.Log(hr, "Getting ApplicationGroups Count", m_storeName);

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;

		for (int i = 1; i <= lCount; i++) {

			hr = Catch(() => (IAzApplicationGroup)spAzAppGroups[i], out var spSrcAppGroup);

			CAzLogging.Log(hr, "Getting ApplicationGroup Item", m_storeName);

			if (hr.Failed || spSrcAppGroup is null)
				goto lError1;

			CAzAppGroup oldAppGroup = new(spSrcAppGroup, false);

			IAzApplicationGroup? spNewAppGroup;
			if (!bCreateLinks) {

				hr = Catch(() => m_native.CreateApplicationGroup(oldAppGroup.Name), out spNewAppGroup);

				CAzLogging.Log(hr, "Creating new application group for store", m_storeName);

				if (hr.Failed)
					goto lError1;

			} else {

				hr = Catch(() => m_native.OpenApplicationGroup(oldAppGroup.Name), out spNewAppGroup);

				CAzLogging.Log(hr, "Opening New App Group Object to create links");

				if (hr.Failed)
					goto lError1;
			}

			CAzAppGroup newAppGroup = new(spNewAppGroup!, !bCreateLinks);

			hr = bCreateLinks ? newAppGroup.CopyLinks(oldAppGroup) : newAppGroup.Copy(oldAppGroup);

			CAzLogging.Log(hr, "Setting Application group properties", m_storeName);

			if (hr.Failed)
				goto lError1;
		}

lError1:
		CAzLogging.Exiting("CreateAppGroups");
		return hr;
	}


	/*++

	Routine description:

	This method is a utility method which takes a VARIANT
	which contains a SAFEARRAY of [MarshalAs(UnmanagedType.BStr)] string which are then set
	ref into ref this object`s Authorization Stores Native interface
	by calling the target method

	Arguments: cVVar - Variant which contains the SAFEARRAY
	targetMethod - TargetMethod which is invoked for Setting
	each element contained in the SAFEARRAY

	Return Value:

	Returns success, appropriate failure value of the procedures done within 
	this method

	--*/
	protected HRESULT InitializeUsingSafeArray(object cVVar, Action<string, object> targetMethod)
	{
		CAzLogging.Entering("InitializeUsingSafeArray");

		HRESULT hr = 0;
		if (cVVar is System.Collections.IEnumerable e)
		{
			foreach (var s in e.OfType<string>())
			{
				hr = Catch(() => targetMethod(s, new object()));
				CAzLogging.Log(hr, "Calling Set Method in InitializeUsingSafeArray", m_storeName);
			}

			if (hr.Failed)
				goto lEnd;
		}
		else
			goto lEnd;

		CAzLogging.Log(hr, "Calling Set Method in InitializeUsingSafeArray", m_storeName);

lEnd:
		CAzLogging.Exiting("InitializeUsingSafeArray");

		return hr;
	}
}