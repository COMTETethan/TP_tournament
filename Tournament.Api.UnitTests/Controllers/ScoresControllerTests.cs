using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class ScoresControllerTests
{
    private readonly Mock<IScoreService> _mockService = new();
    private readonly ScoresController _controller;

    public ScoresControllerTests()
    {
        _controller = new ScoresController(_mockService.Object);
    }

    // ── GET /api/players/{playerId}/score ──────────────────────

    [Fact]
    public async Task GetPlayerScore_ExistingPlayer_ReturnsOk()
    {
        // Arrange
        var response = new PlayerScoreResponse(PlayerId: 1, PlayerName: "Sir Galahad", FinalScore: 14, IsDisqualified: false);
        _mockService.Setup(s => s.GetPlayerScoreAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayerScore(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.As<PlayerScoreResponse>().FinalScore.Should().Be(14);
    }

    [Fact]
    public async Task GetPlayerScore_NonExistingPlayer_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetPlayerScoreAsync(99))
                    .ThrowsAsync(new PlayerNotFoundException(99));

        // Act
        var result = await _controller.GetPlayerScore(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/tournaments/{tournamentId}/ranking ────────────

    [Fact]
    public async Task GetTournamentRanking_ExistingTournament_ReturnsOkWithSortedRanking()
    {
        // Arrange
        var ranking = new RankingResponse(1, new List<PlayerScoreResponse>
        {
            new(1, "Dame Morgane",   14, false),
            new(2, "Sir Galahad",    6,  false),
            new(3, "Chevalier Noir", 0,  true),
        });
        _mockService.Setup(s => s.GetTournamentRankingAsync(1)).ReturnsAsync(ranking);

        // Act
        var result = await _controller.GetTournamentRanking(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value.As<RankingResponse>();
        body.Ranking.Should().HaveCount(3);
        body.Ranking[0].FinalScore.Should().BeGreaterThan(body.Ranking[1].FinalScore);
    }

    // ── GET /api/tournaments/{tournamentId}/champion ───────────

    [Fact]
    public async Task GetTournamentChampion_ExistingTournament_ReturnsOkWithChampion()
    {
        // Arrange
        var champion = new PlayerScoreResponse(1, "Dame Morgane", 14, false);
        _mockService.Setup(s => s.GetTournamentChampionAsync(1)).ReturnsAsync(champion);

        // Act
        var result = await _controller.GetTournamentChampion(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.As<PlayerScoreResponse>().PlayerName.Should().Be("Dame Morgane");
    }
}
