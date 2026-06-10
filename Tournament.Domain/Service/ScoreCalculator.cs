namespace Tournament.Domain.Service;

public class ScoreCalculator
{
    /// <summary>
    /// Calculates the final score of a player according to tournament rules.
    /// </summary>
    /// <param name="matches">Match results in chronological order</param>
    /// <param name="isDisqualified">True if the player is disqualified</param>
    /// <param name="penaltyPoints">Penalty points to subtract (must be >= 0)</param>
    /// <returns>Final score (never negative)</returns>
    public int CalculateScore(List<MatchResult> matches, bool isDisqualified = false, int penaltyPoints = 0)
    {
        // TODO: implement according to tournament rules
        throw new NotImplementedException();
    }
}
