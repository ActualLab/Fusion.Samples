using Samples.Blazor.Abstractions;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Samples.Blazor.Server.Services;

public class ChatService(
    IAuth auth,
    IAuthBackend authBackend,
    IServiceProvider services)
    : DbServiceBase<AppDbContext>(services), IChatService
{
    private static readonly string[] Quotes = [
        "The only way to do great work is to love what you do.",
        "Simplicity is the ultimate sophistication.",
        "Programs must be written for people to read, and only incidentally for machines to execute.",
        "There are only two hard things in computer science: cache invalidation and naming things.",
        "Premature optimization is the root of all evil.",
        "Talk is cheap. Show me the code.",
        "Any sufficiently advanced technology is indistinguishable from magic.",
        "Making it work is easy; making it fast is harder; making it simple is hardest.",
        "A distributed system is one where a machine you've never heard of can break your own.",
        "Perfection is achieved not when there is nothing more to add, but when there is nothing left to take away.",
    ];

    // Commands

    public virtual async Task<ChatMessage> Post(
        Chat_Post command, CancellationToken cancellationToken = default)
    {
        var (text, session) = command;
        if (Invalidation.IsActive) {
            _ = PseudoGetAnyChatTail();
            return default!;
        }

        text = NormalizeText(text);
        var user = await auth.GetUser(session, cancellationToken).Require();

        await using var dbContext = await DbHub.CreateOperationDbContext(cancellationToken);
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

    [ComputeMethod(AutoInvalidationDelay = 60)]
    public virtual async Task<long> GetUserCount(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await DbHub.CreateDbContext(cancellationToken);
        return await dbContext.Users.AsQueryable().LongCountAsync(cancellationToken);
    }

    [ComputeMethod(AutoInvalidationDelay = 60)]
    public virtual async Task<long> GetActiveUserCount(CancellationToken cancellationToken = default)
    {
        var minLastSeenAt = (Clocks.SystemClock.Now - TimeSpan.FromMinutes(5)).ToDateTime();
        await using var dbContext = await DbHub.CreateDbContext(cancellationToken);
        return await dbContext.Sessions.AsQueryable()
            .Where(s => s.LastSeenAt >= minLastSeenAt)
            .Select(s => s.UserId)
            .Distinct()
            .LongCountAsync(cancellationToken);
    }

    public virtual async Task<ChatMessageList> GetChatTail(int length, CancellationToken cancellationToken = default)
    {
        await PseudoGetAnyChatTail();
        await using var dbContext = await DbHub.CreateDbContext(cancellationToken);

        // Fetching messages from DB
        var messages = await dbContext.ChatMessages.AsQueryable()
            .OrderByDescending(m => m.Id)
            .Take(length)
            .ToListAsync(cancellationToken);
        messages.Reverse();

        // Fetching users via GetUserAsync
        var userIds = messages.Select(m => m.UserId).Distinct().ToArray();
        var userTasks = userIds.Select(async id => {
            var user = await authBackend.GetUser("", id, cancellationToken);
            return user.OrGuest("<Deleted user>").ToClientSideUser();
        });
        var users = (await Task.WhenAll(userTasks)).OfType<User>();

        // Composing the end result
        return new ChatMessageList() {
            Messages = [..messages],
            Users = users.ToImmutableDictionary(u => u.Id),
        };
    }

    // Helpers

    [ComputeMethod]
    protected virtual Task<Unit> PseudoGetAnyChatTail() => TaskExt.UnitTask;

    [CommandHandler(IsFilter = true, Priority = 1)]
    protected virtual async Task OnSignIn(AuthBackend_SignIn command, CancellationToken cancellationToken)
    {
        var context = CommandContext.GetCurrent();
        await context.InvokeRemainingHandlers(cancellationToken);
        if (Invalidation.IsActive) {
            // Built-in AuthBackend_SignIn command handler sets this flag:
            var isNewUser = context.Operation.Items.KeylessGet(false);
            if (isNewUser) {
                _ = GetUserCount(default);
                _ = GetActiveUserCount(default);
            }
        }
    }

    private static string NormalizeText(string text)
        => text.IsNullOrEmpty() ? Quotes[Random.Shared.Next(Quotes.Length)] : text;
}
