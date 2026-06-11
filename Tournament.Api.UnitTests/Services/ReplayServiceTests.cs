using FluentAssertions;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Replay")]
[Trait("Layer", "Service")]
public class ReplayServiceTests
{
    private readonly ReplayService _service = new();

    [Fact]
    public async Task StartReplayAsync_ExistingDuel_ReturnsReplayWithIsCompleteFalse()
    {
        // Arrange
        const int duelId = 1;

        // Act
        var result = await _service.StartReplayAsync(duelId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.DuelId.Should().Be(duelId);
        result.IsComplete.Should().BeFalse("replay starts as incomplete");
        result.SchemaVersion.Should().Be(1);
        result.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
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

    [Fact]
    public async Task GetReplayAsync_ExistingDuel_ReturnsReplayResponse()
    {
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

    [Fact]
    public async Task GetEventsAsync_AfterAddingEvents_ReturnsEventsSortedByOrder()
    {
        // Arrange
        const int duelId = 1;
        await _service.StartReplayAsync(duelId);
        await _service.AddEventAsync(duelId, new AddReplayEventRequest("DUEL_START", 0,    null, null, null));
        await _service.AddEventAsync(duelId, new AddReplayEventRequest("ATTACK",     500,  1,    2,    null));
        await _service.AddEventAsync(duelId, new AddReplayEventRequest("TOUCH",      1200, 1,    2,    "{\"damage\":3}"));

        // Act
        var events = (await _service.GetEventsAsync(duelId)).ToList();

        // Assert
        events.Should().HaveCountGreaterThanOrEqualTo(3);
        events.Select(e => e.EventOrder).Should().BeInAscendingOrder();
        var attack = events.First(e => e.EventType == "ATTACK");
        attack.Id.Should().BeGreaterThan(0L);
        attack.ReplayId.Should().BeGreaterThan(0);
        attack.ActorPlayerId.Should().Be(1);
        attack.TargetPlayerId.Should().Be(2);
        attack.Payload.Should().BeNull();
        var touch = events.First(e => e.EventType == "TOUCH");
        touch.Payload.Should().Be("{\"damage\":3}");
    }

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
        result.Player1SkinName.Should().BeNull();
        result.Player1AssetKey.Should().BeNull();
        result.Player2SkinName.Should().BeNull();
        result.Player2AssetKey.Should().BeNull();
        result.BackgroundSkinName.Should().BeNull();
        result.BackgroundAssetKey.Should().BeNull();
    }

    [Fact]
    public async Task GetCosmeticSnapshotAsync_UnknownDuel_ThrowsDuelNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetCosmeticSnapshotAsync(9999);

        // Assert
        await act.Should().ThrowAsync<DuelNotFoundException>()
                 .Where(e => e.DuelId == 9999);
    }

    [Fact]
    public async Task AddEventAsync_NonExistingReplay_ThrowsReplayNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.AddEventAsync(8888, new AddReplayEventRequest("ATTACK", 100, null, null, null));

        // Assert
        await act.Should().ThrowAsync<ReplayNotFoundException>();
    }

    [Fact]
    public async Task GetEventsAsync_NonExistingReplay_ThrowsReplayNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetEventsAsync(8888);

        // Assert
        await act.Should().ThrowAsync<ReplayNotFoundException>();
    }

    [Fact]
    public async Task CompleteReplayAsync_NonExistingReplay_ThrowsReplayNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.CompleteReplayAsync(8888);

        // Assert
        await act.Should().ThrowAsync<ReplayNotFoundException>();
    }
}
