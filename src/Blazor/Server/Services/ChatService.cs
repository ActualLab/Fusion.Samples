using System.Collections.Generic;
using System.Security.Authentication;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Samples.Blazor.Abstractions;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework;

namespace Samples.Blazor.Server.Services;

public class ChatService : DbServiceBase<AppDbContext>, IChatService
{
    private readonly ILogger _log;
    private readonly IAuth _auth;
    private readonly IAuthBackend _authBackend;
    private readonly IForismaticClient _forismaticClient;

    public ChatService(
        IAuth auth,
        IAuthBackend authBackend,
        IForismaticClient forismaticClient,
        IServiceProvider services,
        ILogger<ChatService>? log = null)
        : base(services)
    {
        _log = log ??= NullLogger<ChatService>.Instance;
        _auth = auth;
        _authBackend = authBackend;
        _forismaticClient = forismaticClient;
    }

    // Commands

    public virtual async Task<ChatMessage> Post(
        IChatService.PostCommand command, CancellationToken cancellationToken = default)
    {
        var (text, session) = command;
        var context = CommandContext.GetCurrent();
        if (Computed.IsInvalidating()) {
            _ = PseudoGetAnyChatTail();
            return default!;
        }

        text = await NormalizeText(text, cancellationToken);
        var user = await GetCurrentUser(session, cancellationToken);
        if (user == null)
            throw new AuthenticationException("Please sign in first.");

        await using var dbContext = await CreateCommandDbContext(cancellationToken);
        var message = new ChatMessage() {
            CreatedAt = DateTime.UtcNow,
            UserId = user.Id,
            Text = text,
        };
        await dbContext.ChatMessages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    // Queries

    [ComputeMethod(KeepAliveTime = 61, AutoInvalidateTime = 60)]
    public virtual async Task<long> GetUserCount(CancellationToken cancellationToken = default)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Users.AsQueryable().LongCountAsync(cancellationToken);
    }

    [ComputeMethod(KeepAliveTime = 61, AutoInvalidateTime = 60)]
    public virtual async Task<long> GetActiveUserCount(CancellationToken cancellationToken = default)
    {
        var minLastSeenAt = (Clocks.SystemClock.Now - TimeSpan.FromMinutes(5)).ToDateTime();
        await using var dbContext = CreateDbContext();
        return await dbContext.Sessions.AsQueryable()
            .Where(s => s.LastSeenAt >= minLastSeenAt)
            .Select(s => s.UserId)
            .Distinct()
            .LongCountAsync(cancellationToken);
    }

    public virtual async Task<ChatUser> GetCurrentUser(Session session, CancellationToken cancellationToken = default)
    {
        var user = await _auth.GetUser(session, cancellationToken);
        return ToChatUser(user);
    }

    public virtual async Task<ChatUser> GetUser(long id, CancellationToken cancellationToken = default)
    {
        var user = await _authBackend.GetUser(id.ToString(), cancellationToken);
        return ToChatUser(user ?? throw new KeyNotFoundException());
    }

    public virtual async Task<ChatPage> GetChatTail(int length, CancellationToken cancellationToken = default)
    {
        await PseudoGetAnyChatTail();
        await using var dbContext = CreateDbContext();

        // Fetching messages from DB
        var messages = await dbContext.ChatMessages.AsQueryable()
            .OrderByDescending(m => m.Id)
            .Take(length)
            .ToListAsync(cancellationToken);
        messages.Reverse();

        // Fetching users via GetUserAsync
        var userIds = messages.Select(m => m.UserId).Distinct().ToArray();
        var userTasks = userIds.Select(id => GetUser(id, cancellationToken));
        var users = await Task.WhenAll(userTasks);

        // Composing the end result
        return new ChatPage(messages, users.ToDictionary(u => u.Id));
    }

    public virtual Task<ChatPage> GetChatPage(long minMessageId, long maxMessageId, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    // Helpers

    [ComputeMethod]
    protected virtual Task<Unit> PseudoGetAnyChatTail() => TaskExt.UnitTask;

    [CommandHandler(IsFilter = true, Priority = 1)]
    protected virtual async Task OnSignIn(SignInCommand command, CancellationToken cancellationToken)
    {
        var context = CommandContext.GetCurrent();
        await context.InvokeRemainingHandlers(cancellationToken);
        if (Computed.IsInvalidating()) {
            var isNewUser = context.Operation().Items.GetOrDefault(false);
            if (isNewUser) {
                _ = GetUserCount(default);
                _ = GetActiveUserCount(default);
            }
        }
    }

    private async ValueTask<string> NormalizeText(
        string text, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(text))
            return text;
        var json = await _forismaticClient.GetQuote(cancellationToken: cancellationToken);
        var jObject = JObject.Parse(json);
        return jObject.Value<string>("quoteText")!;
    }

    private ChatUser ToChatUser(User? user)
    {
        if (user == null || !long.TryParse(user.Id, out var userId))
            return ChatUser.None;
        return new() {
            Id = userId,
            Name = user.Name,
        };
    }
}
