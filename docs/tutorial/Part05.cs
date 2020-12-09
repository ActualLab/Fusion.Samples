using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Swapping;
using static System.Console;

namespace Tutorial
{
    public static class Part05
    {
        #region Part05_Service1
        [ComputeService] // You don't need this attribute if you manually register such services
        public class Service1
        {
            [ComputeMethod]
            public virtual async Task<string> GetAsync(string key)
            {
                WriteLine($"{nameof(GetAsync)}({key})");
                return key;
            }
        }

        public static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddFusion();
            services.AttributeBased().AddServicesFrom(Assembly.GetExecutingAssembly());
            return services.BuildServiceProvider();
        }
        #endregion

        public static async Task Caching1()
        {
            #region Part05_Caching1
            var service = CreateServices().GetRequiredService<Service1>();
            // var computed = await Computed.CaptureAsync(_ => counters.GetAsync("a"));
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("a"));
            GC.Collect();
            WriteLine("GC.Collect()");
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("a"));
            #endregion
        }

        public static async Task Caching2()
        {
            #region Part05_Caching2
            var service = CreateServices().GetRequiredService<Service1>();
            var computed = await Computed.CaptureAsync(_ => service.GetAsync("a"));
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("a"));
            GC.Collect();
            WriteLine("GC.Collect()");
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("a"));
            #endregion
        }

        #region Part05_Service2
        [ComputeService]
        public class Service2
        {
            [ComputeMethod]
            public virtual async Task<string> GetAsync(string key)
            {
                WriteLine($"{nameof(GetAsync)}({key})");
                return key;
            }

            [ComputeMethod]
            public virtual async Task<string> CombineAsync(string key1, string key2)
            {
                WriteLine($"{nameof(CombineAsync)}({key1}, {key2})");
                return await GetAsync(key1) + await GetAsync(key2);
            }
        }
        #endregion

        public static async Task Caching3()
        {
            #region Part05_Caching3
            var service = CreateServices().GetRequiredService<Service2>();
            var computed = await Computed.CaptureAsync(_ => service.CombineAsync("a", "b"));
            WriteLine("computed = CombineAsync(a, b) completed");
            WriteLine(await service.CombineAsync("a", "b"));
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("b"));
            WriteLine(await service.CombineAsync("a", "c"));
            GC.Collect();
            WriteLine("GC.Collect() completed");
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("b"));
            WriteLine(await service.CombineAsync("a", "c"));
            #endregion
        }

        public static async Task Caching4()
        {
            #region Part05_Caching4
            var service = CreateServices().GetRequiredService<Service2>();
            var computed = await Computed.CaptureAsync(_ => service.GetAsync("a"));
            WriteLine("computed = GetAsync(a) completed");
            WriteLine(await service.CombineAsync("a", "b"));
            GC.Collect();
            WriteLine("GC.Collect() completed");
            WriteLine(await service.CombineAsync("a", "b"));
            #endregion
        }

        #region Part05_Service3
        [ComputeService]
        public class Service3
        {
            [ComputeMethod]
            public virtual async Task<string> GetAsync(string key)
            {
                WriteLine($"{nameof(GetAsync)}({key})");
                return key;
            }

            [ComputeMethod(KeepAliveTime = 0.3)] // KeepAliveTime was added
            public virtual async Task<string> CombineAsync(string key1, string key2)
            {
                WriteLine($"{nameof(CombineAsync)}({key1}, {key2})");
                return await GetAsync(key1) + await GetAsync(key2);
            }
        }
        #endregion

        public static async Task Caching5()
        {
            #region Part05_Caching5
            var service = CreateServices().GetRequiredService<Service3>();
            WriteLine(await service.CombineAsync("a", "b"));
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("x"));
            GC.Collect();
            WriteLine("GC.Collect()");
            WriteLine(await service.CombineAsync("a", "b"));
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("x"));
            await Task.Delay(1000);
            GC.Collect();
            WriteLine("Task.Delay(...) and GC.Collect()");
            WriteLine(await service.CombineAsync("a", "b"));
            WriteLine(await service.GetAsync("a"));
            WriteLine(await service.GetAsync("x"));
            #endregion
        }

        #region Part05_Service4
        [ComputeService]
        public class Service4
        {
            [ComputeMethod(KeepAliveTime = 1), Swap(0.1)]
            public virtual async Task<string> GetAsync(string key)
            {
                WriteLine($"{nameof(GetAsync)}({key})");
                return key;
            }
        }

        [Service(typeof(ISwapService))]
        public class DemoSwapService : SimpleSwapService
        {
            protected override ValueTask StoreAsync(string key, string value, CancellationToken cancellationToken)
            {
                WriteLine($"Swap: {key} <- {value}");
                return base.StoreAsync(key, value, cancellationToken);
            }

            protected override ValueTask<bool> RenewAsync(string key, CancellationToken cancellationToken)
            {
                WriteLine($"Swap: {key} <- [try renew]");
                return base.RenewAsync(key, cancellationToken);
            }

            protected override async ValueTask<Option<string>> LoadAsync(string key, CancellationToken cancellationToken)
            {
                var result = await base.LoadAsync(key, cancellationToken);
                WriteLine($"Swap: {key} -> {result}");
                return result;
            }
        }
        #endregion

        public static async Task Caching6()
        {
            #region Part05_Caching6
            var service = CreateServices().GetRequiredService<Service4>();
            WriteLine(await service.GetAsync("a"));
            await Task.Delay(500);
            GC.Collect();
            WriteLine("Task.Delay(500) and GC.Collect()");
            WriteLine(await service.GetAsync("a"));
            await Task.Delay(1500);
            GC.Collect();
            WriteLine("Task.Delay(1500) and GC.Collect()");
            WriteLine(await service.GetAsync("a"));
            #endregion
        }
    }
}
