using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class SkinsControllerTests
{
    private readonly Mock<ISkinService> _mockService = new();
    private readonly SkinsController _controller;

    public SkinsControllerTests() => _controller = new SkinsController(_mockService.Object);

    // ── CreateSkin ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSkin_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreateSkinRequest("PLAYER", "Dark Knight", "skins/player/dark_knight");
        var response = new SkinResponse(1, "PLAYER", "Dark Knight", "skins/player/dark_knight", false, true, DateTime.UtcNow);
        _mockService.Setup(s => s.CreateSkinAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.CreateSkin(request);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateSkin_InvalidCategory_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSkinRequest("WEAPON", "Sword", "skins/weapon/sword");
        _mockService.Setup(s => s.CreateSkinAsync(request))
                    .ThrowsAsync(new InvalidSkinCategoryException("WEAPON"));

        // Act
        var result = await _controller.CreateSkin(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetAllSkins ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSkins_ReturnsOkWithList()
    {
        // Arrange
        var skins = new List<SkinResponse>
        {
            new(1, "PLAYER",     "Classic Knight", "skins/player/classic", false, true, DateTime.UtcNow),
            new(2, "BACKGROUND", "Stone Arena",    "skins/bg/stone",       false, true, DateTime.UtcNow),
        };
        _mockService.Setup(s => s.GetAllSkinsAsync()).ReturnsAsync(skins);

        // Act
        var result = await _controller.GetAllSkins();

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── GetSkin ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSkin_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new SkinResponse(1, "PLAYER", "Classic Knight", "skins/player/classic", false, true, DateTime.UtcNow);
        _mockService.Setup(s => s.GetSkinAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetSkin(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetSkin_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetSkinAsync(999))
                    .ThrowsAsync(new SkinNotFoundException(999));

        // Act
        var result = await _controller.GetSkin(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── DeactivateSkin ─────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateSkin_ExistingId_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(s => s.DeactivateSkinAsync(1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeactivateSkin(1);

        // Assert
        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
    }

    // ── EquipPlayerSkin ────────────────────────────────────────────────────

    [Fact]
    public async Task EquipPlayerSkin_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request  = new EquipPlayerSkinRequest(1);
        var response = new PlayerLoadoutResponse(5, 1, "Classic Knight", "skins/player/classic");
        _mockService.Setup(s => s.EquipPlayerSkinAsync(5, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.EquipPlayerSkin(5, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── GetPlayerLoadout ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPlayerLoadout_ExistingPlayer_ReturnsOk()
    {
        // Arrange
        var response = new PlayerLoadoutResponse(5, 1, "Classic Knight", "skins/player/classic");
        _mockService.Setup(s => s.GetPlayerLoadoutAsync(5)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayerLoadout(5);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── SetTournamentBackground ────────────────────────────────────────────

    [Fact]
    public async Task SetTournamentBackground_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request  = new SetTournamentBackgroundRequest(3);
        var response = new TournamentBackgroundResponse(10, 3, "Stone Arena", "skins/bg/stone");
        _mockService.Setup(s => s.SetTournamentBackgroundAsync(10, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.SetTournamentBackground(10, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── GetTournamentBackground ────────────────────────────────────────────

    [Fact]
    public async Task GetTournamentBackground_ExistingTournament_ReturnsOk()
    {
        // Arrange
        var response = new TournamentBackgroundResponse(10, 3, "Stone Arena", "skins/bg/stone");
        _mockService.Setup(s => s.GetTournamentBackgroundAsync(10)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetTournamentBackground(10);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task DeactivateSkin_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.DeactivateSkinAsync(999))
                    .ThrowsAsync(new SkinNotFoundException(999));

        // Act
        var result = await _controller.DeactivateSkin(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EquipPlayerSkin_PlayerNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new EquipPlayerSkinRequest(1);
        _mockService.Setup(s => s.EquipPlayerSkinAsync(99, request))
                    .ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.EquipPlayerSkin(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EquipPlayerSkin_SkinNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new EquipPlayerSkinRequest(999);
        _mockService.Setup(s => s.EquipPlayerSkinAsync(1, request))
                    .ThrowsAsync(new SkinNotFoundException(999));

        // Act
        var result = await _controller.EquipPlayerSkin(1, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task EquipPlayerSkin_WrongCategory_ReturnsBadRequest()
    {
        // Arrange
        var request = new EquipPlayerSkinRequest(3);
        _mockService.Setup(s => s.EquipPlayerSkinAsync(1, request))
                    .ThrowsAsync(new InvalidSkinCategoryException("BACKGROUND"));

        // Act
        var result = await _controller.EquipPlayerSkin(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPlayerLoadout_NonExistingPlayer_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetPlayerLoadoutAsync(99))
                    .ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.GetPlayerLoadout(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetTournamentBackground_TournamentNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new SetTournamentBackgroundRequest(3);
        _mockService.Setup(s => s.SetTournamentBackgroundAsync(99, request))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.SetTournamentBackground(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetTournamentBackground_SkinNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new SetTournamentBackgroundRequest(999);
        _mockService.Setup(s => s.SetTournamentBackgroundAsync(1, request))
                    .ThrowsAsync(new SkinNotFoundException(999));

        // Act
        var result = await _controller.SetTournamentBackground(1, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetTournamentBackground_WrongCategory_ReturnsBadRequest()
    {
        // Arrange
        var request = new SetTournamentBackgroundRequest(1);
        _mockService.Setup(s => s.SetTournamentBackgroundAsync(1, request))
                    .ThrowsAsync(new InvalidSkinCategoryException("PLAYER"));

        // Act
        var result = await _controller.SetTournamentBackground(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetTournamentBackground_NonExistingTournament_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetTournamentBackgroundAsync(99))
                    .ThrowsAsync(new TournamentNotFoundException(99));

        // Act
        var result = await _controller.GetTournamentBackground(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
