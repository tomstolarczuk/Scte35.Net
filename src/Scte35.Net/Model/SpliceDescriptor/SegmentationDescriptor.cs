using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor;

public sealed class SegmentationDescriptor : ISpliceDescriptor
{
    public SpliceDescriptorTag Tag => SpliceDescriptorTag.Segmentation;

    public uint SegmentationEventId { get; set; }
    public bool SegmentationEventCancelIndicator { get; set; }
    public bool SegmentationEventIdComplianceIndicator { get; set; }
    public bool ProgramSegmentationFlag { get; set; }
    public bool SegmentationDurationFlag { get; set; }
    public bool DeliveryNotRestrictedFlag { get; set; }
    public DeliveryRestrictions? DeliveryRestrictions { get; set; }
    public IList<SegmentationDescriptorComponent> Components { get; } = new List<SegmentationDescriptorComponent>();
    public SegmentationDuration? SegmentationDuration { get; set; }
    public byte[] SegmentationUpidBytes { get; set; } = [];
    public SegmentationUPIDType SegmentationUpidType { get; set; }
    public SegmentationUpid? Upid { get; set; }
    public SegmentationType SegmentationTypeId { get; set; }
    public byte SegmentNum { get; set; }
    public byte SegmentsExpected { get; set; }
    public byte? SubSegmentNum { get; set; }
    public byte? SubSegmentsExpected { get; set; }

    public int PayloadBytes => ComputePayloadBytes();

    private bool TypeAllowsSubSegments(SegmentationType type) =>
        type is SegmentationType.ProviderPOStart
            or SegmentationType.DistributorPOStart
            or SegmentationType.ProviderOverlayPOStart
            or SegmentationType.DistributorOverlayPOStart
            or SegmentationType.ProviderPromoStart
            or SegmentationType.DistributorPromoStart
            or SegmentationType.ProviderAdBlockStart
            or SegmentationType.DistributorAdBlockStart;

    private int ComputePayloadBytes()
    {
        int bytes = 0;

        // cue_identifier + segmentation_event_id + cancel/reserved
        bytes += 4; // identifier
        bytes += 4; // event id
        bytes += 1; // cancel + 7 reserved

        if (SegmentationEventCancelIndicator)
            return bytes;

        bytes += 1; // flags + restrictions (all packed into one byte)

        if (!ProgramSegmentationFlag)
        {
            bytes += 1; // component_count
            bytes += Components.Count * (1 + 5); // per component: tag (1) + (7r + 33 pts) = 5 bytes
        }

        if (SegmentationDurationFlag)
            bytes += 5; // 40-bit duration

        // UPID header + payload
        bytes += 1; // type
        bytes += 1; // length
        bytes += SegmentationUpidBytes.Length;

        // type id + numbering
        bytes += 3; // segmentation_type_id + segment_num + segments_expected

        if (SubSegmentNum.HasValue && SubSegmentsExpected.HasValue)
            bytes += 2;

        return bytes;
    }

