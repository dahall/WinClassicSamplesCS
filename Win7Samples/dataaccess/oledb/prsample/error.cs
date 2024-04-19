using System.Runtime.CompilerServices;
using Vanara.Extensions;
using static Vanara.PInvoke.OleAut32;

namespace PRSample;

public static partial class Program
{
	private static readonly LCID g_ulcid = LCID.LOCALE_USER_DEFAULT;

	//public static void XCHECK_HR(HRESULT hr, [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
	//{
	//	if (g_dwFlags.IsFlagSet(Flags.DISPLAY_METHODCALLS))
	//		Console.Error.WriteLine(hr);
	//	myHandleResult(hr, sourceFilePath, sourceLineNumber);
	//}

	////////////////////////////////////////////////////////////////////////
	// myDisplayErrorInfo
	//
	// This function displays basic error information for an error object that doesn't support the IErrorRecords interface
	////////////////////////////////////////////////////////////////////////
	private static void myDisplayErrorInfo(HRESULT hrReturned, IErrorInfo pIErrorInfo, [CallerFilePath] string pwszFile = "", [CallerLineNumber] int ulLine = 0)
	{
		// Get the description of the error
		pIErrorInfo.GetDescription(out var bstrDescription).ThrowIfFailed();

		// Get the source of the error -- this will be the window title
		pIErrorInfo.GetSource(out var bstrSource).ThrowIfFailed();

		// Display this error information
		Console.Write("\nErrorInfo: HResult: 0x{0:x8}\nDescription: {1}\nSource: {2}\nFile: {3}, Line: {4}\n",
			hrReturned, bstrDescription, bstrSource, pwszFile, ulLine);
	}

	////////////////////////////////////////////////////////////////////////
	// myDisplayErrorRecord
	//
	// This function displays the error information for a single error record, including information from ISQLErrorInfo, if supported
	////////////////////////////////////////////////////////////////////////
	private static void myDisplayErrorRecord(HRESULT hrReturned, uint iRecord, IErrorRecords pIErrorRecords, [CallerFilePath] string pwszFile = "", [CallerLineNumber] int ulLine = 0)
	{
		// Get the IErrorInfo interface pointer for this error record
		IErrorInfo pIErrorInfo = pIErrorRecords.GetErrorInfo(iRecord, g_ulcid);

		// Get the description of this error
		pIErrorInfo.GetDescription(out var bstrDescription).ThrowIfFailed();

		// Get the source of this error
		pIErrorInfo.GetSource(out var bstrSource).ThrowIfFailed();

		// Get the basic error information for this record
		ERRORINFO ErrorInfo = pIErrorRecords.GetBasicErrorInfo(iRecord);

		// If the error object supports ISQLErrorInfo, get this information
		myGetSqlErrorInfo(iRecord, pIErrorRecords, out var bstrSQLInfo, out _);

		// Display the error information to the user
		if (bstrSQLInfo is not null)
		{
			Console.Write("\nErrorRecord: HResult: 0x{0:x8}\nDescription: {1}}\nSQLErrorInfo: {2}\nSource: {3}\nFile: {4}, Line: {5}\n",
				(int)ErrorInfo.hrError, bstrDescription, bstrSQLInfo, bstrSource, pwszFile, ulLine);
		}
		else
		{
			Console.Write("\nErrorRecord: HResult: 0x{0:x8}\nDescription: {1}\nSource: {2}\nFile: {3}, Line: {4}\n",
				(int)ErrorInfo.hrError, bstrDescription, bstrSource, pwszFile, ulLine);
		}
	}

	////////////////////////////////////////////////////////////////////////
	// myGetSqlErrorInfo
	//
	// If the error object supports ISQLErrorInfo, get the SQL error string and native error code for this error
	////////////////////////////////////////////////////////////////////////
	private static void myGetSqlErrorInfo(uint iRecord, IErrorRecords pIErrorRecords, [MarshalAs(UnmanagedType.BStr)] out string? pBstr, out int plNativeError)
	{
		// Attempt to get the ISQLErrorInfo interface for this error record through GetCustomErrorObject. Note that ISQLErrorInfo is not
		// mandatory, so failure is acceptable here
		pIErrorRecords.GetCustomErrorObject(iRecord, //iRecord
			typeof(ISQLErrorInfo).GUID, //riid
			out var pISQLErrorInfo); //ppISQLErrorInfo

		// If we obtained the ISQLErrorInfo interface, get the SQL error string and native error code for this error
		if (pISQLErrorInfo is not null)
			((ISQLErrorInfo)pISQLErrorInfo).GetSQLInfo(out pBstr, out plNativeError);
		else { pBstr = null; plNativeError = 0; }
	}

	////////////////////////////////////////////////////////////////////////
	// myHandleResult
	//
	// This function is called as part of the XCHECK_HR macro; it takes a HRESULT, which is returned by the method called in the XCHECK_HR
	// macro, and the file and line number where the method call was made. If the method call failed, this function attempts to get and
	// display the extended error information for the call from the IErrorInfo, IErrorRecords, and ISQLErrorInfo interfaces.
	////////////////////////////////////////////////////////////////////////
	private static HRESULT myHandleResult(HRESULT hrReturned, [CallerFilePath] string pwszFile = "", [CallerLineNumber] int ulLine = 0)
	{
		HRESULT hr;

		// If the method called as part of the XCHECK_HR macro failed, we will attempt to get extended error information for the call
		if (hrReturned.Failed)
		{
			// Obtain the current Error object, if any, by using the OLE Automation GetErrorInfo function, which will give us back an
			// IErrorInfo interface pointer if successful
			hr = GetErrorInfo(0, out var pIErrorInfo);

			// We've got the IErrorInfo interface pointer on the Error object
			if (hr.Succeeded && pIErrorInfo is not null)
			{
				// OLE DB extends the OLE Automation error model by allowing Error objects to support the IErrorRecords interface; this
				// interface can expose information on multiple errors.
				IErrorRecords? pIErrorRecords = pIErrorInfo as IErrorRecords;
				if (pIErrorRecords is not null)
				{
					// Get the count of error records from the object
					var cRecords = pIErrorRecords.GetRecordCount();

					// Loop through the set of error records and display the error information for each one
					for (uint iErr = 0; iErr < cRecords; iErr++)
					{
						myDisplayErrorRecord(hrReturned, iErr, pIErrorRecords, pwszFile, ulLine);
					}
				}
				// The object didn't support IErrorRecords; display the error information for this single error
				else
				{
					myDisplayErrorInfo(hrReturned, pIErrorInfo, pwszFile, ulLine);
				}
			}
			// There was no Error object, so just display the HRESULT to the user
			else
			{
				Console.Write("\nNo Error Info posted; HResult: 0x{0:x}\nFile: {1}, Line: {2}\n", (int)hrReturned, pwszFile, ulLine);
			}
		}

		return hrReturned;
	}
}