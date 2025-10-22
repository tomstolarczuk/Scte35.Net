namespace Scte35.Net.Core;

public interface IBinarySerializable
{
    void Encode(Span<byte> dest);

    void Decode(ReadOnlySpan<byte> data);

    int PayloadBytes { get; }
}
