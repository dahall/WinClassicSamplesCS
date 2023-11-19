namespace ServiceConfig4;

internal partial class Program
{
	// Public function.

	/***************************************************************************++
	Routine Description:
	The function that parses parameters specific to URL ACL &
	calls Set, Query or Delete.
	Arguments:
	args.Length - Count of arguments.
	args - Pointer to command line arguments.
	Type - Type of operation to be performed.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	public static Win32Error DoUrlAcl(string[] args, HTTPCFG_TYPE Type)
	{
		string? pAcl = default, pUrl = default;

		for (var i = 0; args.Length >= i+2 && args[i][0] is '-' or '/'; i += 2)
		{
			switch (char.ToUpper(args[i][1]))
			{
				case 'U':
					pUrl = args[i + 1];
					break;

				case 'A':
					pAcl = args[i + 1];
					break;

				default:
					Console.Write("{0} is not a valid command.", args[0]);
					return Win32Error.ERROR_INVALID_PARAMETER;
			}
		}

		switch (Type)
		{
			case HTTPCFG_TYPE.HttpCfgTypeSet:
				return DoUrlAclSet(pUrl, pAcl);

			case HTTPCFG_TYPE.HttpCfgTypeQuery:
				return DoUrlAclQuery(pUrl);

			case HTTPCFG_TYPE.HttpCfgTypeDelete:
				return DoUrlAclDelete(pUrl);

			default:
				Console.Write("{0} is not a valid command.", args[0]);
				return Win32Error.ERROR_INVALID_PARAMETER;
		}
	}

	/***************************************************************************++
	Routine Description:
	Deletes an URL ACL entry.
	Arguments:
	pUrl - The URL
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoUrlAclDelete([In, Optional] string? pUrl)
	{
		HTTP_SERVICE_CONFIG_URLACL_SET SetParam = new();
		SetParam.KeyDesc.pUrlPrefix = pUrl ?? "";

		Win32Error Status = HttpDeleteServiceConfiguration(SetParam);

		Console.Write("HttpDeleteServiceConfiguration completed with {0}", Status);
		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Queries for a URL ACL entry.
	Arguments:
	pUrl - The URL (if default, then enumerate the store).
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoUrlAclQuery([In, Optional] string? pUrl)
	{
		HTTP_SERVICE_CONFIG_URLACL_QUERY QueryParam = new();

		if (pUrl is not null)
		{
			// If a URL is specified, we'll Query for an exact entry.
			QueryParam.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact;
			QueryParam.KeyDesc.pUrlPrefix = pUrl;
		}
		else
		{
			// No URL is specified, so enumerate the entire store.
			QueryParam.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext;
		}

		Win32Error Status;
		for (; ; )
		{
			// First, compute bytes required for querying the first entry.
			Status = HttpQueryServiceConfiguration(HTTP_SERVICE_CONFIG_ID.HttpServiceConfigUrlAclInfo, QueryParam,
				out SafeCoTaskMemStruct<HTTP_SERVICE_CONFIG_URLACL_SET> pOutput);

			if (Status.Succeeded)
			{
				// The query succeeded! We'll print the record that we just queried.

				PrintUrlAclRecord(pOutput);

				if (pUrl is not null)
				{
					// If we are not enumerating, we are done.
					break;
				}
				else
				{
					// Since we are enumerating, we'll move on to the next record. This is done by incrementing the cursor, till we get Win32Error.ERROR_NO_MORE_ITEMS.
					QueryParam.dwToken++;
				}
			}
			else if (Win32Error.ERROR_NO_MORE_ITEMS == Status && pUrl is null)
			{
				// We are enumerating and we have reached the end. This is indicated by a Win32Error.ERROR_NO_MORE_ITEMS error code.

				// This is not a real error, since it is used to indicate that we've finished enumeration.

				Status = 0;
				break;
			}
			else
			{
				// Some other error, so we are done
				Console.Write("HttpQueryServiceConfiguration completed with {0}", Status);

				break;
			}
		}

		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Sets an URL ACL entry.
	Arguments:
	pUrl - The URL
	pAcl - The ACL specified as a SDDL string.
	Return Value:
	Success/Failure.
	--***************************************************************************/
	private static Win32Error DoUrlAclSet([In, Optional] string? pUrl, [In, Optional] string? pAcl)
	{
		HTTP_SERVICE_CONFIG_URLACL_SET SetParam = new();
		SetParam.KeyDesc.pUrlPrefix = pUrl ?? "";
		SetParam.ParamDesc.pStringSecurityDescriptor = pAcl ?? "";

		Win32Error Status = HttpSetServiceConfiguration(SetParam);

		Console.Write("HttpSetServiceConfiguration completed with {0}", Status);

		return Status;
	}

	/***************************************************************************++
	Routine Description:
	Prints a record in the URL ACL store.
	Arguments:
	pOutput - A pointer to HTTP_SERVICE_CONFIG_URLACL_SET
	Return Value:
	None.
	--***************************************************************************/
	private static void PrintUrlAclRecord(in HTTP_SERVICE_CONFIG_URLACL_SET pSetParam)
	{
		Console.Write("URL : {0}", pSetParam.KeyDesc.pUrlPrefix);

		Console.WriteLine("ACL : {0}", pSetParam.ParamDesc.pStringSecurityDescriptor);

		Console.WriteLine("------------------------------------------------------------------------------");
	}
}