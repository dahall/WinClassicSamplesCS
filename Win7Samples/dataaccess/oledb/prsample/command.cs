namespace PRSample;

public static partial class Program
{
	/////////////////////////////////////////////////////////////////
	// myCreateCommand
	//
	// This function takes an IUnknown pointer on a Session object and attempts to create a Command object using the Session's
	// IDBCreateCommand interface. Since this interface is optional, this may fail.
	/////////////////////////////////////////////////////////////////
	private static void myCreateCommand(object pUnkSession, out ICommand ppUnkCommand)
	{
		// Attempt to create a Command object from the Session object
		IDBCreateCommand pIDBCreateCommand = (IDBCreateCommand)pUnkSession;
		ppUnkCommand = pIDBCreateCommand.CreateCommand<ICommand>();
	}

	/////////////////////////////////////////////////////////////////
	// myExecuteCommand
	//
	// This function takes an IUnknown pointer on a Command object and performs the following steps to create a new Rowset object:
	// - sets the given properties on the Command object; these properties will be applied by the provider to any Rowset created by this Command
	// - sets the given command text for the Command
	// - executes the command to create a new Rowset object
	/////////////////////////////////////////////////////////////////
	private static void myExecuteCommand(object pUnkCommand, string pwszCommandText, uint cPropSets, DBPROPSET[] rgPropSets, out object? ppUnkRowset)
	{
		// Set the properties on the Command object
		ICommandProperties pICommandProperties = (ICommandProperties)pUnkCommand;
		pICommandProperties.SetProperties(cPropSets, rgPropSets).ThrowIfFailed();

		// Set the text for this Command, using the default command text dialect. All providers that support commands must support this
		// dialect and providers that support SQL must be able to recognize an SQL command as SQL when this dialect is specified
		ICommandText pICommandText = (ICommandText)pUnkCommand;
		pICommandText.SetCommandText(DBGUID_DEFAULT, //guidDialect
			pwszCommandText).ThrowIfFailed(); //pwszCommandText

		// And execute the Command. Note that the user could have entered a non-row returning command, so we will check for that and return
		// failure to prevent the display of the non-existant rowset by the caller
		pICommandText.Execute(default, //pUnkOuter
			typeof(IRowset).GUID, //riid
			default, //pParams
			out _, //pcRowsAffected
			out ppUnkRowset //ppRowset
		).ThrowIfFailed();

		if (ppUnkRowset is null)
		{
			Console.Write("\nThe command executed successfully, but did not return a rowset.\nNo rowset will be displayed.\n");
			throw ((HRESULT)HRESULT.E_FAIL).GetException()!;
		}
	}
}