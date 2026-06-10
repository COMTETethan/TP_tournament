using FluentAssertions;
using Tournament.Domain.Service;

namespace Tournament.UnitTests;

public class TournamentRankingTests
{
    private readonly ScoreCalculator _scoreCalculator = new();
    private readonly TournamentRanking _ranking;

    public TournamentRankingTests()
    {
        _ranking = new TournamentRanking(_scoreCalculator);
    }

    private static MatchResult W() => new(MatchResult.Result.Win);
    private static MatchResult D() => new(MatchResult.Result.Draw);
    private static MatchResult L() => new(MatchResult.Result.Loss);

    // ── GetRanking ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Requirement", "REQ-T-012")]
    public void GetRanking_MultiplePlayers_SortedByScoreDescending()
    {
        // Arrange
        var players = new List<Player>
        {
            new() { Name = "Sir Galahad",   Matches = [W(), L(), D()] },  // 4 pts
            new() { Name = "Dame Morgane",  Matches = [W(), W(), W()] },  // 14 pts
            new() { Name = "Chevalier Noir", Matches = [D(), D()] },       // 2 pts
        };

        // Act
        var ranking = _ranking.GetRanking(players);

        // Assert
        ranking.Should().HaveCount(3);
        ranking[0].Name.Should().Be("Dame Morgane");
        ranking[1].Name.Should().Be("Sir Galahad");
        ranking[2].Name.Should().Be("Chevalier Noir");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-012")]
    public void GetRanking_TiedPlayers_BothPresentInRanking()
    {
        // Arrange
        var players = new List<Player>
        {
            new() { Name = "Player A", Matches = [W(), D()] },  // 4 pts
            new() { Name = "Player B", Matches = [W(), D()] },  // 4 pts
        };

        // Act
        var ranking = _ranking.GetRanking(players);

        // Assert
        ranking.Should().HaveCount(2);
        ranking.Select(p => p.Name).Should().Contain("Player A").And.Contain("Player B");
    }

    // ── GetChampion ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Requirement", "REQ-T-013")]
    public void GetChampion_MultiplePlayers_ReturnsHighestScorePlayer()
    {
        // Arrange
        var players = new List<Player>
        {
            new() { Name = "Sir Galahad",   Matches = [W(), L()] },       // 3 pts
            new() { Name = "Dame Morgane",  Matches = [W(), W(), W()] },  // 14 pts
            new() { Name = "Chevalier Noir", Matches = [W(), D()] },      // 4 pts
        };

        // Act
        var champion = _ranking.GetChampion(players);

        // Assert
        champion.Name.Should().Be("Dame Morgane");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-013")]
    [Trait("Requirement", "REQ-T-006")]
    public void GetChampion_AllDisqualified_ReturnsPlayerWithZeroScore()
    {
        // Arrange
        var players = new List<Player>
        {
            new() { Name = "Player A", Matches = [W(), W()], IsDisqualified = true },
            new() { Name = "Player B", Matches = [W(), W(), W()], IsDisqualified = true },
        };

        // Act
        var champion = _ranking.GetChampion(players);

        // Assert — champion exists but has score 0
        champion.Should().NotBeNull();
        _scoreCalculator.CalculateScore(champion.Matches, champion.IsDisqualified).Should().Be(0);
    }
}
