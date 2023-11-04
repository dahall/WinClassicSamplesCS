using System.Runtime.InteropServices;
using System.Text;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static DrtSdkSample.CAPIWrappers;
using static Vanara.PInvoke.Crypt32;
using static Vanara.PInvoke.Drt;
using static Vanara.PInvoke.Kernel32;

namespace DrtSdkSample
{
	internal static partial class Program
	{
		private const int KEYSIZE = 32;
		private static readonly Dictionary<int, DRT_CONTEXT> contexts = new();

		private static IntPtr AddCtx(DRT_CONTEXT ctx) { var id = System.Diagnostics.Process.GetCurrentProcess().Id; contexts.Add(id, ctx); return new IntPtr(id); }

		private static DRT_CONTEXT GetCtx(IntPtr ptr) => contexts[ptr.ToInt32()];

		//Global variable to enable the asynchronous display of DRT Events
		private static bool g_DisplayEvents = false;

		//
		// Contains the information required for each DRT Registration
		// 
		private struct REG_CONTEXT
		{
			public HDRT_REGISTRATION_CONTEXT hDrtReg;
			public DRT_REGISTRATION regInfo;
		}

		//
		// Contains the information for a DRT Instance
		//
		private class DRT_CONTEXT
		{
			public SafeHDRT hDrt;
			public SafeEventHandle eventHandle;
			public SafeRegisteredWaitHandle DrtWaitEvent;
			public int BootstrapProviderType;
			public int SecurityProviderType;
			public ushort port;
			public DRT_SETTINGS settings;
			public SafePCCERT_CONTEXT pRoot;
			public SafePCCERT_CONTEXT pLocal;
			public List<REG_CONTEXT> registrations = new();
		}

		//********************************************************************************************
		// Function: GetUserChoice
		//
		// Description: Presents an interactive menu to the user and returns the user's choice
		//
		// Input: string *choices - An array of strings representing the choices to be presented to 
		// the users
		//
		//********************************************************************************************
		private static int GetUserChoice(string[] choices)
		{
			Console.Write("---------------------------------------------------------\n");
			for (var i = 0; i < choices.Length; i++)
			{
				Console.Write(" {0}. {1}\n", i + 1, choices[i]);
			}
			Console.Write("---------------------------------------------------------\n");
			return ReadIntegerFromConsole($"Enter a choice (1-{choices.Length}): ", 1, choices.Length, "Invalid Choice");
		}

		private static int ReadIntegerFromConsole(string prompt, int min = 0, int max = int.MaxValue, string outOfRangeMsg = null)
		{
			if (prompt != null) Console.Write(prompt);
			if (ReadLong(out var l, 1) && l >= min && l <= max)
				return (int)l;
			if (outOfRangeMsg != null) Console.WriteLine(outOfRangeMsg);
			return -1;
		}

		//********************************************************************************************
		// Function: DisplayError
		//
		// Description: Maps common HRESULT s to descriptive error strings
		//
		//********************************************************************************************
		private static void DisplayError(HRESULT hr, [System.Runtime.CompilerServices.CallerMemberName] string fnname = "")
		{
			if (hr == HRESULT.S_OK)
				return;
			Console.Write("{0} ", fnname);
			Console.Write("{0}\n", hr);
		}

