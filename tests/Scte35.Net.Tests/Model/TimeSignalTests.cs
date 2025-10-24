using Scte35.Net.Constants;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests.Model
{
    public class TimeSignalCommandTests
    {
        private static byte[] Encode(TimeSignalCommand cmd)
        {
            var buf = new byte[cmd.PayloadBytes];
            cmd.Encode(buf);
            return buf;
        }

        private static TimeSignalCommand RoundTrip(TimeSignalCommand src)
        {
            var bytes = Encode(src);
            var dst = new TimeSignalCommand();
            dst.Decode(bytes);
            return dst;
        }

        [Fact]
        public void Type_And_PayloadLengths()
        {
            var a = new TimeSignalCommand { TimeSpecifiedFlag = false };
            Assert.Equal(SpliceCommandType.TimeSignal, a.Type);
            Assert.Equal(1, a.PayloadBytes);

            var b = new TimeSignalCommand { TimeSpecifiedFlag = true, PtsTime90K = 0 };
            Assert.Equal(5, b.PayloadBytes);
        }

        [Fact]
        public void Encode_FlagFalse_WritesReservedOnes()
        {
            var cmd = new TimeSignalCommand { TimeSpecifiedFlag = false };
            var bytes = Encode(cmd);
            Assert.Equal([0x7F], bytes);
        }

        [Fact]
        public void Encode_FlagTrue_PtsZero_ReservedOnes()
        {
            var cmd = new TimeSignalCommand { TimeSpecifiedFlag = true, PtsTime90K = 0UL };
            var bytes = Encode(cmd);

            Assert.Equal(5, bytes.Length);
            Assert.Equal(0xFE, bytes[0]);
            Assert.Equal(0x00, bytes[1]);
            Assert.Equal(0x00, bytes[2]);
            Assert.Equal(0x00, bytes[3]);
            Assert.Equal(0x00, bytes[4]);
        }

        [Fact]
        public void Encode_FlagTrue_PtsMax_AllOnes()
        {
            var cmd = new TimeSignalCommand { TimeSpecifiedFlag = true, PtsTime90K = Scte35Constants.PtsMax };
            var bytes = Encode(cmd);

            Assert.Equal([0xFF, 0xFF, 0xFF, 0xFF, 0xFF], bytes);
        }

        [Fact]
        public void Decode_AllOnesInReserved_IsAccepted()
        {
            var wire = new byte[] { 0xFE, 0x00, 0x00, 0x00, 0x00 };
            var cmd = new TimeSignalCommand();
            cmd.Decode(wire);

            Assert.True(cmd.TimeSpecifiedFlag);
            Assert.Equal<ulong>(0, cmd.PtsTime90K!.Value);
        }

        [Fact]
        public void RoundTrip_TypicalPts()
        {
            var pts = 123456789UL & Scte35Constants.PtsMax;
            var cmd = new TimeSignalCommand { TimeSpecifiedFlag = true, PtsTime90K = pts };
            var rt = RoundTrip(cmd);

            Assert.True(rt.TimeSpecifiedFlag);
            Assert.Equal(pts, rt.PtsTime90K);
        }

        [Fact]
        public void RoundTrip_FlagFalse()
        {
            var cmd = new TimeSignalCommand { TimeSpecifiedFlag = false };
            var rt = RoundTrip(cmd);

            Assert.False(rt.TimeSpecifiedFlag);
            Assert.Null(rt.PtsTime90K);
        }

        [Fact]
        public void Decode_TooShort_WhenFlagTrue_Throws()
        {
            var bad = new byte[] { 0x80 };
            var cmd = new TimeSignalCommand();
            Assert.ThrowsAny<Exception>(() => cmd.Decode(bad));
        }

        [Fact]
        public void Decode_TrailingBytes_Throws_ForFlagFalse()
        {
            var legal = new TimeSignalCommand { TimeSpecifiedFlag = false };
            var bytes = Encode(legal);

            var withExtra = new byte[] { bytes[0], 0x00 };
            var cmd = new TimeSignalCommand();
            Assert.Throws<InvalidOperationException>(() => cmd.Decode(withExtra));
        }

        [Fact]
        public void Encode_MasksTo33Bits()
        {
            var tooWide = (1UL << 40) - 1;
            var expected = tooWide & Scte35Constants.PtsMax;

            var cmd = new TimeSignalCommand { TimeSpecifiedFlag = true, PtsTime90K = tooWide };
            var rt = RoundTrip(cmd);

            Assert.Equal(expected, rt.PtsTime90K);
        }
    }
}