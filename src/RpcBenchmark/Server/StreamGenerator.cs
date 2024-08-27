using System.Runtime.CompilerServices;

namespace Samples.RpcBenchmark.Server;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

public static class StreamGenerator
{
    private static readonly object Lock = new();
    private static Dictionary<int, byte[]> _dataCache = new();

    public static async IAsyncEnumerable<Item> GetItems(
        GetItemsRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (request.DataSize < 0)
            throw new ArgumentOutOfRangeException(nameof(request));

        for (var i = 0; i < request.Count; i++) {
            cancellationToken.ThrowIfCancellationRequested();
            if (request.DelayEvery > 0 && (i % request.DelayEvery) == 0)
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);

            yield return new Item() {
                Index = i,
                Data = GetData(request.DataSize),
            };
        }
    }

    public static byte[] GetData(int size)
    {
        if (size < 50)
            return new byte[size];

        // This approach is faster than w/ ConcurrentDictionary, if the # of items is tiny
        if (_dataCache.TryGetValue(size, out var data))
            return data;
        lock (Lock) {
            if (_dataCache.TryGetValue(size, out data))
                return data;

            data = new byte[size];
            _dataCache = new Dictionary<int, byte[]>(_dataCache) { { size, data } };
            return data;
        }
    }
}
