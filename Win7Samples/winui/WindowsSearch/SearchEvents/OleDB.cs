#pragma warning disable SYSLIB0004 // Type or member is obsolete
using System;
using System.Data;
using System.Data.Odbc;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using System.Diagnostics;
using System.Globalization;

namespace OleDB;


internal static class ODBC
{

	static internal Exception ConnectionClosed() => ADP.InvalidOperation(Res.GetString(Res.Odbc_ConnectionClosed));

	static internal Exception OpenConnectionNoOwner() => ADP.InvalidOperation(Res.GetString(Res.Odbc_OpenConnectionNoOwner));

	static internal Exception UnknownSQLType(ODBC32.SQL_TYPE sqltype) => ADP.Argument(Res.GetString(Res.Odbc_UnknownSQLType, sqltype.ToString()));
	static internal Exception ConnectionStringTooLong() => ADP.Argument(Res.GetString(Res.OdbcConnection_ConnectionStringTooLong, ODBC32.MAX_CONNECTION_STRING_LENGTH));
	static internal ArgumentException GetSchemaRestrictionRequired() => ADP.Argument(Res.GetString(Res.ODBC_GetSchemaRestrictionRequired));
	static internal ArgumentOutOfRangeException NotSupportedEnumerationValue(Type type, int value) => ADP.ArgumentOutOfRange(Res.GetString(Res.ODBC_NotSupportedEnumerationValue, type.Name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)), type.Name);
	static internal ArgumentOutOfRangeException NotSupportedCommandType(CommandType value)
	{
#if DEBUG
		switch (value)
		{
			case CommandType.Text:
			case CommandType.StoredProcedure:
				Debug.Assert(false, "valid CommandType " + value.ToString());
				break;
			case CommandType.TableDirect:
				break;
			default:
				Debug.Assert(false, "invalid CommandType " + value.ToString());
				break;
		}
#endif
		return ODBC.NotSupportedEnumerationValue(typeof(CommandType), (int)value);
	}
	static internal ArgumentOutOfRangeException NotSupportedIsolationLevel(IsolationLevel value)
	{
#if DEBUG
		switch (value)
		{
			case IsolationLevel.Unspecified:
			case IsolationLevel.ReadUncommitted:
			case IsolationLevel.ReadCommitted:
			case IsolationLevel.RepeatableRead:
			case IsolationLevel.Serializable:
			case IsolationLevel.Snapshot:
				Debug.Assert(false, "valid IsolationLevel " + value.ToString());
				break;
			case IsolationLevel.Chaos:
				break;
			default:
				Debug.Assert(false, "invalid IsolationLevel " + value.ToString());
				break;
		}
#endif
		return ODBC.NotSupportedEnumerationValue(typeof(IsolationLevel), (int)value);
	}

	static internal InvalidOperationException NoMappingForSqlTransactionLevel(int value) => ADP.DataAdapter(Res.GetString(Res.Odbc_NoMappingForSqlTransactionLevel, value.ToString(CultureInfo.InvariantCulture)));

	static internal Exception NegativeArgument() => ADP.Argument(Res.GetString(Res.Odbc_NegativeArgument));
	static internal Exception CantSetPropertyOnOpenConnection() => ADP.InvalidOperation(Res.GetString(Res.Odbc_CantSetPropertyOnOpenConnection));
	static internal Exception CantEnableConnectionpooling(ODBC32.RetCode retcode) => ADP.DataAdapter(Res.GetString(Res.Odbc_CantEnableConnectionpooling, ODBC32.RetcodeToString(retcode)));
	static internal Exception CantAllocateEnvironmentHandle(ODBC32.RetCode retcode) => ADP.DataAdapter(Res.GetString(Res.Odbc_CantAllocateEnvironmentHandle, ODBC32.RetcodeToString(retcode)));
	static internal Exception FailedToGetDescriptorHandle(ODBC32.RetCode retcode) => ADP.DataAdapter(Res.GetString(Res.Odbc_FailedToGetDescriptorHandle, ODBC32.RetcodeToString(retcode)));
	static internal Exception NotInTransaction() => ADP.InvalidOperation(Res.GetString(Res.Odbc_NotInTransaction));
	static internal Exception UnknownOdbcType(OdbcType odbctype) => ADP.InvalidEnumerationValue(typeof(OdbcType), (int)odbctype);
	internal const string Pwd = "pwd";

	static internal void TraceODBC(int level, string method, ODBC32.RetCode retcode) => Bid.TraceSqlReturn("<odbc|API|ODBC|RET> %08X{SQLRETURN}, method=%ls\n", retcode, method);

	internal static short ShortStringLength(string inputString) => checked((short)ADP.StringLength(inputString));
}


internal static class ODBC32
{

	internal enum SQL_HANDLE : short
	{
		ENV = 1,
		DBC = 2,
		STMT = 3,
		DESC = 4,
	}

	// from .\public\sdk\inc\sqlext.h: and .\public\sdk\inc\sql.h
	// must be public because it is serialized by OdbcException
	[Serializable]
	public enum RETCODE : int
	{ // must be int instead of short for Everett OdbcException Serializablity.
		SUCCESS = 0,
		SUCCESS_WITH_INFO = 1,
		ERROR = -1,
		INVALID_HANDLE = -2,
		NO_DATA = 100,
	}

	// must be public because it is serialized by OdbcException
	internal enum RetCode : short
	{
		SUCCESS = 0,
		SUCCESS_WITH_INFO = 1,
		ERROR = -1,
		INVALID_HANDLE = -2,
		NO_DATA = 100,
	}

	internal static string RetcodeToString(RetCode retcode)
	{
		switch (retcode)
		{
			case RetCode.SUCCESS: return "SUCCESS";
			case RetCode.SUCCESS_WITH_INFO: return "SUCCESS_WITH_INFO";
			case RetCode.ERROR: return "ERROR";
			case RetCode.INVALID_HANDLE: return "INVALID_HANDLE";
			case RetCode.NO_DATA: return "NO_DATA";
			default:
				Debug.Assert(false, "Unknown enumerator passed to RetcodeToString method");
				goto case RetCode.ERROR;
		}
	}



	internal enum SQL_CONVERT : ushort
	{
		BIGINT = 53,
		BINARY = 54,
		BIT = 55,
		CHAR = 56,
		DATE = 57,
		DECIMAL = 58,
		DOUBLE = 59,
		FLOAT = 60,
		INTEGER = 61,
		LONGVARCHAR = 62,
		NUMERIC = 63,
		REAL = 64,
		SMALLINT = 65,
		TIME = 66,
		TIMESTAMP = 67,
		TINYINT = 68,
		VARBINARY = 69,
		VARCHAR = 70,
		LONGVARBINARY = 71,
	}

	[Flags]
	internal enum SQL_CVT
	{
		CHAR = 0x00000001,
		NUMERIC = 0x00000002,
		DECIMAL = 0x00000004,
		INTEGER = 0x00000008,
		SMALLINT = 0x00000010,
		FLOAT = 0x00000020,
		REAL = 0x00000040,
		DOUBLE = 0x00000080,
		VARCHAR = 0x00000100,
		LONGVARCHAR = 0x00000200,
		BINARY = 0x00000400,
		VARBINARY = 0x00000800,
		BIT = 0x00001000,
		TINYINT = 0x00002000,
		BIGINT = 0x00004000,
		DATE = 0x00008000,
		TIME = 0x00010000,
		TIMESTAMP = 0x00020000,
		LONGVARBINARY = 0x00040000,
		INTERVAL_YEAR_MONTH = 0x00080000,
		INTERVAL_DAY_TIME = 0x00100000,
		WCHAR = 0x00200000,
		WLONGVARCHAR = 0x00400000,
		WVARCHAR = 0x00800000,
		GUID = 0x01000000,
	}

	internal enum STMT : short
	{
		CLOSE = 0,
		DROP = 1,
		UNBIND = 2,
		RESET_PARAMS = 3,
	}

	internal enum SQL_MAX
	{
		NUMERIC_LEN = 16,
	}

	internal enum SQL_IS
	{
		POINTER = -4,
		INTEGER = -6,
		UINTEGER = -5,
		SMALLINT = -8,
	}


	//SQL Server specific defines
	//
	internal enum SQL_HC                          // from Odbcss.h
	{
		OFF = 0,                //  FOR BROWSE columns are hidden
		ON = 1,                //  FOR BROWSE columns are exposed
	}

	internal enum SQL_NB                          // from Odbcss.h
	{
		OFF = 0,                //  NO_BROWSETABLE is off
		ON = 1,                //  NO_BROWSETABLE is on
	}

	//  SQLColAttributes driver specific defines.
	//  SQLSet/GetDescField driver specific defines.
	//  Microsoft has 1200 thru 1249 reserved for Microsoft SQL Server driver usage.
	//
	internal enum SQL_CA_SS                       // from Odbcss.h
	{
		BASE = 1200,           // SQL_CA_SS_BASE

		COLUMN_HIDDEN = BASE + 11,      //  Column is hidden (FOR BROWSE)
		COLUMN_KEY = BASE + 12,      //  Column is key column (FOR BROWSE)
		VARIANT_TYPE = BASE + 15,
		VARIANT_SQL_TYPE = BASE + 16,
		VARIANT_SERVER_TYPE = BASE + 17,

	}
	internal enum SQL_SOPT_SS                     // from Odbcss.h
	{
		BASE = 1225,           // SQL_SOPT_SS_BASE
		HIDDEN_COLUMNS = BASE + 2,       // Expose FOR BROWSE hidden columns
		NOBROWSETABLE = BASE + 3,       // Set NOBROWSETABLE option
	}

	internal const short SQL_COMMIT = 0;      //Commit
	internal const short SQL_ROLLBACK = 1;      //Abort

	static internal readonly IntPtr SQL_AUTOCOMMIT_OFF = ADP.PtrZero;
	static internal readonly IntPtr SQL_AUTOCOMMIT_ON = new(1);

	internal enum SQL_TRANSACTION
	{
		READ_UNCOMMITTED = 0x00000001,
		READ_COMMITTED = 0x00000002,
		REPEATABLE_READ = 0x00000004,
		SERIALIZABLE = 0x00000008,
		SNAPSHOT = 0x00000020, // VSDD 414121: SQL_TXN_SS_SNAPSHOT == 0x20 (sqlncli.h)
	}

	internal enum SQL_PARAM
	{
		// unused   TYPE_UNKNOWN        =   0,          // SQL_PARAM_TYPE_UNKNOWN
		INPUT = 1,          // SQL_PARAM_INPUT
		INPUT_OUTPUT = 2,          // SQL_PARAM_INPUT_OUTPUT
								   // unused   RESULT_COL          =   3,          // SQL_RESULT_COL
		OUTPUT = 4,          // SQL_PARAM_OUTPUT
		RETURN_VALUE = 5,          // SQL_RETURN_VALUE
	}

	// SQL_API_* values
	// there are a gillion of these I am only defining the ones currently needed
	// others can be added as needed
	internal enum SQL_API : ushort
	{
		SQLCOLUMNS = 40,
		SQLEXECDIRECT = 11,
		SQLGETTYPEINFO = 47,
		SQLPROCEDURECOLUMNS = 66,
		SQLPROCEDURES = 67,
		SQLSTATISTICS = 53,
		SQLTABLES = 54,
	}


	internal enum SQL_DESC : short
	{
		// from sql.h (ODBCVER >= 3.0)
		//
		COUNT = 1001,
		TYPE = 1002,
		LENGTH = 1003,
		OCTET_LENGTH_PTR = 1004,
		PRECISION = 1005,
		SCALE = 1006,
		DATETIME_INTERVAL_CODE = 1007,
		NULLABLE = 1008,
		INDICATOR_PTR = 1009,
		DATA_PTR = 1010,
		NAME = 1011,
		UNNAMED = 1012,
		OCTET_LENGTH = 1013,
		ALLOC_TYPE = 1099,

		// from sqlext.h (ODBCVER >= 3.0)
		//
		CONCISE_TYPE = SQL_COLUMN.TYPE,
		DISPLAY_SIZE = SQL_COLUMN.DISPLAY_SIZE,
		UNSIGNED = SQL_COLUMN.UNSIGNED,
		UPDATABLE = SQL_COLUMN.UPDATABLE,
		AUTO_UNIQUE_VALUE = SQL_COLUMN.AUTO_INCREMENT,

		TYPE_NAME = SQL_COLUMN.TYPE_NAME,
		TABLE_NAME = SQL_COLUMN.TABLE_NAME,
		SCHEMA_NAME = SQL_COLUMN.OWNER_NAME,
		CATALOG_NAME = SQL_COLUMN.QUALIFIER_NAME,

		BASE_COLUMN_NAME = 22,
		BASE_TABLE_NAME = 23,
	}

	// ODBC version 2.0 style attributes
	// All IdentifierValues are ODBC 1.0 unless marked differently
	//
	internal enum SQL_COLUMN
	{
		COUNT = 0,
		NAME = 1,
		TYPE = 2,
		LENGTH = 3,
		PRECISION = 4,
		SCALE = 5,
		DISPLAY_SIZE = 6,
		NULLABLE = 7,
		UNSIGNED = 8,
		MONEY = 9,
		UPDATABLE = 10,
		AUTO_INCREMENT = 11,
		CASE_SENSITIVE = 12,
		SEARCHABLE = 13,
		TYPE_NAME = 14,
		TABLE_NAME = 15,    // (ODBC 2.0)
		OWNER_NAME = 16,    // (ODBC 2.0)
		QUALIFIER_NAME = 17,    // (ODBC 2.0)
		LABEL = 18,
	}

	internal enum SQL_GROUP_BY
	{
		NOT_SUPPORTED = 0,    // SQL_GB_NOT_SUPPORTED
		GROUP_BY_EQUALS_SELECT = 1,    // SQL_GB_GROUP_BY_EQUALS_SELECT
		GROUP_BY_CONTAINS_SELECT = 2,    // SQL_GB_GROUP_BY_CONTAINS_SELECT
		NO_RELATION = 3,    // SQL_GB_NO_RELATION
		COLLATE = 4,    // SQL_GB_COLLATE - added in ODBC 3.0
	}

	// values from sqlext.h
	internal enum SQL_SQL92_RELATIONAL_JOIN_OPERATORS
	{
		CORRESPONDING_CLAUSE = 0x00000001,    // SQL_SRJO_CORRESPONDING_CLAUSE
		CROSS_JOIN = 0x00000002,    // SQL_SRJO_CROSS_JOIN
		EXCEPT_JOIN = 0x00000004,    // SQL_SRJO_EXCEPT_JOIN
		FULL_OUTER_JOIN = 0x00000008,    // SQL_SRJO_FULL_OUTER_JOIN
		INNER_JOIN = 0x00000010,    // SQL_SRJO_INNER_JOIN
		INTERSECT_JOIN = 0x00000020,    // SQL_SRJO_INTERSECT_JOIN
		LEFT_OUTER_JOIN = 0x00000040,    // SQL_SRJO_LEFT_OUTER_JOIN
		NATURAL_JOIN = 0x00000080,    // SQL_SRJO_NATURAL_JOIN
		RIGHT_OUTER_JOIN = 0x00000100,    // SQL_SRJO_RIGHT_OUTER_JOIN
		UNION_JOIN = 0x00000200,    // SQL_SRJO_UNION_JOIN
	}

	// values from sql.h
	internal enum SQL_OJ_CAPABILITIES
	{
		LEFT = 0x00000001,    // SQL_OJ_LEFT
		RIGHT = 0x00000002,    // SQL_OJ_RIGHT
		FULL = 0x00000004,    // SQL_OJ_FULL
		NESTED = 0x00000008,    // SQL_OJ_NESTED
		NOT_ORDERED = 0x00000010,    // SQL_OJ_NOT_ORDERED
		INNER = 0x00000020,    // SQL_OJ_INNER
		ALL_COMPARISON_OPS = 0x00000040,  //SQL_OJ_ALLCOMPARISION+OPS
	}

