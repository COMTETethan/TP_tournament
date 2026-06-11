using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class ObjectivesControllerTests
{
    private readonly Mock<IObjectiveService> _mockService = new();
    private readonly ObjectivesController _controller;

    public ObjectivesControllerTests()
    {
        _controller = new ObjectivesController(_mockService.Object);
    }

    // ── POST /api/objectives ───────────────────────────────────

    [Fact]
    public async Task CreateObjective_ValidRequest_ReturnsCreated()
    {
        var request  = new CreateObjectiveRequest(1, "Premier sang", "desc", "WIN_DUELS", 1, 100, "NONE");
        var response = new ObjectiveResponse(1, 1, "Premier sang", "desc", "WIN_DUELS", 1, 100, "NONE");
        _mockService.Setup(s => s.CreateObjectiveAsync(request)).ReturnsAsync(response);

        var result = await _controller.CreateObjective(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task CreateObjective_EmptyName_ReturnsBadRequest()
    {
        var request = new CreateObjectiveRequest(1, "", "desc", "WIN_DUELS", 1, 100, "NONE");
        _mockService.Setup(s => s.CreateObjectiveAsync(request))
                    .ThrowsAsync(new ArgumentException("Name cannot be empty."));

        var result = await _controller.CreateObjective(request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    // ── GET /api/objectives/{id} ───────────────────────────────

    [Fact]
    public async Task GetObjective_ExistingId_ReturnsOk()
    {
        var response = new ObjectiveResponse(1, 1, "Premier sang", "desc", "WIN_DUELS", 1, 100, "NONE");
        _mockService.Setup(s => s.GetObjectiveAsync(1)).ReturnsAsync(response);

        var result = await _controller.GetObjective(1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetObjective_NonExistingId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetObjectiveAsync(99))
                    .ThrowsAsync(new ObjectiveNotFoundException(99));

        var result = await _controller.GetObjective(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── GET /api/objectives/season/{seasonId} ──────────────────

    [Fact]
    public async Task GetSeasonObjectives_ReturnsOkWithList()
    {
        var list = new List<ObjectiveResponse>
        {
            new(1, 1, "Obj A", "d", "WIN_DUELS",  1, 100, "NONE"),
            new(2, 1, "Obj B", "d", "WIN_STREAK", 3, 300, "DAILY"),
        };
        _mockService.Setup(s => s.GetSeasonObjectivesAsync(1)).ReturnsAsync(list);

        var result = await _controller.GetSeasonObjectives(1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    // ── GET /api/objectives/{id}/players/{playerId}/progress ───

    [Fact]
    public async Task GetPlayerProgress_ReturnsOk()
    {
        var response = new PlayerObjectiveProgressResponse(1, 1, 0, false, null);
        _mockService.Setup(s => s.GetPlayerProgressAsync(1, 1, null)).ReturnsAsync(response);

        var result = await _controller.GetPlayerProgress(1, 1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetPlayerProgress_WithPeriodKey_PassesPeriodKeyToService()
    {
        var response = new PlayerObjectiveProgressResponse(1, 1, 0, false, null, "2026-06-11");
        _mockService.Setup(s => s.GetPlayerProgressAsync(1, 1, "2026-06-11")).ReturnsAsync(response);

        var result = await _controller.GetPlayerProgress(1, 1, "2026-06-11");

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── GET /api/objectives/{id}/players/{playerId}/completions ─

    [Fact]
    public async Task GetPlayerCompletions_ValidIds_ReturnsOk()
    {
        var completions = new List<PlayerObjectiveCompletionResponse>
        {
            new(1, 1, 1, DateTime.UtcNow, 100, null),
            new(2, 1, 1, DateTime.UtcNow, 100, "2026-06-11"),
        };
        _mockService.Setup(s => s.GetPlayerCompletionsAsync(1, 1)).ReturnsAsync(completions);

        var result = await _controller.GetPlayerCompletions(1, 1);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(completions);
    }

    [Fact]
    public async Task GetPlayerCompletions_ObjectiveNotFound_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetPlayerCompletionsAsync(99, 1))
                    .ThrowsAsync(new ObjectiveNotFoundException(99));

        var result = await _controller.GetPlayerCompletions(99, 1);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── PATCH /api/objectives/{id}/players/{playerId}/progress ─

    [Fact]
    public async Task UpdatePlayerProgress_BelowTarget_ReturnsOkNotCompleted()
    {
        var request  = new UpdateObjectiveProgressRequest(2);
        var response = new PlayerObjectiveProgressResponse(1, 1, 2, false, null);
        _mockService.Setup(s => s.UpdatePlayerProgressAsync(1, 1, request)).ReturnsAsync(response);

        var result = await _controller.UpdatePlayerProgress(1, 1, request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var data = ok.Value.Should().BeOfType<PlayerObjectiveProgressResponse>().Subject;
        data.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePlayerProgress_NonExistingObjective_ReturnsNotFound()
    {
        var request = new UpdateObjectiveProgressRequest(1);
        _mockService.Setup(s => s.UpdatePlayerProgressAsync(99, 1, request))
                    .ThrowsAsync(new ObjectiveNotFoundException(99));

        var result = await _controller.UpdatePlayerProgress(99, 1, request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
