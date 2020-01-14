using System;
using System.Data.OleDb;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.SearchApi;
using static Vanara.PInvoke.ShlwApi;

namespace SearchEvents
{
    class evtdemo
    {
        static void Main(string[] args)
        {
            if ((args.Length < 1) || (args.Length == 1 && (args[0] == "-?")) || args[0] == "/?")
            {
                Console.Write("Allows monitoring and prioritization of indexer URLs.\n\n");
                Console.Write("Eventing [drive:][path] [/p[:]priority] [/t[:]duration]\n\n");
                Console.Write("  [drive:][path]\n");
                Console.Write("             Specifies drive and directory of location to watch\n");
                Console.Write("  /p         Prioritizes indexing at the given speed\n");
                Console.Write("  priority     F  Foreground      H High\n");
                Console.Write("               L  Low             D Default\n");
                Console.Write("  /t         Specifies how long in MS to monitor query\n");
                Console.Write("  duration     0     Until all content is indexed\n");
                Console.Write("               NNNN  Monitor for NNNN milliseconds\n\n");
            }
            else if (CoInitializeEx(default, COINIT.COINIT_MULTITHREADED | COINIT.COINIT_DISABLE_OLE1DDE).Succeeded)
            {
                HRESULT hr = HRESULT.S_OK;
                uint dwTimeout = (300 * 1000);                 // default 5 mins
                PRIORITY_LEVEL priority = PRIORITY_LEVEL.PRIORITY_LEVEL_DEFAULT;
                string wszURL = "";

                for (int nArg = 0; (nArg < args.Length && hr.Succeeded); nArg++)
                {
                    if (args[nArg][0] == '/')
                    {
                        if (args[nArg].ToLower()[1] == 'p')
                        {
                            var pwsz = args[nArg].Substring(2);
                            if (pwsz[0] == ':')
                            {
                                pwsz = pwsz.Substring(1);
                            }
                            switch (pwsz.ToLower()[0])
                            {
                                case 'f':
                                    priority = PRIORITY_LEVEL.PRIORITY_LEVEL_FOREGROUND;
                                    break;
                                case 'h':
                                    priority = PRIORITY_LEVEL.PRIORITY_LEVEL_HIGH;
                                    break;
                                case 'l':
                                    priority = PRIORITY_LEVEL.PRIORITY_LEVEL_LOW;
                                    break;
                                case 'd':
                                    priority = PRIORITY_LEVEL.PRIORITY_LEVEL_DEFAULT;
                                    break;
                                default:
                                    hr = HRESULT.E_INVALIDARG;
                                    break;
                            }
                        }
                        else if (args[nArg].ToLower()[1] == 't')
                        {
                            var pwsz = args[nArg].Substring(2);
                            if (pwsz[0] == ':')
                            {
                                pwsz = pwsz.Substring(1);
                            }
                            dwTimeout = uint.Parse(pwsz);
                        }
                        else
                        {
                            hr = HRESULT.E_INVALIDARG;
                        }
                    }
                    else
                    {
                        wszURL = args[nArg];
                    }
                }

                if (hr.Succeeded)
                {
                    var wszQuerySQL = !string.IsNullOrEmpty(wszURL) ? $"SELECT workid FROM SystemIndex WHERE SCOPE='{wszURL}'" : "SELECT workid FROM SystemIndex";
                    WatchEvents(wszQuerySQL, priority, dwTimeout);
                }

                CoUninitialize();
            }
        }

        //*****************************************************************************
        // Watches events on the given query with the given priority for a period of
        // time.  If dwTimeout == 0, then it will monitor until all items are indexed
        // within the query.  Otherwise, it monitors for dwTimeout MS.

