namespace Scte35.Net.Core;

public ref struct BitWriter(Span<byte> buffer)
{
	private readonly Span<byte> _buffer = buffer;
	private int _bitPosition = 0;

	public void WriteBits(uint value, int bitCount)
	{
		if (bitCount <= 0 || bitCount > 32)
			throw new ArgumentOutOfRangeException(nameof(bitCount));

		if (bitCount > BitsRemaining)
			throw new InvalidOperationException("Buffer overflow in BitWriter.");

		for (int i = bitCount - 1; i >= 0; i--)
		{
			int byteIndex = _bitPosition / 8;
			int bitIndex = 7 - (_bitPosition % 8);

			uint bit = (value >> i) & 1;
			_buffer[byteIndex] &= (byte)~(1 << bitIndex); // clear bit
			_buffer[byteIndex] |= (byte)(bit << bitIndex); // set bit

			_bitPosition++;
			if (_bitPosition > _buffer.Length * 8)
				throw new InvalidOperationException("Buffer overflow in BitWriter.");
		}
	}

	public void WriteBit(bool bit) => WriteBits(bit ? 1u : 0u, 1);

	public int BitsWritten => _bitPosition;

	public int BitsRemaining => _buffer.Length * 8 - BitsWritten;
}
