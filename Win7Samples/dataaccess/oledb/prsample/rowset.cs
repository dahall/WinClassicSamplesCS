using Vanara.Extensions;
using Vanara.InteropServices;
using static Vanara.PInvoke.Ole32;

namespace PRSample;

public static partial class Program
{
	/////////////////////////////////////////////////////////////////
	// myAddRowsetProperties
	//
	// This function sets up the given DBPROPSET and DBPROP structures, adding two optional properties that describe features that we would
	// like to use on the Rowset created with these properties applied:
	// - DBPROP_CANFETCHBACKWARDS -- the rowset should support fetching rows backwards from our current cursor position
	// - DBPROP_IRowsetLocate -- the rowset should support the IRowsetLocate interface and its semantics
	/////////////////////////////////////////////////////////////////
	private static void myAddRowsetProperties(out DBPROPSET[] pPropSet)
	{
		// Add the following two properties (as OPTIONAL) to the property array contained in the property set array in order to request that
		// they be supported by the rowset we will create. Because these are optional, the rowset we obtain may or may not support this
		// functionality. We will check for the functionality that we need once the rowset is created and will modify our behavior appropriately
		DBPROP[] rgProperties =
		[
			myAddProperty(DBPROPENUM.DBPROP_CANFETCHBACKWARDS),
			myAddProperty(DBPROPENUM.DBPROP_IRowsetLocate)
		];
		// Initialize the property set array
		pPropSet = [new DBPROPSET() { rgProperties = rgProperties, guidPropertySet = DBPROPSET_ROWSET }];
	}

	/////////////////////////////////////////////////////////////////
	// myCreateAccessor
	//
	// This function takes an IUnknown pointer for a Rowset object and creates an Accessor that describes the layout of the buffer we will
	// use when we fetch data. The provider will fill this buffer according to the description contained in the Accessor that we will create here.
	/////////////////////////////////////////////////////////////////
	private static void myCreateAccessor(object pUnkRowset, out IntPtr phAccessor, out DBBINDING[] prgBindings, out nuint pcbRowSize)
	{
		//HRESULT hr;
		//IAccessor pIAccessor = default;

		// An Accessor is basically a handle to a collection of bindings. To create the Accessor, we need to first create an array of
		// bindings for the columns in the Rowset
		mySetupBindings((IRowset)pUnkRowset, out prgBindings, out pcbRowSize);

		// Now that we have an array of bindings, tell the provider to create the Accessor for those bindings. We get back a handle to this
		// Accessor, which we will use when fetching data
		IAccessor pIAccessor = (IAccessor)pUnkRowset;
		pIAccessor.CreateAccessor(DBACCESSORFLAGS.DBACCESSOR_ROWDATA, (nuint)prgBindings.Length, prgBindings, 0, out phAccessor, default);
	}

	/////////////////////////////////////////////////////////////////
	// myCreateRowset
	//
	// This function creates an OLE DB Rowset object from the given provider's Session object. It first obtains a default table name from the
	// user through the tables schema rowset, if supported, then creates a Rowset object by one of two methods:
	//
	// - If the user requested that the Rowset object be created from a Command object, it creates a Command object, then obtains command
	// text from the user, sets properties and the command text, and finally executes the command to create the Rowset object
	// - Otherwise, the function obtains a table name from the user and calls IOpenRowset::OpenRowset to create a Rowset object over that
	// table that supports the requested properties
	/////////////////////////////////////////////////////////////////
	private static void myCreateRowset(object pUnkSession, out IRowset? ppUnkRowset)
	{
		//HRESULT hr;
		//object pUnkCommand = default;
		//IOpenRowset pIOpenRowset = default;

		// Obtain a default table name from the user by displaying the tables schema rowset if schema rowsets are supported.
		myCreateSchemaRowset(DBSCHEMA_TABLES, pUnkSession, out var wszTableName);

		// Set properties on the rowset, to request additional functionality
		myAddRowsetProperties(out var rgPropSets);

		// If the user requested that the rowset be created from a Command object, create a Command, set its properties and text and execute
		// it to create the Rowset object
		if (g_dwFlags.IsFlagSet(Flags.USE_COMMAND))
		{
			//ushort[] wszCommandText = new ushort[MAX_NAME_LEN + 1];

			// Attempt to create the Command object from the provider's Session object. Note that Commands are not supported by all
			// providers, and this will fail in that case
			myCreateCommand(pUnkSession, out var pUnkCommand);

			// Get the command text that we will execute from the user
			if (!myGetInputFromUser(out var wszCommandText, $"\nType the command to execute [Enter = `select from {0}`]: ", wszTableName))
			{
				wszCommandText = string.Format("select from {0}", wszTableName);
			}

			// And execute the Command the user entered
			myExecuteCommand(pUnkCommand, wszCommandText, 1, rgPropSets, out ppUnkRowset);
		}
		// Otherwise, the user gets the default behavior, which is to use IOpenRowset to create the Rowset object from the Session object.
		// IOpenRowset is supported by all providers; it takes a TableID and creates a rowset containing all rows in that table. It is
		// similar to using SQL command text of "ref select from TableID"
		else
		{
			// Create the TableID
			using SafeLPWSTR pwszTableName = new(wszTableName);
			DBID TableID = new() { eKind = DBKIND.DBKIND_NAME, uName = new() { pwszName = pwszTableName } };

			// Obtain the table name from the user
			myGetInputFromUser(out wszTableName, "\nType the name of the table to use [Enter = `{0}`]: ", wszTableName);

			// Get the IOpenRowset interface and create a Rowset object over the requested table through OpenRowset
			IOpenRowset pIOpenRowset = (IOpenRowset)pUnkSession;
			using var sProps = new SafeDBPROPSETListHandle(rgPropSets);
			pIOpenRowset.OpenRowset(default, //pUnkOuter
				TableID, //pTableID
				default, //pIndexID
				sProps, // rgPropSets, //rgPropSets
				out ppUnkRowset //ppRowset
			).ThrowIfFailed();
		}
	}

