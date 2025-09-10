using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Samples.Blazor.UI;

RuntimeCodegen.Mode = RuntimeCodegenMode.InterpretedExpressions;
Console.WriteLine(RuntimeCodegen.Mode);
Console.WriteLine(RuntimeCodegen.NativeMode);
Console.WriteLine(CpuTimestamp.TickFrequency);

var builder = WebAssemblyHostBuilder.CreateDefault(args);
ClientStartup.ConfigureServices(builder.Services, builder);
var host = builder.Build();
StaticLog.Factory = host.Services.LoggerFactory();
// ComponentInfo.DebugLog = StaticLog.For<ComponentInfo>();
await host.RunAsync();
