using System.Runtime.Serialization;
using MemoryPack;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class Pet {
    [DataMember(Order = 0), MemoryPackOrder(0)] public string Name { get; set; } = null!;
    [DataMember(Order = 1), MemoryPackOrder(1)] public Color Color { get; set; }
}
