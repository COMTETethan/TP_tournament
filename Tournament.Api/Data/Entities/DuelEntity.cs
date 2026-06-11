namespace Tournament.Api.Data.Entities;

public class DuelEntity
{
    public int          Id           { get; set; }
    public int          TournamentId { get; set; }
    public int          Player1Id    { get; set; }
    public int          Player2Id    { get; set; }
    public DuelOutcome? Outcome      { get; set; }
    public int          DuelOrder    { get; set; }
    public DateTime     PlayedAt     { get; set; } = DateTime.UtcNow;
    public TimeSpan?    Duration     { get; set; }
}
