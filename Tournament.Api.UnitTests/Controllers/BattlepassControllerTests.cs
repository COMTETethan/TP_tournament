using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Battlepass")]
[Trait("Layer", "Controller")]
public class BattlepassControllerTests
{
    private readonly Mock<IBattlepassService> _mockService = new();
    private readonly BattlepassController _controller;

    public BattlepassControllerTests()
    {
        _controller = new BattlepassController(_mockService.Object);
    }

    [Fact]
    public async Task CreateBattlepass_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreateBattlepassRequest(SeasonId: 1, TotalTiers: 100, HasPremiumTrack: true);
        var response = new BattlepassResponse(1, 1, 100, true);
        _mockService.Setup(s => s.CreateBattlepassAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.CreateBattlepass(request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateBattlepass_ZeroTiers_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateBattlepassRequest(1, 0, false);
        _mockService.Setup(s => s.CreateBattlepassAsync(request))
                    .ThrowsAsync(new ArgumentException("TotalTiers must be > 0."));

        // Act
        var result = await _controller.CreateBattlepass(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetBattlepassBySeason_Existing_ReturnsOk()
    {
        // Arrange
        var response = new BattlepassResponse(1, 1, 100, true);
        _mockService.Setup(s => s.GetBattlepassBySeasonAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetBattlepassBySeason(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetBattlepassBySeason_NotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetBattlepassBySeasonAsync(99))
                    .ThrowsAsync(new BattlepassNotFoundException(99));

        // Act
        var result = await _controller.GetBattlepassBySeason(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task AddTier_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new AddBattlepassTierRequest(10, 1000, false, "TITLE", "{\"title\":\"Écuyer\"}");
        var response = new BattlepassTierResponse(1, 1, 10, 1000, false, "TITLE", "{\"title\":\"Écuyer\"}");
        _mockService.Setup(s => s.AddTierAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddTier(1, request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task AddTier_NonExistingBattlepass_ReturnsNotFound()
    {
        // Arrange
        var request = new AddBattlepassTierRequest(1, 100, false, "TITLE", "{}");
        _mockService.Setup(s => s.AddTierAsync(99, request))
                    .ThrowsAsync(new BattlepassNotFoundException(99));

        // Act
        var result = await _controller.AddTier(99, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetTiers_ExistingBattlepass_ReturnsOkWithList()
    {
        // Arrange
        var tiers = new List<BattlepassTierResponse>
        {
            new(1, 1, 10, 1000, false, "TITLE", "{}"),
            new(2, 1, 25, 2500, false, "SKIN",  "{}"),
        };
        _mockService.Setup(s => s.GetTiersAsync(1)).ReturnsAsync(tiers);

        // Act
        var result = await _controller.GetTiers(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(tiers);
    }

    [Fact]
    public async Task GetPlayerProgress_ReturnsOk()
    {
        // Arrange
        var response = new PlayerBattlepassProgressResponse(1, 1, 1, 0, 0, false);
        _mockService.Setup(s => s.GetPlayerProgressAsync(1, 1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayerProgress(1, 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AddXp_ValidAmount_ReturnsOk()
    {
        // Arrange
        var request  = new AddXpRequest(500);
        var response = new PlayerBattlepassProgressResponse(1, 1, 1, 500, 0, false);
        _mockService.Setup(s => s.AddXpAsync(1, 1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddXp(1, 1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AddXp_NegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddXpRequest(-10);
        _mockService.Setup(s => s.AddXpAsync(1, 1, request))
                    .ThrowsAsync(new ArgumentException("XP must be positive."));

        // Act
        var result = await _controller.AddXp(1, 1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }
}
