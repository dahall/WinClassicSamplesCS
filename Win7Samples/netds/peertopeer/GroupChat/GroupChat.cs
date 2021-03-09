using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.P2P;
using static Vanara.PInvoke.Ws2_32;

namespace GroupChat
{
	public partial class GroupChat : Form
	{
		// File Extensions
		internal const string c_wzFileExtIdt = "idt";

		internal const string c_wzFileExtInv = "inv";

		internal const int MAX_INVITATION = 64 * 1024;

		// The unique identifier for Whisper (Private chat) messages
		private static readonly SafeHGlobalStruct<Guid> DATA_TYPE_WHISPER_MESSAGE = new Guid(0x4d5b2f11, 0x6522, 0x433b, 0x84, 0xef, 0xa2, 0x98, 0xe6, 0x7, 0xbb, 0xbb);

		// The unique identifier for chat messages
		private static readonly SafeHGlobalStruct<Guid> RECORD_TYPE_CHAT_MESSAGE = new Guid(0x4d5b2f11, 0x6522, 0x433b, 0x84, 0xef, 0xa2, 0x98, 0xe6, 0x7, 0x57, 0xb0);

		// Authentication scheme (password or GMC)
		private PEER_GROUP_AUTHENTICATION_SCHEME g_dwAuth = PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_GMC_AUTHENTICATION;

		// global (TRUE) or local (FALSE) group scope
		private bool g_fGlobalScope = true;

		private SafeEventHandle g_hEvent;
		private HGROUP g_hGroup;
		private SafeGroupHPEEREVENT g_hPeerEvent;
		private SafeRegisteredWaitHandle g_hWait;
		private ulong g_ullConnectionId;
		private string g_wzDCName;
		private string g_wzName;

		public GroupChat()
		{
			Main = this;
			InitSystem();
			InitializeComponent();
		}

		internal static GroupChat Main { get; private set; }

		//-----------------------------------------------------------------------------
		// Function: BrowseHelper
		//
		// Purpose:  Use the common dialog to get/set a path.
		//
		// Returns:  nothing
		//
		internal static void BrowseHelper(IWin32Window hDlg, TextBox idEditbox, string pwzFileType, string pwzFileExtension, bool fOpen)
		{
			var filter = string.Format("{0} (*.{1})|*.{1}", pwzFileType, pwzFileExtension);
			FileDialog fd = fOpen ? (FileDialog)new OpenFileDialog() : new SaveFileDialog();
			fd.Filter = filter;
			fd.FileName = idEditbox.Text;
			fd.DefaultExt = pwzFileExtension;
			if (fd.ShowDialog(hDlg) == DialogResult.OK)
				idEditbox.Text = fd.FileName;
		}

		internal static void CleanupSystem()
		{
			PeerGroupShutdown();
			WSACleanup();
		}

