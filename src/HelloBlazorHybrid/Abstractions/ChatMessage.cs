using System.Runtime.Serialization;

namespace Samples.HelloBlazorHybrid.Abstractions;

[DataContract]
public partial record ChatMessage(
    [property: DataMember] DateTime Time,
    [property: DataMember] string Name,
    [property: DataMember] string Text
);
