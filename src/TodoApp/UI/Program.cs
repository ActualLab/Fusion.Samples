using System.Collections.Generic;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ActualLab.Collections;
using ActualLab.Fusion.Authentication;
using ActualLab.Trimming;
using MessagePack.ImmutableCollection;
using Samples.TodoApp.UI;

// MemoryPack's built-in PriorityQueue<,> formatter target is reached only via
// reflection at startup, so TrimMode=full strips it unless we keep it here.
CodeKeeper.Keep<PriorityQueue<object, object>>();

// The Authentication page lists sessions (ImmutableArray<SessionInfo>), and
// SessionInfo.Options is a typeless ImmutableOptionSet - MessagePack resolves these
// formatters via reflection, which full trimming can't trace. Keep the ones this
// sample actually uses (it never stores option values, so no value formatters needed).
CodeKeeper.Keep<ImmutableArrayFormatter<SessionInfo>>();
CodeKeeper.KeepSerializable<SessionInfo>();
CodeKeeper.KeepSerializable<ImmutableOptionSet>();

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