		//-----------------------------------------------------------------------------
		// Function: DisplayError
		//
		// Purpose:  Display an error message.
		//
		// Returns:  nothing
		//
		internal static void DisplayError(string pwzMsg)
		{
			MessageBox.Show(ActiveForm, pwzMsg, "Group Chat Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		internal static void DisplayHrError(string pwzMsg, HRESULT hr)
		{
			DisplayError(pwzMsg + "\r\n\r\n" + hr.ToString());
		}

		//-------------------------------------------------------------------------
		// Function: GetLocalCloudName
		//
		// Purpose:  Retrieve first available local cloud name
		//
		// Arguments:
		//   cchCloudNameSize[in]: number of characters in pwzCloudName
		//                         (usually MAX_CLOUD_NAME)
		//   pwzCloudName [out]  : location to which cloud name will be copied
		//
		// Returns:  HRESULT
		//
		internal static HRESULT GetLocalCloudName(out string pwzCloudName)
		{
			HRESULT hr = HRESULT.S_OK;
			pwzCloudName = null;
			SafeHGlobalStruct<WSAQUERYSET> pResults = new SafeHGlobalStruct<WSAQUERYSET>(IntPtr.Zero);

			// Fill out information for WSA query
			var CloudInfo = new PNRPCLOUDINFO
			{
				dwSize = (uint)Marshal.SizeOf<PNRPCLOUDINFO>(),
				Cloud = new PNRP_CLOUD_ID { Scope = PNRP_SCOPE.PNRP_LINK_LOCAL_SCOPE }
			};

			using var pCloudInfo = new PinnedObject(CloudInfo);
			var blPnrpData = new BLOB
			{
				cbSize = CloudInfo.dwSize,
				pBlobData = pCloudInfo
			};

			using var pGuid = new PinnedObject(SVCID_PNRPCLOUD);
			using var pBlob = new PinnedObject(blPnrpData);
			var querySet = new WSAQUERYSET
			{
				dwSize = (uint)Marshal.SizeOf<WSAQUERYSET>(),
				dwNameSpace = NS.NS_PNRPCLOUD,
				lpServiceClassId = (IntPtr)pGuid,
				lpBlob = pBlob
			};

			var iErr = WSALookupServiceBegin(querySet, LUP.LUP_RETURN_NAME, out var hLookup);

			if (iErr.Failed)
			{
				return iErr.ToHRESULT();
			}
			else
			{
				var tempResultSet = new SafeHGlobalStruct<WSAQUERYSET>();
				uint dwResultSize = tempResultSet.Size;

				// Get size of results
				iErr = WSALookupServiceNext(hLookup, 0, ref dwResultSize, tempResultSet);

				if (iErr.Failed)
				{
					var dwErr = WSAGetLastError();

					if (dwErr == 10014 /*WSAEFAULT*/)
					{
						// allocate space for results
						pResults = new SafeHGlobalStruct<WSAQUERYSET>(dwResultSize);
						if (pResults.IsInvalid)
						{
							hr = WSAGetLastError().ToHRESULT();
						}
					}
					else
					{
						hr = dwErr.ToHRESULT();
					}
				}
			}

			if (hr.Succeeded)
			{
				// retrieve the local cloud information
				uint dwResultSize = pResults.Size;
				iErr = WSALookupServiceNext(hLookup, 0, ref dwResultSize, pResults);
				if (iErr.Failed)
				{
					hr = WSAGetLastError().ToHRESULT();
				}
			}

			// Copy the cloud name (if applicable) and scope ID
			if (hr.Succeeded)
			{
				pwzCloudName = pResults.Value.lpszServiceInstanceName;
				if (hr.Failed)
				{
					DisplayHrError("Failed to copy cloud name", hr);
				}
			}

			if (!hLookup.IsNull)
			{
				WSALookupServiceEnd(hLookup);
			}

			pResults.Dispose();
			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: GetSelectedIdentity
		//
		// Purpose:  Get the currently selected identity.
		//
		// Returns:  A pointer to the identity string.
		//
		internal static string GetSelectedIdentity(ComboBox cb) => cb.SelectedIndex == -1 ? null : ((PEER_NAME_PAIR)cb.SelectedItem).pwzPeerName;

		//-----------------------------------------------------------------------------
		// Function: InitSystem
		//
		// Purpose:  Initialize the main system (Peer-to-Peer, windows, controls, etc.)
		//
		// Returns:  S_OK if the system was successfully initialized
		//
		internal static HRESULT InitSystem()
		{
			// Setup Winsock
			var hr = WSAStartup(Macros.MAKEWORD(2, 2), out _).ToHRESULT();
			if (hr.Failed)
			{
				DisplayHrError("Unable to Intialize WSA.", hr);
				return HRESULT.E_UNEXPECTED;
			}

			return PeerGroupStartup(PEER_GROUP_VERSION, out _);
		}

		//-----------------------------------------------------------------------------
		// Function: RefreshGroupCombo
		//
		// Purpose:  Given an identity name (in pwzIdentity), fills the combo box (specified by hwnd) with
		//           all the groups accessible by the specified identity.  The ItemData of each entry
		//           in the combobox points to the groupid.  For these pointers to remain valid after
		//           the call, the PEER_NAME_PAIR array is returned, and must be freed via PeerFreeData( )
		//           by the calling function OpenGroupProc( ).
		//
		// Returns:  HRESULT
		//
		internal static void RefreshGroupCombo(ComboBox hwndCtrl, string pwzIdentity)
		{
			hwndCtrl.Items.Clear();
			if (pwzIdentity is null) return;
			using var h = PeerEnumGroups(pwzIdentity);
			var groups = Array.ConvertAll(h.ToArray(), p => (object)p);
			if (groups.Length > 0)
			{
				hwndCtrl.Items.AddRange(groups);
				hwndCtrl.SelectedIndex = 0;
			}
		}

		//-----------------------------------------------------------------------------
		// Function: RefreshIdentityCombo
		//
		// Purpose:  Fills the specified combo box w/ all the available identities.  The combo box will show
		//           the friendly names for the identities, and the ItemData will point to the PeerNames.  For
		//           these pointers to remain valid the PEER_NAME_PAIR array is returned w/ the function and
		//           must be freed via PeerFreeData( ) by the calling function.
		//
		// Returns:  HRESULT
		//
		internal static void RefreshIdentityCombo(ComboBox hwndCtrl, bool bAddNullIdentity)
		{
			hwndCtrl.Items.Clear();
			using (var h = PeerEnumIdentities())
			{
				var names = Array.ConvertAll(h.ToArray(), p => (object)p);
				if (names.Length > 0)
				{
					hwndCtrl.Items.AddRange(names);
				}
			}
			if (bAddNullIdentity)
			{
				var ni = new PEER_NAME_PAIR { dwSize = (uint)Marshal.SizeOf(typeof(PEER_NAME_PAIR)), pwzFriendlyName = "NULL Identity" };
				hwndCtrl.Items.Add(ni);
			}
			if (hwndCtrl.Items.Count > 0)
			{
				hwndCtrl.SelectedIndex = 0;
			}
		}

		//-----------------------------------------------------------------------------
		// Function: CleanupGroup
		//
		// Purpose:  Clean up all the global variables associated with this group.
		//
		// Returns:  nothing
		//
		internal void CleanupGroup()
		{
			if (g_hPeerEvent != null)
			{
				g_hPeerEvent.Dispose();
				g_hPeerEvent = null;
			}

			if (g_hEvent != null)
			{
				g_hEvent.Dispose();
				g_hEvent = null;
			}

			if (g_hWait != null)
			{
				g_hWait.Dispose();
				g_hWait = null;
			}

			if (!g_hGroup.IsNull)
			{
				PeerGroupClose(g_hGroup);
				g_hGroup = HGROUP.NULL;
			}
		}

		//-----------------------------------------------------------------------------
		// Function: CreateGroup
		//
		// Purpose:  Creates a new group with the friendly name.
		//
		// Parameters:
		//      pwzName     [in] : Friendly name of group
		//      pwzIdentity [in] : Path to identity file of group creator
		//      pwzPassword [in] : Password of group (if applicable)
		//
		// Returns:  HRESULT
		//
		internal HRESULT CreateGroup(string pwzName, string pwzIdentity, string pwzPassword, bool pwdAuth, bool globalScope)
		{
			HRESULT hr = HRESULT.S_OK;

			// Retrieve user's selection on authentication method
			if (pwdAuth)
			{
				g_dwAuth = PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_PASSWORD_AUTHENTICATION;
			}
			else
			{
				g_dwAuth = PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_GMC_AUTHENTICATION;
				pwzPassword = null;
			}

			g_fGlobalScope = globalScope;

			if (hr.Succeeded)
			{
				if (string.IsNullOrEmpty(pwzName))
				{
					hr = HRESULT.E_INVALIDARG;
					DisplayHrError("Please enter a group name.", hr);
				}
			}

			if (hr.Succeeded)
			{
				CleanupGroup();

				PEER_GROUP_PROPERTIES props = new PEER_GROUP_PROPERTIES
				{
					dwSize = (uint)Marshal.SizeOf<PEER_GROUP_PROPERTIES>(),
					pwzClassifier = "SampleChatGroup",
					pwzFriendlyName = pwzName,
					pwzCreatorPeerName = pwzIdentity,
					dwAuthenticationSchemes = g_dwAuth
				};
				if (g_dwAuth == PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_PASSWORD_AUTHENTICATION)
				{
					props.groupPasswordRole = PEER_GROUP_ROLE_ADMIN;
					props.pwzGroupPassword = pwzPassword;
				}

				if (g_fGlobalScope)
				{
					props.pwzCloud = null;
					hr = PeerGroupCreate(props, out g_hGroup);
				}
				else
				{
					hr = GetLocalCloudName(out var pwzCloudName);
					if (hr.Succeeded)
					{
						props.pwzCloud = pwzCloudName;
						hr = PeerGroupCreate(props, out g_hGroup);
					}
				}

				if (hr.Failed)
				{
					if (hr == HRESULT.PEER_E_PASSWORD_DOES_NOT_MEET_POLICY)
					{
						DisplayHrError("Password does not meet local policy.", hr);
					}
					else
					{
						DisplayHrError("Failed to create a new group.", hr);
					}
				}
			}

			if (hr.Succeeded)
			{
				hr = PrepareToChat();
			}

			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: JoinGroup
		//
		// Purpose:  Uses the invitation to join a group with a specific identity.
		//           Displays a message if there was an error.
		//
		// Parameters:
		//  pwzIdentity [in] : Path to identity file (not used w/password based groups)
		//  pwzFileName [in] : Path to the group invitation
		//  pwzPassword [in] : Password of group (if applicable)
		//
		// Returns:  HRESULT
		//
		internal HRESULT JoinGroup(string pwzIdentity, string pwzFileName, string pwzPassword, bool pwdAuth)
		{
			HRESULT hr;
			string wzInvitation = null;
			g_dwAuth = pwdAuth ? PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_PASSWORD_AUTHENTICATION : PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_GMC_AUTHENTICATION;
			try
			{
				using var fs = File.OpenRead(pwzFileName);
				var bytes = new byte[MAX_INVITATION * 2];
				fs.Read(bytes, 0, bytes.Length);
				wzInvitation = Encoding.Unicode.GetString(bytes);
			}
			catch
			{
				hr = HRESULT.E_FAIL;
				DisplayHrError("Error opening group invitation file", hr);
				return hr;
			}

			if (g_dwAuth == PEER_GROUP_AUTHENTICATION_SCHEME.PEER_GROUP_GMC_AUTHENTICATION)
			{
				// NULL parameter indicates that the cloud name will be selected automatically
				hr = PeerGroupJoin(pwzIdentity, wzInvitation, default, out g_hGroup);

				// In case of failure, try using a local cloud name
				if (hr.Failed)
				{
					hr = GetLocalCloudName(out var wzCloudName);
					if (hr.Failed)
					{
						DisplayHrError("Could not find local cloud name.", hr);
					}
					else
					{
						hr = PeerGroupJoin(pwzIdentity, wzInvitation, wzCloudName, out g_hGroup);
					}
				}
			}
			else
			{
				// NULL parameter indicates that the cloud name will be selected automatically
				hr = PeerGroupPasswordJoin(pwzIdentity, wzInvitation, pwzPassword, default, out g_hGroup);
				// In case of failure, try using a local cloud name
				if (hr.Failed)
				{
					hr = GetLocalCloudName(out var wzCloudName);
					if (hr.Failed)
					{
						DisplayHrError("Could not find local cloud name.", hr);
					}
					else
					{
						hr = PeerGroupPasswordJoin(pwzIdentity, wzInvitation, pwzPassword, wzCloudName, out g_hGroup);
					}
				}
			}

			if (hr.Succeeded)
			{
				hr = PrepareToChat();
			}
			else
			{
				DisplayHrError("Failed to join group.", hr);
			}

			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: OpenGroup
		//
		// Purpose:  Open an existing group for a particular identity.
		//           Displays a message if there was an error.
		//
		// Returns:  HRESULT
		//
		internal HRESULT OpenGroup(string pwzIdentity, string pwzName)
		{
			HRESULT hr = HRESULT.S_OK;
			g_hGroup = HGROUP.NULL;

			if (hr.Succeeded)
			{
				if (string.IsNullOrEmpty(pwzName))
				{
					DisplayHrError("Please select a group.", hr = HRESULT.E_INVALIDARG);
				}
			}

			if (hr.Succeeded)
			{
				// Release any previous group resources
				CleanupGroup();

				// The NULL parameter indicates that we'll let grouping try to automatically determine which cloud to use
				hr = PeerGroupOpen(pwzIdentity, pwzName, default, out g_hGroup);

				// If NULL does not work, we try the first link local cloud
				if (hr.Failed)
				{
					hr = GetLocalCloudName(out var wzCloudName);
					if (hr.Succeeded)
					{
						hr = PeerGroupOpen(pwzIdentity, pwzName, wzCloudName, out g_hGroup);
					}
				}
				// Otherwise, return failure
				if (hr.Failed)
				{
					DisplayHrError("Failed to open group.", hr);
				}
			}

			if (hr.Succeeded)
			{
				hr = Main.PrepareToChat();
			}

			if (hr.Succeeded)
			{
				GetFriendlyNameForIdentity(g_hGroup, pwzIdentity, out g_wzName);
			}

			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: PrepareToChat
		//
		// Purpose:  Does the initial hookup of the group required after a successful
		//           create, open or join.
		//
		// Returns:  HRESULT
		//
		internal HRESULT PrepareToChat()
		{
			HRESULT hr = RegisterForEvents();

			if (hr.Failed)
			{
				DisplayHrError("Unable to register for events.", hr);
			}
			if (hr.Succeeded)
			{
				hr = PeerGroupConnect(g_hGroup);
				if (hr.Failed)
				{
					DisplayHrError("Unable to connect to the group.", hr);
				}
			}
			if (hr.Succeeded)
			{
				IDM_CREATEINVITATION.Enabled = true;
				hr = PeerGroupGetStatus(g_hGroup, out var dwStatus);
				if (hr.Succeeded)
				{
					ProcessStatusChanged(dwStatus);
				}
			}
			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: RegisterForEvents
		//
		// Purpose:  Registers the EventCallback function so it will be called for only
		//           those events that are specified.
		//
		// Returns:  HRESULT
		//
		internal HRESULT RegisterForEvents()
		{
			HRESULT hr = HRESULT.S_OK;
			PEER_GROUP_EVENT_REGISTRATION[] regs = new[] {
				new PEER_GROUP_EVENT_REGISTRATION { eventType = PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_RECORD_CHANGED, pType = RECORD_TYPE_CHAT_MESSAGE },
				new PEER_GROUP_EVENT_REGISTRATION { eventType = PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_MEMBER_CHANGED },
				new PEER_GROUP_EVENT_REGISTRATION { eventType = PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_STATUS_CHANGED },
				new PEER_GROUP_EVENT_REGISTRATION { eventType = PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_DIRECT_CONNECTION, pType = DATA_TYPE_WHISPER_MESSAGE },
				new PEER_GROUP_EVENT_REGISTRATION { eventType = PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_INCOMING_DATA },
			};

			g_hEvent = CreateEvent(null, false, false);
			if (g_hEvent.IsNull)
			{
				hr = Win32Error.GetLastError().ToHRESULT();
			}
			else
			{
				hr = PeerGroupRegisterEvent(g_hGroup, g_hEvent, (uint)regs.Length, regs, out g_hPeerEvent);
			}

			if (hr.Succeeded)
			{
				if (!RegisterWaitForSingleObject(out g_hWait, g_hEvent, EventCallback, default, INFINITE, WT.WT_EXECUTEDEFAULT))
				{
					hr = HRESULT.E_UNEXPECTED;
				}
			}

			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: SetStatus
		//
		// Purpose:  Set the text of the status bar.
		//
		// Returns:  nothing
		//
		internal void SetStatus(string pwzStatus) => SB_PART_MESSAGE.Text = pwzStatus;

		//-----------------------------------------------------------------------------
		// Function: GetFriendlyNameForIdentity
		//
		// Purpose:  Retrieve the friendly name for an identity
		//
		// Returns:  The number of characters copied to the buffer
		//           (not including the terminating null.)
		//
		private static int GetFriendlyNameForIdentity(
			HGROUP hGroup,
			string pwzIdentity, // The identity to find
			out string pwzName) // The buffer for the friendly name
		{
			// Always provide a default friendly name
			pwzName = "?";

			using var h = PeerGroupEnumMembers(hGroup, 0, pwzIdentity);
			foreach (var ppMember in h)
				pwzName = ppMember.pCredentialInfo.ToStructure<PEER_CREDENTIAL_INFO>().pwzFriendlyName;

			return pwzName.Length;
		}

		//-----------------------------------------------------------------------------
		// Function: AddChatRecord
		//
		// Purpose:  This adds a new chat message record to the group.
		//
		// Returns:  HRESULT
		//
		private HRESULT AddChatRecord(string pwzMessage)
		{
			// calculate 2 minute expiration time in 100 nanosecond resolution
			var ulExpire = DateTime.Now + TimeSpan.FromMinutes(2);

			// Set up the record
			using var pMsg = new SafeCoTaskMemString(pwzMessage);
			var record = new PEER_RECORD
			{
				dwSize = (uint)Marshal.SizeOf<PEER_RECORD>(),
				data = new PEER_DATA { cbData = pMsg.Size, pbData = pMsg },
				ftExpiration = ulExpire.ToFileTimeStruct()
			};

			PeerGroupUniversalTimeToPeerTime(g_hGroup, record.ftExpiration, out record.ftExpiration);

			// Set the record type Guid
			record.type = RECORD_TYPE_CHAT_MESSAGE;

			// Add the record to the database
			var hr = PeerGroupAddRecord(g_hGroup, record, out var idRecord);
			if (hr.Failed)
			{
				DisplayHrError("Failed to add a chat record to the group.", hr);
			}

			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: AddParticipant
		//
		// Purpose: Adds a participant name to the member list.
		// Allocates a copy of the identity string to store in the data area
		// of the list item.
		//
		// Returns: nothing
		//
		private void AddParticipant(string pwzIdentity)
		{
			GetFriendlyNameForIdentity(g_hGroup, pwzIdentity, out var wzUserName);
			AddParticipantName(pwzIdentity, wzUserName);
			DisplayChatMessage(pwzIdentity, "has joined the group");
		}

		//-----------------------------------------------------------------------------
		// Function: AddParticipantName
		//
		// Purpose: Adds a participant name to the list.
		// Allocates a copy of the identity string to store in the data area
		// of the list item.
		//
		// Returns: nothing
		//
		private void AddParticipantName(string pwzIdentity, string pwzUserName)
		{
			var iItem = IDC_MEMBERS.Items.Add(new PEER_NAME_PAIR { pwzFriendlyName = pwzUserName, pwzPeerName = pwzIdentity });
			if (iItem < 0)
			{
				DisplayError("Unable to add participant name");
			}
		}

		//-----------------------------------------------------------------------------
		// Function: ClearParticipantList
		//
		// Purpose:  Clear the list of partipants.
		//
		// Returns:  nothing
		//
		private void ClearParticipantList()
		{
			IDC_MEMBERS.Items.Clear();
		}

		private void CmdCloseGroup()
		{
			CleanupGroup();
			ProcessStatusChanged(0);
			SetStatus("Closed group");
		}

		//-----------------------------------------------------------------------------
		// Function: DeleteParticipant
		//
		// Purpose:  Deletes the participant and associated data from the member list.
		//
		// Returns:  nothing
		//
		private void DeleteParticipant(int iItem)
		{
			IDC_MEMBERS.Items.RemoveAt(iItem);
		}

		//-----------------------------------------------------------------------------
		// Function: DisplayChatMessage
		//
		// Purpose:  Display a chat message for an identity.
		//
		// Returns:  nothing
		//
		private void DisplayChatMessage(string pwzIdentity, string pwzMsg)
		{
			// Retrieve the friendly name for the identity
			GetFriendlyNameForIdentity(g_hGroup, pwzIdentity, out var wzName);

			// Format the message
			var wzMessage = $"[{wzName}]: {pwzMsg}\r\n";

			DisplayMsg(wzMessage);
		}

		//-----------------------------------------------------------------------------
		// Function: DisplayMsg
		//
		// Purpose:  Display a message in the window.
		//
		// Returns:  nothing
		//
		private void DisplayMsg(string pwzMsg)
		{
			IDC_MESSAGES.Text += pwzMsg;
		}

		//-----------------------------------------------------------------------------
		// Function: DisplayReceivedWhisper
		//
		// Purpose:  Display a whispered message for an identity.
		//
		// Returns:  nothing
		//
		private void DisplayReceivedWhisper(string pwzIdentity, string pwzMsg)
		{
			// Format the incoming message with the originator's name
			GetFriendlyNameForIdentity(g_hGroup, pwzIdentity, out var wzName);

			// Format the message
			DisplayMsg($"<Whisper from {wzName}>: {pwzMsg}\r\n");
		}

		//-----------------------------------------------------------------------------
		// Function: DisplaySentWhisper
		//
		// Purpose:  Display a whispered message that was sent.
		//
		// Returns:  nothing
		//
		private void DisplaySentWhisper(string pwzMsg)
		{
			// Format the originator's message
			var wzMsg = $"<Whisper to {g_wzDCName}>: {pwzMsg}\r\n";
			DisplayMsg(wzMsg);
		}

		//-----------------------------------------------------------------------------
		// Function: EventCallback
		//
		// Purpose:  Handle events raised by the grouping infrastructure.
		//
		// Returns:  nothing
		//
		private void EventCallback(IntPtr _, bool __)
		{
			while (true)
			{
				HRESULT hr = PeerGroupGetEventData(g_hPeerEvent, out var pData);
				if (hr.Failed || HRESULT.PEER_S_NO_EVENT_DATA == hr)
				{
					break;
				}

				using (pData)
				{
					var pEventData = pData.ToStructure<PEER_GROUP_EVENT_DATA>();
					Invoke((MethodInvoker)delegate
					{
						switch (pEventData.eventType)
						{
							case PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_RECORD_CHANGED:
								ProcessRecordChanged(pEventData.recordChangeData);
								break;

							case PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_STATUS_CHANGED:
								ProcessStatusChanged(pEventData.dwStatus);
								break;

							case PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_MEMBER_CHANGED:
								ProcessMemberChanged(pEventData.memberChangeData);
								break;

							case PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_DIRECT_CONNECTION:
								break;

							case PEER_GROUP_EVENT_TYPE.PEER_GROUP_EVENT_INCOMING_DATA:
								ProcessIncomingData(pEventData);
								break;

							default:
								break;
						}
					});
				}
			}
		}

		//-----------------------------------------------------------------------------
		// Function: FindParticipant
		//
		// Purpose: Find a participant of the group chat in the list based on their identity.
		//
		// Returns: the index of position in the list or -1 if not found.
		//
		private int FindParticipant(string pwzIdentity)
		{
			return IDC_MEMBERS.Items.IndexOf(pwzIdentity);
		}

		//-----------------------------------------------------------------------------
		// Function: GetSelectedChatMember
		//
		// Purpose:  Get the currently selected member from the members list.
		//
		// Returns:  A pointer to the identity string.
		//
		private string GetSelectedChatMember() => IDC_MEMBERS.SelectedItem?.ToString();

		private void IDC_MEMBERS_DoubleClick(object sender, EventArgs e)
		{
			var pwzIdentity = GetSelectedChatMember();
			if (SetupDirectConnection(pwzIdentity).Succeeded)
			{
				new IDD_WHISPERMESSAGE().ShowDialog();
			}
		}

		private void IDC_SEND_Click(object sender, EventArgs e) => ProcessSendButton();

		private void IDM_ABOUT_Click(object sender, EventArgs e) => new IDD_ABOUTBOX().ShowDialog(this);

		private void IDM_CLEARTEXT_Click(object sender, EventArgs e) => IDC_MESSAGES.Text = "";

		private void IDM_CLOSEGROUP_Click(object sender, EventArgs e) => CmdCloseGroup();

		private void IDM_CREATEGROUP_Click(object sender, EventArgs e) => new IDD_CREATEGROUP().ShowDialog(this);

		private void IDM_CREATEIDENTITY_Click(object sender, EventArgs e) => new IDD_NEWIDENTITY().ShowDialog(this);

		private void IDM_DELETEGROUP_Click(object sender, EventArgs e) => new IDD_DELETEGROUP().ShowDialog(this);

		private void IDM_DELETEIDENTITY_Click(object sender, EventArgs e) => new IDD_DELETEIDENTITY().ShowDialog(this);

		private void IDM_EXIT_Click(object sender, EventArgs e) => Close();

		private void IDM_JOINGROUP_Click(object sender, EventArgs e) => new IDD_JOINGROUP().ShowDialog(this);

		private void IDM_OPENGROUP_Click(object sender, EventArgs e) => new IDD_OPENGROUP().ShowDialog(this);

		private void IDM_SAVEIDENTITYINFO_Click(object sender, EventArgs e) => new IDD_SAVEIDENTITYINFO().ShowDialog(this);

		//-----------------------------------------------------------------------------
		// Function: ProcessIncomingData
		//
		// Purpose: Processes the PEER_GROUP_EVENT_INCOMING_DATA event.
		//
		// Returns: nothing
		//
		private void ProcessIncomingData(in PEER_GROUP_EVENT_DATA pEventData)
		{
			var evConnId = pEventData.incomingData.ullConnectionId;
			// Get a list of all the active direct connections
			using var h = PeerGroupEnumConnections(g_hGroup, PEER_CONNECTION_FLAGS.PEER_CONNECTION_DIRECT);
			foreach (var pConnectionInfo in h.Where(ci => ci.ullConnectionId == evConnId))
			{
				// assume pbData is a null terminated string
				DisplayReceivedWhisper(pConnectionInfo.pwzPeerId, Marshal.PtrToStringUni(pEventData.incomingData.data.pbData));
			}

			PeerGroupCloseDirectConnection(g_hGroup, evConnId);
		}

		//-----------------------------------------------------------------------------
		// Function: ProcessMemberChanged
		//
		// Purpose: Processes the PEER_GROUP_EVENT_MEMBER_CHANGED event.
		//
		// Returns: nothing
		//
		private void ProcessMemberChanged(in PEER_EVENT_MEMBER_CHANGE_DATA pData)
		{
			switch (pData.changeType)
			{
				case PEER_MEMBER_CHANGE_TYPE.PEER_MEMBER_CONNECTED:
					// This check must be made in case PEER_MEMBER_UPDATED is fired first
					if (FindParticipant(pData.pwzIdentity) == -1)
					{
						AddParticipant(pData.pwzIdentity);
					}
					break;

				case PEER_MEMBER_CHANGE_TYPE.PEER_MEMBER_DISCONNECTED:
					RemoveParticipant(pData.pwzIdentity);
					break;

				case PEER_MEMBER_CHANGE_TYPE.PEER_MEMBER_UPDATED:
					if (FindParticipant(pData.pwzIdentity) != -1)
					{
						RemoveParticipant(pData.pwzIdentity);
					}
					AddParticipant(pData.pwzIdentity);
					break;

				default:
					break;
			}
		}

		//-----------------------------------------------------------------------------
		// Function: ProcessRecordChanged
		//
		// Purpose: Processes the PEER_GROUP_EVENT_RECORD_CHANGED event.
		//
		// Returns: nothing
		//
		private void ProcessRecordChanged(in PEER_EVENT_RECORD_CHANGE_DATA pData)
		{
			switch (pData.changeType)
			{
				case PEER_RECORD_CHANGE_TYPE.PEER_RECORD_ADDED:
					if (pData.recordType.Equals(RECORD_TYPE_CHAT_MESSAGE.Value))
					{
						HRESULT hr = PeerGroupGetRecord(g_hGroup, pData.recordId, out var pRecord);
						if (hr.Succeeded)
						{
							var rec = pRecord.ToStructure<PEER_RECORD>();
							DisplayChatMessage(rec.pwzCreatorId, Marshal.PtrToStringUni(rec.data.pbData));
						}
					}
					break;

				default:
					break;
			}
		}

		//-----------------------------------------------------------------------------
		// Function: ProcessSendButton
		//
		// Purpose:  Get the text and send it as a chat message.
		//
		// Returns:  nothing
		//
		private void ProcessSendButton()
		{
			if (g_hGroup.IsNull || IDC_TEXTBOX.TextLength == 0 || AddChatRecord(IDC_TEXTBOX.Text).Failed)
			{
				return;
			}

			// Clear the text box and prepare for the next line
			IDC_TEXTBOX.Text = "";
			IDC_TEXTBOX.Focus();
		}

		//-----------------------------------------------------------------------------
		// Function: ProcessStatusChanged
		//
		// Purpose:  Processes the PEER_GROUP_EVENT_STATUS_CHANGED event.
		//
		// Returns:  nothing
		//
		private void ProcessStatusChanged(PEER_GROUP_STATUS dwStatus)
		{
			string pwzStatus = "";
			string wzChatTitle = "Offline";

			if (dwStatus.IsFlagSet(PEER_GROUP_STATUS.PEER_GROUP_STATUS_HAS_CONNECTIONS))
			{
				pwzStatus = "connected";
				if (PeerGroupGetProperties(g_hGroup, out var pProperties).Succeeded)
				{
					var props = pProperties.ToStructure<PEER_GROUP_PROPERTIES>();
					wzChatTitle = $"Chatting in {props.pwzFriendlyName}";
				}
			}
			else if (dwStatus.IsFlagSet(PEER_GROUP_STATUS.PEER_GROUP_STATUS_LISTENING))
			{
				pwzStatus = "listening";
				if (PeerGroupGetProperties(g_hGroup, out var pProperties).Succeeded)
				{
					var props = pProperties.ToStructure<PEER_GROUP_PROPERTIES>();
					wzChatTitle = $"Waiting to chat in {props.pwzFriendlyName}";
				}
			}

			IDC_STATIC_MEMBERS.Text = wzChatTitle;
			SB_PART_STATUS.Text = pwzStatus;

			UpdateParticipantList();
		}

		//-----------------------------------------------------------------------------
		// Function: RemoveParticipant
		//
		// Purpose:  Removes a participant from the member list based on their identity.
		//
		// Returns:  nothing
		//
		private void RemoveParticipant(string pwzIdentity)
		{
			int iItem = FindParticipant(pwzIdentity);
			if (iItem < 0)
			{
				DisplayError("Unable to find participant");
			}
			else
			{
				DeleteParticipant(iItem);
				DisplayChatMessage(pwzIdentity, "has left the group");
			}
		}

		//-----------------------------------------------------------------------------
		// Function: SetupDirectConnection
		//
		// Purpose:  Setup the DirectConnection for the Whisper.
		//
		// Returns:  HRESULT
		//
		private HRESULT SetupDirectConnection(string pwzIdentity)
		{
			HRESULT hr = HRESULT.S_OK;

			GetFriendlyNameForIdentity(g_hGroup, pwzIdentity, out g_wzDCName);

			// Determine the identity of the peer
			using var h = PeerGroupEnumMembers(g_hGroup, PEER_MEMBER_FLAGS.PEER_MEMBER_PRESENT);
			var ppMember = h.FirstOrDefault();

			if (ppMember.cAddresses > 0)
			{
				hr = PeerGroupOpenDirectConnection(g_hGroup, pwzIdentity, ppMember.pAddresses.ToStructure<PEER_ADDRESS>(), out g_ullConnectionId);

				if (hr.Failed)
				{
					if (HRESULT.PEER_E_CONNECT_SELF == hr)
					{
						DisplayHrError("\nCan't whisper to yourself.\n\nPEER_E_CONNECT_SELF", hr);
					}
					else if (HRESULT.E_INVALIDARG == hr)
					{
						DisplayHrError("Can’t open direct connection.", hr);
					}
				}
			}
			return hr;
		}

		//-----------------------------------------------------------------------------
		// Function: UpdateParticipantList
		//
		// Purpose:  Update the list of partipants.
		//
		// Returns:  nothing
		//
		private void UpdateParticipantList()
		{
			ClearParticipantList();
			if (g_hGroup.IsNull)
			{
				return;
			}

			// Retreive only the members currently present in the group.
			using var h = PeerGroupEnumMembers(g_hGroup, PEER_MEMBER_FLAGS.PEER_MEMBER_PRESENT);
			foreach (var ppMember in h.Where(m => m.dwFlags.IsFlagSet(PEER_MEMBER_FLAGS.PEER_MEMBER_PRESENT)))
			{
				AddParticipantName(ppMember.pwzIdentity, ppMember.pCredentialInfo.ToStructure<PEER_CREDENTIAL_INFO>().pwzFriendlyName);
			}
		}
	}
}