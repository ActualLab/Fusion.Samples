using System.Runtime.Serialization;
using ActualLab.Fusion.Authentication;

namespace Samples.Blazor.Abstractions;

[DataContract]
public partial record ComposedValue
{
    [DataMember] public string Parameter { get; init; } = "";
    [DataMember] public double Uptime { get; init; }
    [DataMember] public double? Sum { get; init; }
    [DataMember] public string LastChatMessage { get; init; } = "";
    [DataMember] public User? User { get; init; }
    [DataMember] public long ActiveUserCount { get; init; }
}

public interface IComposerService : IComputeService
{
    [ComputeMethod]
    Task<ComposedValue> GetComposedValue(
        Session session, string parameter,
        CancellationToken cancellationToken = default);
}

public interface ILocalComposerService : IComposerService { }