		//********************************************************************************************
		// Function: DrtEventCallback
		//
		// Description: Callback to handle general DRT Events. 
		// These include registration state changes, leafset changes, and status changes.
		//
		//********************************************************************************************
		private static void DrtEventCallback(IntPtr Param, bool TimedOut)
		{
			HRESULT hr;
			var Drt = GetCtx(Param);

			hr = DrtGetEventDataSize(Drt.hDrt, out var ulDrtEventDataLen);
			if (hr.Failed)
			{
				if (hr != HRESULT.DRT_E_NO_MORE)
					Console.Write(" DrtGetEventDataSize failed: {0}\n", hr);
				goto Cleanup;
			}

			using (var pEventData = new SafeCoTaskMemStruct<DRT_EVENT_DATA>(ulDrtEventDataLen))
			{
				if (pEventData.IsInvalid)
				{
					Console.Write(" Out of memory\n");
					goto Cleanup;
				}

				hr = DrtGetEventData(Drt.hDrt, ulDrtEventDataLen, pEventData);
				if (hr.Failed)
				{
					if (hr != HRESULT.DRT_E_NO_MORE)
						Console.Write(" DrtGetEventData failed: {0}\n", hr);
					goto Cleanup;
				}

				switch (pEventData.Value.type)
				{
					case DRT_EVENT_TYPE.DRT_EVENT_STATUS_CHANGED:
						switch (pEventData.Value.union.statusChange.status)
						{
							case DRT_STATUS.DRT_ACTIVE:
								SetConsoleTitle("DrtSdkSample Current Drt Status: Active");
								if (g_DisplayEvents)
									Console.Write(" DRT Status Changed to Active\n");
								break;
							case DRT_STATUS.DRT_ALONE:
								SetConsoleTitle("DrtSdkSample Current Drt Status: Alone");
								if (g_DisplayEvents)
									Console.Write(" DRT Status Changed to Alone\n");
								break;
							case DRT_STATUS.DRT_NO_NETWORK:
								SetConsoleTitle("DrtSdkSample Current Drt Status: No Network");
								if (g_DisplayEvents)
									Console.Write(" DRT Status Changed to No Network\n");
								break;
							case DRT_STATUS.DRT_FAULTED:
								SetConsoleTitle("DrtSdkSample Current Drt Status: Faulted");
								if (g_DisplayEvents)
									Console.Write(" DRT Status Changed to Faulted\n");
								break;
						}

						break;
					case DRT_EVENT_TYPE.DRT_EVENT_LEAFSET_KEY_CHANGED:
						if (g_DisplayEvents)
						{
							switch (pEventData.Value.union.leafsetKeyChange.change)
							{
								case DRT_LEAFSET_KEY_CHANGE_TYPE.DRT_LEAFSET_KEY_ADDED:
									Console.Write(" Leafset Key Added Event: {0}\n", pEventData.Value.hr);
									break;
								case DRT_LEAFSET_KEY_CHANGE_TYPE.DRT_LEAFSET_KEY_DELETED:
									Console.Write(" Leafset Key Deleted Event: {0}\n", pEventData.Value.hr);
									break;
							}
						}

						break;
					case DRT_EVENT_TYPE.DRT_EVENT_REGISTRATION_STATE_CHANGED:
						if (g_DisplayEvents)
							Console.Write(" Registration State Changed Event: [hr: 0x%x, registration state: %i]\n", pEventData.Value.hr, pEventData.Value.union.registrationStateChange.state);
						break;
				}
			}
			Cleanup:
			return;
		}


