namespace Tournament.Api.Exceptions;

public class ChallengeNotFoundException : Exception
{
    public int ChallengeId { get; }

    public ChallengeNotFoundException(int challengeId)
        : base($"Challenge with id {challengeId} was not found.")
    {
        ChallengeId = challengeId;
    }
}
