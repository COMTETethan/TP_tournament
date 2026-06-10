namespace Tournament.Domain.Service;

public class Player
{
    public string Name { get; set; } = string.Empty;
    public List<MatchResult> Matches { get; set; } = new();
    public bool IsDisqualified { get; set; }
    public int PenaltyPoints { get; set; }
}
