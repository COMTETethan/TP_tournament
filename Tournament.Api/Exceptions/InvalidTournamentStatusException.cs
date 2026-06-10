namespace Tournament.Api.Exceptions;

public class InvalidTournamentStatusException : Exception
{
    public string AttemptedStatus { get; }

    public InvalidTournamentStatusException(string attemptedStatus)
        : base($"'{attemptedStatus}' is not a valid tournament status. Accepted values: OPEN, IN_PROGRESS, CLOSED.")
    {
        AttemptedStatus = attemptedStatus;
    }
}
