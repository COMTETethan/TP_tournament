using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class TournamentsControllerTests
{
    private readonly Mock<ITournamentService> _mockService = new();
    private readonly TournamentsController _controller;

    public TournamentsControllerTests()
    {
        _controller = new TournamentsController(_mockService.Object);
    }

    // ── POST /api/tournaments ──────────────────────────────────

    [Fact]
    public async Task CreateTournament_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreateTournamentRequest("Grand Tournament");
        var response = new TournamentResponse(1, "Grand Tournament", "OPEN", DateTime.UtcNow);
        _mockService.Setup(s => s.CreateTournamentAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateTournament_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateTournamentRequest("");
        _mockService.Setup(s => s.CreateTournamentAsync(request))
                    .ThrowsAsync(new ArgumentException("Name cannot be empty."));

        // Act
        var result = await _controller.CreateTournament(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    // ── GET /api/tournaments/{id} ──────────────────────────────

    [Fact]
    public async Task GetTournament_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new TournamentResponse(1, "Grand Tournament", "OPEN", DateTime.UtcNow);
        _mockService.Setup(s => s.GetTournamentAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetTournament(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetTournament_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetTournamentAsync(99))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.GetTournament(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/tournaments ───────────────────────────────────

    [Fact]
    public async Task GetAllTournaments_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<TournamentResponse>
        {
            new(1, "Tournament A", "OPEN",        DateTime.UtcNow),
            new(2, "Tournament B", "IN_PROGRESS", DateTime.UtcNow),
        };
        _mockService.Setup(s => s.GetAllTournamentsAsync()).ReturnsAsync(list);

        // Act
        var result = await _controller.GetAllTournaments();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    // ── PATCH /api/tournaments/{id}/status ────────────────────

    [Fact]
    public async Task UpdateTournamentStatus_ValidStatus_ReturnsOk()
    {
        // Arrange
        var request  = new UpdateTournamentStatusRequest("IN_PROGRESS");
        var response = new TournamentResponse(1, "Grand Tournament", "IN_PROGRESS", DateTime.UtcNow);
        _mockService.Setup(s => s.UpdateTournamentStatusAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateTournamentStatus(1, request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task UpdateTournamentStatus_InvalidStatus_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateTournamentStatusRequest("UNKNOWN");
        _mockService.Setup(s => s.UpdateTournamentStatusAsync(1, request))
                    .ThrowsAsync(new InvalidTournamentStatusException("UNKNOWN"));

        // Act
        var result = await _controller.UpdateTournamentStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task UpdateTournamentStatus_NonExistingTournament_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateTournamentStatusRequest("IN_PROGRESS");
        _mockService.Setup(s => s.UpdateTournamentStatusAsync(99, request))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.UpdateTournamentStatus(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
