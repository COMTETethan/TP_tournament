using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Requests;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class CombatsControllerTests
{
    private readonly Mock<ICombatService> _mockService = new();
    private readonly CombatsController _controller;

    public CombatsControllerTests()
    {
        _controller = new CombatsController(_mockService.Object);
    }

    private static CombatResponse FakeCombat(
        int id = 1, string status = "IN_PROGRESS", int turn = 1, int? winner = null)
    {
        var c1 = new CombatantState(1, "Arthur", 1, 1, 110, 110, false, new List<ActiveEffectState>());
        var c2 = new CombatantState(2, "Merlin", 2, 1, 110, 110, false, new List<ActiveEffectState>());
        return new CombatResponse(id, status, turn, winner, c1, c2, new List<CombatLogEntry>(), DateTime.UtcNow);
    }

    // ── POST /api/combats ──────────────────────────────────────

    [Fact]
    public async Task StartCombat_ValidRequest_ReturnsCreated()
    {
        var request  = new CreateCombatRequest(new("Arthur", 1, 1), new("Merlin", 2, 1));
        var response = FakeCombat();
        _mockService.Setup(s => s.StartCombatAsync(request)).ReturnsAsync(response);

        var result = await _controller.StartCombat(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        created.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task StartCombat_UnknownClass_ReturnsNotFound()
    {
        var request = new CreateCombatRequest(new("Ghost", 999, 1), new("Merlin", 2, 1));
        _mockService.Setup(s => s.StartCombatAsync(request)).ThrowsAsync(new ClassNotFoundException(999));

        var result = await _controller.StartCombat(request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task StartCombat_InvalidArgument_ReturnsBadRequest()
    {
        var request = new CreateCombatRequest(new("", 1, 1), new("Merlin", 2, 1));
        _mockService.Setup(s => s.StartCombatAsync(request))
                    .ThrowsAsync(new ArgumentException("Champion name cannot be empty."));

        var result = await _controller.StartCombat(request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    // ── GET /api/combats ───────────────────────────────────────

    [Fact]
    public async Task GetAllCombats_ReturnsOkWithList()
    {
        var list = new List<CombatResponse> { FakeCombat() };
        _mockService.Setup(s => s.GetAllCombatsAsync()).ReturnsAsync(list);

        var result = await _controller.GetAllCombats();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(list);
    }

    // ── GET /api/combats/{id} ──────────────────────────────────

    [Fact]
    public async Task GetCombat_ExistingId_ReturnsOk()
    {
        _mockService.Setup(s => s.GetCombatAsync(1)).ReturnsAsync(FakeCombat());

        var result = await _controller.GetCombat(1);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetCombat_UnknownId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetCombatAsync(99)).ThrowsAsync(new CombatNotFoundException(99));

        var result = await _controller.GetCombat(99);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    // ── POST /api/combats/{id}/actions ─────────────────────────

    [Fact]
    public async Task SubmitAction_ValidAction_ReturnsOk()
    {
        var request = new SubmitActionRequest(1, 1);
        _mockService.Setup(s => s.SubmitActionAsync(1, request)).ReturnsAsync(FakeCombat());

        var result = await _controller.SubmitAction(1, request);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task SubmitAction_UnknownCombat_ReturnsNotFound()
    {
        var request = new SubmitActionRequest(1, 1);
        _mockService.Setup(s => s.SubmitActionAsync(99, request)).ThrowsAsync(new CombatNotFoundException(99));

        var result = await _controller.SubmitAction(99, request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SubmitAction_UnknownSkill_ReturnsNotFound()
    {
        var request = new SubmitActionRequest(1, 9999);
        _mockService.Setup(s => s.SubmitActionAsync(1, request)).ThrowsAsync(new SkillNotFoundException(9999));

        var result = await _controller.SubmitAction(1, request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task SubmitAction_InvalidAction_ReturnsBadRequest()
    {
        var request = new SubmitActionRequest(3, 1);
        _mockService.Setup(s => s.SubmitActionAsync(1, request))
                    .ThrowsAsync(new InvalidCombatActionException("Invalid slot 3; expected 1 or 2."));

        var result = await _controller.SubmitAction(1, request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }

    // ── POST /api/combats/{id}/forfeit ─────────────────────────

    [Fact]
    public async Task Forfeit_ValidRequest_ReturnsOk()
    {
        var request = new ForfeitRequest(1);
        _mockService.Setup(s => s.ForfeitAsync(1, request))
                    .ReturnsAsync(FakeCombat(status: "COMPLETED", winner: 2));

        var result = await _controller.Forfeit(1, request);

        result.Should().BeOfType<OkObjectResult>().Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Forfeit_UnknownCombat_ReturnsNotFound()
    {
        var request = new ForfeitRequest(1);
        _mockService.Setup(s => s.ForfeitAsync(99, request)).ThrowsAsync(new CombatNotFoundException(99));

        var result = await _controller.Forfeit(99, request);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Forfeit_AlreadyCompleted_ReturnsBadRequest()
    {
        var request = new ForfeitRequest(1);
        _mockService.Setup(s => s.ForfeitAsync(1, request))
                    .ThrowsAsync(new InvalidCombatActionException("Combat is already over."));

        var result = await _controller.Forfeit(1, request);

        result.Should().BeOfType<BadRequestObjectResult>().Which.StatusCode.Should().Be(400);
    }
}
