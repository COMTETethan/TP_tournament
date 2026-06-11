namespace Tournament.Api.Exceptions;

public class InvalidSeasonStatusException : Exception
{
    public string Status { get; }

    public InvalidSeasonStatusException(string status)
        : base($"Invalid season status transition: '{status}'.")
    {
        Status = status;
    }
}
