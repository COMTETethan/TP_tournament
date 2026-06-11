using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "Season")]
[Trait("Layer", "Service")]
public class SeasonServiceTests
{
    private readonly SeasonService _service = new();

    private static readonly DateTime Start = new(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime End   = new(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

    [Fact]
    public async Task CreateSeasonAsync_ValidRequest_ReturnsSeasonWithUpcomingStatus()
    {
        // Arrange
        var request = new CreateSeasonRequest("Saison 1 — L'Aube des Chevaliers", Start, End);

        // Act
        var result = await _service.CreateSeasonAsync(request);

        // Assert
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
        // Arrange
        var request = new CreateSeasonRequest("", Start, End);

        // Act
        Func<Task> act = () => _service.CreateSeasonAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateSeasonAsync_EndBeforeStart_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateSeasonRequest("Saison invalide", End, Start);

        // Act
        Func<Task> act = () => _service.CreateSeasonAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetSeasonAsync_ExistingSeason_ReturnsSeason()
    {
        // Arrange
        var created = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Test", Start, End));

        // Act
        var result = await _service.GetSeasonAsync(created.Id);

        // Assert
        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Saison Test");
    }

    [Fact]
    public async Task GetSeasonAsync_NonExistingSeason_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetSeasonAsync(9999);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>()
                 .Where(e => e.SeasonId == 9999);
    }

    [Fact]
    public async Task GetAllSeasonsAsync_AfterCreatingTwo_ReturnsBoth()
    {
        // Arrange
        await _service.CreateSeasonAsync(new CreateSeasonRequest("S1", Start, End));
        await _service.CreateSeasonAsync(new CreateSeasonRequest("S2", Start, End));

        // Act
        var result = await _service.GetAllSeasonsAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_UpcomingToActive_ReturnsActiveSeason()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Active", Start, End));

        // Act
        var result = await _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ACTIVE"));

        // Assert
        result.Status.Should().Be("ACTIVE");
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_ActiveToEnded_ReturnsEndedSeason()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Terminée", Start, End));
        await _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ACTIVE"));

        // Act
        var result = await _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ENDED"));

        // Assert
        result.Status.Should().Be("ENDED");
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_InvalidTransition_ThrowsInvalidSeasonStatusException()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison", Start, End));

        // Act
        Func<Task> act = () => _service.UpdateSeasonStatusAsync(season.Id, new UpdateSeasonStatusRequest("ENDED"));

        // Assert
        await act.Should().ThrowAsync<InvalidSeasonStatusException>();
    }

    [Fact]
    public async Task UpdateSeasonStatusAsync_NonExistingSeason_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.UpdateSeasonStatusAsync(9999, new UpdateSeasonStatusRequest("ACTIVE"));

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>()
                 .Where(e => e.SeasonId == 9999);
    }

    [Fact]
    public async Task GetPlayerSeasonalStatsAsync_NewPlayer_ReturnsZeroStats()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Stats", Start, End));

        // Act
        var result = await _service.GetPlayerSeasonalStatsAsync(season.Id, 1);

        // Assert
        result.Should().NotBeNull();
        result.PlayerId.Should().Be(1);
        result.SeasonId.Should().Be(season.Id);
        result.TotalScore.Should().Be(0);
        result.TournamentsPlayed.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerSeasonalStatsAsync_CalledTwice_ReturnsSameStats()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Stats 2", Start, End));

        var first  = await _service.GetPlayerSeasonalStatsAsync(season.Id, 1);
        // Act
        var second = await _service.GetPlayerSeasonalStatsAsync(season.Id, 1);

        // Assert
        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task GetPlayerSeasonalStatsAsync_TwoPlayers_EachGetsOwnStats()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Multi", Start, End));

        await _service.GetPlayerSeasonalStatsAsync(season.Id, 1);
        // Act
        var p2 = await _service.GetPlayerSeasonalStatsAsync(season.Id, 2);

        // Assert
        p2.PlayerId.Should().Be(2);
        p2.TotalScore.Should().Be(0);
    }

    [Fact]
    public async Task GetPlayerSeasonalStatsAsync_TwoSeasons_EachGetsOwnStats()
    {
        // Arrange
        var s1 = await _service.CreateSeasonAsync(new CreateSeasonRequest("S1", Start, End));
        var s2 = await _service.CreateSeasonAsync(new CreateSeasonRequest("S2", Start, End));

        await _service.GetPlayerSeasonalStatsAsync(s1.Id, 1);
        // Act
        var result = await _service.GetPlayerSeasonalStatsAsync(s2.Id, 1);

        // Assert
        result.SeasonId.Should().Be(s2.Id);
        result.TotalScore.Should().Be(0);
    }

    [Fact]
    public async Task AddTournamentToSeasonAsync_ValidSeason_Completes()
    {
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison T", Start, End));

        await _service.AddTournamentToSeasonAsync(season.Id, tournamentId: 1);

    }

    [Fact]
    public async Task AddTournamentToSeasonAsync_NonExistingSeason_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.AddTournamentToSeasonAsync(9999, tournamentId: 1);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>();
    }

    [Fact]
    public async Task GetAllPlayerSeasonalStatsAsync_WithStats_ReturnsStats()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison All", Start, End));
        await _service.GetPlayerSeasonalStatsAsync(season.Id, 1);
        await _service.GetPlayerSeasonalStatsAsync(season.Id, 2);

        // Act
        var result = await _service.GetAllPlayerSeasonalStatsAsync(season.Id);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
        result.Should().OnlyContain(s => s.SeasonId == season.Id);
    }

    [Fact]
    public async Task GetAllPlayerSeasonalStatsAsync_EmptySeason_ReturnsEmpty()
    {
        // Arrange
        var season = await _service.CreateSeasonAsync(new CreateSeasonRequest("Saison Vide", Start, End));

        // Act
        var result = await _service.GetAllPlayerSeasonalStatsAsync(season.Id);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPlayerSeasonalStatsAsync_NonExistingSeason_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetAllPlayerSeasonalStatsAsync(9999);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>();
    }
}
