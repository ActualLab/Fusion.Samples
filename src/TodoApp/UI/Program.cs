using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.TodoApp.UI;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
ClientStartup.ConfigureServices(builder.Services, builder);
var host = builder.Build();
StaticLog.Factory = host.Services.LoggerFactory();
await host.RunAsync();
