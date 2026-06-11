using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

[Trait("Category", "Objective")]
[Trait("Layer", "Controller")]
public class ObjectivesControllerTests
{
    private readonly Mock<IObjectiveService> _mockService = new();
    private readonly ObjectivesController _controller;

    public ObjectivesControllerTests()
    {
        _controller = new ObjectivesController(_mockService.Object);
    }

    [Fact]
    public async Task CreateObjective_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new CreateObjectiveRequest(1, "Premier sang", "desc", "WIN_DUELS", 1, 100, "NONE");
        var response = new ObjectiveResponse(1, 1, "Premier sang", "desc", "WIN_DUELS", 1, 100, "NONE");
        _mockService.Setup(s => s.CreateObjectiveAsync(request)).ReturnsAsync(response);

        // Act
        var result = await _controller.CreateObjective(request);

        // Assert
        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateObjective_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateObjectiveRequest(1, "", "desc", "WIN_DUELS", 1, 100, "NONE");
        _mockService.Setup(s => s.CreateObjectiveAsync(request))
                    .ThrowsAsync(new ArgumentException("Name cannot be empty."));

        // Act
        var result = await _controller.CreateObjective(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task GetObjective_ExistingId_ReturnsOk()
    {
        // Arrange
        var response = new ObjectiveResponse(1, 1, "Premier sang", "desc", "WIN_DUELS", 1, 100, "NONE");
        _mockService.Setup(s => s.GetObjectiveAsync(1)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetObjective(1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetObjective_NonExistingId_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetObjectiveAsync(99))
                    .ThrowsAsync(new ObjectiveNotFoundException(99));

        // Act
        var result = await _controller.GetObjective(99);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetSeasonObjectives_ReturnsOkWithList()
    {
        // Arrange
        var list = new List<ObjectiveResponse>
        {
            new(1, 1, "Obj A", "d", "WIN_DUELS",  1, 100, "NONE"),
            new(2, 1, "Obj B", "d", "WIN_STREAK", 3, 300, "DAILY"),
        };
        _mockService.Setup(s => s.GetSeasonObjectivesAsync(1)).ReturnsAsync(list);

        // Act
        var result = await _controller.GetSeasonObjectives(1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task GetPlayerProgress_ReturnsOk()
    {
        // Arrange
        var response = new PlayerObjectiveProgressResponse(1, 1, 0, false, null);
        _mockService.Setup(s => s.GetPlayerProgressAsync(1, 1, null)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayerProgress(1, 1);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPlayerProgress_WithPeriodKey_PassesPeriodKeyToService()
    {
        // Arrange
        var response = new PlayerObjectiveProgressResponse(1, 1, 0, false, null, "2026-06-11");
        _mockService.Setup(s => s.GetPlayerProgressAsync(1, 1, "2026-06-11")).ReturnsAsync(response);

        // Act
        var result = await _controller.GetPlayerProgress(1, 1, "2026-06-11");

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPlayerCompletions_ValidIds_ReturnsOk()
    {
        // Arrange
        var completions = new List<PlayerObjectiveCompletionResponse>
        {
            new(1, 1, 1, DateTime.UtcNow, 100, null),
            new(2, 1, 1, DateTime.UtcNow, 100, "2026-06-11"),
        };
        _mockService.Setup(s => s.GetPlayerCompletionsAsync(1, 1)).ReturnsAsync(completions);

        // Act
        var result = await _controller.GetPlayerCompletions(1, 1);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(completions);
    }

    [Fact]
    public async Task GetPlayerCompletions_ObjectiveNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetPlayerCompletionsAsync(99, 1))
                    .ThrowsAsync(new ObjectiveNotFoundException(99));

        // Act
        var result = await _controller.GetPlayerCompletions(99, 1);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdatePlayerProgress_BelowTarget_ReturnsOkNotCompleted()
    {
        // Arrange
        var request  = new UpdateObjectiveProgressRequest(2);
        var response = new PlayerObjectiveProgressResponse(1, 1, 2, false, null);
        _mockService.Setup(s => s.UpdatePlayerProgressAsync(1, 1, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.UpdatePlayerProgress(1, 1, request);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var data = ok.Value.Should().BeOfType<PlayerObjectiveProgressResponse>().Subject;
        data.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePlayerProgress_NonExistingObjective_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateObjectiveProgressRequest(1);
        _mockService.Setup(s => s.UpdatePlayerProgressAsync(99, 1, request))
                    .ThrowsAsync(new ObjectiveNotFoundException(99));

        // Act
        var result = await _controller.UpdatePlayerProgress(99, 1, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
