namespace Scte35.Net.Model;

public sealed class SegmentationDescriptorComponent
{
    public byte ComponentTag { get; set; }
    public ulong PtsOffset90K { get; set; }
}