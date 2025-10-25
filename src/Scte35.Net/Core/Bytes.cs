namespace Scte35.Net.Core
{
	public static class Bytes
	{
		private static readonly char[] HexLower = "0123456789abcdef".ToCharArray();

		public static byte[] FromHex(string hex)
		{
			if (hex == null) throw new ArgumentNullException(nameof(hex));
			if ((hex.Length & 1) != 0) throw new FormatException("Hex length must be even.");

			int len = hex.Length / 2;
			byte[] result = new byte[len];

			for (int i = 0; i < len; i++)
			{
				int hi = FromHexNibble(hex[2 * i]);
				int lo = FromHexNibble(hex[2 * i + 1]);
				if ((hi | lo) < 0) throw new FormatException("Invalid hex character.");
				result[i] = (byte)((hi << 4) | lo);
			}

			return result;
		}

		public static bool TryFromHex(string hex, out byte[] bytes)
		{
			try
			{
				bytes = FromHex(hex);
				return true;
			}
			catch
			{
				bytes = [];
				return false;
			}
		}

		public static string ToHex(ReadOnlySpan<byte> data, bool lower = true)
		{
			var alpha = lower ? HexLower : "0123456789ABCDEF".ToCharArray();
			char[] chars = new char[data.Length * 2];
			for (int i = 0, c = 0; i < data.Length; i++)
			{
				byte b = data[i];
				chars[c++] = alpha[b >> 4];
				chars[c++] = alpha[b & 0xF];
			}

			return new string(chars);
		}

		public static byte[] FromBase64(string s)
			=> Convert.FromBase64String(s);

		public static string ToBase64(ReadOnlySpan<byte> data)
			=> Convert.ToBase64String(data.ToArray());

		private static int FromHexNibble(char c)
		{
			if ((uint)(c - '0') <= 9) return c - '0';
			if ((uint)(c - 'a') <= 5) return c - 'a' + 10;
			if ((uint)(c - 'A') <= 5) return c - 'A' + 10;
			return -1;
		}
	}
}