using Scte35.Net.Core;

namespace Scte35.Net.Tests.Core;

public class BitReaderTests
{
    [Fact]
    public void ReadBitsAcrossByteBoundaryReturnsExpectedValue()
    {
        var reader = new BitReader([0xAB, 0xCD]);

        var value = reader.ReadBits(12);

        Assert.Equal(0xABCu, value);
        Assert.Equal(4, reader.BitsRemaining);
    }

    [Fact]
    public void ReadBitsWithInsufficientDataThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var reader = new BitReader([0xFF]);
            reader.ReadBits(9);
        });
    }

    [Fact]
    public void SkipBitsNegativeCountThrows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var reader = new BitReader([0x00]);
            reader.SkipBits(-1);
        });
    }

    [Fact]
    public void SkipBitsBeyondBufferThrows()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            var reader = new BitReader([0xFF, 0xFF]);
            reader.SkipBits(17);
        });
    }

    [Fact]
    public void SkipBitsAdvancesPositionForSubsequentRead()
    {
        var reader = new BitReader([0b1010_1100]);

        reader.SkipBits(4);
        var value = reader.ReadBits(4);

        Assert.Equal(0b1100u, value);
        Assert.Equal(0, reader.BitsRemaining);
    }
}

public class BitWriterTests
{
    [Fact]
    public void WriteBitsAcrossByteBoundaryProducesExpectedBuffer()
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
    public void WriteBitAppendsSingleBit()
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
    public void WriteBitsBeyondCapacityThrows()
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
}
