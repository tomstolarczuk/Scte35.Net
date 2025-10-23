using System.Text;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model;

public sealed class SegmentationUpid
{
    public SegmentationUPIDType Type { get; set; }
    public string? Format { get; set; }
    public uint? FormatIdentifier { get; set; }
    public string Value { get; set; } = string.Empty;

    public static SegmentationUpid FromBytes(SegmentationUPIDType type, ReadOnlySpan<byte> buf,
        uint? formatIdentifier = null)
    {
        var upid = new SegmentationUpid { Type = type, FormatIdentifier = formatIdentifier };

        switch (type)
        {
            case SegmentationUPIDType.ISCI:
            case SegmentationUPIDType.AdID:
            case SegmentationUPIDType.ADSInformation:
            case SegmentationUPIDType.UniformResourceIdentifier:
                upid.Format = "ascii";
                upid.Value = ToAsciiPrintable(buf);
                break;

            case SegmentationUPIDType.UniversalUniqueIdentifier:
                upid.Format = "uuid";
                upid.Value = TryFormatUuid(buf, out var uuidText) ? uuidText : ToHex(buf);
                break;

            case SegmentationUPIDType.UMID:
                upid.Format = "umid";
                upid.Value = FormatUmid(buf);
                break;

            case SegmentationUPIDType.EntertainmentIdentifierRegistry:
                upid.Format = "eidr";
                upid.Value = FormatEidr(buf);
                break;

            case SegmentationUPIDType.ATSCContentIdentifier:
                upid.Format = "atsc";
                upid.Value = ToHex(buf);
                break;

            case SegmentationUPIDType.ISAN:
            case SegmentationUPIDType.ISANDeprecated:
                upid.Format = "isan";
                upid.Value = FormatIsan(buf);
                break;

            case SegmentationUPIDType.MultipleUPID:
                upid.Format = "mid";
                upid.Value = FormatMid(buf);
                break;

            case SegmentationUPIDType.NotUsed:
                upid.Format = "none";
                upid.Value = buf.Length == 0 ? string.Empty : ToHex(buf);
                break;

            case SegmentationUPIDType.UserDefined:
                upid.Format = "user";
                upid.Value = PreferAsciiOrHex(buf);
                break;

            case SegmentationUPIDType.TribuneIdentifier:
            case SegmentationUPIDType.TurnerIdentifier:
            case SegmentationUPIDType.AdvertisingDigitalIdentifier:
            case SegmentationUPIDType.ServiceContentReferenceIdentifier:
                upid.Format = "ascii";
                upid.Value = ToAsciiPrintable(buf);
                break;

            case SegmentationUPIDType.ManagedPrivateUPID:
                upid.Format = "hex";
                upid.Value = ToHex(buf);
                break;

            default:
                upid.Format = "hex";
                upid.Value = ToHex(buf);
                break;
        }

        return upid;
    }

    public string Name() => Type.ToString();
    
    public string ASCIIValue() => ToAsciiPrintable(Encoding.ASCII.GetBytes(Value));

    private static string ToHex(ReadOnlySpan<byte> bytes) => Convert.ToHexString(bytes);

    private static string ToAsciiPrintable(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        foreach (var b in bytes)
            sb.Append(b >= 0x20 && b <= 0x7E ? (char)b : '.');
        return sb.ToString();
    }

    private static string PreferAsciiOrHex(ReadOnlySpan<byte> bytes)
    {
        foreach (var b in bytes)
            if (b < 0x20 || b > 0x7E)
                return ToHex(bytes);
        return ToAsciiPrintable(bytes);
    }

    private static bool TryFormatUuid(ReadOnlySpan<byte> bytes, out string uuid)
    {
        uuid = string.Empty;
        if (bytes.Length != 16) return false;
        var a = BitConverter.ToUInt32(bytes[..4]);
        var b = BitConverter.ToUInt16(bytes.Slice(4, 2));
        var c = BitConverter.ToUInt16(bytes.Slice(6, 2));
        var d = bytes.Slice(8, 2).ToArray();
        var e = bytes.Slice(10, 6).ToArray();
        uuid = string.Format("{0:x8}-{1:x4}-{2:x4}-{3:x2}{4:x2}-{5}", a, b, c, d[0], d[1],
            Convert.ToHexString(e).ToLowerInvariant());
        return true;
    }

    private static string FormatUmid(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length != 32) return ToHex(bytes);
        var sb = new StringBuilder(32 * 2 + 3);
        for (int i = 0; i < 32; i++)
        {
            if (i > 0 && i % 8 == 0) sb.Append('-');
            sb.Append(bytes[i].ToString("x2"));
        }

        return sb.ToString();
    }

    private static string FormatEidr(ReadOnlySpan<byte> bytes) => ToHex(bytes);

    private static string FormatIsan(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 12 || bytes.Length == 16)
            return ToHex(bytes);
        foreach (var b in bytes)
            if (b < 0x20 || b > 0x7E)
                return ToHex(bytes);
        return ToAsciiPrintable(bytes);
    }

    private static string FormatMid(ReadOnlySpan<byte> bytes)
    {
        var children = new List<string>();
        int idx = 0;
        while (idx + 2 <= bytes.Length)
        {
            var t = (SegmentationUPIDType)bytes[idx++];
            var l = bytes[idx++];
            if (idx + l > bytes.Length) break;
            var val = bytes.Slice(idx, l);
            var child = FromBytes(t, val);
            children.Add($"{child.Type.ToString().ToLowerInvariant()}:{child.Value}");
            idx += l;
        }

        return "[" + string.Join(", ", children) + "]";
    }
}