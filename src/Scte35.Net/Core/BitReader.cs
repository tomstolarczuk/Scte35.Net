namespace Scte35.Net.Core;

public ref struct BitReader(ReadOnlySpan<byte> buffer)
{
	private readonly ReadOnlySpan<byte> _buffer = buffer;
	private int _bitPosition = 0;


	public int BitsRemaining => _buffer.Length * 8 - _bitPosition;

	public int BytesRemaining => _buffer.Length - _bitPosition / 8;

	public int BitPosition => _bitPosition;

	public int BytePosition => _bitPosition / 8;


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

	public ulong ReadBits64(int bitCount)
	{
		if (bitCount is <= 0 or > 64)
			throw new ArgumentOutOfRangeException(nameof(bitCount));
		if (bitCount > BitsRemaining)
			throw new InvalidOperationException("Not enough bits remaining.");

		ulong result = 0;
		for (int i = 0; i < bitCount; i++)
		{
			int byteIndex = _bitPosition / 8;
			int bitIndex = 7 - (_bitPosition % 8);
			ulong bit = (ulong)((_buffer[byteIndex] >> bitIndex) & 1);
			result = (result << 1) | bit;
			_bitPosition++;
		}

		return result;
	}

	public bool ReadBit() => ReadBits(1) == 1;

	public byte ReadByte() => (byte)ReadBits(8);

	public ushort ReadUInt16() => (ushort)ReadBits(16);

	public uint ReadUInt32() => ReadBits(32);

	public ulong ReadUInt64() => ReadBits64(64);

	public void SkipBits(int count)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count));
		_bitPosition += count;
		if (_bitPosition > _buffer.Length * 8)
			throw new InvalidOperationException("Skipped past end of buffer.");
	}

	public void AlignToNextByte()
	{
		int mod = _bitPosition % 8;
		if (mod != 0)
			SkipBits(8 - mod);
	}

	// Returns a slice of bytes, aligned to next byte boundary
	public ReadOnlySpan<byte> ReadBytesAligned(int count)
	{
		AlignToNextByte();
		int start = _bitPosition / 8;
		int end = start + count;
		if (end > _buffer.Length)
			throw new InvalidOperationException("Not enough bytes remaining.");
		_bitPosition += count * 8;
		return _buffer.Slice(start, count);
	}

	// Peek ahead without moving cursor
	public uint PeekBits(int bitCount)
	{
		int saved = _bitPosition;
		uint value = ReadBits(bitCount);
		_bitPosition = saved;
		return value;
	}

	public ulong PeekBits64(int bitCount)
	{
		int saved = _bitPosition;
		ulong value = ReadBits64(bitCount);
		_bitPosition = saved;
		return value;
	}

	// Skip to a specific byte offset
	public void SeekToByte(int byteOffset)
	{
		int bitOffset = byteOffset * 8;
		if (bitOffset < 0 || bitOffset > _buffer.Length * 8)
			throw new ArgumentOutOfRangeException(nameof(byteOffset));
		_bitPosition = bitOffset;
	}

	// Return a new sub-reader for remaining bits or N bytes
	public BitReader SliceBits(int bitCount)
	{
		if (bitCount > BitsRemaining)
			throw new ArgumentOutOfRangeException(nameof(bitCount));
		int startByte = _bitPosition / 8;
		int endByte = (_bitPosition + bitCount + 7) / 8;
		var slice = _buffer.Slice(startByte, endByte - startByte);
		var sub = new BitReader(slice);
		sub.SkipBits(_bitPosition % 8); // align to current bit offset
		return sub;
	}

	public BitReader SliceBytes(int byteCount)
	{
		AlignToNextByte();
		int start = _bitPosition / 8;
		var slice = _buffer.Slice(start, byteCount);
		_bitPosition += byteCount * 8;
		return new BitReader(slice);
	}
}