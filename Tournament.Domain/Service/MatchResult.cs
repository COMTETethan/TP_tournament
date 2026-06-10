namespace Tournament.Domain.Service;

public class MatchResult
{
    public enum Result
    {
        Win,
        Draw,
        Loss
    }

    public Result Outcome { get; set; }

    public MatchResult(Result outcome)
    {
        Outcome = outcome;
    }

    public MatchResult() { }
}
