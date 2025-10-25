using Scte35.Net.Core;

namespace Scte35.Net.Tests.Core;

public class BinaryExtensionsTests
{
	[Fact]
	public void ReadWriteUInt24BE_RoundTrips()
	{
		Span<byte> buffer = stackalloc byte[3];
		uint expected = 0xABCDE;

		buffer.WriteUInt24BE(expected);
		uint actual = BinaryExtensions.ReadUInt24BE(buffer);

		Assert.Equal(expected, actual);
	}

	[Fact]
	public void ReadWriteUInt32BE_RoundTrips()
	{
		Span<byte> buf = stackalloc byte[4];
		uint expected = 0xDEADBEEF;

		buf.WriteUInt32BE(expected);
		uint actual = ((ReadOnlySpan<byte>)buf).ReadUInt32BE();

		Assert.Equal(expected, actual);
		Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], buf.ToArray());
	}

	[Fact]
	public void ReadUInt32BE_TooSmallThrows()
	{
		Assert.Throws<ArgumentException>(InvokeReadUInt32TooSmall);
	}

	[Fact]
	public void WriteUInt32BE_TooSmallThrows()
	{
		Assert.Throws<ArgumentException>(InvokeWriteUInt32TooSmall);
	}

	[Fact]
	public void ReadUInt24BE_TooSmallThrows()
	{
		Assert.Throws<ArgumentException>(InvokeReadUInt24TooSmall);
	}

	[Fact]
	public void WriteUInt24BE_TooSmallThrows()
	{
		Assert.Throws<ArgumentException>(InvokeWriteUInt24TooSmall);
	}

	[Fact]
	public void SliceExact_WithinBoundsReturnsExpectedSlice()
	{
		ReadOnlySpan<byte> span = [0, 1, 2, 3, 4];

		ReadOnlySpan<byte> slice = span.SliceExact(1, 3);

		Assert.Equal([1, 2, 3], slice.ToArray());
	}

	[Fact]
	public void SliceExact_ReadOnlyOutOfBoundsThrows()
	{
		Assert.Throws<ArgumentOutOfRangeException>(InvokeSliceExactReadOnlyOutOfBounds);
	}

	[Fact]
	public void SliceExact_WritableOutOfBoundsThrows()
	{
		Assert.Throws<ArgumentOutOfRangeException>(InvokeSliceExactWritableOutOfBounds);
	}

	[Fact]
	public void SliceExact_WritableWithinBoundsReturnsExpectedSlice()
	{
		Span<byte> span = stackalloc byte[5];
		span[0] = 10;
		span[1] = 11;
		span[2] = 12;
		span[3] = 13;
		span[4] = 14;

		Span<byte> slice = span.SliceExact(1, 3);

		Assert.Equal([11, 12, 13], slice.ToArray());

		slice[0] = 99;
		Assert.Equal(99, span[1]);
	}

	[Fact]
	public void SliceExact_ZeroLengthIsAllowed()
	{
		ReadOnlySpan<byte> ro = [1, 2, 3];
		var empty = ro.SliceExact(1, 0);
		Assert.Equal(0, empty.Length);

		Span<byte> w = stackalloc byte[3];
		var emptyW = w.SliceExact(3, 0);
		Assert.Equal(0, emptyW.Length);
	}

	[Fact]
	public void ReadUInt24BE_ExactlyThreeBytesWorks()
	{
		ReadOnlySpan<byte> ro = [0x01, 0x23, 0x45];
		Assert.Equal(0x012345u, ro.ReadUInt24BE());
	}

	[Fact]
	public void WriteUInt24BE_ExactlyThreeBytesWorks()
	{
		Span<byte> w = stackalloc byte[3];
		w.WriteUInt24BE(0x010203);
		Assert.Equal([0x01, 0x02, 0x03], w.ToArray());
	}

	private static void InvokeReadUInt32TooSmall()
	{
		((ReadOnlySpan<byte>)new byte[] { 0xAA, 0xBB, 0xCC }).ReadUInt32BE();
	}

	private static void InvokeWriteUInt32TooSmall()
	{
		Span<byte> span = stackalloc byte[3];
		span.WriteUInt32BE(0u);
	}

	private static void InvokeReadUInt24TooSmall()
	{
		((ReadOnlySpan<byte>)new byte[] { 0xAA, 0xBB }).ReadUInt24BE();
	}

	private static void InvokeWriteUInt24TooSmall()
	{
		Span<byte> span = stackalloc byte[2];
		span.WriteUInt24BE(0);
	}

	private static void InvokeSliceExactReadOnlyOutOfBounds()
	{
		((ReadOnlySpan<byte>)new byte[] { 0, 1, 2 }).SliceExact(2, 2);
	}

	private static void InvokeSliceExactWritableOutOfBounds()
	{
		Span<byte> span = stackalloc byte[3];
		span.SliceExact(3, 1);
	}
}