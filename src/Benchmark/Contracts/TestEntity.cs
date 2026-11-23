using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Samples.Benchmark;

[DataContract]
public sealed partial record TestItem : IHasId<long>
{
    [Key]
    [DataMember] public long Id { get; init; }
    [DataMember] public long Version { get; init; }
    [DataMember] public DateTime CreatedAt { get; init; }
    [DataMember] public DateTime ModifiedAt { get; init; }
    [DataMember] public string Name { get; init; } = "";
}
