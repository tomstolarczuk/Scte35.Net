using Scte35.Net.Core;
using Scte35.Net.Model;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Tests.Conformance;

public class SpecVectorsTests
{
    public record Vector(
        string Id,
        string Name,
        string Command, // "time_signal", "splice_insert", etc.
        string Encoding, // "base64" | "hex"
        string Payload
    );

    private static readonly Vector[] Vectors =
    [
        new("14.1", "time_signal – Placement Opportunity Start", "time_signal",
            "base64",
            "/DA0AAAAAAAA///wBQb+cr0AUAAeAhxDVUVJSAAAjn/PAAGlmbAICAAAAAAsoKGKNAIAmsnRfg=="),

        new("14.2", "splice_insert", "splice_insert",
            "base64",
            "/DAvAAAAAAAA///wFAVIAACPf+/+c2nALv4AUsz1AAAAAAAKAAhDVUVJAAABNWLbowo="),

        new("14.3", "time_signal – Placement Opportunity End", "time_signal",
            "base64",
            "/DAvAAAAAAAA///wBQb+dGKQoAAZAhdDVUVJSAAAjn+fCAgAAAAALKChijUCAKnMZ1g="),

        new("14.4", "time_signal – Program Start/End", "time_signal",
            "base64",
            "/DBIAAAAAAAA///wBQb+ek2ItgAyAhdDVUVJSAAAGH+fCAgAAAAALMvDRBEAAAIXQ1VFSUgAABl/nwgIAAAAACyk26AQAACZcuND"),

        new("14.5", "time_signal – Program Overlap Start", "time_signal",
            "base64",
            "/DAvAAAAAAAA///wBQb+rr//ZAAZAhdDVUVJSAAACH+fCAgAAAAALKVs9RcAAJUdsKg="),

        new("14.6", "time_signal – Program Blackout Override / Program End", "time_signal",
            "base64",
            "/DBIAAAAAAAA///wBQb+ky44CwAyAhdDVUVJSAAACn+fCAgAAAAALKCh4xgAAAIXQ1VFSUgAAAl/nwgIAAAAACygoYoRAAC0IX6w"),

        new("14.7", "time_signal – Program End", "time_signal",
            "base64",
            "/DAvAAAAAAAA///wBQb+rvF8TAAZAhdDVUVJSAAAB3+fCAgAAAAALKVslxEAAMSHai4="),

        new("14.8", "time_signal – Program Start/End - Placement Opportunity End", "time_signal",
            "base64",
            "/DBhAAAAAAAA///wBQb+qM1E7QBLAhdDVUVJSAAArX+fCAgAAAAALLLXnTUCAAIXQ1VFSUgAACZ/nwgIAAAAACyy150RAAACF0NVRUlIAAAnf58ICAAAAAAsstezEAAAihiGnw==")
    ];

    public static IEnumerable<object[]> GetVectors()
    {
        foreach (var v in Vectors)
            yield return [v];
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void Decode_Vector_HasValidCrc_And_Command(Vector v)
    {
        var sis = DecodeVector(v);

        Assert.True(sis.Crc32Valid);
        Assert.Equal(ParseCommand(v.Command), sis.SpliceCommandType);
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void RoundTrip_Vector_Reencodes_And_Parses(Vector v)
    {
        var original = DecodeVector(v);

        var bytes = Scte35.Encode(original);
        var sis = Scte35.Decode(bytes);

        Assert.True(sis.Crc32Valid);
        Assert.Equal(original.SpliceCommandType, sis.SpliceCommandType);
        Assert.Equal(original.SpliceDescriptors.Count, sis.SpliceDescriptors.Count);

        // no exceptions
        Scte35.ToBase64(sis);
        Scte35.ToHex(sis);
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void Strict_RoundTrip_Matches_OriginalBytes(Vector v)
    {
        var originalBytes = GetOriginalBytes(v);

        var sis = Scte35.Decode(originalBytes);
        var roundTrip = Scte35.Encode(sis);

        Assert.Equal(originalBytes, roundTrip);
    }

    [Theory]
    [MemberData(nameof(GetVectors))]
    public void Strict_RoundTrip_Matches_OriginalString(Vector v)
    {
        var sis = DecodeVector(v);

        if (v.Encoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
        {
            var b64 = Scte35.ToBase64(sis);
            Assert.Equal(v.Payload, b64);
        }
        else
        {
            var hexOrig = NormalizeHex(v.Payload).ToLowerInvariant();
            var hexNew = Scte35.ToHex(sis, lower: true);
            Assert.Equal(hexOrig, hexNew);
        }
    }

    private static SpliceInfoSection DecodeVector(Vector v)
        => v.Encoding.Equals("base64", StringComparison.OrdinalIgnoreCase)
            ? Scte35.FromBase64(v.Payload)
            : Scte35.FromHex(NormalizeHex(v.Payload));

    private static byte[] GetOriginalBytes(Vector v)
    {
        if (v.Encoding.Equals("base64", StringComparison.OrdinalIgnoreCase))
            return Convert.FromBase64String(v.Payload);

        var s = NormalizeHex(v.Payload);
        return Bytes.FromHex(s);
    }

    private static SpliceCommandType ParseCommand(string s) => s.ToLowerInvariant() switch
    {
        "time_signal" => SpliceCommandType.TimeSignal,
        "splice_insert" => SpliceCommandType.SpliceInsert,
        "splice_schedule" => SpliceCommandType.SpliceSchedule,
        "splice_null" => SpliceCommandType.SpliceNull,
        "private_command" => SpliceCommandType.PrivateCommand,
        "bandwidth_reservation" => SpliceCommandType.BandwidthReservation,
        _ => throw new ArgumentOutOfRangeException(nameof(s), $"Unknown command '{s}'")
    };

    private static string NormalizeHex(string hex)
    {
        var s = hex.Trim();
        if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            s = s[2..];
        return new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }
}