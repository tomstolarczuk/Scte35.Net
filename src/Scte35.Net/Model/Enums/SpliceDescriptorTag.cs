using Scte35.Net.Constants;

namespace Scte35.Net.Model.Enums;

public enum SpliceDescriptorTag : byte
{
	Avail = Scte35Constants.AvailDescriptorTag,
	Dtmf = Scte35Constants.DtmfDescriptorTag,
	Segmentation = Scte35Constants.SegmentationDescriptorTag,
	Time = Scte35Constants.TimeDescriptorTag,
	Audio = Scte35Constants.AudioDescriptorTag,
	Private = Scte35Constants.PrivateDescriptorTag,
}