using Scte35.Net.Core;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
    public class PrivateDescriptorTests
    {
        private static byte[] MakeBytes(int len)
        {
            var data = new byte[len];
            var rng = new Random(12345);
            rng.NextBytes(data);
            return data;
        }

        private static byte[] MakeSequentialBytes(int len)
        {
            var data = new byte[len];
            for (int i = 0; i < len; i++) data[i] = (byte)(i & 0xFF);
            return data;
        }

        [Fact]
        public void Constructor_ValidPrivateTag_Succeeds()
        {
            var d = new PrivateDescriptor(0x80);
            Assert.Equal(0x80, d.PrivateTag);
        }

        [Theory]
        [InlineData(0x7F)]
        [InlineData(0xFF)]
        [InlineData(0x00)]
        [InlineData(0x50)]
        public void Constructor_InvalidPrivateTag_Throws(byte privateTag)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new PrivateDescriptor(privateTag));
            Assert.Contains("0x80..0xFE", ex.Message);
        }

        [Fact]
        public void PrivateBytes_DefaultsToEmptyArray()
        {
            var d = new PrivateDescriptor(0x80);
            Assert.NotNull(d.PrivateBytes);
            Assert.Empty(d.PrivateBytes);
        }

        [Fact]
        public void PayloadBytes_EmptyPrivateBytes_Is4()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x12345678,
                PrivateBytes = []
            };
            Assert.Equal(4, d.PayloadBytes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(64)]
        [InlineData(255)]
        [InlineData(1024)]
        public void PayloadBytes_VariousLengths_Is4PlusLength(int length)
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x11223344,
                PrivateBytes = new byte[length]
            };
            Assert.Equal(4 + length, d.PayloadBytes);
        }

        [Fact]
        public void EncodeDecode_EmptyPrivateBytes_RoundTrips()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0xDEADBEEF,
                PrivateBytes = []
            };

            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);

            var d2 = new PrivateDescriptor(0x80);
            d2.Decode(buf);

            Assert.Equal(0xDEADBEEFu, d2.Identifier);
            Assert.Empty(d2.PrivateBytes);
        }

        [Fact]
        public void EncodeDecode_WithPrivateBytes_RoundTrips()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x12345678,
                PrivateBytes = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE]
            };

            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);

            var d2 = new PrivateDescriptor(0x80);
            d2.Decode(buf);

            Assert.Equal(0x12345678u, d2.Identifier);
            Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD, 0xEE], d2.PrivateBytes);
        }

        [Fact]
        public void EncodeDecode_LargePrivateBytes_RoundTrips()
        {
            var privateData = MakeSequentialBytes(256);
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0xCAFEBABE,
                PrivateBytes = privateData
            };

            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);

            var d2 = new PrivateDescriptor(0x80);
            d2.Decode(buf);

            Assert.Equal(0xCAFEBABEu, d2.Identifier);
            Assert.Equal(privateData, d2.PrivateBytes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(64)]
        [InlineData(256)]
        [InlineData(1024)]
        [InlineData(4096)]
        public void EncodeDecode_Fuzz_RoundTrips(int payloadLen)
        {
            var bytes = MakeBytes(payloadLen);
            var d = new PrivateDescriptor(0x90)
            {
                Identifier = 0xAABBCCDD,
                PrivateBytes = bytes
            };
            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);

            var d2 = new PrivateDescriptor(0xFE);
            d2.Decode(buf);

            Assert.Equal(0xAABBCCDDu, d2.Identifier);
            Assert.Equal(bytes, d2.PrivateBytes);
            Assert.Equal(0xFE, d2.PrivateTag);
        }

        [Fact]
        public void Encode_WritesIdentifierFirst()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x01020304,
                PrivateBytes = [0xAA]
            };

            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);

            Assert.Equal(0x01020304u, BinaryExtensions.ReadUInt32BE(buf.AsSpan(0, 4)));
        }

        [Fact]
        public void Encode_WritesPrivateBytesAfterIdentifier()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x11223344,
                PrivateBytes = [0xDE, 0xAD, 0xBE, 0xEF]
            };

            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);

            Assert.Equal(0xDE, buf[4]);
            Assert.Equal(0xAD, buf[5]);
            Assert.Equal(0xBE, buf[6]);
            Assert.Equal(0xEF, buf[7]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void Decode_TooShort_Throws(int len)
        {
            var d = new PrivateDescriptor(0x80);
            var buf = new byte[len];
            Assert.ThrowsAny<Exception>(() => d.Decode(buf));
        }

        [Fact]
        public void Decode_ExtraBytes_BecomesPrivateBytes()
        {
            var buf = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0x11, 0x22, 0x33 };
            var d = new PrivateDescriptor(0x80);
            d.Decode(buf);

            Assert.Equal(0xAABBCCDDu, d.Identifier);
            Assert.Equal([0x11, 0x22, 0x33], d.PrivateBytes);
        }

        [Fact]
        public void Encode_DestTooSmall_Throws()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x11111111,
                PrivateBytes = [0x01, 0x02, 0x03]
            };
            var tooSmall = new byte[d.PayloadBytes - 1];
            Assert.ThrowsAny<Exception>(() => d.Encode(tooSmall));
        }

        [Fact]
        public void Encode_BufferContent_IsDeterministic()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x99887766,
                PrivateBytes = [0x11, 0x22, 0x33, 0x44]
            };
            var buf1 = new byte[d.PayloadBytes];
            var buf2 = new byte[d.PayloadBytes];

            d.Encode(buf1);
            d.Encode(buf2);

            Assert.Equal(buf1, buf2);
        }

        [Fact]
        public void Encode_DoesNotOverrun_Buffer()
        {
            var d = new PrivateDescriptor(0x80)
            {
                Identifier = 0x12345678,
                PrivateBytes = MakeSequentialBytes(32)
            };

            var oversized = new byte[d.PayloadBytes + 16];
            for (int i = 0; i < oversized.Length; i++) oversized[i] = 0xCC;

            d.Encode(oversized);

            // payload written region must differ, tail must remain 0xCC
            for (int i = d.PayloadBytes; i < oversized.Length; i++)
                Assert.Equal(0xCC, oversized[i]);
        }

        [Fact]
        public void Decode_PrivateBytes_AreCopied_NotAliased()
        {
            var src = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 1, 2, 3, 4 };
            var d = new PrivateDescriptor(0x80);
            d.Decode(src);

            // mutate source buffer after decode
            src[4] = 9;
            src[5] = 9;
            src[6] = 9;
            src[7] = 9;

            Assert.Equal([1, 2, 3, 4], d.PrivateBytes);
        }
    }
}