using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.TodoApp.UI;

// TEMP diagnostic: surface the full exception chain (incl. module-initializer inner
// exceptions) to the browser console so we can identify what full trimming stripped.
try {
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    ClientStartup.ConfigureServices(builder.Services, builder);
    var host = builder.Build();
    StaticLog.Factory = host.Services.LoggerFactory();
    await host.RunAsync();
}
catch (Exception ex) {
    for (var e = ex; e != null; e = e.InnerException)
        Console.WriteLine($"[BOOT-EX] {e.GetType().FullName}: {e.Message}\n{e.StackTrace}");
    throw;
}
