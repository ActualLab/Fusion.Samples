using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.Blazor.UI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
ClientStartup.ConfigureServices(builder.Services, builder);
var host = builder.Build();
StaticLog.Factory = host.Services.LoggerFactory();
// ComponentInfo.DebugLog = StaticLog.For<ComponentInfo>();
await host.RunAsync();
