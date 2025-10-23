using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor;

public sealed class PrivateDescriptor : ISpliceDescriptor
{
	public SpliceDescriptorTag Tag => SpliceDescriptorTag.Private;
	public uint Identifier { get; set; }
	public byte PrivateTag { get; set; }
	public byte[] PrivateBytes { get; set; } = [];
	public int PayloadBytes => 4 + PrivateBytes.Length;

	public PrivateDescriptor(byte privateTag)
	{
		if (privateTag < 0x80 || privateTag > 0xFE)
			throw new ArgumentOutOfRangeException(nameof(privateTag), "Private tag must be 0x80..0xFE.");
		PrivateTag = privateTag;
	}

	public void Decode(ReadOnlySpan<byte> data)
	{
		PayloadValidator.RequireMinLength(data, 4);

		var r = new BitReader(data);

		// vendor identifier - not CUEI
		Identifier = r.ReadUInt32();

		// the rest is raw vendor data
		PrivateBytes = r.BytesRemaining > 0
			? r.ReadBytesAligned(r.BytesRemaining).ToArray()
			: [];

		if (r.BitsRemaining != 0)
			throw new InvalidOperationException("Trailing bits in private descriptor payload.");
	}

	public void Encode(Span<byte> dest)
	{
		PayloadValidator.RequireMinLength(dest, PayloadBytes);

		var w = new BitWriter(dest);
		w.WriteBits(Identifier, 32);
		if (PrivateBytes.Length > 0)
			w.WriteBytesAligned(PrivateBytes);

		if (w.BitsWritten != PayloadBytes * 8)
			throw new InvalidOperationException("PrivateDescriptor payload size mismatch after encode.");
	}
}