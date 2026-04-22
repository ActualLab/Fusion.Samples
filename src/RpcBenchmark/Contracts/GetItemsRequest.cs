using System.Runtime.Serialization;
using MemoryPack;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant), GenerateShape]
public sealed partial class GetItemsRequest
{
    [DataMember(Order = 0), MemoryPackOrder(0), NbKey(0)] public int DataSize { get; set; }
    [DataMember(Order = 1), MemoryPackOrder(1), NbKey(1)] public int DelayEvery { get; set; } = 1;
    [DataMember(Order = 2), MemoryPackOrder(2), NbKey(2)] public int Count { get; set; }
}
