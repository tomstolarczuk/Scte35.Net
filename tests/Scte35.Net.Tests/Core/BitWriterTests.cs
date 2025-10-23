using Scte35.Net.Core;

namespace Scte35.Net.Tests.Core;

public class BitWriterTests
{
	[Fact]
	public void WriteBits_AcrossByteBoundaryProducesExpectedBuffer()
	{
		var buffer = new byte[2];
		var writer = new BitWriter(buffer);

		writer.WriteBits(0xABC, 12);

		Assert.Equal(12, writer.BitsWritten);
		Assert.Equal(4, writer.BitsRemaining);
		Assert.Equal(0xAB, buffer[0]);
		Assert.Equal(0xC0, buffer[1]);
	}

	[Fact]
	public void WriteBit_AppendsSingleBit()
	{
		var buffer = new byte[1];
		var writer = new BitWriter(buffer);

		writer.WriteBit(true);
		writer.WriteBit(false);
		writer.WriteBit(true);
		writer.WriteBit(true);

		Assert.Equal(4, writer.BitsWritten);
		Assert.Equal(4, writer.BitsRemaining);
		Assert.Equal(0b1011_0000, buffer[0]);
	}

	[Fact]
	public void WriteBits_BeyondCapacityThrows()
	{
		var buffer = new byte[1];
		var writer = new BitWriter(buffer);

		writer.WriteBits(0b1111_1111, 8);

		InvalidOperationException? captured = null;
		try
		{
			writer.WriteBits(1, 1);
		}
		catch (InvalidOperationException ex)
		{
			captured = ex;
		}

		Assert.NotNull(captured);
		Assert.Equal(8, writer.BitsWritten);
		Assert.Equal(0, writer.BitsRemaining);
		Assert.Equal(0b1111_1111, buffer[0]);
	}

	[Fact]
	public void ReadBits64_Reads33BitValue()
	{
		// Prepare a 33-bit value using BitWriter, then read it back.
		var buf = new byte[5];
		var w = new BitWriter(buf);
		w.WriteBits64(0x1_2345_6789UL, 33);

		var r = new BitReader(buf);
		var v = r.ReadBits64(33);

		Assert.Equal(0x1_2345_6789UL, v);
		Assert.Equal(7, r.BitsRemaining); // 40 - 33 = 7 bits left
	}

	[Fact]
	public void WriteBits64_Writes33BitValue()
	{
		var buf = new byte[5];
		var w = new BitWriter(buf);
		w.WriteBits64(0x1_ABCDE_FEDUL, 33);

		var r = new BitReader(buf);
		var v = r.ReadBits64(33);
		Assert.Equal(0x1_ABCDE_FEDUL, v);
	}

	[Fact]
	public void AlignToNextByte_SkipsToBoundary()
	{
		var buf = new byte[2];
		var w = new BitWriter(buf);
		w.WriteBits(0b101, 3);
		w.AlignToNextByte();
		w.WriteBits(0xAB, 8);

		Assert.Equal(16, w.BitsWritten);
		Assert.Equal(0b1010_0000, buf[0]);
		Assert.Equal(0xAB, buf[1]);
	}

	[Fact]
	public void WriteBytesAligned_CopiesAndAdvances()
	{
		var buf = new byte[4];
		var w = new BitWriter(buf);
		w.WriteBits(0b111, 3);  // misalign
		w.WriteBytesAligned("\u07ad"u8);
		Assert.Equal(24, w.BitsWritten);
		Assert.Equal(0b1110_0000, buf[0]); // 111xxxxx
		Assert.Equal(0xDE, buf[1]);
		Assert.Equal(0xAD, buf[2]);
	}

	[Fact]
	public void SeekToByte_MovesCursorForNextWrites()
	{
		var buf = new byte[3];
		var w = new BitWriter(buf);
		w.SeekToByte(1);
		w.WriteBits(0xFF, 8);
		Assert.Equal(16, w.BitsWritten);
		Assert.Equal(0x00, buf[0]);
		Assert.Equal(0xFF, buf[1]);
	}

	[Fact]
	public void SliceBytes_ReturnsSubWriterAndAdvancesParent()
	{
		var buf = new byte[4];
		var w = new BitWriter(buf);
		var sub = w.SliceBytes(2); // covers buf[0..1]
		sub.WriteBits(0xAB, 8);
		sub.WriteBits(0xCD, 8);
		w.WriteBits(0xEF, 8);      // parent now at buf[2]
		Assert.Equal([0xAB, 0xCD, 0xEF, 0x00], buf);
	}
}