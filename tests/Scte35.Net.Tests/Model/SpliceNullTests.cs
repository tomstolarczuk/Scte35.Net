using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests.Model
{
    public class SpliceNullCommandTests
    {
        [Fact]
        public void Type_And_PayloadBytes_Are_Correct()
        {
            var cmd = new SpliceNullCommand();
            Assert.Equal(SpliceCommandType.SpliceNull, cmd.Type);
            Assert.Equal(0, cmd.PayloadBytes);
        }

        [Fact]
        public void Encode_Writes_NoBytes()
        {
            var cmd = new SpliceNullCommand();
            cmd.Encode([]);
        }

        [Fact]
        public void Decode_Reads_NoBytes()
        {
            var cmd = new SpliceNullCommand();
            cmd.Decode(ReadOnlySpan<byte>.Empty);
        }

        [Fact]
        public void Encode_WithNonZeroBuffer_Throws()
        {
            var cmd = new SpliceNullCommand();
            var buf = new byte[1];
            Assert.Throws<InvalidOperationException>(() => cmd.Encode(buf));
        }

        [Fact]
        public void Decode_WithNonZeroData_Throws()
        {
            var cmd = new SpliceNullCommand();
            var data = new byte[] { 0x00 };
            Assert.Throws<InvalidOperationException>(() => cmd.Decode(data));
        }
    }
}