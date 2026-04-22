using System.Runtime.Serialization;
using MemoryPack;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant), GenerateShape]
public sealed partial class User : IHasId<long>
{
    [DataMember(Order = 0), MemoryPackOrder(0), NbKey(0)] public long Id { get; set; }
    [DataMember(Order = 1), MemoryPackOrder(1), NbKey(1)] public long Version { get; set; }
    [DataMember(Order = 2), MemoryPackOrder(2), NbKey(2)] public DateTime CreatedAt { get; set; }
    [DataMember(Order = 3), MemoryPackOrder(3), NbKey(3)] public DateTime ModifiedAt { get; set; }
    [DataMember(Order = 4), MemoryPackOrder(4), NbKey(4)] public string Name { get; set; } = "";
}
