using FluentAssertions;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class ReplayServiceTests
{
    private readonly ReplayService _service = new();

    // ── StartReplayAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task StartReplayAsync_ExistingDuel_ReturnsReplayWithIsCompleteFalse()
    {
        // Arrange
        const int duelId = 1;

        // Act
        var result = await _service.StartReplayAsync(duelId);

        // Assert
        result.Should().NotBeNull();
        result.DuelId.Should().Be(duelId);
        result.IsComplete.Should().BeFalse("replay starts as incomplete");
        result.SchemaVersion.Should().Be(1);
    }

    [Fact]
    public async Task StartReplayAsync_NonExistingDuel_ThrowsDuelNotFoundException()
    {
        // Arrange
        const int nonExistingDuelId = 9999;

        // Act
        Func<Task> act = () => _service.StartReplayAsync(nonExistingDuelId);

        // Assert
        await act.Should().ThrowAsync<DuelNotFoundException>()
                 .Where(e => e.DuelId == nonExistingDuelId);
    }

    // ── GetReplayAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetReplayAsync_ExistingDuel_ReturnsReplayResponse()
    {
        // Arrange — start a replay first so it exists
        const int duelId = 1;
        await _service.StartReplayAsync(duelId);

        // Act
        var result = await _service.GetReplayAsync(duelId);

        // Assert
        result.DuelId.Should().Be(duelId);
    }

    [Fact]
    public async Task GetReplayAsync_NonExistingDuel_ThrowsReplayNotFoundException()
    {
        // Arrange
        const int nonExistingDuelId = 9999;

        // Act
        Func<Task> act = () => _service.GetReplayAsync(nonExistingDuelId);

        // Assert
        await act.Should().ThrowAsync<ReplayNotFoundException>()
                 .Where(e => e.DuelId == nonExistingDuelId);
    }

    // ── AddEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddEventAsync_ValidRequest_ReturnsReplayEventResponse()
    {
        // Arrange
        const int duelId = 1;
        await _service.StartReplayAsync(duelId);
        var request = new AddReplayEventRequest("ATTACK", 1500, 1, 2, null);

        // Act
        var result = await _service.AddEventAsync(duelId, request);

        // Assert
        result.Should().NotBeNull();
        result.EventType.Should().Be("ATTACK");
        result.OccurredAtMs.Should().Be(1500);
    }

    [Fact]
    public async Task AddEventAsync_InvalidEventType_ThrowsArgumentException()
    {
        // Arrange
        const int duelId = 1;
        await _service.StartReplayAsync(duelId);
        var request = new AddReplayEventRequest("BANANA", 0, null, null, null);

        // Act
        Func<Task> act = () => _service.AddEventAsync(duelId, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*event type*");
    }

    // ── CompleteReplayAsync ────────────────────────────────────────────────

    [Fact]
    public async Task CompleteReplayAsync_ExistingDuel_SetsIsCompleteTrue()
    {
        // Arrange
        const int duelId = 1;
        await _service.StartReplayAsync(duelId);

        // Act
        var result = await _service.CompleteReplayAsync(duelId);

        // Assert
        result.IsComplete.Should().BeTrue();
        result.DuelId.Should().Be(duelId);
    }

    // ── GetCosmeticSnapshotAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetCosmeticSnapshotAsync_ExistingDuel_ReturnsCosmeticSnapshot()
    {
        // Arrange
        const int duelId = 1;

        // Act
        var result = await _service.GetCosmeticSnapshotAsync(duelId);

        // Assert
        result.Should().NotBeNull();
        result.DuelId.Should().Be(duelId);
    }
}
