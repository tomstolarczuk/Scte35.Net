using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model;

public sealed class DeliveryRestrictions
{
    public bool WebDeliveryAllowedFlag { get; set; }
    public bool NoRegionalBlackoutFlag { get; set; }
    public bool ArchiveAllowedFlag { get; set; }
    public DeviceRestrictions DeviceRestrictions { get; set; }
}