        static void WatchEvents(string pwszQuerySQL, PRIORITY_LEVEL priority, uint dwTimeout)
        {
            IRowsetPrioritization spRowsetPrioritization;
            CRowsetEventListener spListener;

            HRESULT hr = OpenSession(out var spDBCreateCommand);
            if (hr.Succeeded)
            {
                hr = ExecuteQuery(spDBCreateCommand, pwszQuerySQL, true, typeof(IRowset).GUID, out var spRowset));
            }
            if (hr.Succeeded)
            {
                spRowsetPrioritization = (IRowsetPrioritization)spRowset;
            }
            if (hr.Succeeded)
            {
                hr = CreateComObject(&spListener);
            }
            if (hr.Succeeded)
            {
                spListener.spDBCreateCommand = spDBCreateCommand;

                uint dwAdviseID = 0;
                hr = ConnectToConnectionPoint(spListener.GetUnknown(), typeof(IRowsetEvents).GUID, true, spRowset, ref dwAdviseID, out _);
                if (hr.Succeeded)
                {
                    spRowsetPrioritization.GetScopeStatistics(out var indexedDocumentCount, out var oustandingAddCount, out var oustandingModifyCount);

                    Console.Write("Prioritization and Eventing Demo\n\n");
                    Console.Write("Query:               %S\n\n", pwszQuerySQL);
                    Console.Write("Indexed Docs:        %u\n", indexedDocumentCount);
                    Console.Write("Oustanding Adds:     %u\n", oustandingAddCount);
                    Console.Write("Oustanding Modifies: %u\n\n", oustandingModifyCount);
                    Console.Write("Setting Priority:    %S\n\n", PriorityLevelToString(priority));
                    Console.Write("Now monitoring events for this query...\n\n");

                    spRowsetPrioritization.SetScopePriority(priority, 1000);

                    if (dwTimeout == 0)
                    {
                        while (hr.Succeeded && ((oustandingAddCount > 0) || (oustandingModifyCount > 0)))
                        {
                            Sleep(1000);
                            hr = spRowsetPrioritization.GetScopeStatistics(out indexedDocumentCount, out oustandingAddCount, out oustandingModifyCount);
                        }
                    }
                    else
                    {
                        Sleep(dwTimeout);
                    }

                    ConnectToConnectionPoint(spListener.GetUnknown(), typeof(IRowsetEvents).Guid, false, spRowset, &dwAdviseID, default);
                }
            }

            if (hr.Failed)
            {
                Console.Write("Failure: %08X\n", hr);
            }
        }

        //*****************************************************************************
        // Open a database session...

        static HRESULT OpenSession(out OleDbConnection pConnection)
        {
            try
            {
                var hlpr = new ISearchCatalogManager();
                pConnection = new OleDbConnection("Provider=Search.CollatorDSO;Extended Properties='Application=Windows';");
                pConnection.Open();
                return HRESULT.S_OK;
            }
            catch (COMException ex)
            {
                pConnection = null;
                return ex.HResult;
            }
        }

        //*****************************************************************************
        // Run a query against the database, optionally enabling eventing...

        static HRESULT ExecuteQuery(OleDbConnection pConnection, string pwszQuerySQL, bool fEnableEventing, out OleDbDataReader rdr)
        {
            if (!pwszQuerySQL.TrimEnd().EndsWith(" FROM SYSTEMINDEX", StringComparison.InvariantCultureIgnoreCase))
                pwszQuerySQL = pwszQuerySQL.TrimEnd() + " FROM SYSTEMINDEX";
            var cmd = new OleDbCommand(pwszQuerySQL);
            cmd.Parameters.AddWithValue()
            rdr = cmd.ExecuteReader();
            HRESULT hr = pDBCreateCommand..CreateCommand(0, typeof(ICommand).Guid, &spUnknownCommand);
            if (SUCCEEDED(hr))
            {
                hr = spUnknownCommand.QueryInterface(&spCommandProperties);
            }
            if (SUCCEEDED(hr))
            {
                DBPROP rgProps[2] = { };
                DBPROPSET propSet = { };

                rgProps[propSet.cProperties].dwPropertyID = DBPROP_USEEXTENDEDDBTYPES;
                rgProps[propSet.cProperties].dwOptions = DBPROPOPTIONS_OPTIONAL;
                rgProps[propSet.cProperties].vValue.vt = VT_BOOL;
                rgProps[propSet.cProperties].vValue.boolVal = VARIANT_TRUE;
                propSet.cProperties++;

                if (fEnableEventing)
                {
                    rgProps[propSet.cProperties].dwPropertyID = DBPROP_ENABLEROWSETEVENTS;
                    rgProps[propSet.cProperties].dwOptions = DBPROPOPTIONS_OPTIONAL;
                    rgProps[propSet.cProperties].vValue.vt = VT_BOOL;
                    rgProps[propSet.cProperties].vValue.boolVal = VARIANT_TRUE;
                    propSet.cProperties++;
                }

                propSet.rgProperties = rgProps;
                static const GUID guidQueryExt = DBPROPSET_QUERYEXT;
                propSet.guidPropertySet = guidQueryExt;

                hr = spCommandProperties.SetProperties(1, &propSet);
            }
            if (SUCCEEDED(hr))
            {
                hr = spUnknownCommand.QueryInterface(&spCommandText);
            }
            if (SUCCEEDED(hr))
            {
                hr = spCommandText.SetCommandText(DBGUID_DEFAULT, pwszQuerySQL);
            }
            if (SUCCEEDED(hr))
            {
                DBROWCOUNT cRows;
                hr = spCommandText.Execute(default, riid, default, &cRows, reinterpret_cast<IUnknown**>(ppv));
            }

            return hr;
        }


