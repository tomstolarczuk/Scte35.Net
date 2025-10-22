using Scte35.Net.Constants;

namespace Scte35.Net.Model.Enums;

public enum SapType : byte
{
	Type1 = Scte35Constants.SapTypeStartsWithOther,
	Type2 = Scte35Constants.SapTypeStartsWithVideo,
	Type3 = Scte35Constants.SapTypeStartsWithAudio,
	NotSpecified = Scte35Constants.SapTypeNotSpecified,
}