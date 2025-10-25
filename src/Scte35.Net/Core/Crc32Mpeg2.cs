namespace Scte35.Net.Core;

public static class Crc32Mpeg2
{
    private const uint Poly = 0x04C11DB7u;
    private static readonly uint[] Table = BuildTable();

    private static uint[] BuildTable()
    {
        var t = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint c = i << 24;
            for (int j = 0; j < 8; j++)
                c = (c & 0x80000000u) != 0 ? (c << 1) ^ Poly : (c << 1);
            t[i] = c;
        }
        return t;
    }

    public static uint Compute(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFu;
        foreach (byte b in data)
        {
            uint idx = ((crc >> 24) ^ b) & 0xFFu;
            crc = (crc << 8) ^ Table[idx];
        }
        return crc;
    }
    
    // slower
    public static uint ComputePsiCrc32(ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFFFFFFU;
        foreach (var b in data)
        {
            crc ^= (uint)(b << 24);
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x80000000U) != 0 ? (crc << 1) ^ Poly : (crc << 1);
            }
        }

        return crc;
    }
}