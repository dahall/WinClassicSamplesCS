using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using Vanara.PInvoke;

namespace Vanara;

/// <summary>
/// <para>Represents a 64-bit unsigned integer value.</para>
/// <note type="note">Your C compiler may support 64-bit integers natively. For example, Microsoft Visual C++ supports the __int64 sized
/// integer type. For more information, see the documentation included with your C compiler.</note>
/// </summary>
/// <remarks>
/// The <c>ULARGE_INTEGER</c> structure is actually a union. If your compiler has built-in support for 64-bit integers, use the
/// <c>QuadPart</c> member to store the 64-bit integer. Otherwise, use the <c>LowPart</c> and <c>HighPart</c> members to store the 64-bit integer.
/// </remarks>
// https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-ularge_integer-r1 typedef union _ULARGE_INTEGER { struct { DWORD
// LowPart; DWORD HighPart; } DUMMYSTRUCTNAME; struct { DWORD LowPart; DWORD HighPart; } u; ULONGLONG QuadPart; } ULARGE_INTEGER;
[PInvokeData("winnt.h", MSDNShortId = "NS:winnt._ULARGE_INTEGER~r1")]
[StructLayout(LayoutKind.Explicit), Serializable]
[DebuggerDisplay("{QuadPart}:{LowPart}/{HighPart}")]
[TypeConverter(typeof(ULARGE_INTEGERTypeConverter))]
public struct ULARGE_INTEGER : IEquatable<ULARGE_INTEGER>, IEquatable<ulong>, IComparable<ULARGE_INTEGER>, IComparable<ulong>, IConvertible, IComparable
{
	/// <summary>The low part</summary>
	[FieldOffset(0), IgnoreDataMember]
	public uint LowPart;

	/// <summary>The high part</summary>
	[FieldOffset(4), IgnoreDataMember]
	public uint HighPart;

	/// <summary>An unsigned 64-bit integer.</summary>
	[FieldOffset(0)]
	public ulong QuadPart;

	/// <summary>Initializes a new instance of the <see cref="ULARGE_INTEGER"/> struct.</summary>
	/// <param name="ul">A <see cref="ulong"/> value.</param>
	public ULARGE_INTEGER(ulong ul) => QuadPart = ul;

	/// <summary>Initializes a new instance of the <see cref="ULARGE_INTEGER"/> struct.</summary>
	/// <param name="lowpart">A <see cref="uint"/> value representing the lower DWORD of the value.</param>
	/// <param name="highpart">A <see cref="uint"/> value representing the upper DWORD of the value.</param>
	public ULARGE_INTEGER(uint lowpart, uint highpart)
	{
		LowPart = lowpart;
		HighPart = highpart;
	}

	/// <summary>Performs an implicit conversion from <see cref="ulong"/> to <see cref="ULARGE_INTEGER"/>.</summary>
	/// <param name="ul">A <see cref="ulong"/> value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator ULARGE_INTEGER(ulong ul) => new(ul);

	/// <summary>Performs an implicit conversion from <see cref="ULARGE_INTEGER"/> to <see cref="ulong"/>.</summary>
	/// <param name="ul">A <see cref="ULARGE_INTEGER"/> value.</param>
	/// <returns>The result of the conversion.</returns>
	public static implicit operator ulong(ULARGE_INTEGER ul) => ul.QuadPart;

	/// <inheritdoc/>
	public static bool operator !=(ULARGE_INTEGER left, ULARGE_INTEGER right) => !(left == right);

