using System.Text;
using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor;

public sealed class DtmfDescriptor : ISpliceDescriptor
{
	public SpliceDescriptorTag Tag => SpliceDescriptorTag.Dtmf;

	public byte Preroll { get; set; }

	public string Chars { get; set; } = string.Empty;

	public int PayloadBytes => 4 + 1 + 1 + Chars.Length;

	public void Decode(ReadOnlySpan<byte> data)
	{
		PayloadValidator.RequireMinLength(data, 6);

		var r = new BitReader(data);

		DescriptorDecoding.RequireCueIdentifier(ref r);

		Preroll = r.ReadByte();
		byte count = (byte)r.ReadBits(3);
		r.SkipBits(5); // reserved

		if (r.BytesRemaining != count)
			throw new InvalidOperationException(
				$"DTMF count {count} does not match remaining {r.BytesRemaining} bytes.");

		var dtmfBytes = r.ReadBytesAligned(count);
		ValidateDtmfChars(dtmfBytes);
		Chars = Encoding.ASCII.GetString(dtmfBytes);

		if (r.BitsRemaining != 0)
			throw new InvalidOperationException("Trailing data in DTMF descriptor payload.");
	}

	public void Encode(Span<byte> dest)
	{
		int needed = PayloadBytes;
		PayloadValidator.RequireMinLength(dest, needed);

		if (Chars.Length > 255)
			throw new InvalidOperationException("DTMF chars length exceeds 255.");

		var ascii = Encoding.ASCII.GetBytes(Chars);
		ValidateDtmfChars(ascii);

		var w = new BitWriter(dest);
		w.WriteBits(Scte35Constants.CueIdentifier, 32);
		w.WriteBits(Preroll, 8);
		w.WriteBits((byte)ascii.Length, 3);
		w.WriteBits(Scte35Constants.Reserved, 5);
		w.WriteBytesAligned(ascii);

		if (w.BitsWritten != needed * 8)
			throw new InvalidOperationException("DTMF payload size mismatch after encode.");
	}

	private static void ValidateDtmfChars(ReadOnlySpan<byte> bytes)
	{
		foreach (var b in bytes)
		{
			bool ok = b is >= (byte)'0' and <= (byte)'9' or (byte)'*' or (byte)'#';
			if (!ok)
				throw new InvalidOperationException($"Invalid DTMF char 0x{b:X2}. Allowed: 0-9, '*', '#'.");
		}
	}
}