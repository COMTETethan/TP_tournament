namespace Tournament.Api.Exceptions;

public class RegistrationNotFoundException : Exception
{
    public int TournamentId { get; }
    public int PlayerId { get; }

    public RegistrationNotFoundException(int tournamentId, int playerId)
        : base($"Player {playerId} is not registered in tournament {tournamentId}.")
    {
        TournamentId = tournamentId;
        PlayerId = playerId;
    }
}
