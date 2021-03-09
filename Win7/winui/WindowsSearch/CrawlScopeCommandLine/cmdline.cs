using System;
using Vanara.PInvoke;

namespace std
{
	// Base abstract class for command line parsing classes
	public abstract class CParamBase
	{
		public CParamBase()
		{
		}

		public bool Exists { get; protected set; } = false;

		// Command line parsing function
		public static Win32Error ParseParams(CParamBase[] pParams, string[] args)
		{
			Win32Error iRes = Win32Error.ERROR_SUCCESS;
			var i = 0;
			while (Win32Error.ERROR_SUCCESS == iRes && args.Length > i)
			{
				var iIncrement = 0;
				for (uint j = 0; Win32Error.ERROR_SUCCESS == iRes && j < pParams.Length; j++)
				{
					iRes = pParams[j].Init(args[i..], ref iIncrement);
					if (Win32Error.ERROR_SUCCESS == iRes && 0 != iIncrement)
					{
						i += iIncrement;
						break;
					}
				}

				if (Win32Error.ERROR_SUCCESS == iRes && 0 == iIncrement)
				{
					iRes = Win32Error.ERROR_INVALID_PARAMETER;
					Console.WriteLine("Unknown parameter: " + args[i]);
				}
			}

			return iRes;
		}

		public abstract Win32Error Init(string[] args, ref int rParamsProcessed);
	}

	// Parameter with two alternative values, one value is associated with true, another one is false correspondingly.
	public class CExclFlagParam : CParamBase
	{
		protected bool m_fFlag = false;
		protected string m_szFalseParamName;
		protected string m_szTrueParamName;

		public CExclFlagParam(string szTrueParamName, string szFalseParamName)
		{
			m_szTrueParamName = szTrueParamName;
			m_szFalseParamName = szFalseParamName;
		}

		public bool Get() => m_fFlag;

		public override Win32Error Init(string[] args, ref int rParamsProcessed)
		{
			var iRes = Win32Error.ERROR_SUCCESS;
			rParamsProcessed = 0;
			if (args.Length > 0 && (args[0].StartsWith('/') || args[0].StartsWith('-')))
			{
				if (args[0][1..].Equals(m_szTrueParamName, StringComparison.OrdinalIgnoreCase))
				{
					if (!Exists)
					{
						Exists = true;
						m_fFlag = true;
						rParamsProcessed = 1;
					}
					else
					{
						iRes = Win32Error.ERROR_INVALID_PARAMETER;
					}
				}
				else if (args[0][1..].Equals(m_szFalseParamName, StringComparison.OrdinalIgnoreCase))
				{
					if (!Exists)
					{
						Exists = true;
						m_fFlag = false;
						rParamsProcessed = 1;
					}
					else
					{
						iRes = Win32Error.ERROR_INVALID_PARAMETER;
					}
				}
			}

			if (Win32Error.ERROR_INVALID_PARAMETER == iRes)
			{
				Console.WriteLine($"/{m_szTrueParamName} and /{m_szFalseParamName} parameters can't be used together!");
			}

			return iRes;
		}
	}

	// Simple flag-type parameter, no value, just checking if it's present in command line or not.
	public class CFlagParam : CParamBase
	{
		protected string m_szParamName;

		public CFlagParam(string szParamName) => m_szParamName = szParamName;

		public override Win32Error Init(string[] args, ref int rParamsProcessed)
		{
			rParamsProcessed = 0;
			if (args.Length > 0 && (args[0].StartsWith('/') || args[0].StartsWith('-')) && args[0][1..].Equals(m_szParamName, StringComparison.OrdinalIgnoreCase))
			{
				Exists = true;
				rParamsProcessed = 1;
			}

			return Win32Error.ERROR_SUCCESS;
		}
	}

	// Parameter followed by some value
	public class CSetValueParam : CParamBase
	{
		protected string m_szParamName;
		protected string m_szValue;

		public CSetValueParam(string szName)
		{
			m_szParamName = szName;
			m_szValue = null;
		}

		public string Get() => m_szValue;

		public override Win32Error Init(string[] args, ref int rParamsProcessed)
		{
			var iRes = Win32Error.ERROR_SUCCESS;
			rParamsProcessed = 0;
			if ((args[0].StartsWith('/') || args[0].StartsWith('-')) && args[0][1..].Equals(m_szParamName, StringComparison.OrdinalIgnoreCase))
			{
				if (!Exists)
				{
					Exists = true;
					if (1 < args.Length && // if it's not last word in command line
						!args[1].StartsWith('/') && // and not followed by other parameter
						!args[1].StartsWith('-')) // Parameter values starting with '/' and '-' are not supported
					{
						m_szValue = args[1];
						rParamsProcessed = 2;
					}
					else
					{
						iRes = Win32Error.ERROR_INVALID_PARAMETER;
						Console.WriteLine($"No valid value following parameter /{m_szParamName}!");
					}
				}
				else
				{
					iRes = Win32Error.ERROR_INVALID_PARAMETER;
					Console.WriteLine($"More than one instane of /{m_szParamName} were found in command line!");
				}
			}

			return iRes;
		}
	}
}