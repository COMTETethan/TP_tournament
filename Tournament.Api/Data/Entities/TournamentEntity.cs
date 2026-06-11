namespace Tournament.Api.Data.Entities;

public class TournamentEntity
{
    public int              Id        { get; set; }
    public string           Name      { get; set; } = string.Empty;
    public TournamentStatus Status    { get; set; } = TournamentStatus.OPEN;
    public DateTime         CreatedAt { get; set; } = DateTime.UtcNow;
}