		//********************************************************************************************
		// Function: InitializeDrt
		//
		// Description: Initializes and brings a DRT instance online
		// 1) Brings up an ipv6 transport layer
		// 2) Attaches a security provider (according to user's choice)
		// 3) Attaches a bootstrap provider (according to user's choice)
		// 4) Calls DrtOpen to bring the DRT instance online
		//
		//********************************************************************************************
		private static bool InitializeDrt(DRT_CONTEXT Drt)
		{
			string pwszCompName = default;
			string pwszBootstrapHostname = null;

			//
			// Initialize DrtSettings
			//
			Drt.port = 0;
			Drt.settings.pwzDrtInstancePrefix = "Local_DRT";
			Drt.settings.dwSize = (uint)Marshal.SizeOf<DRT_SETTINGS>();
			Drt.settings.cbKey = 32; // KEYSIZE
			Drt.settings.ulMaxRoutingAddresses = 4;
			Drt.settings.bProtocolMajorVersion = 0x6;
			Drt.settings.bProtocolMinorVersion = 0x65;
			Drt.settings.eSecurityMode = DRT_SECURITY_MODE.DRT_SECURE_CONFIDENTIALPAYLOAD;
			Drt.settings.hTransport = default;
			Drt.settings.pSecurityProvider = default;
			Drt.settings.pBootstrapProvider = default;
			Drt.hDrt = default;

			//
			// *Transport*
			//

			var hr = DrtCreateIpv6UdpTransport(DRT_SCOPE.DRT_GLOBAL_SCOPE, 0, 300, ref Drt.port, out var hTransport);
			if (hr.Failed) { DisplayError(hr, "DrtCreateTransport"); goto Cleanup; }
			Drt.settings.hTransport = hTransport.ReleaseOwnership();

			//
			// *Security Provider*
			//

			if (Drt.SecurityProviderType == 0) //Null Security Provider
			{
				hr = DrtCreateNullSecurityProvider(out Drt.settings.pSecurityProvider);
			}
			else if (Drt.SecurityProviderType == 1) //Derived Key Security Provider
			{
				hr = ReadCertFromFile("RootCertificate.cer", out Drt.pRoot, out _);
				if (hr.Failed)
				{
					Console.Write("No RootCertificate.cer file found in the current directory, Creating a new root certificate.\n");
					hr = MakeCert("RootCertificate.cer", "RootCert", default, default);
					if (hr.Failed) { DisplayError(hr, "MakeCert"); goto Cleanup; }
					hr = ReadCertFromFile("RootCertificate.cer", out Drt.pRoot, out _);
					if (hr.Failed) { DisplayError(hr, "ReadCertFromFile"); goto Cleanup; }
				}

				// We now have a root cert, read an existing local cert or create one based on root cert
				hr = ReadCertFromFile("LocalCertificate.cer", out Drt.pLocal, out _);
				if (hr.Failed)
				{
					Console.Write("No LocalCertificate.cer file found in the current directory, Creating a new local certificate.\n");
					hr = MakeCert("LocalCertificate.cer", "LocalCert", "RootCertificate.cer", "RootCert");
					if (hr.Failed) { DisplayError(hr, "MakeCert"); goto Cleanup; }
					hr = ReadCertFromFile("LocalCertificate.cer", out Drt.pLocal, out _);
					if (hr.Failed) { DisplayError(hr, "ReadCertFromFile"); goto Cleanup; }
				}
				hr = DrtCreateDerivedKeySecurityProvider(Drt.pRoot, Drt.pLocal, out Drt.settings.pSecurityProvider);
			}
			else if (Drt.SecurityProviderType == 2) //Custom Security Provider
			{
				Drt.settings.pSecurityProvider = MakeGCPtr(new CCustomNullSecurityProvider().provider);
			}
			else
			{
				Console.Write("Invalid Security Provider passed to InitializeDrt");
				hr = HRESULT.E_FAIL;
			}
			if (hr.Failed) { DisplayError(hr, "DrtCreateSecurityProvider"); goto Cleanup; }

			//
			// *Bootstrap Provider*
			//

			if (Drt.BootstrapProviderType == 0) //DNS Bootstrap Provider
			{
				GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNameDnsFullyQualified, out pwszCompName);

				Console.Write("Enter 'hostname port' for DNS Bootstrap Provider (currently {0} {1}):\n", pwszCompName, Drt.port);
				var rl = Console.ReadLine();
				var parts = rl.Split(' ');
				if (parts.Length != 2 || !ushort.TryParse(parts[1], out var usBootstrapPort))
				{
					hr = HRESULT.E_INVALIDARG;
					Console.Write("Invalid hostname:port\n");
					goto Cleanup;
				}
				pwszBootstrapHostname = parts[0];
				Console.Write("DNS Bootstrapping from: {0}:{1}\n", pwszBootstrapHostname, usBootstrapPort);

				hr = DrtCreateDnsBootstrapResolver(usBootstrapPort, pwszBootstrapHostname, out Drt.settings.pBootstrapProvider);
			}
			else if (Drt.BootstrapProviderType == 1) //PNRP Bootstrap Provider
			{
				Console.Write("Enter a PNRP name for PNRP Bootstrap Provider (IE 0.DemoName)\n");
				pwszBootstrapHostname = Console.ReadLine();
				if (pwszBootstrapHostname.Length > 1024)
				{
					Console.Write("Invalid PNRP name\n");
					goto Cleanup;
				}
				Console.Write("PNRP Bootstrapping from: {0}\n", pwszBootstrapHostname);
				hr = DrtCreatePnrpBootstrapResolver(true, pwszBootstrapHostname, "Global_", default, out Drt.settings.pBootstrapProvider);
			}
			else if (Drt.BootstrapProviderType == 2) //Custom Bootstrap Provider
			{
				GetComputerNameEx(COMPUTER_NAME_FORMAT.ComputerNameDnsFullyQualified, out pwszCompName);

				Console.Write("Enter 'hostname port' for Custom Bootstrap Provider (currently {0} {1}\n", pwszCompName, Drt.port);
				var rl = Console.ReadLine();
				var parts = rl.Split(' ');
				if (parts.Length != 2 || !ushort.TryParse(parts[1], out var usBootstrapPort))
				{
					hr = HRESULT.E_INVALIDARG;
					Console.Write("Invalid hostname:port\n");
					goto Cleanup;
				}
				pwszBootstrapHostname = parts[0];
				Console.Write("Custom Bootstrapping from: {0}:{1}\n", pwszBootstrapHostname, usBootstrapPort);

				var bs = new CustomDnsBootStrapper();
				hr = bs.Init(usBootstrapPort, pwszBootstrapHostname, out var bsProv);
				if (hr.Succeeded)
					Drt.settings.pBootstrapProvider = MakeGCPtr(bsProv);
			}
			else
			{
				Console.Write("Invalid Bootstrap Provider passed to InitializeDrt");
				hr = HRESULT.E_FAIL;
			}
			if (hr.Failed) { DisplayError(hr, "DrtCreateBootstrapResolver"); goto Cleanup; }

