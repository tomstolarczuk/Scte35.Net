using Scte35.Net.Constants;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;

namespace Scte35.Net.Tests.Model
{
    public class SpliceScheduleCommandTests
    {
        private static byte[] Encode(SpliceScheduleCommand cmd)
        {
            var buf = new byte[cmd.PayloadBytes];
            cmd.Encode(buf);
            return buf;
        }

        private static SpliceScheduleCommand RoundTrip(SpliceScheduleCommand src)
        {
            var bytes = Encode(src);
            var dst = new SpliceScheduleCommand();
            dst.Decode(bytes);
            return dst;
        }

        [Fact]
        public void Type_And_EmptySchedule_Length()
        {
            var cmd = new SpliceScheduleCommand();
            Assert.Equal(SpliceCommandType.SpliceSchedule, cmd.Type);
            Assert.Equal(1, cmd.PayloadBytes);
        }

        [Fact]
        public void Encode_OneProgramEvent_NoDuration_KnownBytes()
        {
            var e = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 0x11223344,
                SpliceEventCancelIndicator = false,
                EventIdComplianceFlag = true, // on-wire: flag = 0
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                DurationFlag = false,
                UtcSpliceTime = 0x01020304,
                UniqueProgramId = 0x0A0B,
                AvailNum = 0x0C,
                AvailsExpected = 0x0D
            };
            var cmd = new SpliceScheduleCommand();
            cmd.Events.Add(e);

            var bytes = Encode(cmd);

            var expected = new byte[]
            {
                0x01, // splice_count
                0x11, 0x22, 0x33, 0x44, // splice_event_id
                0b_0_0_111111, // cancel=0, event_id_compliance_flag=0, reserved(6)=all-ones => 0x7F
                0b_1_1_0_11111, // out_of_network=1, program=1, duration=0, reserved(5)=all-ones => 0xE0 | 0x1F = 0xFF
                0x01, 0x02, 0x03, 0x04, // utc_splice_time
                0x0A, 0x0B, // unique_program_id
                0x0C, // avail_num
                0x0D // avails_expected
            };

            Assert.Equal(expected, bytes);
        }

        [Fact]
        public void RoundTrip_ProgramEvent_WithDuration()
        {
            var e = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 0xAABBCCDD,
                OutOfNetworkIndicator = false,
                ProgramSpliceFlag = true,
                DurationFlag = true,
                UtcSpliceTime = 0x01020304,
                Break = new SpliceScheduleCommand.BreakDuration
                {
                    AutoReturn = true,
                    Duration90K = 10800000UL // 120s (120*90000)
                },
                UniqueProgramId = 0x1234,
                AvailNum = 1,
                AvailsExpected = 2
            };
            var cmd = new SpliceScheduleCommand();
            cmd.Events.Add(e);

            var rt = RoundTrip(cmd);
            Assert.Single(rt.Events);
            var r = rt.Events[0];

