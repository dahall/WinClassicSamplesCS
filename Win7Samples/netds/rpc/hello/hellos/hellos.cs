using Vanara.PInvoke;
using static Vanara.PInvoke.Rpc;

namespace hello
{
	static class hellos
	{
		public static int Main(string[] args)
		{
			string pszProtocolSequence = "ncacn_ip_tcp";
			IntPtr pszSecurity = default;
			string pszEndpoint = "8765";
			string pszSpn = default;
			uint cMinCalls = 1;
			uint cMaxCalls = 20;
			bool fDontWait = false;
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
						case 'e':
							pszEndpoint = args[++i];
							break;
						case 'a':
							pszSpn = args[++i];
							break;
						case 'm':
							cMaxCalls = uint.Parse(args[++i]);
							break;
						case 'n':
							cMinCalls = uint.Parse(args[++i]);
							break;
						case 'f':
							fDontWait = bool.Parse(args[++i]);
							break;

						case 'h':
						case '?':
						default:
							return Usage();
					}
				}
				else
					return Usage();
			}

			var status = RpcServerUseProtseqEp(pszProtocolSequence, cMaxCalls, pszEndpoint, pszSecurity); // Security descriptor
			Console.Write("RpcServerUseProtseqEp returned {0}\n", status);
			if (status.Failed)
			{
				exit(status);
			}

			// User did not specify spn, construct one.
			pszSpn ??= Spn.Make();

			// Using Negotiate as security provider.
			status = RpcServerRegisterAuthInfo(pszSpn, RPC_C_AUTHN.RPC_C_AUTHN_GSS_NEGOTIATE);

			Console.Write("RpcServerRegisterAuthInfo returned {0}\n", status);
			if (status.Failed)
			{
				exit(status);
			}

			status = RpcServerRegisterIfEx(hello_ServerIfHandle, default, default, 0, RPC_C_LISTEN_MAX_CALLS_DEFAULT, default);
			Console.Write("RpcServerRegisterIfEx returned {0}\n", status);
			if (status.Failed)
			{
				exit(status);
			}

			Console.Write("Calling RpcServerListen\n");
			status = RpcServerListen(cMinCalls, cMaxCalls, fDontWait);
			Console.Write("RpcServerListen returned: {0}\n", status);
			if (status.Failed)
			{
				exit(status);
			}

			if (fDontWait)
			{
				Console.Write("Calling RpcMgmtWaitServerListen\n");
				status = RpcMgmtWaitServerListen(); // wait operation
				Console.Write("RpcMgmtWaitServerListen returned: {0}\n", status);
				if (status.Failed)
				{
					exit(status);
				}
			}

			return 0;

			static void exit(Win32Error err) => Environment.Exit((int)(uint)err);
		}

		private static int Usage()
		{
			Console.Error.Write("Usage: {0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
			Console.Error.Write(" -p protocol_sequence\n");
			Console.Error.Write(" -e endpoint\n");
			Console.Error.Write(" -a server principal name\n");
			Console.Error.Write(" -m maxcalls\n");
			Console.Error.Write(" -n mincalls\n");
			Console.Error.Write(" -f flag_wait_op\n");
			return 1;
		}
	}
}
