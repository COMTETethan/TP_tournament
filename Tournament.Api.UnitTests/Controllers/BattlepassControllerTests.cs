using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class BattlepassControllerTests
{
    private readonly Mock<IBattlepassService> _mockService = new();
    private readonly BattlepassController _controller;

    public BattlepassControllerTests()
    {
        _controller = new BattlepassController(_mockService.Object);
    }

    // ── POST /api/battlepasses ─────────────────────────────────

    [Fact]
    public async Task CreateBattlepass_ValidRequest_ReturnsCreated()
    {
        var request  = new CreateBattlepassRequest(SeasonId: 1, TotalTiers: 100, HasPremiumTrack: true);
        var response = new BattlepassResponse(1, 1, 100, true);
        _mockService.Setup(s => s.CreateBattlepassAsync(request)).ReturnsAsync(response);

        var result = await _controller.CreateBattlepass(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateBattlepass_ZeroTiers_ReturnsBadRequest()
    {
        var request = new CreateBattlepassRequest(1, 0, false);
        _mockService.Setup(s => s.CreateBattlepassAsync(request))
                    .ThrowsAsync(new ArgumentException("TotalTiers must be > 0."));

        var result = await _controller.CreateBattlepass(request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    // ── GET /api/battlepasses/season/{seasonId} ────────────────

    [Fact]
    public async Task GetBattlepassBySeason_Existing_ReturnsOk()
    {
        var response = new BattlepassResponse(1, 1, 100, true);
        _mockService.Setup(s => s.GetBattlepassBySeasonAsync(1)).ReturnsAsync(response);

        var result = await _controller.GetBattlepassBySeason(1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetBattlepassBySeason_NotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetBattlepassBySeasonAsync(99))
                    .ThrowsAsync(new BattlepassNotFoundException(99));

        var result = await _controller.GetBattlepassBySeason(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── POST /api/battlepasses/{id}/tiers ──────────────────────

    [Fact]
    public async Task AddTier_ValidRequest_ReturnsCreated()
    {
        var request  = new AddBattlepassTierRequest(10, 1000, false, "TITLE", "{\"title\":\"Écuyer\"}");
        var response = new BattlepassTierResponse(1, 1, 10, 1000, false, "TITLE", "{\"title\":\"Écuyer\"}");
        _mockService.Setup(s => s.AddTierAsync(1, request)).ReturnsAsync(response);

        var result = await _controller.AddTier(1, request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task AddTier_NonExistingBattlepass_ReturnsNotFound()
    {
        var request = new AddBattlepassTierRequest(1, 100, false, "TITLE", "{}");
        _mockService.Setup(s => s.AddTierAsync(99, request))
                    .ThrowsAsync(new BattlepassNotFoundException(99));

        var result = await _controller.AddTier(99, request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/battlepasses/{id}/tiers ───────────────────────

    [Fact]
    public async Task GetTiers_ExistingBattlepass_ReturnsOkWithList()
    {
        var tiers = new List<BattlepassTierResponse>
        {
            new(1, 1, 10, 1000, false, "TITLE", "{}"),
            new(2, 1, 25, 2500, false, "SKIN",  "{}"),
        };
        _mockService.Setup(s => s.GetTiersAsync(1)).ReturnsAsync(tiers);

        var result = await _controller.GetTiers(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(tiers);
    }

    // ── GET /api/battlepasses/{id}/players/{playerId}/progress ─

    [Fact]
    public async Task GetPlayerProgress_ReturnsOk()
    {
        var response = new PlayerBattlepassProgressResponse(1, 1, 1, 0, 0, false);
        _mockService.Setup(s => s.GetPlayerProgressAsync(1, 1)).ReturnsAsync(response);

        var result = await _controller.GetPlayerProgress(1, 1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── POST /api/battlepasses/{id}/players/{playerId}/xp ──────

    [Fact]
    public async Task AddXp_ValidAmount_ReturnsOk()
    {
        var request  = new AddXpRequest(500);
        var response = new PlayerBattlepassProgressResponse(1, 1, 1, 500, 0, false);
        _mockService.Setup(s => s.AddXpAsync(1, 1, request)).ReturnsAsync(response);

        var result = await _controller.AddXp(1, 1, request);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AddXp_NegativeAmount_ReturnsBadRequest()
    {
        var request = new AddXpRequest(-10);
        _mockService.Setup(s => s.AddXpAsync(1, 1, request))
                    .ThrowsAsync(new ArgumentException("XP must be positive."));

        var result = await _controller.AddXp(1, 1, request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }
}
