using System.IO;
using Vanara.PInvoke;

namespace AzMigrate;

internal enum LogLevel
{
	LOG_LOGFILE = 0x0,
	LOG_ERROR = 0x1,
	LOG_TRACE = 0x3,
	LOG_DEBUG = 0x7,
}

internal static class CAzLogging
{
	public static bool MIGRATE_SUCCESS;
	private static LogLevel currentLogLevel = 0;
	private static StreamWriter? logfile;

	public static string TimeBuf => DateTime.Now.ToString("MM/dd HH:mm:ss");

	public static void Close()
	{
		if (currentLogLevel == LogLevel.LOG_LOGFILE)
		{
			logfile?.Close();
		}
	}

	public static void Entering(string strMsg)
	{
		if (currentLogLevel == LogLevel.LOG_DEBUG)
		{
			Console.WriteLine($"{TimeBuf}: Entering {strMsg}");
		}
	}

	public static void Exiting(string strMsg)
	{
		if (currentLogLevel == LogLevel.LOG_DEBUG)
			Console.WriteLine($"{TimeBuf}: Exiting {strMsg}");
	}

	public static string GetMsgBuf(HRESULT hr) => hr.ToString();

	public static void Initialize(LogLevel pcurrentLogLevel)
	{
		currentLogLevel = pcurrentLogLevel;

		MIGRATE_SUCCESS = true;
	}

	public static void Initialize(LogLevel pcurrentLogLevel, string plogfile)
	{
		MIGRATE_SUCCESS = true;

		if (pcurrentLogLevel != LogLevel.LOG_LOGFILE)
		{
			Initialize(pcurrentLogLevel);

			return;
		}

		currentLogLevel = pcurrentLogLevel;

		// Converting back to Ansi as Open does not accept wide characters
		logfile = new StreamWriter(plogfile);
	}

	public static void Log(LogLevel LogLevel, string strMsg)
	{
		if (currentLogLevel <= LogLevel)
			Console.WriteLine($"{TimeBuf}: {strMsg}");

		if (currentLogLevel == LogLevel.LOG_LOGFILE)
			logfile?.WriteLine($"{TimeBuf}: {strMsg}");
	}

	public static void Log(HRESULT hr, string strMsg, string strEntityName, AzRoles.AZ_PROP_CONSTANTS pPropID)
	{
		if (hr.Succeeded)
		{
			if (currentLogLevel == LogLevel.LOG_TRACE)
				Console.WriteLine($"{TimeBuf}: {strMsg}{pPropID} for entity:{strEntityName} SUCCESS.");

			if (currentLogLevel == LogLevel.LOG_LOGFILE)
				logfile?.WriteLine($"{TimeBuf}: {strMsg}{pPropID} for entity:{strEntityName} SUCCESS.");
		}
		else
		{
			if (hr.Code == Win32Error.ERROR_NOT_SUPPORTED)
			{
				Console.WriteLine($"{TimeBuf}: {strMsg}{pPropID} for entity:{strEntityName} WARNING.NOT SUPPORTED.");

				if (currentLogLevel == LogLevel.LOG_LOGFILE)
				{
					logfile?.WriteLine($"{TimeBuf}: {strMsg}{pPropID} for entity:{strEntityName} WARNING.NOT SUPPORTED.");
				}

				goto lDone;
			}

			MIGRATE_SUCCESS = false;

			Console.WriteLine($"{TimeBuf}: {strMsg}{pPropID} for entity:{strEntityName} FAILED.ERROR MSG: {GetMsgBuf(hr)}");
			if (currentLogLevel == LogLevel.LOG_LOGFILE)
				logfile?.WriteLine($"{TimeBuf}: {strMsg}{pPropID} for entity:{strEntityName} FAILED.ERROR MSG: {GetMsgBuf(hr)}");
lDone:
			return;
		}
	}

	public static void Log(HRESULT hr, string strMsg, string strEntityName)
	{
		if (hr.Succeeded)
		{
			if (currentLogLevel == LogLevel.LOG_TRACE)
				Console.WriteLine($"{TimeBuf}: {strMsg} for entity:{strEntityName} SUCCESS.");

			if (currentLogLevel == LogLevel.LOG_LOGFILE)
				logfile?.WriteLine($"{TimeBuf}: {strMsg} for entity:{strEntityName} SUCCESS.");
		}
		else
		{
			if (hr.Code == Win32Error.ERROR_NOT_SUPPORTED)
			{
				Console.WriteLine($"{TimeBuf}: {strMsg} for entity:{strEntityName} WARNING.NOT SUPPORTED.");

				if (currentLogLevel == LogLevel.LOG_LOGFILE)
					logfile?.WriteLine($"{TimeBuf}: {strMsg} for entity:{strEntityName} WARNING.NOT SUPPORTED.");

				goto lDone;
			}

			MIGRATE_SUCCESS = false;

			Console.WriteLine($"{TimeBuf}: {strMsg} for entity:{strEntityName} FAILED.ERROR MSG: {GetMsgBuf(hr)}");

			if (currentLogLevel == LogLevel.LOG_LOGFILE)
			{
				logfile?.WriteLine($"{TimeBuf}: {strMsg} for entity:{strEntityName} FAILED.ERROR MSG: {GetMsgBuf(hr)}");
			}
		}

lDone:
		return;
	}

	public static void Log(HRESULT hr, string strMsg)
	{
		if (hr.Succeeded)
		{
			if (currentLogLevel == LogLevel.LOG_TRACE)
				Console.WriteLine($"{TimeBuf}: {strMsg} SUCCESS.");

			if (currentLogLevel == LogLevel.LOG_LOGFILE)
			{
				logfile?.WriteLine($"{TimeBuf}: {strMsg} SUCCESS.");
			}
		}
		else
		{
			if (hr.Code == Win32Error.ERROR_NOT_SUPPORTED)
			{
				Console.WriteLine($"{TimeBuf}: {strMsg} WARNING.NOT SUPPORTED.");

				if (currentLogLevel == LogLevel.LOG_LOGFILE)
					logfile?.WriteLine($"{TimeBuf}: {strMsg} WARNING.NOT SUPPORTED.");

				goto lDone;
			}

			MIGRATE_SUCCESS = false;

			Console.WriteLine($"{TimeBuf}: {strMsg} FAILED.ERROR MSG: {GetMsgBuf(hr)}");

			if (currentLogLevel == LogLevel.LOG_LOGFILE)
				logfile?.WriteLine($"{TimeBuf}: {strMsg} FAILED.ERROR MSG: {GetMsgBuf(hr)}");
		}
lDone:
		return;
	}
}