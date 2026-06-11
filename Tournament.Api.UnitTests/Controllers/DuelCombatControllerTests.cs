using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "DuelCombat")]
[Trait("Layer", "Controller")]
public class DuelCombatControllerTests
{
    private readonly Mock<IDuelCombatService> _mockService = new();
    private readonly DuelCombatController _controller;

    public DuelCombatControllerTests()
    {
        _controller = new DuelCombatController(_mockService.Object);
    }

    private static DuelCombatResponse FakeDuelCombat(string status = "IN_PROGRESS", int? winnerSlot = null, int? winnerPlayerId = null, string? outcome = null)
    {
        var c1 = new CombatantState(1, "Arthur", 1, 2, 120, 120, false, new List<ActiveEffectState>());
        var c2 = new CombatantState(2, "Mordred", 5, 1, 110, 110, false, new List<ActiveEffectState>());
        var combat = new CombatResponse(7, status, 1, winnerSlot, c1, c2, new List<CombatLogEntry>(), DateTime.UtcNow);
        return new DuelCombatResponse(3, 1, 7, status, winnerSlot, winnerPlayerId, outcome, combat);
    }

    [Fact]
    public async Task StartCombatForDuel_Valid_ReturnsCreated()
    {
        // Arrange
        _mockService.Setup(s => s.StartFromDuelAsync(3)).ReturnsAsync(FakeDuelCombat());

        // Act
        var result = await _controller.StartCombatForDuel(3);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>().Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task StartCombatForDuel_UnknownDuel_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.StartFromDuelAsync(99)).ThrowsAsync(new DuelNotFoundException(99));

        // Act
        var result = await _controller.StartCombatForDuel(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task StartCombatForDuel_PlayerWithoutClass_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.StartFromDuelAsync(3))
                    .ThrowsAsync(new InvalidCombatActionException("Player has no class."));

        // Act
        var result = await _controller.StartCombatForDuel(3);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetDuelCombat_Existing_ReturnsOk()
    {
        // Arrange
        _mockService.Setup(s => s.GetByDuelAsync(3)).ReturnsAsync(FakeDuelCombat());

        // Act
        var result = await _controller.GetDuelCombat(3);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetDuelCombat_NoCombatStarted_ReturnsBadRequest()
    {
        // Arrange
        _mockService.Setup(s => s.GetByDuelAsync(3))
                    .ThrowsAsync(new InvalidCombatActionException("No combat started."));

        // Act
        var result = await _controller.GetDuelCombat(3);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task SubmitAction_Valid_ReturnsOk()
    {
        // Arrange
        var request = new SubmitActionRequest(1, 1);
        _mockService.Setup(s => s.SubmitActionAsync(3, request)).ReturnsAsync(FakeDuelCombat());

        // Act
        var result = await _controller.SubmitAction(3, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SubmitAction_UnknownSkill_ReturnsNotFound()
    {
        // Arrange
        var request = new SubmitActionRequest(1, 9999);
        _mockService.Setup(s => s.SubmitActionAsync(3, request)).ThrowsAsync(new SkillNotFoundException(9999));

        // Act
        var result = await _controller.SubmitAction(3, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SubmitAction_Invalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new SubmitActionRequest(3, 1);
        _mockService.Setup(s => s.SubmitActionAsync(3, request))
                    .ThrowsAsync(new InvalidCombatActionException("Invalid slot."));

        // Act
        var result = await _controller.SubmitAction(3, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Forfeit_Valid_ReturnsOk()
    {
        // Arrange
        var request = new ForfeitRequest(1);
        _mockService.Setup(s => s.ForfeitAsync(3, request))
                    .ReturnsAsync(FakeDuelCombat("COMPLETED", 2, 2, "PLAYER2_WIN"));

        // Act
        var result = await _controller.Forfeit(3, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Forfeit_UnknownDuel_ReturnsNotFound()
    {
        // Arrange
        var request = new ForfeitRequest(1);
        _mockService.Setup(s => s.ForfeitAsync(99, request)).ThrowsAsync(new DuelNotFoundException(99));

        // Act
        var result = await _controller.Forfeit(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetReplay_Existing_ReturnsOk()
    {
        // Arrange
        var replay = new CombatReplayResponse(7, "COMPLETED", 1, 5,
            new ReplayChampion(1, "Arthur", 1, "Knight", 2, 120),
            new ReplayChampion(2, "Mordred", 5, "Berserker", 1, 110),
            DateTime.UtcNow, DateTime.UtcNow, new List<CombatEventResponse>());
        _mockService.Setup(s => s.GetReplayByDuelAsync(3)).ReturnsAsync(replay);

        // Act
        var result = await _controller.GetReplay(3);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetReplay_NoCombat_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetReplayByDuelAsync(3)).ThrowsAsync(new CombatNotFoundException(7));

        // Act
        var result = await _controller.GetReplay(3);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