	internal enum SQL_UPDATABLE
	{
		READONLY = 0,    // SQL_ATTR_READ_ONLY
		WRITE = 1,    // SQL_ATTR_WRITE
		READWRITE_UNKNOWN = 2,    // SQL_ATTR_READWRITE_UNKNOWN
	}

	internal enum SQL_IDENTIFIER_CASE
	{
		UPPER = 1,    // SQL_IC_UPPER
		LOWER = 2,    // SQL_IC_LOWER
		SENSITIVE = 3,    // SQL_IC_SENSITIVE
		MIXED = 4,    // SQL_IC_MIXED
	}

	// Uniqueness parameter in the SQLStatistics function
	internal enum SQL_INDEX : short
	{
		UNIQUE = 0,
		ALL = 1,
	}

	// Reserved parameter in the SQLStatistics function
	internal enum SQL_STATISTICS_RESERVED : short
	{
		QUICK = 0,                // SQL_QUICK
		ENSURE = 1,                // SQL_ENSURE
	}

	// Identifier type parameter in the SQLSpecialColumns function
	internal enum SQL_SPECIALCOLS : ushort
	{
		BEST_ROWID = 1,            // SQL_BEST_ROWID
		ROWVER = 2,            // SQL_ROWVER
	}

	// Scope parameter in the SQLSpecialColumns function
	internal enum SQL_SCOPE : ushort
	{
		CURROW = 0,            // SQL_SCOPE_CURROW
		TRANSACTION = 1,           // SQL_SCOPE_TRANSACTION
		SESSION = 2,           // SQL_SCOPE_SESSION
	}

	internal enum SQL_NULLABILITY : ushort
	{
		NO_NULLS = 0,                // SQL_NO_NULLS
		NULLABLE = 1,                // SQL_NULLABLE
		UNKNOWN = 2,                // SQL_NULLABLE_UNKNOWN
	}

	internal enum SQL_SEARCHABLE
	{
		UNSEARCHABLE = 0,        // SQL_UNSEARCHABLE
		LIKE_ONLY = 1,        // SQL_LIKE_ONLY
		ALL_EXCEPT_LIKE = 2,        // SQL_ALL_EXCEPT_LIKE
		SEARCHABLE = 3,        // SQL_SEARCHABLE
	}

	internal enum SQL_UNNAMED
	{
		NAMED = 0,                   // SQL_NAMED
		UNNAMED = 1,                 // SQL_UNNAMED
	}
	// todo:move
	// internal constants
	// not odbc specific
	//
	internal enum HANDLER
	{
		IGNORE = 0x00000000,
		THROW = 0x00000001,
	}

	// values for SQLStatistics TYPE column
	internal enum SQL_STATISTICSTYPE
	{
		TABLE_STAT = 0,                    // TABLE Statistics
		INDEX_CLUSTERED = 1,                    // CLUSTERED index statistics
		INDEX_HASHED = 2,                    // HASHED index statistics
		INDEX_OTHER = 3,                    // OTHER index statistics
	}

	// values for SQLProcedures PROCEDURE_TYPE column
	internal enum SQL_PROCEDURETYPE
	{
		UNKNOWN = 0,                    // procedure is of unknow type
		PROCEDURE = 1,                    // procedure is a procedure
		FUNCTION = 2,                    // procedure is a function
	}

	// private constants
	// to define data types (see below)
	//
	private const int SIGNED_OFFSET = -20;    // SQL_SIGNED_OFFSET
	private const int UNSIGNED_OFFSET = -22;    // SQL_UNSIGNED_OFFSET

	//C Data Types - used when getting data (SQLGetData)
	internal enum SQL_C : short
	{
		CHAR = 1,                     //SQL_C_CHAR
		WCHAR = -8,                     //SQL_C_WCHAR
		SLONG = 4 + SIGNED_OFFSET,     //SQL_C_LONG+SQL_SIGNED_OFFSET
									   //          ULONG           =    4 + UNSIGNED_OFFSET,   //SQL_C_LONG+SQL_UNSIGNED_OFFSET
		SSHORT = 5 + SIGNED_OFFSET,     //SQL_C_SSHORT+SQL_SIGNED_OFFSET
										//          USHORT          =    5 + UNSIGNED_OFFSET,   //SQL_C_USHORT+SQL_UNSIGNED_OFFSET
		REAL = 7,                     //SQL_C_REAL
		DOUBLE = 8,                     //SQL_C_DOUBLE
		BIT = -7,                     //SQL_C_BIT
									  //          STINYINT        =   -6 + SIGNED_OFFSET,     //SQL_C_STINYINT+SQL_SIGNED_OFFSET
		UTINYINT = -6 + UNSIGNED_OFFSET,   //SQL_C_UTINYINT+SQL_UNSIGNED_OFFSET
		SBIGINT = -5 + SIGNED_OFFSET,     //SQL_C_SBIGINT+SQL_SIGNED_OFFSET
		UBIGINT = -5 + UNSIGNED_OFFSET,   //SQL_C_UBIGINT+SQL_UNSIGNED_OFFSET
		BINARY = -2,                     //SQL_C_BINARY
		TIMESTAMP = 11,                     //SQL_C_TIMESTAMP

		TYPE_DATE = 91,                     //SQL_C_TYPE_DATE
		TYPE_TIME = 92,                     //SQL_C_TYPE_TIME
		TYPE_TIMESTAMP = 93,                     //SQL_C_TYPE_TIMESTAMP

		NUMERIC = 2,                     //SQL_C_NUMERIC
		GUID = -11,                    //SQL_C_GUID
		DEFAULT = 99,                     //SQL_C_DEFAULT
		ARD_TYPE = -99,                    //SQL_ARD_TYPE
	}

	//SQL Data Types - returned as column types (SQLColAttribute)
	internal enum SQL_TYPE : short
	{
		CHAR = SQL_C.CHAR,             //SQL_CHAR
		VARCHAR = 12,                     //SQL_VARCHAR
		LONGVARCHAR = -1,                     //SQL_LONGVARCHAR
		WCHAR = SQL_C.WCHAR,            //SQL_WCHAR
		WVARCHAR = -9,                     //SQL_WVARCHAR
		WLONGVARCHAR = -10,                    //SQL_WLONGVARCHAR
		DECIMAL = 3,                      //SQL_DECIMAL
		NUMERIC = SQL_C.NUMERIC,          //SQL_NUMERIC
		SMALLINT = 5,                      //SQL_SMALLINT
		INTEGER = 4,                      //SQL_INTEGER
		REAL = SQL_C.REAL,             //SQL_REAL
		FLOAT = 6,                      //SQL_FLOAT
		DOUBLE = SQL_C.DOUBLE,           //SQL_DOUBLE
		BIT = SQL_C.BIT,              //SQL_BIT
		TINYINT = -6,                     //SQL_TINYINT
		BIGINT = -5,                     //SQL_BIGINT
		BINARY = SQL_C.BINARY,           //SQL_BINARY
		VARBINARY = -3,                     //SQL_VARBINARY
		LONGVARBINARY = -4,                     //SQL_LONGVARBINARY

		//          DATE            =   9,                      //SQL_DATE
		TYPE_DATE = SQL_C.TYPE_DATE,        //SQL_TYPE_DATE
		TYPE_TIME = SQL_C.TYPE_TIME,        //SQL_TYPE_TIME
		TIMESTAMP = SQL_C.TIMESTAMP,        //SQL_TIMESTAMP
		TYPE_TIMESTAMP = SQL_C.TYPE_TIMESTAMP,   //SQL_TYPE_TIMESTAMP


		GUID = SQL_C.GUID,             //SQL_GUID

		//  from odbcss.h in mdac 9.0 sources!
		//  Driver specific SQL type defines.
		//  Microsoft has -150 thru -199 reserved for Microsoft SQL Server driver usage.
		//
		SS_VARIANT = -150,
		SS_UDT = -151,
		SS_XML = -152,
		SS_UTCDATETIME = -153,
		SS_TIME_EX = -154,
	}

	internal const short SQL_ALL_TYPES = 0;
	static internal readonly IntPtr SQL_HANDLE_NULL = ADP.PtrZero;
	internal const int SQL_NULL_DATA = -1;   // sql.h
	internal const int SQL_NO_TOTAL = -4;   // sqlext.h

	internal const int SQL_DEFAULT_PARAM = -5;
	//      internal const Int32  SQL_IGNORE         = -6;

	// column ordinals for SQLProcedureColumns result set
	// this column ordinals are not defined in any c/c++ header but in the ODBC Programmer's Reference under SQLProcedureColumns
	//
	internal const int COLUMN_NAME = 4;
	internal const int COLUMN_TYPE = 5;
	internal const int DATA_TYPE = 6;
	internal const int COLUMN_SIZE = 8;
	internal const int DECIMAL_DIGITS = 10;
	internal const int NUM_PREC_RADIX = 11;

	internal enum SQL_ATTR
	{
		APP_ROW_DESC = 10010,              // (ODBC 3.0)
		APP_PARAM_DESC = 10011,              // (ODBC 3.0)
		IMP_ROW_DESC = 10012,              // (ODBC 3.0)
		IMP_PARAM_DESC = 10013,              // (ODBC 3.0)
		METADATA_ID = 10014,              // (ODBC 3.0)
		ODBC_VERSION = 200,
		CONNECTION_POOLING = 201,
		AUTOCOMMIT = 102,
		TXN_ISOLATION = 108,
		CURRENT_CATALOG = 109,
		LOGIN_TIMEOUT = 103,
		QUERY_TIMEOUT = 0,                  // from sqlext.h
		CONNECTION_DEAD = 1209,               // from sqlext.h

		// from sqlncli.h
		SQL_COPT_SS_BASE = 1200,
		SQL_COPT_SS_ENLIST_IN_DTC = (SQL_COPT_SS_BASE + 7),
		SQL_COPT_SS_TXN_ISOLATION = (SQL_COPT_SS_BASE + 27), // Used to set/get any driver-specific or ODBC-defined TXN iso level
	}

	//SQLGetInfo
	internal enum SQL_INFO : ushort
	{
		DATA_SOURCE_NAME = 2,    // SQL_DATA_SOURCE_NAME in sql.h
		SERVER_NAME = 13,   // SQL_SERVER_NAME in sql.h
		DRIVER_NAME = 6,    // SQL_DRIVER_NAME as defined in sqlext.h
		DRIVER_VER = 7,    // SQL_DRIVER_VER as defined in sqlext.h
		ODBC_VER = 10,   // SQL_ODBC_VER as defined in sqlext.h
		SEARCH_PATTERN_ESCAPE = 14,   // SQL_SEARCH_PATTERN_ESCAPE from sql.h
		DBMS_VER = 18,
		DBMS_NAME = 17,   // SQL_DBMS_NAME as defined in sqlext.h
		IDENTIFIER_CASE = 28,   // SQL_IDENTIFIER_CASE from sql.h
		IDENTIFIER_QUOTE_CHAR = 29,   // SQL_IDENTIFIER_QUOTE_CHAR from sql.h
		CATALOG_NAME_SEPARATOR = 41,   // SQL_CATALOG_NAME_SEPARATOR
		DRIVER_ODBC_VER = 77,   // SQL_DRIVER_ODBC_VER as defined in sqlext.h
		GROUP_BY = 88,   // SQL_GROUP_BY as defined in  sqlext.h
		KEYWORDS = 89,   // SQL_KEYWORDS as defined in sqlext.h
		ORDER_BY_COLUMNS_IN_SELECT = 90,   // SQL_ORDER_BY_COLUNS_IN_SELECT in sql.h
		QUOTED_IDENTIFIER_CASE = 93,   // SQL_QUOTED_IDENTIFIER_CASE in sqlext.h
		SQL_OJ_CAPABILITIES_30 = 115, //SQL_OJ_CAPABILITIES from sql.h
		SQL_OJ_CAPABILITIES_20 = 65003, //SQL_OJ_CAPABILITIES from sqlext.h
		SQL_SQL92_RELATIONAL_JOIN_OPERATORS = 161, //SQL_SQL92_RELATIONAL_JOIN_OPERATORS from sqlext.h

	}

	static internal readonly IntPtr SQL_OV_ODBC3 = new(3);
	internal const int SQL_NTS = -3;       //flags for null-terminated string

	//Pooling
	static internal readonly IntPtr SQL_CP_OFF = new(0);       //Connection Pooling disabled
	static internal readonly IntPtr SQL_CP_ONE_PER_DRIVER = new(1);       //One pool per driver
	static internal readonly IntPtr SQL_CP_ONE_PER_HENV = new(2);       //One pool per environment

	/* values for SQL_ATTR_CONNECTION_DEAD */
	internal const int SQL_CD_TRUE = 1;
	internal const int SQL_CD_FALSE = 0;

	internal const int SQL_DTC_DONE = 0;
	internal const int SQL_IS_POINTER = -4;
	internal const int SQL_IS_PTR = 1;

	internal enum SQL_DRIVER
	{
		NOPROMPT = 0,
		COMPLETE = 1,
		PROMPT = 2,
		COMPLETE_REQUIRED = 3,
	}

	// todo:move
	// internal const. not odbc specific
	//
	// Connection string max length
	internal const int MAX_CONNECTION_STRING_LENGTH = 1024;

	// Column set for SQLPrimaryKeys
	internal enum SQL_PRIMARYKEYS : short
	{
		/*
					CATALOGNAME         = 1,                    // TABLE_CAT
					SCHEMANAME          = 2,                    // TABLE_SCHEM
					TABLENAME           = 3,                    // TABLE_NAME
		*/
		COLUMNNAME = 4,                    // COLUMN_NAME
		/*
					KEY_SEQ             = 5,                    // KEY_SEQ
					PKNAME              = 6,                    // PK_NAME
		*/
	}

	// Column set for SQLStatistics
	internal enum SQL_STATISTICS : short
	{
		/*
					CATALOGNAME         = 1,                    // TABLE_CAT
					SCHEMANAME          = 2,                    // TABLE_SCHEM
					TABLENAME           = 3,                    // TABLE_NAME
					NONUNIQUE           = 4,                    // NON_UNIQUE
					INDEXQUALIFIER      = 5,                    // INDEX_QUALIFIER
		*/
		INDEXNAME = 6,                    // INDEX_NAME
		/*
					TYPE                = 7,                    // TYPE
		*/
		ORDINAL_POSITION = 8,                    // ORDINAL_POSITION
		COLUMN_NAME = 9,                    // COLUMN_NAME
		/*
					ASC_OR_DESC         = 10,                   // ASC_OR_DESC
					CARDINALITY         = 11,                   // CARDINALITY
					PAGES               = 12,                   // PAGES
					FILTER_CONDITION    = 13,                   // FILTER_CONDITION
		*/
	}

	// Column set for SQLSpecialColumns
	internal enum SQL_SPECIALCOLUMNSET : short
	{
		/*
					SCOPE               = 1,                    // SCOPE
		*/
		COLUMN_NAME = 2,                    // COLUMN_NAME
		/*
					DATA_TYPE           = 3,                    // DATA_TYPE
					TYPE_NAME           = 4,                    // TYPE_NAME
					COLUMN_SIZE         = 5,                    // COLUMN_SIZE
					BUFFER_LENGTH       = 6,                    // BUFFER_LENGTH
					DECIMAL_DIGITS      = 7,                    // DECIMAL_DIGITS
					PSEUDO_COLUMN       = 8,                    // PSEUDO_COLUMN
		*/
	}

