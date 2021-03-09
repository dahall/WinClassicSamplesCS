﻿using System;
using System.Data.OleDb;
using static Vanara.PInvoke.SearchApi;

namespace Microsoft.Samples.WindowsSearch.DSearch
{
    /// <summary>
    /// This class is simply a static console application to query the Windows Search using interop for ISearchQueryHelper
    /// </summary>
    class DSearch
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ds [file search pattern] [userQuery]");
                Console.WriteLine("[file search pattern] can include '*' and '?' wildcards");
                Console.WriteLine("[userQuery] query to look for inside the document (with operators of OR, AND). Optional.");
                return;
            }

            // First parameter is file name pattern
            var pattern = args[0];
            var userQuery = " ";
            // Everything else is considered to be a search query
            for (var i = 1; i < args.Length; i++)
            {
                userQuery += args[i] + " ";
            }

            // This uses the Microsoft.Search.Interop assembly
            var manager = new ISearchManager();

            // SystemIndex catalog is the default catalog in Windows
            var catalogManager = manager.GetCatalog("SystemIndex");

            // Get the ISearchQueryHelper which will help us to translate AQS --> SQL necessary to query the indexer
            var queryHelper = catalogManager.GetQueryHelper();

            // Set the number of results we want. Don't set this property if all results are needed.
            queryHelper.QueryMaxResults = 10;

            // Set list of columns we want
            queryHelper.QuerySelectColumns = "System.ItemPathDisplay";

            // Set additional query restriction
            queryHelper.QueryWhereRestrictions = "AND scope='file:'";

            // convert file pattern if it is not '*'. Don't create restriction for '*' as it includes all files.
            if (pattern != "*")
            {
                pattern = pattern.Replace("*", "%");
                pattern = pattern.Replace("?", "_");

                if (pattern.Contains("%") || pattern.Contains("_"))
                {
                    queryHelper.QueryWhereRestrictions += " AND System.FileName LIKE '" + pattern + "' ";
                }
                else
                {
                    // if there are no wildcards we can use a contains which is much faster as it uses the index
                    queryHelper.QueryWhereRestrictions += " AND Contains(System.FileName, '" + pattern + "') ";
                }
            }

            // Set sorting order 
            queryHelper.QuerySorting = "System.DateModified DESC";

            // Generate SQL from our parameters, converting the userQuery from AQS->WHERE clause
            var sqlQuery = queryHelper.GenerateSQLFromUserQuery(userQuery);
            Console.WriteLine(sqlQuery);

            // --- Perform the query ---
            // create an OleDbConnection object which connects to the indexer provider with the windows application
            using var conn = new OleDbConnection(queryHelper.ConnectionString);
            // open the connection
            conn.Open();

            // now create an OleDB command object with the query we built above and the connection we just opened.
            using var command = new OleDbCommand(sqlQuery, conn);
            // execute the command, which returns the results as an OleDbDataReader.
            using var WDSResults = command.ExecuteReader();
            while (WDSResults.Read())
            {
                // col 0 is our path in display format
                Console.WriteLine("{0}", WDSResults.GetString(0));
            }
        }
    }
}