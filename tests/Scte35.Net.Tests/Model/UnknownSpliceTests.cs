using Scte35.Net.Model;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests.Model
{
    public class UnknownSpliceCommandTests
    {
        private static UnknownSpliceCommand WithPayload(byte type, byte[] payload)
        {
            var cmd = new UnknownSpliceCommand(type);
            cmd.Decode(payload);
            return cmd;
        }

        [Fact]
        public void Constructor_StoresType_And_TypeCastsBackToByte()
        {
            const byte t = 0xAA;
            var cmd = new UnknownSpliceCommand(t);

            Assert.Equal(t, cmd.CommandType);
            Assert.Equal(t, (byte)cmd.Type);
        }

        [Fact]
        public void Decode_SetsPayload_And_Length()
        {
            var data = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            var cmd = new UnknownSpliceCommand(0xAA);

            cmd.Decode(data);

            Assert.Equal(4, cmd.PayloadBytes);
            Assert.Equal(data, cmd.Payload);
        }

        [Fact]
        public void Decode_Empty_SetsEmptyArray()
        {
            var cmd = new UnknownSpliceCommand(0xAA);
            cmd.Decode(ReadOnlySpan<byte>.Empty);
            Assert.NotNull(cmd.Payload);
            Assert.Empty(cmd.Payload);
            Assert.Equal(0, cmd.PayloadBytes);
        }

        [Fact]
        public void Decode_CopiesPayload_NotAliased()
        {
            var data = new byte[] { 1, 2, 3 };
            var cmd = new UnknownSpliceCommand(0xAA);

            cmd.Decode(data);
            data[0] = 0xFF; // mutate source

            Assert.Equal([1, 2, 3], cmd.Payload); // unchanged inside
        }

        [Fact]
        public void Encode_ExactLength_Succeeds()
        {
            var payload = new byte[] { 0x10, 0x20, 0x30 };
            var cmd = WithPayload(0xAA, payload);

            var buf = new byte[cmd.PayloadBytes];
            cmd.Encode(buf);

            Assert.Equal(payload, buf);
        }

        [Fact]
        public void Encode_BufferTooSmall_Throws()
        {
            var payload = new byte[] { 0x00, 0x01, 0x02 };
            var cmd = WithPayload(0xAA, payload);

            var tooSmall = new byte[payload.Length - 1];
            Assert.ThrowsAny<Exception>(() => cmd.Encode(tooSmall));
        }

        [Fact]
        public void Encode_WithZeroPayload_RequiresZeroLengthBuffer()
        {
            var cmd = new UnknownSpliceCommand(0xAA);
            cmd.Decode(ReadOnlySpan<byte>.Empty); // zero payload

            cmd.Encode([]);

            var bad = new byte[1];
            Assert.ThrowsAny<Exception>(() => cmd.Encode(bad));
        }

        [Fact]
        public void RoundTrip_ViaSpliceInfoSection_PreservesTypeAndPayload()
        {
            var unk = WithPayload(0xA0, new byte[] { 0xDE, 0xAD });

            var sis = new SpliceInfoSection
            {
                SpliceCommand = unk
            };

            var wire = new byte[sis.PayloadBytes];
            sis.Encode(wire);

            var dec = new SpliceInfoSection();
            dec.Decode(wire);

            var outCmd = Assert.IsType<UnknownSpliceCommand>(dec.SpliceCommand);
            Assert.Equal((byte)0xA0, outCmd.CommandType);
            Assert.Equal(new byte[] { 0xDE, 0xAD }, outCmd.Payload);
            Assert.True(dec.Crc32Valid);
        }
    }
}