using System.Runtime.Serialization;
using MemoryPack;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class Item
{
    [DataMember(Order = 0), MemoryPackOrder(0)] public long Index { get; set; }
    [DataMember(Order = 1), MemoryPackOrder(1)] public byte[]? Data { get; set; }
}
