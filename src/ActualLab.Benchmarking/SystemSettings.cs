using System.Runtime.InteropServices;
using ActualLab.Diagnostics;
using ActualLab.OS;
using ActualLab.Rpc;
using ActualLab.Rpc.Clients;
using ActualLab.Rpc.Infrastructure;
using ActualLab.Rpc.Serialization;
using ActualLab.Rpc.WebSockets;

namespace ActualLab.Benchmarking;

public static class SystemSettings
{
    private static readonly object Lock = new();
    private static bool _isApplied;

    public static void Apply(int minWorkerThreads, int minIOThreads, string serializationFormat)
    {
        lock (Lock) {
            if (_isApplied)
                return;

            // Thread pool
            ThreadPool.GetMinThreads(out var currentMinWorkerThreads, out var currentMinIOThreads);
            currentMinWorkerThreads = Math.Max(currentMinWorkerThreads, minWorkerThreads);
            currentMinIOThreads = Math.Max(currentMinIOThreads, minIOThreads);
            ThreadPool.SetMinThreads(currentMinWorkerThreads, currentMinIOThreads);
            ThreadPool.SetMaxThreads(16_384, 16_384);

            // ActualLab.* tweaks
            RuntimeInfo.IsServer = true;
            RpcDiagnosticsOptions.Default = RpcDiagnosticsOptions.Default with {
                CallTracerFactory = _ => null,
            };

            // ActualLab.Rpc serialization formats
            var custom = new RpcSerializationFormat("custom", // You can play with your custom settings here
                () => new RpcByteArgumentSerializerV4(MemoryPackByteSerializer.Default),
                peer => new RpcByteMessageSerializerV5(peer));
            var allFormats = RpcSerializationFormat.All.Add(custom);
            var key = (Symbol)serializationFormat.ToLowerInvariant();
            var selectedFormat = allFormats.FirstOrDefault(x => x.Key == key);
            if (selectedFormat == null) {
                Error.WriteLine($"Invalid serialization format: {key.Value}");
                Error.WriteLine($"Supported formats: {RpcSerializationFormat.All.Select(x => x.Key).ToDelimitedString()}");
                throw new ArgumentOutOfRangeException(nameof(serializationFormat));
            }
            RpcSerializationFormatResolver.Default = new RpcSerializationFormatResolver(selectedFormat.Key, allFormats);

            // RPC call tracking tweaks
            RpcLimits.Default = RpcLimits.Default with {
                CallTimeoutCheckPeriod = new RandomTimeSpan(15, 0.1), // 15s
            };

            // RPC argument and message serializer tweaks
            RpcArgumentSerializer.CopyThreshold = 1024; // Used only by (compute methods + remote computed cache)

            // WebSocketChannel<RpcMessage> settings.
            // They're here mostly for convenience - the values here are the same as the default ones.
            RpcWebSocketClientOptions.Default = RpcWebSocketClientOptions.Default with {
                WebSocketTransportOptionsFactory = (_, _) => RpcWebSocketTransport.Options.Default with {
                    FrameSize = 12_000, // 8 x 1500(MTU) minus some reserve
                    BufferSize = 16_000,
                    MaxBufferSize = 256_000,
                    // Use of UnboundedChannelOptions is totally fine here: if the message is enqueued
                    WriteChannelOptions = new UnboundedChannelOptions() {
                        // FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true, // Must be true
                        SingleWriter = false, // Must be false
                        AllowSynchronousContinuations = false, // Must be false, setting it to true will kill the throughput!
                    },
                }
            };

            WriteLine("System-wide settings:");
            WriteLine($"  .NET version:         {RuntimeInfo.DotNet.VersionString ?? RuntimeInformation.FrameworkDescription}");
            WriteLine($"  Thread pool settings: {currentMinWorkerThreads}+ worker, {currentMinIOThreads}+ I/O threads");
            WriteLine($"  Serialization format: {selectedFormat.Key} (affects only ActualLab.Rpc tests)");
            WriteLine($"  ActualLab.Fusion:     v{typeof(Computed).Assembly.GetInformationalVersion()}");
            _isApplied = true;
        }
    }
}
