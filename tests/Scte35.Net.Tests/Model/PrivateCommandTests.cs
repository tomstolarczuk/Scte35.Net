using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests.Model
{
    public class PrivateCommandTests
    {
        private static byte[] Encode(PrivateCommand cmd)
        {
            var buf = new byte[cmd.PayloadBytes];
            cmd.Encode(buf);
            Assert.Equal(cmd.PayloadBytes, buf.Length);
            return buf;
        }

        private static PrivateCommand RoundTrip(PrivateCommand src)
        {
            var bytes = Encode(src);
            var dst = new PrivateCommand();
            dst.Decode(bytes);
            return dst;
        }

        [Fact]
        public void Type_And_Length()
        {
            var cmd = new PrivateCommand();
            Assert.Equal(SpliceCommandType.PrivateCommand, cmd.Type);
            Assert.Equal(4, cmd.PayloadBytes);
        }

        [Fact]
        public void Encode_KnownBytes_BigEndianIdentifier()
        {
            var cmd = new PrivateCommand
            {
                Identifier = 0x11223344u,
                PrivateBytes = [0xAA, 0xBB, 0xCC]
            };

            var bytes = Encode(cmd);

            var expected = new byte[] { 0x11, 0x22, 0x33, 0x44, 0xAA, 0xBB, 0xCC };
            Assert.Equal(expected, bytes);
        }

        [Fact]
        public void RoundTrip_NoPrivateBytes()
        {
            var cmd = new PrivateCommand
            {
                Identifier = 0xDEADBEEFu,
                PrivateBytes = []
            };

            var rt = RoundTrip(cmd);

            Assert.Equal(0xDEADBEEFu, rt.Identifier);
            Assert.Empty(rt.PrivateBytes);
        }

        [Fact]
        public void RoundTrip_WithPrivateBytes()
        {
            var payload = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
            var cmd = new PrivateCommand
            {
                Identifier = 0xAABBCCDDu,
                PrivateBytes = payload
            };

            var rt = RoundTrip(cmd);

            Assert.Equal(0xAABBCCDDu, rt.Identifier);
            Assert.True(payload.SequenceEqual(rt.PrivateBytes));
        }

        [Fact]
        public void Decode_TooShort_Throws()
        {
            var data = new byte[] { 0x00, 0x01, 0x02 };
            var cmd = new PrivateCommand();
            Assert.ThrowsAny<Exception>(() => cmd.Decode(data));
        }

        [Fact]
        public void Encode_BufferTooSmall_Throws()
        {
            var cmd = new PrivateCommand
            {
                Identifier = 0x01020304u,
                PrivateBytes = [1, 2, 3, 4]
            };

            // too-small buffer
            var buf = new byte[cmd.PayloadBytes - 1];
            Assert.ThrowsAny<Exception>(() => cmd.Encode(buf));
        }
    }
}