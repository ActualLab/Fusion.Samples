using System.Runtime.Serialization;
using MemoryPack;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant), GenerateShape]
public sealed partial class HelloReply
{
    [DataMember(Order = 0), MemoryPackOrder(0), NbKey(0)] public Hello Response { get; set; } = null!;
}
