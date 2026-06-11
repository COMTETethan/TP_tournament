using Microsoft.EntityFrameworkCore;
using Tournament.Api.Data;
using Tournament.Api.Data.Entities;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class PlayerServiceTests
{
    private static TournamentDbContext CreateDb()
    {
        var db = new TournamentDbContext(
            new DbContextOptionsBuilder<TournamentDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        // Seed tournament so player creation can succeed
        db.Tournaments.Add(new TournamentEntity { Name = "Test Tournament", Status = TournamentStatus.OPEN, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return db;
    }

    private readonly TournamentDbContext _db      = CreateDb();
    private readonly PlayerService       _service;

    public PlayerServiceTests() => _service = new PlayerService(_db);

    private int TournamentId => _db.Tournaments.First().Id;

    // ── AddPlayerAsync ─────────────────────────────────────────

    [Fact]
    public async Task AddPlayerAsync_ValidRequest_ReturnsPlayerResponse()
    {
        var result = await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Sir Galahad"));

        result.Should().NotBeNull();
        result.Name.Should().Be("Sir Galahad");
        result.TournamentId.Should().Be(TournamentId);
        result.IsDisqualified.Should().BeFalse();
        result.PenaltyPoints.Should().Be(0);
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AddPlayerAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.AddPlayerAsync(9999, new CreatePlayerRequest("Sir Galahad"));

        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    // ── GetPlayerAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetPlayerAsync_ExistingId_ReturnsPlayerResponse()
    {
        var created = await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Sir Galahad"));

        var result = await _service.GetPlayerAsync(created.Id);

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Sir Galahad");
    }

    [Fact]
    public async Task GetPlayerAsync_NonExistingId_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.GetPlayerAsync(9999);

        await act.Should().ThrowAsync<PlayerNotFoundException>()
                 .Where(e => e.PlayerId == 9999);
    }

    // ── GetTournamentPlayersAsync ──────────────────────────────

    [Fact]
    public async Task GetTournamentPlayersAsync_AfterAddingTwo_ReturnsBothPlayers()
    {
        await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Sir Galahad"));
        await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Dame Morgane"));

        var result = await _service.GetTournamentPlayersAsync(TournamentId);

        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Select(p => p.Name).Should().Contain("Sir Galahad").And.Contain("Dame Morgane");
    }

    [Fact]
    public async Task GetTournamentPlayersAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.GetTournamentPlayersAsync(9999);

        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    // ── DisqualifyPlayerAsync ──────────────────────────────────

    [Fact]
    public async Task DisqualifyPlayerAsync_ActivePlayer_SetsIsDisqualifiedTrue()
    {
        var created = await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Sir Galahad"));

        var result = await _service.DisqualifyPlayerAsync(created.Id);

        result.IsDisqualified.Should().BeTrue();
        result.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task DisqualifyPlayerAsync_NonExistingPlayer_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.DisqualifyPlayerAsync(9999);

        await act.Should().ThrowAsync<PlayerNotFoundException>();
    }

    // ── AddPenaltyAsync ────────────────────────────────────────

    [Fact]
    public async Task AddPenaltyAsync_ValidPenalty_AccumulatesPenaltyPoints()
    {
        var created = await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Sir Galahad"));

        var result = await _service.AddPenaltyAsync(created.Id, new AddPenaltyRequest(PenaltyPoints: 3));

        result.PenaltyPoints.Should().Be(3);
    }

    [Fact]
    public async Task AddPenaltyAsync_NegativePenalty_ThrowsArgumentException()
    {
        var created = await _service.AddPlayerAsync(TournamentId, new CreatePlayerRequest("Sir Galahad"));

        Func<Task> act = () => _service.AddPenaltyAsync(created.Id, new AddPenaltyRequest(PenaltyPoints: -1));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*PenaltyPoints*");
    }

    [Fact]
    public async Task AddPenaltyAsync_NonExistingPlayer_ThrowsPlayerNotFoundException()
    {
        Func<Task> act = () => _service.AddPenaltyAsync(9999, new AddPenaltyRequest(1));

        await act.Should().ThrowAsync<PlayerNotFoundException>();
    }
}
