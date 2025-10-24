using Scte35.Net.Constants;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests.Model
{
    public class SpliceInsertCommandTests
    {
        private static byte[] Encode(SpliceInsertCommand cmd)
        {
            var buf = new byte[cmd.PayloadBytes];
            cmd.Encode(buf);
            Assert.Equal(cmd.PayloadBytes, buf.Length);
            return buf;
        }

        private static SpliceInsertCommand RoundTrip(SpliceInsertCommand src)
        {
            var bytes = Encode(src);
            var dst = new SpliceInsertCommand();
            dst.Decode(bytes);
            return dst;
        }

        [Fact]
        public void Type_Is_Correct()
        {
            var cmd = new SpliceInsertCommand();
            Assert.Equal(SpliceCommandType.SpliceInsert, cmd.Type);
        }

        [Fact]
        public void CancelledEvent_Encodes_HeaderOnly_And_Decodes()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0x01020304,
                SpliceEventCancelIndicator = true
            };

            var bytes = Encode(cmd);

            Assert.Equal(5, bytes.Length);
            Assert.Equal([0x01, 0x02, 0x03, 0x04], bytes.Take(4).ToArray());
            Assert.Equal((byte)0xFF, bytes[4]);

            var rt = new SpliceInsertCommand();
            rt.Decode(bytes);

            Assert.Equal(0x01020304u, rt.SpliceEventId);
            Assert.True(rt.SpliceEventCancelIndicator);
        }

        [Fact]
        public void ProgramMode_Immediate_NoDuration_RoundTrip()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0xAABBCCDD,
                SpliceEventCancelIndicator = false,
                EventIdComplianceFlag = true, // on wire: bit=0
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                DurationFlag = false,
                SpliceImmediateFlag = true,
                UniqueProgramId = 0x1234,
                AvailNum = 1,
                AvailsExpected = 2
            };

            var bytes = Encode(cmd);
            // 4 + 1 + 1 + 2 + 1 + 1 = 10
            Assert.Equal(10, bytes.Length);

            var rt = new SpliceInsertCommand();
            rt.Decode(bytes);

            Assert.Equal(cmd.SpliceEventId, rt.SpliceEventId);
            Assert.False(rt.SpliceEventCancelIndicator);
            Assert.True(rt.EventIdComplianceFlag);
            Assert.True(rt.OutOfNetworkIndicator);
            Assert.True(rt.ProgramSpliceFlag);
            Assert.True(rt.SpliceImmediateFlag);
            Assert.False(rt.DurationFlag);
            Assert.Null(rt.PtsTime90K);
            Assert.Equal((ushort)0x1234, rt.UniqueProgramId);
            Assert.Equal((byte)1, rt.AvailNum);
            Assert.Equal((byte)2, rt.AvailsExpected);
        }

        [Fact]
        public void ProgramMode_WithSpliceTime_TimeSpecifiedFalse_OneByteSpliceTime()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0x0BADF00D,
                OutOfNetworkIndicator = false,
                ProgramSpliceFlag = true,
                DurationFlag = false,
                SpliceImmediateFlag = false,
                TimeSpecifiedFlag = false,
                UniqueProgramId = 0xBEEF,
                AvailNum = 3,
                AvailsExpected = 3
            };

            var bytes = Encode(cmd);

            Assert.Equal(11, bytes.Length);

            byte spliceTimeByte = bytes[6];
            Assert.Equal(0x7F, spliceTimeByte);

            var rt = RoundTrip(cmd);
            Assert.False(rt.TimeSpecifiedFlag);
            Assert.Null(rt.PtsTime90K);
        }

        [Fact]
        public void ProgramMode_WithSpliceTime_TimeSpecifiedTrue_EncodesReservedAndPts()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 123,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                DurationFlag = false,
                SpliceImmediateFlag = false,
                TimeSpecifiedFlag = true,
                PtsTime90K = 0UL, // keep it simple
                UniqueProgramId = 1,
                AvailNum = 0,
                AvailsExpected = 0
            };

            var bytes = Encode(cmd);
            // 4 + 1 + 1 + 5 + 2 + 1 + 1 = 15
            Assert.Equal(15, bytes.Length);

            Assert.Equal(0xFE, bytes[6]);
            Assert.Equal(0x00, bytes[7]);
            Assert.Equal(0x00, bytes[8]);
            Assert.Equal(0x00, bytes[9]);
            Assert.Equal(0x00, bytes[10]);

            var rt = RoundTrip(cmd);
            Assert.True(rt.TimeSpecifiedFlag);
            Assert.Equal<ulong>(0, rt.PtsTime90K!.Value);
        }

        [Fact]
        public void BreakDuration_RoundTrip_And_MaskedTo33Bits()
        {
            var big = (1UL << 40) - 1; // 40-bit number; should be masked to 33 bits
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 42,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                SpliceImmediateFlag = true, // no splice_time
                DurationFlag = true,
                Break = new SpliceInsertCommand.BreakDuration { AutoReturn = true, Duration90K = big },
                UniqueProgramId = 0x1234,
                AvailNum = 7,
                AvailsExpected = 7
            };

            var rt = RoundTrip(cmd);
            Assert.True(rt.DurationFlag);
            Assert.NotNull(rt.Break);
            Assert.True(rt.Break!.AutoReturn);
            Assert.Equal(big & Scte35Constants.PtsMax, rt.Break!.Duration90K);
        }

        [Fact]
        public void ComponentMode_MixedSpliceTimes_RoundTrip()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0x99,
                OutOfNetworkIndicator = false,
                ProgramSpliceFlag = false, // component mode
                SpliceImmediateFlag = false, // per-component splice_time()
                DurationFlag = false,
                UniqueProgramId = 0x0102,
                AvailNum = 5,
                AvailsExpected = 5
            };

            cmd.Components.Add(new SpliceInsertCommand.Component
            {
                ComponentTag = 0x11,
                TimeSpecifiedFlag = true,
                PtsTime90K = 0x12345678UL & Scte35Constants.PtsMax
            });
            cmd.Components.Add(new SpliceInsertCommand.Component
            {
                ComponentTag = 0x22,
                TimeSpecifiedFlag = false,
                PtsTime90K = null
            });

            var rt = RoundTrip(cmd);

            Assert.False(rt.ProgramSpliceFlag);
            Assert.False(rt.SpliceImmediateFlag);
            Assert.Equal(2, rt.Components.Count);

            var c0 = rt.Components[0];
            Assert.Equal((byte)0x11, c0.ComponentTag);
            Assert.True(c0.TimeSpecifiedFlag);
            Assert.Equal(0x12345678UL & Scte35Constants.PtsMax, c0.PtsTime90K!.Value);

            var c1 = rt.Components[1];
            Assert.Equal((byte)0x22, c1.ComponentTag);
            Assert.False(c1.TimeSpecifiedFlag);
            Assert.Null(c1.PtsTime90K);
        }

        [Fact]
        public void ComponentMode_Immediate_NoSpliceTimePerComponent()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0x77,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = false,
                SpliceImmediateFlag = true, // no per-component splice_time()
                DurationFlag = false,
                UniqueProgramId = 1,
                AvailNum = 0,
                AvailsExpected = 0
            };
            cmd.Components.Add(new SpliceInsertCommand.Component { ComponentTag = 1 });
            cmd.Components.Add(new SpliceInsertCommand.Component { ComponentTag = 2 });

            var rt = RoundTrip(cmd);
            Assert.True(rt.SpliceImmediateFlag);
            Assert.Equal(2, rt.Components.Count);
            Assert.False(rt.Components[0].TimeSpecifiedFlag);
            Assert.False(rt.Components[1].TimeSpecifiedFlag);
            Assert.Null(rt.Components[0].PtsTime90K);
            Assert.Null(rt.Components[1].PtsTime90K);
        }

        [Fact]
        public void ComponentMode_ZeroComponents_ThrowsOnEncode()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 3,
                ProgramSpliceFlag = false,
                SpliceImmediateFlag = false,
                DurationFlag = false,
                UniqueProgramId = 0,
                AvailNum = 0,
                AvailsExpected = 0
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => Encode(cmd));
        }

        [Fact]
        public void DurationFlagTrue_WithoutBreak_ThrowsOnEncode()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 4,
                ProgramSpliceFlag = true,
                SpliceImmediateFlag = true,
                DurationFlag = true, // break is null tho
                UniqueProgramId = 1,
                AvailNum = 1,
                AvailsExpected = 1
            };

            Assert.Throws<InvalidOperationException>(() => Encode(cmd));
        }

        [Fact]
        public void PayloadBytes_Matches_Encode_Length_InTypicalProgramCase()
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0xAA55AA55,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                DurationFlag = true,
                SpliceImmediateFlag = false,
                TimeSpecifiedFlag = true,
                PtsTime90K = 0x1_2345_6789UL & Scte35Constants.PtsMax,
                Break = new SpliceInsertCommand.BreakDuration
                {
                    AutoReturn = false,
                    Duration90K = 180 * 90000UL
                },
                UniqueProgramId = 0x7777,
                AvailNum = 9,
                AvailsExpected = 9
            };

            var bytes = Encode(cmd);
            Assert.Equal(cmd.PayloadBytes, bytes.Length);
        }

        [Fact]
        public void Decode_TrailingBytes_Throws()
        {
            var good = new SpliceInsertCommand
            {
                SpliceEventId = 1,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                SpliceImmediateFlag = true,
                DurationFlag = false,
                UniqueProgramId = 0x1234,
                AvailNum = 1,
                AvailsExpected = 1
            };
            var wire = Encode(good);
            var bad = wire.Concat(new byte[] { 0x00 }).ToArray();

            var victim = new SpliceInsertCommand();
            Assert.Throws<InvalidOperationException>(() => victim.Decode(bad));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EventIdComplianceIndicator_RoundTrips(bool compliant)
        {
            var cmd = new SpliceInsertCommand
            {
                SpliceEventId = 0xCAFEBABE,
                SpliceEventCancelIndicator = false,
                EventIdComplianceFlag = compliant,
                OutOfNetworkIndicator = false,
                ProgramSpliceFlag = true,
                DurationFlag = false,
                SpliceImmediateFlag = true,
                UniqueProgramId = 0,
                AvailNum = 0,
                AvailsExpected = 0
            };

            var rt = RoundTrip(cmd);
            Assert.Equal(compliant, rt.EventIdComplianceFlag);
        }
    }
}