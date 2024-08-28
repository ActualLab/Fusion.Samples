using Samples.Blazor.Abstractions;
using ActualLab.Fusion.Authentication;
using ActualLab.Fusion.Extensions;
using ActualLab.Fusion.UI;
using ActualLab.Rpc;
using ActualLab.Rpc.Infrastructure;
using ActualLab.Rpc.Serialization;
using ActualLab.Rpc.WebSockets;
using static System.Console;

RpcDefaultDelegates.WebSocketChannelOptionsProvider =
    (_, _) => WebSocketChannel<RpcMessage>.Options.Default with {
        // We should use the same serializer as on the server side
        Serializer = new FastRpcMessageByteSerializer(MemoryPackByteSerializer.Default),
    };

var services = CreateServiceProvider();
var stateFactory = services.StateFactory();
var chat = services.GetRequiredService<IChatService>();
var seenMessageIds = new ConcurrentDictionary<long, Unit>();
using var timeState = stateFactory.NewComputed<ChatMessageList>(async (s, cancellationToken) => {
    var chatPage = await chat.GetChatTail(10, cancellationToken);
    foreach (var message in chatPage.Messages) {
        if (!seenMessageIds.TryAdd(message.Id, default))
            continue;
        WriteLine($"{chatPage.Users[message.UserId].Name}: {message.Text}");
    }
    return chatPage;
});
WriteLine("ComputedState created, waiting for new chat messages.");
WriteLine("Press <Enter> to stop.");
ReadLine();

static IServiceProvider CreateServiceProvider()
{
    var services = new ServiceCollection();
    services.AddLogging(logging => {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Warning);
        logging.AddConsole();
    });

    var baseUri = new Uri("http://localhost:5005");
    var apiBaseUri = new Uri($"{baseUri}api/");

    // Fusion
    var fusion = services.AddFusion();
    fusion.Rpc.AddWebSocketClient(baseUri);
    fusion.AddAuthClient();

    // Fusion services
    fusion.AddFusionTime();
    fusion.AddClient<ITimeService>();
    fusion.AddClient<IScreenshotService>();
    fusion.AddClient<IChatService>();
    fusion.AddClient<IComposerService>();
    fusion.AddClient<ISumService>();

    // Default update delay is 0.1s
    services.AddTransient<IUpdateDelayer>(c => new UpdateDelayer(c.UIActionTracker(), 0.1));

    return services.BuildServiceProvider();
}
