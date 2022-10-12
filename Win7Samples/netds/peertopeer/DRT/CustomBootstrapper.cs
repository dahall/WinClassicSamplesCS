using System;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.Extensions;
using Vanara.InteropServices;
using Vanara.PInvoke;
using static Vanara.PInvoke.Drt;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Ws2_32;

namespace DrtSdkSample
{
	internal class CBootStrapResolveContext : IDisposable
	{
		private uint m_CallbackThreadId;
		private uint m_dwMaxResults;
		private uint m_dwTimeout;
		private bool m_fEndResolve;
		private bool m_fResolveInProgress;
		private bool m_fSplitDetect;
		private SafeEventHandle m_hCallbackComplete;
		private CRITICAL_SECTION m_Lock;
		private bool m_LockCreated;
		private int m_lRefCount;

		public CBootStrapResolveContext()
		{
		}

		public void Dispose()
		{
			m_hCallbackComplete?.Dispose();
			m_hCallbackComplete = default;

			if (m_LockCreated)
			{
				DeleteCriticalSection(ref m_Lock);
				m_LockCreated = false;
			}
		}

		public HRESULT Init(bool fSplitDetect, uint dwTimeout, uint dwMaxResults)
		{
			HRESULT hr = HRESULT.S_OK;

			m_LockCreated = InitializeCriticalSectionAndSpinCount(out m_Lock, 0x80001000);
			if (!m_LockCreated)
			{
				hr = HRESULT.E_OUTOFMEMORY;
			}

			m_fSplitDetect = fSplitDetect;
			m_dwTimeout = dwTimeout;
			m_dwMaxResults = dwMaxResults;

			return hr;
		}

		internal void AddRef()
		{
			if (m_lRefCount == 0)
				GC.SuppressFinalize(this);
			InterlockedIncrement(ref m_lRefCount);
		}

		internal void EndResolve()
		{
			var fWaitForCallback = false;

			var CallbackComplete = CreateEvent(default, true, false, default);

			EnterCriticalSection(ref m_Lock);
			if (m_fResolveInProgress && (GetCurrentThreadId() != m_CallbackThreadId))
			{
				if (m_fEndResolve == false)
				{
					// This is the first thread to call EndResolve and we need to wait for a callback to complete so initialize the class
					// member event
					m_fEndResolve = true;
					m_hCallbackComplete = CallbackComplete;
				}
				fWaitForCallback = true;
			}
			LeaveCriticalSection(ref m_Lock);

			if (!CallbackComplete.IsInvalid && (m_hCallbackComplete != CallbackComplete))
			{
				// This thread was not the first to call EndResolve, so its event is not in use, release it (m_hCallbackComplete is released
				// in the destructor)
				CallbackComplete.Dispose();
			}

			if (fWaitForCallback && m_hCallbackComplete != null)
			{
				WaitForSingleObject(m_hCallbackComplete, INFINITE);
			}
		}

		internal HRESULT IssueResolve([In, Optional] IntPtr pvCallbackContext, DRT_BOOTSTRAP_RESOLVE_CALLBACK callback, string szPortString, string address)
		{
			HRESULT hr = HRESULT.S_OK;
			EnterCriticalSection(ref m_Lock);
			m_fResolveInProgress = true;
			m_CallbackThreadId = GetCurrentThreadId();
			LeaveCriticalSection(ref m_Lock);

			if (m_dwMaxResults > 0)
			{
				var addresses = address.Split(new[] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
				foreach (var CurrentAddress in addresses)
				{
					if (m_fEndResolve)
					{
						goto exit;
					}

					// Retrieve bootstrap possibilities
					var addrInf = new ADDRINFOW
					{
						ai_flags = ADDRINFO_FLAGS.AI_CANONNAME,
						ai_family = ADDRESS_FAMILY.AF_UNSPEC,
						ai_socktype = SOCK.SOCK_STREAM
					};

					var nStat = GetAddrInfoW(CurrentAddress, szPortString, addrInf, out var results);
					if (nStat.Succeeded)
					{
						using (results)
						{
							var cbSA6 = Marshal.SizeOf<SOCKADDR_IN6>();
							using var psockAddrs = new SafeNativeArray<SOCKADDR_IN6>(results.Select(a => { using var ar = a.addr; return (SOCKADDR_IN6)ar; }).ToArray());
							var Addresses = new SOCKET_ADDRESS_LIST
							{
								iAddressCount = psockAddrs.Count,
								Address = psockAddrs.Select((a, i) => new SOCKET_ADDRESS { iSockaddrLength = cbSA6, lpSockaddr = ((IntPtr)psockAddrs).Offset(cbSA6) }).ToArray()
							};

							// Call the callback to signal completion
							using var pAddresses = Addresses.Pack();
							callback?.Invoke(hr, pvCallbackContext, pAddresses, false);
						}
					}
					else
					{
						// GetAddrInfoW Failed but there may be more addresses in the string so keep going otherwise we return
						// HRESULT.E_NO_MORE and retry next cycle
					}
				}
			}

			// Tell the drt there will be no more results
			callback?.Invoke(HRESULT.DRT_E_NO_MORE, pvCallbackContext, default, false);

			exit:
			EnterCriticalSection(ref m_Lock);
			if (m_hCallbackComplete != null && !m_hCallbackComplete.IsInvalid)
			{
				// Notify EndResolve that callbacks have completed
				m_hCallbackComplete.Set();
			}
			m_fResolveInProgress = false;
			LeaveCriticalSection(ref m_Lock);

			return hr;
		}

		internal void Release()
		{
			if (InterlockedDecrement(ref m_lRefCount) == 0)
				GC.ReRegisterForFinalize(this);
		}
	}

	internal class CustomDnsBootStrapper
	{
		private const int DNS_ADDRESS_QUERY = 20;

