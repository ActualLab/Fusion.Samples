using System.ComponentModel;
using Ookii.CommandLine;
using Ookii.CommandLine.Commands;
using Ookii.CommandLine.Validation;
using ActualLab.OS;

namespace Samples.RpcBenchmark;

public abstract class BenchmarkCommandBase : AsyncCommandBase
{
    [CommandLineArgument]
    [Description("Min. thread pool thread count.")]
    [ValueDescription("Number")]
    [ValidateRange(1, null)]
    [Alias("wt")]
    public int MinWorkerThreads { get; set; } = HardwareInfo.ProcessorCount;

    [CommandLineArgument]
    [Description("Min. thread pool IO thread count.")]
    [ValueDescription("Number")]
    [ValidateRange(1, null)]
    [Alias("iot")]
    public int MinIOThreads { get; set; } = HardwareInfo.ProcessorCount * 10;

    [CommandLineArgument]
    [Description("RpcSerializationFormat to use in ActualLab.Rpc tests.")]
    [ValueDescription("mempack5c,mempack5,msgpack5c,msgpack5,json5,njson5,...")]
    [Alias("f")]
    public string SerializationFormat { get; set; } = "mempack5c";
}
