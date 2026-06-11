using Microsoft.EntityFrameworkCore;
using Tournament.Api.Data;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class TournamentServiceTests
{
    private static TournamentDbContext CreateDb() => new(
        new DbContextOptionsBuilder<TournamentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private readonly TournamentDbContext _db      = CreateDb();
    private readonly TournamentService   _service;

    public TournamentServiceTests() => _service = new TournamentService(_db);

    // ── CreateTournamentAsync ──────────────────────────────────

    [Fact]
    public async Task CreateTournamentAsync_ValidName_ReturnsTournamentWithOpenStatus()
    {
        var request = new CreateTournamentRequest("Grand Tournament");

        var result = await _service.CreateTournamentAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("Grand Tournament");
        result.Status.Should().Be("OPEN");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTournamentAsync_EmptyName_ThrowsArgumentException()
    {
        var request = new CreateTournamentRequest("");

        Func<Task> act = () => _service.CreateTournamentAsync(request);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Name*");
    }

    // ── GetTournamentAsync ─────────────────────────────────────

    [Fact]
    public async Task GetTournamentAsync_ExistingId_ReturnsTournamentResponse()
    {
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Test"));

        var result = await _service.GetTournamentAsync(created.Id);

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetTournamentAsync_NonExistingId_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.GetTournamentAsync(9999);

        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    // ── GetAllTournamentsAsync ─────────────────────────────────

    [Fact]
    public async Task GetAllTournamentsAsync_AfterCreatingTwo_ReturnsBothTournaments()
    {
        await _service.CreateTournamentAsync(new CreateTournamentRequest("A"));
        await _service.CreateTournamentAsync(new CreateTournamentRequest("B"));

        var result = await _service.GetAllTournamentsAsync();

        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── UpdateTournamentStatusAsync ────────────────────────────

    [Fact]
    public async Task UpdateTournamentStatusAsync_ValidStatus_UpdatesAndReturnsNewStatus()
    {
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Test"));

        var result = await _service.UpdateTournamentStatusAsync(created.Id, new UpdateTournamentStatusRequest("IN_PROGRESS"));

        result.Status.Should().Be("IN_PROGRESS");
        result.Id.Should().Be(created.Id);
    }

    [Theory]
    [InlineData("BANANA")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("DONE")]
    public async Task UpdateTournamentStatusAsync_InvalidStatus_ThrowsInvalidTournamentStatusException(string invalidStatus)
    {
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Test"));

        Func<Task> act = () => _service.UpdateTournamentStatusAsync(created.Id, new UpdateTournamentStatusRequest(invalidStatus));

        await act.Should().ThrowAsync<InvalidTournamentStatusException>();
    }

    [Fact]
    public async Task UpdateTournamentStatusAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        Func<Task> act = () => _service.UpdateTournamentStatusAsync(9999, new UpdateTournamentStatusRequest("IN_PROGRESS"));

        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }
}