	internal const short SQL_DIAG_SQLSTATE = 4;
	internal const short SQL_RESULT_COL = 3;

	// Helpers
	static internal List<OdbcError> GetDiagErrors(string source, OdbcHandle hrHandle, RetCode retcode)
	{
		List<OdbcError> errors = new();
		GetDiagErrors(errors, source, hrHandle, retcode);
		return errors;
	}

	static internal void GetDiagErrors(OdbcErrorCollection errors, string source, OdbcHandle hrHandle, RetCode retcode)
	{
		Debug.Assert(retcode != ODBC32.RetCode.INVALID_HANDLE, "retcode must never be ODBC32.RetCode.INVALID_HANDLE");
		if (RetCode.SUCCESS != retcode)
		{
			short iRec = 0;
			short cchActual = 0;

			StringBuilder message = new(1024);
			bool moreerrors = true;
			while (moreerrors)
			{

				++iRec;

				retcode = hrHandle.GetDiagnosticRecord(iRec, out string sqlState, message, out int NativeError, out cchActual);
				if ((RetCode.SUCCESS_WITH_INFO == retcode) && (message.Capacity - 1 < cchActual))
				{
					message.Capacity = cchActual + 1;
					retcode = hrHandle.GetDiagnosticRecord(iRec, out sqlState, message, out NativeError, out cchActual);
				}

				//Note: SUCCESS_WITH_INFO from SQLGetDiagRec would be because
				//the buffer is not large enough for the error string.
				moreerrors = (retcode == RetCode.SUCCESS || retcode == RetCode.SUCCESS_WITH_INFO);
				if (moreerrors)
				{
					//Sets up the InnerException as well...
					errors.Add(new OdbcError(
						source,
						message.ToString(),
						sqlState,
						NativeError
						)
					);
				}
			}
		}
	}

	//
	// ODBC32
	//
	[DllImport("odbc32.dll")]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLAllocHandle(
		/*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
		/*SQLHANDLE*/IntPtr InputHandle,
		/*SQLHANDLE* */out IntPtr OutputHandle);

	[DllImport("odbc32.dll")]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLAllocHandle(
		/*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
		/*SQLHANDLE*/OdbcHandle InputHandle,
		/*SQLHANDLE* */out IntPtr OutputHandle);


	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLBindCol(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/ushort ColumnNumber,
		/*SQLSMALLINT*/ODBC32.SQL_C TargetType,
		/*SQLPOINTER*/HandleRef TargetValue,
		/*SQLLEN*/IntPtr BufferLength,
		/*SQLLEN* */IntPtr StrLen_or_Ind);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLBindCol(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/ushort ColumnNumber,
		/*SQLSMALLINT*/ODBC32.SQL_C TargetType,
		/*SQLPOINTER*/IntPtr TargetValue,
		/*SQLLEN*/IntPtr BufferLength,
		/*SQLLEN* */IntPtr StrLen_or_Ind);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLBindParameter(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/ushort ParameterNumber,
		/*SQLSMALLINT*/short ParamDirection,
		/*SQLSMALLINT*/ODBC32.SQL_C SQLCType,
		/*SQLSMALLINT*/short SQLType,
		/*SQLULEN*/IntPtr cbColDef,
		/*SQLSMALLINT*/IntPtr ibScale,
		/*SQLPOINTER*/HandleRef rgbValue,
		/*SQLLEN*/IntPtr BufferLength,
		/*SQLLEN* */HandleRef StrLen_or_Ind);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLCancel(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLCloseCursor(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLColAttributeW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/short ColumnNumber,
		/*SQLUSMALLINT*/short FieldIdentifier,
		/*SQLPOINTER*/CNativeBuffer CharacterAttribute,
		/*SQLSMALLINT*/short BufferLength,
		/*SQLSMALLINT* */out short StringLength,
		/*SQLPOINTER*/out IntPtr NumericAttribute);

	// note: in sql.h this is defined differently for the 64Bit platform.
	// However, for us the code is not different for SQLPOINTER or SQLLEN ...
	// frome sql.h:
	// #ifdef _WIN64
	// SQLRETURN  SQL_API SQLColAttribute (SQLHSTMT StatementHandle,
	//            SQLUSMALLINT ColumnNumber, SQLUSMALLINT FieldIdentifier,
	//            SQLPOINTER CharacterAttribute, SQLSMALLINT BufferLength,
	//            SQLSMALLINT *StringLength, SQLLEN *NumericAttribute);
	// #else
	// SQLRETURN  SQL_API SQLColAttribute (SQLHSTMT StatementHandle,
	//            SQLUSMALLINT ColumnNumber, SQLUSMALLINT FieldIdentifier,
	//            SQLPOINTER CharacterAttribute, SQLSMALLINT BufferLength,
	//            SQLSMALLINT *StringLength, SQLPOINTER NumericAttribute);
	// #endif


	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLColumnsW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string TableName,
		/*SQLSMALLINT*/short NameLen3,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string ColumnName,
		/*SQLSMALLINT*/short NameLen4);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLDisconnect(
		/*SQLHDBC*/IntPtr ConnectionHandle);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLDriverConnectW(
		/*SQLHDBC*/OdbcConnectionHandle hdbc,
		/*SQLHWND*/IntPtr hwnd,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string               connectionstring,
		/*SQLSMALLINT*/short cbConnectionstring,
		/*SQLCHAR* */IntPtr connectionstringout,
		/*SQLSMALLINT*/short cbConnectionstringoutMax,
		/*SQLSMALLINT* */out short cbConnectionstringout,
		/*SQLUSMALLINT*/short fDriverCompletion);

	[DllImport("odbc32.dll")]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLEndTran(
		/*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
		/*SQLHANDLE*/IntPtr Handle,
		/*SQLSMALLINT*/short CompletionType);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLExecDirectW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string   StatementText,
		/*SQLINTEGER*/int TextLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLExecute(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLFetch(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle);

	[DllImport("odbc32.dll")]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLFreeHandle(
		/*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
		/*SQLHSTMT*/IntPtr StatementHandle);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLFreeStmt(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/ODBC32.STMT Option);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetConnectAttrW(
		/*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/byte[] Value,
		/*SQLINTEGER*/int BufferLength,
		/*SQLINTEGER* */out int StringLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetData(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/ushort ColumnNumber,
		/*SQLSMALLINT*/ODBC32.SQL_C TargetType,
		/*SQLPOINTER*/CNativeBuffer TargetValue,
		/*SQLLEN*/IntPtr BufferLength, // sql.h differs from MSDN
		/*SQLLEN* */out IntPtr StrLen_or_Ind);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetDescFieldW(
		/*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
		/*SQLUSMALLINT*/short RecNumber,
		/*SQLUSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
		/*SQLPOINTER*/CNativeBuffer ValuePointer,
		/*SQLINTEGER*/int BufferLength,
		/*SQLINTEGER* */out int StringLength);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetDiagRecW(
		/*SQLSMALLINT*/ODBC32.SQL_HANDLE HandleType,
		/*SQLHANDLE*/OdbcHandle Handle,
		/*SQLSMALLINT*/short RecNumber,
		/*SQLCHAR* */  StringBuilder rchState,
		/*SQLINTEGER* */out int NativeError,
		/*SQLCHAR* */StringBuilder MessageText,
		/*SQLSMALLINT*/short BufferLength,
		/*SQLSMALLINT* */out short TextLength);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetDiagFieldW(
	   /*SQLSMALLINT*/ ODBC32.SQL_HANDLE HandleType,
	   /*SQLHANDLE*/   OdbcHandle Handle,
	   /*SQLSMALLINT*/ short RecNumber,
	   /*SQLSMALLINT*/ short DiagIdentifier,
	   [MarshalAs(UnmanagedType.LPWStr)]
		   /*SQLPOINTER*/  StringBuilder    rchState,
	   /*SQLSMALLINT*/ short BufferLength,
	   /*SQLSMALLINT* */ out short StringLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetFunctions(
		/*SQLHBDC*/OdbcConnectionHandle hdbc,
		/*SQLUSMALLINT*/ODBC32.SQL_API fFunction,
		/*SQLUSMALLINT* */out short pfExists);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetInfoW(
		/*SQLHBDC*/OdbcConnectionHandle hdbc,
		/*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
		/*SQLPOINTER*/byte[] rgbInfoValue,
		/*SQLSMALLINT*/short cbInfoValueMax,
		/*SQLSMALLINT* */out short pcbInfoValue);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetInfoW(
		/*SQLHBDC*/OdbcConnectionHandle hdbc,
		/*SQLUSMALLINT*/ODBC32.SQL_INFO fInfoType,
		/*SQLPOINTER*/byte[] rgbInfoValue,
		/*SQLSMALLINT*/short cbInfoValueMax,
		/*SQLSMALLINT* */IntPtr pcbInfoValue);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetStmtAttrW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/out IntPtr Value,
		/*SQLINTEGER*/int BufferLength,
		/*SQLINTEGER*/out int StringLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLGetTypeInfo(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLSMALLINT*/short fSqlType);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLMoreResults(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLNumResultCols(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLSMALLINT* */out short ColumnCount);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLPrepareW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string   StatementText,
		/*SQLINTEGER*/int TextLength);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLPrimaryKeysW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */ string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string TableName,
		/*SQLSMALLINT*/short NameLen3);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLProcedureColumnsW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string ProcName,
		/*SQLSMALLINT*/short NameLen3,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string ColumnName,
		/*SQLSMALLINT*/short NameLen4);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLProceduresW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)] /*SQLCHAR* */ string ProcName,
		/*SQLSMALLINT*/short NameLen3);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLRowCount(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLLEN* */out IntPtr RowCount);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
		/*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/System.Transactions.IDtcTransaction Value,
		/*SQLINTEGER*/int StringLength);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
		/*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/string Value,
		/*SQLINTEGER*/int StringLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW(
		/*SQLHBDC*/OdbcConnectionHandle ConnectionHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/IntPtr Value,
		/*SQLINTEGER*/int StringLength);

	[DllImport("odbc32.dll")]
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetConnectAttrW( // used only for AutoCommitOn
		/*SQLHBDC*/IntPtr ConnectionHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/IntPtr Value,
		/*SQLINTEGER*/int StringLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetDescFieldW(
		/*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
		/*SQLSMALLINT*/short ColumnNumber,
		/*SQLSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
		/*SQLPOINTER*/HandleRef CharacterAttribute,
		/*SQLINTEGER*/int BufferLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetDescFieldW(
		/*SQLHSTMT*/OdbcDescriptorHandle StatementHandle,
		/*SQLSMALLINT*/short ColumnNumber,
		/*SQLSMALLINT*/ODBC32.SQL_DESC FieldIdentifier,
		/*SQLPOINTER*/IntPtr CharacterAttribute,
		/*SQLINTEGER*/int BufferLength);

	[DllImport("odbc32.dll")]
	// user can set SQL_ATTR_CONNECTION_POOLING attribute with envHandle = null, this attribute is process-level attribute
	[ResourceExposure(ResourceScope.Process)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetEnvAttr(
		/*SQLHENV*/OdbcEnvironmentHandle EnvironmentHandle,
		/*SQLINTEGER*/ODBC32.SQL_ATTR Attribute,
		/*SQLPOINTER*/IntPtr Value,
		/*SQLINTEGER*/ODBC32.SQL_IS StringLength);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSetStmtAttrW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLINTEGER*/int Attribute,
		/*SQLPOINTER*/IntPtr Value,
		/*SQLINTEGER*/int StringLength);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLSpecialColumnsW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		/*SQLUSMALLINT*/ODBC32.SQL_SPECIALCOLS IdentifierType,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string TableName,
		/*SQLSMALLINT*/short NameLen3,
		/*SQLUSMALLINT*/ODBC32.SQL_SCOPE Scope,
		/*SQLUSMALLINT*/ ODBC32.SQL_NULLABILITY Nullable);

	[DllImport("odbc32.dll", CharSet = CharSet.Unicode)]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLStatisticsW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string TableName,
		/*SQLSMALLINT*/short NameLen3,
		/*SQLUSMALLINT*/short Unique,
		/*SQLUSMALLINT*/short Reserved);

	[DllImport("odbc32.dll")]
	[ResourceExposure(ResourceScope.None)]
	static internal extern /*SQLRETURN*/ODBC32.RetCode SQLTablesW(
		/*SQLHSTMT*/OdbcStatementHandle StatementHandle,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string CatalogName,
		/*SQLSMALLINT*/short NameLen1,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string SchemaName,
		/*SQLSMALLINT*/short NameLen2,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string TableName,
		/*SQLSMALLINT*/short NameLen3,
		[In, MarshalAs(UnmanagedType.LPWStr)]
			/*SQLCHAR* */string TableType,
		/*SQLSMALLINT*/short NameLen4);


	// we wrap the vtable entry which is just a function pointer as a delegate
	// since code (unlike data) doesn't move around within the process, it is safe to cache the delegate

	// we do not expect native to change its vtable entry at run-time (especially since these are free-threaded objects)
	// however to be extra safe double check the function pointer is the same as the cached delegate
	// whenever we encounter a new instance of the data


	// dangerous delegate around IUnknown::QueryInterface (0th vtable entry)
	[System.Security.SuppressUnmanagedCodeSecurityAttribute()]
	internal delegate int IUnknownQueryInterface(
			IntPtr pThis,
			ref Guid riid,
			ref IntPtr ppInterface);

	// dangerous delegate around IDataInitialize::GetDataSource (4th vtable entry)
	[System.Security.SuppressUnmanagedCodeSecurityAttribute()]
	internal delegate OleDbHResult IDataInitializeGetDataSource(
			IntPtr pThis, // first parameter is always the 'this' value, must use use result from QI
			IntPtr pUnkOuter,
			int dwClsCtx,
			[MarshalAs(UnmanagedType.LPWStr)] string pwszInitializationString,
			ref Guid riid,
			ref DataSourceWrapper ppDataSource);

	// dangerous wrapper around IDBInitialize::Initialize (4th vtable entry)
	[System.Security.SuppressUnmanagedCodeSecurityAttribute()]
	internal delegate OleDbHResult IDBInitializeInitialize(
			IntPtr pThis); // first parameter is always the 'this' value, must use use result from QI

	// dangerous wrapper around IDBCreateSession::CreateSession (4th vtable entry)
	[System.Security.SuppressUnmanagedCodeSecurityAttribute()]
	internal delegate OleDbHResult IDBCreateSessionCreateSession(
			IntPtr pThis, // first parameter is always the 'this' value, must use use result from QI
			IntPtr pUnkOuter,
			ref Guid riid,
			ref SessionWrapper ppDBSession);

	// dangerous wrapper around IDBCreateCommand::CreateCommand (4th vtable entry)
	[System.Security.SuppressUnmanagedCodeSecurityAttribute()]
	internal delegate OleDbHResult IDBCreateCommandCreateCommand(
			IntPtr pThis, // first parameter is always the 'this' value, must use use result from QI
			IntPtr pUnkOuter,
			ref Guid riid,
			[MarshalAs(UnmanagedType.Interface)] ref object ppCommand);

}

