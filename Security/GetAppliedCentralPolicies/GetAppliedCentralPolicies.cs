using Vanara;
using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.ActiveDS;
using static Vanara.PInvoke.AdvApi32;

class Program
{
	const string pszPolicyContainer = "LDAP://CN=Central Access Policies,CN=Claims Configuration,CN=Services,CN=Configuration,{0}";
	const string pszLdap = "LDAP://{0}";

	//-----------------------------------------------------------------------------
	//
	// wmain
	//
	//-----------------------------------------------------------------------------
	internal static int Main()
	{
		bool Succeeded = false;

		if (!GetCentralAccessPolicyIDs(out var AppliedCapIds))
		{
			goto Cleanup;
		}
		else if (AppliedCapIds is null || AppliedCapIds.Length < 1)
		{
			// No Central Access Policies were found
			Console.Write("No Central Access Policies IDs were returned\n");
			goto Cleanup;
		}

		if (!DisplayCentralAccessPolicyInformation(AppliedCapIds))
		{
			goto Cleanup;
		}

		Succeeded = true;

Cleanup:

		return Succeeded ? 0 : 1;
	}

	static bool GetCentralAccessPolicyIDs(out PSID[] CapIDs) // valid on success
	{
		// Get the applied CAP IDs on the machine
		try
		{
			CapIDs = LsaGetAppliedCAPIDs().ToArray();
			return true;
		}
		catch (Exception ex)
		{
			Console.Write("LsaGetAppliedCAPIDs Error: {0}\n", ex);
			CapIDs = [];
			return false;
		}
	}

	static bool GetNameForPolicyDirectory(out string PolicyDir)
	{
		PolicyDir = "";

		// Get the rootDSE for Active Directory
		var hr = ADsGetObject("LDAP://rootDSE", out IADs? PtrRootDse);
		if (hr.Failed)
		{
			Console.Write("ADsGetObject Error 0x{0:X}\n", (int)hr);
			goto Cleanup;
		}

		string Context = "defaultNamingContext";

		// Get the defaultNamingContext (where the policies live)
		object? NamingContext = null;
		try { NamingContext = PtrRootDse!.Get(Context); }
		catch (Exception ex)
		{
			Console.Write("IADs.Get(\"defaultNamingContext\") Error 0x{0:X}\n", ex.HResult);
			goto Cleanup;
		}

		if (NamingContext is not string)
		{
			Console.Write("IADs.Get(\"defaultNamingContext\") returned non-string type\n");
			goto Cleanup;
		}

		try
		{
			// Construct the distinguished name for the CAP container
			PolicyDir = string.Format(pszPolicyContainer, NamingContext);
		}
		catch (Exception ex)
		{
			Console.Write("Memory allocation error 0x{0:X}\n", ex.HResult);
			goto Cleanup;
		}

		return true;

Cleanup:

		return false;
	}

