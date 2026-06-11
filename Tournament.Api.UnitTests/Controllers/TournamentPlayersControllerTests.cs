using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Registration")]
[Trait("Layer", "Controller")]
public class TournamentPlayersControllerTests
{
    private readonly Mock<ITournamentPlayerService> _mockService = new();
    private readonly TournamentPlayersController _controller;

    public TournamentPlayersControllerTests()
    {
        _controller = new TournamentPlayersController(_mockService.Object);
    }

    private static RegistrationResponse Reg(bool dq = false, int penalties = 0)
        => new(1, 5, "Sir Galahad", 1, 2, dq, penalties);

    [Fact]
    public async Task Register_Valid_ReturnsCreated()
    {
        // Arrange
        _mockService.Setup(s => s.RegisterAsync(1, 5)).ReturnsAsync(Reg());

        // Act
        var result = await _controller.Register(1, 5);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>().Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Register_UnknownTournament_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.RegisterAsync(99, 5)).ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.Register(99, 5);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Register_UnknownPlayer_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.RegisterAsync(1, 99)).ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.Register(1, 99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Register_AlreadyRegistered_ReturnsConflict()
    {
        // Arrange
        _mockService.Setup(s => s.RegisterAsync(1, 5)).ThrowsAsync(new PlayerAlreadyRegisteredException(1, 5));

        // Act
        var result = await _controller.Register(1, 5);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>().Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task GetTournamentPlayers_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<RegistrationResponse> { Reg(), Reg(dq: true) };
        _mockService.Setup(s => s.GetTournamentPlayersAsync(1)).ReturnsAsync(list);

        // Act
        var result = await _controller.GetTournamentPlayers(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetTournamentPlayers_UnknownTournament_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetTournamentPlayersAsync(99)).ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.GetTournamentPlayers(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Disqualify_Valid_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.DisqualifyAsync(1, 5)).ReturnsAsync(Reg(dq: true));

        // Act
        var result = await _controller.Disqualify(1, 5);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Disqualify_NotRegistered_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.DisqualifyAsync(1, 99)).ThrowsAsync(new RegistrationNotFoundException(1, 99));

        // Act
        var result = await _controller.Disqualify(1, 99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddPenalty_Valid_ReturnsOk()
    {
        // Arrange
        var request = new AddPenaltyRequest(3);
        _mockService.Setup(s => s.AddPenaltyAsync(1, 5, request)).ReturnsAsync(Reg(penalties: 3));

        // Act
        var result = await _controller.AddPenalty(1, 5, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AddPenalty_NotRegistered_ReturnsNotFound()
    {
        // Arrange
        var request = new AddPenaltyRequest(3);
        _mockService.Setup(s => s.AddPenaltyAsync(1, 99, request)).ThrowsAsync(new RegistrationNotFoundException(1, 99));

        // Act
        var result = await _controller.AddPenalty(1, 99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddPenalty_Negative_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddPenaltyRequest(-1);
        _mockService.Setup(s => s.AddPenaltyAsync(1, 5, request)).ThrowsAsync(new ArgumentException("PenaltyPoints must be non-negative."));

        // Act
        var result = await _controller.AddPenalty(1, 5, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }
}