[Guid("00000567-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ADORecordConstruction
{

	[return: MarshalAs(UnmanagedType.Interface)] object get_Row();

	//void put_Row(
	//    [In, MarshalAs(UnmanagedType.Interface)] object pRow);

	//void put_ParentRow(
	//    [In, MarshalAs(UnmanagedType.Interface)]object pRow);
}

[Guid("00000283-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ADORecordsetConstruction
{

	[return: MarshalAs(UnmanagedType.Interface)] object get_Rowset();

	[Obsolete("not used", true)] void put_Rowset(/*deleted parameters signature*/);

	/*[return:MarshalAs(UnmanagedType.SysInt)]*/
	IntPtr get_Chapter();

	//[[PreserveSig]
	//iint put_Chapter (
	//         [In]
	//         IntPtr pcRefCount);

	//[[PreserveSig]
	//iint get_RowPosition (
	//         [Out, MarshalAs(UnmanagedType.Interface)]
	//         out object ppRowPos);

	//[[PreserveSig]
	//iint put_RowPosition (
	//         [In, MarshalAs(UnmanagedType.Interface)]
	//         object pRowPos);
}

[Guid("0000050E-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
internal interface Recordset15
{
	[Obsolete("not used", true)] void get_Properties(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_AbsolutePosition(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_AbsolutePosition(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void putref_ActiveConnection(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_ActiveConnection(/*deleted parameters signature*/);

	/*[return:MarshalAs(UnmanagedType.Variant)]*/
	object get_ActiveConnection();

	[Obsolete("not used", true)] void get_BOF(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Bookmark(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_Bookmark(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_CacheSize(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_CacheSize(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_CursorType(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_CursorType(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_EOF(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Fields(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_LockType(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_LockType(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_MaxRecords(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_MaxRecords(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_RecordCount(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void putref_Source(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_Source(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Source(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void AddNew(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void CancelUpdate(/*deleted parameters signature*/);

	[PreserveSig] OleDbHResult Close();

	[Obsolete("not used", true)] void Delete(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void GetRows(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void Move(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void MoveNext();
	[Obsolete("not used", true)] void MovePrevious();
	[Obsolete("not used", true)] void MoveFirst();
	[Obsolete("not used", true)] void MoveLast();
	[Obsolete("not used", true)] void Open(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void Requery(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void _xResync(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void Update(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_AbsolutePage(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_AbsolutePage(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_EditMode(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Filter(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_Filter(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_PageCount(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_PageSize(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_PageSize(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Sort(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_Sort(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Status(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_State(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void _xClone(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void UpdateBatch(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void CancelBatch(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_CursorLocation(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_CursorLocation(/*deleted parameters signature*/);

	[PreserveSig]
	OleDbHResult NextRecordset(
		[Out] out object RecordsAffected,
		[Out, MarshalAs(UnmanagedType.Interface)] out object ppiRs);

	//[ Obsolete("not used", true)] void Supports(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void get_Collect(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void put_Collect(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void get_MarshalOptions(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void put_MarshalOptions(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void Find(/*deleted parameters signature*/);
}

internal enum OleDbHResult
{ // OLEDB Error codes
	CO_E_CLASSSTRING = unchecked((int)0x800401f3),
	REGDB_E_CLASSNOTREG = unchecked((int)0x80040154),
	CO_E_NOTINITIALIZED = unchecked((int)0x800401F0),

	S_OK = 0x00000000,
	S_FALSE = 0x00000001,

	E_UNEXPECTED = unchecked((int)0x8000FFFF),
	E_NOTIMPL = unchecked((int)0x80004001),
	E_OUTOFMEMORY = unchecked((int)0x8007000E),
	E_INVALIDARG = unchecked((int)0x80070057),
	E_NOINTERFACE = unchecked((int)0x80004002),
	E_POINTER = unchecked((int)0x80004003),
	E_HANDLE = unchecked((int)0x80070006),
	E_ABORT = unchecked((int)0x80004004),
	E_FAIL = unchecked((int)0x80004005),
	E_ACCESSDENIED = unchecked((int)0x80070005),

	// MessageId: DB_E_BADACCESSORHANDLE
	// MessageText:
	//  Accessor is invalid.
	DB_E_BADACCESSORHANDLE = unchecked((int)0x80040E00),

	// MessageId: DB_E_ROWLIMITEXCEEDED
	// MessageText:
	//  Row could not be inserted into the rowset without exceeding provider's maximum number of active rows.
	DB_E_ROWLIMITEXCEEDED = unchecked((int)0x80040E01),

	// MessageId: DB_E_REOLEDBNLYACCESSOR
	// MessageText:
	//  Accessor is read-only. Operation failed.
	DB_E_REOLEDBNLYACCESSOR = unchecked((int)0x80040E02),

	// MessageId: DB_E_SCHEMAVIOLATION
	// MessageText:
	//  Values violate the database schema.
	DB_E_SCHEMAVIOLATION = unchecked((int)0x80040E03),

	// MessageId: DB_E_BADROWHANDLE
	// MessageText:
	//  Row handle is invalid.
	DB_E_BADROWHANDLE = unchecked((int)0x80040E04),

	// MessageId: DB_E_OBJECTOPEN
	// MessageText:
	//  Object was open.
	DB_E_OBJECTOPEN = unchecked((int)0x80040E05),

	// MessageId: DB_E_BADCHAPTER
	// MessageText:
	//  Chapter is invalid.
	DB_E_BADCHAPTER = unchecked((int)0x80040E06),

	// MessageId: DB_E_CANTCONVERTVALUE
	// MessageText:
	//  Data or literal value could not be converted to the type of the column in the data source, and the provider was unable to determine which columns could not be converted.  Data overflow or sign mismatch was not the cause.
	DB_E_CANTCONVERTVALUE = unchecked((int)0x80040E07),

	// MessageId: DB_E_BADBINDINFO
	// MessageText:
	//  Binding information is invalid.
	DB_E_BADBINDINFO = unchecked((int)0x80040E08),

	// MessageId: DB_SEC_E_PERMISSIONDENIED
	// MessageText:
	//  Permission denied.
	DB_SEC_E_PERMISSIONDENIED = unchecked((int)0x80040E09),

	// MessageId: DB_E_NOTAREFERENCECOLUMN
	// MessageText:
	//  Column does not contain bookmarks or chapters.
	DB_E_NOTAREFERENCECOLUMN = unchecked((int)0x80040E0A),

	// MessageId: DB_E_LIMITREJECTED
	// MessageText:
	//  Cost limits were rejected.
	DB_E_LIMITREJECTED = unchecked((int)0x80040E0B),

	// MessageId: DB_E_NOCOMMAND
	// MessageText:
	//  Command text was not set for the command object.
	DB_E_NOCOMMAND = unchecked((int)0x80040E0C),

	// MessageId: DB_E_COSTLIMIT
	// MessageText:
	//  Query plan within the cost limit cannot be found.
	DB_E_COSTLIMIT = unchecked((int)0x80040E0D),

	// MessageId: DB_E_BADBOOKMARK
	// MessageText:
	//  Bookmark is invalid.
	DB_E_BADBOOKMARK = unchecked((int)0x80040E0E),

	// MessageId: DB_E_BADLOCKMODE
	// MessageText:
	//  Lock mode is invalid.
	DB_E_BADLOCKMODE = unchecked((int)0x80040E0F),

	// MessageId: DB_E_PARAMNOTOPTIONAL
	// MessageText:
	//  No value given for one or more required parameters.
	DB_E_PARAMNOTOPTIONAL = unchecked((int)0x80040E10),

	// MessageId: DB_E_BADCOLUMNID
	// MessageText:
	//  Column ID is invalid.
	DB_E_BADCOLUMNID = unchecked((int)0x80040E11),

	// MessageId: DB_E_BADRATIO
	// MessageText:
	//  Numerator was greater than denominator. Values must express ratio between zero and 1.
	DB_E_BADRATIO = unchecked((int)0x80040E12),

	// MessageId: DB_E_BADVALUES
	// MessageText:
	//  Value is invalid.
	DB_E_BADVALUES = unchecked((int)0x80040E13),

	// MessageId: DB_E_ERRORSINCOMMAND
	// MessageText:
	//  One or more errors occurred during processing of command.
	DB_E_ERRORSINCOMMAND = unchecked((int)0x80040E14),

	// MessageId: DB_E_CANTCANCEL
	// MessageText:
	//  Command cannot be canceled.
	DB_E_CANTCANCEL = unchecked((int)0x80040E15),

	// MessageId: DB_E_DIALECTNOTSUPPORTED
	// MessageText:
	//  Command dialect is not supported by this provider.
	DB_E_DIALECTNOTSUPPORTED = unchecked((int)0x80040E16),

	// MessageId: DB_E_DUPLICATEDATASOURCE
	// MessageText:
	//  Data source object could not be created because the named data source already exists.
	DB_E_DUPLICATEDATASOURCE = unchecked((int)0x80040E17),

	// MessageId: DB_E_CANNOTRESTART
	// MessageText:
	//  Rowset position cannot be restarted.
	DB_E_CANNOTRESTART = unchecked((int)0x80040E18),

	// MessageId: DB_E_NOTFOUND
	// MessageText:
	//  Object or data matching the name, range, or selection criteria was not found within the scope of this operation.
	DB_E_NOTFOUND = unchecked((int)0x80040E19),

	// MessageId: DB_E_NEWLYINSERTED
	// MessageText:
	//  Identity cannot be determined for newly inserted rows.
	DB_E_NEWLYINSERTED = unchecked((int)0x80040E1B),

	// MessageId: DB_E_CANNOTFREE
	// MessageText:
	//  Provider has ownership of this tree.
	DB_E_CANNOTFREE = unchecked((int)0x80040E1A),

	// MessageId: DB_E_GOALREJECTED
	// MessageText:
	//  Goal was rejected because no nonzero weights were specified for any goals supported. Current goal was not changed.
	DB_E_GOALREJECTED = unchecked((int)0x80040E1C),

	// MessageId: DB_E_UNSUPPORTEDCONVERSION
	// MessageText:
	//  Requested conversion is not supported.
	DB_E_UNSUPPORTEDCONVERSION = unchecked((int)0x80040E1D),

	// MessageId: DB_E_BADSTARTPOSITION
	// MessageText:
	//  No rows were returned because the offset value moves the position before the beginning or after the end of the rowset.
	DB_E_BADSTARTPOSITION = unchecked((int)0x80040E1E),

	// MessageId: DB_E_NOQUERY
	// MessageText:
	//  Information was requested for a query and the query was not set.
	DB_E_NOQUERY = unchecked((int)0x80040E1F),

	// MessageId: DB_E_NOTREENTRANT
	// MessageText:
	//  Consumer's event handler called a non-reentrant method in the provider.
	DB_E_NOTREENTRANT = unchecked((int)0x80040E20),

	// MessageId: DB_E_ERRORSOCCURRED
	// MessageText:
	//  Multiple-step operation generated errors. Check each status value. No work was done.
	DB_E_ERRORSOCCURRED = unchecked((int)0x80040E21),

	// MessageId: DB_E_NOAGGREGATION
	// MessageText:
	//  Non-NULL controlling IUnknown was specified, and either the requested interface was not
	//  IUnknown, or the provider does not support COM aggregation.
	DB_E_NOAGGREGATION = unchecked((int)0x80040E22),

	// MessageId: DB_E_DELETEDROW
	// MessageText:
	//  Row handle referred to a deleted row or a row marked for deletion.
	DB_E_DELETEDROW = unchecked((int)0x80040E23),

	// MessageId: DB_E_CANTFETCHBACKWARDS
	// MessageText:
	//  Rowset does not support fetching backward.
	DB_E_CANTFETCHBACKWARDS = unchecked((int)0x80040E24),

	// MessageId: DB_E_ROWSNOTRELEASED
	// MessageText:
	//  Row handles must all be released before new ones can be obtained.
	DB_E_ROWSNOTRELEASED = unchecked((int)0x80040E25),

	// MessageId: DB_E_BADSTORAGEFLAG
	// MessageText:
	//  One or more storage flags are not supported.
	DB_E_BADSTORAGEFLAG = unchecked((int)0x80040E26),

	// MessageId: DB_E_BADCOMPAREOP
	// MessageText:
	//  Comparison operator is invalid.
	DB_E_BADCOMPAREOP = unchecked((int)0x80040E27),

	// MessageId: DB_E_BADSTATUSVALUE
	// MessageText:
	//  Status flag was neither DBCOLUMNSTATUS_OK nor
	//  DBCOLUMNSTATUS_ISNULL.
	DB_E_BADSTATUSVALUE = unchecked((int)0x80040E28),

	// MessageId: DB_E_CANTSCROLLBACKWARDS
	// MessageText:
	//  Rowset does not support scrolling backward.
	DB_E_CANTSCROLLBACKWARDS = unchecked((int)0x80040E29),

	// MessageId: DB_E_BADREGIONHANDLE
	// MessageText:
	//  Region handle is invalid.
	DB_E_BADREGIONHANDLE = unchecked((int)0x80040E2A),

	// MessageId: DB_E_NONCONTIGUOUSRANGE
	// MessageText:
	//  Set of rows is not contiguous to, or does not overlap, the rows in the watch region.
	DB_E_NONCONTIGUOUSRANGE = unchecked((int)0x80040E2B),

	// MessageId: DB_E_INVALIDTRANSITION
	// MessageText:
	//  Transition from ALL* to MOVE* or EXTEND* was specified.
	DB_E_INVALIDTRANSITION = unchecked((int)0x80040E2C),

	// MessageId: DB_E_NOTASUBREGION
	// MessageText:
	//  Region is not a proper subregion of the region identified by the watch region handle.
	DB_E_NOTASUBREGION = unchecked((int)0x80040E2D),

	// MessageId: DB_E_MULTIPLESTATEMENTS
	// MessageText:
	//  Multiple-statement commands are not supported by this provider.
	DB_E_MULTIPLESTATEMENTS = unchecked((int)0x80040E2E),

	// MessageId: DB_E_INTEGRITYVIOLATION
	// MessageText:
	//  Value violated the integrity constraints for a column or table.
	DB_E_INTEGRITYVIOLATION = unchecked((int)0x80040E2F),

	// MessageId: DB_E_BADTYPENAME
	// MessageText:
	//  Type name is invalid.
	DB_E_BADTYPENAME = unchecked((int)0x80040E30),

	// MessageId: DB_E_ABORTLIMITREACHED
	// MessageText:
	//  Execution stopped because a resource limit was reached. No results were returned.
	DB_E_ABORTLIMITREACHED = unchecked((int)0x80040E31),

	// MessageId: DB_E_ROWSETINCOMMAND
	// MessageText:
	//  Command object whose command tree contains a rowset or rowsets cannot be cloned.
	DB_E_ROWSETINCOMMAND = unchecked((int)0x80040E32),

	// MessageId: DB_E_CANTTRANSLATE
	// MessageText:
	//  Current tree cannot be represented as text.
	DB_E_CANTTRANSLATE = unchecked((int)0x80040E33),

	// MessageId: DB_E_DUPLICATEINDEXID
	// MessageText:
	//  Index already exists.
	DB_E_DUPLICATEINDEXID = unchecked((int)0x80040E34),

	// MessageId: DB_E_NOINDEX
	// MessageText:
	//  Index does not exist.
	DB_E_NOINDEX = unchecked((int)0x80040E35),

	// MessageId: DB_E_INDEXINUSE
	// MessageText:
	//  Index is in use.
	DB_E_INDEXINUSE = unchecked((int)0x80040E36),

	// MessageId: DB_E_NOTABLE
	// MessageText:
	//  Table does not exist.
	DB_E_NOTABLE = unchecked((int)0x80040E37),

	// MessageId: DB_E_CONCURRENCYVIOLATION
	// MessageText:
	//  Rowset used optimistic concurrency and the value of a column has changed since it was last read.
	DB_E_CONCURRENCYVIOLATION = unchecked((int)0x80040E38),

	// MessageId: DB_E_BADCOPY
	// MessageText:
	//  Errors detected during the copy.
	DB_E_BADCOPY = unchecked((int)0x80040E39),

	// MessageId: DB_E_BADPRECISION
	// MessageText:
	//  Precision is invalid.
	DB_E_BADPRECISION = unchecked((int)0x80040E3A),

	// MessageId: DB_E_BADSCALE
	// MessageText:
	//  Scale is invalid.
	DB_E_BADSCALE = unchecked((int)0x80040E3B),

	// MessageId: DB_E_BADTABLEID
	// MessageText:
	//  Table ID is invalid.
	DB_E_BADTABLEID = unchecked((int)0x80040E3C),

	// MessageId: DB_E_BADTYPE
	// MessageText:
	//  Type is invalid.
	DB_E_BADTYPE = unchecked((int)0x80040E3D),

	// MessageId: DB_E_DUPLICATECOLUMNID
	// MessageText:
	//  Column ID already exists or occurred more than once in the array of columns.
	DB_E_DUPLICATECOLUMNID = unchecked((int)0x80040E3E),

	// MessageId: DB_E_DUPLICATETABLEID
	// MessageText:
	//  Table already exists.
	DB_E_DUPLICATETABLEID = unchecked((int)0x80040E3F),

	// MessageId: DB_E_TABLEINUSE
	// MessageText:
	//  Table is in use.
	DB_E_TABLEINUSE = unchecked((int)0x80040E40),

	// MessageId: DB_E_NOLOCALE
	// MessageText:
	//  Locale ID is not supported.
	DB_E_NOLOCALE = unchecked((int)0x80040E41),

	// MessageId: DB_E_BADRECORDNUM
	// MessageText:
	//  Record number is invalid.
	DB_E_BADRECORDNUM = unchecked((int)0x80040E42),

	// MessageId: DB_E_BOOKMARKSKIPPED
	// MessageText:
	//  Form of bookmark is valid, but no row was found to match it.
	DB_E_BOOKMARKSKIPPED = unchecked((int)0x80040E43),


	// MessageId: DB_E_BADPROPERTYVALUE
	// MessageText:
	//  Property value is invalid.
	DB_E_BADPROPERTYVALUE = unchecked((int)0x80040E44),

	// MessageId: DB_E_INVALID
	// MessageText:
	//  Rowset is not chaptered.
	DB_E_INVALID = unchecked((int)0x80040E45),

	// MessageId: DB_E_BADACCESSORFLAGS
	// MessageText:
	//  One or more accessor flags were invalid.
	DB_E_BADACCESSORFLAGS = unchecked((int)0x80040E46),

	// MessageId: DB_E_BADSTORAGEFLAGS
	// MessageText:
	//  One or more storage flags are invalid.
	DB_E_BADSTORAGEFLAGS = unchecked((int)0x80040E47),

	// MessageId: DB_E_BYREFACCESSORNOTSUPPORTED
	// MessageText:
	//  Reference accessors are not supported by this provider.
	DB_E_BYREFACCESSORNOTSUPPORTED = unchecked((int)0x80040E48),

	// MessageId: DB_E_NULLACCESSORNOTSUPPORTED
	// MessageText:
	//  Null accessors are not supported by this provider.
	DB_E_NULLACCESSORNOTSUPPORTED = unchecked((int)0x80040E49),

	// MessageId: DB_E_NOTPREPARED
	// MessageText:
	//  Command was not prepared.
	DB_E_NOTPREPARED = unchecked((int)0x80040E4A),

	// MessageId: DB_E_BADACCESSORTYPE
	// MessageText:
	//  Accessor is not a parameter accessor.
	DB_E_BADACCESSORTYPE = unchecked((int)0x80040E4B),

	// MessageId: DB_E_WRITEONLYACCESSOR
	// MessageText:
	//  Accessor is write-only.
	DB_E_WRITEONLYACCESSOR = unchecked((int)0x80040E4C),

	// MessageId: DB_SEC_E_AUTH_FAILED
	// MessageText:
	//  Authentication failed.
	DB_SEC_E_AUTH_FAILED = unchecked((int)0x80040E4D),

	// MessageId: DB_E_CANCELED
	// MessageText:
	//  Operation was canceled.
	DB_E_CANCELED = unchecked((int)0x80040E4E),

	// MessageId: DB_E_CHAPTERNOTRELEASED
	// MessageText:
	//  Rowset is single-chaptered. The chapter was not released.
	DB_E_CHAPTERNOTRELEASED = unchecked((int)0x80040E4F),

	// MessageId: DB_E_BADSOURCEHANDLE
	// MessageText:
	//  Source handle is invalid.
	DB_E_BADSOURCEHANDLE = unchecked((int)0x80040E50),

	// MessageId: DB_E_PARAMUNAVAILABLE
	// MessageText:
	//  Provider cannot derive parameter information and SetParameterInfo has not been called.
	DB_E_PARAMUNAVAILABLE = unchecked((int)0x80040E51),

	// MessageId: DB_E_ALREADYINITIALIZED
	// MessageText:
	//  Data source object is already initialized.
	DB_E_ALREADYINITIALIZED = unchecked((int)0x80040E52),

	// MessageId: DB_E_NOTSUPPORTED
	// MessageText:
	//  Method is not supported by this provider.
	DB_E_NOTSUPPORTED = unchecked((int)0x80040E53),

	// MessageId: DB_E_MAXPENDCHANGESEXCEEDED
	// MessageText:
	//  Number of rows with pending changes exceeded the limit.
	DB_E_MAXPENDCHANGESEXCEEDED = unchecked((int)0x80040E54),

	// MessageId: DB_E_BADORDINAL
	// MessageText:
	//  Column does not exist.
	DB_E_BADORDINAL = unchecked((int)0x80040E55),

	// MessageId: DB_E_PENDINGCHANGES
	// MessageText:
	//  Pending changes exist on a row with a reference count of zero.
	DB_E_PENDINGCHANGES = unchecked((int)0x80040E56),

	// MessageId: DB_E_DATAOVERFLOW
	// MessageText:
	//  Literal value in the command exceeded the range of the type of the associated column.
	DB_E_DATAOVERFLOW = unchecked((int)0x80040E57),

	// MessageId: DB_E_BADHRESULT
	// MessageText:
	//  HRESULT is invalid.
	DB_E_BADHRESULT = unchecked((int)0x80040E58),

	// MessageId: DB_E_BADLOOKUPID
	// MessageText:
	//  Lookup ID is invalid.
	DB_E_BADLOOKUPID = unchecked((int)0x80040E59),

	// MessageId: DB_E_BADDYNAMICERRORID
	// MessageText:
	//  DynamicError ID is invalid.
	DB_E_BADDYNAMICERRORID = unchecked((int)0x80040E5A),

	// MessageId: DB_E_PENDINGINSERT
	// MessageText:
	//  Most recent data for a newly inserted row could not be retrieved because the insert is pending.
	DB_E_PENDINGINSERT = unchecked((int)0x80040E5B),

	// MessageId: DB_E_BADCONVERTFLAG
	// MessageText:
	//  Conversion flag is invalid.
	DB_E_BADCONVERTFLAG = unchecked((int)0x80040E5C),

	// MessageId: DB_E_BADPARAMETERNAME
	// MessageText:
	//  Parameter name is unrecognized.
	DB_E_BADPARAMETERNAME = unchecked((int)0x80040E5D),

	// MessageId: DB_E_MULTIPLESTORAGE
	// MessageText:
	//  Multiple storage objects cannot be open simultaneously.
	DB_E_MULTIPLESTORAGE = unchecked((int)0x80040E5E),

	// MessageId: DB_E_CANTFILTER
	// MessageText:
	//  Filter cannot be opened.
	DB_E_CANTFILTER = unchecked((int)0x80040E5F),

	// MessageId: DB_E_CANTORDER
	// MessageText:
	//  Order cannot be opened.
	DB_E_CANTORDER = unchecked((int)0x80040E60),

	// MessageId: MD_E_BADTUPLE
	// MessageText:
	//  Tuple is invalid.
	MD_E_BADTUPLE = unchecked((int)0x80040E61),

	// MessageId: MD_E_BADCOORDINATE
	// MessageText:
	//  Coordinate is invalid.
	MD_E_BADCOORDINATE = unchecked((int)0x80040E62),

	// MessageId: MD_E_INVALIDAXIS
	// MessageText:
	//  Axis is invalid.
	MD_E_INVALIDAXIS = unchecked((int)0x80040E63),

	// MessageId: MD_E_INVALIDCELLRANGE
	// MessageText:
	//  One or more cell ordinals is invalid.
	MD_E_INVALIDCELLRANGE = unchecked((int)0x80040E64),

	// MessageId: DB_E_NOCOLUMN
	// MessageText:
	//  Column ID is invalid.
	DB_E_NOCOLUMN = unchecked((int)0x80040E65),

	// MessageId: DB_E_COMMANDNOTPERSISTED
	// MessageText:
	//  Command does not have a DBID.
	DB_E_COMMANDNOTPERSISTED = unchecked((int)0x80040E67),

	// MessageId: DB_E_DUPLICATEID
	// MessageText:
	//  DBID already exists.
	DB_E_DUPLICATEID = unchecked((int)0x80040E68),

	// MessageId: DB_E_OBJECTCREATIONLIMITREACHED
	// MessageText:
	//  Session cannot be created because maximum number of active sessions was already reached. Consumer must release one or more sessions before creating a new session object.
	DB_E_OBJECTCREATIONLIMITREACHED = unchecked((int)0x80040E69),

	// MessageId: DB_E_BADINDEXID
	// MessageText:
	//  Index ID is invalid.
	DB_E_BADINDEXID = unchecked((int)0x80040E72),

	// MessageId: DB_E_BADINITSTRING
	// MessageText:
	//  Format of the initialization string does not conform to the OLE DB specification.
	DB_E_BADINITSTRING = unchecked((int)0x80040E73),

	// MessageId: DB_E_NOPROVIDERSREGISTERED
	// MessageText:
	//  No OLE DB providers of this source type are registered.
	DB_E_NOPROVIDERSREGISTERED = unchecked((int)0x80040E74),

	// MessageId: DB_E_MISMATCHEDPROVIDER
	// MessageText:
	//  Initialization string specifies a provider that does not match the active provider.
	DB_E_MISMATCHEDPROVIDER = unchecked((int)0x80040E75),

	// MessageId: DB_E_BADCOMMANDID
	// MessageText:
	//  DBID is invalid.
	DB_E_BADCOMMANDID = unchecked((int)0x80040E76),

	// MessageId: SEC_E_BADTRUSTEEID
	// MessageText:
	//  Trustee is invalid.
	SEC_E_BADTRUSTEEID = unchecked((int)0x80040E6A),

	// MessageId: SEC_E_NOTRUSTEEID
	// MessageText:
	//  Trustee was not recognized for this data source.
	SEC_E_NOTRUSTEEID = unchecked((int)0x80040E6B),

	// MessageId: SEC_E_NOMEMBERSHIPSUPPORT
	// MessageText:
	//  Trustee does not support memberships or collections.
	SEC_E_NOMEMBERSHIPSUPPORT = unchecked((int)0x80040E6C),

	// MessageId: SEC_E_INVALIDOBJECT
	// MessageText:
	//  Object is invalid or unknown to the provider.
	SEC_E_INVALIDOBJECT = unchecked((int)0x80040E6D),

	// MessageId: SEC_E_NOOWNER
	// MessageText:
	//  Object does not have an owner.
	SEC_E_NOOWNER = unchecked((int)0x80040E6E),

	// MessageId: SEC_E_INVALIDACCESSENTRYLIST
	// MessageText:
	//  Access entry list is invalid.
	SEC_E_INVALIDACCESSENTRYLIST = unchecked((int)0x80040E6F),

	// MessageId: SEC_E_INVALIDOWNER
	// MessageText:
	//  Trustee supplied as owner is invalid or unknown to the provider.
	SEC_E_INVALIDOWNER = unchecked((int)0x80040E70),

	// MessageId: SEC_E_INVALIDACCESSENTRY
	// MessageText:
	//  Permission in the access entry list is invalid.
	SEC_E_INVALIDACCESSENTRY = unchecked((int)0x80040E71),

	// MessageId: DB_E_BADCONSTRAINTTYPE
	// MessageText:
	//  ConstraintType is invalid or not supported by the provider.
	DB_E_BADCONSTRAINTTYPE = unchecked((int)0x80040E77),

	// MessageId: DB_E_BADCONSTRAINTFORM
	// MessageText:
	//  ConstraintType is not DBCONSTRAINTTYPE_FOREIGNKEY and cForeignKeyColumns is not zero.
	DB_E_BADCONSTRAINTFORM = unchecked((int)0x80040E78),

	// MessageId: DB_E_BADDEFERRABILITY
	// MessageText:
	//  Specified deferrability flag is invalid or not supported by the provider.
	DB_E_BADDEFERRABILITY = unchecked((int)0x80040E79),

	// MessageId: DB_E_BADMATCHTYPE
	// MessageText:
	//  MatchType is invalid or the value is not supported by the provider.
	DB_E_BADMATCHTYPE = unchecked((int)0x80040E80),

	// MessageId: DB_E_BADUPDATEDELETERULE
	// MessageText:
	//  Constraint update rule or delete rule is invalid.
	DB_E_BADUPDATEDELETERULE = unchecked((int)0x80040E8A),

	// MessageId: DB_E_BADCONSTRAINTID
	// MessageText:
	//  Constraint does not exist.
	DB_E_BADCONSTRAINTID = unchecked((int)0x80040E8B),

	// MessageId: DB_E_BADCOMMANDFLAGS
	// MessageText:
	//  Command persistence flag is invalid.
	DB_E_BADCOMMANDFLAGS = unchecked((int)0x80040E8C),

	// MessageId: DB_E_OBJECTMISMATCH
	// MessageText:
	//  rguidColumnType points to a GUID that does not match the object type of this column, or this column was not set.
	DB_E_OBJECTMISMATCH = unchecked((int)0x80040E8D),

	// MessageId: DB_E_NOSOURCEOBJECT
	// MessageText:
	//  Source row does not exist.
	DB_E_NOSOURCEOBJECT = unchecked((int)0x80040E91),

	// MessageId: DB_E_RESOURCELOCKED
	// MessageText:
	//  OLE DB object represented by this URL is locked by one or more other processes.
	DB_E_RESOURCELOCKED = unchecked((int)0x80040E92),

	// MessageId: DB_E_NOTCOLLECTION
	// MessageText:
	//  Client requested an object type that is valid only for a collection.
	DB_E_NOTCOLLECTION = unchecked((int)0x80040E93),

	// MessageId: DB_E_REOLEDBNLY
	// MessageText:
	//  Caller requested write access to a read-only object.
	DB_E_REOLEDBNLY = unchecked((int)0x80040E94),

	// MessageId: DB_E_ASYNCNOTSUPPORTED
	// MessageText:
	//  Asynchronous binding is not supported by this provider.
	DB_E_ASYNCNOTSUPPORTED = unchecked((int)0x80040E95),

	// MessageId: DB_E_CANNOTCONNECT
	// MessageText:
	//  Connection to the server for this URL cannot be established.
	DB_E_CANNOTCONNECT = unchecked((int)0x80040E96),

	// MessageId: DB_E_TIMEOUT
	// MessageText:
	//  Timeout occurred when attempting to bind to the object.
	DB_E_TIMEOUT = unchecked((int)0x80040E97),

	// MessageId: DB_E_RESOURCEEXISTS
	// MessageText:
	//  Object cannot be created at this URL because an object named by this URL already exists.
	DB_E_RESOURCEEXISTS = unchecked((int)0x80040E98),

	// MessageId: DB_E_RESOURCEOUTOFSCOPE
	// MessageText:
	//  URL is outside of scope.
	DB_E_RESOURCEOUTOFSCOPE = unchecked((int)0x80040E8E),

	// MessageId: DB_E_DROPRESTRICTED
	// MessageText:
	//  Column or constraint could not be dropped because it is referenced by a dependent view or constraint.
	DB_E_DROPRESTRICTED = unchecked((int)0x80040E90),

	// MessageId: DB_E_DUPLICATECONSTRAINTID
	// MessageText:
	//  Constraint already exists.
	DB_E_DUPLICATECONSTRAINTID = unchecked((int)0x80040E99),

	// MessageId: DB_E_OUTOFSPACE
	// MessageText:
	//  Object cannot be created at this URL because the server is out of physical storage.
	DB_E_OUTOFSPACE = unchecked((int)0x80040E9A),

	// MessageId: DB_SEC_E_SAFEMODE_DENIED
	// MessageText:
	//  Safety settings on this computer prohibit accessing a data source on another domain.
	DB_SEC_E_SAFEMODE_DENIED = unchecked((int)0x80040E9B),

	// MessageId: DB_S_ROWLIMITEXCEEDED
	// MessageText:
	//  Fetching requested number of rows will exceed total number of active rows supported by the rowset.
	DB_S_ROWLIMITEXCEEDED = 0x00040EC0,

	// MessageId: DB_S_COLUMNTYPEMISMATCH
	// MessageText:
	//  One or more column types are incompatible. Conversion errors will occur during copying.
	DB_S_COLUMNTYPEMISMATCH = 0x00040EC1,

	// MessageId: DB_S_TYPEINFOOVERRIDDEN
	// MessageText:
	//  Parameter type information was overridden by caller.
	DB_S_TYPEINFOOVERRIDDEN = 0x00040EC2,

	// MessageId: DB_S_BOOKMARKSKIPPED
	// MessageText:
	//  Bookmark was skipped for deleted or nonmember row.
	DB_S_BOOKMARKSKIPPED = 0x00040EC3,

	// MessageId: DB_S_NONEXTROWSET
	// MessageText:
	//  No more rowsets.
	DB_S_NONEXTROWSET = 0x00040EC5,

	// MessageId: DB_S_ENDOFROWSET
	// MessageText:
	//  Start or end of rowset or chapter was reached.
	DB_S_ENDOFROWSET = 0x00040EC6,

	// MessageId: DB_S_COMMANDREEXECUTED
	// MessageText:
	//  Command was reexecuted.
	DB_S_COMMANDREEXECUTED = 0x00040EC7,

	// MessageId: DB_S_BUFFERFULL
	// MessageText:
	//  Operation succeeded, but status array or string buffer could not be allocated.
	DB_S_BUFFERFULL = 0x00040EC8,

	// MessageId: DB_S_NORESULT
	// MessageText:
	//  No more results.
	DB_S_NORESULT = 0x00040EC9,

	// MessageId: DB_S_CANTRELEASE
	// MessageText:
	//  Server cannot release or downgrade a lock until the end of the transaction.
	DB_S_CANTRELEASE = 0x00040ECA,

	// MessageId: DB_S_GOALCHANGED
	// MessageText:
	//  Weight is not supported or exceeded the supported limit, and was set to 0 or the supported limit.
	DB_S_GOALCHANGED = 0x00040ECB,

	// MessageId: DB_S_UNWANTEDOPERATION
	// MessageText:
	//  Consumer does not want to receive further notification calls for this operation.
	DB_S_UNWANTEDOPERATION = 0x00040ECC,

	// MessageId: DB_S_DIALECTIGNORED
	// MessageText:
	//  Input dialect was ignored and command was processed using default dialect.
	DB_S_DIALECTIGNORED = 0x00040ECD,

	// MessageId: DB_S_UNWANTEDPHASE
	// MessageText:
	//  Consumer does not want to receive further notification calls for this phase.
	DB_S_UNWANTEDPHASE = 0x00040ECE,

	// MessageId: DB_S_UNWANTEDREASON
	// MessageText:
	//  Consumer does not want to receive further notification calls for this reason.
	DB_S_UNWANTEDREASON = 0x00040ECF,

	// MessageId: DB_S_ASYNCHRONOUS
	// MessageText:
	//  Operation is being processed asynchronously.
	DB_S_ASYNCHRONOUS = 0x00040ED0,

	// MessageId: DB_S_COLUMNSCHANGED
	// MessageText:
	//  Command was executed to reposition to the start of the rowset. Either the order of the columns changed, or columns were added to or removed from the rowset.
	DB_S_COLUMNSCHANGED = 0x00040ED1,

	// MessageId: DB_S_ERRORSRETURNED
	// MessageText:
	//  Method had some errors, which were returned in the error array.
	DB_S_ERRORSRETURNED = 0x00040ED2,

	// MessageId: DB_S_BADROWHANDLE
	// MessageText:
	//  Row handle is invalid.
	DB_S_BADROWHANDLE = 0x00040ED3,

	// MessageId: DB_S_DELETEDROW
	// MessageText:
	//  Row handle referred to a deleted row.
	DB_S_DELETEDROW = 0x00040ED4,

	// MessageId: DB_S_TOOMANYCHANGES
	// MessageText:
	//  Provider cannot keep track of all the changes. Client must refetch the data associated with the watch region by using another method.
	DB_S_TOOMANYCHANGES = 0x00040ED5,

	// MessageId: DB_S_STOPLIMITREACHED
	// MessageText:
	//  Execution stopped because a resource limit was reached. Results obtained so far were returned, but execution cannot resume.
	DB_S_STOPLIMITREACHED = 0x00040ED6,

	// MessageId: DB_S_LOCKUPGRADED
	// MessageText:
	//  Lock was upgraded from the value specified.
	DB_S_LOCKUPGRADED = 0x00040ED8,

	// MessageId: DB_S_PROPERTIESCHANGED
	// MessageText:
	//  One or more properties were changed as allowed by provider.
	DB_S_PROPERTIESCHANGED = 0x00040ED9,

	// MessageId: DB_S_ERRORSOCCURRED
	// MessageText:
	//  Multiple-step operation completed with one or more errors. Check each status value.
	DB_S_ERRORSOCCURRED = 0x00040EDA,

	// MessageId: DB_S_PARAMUNAVAILABLE
	// MessageText:
	//  Parameter is invalid.
	DB_S_PARAMUNAVAILABLE = 0x00040EDB,

	// MessageId: DB_S_MULTIPLECHANGES
	// MessageText:
	//  Updating a row caused more than one row to be updated in the data source.
	DB_S_MULTIPLECHANGES = 0x00040EDC,

	// MessageId: DB_S_NOTSINGLETON
	// MessageText:
	//  Row object was requested on a non-singleton result. First row was returned.
	DB_S_NOTSINGLETON = 0x00040ED7,

	// MessageId: DB_S_NOROWSPECIFICCOLUMNS
	// MessageText:
	//  Row has no row-specific columns.
	DB_S_NOROWSPECIFICCOLUMNS = 0x00040EDD,

	XACT_E_FIRST = unchecked((int)0x8004d000),
	XACT_E_LAST = unchecked((int)0x8004d022),
	XACT_S_FIRST = 0x4d000,
	XACT_S_LAST = 0x4d009,
	XACT_E_ALREADYOTHERSINGLEPHASE = unchecked((int)0x8004d000),
	XACT_E_CANTRETAIN = unchecked((int)0x8004d001),
	XACT_E_COMMITFAILED = unchecked((int)0x8004d002),
	XACT_E_COMMITPREVENTED = unchecked((int)0x8004d003),
	XACT_E_HEURISTICABORT = unchecked((int)0x8004d004),
	XACT_E_HEURISTICCOMMIT = unchecked((int)0x8004d005),
	XACT_E_HEURISTICDAMAGE = unchecked((int)0x8004d006),
	XACT_E_HEURISTICDANGER = unchecked((int)0x8004d007),
	XACT_E_ISOLATIONLEVEL = unchecked((int)0x8004d008),
	XACT_E_NOASYNC = unchecked((int)0x8004d009),
	XACT_E_NOENLIST = unchecked((int)0x8004d00a),
	XACT_E_NOISORETAIN = unchecked((int)0x8004d00b),
	XACT_E_NORESOURCE = unchecked((int)0x8004d00c),
	XACT_E_NOTCURRENT = unchecked((int)0x8004d00d),
	XACT_E_NOTRANSACTION = unchecked((int)0x8004d00e),
	XACT_E_NOTSUPPORTED = unchecked((int)0x8004d00f),
	XACT_E_UNKNOWNRMGRID = unchecked((int)0x8004d010),
	XACT_E_WRONGSTATE = unchecked((int)0x8004d011),
	XACT_E_WRONGUOW = unchecked((int)0x8004d012),
	XACT_E_XTIONEXISTS = unchecked((int)0x8004d013),
	XACT_E_NOIMPORTOBJECT = unchecked((int)0x8004d014),
	XACT_E_INVALIDCOOKIE = unchecked((int)0x8004d015),
	XACT_E_INDOUBT = unchecked((int)0x8004d016),
	XACT_E_NOTIMEOUT = unchecked((int)0x8004d017),
	XACT_E_ALREADYINPROGRESS = unchecked((int)0x8004d018),
	XACT_E_ABORTED = unchecked((int)0x8004d019),
	XACT_E_LOGFULL = unchecked((int)0x8004d01a),
	XACT_E_TMNOTAVAILABLE = unchecked((int)0x8004d01b),
	XACT_E_CONNECTION_DOWN = unchecked((int)0x8004d01c),
	XACT_E_CONNECTION_DENIED = unchecked((int)0x8004d01d),
	XACT_E_REENLISTTIMEOUT = unchecked((int)0x8004d01e),
	XACT_E_TIP_CONNECT_FAILED = unchecked((int)0x8004d01f),
	XACT_E_TIP_PROTOCOL_ERROR = unchecked((int)0x8004d020),
	XACT_E_TIP_PULL_FAILED = unchecked((int)0x8004d021),
	XACT_E_DEST_TMNOTAVAILABLE = unchecked((int)0x8004d022),
	XACT_E_CLERKNOTFOUND = unchecked((int)0x8004d080),
	XACT_E_CLERKEXISTS = unchecked((int)0x8004d081),
	XACT_E_RECOVERYINPROGRESS = unchecked((int)0x8004d082),
	XACT_E_TRANSACTIONCLOSED = unchecked((int)0x8004d083),
	XACT_E_INVALIDLSN = unchecked((int)0x8004d084),
	XACT_E_REPLAYREQUEST = unchecked((int)0x8004d085),
	XACT_S_ASYNC = 0x4d000,
	XACT_S_DEFECT = 0x4d001,
	XACT_S_REOLEDBNLY = 0x4d002,
	XACT_S_SOMENORETAIN = 0x4d003,
	XACT_S_OKINFORM = 0x4d004,
	XACT_S_MADECHANGESCONTENT = 0x4d005,
	XACT_S_MADECHANGESINFORM = 0x4d006,
	XACT_S_ALLNORETAIN = 0x4d007,
	XACT_S_ABORTING = 0x4d008,
	XACT_S_SINGLEPHASE = 0x4d009,

	STG_E_INVALIDFUNCTION = unchecked((int)0x80030001),
	STG_E_FILENOTFOUND = unchecked((int)0x80030002),
	STG_E_PATHNOTFOUND = unchecked((int)0x80030003),
	STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004),
	STG_E_ACCESSDENIED = unchecked((int)0x80030005),
	STG_E_INVALIDHANDLE = unchecked((int)0x80030006),
	STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008),
	STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
	STG_E_NOMOREFILES = unchecked((int)0x80030012),
	STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013),
	STG_E_SEEKERROR = unchecked((int)0x80030019),
	STG_E_WRITEFAULT = unchecked((int)0x8003001D),
	STG_E_READFAULT = unchecked((int)0x8003001E),
	STG_E_SHAREVIOLATION = unchecked((int)0x80030020),
	STG_E_LOCKVIOLATION = unchecked((int)0x80030021),
	STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050),
	STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
	STG_E_MEDIUMFULL = unchecked((int)0x80030070),
	STG_E_PROPSETMISMATCHED = unchecked((int)0x800300F0),
	STG_E_ABNORMALAPIEXIT = unchecked((int)0x800300FA),
	STG_E_INVALIDHEADER = unchecked((int)0x800300FB),
	STG_E_INVALIDNAME = unchecked((int)0x800300FC),
	STG_E_UNKNOWN = unchecked((int)0x800300FD),
	STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE),
	STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
	STG_E_INUSE = unchecked((int)0x80030100),
	STG_E_NOTCURRENT = unchecked((int)0x80030101),
	STG_E_REVERTED = unchecked((int)0x80030102),
	STG_E_CANTSAVE = unchecked((int)0x80030103),
	STG_E_OLDFORMAT = unchecked((int)0x80030104),
	STG_E_OLDDLL = unchecked((int)0x80030105),
	STG_E_SHAREREQUIRED = unchecked((int)0x80030106),
	STG_E_NOTFILEBASEDSTORAGE = unchecked((int)0x80030107),
	STG_E_EXTANTMARSHALLINGS = unchecked((int)0x80030108),
	STG_E_DOCFILECORRUPT = unchecked((int)0x80030109),
	STG_E_BADBASEADDRESS = unchecked((int)0x80030110),
	STG_E_INCOMPLETE = unchecked((int)0x80030201),
	STG_E_TERMINATED = unchecked((int)0x80030202),
	STG_S_CONVERTED = 0x00030200,
	STG_S_BLOCK = 0x00030201,
	STG_S_RETRYNOW = 0x00030202,
	STG_S_MONITORING = 0x00030203,
}

[Guid("00000562-0000-0010-8000-00AA006D2EA4"), InterfaceType(ComInterfaceType.InterfaceIsDual), ComImport, SuppressUnmanagedCodeSecurity]
internal interface _ADORecord
{
	[Obsolete("not used", true)] void get_Properties(/*deleted parameters signature*/);

	/*[return:MarshalAs(UnmanagedType.Variant)]*/
	object get_ActiveConnection();

	[Obsolete("not used", true)] void put_ActiveConnection(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void putref_ActiveConnection(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_State(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Source(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_Source(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void putref_Source(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_Mode(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void put_Mode(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void get_ParentURL(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void MoveRecord(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void CopyRecord(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void DeleteRecord(/*deleted parameters signature*/);
	[Obsolete("not used", true)] void Open(/*deleted parameters signature*/);

	[PreserveSig] OleDbHResult Close();

	//[ Obsolete("not used", true)] void get_Fields(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void get_RecordType(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void GetChildren(/*deleted parameters signature*/);
	//[ Obsolete("not used", true)] void Cancel();
}

/*
		typedef ULONGLONG           DBLENGTH;

		// Offset within a rowset
		typedef LONGLONG                DBROWOFFSET;

		// Number of rows
		typedef LONGLONG                DBROWCOUNT;

		typedef ULONGLONG           DBCOUNTITEM;

		// Ordinal (column number, etc.)
		typedef ULONGLONG           DBORDINAL;

		typedef LONGLONG                DB_LORDINAL;

		// Bookmarks
		typedef ULONGLONG           DBBKMARK;
		// Offset in the buffer

		typedef ULONGLONG           DBBYTEOFFSET;
		// Reference count of each row/accessor  handle

		typedef ULONG               DBREFCOUNT;

		// Parameters
		typedef ULONGLONG           DB_UPARAMS;

		typedef LONGLONG                DB_LPARAMS;

		// hash values corresponding to the elements (bookmarks)
		typedef DWORDLONG           DBHASHVALUE;

		// For reserve
		typedef DWORDLONG           DB_DWRESERVE;

		typedef LONGLONG                DB_LRESERVE;

		typedef ULONGLONG           DB_URESERVE;
*/



[ComImport, Guid("0C733A8C-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), SuppressUnmanagedCodeSecurity]
internal interface IAccessor
{

	[Obsolete("not used", true)] void AddRefAccessor(/*deleted parameters signature*/);

	/*[local]
		HRESULT CreateAccessor(
			[in] DBACCESSORFLAGS dwAccessorFlags,
			[in] DBCOUNTITEM cBindings,
			[in, size_is(cBindings)] const DBBINDING rgBindings[],
			[in] DBLENGTH cbRowSize,
			[out] HACCESSOR * phAccessor,
			[out, size_is(cBindings)] DBBINDSTATUS rgStatus[]
		);*/
	[PreserveSig]
	OleDbHResult CreateAccessor(
		[In] int dwAccessorFlags,
		[In] IntPtr cBindings,
		[In] /*tagDBBINDING[]*/SafeHandle rgBindings,
		[In] IntPtr cbRowSize,
		[Out] out IntPtr phAccessor,
		[In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4)] int[] rgStatus);

	[Obsolete("not used", true)] void GetBindings(/*deleted parameters signature*/);

	/*[local]
		HRESULT ReleaseAccessor(
			[in] HACCESSOR hAccessor,
			[in, out, unique] DBREFCOUNT * pcRefCount
		);*/
	[PreserveSig]
	OleDbHResult ReleaseAccessor(
		[In] IntPtr hAccessor,
		[Out] out int pcRefCount);
}

[Guid("0C733A93-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IChapteredRowset
{

	[Obsolete("not used", true)] void AddRefChapter(/*deleted parameters signature*/);

	/*[local]
		HRESULT ReleaseChapter(
			[in] HCHAPTER hChapter,
			[out] DBREFCOUNT * pcRefCount
		);*/
	[PreserveSig, ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
	OleDbHResult ReleaseChapter(
		[In] IntPtr hChapter,
		[Out] out int pcRefCount);
}

[Guid("0C733A11-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IColumnsInfo
{

	/*[local]
		HRESULT GetColumnInfo(
			[in, out] DBORDINAL * pcColumns,
			[out, size_is(,(ULONG)*pcColumns)] DBCOLUMNINFO ** prgInfo,
			[out] OLECHAR ** ppStringsBuffer
		);*/
	[PreserveSig]
	OleDbHResult GetColumnInfo(
		[Out] out IntPtr pcColumns,
		[Out] out IntPtr prgInfo,
		[Out] out IntPtr ppStringsBuffer);

	//[PreserveSig]
	//int MapColumnIDs(/* deleted parameters*/);
}

[Guid("0C733A10-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IColumnsRowset
{

	/*[local]
		HRESULT GetAvailableColumns(
			[in, out] DBORDINAL * pcOptColumns,
			[out, size_is(,(ULONG)*pcOptColumns)] DBID ** prgOptColumns
		);*/
	[PreserveSig]
	OleDbHResult GetAvailableColumns(
		[Out] out IntPtr pcOptColumns,
		[Out] out IntPtr prgOptColumns);

	/*[local]
		HRESULT GetColumnsRowset(
			[in] IUnknown * pUnkOuter,
			[in] DBORDINAL cOptColumns,
			[in, size_is((ULONG)cOptColumns)] const DBID rgOptColumns[],
			[in] REFIID riid,
			[in] ULONG cPropertySets,
			[in, out, size_is((ULONG)cPropertySets)] DBPROPSET rgPropertySets[],
			[out, iid_is(riid)] IUnknown ** ppColRowset
		);*/
	[PreserveSig]
	OleDbHResult GetColumnsRowset(
		[In] IntPtr pUnkOuter,
		[In] IntPtr cOptColumns,
		[In] SafeHandle rgOptColumns,
		[In] ref Guid riid,
		[In] int cPropertySets,
		[In] IntPtr rgPropertySets,
		[Out, MarshalAs(UnmanagedType.Interface)] out IRowset ppColRowset);
}


[Guid("0C733A26-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ICommandPrepare
{

	/*[local]
		HRESULT Prepare(
			[in] ULONG cExpectedRuns
		);*/
	[PreserveSig]
	OleDbHResult Prepare(
		[In] int cExpectedRuns);

	//[PreserveSig]
	//int Unprepare();
}

internal enum DBBindStatus
{
	OK = 0,
	BADORDINAL = 1,
	UNSUPPORTEDCONVERSION = 2,
	BADBINDINFO = 3,
	BADSTORAGEFLAGS = 4,
	NOINTERFACE = 5,
	MULTIPLESTORAGE = 6
}

#if false
	typedef struct tagDBPARAMBINDINFO {
		LPOLESTR pwszDataSourceType;
		LPOLESTR pwszName;
		DBLENGTH ulParamSize;
		DBPARAMFLAGS dwFlags;
		BYTE bPrecision;
		BYTE bScale;
	}
#endif

#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
internal struct tagDBPARAMBINDINFO
{
	internal IntPtr pwszDataSourceType;
	internal IntPtr pwszName;
	internal IntPtr ulParamSize;
	internal int dwFlags;
	internal byte bPrecision;
	internal byte bScale;

#if DEBUG
	public override string ToString()
	{
		StringBuilder builder = new();
		builder.Append("tagDBPARAMBINDINFO").Append(Environment.NewLine);
		if (IntPtr.Zero != pwszDataSourceType)
		{
			builder.Append("pwszDataSourceType =").Append(Marshal.PtrToStringUni(pwszDataSourceType)).Append(Environment.NewLine);
		}
		builder.Append("\tulParamSize  =" + ulParamSize.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tdwFlags     =0x" + dwFlags.ToString("X4", CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tPrecision   =" + bPrecision.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tScale       =" + bScale.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		return builder.ToString();
	}
#endif
}

#if false
	typedef struct tagDBBINDING {
		DBORDINAL iOrdinal;
		DBBYTEOFFSET obValue;
		DBBYTEOFFSET obLength;
		DBBYTEOFFSET obStatus;
		ITypeInfo *pTypeInfo;
		DBOBJECT *pObject;
		DBBINDEXT *pBindExt;
		DBPART dwPart;
		DBMEMOWNER dwMemOwner;
		DBPARAMIO eParamIO;
		DBLENGTH cbMaxLen;
		DWORD dwFlags;
		DBTYPE wType;
		BYTE bPrecision;
		BYTE bScale;
	}
#endif

#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
internal sealed class tagDBBINDING
{

	internal IntPtr iOrdinal;
	internal IntPtr obValue;
	internal IntPtr obLength;
	internal IntPtr obStatus;

	internal IntPtr pTypeInfo;
	internal IntPtr pObject;
	internal IntPtr pBindExt;

	internal int dwPart;
	internal int dwMemOwner;
	internal int eParamIO;

	internal IntPtr cbMaxLen;

	internal int dwFlags;
	internal short wType;
	internal byte bPrecision;
	internal byte bScale;

	internal tagDBBINDING()
	{
	}

#if DEBUG
	public override string ToString()
	{
		StringBuilder builder = new();
		builder.Append("tagDBBINDING").Append(Environment.NewLine);
		builder.Append("\tOrdinal     =" + iOrdinal.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tValueOffset =" + obValue.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tLengthOffset=" + obLength.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tStatusOffset=" + obStatus.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tMaxLength   =" + cbMaxLen.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tDB_Type     =" + ODB.WLookup(wType)).Append(Environment.NewLine);
		builder.Append("\tPrecision   =" + bPrecision.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\tScale       =" + bScale.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		return builder.ToString();
	}
#endif
}

#if false
	typedef struct tagDBCOLUMNACCESS {
		void *pData;
		DBID columnid;
		DBLENGTH cbDataLen;
		DBSTATUS dwStatus;
		DBLENGTH cbMaxLen;
		DB_DWRESERVE dwReserved;
		DBTYPE wType;
		BYTE bPrecision;
		BYTE bScale;
	} 
#endif

#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
internal struct tagDBCOLUMNACCESS
{

	internal IntPtr pData;
	internal tagDBIDX columnid;
	internal IntPtr cbDataLen;
	internal int dwStatus;
	internal IntPtr cbMaxLen;
	internal IntPtr dwReserved;
	internal short wType;
	internal byte bPrecision;
	internal byte bScale;
}

#if false
	typedef struct tagDBID {
	/* [switch_is][switch_type] */ union {
		/* [case()] */ GUID guid;
		/* [case()] */ GUID *pguid;
		/* [default] */  /* Empty union arm */ 
		}   uGuid;
	DBKIND eKind;
	/* [switch_is][switch_type] */ union  {
		/* [case()] */ LPOLESTR pwszName;
		/* [case()] */ ULONG ulPropid;
		/* [default] */  /* Empty union arm */ 
		}   uName;
	}
#endif

#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
internal struct tagDBIDX
{
	internal Guid uGuid;
	internal int eKind;
	internal IntPtr ulPropid;
}

#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
internal sealed class tagDBID
{
	internal Guid uGuid;
	internal int eKind;
	internal IntPtr ulPropid;
}

#if false
	typedef struct tagDBLITERALINFO {
		LPOLESTR pwszLiteralValue;
		LPOLESTR pwszInvalidChars;
		LPOLESTR pwszInvalidStartingChars;
		DBLITERAL lt;
		BOOL fSupported;
		ULONG cchMaxLen;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBLITERALINFO
{

	[MarshalAs(UnmanagedType.LPWStr)]
	internal string? pwszLiteralValue = null;

	[MarshalAs(UnmanagedType.LPWStr)]
	internal string? pwszInvalidChars = null;

	[MarshalAs(UnmanagedType.LPWStr)]
	internal string? pwszInvalidStartingChars = null;

	internal int it;

	internal int fSupported;

	internal int cchMaxLen;

	internal tagDBLITERALINFO()
	{
	}
}

#if false
	typedef struct tagDBPROPSET {
		/* [size_is] */ DBPROP *rgProperties;
		ULONG cProperties;
		GUID guidPropertySet;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBPROPSET
{
	internal IntPtr rgProperties;
	internal int cProperties;
	internal Guid guidPropertySet;

	internal tagDBPROPSET()
	{
	}

	internal tagDBPROPSET(int propertyCount, Guid propertySet)
	{
		cProperties = propertyCount;
		guidPropertySet = propertySet;
	}
}

#if false
	typedef struct tagDBPROP {
		DBPROPID dwPropertyID;
		DBPROPOPTIONS dwOptions;
		DBPROPSTATUS dwStatus;
		DBID colid;
		VARIANT vValue;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBPROP
{
	internal int dwPropertyID;
	internal int dwOptions;
	internal OleDbPropertyStatus dwStatus;

	internal tagDBIDX columnid;

	// Variant
	[MarshalAs(UnmanagedType.Struct)] internal object vValue;

	internal tagDBPROP()
	{
	}

	internal tagDBPROP(int propertyID, bool required, object value)
	{
		dwPropertyID = propertyID;
		dwOptions = ((required) ? ODB.DBPROPOPTIONS_REQUIRED : ODB.DBPROPOPTIONS_OPTIONAL);
		vValue = value;
	}
}

#if false
	typedef struct tagDBPARAMS {
		void *pData;
		DB_UPARAMS cParamSets;
		HACCESSOR hAccessor;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBPARAMS
{
	internal IntPtr pData;
	internal int cParamSets;
	internal IntPtr hAccessor;

	internal tagDBPARAMS()
	{
	}
}

#if false
	typedef struct tagDBCOLUMNINFO {
		LPOLESTR pwszName;
		ITypeInfo *pTypeInfo;
		DBORDINAL iOrdinal;
		DBCOLUMNFLAGS dwFlags;
		DBLENGTH ulColumnSize;
		DBTYPE wType;
		BYTE bPrecision;
		BYTE bScale;
		DBID columnid;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBCOLUMNINFO
{

	[MarshalAs(UnmanagedType.LPWStr)]
	internal string? pwszName = null;

	//[MarshalAs(UnmanagedType.Interface)]
	internal IntPtr pTypeInfo = (IntPtr)0;

	internal IntPtr iOrdinal = (IntPtr)0;

	internal int dwFlags = 0;

	internal IntPtr ulColumnSize = (IntPtr)0;

	internal short wType = 0;

	internal byte bPrecision = 0;

	internal byte bScale = 0;

	internal tagDBIDX columnid;

	internal tagDBCOLUMNINFO()
	{
	}
#if DEBUG
	public override string ToString()
	{
		StringBuilder builder = new();
		builder.Append("tagDBCOLUMNINFO: " + Convert.ToString(pwszName, CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + iOrdinal.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + "0x" + dwFlags.ToString("X8", CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + ulColumnSize.ToInt64().ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + "0x" + wType.ToString("X2", CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + bPrecision.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + bScale.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		builder.Append("\t" + columnid.eKind.ToString(CultureInfo.InvariantCulture)).Append(Environment.NewLine);
		return builder.ToString();
	}
#endif
}

#if false
	typedef struct tagDBPROPINFOSET {
		/* [size_is] */ PDBPROPINFO rgPropertyInfos;
		ULONG cPropertyInfos;
		GUID guidPropertySet;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBPROPINFOSET
{

	internal IntPtr rgPropertyInfos;
	internal int cPropertyInfos;
	internal Guid guidPropertySet;

	internal tagDBPROPINFOSET()
	{
	}
}

#if false
	typedef struct tagDBPROPINFO {
		LPOLESTR pwszDescription;
		DBPROPID dwPropertyID;
		DBPROPFLAGS dwFlags;
		VARTYPE vtType;
		VARIANT vValues;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
sealed internal class tagDBPROPINFO
{

	[MarshalAs(UnmanagedType.LPWStr)] internal string pwszDescription;

	internal int dwPropertyID;
	internal int dwFlags;

	internal short vtType;

	[MarshalAs(UnmanagedType.Struct)] internal object vValue;

	internal tagDBPROPINFO()
	{
	}
}

#if false
	typedef struct tagDBPROPIDSET {
		/* [size_is] */ DBPROPID *rgPropertyIDs;
		ULONG cPropertyIDs;
		GUID guidPropertySet;
	}
#endif
#if (WIN32 && !ARCH_arm)
	[StructLayoutAttribute(LayoutKind.Sequential, Pack = 2)]
#else
[StructLayoutAttribute(LayoutKind.Sequential, Pack = 8)]
#endif
internal struct tagDBPROPIDSET
{
	internal IntPtr rgPropertyIDs;
	internal int cPropertyIDs;
	internal Guid guidPropertySet;
}

[Guid("0C733A79-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ICommandProperties
{

	/*[local]
		HRESULT GetProperties(
			[in] const ULONG cPropertyIDSets,
			[in, size_is(cPropertyIDSets)] const DBPROPIDSET rgPropertyIDSets[],
			[in, out] ULONG * pcPropertySets,
			[out, size_is(,*pcPropertySets)] DBPROPSET ** prgPropertySets
		);*/
	[PreserveSig]
	OleDbHResult GetProperties(
		[In] int cPropertyIDSets,
		[In] SafeHandle rgPropertyIDSets,
		[Out] out int pcPropertySets,
		[Out] out IntPtr prgPropertySets);

	/*[local]
		HRESULT SetProperties(
			[in] ULONG cPropertySets,
			[in, out, unique, size_is(cPropertySets)] DBPROPSET rgPropertySets[]
		);*/
	[PreserveSig]
	OleDbHResult SetProperties(
		[In] int cPropertySets,
		[In] SafeHandle rgPropertySets);
}

[Guid("0C733A27-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ICommandText
{

	/*[local]
		HRESULT Cancel(
		);*/
	[PreserveSig] OleDbHResult Cancel();

	/*[local]
		HRESULT Execute(
			[in] IUnknown * pUnkOuter,
			[in] REFIID riid,
			[in, out] DBPARAMS * pParams,
			[out] DBROWCOUNT * pcRowsAffected,
			[out, iid_is(riid)] IUnknown ** ppRowset
		);*/
	[PreserveSig]
	OleDbHResult Execute(
		[In] IntPtr pUnkOuter,
		[In] ref Guid riid,
		[In] tagDBPARAMS pDBParams,
		[Out] out IntPtr pcRowsAffected,
		[Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);

	[Obsolete("not used", true)] void GetDBSession(/*deleted parameter signature*/);

	[Obsolete("not used", true)] void GetCommandText(/*deleted parameter signature*/);

	/*[local]
		HRESULT SetCommandText(
			[in] REFGUID rguidDialect,
			[in, unique] LPCOLESTR pwszCommand
		);*/
	[PreserveSig]
	OleDbHResult SetCommandText(
		[In] ref Guid rguidDialect,
		[In, MarshalAs(UnmanagedType.LPWStr)] string pwszCommand);
}

[Guid("0C733A64-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ICommandWithParameters
{

	[Obsolete("not used", true)] void GetParameterInfo(/*deleted parameters signature*/);

	[Obsolete("not used", true)] void MapParameterNames(/*deleted parameter signature*/);

	/*[local]
		HRESULT SetParameterInfo(
			[in] DB_UPARAMS cParams,
			[in, unique, size_is((ULONG)cParams)] const DB_UPARAMS rgParamOrdinals[],
			[in, unique, size_is((ULONG)cParams)] const DBPARAMBINDINFO rgParamBindInfo[]
		);*/
	[PreserveSig]
	OleDbHResult SetParameterInfo(
		[In] IntPtr cParams,
		[In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] rgParamOrdinals,
		[In, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct)] tagDBPARAMBINDINFO[] rgParamBindInfo);
}

[Guid("2206CCB1-19C1-11D1-89E0-00C04FD7A829"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IDataInitialize
{

}

[Guid("0C733A89-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IDBInfo
{

	/*[local]
		HRESULT	GetKeywords(
			[out] LPOLESTR * ppwszKeywords
		);*/
	[PreserveSig]
	OleDbHResult GetKeywords(
		[Out, MarshalAs(UnmanagedType.LPWStr)] out string ppwszKeywords);

	/*[local]
		HRESULT GetLiteralInfo(
			[in] ULONG cLiterals,
			[in, size_is(cLiterals)] const DBLITERAL rgLiterals[],
			[in, out] ULONG * pcLiteralInfo,
			[out, size_is(,*pcLiteralInfo)] DBLITERALINFO ** prgLiteralInfo,
			[out] OLECHAR ** ppCharBuffer
		);*/
	[PreserveSig]
	OleDbHResult GetLiteralInfo(
		[In] int cLiterals,
		[In, MarshalAs(UnmanagedType.LPArray)] int[] rgLiterals,
		[Out] out int pcLiteralInfo,
		[Out] out IntPtr prgLiteralInfo,
		[Out] out IntPtr ppCharBuffer);
}

[Guid("0C733A8A-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IDBProperties
{

	/*[local]
		HRESULT GetProperties(
			[in] const ULONG cPropertyIDSets,
			[in, size_is(cPropertyIDSets)] const DBPROPIDSET rgPropertyIDSets[],
			[in, out] ULONG * pcPropertySets,
			[out, size_is(,*pcPropertySets)] DBPROPSET ** prgPropertySets
		);*/
	[PreserveSig]
	OleDbHResult GetProperties(
		[In] int cPropertyIDSets,
		[In] SafeHandle rgPropertyIDSets,
		[Out] out int pcPropertySets,
		[Out] out IntPtr prgPropertySets);

	[PreserveSig]
	OleDbHResult GetPropertyInfo(
		[In] int cPropertyIDSets,
		[In] SafeHandle rgPropertyIDSets,
		[Out] out int pcPropertySets,
		[Out] out IntPtr prgPropertyInfoSets,
		[Out] out IntPtr ppDescBuffer);

	[PreserveSig]
	OleDbHResult SetProperties(
		[In] int cPropertySets,
		[In] SafeHandle rgPropertySets);
}

[Guid("0C733A7B-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IDBSchemaRowset
{

	/*[local]
		HRESULT GetRowset(
			[in] IUnknown * pUnkOuter,
			[in] REFGUID rguidSchema,
			[in] ULONG cRestrictions,
			[in, size_is(cRestrictions)] const VARIANT rgRestrictions[],
			[in] REFIID riid,
			[in] ULONG cPropertySets,
			[in, out, unique, size_is(cPropertySets)] DBPROPSET rgPropertySets[],
			[out, iid_is(riid)] IUnknown ** ppRowset
		);*/
	[PreserveSig]
	OleDbHResult GetRowset(
		[In] IntPtr pUnkOuter,
		[In] ref Guid rguidSchema,
		[In] int cRestrictions,
		[In, MarshalAs(UnmanagedType.LPArray)] object[] rgRestrictions,
		[In] ref Guid riid,
		[In] int cPropertySets,
		[In] IntPtr rgPropertySets,
		[Out, MarshalAs(UnmanagedType.Interface)] out IRowset ppRowset);

	/*[local]
		HRESULT GetSchemas(
			[in, out] ULONG * pcSchemas,
			[out, size_is(,*pcSchemas)] GUID ** prgSchemas,
			[out, size_is(,*pcSchemas)] ULONG ** prgRestrictionSupport
		);*/
	[PreserveSig]
	OleDbHResult GetSchemas(
		[Out] out int pcSchemas,
		[Out] out IntPtr rguidSchema,
		[Out] out IntPtr prgRestrictionSupport);
}

[Guid("1CF2B120-547D-101B-8E65-08002B2BD119"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IErrorInfo
{

	[Obsolete("not used", true)] void GetGUID(/*deleted parameter signature*/);

	[PreserveSig]
	OleDbHResult GetSource(
		[Out, MarshalAs(UnmanagedType.BStr)] out string pBstrSource);

	[PreserveSig]
	OleDbHResult GetDescription(
		[Out, MarshalAs(UnmanagedType.BStr)] out string pBstrDescription);

	//[ Obsolete("not used", true)] void GetHelpFile(/*deleted parameter signature*/);

	//[ Obsolete("not used", true)] void GetHelpContext(/*deleted parameter signature*/);
}
#if false
		MIDL_INTERFACE("1CF2B120-547D-101B-8E65-08002B2BD119")
		IErrorInfo : public IUnknown
			virtual HRESULT STDMETHODCALLTYPE GetGUID(
				/* [out] */ GUID *pGUID) = 0;
			virtual HRESULT STDMETHODCALLTYPE GetSource(
				/* [out] */ BSTR *pBstrSource) = 0;
			virtual HRESULT STDMETHODCALLTYPE GetDescription(
				/* [out] */ BSTR *pBstrDescription) = 0;
			virtual HRESULT STDMETHODCALLTYPE GetHelpFile(
				/* [out] */ BSTR *pBstrHelpFile) = 0;
			virtual HRESULT STDMETHODCALLTYPE GetHelpContext(
				/* [out] */ DWORD *pdwHelpContext) = 0;
#endif

[Guid("0C733A67-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IErrorRecords
{

	[Obsolete("not used", true)] void AddErrorRecord(/*deleted parameter signature*/);

	[Obsolete("not used", true)] void GetBasicErrorInfo(/*deleted parameter signature*/);

	[PreserveSig]
	OleDbHResult GetCustomErrorObject( // may return E_NOINTERFACE when asking for IID_ISQLErrorInfo
		[In] int ulRecordNum,
		[In] ref Guid riid,
		[Out, MarshalAs(UnmanagedType.Interface)] out ISQLErrorInfo ppObject);

	[return: MarshalAs(UnmanagedType.Interface)]
	IErrorInfo GetErrorInfo(
		[In] int ulRecordNum,
		[In] int lcid);

	[Obsolete("not used", true)] void GetErrorParameters(/*deleted parameter signature*/);

	int GetRecordCount();
}
#if false
	MIDL_INTERFACE("0c733a67-2a1c-11ce-ade5-00aa0044773d")
	IErrorRecords : public IUnknown
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE AddErrorRecord(
			/* [in] */ ERRORINFO *pErrorInfo,
			/* [in] */ DWORD dwLookupID,
			/* [in] */ DISPPARAMS *pdispparams,
			/* [in] */ IUnknown *punkCustomError,
			/* [in] */ DWORD dwDynamicErrorID) = 0;
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetBasicErrorInfo(
			/* [in] */ ULONG ulRecordNum,
			/* [out] */ ERRORINFO *pErrorInfo) = 0;
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetCustomErrorObject(
			/* [in] */ ULONG ulRecordNum,
			/* [in] */ REFIID riid,
			/* [iid_is][out] */ IUnknown **ppObject) = 0;
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetErrorInfo(
			/* [in] */ ULONG ulRecordNum,
			/* [in] */ LCID lcid,
			/* [out] */ IErrorInfo **ppErrorInfo) = 0;
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetErrorParameters(
			/* [in] */ ULONG ulRecordNum,
			/* [out] */ DISPPARAMS *pdispparams) = 0;
		virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetRecordCount(
			/* [out] */ ULONG *pcRecords) = 0;
#endif

[Guid("0C733A90-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IMultipleResults
{

	/*[local]
		HRESULT GetResult(
			[in] IUnknown * pUnkOuter,
			[in] DBRESULTFLAG lResultFlag,
			[in] REFIID riid,
			[out] DBROWCOUNT * pcRowsAffected,
			[out, iid_is(riid)] IUnknown ** ppRowset
		);*/
	[PreserveSig]
	OleDbHResult GetResult(
		[In] IntPtr pUnkOuter,
		[In] IntPtr lResultFlag,
		[In] ref Guid riid,
		[Out] out IntPtr pcRowsAffected,
		[Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);
}
#if false
		enum DBRESULTFLAGENUM {
			DBRESULTFLAG_DEFAULT = 0,
			DBRESULTFLAG_ROWSET = 1,
			DBRESULTFLAG_ROW = 2
		}
		MIDL_INTERFACE("0c733a90-2a1c-11ce-ade5-00aa0044773d")
		IMultipleResults : public IUnknown
			virtual /* [local] */ HRESULT STDMETHODCALLTYPE GetResult(
				/* [in] */ IUnknown *pUnkOuter,
				/* [in] */ DBRESULTFLAG lResultFlag,
				/* [in] */ REFIID riid,
				/* [out] */ DBROWCOUNT *pcRowsAffected,
				/* [iid_is][out] */ IUnknown **ppRowset) = 0;
#endif

[Guid("0C733A69-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IOpenRowset
{

	[PreserveSig]
	OleDbHResult OpenRowset(
		[In] IntPtr pUnkOuter,
		[In] tagDBID pTableID,
		[In] IntPtr pIndexID,
		[In] ref Guid riid,
		[In] int cPropertySets,
		[In] IntPtr rgPropertySets,
		[Out, MarshalAs(UnmanagedType.Interface)] out object ppRowset);
}

[Guid("0C733AB4-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IRow
{

	[PreserveSig]
	OleDbHResult GetColumns(
		[In] IntPtr cColumns,
		[In, Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct)] tagDBCOLUMNACCESS[] rgColumns);

	//[ Obsolete("not used", true)] void GetSourceRowset(/*deleted parameter signature*/);
	//[ Obsolete("not used", true)] void Open(/*deleted parameter signature*/);
}

[Guid("0C733A7C-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IRowset
{

	[Obsolete("not used", true)] void AddRefRows(/*deleted parameter signature*/);

	/*HRESULT GetData(
			[in] HROW hRow,
			[in] HACCESSOR hAccessor,
			[out] void * pData
		);*/
	[PreserveSig]
	OleDbHResult GetData(
		[In] IntPtr hRow,
		[In] IntPtr hAccessor,
		[In] IntPtr pData);

	/*HRESULT GetNextRows(
			[in] HCHAPTER hReserved,
			[in] DBROWOFFSET lRowsOffset,
			[in] DBROWCOUNT cRows,
			[out] DBCOUNTITEM * pcRowsObtained,
			[out, size_is(,cRows)] HROW ** prghRows
		);*/
	[PreserveSig]
	OleDbHResult GetNextRows(
		[In] IntPtr hChapter,
		[In] IntPtr lRowsOffset,
		[In] IntPtr cRows,
		[Out] out IntPtr pcRowsObtained,
		[In] ref IntPtr pprghRows);

	/*HRESULT ReleaseRows(
			[in] DBCOUNTITEM cRows,
			[in, size_is(cRows)] const HROW rghRows[],
			[in, size_is(cRows)] DBROWOPTIONS rgRowOptions[],
			[out, size_is(cRows)] DBREFCOUNT rgRefCounts[],
			[out, size_is(cRows)] DBROWSTATUS rgRowStatus[]
		);*/
	[PreserveSig]
	OleDbHResult ReleaseRows(
		[In] IntPtr cRows,
		[In] SafeHandle rghRows,
		[In/*, MarshalAs(UnmanagedType.LPArray)*/] IntPtr/*int[]*/ rgRowOptions,
		[In/*, Out, MarshalAs(UnmanagedType.LPArray)*/] IntPtr/*int[]*/ rgRefCounts,
		[In/*, Out, MarshalAs(UnmanagedType.LPArray)*/] IntPtr/*int[]*/ rgRowStatus);

	[Obsolete("not used", true)] void RestartPosition(/*deleted parameter signature*/);
}

[Guid("0C733A55-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface IRowsetInfo
{

	/*[local]
		HRESULT GetProperties(
			[in] const ULONG cPropertyIDSets,
			[in, size_is(cPropertyIDSets)] const DBPROPIDSET rgPropertyIDSets[],
			[in, out] ULONG * pcPropertySets,
			[out, size_is(,*pcPropertySets)] DBPROPSET ** prgPropertySets
		);*/
	[PreserveSig]
	OleDbHResult GetProperties(
		[In] int cPropertyIDSets,
		[In] SafeHandle rgPropertyIDSets,
		[Out] out int pcPropertySets,
		[Out] out IntPtr prgPropertySets);

	[PreserveSig]
	OleDbHResult GetReferencedRowset(
		[In] IntPtr iOrdinal,
		[In] ref Guid riid,
		[Out, MarshalAs(UnmanagedType.Interface)] out IRowset ppRowset);

	//[PreserveSig]
	//int GetSpecification(/*deleted parameter signature*/);
}

[Guid("0C733A74-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ISQLErrorInfo
{

	[return: MarshalAs(UnmanagedType.I4)]
	int GetSQLInfo(
		[Out, MarshalAs(UnmanagedType.BStr)] out string pbstrSQLState);
}

[Guid("0C733A5F-2A1C-11CE-ADE5-00AA0044773D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport, SuppressUnmanagedCodeSecurity]
internal interface ITransactionLocal
{

	[Obsolete("not used", true)] void Commit(/*deleted parameter signature*/);

	[Obsolete("not used", true)] void Abort(/*deleted parameter signature*/);

	[Obsolete("not used", true)] void GetTransactionInfo(/*deleted parameter signature*/);

	[Obsolete("not used", true)] void GetOptionsObject(/*deleted parameter signature*/);

	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
	[PreserveSig]
	OleDbHResult StartTransaction(
		[In] int isoLevel,
		[In] int isoFlags,
		[In] IntPtr pOtherOptions,
		[Out] out int pulTransactionLevel);
}