	/// <inheritdoc/>
	public static bool operator <(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.CompareTo(right) < 0;

	/// <inheritdoc/>
	public static bool operator <=(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.CompareTo(right) <= 0;

	/// <inheritdoc/>
	public static bool operator ==(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.Equals(right);

	/// <inheritdoc/>
	public static bool operator >(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.CompareTo(right) > 0;

	/// <inheritdoc/>
	public static bool operator >=(ULARGE_INTEGER left, ULARGE_INTEGER right) => left.CompareTo(right) >= 0;

	/// <inheritdoc/>
	public int CompareTo(ulong other) => QuadPart.CompareTo(other);

	/// <inheritdoc/>
	public int CompareTo(ULARGE_INTEGER other) => QuadPart.CompareTo(other.QuadPart);

	/// <inheritdoc/>
	public override bool Equals(object? obj) => obj is ULARGE_INTEGER uli && Equals(uli) || obj is ulong ul && Equals(ul);

	/// <inheritdoc/>
	public bool Equals(ULARGE_INTEGER other) => QuadPart == other.QuadPart;

	/// <inheritdoc/>
	public bool Equals(ulong other) => QuadPart == other;

	/// <inheritdoc/>
	public override int GetHashCode() => QuadPart.GetHashCode();

	/// <inheritdoc/>
	public TypeCode GetTypeCode() => QuadPart.GetTypeCode();

	/// <inheritdoc/>
	public string ToString(IFormatProvider? provider) => QuadPart.ToString(provider);

	/// <inheritdoc/>
	public override string ToString() => QuadPart.ToString();

	/// <inheritdoc/>
	int IComparable.CompareTo(object? obj) => QuadPart.CompareTo(obj);

	/// <inheritdoc/>
	bool IConvertible.ToBoolean(IFormatProvider? provider) => ((IConvertible)QuadPart).ToBoolean(provider);

	/// <inheritdoc/>
	byte IConvertible.ToByte(IFormatProvider? provider) => ((IConvertible)QuadPart).ToByte(provider);

	/// <inheritdoc/>
	char IConvertible.ToChar(IFormatProvider? provider) => ((IConvertible)QuadPart).ToChar(provider);

	/// <inheritdoc/>
	DateTime IConvertible.ToDateTime(IFormatProvider? provider) => ((IConvertible)QuadPart).ToDateTime(provider);

	/// <inheritdoc/>
	decimal IConvertible.ToDecimal(IFormatProvider? provider) => ((IConvertible)QuadPart).ToDecimal(provider);

	/// <inheritdoc/>
	double IConvertible.ToDouble(IFormatProvider? provider) => ((IConvertible)QuadPart).ToDouble(provider);

	/// <inheritdoc/>
	short IConvertible.ToInt16(IFormatProvider? provider) => ((IConvertible)QuadPart).ToInt16(provider);

	/// <inheritdoc/>
	int IConvertible.ToInt32(IFormatProvider? provider) => ((IConvertible)QuadPart).ToInt32(provider);

	/// <inheritdoc/>
	long IConvertible.ToInt64(IFormatProvider? provider) => ((IConvertible)QuadPart).ToInt64(provider);

	/// <inheritdoc/>
	sbyte IConvertible.ToSByte(IFormatProvider? provider) => ((IConvertible)QuadPart).ToSByte(provider);

	/// <inheritdoc/>
	float IConvertible.ToSingle(IFormatProvider? provider) => ((IConvertible)QuadPart).ToSingle(provider);

	/// <inheritdoc/>
	object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ((IConvertible)QuadPart).ToType(conversionType, provider);

	/// <inheritdoc/>
	ushort IConvertible.ToUInt16(IFormatProvider? provider) => ((IConvertible)QuadPart).ToUInt16(provider);

	/// <inheritdoc/>
	uint IConvertible.ToUInt32(IFormatProvider? provider) => ((IConvertible)QuadPart).ToUInt32(provider);

	/// <inheritdoc/>
	ulong IConvertible.ToUInt64(IFormatProvider? provider) => ((IConvertible)QuadPart).ToUInt64(provider);

	internal class ULARGE_INTEGERTypeConverter : UInt64Converter
	{
		public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
			base.CanConvertFrom(context, value.GetType()) ? new ULARGE_INTEGER((ulong)(base.ConvertFrom(context, culture, value) ?? 0)) : throw new ArgumentException(null, nameof(value));

		public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) =>
					value is ULARGE_INTEGER b ? base.ConvertTo(context, culture, b.QuadPart, destinationType) : throw new ArgumentException(null, nameof(value));
	}
}