    public void Encode(Span<byte> dest)
    {
        PayloadValidator.RequireMinLength(dest, PayloadBytes);

        var w = new BitWriter(dest);

        w.WriteUInt32(Scte35Constants.CueIdentifier);

        w.WriteUInt32(SegmentationEventId);
        w.WriteBit(SegmentationEventCancelIndicator);
        w.WriteBit(!SegmentationEventIdComplianceIndicator);
        w.WriteBits(Scte35Constants.Reserved, 6);

        if (SegmentationEventCancelIndicator)
        {
            if (w.BitsWritten != PayloadBytes * 8)
                throw new InvalidOperationException("Payload size mismatch (cancelled).");
            return;
        }

        w.WriteBit(ProgramSegmentationFlag);
        w.WriteBit(SegmentationDurationFlag);
        w.WriteBit(DeliveryNotRestrictedFlag);

        if (!DeliveryNotRestrictedFlag)
        {
            var d = DeliveryRestrictions ??
                    throw new InvalidOperationException(
                        "DeliveryRestrictions required when DeliveryNotRestrictedFlag == false.");
            w.WriteBit(d.WebDeliveryAllowedFlag);
            w.WriteBit(d.NoRegionalBlackoutFlag);
            w.WriteBit(d.ArchiveAllowedFlag);
            w.WriteBits((uint)d.DeviceRestrictions, 2);
        }
        else
        {
            w.WriteBits(Scte35Constants.Reserved, 5);
        }

        if (!ProgramSegmentationFlag)
        {
            PayloadValidator.RequireRange(Components.Count, 0, 0xFF);
            w.WriteByte((byte)Components.Count);

            foreach (var c in Components)
            {
                w.WriteByte(c.ComponentTag);
                w.WriteBits(Scte35Constants.Reserved, 7);
                ulong pts = c.PtsOffset90K & Scte35Constants.PtsMax; // 33 bits
                w.WriteBits64(pts, 33);
            }
        }

        if (SegmentationDurationFlag)
        {
            ulong dur = (SegmentationDuration?.Ticks90K ?? 0UL) & 0xFFFFFFFFFFUL; // 40 bits
            w.WriteBits64(dur, 40);
        }

        w.WriteByte((byte)SegmentationUpidType);
        PayloadValidator.RequireRange(SegmentationUpidBytes.Length, 0, 255);
        w.WriteByte((byte)SegmentationUpidBytes.Length);
        if (SegmentationUpidBytes.Length > 0)
            w.WriteBytesAligned(SegmentationUpidBytes);

        w.WriteByte((byte)SegmentationTypeId);
        w.WriteByte(SegmentNum);
        w.WriteByte(SegmentsExpected);

        bool allowSub = TypeAllowsSubSegments((SegmentationType)SegmentationTypeId);

        if (SubSegmentNum.HasValue || SubSegmentsExpected.HasValue)
        {
            if (!SubSegmentNum.HasValue || !SubSegmentsExpected.HasValue)
                throw new InvalidOperationException(
                    "Both SubSegmentNum and SubSegmentsExpected must be set together.");

            if (!allowSub)
                throw new InvalidOperationException(
                    $"Sub-segment fields are not allowed for segmentation_type_id 0x{(byte)SegmentationTypeId:X2} ({SegmentationTypeId}).");

            w.WriteByte(SubSegmentNum.Value);
            w.WriteByte(SubSegmentsExpected.Value);
        }

        if (w.BitsWritten != PayloadBytes * 8)
            throw new InvalidOperationException("Payload size mismatch.");
    }

    public void Decode(ReadOnlySpan<byte> data)
    {
        var r = new BitReader(data);

        DescriptorDecoding.RequireCueIdentifier(ref r);

        SegmentationEventId = r.ReadUInt32();
        SegmentationEventCancelIndicator = r.ReadBit();
        SegmentationEventIdComplianceIndicator = !r.ReadBit();
        r.SkipBits(6);

        if (SegmentationEventCancelIndicator)
        {
            if (r.BitsRemaining != 0)
                throw new InvalidOperationException("Trailing data present on cancelled segmentation_descriptor.");
            return;
        }

        ProgramSegmentationFlag = r.ReadBit();
        SegmentationDurationFlag = r.ReadBit();
        DeliveryNotRestrictedFlag = r.ReadBit();

        if (!DeliveryNotRestrictedFlag)
        {
            DeliveryRestrictions = new DeliveryRestrictions
            {
                WebDeliveryAllowedFlag = r.ReadBit(),
                NoRegionalBlackoutFlag = r.ReadBit(),
                ArchiveAllowedFlag = r.ReadBit(),
                DeviceRestrictions = (DeviceRestrictions)r.ReadBits(2)
            };
        }
        else
        {
            r.SkipBits(5);
        }

        if (!ProgramSegmentationFlag)
        {
            int componentCount = r.ReadByte();
            for (int i = 0; i < componentCount; i++)
            {
                byte tag = r.ReadByte();
                r.SkipBits(7);
                ulong pts = r.ReadBits64(33);
                Components.Add(new SegmentationDescriptorComponent
                {
                    ComponentTag = tag,
                    PtsOffset90K = pts
                });
            }
        }

        if (SegmentationDurationFlag)
        {
            ulong dur = r.ReadBits64(40);
            SegmentationDuration = new SegmentationDuration(dur);
        }

        SegmentationUpidType = (SegmentationUPIDType)r.ReadByte();
        int upidLen = r.ReadByte();
        PayloadValidator.RequireRange(upidLen, 0, r.BitsRemaining / 8);
        SegmentationUpidBytes = upidLen > 0 ? r.ReadBytesAligned(upidLen).ToArray() : [];

        // parse into a high-level representation (tolerate failures)
        try
        {
            Upid = SegmentationUpid.FromBytes(SegmentationUpidType, SegmentationUpidBytes);
        }
        catch
        {
            Upid = null;
        }

        SegmentationTypeId = (SegmentationType)r.ReadByte();
        SegmentNum = r.ReadByte();
        SegmentsExpected = r.ReadByte();

        bool allowSub = TypeAllowsSubSegments(SegmentationTypeId);
        if (allowSub && r.BitsRemaining == 16)
        {
            SubSegmentNum = r.ReadByte();
            SubSegmentsExpected = r.ReadByte();
        }
        else if (r.BitsRemaining != 0)
        {
            throw new InvalidOperationException("Trailing data present in segmentation_descriptor.");
        }
    }
}