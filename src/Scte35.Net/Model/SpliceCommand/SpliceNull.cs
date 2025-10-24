using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand;

public sealed class SpliceNullCommand : IBinarySerializable
{
    public SpliceCommandType Type => SpliceCommandType.SpliceNull;
    public int PayloadBytes => 0;

    public void Encode(Span<byte> dest)
    {
        PayloadValidator.RequireExactLength(dest, 0);
    }

    public void Decode(ReadOnlySpan<byte> data)
    {
        PayloadValidator.RequireExactLength(data, 0);
    }
}