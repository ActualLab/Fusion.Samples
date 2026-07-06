using System.Collections.Generic;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ActualLab.Trimming;
using Samples.Blazor.UI;

// Full trimming would drop MemoryPack's built-in PriorityQueue<,> formatter target,
// which its formatter provider initializes on the first serialization at startup.
CodeKeeper.Keep<PriorityQueue<object, object>>();

try {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    ClientStartup.ConfigureServices(builder.Services, builder);
    var host = builder.Build();
    StaticLog.Factory = host.Services.LoggerFactory();
    await host.RunAsync();
}
catch (Exception error) {
    // WASM surfaces only the outermost exception; log the whole chain so a boot
    // failure (e.g. a type trimmed away) is diagnosable from the browser console.
    for (var e = error; e != null; e = e.InnerException)
        Console.WriteLine($"{e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
    throw;
}
