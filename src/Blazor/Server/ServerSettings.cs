namespace Samples.Blazor.Server;

public class ServerSettings
{
    public bool AssumeHttps { get; set; } = false;

    public string MicrosoftAccountClientId { get; set; } = "6839dbf7-d1d3-4eb2-a7e1-ce8d48f34d00";
    public string MicrosoftAccountClientSecret { get; set; } =
        Encoding.UTF8.GetString(Convert.FromBase64String(
            "cFo4OFF+V3JXZXAuMnFrfkVFd1o5akR0TXk3UDNwRG9iazMxWmFkaw=="));

    public string GitHubClientId { get; set; } = "Iv23liclgDFiYO8LJoHM";
    public string GitHubClientSecret { get; set; } =
        Encoding.UTF8.GetString(Convert.FromBase64String(
            "ZmRiYWU5MWUyMDg2NjM2ODlmMmM1MTliNDI0ZjZiZWU0NjI2MGVlNw=="));
}
