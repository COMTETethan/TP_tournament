using Tournament.Api.Contracts;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;
using Tournament.Api.Services;
using Tournament.Api.UnitTests.TestData;

namespace Tournament.Api.UnitTests.Services;

[Trait("Category", "SeasonReward")]
[Trait("Layer", "Service")]
public class SeasonRewardServiceTests
{
    private readonly Mock<ISeasonService> _mockSeasonService = new();
    private readonly SeasonRewardService _service;

    private static readonly SeasonResponse TestSeason = new(
        1, "Saison 1", "ACTIVE",
        new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
        new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc),
        DateTime.UtcNow);

    public SeasonRewardServiceTests()
    {
        _mockSeasonService.Setup(s => s.GetSeasonAsync(1)).ReturnsAsync(TestSeason);
        _mockSeasonService.Setup(s => s.GetSeasonAsync(99)).ThrowsAsync(new SeasonNotFoundException(99));
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(99))
                          .ThrowsAsync(new SeasonNotFoundException(99));
        _service = new SeasonRewardService(_mockSeasonService.Object);
    }

    [Fact]
    public async Task CreateSeasonRewardAsync_ValidRequest_ReturnsReward()
    {
        // Arrange
        var request = new CreateSeasonRewardRequest(1, 1, "SKIN", "{}", "Champion");

        // Act
        var result = await _service.CreateSeasonRewardAsync(1, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.SeasonId.Should().Be(1);
        result.RankMin.Should().Be(1);
        result.RankMax.Should().Be(1);
        result.Label.Should().Be("Champion");
    }

    [Fact]
    public async Task CreateSeasonRewardAsync_NullRankMax_AllowsOpenRange()
    {
        // Arrange
        var request = new CreateSeasonRewardRequest(11, null, "TITLE", "{}", "Participant");

        // Act
        var result = await _service.CreateSeasonRewardAsync(1, request);

        // Assert
        result.RankMax.Should().BeNull();
    }

    [Theory]
    [ClassData(typeof(InvalidSeasonRewardCases))]
    public async Task CreateSeasonRewardAsync_InvalidRequest_ThrowsArgumentException(
        CreateSeasonRewardRequest request, string reason)
    {
        // Act
        Func<Task> act = () => _service.CreateSeasonRewardAsync(1, request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>(reason);
    }

    [Fact]
    public async Task CreateSeasonRewardAsync_SeasonNotFound_ThrowsSeasonNotFoundException()
    {
        // Arrange
        var request = new CreateSeasonRewardRequest(1, null, "SKIN", "{}", "Champion");

        // Act
        Func<Task> act = () => _service.CreateSeasonRewardAsync(99, request);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>();
    }

    [Fact]
    public async Task GetSeasonRewardsAsync_NoRewards_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetSeasonRewardsAsync(1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSeasonRewardsAsync_WithRewards_ReturnsAll()
    {
        // Arrange
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(1, 1, "SKIN", "{}", "Champion"));
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(2, 10, "TITLE", "{}", "Top 10"));

        // Act
        var result = await _service.GetSeasonRewardsAsync(1);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSeasonRewardsAsync_SeasonNotFound_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetSeasonRewardsAsync(99);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>();
    }

    [Fact]
    public async Task DistributeRewardsAsync_NoPlayers_ReturnsEmpty()
    {
        // Arrange
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(1))
                          .ReturnsAsync(Enumerable.Empty<SeasonalStatsResponse>());

        // Act
        var result = await _service.DistributeRewardsAsync(1);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DistributeRewardsAsync_AssignsCorrectBracket()
    {
        // Arrange
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(1))
                          .ReturnsAsync(new[]
                          {
                              new SeasonalStatsResponse(1, 1, 100, 3, 2, 1, 0, 2, null),
                              new SeasonalStatsResponse(2, 1, 80,  2, 1, 1, 0, 1, null),
                          });
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(1, 1,    "SKIN",  "{}", "Champion"));
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(2, null, "TITLE", "{}", "Participant"));

        // Act
        var result = (await _service.DistributeRewardsAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Single(r => r.PlayerId == 1).SeasonRank.Should().Be(1);
        result.Single(r => r.PlayerId == 2).SeasonRank.Should().Be(2);
    }

    [Fact]
    public async Task DistributeRewardsAsync_TiedPlayers_GetSameRank()
    {
        // Arrange
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(1))
                          .ReturnsAsync(new[]
                          {
                              new SeasonalStatsResponse(1, 1, 100, 3, 2, 1, 0, 2, null),
                              new SeasonalStatsResponse(2, 1, 100, 2, 1, 1, 0, 1, null),
                          });
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(1, 2, "SKIN", "{}", "Top 2"));

        // Act
        var result = (await _service.DistributeRewardsAsync(1)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.All(r => r.SeasonRank == 1).Should().BeTrue();
    }

    [Fact]
    public async Task DistributeRewardsAsync_CalledTwice_IsIdempotent()
    {
        // Arrange
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(1))
                          .ReturnsAsync(new[] { new SeasonalStatsResponse(1, 1, 100, 3, 2, 1, 0, 2, null) });
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(1, null, "SKIN", "{}", "Champion"));

        await _service.DistributeRewardsAsync(1);
        // Act
        var secondCall = (await _service.DistributeRewardsAsync(1)).ToList();

        // Assert
        secondCall.Should().BeEmpty();
        (await _service.GetPlayerSeasonRewardsAsync(1, 1)).Should().HaveCount(1);
    }

    [Fact]
    public async Task DistributeRewardsAsync_SeasonNotFound_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.DistributeRewardsAsync(99);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>();
    }

    [Fact]
    public async Task GetPlayerSeasonRewardsAsync_AfterDistribution_ReturnsPlayerRewards()
    {
        // Arrange
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(1))
                          .ReturnsAsync(new[] { new SeasonalStatsResponse(1, 1, 100, 3, 2, 1, 0, 2, null) });
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(1, null, "SKIN", "{}", "Champion"));
        await _service.DistributeRewardsAsync(1);

        // Act
        var result = (await _service.GetPlayerSeasonRewardsAsync(1, 1)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].PlayerId.Should().Be(1);
        result[0].SeasonRank.Should().Be(1);
    }

    [Fact]
    public async Task GetPlayerSeasonRewardsAsync_BeforeDistribution_ReturnsEmpty()
    {
        // Act
        var result = await _service.GetPlayerSeasonRewardsAsync(1, 99);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPlayerSeasonRewardsAsync_SeasonNotFound_ThrowsSeasonNotFoundException()
    {
        // Act
        Func<Task> act = () => _service.GetPlayerSeasonRewardsAsync(99, 1);

        // Assert
        await act.Should().ThrowAsync<SeasonNotFoundException>();
    }

    [Fact]
    public async Task GetPlayerSeasonRewardsAsync_RewardsExistForOtherPlayer_ReturnsEmpty()
    {
        // Arrange
        _mockSeasonService.Setup(s => s.GetAllPlayerSeasonalStatsAsync(1))
                          .ReturnsAsync(new[] { new SeasonalStatsResponse(1, 1, 100, 3, 2, 1, 0, 2, null) });
        await _service.CreateSeasonRewardAsync(1, new CreateSeasonRewardRequest(1, null, "SKIN", "{}", "Champion"));
        await _service.DistributeRewardsAsync(1);

        // Act
        var result = await _service.GetPlayerSeasonRewardsAsync(1, 2);

        // Assert
        result.Should().BeEmpty();
    }
}
