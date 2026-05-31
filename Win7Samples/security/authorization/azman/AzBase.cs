using Vanara.Extensions.Reflection;
using Vanara.PInvoke;

namespace AzMigrate;

internal abstract class CAzBase<T> where T : class
{
	protected bool m_isNew;
	protected string m_name;
	protected T m_native;

	public CAzBase(T pNative, bool bIsNew)
	{
		CAzLogging.Entering("Constructor");
		m_native = pNative;
		m_name = pNative.GetPropertyValue("Name", "")!;
		m_isNew = bIsNew;
		CAzLogging.Exiting("Constructor");
	}

	public string Name => m_name;

	protected HRESULT InitializeUsingSafeArray(object? @var, Action<string, object?> targetMethod)
	{
		CAzLogging.Entering("InitializeUsingSafeArray");

		HRESULT hr = 0;
		if (@var is string[] sa && sa.Length > 0)
		{
			for (int idx = 0; idx < sa.Length; idx++)
			{
				hr = Catch(() => targetMethod.Invoke(sa[idx], null));
				CAzLogging.Log(hr, "Calling Set Method in InitializeUsingSafeArray", Name);
			}
		}

		CAzLogging.Exiting("InitializeUsingSafeArray");

		return hr;
	}

	protected static HRESULT Catch<TRet>(Func<TRet> func, out TRet? result) => CAzHelper.Catch<TRet>(func, out result);

	protected static HRESULT Catch(Action func) => CAzHelper.Catch(func);
}