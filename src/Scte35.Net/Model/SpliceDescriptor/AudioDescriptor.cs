using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor;

public sealed class AudioDescriptor : ISpliceDescriptor
{
	private const int HeaderBits = 32 + 4 + 4;
	private const int ChannelBits = 8 + 24 + 3 + 4 + 1;

	public SpliceDescriptorTag Tag => SpliceDescriptorTag.Audio;

	public IList<AudioChannel> AudioChannels { get; } = new List<AudioChannel>();

	public int PayloadBytes => (HeaderBits + AudioChannels.Count * ChannelBits) / 8;

	public void Decode(ReadOnlySpan<byte> data)
	{
		PayloadValidator.RequireMinLength(data, 4);

		var r = new BitReader(data);

		DescriptorDecoding.RequireCueIdentifier(ref r);

		int audioCount = (int)r.ReadBits(4);
		if (audioCount > 0xF)
			throw new InvalidOperationException($"Invalid audio_count {audioCount} (> 15)");

		if (r.ReadBits(4) != 0xF)
			throw new InvalidOperationException("Reserved bits not set to 1.");

		AudioChannels.Clear();
		Span<char> isoBuf = stackalloc char[3];

		for (int i = 0; i < audioCount; i++)
		{
			byte componentTag = r.ReadByte();
			isoBuf[0] = (char)r.ReadByte();
			isoBuf[1] = (char)r.ReadByte();
			isoBuf[2] = (char)r.ReadByte();
			byte bitStreamMode = (byte)r.ReadBits(3);
			byte numChannels = (byte)r.ReadBits(4);
			bool fullServiceAudio = r.ReadBit();

			var channel = new AudioChannel(
				componentTag,
				new string(isoBuf),
				bitStreamMode,
				numChannels,
				fullServiceAudio);

			AudioChannels.Add(channel);
		}

		if (r.BitsRemaining != 0)
			throw new InvalidOperationException("Trailing data in payload.");
	}

	public void Encode(Span<byte> dest)
	{
		PayloadValidator.RequireMinLength(dest, PayloadBytes);

		if (AudioChannels.Count > 0xF)
			throw new InvalidOperationException("Cannot encode more than 15 channels.");

		var w = new BitWriter(dest);
		w.WriteUInt32(Scte35Constants.CueIdentifier);
		w.WriteBits((uint)AudioChannels.Count, 4);
		w.WriteBits(Scte35Constants.Reserved, 4); // reserved bits set to 1 per spec

		foreach (var ch in AudioChannels)
		{
			ch.Validate();

			w.WriteByte(ch.ComponentTag);

			var iso = ch.IsoLanguageCode;
			w.WriteByte((byte)iso[0]);
			w.WriteByte((byte)iso[1]);
			w.WriteByte((byte)iso[2]);

			w.WriteBits(ch.BitStreamMode, 3);
			w.WriteBits(ch.NumChannels, 4);
			w.WriteBit(ch.FullServiceAudio);
		}

		if (w.BitsWritten != PayloadBytes * 8)
			throw new InvalidOperationException("Payload size mismatch after encode.");
	}

	public readonly struct AudioChannel
	{
		public AudioChannel(byte componentTag, string isoLanguageCode, byte bitStreamMode, byte numChannels,
			bool fullServiceAudio)
		{
			if (isoLanguageCode is null)
				throw new ArgumentNullException(nameof(isoLanguageCode));

			if (isoLanguageCode.Length != 3)
				throw new ArgumentException("ISO language code must be exactly 3 characters.", nameof(isoLanguageCode));

			if (!IsAscii(isoLanguageCode))
				throw new ArgumentException("ISO language code must contain only ASCII characters.",
					nameof(isoLanguageCode));

			if (bitStreamMode > 0x7)
				throw new ArgumentOutOfRangeException(nameof(bitStreamMode), "bitStreamMode must fit in 3 bits.");

			if (numChannels > 0xF)
				throw new ArgumentOutOfRangeException(nameof(numChannels), "numChannels must fit in 4 bits.");

			ComponentTag = componentTag;
			IsoLanguageCode = isoLanguageCode;
			BitStreamMode = bitStreamMode;
			NumChannels = numChannels;
			FullServiceAudio = fullServiceAudio;
		}

		public byte ComponentTag { get; }

		public string IsoLanguageCode { get; }

		public byte BitStreamMode { get; }

		public byte NumChannels { get; }

		public bool FullServiceAudio { get; }

		internal void Validate()
		{
			if (IsoLanguageCode.Length != 3)
				throw new InvalidOperationException("ISO language code must be exactly 3 characters.");
			if (!IsAscii(IsoLanguageCode))
				throw new InvalidOperationException("ISO language code must contain only ASCII characters.");
			if (BitStreamMode > 0x7)
				throw new InvalidOperationException("bitStreamMode must fit in 3 bits.");
			if (NumChannels > 0xF)
				throw new InvalidOperationException("numChannels must fit in 4 bits.");
		}

		private static bool IsAscii(string value)
		{
			return value.All(c => c <= 0x7F);
		}
	}
}