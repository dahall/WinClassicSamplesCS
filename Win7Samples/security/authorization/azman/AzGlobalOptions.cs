namespace AzMigrate;

static class CAzGlobalOptions
{
	static CAzGlobalOptions() => CAzLogging.Initialize(LogLevel.LOG_ERROR);

	public static string m_bstrSourceStoreName = "";
	public static string m_bstrDestStoreName = "";
	public static bool m_bOverWrite;
	public static bool m_bSpecificApp;
	public static bool m_bIgnoreMembers;
	public static bool m_bVerbose;
	public static bool m_bIgnorePolicyAdmins;
	public static bool m_bVersionTwo;
	public static List<string> m_bstrAppNames = [];

	public const string LOGFILETAG = "/l";
	public const string APPNAMETAG = "/a";
	public const string OVERWRITETAG = "/o";
	public const string IGNOREMEMBERSTAG = "/im";
	public const string IGNOREPOLICYADMINSTAG = "/ip";
	public const string VERBOSETAG = "/v";
	public const string HELPTAG = "/?";
	public const int LOGFILETAG_LEN = 2;
	public const int APPNAMETAG_LEN = 2;
	public const int OVERWRITETAG_LEN = 2;
	public const int IGNOREMEMBERSTAG_LEN = 3;
	public const int IGNOREPOLICYADMINSTAG_LEN = 3;
	public const int VERBOSETAG_LEN = 2;
	public const int HELPTAG_LEN = 2;
}