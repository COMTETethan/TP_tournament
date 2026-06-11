using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Season")]
[Trait("Layer", "Controller")]
public class SeasonsControllerTests
{
    private readonly Mock<ISeasonService> _mockService = new();
    private readonly SeasonsController _controller;

    private static readonly DateTime Start = new(2026, 7,  1,  0,  0,  0, DateTimeKind.Utc);
    private static readonly DateTime End   = new(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc);

    public SeasonsControllerTests()
    {
        _controller = new SeasonsController(_mockService.Object);
    }

    [Fact]
    public async Task CreateSeason_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreateSeasonRequest("Saison 1", Start, End);
        var response = new SeasonResponse(1, "Saison 1", "UPCOMING", Start, End, DateTime.UtcNow);
        _mockService.Setup(s => s.CreateSeasonAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.CreateSeason(request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateSeason_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateSeasonRequest("", Start, End);
        _mockService.Setup(s => s.CreateSeasonAsync(request))
                    .ThrowsAsync(new ArgumentException("Name cannot be empty."));

        // Act
        var result = await _controller.CreateSeason(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetSeason_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new SeasonResponse(1, "Saison 1", "UPCOMING", Start, End, DateTime.UtcNow);
        _mockService.Setup(s => s.GetSeasonAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetSeason(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetSeason_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetSeasonAsync(99))
                    .ThrowsAsync(new SeasonNotFoundException(99));

        // Act
        var result = await _controller.GetSeason(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetAllSeasons_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<SeasonResponse>
        {
            new(1, "S1", "UPCOMING", Start, End, DateTime.UtcNow),
            new(2, "S2", "ACTIVE",   Start, End, DateTime.UtcNow),
        };
        _mockService.Setup(s => s.GetAllSeasonsAsync()).ReturnsAsync(list);

        // Act
        var result = await _controller.GetAllSeasons();

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task UpdateSeasonStatus_ValidTransition_ReturnsOk()
    {
        // Arrange
        var request  = new UpdateSeasonStatusRequest("ACTIVE");
        var response = new SeasonResponse(1, "S1", "ACTIVE", Start, End, DateTime.UtcNow);
        _mockService.Setup(s => s.UpdateSeasonStatusAsync(1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateSeasonStatus(1, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task UpdateSeasonStatus_InvalidTransition_ReturnsBadRequest()
    {
        // Arrange
        var request = new UpdateSeasonStatusRequest("ENDED");
        _mockService.Setup(s => s.UpdateSeasonStatusAsync(1, request))
                    .ThrowsAsync(new InvalidSeasonStatusException("ENDED"));

        // Act
        var result = await _controller.UpdateSeasonStatus(1, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task AddTournamentToSeason_ValidIds_ReturnsNoContent()
    {
        // Arrange
        _mockService.Setup(s => s.AddTournamentToSeasonAsync(1, 1)).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.AddTournamentToSeason(1, 1);

        // Assert
        result.Should().BeOfType<NoContentResult>().Which.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task AddTournamentToSeason_NonExistingSeason_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.AddTournamentToSeasonAsync(99, 1))
                    .ThrowsAsync(new SeasonNotFoundException(99));

        // Act
        var result = await _controller.AddTournamentToSeason(99, 1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetPlayerSeasonalStats_ExistingPlayerAndSeason_ReturnsOk()
    {
        // Arrange
        var response = new SeasonalStatsResponse(1, 1, 0, 0, 0, 0, 0, 0, null);
        _mockService.Setup(s => s.GetPlayerSeasonalStatsAsync(1, 1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayerSeasonalStats(1, 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }
}
