namespace Tournament.Api.Exceptions;

public class DuelNotFoundException : Exception
{
    public int DuelId { get; }

    public DuelNotFoundException(int duelId)
        : base($"Duel with id {duelId} was not found.")
    {
        DuelId = duelId;
    }
}
