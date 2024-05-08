using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.OleDb;

namespace RowsetViewer;
internal class CBinder : CBase
{
	IBindResource m_pIBindResource; //Binder interface
	ICreateRow m_pICreateRow; //Binder interface
	IDBProperties m_pIDBProperties; //Binder interface
	IDBBinderProperties m_pIDBBinderProperties; //Binder interface
	IRegisterProvider m_pIRegisterProvider; //Binder interface

	//Saved URL
	string m_pwszURL;
	public virtual string ObjectName => "Binder";
	public virtual uint ObjectMenu = IDM_BINDERMENU;
	public virtual int ObjectImage => IMAGE_CHAPTER;
	public virtual Guid DefaultInterface => typeof(IBindResource).GUID;
	public virtual void CreateInstanceBinder(in Guid clsidProv)
	{
		// Create IBindResource instance
	}
	public virtual void SetProperties(DBPROPSET[]? rgPropSets)
	{
		m_pIDBProperties?.SetProperties((uint)(rgPropSets?.Length ?? 0), rgPropSets).ThrowIfFailed();
	}
}
