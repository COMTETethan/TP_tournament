using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;

namespace Tournament.Api.UnitTests.Services;

public class TournamentServiceTests
{
    private readonly TournamentService _service = new();

    // ── CreateTournamentAsync ──────────────────────────────────

    [Fact]
    public async Task CreateTournamentAsync_ValidName_ReturnsTournamentWithOpenStatus()
    {
        // Arrange
        var request = new CreateTournamentRequest("Grand Tournament");

        // Act
        var result = await _service.CreateTournamentAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Grand Tournament");
        result.Status.Should().Be("OPEN");
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateTournamentAsync_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var request = new CreateTournamentRequest("");

        // Act
        Func<Task> act = () => _service.CreateTournamentAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*Name*");
    }

    // ── GetTournamentAsync ─────────────────────────────────────

    [Fact]
    public async Task GetTournamentAsync_ExistingId_ReturnsTournamentResponse()
    {
        // Arrange — create first, then retrieve
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Test"));

        // Act
        var result = await _service.GetTournamentAsync(created.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(created.Id);
        result.Name.Should().Be("Test");
    }

    [Fact]
    public async Task GetTournamentAsync_NonExistingId_ThrowsTournamentNotFoundException()
    {
        // Arrange
        const int nonExistingId = 9999;

        // Act
        Func<Task> act = () => _service.GetTournamentAsync(nonExistingId);

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == nonExistingId);
    }

    // ── GetAllTournamentsAsync ─────────────────────────────────

    [Fact]
    public async Task GetAllTournamentsAsync_AfterCreatingTwo_ReturnsBothTournaments()
    {
        // Arrange
        await _service.CreateTournamentAsync(new CreateTournamentRequest("A"));
        await _service.CreateTournamentAsync(new CreateTournamentRequest("B"));

        // Act
        var result = await _service.GetAllTournamentsAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── UpdateTournamentStatusAsync ────────────────────────────

    [Fact]
    public async Task UpdateTournamentStatusAsync_ValidStatus_UpdatesAndReturnsNewStatus()
    {
        // Arrange
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Test"));
        var request = new UpdateTournamentStatusRequest("IN_PROGRESS");

        // Act
        var result = await _service.UpdateTournamentStatusAsync(created.Id, request);

        // Assert
        result.Status.Should().Be("IN_PROGRESS");
        result.Id.Should().Be(created.Id);
    }

    [Fact]
    public async Task UpdateTournamentStatusAsync_InvalidStatus_ThrowsInvalidTournamentStatusException()
    {
        // Arrange
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Test"));
        var request = new UpdateTournamentStatusRequest("BANANA");

        // Act
        Func<Task> act = () => _service.UpdateTournamentStatusAsync(created.Id, request);

        // Assert
        await act.Should().ThrowAsync<InvalidTournamentStatusException>()
                 .Where(e => e.AttemptedStatus == "BANANA");
    }

    [Fact]
    public async Task UpdateTournamentStatusAsync_NonExistingTournament_ThrowsTournamentNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.UpdateTournamentStatusAsync(9999, new UpdateTournamentStatusRequest("IN_PROGRESS"));

        // Assert
        await act.Should().ThrowAsync<TournamentNotFoundException>()
                 .Where(e => e.TournamentId == 9999);
    }

    [Fact]
    public async Task UpdateTournamentStatusAsync_EmptyStatus_ThrowsInvalidTournamentStatusException()
    {
        var created = await _service.CreateTournamentAsync(new CreateTournamentRequest("Tournoi Vide"));

        Func<Task> act = () => _service.UpdateTournamentStatusAsync(created.Id, new UpdateTournamentStatusRequest(""));

        await act.Should().ThrowAsync<InvalidTournamentStatusException>();
    }
}
