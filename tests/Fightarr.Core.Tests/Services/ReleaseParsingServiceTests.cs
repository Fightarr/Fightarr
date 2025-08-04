using Fightarr.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace Fightarr.Core.Tests.Services;

public class ReleaseParsingServiceTests
{
    private readonly ReleaseParsingService _service;
    private readonly Mock<ILogger<ReleaseParsingService>> _mockLogger;

    public ReleaseParsingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ReleaseParsingService>>();
        _service = new ReleaseParsingService(_mockLogger.Object);
    }

    [Theory]
    [InlineData("UFC.294.Makhachev.vs.Volkanovski.2.2023.1080p.WEB-DL.H264-EXAMPLE")]
    [InlineData("Bellator.300.Nurmagomedov.vs.Primus.2023.720p.HDTV.x264-GROUP")]
    [InlineData("ONE.Championship.Friday.Fights.46.2023.480p.WEBRip.x264-TEST")]
    public void IsValidRelease_WithValidMMARelease_ReturnsTrue(string releaseName)
    {
        // Act
        var result = _service.IsValidRelease(releaseName);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("Random.Movie.2023.1080p.BluRay.x264-GROUP")]
    [InlineData("TV.Show.S01E01.1080p.WEB-DL.x264-GROUP")]
    public void IsValidRelease_WithInvalidRelease_ReturnsFalse(string releaseName)
    {
        // Act
        var result = _service.IsValidRelease(releaseName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("UFC.294.Makhachev.vs.Volkanovski.2.2023.1080p.WEB-DL.H264-EXAMPLE", 2023)]
    [InlineData("Bellator.300.2022.720p.HDTV.x264-GROUP", 2022)]
    [InlineData("UFC.Fight.Night.2021.1080p.WEB-DL.x264-TEST", 2021)]
    [InlineData("Random.Release.Without.Year.1080p.WEB-DL.x264-TEST", null)]
    public void ExtractYear_WithVariousReleases_ReturnsCorrectYear(string releaseName, int? expectedYear)
    {
        // Act
        var result = _service.ExtractYear(releaseName);

        // Assert
        result.Should().Be(expectedYear);
    }

    [Theory]
    [InlineData("UFC.294.2023.1080p.WEB-DL.H264-EXAMPLE", 1080)]
    [InlineData("Bellator.300.2022.720p.HDTV.x264-GROUP", 720)]
    [InlineData("ONE.Championship.2023.4K.UHD.WEB-DL.x265-TEST", 2160)]
    [InlineData("UFC.Fight.Night.2021.480p.WEBRip.x264-GROUP", 480)]
    public void ParseQuality_WithVariousQualities_ReturnsCorrectResolution(string releaseName, int expectedResolution)
    {
        // Act
        var result = _service.ParseQuality(releaseName);

        // Assert
        result.Should().NotBeNull();
        result!.Resolution.Should().Be(expectedResolution);
    }

    [Theory]
    [InlineData("UFC.294.2023.1080p.WEB-DL.H264-EXAMPLE", "WEB-DL")]
    [InlineData("Bellator.300.2022.720p.HDTV.x264-GROUP", "HDTV")]
    [InlineData("ONE.Championship.2023.1080p.WEBRip.x264-TEST", "WEBRip")]
    [InlineData("UFC.Fight.Night.2021.1080p.BluRay.x264-GROUP", "BluRay")]
    public void ParseQuality_WithVariousSources_ReturnsCorrectSource(string releaseName, string expectedSource)
    {
        // Act
        var result = _service.ParseQuality(releaseName);

        // Assert
        result.Should().NotBeNull();
        result!.Source.Should().Be(expectedSource);
    }

    [Fact]
    public void ParseRelease_WithCompleteUFCRelease_ReturnsCorrectParsedRelease()
    {
        // Arrange
        var releaseName = "UFC.294.Makhachev.vs.Volkanovski.2.2023.1080p.WEB-DL.H264-EXAMPLE";

        // Act
        var result = _service.ParseRelease(releaseName);

        // Assert
        result.Should().NotBeNull();
        result!.OriginalTitle.Should().Be(releaseName);
        result.Promotion.Should().Be("UFC");
        result.Year.Should().Be(2023);
        result.Quality.Should().NotBeNull();
        result.Quality!.Resolution.Should().Be(1080);
        result.Quality.Source.Should().Be("WEB-DL");
        result.ReleaseGroup.Should().Be("EXAMPLE");
    }

    [Fact]
    public void ParseRelease_WithBellatorRelease_ReturnsCorrectPromotion()
    {
        // Arrange
        var releaseName = "Bellator.300.Nurmagomedov.vs.Primus.2023.720p.HDTV.x264-GROUP";

        // Act
        var result = _service.ParseRelease(releaseName);

        // Assert
        result.Should().NotBeNull();
        result!.Promotion.Should().Be("Bellator");
        result.Year.Should().Be(2023);
    }

    [Fact]
    public void ParseRelease_WithONEChampionshipRelease_ReturnsCorrectPromotion()
    {
        // Arrange
        var releaseName = "ONE.Championship.Friday.Fights.46.2023.1080p.WEB-DL.x264-TEST";

        // Act
        var result = _service.ParseRelease(releaseName);

        // Assert
        result.Should().NotBeNull();
        result!.Promotion.Should().Be("ONE Championship");
    }

    [Theory]
    [InlineData("UFC.294.Makhachev.vs.Volkanovski.2", "UFC 294 Makhachev vs Volkanovski 2")]
    [InlineData("Bellator.300.Main.Card", "Bellator 300 Main Card")]
    [InlineData("ONE_Championship_Friday_Fights", "ONE Championship Friday Fights")]
    public void NormalizeEventName_WithVariousFormats_ReturnsNormalizedName(string input, string expected)
    {
        // Act
        var result = _service.NormalizeEventName(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ParseRelease_WithNullOrEmpty_ReturnsNull()
    {
        // Act & Assert
        _service.ParseRelease(null!).Should().BeNull();
        _service.ParseRelease("").Should().BeNull();
        _service.ParseRelease("   ").Should().BeNull();
    }

    [Theory]
    [InlineData("UFC.294.PRELIMS.2023.1080p.WEB-DL.x264-GROUP", "PRELIMS")]
    [InlineData("Bellator.300.MAIN.CARD.2023.720p.HDTV.x264-GROUP", "MAIN CARD")]
    [InlineData("UFC.Fight.Night.EARLY.PRELIMS.2023.480p.WEBRip.x264-TEST", "EARLY PRELIMS")]
    public void ParseRelease_WithEditionInfo_ReturnsCorrectEdition(string releaseName, string expectedEdition)
    {
        // Act
        var result = _service.ParseRelease(releaseName);

        // Assert
        result.Should().NotBeNull();
        result!.Edition.Should().Be(expectedEdition);
    }
}
