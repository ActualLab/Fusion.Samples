using System.Runtime.Serialization;

namespace Samples.HelloBlazorHybrid.Abstractions;

public interface IChatService : IComputeService
{
    [CommandHandler]
    Task PostMessage(Chat_Post command, CancellationToken cancellationToken = default);

    [ComputeMethod]
    Task<int> GetMessageCount();
    [ComputeMethod]
    Task<ChatMessage[]> GetMessages(int count, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<Unit> PseudoGetAnyTail();
}

[DataContract]
// ReSharper disable once InconsistentNaming
public partial record Chat_Post(
    [property: DataMember] string Name,
    [property: DataMember] string Text
    ) : ICommand<Unit>;
