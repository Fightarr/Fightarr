namespace Fightarr.Core.Models;

public class DownloadClient
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DownloadClientType Type { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? Category { get; set; }
    public string? DownloadPath { get; set; }
    public bool UseSsl { get; set; } = false;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 1;
}

public enum DownloadClientType
{
    qBittorrent,
    Transmission,
    Deluge,
    SABnzbd,
    NZBGet
}
