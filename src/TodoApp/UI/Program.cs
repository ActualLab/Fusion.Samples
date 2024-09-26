using ActualLab.DependencyInjection;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Samples.TodoApp.UI;

public class Program
{
    public static Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        StartupHelper.ConfigureServices(builder.Services, builder);
        var host = builder.Build();
        StaticLog.Factory = host.Services.LoggerFactory();
        return host.RunAsync();
    }
}
