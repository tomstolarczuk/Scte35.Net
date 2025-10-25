namespace Scte35.Net.Core;

public ref struct BitWriter(Span<byte> buffer)
{
	private readonly Span<byte> _buffer = buffer;
	private int _bitPosition = 0;

	public int BitsWritten => _bitPosition;
	public int BitsRemaining => _buffer.Length * 8 - _bitPosition;
	public int BytePosition => _bitPosition / 8;

	public void WriteBits(uint value, int bitCount)
	{
		if (bitCount is <= 0 or > 32)
			throw new ArgumentOutOfRangeException(nameof(bitCount));

		if (bitCount > BitsRemaining)
			throw new InvalidOperationException("Buffer overflow in BitWriter.");

		for (int i = bitCount - 1; i >= 0; i--)
		{
			int byteIndex = _bitPosition / 8;
			int bitIndex = 7 - _bitPosition % 8;
			uint bit = (value >> i) & 1;

			_buffer[byteIndex] &= (byte)~(1 << bitIndex); // clear
			_buffer[byteIndex] |= (byte)(bit << bitIndex); // set

			_bitPosition++;
		}
	}

	public void WriteBits64(ulong value, int bitCount)
	{
		if (bitCount is <= 0 or > 64)
			throw new ArgumentOutOfRangeException(nameof(bitCount));

		if (bitCount > BitsRemaining)
			throw new InvalidOperationException("Buffer overflow in BitWriter.");

		for (int i = bitCount - 1; i >= 0; i--)
		{
			int byteIndex = _bitPosition / 8;
			int bitIndex = 7 - (_bitPosition % 8);
			ulong bit = (value >> i) & 1;

			_buffer[byteIndex] &= (byte)~(1 << bitIndex);
			_buffer[byteIndex] |= (byte)(bit << bitIndex);

			_bitPosition++;
		}
	}

	public void WriteBit(bool bit) => WriteBits(bit ? 1u : 0u, 1);

	public void WriteByte(byte value) => WriteBits(value, 8);

	public void WriteUInt16(ushort value) => WriteBits(value, 16);

	public void WriteUInt24(uint value) => WriteBits(value, 24);

	public void WriteUInt32(uint value) => WriteBits(value, 32);

	public void WriteUInt64(ulong value) => WriteBits64(value, 64);
	
	public void WritePts33(ulong pts)
	{
		if (pts > (1UL << 33) - 1) throw new ArgumentOutOfRangeException(nameof(pts), "Must fit in 33 bits.");
		WriteBits((uint)(pts >> 32), 1);
		WriteBits((uint)(pts & 0xFFFF_FFFFu), 32);
	}

	public void AlignToNextByte()
	{
		int mod = _bitPosition % 8;
		if (mod != 0)
			SkipBits(8 - mod);
	}

	public void SkipBits(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));
		_bitPosition += count;
		if (_bitPosition > _buffer.Length * 8)
			throw new InvalidOperationException("Buffer overflow in BitWriter.");
	}

	public void WriteBytesAligned(ReadOnlySpan<byte> src)
	{
		AlignToNextByte();
		int start = _bitPosition / 8;
		if (src.Length > _buffer.Length - start)
			throw new InvalidOperationException("Buffer overflow in WriteBytesAligned.");
		src.CopyTo(_buffer.Slice(start, src.Length));
		_bitPosition += src.Length * 8;
	}

	public void SeekToByte(int byteOffset)
	{
		int bitOffset = byteOffset * 8;
		if (bitOffset < 0 || bitOffset > _buffer.Length * 8)
			throw new ArgumentOutOfRangeException(nameof(byteOffset));
		_bitPosition = bitOffset;
	}

	public BitWriter SliceBytes(int byteCount)
	{
		AlignToNextByte();
		int start = _bitPosition / 8;
		if (start + byteCount > _buffer.Length)
			throw new ArgumentOutOfRangeException(nameof(byteCount));

		var slice = _buffer.Slice(start, byteCount);
		_bitPosition += byteCount * 8;
		return new BitWriter(slice);
	}

	public void Clear()
	{
		_buffer.Clear();
		_bitPosition = 0;
	}

	public ReadOnlySpan<byte> GetWrittenBytes()
	{
		int len = (_bitPosition + 7) / 8;
		return _buffer.Slice(0, len);
	}
}