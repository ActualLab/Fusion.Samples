using System.Runtime.Serialization;
using MemoryPack;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, MemoryPackable(GenerateType.VersionTolerant), GenerateShape]
public sealed partial class Hello
{
    [DataMember(Order = 0), MemoryPackOrder(0), NbKey(0)] public string Name { get; set; } = null!;
    [DataMember(Order = 1), MemoryPackOrder(1), NbKey(1)] public double Double { get; set; }
    [DataMember(Order = 2), MemoryPackOrder(2), NbKey(2)] public float Float { get; set; }
    [DataMember(Order = 3), MemoryPackOrder(3), NbKey(3)] public bool Bool { get; set; }
    [DataMember(Order = 4), MemoryPackOrder(4), NbKey(4)] public int Int32 { get; set; }
    [DataMember(Order = 5), MemoryPackOrder(5), NbKey(5)] public long Int64 { get; set; }
    [DataMember(Order = 6), MemoryPackOrder(6), NbKey(6)] public string? ChoiceString { get; set; }
    [DataMember(Order = 7), MemoryPackOrder(7), NbKey(7)] public bool? ChoiceBool { get; set; }
    [DataMember(Order = 8), MemoryPackOrder(8), NbKey(8)] public Pet[] Pets { get; set; } = null!;
}
