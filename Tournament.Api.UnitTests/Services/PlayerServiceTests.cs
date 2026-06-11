using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class PlayerServiceTests
{
    private readonly PlayerService _service = new();

    // ── AddPlayerAsync ─────────────────────────────────────────

    [Fact]
    public async Task AddPlayerAsync_ValidRequest_ReturnsPlayerResponse()
    {
        // Arrange
        var request = new CreatePlayerRequest("Sir Galahad");

        // Act
        var result = await _service.AddPlayerAsync(tournamentId: 1, request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Sir Galahad");
        result.TournamentId.Should().Be(1);
        result.IsDisqualified.Should().BeFalse();
        result.PenaltyPoints.Should().Be(0);
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddPlayerAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Arrange
        var request = new CreatePlayerRequest("Sir Galahad");
        const int nonExistingTournamentId = 9999;

        // Act
        Func<Task> act = () => _service.AddPlayerAsync(nonExistingTournamentId, request);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == nonExistingTournamentId);
    }

    [Fact]
    public async Task AddPlayerAsync_WithClassAndLevel_StoresChampionAttributes()
    {
        var request = new CreatePlayerRequest("Sir Galahad", ClassId: 1, Level: 4);

        var result = await _service.AddPlayerAsync(1, request);

        result.ClassId.Should().Be(1);
        result.Level.Should().Be(4);
    }

    [Fact]
    public async Task AddPlayerAsync_NoClass_DefaultsToNoClassAndLevelOne()
    {
        var result = await _service.AddPlayerAsync(1, new CreatePlayerRequest("Squire"));

        result.ClassId.Should().BeNull();
        result.Level.Should().Be(1);
    }

    [Fact]
    public async Task AddPlayerAsync_UnknownClass_ThrowsClassNotFoundException()
    {
        var request = new CreatePlayerRequest("Sir Galahad", ClassId: 999, Level: 1);

        Func<Task> act = () => _service.AddPlayerAsync(1, request);

        await act.Should().ThrowAsync<ClassNotFoundException>()
                 .Where(e => e.ClassId == 999);
    }

    [Fact]
    public async Task AddPlayerAsync_LevelBelowOne_ThrowsArgumentException()
    {
        var request = new CreatePlayerRequest("Sir Galahad", ClassId: 1, Level: 0);

        Func<Task> act = () => _service.AddPlayerAsync(1, request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetPlayerAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPlayerAsync_ExistingId_ReturnsPlayerResponse()
    {
        // Arrange
        var created = await _service.AddPlayerAsync(1, new CreatePlayerRequest("Sir Galahad"));

        // Act
        var result = await _service.GetPlayerAsync(created.Id);

        // Assert
        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Sir Galahad");
    }

    [Fact]
    public async Task GetPlayerAsync_NonExistingId_ThrowsPlayerNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetPlayerAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<PlayerNotFoundException>()
                 .Where(e => e.PlayerId == nonExistingId);
    }

    // ── GetTournamentPlayersAsync ──────────────────────────────

    [Fact]
    public async Task GetTournamentPlayersAsync_AfterAddingTwo_ReturnsBothPlayers()
    {
        // Arrange
        await _service.AddPlayerAsync(1, new CreatePlayerRequest("Sir Galahad"));
        await _service.AddPlayerAsync(1, new CreatePlayerRequest("Dame Morgane"));

        // Act
        var result = await _service.GetTournamentPlayersAsync(tournamentId: 1);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Select(p => p.Name).Should().Contain("Sir Galahad").And.Contain("Dame Morgane");
    }

    [Fact]
    public async Task GetTournamentPlayersAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetTournamentPlayersAsync(9999);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    // ── DisqualifyPlayerAsync ──────────────────────────────────

    [Fact]
    public async Task DisqualifyPlayerAsync_ActivePlayer_SetsIsDisqualifiedTrue()
    {
        // Arrange
        var created = await _service.AddPlayerAsync(1, new CreatePlayerRequest("Sir Galahad"));

        // Act
        var result = await _service.DisqualifyPlayerAsync(created.Id);

        // Assert
        result.IsDisqualified.Should().BeTrue();
        result.Id.Should().Be(created.Id);
    }

    // ── AddPenaltyAsync ────────────────────────────────────────

    [Fact]
    public async Task AddPenaltyAsync_ValidPenalty_AccumulatesPenaltyPoints()
    {
        // Arrange
        var created = await _service.AddPlayerAsync(1, new CreatePlayerRequest("Sir Galahad"));
        var request = new AddPenaltyRequest(PenaltyPoints: 3);

        // Act
        var result = await _service.AddPenaltyAsync(created.Id, request);

        // Assert
        result.PenaltyPoints.Should().Be(3);
    }

    [Fact]
    public async Task AddPenaltyAsync_NegativePenalty_ThrowsArgumentException()
    {
        // Arrange
        var created = await _service.AddPlayerAsync(1, new CreatePlayerRequest("Sir Galahad"));
        var request = new AddPenaltyRequest(PenaltyPoints: -1);

        // Act
        Func<Task> act = () => _service.AddPenaltyAsync(created.Id, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*PenaltyPoints*");
    }

    [Fact]
    public async Task DisqualifyPlayerAsync_NonExistingPlayer_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.DisqualifyPlayerAsync(9999);

        await act.Should().ThrowAsync<PlayerNotFoundException>();
    }

    [Fact]
    public async Task AddPenaltyAsync_NonExistingPlayer_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.AddPenaltyAsync(9999, new AddPenaltyRequest(1));

        await act.Should().ThrowAsync<PlayerNotFoundException>();
    }
}
