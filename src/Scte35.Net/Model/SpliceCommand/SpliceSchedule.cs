using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand
{
    public sealed class SpliceScheduleCommand : IBinarySerializable
    {
        public SpliceCommandType Type => SpliceCommandType.SpliceSchedule;
        public IList<Event> Events { get; } = new List<Event>();
        public int PayloadBytes
        {
            get
            {
                int bytes = 1;
                foreach (var e in Events)
                    bytes += e.PayloadBytes;
                return bytes;
            }
        }

        public void Encode(Span<byte> dest)
        {
            PayloadValidator.RequireMinLength(dest, PayloadBytes);

            var w = new BitWriter(dest);

            PayloadValidator.RequireRange(Events.Count, 0, 0xFF);
            w.WriteByte((byte)Events.Count);

            foreach (var e in Events)
                e.Encode(ref w);

            if (w.BitsWritten != PayloadBytes * 8)
                throw new InvalidOperationException("splice_schedule payload size mismatch.");
        }

        public void Decode(ReadOnlySpan<byte> data)
        {
            var r = new BitReader(data);

            int count = r.ReadByte();
            
            Events.Clear();
            for (int i = 0; i < count; i++)
            {
                var e = new Event();
                e.Decode(ref r);
                Events.Add(e);
            }

            if (r.BitsRemaining != 0)
                throw new InvalidOperationException("Trailing bits present in splice_schedule payload.");
        }

        public sealed class Event
        {
            public uint SpliceEventId { get; set; }
            public bool SpliceEventCancelIndicator { get; set; }
            public bool EventIdComplianceFlag { get; set; } = true;
            public bool OutOfNetworkIndicator { get; set; }
            public bool ProgramSpliceFlag { get; set; } = true;
            public bool DurationFlag { get; set; }
            public uint? UtcSpliceTime { get; set; }
            public IList<Component> Components { get; } = new List<Component>();
            public BreakDuration? Break { get; set; }
            public ushort UniqueProgramId { get; set; }
            public byte AvailNum { get; set; }
            public byte AvailsExpected { get; set; }

            public int PayloadBytes
            {
                get
                {
                    int bytes = 0;
                    bytes += 4; // splice_event_id
                    bytes += 1; // cancel + event_id_compliance_flag + 6 reserved

                    if (SpliceEventCancelIndicator) return bytes;

                    bytes += 1; // out_of_network + program_splice + duration_flag + 5 reserved

                    if (ProgramSpliceFlag)
                    {
                        bytes += 4; // utc_splice_time
                    }
                    else
                    {
                        bytes += 1; // component_count
                        bytes += Components.Count * (1 + 4); // tag + utc_splice_time
                    }

                    if (DurationFlag)
                        bytes += 5; // break_duration(): 1+6+33 bits = 5 bytes

                    bytes += 2; // unique_program_id
                    bytes += 1; // avail_num
                    bytes += 1; // avails_expected

                    return bytes;
                }
            }

            internal void Encode(ref BitWriter w)
            {
                // header
                w.WriteUInt32(SpliceEventId);
                w.WriteBit(SpliceEventCancelIndicator);
                w.WriteBit(!EventIdComplianceFlag); // 0 => compliant, 1 => not specified
                w.WriteBits(Scte35Constants.Reserved, 6);

                if (SpliceEventCancelIndicator)
                    return;

                // flags
                w.WriteBit(OutOfNetworkIndicator);
                w.WriteBit(ProgramSpliceFlag);
                w.WriteBit(DurationFlag);
                w.WriteBits(Scte35Constants.Reserved, 5);

                if (ProgramSpliceFlag)
                {
                    w.WriteUInt32(UtcSpliceTime ?? 0U);
                }
                else
                {
                    PayloadValidator.RequireRange(Components.Count, 1, 0xFF);
                    w.WriteByte((byte)Components.Count);

                    foreach (var c in Components)
                    {
                        w.WriteByte(c.ComponentTag);
                        w.WriteUInt32(c.UtcSpliceTime);
                    }
                }

                if (DurationFlag)
                {
                    var b = Break ??
                            throw new InvalidOperationException("BreakDuration required when DurationFlag==true.");
                    b.Encode(ref w);
                }

                w.WriteUInt16(UniqueProgramId);
                w.WriteByte(AvailNum);
                w.WriteByte(AvailsExpected);
            }

            internal void Decode(ref BitReader r)
            {
                SpliceEventId = r.ReadUInt32();
                SpliceEventCancelIndicator = r.ReadBit();
                EventIdComplianceFlag = !r.ReadBit();
                r.SkipBits(6);

                if (SpliceEventCancelIndicator)
                    return;

                OutOfNetworkIndicator = r.ReadBit();
                ProgramSpliceFlag = r.ReadBit();
                DurationFlag = r.ReadBit();
                r.SkipBits(5);

                if (ProgramSpliceFlag)
                {
                    UtcSpliceTime = r.ReadUInt32();
                }
                else
                {
                    int componentCount = r.ReadByte();
                    
                    PayloadValidator.RequireRange(componentCount, 1, 0xFF);
                    
                    Components.Clear();
                    for (int j = 0; j < componentCount; j++)
                    {
                        byte tag = r.ReadByte();
                        uint utc = r.ReadUInt32();
                        Components.Add(new Component { ComponentTag = tag, UtcSpliceTime = utc });
                    }
                }

                if (DurationFlag)
                {
                    var b = new BreakDuration();
                    b.Decode(ref r);
                    Break = b;
                }

                UniqueProgramId = r.ReadUInt16();
                AvailNum = r.ReadByte();
                AvailsExpected = r.ReadByte();
            }
        }

        public sealed class Component
        {
            public byte ComponentTag { get; set; }

            public uint
                UtcSpliceTime
            {
                get;
                set;
            }
        }

        public sealed class BreakDuration
        {
            public bool AutoReturn { get; set; }

            public ulong
                Duration90K
            {
                get;
                set;
            }

            internal void Encode(ref BitWriter w)
            {
                w.WriteBit(AutoReturn);
                w.WriteBits(Scte35Constants.Reserved, 6);
                w.WriteBits64(Duration90K & Scte35Constants.PtsMax, 33);
            }

            internal void Decode(ref BitReader r)
            {
                AutoReturn = r.ReadBit();
                r.SkipBits(6);
                Duration90K = r.ReadBits64(33);
            }
        }
    }
}