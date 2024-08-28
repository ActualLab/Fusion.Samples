using System.Runtime.Serialization;
using MemoryPack;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial class HelloReply
{
    [DataMember(Order = 0), MemoryPackOrder(0)] public Hello Response { get; set; } = null!;
}
