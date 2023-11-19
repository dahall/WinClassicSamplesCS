using Vanara.PInvoke;
using static Vanara.PInvoke.Rpc;

namespace hello;

internal class helloc
{
	public static int Main(string[] args)
	{
		string? pszUuid = null;
		var pszProtocolSequence = "ncacn_ip_tcp";
		string? pszNetworkAddress = null;
		var pszEndpoint = "8765";
		string? pszSpn = null;
		string? pszOptions = null;
		var pszString = "hello, world";
		uint ulCode;
		int i;

		// allow the user to override settings with command line switches
		for (i = 0; i < args.Length; i++)
		{
			if ((args[i][0] == '-') || (args[i][0] == '/'))
			{
				switch (char.ToLower(args[i][1]))
				{
					case 'p': // protocol sequence
						pszProtocolSequence = args[++i];
						break;

					case 'n': // network address
						pszNetworkAddress = args[++i];
						break;

					case 'e': // endpoint
						pszEndpoint = args[++i];
						break;

					case 'a':
						pszSpn = args[++i];
						break;

					case 'o':
						pszOptions = args[++i];
						break;

					case 's':
						pszString = args[++i];
						break;

					case 'h':
					case '?':
					default:
						return Usage();
				}
			}
			else
			{
				return Usage();
			}
		}

		// Use a convenience function to concatenate the elements of the string binding into the proper sequence.
		Win32Error status = RpcStringBindingCompose(pszUuid, pszProtocolSequence, pszNetworkAddress, pszEndpoint, pszOptions, out var pszStringBinding);
		Console.Write("RpcStringBindingCompose returned {0}\n", status);
		Console.Write("pszStringBinding = {0}\n", pszStringBinding);
		if (status.Failed)
		{
			exit(status);
		}

		// Set the binding handle that will be used to bind to the server.
		status = RpcBindingFromStringBinding(pszStringBinding, out SafeRPC_BINDING_HANDLE hello_IfHandle);
		Console.Write("RpcBindingFromStringBinding returned {0}\n", status);
		if (status.Failed)
		{
			exit(status);
		}
		using (hello_IfHandle)
		{
			// User did not specify spn, construct one.
			pszSpn ??= Spn.Make();

			// Set the quality of service on the binding handle
			var SecQos = new RPC_SECURITY_QOS
			{
				Version = RPC_C_SECURITY_QOS_VERSION,
				Capabilities = RPC_C_QOS_CAPABILITIES.RPC_C_QOS_CAPABILITIES_MUTUAL_AUTH,
				IdentityTracking = RPC_C_QOS_IDENTITY.RPC_C_QOS_IDENTITY_DYNAMIC,
				ImpersonationType = RPC_C_IMP_LEVEL.RPC_C_IMP_LEVEL_IDENTIFY
			};

			// Set the security provider on binding handle
			status = RpcBindingSetAuthInfoEx(hello_IfHandle, pszSpn, RPC_C_AUTHN_LEVEL.RPC_C_AUTHN_LEVEL_PKT_PRIVACY, RPC_C_AUTHN.RPC_C_AUTHN_GSS_NEGOTIATE,
				default, RPC_C_AUTHZ.RPC_C_AUTHZ_NONE, in SecQos);
			Console.Write("RpcBindingSetAuthInfoEx returned {0}\n", status);
			if (status.Failed)
			{
				exit(status);
			}

			Console.Write("Calling the remote procedure 'HelloProc'\n");
			Console.Write("Print the string '{0}' on the server\n", pszString);

			try
			{
				HelloProc(hello_IfHandle, pszString); // make call with user message
				Console.Write("Calling the remote procedure 'Shutdown'\n");
				Shutdown(hello_IfHandle); // shut down the server side
			}
			catch (Exception)
			{
				ulCode = ((((RpcExceptionCode() != STATUS_ACCESS_VIOLATION) &&
				(RpcExceptionCode() != STATUS_DATATYPE_MISALIGNMENT) &&
				(RpcExceptionCode() != STATUS_PRIVILEGED_INSTRUCTION) &&
				(RpcExceptionCode() != STATUS_BREAKPOINT) &&
				(RpcExceptionCode() != STATUS_STACK_OVERFLOW) &&
				(RpcExceptionCode() != STATUS_IN_PAGE_ERROR) &&
				(RpcExceptionCode() != STATUS_GUARD_PAGE_VIOLATION)
				)
				? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH))
				Console.Write("Runtime reported exception 0x%lx = %ld\n", ulCode, ulCode);
			}
		}

		return 0;

		static void exit(Win32Error err) => Environment.Exit((int)(uint)err);
	}

	private static int Usage()
	{
		Console.Error.Write("Usage: {0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
		Console.Error.Write(" -p protocol_sequence\n");
		Console.Error.Write(" -n network_address\n");
		Console.Error.Write(" -e endpoint\n");
		Console.Error.Write(" -a server principal name\n");
		Console.Error.Write(" -o options\n");
		Console.Error.Write(" -s string\n");
		return 1;
	}
}