			//
			// *Make sure the Windows Firewall is open*
			// Also open port 3540 (used by PNRP, if the PNRP bootstrap provider is chosen)
			//

			if (Drt.BootstrapProviderType == 1)
				hr = FirewallConfig.OpenFirewallForDrtSdkSample(true);
			else
				hr = FirewallConfig.OpenFirewallForDrtSdkSample(false);

			if (hr.Failed) { DisplayError(hr, "OpenFirewallForDrtSdkSample"); goto Cleanup; }

			//
			// Open the DRT
			//

			Drt.eventHandle = CreateEvent(default, false, false, default);
			if (default == Drt.eventHandle)
			{
				hr = HRESULT.E_OUTOFMEMORY;
				goto Cleanup;
			}

			hr = DrtOpen(Drt.settings, Drt.eventHandle, default, out Drt.hDrt);
			if (hr.Failed) { DisplayError(hr, "DrtOpen"); goto Cleanup; }

			//
			// Register a callback to handle DRT Events
			//
			RegisterWaitForSingleObject(out Drt.DrtWaitEvent, Drt.eventHandle, DrtEventCallback, AddCtx(Drt), INFINITE, WT.WT_EXECUTEDEFAULT);

			Cleanup:
			return hr.Succeeded;
		}


		//********************************************************************************************
		// Function: GetKeyFromUser
		//
		// Description: Gets a registration key from the user (used for registration and search)
		//
		//********************************************************************************************
		private static bool GetKeyFromUser(string pcwszKeyName, SafeDRT_DATA KeyData)
		{
			Console.Write("Enter {0} as a string of hex digits, Example: 01 ff 0a b8 80 z\n", pcwszKeyName);
			Console.Write("The current keysize is {0} bytes. Enter z as the last digit and the remainder of the key will be zero-filled (Most significant byte is first)\n", KEYSIZE);
			int i;
			for (i = KEYSIZE - 1; i >= 0; i--)
			{
				if (!ReadHex(out var hexdigit, 2))
					break;
				KeyData[i] = (byte)hexdigit;
			}
			for (; i >= 0; i--)
				KeyData[i] = 0;
			Console.Write("Resulting {0}:\n", pcwszKeyName);
			for (i = KEYSIZE - 1; i >= 0; i--)
				Console.Write("{0:X2} ", KeyData[i]);
			Console.Write("\n");

			return true;
		}

		private static bool ReadHex(out long value, int maxLen = 16) => ReadLong(out value, maxLen, System.Globalization.NumberStyles.HexNumber, "0123456789ABCDEFabcdef");

