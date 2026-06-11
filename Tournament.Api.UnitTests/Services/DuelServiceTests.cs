using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class DuelServiceTests
{
    private readonly DuelService _service = new();

    // ── CreateDuelAsync ────────────────────────────────────────

    [Fact]
    public async Task CreateDuelAsync_ValidRequest_ReturnsDuelResponse()
    {
        // Arrange
        var request = new CreateDuelRequest(Player1Id: 1, Player2Id: 2, DuelOrder: 1);

        // Act
        var result = await _service.CreateDuelAsync(tournamentId: 1, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TournamentId.Should().Be(1);
        result.Player1Id.Should().Be(1);
        result.Player2Id.Should().Be(2);
        result.Outcome.Should().BeNull("duel just created, no outcome yet");
        result.DurationSeconds.Should().BeNull("duel has not ended yet");
        result.PlayedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateDuelAsync_SamePlayerTwice_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateDuelRequest(Player1Id: 1, Player2Id: 1, DuelOrder: 1);

        // Act
        Func<Task> act = () => _service.CreateDuelAsync(tournamentId: 1, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*same player*");
    }

    // ── GetDuelAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetDuelAsync_ExistingId_ReturnsDuelResponse()
    {
        // Arrange
        var created = await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));

        // Act
        var result = await _service.GetDuelAsync(created.Id);

        // Assert
        result.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetDuelAsync_NonExistingId_ThrowsDuelNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetDuelAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<DuelNotFoundException>()
                 .Where(e => e.DuelId == nonExistingId);
    }

    // ── GetTournamentDuelsAsync ────────────────────────────────

    [Fact]
    public async Task GetTournamentDuelsAsync_AfterCreatingTwo_ReturnsBothDuels()
    {
        // Arrange
        await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));
        await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 3, 2));

        // Act
        var result = await _service.GetTournamentDuelsAsync(tournamentId: 1);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── SetDuelOutcomeAsync ────────────────────────────────────

    [Fact]
    public async Task SetDuelOutcomeAsync_ValidOutcome_UpdatesOutcome()
    {
        // Arrange
        var created = await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));
        var request = new SetDuelOutcomeRequest("PLAYER1_WIN");

        // Act
        var result = await _service.SetDuelOutcomeAsync(created.Id, request);

        // Assert
        result.Outcome.Should().Be("PLAYER1_WIN");
    }

    [Fact]
    public async Task SetDuelOutcomeAsync_InvalidOutcome_ThrowsArgumentException()
    {
        // Arrange
        var created = await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));
        var request = new SetDuelOutcomeRequest("BANANA");

        // Act
        Func<Task> act = () => _service.SetDuelOutcomeAsync(created.Id, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*outcome*");
    }

    // ── EndDuelAsync ───────────────────────────────────────────

    [Fact]
    public async Task EndDuelAsync_ValidDuration_SetsDurationAndTimestamp()
    {
        // Arrange
        var created = await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));
        var request = new EndDuelRequest(DurationSeconds: 180);

        // Act
        var result = await _service.EndDuelAsync(created.Id, request);

        // Assert
        result.DurationSeconds.Should().Be(180);
    }

    [Fact]
    public async Task EndDuelAsync_AlreadyEndedDuel_ThrowsDuelAlreadyEndedException()
    {
        // Arrange
        var created = await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));
        await _service.EndDuelAsync(created.Id, new EndDuelRequest(180));

        // Act — second call should fail
        Func<Task> act = () => _service.EndDuelAsync(created.Id, new EndDuelRequest(200));

        // Assert
        await act.Should().ThrowAsync<DuelAlreadyEndedException>()
                 .Where(e => e.DuelId == created.Id);
    }

    [Fact]
    public async Task SetDuelOutcomeAsync_WhitespaceOutcome_ThrowsArgumentException()
    {
        var created = await _service.CreateDuelAsync(1, new CreateDuelRequest(1, 2, 1));

        Func<Task> act = () => _service.SetDuelOutcomeAsync(created.Id, new SetDuelOutcomeRequest("   "));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetDuelOutcomeAsync_NonExistingDuel_ThrowsDuelNotFoundException()
    {
        Func<Task> act = () => _service.SetDuelOutcomeAsync(9999, new SetDuelOutcomeRequest("PLAYER1_WIN"));

        await act.Should().ThrowAsync<DuelNotFoundException>();
    }

    [Fact]
    public async Task EndDuelAsync_NonExistingDuel_ThrowsDuelNotFoundException()
    {
        Func<Task> act = () => _service.EndDuelAsync(9999, new EndDuelRequest(120));

        await act.Should().ThrowAsync<DuelNotFoundException>();
    }
}
