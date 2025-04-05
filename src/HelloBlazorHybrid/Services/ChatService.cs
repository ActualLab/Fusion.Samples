using Samples.HelloBlazorHybrid.Abstractions;

namespace Samples.HelloBlazorHybrid.Services;

public class ChatService : IChatService
{
    private volatile ImmutableList<ChatMessage> _messages = ImmutableList<ChatMessage>.Empty;

    // ReSharper disable once ChangeFieldTypeToSystemThreadingLock
    private readonly object _lock = new();

    [CommandHandler]
    public virtual Task PostMessage(Chat_Post command, CancellationToken cancellationToken = default)
    {
        var (name, message) = command;
        lock (_lock) {
            _messages = _messages.Add(new(DateTime.Now, name, message));
        }

        using (Invalidation.Begin()) {
            _ = GetMessageCount();
            _ = PseudoGetAnyTail();
        }
        return Task.CompletedTask;
    }

    [ComputeMethod]
    public virtual Task<int> GetMessageCount()
        => Task.FromResult(_messages.Count);

    [ComputeMethod]
    public virtual async Task<ChatMessage[]> GetMessages(
        int count, CancellationToken cancellationToken = default)
    {
        // Fake dependency used to invalidate all GetMessages(...) independently on count argument
        await PseudoGetAnyTail();
        return _messages.TakeLast(count).ToArray();
    }

    [ComputeMethod]
    public virtual Task<Unit> PseudoGetAnyTail() => TaskExt.UnitTask;
}