		private static bool ReadLong(out long value, int maxLen = 29, System.Globalization.NumberStyles styles = System.Globalization.NumberStyles.Integer, string validChars = null)
		{
			if (validChars is null) validChars = StdNumChars();
			ConsoleKeyInfo ki;
			var sb = new StringBuilder(maxLen);
			value = 0;
			var len = 0;
			while (validChars.Contains((ki = Console.ReadKey()).KeyChar) && ++len <= maxLen)
				sb.Append(ki.KeyChar);
			return (ki.Key == ConsoleKey.Enter || len == maxLen) && long.TryParse(sb.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier, null, out value);

			static string StdNumChars()
			{
				var nf = System.Globalization.NumberFormatInfo.CurrentInfo;
				return string.Join("", nf.NativeDigits) + nf.NegativeSign + nf.NumberDecimalSeparator + nf.NumberGroupSeparator;
			}
		}

		//********************************************************************************************
		// Function: PerformDrtSearch
		//
		// Description: Initializes and performs a search through the DRT
		//
		//********************************************************************************************
		private static unsafe bool PerformDrtSearch(in DRT_CONTEXT Drt, int SearchType)
		{
			HRESULT hr = HRESULT.S_OK;
			uint dwSize = 1024;
			bool fKeyFound = false;
			HDRT_SEARCH_CONTEXT SearchContext = default;
			using SafeCoTaskMemStruct<DRT_SEARCH_RESULT> pSearchResult = new();
			using SafeDRT_DATA searchKey = new(KEYSIZE);
			using SafeDRT_DATA minKey = new(KEYSIZE);
			using SafeDRT_DATA maxKey = new(KEYSIZE);

			//Create a manual reset event 
			//The DRT will reset the event when the search result buffer has been consumed
			using var hDrtSearchEvent = CreateEvent(default, true, false, default);
			if (hDrtSearchEvent.IsInvalid)
			{
				Console.Write("Out of memory\n");
				goto Cleanup;
			}


			if (!GetKeyFromUser("Search Key", searchKey))
				goto Cleanup;

			if (SearchType == 2) //Simple DRT Search
			{
				hr = DrtStartSearch(Drt.hDrt, (DRT_DATA)searchKey, IntPtr.Zero, 5000, hDrtSearchEvent, default, out SearchContext);
			}
			else
			{
				//Set Some Defaults for SearchInfo
				var SearchInfo = new DRT_SEARCH_INFO
				{
					dwSize = (uint)Marshal.SizeOf<DRT_SEARCH_INFO>(),
					fIterative = false,
					fAllowCurrentInstanceMatch = true,
					fAnyMatchInRange = false,
					cMaxEndpoints = 1,
					pMinimumKey = minKey,
					pMaximumKey = maxKey
				};
				if (SearchType == 3) //Nearest Match Search
				{
					SearchInfo.fAnyMatchInRange = false;
					minKey.Fill(0, KEYSIZE);
					maxKey.Fill(0xFF, KEYSIZE);
				}
				else if (SearchType == 4) //Iterative Search
				{
					SearchInfo.fIterative = true;
					searchKey.AsSpan<byte>(searchKey.Size).CopyTo(minKey.AsSpan<byte>(minKey.Size));
				}
				else if (SearchType == 5) //Range Search
				{
					SearchInfo.fAnyMatchInRange = true;
					if (!GetKeyFromUser("Min Search Key (01 z)", minKey))
						goto Cleanup;
					if (!GetKeyFromUser("Max Search Key (ff z)", maxKey))
						goto Cleanup;
				}
				else
				{
					Console.Write("Invalid Search Type passed to DrtPerformSearch");
					goto Cleanup;
				}

				hr = DrtStartSearch(Drt.hDrt, (DRT_DATA)searchKey, SearchInfo, 5000, hDrtSearchEvent, default, out SearchContext);
			}
			if (hr.Failed) { DisplayError(hr, "DrtStartSearch"); goto Cleanup; }

			do
			{
				var dwRes = WaitForSingleObject(hDrtSearchEvent, 30 * 1000);

				if (dwRes == WAIT_STATUS.WAIT_OBJECT_0)
				{
					hr = DrtGetSearchResultSize(SearchContext, out dwSize);
					if (hr != HRESULT.S_OK)
					{
						continue;
					}
					pSearchResult.Size = dwSize;
					hr = DrtGetSearchResult(SearchContext, dwSize, pSearchResult);
					if (hr != HRESULT.S_OK)
					{
						continue;
					}
					if (pSearchResult.AsRef().type == DRT_MATCH_TYPE.DRT_MATCH_EXACT)
					{
						fKeyFound = true;
						Console.WriteLine("*Found Key*: ");
						Console.Write(((byte[])pSearchResult.AsRef().registration.key).ToHexDumpString());
						PrintSearchPath(SearchContext);
					}
					else if (pSearchResult.AsRef().type == DRT_MATCH_TYPE.DRT_MATCH_NEAR)
					{
						Console.WriteLine("*Found Near Match*: ");
						Console.Write(((byte[])pSearchResult.AsRef().registration.key).ToHexDumpString());
						if (SearchType == 3)
							fKeyFound = true;
						PrintSearchPath(SearchContext);
					}
					else if (pSearchResult.AsRef().type == DRT_MATCH_TYPE.DRT_MATCH_INTERMEDIATE)
					{
						Console.WriteLine("Intermediate Match: ");
						Console.Write(((byte[])pSearchResult.AsRef().registration.key).ToHexDumpString());
						DrtContinueSearch(SearchContext);
					}
				}
				else
				{
					Console.Write("Drt Search Timed out\n");
					break;
				}
			} while ((hr == HRESULT.DRT_E_SEARCH_IN_PROGRESS) || (hr == HRESULT.S_OK));
			DrtEndSearch(SearchContext);

			//
			// When the search is finished, the HRESULT should be DRT_E_NO_MORE
			//
			if (hr != HRESULT.DRT_E_NO_MORE)
			{
				Console.Write("Unexpected HRESULT from DrtGetSearchResult: 0x%x\n", hr);
			}

			if (!fKeyFound)
				Console.Write("Could not find key\n");

			Cleanup:

			return true;
		}

