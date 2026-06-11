using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Duel")]
[Trait("Layer", "Controller")]
public class DuelsControllerTests
{
    private readonly Mock<IDuelService> _mockService = new();
    private readonly DuelsController _controller;

    public DuelsControllerTests()
    {
        _controller = new DuelsController(_mockService.Object);
    }

    private static DuelResponse MakeDuel(int id = 1, string? outcome = null, int? duration = null)
        => new(id, 1, 1, 2, outcome, 1, DateTime.UtcNow, duration);

    [Fact]
    public async Task CreateDuel_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreateDuelRequest(Player1Id: 1, Player2Id: 2, DuelOrder: 1);
        var response = MakeDuel();
        _mockService.Setup(s => s.CreateDuelAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.CreateDuel(1, request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetTournamentDuels_ExistingTournament_ReturnsOk()
    {
        // Arrange
        var duels = new List<DuelResponse> { MakeDuel(1), MakeDuel(2) };
        _mockService.Setup(s => s.GetTournamentDuelsAsync(1)).ReturnsAsync(duels);

        // Act
        var result = await _controller.GetTournamentDuels(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(duels);
    }

    [Fact]
    public async Task GetDuel_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = MakeDuel();
        _mockService.Setup(s => s.GetDuelAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetDuel(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetDuel_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetDuelAsync(99))
                    .ThrowsAsync(new DuelNotFoundException(99));

        // Act
        var result = await _controller.GetDuel(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SetDuelOutcome_ValidOutcome_ReturnsOk()
    {
        // Arrange
        var request  = new SetDuelOutcomeRequest("PLAYER1_WIN");
        var response = MakeDuel(outcome: "PLAYER1_WIN");
        _mockService.Setup(s => s.SetDuelOutcomeAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.SetDuelOutcome(1, request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.As<DuelResponse>().Outcome.Should().Be("PLAYER1_WIN");
    }

    [Fact]
    public async Task EndDuel_ValidDuration_ReturnsOk()
    {
        // Arrange
        var request  = new EndDuelRequest(DurationSeconds: 180);
        var response = MakeDuel(duration: 180);
        _mockService.Setup(s => s.EndDuelAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.EndDuel(1, request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.As<DuelResponse>().DurationSeconds.Should().Be(180);
    }

    [Fact]
    public async Task EndDuel_AlreadyEnded_ReturnsBadRequest()
    {
        // Arrange
        var request = new EndDuelRequest(DurationSeconds: 200);
        _mockService.Setup(s => s.EndDuelAsync(1, request))
                    .ThrowsAsync(new DuelAlreadyEndedException(1));

        // Act
        var result = await _controller.EndDuel(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateDuel_TournamentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new CreateDuelRequest(Player1Id: 1, Player2Id: 2, DuelOrder: 1);
        _mockService.Setup(s => s.CreateDuelAsync(99, request))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.CreateDuel(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CreateDuel_SamePlayerBothSides_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateDuelRequest(Player1Id: 1, Player2Id: 1, DuelOrder: 1);
        _mockService.Setup(s => s.CreateDuelAsync(1, request))
                    .ThrowsAsync(new ArgumentException("same player"));

        // Act
        var result = await _controller.CreateDuel(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTournamentDuels_NonExistingTournament_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetTournamentDuelsAsync(99))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.GetTournamentDuels(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetDuelOutcome_NonExistingDuel_ReturnsNotFound()
    {
        // Arrange
        var request = new SetDuelOutcomeRequest("PLAYER1_WIN");
        _mockService.Setup(s => s.SetDuelOutcomeAsync(99, request))
                    .ThrowsAsync(new DuelNotFoundException(99));

        // Act
        var result = await _controller.SetDuelOutcome(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetDuelOutcome_InvalidOutcome_ReturnsBadRequest()
    {
        // Arrange
        var request = new SetDuelOutcomeRequest("BANANA");
        _mockService.Setup(s => s.SetDuelOutcomeAsync(1, request))
                    .ThrowsAsync(new ArgumentException("Invalid duel outcome."));

        // Act
        var result = await _controller.SetDuelOutcome(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EndDuel_NonExistingDuel_ReturnsNotFound()
    {
        // Arrange
        var request = new EndDuelRequest(DurationSeconds: 120);
        _mockService.Setup(s => s.EndDuelAsync(99, request))
                    .ThrowsAsync(new DuelNotFoundException(99));

        // Act
        var result = await _controller.EndDuel(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
