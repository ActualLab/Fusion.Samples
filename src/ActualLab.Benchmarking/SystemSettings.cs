using System.Diagnostics;
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

            // Common ActualLab.Rpc tweaks
            RpcDefaults.Mode = RpcMode.Server;
            RpcDefaultDelegates.CallTracerFactory = _ => null;

            // ActualLab.Rpc serialization formats
            var custom = new RpcSerializationFormat("custom", // You can play with your custom settings here
                () => new RpcByteArgumentSerializer(MemoryPackByteSerializer.Default),
                peer => new RpcByteMessageSerializer(peer) { AllowProjection = true });
            var allFormats = RpcSerializationFormat.All.Add(custom);
            var key = (Symbol)serializationFormat.ToLowerInvariant();
            var selectedFormat = allFormats.FirstOrDefault(x => x.Key == key);
            if (selectedFormat == null) {
                Error.WriteLine($"Invalid serialization format: {key.Value}");
                Error.WriteLine($"Supported formats: {RpcSerializationFormat.All.Select(x => x.Key).ToDelimitedString()}");
                throw new ArgumentOutOfRangeException(nameof(serializationFormat));
            }
            RpcSerializationFormatResolver.Default = new RpcSerializationFormatResolver(selectedFormat.Key, allFormats.ToArray());

            // RpcByteArgumentSerializer.CopySizeThreshold = 1024;
            RpcDefaultDelegates.WebSocketChannelOptionsProvider =
                (peer, _) => WebSocketChannel<RpcMessage>.Options.Default with {
                    FrameDelayerFactory = null, // Super important: by default there is a frame delayer
                    Serializer = peer.Hub.SerializationFormats.Get(peer.Ref).MessageSerializerFactory.Invoke(peer),
                    MinReadBufferSize = 16_384,
                    MinWriteBufferSize = 16_384,
                    WriteFrameSize = 8_900, // 6 x 1500(MTU) minus some reserve
                };

            WriteLine("System-wide settings:");
            WriteLine($"  Thread pool settings: {currentMinWorkerThreads}+ worker, {currentMinIOThreads}+ I/O threads");
            WriteLine($"  Serialization format: {selectedFormat.Key} (affects only ActualLab.Rpc tests)");
            _isApplied = true;
        }
    }

}
