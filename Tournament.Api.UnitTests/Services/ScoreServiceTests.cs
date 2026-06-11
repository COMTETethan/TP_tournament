using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;
using Tournament.Api.UnitTests.TestData;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Score")]
[Trait("Layer", "Service")]
public class ScoreServiceTests
{
    private readonly Mock<ITournamentPlayerService> _registrations = new();
    private readonly Mock<IDuelService>             _duels        = new();
    private readonly Mock<ITournamentService>       _tournaments  = new();
    private readonly ScoreService                   _service;

    private static readonly DateTime Now = DateTime.UtcNow;
    private static readonly TournamentResponse T1 = new(1, "Grand Prix", "ACTIVE", Now);

    private static readonly RegistrationResponse R1 = new(1, 1, "Arthur",   1, 1, IsDisqualified: false, PenaltyPoints: 0);
    private static readonly RegistrationResponse R2 = new(1, 2, "Morgane",  2, 1, IsDisqualified: true,  PenaltyPoints: 0);
    private static readonly RegistrationResponse R3 = new(1, 3, "Lancelot", 4, 1, IsDisqualified: false, PenaltyPoints: 0);

    private static readonly List<DuelResponse> T1Duels = new()
    {
        new(1, 1, Player1Id: 1, Player2Id: 2, Outcome: "PLAYER1_WIN", DuelOrder: 1, PlayedAt: Now, DurationSeconds: 120),
        new(2, 1, Player1Id: 1, Player2Id: 2, Outcome: "PLAYER1_WIN", DuelOrder: 2, PlayedAt: Now, DurationSeconds: 150),
    };

    public ScoreServiceTests()
    {
        _service = new ScoreService(_registrations.Object, _duels.Object, _tournaments.Object);
    }

    [Fact]
    public void DefaultConstructor_CreatesInstanceWithInMemoryServices()
    {
        // Act
        var service = new ScoreService();
        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_ActivePlayerWithWins_ReturnsCorrectScore()
    {
        // Arrange
        _registrations.Setup(s => s.GetRegistrationAsync(1, 1)).ReturnsAsync(R1);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(T1Duels);

        // Act
        var result = await _service.GetPlayerScoreAsync(1, 1);

        // Assert
        result.PlayerId.Should().Be(1);
        result.FinalScore.Should().Be(6, "2 wins × 3 pts = 6");
        result.IsDisqualified.Should().BeFalse();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_DisqualifiedRegistration_ReturnsZero()
    {
        // Arrange
        _registrations.Setup(s => s.GetRegistrationAsync(1, 2)).ReturnsAsync(R2);

        // Act
        var result = await _service.GetPlayerScoreAsync(1, 2);

        // Assert
        result.FinalScore.Should().Be(0, "disqualified registrations always score 0");
        result.IsDisqualified.Should().BeTrue();
    }

    [Fact]
    public async Task GetPlayerScoreAsync_NotRegistered_ThrowsRegistrationNotFoundException()
    {
        // Arrange
        _registrations.Setup(s => s.GetRegistrationAsync(1, 9999))
                      .ThrowsAsync(new RegistrationNotFoundException(1, 9999));

        // Act
        Func<Task> act = () => _service.GetPlayerScoreAsync(1, 9999);

        // Assert
        await act.Should().ThrowAsync<RegistrationNotFoundException>()
                 .Where(e => e.PlayerId == 9999);
    }

    [Fact]
    public async Task GetPlayerScoreAsync_LossAndDraw_FlooredAtZero()
    {
        // Arrange
        var duels = new List<DuelResponse>
        {
            new(3, 1, Player1Id: 1, Player2Id: 2, Outcome: "PLAYER2_WIN", DuelOrder: 1, PlayedAt: Now, DurationSeconds: 120),
            new(4, 1, Player1Id: 1, Player2Id: 2, Outcome: "DRAW",        DuelOrder: 2, PlayedAt: Now, DurationSeconds: 90),
        };
        _registrations.Setup(s => s.GetRegistrationAsync(1, 1)).ReturnsAsync(R1);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(duels);

        // Act
        var result = await _service.GetPlayerScoreAsync(1, 1);

        // Assert
        result.FinalScore.Should().Be(0, "1 loss (-1) + 1 draw (+1) = 0, floored at 0");
    }

    [Fact]
    public async Task GetPlayerScoreAsync_PlayerAsPlayer2_AllOutcomes_ReturnsCorrectScore()
    {
        // Arrange
        var duels = new List<DuelResponse>
        {
            new(10, 1, Player1Id: 1, Player2Id: 3, Outcome: "PLAYER2_WIN", DuelOrder: 1, PlayedAt: Now, DurationSeconds: 120),
            new(11, 1, Player1Id: 1, Player2Id: 3, Outcome: "PLAYER1_WIN", DuelOrder: 2, PlayedAt: Now, DurationSeconds: 150),
            new(12, 1, Player1Id: 1, Player2Id: 3, Outcome: "DRAW",        DuelOrder: 3, PlayedAt: Now, DurationSeconds: 90),
        };
        _registrations.Setup(s => s.GetRegistrationAsync(1, 3)).ReturnsAsync(R3);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(duels);

        // Act
        var result = await _service.GetPlayerScoreAsync(1, 3);

        // Assert
        result.FinalScore.Should().Be(3, "1 win (3) + 1 loss (-1) + 1 draw (+1) = 3");
    }

    [Fact]
    public async Task GetTournamentRankingAsync_MultiplePlayers_ReturnsSortedByScoreDescending()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(T1);
        _registrations.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(new[] { R1, R2, R3 });
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(T1Duels);

        // Act
        var result = await _service.GetTournamentRankingAsync(1);

        // Assert
        result.TournamentId.Should().Be(1);
        result.Ranking.Should().HaveCount(3);
        result.Ranking.Select(p => p.FinalScore).Should().BeInDescendingOrder();
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

    [Fact]
    public async Task GetTournamentChampionAsync_MultiplePlayers_ReturnsHighestScore()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(T1);
        _registrations.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(new[] { R1, R2, R3 });
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(T1Duels);

        // Act
        var champion = await _service.GetTournamentChampionAsync(1);

        // Assert
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
        await act.Should().ThrowAsync<TournamentNotFoundException>();
    }

    [Fact]
    public async Task GetTournamentChampionAsync_EmptyTournament_ThrowsInvalidOperationException()
    {
        // Arrange
        _tournaments.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(T1);
        _registrations.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(Array.Empty<RegistrationResponse>());

        // Act
        Func<Task> act = () => _service.GetTournamentChampionAsync(1);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [ClassData(typeof(PlayerScoreCases))]
    public async Task GetPlayerScoreAsync_VariousOutcomes_ReturnsExpectedScore(
        string[] outcomes, int penaltyPoints, int expectedScore, string _reason)
    {
        // Arrange
        var registration = new RegistrationResponse(1, 50, "Test", 1, 1, IsDisqualified: false, PenaltyPoints: penaltyPoints);
        var duels = outcomes
            .Select((outcome, i) => new DuelResponse(
                200 + i, 1, Player1Id: 50, Player2Id: 99,
                Outcome: outcome, DuelOrder: i + 1,
                PlayedAt: Now, DurationSeconds: 120))
            .ToList();

        _registrations.Setup(s => s.GetRegistrationAsync(1, 50)).ReturnsAsync(registration);
        _duels.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(duels);

        // Act
        var result = await _service.GetPlayerScoreAsync(1, 50);

        // Assert
        result.FinalScore.Should().Be(expectedScore, _reason);
    }
}
