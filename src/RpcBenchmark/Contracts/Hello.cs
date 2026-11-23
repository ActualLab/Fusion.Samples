using System.Runtime.Serialization;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, GenerateShape]
public sealed partial class Hello
{
    [DataMember(Order = 0), NbKey(0)] public string Name { get; set; } = null!;
    [DataMember(Order = 1), NbKey(1)] public double Double { get; set; }
    [DataMember(Order = 2), NbKey(2)] public float Float { get; set; }
    [DataMember(Order = 3), NbKey(3)] public bool Bool { get; set; }
    [DataMember(Order = 4), NbKey(4)] public int Int32 { get; set; }
    [DataMember(Order = 5), NbKey(5)] public long Int64 { get; set; }
    [DataMember(Order = 6), NbKey(6)] public string? ChoiceString { get; set; }
    [DataMember(Order = 7), NbKey(7)] public bool? ChoiceBool { get; set; }
    [DataMember(Order = 8), NbKey(8)] public Pet[] Pets { get; set; } = null!;
}
