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
    TribuneIdentifier = Scte35Constants.SegmentationUPIDTypeTribuneIdentifier,
    TurnerIdentifier = Scte35Constants.SegmentationUPIDTypeTurnerIdentifier,
    AdvertisingDigitalIdentifier = Scte35Constants.SegmentationUPIDTypeAdvertisingDigitalIdentifier,
    EntertainmentIdentifierRegistry = Scte35Constants.SegmentationUPIDTypeEntertainmentIdentifierRegistry,
    ATSCContentIdentifier = Scte35Constants.SegmentationUPIDTypeATSCContentIdentifier,
    ManagedPrivateUPID = Scte35Constants.SegmentationUPIDTypeManagedPrivateUPID,
    MultipleUPID = Scte35Constants.SegmentationUPIDTypeMultipleUPID,
    ADSInformation = Scte35Constants.SegmentationUPIDTypeADSInformation,
    UniformResourceIdentifier = Scte35Constants.SegmentationUPIDTypeUniformResourceIdentifier,
    UniversalUniqueIdentifier = Scte35Constants.SegmentationUPIDTypeUniversalUniqueIdentifier,
    ServiceContentReferenceIdentifier = Scte35Constants.SegmentationUPIDTypeServiceContentReferenceIdentifier,
}