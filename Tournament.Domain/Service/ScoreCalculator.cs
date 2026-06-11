namespace Tournament.Domain.Service;

public class ScoreCalculator : IScoreCalculator
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
        if (matches is null)
            throw new ArgumentNullException(nameof(matches));

        if (penaltyPoints < 0)
            throw new ArgumentException("Penalty points must be non-negative", nameof(penaltyPoints));

        if (isDisqualified)
            return 0;

        var basePoints = 0;
        var bonusPoints = 0;
        var currentWinStreak = 0;

        foreach (var m in matches)
        {
            switch (m.Outcome)
            {
                case MatchResult.Result.Win:
                    basePoints += 3;
                    currentWinStreak++;
                    break;
                case MatchResult.Result.Draw:
                    basePoints += 1;
                    if (currentWinStreak >= 3)
                    {
                        bonusPoints += 5;
                    }
                    currentWinStreak = 0;
                    break;
                case MatchResult.Result.Loss:
                    // no points for loss
                    if (currentWinStreak >= 3)
                    {
                        bonusPoints += 5;
                    }
                    currentWinStreak = 0;
                    break;
            }
        }

        if (currentWinStreak >= 3)
        {
            bonusPoints += 5;
        }

        var total = basePoints + bonusPoints - penaltyPoints;
        return total < 0 ? 0 : total;
    }
}
