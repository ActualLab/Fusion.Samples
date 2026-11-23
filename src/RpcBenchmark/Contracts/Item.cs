using System.Runtime.Serialization;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, GenerateShape]
public sealed partial class Item
{
    [DataMember(Order = 0), NbKey(0)] public long Index { get; set; }
    [DataMember(Order = 1), NbKey(1)] public byte[]? Data { get; set; }
}
