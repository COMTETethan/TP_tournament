using Microsoft.AspNetCore.Mvc;
using Tournament.Api.Contracts;
using Tournament.Api.Controllers;
using Tournament.Api.DTOs.Responses;
using Tournament.Api.Exceptions;

namespace Tournament.Api.UnitTests.Controllers;

public class SkillsControllerTests
{
    private readonly Mock<IClassService> _mockService = new();
    private readonly SkillsController _controller;

    public SkillsControllerTests()
    {
        _controller = new SkillsController(_mockService.Object);
    }

    // ── GET /api/skills/{id} ───────────────────────────────────

    [Fact]
    public async Task GetSkill_ExistingId_ReturnsOk()
    {
        var response = new SkillResponse(4, 1, "War Cry", "AURA", 10, 3, "ATTACK_UP", "desc");
        _mockService.Setup(s => s.GetSkillAsync(4)).ReturnsAsync(response);

        var result = await _controller.GetSkill(4);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        ok.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task GetSkill_UnknownId_ReturnsNotFound()
    {
        _mockService.Setup(s => s.GetSkillAsync(9999)).ThrowsAsync(new SkillNotFoundException(9999));

        var result = await _controller.GetSkill(9999);

        result.Should().BeOfType<NotFoundObjectResult>().Which.StatusCode.Should().Be(404);
    }
}
