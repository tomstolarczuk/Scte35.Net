using Scte35.Net.Constants;

namespace Scte35.Net.Model.Enums;

public enum SapType : byte
{
	ClosedGopNoLeadingPictures = Scte35Constants.SapTypeClosedGopNoLeadingPictures,
	ClosedGopLeadingPictures = Scte35Constants.SapTypeClosedGopLeadingPictures,
	OpenGop = Scte35Constants.SapTypeOpenGop,
	NotSpecified = Scte35Constants.SapTypeNotSpecified,
}