	/////////////////////////////////////////////////////////////////
	// myDisplayColumnNames
	//
	// This function takes an IUnknown pointer to a Rowset object and displays the names of the columns of that Rowset.
	/////////////////////////////////////////////////////////////////
	private static void myDisplayColumnNames(object pUnkRowset, uint[] rgDispSize)
	{
		// Get the IColumnsInfo interface for the Rowset
		IColumnsInfo pIColumnsInfo = (IColumnsInfo)pUnkRowset;

		// Get the columns information
		pIColumnsInfo.GetColumnInfo(out var rgColumnInfo);

		// Display the title of the row index column
		Console.Write(" Row | ");

		// Display all column names
		for (uint iCol = 0; iCol < rgColumnInfo.Length; iCol++)
		{
			string pwszColName = rgColumnInfo[iCol].pwszName ?? (rgColumnInfo[iCol].iOrdinal == 0 ? "Bmk" : "(null)");

			// Ensure that the name is no longer than MAX_DISPLAY_SIZE Figure out how many spaces we need to print after this column name
			string wszColumn = pwszColName.FixLen(Math.Min((int)rgDispSize[iCol], MAX_DISPLAY_SIZE));

			// Print the column name
			Console.Write(wszColumn);

			// Now end the column with a separator marker if necessary
			if (iCol < rgColumnInfo.Length - 1)
				Console.Write(" | ");
		}

		// Done with the header, so print a newline
		Console.Write("\n");
	}

	private static string FixLen(this string? s, int length) => s?.PadRight(length).Substring(0, length) ?? new(' ', length);