            Assert.Equal(e.SpliceEventId, r.SpliceEventId);
            Assert.False(r.SpliceEventCancelIndicator);
            Assert.True(r.EventIdComplianceFlag);
            Assert.False(r.OutOfNetworkIndicator);
            Assert.True(r.ProgramSpliceFlag);
            Assert.True(r.DurationFlag);
            Assert.Equal(e.UtcSpliceTime, r.UtcSpliceTime);
            Assert.NotNull(r.Break);
            Assert.True(r.Break!.AutoReturn);
            Assert.Equal(e.Break!.Duration90K & Scte35Constants.PtsMax, r.Break!.Duration90K);
            Assert.Equal<ushort>(0x1234, r.UniqueProgramId);
            Assert.Equal<byte>(1, r.AvailNum);
            Assert.Equal<byte>(2, r.AvailsExpected);
        }

        [Fact]
        public void Encode_ComponentMode_TwoComponents()
        {
            var ev = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 0x00000001,
                ProgramSpliceFlag = false,
                DurationFlag = false,
                OutOfNetworkIndicator = true,
                UniqueProgramId = 0x2222,
                AvailNum = 3,
                AvailsExpected = 4
            };
            ev.Components.Add(new SpliceScheduleCommand.Component { ComponentTag = 0xAA, UtcSpliceTime = 0x01020304 });
            ev.Components.Add(new SpliceScheduleCommand.Component { ComponentTag = 0xBB, UtcSpliceTime = 0x11121314 });

            var cmd = new SpliceScheduleCommand();
            cmd.Events.Add(ev);

            var bytes = Encode(cmd);
            var dec = new SpliceScheduleCommand();
            dec.Decode(bytes);

            var r = dec.Events.Single();
            Assert.False(r.ProgramSpliceFlag);
            Assert.Equal(2, r.Components.Count);
            Assert.Equal((byte)0xAA, r.Components[0].ComponentTag);
            Assert.Equal((uint)0x01020304, r.Components[0].UtcSpliceTime);
            Assert.Equal((byte)0xBB, r.Components[1].ComponentTag);
            Assert.Equal((uint)0x11121314, r.Components[1].UtcSpliceTime);
            Assert.Equal<ushort>(0x2222, r.UniqueProgramId);
            Assert.Equal<byte>(3, r.AvailNum);
            Assert.Equal<byte>(4, r.AvailsExpected);
        }

        [Fact]
        public void Encode_ComponentMode_ZeroComponents_Throws()
        {
            var ev = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 7,
                ProgramSpliceFlag = false,
                DurationFlag = false,
                UniqueProgramId = 1,
                AvailNum = 1,
                AvailsExpected = 1
            };
            var cmd = new SpliceScheduleCommand();
            cmd.Events.Add(ev);

            Assert.Throws<ArgumentOutOfRangeException>(() => Encode(cmd));
        }

        [Fact]
        public void Encode_CancelledEvent_WritesOnlyHeader()
        {
            var ev = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 0xDEADBEEF,
                SpliceEventCancelIndicator = true,
                EventIdComplianceFlag = true // on-wire: 0
            };
            var cmd = new SpliceScheduleCommand();
            cmd.Events.Add(ev);

            var bytes = Encode(cmd);

            Assert.Equal(1 + 4 + 1, bytes.Length); // splice_count + id + header byte
            Assert.Equal((byte)0x01, bytes[0]); // splice_count
            Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], bytes.Skip(1).Take(4).ToArray());
            Assert.Equal((byte)0xBF, bytes[5]); // 0b_1011_1111, compliance_flag=0, cancel=1
        }

        [Fact]
        public void Decode_TrailingBytes_Throws()
        {
            var ev = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 1,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                UtcSpliceTime = 5,
                DurationFlag = false,
                UniqueProgramId = 0x1234,
                AvailNum = 1,
                AvailsExpected = 1
            };
            var cmd = new SpliceScheduleCommand();
            cmd.Events.Add(ev);

            var good = Encode(cmd);
            var bad = good.Concat(new byte[] { 0x00 }).ToArray();

            var victim = new SpliceScheduleCommand();
            Assert.Throws<InvalidOperationException>(() => victim.Decode(bad));
        }

        [Fact]
        public void BreakDuration_MasksTo33Bits()
        {
            var tooWide = (1UL << 40) - 1; // 40-bit
            var ev = new SpliceScheduleCommand.Event
            {
                SpliceEventId = 9,
                OutOfNetworkIndicator = true,
                ProgramSpliceFlag = true,
                UtcSpliceTime = 10,
                DurationFlag = true,
                Break = new SpliceScheduleCommand.BreakDuration { AutoReturn = false, Duration90K = tooWide },
                UniqueProgramId = 1,
                AvailNum = 2,
                AvailsExpected = 3
            };
            var rt = RoundTrip(new SpliceScheduleCommand { Events = { ev } });

            var rtev = rt.Events[0];
            Assert.True(rtev.DurationFlag);
            Assert.NotNull(rtev.Break);
            Assert.Equal(tooWide & Scte35Constants.PtsMax, rtev.Break!.Duration90K);
        }
    }
}