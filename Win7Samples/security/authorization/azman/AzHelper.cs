using Vanara.PInvoke;
using static Vanara.PInvoke.AzRoles;

namespace AzMigrate;

static class CAzHelper
{
	public static HRESULT Catch<TRet>(Func<TRet> func, out TRet? result)
	{
		try
		{
			result = func();
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			result = default;
			return ex.HResult;
		}
	}

	public static HRESULT Catch(Action func)
	{
		try
		{
			func();
			return HRESULT.S_OK;
		}
		catch (Exception ex)
		{
			return ex.HResult;
		}
	}

	/*++

	Routine description:

		This method copies tasks from the source native AzMan interface to 
		the destination native AzMan interface

	Arguments: pNativeSource - Source native interface
			   pNativeNew - destination native interface

	Return Value:

		Returns success, appropriate failure value of the get/set methods done within 
		this method

	Description:
		2 Pass method used. In the first pass, create all the task objects;
		In the second pass, create the links between the task objects.

	--*/
	public static HRESULT CreateTasks(IAzApplicationScope pNativeSource, IAzApplicationScope pNativeNew)
	{
		// Copy all tasks objects; donot create any links between then
		HRESULT hr = CreateTasks(pNativeSource, pNativeNew, false);

		//Create links between the tasks now.
		if (hr.Succeeded)
			hr = CreateTasks(pNativeSource, pNativeNew, true);

		return hr;
	}

	/*++

	Routine description:

		This method copies tasks from the source native AzMan interface to 
		the destination native AzMan interface.
		If createlinks == true, then donot create tasks, only create links
		else create the new task objects

	Arguments: pNativeSource - Source native interface, 
			   pNativeNew -  destination native interface, 
			   bCreateLinks - createLinks

	Return Value:

		Returns success, appropriate failure value of the get/set methods done within 
		this method
	--*/
	public static HRESULT CreateTasks(IAzApplicationScope pNativeSource, IAzApplicationScope pNativeNew, bool bCreateLinks)
	{
		CAzLogging.Entering("CreateTasks");

		HRESULT hr = Catch(() => (IAzTasks)pNativeSource.Tasks, out var spAzTasks);

		CAzLogging.Log(hr, "Getting Tasks");

		if (hr.Failed)
			goto lError1;

		hr = Catch(() => spAzTasks!.Count, out var lCount);

		CAzLogging.Log(hr, "Getting Task Object count");

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;

		for (int i = 1; i <= lCount; i++)
		{
			hr = Catch(() => (IAzTask)spAzTasks![i], out var spOldTask);

			CAzLogging.Log(hr, "Getting Task Item");

			if (hr.Failed)
				goto lError1;

			CAzTask oldTask = new(spOldTask!, false);

			IAzTask? spNewTask;
			if (!bCreateLinks)
			{

				hr = Catch(() => pNativeNew.CreateTask(oldTask.Name), out spNewTask);

				CAzLogging.Log(hr, "Creating New Task Object");

				if (hr.Failed)
					goto lError1;

			}
			else
			{

				hr = Catch(() => pNativeNew.OpenTask(oldTask.Name, null), out spNewTask);

				CAzLogging.Log(hr, "Opening New Task Object to create links");

				if (hr.Failed)
					goto lError1;

			}

			CAzTask newTask = new(spNewTask!, !bCreateLinks);

			hr = bCreateLinks ? newTask.CopyLinks(oldTask) : newTask.Copy(oldTask);

			CAzLogging.Log(hr, "Copying Task properties");

			if (hr.Failed)
				goto lError1;

		}
lError1:
		CAzLogging.Exiting("CreateTasks");

		return hr;
	}

	/*++

	Routine description:

		This method copies appgroups from the source native AzMan interface to 
		the destination native AzMan interface

	Arguments: pNativeSource - Source native interface
			   pNativeNew    - Destination native interface

	Return Value:

		Returns success, appropriate failure value of the get/set methods done within 
		this method

	Description:
		2 Pass method used. In the first pass, create all the appgroup objects;
		In the second pass, create the links between the appgroup objects.

	--*/
	public static HRESULT CreateAppGroups(IAzApplicationScope pNativeSource, IAzApplicationScope pNativeNew)
	{
		HRESULT hr = CreateAppGroups(pNativeSource, pNativeNew, false);

		if (hr.Succeeded)
			hr = CreateAppGroups(pNativeSource, pNativeNew, true);

		return hr;
	}

