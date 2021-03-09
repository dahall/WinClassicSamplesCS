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
    static class evtdemo
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
            OleDbDataReader spRowset = null;
            IRowsetPrioritization spRowsetPrioritization;
            CRowsetEventListener spListener;

            var hr = OpenSession(out var spDBCreateCommand);
            if (hr.Succeeded)
                hr = ExecuteQuery(spDBCreateCommand, pwszQuerySQL, true, out spRowset);

            if (hr.Succeeded)
                spRowsetPrioritization = (IRowsetPrioritization)Marshal.GetObjectForIUnknown(Marshal.GetComInterfaceForObject<OleDbDataReader, IRowsetPrioritization>(spRowset));

            if (hr.Succeeded)
            {
                spListener = new CRowsetEventListener(spDBCreateCommand);

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
                //var hlpr = new ISearchCatalogManager();
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
            // TODO: Add properties DBPROP_USEEXTENDEDDBTYPES = true, DBPROP_ENABLEROWSETEVENTS = true
            rdr = cmd.ExecuteReader();
        }


        //*****************************************************************************
        // Retrieves the URL from a given workid

        static HRESULT RetrieveURL(OleDbConnection pDBCreateCommand, in PROPVARIANT itemID, out string pwszURL)
        {
            OleDbDataReader spRowset;

            HRESULT hr = (itemID.vt == VARTYPE.VT_UI4) ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
            if (hr.Succeeded)
            {
                var wszQuery = $"SELECT TOP 1 System.ItemUrl FROM SystemIndex WHERE workid={itemID.ulVal}";
                hr = ExecuteQuery(pDBCreateCommand, wszQuery, false, out spRowset);
            }
            if (hr.Succeeded)
            {
                CComPtr<IGetRow> spGetRow;
                DBCOUNTITEM ciRowsRetrieved = 0;
                HROW hRow = default;
                HROW* phRow = &hRow;
                CComPtr<IPropertyStore> spPropertyStore;

                hr = spRowset.GetNextRows(DB_NULL_HCHAPTER, 0, 1, &ciRowsRetrieved, &phRow);
                if (hr.Succeeded)
                {
                    hr = spRowset.QueryInterface(&spGetRow);
                    if (hr.Succeeded)
                    {
                        CComPtr<IUnknown> spUnknownPropertyStore;
                        hr = spGetRow.GetRowFromHROW(default, hRow, typeof(IPropertyStore).Guid, &spUnknownPropertyStore);
                        if (hr.Succeeded)
                        {
                            hr = spUnknownPropertyStore.QueryInterface(&spPropertyStore);
                        }
                    }
                    if (hr.Succeeded)
                    {
                        PROPVARIANT var = { };
                        hr = spPropertyStore.GetValue(PKEY_ItemUrl, &var);
                        if (hr.Succeeded)
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

        static string ItemStateToString(ROWSETEVENT_ITEMSTATE itemState) => itemState switch
        {
            ROWSETEVENT_ITEMSTATE.ROWSETEVENT_ITEMSTATE_NOTINROWSET => "NotInRowset",
            ROWSETEVENT_ITEMSTATE.ROWSETEVENT_ITEMSTATE_INROWSET => "InRowset",
            ROWSETEVENT_ITEMSTATE.ROWSETEVENT_ITEMSTATE_UNKNOWN => "Unknown",
            _ => "",
        };

        //*****************************************************************************

        static string PriorityLevelToString(PRIORITY_LEVEL priority) => priority switch
        {
            PRIORITY_LEVEL.PRIORITY_LEVEL_FOREGROUND => "Foreground",
            PRIORITY_LEVEL.PRIORITY_LEVEL_HIGH => "High",
            PRIORITY_LEVEL.PRIORITY_LEVEL_LOW => "Low",
            PRIORITY_LEVEL.PRIORITY_LEVEL_DEFAULT => "Default",
            _ => "",
        };

        [ComVisible(true)]
        public class CRowsetEventListener : IRowsetEvents
        {
            OleDbConnection spDBCreateCommand;

            public CRowsetEventListener(OleDbConnection conn)
            {
                spDBCreateCommand = conn;
            }

            void IRowsetEvents.OnNewItem(PROPVARIANT itemID, ROWSETEVENT_ITEMSTATE newItemState)
            {
                // This event is received when the indexer has completed indexing of a NEW item that falls within the
                // scope of your query.  If your query is for C:\users, then only newly indexed items within C:\users
                // will be given.

                Console.Write("OnNewItem( newItemState: {0} )\n\t\t", ItemStateToString(newItemState));
                PrintURL(itemID);
            }

            void IRowsetEvents.OnChangedItem(PROPVARIANT itemID, ROWSETEVENT_ITEMSTATE rowsetItemState, ROWSETEVENT_ITEMSTATE changedItemState)
            {
                // This event is received when the indexer has completed re-indexing of an item that was already in
                // the index that falls within the scope of your query.  The rowsetItemState parameter indicates the
                // state of the item regarding your query when it was initially executed.  The changedItemState
                // represents the state of the item following reindexing.

                Console.Write("OnChangedItem( rowsetItemState: {0} changedItemState: {1} )\n\t\t", ItemStateToString(rowsetItemState), ItemStateToString(changedItemState));
                PrintURL(itemID);
            }

            void IRowsetEvents.OnDeletedItem(PROPVARIANT itemID, ROWSETEVENT_ITEMSTATE deletedItemState)
            {
                // This event is received when the indexer has completed deletion of an item that was already in
                // the index that falls within the scope of your query.  Note that the item may not have been in your
                // original query even if the original query was solely scope-based if the item was added following
                // your query.

                Console.Write("OnDeletedItem( deletedItemState: {0} )\n\t\t", ItemStateToString(deletedItemState));
                PrintURL(itemID);
            }

            void IRowsetEvents.OnRowsetEvent(ROWSETEVENT_TYPE eventType, PROPVARIANT eventData)
            {
                switch (eventType)
                {
                    case ROWSETEVENT_TYPE.ROWSETEVENT_TYPE_DATAEXPIRED:
                        // This event signals that your rowset is no longer valid, so further calls made to the rowset
                        // will fail.  This can happen if the client (your application) loses its connection to the
                        // indexer.  Indexer restarts or network problems with remote queries could cause this.

                        Console.Write("OnRowsetEvent( ROWSETEVENT_TYPE_DATAEXPIRED )\n\t\tData backing the rowset has expired.  Requerying is needed.\n");
                        break;

                    case ROWSETEVENT_TYPE.ROWSETEVENT_TYPE_FOREGROUNDLOST:
                        // This event signals that a previous request for PRIORITY_LEVEL_FOREGROUND made on this rowset
                        // has been downgraded to PRIORITY_LEVEL_HIGH.  The most likely cause of this is another query
                        // having requested foreground prioritization.  The indexer treats prioritization requests as a
                        // stack where only the top request on the stack may have foreground priority.

                        Console.Write("OnRowsetEvent( ROWSETEVENT_TYPE_FOREGROUNDLOST )\n\t\tForeground priority has been downgraded to high priority.\n");
                        break;

                    case ROWSETEVENT_TYPE.ROWSETEVENT_TYPE_SCOPESTATISTICS:
                        // This informational event is sent periodically when there has been a prioritization request for
                        // any value other than PRIORITY_LEVEL_DEFAULT.  This event allows tracking indexing progress in
                        // response to a prioritization reqeust.

                        Console.Write("OnRowsetEvent( ROWSETEVENT_TYPE_SCOPESTATISTICS )\n\t\tStatistics( indexedDocs:{0} docsToAddCount:{1} docsToReindexCount: {2} )\n", eventData.caul.pElems[0], eventData.caul.pElems[1], eventData.caul.pElems[2]);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            void PrintURL(PROPVARIANT itemID)
            {
                string wszURL = "";
                HRESULT hr = itemID.vt == VARTYPE.VT_UI4 ? HRESULT.S_OK : HRESULT.E_INVALIDARG;
                if (hr.Succeeded)
                {
                    hr = RetrieveURL(spDBCreateCommand, itemID, out wszURL);
                    if (hr.Failed)
                    {
                        // It's possible that for some items we won't be able to retrieve the URL.
                        // This can happen when our application doesn't have sufficient priveledges to read the URL
                        // or if the URL has been deleted from the system.

                        wszURL = "URL-Lookup-NotFound";
                    }
                }
                if (hr.Succeeded)
                    Console.Write("workid: {0};  URL: {1}\n", itemID.ulVal, wszURL);
                else
                    throw new InvalidOperationException();
            }
        }
    }
}