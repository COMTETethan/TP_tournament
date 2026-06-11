using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class SeasonRewardsControllerTests
{
    private readonly Mock<ISeasonRewardService> _mockService = new();
    private readonly SeasonRewardsController _controller;

    public SeasonRewardsControllerTests()
    {
        _controller = new SeasonRewardsController(_mockService.Object);
    }

    // ── POST /api/seasons/{seasonId}/rewards ───────────────────

    [Fact]
    public async Task CreateSeasonReward_ValidRequest_ReturnsCreated()
    {
        var request  = new CreateSeasonRewardRequest(1, 1, "SKIN", "{}", "Champion");
        var response = new SeasonRewardResponse(1, 1, 1, 1, "SKIN", "{}", "Champion");
        _mockService.Setup(s => s.CreateSeasonRewardAsync(1, request)).ReturnsAsync(response);

        var result = await _controller.CreateSeasonReward(1, request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateSeasonReward_SeasonNotFound_ReturnsNotFound()
    {
        var request = new CreateSeasonRewardRequest(1, null, "SKIN", "{}", "Champion");
        _mockService.Setup(s => s.CreateSeasonRewardAsync(99, request))
                    .ThrowsAsync(new SeasonNotFoundException(99));

        var result = await _controller.CreateSeasonReward(99, request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task CreateSeasonReward_InvalidRank_ReturnsBadRequest()
    {
        var request = new CreateSeasonRewardRequest(0, null, "SKIN", "{}", "Champion");
        _mockService.Setup(s => s.CreateSeasonRewardAsync(1, request))
                    .ThrowsAsync(new ArgumentException("RankMin must be > 0."));

        var result = await _controller.CreateSeasonReward(1, request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    // ── GET /api/seasons/{seasonId}/rewards ────────────────────

    [Fact]
    public async Task GetSeasonRewards_ExistingSeason_ReturnsOkWithList()
    {
        var rewards = new List<SeasonRewardResponse>
        {
            new(1, 1, 1,  1,    "SKIN",  "{}", "Champion"),
            new(2, 1, 2,  10,   "TITLE", "{}", "Top 10"),
            new(3, 1, 11, null, "TITLE", "{}", "Participant"),
        };
        _mockService.Setup(s => s.GetSeasonRewardsAsync(1)).ReturnsAsync(rewards);

        var result = await _controller.GetSeasonRewards(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(rewards);
    }

    [Fact]
    public async Task GetSeasonRewards_SeasonNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetSeasonRewardsAsync(99))
                    .ThrowsAsync(new SeasonNotFoundException(99));

        var result = await _controller.GetSeasonRewards(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── POST /api/seasons/{seasonId}/rewards/distribute ────────

    [Fact]
    public async Task DistributeRewards_ValidSeason_ReturnsOkWithDistributed()
    {
        var distributed = new List<PlayerSeasonRewardResponse>
        {
            new(1, 1, 1, 1, 1, DateTime.UtcNow),
            new(2, 2, 1, 2, 2, DateTime.UtcNow),
        };
        _mockService.Setup(s => s.DistributeRewardsAsync(1)).ReturnsAsync(distributed);

        var result = await _controller.DistributeRewards(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(distributed);
    }

    [Fact]
    public async Task DistributeRewards_SeasonNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.DistributeRewardsAsync(99))
                    .ThrowsAsync(new SeasonNotFoundException(99));

        var result = await _controller.DistributeRewards(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/seasons/{seasonId}/rewards/players/{playerId} ─

    [Fact]
    public async Task GetPlayerSeasonRewards_ValidIds_ReturnsOkWithRewards()
    {
        var rewards = new List<PlayerSeasonRewardResponse>
        {
            new(1, 1, 1, 1, 1, DateTime.UtcNow),
        };
        _mockService.Setup(s => s.GetPlayerSeasonRewardsAsync(1, 1)).ReturnsAsync(rewards);

        var result = await _controller.GetPlayerSeasonRewards(1, 1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(rewards);
    }

    [Fact]
    public async Task GetPlayerSeasonRewards_SeasonNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetPlayerSeasonRewardsAsync(99, 1))
                    .ThrowsAsync(new SeasonNotFoundException(99));

        var result = await _controller.GetPlayerSeasonRewards(99, 1);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
