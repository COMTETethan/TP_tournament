using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class ScoreServiceTests
{
    private readonly ScoreService _service = new();

    // ── GetPlayerScoreAsync ────────────────────────────────────

    [Fact]
    public async Task GetPlayerScoreAsync_HealthyPlayerWithWins_ReturnsCorrectScore()
    {
        // Arrange — player 1 in tournament 1 has 3 consecutive wins (9 + 5 bonus = 14)
        const int playerId = 1;

        // Act
        var result = await _service.GetPlayerScoreAsync(playerId);

        // Assert
        result.Should().NotBeNull();
        result.PlayerId.Should().Be(playerId);
        result.FinalScore.Should().BeGreaterThanOrEqualTo(0, "score is never negative");
        result.IsDisqualified.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_DisqualifiedPlayer_ReturnsFinalScoreOfZero()
    {
        // Arrange — player 2 is disqualified
        const int disqualifiedPlayerId = 2;

        // Act
        var result = await _service.GetPlayerScoreAsync(disqualifiedPlayerId);

        // Assert
        result.FinalScore.Should().Be(0, "disqualified players always score 0");
        result.IsDisqualified.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_NonExistingPlayer_ThrowsPlayerNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetPlayerScoreAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<PlayerNotFoundException>()
                 .Where(e => e.PlayerId == nonExistingId);
    }

    // ── GetTournamentRankingAsync ──────────────────────────────

    [Fact]
    public async Task GetTournamentRankingAsync_MultiplePlayers_ReturnsSortedByScoreDescending()
    {
        // Arrange
        const int tournamentId = 1;

        // Act
        var result = await _service.GetTournamentRankingAsync(tournamentId);

        // Assert
        result.Should().NotBeNull();
        result.TournamentId.Should().Be(tournamentId);
        result.Ranking.Should().NotBeNull();

        // Scores must be in descending order
        var scores = result.Ranking.Select(p => p.FinalScore).ToList();
        scores.Should().BeInDescendingOrder("ranking must be sorted by score descending");
    }

    [Fact]
    public async Task GetTournamentRankingAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetTournamentRankingAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == nonExistingId);
    }

    // ── GetTournamentChampionAsync ─────────────────────────────

    [Fact]
    public async Task GetTournamentChampionAsync_MultiplePlayers_ReturnsPlayerWithHighestScore()
    {
        // Arrange
        const int tournamentId = 1;

        // Act
        var champion = await _service.GetTournamentChampionAsync(tournamentId);

        // Assert
        champion.Should().NotBeNull();

        // Champion must have the highest score of all ranked players
        var ranking = await _service.GetTournamentRankingAsync(tournamentId);
        champion.FinalScore.Should().Be(ranking.Ranking.Max(p => p.FinalScore));
    }

    [Fact]
    public async Task GetTournamentChampionAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetTournamentChampionAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == nonExistingId);
    }
}
