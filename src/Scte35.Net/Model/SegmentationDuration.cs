using Scte35.Net.Constants;

namespace Scte35.Net.Model;

public readonly struct SegmentationDuration(ulong ticks90K)
{
    public ulong Ticks90K { get; } = ticks90K;
    public TimeSpan ToTimeSpan() => TimeSpan.FromSeconds(Ticks90K / Scte35Constants.TicksPerSecond);
}