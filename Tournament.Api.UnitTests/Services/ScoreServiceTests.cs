using FluentAssertions;
using Moq;
using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class ScoreServiceTests
{
    private readonly Mock<IPlayerService>     _players     = new();
    private readonly Mock<IDuelService>       _duels       = new();
    private readonly Mock<ITournamentService> _tournaments = new();
    private readonly ScoreService             _service;

    // Reusable fixture data
    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly TournamentResponse T1 = new(1, "Grand Prix", "ACTIVE", Now);
    private static readonly PlayerResponse P1  = new(1, 1, "Arthur", IsDisqualified: false, PenaltyPoints: 0);
    private static readonly PlayerResponse P2  = new(2, 1, "Morgane", IsDisqualified: true,  PenaltyPoints: 0);
    private static readonly PlayerResponse P3  = new(3, 1, "Lancelot", IsDisqualified: false, PenaltyPoints: 0);

    // Two wins for P1, two losses for P2 — 6 pts for P1, 0 for P2 (disqualified), 0 for P3 (no duels)
    private static readonly List<DuelResponse> T1Duels = new()
    {
        new(1, 1, Player1Id: 1, Player2Id: 2, Outcome: "PLAYER1_WIN", DuelOrder: 1, PlayedAt: Now, DurationSeconds: 120),
        new(2, 1, Player1Id: 1, Player2Id: 2, Outcome: "PLAYER1_WIN", DuelOrder: 2, PlayedAt: Now, DurationSeconds: 150),
    };

    public ScoreServiceTests()
    {
        _service = new ScoreService(_players.Object, _duels.Object, _tournaments.Object);
    }

    [Fact]
    public void DefaultConstructor_CreatesInstanceWithInMemoryServices()
    {
        // Used by ASP.NET Core DI when resolving with concrete in-memory implementations
        var service = new ScoreService();
        service.Should().NotBeNull();
    }

    // ── GetPlayerScoreAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayerScoreAsync_HealthyPlayerWithWins_ReturnsCorrectScore()
    {
        // Arrange
        _players.Setup(s => s.GetPlayerAsync(1)).ReturnsAsync(P1);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(T1Duels);

        // Act
        var result = await _service.GetPlayerScoreAsync(1);

        // Assert
        result.PlayerId.Should().Be(1);
        result.FinalScore.Should().Be(6, "2 wins × 3 pts = 6");
        result.IsDisqualified.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_DisqualifiedPlayer_ReturnsFinalScoreOfZero()
    {
        // Arrange
        _players.Setup(s => s.GetPlayerAsync(2)).ReturnsAsync(P2);

        // Act
        var result = await _service.GetPlayerScoreAsync(2);

        // Assert
        result.FinalScore.Should().Be(0, "disqualified players always score 0");
        result.IsDisqualified.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_NonExistingPlayer_ThrowsPlayerNotFoundException()
    {
        // Arrange
        _players.Setup(s => s.GetPlayerAsync(9999)).ThrowsAsync(new PlayerNotFoundException(9999));

        // Act
        Func<Task> act = () => _service.GetPlayerScoreAsync(9999);

        // Assert
        await act.Should().ThrowAsync<PlayerNotFoundException>()
                 .Where(e => e.PlayerId == 9999);
    }

    // ── GetTournamentRankingAsync ──────────────────────────────────────────────

    [Fact]
    public async Task GetTournamentRankingAsync_MultiplePlayers_ReturnsSortedByScoreDescending()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(T1);
        _players.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(new[] { P1, P2, P3 });
        _players.Setup(s => s.GetPlayerAsync(1)).ReturnsAsync(P1);
        _players.Setup(s => s.GetPlayerAsync(2)).ReturnsAsync(P2);
        _players.Setup(s => s.GetPlayerAsync(3)).ReturnsAsync(P3);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(T1Duels);

        // Act
        var result = await _service.GetTournamentRankingAsync(1);

        // Assert
        result.TournamentId.Should().Be(1);
        result.Ranking.Should().HaveCount(3);
        var scores = result.Ranking.Select(p => p.FinalScore).ToList();
        scores.Should().BeInDescendingOrder("ranking must be sorted by score descending");
        result.Ranking[0].PlayerId.Should().Be(1, "player 1 has the most points");
    }

    [Fact]
    public async Task GetTournamentRankingAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(9999)).ThrowsAsync(new TournamentNotFoundException(9999));

        // Act
        Func<Task> act = () => _service.GetTournamentRankingAsync(9999);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    // ── GetTournamentChampionAsync ─────────────────────────────────────────────

    [Fact]
    public async Task GetTournamentChampionAsync_MultiplePlayers_ReturnsPlayerWithHighestScore()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(T1);
        _players.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(new[] { P1, P2, P3 });
        _players.Setup(s => s.GetPlayerAsync(1)).ReturnsAsync(P1);
        _players.Setup(s => s.GetPlayerAsync(2)).ReturnsAsync(P2);
        _players.Setup(s => s.GetPlayerAsync(3)).ReturnsAsync(P3);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(T1Duels);

        // Act
        var champion = await _service.GetTournamentChampionAsync(1);

        // Assert
        champion.Should().NotBeNull();
        champion.PlayerId.Should().Be(1);
        champion.FinalScore.Should().Be(6);
    }

    [Fact]
    public async Task GetTournamentChampionAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(9999)).ThrowsAsync(new TournamentNotFoundException(9999));

        // Act
        Func<Task> act = () => _service.GetTournamentChampionAsync(9999);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    [Fact]
    public async Task GetTournamentChampionAsync_EmptyTournament_ThrowsInvalidOperationException()
    {
        // Arrange — tournament exists but has no players
        _tournaments.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(T1);
        _players.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(Array.Empty<PlayerResponse>());

        // Act
        Func<Task> act = () => _service.GetTournamentChampionAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_Player1LossAndDraw_ReturnsCorrectScore()
    {
        var duels = new List<DuelResponse>
        {
            new(3, 1, Player1Id: 1, Player2Id: 2, Outcome: "PLAYER2_WIN", DuelOrder: 1, PlayedAt: Now, DurationSeconds: 120),
            new(4, 1, Player1Id: 1, Player2Id: 2, Outcome: "DRAW",        DuelOrder: 2, PlayedAt: Now, DurationSeconds: 90),
        };
        _players.Setup(s => s.GetPlayerAsync(1)).ReturnsAsync(P1);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(duels);

        var result = await _service.GetPlayerScoreAsync(1);

        result.FinalScore.Should().Be(0, "1 loss (-1) + 1 draw (+1) - 0 penalty = 0, floored at 0");
    }

    [Fact]
    public async Task GetPlayerScoreAsync_PlayerAsPlayer2_AllOutcomes_ReturnsCorrectScore()
    {
        var p3 = new PlayerResponse(3, 1, "Lancelot", IsDisqualified: false, PenaltyPoints: 0);
        var duels = new List<DuelResponse>
        {
            new(10, 1, Player1Id: 1, Player2Id: 3, Outcome: "PLAYER2_WIN", DuelOrder: 1, PlayedAt: Now, DurationSeconds: 120),
            new(11, 1, Player1Id: 1, Player2Id: 3, Outcome: "PLAYER1_WIN", DuelOrder: 2, PlayedAt: Now, DurationSeconds: 150),
            new(12, 1, Player1Id: 1, Player2Id: 3, Outcome: "DRAW",        DuelOrder: 3, PlayedAt: Now, DurationSeconds: 90),
        };
        _players.Setup(s => s.GetPlayerAsync(3)).ReturnsAsync(p3);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(duels);

        var result = await _service.GetPlayerScoreAsync(3);

        result.FinalScore.Should().Be(3, "1 win (3) + 1 loss (-1) + 1 draw (+1) = 3");
    }
}
