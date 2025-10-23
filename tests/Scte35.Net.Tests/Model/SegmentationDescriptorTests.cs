using Scte35.Net.Constants;
using Scte35.Net.Model;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
    public class SegmentationDescriptorTests
    {
        private static byte[] Encode(SegmentationDescriptor d)
        {
            var buf = new byte[d.PayloadBytes];
            d.Encode(buf);
            Assert.Equal(d.PayloadBytes, buf.Length); // sanity
            return buf;
        }

        private static SegmentationDescriptor RoundTrip(SegmentationDescriptor src)
        {
            var bytes = Encode(src);
            var dst = new SegmentationDescriptor();
            dst.Decode(bytes);
            return dst;
        }

        [Fact]
        public void RoundTrip_ProgramMode_WithDuration_UpidNone_NoRestrictions()
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 0xAABBCCDD,
                SegmentationEventCancelIndicator = false,
                SegmentationEventIdComplianceIndicator = true, // should invert to bit 0 on wire
                ProgramSegmentationFlag = true,
                SegmentationDurationFlag = true,
                DeliveryNotRestrictedFlag = true,
                SegmentationDuration90K = 1234567890UL,
                SegmentationUpidType = SegmentationUPIDType.NotUsed,
                SegmentationUpidBytes = [],
                SegmentationTypeId = SegmentationType.ProgramStart,
                SegmentNum = 1,
                SegmentsExpected = 3
            };

            var rt = RoundTrip(d);

            Assert.Equal(d.SegmentationEventId, rt.SegmentationEventId);
            Assert.Equal(d.SegmentationEventCancelIndicator, rt.SegmentationEventCancelIndicator);
            Assert.Equal(d.SegmentationEventIdComplianceIndicator, rt.SegmentationEventIdComplianceIndicator);
            Assert.True(rt.ProgramSegmentationFlag);
            Assert.True(rt.SegmentationDurationFlag);
            Assert.True(rt.DeliveryNotRestrictedFlag);
            Assert.Equal(d.SegmentationDuration90K, rt.SegmentationDuration90K);
            Assert.Equal(d.SegmentationUpidType, rt.SegmentationUpidType);
            Assert.Equal(d.SegmentationUpidBytes, rt.SegmentationUpidBytes);
            Assert.Equal(d.SegmentationTypeId, rt.SegmentationTypeId);
            Assert.Equal(d.SegmentNum, rt.SegmentNum);
            Assert.Equal(d.SegmentsExpected, rt.SegmentsExpected);
            Assert.Null(rt.SubSegmentNum);
            Assert.Null(rt.SubSegmentsExpected);
            Assert.Empty(rt.Components);
        }

        [Fact]
        public void RoundTrip_CancelledEvent_EncodesMinimalPayload()
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 0x01020304,
                SegmentationEventCancelIndicator = true,
                SegmentationEventIdComplianceIndicator = false, // on wire -> 1
                // all other fields ignored when cancelled
            };

            var bytes = Encode(d);
            Assert.Equal(9, bytes.Length);

            var rt = new SegmentationDescriptor();
            rt.Decode(bytes);

            Assert.True(rt.SegmentationEventCancelIndicator);
            Assert.Equal(0x01020304u, rt.SegmentationEventId);
            Assert.False(rt.SegmentationEventIdComplianceIndicator);
        }

        [Fact]
        public void RoundTrip_WithDeliveryRestrictions_ComponentMode_With33BitPtsEdges()
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 0xDEADBEEF,
                SegmentationEventCancelIndicator = false,
                SegmentationEventIdComplianceIndicator = true,
                ProgramSegmentationFlag = false, // component mode
                SegmentationDurationFlag = false,
                DeliveryNotRestrictedFlag = false,
                DeliveryRestrictions = new DeliveryRestrictions
                {
                    WebDeliveryAllowedFlag = true,
                    NoRegionalBlackoutFlag = false,
                    ArchiveAllowedFlag = true,
                    DeviceRestrictions = (DeviceRestrictions)2 // arbitrary 2-bit value
                },
                SegmentationUpidType = SegmentationUPIDType.NotUsed,
                SegmentationUpidBytes = [],
                SegmentationTypeId = SegmentationType.BreakStart,
                SegmentNum = 7,
                SegmentsExpected = 7
            };

            d.Components.Add(new SegmentationDescriptorComponent
            {
                ComponentTag = 0x11,
                PtsOffset90K = 0UL
            });
            d.Components.Add(new SegmentationDescriptorComponent
            {
                ComponentTag = 0x22,
                PtsOffset90K = Scte35Constants.PtsMax // 33-bit max (0x1FFFFFFFF)
            });

            var rt = RoundTrip(d);

            Assert.False(rt.ProgramSegmentationFlag);
            Assert.False(rt.SegmentationDurationFlag);
            Assert.False(rt.DeliveryNotRestrictedFlag);
            Assert.NotNull(rt.DeliveryRestrictions);
            Assert.True(rt.DeliveryRestrictions!.WebDeliveryAllowedFlag);
            Assert.False(rt.DeliveryRestrictions.NoRegionalBlackoutFlag);
            Assert.True(rt.DeliveryRestrictions.ArchiveAllowedFlag);
            Assert.Equal((DeviceRestrictions)2, rt.DeliveryRestrictions.DeviceRestrictions);

            Assert.Equal(2, rt.Components.Count);
            Assert.Equal((byte)0x11, rt.Components[0].ComponentTag);
            Assert.Equal(0UL, rt.Components[0].PtsOffset90K);
            Assert.Equal((byte)0x22, rt.Components[1].ComponentTag);
            Assert.Equal(Scte35Constants.PtsMax, rt.Components[1].PtsOffset90K);

            Assert.Equal(d.SegmentationTypeId, rt.SegmentationTypeId);
            Assert.Equal(7, rt.SegmentNum);
            Assert.Equal(7, rt.SegmentsExpected);
        }

        [Fact]
        public void RoundTrip_SubSegmentFields_AllowedTypes()
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 1234,
                SegmentationEventCancelIndicator = false,
                SegmentationEventIdComplianceIndicator = true,
                ProgramSegmentationFlag = true,
                SegmentationDurationFlag = false,
                DeliveryNotRestrictedFlag = true,
                SegmentationUpidType = SegmentationUPIDType.NotUsed,
                SegmentationUpidBytes = [],
                SegmentationTypeId = SegmentationType.ProviderPOStart, // allowed
                SegmentNum = 2,
                SegmentsExpected = 5,
                SubSegmentNum = 1,
                SubSegmentsExpected = 4
            };

            var rt = RoundTrip(d);

            Assert.Equal(SegmentationType.ProviderPOStart, rt.SegmentationTypeId);
            Assert.Equal((byte)1, rt.SubSegmentNum);
            Assert.Equal((byte)4, rt.SubSegmentsExpected);
        }

        [Fact]
        public void Encode_SubSegmentFields_DisallowedType_Throws()
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 1,
                ProgramSegmentationFlag = true,
                SegmentationDurationFlag = false,
                DeliveryNotRestrictedFlag = true,
                SegmentationUpidType = SegmentationUPIDType.NotUsed,
                SegmentationUpidBytes = [],
                SegmentationTypeId = SegmentationType.ProgramStart, // not allowed
                SegmentNum = 0,
                SegmentsExpected = 0,
                SubSegmentNum = 1,
                SubSegmentsExpected = 2
            };

            Assert.Throws<InvalidOperationException>(() => Encode(d));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void RoundTrip_ComplianceIndicator_BothWays(bool compliant)
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 0xCAFEBABE,
                SegmentationEventCancelIndicator = false,
                SegmentationEventIdComplianceIndicator = compliant,
                ProgramSegmentationFlag = true,
                SegmentationDurationFlag = false,
                DeliveryNotRestrictedFlag = true,
                SegmentationUpidType = SegmentationUPIDType.NotUsed,
                SegmentationUpidBytes = [],
                SegmentationTypeId = SegmentationType.ProgramStart,
                SegmentNum = 0,
                SegmentsExpected = 0
            };

            var rt = RoundTrip(d);
            Assert.Equal(compliant, rt.SegmentationEventIdComplianceIndicator);
        }

        [Fact]
        public void RoundTrip_WithUpidPayload_NonZeroLength()
        {
            var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };

            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 42,
                ProgramSegmentationFlag = true,
                SegmentationDurationFlag = false,
                DeliveryNotRestrictedFlag = true,
                SegmentationUpidType = SegmentationUPIDType.UserDefined,
                SegmentationUpidBytes = payload,
                SegmentationTypeId = SegmentationType.ContentIdentification,
                SegmentNum = 9,
                SegmentsExpected = 9
            };

            var rt = RoundTrip(d);

            Assert.Equal(SegmentationUPIDType.UserDefined, rt.SegmentationUpidType);
            Assert.True(payload.SequenceEqual(rt.SegmentationUpidBytes));
        }

        [Fact]
        public void ComputePayloadBytes_MatchesEncodeLength_InTypicalCase()
        {
            var d = new SegmentationDescriptor
            {
                SegmentationEventId = 0xAA55AA55,
                SegmentationEventCancelIndicator = false,
                SegmentationEventIdComplianceIndicator = true,
                ProgramSegmentationFlag = true,
                SegmentationDurationFlag = true,
                SegmentationDuration90K = 0x123456789AUL & Scte35Constants.SegmentationDurationMax,
                DeliveryNotRestrictedFlag = false,
                DeliveryRestrictions = new DeliveryRestrictions
                {
                    WebDeliveryAllowedFlag = false,
                    NoRegionalBlackoutFlag = true,
                    ArchiveAllowedFlag = false,
                    DeviceRestrictions = (DeviceRestrictions)3
                },
                SegmentationUpidType = SegmentationUPIDType.NotUsed,
                SegmentationUpidBytes = [],
                SegmentationTypeId = SegmentationType.ProviderAdBlockStart,
                SegmentNum = 1,
                SegmentsExpected = 2,
                SubSegmentNum = 1,
                SubSegmentsExpected = 2
            };

            var bytes = Encode(d);
            Assert.Equal(d.PayloadBytes, bytes.Length);
        }
    }
}