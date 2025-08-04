using Fightarr.Core.Models;

namespace Fightarr.Core.Services;

public interface IReleaseParsingService
{
    ParsedRelease? ParseRelease(string releaseName);
    bool IsValidRelease(string releaseName);
    string NormalizeEventName(string eventName);
    int? ExtractYear(string releaseName);
    Quality? ParseQuality(string releaseName);
}

public class ParsedRelease
{
    public string EventName { get; set; } = string.Empty;
    public string Promotion { get; set; } = string.Empty;
    public int? Year { get; set; }
    public Quality? Quality { get; set; }
    public string? Edition { get; set; }
    public List<string> Languages { get; set; } = new();
    public string ReleaseGroup { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
}
