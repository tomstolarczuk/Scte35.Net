using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand
{
    public sealed class SpliceInsertCommand : ISpliceCommand
    {
        public SpliceCommandType Type => SpliceCommandType.SpliceInsert;
        public uint SpliceEventId { get; set; }
        public bool SpliceEventCancelIndicator { get; set; }
        public bool EventIdComplianceFlag { get; set; } = true;
        public bool OutOfNetworkIndicator { get; set; }
        public bool ProgramSpliceFlag { get; set; } = true;
        public bool DurationFlag { get; set; }
        public bool SpliceImmediateFlag { get; set; }
        public bool TimeSpecifiedFlag { get; set; }
        public ulong? PtsTime90K { get; set; }
        public IList<Component> Components { get; } = new List<Component>();
        public BreakDuration? Break { get; set; }
        public ushort UniqueProgramId { get; set; }
        public byte AvailNum { get; set; }
        public byte AvailsExpected { get; set; }

        public int PayloadBytes => ComputePayloadBytes();

        private int ComputePayloadBytes()
        {
            int bytes = 0;
            bytes += 4; // splice_event_id
            bytes += 1; // cancel + 7 reserved

            if (SpliceEventCancelIndicator)
                return bytes;

            bytes += 1; // flag byte: 5 flags + 3 reserved

            if (ProgramSpliceFlag)
            {
                if (!SpliceImmediateFlag)
                {
                    // splice_time(): 1 + (6 + 33) or 1 + 7
                    bytes += TimeSpecifiedFlag ? 5 : 1;
                }
            }
            else
            {
                bytes += 1; // component_count
                bytes += Components.Count; // component_tag (1 each)
                if (!SpliceImmediateFlag)
                {
                    // each component has a splice_time() (time_specified_flag may be 0/1)
                    foreach (var c in Components)
                        bytes += c.TimeSpecifiedFlag ? 5 : 1;
                }
            }

            if (DurationFlag)
                bytes += 5; // break_duration(): 1 + 6 + 33

            bytes += 2; // unique_program_id
            bytes += 1; // avail_num
            bytes += 1; // avails_expected

            return bytes;
        }

        public void Encode(Span<byte> dest)
        {
            PayloadValidator.RequireMinLength(dest, PayloadBytes);
            var w = new BitWriter(dest);

            // header
            w.WriteUInt32(SpliceEventId);
            w.WriteBit(SpliceEventCancelIndicator);
            w.WriteBits(Scte35Constants.Reserved, 7);

            if (!SpliceEventCancelIndicator)
            {
                // flags (+ event_id_compliance_flag + 3 reserved)
                w.WriteBit(OutOfNetworkIndicator);
                w.WriteBit(ProgramSpliceFlag);
                w.WriteBit(DurationFlag);
                w.WriteBit(SpliceImmediateFlag);
                w.WriteBit(!EventIdComplianceFlag); // 0 => compliant
                w.WriteBits(Scte35Constants.Reserved, 3);

                // splice_time()
                if (ProgramSpliceFlag)
                {
                    if (!SpliceImmediateFlag)
                        WriteSpliceTime(ref w, TimeSpecifiedFlag, PtsTime90K);
                }
                else
                {
                    PayloadValidator.RequireRange(Components.Count, 1, 0xFF);
                    w.WriteBits((uint)Components.Count, 8);
                    foreach (var c in Components)
                    {
                        w.WriteBits(c.ComponentTag, 8);
                        if (!SpliceImmediateFlag)
                            WriteSpliceTime(ref w, c.TimeSpecifiedFlag, c.PtsTime90K);
                    }
                }

                if (DurationFlag)
                {
                    (Break ?? throw new InvalidOperationException(
                            "BreakDuration must be provided when DurationFlag==true."))
                        .Encode(ref w);
                }

                w.WriteUInt16(UniqueProgramId);
                w.WriteBits(AvailNum, 8);
                w.WriteBits(AvailsExpected, 8);
            }

            if (w.BitsWritten != PayloadBytes * 8)
                throw new InvalidOperationException("splice_insert payload size mismatch.");
        }

        public void Decode(ReadOnlySpan<byte> data)
        {
            var r = new BitReader(data);

            SpliceEventId = r.ReadUInt32();
            SpliceEventCancelIndicator = r.ReadBit();
            r.SkipBits(7); // reserved

            if (!SpliceEventCancelIndicator)
            {
                OutOfNetworkIndicator = r.ReadBit();
                ProgramSpliceFlag = r.ReadBit();
                DurationFlag = r.ReadBit();
                SpliceImmediateFlag = r.ReadBit();
                EventIdComplianceFlag = !r.ReadBit(); // 0 => compliant
                r.SkipBits(3); // reserved

                if (ProgramSpliceFlag)
                {
                    if (!SpliceImmediateFlag)
                    {
                        ReadSpliceTime(ref r, out var tsf, out var pts);
                        TimeSpecifiedFlag = tsf;
                        PtsTime90K = pts;
                    }
                    else
                    {
                        TimeSpecifiedFlag = false;
                        PtsTime90K = null;
                    }
                }
                else
                {
                    int componentCount = (int)r.ReadBits(8);
                    PayloadValidator.RequireRange(componentCount, 1, 0xFF);
                    Components.Clear();

                    for (int i = 0; i < componentCount; i++)
                    {
                        var tag = r.ReadByte();
                        bool tsf = false;
                        ulong? pts = null;

                        if (!SpliceImmediateFlag)
                            ReadSpliceTime(ref r, out tsf, out pts);

                        Components.Add(new Component
                        {
                            ComponentTag = tag,
                            TimeSpecifiedFlag = tsf,
                            PtsTime90K = pts
                        });
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

            if (r.BitsRemaining != 0)
                throw new InvalidOperationException("Trailing bits present in splice_insert payload.");
        }


        private static void WriteSpliceTime(ref BitWriter w, bool timeSpecified, ulong? pts90K)
        {
            w.WriteBit(timeSpecified);
            if (timeSpecified)
            {
                w.WriteBits(Scte35Constants.Reserved, 6);
                ulong pts = (pts90K ?? 0UL) & Scte35Constants.PtsMax;
                w.WritePts33(pts);
            }
            else
            {
                w.WriteBits(Scte35Constants.Reserved, 7);
            }
        }

        private static void ReadSpliceTime(ref BitReader r, out bool timeSpecified, out ulong? pts90K)
        {
            timeSpecified = r.ReadBit();
            if (timeSpecified)
            {
                r.SkipBits(6);
                pts90K = r.ReadPts33();
            }
            else
            {
                r.SkipBits(7);
                pts90K = null;
            }
        }

        public sealed class Component
        {
            public byte ComponentTag { get; set; }
            public bool TimeSpecifiedFlag { get; set; }
            public ulong? PtsTime90K { get; set; }
        }

        public sealed class BreakDuration
        {
            public bool AutoReturn { get; set; }
            public ulong Duration90K { get; set; }

            internal void Encode(ref BitWriter w)
            {
                w.WriteBit(AutoReturn);
                w.WriteBits(Scte35Constants.Reserved, 6);
                w.WritePts33(Duration90K & Scte35Constants.PtsMax);
            }

            internal void Decode(ref BitReader r)
            {
                AutoReturn = r.ReadBit();
                r.SkipBits(6);
                Duration90K = r.ReadPts33();
            }
        }
    }
}