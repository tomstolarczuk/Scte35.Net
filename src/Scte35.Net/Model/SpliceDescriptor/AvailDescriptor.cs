using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor;

public sealed class AvailDescriptor : ISpliceDescriptor
{
	public uint ProviderAvailId { get; set; }

	public int PayloadBytes => 8;

	public SpliceDescriptorTag Tag => SpliceDescriptorTag.Avail;


	public void Encode(Span<byte> dest)
	{
		PayloadValidator.RequireMinLength(dest, PayloadBytes);

		var w = new BitWriter(dest);
		w.WriteUInt32(Scte35Constants.CueIdentifier);
		w.WriteUInt32(ProviderAvailId);

		if (w.BitsWritten != PayloadBytes * 8)
			throw new InvalidOperationException($"{nameof(AvailDescriptor)}.{nameof(Encode)}: payload size mismatch.");
	}

	public void Decode(ReadOnlySpan<byte> data)
	{
		PayloadValidator.RequireExactLength(data, PayloadBytes);

		var r = new BitReader(data);
		DescriptorDecoding.RequireCueIdentifier(ref r);
		ProviderAvailId = r.ReadUInt32();

		if (r.BitsRemaining != 0)
			throw new InvalidOperationException($"{nameof(AvailDescriptor)}.{nameof(Decode)}: trailing data present.");
	}
}