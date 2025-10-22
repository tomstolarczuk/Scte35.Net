using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceDescriptor;

public interface ISpliceDescriptor : IBinarySerializable
{
    SpliceDescriptorTag Tag { get; }
}
