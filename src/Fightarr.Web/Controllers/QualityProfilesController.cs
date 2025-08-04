using Fightarr.Core.Models;
using Fightarr.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fightarr.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QualityProfilesController : ControllerBase
{
    private readonly FightarrDbContext _context;
    private readonly ILogger<QualityProfilesController> _logger;

    public QualityProfilesController(FightarrDbContext context, ILogger<QualityProfilesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<QualityProfile>>> GetQualityProfiles()
    {
        try
        {
            var profiles = await _context.QualityProfiles.ToListAsync();
            return Ok(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quality profiles");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QualityProfile>> GetQualityProfile(int id)
    {
        try
        {
            var profile = await _context.QualityProfiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quality profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<QualityProfile>> CreateQualityProfile([FromBody] CreateQualityProfileRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest("Name is required");
            }

            var profile = new QualityProfile
            {
                Name = request.Name,
                Items = request.Items ?? new List<QualityItem>(),
                Cutoff = request.Cutoff,
                Language = request.Language ?? "English"
            };

            _context.QualityProfiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created quality profile: {ProfileName}", profile.Name);
            return CreatedAtAction(nameof(GetQualityProfile), new { id = profile.Id }, profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quality profile");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<QualityProfile>> UpdateQualityProfile(int id, [FromBody] UpdateQualityProfileRequest request)
    {
        try
        {
            var profile = await _context.QualityProfiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            profile.Name = request.Name ?? profile.Name;
            profile.Items = request.Items ?? profile.Items;
            profile.Cutoff = request.Cutoff ?? profile.Cutoff;
            profile.Language = request.Language ?? profile.Language;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated quality profile: {ProfileName}", profile.Name);
            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating quality profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteQualityProfile(int id)
    {
        try
        {
            var profile = await _context.QualityProfiles.FindAsync(id);
            if (profile == null)
            {
                return NotFound();
            }

            // Check if profile is in use
            var eventsUsingProfile = await _context.FightEvents
                .Where(e => e.QualityProfileId == id)
                .CountAsync();

            if (eventsUsingProfile > 0)
            {
                return BadRequest($"Cannot delete quality profile. It is being used by {eventsUsingProfile} events.");
            }

            _context.QualityProfiles.Remove(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted quality profile: {ProfileName}", profile.Name);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting quality profile {ProfileId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class CreateQualityProfileRequest
{
    public string Name { get; set; } = string.Empty;
    public List<QualityItem>? Items { get; set; }
    public int Cutoff { get; set; }
    public string? Language { get; set; }
}

public class UpdateQualityProfileRequest
{
    public string? Name { get; set; }
    public List<QualityItem>? Items { get; set; }
    public int? Cutoff { get; set; }
    public string? Language { get; set; }
}
