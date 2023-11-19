using Vanara.Extensions;
using Vanara.PInvoke;
using static Vanara.PInvoke.AdvApi32;

namespace Audit;

internal static class Audit
{
	public static int Main(string[] args)
	{
		// pickup machine name if appropriate
		var wComputerName = args.Length == 2 ? args[1] : null; // local machine

		try
		{
			// display current audit state
			using (var PolicyHandle = LsaOpenPolicy(LsaPolicyRights.POLICY_VIEW_AUDIT_INFORMATION, wComputerName))
			{
				// display current auditing status
				DisplayAudit(PolicyHandle);
			}

			// enable success and failure audits of logon/logoff events
			using (var PolicyHandle = LsaOpenPolicy(LsaPolicyRights.POLICY_VIEW_AUDIT_INFORMATION | LsaPolicyRights.POLICY_SET_AUDIT_REQUIREMENTS, wComputerName))
			{
				// enable audits
				SetAuditMode(PolicyHandle, true);

				// enable success and failure auditing of logon/logoff
				SetAuditEvent(PolicyHandle, POLICY_AUDIT_EVENT_TYPE.AuditCategoryLogon, POLICY_AUDIT_EVENT_OPTIONS.POLICY_AUDIT_EVENT_SUCCESS | POLICY_AUDIT_EVENT_OPTIONS.POLICY_AUDIT_EVENT_FAILURE);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			return 1;
		}

		return 0;
	}

	private static void DisplayAudit(LSA_HANDLE PolicyHandle)
	{
		// obtain AuditEvents
		var AuditEvents = LsaQueryInformationPolicy<POLICY_AUDIT_EVENTS_INFO>(PolicyHandle);

		// successfully obtained AuditEventsInformation. Now display.
		if (AuditEvents.AuditingMode)
		{
			Console.Write("Auditing Enabled\n");
		}
		else
		{
			Console.Write("Auditing Disabled\n");
		}

		for (var i = 0U; i < AuditEvents.MaximumAuditEventCount; i++)
		{
			DisplayAuditEventOption(i, AuditEvents.EventAuditingOptions![i]);
		}
	}

	private static void DisplayAuditEventOption(uint EventTypeIndex, POLICY_AUDIT_EVENT_OPTIONS EventOption)
	{
		Console.Write("AuditCategory");

		switch ((POLICY_AUDIT_EVENT_TYPE)EventTypeIndex)
		{
			case POLICY_AUDIT_EVENT_TYPE.AuditCategorySystem:
				Console.Write("System");
				break;

			case POLICY_AUDIT_EVENT_TYPE.AuditCategoryLogon:
				Console.Write("Logon");
				break;

			case POLICY_AUDIT_EVENT_TYPE.AuditCategoryObjectAccess:
				Console.Write("ObjectAccess");
				break;

			case POLICY_AUDIT_EVENT_TYPE.AuditCategoryPrivilegeUse:
				Console.Write("PrivilegeUse");
				break;

			case POLICY_AUDIT_EVENT_TYPE.AuditCategoryDetailedTracking:
				Console.Write("DetailedTracking");
				break;

			case POLICY_AUDIT_EVENT_TYPE.AuditCategoryPolicyChange:
				Console.Write("PolicyChange");
				break;

			case POLICY_AUDIT_EVENT_TYPE.AuditCategoryAccountManagement:
				Console.Write("AccountManagement");
				break;

			default:
				Console.Write("Unknown");
				break;
		}

		if (EventOption.IsFlagSet(POLICY_AUDIT_EVENT_OPTIONS.POLICY_AUDIT_EVENT_SUCCESS))
			Console.Write(" AUDIT_EVENT_SUCCESS");

		if (EventOption.IsFlagSet(POLICY_AUDIT_EVENT_OPTIONS.POLICY_AUDIT_EVENT_FAILURE))
			Console.Write(" AUDIT_EVENT_FAILURE");

		Console.Write("\n");
	}

	private static void SetAuditEvent(LSA_HANDLE PolicyHandle, POLICY_AUDIT_EVENT_TYPE EventType, POLICY_AUDIT_EVENT_OPTIONS EventOption)
	{
		// obtain AuditEvents
		var pae = LsaQueryInformationPolicy<POLICY_AUDIT_EVENTS_INFO>(PolicyHandle);

		// ensure we were passed a valid EventType and EventOption
		if ((uint)EventType > pae.MaximumAuditEventCount || !EventOption.IsValid())
		{
			throw ((NTStatus)NTStatus.STATUS_INVALID_PARAMETER).GetException()!;
		}

		// set all auditevents to the unchanged status...
		for (var i = 0U; i < pae.MaximumAuditEventCount; i++)
		{
			pae.EventAuditingOptions![i] = POLICY_AUDIT_EVENT_OPTIONS.POLICY_AUDIT_EVENT_UNCHANGED;
		}

		// ...and update only the specified EventType
		pae.EventAuditingOptions![(int)EventType] = EventOption;

		// set the new AuditEvents
		LsaSetInformationPolicy(PolicyHandle, pae);
	}

	private static void SetAuditMode(LSA_HANDLE PolicyHandle, bool bEnable)
	{
		// obtain current AuditEvents
		var AuditEvents = LsaQueryInformationPolicy<POLICY_AUDIT_EVENTS_INFO>(PolicyHandle);

		// update the relevant member
		AuditEvents.AuditingMode = bEnable;

		// set all auditevents to the unchanged status...
		for (var i = 0U; i < AuditEvents.MaximumAuditEventCount; i++)
		{
			AuditEvents.EventAuditingOptions![i] = POLICY_AUDIT_EVENT_OPTIONS.POLICY_AUDIT_EVENT_UNCHANGED;
		}

		// set the new auditing mode (enabled or disabled)
		LsaSetInformationPolicy(PolicyHandle, AuditEvents);
	}
}