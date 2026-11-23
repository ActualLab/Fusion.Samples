using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using ActualLab.Fusion.Authentication;

namespace Samples.Blazor.Abstractions;

// Entity
[Index(nameof(UserId))]
[Index(nameof(CreatedAt))]
[DataContract]
public partial class ChatMessage
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [DataMember] public long Id { get; set; }
    [DataMember] public string UserId { get; set; } = "";
    [DataMember] public DateTime CreatedAt { get; set; }
    [Required, MaxLength(4000)]
    [DataMember] public string Text { get; set; } = "";
}

[DataContract]
public partial record ChatMessageList
{
    // Must be sorted by ChatMessage.Id
    [DataMember] public ImmutableArray<ChatMessage> Messages { get; init; } = ImmutableArray<ChatMessage>.Empty;
    [DataMember] public ImmutableDictionary<string, User> Users { get; init; } = ImmutableDictionary<string, User>.Empty;
}

public interface IChatService : IComputeService
{
    // Commands
    [CommandHandler]
    Task<ChatMessage> Post(Chat_Post command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod]
    Task<ChatMessageList> GetChatTail(int length, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<long> GetUserCount(CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<long> GetActiveUserCount(CancellationToken cancellationToken = default);
}

[DataContract]
// ReSharper disable once InconsistentNaming
public partial record Chat_Post(
    [property: DataMember] string Text,
    [property: DataMember] Session Session
    ) : ISessionCommand<ChatMessage>;
