using System.Runtime.Serialization;
using MemoryPack;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant), GenerateShape]
public sealed partial class Item
{
    [DataMember(Order = 0), MemoryPackOrder(0), NbKey(0)] public long Index { get; set; }
    [DataMember(Order = 1), MemoryPackOrder(1), NbKey(1)] public byte[]? Data { get; set; }
}