		//
		// Prints the search path that corresponds to the latest resolve result
		//
		private static void PrintSearchPath(HDRT_SEARCH_CONTEXT SearchContext)
		{
			var hr = DrtGetSearchPathSize(SearchContext, out var ulSearchPathLen);
			if (hr.Failed)
				return;

			using var pSearchPath = new SafeCoTaskMemStruct<DRT_ADDRESS_LIST>(ulSearchPathLen);

			hr = DrtGetSearchPath(SearchContext, ulSearchPathLen, pSearchPath);
			if (hr.Failed)
				return;

			Console.Write("Search Path:\n");
			for (uint i = 0; i < pSearchPath.Value.AddressCount; i++)
			{
				var addr = pSearchPath.AsRef().AddressList[i].socketAddress;
				Console.Write("Port: {0} Flags: {1}, Nearness: {2} Latency: {3}\n",
					((Ws2_32.SOCKADDR_IN6)addr).sin6_port,
					pSearchPath.AsRef().AddressList[i].flags,
					pSearchPath.AsRef().AddressList[i].nearness,
					pSearchPath.AsRef().AddressList[i].latency);
			}
		}

		//********************************************************************************************
		// Function: RegisterKey
		//
		// Description: Registers a key in the current DRT Instance
		//
		//********************************************************************************************
		private static bool RegisterKey(in DRT_CONTEXT Drt)
		{
			HRESULT hr = HRESULT.S_OK;
			using var newKey = new SafeDRT_DATA(KEYSIZE);
			using var newPayload = new SafeDRT_DATA(KEYSIZE);
			SafePCCERT_CONTEXT pRegCertContext = default;

			if (Drt.SecurityProviderType == 1) // Derived Key Security Provider
			{
				Console.Write("Generating a new certificate for the new registration...\n");
				hr = MakeCert("LastRegisteredCert.cer", "LocalCert", "RootCertificate.cer", "RootCert");
				if (hr.Failed) { DisplayError(hr, "MakeCert"); goto Cleanup; }
				Console.Write("Creating a new key based on the generated certificate...\n");
				hr = ReadCertFromFile("LastRegisteredCert.cer", out pRegCertContext, out _);
				if (hr.Failed) { DisplayError(hr, "ReadCertFromFile"); goto Cleanup; }
				hr = DrtCreateDerivedKey(pRegCertContext, newKey);
				if (hr.Failed) { DisplayError(hr, "DrtCreateDerivedKey"); goto Cleanup; }
			}
			else
			{
				if (!GetKeyFromUser("Registration Key", newKey))
					goto Cleanup;
			}

			REG_CONTEXT reg = new();
			reg.regInfo.appData = (DRT_DATA)newPayload;
			reg.regInfo.key = (DRT_DATA)newKey;

			hr = DrtRegisterKey(Drt.hDrt, reg.regInfo, pRegCertContext.DangerousGetHandle(), out reg.hDrtReg);
			if (hr.Failed) { DisplayError(hr, "DrtRegisterKey"); goto Cleanup; }

			if (hr.Succeeded)
			{
				Drt.registrations.Add(reg);

				// newKeyData and newPayloadData will be freed on unregister
				newKey.TakeOwnership();
				newPayload.TakeOwnership();

				Console.WriteLine("Successfully Registered: ");
				Console.Write(((byte[])reg.regInfo.key).ToHexDumpString());
			}

		Cleanup:

			pRegCertContext?.Dispose();
			return true;
		}


