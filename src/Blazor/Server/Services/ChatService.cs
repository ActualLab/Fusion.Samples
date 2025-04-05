using Newtonsoft.Json.Linq;
using Samples.Blazor.Abstractions;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Samples.Blazor.Server.Services;

public class ChatService(
    IAuth auth,
    IAuthBackend authBackend,
    IForismaticClient forismaticClient,
    IServiceProvider services)
    : DbServiceBase<AppDbContext>(services), IChatService
{
    // Commands

    public virtual async Task<ChatMessage> Post(
        Chat_Post command, CancellationToken cancellationToken = default)
    {
        var (text, session) = command;
        var context = CommandContext.GetCurrent();
        if (Invalidation.IsActive) {
            _ = PseudoGetAnyChatTail();
            return default!;
        }

        text = await NormalizeText(text, cancellationToken);
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
            var isNewUser = context.Operation.Items.GetOrDefault(false);
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
        var json = await forismaticClient.GetQuote(cancellationToken: cancellationToken);
        var jObject = JObject.Parse(json);
        return jObject.Value<string>("quoteText")!;
    }
}
