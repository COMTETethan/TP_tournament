using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class SeasonServiceTests
{
    private readonly SeasonService _service = new();

    private static readonly DateTime Start = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End   = new(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

    // ── CreateSeasonAsync ──────────────────────────────────────

    [Fact]
    public async Task CreateSeasonAsync_ValidRequest_ReturnsSeasonWithUpcomingStatus()
    {
        var request = new CreateSeasonRequest("Saison 1 — L'Aube des Chevaliers", Start, End);

        var result = await _service.CreateSeasonAsync(request);

        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be(request.Name);
        result.Status.Should().Be("UPCOMING");
        result.StartDate.Should().Be(Start);
        result.EndDate.Should().Be(End);
    }

    [Fact]
    public async Task CreateSeasonAsync_EmptyName_ThrowsArgumentException()
    {
        var request = new CreateSeasonRequest("", Start, End);

        Func<Task> act = () => _service.CreateSeasonAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSeasonAsync_EndBeforeStart_ThrowsArgumentException()
    {
        var request = new CreateSeasonRequest("Saison invalide", End, Start);

        Func<Task> act = () => _service.CreateSeasonAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── GetSeasonAsync ─────────────────────────────────────────

    [Fact]
    public async Task GetSeasonAsync_ExistingSeason_ReturnsSeason()
    {
        var created = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Test", Start, End));

        var result = await _service.GetSeasonAsync(created.Id);

        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Saison Test");
    }

    [Fact]
    public async Task GetSeasonAsync_NonExistingSeason_ThrowsSeasonNotFoundException()
    {
        Func<Task> act = () => _service.GetSeasonAsync(9999);

        await act.Should().ThrowAsync<SeasonNotFoundException>()
                 .Where(e => e.SeasonId == 9999);
    }

    // ── GetAllSeasonsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetAllSeasonsAsync_AfterCreatingTwo_ReturnsBoth()
    {
        await _service.CreateSeasonAsync(new CreateSeasonRequest("S1", Start, End));
        await _service.CreateSeasonAsync(new CreateSeasonRequest("S2", Start, End));

        var result = await _service.GetAllSeasonsAsync();

        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── UpdateSeasonStatusAsync ────────────────────────────────

    [Fact]
    public async Task UpdateSeasonStatusAsync_UpcomingToActive_ReturnsActiveSeason()
    {
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Active", Start, End));

        var result = await _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ACTIVE"));

        result.Status.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_ActiveToEnded_ReturnsEndedSeason()
    {
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Terminée", Start, End));
        await _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ACTIVE"));

        var result = await _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ENDED"));

        result.Status.Should().Be("ENDED");
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_InvalidTransition_ThrowsInvalidSeasonStatusException()
    {
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison", Start, End));

        // Cannot go directly UPCOMING → ENDED
        Func<Task> act = () => _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ENDED"));

        await act.Should().ThrowAsync<InvalidSeasonStatusException>();
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_NonExistingSeason_ThrowsSeasonNotFoundException()
    {
        Func<Task> act = () => _service.UpdateSeasonStatusAsync(9999, new UpdateSeasonStatusRequest("ACTIVE"));

        await act.Should().ThrowAsync<SeasonNotFoundException>()
                 .Where(e => e.SeasonId == 9999);
    }

    // ── GetPlayerSeasonalStatsAsync ────────────────────────────

    [Fact]
    public async Task GetPlayerSeasonalStatsAsync_NewPlayer_ReturnsZeroStats()
    {
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Stats", Start, End));

        var result = await _service.GetPlayerSeasonalStatsAsync(season.Id, 1);

        result.Should().NotBeNull();
        result.PlayerId.Should().Be(1);
        result.SeasonId.Should().Be(season.Id);
        result.TotalScore.Should().Be(0);
        result.TournamentsPlayed.Should().Be(0);
    }
}
