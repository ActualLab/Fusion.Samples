using ActualLab.OS;

namespace Samples.Benchmark;

public static class Settings
{
    public static readonly string BaseUrl = "http://127.0.0.1:22333/";
    public static readonly string DbConnectionString =
        "Server={0};Database=fusion_benchmark;Port=5432;" +
        "User Id=postgres;Password=postgres;" +
        "Minimum Pool Size=20;Maximum Pool Size=200;Multiplexing=true";

    public static readonly int ItemCount = 1_000;
    public static readonly int CpuCount = (int)HardwareInfo.ProcessorCount;
    public static readonly int ClientConcurrency = 200;
    public static readonly int WriterCount = 1;
    public static readonly double Duration = 5; // In seconds
    public static readonly double WarmupDuration = Duration;
    public static readonly int TryCount = 6;
}
