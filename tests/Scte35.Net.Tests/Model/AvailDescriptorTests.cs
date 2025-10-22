using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
    public class AvailDescriptorTests
    {
        [Fact]
        public void PayloadBytes_Is8()
        {
            var d = new AvailDescriptor();
            Assert.Equal(8, d.PayloadBytes);
        }

        [Fact]
        public void Tag_IsAvail()
        {
            var d = new AvailDescriptor();
            Assert.Equal(SpliceDescriptorTag.Avail, d.Tag);
        }

        [Theory]
        [InlineData(0u)]
        [InlineData(1u)]
        [InlineData(0x12345678u)]
        [InlineData(0x80000000u)]
        [InlineData(0xFFFFFFFFu)]
        public void EncodeDecode_Roundtrip(uint providerAvailId)
        {
            var d = new AvailDescriptor { ProviderAvailId = providerAvailId };
            var buf = new byte[d.PayloadBytes];

            d.Encode(buf);

            var d2 = new AvailDescriptor();
            d2.Decode(buf);

            Assert.Equal(providerAvailId, d2.ProviderAvailId);
        }

        [Fact]
        public void Encode_WritesCueIdentifierFirst()
        {
            var d = new AvailDescriptor { ProviderAvailId = 0xAABBCCDDu };
            var buf = new byte[d.PayloadBytes];

            d.Encode(buf);

            var header = BinaryExtensions.ReadUInt32BE(buf.AsSpan(0, 4));
            Assert.Equal(Scte35Constants.CueIdentifier, header);
        }

        [Fact]
        public void Decode_InvalidCueIdentifier_Throws()
        {
            var buf = new byte[8];

            // some random wrong data
            buf[0] = 0xDE; buf[1] = 0xAD; buf[2] = 0xBE; buf[3] = 0xEF;
            buf[4] = 0x01; buf[5] = 0x23; buf[6] = 0x45; buf[7] = 0x67;

            var d = new AvailDescriptor();
            Assert.ThrowsAny<Exception>(() => d.Decode(buf));
        }

        [Fact]
        public void Encode_DestTooSmall_Throws()
        {
            var d = new AvailDescriptor { ProviderAvailId = 0x11223344u };
            var small = new byte[d.PayloadBytes - 1];

            Assert.ThrowsAny<Exception>(() => d.Encode(small));
        }

        [Theory]
        [InlineData(0)]  // too small
        [InlineData(7)]  // too small
        [InlineData(9)]  // too large
        [InlineData(12)] // too large
        public void Decode_WrongLength_Throws(int len)
        {
            var d = new AvailDescriptor();
            var buf = new byte[len];
            Assert.ThrowsAny<Exception>(() => d.Decode(buf));
        }

        [Fact]
        public void Encode_BufferContent_IsDeterministic()
        {
            var d = new AvailDescriptor { ProviderAvailId = 0x01020304u };
            var buf1 = new byte[d.PayloadBytes];
            var buf2 = new byte[d.PayloadBytes];

            d.Encode(buf1);
            d.Encode(buf2);

            Assert.Equal(buf1, buf2);
        }
    }
}