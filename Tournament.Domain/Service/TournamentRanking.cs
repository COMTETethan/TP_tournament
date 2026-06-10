namespace Tournament.Domain.Service;

public class TournamentRanking
{
    private readonly ScoreCalculator _scoreCalculator;

    public TournamentRanking(ScoreCalculator scoreCalculator)
    {
        _scoreCalculator = scoreCalculator;
    }

    /// <summary>
    /// Returns players sorted by final score descending.
    /// </summary>
    public List<Player> GetRanking(List<Player> players)
    {
        if (players is null)
            throw new ArgumentNullException(nameof(players));

        return players
            .OrderByDescending(p => _scoreCalculator.CalculateScore(p.Matches, p.IsDisqualified, p.PenaltyPoints))
            .ToList();
    }

    /// <summary>
    /// Returns the player with the highest final score.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when players is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when players list is empty.</exception>
    public Player GetChampion(List<Player> players)
    {
        if (players is null)
            throw new ArgumentNullException(nameof(players));

        if (players.Count == 0)
            throw new InvalidOperationException("Players list cannot be empty.");

        return players
            .OrderByDescending(p => _scoreCalculator.CalculateScore(p.Matches, p.IsDisqualified, p.PenaltyPoints))
            .First();
    }
}
