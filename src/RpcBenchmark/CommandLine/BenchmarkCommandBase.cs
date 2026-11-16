using System.ComponentModel;
using Ookii.CommandLine;
using Ookii.CommandLine.Commands;
using Ookii.CommandLine.Validation;
using ActualLab.OS;

namespace Samples.RpcBenchmark;

public abstract class BenchmarkCommandBase : AsyncCommandBase
{
    [CommandLineArgument]
    [Description("Min. thread pool thread count.")]
    [ValueDescription("Number")]
    [ValidateRange(1, null)]
    [Alias("wt")]
    public int MinWorkerThreads { get; set; } = HardwareInfo.ProcessorCount;

    [CommandLineArgument]
    [Description("Min. thread pool IO thread count.")]
    [ValueDescription("Number")]
    [ValidateRange(1, null)]
    [Alias("iot")]
    public int MinIOThreads { get; set; } = HardwareInfo.ProcessorCount * 10;

    [CommandLineArgument]
    [Description("RPC transport and serialization format for ActualLab.Rpc tests. Format: [transport:]format (transport: ws|http, default ws).")]
    [ValueDescription("[ws|http:]msgpack6c,msgpack6,mempack6c,mempack6,nmsgpack6c,nmsgpack6,json5,njson5,...")]
    [Alias("f")]
    public string SerializationFormat { get; set; } = "msgpack6c";

    [CommandLineArgument]
    [Description("Use HTTPS (TLS) instead of plain HTTP. Passing an https:// URL has the same effect.")]
    public bool Tls { get; set; }

    public static string GetHttp2Url(string url)
    {
        // Without TLS Kestrel can't serve HTTP/1.1 and HTTP/2 on the same port, so HTTP/2 gets the next one
        var uri = new Uri(url);
        return uri.Scheme == Uri.UriSchemeHttps
            ? url
            : $"http://{uri.Host}:{uri.Port + 1}{uri.PathAndQuery}";
    }

    // Protected methods

    protected string NormalizeUrl(string url)
    {
        url = url.NormalizeBaseUrl();
        if (!Tls)
            return url;

        // Keeps the port even when it's implicit: the server's and the client's URLs must map the same way
        var uri = new Uri(url);
        return uri.Scheme == Uri.UriSchemeHttp
            ? $"https://{uri.Host}:{uri.Port}{uri.PathAndQuery}"
            : url;
    }

    protected (string Transport, string Format) ParseSerializationFormat()
    {
        var s = SerializationFormat;
        var i = s.IndexOf(':');
        if (i < 0)
            return ("ws", s);

        var transport = s[..i].ToLowerInvariant();
        if (transport is not ("ws" or "http"))
            throw new ArgumentException($"Unsupported RPC transport '{transport}'. Use 'ws' or 'http'.");
        return (transport, s[(i + 1)..]);
    }
}
