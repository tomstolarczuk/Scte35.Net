using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
	public class AudioDescriptorTests
	{
		private static AudioDescriptor.AudioChannel Ch(
			byte componentTag,
			string iso,
			byte bitStreamMode,
			byte numChannels,
			bool fullServiceAudio
		) => new(componentTag, iso, bitStreamMode, numChannels, fullServiceAudio);

		[Fact]
		public void PayloadBytes_Empty_Is5Bytes()
		{
			var d = new AudioDescriptor();
			Assert.Equal(5, d.PayloadBytes);
		}

		[Fact]
		public void PayloadBytes_NChannels_Is5Plus5PerChannel()
		{
			var d = new AudioDescriptor();
			for (int n = 0; n <= 15; n++)
			{
				d.AudioChannels.Clear();
				for (int i = 0; i < n; i++)
					d.AudioChannels.Add(Ch((byte)i, "eng", 3, 2, true));

				Assert.Equal(5 + 5 * n, d.PayloadBytes);
			}
		}

		[Fact]
		public void EncodeDecode_ZeroChannels_RoundTrips()
		{
			var d = new AudioDescriptor();

			var buf = new byte[d.PayloadBytes];
			d.Encode(buf);

			var d2 = new AudioDescriptor();
			d2.Decode(buf);

			Assert.Empty(d2.AudioChannels);
		}

		[Fact]
		public void EncodeDecode_SingleChannel_RoundTrips()
		{
			var d = new AudioDescriptor();
			d.AudioChannels.Add(Ch(0x11, "eng", 5, 2, true));

			var buf = new byte[d.PayloadBytes];
			d.Encode(buf);

			var d2 = new AudioDescriptor();
			d2.Decode(buf);

			Assert.Single(d2.AudioChannels);
			var ch = d2.AudioChannels[0];
			Assert.Equal((byte)0x11, ch.ComponentTag);
			Assert.Equal("eng", ch.IsoLanguageCode);
			Assert.Equal((byte)5, ch.BitStreamMode);
			Assert.Equal((byte)2, ch.NumChannels);
			Assert.True(ch.FullServiceAudio);
		}

		[Fact]
		public void EncodeDecode_FifteenChannels_RoundTrips_AndSizeMatches()
		{
			var d = new AudioDescriptor();

			for (int i = 0; i < 15; i++)
				d.AudioChannels.Add(Ch((byte)(0x20 + i), "pol", (byte)(i % 8), (byte)(i % 16), i % 2 == 0));

			Assert.Equal(5 + 15 * 5, d.PayloadBytes);

			var buf = new byte[d.PayloadBytes];
			d.Encode(buf);

			var d2 = new AudioDescriptor();
			d2.Decode(buf);

			Assert.Equal(15, d2.AudioChannels.Count);

			Assert.Equal("pol", d2.AudioChannels[0].IsoLanguageCode);
			Assert.Equal("pol", d2.AudioChannels[14].IsoLanguageCode);
			Assert.Equal((byte)0x20, d2.AudioChannels[0].ComponentTag);
			Assert.Equal((byte)0x2E, d2.AudioChannels[14].ComponentTag);
		}

		[Fact]
		public void Encode_MoreThan15Channels_Throws()
		{
			var d = new AudioDescriptor();
			for (int i = 0; i < 16; i++)
				d.AudioChannels.Add(Ch((byte)i, "eng", 1, 1, false));

			var buf = new byte[5 + 16 * 5];
			var ex = Assert.Throws<InvalidOperationException>(() => d.Encode(buf));
			Assert.Contains("more than 15 channels", ex.Message, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void Decode_TrailingData_Throws()
		{
			var d = new AudioDescriptor();
			d.AudioChannels.Add(Ch(1, "eng", 3, 2, true));

			var payload = new byte[d.PayloadBytes];
			d.Encode(payload);

			// add 1 extra byte to trigger trailing data detection
			var withExtra = new byte[payload.Length + 1];
			Buffer.BlockCopy(payload, 0, withExtra, 0, payload.Length);
			withExtra[^1] = 0x00;

			var d2 = new AudioDescriptor();
			var ex = Assert.Throws<InvalidOperationException>(() => d2.Decode(withExtra));
			Assert.Contains("trailing data", ex.Message, StringComparison.OrdinalIgnoreCase);
		}

		[Theory]
		[InlineData(null, typeof(ArgumentNullException))]
		[InlineData("en", typeof(ArgumentException))] // too short
		[InlineData("engl", typeof(ArgumentException))] // too long
		[InlineData("e\x80g", typeof(ArgumentException))] // non-ASCII
		public void AudioChannel_IsoValidation(string iso, Type expectedException)
		{
			var ex = Assert.Throws(expectedException, () => new AudioDescriptor.AudioChannel(1, iso, 0, 0, false));
			Assert.NotNull(ex);
		}

		[Theory]
		[InlineData(8)] // does not fit in 3 bits
		[InlineData(255)]
		public void AudioChannel_BitStreamMode_OutOfRange_Throws(byte bsm)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new AudioDescriptor.AudioChannel(1, "eng", bsm, 0, false));
		}

		[Theory]
		[InlineData(16)] // does not fit in 4 bits
		[InlineData(255)]
		public void AudioChannel_NumChannels_OutOfRange_Throws(byte num)
		{
			Assert.Throws<ArgumentOutOfRangeException>(() => new AudioDescriptor.AudioChannel(1, "eng", 0, num, false));
		}


		[Fact]
		public void Encode_SetsReservedNibbleToF()
		{
			var d = new AudioDescriptor(); // zero channels
			var buf = new byte[d.PayloadBytes];
			d.Encode(buf);

			// Byte 4 holds the 4-bit count and 4-bit reserved. We expect reserved to be 0xF.
			// In typical MSB-first packing, the low nibble is reserved here.
			Assert.Equal(0xF, buf[4] & 0xF);
		}
	}
}