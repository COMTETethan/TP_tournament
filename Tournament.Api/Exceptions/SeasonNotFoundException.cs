namespace Tournament.Api.Exceptions;

public class SeasonNotFoundException : Exception
{
    public int SeasonId { get; }

    public SeasonNotFoundException(int seasonId)
        : base($"Season with id {seasonId} was not found.")
    {
        SeasonId = seasonId;
    }
}
