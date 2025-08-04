namespace Fightarr.Core.Models;

public class Indexer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IndexerType Type { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public List<string> Categories { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 25;
    public TimeSpan SearchTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableRss { get; set; } = true;
    public bool EnableAutomaticSearch { get; set; } = true;
    public bool EnableInteractiveSearch { get; set; } = true;
}

public enum IndexerType
{
    Torznab,
    Newznab,
    Prowlarr,
    Custom
}
