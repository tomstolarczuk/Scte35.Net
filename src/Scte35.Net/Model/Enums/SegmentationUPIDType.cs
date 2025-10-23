using Scte35.Net.Constants;

namespace Scte35.Net.Model.Enums;

public enum SegmentationUPIDType
{
    NotUsed = Scte35Constants.SegmentationUPIDTypeNotUsed,
    UserDefined = Scte35Constants.SegmentationUPIDTypeUserDefined,
    ISCI = Scte35Constants.SegmentationUPIDTypeISCI,
    AdID = Scte35Constants.SegmentationUPIDTypeAdID,
    UMID = Scte35Constants.SegmentationUPIDTypeUMID,
    ISANDeprecated = Scte35Constants.SegmentationUPIDTypeISANDeprecated,
    ISAN = Scte35Constants.SegmentationUPIDTypeISAN,
    TID = Scte35Constants.SegmentationUPIDTypeTID,
    TI = Scte35Constants.SegmentationUPIDTypeTI,
    ADI = Scte35Constants.SegmentationUPIDTypeADI,
    EIDR = Scte35Constants.SegmentationUPIDTypeEIDR,
    ATSC = Scte35Constants.SegmentationUPIDTypeATSC,
    MPU = Scte35Constants.SegmentationUPIDTypeMPU,
    MID = Scte35Constants.SegmentationUPIDTypeMID,
    ADS = Scte35Constants.SegmentationUPIDTypeADS,
    URI = Scte35Constants.SegmentationUPIDTypeURI,
    UUID = Scte35Constants.SegmentationUPIDTypeUUID,
    SCR = Scte35Constants.SegmentationUPIDTypeSCR,
}