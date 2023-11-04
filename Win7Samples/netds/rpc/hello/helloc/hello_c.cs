using System.Runtime.InteropServices;
using static Vanara.PInvoke.Rpc;

namespace hello
{
	public class hello_c : ihello
	{
		public const int TYPE_FORMAT_STRING_SIZE = 7 ;                                
		public const int PROC_FORMAT_STRING_SIZE = 63;                                
		public const int EXPR_FORMAT_STRING_SIZE = 1 ;                                
		public const int TRANSMIT_AS_TABLE_SIZE  = 0 ;
		public const int WIRE_MARSHAL_TABLE_SIZE = 0;
		public const int GENERIC_BINDING_TABLE_SIZE = 0;

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct hello_MIDL_TYPE_FORMAT_STRING
		{
			public short Pad;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = TYPE_FORMAT_STRING_SIZE)]
			public string Format;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct hello_MIDL_PROC_FORMAT_STRING
		{
			public short Pad;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = PROC_FORMAT_STRING_SIZE)]
			public string Format;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct hello_MIDL_EXPR_FORMAT_STRING
		{
			public long Pad;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = EXPR_FORMAT_STRING_SIZE)]
			public string Format;
		}

		public static readonly RPC_SYNTAX_IDENTIFIER RpcTransferSyntax =
			new(new Guid(0x8A885D04, 0x1CEB, 0x11C9, 0x9F, 0xE8, 0x08, 0x00, 0x2B, 0x10, 0x48, 0x60), 2, 0);

		public static hello_MIDL_TYPE_FORMAT_STRING hello__MIDL_TypeFormatString;
		public static hello_MIDL_PROC_FORMAT_STRING hello__MIDL_ProcFormatString;
		public static hello_MIDL_EXPR_FORMAT_STRING hello__MIDL_ExprFormatString;

		/* Standard interface: hello, ver. 1.0,
		   GUID={0x906B0CE0,0xC70B,0x1067,{0xB3,0x17,0x00,0xDD,0x01,0x06,0x62,0xDA}} */

		public static RPC_BINDING_HANDLE hello_IfHandle;

		public static readonly RPC_CLIENT_INTERFACE hello___RpcClientInterface = new()
		{
			Length = (uint)Marshal.SizeOf(typeof(RPC_CLIENT_INTERFACE)),
			InterfaceId = new RPC_SYNTAX_IDENTIFIER(new Guid(0x906B0CE0, 0xC70B, 0x1067, 0xB3, 0x17, 0x00, 0xDD, 0x01, 0x06, 0x62, 0xDA), 1, 0),
			TransferSyntax = new RPC_SYNTAX_IDENTIFIER(new Guid(0x8A885D04, 0x1CEB, 0x11C9, 0x9F, 0xE8, 0x08, 0x00, 0x2B, 0x10, 0x48, 0x60), 2, 0)
		};

		public static RPC_IF_HANDLE hello_ClientIfHandle = (RPC_IF_HANDLE) & hello___RpcClientInterface;

		extern const MIDL_STUB_DESC hello_StubDesc;

		public static RPC_BINDING_HANDLE hello__MIDL_AutoBindHandle;

		public void HelloProc([In] RPC_BINDING_HANDLE h1, [In] string pszString)
		{
			NdrClientCall2(
						  (PMIDL_STUB_DESC) & hello_StubDesc,
						  (PFORMAT_STRING) & hello__MIDL_ProcFormatString.Format[0],
						  (unsigned char * ) & h1);
		}

		public void Shutdown([In] RPC_BINDING_HANDLE h1)
		{
			NdrClientCall2(
						  (PMIDL_STUB_DESC) & hello_StubDesc,
						  (PFORMAT_STRING) & hello__MIDL_ProcFormatString.Format[34],
						  (unsigned char * ) & h1);
		}


		static readonly hello_MIDL_PROC_FORMAT_STRING hello__MIDL_ProcFormatString = new()
		{ 
		0,
		{

	/* Procedure HelloProc */

			0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/*  2 */	NdrFcLong( 0x0 ),	/* 0 */
/*  6 */	NdrFcShort( 0x0 ),	/* 0 */
/*  8 */	NdrFcShort( 0x8 ),	/* x86 Stack size/offset = 8 */
/* 10 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 12 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 14 */	NdrFcShort( 0x0 ),	/* 0 */
/* 16 */	NdrFcShort( 0x0 ),	/* 0 */
/* 18 */	0x42,		/* Oi2 Flags:  clt must size, has ext, */
			0x1,		/* 1 */
/* 20 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 22 */	NdrFcShort( 0x0 ),	/* 0 */
/* 24 */	NdrFcShort( 0x0 ),	/* 0 */
/* 26 */	NdrFcShort( 0x0 ),	/* 0 */

	/* Parameter pszString */

/* 28 */	NdrFcShort( 0x10b ),	/* Flags:  must size, must free, in, simple ref, */
/* 30 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 32 */	NdrFcShort( 0x4 ),	/* Type Offset=4 */

	/* Procedure Shutdown */

/* 34 */	0x0,		/* 0 */
			0x48,		/* Old Flags:  */
/* 36 */	NdrFcLong( 0x0 ),	/* 0 */
/* 40 */	NdrFcShort( 0x1 ),	/* 1 */
/* 42 */	NdrFcShort( 0x4 ),	/* x86 Stack size/offset = 4 */
/* 44 */	0x32,		/* FC_BIND_PRIMITIVE */
			0x0,		/* 0 */
/* 46 */	NdrFcShort( 0x0 ),	/* x86 Stack size/offset = 0 */
/* 48 */	NdrFcShort( 0x0 ),	/* 0 */
/* 50 */	NdrFcShort( 0x0 ),	/* 0 */
/* 52 */	0x40,		/* Oi2 Flags:  has ext, */
			0x0,		/* 0 */
/* 54 */	0x8,		/* 8 */
			0x1,		/* Ext Flags:  new corr desc, */
/* 56 */	NdrFcShort( 0x0 ),	/* 0 */
/* 58 */	NdrFcShort( 0x0 ),	/* 0 */
/* 60 */	NdrFcShort( 0x0 ),	/* 0 */

			0x0
		}
	};

		static readonly hello_MIDL_TYPE_FORMAT_STRING hello__MIDL_TypeFormatString =
			{
		0,
		{
			NdrFcShort( 0x0 ),	/* 0 */
/*  2 */	
			0x11, 0x8,	/* FC_RP [simple_pointer] */
/*  4 */	
			0x22,		/* FC_C_CSTRING */
			0x5c,		/* FC_PAD */

			0x0
		}
	};

		static const readonly short hello_FormatStringOffsetTable[] =
			{
	0,
	34
	};


		static const MIDL_STUB_DESC hello_StubDesc =
			{
	(void *)& hello___RpcClientInterface,
	MIDL_user_allocate,
	MIDL_user_free,
	&hello_IfHandle,
	0,
	0,
	0,
	0,
	hello__MIDL_TypeFormatString.Format,
	1, /* -error bounds_check flag */
	0x50002, /* Ndr library version */
	0,
	0x801026e, /* MIDL Version 8.1.622 */
	0,
	0,
	0,  /* notify & notify_flag routine table */
	0x1, /* MIDL flag */
	0, /* cs routines */
	0,   /* proxy/server info */
	0
	};
	}
}