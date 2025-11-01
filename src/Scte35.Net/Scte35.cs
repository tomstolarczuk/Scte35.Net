using Scte35.Net.Model;

namespace Scte35.Net
{
    public static class Scte35
    {
        // ---------------------------
        // Decode from raw bytes
        // ---------------------------
        public static SpliceInfoSection Decode(ReadOnlySpan<byte> data, Func<uint, bool>? privateIdRecognizer = null)
        {
            var sis = new SpliceInfoSection();
            if (privateIdRecognizer is not null)
                sis.PrivateCommandIdIsRecognized = privateIdRecognizer;

            sis.Decode(data);
            return sis;
        }

        public static bool TryDecode(ReadOnlySpan<byte> data, out SpliceInfoSection? section, Func<uint, bool>? privateIdRecognizer = null)
        {
            try
            {
                section = Decode(data, privateIdRecognizer);
                return true;
            }
            catch
            {
                section = null;
                return false;
            }
        }

        // ---------------------------
        // Decode from Base64 / Hex
        // ---------------------------
        public static SpliceInfoSection FromBase64(string base64, Func<uint, bool>? privateIdRecognizer = null)
        {
            var bytes = Convert.FromBase64String(base64);
            return Decode(bytes, privateIdRecognizer);
        }

        public static bool TryFromBase64(string base64, out SpliceInfoSection? section, Func<uint, bool>? privateIdRecognizer = null)
        {
            try
            {
                section = FromBase64(base64, privateIdRecognizer);
                return true;
            }
            catch
            {
                section = null;
                return false;
            }
        }

        public static SpliceInfoSection FromHex(string hex, Func<uint, bool>? privateIdRecognizer = null)
        {
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex[2..];

            var bytes = Convert.FromHexString(hex);
            return Decode(bytes, privateIdRecognizer);
        }

        public static bool TryFromHex(string hex, out SpliceInfoSection? section, Func<uint, bool>? privateIdRecognizer = null)
        {
            try
            {
                section = FromHex(hex, privateIdRecognizer);
                return true;
            }
            catch
            {
                section = null;
                return false;
            }
        }

        // ---------------------------
        // Encode to raw bytes
        // ---------------------------
        public static byte[] Encode(SpliceInfoSection section)
        {
            // SpliceInfoSection.PayloadBytes == header(3) + section_length bytes (includes CRC_32)
            int len = section.PayloadBytes;
            var buffer = new byte[len];
            section.Encode(buffer);
            return buffer;
        }

        // ---------------------------
        // Encode to Base64 / Hex
        // ---------------------------
        public static string ToBase64(SpliceInfoSection section)
            => Convert.ToBase64String(Encode(section));

        public static string ToHex(SpliceInfoSection section, bool lower = true)
        {
            var hex = Convert.ToHexString(Encode(section));
            return lower ? hex.ToLowerInvariant() : hex;
        }
    }
}
