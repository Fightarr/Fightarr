using Fightarr.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace Fightarr.Core.Services;

public class MetadataService : IMetadataService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MetadataService> _logger;
    private readonly string _baseUrl;

    public MetadataService(HttpClient httpClient, IConfiguration configuration, ILogger<MetadataService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["MetadataProvider:BaseUrl"] ?? "https://api.fightarr.com";
        
        // Set default headers
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Fightarr/1.0");
        
        var apiKey = configuration["MetadataProvider:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }
    }

    public async Task<List<FightEvent>> SearchEventsAsync(string query)
    {
        try
        {
            var encodedQuery = Uri.EscapeDataString(query);
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/search?query={encodedQuery}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to search events. Status: {StatusCode}", response.StatusCode);
                return new List<FightEvent>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<List<FightEvent>>(content) ?? new List<FightEvent>();
            
            _logger.LogInformation("Found {Count} events for query: {Query}", events.Count, query);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for events with query: {Query}", query);
            return new List<FightEvent>();
        }
    }

    public async Task<FightEvent?> GetEventByIdAsync(string externalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/event/{externalId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get event by ID. Status: {StatusCode}, ID: {ExternalId}", 
                    response.StatusCode, externalId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var fightEvent = JsonConvert.DeserializeObject<FightEvent>(content);
            
            _logger.LogInformation("Retrieved event: {EventName}", fightEvent?.EventName);
            return fightEvent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting event by ID: {ExternalId}", externalId);
            return null;
        }
    }

    public async Task<List<FightEvent>> GetUpcomingEventsAsync(int days = 30)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/events/upcoming?days={days}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get upcoming events. Status: {StatusCode}", response.StatusCode);
                return new List<FightEvent>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<List<FightEvent>>(content) ?? new List<FightEvent>();
            
            _logger.LogInformation("Retrieved {Count} upcoming events", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting upcoming events");
            return new List<FightEvent>();
        }
    }

    public async Task<List<FightEvent>> GetRecentEventsAsync(int days = 7)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/v1/events/recent?days={days}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get recent events. Status: {StatusCode}", response.StatusCode);
                return new List<FightEvent>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var events = JsonConvert.DeserializeObject<List<FightEvent>>(content) ?? new List<FightEvent>();
            
            _logger.LogInformation("Retrieved {Count} recent events", events.Count);
            return events;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent events");
            return new List<FightEvent>();
        }
    }

    public Task<FightEvent?> UpdateEventMetadataAsync(int eventId)
    {
        try
        {
            // This would typically fetch from your local database first, then update from metadata API
            _logger.LogInformation("Updating metadata for event ID: {EventId}", eventId);
            
            // Implementation would depend on your local data access layer
            // For now, return null as placeholder
            return Task.FromResult<FightEvent?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating event metadata for ID: {EventId}", eventId);
            return Task.FromResult<FightEvent?>(null);
        }
    }
}