namespace Tournament.Api.Exceptions;

public class ReplayNotFoundException : Exception
{
    public int DuelId { get; }

    public ReplayNotFoundException(int duelId)
        : base($"No replay found for duel with id {duelId}.")
    {
        DuelId = duelId;
    }
}
