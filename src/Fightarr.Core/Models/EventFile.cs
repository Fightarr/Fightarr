namespace Fightarr.Core.Models;

public class EventFile
{
    public int Id { get; set; }
    
    public int FightEventId { get; set; }
    public FightEvent FightEvent { get; set; } = null!;
    
    public string RelativePath { get; set; } = string.Empty;
    
    public string Path { get; set; } = string.Empty;
    
    public long Size { get; set; }
    
    public DateTime DateAdded { get; set; } = DateTime.UtcNow;
    
    public Quality Quality { get; set; } = null!;
    
    public int QualityId { get; set; }
    
    public string? Edition { get; set; }
    
    public List<string> Languages { get; set; } = new();
}
