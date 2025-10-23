using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor
{
	public sealed class TimeDescriptor : ISpliceDescriptor
	{
		public SpliceDescriptorTag Tag => SpliceDescriptorTag.Time;

		public ulong TAISeconds { get; set; }
		public uint TAINs { get; set; }
		public ushort UTCOffset { get; set; }
		public int PayloadBytes => 4 + 6 + 4 + 2;

		public void Decode(ReadOnlySpan<byte> data)
		{
			PayloadValidator.RequireMinLength(data, PayloadBytes);

			var r = new BitReader(data);

			DescriptorDecoding.RequireCueIdentifier(ref r);

			TAISeconds = r.ReadBits64(48);
			TAINs = r.ReadUInt32();
			UTCOffset = r.ReadUInt16();

			if (r.BitsRemaining != 0)
				throw new InvalidOperationException("Trailing bits in TimeDescriptor payload.");
		}

		public void Encode(Span<byte> dest)
		{
			int needed = PayloadBytes;
			PayloadValidator.RequireMinLength(dest, needed);

			var w = new BitWriter(dest);

			w.WriteUInt32(Scte35Constants.CueIdentifier);
			w.WriteBits64(TAISeconds, 48);
			w.WriteUInt32(TAINs);
			w.WriteUInt16(UTCOffset);

			if (w.BitsWritten != needed * 8)
				throw new InvalidOperationException("TimeDescriptor payload size mismatch after encode.");
		}
	}
}