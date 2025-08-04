using Fightarr.Core.Models;
using Fightarr.Core.Services;
using Fightarr.Data.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Fightarr.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IFightEventRepository _eventRepository;
    private readonly IMetadataService _metadataService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(
        IFightEventRepository eventRepository,
        IMetadataService metadataService,
        ILogger<EventsController> logger)
    {
        _eventRepository = eventRepository;
        _metadataService = metadataService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<FightEvent>>> GetEvents()
    {
        try
        {
            var events = await _eventRepository.GetAllAsync();
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving events");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FightEvent>> GetEvent(int id)
    {
        try
        {
            var fightEvent = await _eventRepository.GetByIdAsync(id);
            if (fightEvent == null)
            {
                return NotFound();
            }
            return Ok(fightEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<List<FightEvent>>> GetUpcomingEvents([FromQuery] int days = 30)
    {
        try
        {
            var fromDate = DateTime.UtcNow;
            var events = await _eventRepository.GetUpcomingAsync(fromDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving upcoming events");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("recent")]
    public async Task<ActionResult<List<FightEvent>>> GetRecentEvents([FromQuery] int days = 7)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            var events = await _eventRepository.GetRecentAsync(fromDate);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent events");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<FightEvent>>> SearchEvents([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required");
            }

            var events = await _eventRepository.SearchAsync(query);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching events with query: {Query}", query);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("metadata/search")]
    public async Task<ActionResult<List<FightEvent>>> SearchMetadata([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest("Query parameter is required");
            }

            var events = await _metadataService.SearchEventsAsync(query);
            return Ok(events);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching metadata with query: {Query}", query);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<FightEvent>> AddEvent([FromBody] AddEventRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.ExternalId))
            {
                return BadRequest("ExternalId is required");
            }

            // Check if event already exists
            if (await _eventRepository.ExistsAsync(request.ExternalId))
            {
                return Conflict("Event already exists");
            }

            // Get metadata from external service
            var metadataEvent = await _metadataService.GetEventByIdAsync(request.ExternalId);
            if (metadataEvent == null)
            {
                return NotFound("Event not found in metadata service");
            }

            // Create new event with user preferences
            var fightEvent = new FightEvent
            {
                ExternalId = metadataEvent.ExternalId,
                EventName = metadataEvent.EventName,
                ShortName = metadataEvent.ShortName,
                EventDate = metadataEvent.EventDate,
                Promotion = metadataEvent.Promotion,
                EventType = metadataEvent.EventType,
                PosterUrl = metadataEvent.PosterUrl,
                BackdropUrl = metadataEvent.BackdropUrl,
                Venue = metadataEvent.Venue,
                Description = metadataEvent.Description,
                Fights = metadataEvent.Fights,
                Monitored = request.Monitored,
                QualityProfileId = request.QualityProfileId,
                RootFolderPath = request.RootFolderPath,
                Status = EventStatus.Missing
            };

            var addedEvent = await _eventRepository.AddAsync(fightEvent);
            return CreatedAtAction(nameof(GetEvent), new { id = addedEvent.Id }, addedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding event");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FightEvent>> UpdateEvent(int id, [FromBody] UpdateEventRequest request)
    {
        try
        {
            var fightEvent = await _eventRepository.GetByIdAsync(id);
            if (fightEvent == null)
            {
                return NotFound();
            }

            // Update properties
            fightEvent.Monitored = request.Monitored ?? fightEvent.Monitored;
            fightEvent.QualityProfileId = request.QualityProfileId ?? fightEvent.QualityProfileId;
            fightEvent.RootFolderPath = request.RootFolderPath ?? fightEvent.RootFolderPath;

            var updatedEvent = await _eventRepository.UpdateAsync(fightEvent);
            return Ok(updatedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        try
        {
            var fightEvent = await _eventRepository.GetByIdAsync(id);
            if (fightEvent == null)
            {
                return NotFound();
            }

            await _eventRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/refresh")]
    public async Task<ActionResult<FightEvent>> RefreshEvent(int id)
    {
        try
        {
            var fightEvent = await _eventRepository.GetByIdAsync(id);
            if (fightEvent == null)
            {
                return NotFound();
            }

            var updatedEvent = await _metadataService.UpdateEventMetadataAsync(id);
            if (updatedEvent == null)
            {
                return StatusCode(500, "Failed to refresh event metadata");
            }

            return Ok(updatedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing event {EventId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}

public class AddEventRequest
{
    public string ExternalId { get; set; } = string.Empty;
    public bool Monitored { get; set; } = true;
    public int? QualityProfileId { get; set; }
    public string? RootFolderPath { get; set; }
}

public class UpdateEventRequest
{
    public bool? Monitored { get; set; }
    public int? QualityProfileId { get; set; }
    public string? RootFolderPath { get; set; }
}
