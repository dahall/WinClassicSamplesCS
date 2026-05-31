using Vanara.PInvoke;
using static Vanara.PInvoke.AzRoles;

namespace AzMigrate;

class CAzApplication(IAzApplication pApp, bool pIsNewApp) : CAzBase<IAzApplication>(pApp , pIsNewApp)
{
	static readonly AZ_PROP_CONSTANTS[] m_props = [
		AZ_PROP_CONSTANTS.AZ_PROP_APPLICATION_DATA,
		AZ_PROP_CONSTANTS.AZ_PROP_APPLICATION_AUTHZ_INTERFACE_CLSID,
		AZ_PROP_CONSTANTS.AZ_PROP_APPLICATION_VERSION,
		AZ_PROP_CONSTANTS.AZ_PROP_APPLY_STORE_SACL,
		AZ_PROP_CONSTANTS.AZ_PROP_DESCRIPTION,
		AZ_PROP_CONSTANTS.AZ_PROP_GENERATE_AUDITS,
		AZ_PROP_CONSTANTS.AZ_PROP_NAME
	];
	const byte m_uchNumberOfProps = 7;

	public IAzApplication Interface => m_native;

	/*++

	Routine description:

	This method copies properties from the source application ref to ref this 
	Application

	Arguments: pApp - Source Application

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/
	public HRESULT Copy(CAzApplication pApp) {

		CAzLogging.Entering("Copy");

		if (!m_isNew)

			return HRESULT.E_FAIL;

		object? cVVar;

		HRESULT hr = 0;

		for (long i = 0; i < m_uchNumberOfProps; i++) {
			hr = Catch(() => pApp.m_native.GetProperty(m_props[i]), out cVVar);

			CAzLogging.Log(hr, "Getting IAzApplication Property ID:", Name, m_props[i]);

			if (hr.Succeeded && cVVar != null) {

				hr = Catch(() => m_native.SetProperty(m_props[i], cVVar));

				CAzLogging.Log(hr, "Setting IAzApplication Property ID:", Name, m_props[i]);
			}
		}

		if (!CAzGlobalOptions.m_bIgnorePolicyAdmins) {
			hr = Catch(() => cVVar = pApp.m_native.DelegatedPolicyUsers, out cVVar);
			CAzLogging.Log(hr, "Getting IAzApplication Delegated Policy Users", Name);
			if (hr.Succeeded)
			{
				hr = InitializeUsingSafeArray(cVVar, m_native.AddDelegatedPolicyUser);
				CAzLogging.Log(hr, "Setting IAzApplication Delegated Policy Users", Name);
			}

			hr = Catch(() => cVVar = pApp.m_native.PolicyAdministrators, out cVVar);
			CAzLogging.Log(hr, "Getting IAzApplication Policy Admins", Name);
			if (hr.Succeeded)
			{
				hr = InitializeUsingSafeArray(cVVar, m_native.AddPolicyAdministrator);
				CAzLogging.Log(hr, "Setting IAzApplication Policy Admins", Name);
			}

			hr = Catch(() => cVVar = pApp.m_native.PolicyReaders, out cVVar);
			CAzLogging.Log(hr, "Getting IAzApplication Policy Readers", Name);
			if (hr.Succeeded)
			{
				hr = InitializeUsingSafeArray(cVVar, m_native.AddPolicyReader);
				CAzLogging.Log(hr, "Setting IAzApplication Delegated Policy Readers", Name);
			}

		}
		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting for IAzApplication", Name);

		hr = CreateChildItems(pApp);

		CAzLogging.Log(hr, "Creating Child Objects for IAzApplication", Name);

		CAzLogging.Exiting("Copy");
		return hr;
	}

	/*++

	Routine description:

	This method creates child objects(like Roles, Scopes etc)
	found in the source application ref into ref this 
	Application

	Arguments: pApp - Source Application

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/

	protected HRESULT CreateChildItems(CAzApplication pApp) {

		/*Below order has to be the same

		First create operations, then app groups, then tasks, then ref roles */

		CAzLogging.Entering("CAzApplication.CreateChildItems");

		HRESULT hr = CreateOperations(pApp);

		CAzLogging.Log(hr, "Creating Operations for IAzApplication", Name);

