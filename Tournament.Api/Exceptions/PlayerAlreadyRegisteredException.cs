namespace Tournament.Api.Exceptions;

public class PlayerAlreadyRegisteredException : Exception
{
    public int TournamentId { get; }
    public int PlayerId { get; }

    public PlayerAlreadyRegisteredException(int tournamentId, int playerId)
        : base($"Player {playerId} is already registered in tournament {tournamentId}.")
    {
        TournamentId = tournamentId;
        PlayerId = playerId;
    }
}