	static bool DisplayCentralAccessPolicyInformation(in PSID[] CapIDs)
	{
		HRESULT hr = HRESULT.S_OK;
		bool Succeeded = false;
		bool IsAppliedPolicy = false;
		string ID;
		string CN;
		string Description;
		string Rules;

		// Get the full distinguished name for the Central Access Policy container
		if (!GetNameForPolicyDirectory(out var PolicyDir))
		{
			goto Cleanup;
		}

		// Bind to the Active Directory container for Central Access Policies
		hr = ADsGetObject(PolicyDir, out IADsContainer? PtrContainer);
		if (hr.Failed)
		{
			Console.Write("ADsGetObject Error 0x{0:X}\n", (int)hr);
			goto Cleanup;
		}

		// Create an enumerator to iterate over the policies in the container
		hr = ADsBuildEnumerator(PtrContainer!, out var PtrPolicyEnum);
		if (hr.Failed)
		{
			Console.Write("ADsBuildEnumerator Error 0x{0:X}\n", (int)hr);
			goto Cleanup;
		}

		try
		{
			ID = "msAuthz-CentralAccessPolicyID";
			CN = "cn";
			Description = "description";
			Rules = "msAuthz-MemberRulesInCentralAccessPolicy";
		}
		catch (Exception e)
		{
			Console.Write("Memory allocation error 0x{0:X}\n", e.HResult);
			goto Cleanup;
		}

		// Get the next policy
		foreach (var PtrPolicy in PtrPolicyEnum.Enum().Cast<IADs?>().WhereNotNull())
		{
			//
			// Get the ID of the policy we're looking at and determine if the SID
			// matches one of the SIDs returned from LsaGetAppliedCAPIDs
			//

			// Get the CAP ID
			// The CAP ID SID is stored as a byte array
			SafePSID IDVariant;
			try
			{
				var bytes = PtrPolicy.Get<byte[]>(ID);
				if (bytes is null)
					continue;
				IDVariant = new(bytes);
			}
			catch
			{
				// If we can't get the ID, then continue on to the next policy.
				Console.Write("Invalid SID array encountered\n");
				goto Cleanup;
			}

			// Iterate over the array of applied CAP IDs and check for a match
			IsAppliedPolicy = CapIDs.Any(c => IDVariant.Equals(c));
			if (!IsAppliedPolicy)
			{
				// The CAP ID didn't match any of the applied CAP IDs
				continue;
			}

			Console.Write("\n -------------------------------------------------------\n");
			Console.Write("| Central Access Policy |");
			Console.Write("\n -------------------------------------------------------\n");

			// Get the policy name
			try
			{
				var NameVariant = PtrPolicy.Get<string>(CN);
				Console.Write($"\n Name : {NameVariant}:\n");
			}
			catch
			{
				Console.Write("IADs.Get failed to retrieve cn with error 0x{0:X}\n", (int)hr);
				goto Cleanup;
			}

			// Get the policy description
			try
			{
				var DescriptionVariant = PtrPolicy.Get<string>(Description);
				Console.Write($"\n Description : {DescriptionVariant}:\n");
			}
			catch
			{
			}

			// Get the rules associated with the policy
			string[] RulesVariant = [];
			try
			{
				RulesVariant = PtrPolicy.GetEx<string[]>(Rules) ?? [];
			}
			catch (Exception e)
			{
				Console.Write("IADs.Get Error 0x{0:X}\n", (int)e.HResult);
				goto Cleanup;
			}

			Console.Write("\n Rules:\n");
			Console.Write(" ------------------------------------------------------\n");

			//
			// Retrieve the Central Access Rules from Active Directory
			//
			if (RulesVariant.Length > 0)
			{
				// Print out each Central Access Rule in the array.
				for (int idx = 0; idx <= RulesVariant.Length; ++idx)
				{
					DisplayCentralAccessRuleInformation(RulesVariant[idx]);
				}
			}
			else
			{
				Console.Write("Couldn't find any linked Central Access Rules\n");
			}
		}

		Succeeded = true;

Cleanup:

		return Succeeded;
	}

	static bool DisplayCentralAccessRuleInformation(string? Path)
	{
		bool Succeeded = false;

		if (string.IsNullOrEmpty(Path))
		{
			Console.Write("Null path for Central Access Rule encountered\n");
			goto Cleanup;
		}

		string CN = "cn";
		string Description = "description";
		string Condition = "msAuthz-ResourceCondition";
		string EffectivePolicy = "msAuthz-EffectiveSecurityPolicy";
		string ProposedPolicy = "msAuthz-ProposedSecurityPolicy";
		string Rule = string.Format(pszLdap, Path);

		var hr = ADsGetObject(Rule, out IADs? PtrRule);
		if (PtrRule is null || hr.Failed)
		{
			Console.Write("ADsGetObject Error 0x{0:X}\n", (int)hr);
			goto Cleanup;
		}

		// Get the Central Access Rule name
		try
		{
			var NameVariant = PtrRule.Get<string>(CN);
			Console.Write(" Name : {0}\n", NameVariant);
		}
		catch (Exception ex)
		{
			Console.Write("IADs.Get Error 0x{0:X}\n", (int)ex.HResult);
			goto Cleanup;
		}

		// Get the description
		try
		{
			var DescriptionVariant = PtrRule.Get<string>(Description);
			Console.Write(" Description : {0}\n", DescriptionVariant);
		}
		catch
		{
			// Description is optional
		}

		// Get the resource condition
		try
		{
			var ConditionVariant = PtrRule.Get<string>(Condition);
			Console.Write(" Condition : {0}\n", ConditionVariant);
		}
		catch
		{
			// Resource condition is optional
		}

		// Get the security descriptor
		try
		{
			var EffectivePolicyVariant = PtrRule.Get<string>(EffectivePolicy);
			Console.Write(" Effective Policy : {0}\n", EffectivePolicyVariant);
		}
		catch (Exception ex)
		{
			Console.Write("IADs.Get Error 0x{0:X}\n", (int)ex.HResult);
			goto Cleanup;
		}

		// Get the proposed policy
		try
		{
			var ProposedPolicyVariant = PtrRule.Get<string>(ProposedPolicy);
			Console.Write(" Proposed Policy : {0}\n", ProposedPolicyVariant);
		}
		catch
		{
			// Proposed policy is optional
		}

		Console.Write(" ------------------------------------------------------\n");

		Succeeded = true;

Cleanup:

		return Succeeded;
	}
}