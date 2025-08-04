using Fightarr.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Fightarr.Core.Services;

public class ReleaseParsingService : IReleaseParsingService
{
    private readonly ILogger<ReleaseParsingService> _logger;
    
    // Common MMA promotion patterns
    private static readonly Dictionary<string, string> PromotionPatterns = new()
    {
        { @"UFC\.(\d+)", "UFC" },
        { @"UFC\.Fight\.Night", "UFC" },
        { @"UFC\.on\.ESPN", "UFC" },
        { @"Bellator\.(\d+)", "Bellator" },
        { @"ONE\.Championship", "ONE Championship" },
        { @"PFL\.(\d+)", "PFL" },
        { @"Strikeforce", "Strikeforce" },
        { @"PRIDE", "PRIDE" },
        { @"WEC\.(\d+)", "WEC" }
    };

    // Quality patterns
    private static readonly Dictionary<Regex, Quality> QualityPatterns = new()
    {
        { new Regex(@"2160p|4K|UHD", RegexOptions.IgnoreCase), new Quality { Id = 1, Name = "4K", Resolution = 2160, Source = "WEB-DL" } },
        { new Regex(@"1080p", RegexOptions.IgnoreCase), new Quality { Id = 2, Name = "1080p", Resolution = 1080, Source = "WEB-DL" } },
        { new Regex(@"720p", RegexOptions.IgnoreCase), new Quality { Id = 3, Name = "720p", Resolution = 720, Source = "WEB-DL" } },
        { new Regex(@"480p", RegexOptions.IgnoreCase), new Quality { Id = 4, Name = "480p", Resolution = 480, Source = "WEB-DL" } }
    };

    // Source patterns
    private static readonly Dictionary<string, string> SourcePatterns = new()
    {
        { @"WEB-DL|WEBDL", "WEB-DL" },
        { @"WEBRip", "WEBRip" },
        { @"BluRay|Blu-Ray", "BluRay" },
        { @"HDTV", "HDTV" },
        { @"PPV", "PPV" }
    };

    public ReleaseParsingService(ILogger<ReleaseParsingService> logger)
    {
        _logger = logger;
    }

    public ParsedRelease? ParseRelease(string releaseName)
    {
        if (string.IsNullOrWhiteSpace(releaseName))
        {
            return null;
        }

        try
        {
            var parsed = new ParsedRelease
            {
                OriginalTitle = releaseName
            };

            // Extract promotion
            parsed.Promotion = ExtractPromotion(releaseName);
            
            // Extract event name
            parsed.EventName = ExtractEventName(releaseName);
            
            // Extract year
            parsed.Year = ExtractYear(releaseName);
            
            // Extract quality
            parsed.Quality = ParseQuality(releaseName);
            
            // Extract release group
            parsed.ReleaseGroup = ExtractReleaseGroup(releaseName);
            
            // Extract languages
            parsed.Languages = ExtractLanguages(releaseName);
            
            // Extract edition info
            parsed.Edition = ExtractEdition(releaseName);

            _logger.LogDebug("Parsed release: {EventName} ({Year}) - {Quality}", 
                parsed.EventName, parsed.Year, parsed.Quality?.Name);

            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing release: {ReleaseName}", releaseName);
            return null;
        }
    }

    public bool IsValidRelease(string releaseName)
    {
        if (string.IsNullOrWhiteSpace(releaseName))
            return false;

        // Check if it contains MMA-related keywords
        var mmaKeywords = new[] { "UFC", "Bellator", "ONE", "PFL", "Fight", "vs", "MMA" };
        return mmaKeywords.Any(keyword => releaseName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    public string NormalizeEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return string.Empty;

        // Replace dots and underscores with spaces
        var normalized = eventName.Replace('.', ' ').Replace('_', ' ');
        
        // Remove multiple spaces
        normalized = Regex.Replace(normalized, @"\s+", " ");
        
        // Trim
        return normalized.Trim();
    }

    public int? ExtractYear(string releaseName)
    {
        var yearMatch = Regex.Match(releaseName, @"\b(19|20)\d{2}\b");
        if (yearMatch.Success && int.TryParse(yearMatch.Value, out var year))
        {
            return year;
        }
        return null;
    }

    public Quality? ParseQuality(string releaseName)
    {
        foreach (var pattern in QualityPatterns)
        {
            if (pattern.Key.IsMatch(releaseName))
            {
                var quality = pattern.Value;
                
                // Update source if found
                foreach (var sourcePattern in SourcePatterns)
                {
                    if (Regex.IsMatch(releaseName, sourcePattern.Key, RegexOptions.IgnoreCase))
                    {
                        quality.Source = sourcePattern.Value;
                        break;
                    }
                }
                
                return quality;
            }
        }

        // Default quality if none found
        return new Quality { Id = 5, Name = "Unknown", Resolution = 0, Source = "Unknown" };
    }

    private string ExtractPromotion(string releaseName)
    {
        foreach (var pattern in PromotionPatterns)
        {
            if (Regex.IsMatch(releaseName, pattern.Key, RegexOptions.IgnoreCase))
            {
                return pattern.Value;
            }
        }
        return "Unknown";
    }

    private string ExtractEventName(string releaseName)
    {
        // Remove common suffixes (quality, codec, etc.)
        var cleanName = Regex.Replace(releaseName, @"\.(720p|1080p|2160p|4K|x264|x265|H264|H265|WEB-DL|WEBRip|BluRay|HDTV).*$", "", RegexOptions.IgnoreCase);
        
        // Remove year if present
        cleanName = Regex.Replace(cleanName, @"\.(19|20)\d{2}", "");
        
        return NormalizeEventName(cleanName);
    }

    private string ExtractReleaseGroup(string releaseName)
    {
        var groupMatch = Regex.Match(releaseName, @"-([A-Za-z0-9]+)$");
        return groupMatch.Success ? groupMatch.Groups[1].Value : string.Empty;
    }

    private List<string> ExtractLanguages(string releaseName)
    {
        var languages = new List<string>();
        
        var languagePatterns = new Dictionary<string, string>
        {
            { @"\bENGLISH\b", "English" },
            { @"\bSPANISH\b", "Spanish" },
            { @"\bFRENCH\b", "French" },
            { @"\bGERMAN\b", "German" },
            { @"\bJAPANESE\b", "Japanese" },
            { @"\bPORTUGUESE\b", "Portuguese" }
        };

        foreach (var pattern in languagePatterns)
        {
            if (Regex.IsMatch(releaseName, pattern.Key, RegexOptions.IgnoreCase))
            {
                languages.Add(pattern.Value);
            }
        }

        return languages.Any() ? languages : new List<string> { "English" };
    }

    private string? ExtractEdition(string releaseName)
    {
        var editionPatterns = new[]
        {
            @"EXTENDED",
            @"DIRECTORS?.CUT",
            @"UNCUT",
            @"REMASTERED",
            @"PRELIMS",
            @"MAIN.CARD",
            @"EARLY.PRELIMS"
        };

        foreach (var pattern in editionPatterns)
        {
            var match = Regex.Match(releaseName, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Value.Replace('.', ' ');
            }
        }

        return null;
    }
}
