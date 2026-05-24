using System.ComponentModel;
using Ookii.CommandLine;
using Ookii.CommandLine.Commands;

namespace Samples.RpcBenchmark;

[GeneratedParser]
[Command]
[Description("Starts both the client and the server part of this benchmark.")]
public partial class TestCommand : ClientCommand
{
    public override Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var (transport, format) = ParseSerializationFormat();
        SystemSettings.Apply(MinWorkerThreads, MinIOThreads, transport, format);
        var serverCommand = new ServerCommand() { Url = Url, SerializationFormat = SerializationFormat };
        _ = serverCommand.RunAsync(cancellationToken);
        return base.RunAsync(cancellationToken);
    }
}
