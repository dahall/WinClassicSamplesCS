using System;
using System.Linq;
using System.Text;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ole32;
using static Vanara.PInvoke.PropSys;
using static Vanara.PInvoke.SearchApi;
using static Vanara.PInvoke.Shell32;
using CONDITION_OPERATION = Vanara.PInvoke.SearchApi.CONDITION_OPERATION;

namespace StructuredQuerySample
{
	class Program
	{
		static void Main()
		{
			Console.Write("StructuredQuerySample\n");
			Console.Write("Please enter a query in Advanced Query Syntax (AQS), or an empty line to exit\n");
			Console.Write("the program. The program will parse the query, resolve the resulting condition\n");
			Console.Write("tree and display it in tree form. Some sample inputs to try:\n");
			Console.Write("from:bill               modified:last week      subject:(cats OR dogs)\n");
			Console.Write("name:(bubble NOT bath)  author:~~george         taken:<October 2007\n");
			Console.Write("kind:=folder            has:attachment          rating:****\n");
			Console.Write("flower                  readstatus:read         size:42KB\n");
			Console.Write("System.IsShared:=false  exposuretime:>=0.125    received:5/25/2006 .. 7/17/2007\n");
			Console.Write("Note that these queries are for English as UI language. If your UI language is\n");
			Console.Write("different, you should use keywords from that language. Note though that\n");
			Console.Write("System.IsShared:=false is an example of a query in \"canonical syntax\", which\n");
			Console.Write("will work for any UI language and should be used in programmatic use of AQS.\n");
			Console.Write("MSDN on AQS: http://msdn.microsoft.com/en-us/library/aa965711(VS.85).aspx)\n");
			Console.Write("Note also that any times in the condition trees are in UTC, not local time.\n");

			CoInitializeEx(default, COINIT.COINIT_MULTITHREADED).ThrowIfFailed();
			// It is possible to CoCreateInstance a query parser directly using __uuidof(QueryParser) but by
			// using a QueryParserManager we can get a query parser prepared for a certain UI language and catalog.
			var pqpm = new IQueryParserManager();
			// The language given to IQueryParserManager::CreateLoadedParser should be the user interface language of the
			// application, which is often the same as the operating system's default UI language for this user. It is used for
			// deciding what language to be used when interpreting keywords.
			var pqp = pqpm.CreateLoadedParser<IQueryParser>("SystemIndex", GetUserDefaultUILanguage());
			// This sample turns off "natural query syntax", which is the more relaxed and human language like
			// search notation, and also makes searches automatically look for words beginning with what has been
			// specified (so an input of "from:bill" will search for anything from someone whose name begins with
			// "bill".
			pqpm.InitializeOptions(false, true, pqp);
			var szLine = new StringBuilder(1024);
			char ch;
			while ((ch = Console.ReadKey().KeyChar) != '\r')
			{
				Console.Write("\b");
				szLine.Append(ch);

				// The actual work of parsing a query string is done here.
				var pqs = pqp.Parse(szLine.ToString());
				// In this sample we do not bother distinguishing between various parse errors though we could.
				// Note that there will be a solution even if there were parse errors; it just may not be what
				// the user intended.
				var peu = pqs.GetErrors();
				if (peu.Any())
				{
					Console.Write("Some part of the query string could not be parsed.\n");
				}
				pqs.GetQuery(out var pc, out _);
				// IQueryCondition::Resolve and IConditionFactory2::ResolveCondition turn any date/time references
				// (relative, such as "today", and absolute, such as "5/7/2009") into absolute date/time references
				// (in the UTC time zone), and also simplifies the result in various ways.
				// Note that pc is unchanged and could be resolved against additional dates/times.
				// Code that targets only Windows 7 and later can take advantage of IConditionFactory2::ResolveCondition.
				// Code that targets also earlier versions of Windows and Windows Search should use IQueryCondition::Resolve.
				var pcf = (IConditionFactory2)pqs;
				var pcResolved = pcf.ResolveCondition<ICondition2>(pc);
				DisplayQuery(pcResolved, 0);
			}

			CoUninitialize();
		}

		private static void DisplayQuery(ICondition2 pc, int cIndentation)
		{
			var ct = pc.GetConditionType();
			switch (ct)
			{
				case CONDITION_TYPE.CT_AND_CONDITION:
				case CONDITION_TYPE.CT_OR_CONDITION:
					{
						Console.Write("{0}{1}\n", new string(' ', 2 * cIndentation), ct == CONDITION_TYPE.CT_AND_CONDITION ? "AND" : "OR");
						var poaSubs = pc.GetSubConditions<IObjectArray>();
						var cSubs = poaSubs.GetCount();
						for (uint i = 0; i < cSubs; ++i)
						{
							var pcSub = poaSubs.GetAt<ICondition2>(i);
							DisplayQuery(pcSub, cIndentation + 1);
						}
					}
					break;
				case CONDITION_TYPE.CT_NOT_CONDITION:
					{
						Console.Write("{0}{1}\n", new string(' ', 2 * cIndentation), "NOT");
						// ICondition::GetSubConditions can return the single subcondition of a negation node directly.
						var pcSub = pc.GetSubConditions<ICondition2>();
						DisplayQuery(pcSub, cIndentation + 1);
					}
					break;
				case CONDITION_TYPE.CT_LEAF_CONDITION:
					{
						var propvar = new PROPVARIANT();
						pc.GetLeafConditionInfo(out var propkey, out var op, propvar);
						var hr = PSGetPropertyDescription(propkey, typeof(IPropertyDescription).GUID, out var ppv);
						if (hr.Succeeded)
						{
							var ppd = (IPropertyDescription)ppv;
							string pszPropertyName = ppd.GetCanonicalName();
							var propvarString = new PROPVARIANT();
							hr = PropVariantChangeType(propvarString, propvar, PROPVAR_CHANGE_FLAGS.PVCHF_ALPHABOOL, VARTYPE.VT_LPWSTR); // Real applications should prefer PSFormatForDisplay but we want more "raw" values.
							if (hr.Succeeded)
							{
								var pszSemanticType = pc.GetValueType();
								// The semantic type may be NULL; if so, do not display it at all.
								if (pszSemanticType is null)
								{
									pszSemanticType = string.Empty;
								}

								Console.Write("{0}LEAF {1} {2} {3} {4}\n", new string(' ', 2 * cIndentation), pszPropertyName, GetOperationLabel(op), propvarString.pwszVal, pszSemanticType);
							}
						}
					}
					break;
			}
		}

		private static string GetOperationLabel(CONDITION_OPERATION op) => op.IsValid() ? op.ToString() : "???";
	}
}
