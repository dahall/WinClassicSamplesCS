using System.Diagnostics.CodeAnalysis;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;

// All of the LCTYPES new to Windows Vista
LCTYPE[] NewTypes =
{
	LCTYPE.LOCALE_SNAME,
	LCTYPE.LOCALE_SDURATION,
	LCTYPE.LOCALE_SKEYBOARDSTOINSTALL,
	LCTYPE.LOCALE_SSHORTESTDAYNAME1,
	LCTYPE.LOCALE_SSHORTESTDAYNAME2,
	LCTYPE.LOCALE_SSHORTESTDAYNAME3,
	LCTYPE.LOCALE_SSHORTESTDAYNAME4,
	LCTYPE.LOCALE_SSHORTESTDAYNAME5,
	LCTYPE.LOCALE_SSHORTESTDAYNAME6,
	LCTYPE.LOCALE_SSHORTESTDAYNAME7,
	LCTYPE.LOCALE_SISO639LANGNAME2,
	LCTYPE.LOCALE_SISO3166CTRYNAME2,
	LCTYPE.LOCALE_SNAN,
	LCTYPE.LOCALE_SPOSINFINITY,
	LCTYPE.LOCALE_SNEGINFINITY,
	LCTYPE.LOCALE_SSCRIPTS
};

const int BUFFER_SIZE = 255;
StringBuilder wcBuffer = new(BUFFER_SIZE);

// Get system default locale
string? sysLocale = GetSystemDefaultLocaleName(wcBuffer, BUFFER_SIZE) > 0 ? wcBuffer.ToString() : null;

// See which of the input locales are valid (if any)
List<string> userLocales = new();
foreach (string lname in args)
{
	if (!IsValidLocaleName(lname))
	{
		Console.Write("{0} is not a valid locale name\n", lname);
	}
	else
	{
		userLocales.Add(lname);
	}
}

// Enumerate all the locales and report on them
List<string> locales = EnumSystemLocalesEx(LOCALE_FLAGS.LOCALE_ALL).Select(p => p.lpLocaleString).ToList();
if (userLocales.Count > 0)
{
	locales = locales.Intersect(userLocales, LComp.Instance).ToList();
}

foreach (string localeName in locales)
{
	// Print out the locale name we found
	int iResult = GetLocaleInfoEx(localeName, LCTYPE.LOCALE_SENGLISHLANGUAGENAME, wcBuffer, BUFFER_SIZE);

	// If it succeeds, print it out
	if (iResult > 0)
	{
		Console.Write("Locale {0} ({1})\n", localeName, wcBuffer);
	}
	else
	{
		Console.Write("Locale {0} had error {1}\n", localeName, GetLastError());
	}

	// If this is the system locale, let us know. CompareStringEx is probably overkill, but we want to demonstrate that named API.
	if (LComp.Instance.Equals(sysLocale, localeName))
	{
		Console.Write("Locale {0} is the system locale!\n", wcBuffer);
	}

	// Get its LCID
	LCID lcid = LocaleNameToLCID(localeName, default);
	if (lcid != 0)
	{
		Console.Write("LCID for {0} is {1}\n", localeName, lcid);
	}
	else
	{
		Console.Write("Error {0} getting LCID\n", GetLastError());
	}

	// Get today's date
	iResult = GetDateFormatEx(localeName, DATE_FORMAT.DATE_LONGDATE, IntPtr.Zero, default, wcBuffer, BUFFER_SIZE, default);

	if (iResult > 0)
	{
		Console.Write("Date: {0}\n", wcBuffer);
	}
	else
	{
		Console.Write("Error {0} getting today's date for {1}\n", GetLastError(), localeName);
	}

	// Loop through all of the new LCTYPES and do GetLocaleInfoEx on them
	foreach (LCTYPE lcType in NewTypes)
	{
		// Get this uint result for this locale
		iResult = GetLocaleInfoEx(localeName, lcType, wcBuffer, BUFFER_SIZE);

		// If it succeeds, print it out
		if (iResult > 0)
		{
			Console.Write(" {0} has value {1}\n", lcType, wcBuffer);
		}
		else
		{
			Console.Write(" {0} had error {1}\n", lcType, GetLastError());
		}
	}
}

internal class LComp : IEqualityComparer<string>, IComparer<string>
{
	public static readonly LComp Instance = new();
	public int Compare(string? x, string? y) => CompareStringEx(LOCALE_NAME_INVARIANT, COMPARE_STRING.LINGUISTIC_IGNORECASE, x, -1, y, -1) - (int)CSTR_EQUAL;
	public bool Equals(string? x, string? y) => Compare(x, y) == 0;
	public int GetHashCode([DisallowNull] string obj) => obj.GetHashCode();
}