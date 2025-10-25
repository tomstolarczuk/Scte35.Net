using Scte35.Net.Constants;
using Scte35.Net.Model;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
    public class SpliceInfoSectionTests
    {
        private static byte[] Encode(SpliceInfoSection sis)
        {
            var buf = new byte[sis.PayloadBytes];
            sis.Encode(buf);
            Assert.Equal(sis.PayloadBytes, buf.Length);
            return buf;
        }

        private static SpliceInfoSection RoundTrip(SpliceInfoSection input)
        {
            var bytes = Encode(input);
            var outSis = new SpliceInfoSection();
            outSis.Decode(bytes);
            return outSis;
        }

        [Fact]
        public void Minimal_RoundTrip_SpliceNull_NoDescriptors_Unencrypted()
        {
            var sis = new SpliceInfoSection
            {
                SapType = SapType.NotSpecified,
                ProtocolVersion = 0,
                EncryptedPacket = false,
                EncryptionAlgorithm = EncryptionAlgorithm.None,
                PtsAdjustment90K = 0,
                CwIndex = 0,
                Tier = 0x0FFF,
                SpliceCommand = new SpliceNullCommand()
            };

            var bytes = Encode(sis);
            Assert.Equal(sis.PayloadBytes, bytes.Length);

            var dec = new SpliceInfoSection();
            dec.Decode(bytes);

            Assert.True(dec.Crc32Valid);
            Assert.Equal(SpliceCommandType.SpliceNull, dec.SpliceCommandType);
            Assert.Empty(dec.SpliceDescriptors);
            Assert.Equal(SapType.NotSpecified, dec.SapType);
            Assert.Equal((byte)0, dec.ProtocolVersion);
            Assert.False(dec.EncryptedPacket);
            Assert.Equal(EncryptionAlgorithm.None, dec.EncryptionAlgorithm);
            Assert.Equal<ulong>(0, dec.PtsAdjustment90K);
            Assert.Equal((byte)0, dec.CwIndex);
            Assert.Equal<ushort>(0x0FFF, dec.Tier);
        }

        [Fact]
        public void Command_Payload_Slicing_TimeSignal_Then_Private_RoundTrip()
        {
            // time_signal with time_specified_flag = 1
            var timeSignal = new TimeSignalCommand
            {
                TimeSpecifiedFlag = true,
                PtsTime90K = 0x12345678UL & Scte35Constants.PtsMax
            };

            var sis = new SpliceInfoSection
            {
                SpliceCommand = timeSignal
            };

            var rt1 = RoundTrip(sis);
            var cmd1 = Assert.IsType<TimeSignalCommand>(rt1.SpliceCommand);
            Assert.True(cmd1.TimeSpecifiedFlag);
            Assert.Equal<ulong>(0x12345678UL & Scte35Constants.PtsMax, cmd1.PtsTime90K!.Value);

            // swap in a private_command with known bytes
            var priv = new PrivateCommand
            {
                Identifier = 0xA1B2C3D4u,
                PrivateBytes = [0xDE, 0xAD, 0xBE, 0xEF]
            };
            sis.SpliceCommand = priv;

            var rt2 = RoundTrip(sis);
            var cmd2 = Assert.IsType<PrivateCommand>(rt2.SpliceCommand);
            Assert.Equal(0xA1B2C3D4u, cmd2.Identifier);
            Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], cmd2.PrivateBytes);
        }

        [Fact]
        public void Descriptor_Loop_TwoDescriptors_RoundTrip()
        {
            var td = new TimeDescriptor
            {
                TAISeconds = 0x000102030405UL,
                TAINs = 0x0A0B0C0D,
                UTCOffset = 0x3344
            };

            var avail = new AvailDescriptor
            {
                ProviderAvailId = 0x11223344
            };

            var sis = new SpliceInfoSection
            {
                SpliceCommand = new SpliceNullCommand()
            };
            sis.SpliceDescriptors.Add(td);
            sis.SpliceDescriptors.Add(avail);

            var rt = RoundTrip(sis);
            Assert.Equal(2, rt.SpliceDescriptors.Count);

            // descriptor[0] should be Time
            var td2 = Assert.IsType<TimeDescriptor>(rt.SpliceDescriptors[0]);
            Assert.Equal(td.TAISeconds, td2.TAISeconds);
            Assert.Equal(td.TAINs, td2.TAINs);
            Assert.Equal(td.UTCOffset, td2.UTCOffset);

            // descriptor[1] should be Avail
            var avail2 = Assert.IsType<AvailDescriptor>(rt.SpliceDescriptors[1]);
            Assert.Equal<uint>(0x11223344u, avail2.ProviderAvailId);
        }

        [Fact]
        public void CRC_Mismatch_Is_Detected()
        {
            var sis = new SpliceInfoSection
            {
                SpliceCommand = new TimeSignalCommand { TimeSpecifiedFlag = false }
            };

            var wire = new byte[sis.PayloadBytes];
            sis.Encode(wire);

            const int cwIndexOffset = 9;
            wire[cwIndexOffset] ^= 0xFF;

            var dec = new SpliceInfoSection();
            dec.Decode(wire); // no throw now

            Assert.False(dec.Crc32Valid); // mismatch is detected
            // sanity: section still parsed
            Assert.Equal(SpliceCommandType.TimeSignal, dec.SpliceCommandType);
        }

        [Fact]
        public void Encrypted_WithStuffing_And_ECRC32_RoundTrip()
        {
            var sis = new SpliceInfoSection
            {
                EncryptedPacket = true,
                EncryptionAlgorithm = EncryptionAlgorithm.None, // algorithm id present; value OK
                PtsAdjustment90K = 5,
                SpliceCommand = new SpliceNullCommand(),
                AlignmentStuffing = [0xAA, 0xBB, 0xCC],
                ECRC32 = 0x01020304u
            };

            var rt = RoundTrip(sis);

            Assert.True(rt.EncryptedPacket);
            Assert.Equal(EncryptionAlgorithm.None, rt.EncryptionAlgorithm);
            Assert.Equal<ulong>(5, rt.PtsAdjustment90K);
            Assert.Equal([0xAA, 0xBB, 0xCC], rt.AlignmentStuffing);
            Assert.Equal<uint>(0x01020304u, rt.ECRC32!.Value);
            Assert.True(rt.Crc32Valid);
        }

        [Fact]
        public void Unencrypted_WithStuffing_RoundTrip_PreservesStuffing_NoECRC()
        {
            var sis = new SpliceInfoSection
            {
                EncryptedPacket = false,
                SpliceCommand = new SpliceNullCommand(),
                AlignmentStuffing = [1, 2, 3, 4]
            };

            var rt = RoundTrip(sis);
            Assert.False(rt.EncryptedPacket);
            Assert.Equal([1, 2, 3, 4], rt.AlignmentStuffing);
            Assert.Null(rt.ECRC32);
            Assert.True(rt.Crc32Valid);
        }

        [Fact]
        public void Header_Validation_TableId_And_FixedBits()
        {
            var sis = new SpliceInfoSection
            {
                SpliceCommand = new SpliceNullCommand()
            };
            var wire = Encode(sis);

            // corrupt table_id (byte 0)
            var badTid = (byte[])wire.Clone();
            badTid[0] = 0x00;
            var d1 = new SpliceInfoSection();
            Assert.Throws<InvalidOperationException>(() => d1.Decode(badTid));

            // flip section_syntax_indicator and private_indicator (byte 1 bit 7 & 6)
            var badFlags = (byte[])wire.Clone();
            badFlags[1] |= 0xC0;
            var d2 = new SpliceInfoSection();
            Assert.Throws<InvalidOperationException>(() => d2.Decode(badFlags));
        }

        [Fact]
        public void PtsAdjustment_Is_Masked_To_33Bits_On_Encode()
        {
            var tooWide = (1UL << 40) - 1; // 40-bit value
            var sis = new SpliceInfoSection
            {
                PtsAdjustment90K = tooWide,
                SpliceCommand = new SpliceNullCommand()
            };

            var rt = RoundTrip(sis);
            Assert.Equal(tooWide & Scte35Constants.PtsMax, rt.PtsAdjustment90K);
        }

        [Fact]
        public void PayloadBytes_Equals_PayloadBytes_And_Buffer_Fits()
        {
            var sis = new SpliceInfoSection
            {
                SpliceCommand = new TimeSignalCommand { TimeSpecifiedFlag = true, PtsTime90K = 1 }
            };

            var buffer = new byte[sis.PayloadBytes];
            sis.Encode(buffer);
            Assert.Equal(sis.PayloadBytes, buffer.Length);

            var dec = new SpliceInfoSection();
            dec.Decode(buffer);
            Assert.True(dec.Crc32Valid);
            Assert.Equal(dec.PayloadBytes, dec.PayloadBytes);
        }
    }
}