	/////////////////////////////////////////////////////////////////
	// myDisplayRow
	//
	// This function displays the data for a row.
	/////////////////////////////////////////////////////////////////
	private static void myDisplayRow(uint iRow, DBBINDING[] rgBindings, IntPtr pData, uint[] rgDispSize)
	{
		//HRESULT hr = HRESULT.S_OK;
		//DBSTATUS dwStatus;
		//uint ulLength;
		//IntPtr pvValue;
		//uint iCol;
		//uint cbRead;
		//ISequentialStream? pISeqStream = default;
		//SizeT cSpaces;
		//uint iSpace;

		// Display the row number
		Console.Write(" [{0}] | ", iRow);

		// For each column that we have bound, display the data
		for (int iCol = 0; iCol < rgBindings.Length; iCol++)
		{
			// We have bound status, length, and the data value for all columns, so we know that these can all be used
			DBSTATUS dwStatus = (pData + (int)rgBindings[iCol].obStatus).Convert<DBSTATUS>(4);
			uint ulLength = (pData + (int)rgBindings[iCol].obLength).Convert<uint>(4);
			IntPtr pvValue = pData + (int)rgBindings[iCol].obValue;

			// Check the status of this column. This decides exactly what will be displayed for the column
			string wszColumn = "";
			switch (dwStatus)
			{
				// The data is default, so don't try to display it
				case DBSTATUS.DBSTATUS_S_ISNULL:
					wszColumn = "(null)";
					break;

				// The data was fetched, but may have been truncated. Display string data for this column to the user
				case DBSTATUS.DBSTATUS_S_TRUNCATED:
				case DBSTATUS.DBSTATUS_S_OK:
				case DBSTATUS.DBSTATUS_S_DEFAULT:
					// We have either bound the column as a Unicode string (DBTYPE_WSTR) or as an ISequentialStream object (DBTYPE_IUNKNOWN),
					// and have to do different processing for each one of these possibilities
					switch (rgBindings[iCol].wType)
					{
						case DBTYPE.DBTYPE_WSTR:
							// Copy the string data
							wszColumn = Marshal.PtrToStringUni(pvValue) ?? "";
							break;

						case DBTYPE.DBTYPE_IUNKNOWN:
							// We've bound this as an ISequentialStream object, therefore the data in our buffer is a pointer to the object's
							// ISequentialStream interface
							ISequentialStream pISeqStream = (ISequentialStream)Marshal.GetObjectForIUnknown(pvValue);

							// We call ISequentialStream::Read to read bytes from the stream blindly into our buffer, simply as a
							// demonstration of ISequentialStream. To display the data properly, the native provider type of this column
							// should be accounted for; it could be DBTYPE_WSTR, in which case this works, or it could be DBTYPE_STR or
							// DBTYPE_BYTES, in which case this won't display the data correctly
							byte[] col = new byte[2 * (MAX_DISPLAY_SIZE + 1)];
							pISeqStream.Read(col, //pBuffer
								MAX_DISPLAY_SIZE, //cBytes
								out var cbRead //pcBytesRead
							).ThrowIfFailed();

							// Since streams don't provide default-termination, we'll NULL-terminate the resulting string ourselves
							wszColumn = Encoding.Unicode.GetString(col, 0, (int)cbRead / 2);

							break;
					}
					break;

				// This is an error status, so don't try to display the data
				default:
					wszColumn = "(error status)";
					break;
			}

			// Determine how many spaces we need to add after displaying this data to align it with this column in other rows Print the
			// column data
			Console.Write(wszColumn.FixLen(Math.Min((int)rgDispSize[iCol], MAX_DISPLAY_SIZE)));

			// Now end the column with a separator marker if necessary
			if (iCol < rgBindings.Length - 1)
				Console.Write(" | ");
		}

		// Print the row separator
		Console.Write("\n");
	}

