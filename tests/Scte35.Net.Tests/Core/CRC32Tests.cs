using Scte35.Net.Core;

namespace Scte35.Net.Tests.Core;

public class Crc32Mpeg2Tests
{
    [Fact]
    public void KnownVector_123456789()
    {
        var bytes = "123456789"u8.ToArray();
        Assert.Equal(0x0376E6E7u, Crc32Mpeg2.Compute(bytes));
    }

    [Fact]
    public void TableAndBitwise_Match_OnRandomData()
    {
        var rnd = new Random(1234);
        var buf = new byte[4096];
        rnd.NextBytes(buf);

        uint tableCrc = Crc32Mpeg2.Compute(buf);
        uint bitCrc = Crc32Mpeg2.ComputePsiCrc32(buf);
        Assert.Equal(bitCrc, tableCrc);
    }
}