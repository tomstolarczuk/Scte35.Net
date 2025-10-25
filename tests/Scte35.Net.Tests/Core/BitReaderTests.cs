using Scte35.Net.Core;

namespace Scte35.Net.Tests.Core;

public class BitReaderTests
{
	[Fact]
	public void ReadBits_AcrossByteBoundaryReturnsExpectedValue()
	{
		var reader = new BitReader([0xAB, 0xCD]);

		var value = reader.ReadBits(12);

		Assert.Equal(0xABCu, value);
		Assert.Equal(4, reader.BitsRemaining);
	}

	[Fact]
	public void ReadBits_WithInsufficientDataThrows()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			var reader = new BitReader([0xFF]);
			reader.ReadBits(9);
		});
	}

	[Fact]
	public void SkipBits_NegativeCountThrows()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
		{
			var reader = new BitReader([0x00]);
			reader.SkipBits(-1);
		});
	}

	[Fact]
	public void SkipBits_BeyondBufferThrows()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			var reader = new BitReader([0xFF, 0xFF]);
			reader.SkipBits(17);
		});
	}

	[Fact]
	public void SkipBits_AdvancesPositionForSubsequentRead()
	{
		var reader = new BitReader([0b1010_1100]);

		reader.SkipBits(4);
		var value = reader.ReadBits(4);

		Assert.Equal(0b1100u, value);
		Assert.Equal(0, reader.BitsRemaining);
	}

	[Fact]
	public void ReadPrimitiveHelpers_WorkAsExpected()
	{
		var r = new BitReader([0x12, 0x34, 0x56, 0x78]);
		Assert.Equal((byte)0x12, r.ReadByte());
		Assert.Equal((ushort)0x3456, r.ReadUInt16());
		Assert.Equal(0x78u, r.ReadBits(8));
		Assert.Equal(0, r.BitsRemaining);
	}

	[Fact]
	public void AlignToNextByte_SkipsPartialBits()
	{
		var r = new BitReader([0b1010_1100, 0xFF]);
		r.ReadBits(3); // consume 3 bits
		r.AlignToNextByte(); // should skip remaining 5 bits of first byte
		Assert.Equal(8, r.BitsRemaining); // only second byte remains
		Assert.Equal((byte)0xFF, r.ReadByte());
		Assert.Equal(0, r.BitsRemaining);
	}

	[Fact]
	public void ReadBytesAligned_ReturnsSliceAndAdvances()
	{
		var r = new BitReader([0xAA, 0xBB, 0xCC, 0xDD]);
		r.ReadBits(4); // misalign
		var span = r.ReadBytesAligned(2); // should align, then read 0xBB, 0xCC
		Assert.Equal(0xBB, span[0]);
		Assert.Equal(0xCC, span[1]);
		Assert.Equal(8, r.BitsRemaining);
		Assert.Equal(0xDD, r.ReadByte()); // only 0xDD remains
	}

	[Fact]
	public void PeekBits_DoesNotAdvance()
	{
		var r = new BitReader([0xF0]); // 11110000
		var peek = r.PeekBits(4);
		Assert.Equal(0b1111u, peek);
		Assert.Equal(8, r.BitsRemaining);
		var real = r.ReadBits(4);
		Assert.Equal(0b1111u, real);
		Assert.Equal(4, r.BitsRemaining);
	}

	[Fact]
	public void PeekBits64_DoesNotAdvance()
	{
		var r = new BitReader([0x12, 0x34, 0x56]);
		var peek = r.PeekBits64(12); // 0x123
		Assert.Equal(0x123UL, peek);
		Assert.Equal(24, r.BitsRemaining);
	}

	[Fact]
	public void SeekToByte_MovesCursorSafely()
	{
		var r = new BitReader([0x00, 0x11, 0x22]);
		r.SeekToByte(2);
		Assert.Equal((byte)0x22, r.ReadByte());
		Assert.Equal(0, r.BitsRemaining);
	}

	[Fact]
	public void SliceBits_CreatesSubReaderWithoutAdvancingParent()
	{
		var r = new BitReader([0xAB, 0xCD]); // 1010 1011 1100 1101
		var sub = r.SliceBits(10); // take first 10 bits
		// Parent not advanced yet
		Assert.Equal(16, r.BitsRemaining);

		var val = sub.ReadBits(10); // read from slice only
		Assert.Equal(0b1010101111u, val); // 0x2AF
		Assert.Equal(6, sub.BitsRemaining);

		// Parent still untouched
		Assert.Equal(16, r.BitsRemaining);
	}

	[Fact]
	public void SliceBytes_CreatesSubReaderAndAdvancesParent()
	{
		var r = new BitReader([0xDE, 0xAD, 0xBE, 0xEF]);
		= r.ReadBits(8); // consume 0xDE
		var sub = r.SliceBytes(2); // should give AD BE and advance parent
		Assert.Equal((byte)0xAD, sub.ReadByte());
		Assert.Equal((byte)0xBE, sub.ReadByte());
		Assert.Equal(8, r.BitsRemaining); // only 0xEF left in parent
		Assert.Equal((byte)0xEF, r.ReadByte());
		Assert.Equal(0, r.BitsRemaining);
	}

	[Fact]
	public void ReadBytesAligned_ThrowsOnInsufficientBytes()
	{
		Assert.Throws<InvalidOperationException>(() =>
		{
			var r = new BitReader([0xAA]);
			r.ReadBytesAligned(2);
		});
	}
}