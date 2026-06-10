namespace Tournament.Api.Exceptions;

public class TournamentNotFoundException : Exception
{
    public int TournamentId { get; }

    public TournamentNotFoundException(int tournamentId)
        : base($"Tournament with id {tournamentId} was not found.")
    {
        TournamentId = tournamentId;
    }
}