		//********************************************************************************************
		// Function: UnRegisterKey
		//
		// Description: Unregisters a previously registered key
		//
		//********************************************************************************************
		private static bool UnRegisterKey(in DRT_CONTEXT Drt)
		{
			Console.Write("Current Registrations:\n");
			var choice = GetUserChoice(Drt.registrations.Select(r => ((byte[])r.regInfo.key).ToHexDumpString(KEYSIZE, KEYSIZE, 0)).Append("Cancel").ToArray()) - 1;
			if (choice < 0 || choice == Drt.registrations.Count)
				goto Cleanup;

			Console.WriteLine("Unregistering key: ");
			Console.Write(((byte[])Drt.registrations[choice].regInfo.key).ToHexDumpString(KEYSIZE, KEYSIZE, 0));

			DrtUnregisterKey(Drt.registrations[choice].hDrtReg);

			Marshal.FreeHGlobal(Drt.registrations[choice].regInfo.key.pb);
			Marshal.FreeHGlobal(Drt.registrations[choice].regInfo.appData.pb);
			Drt.registrations.RemoveAt(choice);

			Cleanup:
			return true;
		}


		//********************************************************************************************
		// Function: CleanupDrt
		//
		// Description: Deletes and Frees the various objects and providers used by the DRT
		//
		//********************************************************************************************
		private static void CleanupDrt(DRT_CONTEXT Drt)
		{
			foreach (var reg in Drt.registrations)
			{
				DrtUnregisterKey(reg.hDrtReg);

				Marshal.FreeHGlobal(reg.regInfo.key.pb);
				Marshal.FreeHGlobal(reg.regInfo.appData.pb);
			}
			Drt.registrations.Clear();

			Drt.DrtWaitEvent?.Dispose();
			Drt.hDrt?.Dispose();
			Drt.eventHandle?.Dispose();

			if (Drt.settings.pBootstrapProvider != default)
			{
				if (Drt.BootstrapProviderType == 0)
					DrtDeleteDnsBootstrapResolver(Drt.settings.pBootstrapProvider);
				else if (Drt.BootstrapProviderType == 1)
					DrtDeletePnrpBootstrapResolver(Drt.settings.pBootstrapProvider);
				else if (Drt.BootstrapProviderType == 2)
					DrtDeleteCustomBootstrapResolver(Drt.settings.pBootstrapProvider);
			}

			if (Drt.settings.pSecurityProvider != default)
			{
				if (Drt.SecurityProviderType == 0)
					DrtDeleteNullSecurityProvider(Drt.settings.pSecurityProvider);
				else if (Drt.SecurityProviderType == 1)
					DrtDeleteDerivedKeySecurityProvider(Drt.settings.pSecurityProvider);
				else if (Drt.SecurityProviderType == 2)
					DrtDeleteCustomSecurityProvider(Drt.settings.pSecurityProvider);
			}

			Drt.pRoot?.Dispose();
			Drt.pLocal?.Dispose();
		}

