using System.Runtime.InteropServices;
using ActualLab.Diagnostics;
using ActualLab.OS;
using ActualLab.Rpc;
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
            RpcDefaultDelegates.CallTracerFactory = _ => null;

            // ActualLab.Rpc serialization formats
            var custom = new RpcSerializationFormat("custom", // You can play with your custom settings here
                () => new RpcByteArgumentSerializerV3(MemoryPackByteSerializer.Default),
                peer => new RpcByteMessageSerializerV4(peer) { AllowProjection = true });
            var allFormats = RpcSerializationFormat.All.Add(custom);
            var key = (Symbol)serializationFormat.ToLowerInvariant();
            var selectedFormat = allFormats.FirstOrDefault(x => x.Key == key);
            if (selectedFormat == null) {
                Error.WriteLine($"Invalid serialization format: {key.Value}");
                Error.WriteLine($"Supported formats: {RpcSerializationFormat.All.Select(x => x.Key).ToDelimitedString()}");
                throw new ArgumentOutOfRangeException(nameof(serializationFormat));
            }
            RpcSerializationFormatResolver.Default = new RpcSerializationFormatResolver(selectedFormat.Key, allFormats.ToArray());

            // RPC argument and message serializer tweaks
            RpcArgumentSerializer.CopyThreshold = 1024;
            RpcByteMessageSerializer.Defaults.AllowProjection = true; // Improves large object deserialization performance

            // WebSocketChannel<RpcMessage> settings.
            // They're here mostly for convenience - the values here are the same as the default ones.
            RpcDefaultDelegates.WebSocketChannelOptionsProvider =
                (peer, _) => WebSocketChannel<RpcMessage>.Options.Default with {
                    Serializer = peer.Hub.SerializationFormats.Get(peer.Ref).MessageSerializerFactory.Invoke(peer),
                    WriteFrameSize = 12_000, // 8 x 1500(MTU) minus some reserve
                    MinReadBufferSize = 24_000,
                    MinWriteBufferSize = 24_000,
                    RetainedBufferSize = 120_000,
                    ReadMode = ChannelReadMode.Unbuffered, // Unbuffered is faster (and it's the default read mode)
                    // ReadChannelOptions are unused when ReadMode == ChannelReadMode.Unbuffered
                    ReadChannelOptions = new BoundedChannelOptions(240) {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = true,
                        AllowSynchronousContinuations = false,
                    },
                    WriteChannelOptions = new BoundedChannelOptions(240) {
                        FullMode = BoundedChannelFullMode.Wait,
                        SingleReader = true,
                        SingleWriter = false,
                        AllowSynchronousContinuations = false,
                    },
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
