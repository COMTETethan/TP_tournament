using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class PlayersControllerTests
{
    private readonly Mock<IPlayerService> _mockService = new();
    private readonly PlayersController _controller;

    public PlayersControllerTests()
    {
        _controller = new PlayersController(_mockService.Object);
    }

    // ── POST /api/tournaments/{tournamentId}/players ───────────

    [Fact]
    public async Task AddPlayer_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreatePlayerRequest("Sir Galahad");
        var response = new PlayerResponse(1, 1, "Sir Galahad", false, 0);
        _mockService.Setup(s => s.AddPlayerAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddPlayer(1, request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task AddPlayer_TournamentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CreatePlayerRequest("Sir Galahad");
        _mockService.Setup(s => s.AddPlayerAsync(99, request))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.AddPlayer(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/tournaments/{tournamentId}/players ────────────

    [Fact]
    public async Task GetTournamentPlayers_ExistingTournament_ReturnsOk()
    {
        // Arrange
        var players = new List<PlayerResponse>
        {
            new(1, 1, "Sir Galahad",    false, 0),
            new(2, 1, "Dame Morgane",   false, 0),
            new(3, 1, "Chevalier Noir", false, 5),
        };
        _mockService.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(players);

        // Act
        var result = await _controller.GetTournamentPlayers(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(players);
    }

    // ── GET /api/players/{id} ──────────────────────────────────

    [Fact]
    public async Task GetPlayer_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new PlayerResponse(1, 1, "Sir Galahad", false, 0);
        _mockService.Setup(s => s.GetPlayerAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayer(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetPlayer_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetPlayerAsync(99))
                    .ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.GetPlayer(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ── POST /api/players/{id}/disqualify ──────────────────────

    [Fact]
    public async Task DisqualifyPlayer_ExistingPlayer_ReturnsOk()
    {
        // Arrange
        var response = new PlayerResponse(1, 1, "Sir Galahad", true, 0);
        _mockService.Setup(s => s.DisqualifyPlayerAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.DisqualifyPlayer(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.As<PlayerResponse>().IsDisqualified.Should().BeTrue();
    }

    // ── PATCH /api/players/{id}/penalties ─────────────────────

    [Fact]
    public async Task AddPenalty_ValidPenalty_ReturnsOk()
    {
        // Arrange
        var request  = new AddPenaltyRequest(5);
        var response = new PlayerResponse(1, 1, "Sir Galahad", false, 5);
        _mockService.Setup(s => s.AddPenaltyAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddPenalty(1, request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.As<PlayerResponse>().PenaltyPoints.Should().Be(5);
    }

    [Fact]
    public async Task AddPlayer_InvalidName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreatePlayerRequest("");
        _mockService.Setup(s => s.AddPlayerAsync(1, request))
                    .ThrowsAsync(new ArgumentException("Name cannot be empty."));

        // Act
        var result = await _controller.AddPlayer(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTournamentPlayers_NonExistingTournament_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetTournamentPlayersAsync(99))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.GetTournamentPlayers(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DisqualifyPlayer_NonExistingPlayer_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.DisqualifyPlayerAsync(99))
                    .ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.DisqualifyPlayer(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddPenalty_NonExistingPlayer_ReturnsNotFound()
    {
        // Arrange
        var request = new AddPenaltyRequest(5);
        _mockService.Setup(s => s.AddPenaltyAsync(99, request))
                    .ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.AddPenalty(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AddPenalty_InvalidPenaltyPoints_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddPenaltyRequest(-1);
        _mockService.Setup(s => s.AddPenaltyAsync(1, request))
                    .ThrowsAsync(new ArgumentException("Penalty must be positive."));

        // Act
        var result = await _controller.AddPenalty(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