		private string m_Address;
		private DRT_BOOTSTRAP_PROVIDER m_BootStrapModule;
		private int m_lAttachCount, m_lRefCount;
		private ushort m_Port;
		private string m_szPortString;

		public CustomDnsBootStrapper()
		{
		}

		public HRESULT Init(ushort port, string pwszAddress, out DRT_BOOTSTRAP_PROVIDER ppModule)
		{
			ppModule = default;

			if (pwszAddress is null)
				return HRESULT.E_INVALIDARG;

			m_Address = pwszAddress;
			m_Port = port;
			m_szPortString = m_Port.ToString();
			m_BootStrapModule = new DRT_BOOTSTRAP_PROVIDER
			{
				Attach = Attach,
				Detach = Detach,
				InitResolve = InitResolve,
				IssueResolve = IssueResolve,
				EndResolve = EndResolve,
				Register = Register,
				Unregister = Unregister,
				pvContext = MakeGCPtr(this)
			};
			ppModule = m_BootStrapModule;
			AddRef();

			return HRESULT.S_OK;
		}

		private static void FreeGCPtr(IntPtr ptr) => Program.FreeGCPtr(ptr);

		private static T GetGCObject<T>(IntPtr ptr) => Program.GetGCObject<T>(ptr);

		private static IntPtr MakeGCPtr(object obj) => Program.MakeGCPtr(obj);

		private void AddRef()
		{
			if (m_lRefCount == 0)
				GC.SuppressFinalize(this);
			InterlockedIncrement(ref m_lRefCount);
		}

		private HRESULT Attach(IntPtr pvContext)
		{
			var pBootStrapper = GetGCObject<CustomDnsBootStrapper>(pvContext);
			if (InterlockedCompareExchange(ref pBootStrapper.m_lAttachCount, 1, 0) != 0)
				return HRESULT.DRT_E_BOOTSTRAPPROVIDER_IN_USE;
			pBootStrapper.AddRef();

			return HRESULT.S_OK;
		}

		private void Detach(IntPtr pvContext)
		{
			var pBootStrapper = GetGCObject<CustomDnsBootStrapper>(pvContext);
			InterlockedCompareExchange(ref pBootStrapper.m_lAttachCount, 0, 1);
			pBootStrapper.Release();
		}

		private void EndResolve(IntPtr pvContext, DRT_BOOTSTRAP_RESOLVE_CONTEXT ResolveContext)
		{
			var pResolveContext = GetGCObject<CBootStrapResolveContext>((IntPtr)ResolveContext);
			var pBootStrapper = GetGCObject<CustomDnsBootStrapper>(pvContext);
			pResolveContext.EndResolve();
			pResolveContext.Release();
			FreeGCPtr((IntPtr)ResolveContext);
			pBootStrapper.Release();

			return;
		}

		private HRESULT InitResolve(IntPtr pvContext, bool fSplitDetect, uint dwTimeout, uint cMaxResults, out DRT_BOOTSTRAP_RESOLVE_CONTEXT pResolveContext, out bool fFatalError)
		{
			fFatalError = false;
			pResolveContext = default;
			var pBootStrapper = GetGCObject<CustomDnsBootStrapper>(pvContext);

			var hr = HRESULT.DRT_E_BOOTSTRAPPROVIDER_NOT_ATTACHED;
			if (pBootStrapper.m_lAttachCount != 0)
			{
				var pBSResolveContext = new CBootStrapResolveContext();

				// The cache is not scope aware so we ask for a larger number of addresses than the cache wants. In the expectation that one
				// of them may be good for us
				hr = pBSResolveContext.Init(fSplitDetect, dwTimeout, DNS_ADDRESS_QUERY);

				if (hr.Failed)
				{
					pResolveContext = default;
				}
				else
				{
					pResolveContext = MakeGCPtr(pBSResolveContext);
					pBootStrapper.AddRef();
				}
			}

			if (hr.Failed)
			{
				// CustomDNSResolver has no retry cases, so any failed HRESULT is fatal
				fFatalError = true;
			}

			return hr;
		}

		private HRESULT IssueResolve(IntPtr pvContext, IntPtr pvCallbackContext, DRT_BOOTSTRAP_RESOLVE_CALLBACK callback, DRT_BOOTSTRAP_RESOLVE_CONTEXT ResolveContext, out bool fFatalError)
		{
			fFatalError = false;

			if (callback is null)
			{
				return HRESULT.E_INVALIDARG;
			}

			var hr = HRESULT.DRT_E_BOOTSTRAPPROVIDER_NOT_ATTACHED;
			var pBootStrapper = GetGCObject<CustomDnsBootStrapper>(pvContext);
			if (pBootStrapper.m_lAttachCount != 0)
			{
				var pResolveContext = GetGCObject<CBootStrapResolveContext>((IntPtr)ResolveContext);
				pResolveContext.AddRef();
				hr = pResolveContext.IssueResolve(pvCallbackContext, callback, pBootStrapper.m_szPortString, pBootStrapper.m_Address);
				pResolveContext.Release();
			}

			if (hr.Failed)
			{
				// DNSResolver has no retry cases, so any failed HRESULT is fatal
				fFatalError = true;
			}
			return hr;
		}

		private HRESULT Register(IntPtr pvContext, IntPtr pAddressList) =>
			//Custom DNS resolver Register does nothing at this time
			HRESULT.S_OK;

		public void Release()
		{
			if (InterlockedDecrement(ref m_lRefCount) == 0)
			{
				FreeGCPtr(m_BootStrapModule.pvContext);
				GC.ReRegisterForFinalize(this);
			}
		}

		private void Unregister(IntPtr pvContext)
		{
			//Custom DNS resolver Unregister does nothing at this time
		}
	};
}