        //*****************************************************************************
        // Retrieves the URL from a given workid

        HRESULT RetrieveURL(IDBCreateCommand pDBCreateCommand, in PROPVARIANT itemID, out string pwszURL)
        {
            WCHAR wszQuery[512];
            CComPtr<IRowset> spRowset;

            HRESULT hr = (itemID.vt == VT_UI4) ? S_OK : E_INVALIDARG;
            if (SUCCEEDED(hr))
            {
                hr = StringCchPrintf(wszQuery, ARRAYSIZE(wszQuery), "SELECT TOP 1 System.ItemUrl FROM SystemIndex WHERE workid=%u", itemID.ulVal);
            }
            if (SUCCEEDED(hr))
            {
                hr = ExecuteQuery(pDBCreateCommand, wszQuery, false, IID_PPV_ARGS(&spRowset));
            }
            if (SUCCEEDED(hr))
            {
                CComPtr<IGetRow> spGetRow;
                DBCOUNTITEM ciRowsRetrieved = 0;
                HROW hRow = default;
                HROW* phRow = &hRow;
                CComPtr<IPropertyStore> spPropertyStore;

                hr = spRowset.GetNextRows(DB_NULL_HCHAPTER, 0, 1, &ciRowsRetrieved, &phRow);
                if (SUCCEEDED(hr))
                {
                    hr = spRowset.QueryInterface(&spGetRow);
                    if (SUCCEEDED(hr))
                    {
                        CComPtr<IUnknown> spUnknownPropertyStore;
                        hr = spGetRow.GetRowFromHROW(default, hRow, typeof(IPropertyStore).Guid, &spUnknownPropertyStore);
                        if (SUCCEEDED(hr))
                        {
                            hr = spUnknownPropertyStore.QueryInterface(&spPropertyStore);
                        }
                    }
                    if (SUCCEEDED(hr))
                    {
                        PROPVARIANT var = { };
                        hr = spPropertyStore.GetValue(PKEY_ItemUrl, &var);
                        if (SUCCEEDED(hr))
                        {
                            if (var.vt == VT_LPWSTR)
                            {
                                hr = StringCchCopy(pwszURL, cchURL, var.pwszVal);
                            }
                            else
                            {
                                hr = E_INVALIDARG;
                            }
                    ::PropVariantClear(&var);
                        }
                    }

                    spRowset.ReleaseRows(ciRowsRetrieved, phRow, default, default, default);
                }
            }

            return hr;
        }

        //*****************************************************************************

        string ItemStateToString(ROWSETEVENT_ITEMSTATE itemState)
        {
            switch (itemState)
            {
                case ROWSETEVENT_ITEMSTATE_NOTINROWSET:
                    return "NotInRowset";
                case ROWSETEVENT_ITEMSTATE_INROWSET:
                    return "InRowset";
                case ROWSETEVENT_ITEMSTATE_UNKNOWN:
                    return "Unknown";
            }
            return "";
        }

        //*****************************************************************************

        string PriorityLevelToString(PRIORITY_LEVEL priority)
        {
            switch (priority)
            {
                case PRIORITY_LEVEL_FOREGROUND:
                    return "Foreground";
                case PRIORITY_LEVEL_HIGH:
                    return "High";
                case PRIORITY_LEVEL_LOW:
                    return "Low";
                case PRIORITY_LEVEL_DEFAULT:
                    return "Default";
            }
            return "";
        }
    }
}
