namespace Fightarr.Core.Configuration;

public class FightarrSettings
{
    public string ApplicationName { get; set; } = "Fightarr";
    public string Version { get; set; } = "1.0.0";
    public string ConfigDirectory { get; set; } = string.Empty;
    public string LogDirectory { get; set; } = string.Empty;
    public string DataDirectory { get; set; } = string.Empty;
    public bool EnableMetrics { get; set; } = true;
    public bool EnableAnalytics { get; set; } = false;
    public int ApiPort { get; set; } = 7878;
    public string BindAddress { get; set; } = "*";
    public bool EnableSsl { get; set; } = false;
    public string? SslCertPath { get; set; }
    public string? SslCertPassword { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
    public int MaxLogFiles { get; set; } = 10;
    public long MaxLogSizeBytes { get; set; } = 10485760; // 10MB
    public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromHours(24);
    public bool RequireAuthentication { get; set; } = false;
    public string? AuthenticationMethod { get; set; }
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
