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
        // TODO: implement ranking
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns the player with the highest final score.
    /// </summary>
    public Player GetChampion(List<Player> players)
    {
        // TODO: implement champion selection
        throw new NotImplementedException();
    }
}
