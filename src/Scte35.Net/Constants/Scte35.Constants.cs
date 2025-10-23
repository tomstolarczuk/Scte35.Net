namespace Scte35.Net.Constants;

public static class Scte35Constants
{
    public const uint Reserved = 0xFFFF;

    // Table ID
    public const byte TableId = 0xFC;

    // Cue identifier
    public const uint CueIdentifier = 0x43554549;
    public const string CueIdentifierAscii = "CUEI";

    // Splice Command Types
    public const byte SpliceNullCommand = 0x00;
    public const byte SpliceScheduleCommand = 0x04;
    public const byte SpliceInsertCommand = 0x05;
    public const byte TimeSignalCommand = 0x06;
    public const byte BandwidthReservationCommand = 0x07;
    public const byte PrivateCommand = 0xFF;

    // Descriptor Tags
    public const byte AvailDescriptorTag = 0x00;
    public const byte DtmfDescriptorTag = 0x01;
    public const byte SegmentationDescriptorTag = 0x02;
    public const byte TimeDescriptorTag = 0x03;
    public const byte AudioDescriptorTag = 0x04;
    public const byte PrivateDescriptorTag = 0xFF;

    // SAP (Splice Point) Types
    public const byte SapTypeClosedGopNoLeadingPictures = 0x0;
    public const byte SapTypeClosedGopLeadingPictures = 0x2;
    public const byte SapTypeOpenGop = 0x3;
    public const byte SapTypeNotSpecified = 0x3;

    // PTS and timing
    public const ulong PtsMax = 0x1FFFFFFFFUL; // 33-bit PTS rollover
    public const double TicksPerSecond = 90000.0;

    // Segmentation Types
	public const byte SegmentationTypeNotIndicated = 0x00;
	public const byte SegmentationTypeContentIdentification = 0x01;
	public const byte SegmentationTypeProgramStart = 0x10;
	public const byte SegmentationTypeProgramEnd = 0x11;
	public const byte SegmentationTypeProgramEarlyTermination = 0x12;
	public const byte SegmentationTypeProgramBreakaway = 0x13;
	public const byte SegmentationTypeProgramResumption = 0x14;
	public const byte SegmentationTypeProgramRunoverPlanned = 0x15;
	public const byte SegmentationTypeProgramRunoverUnplanned = 0x16;
	public const byte SegmentationTypeProgramOverlapStart = 0x17;
	public const byte SegmentationTypeProgramBlackoutOverride = 0x18;
	public const byte SegmentationTypeProgramStartInProgress = 0x19;
	public const byte SegmentationTypeChapterStart = 0x20;
	public const byte SegmentationTypeChapterEnd = 0x21;
	public const byte SegmentationTypeBreakStart = 0x22;
	public const byte SegmentationTypeBreakEnd = 0x23;
	public const byte SegmentationTypeOpeningCreditStart = 0x24;
	public const byte SegmentationTypeOpeningCreditEnd = 0x25;
	public const byte SegmentationTypeClosingCreditStart = 0x26;
	public const byte SegmentationTypeClosingCreditEnd = 0x27;
	public const byte SegmentationTypeProviderAdStart = 0x30;
	public const byte SegmentationTypeProviderAdEnd = 0x31;
	public const byte SegmentationTypeDistributorAdStart = 0x32;
	public const byte SegmentationTypeDistributorAdEnd = 0x33;
	public const byte SegmentationTypeProviderPOStart = 0x34;
	public const byte SegmentationTypeProviderPOEnd = 0x35;
	public const byte SegmentationTypeDistributorPOStart = 0x36;
	public const byte SegmentationTypeDistributorPOEnd = 0x37;
	public const byte SegmentationTypeProviderOverlayPOStart = 0x38;
	public const byte SegmentationTypeProviderOverlayPOEnd = 0x39;
	public const byte SegmentationTypeDistributorOverlayPOStart = 0x3a;
	public const byte SegmentationTypeDistributorOverlayPOEnd = 0x3b;
	public const byte SegmentationTypeProviderPromoStart = 0x3c;
	public const byte SegmentationTypeProviderPromoEnd = 0x3d;
	public const byte SegmentationTypeDistributorPromoStart = 0x3e;
	public const byte SegmentationTypeDistributorPromoEnd = 0x3f;
	public const byte SegmentationTypeUnscheduledEventStart = 0x40;
	public const byte SegmentationTypeUnscheduledEventEnd = 0x41;
	public const byte SegmentationTypeAltConOppStart = 0x42;
	public const byte SegmentationTypeAltConOppEnd = 0x43;
	public const byte SegmentationTypeProviderAdBlockStart = 0x44;
	public const byte SegmentationTypeProviderAdBlockEnd = 0x45;
	public const byte SegmentationTypeDistributorAdBlockStart = 0x46;
	public const byte SegmentationTypeDistributorAdBlockEnd = 0x47;
	public const byte SegmentationTypeNetworkStart = 0x50;
	public const byte SegmentationTypeNetworkEnd = 0x51;

    // Segmentation UPID Types
    public const byte SegmentationUPIDTypeNotUsed = 0x00;
    public const byte SegmentationUPIDTypeUserDefined = 0x01;
    public const byte SegmentationUPIDTypeISCI = 0x02;
    public const byte SegmentationUPIDTypeAdID = 0x03;
    public const byte SegmentationUPIDTypeUMID = 0x04;
    public const byte SegmentationUPIDTypeISANDeprecated = 0x05;
    public const byte SegmentationUPIDTypeISAN = 0x06;
    public const byte SegmentationUPIDTypeTribuneIdentifier = 0x07; // TID
    public const byte SegmentationUPIDTypeTurnerIdentifier = 0x08; // TI
    public const byte SegmentationUPIDTypeAdvertisingDigitalIdentifier = 0x09; // ADI
    public const byte SegmentationUPIDTypeEntertainmentIdentifierRegistry = 0x0A; // EIDR
    public const byte SegmentationUPIDTypeATSCContentIdentifier = 0x0B; // ATSC
    public const byte SegmentationUPIDTypeManagedPrivateUPID = 0x0C; // MPU
    public const byte SegmentationUPIDTypeMultipleUPID = 0x0D; // MID
    public const byte SegmentationUPIDTypeADSInformation = 0x0E; // ADS
    public const byte SegmentationUPIDTypeUniformResourceIdentifier = 0x0F; // URI
    public const byte SegmentationUPIDTypeUniversalUniqueIdentifier = 0x10; // UUID
    public const byte SegmentationUPIDTypeServiceContentReferenceIdentifier = 0x11; // SCR

    // Misc
    public const byte DeviceRestrictionsGroup0 = 0x0;
    public const byte DeviceRestrictionsGroup1 = 0x1;
    public const byte DeviceRestrictionsGroup2 = 0x2;
    public const byte DeviceRestrictionsNone = 0x3;
}