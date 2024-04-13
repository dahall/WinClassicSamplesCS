using Vanara.PInvoke.VssApi;
using static Vanara.PInvoke.Ole32;

namespace vshadow;

internal partial class VssClient
{
	private bool m_bDuringRestore = false;
	private VSS_SNAPSHOT_CONTEXT m_dwContext = VSS_SNAPSHOT_CONTEXT.VSS_CTX_BACKUP;
	private IVssBackupComponents m_pVssObject;

	static VssClient()
	{
		// Initialize COM security
		CoInitializeSecurity(Guid.Empty, // Allow *all* VSS writers to communicate back!
			-1, // Default COM authentication service
			default, // Default COM authorization service
			default, // reserved parameter
			Vanara.PInvoke.Rpc.RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_PKT_PRIVACY, // Strongest COM authentication level
			Vanara.PInvoke.Rpc.RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IDENTIFY, // Minimal impersonation abilities
			default, // Default COM authentication settings
			EOLE_AUTHENTICATION_CAPABILITIES.EOAC_NONE // No special options
			).ThrowIfFailed();
	}

	public VssClient(VSS_SNAPSHOT_CONTEXT dwContext = VSS_SNAPSHOT_CONTEXT.VSS_CTX_BACKUP, string xmlDoc = "", bool bDuringRestore = false)
	{
		// Create the internal backup components object
		VssFactory.CreateVssBackupComponents(out m_pVssObject).ThrowIfFailed();

		// We are during restore now?
		m_bDuringRestore = bDuringRestore;

		// Call either Initialize for backup or for restore
		if (m_bDuringRestore)
		{
			m_pVssObject.InitializeForRestore(xmlDoc);
		}
		else
		{
			// Initialize for backup
			if (string.IsNullOrEmpty(xmlDoc))
				m_pVssObject.InitializeForBackup();
			else
				m_pVssObject.InitializeForBackup(xmlDoc);

#if VSS_SERVER

			// Set the context, if different than the default context
			if (dwContext != VSS_CTX_BACKUP)
			{
				ft.WriteLine(L"- Setting the VSS context to: 0x%08lx", dwContext);
				CHECK_COM(m_pVssObject.SetContext(dwContext));
			}

#endif
		}

		// Keep the context
		m_dwContext = dwContext;

		// Set various properties per backup components instance
		m_pVssObject.SetBackupState(true, true, VSS_BACKUP_TYPE.VSS_BT_FULL, false);
	}
}