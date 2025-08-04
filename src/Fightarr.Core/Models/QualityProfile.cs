namespace Fightarr.Core.Models;

public class QualityProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<QualityItem> Items { get; set; } = new();
    public int Cutoff { get; set; }
    public string Language { get; set; } = "English";
}

public class QualityItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Quality Quality { get; set; } = null!;
    public bool Allowed { get; set; }
}

public class Quality
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public int Resolution { get; set; }
}
