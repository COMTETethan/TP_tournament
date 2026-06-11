namespace Tournament.Api.Data.Entities;

public class PlayerEntity
{
    public int      Id              { get; set; }
    public int      TournamentId    { get; set; }
    public string   Name            { get; set; } = string.Empty;
    public bool     IsDisqualified  { get; set; }
    public int      PenaltyPoints   { get; set; }
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
}