	/////////////////////////////////////////////////////////////////
	// myDisplayRowset
	//
	// This function will display data from a Rowset object and will allow the user to perform basic navigation of the rowset.
	//
	// The function takes a pointer to a Rowset object's IUnknown and, optionally, the name of a column and a buffer that will receive the
	// value of that column when the user selects a row.
	/////////////////////////////////////////////////////////////////
	private static HRESULT myDisplayRowset(object pUnkRowset, string? pwszColToReturn, out string? pwszBuffer)
	{
		//HRESULT hr;
		//IRowset pIRowset = default;
		//uint cBindings;
		//ref DBBINDING rgBindings = default;
		//HACCESSOR hAccessor = DB_NULL_HACCESSOR;
		//uint cbRowSize;
		//IntPtr pData = default;
		//ref uint rgDispSize = default;
		//DBCOUNTITEM cRowsObtained;
		//ref HROW rghRows = default;
		//uint iRow;
		int cRows = MAX_ROWS;
		int iRetCol = -1;
		//bool fCanFetchBackwards;
		//uint iIndex;
		IntPtr pCurData;
		pwszBuffer = null;

		// Obtain the IRowset interface for use in fetching rows and data
		IRowset pIRowset = (IRowset)pUnkRowset;

		// Determine whether this rowset supports fetching data backwards; we use this to determine whether the rowset can support moving to
		// the previous set of rows, described in more detail below
		myGetProperty(pUnkRowset, typeof(IRowset).GUID, DBPROPENUM.DBPROP_CANFETCHBACKWARDS, DBPROPSET_ROWSET, out var fCanFetchBackwards);

		// If the caller wants us to return the data for a particular column from a user-selected row, we need to turn the column name into a
		// column ordinal
		if (pwszColToReturn is not null)
			myFindColumn(pUnkRowset, pwszColToReturn, out iRetCol);

		// Create an Accessor. An Accessor is basically a handle to a collection of bindings that describes to the provider how to copy (and
		// convert, if necessary) column data into our buffer. The Accessor that this creates will bind all columns as either DBTYPE_WSTR (a
		// Unicode string) or as an ISequentialStream object (used for BLOB data). This will also give us the size of the row buffer that the
		// Accessor describes to the provider
		myCreateAccessor(pUnkRowset, out var hAccessor, out var rgBindings, out var cbRowSize);

		// Allocate enough memory to hold cRows rows of data; this is where the actual row data from the provider will be placed
		using SafeCoTaskMemHandle pData = new((int)cbRowSize * MAX_ROWS);
		//CHECK_MEMORY(hr, pData);

		// Allocate memory for an array that we will use to calculate the maximum display size used by each column in the current set of rows
		uint[] rgDispSize = new uint[rgBindings.Length];
		//CHECK_MEMORY(hr, rgDispSize);

		// In this loop, we perform the following process:
		// - reset the maximum display size array
		// - try to get cRows row handles from the provider
		// - these handles are then used to actually get the row data from the provider copied into our allocated buffer
		// - calculate the maximum display size for each column
		// - release the row handles to the rows we obtained
		// - display the column names for the rowset
		// - display the row data for the rows that we fetched
		// - get user input
		// - free the provider-allocated row handle array
		// - repeat unless the user has chosen to quit or has selected a row
		HRESULT hr = default;
		while (hr == HRESULT.S_OK)
		{
			// Clear the maximum display size array
			Array.Clear(rgDispSize);

			// Attempt to get cRows row handles from the provider
			pIRowset.GetNextRows(DB_NULL_HCHAPTER, //hChapter
				0, //lOffset
				cRows, //cRows
				out var cRowsObtained, //pcRowsObtained
				out var prghRows //prghRows
			).ThrowIfFailed();
			IntPtr[] rghRows = prghRows.ToArray<IntPtr>((int)cRowsObtained);

			// Loop over the row handles obtained from GetNextRows, actually fetching the data for these rows into our buffer
			for (int iRow = 0; iRow < (int)cRowsObtained; iRow++)
			{
				// Find the location in our buffer where we want to place the data for this row. Note that if we fetched rows backwards
				// (cRows < 0), the row handles obtained from the provider are reversed from the order in which we want to actually display
				// the data on the screen, so we will account for this. This ensures that the resulting order of row data in the pData buffer
				// matches the order we wish to use to display the data
				var iIndex = cRows > 0 ? iRow : (int)cRowsObtained - iRow - 1;
				pCurData = (IntPtr)pData + ((int)cbRowSize * iIndex);

				// Get the data for this row handle. The provider will copy (and convert, if necessary) the data for each of the columns that
				// are described in our Accessor into the given buffer (pCurData)
				pIRowset.GetData(rghRows[iRow], //hRow
					hAccessor, //hAccessor
					pCurData //pData
				).ThrowIfFailed();

				// Update the maximum display size array, accounting for this row
				myUpdateDisplaySize(rgBindings, pCurData, rgDispSize);
			}

			// If we obtained rows, release the row handles for the retrieved rows and display the names of the rowset columns before we
			// display the data
			if (cRowsObtained is not 0)
			{
				// Release the row handles that we obtained
				pIRowset.ReleaseRows(cRowsObtained, //cRows
					rghRows, //rghRows
					default, //rgRowOptions
					default, //rgRefCounts
					default //rgRowStatus
				).ThrowIfFailed();

				// Display the names of the rowset columns
				myDisplayColumnNames(pIRowset, rgDispSize);
			}

			// For each row that we obtained the data for, display this data
			for (uint iRow = 0; iRow < (int)cRowsObtained; iRow++)
			{
				// Get a pointer to the data for this row
				pCurData = (IntPtr)pData + (int)cbRowSize * (int)iRow;

				// And display the row data
				myDisplayRow(iRow, rgBindings, pCurData, rgDispSize);
			}

			// Allow the user to navigate the rowset. This displays the appropriate prompts, gets the user's input, may call
			// IRowset::RestartPosition, and may copy data from a selected row to the selection buffer, if so directed. This will return
			// HRESULT.S_OK if the user asked for more rows, S_FALSE if the user selected a row, or HRESULT.E_FAIL if the user quits
			hr = myInteractWithRowset(pIRowset, // IRowset pointer, for RestartPosition
				out cRows, // updated with fetch direction value
				cRowsObtained, // to indicate selection range
				fCanFetchBackwards, // whether [P]revious is supported
				pData, // data pointer for copying selection
				cbRowSize, // size of rows for copying selection
				iRetCol >= 0 ? // bindings for the selection column,
				rgBindings[iRetCol] : // or default if no selection column
				null,
				out pwszBuffer); // pointer to the selection buffer

			// Since we are allowing the provider to allocate the memory for the row handle array, we will free this memory and reset the
			// pointer to default. If this is not default on the next call to GetNextRows, the provider will assume that it points to an
			// allocated array of the required size (which may not be the case if we obtained less than cRows rows from this last call to GetNextRows
			prghRows.Dispose();
		}

		myFreeBindings(rgBindings);

		return hr;
	}

