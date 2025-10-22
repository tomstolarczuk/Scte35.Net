using Scte35.Net.Core;
using Scte35.Net.Model.Enums;

namespace Scte35.Net.Model.SpliceCommand;

public interface ISpliceCommand : IBinarySerializable
{
    SpliceCommandType Type { get; }
}
