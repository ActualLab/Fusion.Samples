using ActualLab.Rpc;

namespace Samples.HelloRpc;

public interface IClientNotifier : IRpcService
{
    Task Notify(string peerAddress, DateTime time, CancellationToken cancellationToken = default);
}

public class ClientNotifier : IClientNotifier
{
    public Task Notify(string peerAddress, DateTime time, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Server-to-client push: {time}");
        return Task.CompletedTask;
    }
}
