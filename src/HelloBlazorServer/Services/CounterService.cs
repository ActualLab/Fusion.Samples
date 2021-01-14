using System;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Fusion;

namespace Samples.HelloBlazorServer.Services
{
    [ComputeService]
    public class CounterService
    {
        private readonly object _lock = new object();
        private int _count;
        private DateTime _changeTime = DateTime.Now;

        [ComputeMethod]
        public virtual Task<(int, DateTime)> GetCounterAsync()
        {
            lock (_lock) {
                return Task.FromResult((_count, _changeTime));
            }
        }

        public Task IncrementCounterAsync()
        {
            lock (_lock) {
                ++_count;
                _changeTime = DateTime.Now;
            }
            using (Computed.Invalidate())
                GetCounterAsync();
            return Task.CompletedTask;
        }
    }
}
