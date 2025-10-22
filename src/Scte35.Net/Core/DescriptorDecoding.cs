using Scte35.Net.Constants;

namespace Scte35.Net.Core;

public static class DescriptorDecoding
{
    public static void RequireCueIdentifier(ref BitReader r)
    {
        uint id = r.ReadUInt32();
        if (id != Scte35Constants.CueIdentifier)
            throw new InvalidOperationException("Unsupported descriptor identifier (expected CUEI).");
    }
}
