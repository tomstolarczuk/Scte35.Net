using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand;

public sealed class PrivateCommand : IBinarySerializable
{
    public SpliceCommandType Type => SpliceCommandType.PrivateCommand;
    public uint Identifier { get; set; }
    public byte[] PrivateBytes { get; set; } = [];
    public int PayloadBytes => 4 + PrivateBytes.Length;

    public void Encode(Span<byte> dest)
    {
        PayloadValidator.RequireMinLength(dest, PayloadBytes);

        var w = new BitWriter(dest);
        w.WriteUInt32(Identifier);
        
        if (PrivateBytes.Length > 0)
            w.WriteBytesAligned(PrivateBytes);

        if (w.BitsWritten != PayloadBytes * 8)
            throw new InvalidOperationException("PrivateCommand payload size mismatch after encode.");
    }

    public void Decode(ReadOnlySpan<byte> data)
    {
        PayloadValidator.RequireMinLength(data, 4);

        var r = new BitReader(data);
        Identifier = r.ReadUInt32();

        var remainingBytes = r.BitsRemaining / 8;
        PrivateBytes = remainingBytes > 0 ? r.ReadBytesAligned(remainingBytes).ToArray() : [];

        if (r.BitsRemaining != 0)
            throw new InvalidOperationException("Trailing bits present in PrivateCommand payload.");
    }
}