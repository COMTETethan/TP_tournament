using FluentAssertions;
using Moq;
using Tournament.Domain.Service;

namespace Tournament.UnitTests;

[Trait("Category", "TournamentRanking")]
[Trait("Layer", "Domain")]
public class TournamentRankingTests
{
    private readonly Mock<IScoreCalculator> _mockCalculator = new();
    private readonly TournamentRanking _ranking;

    public TournamentRankingTests()
    {
        _ranking = new TournamentRanking(_mockCalculator.Object);
    }

    private void SetupScore(Player player, int score) =>
        _mockCalculator
            .Setup(c => c.CalculateScore(
                It.Is<List<MatchResult>>(m => ReferenceEquals(m, player.Matches)),
                player.IsDisqualified,
                player.PenaltyPoints))
            .Returns(score);

    [Fact]
    [Trait("Requirement", "REQ-T-012")]
    public void GetRanking_MultiplePlayers_SortedByScoreDescending()
    {
        // Arrange
        var galahad = new Player { Name = "Sir Galahad" };
        var morgane = new Player { Name = "Dame Morgane" };
        var noir    = new Player { Name = "Chevalier Noir" };

        SetupScore(galahad, 4);
        SetupScore(morgane, 14);
        SetupScore(noir,    2);

        // Act
        var ranking = _ranking.GetRanking([galahad, morgane, noir]);

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
        var playerA = new Player { Name = "Player A" };
        var playerB = new Player { Name = "Player B" };

        SetupScore(playerA, 4);
        SetupScore(playerB, 4);

        // Act
        var ranking = _ranking.GetRanking([playerA, playerB]);

        // Assert
        ranking.Should().HaveCount(2);
        ranking.Select(p => p.Name).Should().Contain("Player A").And.Contain("Player B");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-012")]
    public void GetRanking_NullPlayers_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ranking.GetRanking(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("players");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-013")]
    public void GetChampion_MultiplePlayers_ReturnsHighestScorePlayer()
    {
        // Arrange
        var galahad = new Player { Name = "Sir Galahad" };
        var morgane = new Player { Name = "Dame Morgane" };
        var noir    = new Player { Name = "Chevalier Noir" };

        SetupScore(galahad, 3);
        SetupScore(morgane, 14);
        SetupScore(noir,    4);

        // Act
        var champion = _ranking.GetChampion([galahad, morgane, noir]);

        // Assert
        champion.Name.Should().Be("Dame Morgane");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-013")]
    [Trait("Requirement", "REQ-T-006")]
    public void GetChampion_AllDisqualified_ReturnsPlayerWithZeroScore()
    {
        // Arrange
        var playerA = new Player { Name = "Player A", IsDisqualified = true };
        var playerB = new Player { Name = "Player B", IsDisqualified = true };

        SetupScore(playerA, 0);
        SetupScore(playerB, 0);

        // Act
        var champion = _ranking.GetChampion([playerA, playerB]);

        champion.Should().NotBeNull();
        _mockCalculator.Object.CalculateScore(champion.Matches, champion.IsDisqualified, champion.PenaltyPoints)
            .Should().Be(0, "disqualified players always score 0");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-013")]
    public void GetChampion_NullPlayers_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => _ranking.GetChampion(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("players");
    }

    [Fact]
    [Trait("Requirement", "REQ-T-013")]
    public void GetChampion_EmptyPlayersList_ThrowsInvalidOperationException()
    {
        // Act
        Action act = () => _ranking.GetChampion([]);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MatchResult_DefaultConstructor_SetsDefaultOutcome()
    {
        // Act
        var result = new MatchResult();

        // Assert
        result.Outcome.Should().Be(default(MatchResult.Result));
    }
}
