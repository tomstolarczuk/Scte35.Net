namespace Scte35.Net.Core;

public ref struct BitReader(ReadOnlySpan<byte> buffer)
{
    private readonly ReadOnlySpan<byte> _buffer = buffer;
    private int _bitPosition = 0;

    public int BitsRemaining => _buffer.Length * 8 - _bitPosition;

    public uint ReadBits(int bitCount)
    {
        if (bitCount is <= 0 or > 32)
            throw new ArgumentOutOfRangeException(nameof(bitCount));

        if (bitCount > BitsRemaining)
            throw new InvalidOperationException("Not enough bits remaining.");

        uint result = 0;
        for (int i = 0; i < bitCount; i++)
        {
            int byteIndex = _bitPosition / 8;
            int bitIndex = 7 - _bitPosition % 8;
            uint bit = (uint)((_buffer[byteIndex] >> bitIndex) & 1);
            result = (result << 1) | bit;
            _bitPosition++;
        }
        return result;
    }

    public bool ReadBit() => ReadBits(1) == 1;

    public void SkipBits(int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        _bitPosition += count;
        if (_bitPosition > _buffer.Length * 8)
            throw new InvalidOperationException("Skipped past end of buffer.");
    }
}