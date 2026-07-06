using System.Collections.Generic;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ActualLab.Api;
using ActualLab.Trimming;
using MessagePack.Formatters;
using Samples.Blazor.UI;

// Retain runtime-resolved serialization code that TrimMode=full strips but the app
// needs over RPC. Both are reached only via reflection, so the trimmer can't see them:
// - MemoryPack's built-in PriorityQueue<,> formatter target, initialized at startup.
// - MessagePack's GenericDictionaryFormatter for ApiMap<string,string> (User.Claims /
//   Identities), which DynamicGenericResolver instantiates via Activator.CreateInstance.
CodeKeeper.Keep<PriorityQueue<object, object>>();
CodeKeeper.Keep<GenericDictionaryFormatter<string, string, ApiMap<string, string>>>();

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
