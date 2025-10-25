using Scte35.Net.Constants;
using Scte35.Net.Model;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests
{
    public class Scte35FacadeTests
    {
        private static SpliceInfoSection MakeNullSection() => new()
        {
            SpliceCommand = new SpliceNullCommand()
        };

        [Fact]
        public void EncodeDecode_RawBytes_RoundTrip_TimeSignal()
        {
            var sis = new SpliceInfoSection
            {
                PtsAdjustment90K = 0x1ABCDEUL & Scte35Constants.PtsMax,
                SpliceCommand = new TimeSignalCommand
                    { TimeSpecifiedFlag = true, PtsTime90K = 0x12345678UL & Scte35Constants.PtsMax }
            };

            var bytes = Scte35.Encode(sis);
            Assert.Equal(sis.PayloadBytes, bytes.Length);

            var dec = Scte35.Decode(bytes);
            Assert.True(dec.Crc32Valid);
            var cmd = Assert.IsType<TimeSignalCommand>(dec.SpliceCommand);
            Assert.True(cmd.TimeSpecifiedFlag);
            Assert.Equal(0x12345678UL & Scte35Constants.PtsMax, cmd.PtsTime90K!.Value);
            Assert.Equal(sis.PtsAdjustment90K, dec.PtsAdjustment90K);
        }

        [Fact]
        public void Base64_RoundTrip()
        {
            var sis = MakeNullSection();

            var b64 = Scte35.ToBase64(sis);
            var dec = Scte35.FromBase64(b64);

            Assert.True(dec.Crc32Valid);
            Assert.IsType<SpliceNullCommand>(dec.SpliceCommand);
        }

        [Fact]
        public void Hex_RoundTrip_And_0xPrefix()
        {
            var sis = MakeNullSection();

            var hexLower = Scte35.ToHex(sis); // default lower==true
            var hexUpper = Scte35.ToHex(sis, lower: false);

            Assert.DoesNotContain(hexLower, c => char.IsLetter(c) && char.IsUpper(c));
            Assert.Contains(hexUpper, c => char.IsLetter(c) && char.IsUpper(c));

            // fromHex with 0x prefix should still work
            var prefixed = "0x" + hexUpper;
            var fromHex = Scte35.FromHex(prefixed);

            Assert.True(fromHex.Crc32Valid);
            Assert.IsType<SpliceNullCommand>(fromHex.SpliceCommand);
        }

        [Fact]
        public void TryFromHex_Invalid_ReturnsFalse()
        {
            Assert.False(Scte35.TryFromHex("GG", out _)); // bad hex chars
            Assert.False(Scte35.TryFromHex("ABC", out _)); // odd length
        }

        [Fact]
        public void TryDecode_InvalidTableId_ReturnsFalse()
        {
            var sis = MakeNullSection();
            var bytes = Scte35.Encode(sis);

            bytes[0] ^= 0xFF; // corrupt table_id

            var ok = Scte35.TryDecode(bytes, out var dec);
            Assert.False(ok);
            Assert.Null(dec);
        }

        [Fact]
        public void Encode_Length_Equals_PayloadBytes()
        {
            var sis = new SpliceInfoSection
            {
                SpliceCommand = new TimeSignalCommand { TimeSpecifiedFlag = false } // 1-byte splice_time
            };

            var buf = Scte35.Encode(sis);
            Assert.Equal(sis.PayloadBytes, buf.Length);
        }

        [Fact]
        public void PrivateCommand_UnknownIdentifier_SetsSkipFlag()
        {
            var sis = new SpliceInfoSection
            {
                SpliceCommand = new PrivateCommand
                {
                    Identifier = 0xAABBCCDDu,
                    PrivateBytes = new byte[] { 1, 2, 3 }
                }
            };

            var buf = Scte35.Encode(sis);

            // recognize only a different vendor ID → this one is "unknown"
            var dec = Scte35.Decode(buf, privateIdRecognizer: id => id == 0x11223344u);

            Assert.True(dec.Crc32Valid);
            Assert.True(dec.SkipDueToUnknownPrivateIdentifier);
            var pc = Assert.IsType<PrivateCommand>(dec.SpliceCommand);
            Assert.Equal(0xAABBCCDDu, pc.Identifier);
            Assert.Equal([1, 2, 3], pc.PrivateBytes);
        }

        [Fact]
        public void UnknownSpliceCommand_RoundTrips_Through_SIS()
        {
            var unk = new UnknownSpliceCommand(0xA0);
            unk.Decode(new byte[] { 0xDE, 0xAD });

            var sis = new SpliceInfoSection { SpliceCommand = unk };
            var buf = Scte35.Encode(sis);

            var dec = Scte35.Decode(buf);
            var outCmd = Assert.IsType<UnknownSpliceCommand>(dec.SpliceCommand);

            Assert.Equal((byte)0xA0, outCmd.CommandType);
            Assert.Equal(new byte[] { 0xDE, 0xAD }, outCmd.Payload);
            Assert.True(dec.Crc32Valid);
        }

        [Fact]
        public void Hex_TryFromHex_Path_Succeeds()
        {
            var sis = new SpliceInfoSection
            {
                SpliceCommand = new SpliceInsertCommand
                {
                    SpliceEventId = 1,
                    OutOfNetworkIndicator = true,
                    ProgramSpliceFlag = true,
                    SpliceImmediateFlag = true,
                    DurationFlag = false,
                    UniqueProgramId = 0x55AA,
                    AvailNum = 2,
                    AvailsExpected = 3
                }
            };

            var hex = Scte35.ToHex(sis);
            Assert.True(Scte35.TryFromHex(hex, out var dec));
            Assert.NotNull(dec);
            Assert.True(dec.Crc32Valid);
            Assert.IsType<SpliceInsertCommand>(dec.SpliceCommand);
        }
    }
}