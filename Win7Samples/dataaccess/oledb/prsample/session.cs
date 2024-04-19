namespace PRSample;

public static partial class Program
{
	/////////////////////////////////////////////////////////////////
	// myCreateSchemaRowset
	//
	// If the provider supports IDBSchemaRowset, this function will obtain the tables schema rowset, will display this rowset to the user,
	// and will allow the user to select a row in the rowset containing the name of a table of interest.
	/////////////////////////////////////////////////////////////////
	private static void myCreateSchemaRowset(Guid guidSchema, object pUnkSession, out string? pwszBuffer)
	{
		// Attempt to obtain the IDBSchemaRowset interface on the Session object. This is not a mandatory interface; if it is not supported,
		// we are done
		IDBSchemaRowset pIDBSchemaRowset = (IDBSchemaRowset)pUnkSession;

		// Set properties on the rowset, to request additional functionality
		myAddRowsetProperties(out var rgPropSets);

		// Get the requested schema rowset; if IDBSchemaRowset is supported, the following schema rowsets are required to be supported:
		// DBSCHEMA_TABLES, DBSCHEMA_COLUMNS, and DBSCHEMA_PROVIDERTYPES We know that we will be asking for one of these, so it is not
		// necessary to call IDBSchemaRowset::GetSchemas in this case
		pIDBSchemaRowset.GetRowset(default, //pUnkOuter
			guidSchema, //guidSchema
			0, //cRestrictions
			default, //rgRestrictions
			typeof(IRowset).GUID, //riid
			1, //cPropSets
			rgPropSets, //rgPropSets
			out var pUnkRowset //ppRowset
		).ThrowIfFailed();

		// Display the rowset to the user; this will allow the user to perform basic navigation of the rowset and will allow the user to
		// select a row containing a desired table name (taken from the TABLE_NAME column)
		myDisplayRowset(pUnkRowset!, "TABLE_NAME", out pwszBuffer);
	}

	/////////////////////////////////////////////////////////////////
	// myCreateSession
	//
	// Create an OLE DB Session object from the given DataSource object. The IDBCreateSession interface is mandatory, so this is a simple operation.
	/////////////////////////////////////////////////////////////////
	private static void myCreateSession(object pUnkDataSource, out object ppUnkSession)
	{
		//Create a Session Object from a Data Source Object
		IDBCreateSession pIDBCreateSession = (IDBCreateSession)pUnkDataSource;
		pIDBCreateSession.CreateSession(default, //pUnkOuter
			typeof(IOpenRowset).GUID, //riid
			out ppUnkSession! //ppSession
		).ThrowIfFailed();
	}
}