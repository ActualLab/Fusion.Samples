using System.Runtime.Serialization;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, GenerateShape]
public sealed partial class Pet {
    [DataMember(Order = 0), NbKey(0)] public string Name { get; set; } = null!;
    [DataMember(Order = 1), NbKey(1)] public Color Color { get; set; }
}
