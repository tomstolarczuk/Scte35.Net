using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand
{
    public sealed class UnknownSpliceCommand : ISpliceCommand
    {
        public UnknownSpliceCommand(byte commandType) => CommandType = commandType;

        public byte CommandType { get; }
        public SpliceCommandType Type => (SpliceCommandType)CommandType;
        public byte[] Payload { get; private set; } = [];
        public int PayloadBytes => Payload.Length;

        public void Encode(Span<byte> dest)
        {
            PayloadValidator.RequireExactLength(dest, PayloadBytes);
            if (PayloadBytes > 0)
                Payload.AsSpan().CopyTo(dest);
        }

        public void Decode(ReadOnlySpan<byte> data)
        {
            Payload = data.Length == 0 ? [] : data.ToArray();
        }
    }
}