	/////////////////////////////////////////////////////////////////
	// myFindColumn
	//
	// Find the index of the column described in pwszName and return S_OK, or, if not found, S_FALSE.
	/////////////////////////////////////////////////////////////////
	private static bool myFindColumn(object pUnkRowset, string pwszName, out int plIndex)
	{
		//HRESULT hr;
		//IColumnsInfo pIColumnsInfo = default;
		//uint cColumns;
		//ref DBCOLUMNINFO rgColumnInfo = default;
		//ref ushort pStringsBuffer = default;
		//uint iCol;

		// Get the IColumnsInfo interface
		IColumnsInfo pIColumnsInfo = (IColumnsInfo)pUnkRowset;

		// Get the columns information
		pIColumnsInfo.GetColumnInfo(out var rgColumnInfo);

		// Search for the column we need
		for (uint iCol = 0; iCol < rgColumnInfo.Length; iCol++)
		{
			// If the column name matches we've found the column...
			if (pwszName == rgColumnInfo[iCol].pwszName)
			{
				plIndex = (int)iCol;
				return true;
			}
		}

		// If we didn't find the column, we'll return S_FALSE
		plIndex = -1;
		return false;
	}

	/////////////////////////////////////////////////////////////////
	// myFreeBindings
	//
	// This function frees a bindings array and any allocated structures contained in that array.
	/////////////////////////////////////////////////////////////////
	private static void myFreeBindings(DBBINDING[] rgBindings)
	{
		// Free any memory used by DBOBJECT structures in the array
		foreach (var b in rgBindings)
			CoTaskMemFree(b.pObject);
	}

	private static HRESULT myInteractWithRowset(IRowset pIRowset, out int pcRows, nuint cRowsObtained, bool fCanFetchBackwards,
			IntPtr pData, nuint cbRowSize, DBBINDING? pBinding, out string? pwszBuffer)
	{
		HRESULT hr = HRESULT.S_OK;
		pcRows = 0;
		pwszBuffer = null;

		// Let the user know if no rows were fetched
		if (cRowsObtained == 0)
			Console.Write("\n*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*\n" +
							"*                                 *\n" +
							"* No rows obtained on this fetch! *\n" +
							"*                                 *\n" +
							"*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*\n");

		// Print navigation options
		if (fCanFetchBackwards)
			Console.Write("\n[P]revious; [N]ext; [R]estart; ");
		else
			Console.Write("\n[N]ext; [R]estart; ");

		// Print selection options
		if (cRowsObtained != 0 && pBinding.HasValue)
			Console.Write("[0]-[{0}] for a row; ", cRowsObtained - 1);

		// User can always quit the program
		Console.Write("[Q]uit? ");

		// Get the user's input
		try
		{
			while (true)
			{
				// Get a character from the console
				var ch = myGetChar();

				// Look for one of the allowed options; if not found, go back around and wait for another input from the user

				// If we're looking for a row selection, allow the user to select a row that we fetched, then copy the data from the
				// requested column into the selection buffer we were passed
				if (pBinding.HasValue && ch >= '0' && ch < (int)('0' + cRowsObtained))
				{
					// Save the data for the selected row
					int nSelection = ch - '0';
					//pwszBuffer is of size cchBuffer+1
					pwszBuffer = Marshal.PtrToStringUni(pData + (int)cbRowSize * nSelection + (int)pBinding.Value.obValue) ?? "";
					hr = HRESULT.S_FALSE;
				}
				// If the provider supports fetching backwards, set *pcRows to -MAX_ROWS. When GetNextRows is called with this value, it will
				// fetch rows backwards from the current position until it fetches MAX_ROWS rows or hits the end of the rowset
				else if (fCanFetchBackwards && ch == 'p')
				{
					// Fetch backwards
					pcRows = -MAX_ROWS;
				}
				// ref Set pcRows so that the next call to GetNextRows fetches MAX_ROWS rows forward from the current position
				else if (ch == 'n')
				{
					// Fetch forward
					pcRows = MAX_ROWS;
				}
				// Call IRowset::RestartPosition and fetch the first MAX_ROWS rows of the rowset forward from there
				else if (ch == 'r')
				{
					// RestartPosition
					pcRows = MAX_ROWS;
					(hr = pIRowset.RestartPosition(DB_NULL_HCHAPTER)).ThrowIfFailed();

					// Restarting a command may return the DB_S_COMMANDREEXECUTED warning. If this happens, we still want the caller to
					// continue to display data, so we will reset the result code to S_OK
					hr = HRESULT.S_OK;
				}
				// Quit the program
				else if (ch == 'q')
				{
					hr = HRESULT.E_FAIL;
				}
				// Invalid option; go back up and get another character from the user
				else
				{
					continue;
				}

				// Echo the character and stop waiting for input
				Console.Write("{0}\n", ch);
				break;
			}
		}
		catch { }
		return hr;
	}

