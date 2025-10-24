using System.Text.RegularExpressions;
using Fightarr.Api.Models;

namespace Fightarr.Api.Services;

/// <summary>
/// Parses media file names to extract quality, resolution, codecs, etc.
/// Based on Sonarr/Radarr parsing logic
/// </summary>
public class MediaFileParser
{
    private readonly ILogger<MediaFileParser> _logger;

    // Quality patterns
    private static readonly Regex QualityPattern = new(@"(?<quality>2160p|1080p|720p|480p|360p|4K|UHD|HD|SD)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SourcePattern = new(@"(?<source>BluRay|Blu-Ray|BDREMUX|BD|WEB-DL|WEBDL|WEBRip|WEB|HDTV|PDTV|DVDRip|DVD|Telecine|HDCAM|CAM|TS|TELESYNC)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex VideoCodecPattern = new(@"(?<codec>x265|x264|h\.?265|h\.?264|HEVC|AVC|XviD|DivX|VP9|AV1)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AudioCodecPattern = new(@"(?<audio>AAC|AC3|E-?AC-?3|DDP|DD|TrueHD|Atmos|DTS(?:-HD)?(?:-MA)?|FLAC|MP3|Opus)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ReleaseGroupPattern = new(@"-(?<group>[A-Z0-9]+)(?:\[.*?\])?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ProperRepackPattern = new(@"\b(?<proper>PROPER|REPACK|REAL)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex EditionPattern = new(@"(?<edition>EXTENDED|UNRATED|DIRECTOR'?S?.?CUT|THEATRICAL|REMASTERED|IMAX|CRITERION)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LanguagePattern = new(@"(?<lang>MULTI|MULTiSUBS|DUAL|DUBBED|SUBBED|GERMAN|FRENCH|SPANISH|ITALIAN|JAPANESE|KOREAN|CHINESE)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Date patterns
    private static readonly Regex DatePattern = new(@"(?<year>\d{4})[-.]?(?<month>\d{2})[-.]?(?<day>\d{2})", RegexOptions.Compiled);
    private static readonly Regex YearPattern = new(@"\b(?<year>19\d{2}|20\d{2})\b", RegexOptions.Compiled);

    public MediaFileParser(ILogger<MediaFileParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a filename or release title to extract metadata
    /// </summary>
    public ParsedFileInfo Parse(string filename)
    {
        var cleanName = CleanFilename(filename);

        var parsed = new ParsedFileInfo
        {
            EventTitle = ExtractEventTitle(cleanName),
            Quality = ExtractQuality(cleanName),
            ReleaseGroup = ExtractReleaseGroup(cleanName),
            Resolution = ExtractResolution(cleanName),
            VideoCodec = ExtractVideoCodec(cleanName),
            AudioCodec = ExtractAudioCodec(cleanName),
            Source = ExtractSource(cleanName),
            AirDate = ExtractAirDate(cleanName),
            Edition = ExtractEdition(cleanName),
            Language = ExtractLanguage(cleanName),
            IsProperOrRepack = ProperRepackPattern.IsMatch(cleanName)
        };

        _logger.LogDebug("Parsed '{Filename}': Title='{Title}', Quality='{Quality}', Group='{Group}'",
            filename, parsed.EventTitle, parsed.Quality, parsed.ReleaseGroup);

        return parsed;
    }

    /// <summary>
    /// Build quality string from parsed information
    /// </summary>
    public string BuildQualityString(ParsedFileInfo parsed)
    {
        var parts = new List<string>();

        if (!string.IsNullOrEmpty(parsed.Resolution))
            parts.Add(parsed.Resolution);
        if (!string.IsNullOrEmpty(parsed.Source))
            parts.Add(parsed.Source);
        if (!string.IsNullOrEmpty(parsed.VideoCodec))
            parts.Add(parsed.VideoCodec);
        if (!string.IsNullOrEmpty(parsed.AudioCodec))
            parts.Add(parsed.AudioCodec);
        if (parsed.IsProperOrRepack)
            parts.Add("PROPER");

        return parts.Any() ? string.Join(" ", parts) : "Unknown";
    }

    private string CleanFilename(string filename)
    {
        // Remove file extension
        var name = Path.GetFileNameWithoutExtension(filename);

        // Replace dots and underscores with spaces
        name = name.Replace('.', ' ').Replace('_', ' ');

        return name;
    }

    private string ExtractEventTitle(string cleanName)
    {
        // Try to extract title before year/date/quality markers
        var markers = new[] {
            @"\b(19\d{2}|20\d{2})\b",
            @"\b(2160p|1080p|720p|480p|4K|UHD)\b",
            @"\b(BluRay|WEB-DL|WEBRip|HDTV)\b"
        };

        foreach (var marker in markers)
        {
            var match = Regex.Match(cleanName, marker, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return cleanName.Substring(0, match.Index).Trim();
            }
        }

        return cleanName.Trim();
    }

    private string? ExtractQuality(string cleanName)
    {
        var resolution = ExtractResolution(cleanName);
        var source = ExtractSource(cleanName);

        if (!string.IsNullOrEmpty(resolution) && !string.IsNullOrEmpty(source))
            return $"{resolution} {source}";
        if (!string.IsNullOrEmpty(resolution))
            return resolution;
        if (!string.IsNullOrEmpty(source))
            return source;

        return null;
    }

    private string? ExtractResolution(string cleanName)
    {
        var match = QualityPattern.Match(cleanName);
        return match.Success ? match.Groups["quality"].Value.ToUpper() : null;
    }

    private string? ExtractSource(string cleanName)
    {
        var match = SourcePattern.Match(cleanName);
        if (!match.Success) return null;

        var source = match.Groups["source"].Value.ToUpper();

        // Normalize source names
        source = source.Replace("BLU-RAY", "BLURAY")
                      .Replace("WEB-DL", "WEBDL")
                      .Replace("WEBRIP", "WEBRip");

        return source;
    }

    private string? ExtractVideoCodec(string cleanName)
    {
        var match = VideoCodecPattern.Match(cleanName);
        if (!match.Success) return null;

        var codec = match.Groups["codec"].Value.ToUpper();

        // Normalize codec names
        if (codec.Contains("265") || codec == "HEVC")
            return "x265";
        if (codec.Contains("264") || codec == "AVC")
            return "x264";

        return codec;
    }

    private string? ExtractAudioCodec(string cleanName)
    {
        var match = AudioCodecPattern.Match(cleanName);
        return match.Success ? match.Groups["audio"].Value.ToUpper() : null;
    }

    private string? ExtractReleaseGroup(string cleanName)
    {
        var match = ReleaseGroupPattern.Match(cleanName);
        return match.Success ? match.Groups["group"].Value : null;
    }

    private string? ExtractEdition(string cleanName)
    {
        var match = EditionPattern.Match(cleanName);
        return match.Success ? match.Groups["edition"].Value : null;
    }

    private string? ExtractLanguage(string cleanName)
    {
        var match = LanguagePattern.Match(cleanName);
        return match.Success ? match.Groups["lang"].Value : null;
    }

    private DateTime? ExtractAirDate(string cleanName)
    {
        // Try full date first (YYYY-MM-DD or YYYY.MM.DD)
        var dateMatch = DatePattern.Match(cleanName);
        if (dateMatch.Success)
        {
            if (DateTime.TryParse($"{dateMatch.Groups["year"].Value}-{dateMatch.Groups["month"].Value}-{dateMatch.Groups["day"].Value}",
                out var fullDate))
            {
                return fullDate;
            }
        }

        // Fall back to year only
        var yearMatch = YearPattern.Match(cleanName);
        if (yearMatch.Success && int.TryParse(yearMatch.Groups["year"].Value, out var year))
        {
            return new DateTime(year, 1, 1);
        }

        return null;
    }
}
