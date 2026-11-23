using System.Runtime.Serialization;
using PolyType;
using NbKey = Nerdbank.MessagePack.KeyAttribute;

namespace Samples.RpcBenchmark;

[DataContract, GenerateShape]
public sealed partial class User : IHasId<long>
{
    [DataMember(Order = 0), NbKey(0)] public long Id { get; set; }
    [DataMember(Order = 1), NbKey(1)] public long Version { get; set; }
    [DataMember(Order = 2), NbKey(2)] public DateTime CreatedAt { get; set; }
    [DataMember(Order = 3), NbKey(3)] public DateTime ModifiedAt { get; set; }
    [DataMember(Order = 4), NbKey(4)] public string Name { get; set; } = "";
}
