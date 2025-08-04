namespace Fightarr.Core.Configuration;

public class MetadataProviderSettings
{
    public string BaseUrl { get; set; } = "https://api.fightarr.com";
    public string? ApiKey { get; set; }
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheExpiration { get; set; } = TimeSpan.FromHours(1);
    public int RateLimitPerMinute { get; set; } = 60;
    public bool EnableCompression { get; set; } = true;
    public string UserAgent { get; set; } = "Fightarr/1.0";
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public bool ValidateSslCertificate { get; set; } = true;
}
