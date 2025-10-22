namespace Scte35.Net.Core;

public static class BinaryExtensions
{
	public static uint ReadUInt24BE(this ReadOnlySpan<byte> span)
	{
		if (span.Length < 3)
			throw new ArgumentException("need at least 3 bytes", nameof(span));

		return (uint)(span[0] << 16 | span[1] << 8 | span[2]);
	}

	public static void WriteUInt24BE(this Span<byte> span, uint value)
	{
		if (span.Length < 3)
			throw new ArgumentException("need at least 3 bytes", nameof(span));

		span[0] = (byte)((value >> 16) & 0xff);
		span[1] = (byte)((value >> 8) & 0xff);
		span[2] = (byte)(value & 0xff);
	}

	public static uint ReadUInt32BE(this ReadOnlySpan<byte> span)
	{
		if (span.Length < 4)
			throw new ArgumentException("need at least 4 bytes", nameof(span));

		return (uint)(span[0] << 24 | span[1] << 16 | span[2] << 8 | span[3]);
	}

	public static void WriteUInt32BE(this Span<byte> span, uint value)
	{
		if (span.Length < 4)
			throw new ArgumentException("need at least 4 bytes", nameof(span));

		span[0] = (byte)(value >> 24);
		span[1] = (byte)(value >> 16);
		span[2] = (byte)(value >> 8);
		span[3] = (byte)value;
	}

	public static ReadOnlySpan<byte> SliceExact(this ReadOnlySpan<byte> span, int offset, int length)
	{
		if ((uint)offset > (uint)span.Length || (uint)length > (uint)(span.Length - offset))
			throw new ArgumentOutOfRangeException();

		return span.Slice(offset, length);
	}

	public static Span<byte> SliceExact(this Span<byte> span, int offset, int length)
	{
		if ((uint)offset > (uint)span.Length || (uint)length > (uint)(span.Length - offset))
			throw new ArgumentOutOfRangeException();

		return span.Slice(offset, length);
	}
}