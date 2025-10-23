using System.Text;
using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Tests.Model
{
	public class DtmfDescriptorTests
	{
		[Fact]
		public void Tag_IsDtmf()
		{
			var d = new DtmfDescriptor();
			Assert.Equal(SpliceDescriptorTag.Dtmf, d.Tag);
		}

		[Theory]
		[InlineData("", 0)]
		[InlineData("#", 5)]
		[InlineData("123", 200)]
		[InlineData("0123456", 255)]
		public void EncodeDecode_Roundtrip_UpTo7Chars(string chars, byte preroll)
		{
			var d = new DtmfDescriptor { Chars = chars, Preroll = preroll };
			var buf = new byte[d.PayloadBytes];

			d.Encode(buf);

			var d2 = new DtmfDescriptor();
			d2.Decode(buf);

			Assert.Equal(chars, d2.Chars);
			Assert.Equal(preroll, d2.Preroll);
		}

		[Fact]
		public void Encode_WritesCueIdentifierFirst()
		{
			var d = new DtmfDescriptor { Chars = "12#", Preroll = 7 };
			var buf = new byte[d.PayloadBytes];

			d.Encode(buf);

			Assert.Equal(Scte35Constants.CueIdentifier, BinaryExtensions.ReadUInt32BE(buf.AsSpan(0, 4)));
		}

		[Theory]
		[InlineData("", 0)]
		[InlineData("1", 1)]
		[InlineData("0123456", 6)]
		public void Encode_Sets3BitCount_And5BitReserved(string chars, byte preroll)
		{
			var d = new DtmfDescriptor { Chars = chars, Preroll = preroll };
			var buf = new byte[d.PayloadBytes];
			d.Encode(buf);

			var r = new BitReader(buf);
			Assert.Equal(Scte35Constants.CueIdentifier, r.ReadUInt32());
			Assert.Equal(preroll, r.ReadByte());

			uint count = r.ReadBits(3);
			uint reserved = r.ReadBits(5);
			Assert.Equal((uint)chars.Length, count);
			Assert.Equal((uint)0x1F, reserved);
		}

		[Fact]
		public void Decode_InvalidCueIdentifier_Throws()
		{
			// build a valid-structure payload, then overwrite the header.
			var d = new DtmfDescriptor { Chars = "12", Preroll = 9 };
			var buf = new byte[d.PayloadBytes];
			d.Encode(buf);

			BinaryExtensions.WriteUInt32BE(buf, 0xDEADBEEFu);
			var d2 = new DtmfDescriptor();
			Assert.ThrowsAny<Exception>(() => d2.Decode(buf));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(5)]
		public void Decode_TooShort_Throws(int len)
		{
			var d = new DtmfDescriptor();
			var buf = new byte[len];
			Assert.ThrowsAny<Exception>(() => d.Decode(buf));
		}

		[Fact]
		public void Encode_DestTooSmall_Throws()
		{
			var d = new DtmfDescriptor { Chars = "1234", Preroll = 1 };
			var tooSmall = new byte[d.PayloadBytes - 1];
			Assert.ThrowsAny<Exception>(() => d.Encode(tooSmall));
		}

		[Fact]
		public void Encode_LengthOver255_Throws()
		{
			var big = new string('1', 256);
			var d = new DtmfDescriptor { Chars = big, Preroll = 0 };
			Assert.Throws<InvalidOperationException>(() =>
			{
				var buf = new byte[6 + big.Length];
				d.Encode(buf);
			});
		}

		[Theory]
		[InlineData("A")]
		[InlineData("1A2")]
		[InlineData(" ")]
		[InlineData("+")]
		[InlineData("x")]
		public void Encode_InvalidChars_Throws(string chars)
		{
			var d = new DtmfDescriptor { Chars = chars, Preroll = 0 };
			var buf = new byte[Math.Max(6, d.PayloadBytes)];
			Assert.Throws<InvalidOperationException>(() => d.Encode(buf));
		}

		[Fact]
		public void Decode_InvalidChars_Throws()
		{
			// craft payload with an invalid char 'A' using BitWriter to set fields precisely.
			var wbuf = new byte[6 + 3];
			var w = new BitWriter(wbuf);
			w.WriteUInt32(Scte35Constants.CueIdentifier);
			w.WriteBits(2, 8); // preroll
			w.WriteBits(3, 3); // count = 3
			w.WriteBits(0x0F, 5); // reserved per encoder
			w.WriteBytesAligned("1A#"u8);

			var d = new DtmfDescriptor();
			Assert.Throws<InvalidOperationException>(() => d.Decode(wbuf));
		}

		[Fact]
		public void Decode_CountMismatch_Throws()
		{
			// count says 3, but we provide 4 chars -> mismatch
			var wbuf = new byte[6 + 4];
			var w = new BitWriter(wbuf);
			w.WriteUInt32(Scte35Constants.CueIdentifier);
			w.WriteBits(0x10, 8); // preroll
			w.WriteBits(3, 3); // count = 3
			w.WriteBits(0x0F, 5); // reserved
			w.WriteBytesAligned(Encoding.ASCII.GetBytes("0123"));

			var d = new DtmfDescriptor();
			var ex = Assert.Throws<InvalidOperationException>(() => d.Decode(wbuf));
			Assert.Contains("does not match remaining", ex.Message, StringComparison.OrdinalIgnoreCase);
		}

		[Fact]
		public void Encode_MoreThan7Chars_DecodeFailsDueTo3BitCount()
		{
			// encoder writes full payload, but only 3-bit count; decode should reject.
			var chars = "01234567"; // 8 chars, exceeds 3-bit max of 7
			var enc = new DtmfDescriptor { Chars = chars, Preroll = 0x55 };
			var buf = new byte[enc.PayloadBytes];
			enc.Encode(buf);

			var dec = new DtmfDescriptor();
			Assert.Throws<InvalidOperationException>(() => dec.Decode(buf));
		}

		[Fact]
		public void Encode_BufferContent_IsDeterministic()
		{
			var d = new DtmfDescriptor { Chars = "012345", Preroll = 42 };
			var buf1 = new byte[d.PayloadBytes];
			var buf2 = new byte[d.PayloadBytes];

			d.Encode(buf1);
			d.Encode(buf2);

			Assert.Equal(buf1, buf2);
		}
	}
}