		private static void DrtDeleteCustomSecurityProvider(IntPtr pProvider)
		{
			if (pProvider == IntPtr.Zero)
				return;

			//DRT_SECURITY_PROVIDER pSecProvider = GetGCObject<DRT_SECURITY_PROVIDER>(pProvider);
			FreeGCPtr(pProvider);
		}

		private static void DrtDeleteCustomBootstrapResolver(IntPtr pResolver)
		{
			CustomDnsBootStrapper pBootStrapper = GetGCObject<CustomDnsBootStrapper>(pResolver);
			pBootStrapper.Release();
			FreeGCPtr(pResolver);
		}

		//********************************************************************************************
		// Function: Main
		//
		// Description: The main function, initializes a DRT according to user specifications and
		// loops allowing the user to interact with the DRT.
		//
		//********************************************************************************************
		private static int Main()
		{
			int nSearchType;

			DRT_CONTEXT LocalDrt = default;

			string[] ppcwzSecurityProviderChoices = {
				"Initialize DRT with Null Security Provider",
				"Initialize DRT with Derived Key Security Provider",
				"Initialize DRT with Custom Security Provider",
				"Exit" };

			string[] ppcwzBootStrapProviderChoices = {
				"Bootstrap with Builtin DNS Bootstrap Provider",
				"Bootstrap with Builtin PNRP Bootstrap Provider",
				"Bootstrap with Custom Bootstrap Provider",
				"Exit" };

			string[] ppcwzSearchChoices = {
				"Register a new key",
				"Unregister a key",
				"Simple DRT Search",
				"Nearest Match Search",
				"Iterative Search",
				"Range Search",
				"Hide DRT Events",
				"Display DRT Events",
				"Exit" };

			SetConsoleTitle("DrtSdkSample Current Drt Status: Initializing");

			using var ws = Ws2_32.SafeWSA.Initialize();
			LocalDrt.SecurityProviderType = GetUserChoice(ppcwzSecurityProviderChoices);
			if (LocalDrt.SecurityProviderType == -1 || LocalDrt.SecurityProviderType == 3)
				goto Cleanup;

			LocalDrt.BootstrapProviderType = GetUserChoice(ppcwzBootStrapProviderChoices);
			if (LocalDrt.BootstrapProviderType == -1 || LocalDrt.BootstrapProviderType == 3)
				goto Cleanup;

			if (!InitializeDrt(LocalDrt))
				goto Cleanup;

			Console.Write("DRT Initialization Complete\n");

			SetConsoleTitle("DrtSdkSample Current Drt Status: Bootstrapping");

			for (; ; )
			{
				nSearchType = GetUserChoice(ppcwzSearchChoices);
				if (nSearchType == 0)
				{
					if (!RegisterKey(LocalDrt))
						goto Cleanup;
				}
				else if (nSearchType == 1)
				{
					if (!UnRegisterKey(LocalDrt))
						goto Cleanup;
				}
				else if (1 < nSearchType && nSearchType < 6)
				{
					if (!PerformDrtSearch(LocalDrt, nSearchType))
						goto Cleanup;
				}
				else if (nSearchType == 6)
				{
					Console.Write("DRT Events will not be displayed\n");
					g_DisplayEvents = false;
				}
				else if (nSearchType == 7)
				{
					Console.Write("DRT Events will be displayed asynchronously\n");
					g_DisplayEvents = true;
				}
				else if (nSearchType == 8)
				{
					goto Cleanup;
				}
			}

			Cleanup:
			CleanupDrt(LocalDrt);
			return 0;
		}

		internal static void FreeGCPtr(IntPtr ptr) { try { GCHandle.FromIntPtr(ptr).Free(); } catch { } }

		internal static T GetGCObject<T>(IntPtr ptr) { try { return (T)GCHandle.FromIntPtr(ptr).Target; } catch { return default; } }

		internal static IntPtr MakeGCPtr(object obj) => GCHandle.Alloc(obj).AddrOfPinnedObject();
	}
}