		hr = CAzHelper.CreateAppGroups(pApp.m_native, m_native);

		CAzLogging.Log(hr, "Creating Application Groups for IAzApplication", Name);

		hr = CAzHelper.CreateTasks(pApp.m_native, m_native);

		CAzLogging.Log(hr, "Creating Tasks for IAzApplication", Name);

		hr = CAzHelper.CreateRoles(pApp.m_native, m_native);

		CAzLogging.Log(hr, "Creating Roles for IAzApplication", Name);

		hr = CreateScopes(pApp);

		CAzLogging.Log(hr, "Creating Scopes for IAzApplication", Name);

		hr = Catch(() => m_native.Submit());

		CAzLogging.Log(hr, "Submitting child object addition for IAzApplication", Name);

		CAzLogging.Exiting("CAzApplication.CreateChildItems");

		return hr;
	}

	/*++

	Routine description:

	This method creates the operation objects
	found in the source application ref into ref this 
	Application

	Arguments: pApp - Source Application

	Return Value:

	Returns success, appropriate failure value 

	--*/

	protected HRESULT CreateOperations(CAzApplication pApp) {

		CAzLogging.Entering("CAzApplication.CreateOperations");

		HRESULT hr = Catch(() => pApp.m_native.Operations, out var spAzOpes);

		CAzLogging.Log(hr, "Getting operations for IAzApplication", Name);

		if (hr.Failed)
			goto lError1;

		hr = Catch(() => spAzOpes!.Count, out var lCount);

		CAzLogging.Log(hr, "Getting operation count for IAzApplication from IAzOperations", Name);

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;

		for (int i = 1; i <= lCount; i++)
		{
			hr = Catch(() => spAzOpes![i], out var cVappl);

			CAzLogging.Log(hr, "Getting operation object", Name);

			if (hr.Failed || cVappl is not IAzOperation spOp)
				goto lError1;

			CAzOperation oldOp = new(spOp, false);

			hr = Catch(() => m_native.CreateOperation(oldOp.Name), out var spNewOp);

			CAzLogging.Log(hr, "Creating Operation for IAzApplication", Name);

			if (hr.Failed || spNewOp is null)
				goto lError1;

			CAzOperation newOp = new(spNewOp, true);

			hr = newOp.Copy(oldOp);

			CAzLogging.Log(hr, "Copying IAzOperation properties for IAzApplication", Name);

			if (hr.Failed)
				goto lError1;
		}
lError1:
		CAzLogging.Exiting("CAzApplication.CreateOperations");

		return hr;
	}

	/*++

	Routine description:

	This method creates the scope objects
	found in the source application ref into ref this 
	Application

	Arguments: pApp - Source Application

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method

	--*/
	protected HRESULT CreateScopes(CAzApplication pApp) {

		CAzLogging.Entering("CAzApplication.CreateScopes");

		HRESULT hr = Catch(() => pApp.m_native.Scopes, out var spAzScopes);

		CAzLogging.Log(hr, "Getting scopes for IAzApplication", Name);

		if (hr.Failed)
			goto lError1;

		hr = Catch(() => spAzScopes!.Count, out var lCount);

		CAzLogging.Log(hr, "Getting operation count for IAzApplication from IAzOperations", Name);

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;
		for (int i = 1; i <= lCount; i++) {

			hr = Catch(() => (IAzScope)spAzScopes![i], out var spSrcScope);

			CAzLogging.Log(hr, "Getting scope object", Name);

			if (hr.Failed || spSrcScope is null)
				goto lError1;

			CAzScope srcScope = new(spSrcScope, false);

			hr = Catch(() => m_native.CreateScope(srcScope.Name), out var spNewScope);

			CAzLogging.Log(hr, "Creating scope for IAzApplication", Name);

			if (hr.Failed || spNewScope is null)
				goto lError1;

			CAzScope newScope = new(spNewScope, true);

			hr = newScope.Copy(srcScope);

			CAzLogging.Log(hr, "Copying IAzScope properties for IAzApplication", Name);

			if (hr.Failed)
				goto lError1;
		}
lError1:
		CAzLogging.Exiting("CAzApplication.CreateScopes");

		return hr;
	}
}