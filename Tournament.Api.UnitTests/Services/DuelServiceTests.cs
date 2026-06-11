using Microsoft.EntityFrameworkCore;
using Tournament.Api.Data;
using Tournament.Api.Data.Entities;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;
using Tournament.Api.UnitTests.TestData;

namespace Tournament.Api.UnitTests.Services;

public class DuelServiceTests
{
    private static TournamentDbContext CreateDb()
    {
        var db = new TournamentDbContext(
            new DbContextOptionsBuilder<TournamentDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        db.Tournaments.Add(new TournamentEntity { Name = "Test Tournament", Status = TournamentStatus.OPEN, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return db;
    }

    private readonly TournamentDbContext _db     = CreateDb();
    private readonly DuelService         _service;

    public DuelServiceTests() => _service = new DuelService(_db);

    private int TournamentId => _db.Tournaments.First().Id;

    // ── CreateDuelAsync ────────────────────────────────────────

    [Fact]
    public async Task CreateDuelAsync_ValidRequest_ReturnsDuelResponse()
    {
        var result = await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(Player1Id: 1, Player2Id: 2, DuelOrder: 1));

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TournamentId.Should().Be(TournamentId);
        result.Player1Id.Should().Be(1);
        result.Player2Id.Should().Be(2);
        result.Outcome.Should().BeNull("duel just created, no outcome yet");
        result.DurationSeconds.Should().BeNull("duel has not ended yet");
        result.PlayedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateDuelAsync_SamePlayerTwice_ThrowsArgumentException()
    {
        Func<Task> act = () => _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(Player1Id: 1, Player2Id: 1, DuelOrder: 1));

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*same player*");
    }

    [Fact]
    public async Task CreateDuelAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.CreateDuelAsync(9999, new CreateDuelRequest(1, 2, 1));

        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    // ── GetDuelAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetDuelAsync_ExistingId_ReturnsDuelResponse()
    {
        var created = await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 2, 1));

        var result = await _service.GetDuelAsync(created.Id);

        result.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task GetDuelAsync_NonExistingId_ThrowsDuelNotFoundException()
    {
        Func<Task> act = () => _service.GetDuelAsync(9999);

        await act.Should().ThrowAsync<DuelNotFoundException>()
                 .Where(e => e.DuelId == 9999);
    }

    // ── GetTournamentDuelsAsync ────────────────────────────────

    [Fact]
    public async Task GetTournamentDuelsAsync_AfterCreatingTwo_ReturnsBothDuels()
    {
        await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 2, 1));
        await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 3, 2));

        var result = await _service.GetTournamentDuelsAsync(TournamentId);

        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── SetDuelOutcomeAsync ────────────────────────────────────

    [Fact]
    public async Task SetDuelOutcomeAsync_ValidOutcome_UpdatesOutcome()
    {
        var created = await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 2, 1));

        var result = await _service.SetDuelOutcomeAsync(created.Id, new SetDuelOutcomeRequest("PLAYER1_WIN"));

        result.Outcome.Should().Be("PLAYER1_WIN");
    }

    [Theory]
    [ClassData(typeof(InvalidDuelOutcomeCases))]
    public async Task SetDuelOutcomeAsync_InvalidOutcome_ThrowsArgumentException(string invalidOutcome)
    {
        var created = await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 2, 1));

        Func<Task> act = () => _service.SetDuelOutcomeAsync(created.Id, new SetDuelOutcomeRequest(invalidOutcome));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetDuelOutcomeAsync_NonExistingDuel_ThrowsDuelNotFoundException()
    {
        Func<Task> act = () => _service.SetDuelOutcomeAsync(9999, new SetDuelOutcomeRequest("PLAYER1_WIN"));

        await act.Should().ThrowAsync<DuelNotFoundException>();
    }

    // ── EndDuelAsync ───────────────────────────────────────────

    [Fact]
    public async Task EndDuelAsync_ValidDuration_SetsDurationAndTimestamp()
    {
        var created = await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 2, 1));

        var result = await _service.EndDuelAsync(created.Id, new EndDuelRequest(DurationSeconds: 180));

        result.DurationSeconds.Should().Be(180);
    }

    [Fact]
    public async Task EndDuelAsync_AlreadyEndedDuel_ThrowsDuelAlreadyEndedException()
    {
        var created = await _service.CreateDuelAsync(TournamentId, new CreateDuelRequest(1, 2, 1));
        await _service.EndDuelAsync(created.Id, new EndDuelRequest(180));

        Func<Task> act = () => _service.EndDuelAsync(created.Id, new EndDuelRequest(200));

        await act.Should().ThrowAsync<DuelAlreadyEndedException>()
                 .Where(e => e.DuelId == created.Id);
    }

    [Fact]
    public async Task EndDuelAsync_NonExistingDuel_ThrowsDuelNotFoundException()
    {
        Func<Task> act = () => _service.EndDuelAsync(9999, new EndDuelRequest(120));

        await act.Should().ThrowAsync<DuelNotFoundException>();
    }
}
