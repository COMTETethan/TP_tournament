using Microsoft.AspNetCore.Mvc;
using Moq;
using FluentAssertions;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class ReplaysControllerTests
{
    private readonly Mock<IReplayService> _mockService = new();
    private readonly ReplaysController _controller;

    public ReplaysControllerTests() => _controller = new ReplaysController(_mockService.Object);

    // ── StartReplay ────────────────────────────────────────────────────────

    [Fact]
    public async Task StartReplay_ExistingDuel_ReturnsCreated()
    {
        // Arrange
        var response = new ReplayResponse(1, 42, 1, DateTime.UtcNow, false);
        _mockService.Setup(s => s.StartReplayAsync(42)).ReturnsAsync(response);

        // Act
        var result = await _controller.StartReplay(42);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task StartReplay_NonExistingDuel_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.StartReplayAsync(999))
                    .ThrowsAsync(new DuelNotFoundException(999));

        // Act
        var result = await _controller.StartReplay(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetReplay ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReplay_ExistingDuel_ReturnsOk()
    {
        // Arrange
        var response = new ReplayResponse(1, 42, 1, DateTime.UtcNow, true);
        _mockService.Setup(s => s.GetReplayAsync(42)).ReturnsAsync(response);

        // Act
        var result = await _controller.GetReplay(42);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetReplay_NonExistingDuel_ReturnsNotFound()
    {
        // Arrange
        _mockService.Setup(s => s.GetReplayAsync(999))
                    .ThrowsAsync(new ReplayNotFoundException(999));

        // Act
        var result = await _controller.GetReplay(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── AddEvent ───────────────────────────────────────────────────────────

    [Fact]
    public async Task AddEvent_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request  = new AddReplayEventRequest("ATTACK", 1500, 1, 2, null);
        var response = new ReplayEventResponse(1L, 1, 1, "ATTACK", 1, 2, 1500, null);
        _mockService.Setup(s => s.AddEventAsync(42, request)).ReturnsAsync(response);

        // Act
        var result = await _controller.AddEvent(42, request);

        // Assert
        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task AddEvent_InvalidEventType_ReturnsBadRequest()
    {
        // Arrange
        var request = new AddReplayEventRequest("INVALID_TYPE", 0, null, null, null);
        _mockService.Setup(s => s.AddEventAsync(42, request))
                    .ThrowsAsync(new ArgumentException("Unknown event type"));

        // Act
        var result = await _controller.AddEvent(42, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── GetEvents ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvents_ExistingDuel_ReturnsOk()
    {
        // Arrange
        var events = new List<ReplayEventResponse>
        {
            new(1L, 1, 1, "DUEL_START", null, null, 0,    null),
            new(2L, 1, 2, "ATTACK",     1,    2,    500,  null),
        };
        _mockService.Setup(s => s.GetEventsAsync(42)).ReturnsAsync(events);

        // Act
        var result = await _controller.GetEvents(42);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    // ── CompleteReplay ─────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteReplay_ExistingDuel_ReturnsOkWithIsCompleteTrue()
    {
        // Arrange
        var response = new ReplayResponse(1, 42, 1, DateTime.UtcNow, true);
        _mockService.Setup(s => s.CompleteReplayAsync(42)).ReturnsAsync(response);

        // Act
        var result = await _controller.CompleteReplay(42);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ((ReplayResponse)ok.Value!).IsComplete.Should().BeTrue();
    }

    // ── GetCosmeticSnapshot ────────────────────────────────────────────────

    [Fact]
    public async Task GetCosmeticSnapshot_ExistingDuel_ReturnsOk()
    {
        // Arrange
        var snapshot = new CosmeticSnapshotResponse(42, "Classic Knight", "skins/player/classic", null, null, "Stone Arena", "skins/bg/stone");
        _mockService.Setup(s => s.GetCosmeticSnapshotAsync(42)).ReturnsAsync(snapshot);

        // Act
        var result = await _controller.GetCosmeticSnapshot(42);

        // Assert
        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }
}
