namespace Tournament.Api.Exceptions;

public class DuelAlreadyEndedException : Exception
{
    public int DuelId { get; }

    public DuelAlreadyEndedException(int duelId)
        : base($"Duel {duelId} has already ended and cannot be modified.")
    {
        DuelId = duelId;
    }
}
