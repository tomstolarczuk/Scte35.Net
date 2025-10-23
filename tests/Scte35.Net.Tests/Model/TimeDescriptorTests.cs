using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
    public class TimeDescriptorTests
    {
        private static byte[] Encode(TimeDescriptor d)
        {
            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);
            Assert.Equal(d.PayloadBytes, buf.Length);
            return buf;
        }

        private static TimeDescriptor RoundTrip(TimeDescriptor src)
        {
            var bytes = Encode(src);
            var dst = new TimeDescriptor();
            dst.Decode(bytes);
            return dst;
        }

        [Fact]
        public void Tag_And_PayloadBytes_Are_Correct()
        {
            var d = new TimeDescriptor();
            Assert.Equal(SpliceDescriptorTag.Time, d.Tag);
            Assert.Equal(16, d.PayloadBytes);
        }

        [Fact]
        public void Encode_Matches_ExactWireBytes_ForKnownValues()
        {
            var d = new TimeDescriptor
            {
                TAISeconds = 0x000102030405UL,
                TAINs = 0x0A0B0C0D,
                UTCOffset = 0x1122
            };

            var bytes = Encode(d);

            var expected = new byte[]
            {
                0x43, 0x55, 0x45, 0x49, // identifier
                0x00, 0x01, 0x02, 0x03, 0x04, 0x05, // tai seconds
                0x0A, 0x0B, 0x0C, 0x0D, // tai ns
                0x11, 0x22 // utc offset
            };

            Assert.Equal(expected, bytes);
        }

        [Fact]
        public void RoundTrip_Symmetric_ForTypicalValues()
        {
            var d = new TimeDescriptor
            {
                TAISeconds = 1_700_000_000UL,
                TAINs = 123_456_789,
                UTCOffset = 37
            };

            var rt = RoundTrip(d);

            Assert.Equal(d.TAISeconds, rt.TAISeconds);
            Assert.Equal(d.TAINs, rt.TAINs);
            Assert.Equal(d.UTCOffset, rt.UTCOffset);
        }

        [Fact]
        public void RoundTrip_BoundaryValues_MaxWidths()
        {
            var d = new TimeDescriptor
            {
                TAISeconds = (1UL << 48) - 1, // 0xFFFFFFFFFFFF
                TAINs = 0xFFFFFFFF,
                UTCOffset = 0xFFFF
            };

            var rt = RoundTrip(d);

            Assert.Equal(d.TAISeconds, rt.TAISeconds);
            Assert.Equal(d.TAINs, rt.TAINs);
            Assert.Equal(d.UTCOffset, rt.UTCOffset);
        }

        [Fact]
        public void Decode_TooShort_Throws()
        {
            // 15 bytes instead of 16
            var tooShort = new byte[15];
            var d = new TimeDescriptor();
            Assert.ThrowsAny<Exception>(() => d.Decode(tooShort));
        }

        [Fact]
        public void Decode_TrailingBytes_Throws()
        {
            var d = new TimeDescriptor
            {
                TAISeconds = 0x000102030405UL,
                TAINs = 99,
                UTCOffset = 1
            };
            var bytes = Encode(d);

            var withExtra = new byte[bytes.Length + 1];
            Buffer.BlockCopy(bytes, 0, withExtra, 0, bytes.Length);

            var victim = new TimeDescriptor();
            var ex = Assert.Throws<InvalidOperationException>(() => victim.Decode(withExtra));
            Assert.Contains("Trailing bits", ex.Message);
        }
    }
}