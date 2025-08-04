using System.ComponentModel.DataAnnotations;

namespace Fightarr.Core.Models;

public class Fight
{
    public int Id { get; set; }
    
    public int FightEventId { get; set; }
    public FightEvent FightEvent { get; set; } = null!;
    
    public int FightOrder { get; set; }
    
    public string? Division { get; set; }
    
    public List<string> Fighters { get; set; } = new();
    
    public bool IsMainEvent { get; set; }
    
    public bool IsTitleFight { get; set; }
    
    public string? Result { get; set; }
    
    public string? Method { get; set; }
    
    public int? Round { get; set; }
    
    public TimeSpan? Time { get; set; }
}
