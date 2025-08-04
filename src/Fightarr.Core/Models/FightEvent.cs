using System.ComponentModel.DataAnnotations;

namespace Fightarr.Core.Models;

public class FightEvent
{
    public int Id { get; set; }
    
    [Required]
    public string ExternalId { get; set; } = string.Empty;
    
    [Required]
    public string EventName { get; set; } = string.Empty;
    
    public string ShortName { get; set; } = string.Empty;
    
    [Required]
    public DateTime EventDate { get; set; }
    
    [Required]
    public string Promotion { get; set; } = string.Empty;
    
    public EventType EventType { get; set; }
    
    public string? PosterUrl { get; set; }
    
    public string? BackdropUrl { get; set; }
    
    public string? Venue { get; set; }
    
    public string? Description { get; set; }
    
    public EventStatus Status { get; set; } = EventStatus.Announced;
    
    public bool Monitored { get; set; } = false;
    
    public QualityProfile? QualityProfile { get; set; }
    
    public int? QualityProfileId { get; set; }
    
    public string? RootFolderPath { get; set; }
    
    public List<Fight> Fights { get; set; } = new();
    
    public List<EventFile> EventFiles { get; set; } = new();
    
    public DateTime Added { get; set; } = DateTime.UtcNow;
    
    public DateTime? LastInfoSync { get; set; }
    
    // Computed properties for compatibility
    public string Title => EventName;
    public int Year => EventDate.Year;
    public string Overview => Description ?? GenerateOverview();
    
    private string GenerateOverview()
    {
        if (!Fights.Any()) return $"{Promotion} event on {EventDate:MMMM dd, yyyy}";
        
        var mainEvent = Fights.OrderBy(f => f.FightOrder).FirstOrDefault();
        if (mainEvent != null)
        {
            return $"{Promotion} event featuring {string.Join(" vs ", mainEvent.Fighters)} and {Fights.Count - 1} other fights.";
        }
        
        return $"{Promotion} event with {Fights.Count} fights on {EventDate:MMMM dd, yyyy}";
    }
}

public enum EventType
{
    PPV,
    FightNight,
    Championship,
    Tournament,
    Other
}

public enum EventStatus
{
    Announced,
    Available,
    Downloaded,
    Missing
}
