using ActualLab.Rpc;
using ActualLab.Rpc.Infrastructure;
using ActualLab.Rpc.Serialization;
using ActualLab.Rpc.WebSockets;
using Ookii.CommandLine;
using Ookii.CommandLine.Commands;

namespace Samples.RpcBenchmark;

public static class Program
{
    public static readonly string DefaultUrl = "https://localhost:22444/";
    public static readonly CancellationTokenSource StopTokenSource = new();
    public static readonly CancellationToken StopToken = StopTokenSource.Token;

    public static async Task<int> Main(string[] args)
    {
        SizeHintProviders.Register<Item>(static x => 16 + x.Data?.Length ?? 0);

        TreatControlCAsInput = false;
        CancelKeyPress += (_, ea) => {
            StopTokenSource.Cancel();
            ea.Cancel = true;
        };

        if (args.Length == 0)
            return await new TestCommand().RunAsync();

        var options = new CommandOptions() {
            ArgumentNamePrefixes = new [] { "-" },
            CommandNameTransform = NameTransform.DashCase,
            ArgumentNameTransform = NameTransform.DashCase,
            StripCommandNameSuffix = "Command",
        };
        var commandManager = new CommandManager(options);
        return await commandManager.RunCommandAsync(args) ?? 1;
    }
}