	/////////////////////////////////////////////////////////////////
	// mySetupBindings
	//
	// This function takes an IUnknown pointer from a Rowset object and creates a bindings array that describes how we want the data we fetch
	// from the Rowset to be laid out in memory. It also calculates the total size of a row so that we can use this to allocate memory for
	// the rows that we will fetch later.
	//
	// For each column in the Rowset, there will be a corresponding element in the bindings array that describes how the provider should
	// transfer the data, including length and status, for that column. This element also specifies the data type that the provider should
	// return the column as. We will bind all columns as DBTYPE_WSTR, with a few exceptions detailed below, as providers are required to
	// support the conversion of their column data to this type in the vast majority of cases. The exception to our binding as DBTYPE_WSTR is
	// if the native column data type is DBTYPE_IUNKNOWN or if the user has requested that BLOB columns be bound as ISequentialStream
	// objects, in which case we will bind those columns as ISequentialStream objects.
	/////////////////////////////////////////////////////////////////
	private static void mySetupBindings(IRowset pUnkRowset, out DBBINDING[] prgBindings, out nuint pcbRowSize)
	{
		//HRESULT hr;
		//uint cColumns;
		//ref DBCOLUMNINFO rgColumnInfo = default;
		//string pStringBuffer = default;
		//IColumnsInfo pIColumnsInfo = default;

		//uint iCol;
		//uint dwOffset;
		//ref DBBINDING rgBindings = default;

		//uint cStorageObjs;
		//bool fMultipleObjs = false;

		// Obtain the column information for the Rowset; from this, we can find out the following information that we need to construct the
		// bindings array:
		// - the number of columns
		// - the ordinal of each column
		// - the precision and scale of numeric columns
		// - the OLE DB data type of the column
		IColumnsInfo pIColumnsInfo = (IColumnsInfo)pUnkRowset;
		pIColumnsInfo.GetColumnInfo(out var rgColumnInfo);

		// Allocate memory for the bindings array; there is a one-to-one mapping between the columns returned from GetColumnInfo and our bindings
		DBBINDING[] rgBindings = new DBBINDING[rgColumnInfo.Length];

		// Determine if the Rowset supports multiple storage object bindings; if it does not, we will only bind the first BLOB column or
		// IUnknown column as an ISequentialStream object, and will bind the rest as DBTYPE_WSTR
		myGetProperty(pUnkRowset, typeof(IRowset).GUID, DBPROPENUM.DBPROP_MULTIPLESTORAGEOBJECTS, DBPROPSET_ROWSET, out var fMultipleObjs);

		// Construct the binding array element for each column
		nuint dwOffset = 0, cStorageObjs = 0;
		for (uint iCol = 0; iCol < rgColumnInfo.Length; iCol++)
		{
			// This binding applies to the ordinal of this column
			rgBindings[iCol].iOrdinal = rgColumnInfo[iCol].iOrdinal;

			// We are asking the provider to give us the data for this column (DBPART_VALUE), the length of that data (DBPART_LENGTH), and
			// the status of the column (DBPART_STATUS)
			rgBindings[iCol].dwPart = DBPART.DBPART_VALUE | DBPART.DBPART_LENGTH | DBPART.DBPART_STATUS;

			// The following values are the offsets to the status, length, and data value that the provider will fill with the appropriate
			// values when we fetch data later. When we fetch data, we will pass a pointer to a buffer that the provider will copy column
			// data to, in accordance with the binding we have provided for that column; these are offsets into that future buffer
			rgBindings[iCol].obStatus = dwOffset;
			rgBindings[iCol].obLength = dwOffset + InteropExtensions.SizeOf<DBSTATUS>();
			rgBindings[iCol].obValue = dwOffset + InteropExtensions.SizeOf<DBSTATUS>() + InteropExtensions.SizeOf<uint>();

			// Any memory allocated for the data value will be owned by us, the client. Note that no data will be allocated in this case, as
			// the DBTYPE_WSTR bindings we are using will tell the provider to simply copy data directly into our provided buffer
			rgBindings[iCol].dwMemOwner = DBMEMOWNER.DBMEMOWNER_CLIENTOWNED;

			// This is not a parameter binding
			rgBindings[iCol].eParamIO = DBPARAMIO.DBPARAMIO_NOTPARAM;

			// We want to use the precision and scale of the column
			rgBindings[iCol].bPrecision = rgColumnInfo[iCol].bPrecision;
			rgBindings[iCol].bScale = rgColumnInfo[iCol].bScale;

			// Bind this column as DBTYPE_WSTR, which tells the provider to copy a Unicode string representation of the data into our buffer,
			// converting from the native type if necessary
			rgBindings[iCol].wType = DBTYPE.DBTYPE_WSTR;

			// Initially, we set the length for this data in our buffer to 0; the correct value for this will be calculated directly below
			rgBindings[iCol].cbMaxLen = 0;

			// Determine the maximum number of bytes required in our buffer to contain the Unicode string representation of the provider's
			// native data type, including room for the default-termination character
			rgBindings[iCol].cbMaxLen = rgColumnInfo[iCol].wType switch
			{
				DBTYPE.DBTYPE_NULL or DBTYPE.DBTYPE_EMPTY or DBTYPE.DBTYPE_I1 or DBTYPE.DBTYPE_I2 or DBTYPE.DBTYPE_I4 or DBTYPE.DBTYPE_UI1 or DBTYPE.DBTYPE_UI2 or DBTYPE.DBTYPE_UI4 or DBTYPE.DBTYPE_R4 or DBTYPE.DBTYPE_BOOL or DBTYPE.DBTYPE_I8 or DBTYPE.DBTYPE_UI8 or DBTYPE.DBTYPE_R8 or DBTYPE.DBTYPE_CY or DBTYPE.DBTYPE_ERROR => (25 + 1) * 2,// When the above types are converted to a string, they
																																																																																					   // will
																																																																																					   // all
																																																																																					   // fit
																																																																																					   // into
																																																																																					   // 25
																																																																																					   // characters,
																																																																																					   // so
																																																																																					   // use
																																																																																					   // that
																																																																																					   // plus
																																																																																					   // space
																																																																																					   // for
																																																																																					   // the
																																																																																					   // NULL-terminator sizeof(WCHAR);
				DBTYPE.DBTYPE_DECIMAL or DBTYPE.DBTYPE_NUMERIC or DBTYPE.DBTYPE_DATE or DBTYPE.DBTYPE_DBDATE or DBTYPE.DBTYPE_DBTIMESTAMP or DBTYPE.DBTYPE_GUID => (50 + 1) * 2,// Converted to a string, the above types will all fit into
																																												// 50 characters, so use that plus space for the terminator
																																												//sizeof(WCHAR)
				DBTYPE.DBTYPE_BYTES => (rgColumnInfo[iCol].ulColumnSize * 2 + 1) * 2,// In converting DBTYPE_BYTES to a string, each byte
																					 // becomes two characters (e.g. 0xFF . "FF"), so we will
																					 // use double the maximum size of the column plus
																					 // include space for the NULL-terminator sizeof(WCHAR);
				DBTYPE.DBTYPE_STR or DBTYPE.DBTYPE_WSTR or DBTYPE.DBTYPE_BSTR => (rgColumnInfo[iCol].ulColumnSize + 1) * 1,// Going from a string to our string representation,
																														   // we can just
																														   // take the
																														   // maximum size of
																														   // the column, a
																														   // count of
																														   // characters, and
																														   // include space
																														   // for the include
																														   // space for the
																														   // NULL-terminator Marshal.SizeOf(typeof(ushort));
				_ => MAX_COL_SIZE,// For any other type, we will simply use our maximum
								  // column buffer size, since the display size of these columns may be variable (e.g. DBTYPE_VARIANT) or
								  // unknown (e.g. provider-specific types)
			};

			// If the provider's native data type for this column is DBTYPE_IUNKNOWN or this is a BLOB column and the user has requested that
			// we bind BLOB columns as ISequentialStream objects, bind this column as an ISequentialStream object if the provider supports
			// our creating another ISequentialStream binding
			if ((rgColumnInfo[iCol].wType == DBTYPE.DBTYPE_IUNKNOWN ||
				rgColumnInfo[iCol].dwFlags.IsFlagSet(DBCOLUMNFLAGS.DBCOLUMNFLAGS_ISLONG) &&
				g_dwFlags.IsFlagSet(Flags.USE_ISEQSTREAM)) &&
				(fMultipleObjs || cStorageObjs == 0))
			{
				// To create an ISequentialStream object, we will bind this column as DBTYPE_IUNKNOWN to indicate that we are requesting this
				// column as an object
				rgBindings[iCol].wType = DBTYPE.DBTYPE_IUNKNOWN;

				// We want to allocate enough space in our buffer for the ISequentialStream pointer we will obtain from the provider
				rgBindings[iCol].cbMaxLen = (uint)IntPtr.Size;

				// Direct the provider to create an ISequentialStream object over the data for this column We want read access on the
				// ISequentialStream object that the provider will create for us
				DBOBJECT obj = new() { iid = typeof(ISequentialStream).GUID, dwFlags = (uint)STGM.STGM_READ };

				// To specify the type of object that we want from the provider, we need to create a DBOBJECT structure and place it in our
				// binding for this column
				Marshal.StructureToPtr(obj, rgBindings[iCol].pObject, false);

				// Keep track of the number of storage objects (ISequentialStream is a storage interface) that we have requested, so that we
				// can avoid requesting multiple storage objects from a provider that only supports a single storage object in our bindings
				cStorageObjs++;
			}

			// Ensure that the bound maximum length is no more than the maximum column size in bytes that we've defined
			rgBindings[iCol].cbMaxLen = Math.Min(rgBindings[iCol].cbMaxLen, MAX_COL_SIZE);

			// Update the offset past the end of this column's data, so that the next column will begin in the correct place in the buffer
			dwOffset = (uint)rgBindings[iCol].cbMaxLen + rgBindings[iCol].obValue;

			// Ensure that the data for the next column will be correctly aligned for all platforms, or, if we're done with columns, that if
			// we allocate space for multiple rows that the data for every row is correctly aligned
			dwOffset = ROUNDUP(dwOffset);
		}

		// Return the row size (the current dwOffset is the size of the row), the count of bindings, and the bindings array to the ref caller
		pcbRowSize = dwOffset;
		prgBindings = rgBindings;
	}

