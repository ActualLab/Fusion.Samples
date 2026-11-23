using System.Runtime.Serialization;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, GenerateShape]
public sealed partial class HelloRequest
{
    [DataMember(Order = 0), NbKey(0)] public Hello Request { get; set; } = null!;
}
