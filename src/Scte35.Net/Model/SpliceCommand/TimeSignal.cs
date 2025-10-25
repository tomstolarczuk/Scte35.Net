using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand;

public sealed class TimeSignalCommand : ISpliceCommand
{
    public SpliceCommandType Type => SpliceCommandType.TimeSignal;
    public bool TimeSpecifiedFlag { get; set; }
    public ulong? PtsTime90K { get; set; }
    public int PayloadBytes => TimeSpecifiedFlag ? 5 : 1;

    public void Encode(Span<byte> dest)
    {
        PayloadValidator.RequireMinLength(dest, PayloadBytes);

        var w = new BitWriter(dest);

        w.WriteBit(TimeSpecifiedFlag);

        if (TimeSpecifiedFlag)
        {
            w.WriteBits(Scte35Constants.Reserved, 6);

            ulong pts = (PtsTime90K ?? 0UL) & Scte35Constants.PtsMax;
            w.WritePts33(pts);
        }
        else
        {
            w.WriteBits(Scte35Constants.Reserved, 7);
        }

        if (w.BitsWritten != PayloadBytes * 8)
            throw new InvalidOperationException("time_signal payload size mismatch.");
    }

    public void Decode(ReadOnlySpan<byte> data)
    {
        PayloadValidator.RequireMinLength(data, 1);

        var r = new BitReader(data);

        TimeSpecifiedFlag = r.ReadBit();

        if (TimeSpecifiedFlag)
        {
            PayloadValidator.RequireExactLength(data, 5);

            r.SkipBits(6);
            PtsTime90K = r.ReadPts33();
        }
        else
        {
            r.SkipBits(7);
            PayloadValidator.RequireExactLength(data, 1);
            PtsTime90K = null;
        }

        if (r.BitsRemaining != 0)
            throw new InvalidOperationException("Trailing bits present in time_signal payload.");
    }
}