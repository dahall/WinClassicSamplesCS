using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System
{
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{QuadPart}:{LowPart}/{HighPart}")]
	internal struct ULARGE_INTEGER : IEquatable<ULARGE_INTEGER>, IEquatable<ulong>
	{
		[FieldOffset(0)]
		public uint LowPart;
		[FieldOffset(4)]
		public uint HighPart;
		[FieldOffset(0)]
		public ulong QuadPart;

		public static implicit operator ulong(ULARGE_INTEGER ul) => ul.QuadPart;

		public static implicit operator ULARGE_INTEGER(ulong ul) => new() { QuadPart = ul };

		public static bool operator ==(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.Equals(right);
		public static bool operator !=(ULARGE_INTEGER left, ULARGE_INTEGER right) => !(left == right);

		public override bool Equals(object obj) => obj is ULARGE_INTEGER uli && Equals(uli) || obj is ulong ul && Equals(ul);
		public bool Equals(ULARGE_INTEGER other) => QuadPart == other.QuadPart;
		public bool Equals(ulong other) => QuadPart == other;
		public override int GetHashCode() => HashCode.Combine(QuadPart);
	}
}