	//public delegate HRESULT GetCollectionMethod(out IReadOnlyList<object> collection);
	//public delegate HRESULT CreateMethod<TNew>(string name, object var, out TNew newObj);
	//public static HRESULT CreateChildren<IAzParentNative, CAzNewClass, IAzCollection, IAzNewNative>(GetCollectionMethod getCollectionMethod, CreateMethod<IAzNewNative> createMethod)
	//	where IAzParentNative : class
	//	where CAzNewClass : class, new()
	//	where IAzNewNative : class
	//{
	//	getCollectionMethod(out IReadOnlyList<object> spAzColl);
	//	//CHECK_HR("Getting Operations for Application");
	//	var lCount = spAzColl.Count;
	//	if (lCount == 0)
	//		return HRESULT.S_OK;
	//	for (int i = 1; i <= lCount; i++) {
	//		var spOp = spAzColl[i] as IAzNewNative;
	//		CAzNewClass oldOp = new(spOp, false);
	//		createMethod(oldOp.Name, null, out var spNewOp).ThrowIfFailed("Creating new Operation in Destination Store");
	//		CAzNewClass newOp = new(spNewOp, true);
	//		newOp.Copy(oldOp).ThrowIfFailed("Copy Operation Properties");
	//	}

	//	return HRESULT.S_OK;
	//}


	/*++

	Routine description:

		This method copies roles from the source native AzMan interface to 
		the destination native AzMan interface

	Arguments: pNativeSource - Source native interface, 
			   pNativeNew -  destination native interface, 

	Return Value:

		Returns success, appropriate failure value of the get/set methods done within 
		this method

	--*/
	public static HRESULT CreateRoles(IAzApplicationScope pNativeSource, IAzApplicationScope pNativeNew)
	{
		CAzLogging.Entering("CreateRoles");

		HRESULT hr = Catch(() => (IAzRoles)pNativeSource.Roles, out var spAzRoles);

		CAzLogging.Log(hr, "Getting Roles");

		if (hr.Failed)
			goto lError1;

		hr = Catch(() => spAzRoles!.Count, out var lCount);

		CAzLogging.Log(hr, "Getting Role Object count");

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;

		for (int i = 1; i <= lCount; i++)
		{
			hr = Catch(() => (IAzRole)spAzRoles![i], out var spRole);

			CAzLogging.Log(hr, "Getting Role Item");

			if (hr.Failed || spRole is null)
				goto lError1;

			CAzRole role = new(spRole, false);

			hr = Catch(() => pNativeNew.CreateRole(role.Name), out var spNewRole);

			CAzLogging.Log(hr, "Creating New Role Object");

			if (hr.Failed || spNewRole is null)
				goto lError1;

			CAzRole newRole = new(spNewRole, true);

			hr = newRole.Copy(role);

			CAzLogging.Log(hr, "Copying Role properties");

			if (hr.Failed)
				goto lError1;
		}
lError1:
		CAzLogging.Exiting("CreateRoles");

		return hr;
	}

	/*++

	Routine description:

	This method copies appgroups from the source native AzMan interface to 
	the destination native AzMan interface.
	If createlinks == true, then donot create appgroups, only create links
	else create the new appgroup objects

	Arguments: pNativeSource - Source native interface, 
	pNativeNew - destination native interface, 
	bCreateLinks - createLinks

	Return Value:

	Returns success, appropriate failure value of the get/set methods done within 
	this method
	--*/
	public static HRESULT CreateAppGroups(IAzApplicationScope pNativeSource, IAzApplicationScope pNativeNew, bool bCreateLinks)
	{
		CAzLogging.Entering("CreateAppGroups");

		HRESULT hr = Catch(() => (IAzApplicationGroups)pNativeSource.ApplicationGroups, out var spAzAppGroups);

		CAzLogging.Log(hr, "Getting App Groups");

		if (hr.Failed)
			goto lError1;

		hr = Catch(() => spAzAppGroups!.Count, out var lCount);

		CAzLogging.Log(hr, "Getting App Group Object count");

		if (hr.Failed)
			goto lError1;

		if (lCount == 0)
			goto lError1;

		for (int i = 1; i <= lCount; i++) {

			hr = Catch(() => (IAzApplicationGroup)spAzAppGroups![i], out var spSrcAppGroup);

			CAzLogging.Log(hr, "Getting App Group Item");

			if (hr.Failed || spSrcAppGroup is null)
				goto lError1;

			CAzAppGroup oldAppGroup = new(spSrcAppGroup, false);

			IAzApplicationGroup? spNewAppGroup;
			if (!bCreateLinks) {

				hr = Catch(() => pNativeNew.CreateApplicationGroup(oldAppGroup.Name), out spNewAppGroup);

				CAzLogging.Log(hr, "Creating New App Group Object");

				if (hr.Failed || spNewAppGroup is null)
					goto lError1;

			} else {

				hr = Catch(() => pNativeNew.OpenApplicationGroup(oldAppGroup.Name), out spNewAppGroup);

				CAzLogging.Log(hr, "Opening New App Group Object to create links");

				if (hr.Failed || spNewAppGroup is null)
					goto lError1;

			}

			CAzAppGroup newAppGroup = new(spNewAppGroup, !bCreateLinks);

			hr = bCreateLinks ? newAppGroup.CopyLinks(oldAppGroup) : newAppGroup.Copy(oldAppGroup);

			CAzLogging.Log(hr, "Copying App Group properties");

			if (hr.Failed)
				goto lError1;

		}
lError1:
		CAzLogging.Exiting("CreateAppGroups");

		return hr;
	}
}