	/////////////////////////////////////////////////////////////////
	// myInteractWithRowset
	//
	// This function allows the user to interact with the rowset. It prompts the user appropriately, gets the user's input, may call
	// IRowset::RestartPosition if the user requests a restart, and will copy data from a selected row to the selection buffer.
	/////////////////////////////////////////////////////////////////
	/*STAY*/

	/////////////////////////////////////////////////////////////////
	// myUpdateDisplaySize
	//
	// This function updates the rgDispSize array, keeping the maximum of the display size needed for the given data and the previous maximum
	// size already in the array.
	/////////////////////////////////////////////////////////////////
	private static void myUpdateDisplaySize(DBBINDING[] rgBindings, IntPtr pData, uint[] rgDispSize)
	{
		//DBSTATUS dwStatus;
		//uint cchLength;
		//uint iCol;

		// Loop through the bindings, comparing the size of each column against the previously found maximum size for that column
		for (int iCol = 0; iCol < rgBindings.Length; iCol++)
		{
			DBSTATUS dwStatus = (pData + (int)rgBindings[iCol].obStatus).Convert<DBSTATUS>(4);
			uint cchLength = (pData + (int)rgBindings[iCol].obLength).Convert<uint>(4) / 2;

			// The length that we need to display depends on the status of this column and generally on the data in the column
			switch (dwStatus)
			{
				case DBSTATUS.DBSTATUS_S_ISNULL:
					cchLength = 6; // "(null)"
					break;

				case DBSTATUS.DBSTATUS_S_TRUNCATED:
				case DBSTATUS.DBSTATUS_S_OK:
				case DBSTATUS.DBSTATUS_S_DEFAULT:
					if (rgBindings[iCol].wType == DBTYPE.DBTYPE_IUNKNOWN)
						cchLength = 2 + 8; // "0x%08lx"

					// Ensure that the length is at least the minimum display size
					cchLength = Math.Max(cchLength, MIN_DISPLAY_SIZE);
					break;

				default:
					cchLength = 14; // "(error status)"
					break;
			}

			if (rgDispSize[iCol] < cchLength)
				rgDispSize[iCol] = cchLength;
		}
	}
}